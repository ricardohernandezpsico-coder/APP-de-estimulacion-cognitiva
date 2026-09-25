using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Piloto de la Fase 1 del roadmap de migración (ver NeuroVida/CLAUDE.md): "Secuencia
    /// Lumínica" completo dentro de Unity, con el mismo contrato de resultado que hoy
    /// escribe <c>SequenceGameContainer.endSession</c> del lado Android.
    ///
    /// Reglas de juego (las dio Ricardo en la cuarta/quinta pasada, no son interpretación
    /// propia; el motor <see cref="SequenceDDAEngine"/> sigue intacto y testeado 1:1 con
    /// Kotlin pero ya NO se usa en este juego):
    ///   - Tabla fija de 16+ niveles (<see cref="SequenceLevelDatabase"/>) con cuadrícula,
    ///     longitud de secuencia y velocidad (ISI) por peldaño.
    ///   - Avance: 2 aciertos consecutivos en el nivel actual -> sube 1 nivel.
    ///   - Cada error: pierde 1 corazón (visual, inmediato) Y el DDA ayuda ralentizando la
    ///     siguiente secuencia (+200ms de ISI). El nivel NO baja por un error.
    ///   - Fin de partida: 3 vidas perdidas (3 errores totales).
    ///
    /// Rediseño de interfaz (23-sep, pedido de Ricardo: "que tenga la calidad de un juego
    /// de la App Store tipo Lumosity"):
    ///   - Fichas 3D estilo "clay" (<see cref="TileSprites"/>), paleta vívida y bien separada
    ///     (<see cref="TilePalette"/>), que en reposo se ven atenuadas y al iluminarse se
    ///     encienden con un resplandor, una onda y un leve rebote.
    ///   - HUD: insignia de nivel con color por fase + nombre de la fase, vidas en píldora
    ///     (compartida con Parejas) y puntos de progreso de la secuencia (uno por paso) en
    ///     vez de una barra: durante la presentación cuentan cuántas luces hay y durante la
    ///     respuesta se llenan de verde.
    ///   - Estado en una píldora con punto de color ("Observa" / "Tu turno" / "¡Correcto!").
    ///   - Fluidez: la pantalla completa "3-2-1" abre solo la partida; entre rondas y entre
    ///     niveles todo ocurre sobre el mismo tablero (fichas que entran/salen con rebote,
    ///     aviso flotante de nivel, ola de celebración, chispas). Antes cada ronda repetía la
    ///     pantalla completa (Ricardo: "falta fluidez").
    ///   - Error: sacudida del tablero, la ficha equivocada se tiñe de rojo y se muestra
    ///     cuál era la correcta.
    /// </summary>
    public class SequenceGameController : MonoBehaviour
    {
        /// <summary>Mismo id de texto que <c>GameRegistry</c> del lado Kotlin (ver
        /// Models.kt) -- no un id numérico.</summary>
        public const string GameId = "secuencia";

        private const int MaxLives = 3;
        private const float IsiSlowdownMs = 200f; // regla anti-frustración
        private const int SafetyMaxRounds = 60; // la escalera de niveles no tiene techo natural; esto evita una sesión infinita

        // ---- Constantes de layout ----
        // 1080x1920 de referencia -> 1080/360dp = 3 unidades de Canvas por dp. Con
        // CanvasScaler en ScaleWithScreenSize, TODOS los tamaños de acá en más están en
        // estas "unidades de referencia", no en píxeles reales de pantalla.
        private const float UnitsPerDp = 3f;
        private const float MarginDp = 20f;
        private const float HudHeightDp = 128f;
        private const float BottomReservedDp = 150f; // espacio para la píldora de estado debajo de la grilla
        private const float GridPaddingDp = 16f;
        private const float GridSpacingDp = 12f;
        private const float MinCellSizeDp = 48f;   // piso real de accesibilidad Android
        private const float MaxCellSizeDp = 260f;  // cap estético para 2x2 en pantallas grandes

        private SequenceInitConfig _config;

        private int _round = 1;
        private int _correctRounds = 0;
        private int _roundsPlayed = 0;
        private int _lives;
        private int _currentLevelIndex;
        private int _consecutiveCorrectAtLevel;
        private bool _isiSlowed;
        private int _peakLevelIndex;
        private readonly List<double> _reactionTimesMs = new List<double>();
        private float _inputReadyAtTime;
        private List<int> _sequence = new List<int>();
        private List<int> _userInput = new List<int>();
        private bool _awaitingInput;
        private int _wrongTile = -1;
        private int _expectedTile = -1;

        // -- UI construida por código --
        private RectTransform _safeAreaContentRect;
        private Text _feedbackText;
        private Text _hudBadgeText;
        private Image _hudBadgeImage;
        private Text _hudPhaseText;
        private Text _hudSubText;
        private LivesHud _livesHud;
        private ExitButton _exit;
        private PhasePill _phasePill;
        private Toast _toast;
        private RectTransform _dotsRect;
        private readonly List<Image> _dots = new List<Image>();
        private Image _feedbackFlashImage;
        private RectTransform _boardPanelRect;
        private RectTransform _distractorLayerRect;
        private RectTransform _gridContainerRect;
        private RectTransform _fxLayerRect;
        private CountdownScreen _countdown;
        private GridLayoutGroup _gridLayout;
        private int _currentGridRows = -1; // -1 fuerza la primera construcción de la grilla
        private int _currentGridCols = -1;
        private float _boardContentWidth;
        private float _boardContentHeight;
        private float _cellSize;
        private readonly Dictionary<int, Button> _tileButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, Image> _tileImages = new Dictionary<int, Image>();
        private readonly Dictionary<int, Image> _tileGlows = new Dictionary<int, Image>();
        private readonly Dictionary<int, Coroutine> _tilePulseCoroutines = new Dictionary<int, Coroutine>();
        private readonly Dictionary<float, AudioClip> _toneCache = new Dictionary<float, AudioClip>();
        private AudioSource _audioSource;
        private AudioSource _distractorAudioSource; // sin AudioReverbFilter -- el drone no debe sonar "en la sala" como los tonos reales

        private static readonly Color BackgroundColor = new Color(0x0F / 255f, 0x17 / 255f, 0x2A / 255f);
        private static readonly Color PhaseBlue = new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f);
        private static readonly Color PhaseGreen = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color PhaseAmber = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color PhaseRed = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color DotPending = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color DotShown = new Color(1f, 1f, 1f, 0.90f);

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            gameObject.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Room;

            var distractorAudioGo = new GameObject("DistractorAudio");
            distractorAudioGo.transform.SetParent(transform, false);
            _distractorAudioSource = distractorAudioGo.AddComponent<AudioSource>();
            _distractorAudioSource.clip = DistractorDrone.Get();
            _distractorAudioSource.loop = true;

            BuildUi();
            gameObject.SetActive(false); // se activa desde GameEntryPoint al recibir la config real
        }

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;

            _exit.Hide();
            _round = 1;
            _correctRounds = 0;
            _roundsPlayed = 0;
            _lives = MaxLives;
            _consecutiveCorrectAtLevel = 0;
            _isiSlowed = false;
            _currentLevelIndex = SeedLevelIndex();
            _peakLevelIndex = _currentLevelIndex;
            _reactionTimesMs.Clear();
            _currentGridRows = -1; // fuerza reconstruir la grilla al tamaño inicial correcto

            StopAllCoroutines();
            StartCoroutine(BeginRound(_round, firstRound: true));
        }

        /// <summary>Punto de partida en la tabla de 16 niveles según nivel de la app +
        /// maestría entre sesiones -- mismo espíritu que el viejo SeedSpan/SeedIsiMs
        /// (Kotlin), pero mapeado a un índice de nivel en vez de un span continuo.</summary>
        private int SeedLevelIndex()
        {
            int fromAppLevel = Mathf.Clamp(_config.config.level, 1, 6);
            int fromMastery = Mathf.Clamp(_config.config.base_intensity / 20, 0, 4);
            return Mathf.Clamp(fromAppLevel + fromMastery, 1, SequenceLevelDatabase.MaxDefinedLevel);
        }

        // ---- Fases / colores por nivel ----

        private static string PhaseNameFor(int level)
        {
            if (level <= 3) return "Calentamiento";
            if (level <= 6) return "Consolidación";
            if (level <= 9) return "Reto visoespacial";
            if (level <= 12) return "Memoria de trabajo";
            return "Nivel experto";
        }

        private static Color AccentFor(int level)
        {
            if (level <= 3) return PhaseGreen;
            if (level <= 6) return PhaseBlue;
            if (level <= 9) return new Color(0xA8 / 255f, 0x55 / 255f, 0xF7 / 255f);
            if (level <= 12) return new Color(0xF9 / 255f, 0x73 / 255f, 0x16 / 255f);
            return new Color(0xEC / 255f, 0x48 / 255f, 0x99 / 255f);
        }

        // ---- Flujo de ronda ----

        /// <summary>Arma la ronda: la primera abre con la pantalla completa "3-2-1" (única
        /// pantalla completa de la partida); las siguientes ocurren sobre el mismo tablero,
        /// con las fichas saliendo/entrando si la grilla cambia de tamaño.</summary>
        private IEnumerator BeginRound(int roundNumber, bool firstRound)
        {
            var level = SequenceLevelDatabase.Get(_currentLevelIndex);

            if (firstRound)
            {
                EnsureGridDimension(level.GridRows, level.GridCols);
                UpdateHud();
                _safeAreaContentRect.gameObject.SetActive(false);
                yield return StartCoroutine(_countdown.Play(
                    $"Nivel {_currentLevelIndex}",
                    "¿Listos?",
                    () =>
                    {
                        _safeAreaContentRect.gameObject.SetActive(true);
                        StartCoroutine(AnimateTilesIn());
                    }));
                _safeAreaContentRect.gameObject.SetActive(true);
            }
            else
            {
                bool gridChanges = level.GridRows != _currentGridRows || level.GridCols != _currentGridCols;
                if (gridChanges) yield return StartCoroutine(AnimateTilesOut());
                EnsureGridDimension(level.GridRows, level.GridCols);
                UpdateHud();
                if (gridChanges)
                {
                    StartCoroutine(AnimateTilesIn());
                    yield return new WaitForSeconds(0.35f);
                }
            }

            yield return StartCoroutine(PresentSequence(roundNumber, level));
        }

        private static int DistractorCountFor(SequenceLevelConfig level)
        {
            // El spec de 16 niveles solo dice SI hay distractor visual por nivel, no
            // cuántos -- reutiliza la misma escala 0-4 que tenía el motor DDA viejo.
            if (!level.HasVisualDistractor) return 0;
            return Mathf.Clamp(level.LevelIndex - 10, 1, 4);
        }

        /// <summary>Prende/apaga el zumbido de fondo (ver <see cref="DistractorDrone"/>);
        /// suena durante presentación Y respuesta del nivel actual si <c>HasAudioDistractor</c>;
        /// se apaga entre rondas.</summary>
        private void SetAudioDistractor(bool active)
        {
            if (active && !_distractorAudioSource.isPlaying) _distractorAudioSource.Play();
            else if (!active && _distractorAudioSource.isPlaying) _distractorAudioSource.Stop();
        }

        private IEnumerator PresentSequence(int roundNumber, SequenceLevelConfig level)
        {
            int cellCount = level.TotalTiles;
            _sequence = Enumerable.Range(0, level.SequenceLength)
                .Select(_ => Random.Range(0, cellCount))
                .ToList();
            _userInput.Clear();
            _awaitingInput = false;
            _wrongTile = -1;
            _expectedTile = -1;
            SetTilesInteractable(false);
            BuildDots(_sequence.Count);
            RegenerateDistractors(roundNumber, DistractorCountFor(level));
            SetAudioDistractor(level.HasAudioDistractor);

            _feedbackText.text = "";
            _phasePill.Set($"Observa la secuencia · {_sequence.Count} luces", PhaseBlue);
            yield return new WaitForSeconds(0.6f);

            float isiMs = level.IsiMs + (_isiSlowed ? IsiSlowdownMs : 0f);
            float litMs = isiMs * 0.6f;
            float gapMs = isiMs - litMs;
            for (int i = 0; i < _sequence.Count; i++)
            {
                int tileIndex = _sequence[i];
                SetDotState(i, DotShown, pop: true);
                SetTileGlow(tileIndex, true);
                PlayConcordantTone(TilePalette.Get(tileIndex).ToneHz);
                yield return new WaitForSeconds(litMs / 1000f);
                SetTileGlow(tileIndex, false);
                yield return new WaitForSeconds(gapMs / 1000f);
            }

            _phasePill.Set("Prepárate", PhaseAmber);
            yield return new WaitForSeconds(0.4f);

            ResetDots();
            _phasePill.Set("Tu turno · repite la secuencia", PhaseGreen);
            _awaitingInput = true;
            _inputReadyAtTime = Time.unscaledTime;
            SetTilesInteractable(true);
        }

        private void OnTileTapped(int tileIndex)
        {
            if (!_awaitingInput) return;

            float now = Time.unscaledTime;
            double reactionMs = System.Math.Max(0, (now - _inputReadyAtTime) * 1000.0);
            _reactionTimesMs.Add(reactionMs);
            _inputReadyAtTime = now;

            PlayConcordantTone(TilePalette.Get(tileIndex).ToneHz);

            var expected = _sequence.ElementAtOrDefault(_userInput.Count);
            bool? roundResult = null;

            if (expected != tileIndex)
            {
                roundResult = false;
                _wrongTile = tileIndex;
                _expectedTile = expected;
                SetDotState(_userInput.Count, PhaseRed, pop: true);
                StartCoroutine(FlashTileError(tileIndex));
            }
            else
            {
                SetTileGlow(tileIndex, true);
                StartCoroutine(ReleaseTileGlowAfter(tileIndex, 0.18f));
                SetDotState(_userInput.Count, PhaseGreen, pop: true);
                _userInput.Add(tileIndex);
                if (_userInput.Count == _sequence.Count)
                {
                    roundResult = true;
                    double avg = _reactionTimesMs.Skip(System.Math.Max(0, _reactionTimesMs.Count - _sequence.Count)).Average();
                    _feedbackText.text = $"{(int)avg} ms de reacción promedio";
                }
            }

            if (roundResult.HasValue)
            {
                _awaitingInput = false;
                SetTilesInteractable(false);
                StartCoroutine(FinishRound(roundResult.Value, _round));
            }
        }

        /// <summary>Aplica las reglas (avance / anti-frustración) y decide si la partida
        /// sigue o termina. Todo el feedback ocurre sobre el tablero, sin pantallas completas.</summary>
        private IEnumerator FinishRound(bool wasCorrect, int roundNumber)
        {
            _roundsPlayed++;
            SetAudioDistractor(false); // se apaga entre rondas; PresentSequence lo prende de nuevo si el próximo nivel lo pide
            float hold;

            if (wasCorrect)
            {
                _correctRounds++;
                _consecutiveCorrectAtLevel++;
                _isiSlowed = false;
                UpdateHud();
                PlaySuccessFeedback();
                _phasePill.Set("¡Correcto!", PhaseGreen);
                StartCoroutine(CelebrateWave());
                StartCoroutine(UiFx.SparkBurst(_fxLayerRect, Vector2.zero, new Color(1f, 0.88f, 0.35f), 16, _boardContentWidth * 0.6f, 44f, 0.65f));
                hold = 0.75f;

                if (_consecutiveCorrectAtLevel >= 2)
                {
                    _consecutiveCorrectAtLevel = 0;
                    _currentLevelIndex++;
                    _peakLevelIndex = Mathf.Max(_peakLevelIndex, _currentLevelIndex);
                    yield return new WaitForSeconds(0.2f);
                    UpdateHud();
                    _toast.Show($"¡Nivel {_currentLevelIndex}!", PhaseNameFor(_currentLevelIndex), AccentFor(_currentLevelIndex), 1.3f);
                    hold = 0.9f;
                }
            }
            else
            {
                // Cada error cuesta 1 corazón de inmediato Y el DDA ayuda ralentizando la
                // siguiente secuencia (+200ms ISI); el nivel no baja.
                _consecutiveCorrectAtLevel = 0;
                _isiSlowed = true;
                _lives--;
                PlayErrorFeedback();
                UpdateHud();
                _phasePill.Set("Fallaste la secuencia", PhaseRed);
                StartCoroutine(UiFx.Shake(16f, 0.42f, _boardPanelRect, _gridContainerRect, _distractorLayerRect, _fxLayerRect));
                _feedbackText.text = _lives > 0 ? "Perdiste una vida · vamos más despacio" : "Te quedaste sin vidas";
                if (_expectedTile >= 0) StartCoroutine(ShowCorrectTile(_expectedTile));
                hold = 1.2f;
            }

            yield return new WaitForSeconds(hold);
            _feedbackText.text = "";

            if (_lives <= 0)
            {
                EndSession(gameOver: true);
            }
            else if (roundNumber >= SafetyMaxRounds)
            {
                EndSession(gameOver: false);
            }
            else
            {
                _round = roundNumber + 1;
                yield return StartCoroutine(BeginRound(_round, firstRound: false));
            }
        }

        /// <summary>Fin de partida por las 3 vidas perdidas (o el piso de seguridad de
        /// rondas). El puntaje/ELO/nivel definitivos los calcula el lado Kotlin
        /// (<c>NativeReceiver.onGameFinished</c> -> repositorio) a partir de esta
        /// telemetría -- acá solo se reporta lo que realmente pasó en la sesión.</summary>
        private void EndSession(bool gameOver)
        {
            _exit.Show();
            int roundsPlayed = Mathf.Max(1, _roundsPlayed);
            int baseScore = _correctRounds * 100 / roundsPlayed;
            int difficultyBonus = Mathf.Clamp((_peakLevelIndex - 1) * 2, 0, 30);
            double avgReaction = _reactionTimesMs.Count > 0 ? _reactionTimesMs.Average() : 1500.0;
            int speedBonus = avgReaction < 500 ? 10 : avgReaction < 800 ? 5 : 0;
            int finalScore = Mathf.Clamp(baseScore + difficultyBonus + speedBonus, 0, 100);
            var peakLevel = SequenceLevelDatabase.Get(_peakLevelIndex);

            _phasePill.Set(
                gameOver ? $"Fin del juego · {finalScore} puntos" : $"¡Sesión completa! · {finalScore} puntos",
                gameOver ? PhaseAmber : PhaseGreen);

            var telemetry = new SequenceTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new SequenceSessionMetrics
                {
                    correct_rounds = _correctRounds,
                    total_rounds = roundsPlayed,
                    calculated_score = finalScore,
                    average_response_time_ms = avgReaction,
                    final_span_length = peakLevel.SequenceLength,
                    level = _config.config.level,
                    timed = _config.config.timed
                }
            };

            string json = JsonUtility.ToJson(telemetry);
            NativeBridge.ForwardTelemetryToPlatform(json);
        }

        // ---- UI construida por código ----

        private void BuildUi()
        {
            // Bug real encontrado jugando en un dispositivo Android de verdad (22-sep):
            // sin EventSystem en la escena, los Button de uGUI NUNCA reciben el toque.
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("SequenceCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // ancho fijo (1080): el HUD no cambia de proporción según el alto del teléfono
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo full-bleed -- a propósito FUERA del safe area.
            var backgroundGo = new GameObject("Background");
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            var backgroundRect = backgroundGo.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundGo.AddComponent<Image>().color = BackgroundColor;

            // Profundidad: dos resplandores suaves y burbujas de luz que suben despacio.
            UiFx.AddBackgroundGlow(backgroundGo.transform, new Vector2(0.15f, 0.85f), 1500f, new Color(0.49f, 0.36f, 0.95f, 0.24f));
            UiFx.AddBackgroundGlow(backgroundGo.transform, new Vector2(0.9f, 0.12f), 1400f, new Color(0.23f, 0.51f, 0.96f, 0.20f));
            backgroundGo.AddComponent<CountdownAmbient>().Build(backgroundRect, 8);

            // Safe Area real: todo el HUD/grilla/texto vive DENTRO de esto.
            var safeAreaGo = new GameObject("SafeAreaContent");
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            _safeAreaContentRect = safeAreaGo.AddComponent<RectTransform>();
            ApplySafeArea(_safeAreaContentRect);

            BuildHud(_safeAreaContentRect);

            var boardGo = new GameObject("BoardPanel");
            boardGo.transform.SetParent(_safeAreaContentRect, false);
            _boardPanelRect = boardGo.AddComponent<RectTransform>();
            var boardImage = boardGo.AddComponent<Image>();
            boardImage.sprite = RoundedRectSprite.Get(56);
            boardImage.type = Image.Type.Sliced;
            boardImage.color = new Color(1f, 1f, 1f, 0.06f);
            boardImage.raycastTarget = false;
            var boardBorder = boardGo.AddComponent<Outline>();
            boardBorder.effectColor = new Color(1f, 1f, 1f, 0.10f);
            boardBorder.effectDistance = new Vector2(3f, -3f);

            var distractorLayerGo = new GameObject("DistractorLayer");
            distractorLayerGo.transform.SetParent(_safeAreaContentRect, false);
            _distractorLayerRect = distractorLayerGo.AddComponent<RectTransform>();

            var gridGo = new GameObject("GridContainer");
            gridGo.transform.SetParent(_safeAreaContentRect, false);
            _gridContainerRect = gridGo.AddComponent<RectTransform>();
            _gridLayout = gridGo.AddComponent<GridLayoutGroup>();
            _gridLayout.childAlignment = TextAnchor.MiddleCenter;
            _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

            // Capa de efectos (chispas, ondas) por encima de las fichas, sin layout ni toques.
            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safeAreaContentRect, false);
            _fxLayerRect = fxGo.AddComponent<RectTransform>();

            _phasePill = new PhasePill(_safeAreaContentRect, this, UnitsPerDp, 22);
            _feedbackText = CreateCenteredText(_safeAreaContentRect, 17, 900);
            _feedbackText.color = new Color(1f, 1f, 1f, 0.82f);

            _toast = new Toast(_safeAreaContentRect, this, UnitsPerDp);
            _toast.SetTopOffset((HudHeightDp + 4f) * UnitsPerDp);
            _exit = new ExitButton(_safeAreaContentRect, this, UnitsPerDp);

            var flashGo = new GameObject("FeedbackFlash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            var flashRect = flashGo.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            _feedbackFlashImage = flashGo.AddComponent<Image>();
            _feedbackFlashImage.raycastTarget = false;
            _feedbackFlashImage.color = new Color(0f, 0f, 0f, 0f);

            // Última en la jerarquía -> se dibuja encima de todo cuando está activa.
            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        /// <summary>Ajusta <paramref name="target"/> para que sus bordes coincidan
        /// exactamente con <c>Screen.safeArea</c> -- el HUD y todo el contenido
        /// interactivo cuelgan de acá, nunca del Canvas completo, para no cortarse con
        /// notch/barra de estado. Se recalcula en cada <see cref="ApplyResponsiveLayout"/>.</summary>
        private static void ApplySafeArea(RectTransform target)
        {
            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private void BuildHud(Transform parent)
        {
            float hudHeightU = HudHeightDp * UnitsPerDp;
            float marginU = MarginDp * UnitsPerDp;

            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(parent, false);
            var hudRect = hudGo.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.sizeDelta = new Vector2(0f, hudHeightU);
            hudRect.anchoredPosition = Vector2.zero;

            // Insignia de nivel (círculo con el número, color según la fase) + nombre de la
            // fase y datos de la partida a su derecha.
            float badgeSize = 60f * UnitsPerDp;
            var badgeGo = new GameObject("LevelBadge");
            badgeGo.transform.SetParent(hudGo.transform, false);
            var badgeRect = badgeGo.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.sizeDelta = new Vector2(badgeSize, badgeSize);
            badgeRect.anchoredPosition = new Vector2(marginU, -6f * UnitsPerDp);
            _hudBadgeImage = badgeGo.AddComponent<Image>();
            _hudBadgeImage.sprite = DiscSprite.Get();
            _hudBadgeImage.raycastTarget = false;

            var badgeTextGo = new GameObject("BadgeText");
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            var badgeTextRect = badgeTextGo.AddComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;
            _hudBadgeText = badgeTextGo.AddComponent<Text>();
            _hudBadgeText.font = UiFonts.Bold;
            _hudBadgeText.fontSize = Mathf.RoundToInt(30f * UnitsPerDp);
            _hudBadgeText.alignment = TextAnchor.MiddleCenter;
            _hudBadgeText.color = Color.white;
            _hudBadgeText.raycastTarget = false;
            UiFonts.AddSoftShadow(badgeTextGo, 2f, 0.35f);

            // Vidas: 3 corazones ilustrados sobre una píldora (componente compartido con
            // Parejas Ocultas), arriba a la derecha, centrada verticalmente con la insignia.
            float heartSizeU = 26f * UnitsPerDp;
            float livesWidthU = 3f * heartSizeU + 2f * heartSizeU * 0.16f + heartSizeU * 0.44f;
            float livesHeightU = heartSizeU * 1.44f;
            float badgeCenterY = -6f * UnitsPerDp - badgeSize * 0.5f;
            _livesHud = new LivesHud(hudGo.transform, this, MaxLives,
                alignRight: true, marginU: marginU, topOffsetU: badgeCenterY + livesHeightU * 0.5f, heartSizeU: heartSizeU);

            // Textos: ocupan SOLO el hueco entre la insignia y la píldora de vidas (antes
            // se calculaban como % del ancho y en teléfonos angostos pasaban por debajo de
            // los corazones); si no caben, el texto se achica en vez de invadir.
            float textLeft = marginU + badgeSize + 14f * UnitsPerDp;
            float textRightReserve = marginU + livesWidthU + 14f * UnitsPerDp;
            _hudPhaseText = CreateAnchoredText(hudGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -10f * UnitsPerDp), 21, TextAnchor.UpperLeft);
            _hudSubText = CreateAnchoredText(hudGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, -42f * UnitsPerDp), 15, TextAnchor.UpperLeft);
            _hudSubText.color = new Color(1f, 1f, 1f, 0.72f);
            foreach (var t in new[] { _hudPhaseText, _hudSubText })
            {
                var r = t.rectTransform;
                r.offsetMin = new Vector2(textLeft, r.offsetMin.y);
                r.offsetMax = new Vector2(-textRightReserve, r.offsetMax.y);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = Mathf.RoundToInt(t.fontSize * 0.6f);
                t.resizeTextMaxSize = t.fontSize;
            }

            // Puntos de progreso de la secuencia (uno por paso), centrados bajo el HUD.
            var dotsGo = new GameObject("SequenceDots");
            dotsGo.transform.SetParent(hudGo.transform, false);
            _dotsRect = dotsGo.AddComponent<RectTransform>();
            _dotsRect.anchorMin = new Vector2(0.5f, 1f);
            _dotsRect.anchorMax = new Vector2(0.5f, 1f);
            _dotsRect.pivot = new Vector2(0.5f, 1f);
            _dotsRect.sizeDelta = new Vector2(900f, 26f * UnitsPerDp);
            _dotsRect.anchoredPosition = new Vector2(0f, -86f * UnitsPerDp);
            var dotsLayout = dotsGo.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.spacing = 12f * UnitsPerDp;
            dotsLayout.childControlWidth = false;
            dotsLayout.childControlHeight = false;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;
        }

        private void UpdateHud()
        {
            var accent = AccentFor(_currentLevelIndex);
            _hudBadgeImage.color = accent;
            _hudBadgeText.text = _currentLevelIndex.ToString();
            _hudPhaseText.text = PhaseNameFor(_currentLevelIndex);
            _hudSubText.text = $"Ronda {_round} · {_correctRounds} aciertos";
            _livesHud.SetLives(_lives);
        }

        // ---- Puntos de progreso ----

        private void BuildDots(int count)
        {
            foreach (var dot in _dots) if (dot != null) Destroy(dot.gameObject);
            _dots.Clear();
            float size = 18f * UnitsPerDp;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Dot_{i}");
                go.transform.SetParent(_dotsRect, false);
                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(size, size);
                var image = go.AddComponent<Image>();
                image.sprite = DiscSprite.Get();
                image.raycastTarget = false;
                image.color = DotPending;
                _dots.Add(image);
            }
        }

        private void ResetDots()
        {
            foreach (var dot in _dots) if (dot != null) dot.color = DotPending;
        }

        private void SetDotState(int index, Color color, bool pop)
        {
            if (index < 0 || index >= _dots.Count || _dots[index] == null) return;
            _dots[index].color = color;
            if (pop) StartCoroutine(PopRect(_dots[index].rectTransform, 1.5f, 0.22f));
        }

        private static IEnumerator PopRect(RectTransform rect, float peak, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (rect == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                rect.localScale = Vector3.one * Mathf.Lerp(peak, 1f, UiFx.EaseOutCubic(t));
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }

        // ---- Grilla ----

        /// <summary>Reconstruye la grilla si filas/columnas pedidas cambiaron -- pasa
        /// como mucho una vez por ronda, nunca a mitad de una presentación/entrada.
        /// Soporta grillas no cuadradas (2x3, 3x4) según la tabla de niveles.</summary>
        private void EnsureGridDimension(int rows, int cols)
        {
            if (rows == _currentGridRows && cols == _currentGridCols)
            {
                ApplyResponsiveLayout(); // el tamaño de pantalla pudo cambiar (rotación) aunque la grilla no
                return;
            }
            _currentGridRows = rows;
            _currentGridCols = cols;

            foreach (var button in _tileButtons.Values) Destroy(button.gameObject);
            _tileButtons.Clear();
            _tileImages.Clear();
            _tileGlows.Clear();
            _tilePulseCoroutines.Clear();

            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = cols;

            int count = rows * cols;
            for (int i = 0; i < count; i++) CreateTile(i);

            ApplyResponsiveLayout();
        }

        /// <summary>Layout responsivo: la grilla ocupa el área segura disponible con
        /// márgenes/espaciado dentro del rango pedido, sin bajar nunca del piso de
        /// accesibilidad de 48dp por celda. Soporta grillas rectangulares tomando el mínimo
        /// entre el cellSize que entra por ancho y el que entra por alto.</summary>
        private void ApplyResponsiveLayout()
        {
            ApplySafeArea(_safeAreaContentRect); // por si el dispositivo rotó

            Rect safeRect = _safeAreaContentRect.rect;
            float marginU = MarginDp * UnitsPerDp;
            float hudReservedU = HudHeightDp * UnitsPerDp;
            float bottomReservedU = BottomReservedDp * UnitsPerDp;
            float paddingU = GridPaddingDp * UnitsPerDp;
            float spacingU = GridSpacingDp * UnitsPerDp;

            float availableW = Mathf.Max(0f, safeRect.width - marginU * 2f);
            float availableH = Mathf.Max(0f, safeRect.height - hudReservedU - bottomReservedU);

            int rows = Mathf.Max(1, _currentGridRows);
            int cols = Mathf.Max(1, _currentGridCols);
            float rawCellW = (availableW - paddingU * 2f - spacingU * (cols - 1)) / cols;
            float rawCellH = (availableH - paddingU * 2f - spacingU * (rows - 1)) / rows;
            float minCellU = MinCellSizeDp * UnitsPerDp;
            float maxCellU = MaxCellSizeDp * UnitsPerDp;
            float cellSize = Mathf.Clamp(Mathf.Min(rawCellW, rawCellH), minCellU, maxCellU);
            _cellSize = cellSize;

            _boardContentWidth = cellSize * cols + spacingU * (cols - 1);
            _boardContentHeight = cellSize * rows + spacingU * (rows - 1);
            float panelWidth = _boardContentWidth + paddingU * 2f;
            float panelHeight = _boardContentHeight + paddingU * 2f;
            float gridCenterY = (bottomReservedU - hudReservedU) / 2f; // centra el área libre entre el HUD y el pie

            _gridLayout.padding = new RectOffset(Mathf.RoundToInt(paddingU), Mathf.RoundToInt(paddingU), Mathf.RoundToInt(paddingU), Mathf.RoundToInt(paddingU));
            _gridLayout.spacing = new Vector2(spacingU, spacingU);
            _gridLayout.cellSize = new Vector2(cellSize, cellSize);

            _gridContainerRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            _gridContainerRect.anchoredPosition = new Vector2(0f, gridCenterY);

            _boardPanelRect.sizeDelta = _gridContainerRect.sizeDelta;
            _boardPanelRect.anchoredPosition = _gridContainerRect.anchoredPosition;

            _distractorLayerRect.sizeDelta = new Vector2(_boardContentWidth, _boardContentHeight);
            _distractorLayerRect.anchoredPosition = _gridContainerRect.anchoredPosition;

            _fxLayerRect.sizeDelta = _gridContainerRect.sizeDelta;
            _fxLayerRect.anchoredPosition = _gridContainerRect.anchoredPosition;

            float pillY = gridCenterY - panelHeight / 2f - 44f * UnitsPerDp;
            _phasePill.SetPosition(new Vector2(0f, pillY));
            _feedbackText.rectTransform.anchoredPosition = new Vector2(0f, pillY - 52f * UnitsPerDp);
        }

        private void CreateTile(int index)
        {
            var info = TilePalette.Get(index);
            var go = new GameObject($"Tile_{index}");
            go.transform.SetParent(_gridContainerRect, false);
            go.AddComponent<RectTransform>(); // tamaño/posición los maneja GridLayoutGroup solo

            var image = go.AddComponent<Image>();
            image.sprite = TileSprites.Get();
            image.type = Image.Type.Simple;
            image.color = info.NormalColor;

            // Resplandor al encenderse: mancha radial que sobresale de la ficha.
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(-0.30f, -0.30f);
            glowRect.anchorMax = new Vector2(1.30f, 1.30f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            var glow = glowGo.AddComponent<Image>();
            glow.sprite = RadialGlowSprite.Get();
            glow.raycastTarget = false;
            glow.color = new Color(info.LightColor.r, info.LightColor.g, info.LightColor.b, 0f);

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None; // la animación de glow/scale la maneja SetTileGlow
            button.onClick.AddListener(() => OnTileTapped(index));

            // Sin etiqueta de texto: fichas que se distinguen solo por color.

            _tileButtons[index] = button;
            _tileImages[index] = image;
            _tileGlows[index] = glow;
        }

        private Text CreateCenteredText(Transform parent, int fontSizeDp, float widthUnits)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(widthUnits, 60f * UnitsPerDp / 3f + fontSizeDp * UnitsPerDp);
            var text = go.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = Mathf.RoundToInt(fontSizeDp * UnitsPerDp);
            text.font = UiFonts.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            UiFonts.AddSoftShadow(go, 3f, 0.45f);
            return text;
        }

        private Text CreateAnchoredText(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, int fontSizeDp, TextAnchor alignment)
        {
            var go = new GameObject("HudText");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(0f, (fontSizeDp + 20) * UnitsPerDp);
            var text = go.AddComponent<Text>();
            text.alignment = alignment;
            text.fontSize = Mathf.RoundToInt(fontSizeDp * UnitsPerDp);
            text.font = UiFonts.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            UiFonts.AddSoftShadow(go, 3f, 0.45f);
            return text;
        }

        // ---- Entrada/salida de fichas ----

        /// <summary>Las fichas entran con rebote elástico, escalonadas -- tanto al abrir la
        /// partida como cuando el nivel nuevo cambia el tamaño de la grilla.</summary>
        private IEnumerator AnimateTilesIn()
        {
            int count = _tileImages.Count;
            if (count == 0) yield break;
            float stagger = Mathf.Min(0.045f, 0.32f / count);
            const float popSeconds = 0.30f;

            foreach (var image in _tileImages.Values) image.rectTransform.localScale = Vector3.zero;

            float total = stagger * count + popSeconds;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < count; i++)
                {
                    if (!_tileImages.TryGetValue(i, out var image)) continue;
                    float t = Mathf.Clamp01((elapsed - i * stagger) / popSeconds);
                    image.rectTransform.localScale = Vector3.one * (t <= 0f ? 0f : UiFx.EaseOutBack(t));
                }
                yield return null;
            }
            foreach (var image in _tileImages.Values) image.rectTransform.localScale = Vector3.one;
        }

        private IEnumerator AnimateTilesOut()
        {
            int count = _tileImages.Count;
            if (count == 0) yield break;
            const float seconds = 0.20f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                foreach (var image in _tileImages.Values)
                {
                    image.rectTransform.localScale = Vector3.one * (1f - t * t);
                }
                yield return null;
            }
        }

        /// <summary>Ola de celebración al acertar: las fichas se agrandan y brillan una tras otra.</summary>
        private IEnumerator CelebrateWave()
        {
            int count = _tileImages.Count;
            if (count == 0) yield break;
            float stagger = Mathf.Min(0.04f, 0.3f / count);
            const float pulseSeconds = 0.32f;
            float total = stagger * count + pulseSeconds;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < count; i++)
                {
                    if (!_tileImages.TryGetValue(i, out var image)) continue;
                    float t = Mathf.Clamp01((elapsed - i * stagger) / pulseSeconds);
                    float pulse = Mathf.Sin(t * Mathf.PI);
                    var info = TilePalette.Get(i);
                    image.rectTransform.localScale = Vector3.one * (1f + 0.10f * pulse);
                    image.color = Color.Lerp(info.NormalColor, info.LightColor, pulse * 0.8f);
                    _tileGlows[i].color = new Color(info.LightColor.r, info.LightColor.g, info.LightColor.b, 0.55f * pulse);
                }
                yield return null;
            }
            for (int i = 0; i < count; i++)
            {
                if (!_tileImages.TryGetValue(i, out var image)) continue;
                var info = TilePalette.Get(i);
                image.rectTransform.localScale = Vector3.one;
                image.color = info.NormalColor;
                _tileGlows[i].color = new Color(info.LightColor.r, info.LightColor.g, info.LightColor.b, 0f);
            }
        }

        // ---- Glow + scale pop ----

        private void SetTileGlow(int tileIndex, bool on)
        {
            if (!_tileImages.ContainsKey(tileIndex)) return;
            if (_tilePulseCoroutines.TryGetValue(tileIndex, out var running) && running != null) StopCoroutine(running);
            _tilePulseCoroutines[tileIndex] = StartCoroutine(AnimateTileGlow(tileIndex, on));
            if (on)
            {
                var light = TilePalette.Get(tileIndex).LightColor;
                StartCoroutine(UiFx.RingBurst(_tileImages[tileIndex].rectTransform, Vector2.zero,
                    new Color(light.r, light.g, light.b, 0.65f), _cellSize * 0.7f, _cellSize * 1.45f, 0.42f));
            }
        }

        private IEnumerator ReleaseTileGlowAfter(int tileIndex, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            SetTileGlow(tileIndex, false);
        }

        /// <summary>Encendido: la ficha pasa de su tono atenuado a uno claro, crece con un
        /// leve rebote y el resplandor se prende; apagado: vuelve suave a reposo.</summary>
        private IEnumerator AnimateTileGlow(int tileIndex, bool on)
        {
            float durationSeconds = on ? 0.10f : 0.16f;
            var image = _tileImages[tileIndex];
            var glow = _tileGlows[tileIndex];
            var rect = image.rectTransform;
            var info = TilePalette.Get(tileIndex);

            Color fromColor = image.color;
            Color toColor = on ? info.LightColor : info.NormalColor;
            float fromGlow = glow.color.a;
            float toGlow = on ? 0.75f : 0f;
            float fromScale = rect.localScale.x;
            float toScale = on ? 1.08f : 1f;

            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationSeconds);
                image.color = Color.Lerp(fromColor, toColor, t);
                glow.color = new Color(info.LightColor.r, info.LightColor.g, info.LightColor.b, Mathf.Lerp(fromGlow, toGlow, t));
                rect.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
                yield return null;
            }
            image.color = toColor;
            glow.color = new Color(info.LightColor.r, info.LightColor.g, info.LightColor.b, toGlow);
            rect.localScale = Vector3.one * toScale;
        }

        /// <summary>Ficha equivocada: se tiñe de rojo un instante y se sacude.</summary>
        private IEnumerator FlashTileError(int tileIndex)
        {
            if (!_tileImages.TryGetValue(tileIndex, out var image)) yield break;
            var rect = image.rectTransform;
            var info = TilePalette.Get(tileIndex);
            Color red = Color.Lerp(info.NormalColor, PhaseRed, 0.75f);

            const float seconds = 0.45f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                image.color = Color.Lerp(red, info.NormalColor, UiFx.EaseOutCubic(t));
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 8f * (1f - t));
                yield return null;
            }
            image.color = info.NormalColor;
            rect.localRotation = Quaternion.identity;
        }

        /// <summary>Después de un error, señala cuál era la ficha correcta (una pulsación
        /// lenta) -- así el jugador aprende de la falla.</summary>
        private IEnumerator ShowCorrectTile(int tileIndex)
        {
            yield return new WaitForSeconds(0.35f);
            SetTileGlow(tileIndex, true);
            yield return new WaitForSeconds(0.5f);
            SetTileGlow(tileIndex, false);
        }

        private void SetTilesInteractable(bool interactable)
        {
            foreach (var button in _tileButtons.Values) button.interactable = interactable;
        }

        // ---- Distractores de fondo (paridad con DistractorGlow, Kotlin) ----

        private void RegenerateDistractors(int seed, int count)
        {
            for (int i = _distractorLayerRect.childCount - 1; i >= 0; i--)
            {
                Destroy(_distractorLayerRect.GetChild(i).gameObject);
            }
            if (count <= 0) return;

            var random = new System.Random(seed);
            float halfW = _boardContentWidth / 2f;
            float halfH = _boardContentHeight / 2f;
            float minSide = Mathf.Min(_boardContentWidth, _boardContentHeight);
            for (int i = 0; i < count; i++)
            {
                float x = (float)(random.NextDouble() * _boardContentWidth - halfW);
                float y = (float)(random.NextDouble() * _boardContentHeight - halfH);
                float radius = minSide * (0.06f + (float)random.NextDouble() * 0.05f);

                var blobGo = new GameObject($"Distractor_{i}");
                blobGo.transform.SetParent(_distractorLayerRect, false);
                var rect = blobGo.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
                rect.anchoredPosition = new Vector2(x, y);
                var image = blobGo.AddComponent<Image>();
                image.sprite = RadialGlowSprite.Get();
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, 0.07f);
            }
        }

        // ---- Feedback de ronda: destello de pantalla + tono ----

        private static readonly Color SuccessFlashColor = new Color(0x2F / 255f, 0xBF / 255f, 0x71 / 255f, 0.16f);
        private static readonly Color ErrorFlashColor = new Color(0xEF / 255f, 0x47 / 255f, 0x6F / 255f, 0.16f);
        private const float ErrorToneHz = 220f; // A3 -- grave y cálido, no un "buzz" de error

        private void PlaySuccessFeedback()
        {
            StartCoroutine(FlashFeedback(SuccessFlashColor));
            PlayConcordantTone(TilePalette.Get(0).ToneHz);
            PlayConcordantTone(TilePalette.Get(3).ToneHz); // quinta (Do+Sol) -- "tono armónico combinado"
        }

        private void PlayErrorFeedback()
        {
            StartCoroutine(FlashFeedback(ErrorFlashColor));
            PlayConcordantTone(ErrorToneHz);
        }

        private IEnumerator FlashFeedback(Color peakColor)
        {
            const float inSeconds = 0.08f;
            const float outSeconds = 0.35f;
            float elapsed = 0f;
            while (elapsed < inSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _feedbackFlashImage.color = Color.Lerp(new Color(peakColor.r, peakColor.g, peakColor.b, 0f), peakColor, elapsed / inSeconds);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < outSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _feedbackFlashImage.color = Color.Lerp(peakColor, new Color(peakColor.r, peakColor.g, peakColor.b, 0f), elapsed / outSeconds);
                yield return null;
            }
            _feedbackFlashImage.color = new Color(0f, 0f, 0f, 0f);
        }

        // ---- Síntesis armónica -- ver HarmonicTone para el detalle de la envolvente
        // ADSR y los overtonos. La reverb corta se aplica vía AudioReverbFilter (Awake). ----

        private void PlayConcordantTone(float hz)
        {
            if (!_toneCache.TryGetValue(hz, out var clip))
            {
                clip = HarmonicTone.Build(hz);
                _toneCache[hz] = clip;
            }
            _audioSource.PlayOneShot(clip);
        }
    }
}
