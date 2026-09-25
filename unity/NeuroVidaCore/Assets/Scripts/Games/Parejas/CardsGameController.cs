using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Shared;
using NeuroVida.Games.Secuencia; // reusa RoundedRectSprite/RadialGlowSprite/RingSprite/HarmonicTone -- mismos criterios visuales/de audio, sin duplicar

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Piloto de la Fase 2 del roadmap de migración (ver NeuroVida/CLAUDE.md): "Parejas
    /// Ocultas" completo dentro de Unity, con el mismo motor DDA
    /// (<see cref="VisualWorkingMemoryDDA"/>, ya portado y testeado 1:1 contra la versión
    /// Kotlin) y la misma progresión de 10 niveles + sistema de "3 fallas"
    /// (<see cref="CardsGameContract"/>) que <c>CardsGameContainer.kt</c>.
    ///
    /// A diferencia de Secuencia Lumínica (que usa FlowMVI del lado Kotlin y coroutines +
    /// estado inmutable ahí), acá no hay un framework MVI que replicar -- el controlador
    /// muta su propio estado directamente y reconstruye la UI en consecuencia, mismo
    /// estilo imperativo que ya usa <c>SequenceGameController</c>.
    ///
    /// Reglas portadas 1:1 desde <c>CardsGameContainer.kt</c> (ver comentarios en cada
    /// método para el porqué, ya documentado del lado Kotlin):
    ///   - La cantidad de parejas por nivel es la escalera FIJA de <see cref="CardsGameContract"/>,
    ///     nunca depende de D(t).
    ///   - 3 parejas erradas en el tablero actual lo cortan ahí mismo: 1ra vez baja un
    ///     nivel, 2da vez repite ese nivel ya bajado, 3ra vez termina la partida.
    ///   - Un timeout en Modo Reto termina la partida directo SOLO si el perfil de edad
    ///     permite timeouts estrictos (adulto); si no, entra al mismo sistema de 3 fallas.
    ///   - Al completar un tablero, la partida NO corta ahí -- pasa de inmediato al
    ///     siguiente nivel con una pantalla de countdown como transición.
    ///
    /// Sigue sin usar prefabs -- toda la UI se arma por código en <see cref="Awake"/>.
    /// </summary>
    public class CardsGameController : MonoBehaviour
    {
        public const string GameId = "parejas";

        // ---- Constantes de layout (mismo criterio que SequenceGameController) ----
        private const float UnitsPerDp = 3f;
        private const float MarginDp = 20f;
        private const float HudHeightDp = 140f;
        private const float BottomReservedDp = 90f;
        private const float GridPaddingDp = 16f;
        private const float GridSpacingDp = 10f;
        private const float MinCellSizeDp = 48f; // piso real de accesibilidad Android
        private const float MaxCellSizeDp = 200f;
        private const int CardCornerRadiusPx = 24;

        private const float FlipHalfSeconds = 0.08f;
        private const float ResolveDelaySeconds = 0.5f; // mismo valor que Kotlin (bajado de 700 a 500ms a pedido de Ricardo)

        private SequenceInitConfig _config; // contrato de entrada compartido, ver CardsTelemetry.cs
        private DdaUserProfileConfig _profile;
        private VisualWorkingMemoryDDA _dda;
        private System.Random _random = new System.Random();

        // --- Progresión multi-nivel dentro de la misma partida ---
        private int _stage = 1;
        private int _cumulativeScore;
        private int _cumulativeMatchedPairs;
        private int _cumulativeAttempts;
        private int _boardsPlayed;
        private int _mismatchesThisAttempt;
        private int _stageFailures;
        private int _lastReportedTier = -1;
        private bool _gameEnded;

        // --- Tablero actual ---
        private class CardData
        {
            public int Id;
            public string PairKey;
            public SymbolDef Symbol;
            public bool IsFaceUp;
            public bool IsMatched;
        }

        private List<CardData> _cards = new List<CardData>();
        private int _totalPairs;
        private int _matchedPairs;
        private int _attempts;
        private int _currentStreak;
        private bool _isMemorizing;
        private float _memorizeMsLeft;
        private float _previewExposureMs;
        private bool _isTurnLocked;
        private float _difficultyIndex;
        private int _distractorCount;
        private float _distractorOpacity;
        private int _distractorSeed;
        private int? _timeLeftSeconds;
        private int? _totalTimeSeconds;
        private long _elapsedMs;
        private float _firstFlipTime;

        // -- UI construida por código --
        private RectTransform _safeAreaContentRect;
        private PhasePill _phasePill;
        private Toast _toast;
        private bool _isTransitioning;
        private Text _hudStageText;
        private Text _hudMatchedText;
        private Text _hudTimerText;
        // "Vidas" = fallas de nivel restantes antes de terminar la partida (ver
        // CardsGameContract.StageFailuresBeforeGameOver) -- mismo criterio visual que
        // Secuencia Lumínica (3 corazones rojos, se apagan a gris al perder una), a pedido
        // de Ricardo (23-sep) para que la mecánica de "3 fallas" se lea igual de clara acá.
        private LivesHud _livesHud;
        private ExitButton _exit;
        private Image _progressFillImage;
        private RectTransform _boardPanelRect;
        private RectTransform _distractorLayerRect;
        private RectTransform _gridContainerRect;
        private GridLayoutGroup _gridLayout;
        private int _currentGridCols = -1;
        private int _currentGridRows = -1;
        private int _currentTileCount = -1;
        private float _boardContentWidth;
        private float _boardContentHeight;
        private readonly Dictionary<int, Button> _cardButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, Image> _cardImages = new Dictionary<int, Image>();
        private readonly Dictionary<int, Image> _cardSymbolImages = new Dictionary<int, Image>();

        private CountdownScreen _countdown;

        private AudioSource _audioSource;
        private readonly Dictionary<float, AudioClip> _toneCache = new Dictionary<float, AudioClip>();

        private static readonly Color DomainMemoriaColor = new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f);
        // Las cartas traen sus colores en el sprite (ver CardSprites); estos son solo tintes
        // puntuales por encima (Image.color multiplica).
        private static readonly Color CardNeutralTint = Color.white;
        private static readonly Color CardErrorTint = new Color(1f, 0.70f, 0.70f);
        private static readonly Color SparkleColor = new Color(1f, 0.85f, 0.25f);
        private static readonly Color PhaseBlue = new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f);
        private static readonly Color PhaseGreen = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color PhaseAmber = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            gameObject.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Room;

            BuildUi();
            gameObject.SetActive(false);
        }

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _profile = DdaUserProfileConfig.For(DdaUserProfileConfig.ParseAgeBand(config.config.age_band));
            _dda = new VisualWorkingMemoryDDA(
                weightAccuracy: _profile.WeightAccuracy,
                weightReactionTime: _profile.WeightReactionTime,
                previewFloorMs: _profile.MinPreviewExposureMs);

            _stage = 1;
            _cumulativeScore = 0;
            _cumulativeMatchedPairs = 0;
            _cumulativeAttempts = 0;
            _boardsPlayed = 0;
            _mismatchesThisAttempt = 0;
            _stageFailures = 0;
            _lastReportedTier = -1;
            _gameEnded = false;
            _exit.Hide();
            _currentGridCols = -1;
            _currentGridRows = -1;

            StopAllCoroutines();
            StartCoroutine(StartFirstStage());
            StartCoroutine(RunTimer());
        }

        /// <summary>Ancla D(t) al nivel elegido + maestría entre sesiones -- mismo
        /// criterio que <c>seedDifficultyIndex</c> (Kotlin). No decide cuántas parejas
        /// arrancan (siempre las del nivel 1, fijo y fácil vía <see cref="CardsGameContract.PairCountForStage"/>).</summary>
        private float SeedDifficultyIndex()
        {
            float fromLevel = Mathf.Clamp01(Mathf.Clamp(_config.config.level - 1, 0, 4) / 4f) * 0.7f;
            float fromMastery = Mathf.Clamp(_config.config.base_intensity / 30f, 0f, 0.3f);
            return Mathf.Clamp01(fromLevel + fromMastery);
        }

        private IEnumerator StartFirstStage()
        {
            yield return StartCoroutine(PlayCountdownScreen(1, CountdownReason.Start));
            _mismatchesThisAttempt = 0;
            BuildRound(_dda.InitialProfile(SeedDifficultyIndex(), CardsGameContract.PairCountForStage(1)), 1);
        }

        /// <summary>Transición entre niveles FLUIDA, sin pantalla completa: el tablero
        /// anterior se cierra (cartas que se encogen escalonadas), aparece un aviso flotante
        /// con el nivel nuevo y el tablero siguiente entra con rebote. Antes cada cambio de
        /// nivel repetía la pantalla completa "3-2-1" (Ricardo, 23-sep: "van apareciendo
        /// pantallazos entre los niveles, falta fluidez"); ahora el 3-2-1 solo abre la partida.</summary>
        private IEnumerator TransitionToStage(int targetStage, CountdownReason reason)
        {
            _isTransitioning = true;
            _isTurnLocked = true;
            _stage = targetStage;

            if (reason == CountdownReason.Advance)
            {
                _phasePill.Set("¡Nivel completado!", PhaseGreen);
                StartCoroutine(UiFx.RingBurst(_boardPanelRect, Vector2.zero, SparkleColor, _boardContentWidth * 0.3f, _boardContentWidth * 1.3f, 0.6f));
                StartCoroutine(UiFx.SparkBurst(_boardPanelRect, Vector2.zero, SparkleColor, 18, _boardContentWidth * 0.62f, 46f, 0.7f));
                yield return new WaitForSeconds(0.65f);
            }
            else
            {
                _phasePill.Set(reason == CountdownReason.Demoted ? "Bajamos de nivel" : "Repetimos el nivel", PhaseAmber);
                yield return new WaitForSeconds(0.55f);
            }

            yield return StartCoroutine(AnimateBoardOut());

            _mismatchesThisAttempt = 0;
            _isTransitioning = false;
            BuildRound(_dda.CurrentProfile(CardsGameContract.PairCountForStage(targetStage)), targetStage);
            _toast.Show($"Nivel {targetStage}", SubtitleFor(reason), reason == CountdownReason.Advance ? PhaseGreen : PhaseAmber, 1.1f);
        }

        /// <summary>Las cartas se encogen (con un giro leve) de forma escalonada -- cierra el
        /// tablero antes de armar el siguiente.</summary>
        private IEnumerator AnimateBoardOut()
        {
            int count = _cardImages.Count;
            if (count == 0) yield break;
            float stagger = Mathf.Min(0.03f, 0.22f / count);
            const float duration = 0.22f;
            float total = stagger * count + duration;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < count; i++)
                {
                    if (!_cardImages.TryGetValue(i, out var image)) continue;
                    float t = Mathf.Clamp01((elapsed - i * stagger) / duration);
                    float eased = t * t;
                    image.rectTransform.localScale = Vector3.one * (1f - eased);
                    image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, eased * 25f);
                }
                yield return null;
            }
        }

        private void BuildRound(VisualWorkingMemoryDDA.DifficultyProfile profile, int stageNumber)
        {
            var symbols = SymbolBank.Select(profile.PairCount, profile.InterferenceLevel, _random);
            _cards = BuildShuffledDeck(symbols);
            _totalPairs = profile.PairCount;
            _matchedPairs = 0;
            _attempts = 0;
            _currentStreak = 0;
            _isMemorizing = true;
            _previewExposureMs = profile.PreviewExposureMs;
            _memorizeMsLeft = profile.PreviewExposureMs;
            _isTurnLocked = false;
            _stage = stageNumber;
            _difficultyIndex = profile.DifficultyIndex;
            _distractorCount = profile.DistractorCount;
            _distractorOpacity = profile.DistractorOpacity;
            _distractorSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _elapsedMs = 0;

            bool timed = _config.config.timed;
            int? timeBudget = timed ? System.Math.Max(profile.PairCount * 6, 30) : (int?)null;
            _timeLeftSeconds = timeBudget;
            _totalTimeSeconds = timeBudget;

            EnsureGridDimension(profile.GridColumns, profile.GridRows, _cards.Count);
            RegenerateDistractors(_distractorSeed, _distractorCount, _distractorOpacity);
            RenderAllCards();
            StartCoroutine(AnimateDealIn(_cards.Count));
            SetProgress(1f);
            UpdateHud();
            _phasePill.Set("Memoriza las cartas", PhaseBlue);
        }

        private List<CardData> BuildShuffledDeck(List<SymbolDef> symbols)
        {
            var deck = new List<CardData>();
            foreach (var symbol in symbols)
            {
                deck.Add(new CardData { PairKey = symbol.Key, Symbol = symbol, IsFaceUp = true });
                deck.Add(new CardData { PairKey = symbol.Key, Symbol = symbol, IsFaceUp = true });
            }
            // Fisher-Yates
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            // El Id define la casilla de la grilla en la que cada carta se dibuja (ver
            // RenderAllCards/OnCardTapped, que indexan por Id) -- tiene que asignarse
            // DESPUÉS de barajar. Antes se asignaba en el orden de creación (por pares
            // consecutivos) y el shuffle solo reordenaba la LISTA, no ese Id, así que las
            // parejas terminaban siempre en casillas contiguas -- bug reportado por
            // Ricardo ("las parejas están seguidas, no desordenadas", 23-sep).
            for (int i = 0; i < deck.Count; i++) deck[i].Id = i;
            return deck;
        }

        /// <summary>Timer continuo (arranca una vez en <see cref="StartSession"/>, corre
        /// toda la partida) -- resolución de 100ms durante la memorización (necesaria
        /// para pisos tan cortos como 500ms), 1s el resto del tiempo. Mismo comportamiento
        /// de juego que <c>runTimer</c> (Kotlin); ahí la doble resolución existía por un
        /// problema de rendimiento de Compose que no aplica acá, pero se mantiene la
        /// misma cadencia porque es la que calibra el DDA y lo que se ve en pantalla.</summary>
        private IEnumerator RunTimer()
        {
            const float tickSeconds = 0.1f;
            float secondAccumulator = 0f;
            while (!_gameEnded)
            {
                yield return new WaitForSeconds(tickSeconds);
                if (_gameEnded) yield break;
                if (_isTransitioning) continue;
                secondAccumulator += tickSeconds;
                bool isFullSecond = secondAccumulator >= 1f;
                bool timedOut = false;
                int totalPairsAtTimeout = 0;

                if (_isMemorizing)
                {
                    _memorizeMsLeft = Mathf.Max(0f, _memorizeMsLeft - tickSeconds * 1000f);
                    SetProgress(_previewExposureMs > 0f ? _memorizeMsLeft / _previewExposureMs : 0f);
                    if (_memorizeMsLeft <= 0f)
                    {
                        _isMemorizing = false;
                        FlipAllCardsDown();
                        _phasePill.Set("Encuentra las parejas", PhaseGreen);
                    }
                }
                else if (isFullSecond)
                {
                    _elapsedMs += (long)(secondAccumulator * 1000f);
                    if (_timeLeftSeconds.HasValue)
                    {
                        int newLeft = _timeLeftSeconds.Value - 1;
                        if (newLeft <= 0)
                        {
                            timedOut = true;
                            totalPairsAtTimeout = _totalPairs;
                            _timeLeftSeconds = 0;
                        }
                        else
                        {
                            _timeLeftSeconds = newLeft;
                        }
                    }
                    UpdateHud();
                }

                if (isFullSecond) secondAccumulator = 0f;

                if (timedOut)
                {
                    // Se acabó el turno sin completar el intento -> error de OMISIÓN (no
                    // hubo RT real que promediar), mismo criterio que Secuencia.
                    _dda.RegisterTrial(new VisualWorkingMemoryDDA.TrialResult(false, true, 0L), totalPairsAtTimeout);
                    SettleCurrentBoard();
                    if (_profile.AllowStrictTimeouts)
                    {
                        EndSession();
                        yield break;
                    }
                    yield return StartCoroutine(HandleStageFailure());
                }
            }
        }

        private void OnCardTapped(int id)
        {
            if (_isMemorizing || _isTurnLocked) return;
            var card = _cards.Find(c => c.Id == id);
            if (card == null || card.IsFaceUp || card.IsMatched) return;

            int faceUpUnmatched = _cards.Count(c => c.IsFaceUp && !c.IsMatched);
            if (faceUpUnmatched == 0)
            {
                _firstFlipTime = Time.unscaledTime;
                SetCardFaceUp(id, true);
                PlayTone(FlipToneHz, 0.08f, 0.12f);
            }
            else if (faceUpUnmatched == 1)
            {
                SetCardFaceUp(id, true);
                _isTurnLocked = true;
                PlayTone(FlipToneHz, 0.08f, 0.12f);
                StartCoroutine(ResolveTurn());
            }
        }

        private IEnumerator ResolveTurn()
        {
            yield return new WaitForSeconds(ResolveDelaySeconds);

            var faceUp = _cards.Where(c => c.IsFaceUp && !c.IsMatched).ToList();
            if (faceUp.Count != 2) yield break; // idempotencia, mismo criterio que Kotlin

            var a = faceUp[0];
            var b = faceUp[1];
            bool matched = a.PairKey == b.PairKey;

            long reactionMs = (long)System.Math.Max(0f, (Time.unscaledTime - _firstFlipTime) * 1000f);
            var profile = _dda.RegisterTrial(new VisualWorkingMemoryDDA.TrialResult(matched, false, reactionMs), _totalPairs);

            _attempts++;
            _currentStreak = matched ? _currentStreak + 1 : 0;
            if (matched) _matchedPairs++;
            bool justFinished = _matchedPairs >= _totalPairs;

            PlayMatchOrMismatchFeedback(matched);
            if (matched)
            {
                SetCardMatched(a.Id);
                SetCardMatched(b.Id);
            }
            else
            {
                // Sacudida roja visible ANTES de voltear (el turno sigue bloqueado, así que
                // no se puede tocar una carta que visualmente todavía está boca arriba).
                StartCoroutine(AnimateWobble(a.Id));
                StartCoroutine(AnimateWobble(b.Id));
                yield return new WaitForSeconds(0.30f);
                SetCardFaceUp(a.Id, false);
                SetCardFaceUp(b.Id, false);
            }

            _isTurnLocked = false;
            _difficultyIndex = profile.DifficultyIndex;
            _distractorCount = profile.DistractorCount;
            _distractorOpacity = profile.DistractorOpacity;
            RegenerateDistractors(_distractorSeed, _distractorCount, _distractorOpacity);

            UpdateHud();

            int tier = (int)(profile.DifficultyIndex * 10);
            if (tier > _lastReportedTier)
            {
                _lastReportedTier = tier;
                StartCoroutine(PlayLevelUpDelayed());
            }

            if (!matched)
            {
                _mismatchesThisAttempt++;
                if (_mismatchesThisAttempt >= CardsGameContract.MismatchesToFailStage)
                {
                    SettleCurrentBoard();
                    yield return StartCoroutine(HandleStageFailure());
                    yield break;
                }
            }

            if (justFinished)
            {
                SettleCurrentBoard();
                PlayTone(523.25f, 0.5f, 0.22f);
                _stageFailures = 0; // tablero superado de verdad -> las fallas no se arrastran
                if (_stage >= CardsGameContract.TotalStages) EndSession();
                else yield return StartCoroutine(TransitionToStage(_stage + 1, CountdownReason.Advance));
            }
        }

        /// <summary>Suma el puntaje/parejas/intentos del tablero ACTUAL a los acumulados
        /// de la partida -- se llama tanto al completar un tablero de verdad como al
        /// cortarlo por 3 fallas o por timeout (en todos los casos hay progreso real que
        /// debería reflejarse en el resultado final).</summary>
        private void SettleCurrentBoard()
        {
            float efficiency = _attempts == 0 ? 0f : (float)_matchedPairs / Mathf.Max(_attempts, _matchedPairs);
            int baseScore = (int)(efficiency * 100f);
            int speedBonus = (!_config.config.timed && _matchedPairs > 0) ? SpeedBonusFor(_elapsedMs, _totalPairs) : 0;
            int stageScore = Mathf.Clamp(baseScore + speedBonus, _matchedPairs > 0 ? 40 : 0, 100);

            _boardsPlayed++;
            _cumulativeScore += stageScore;
            _cumulativeMatchedPairs += _matchedPairs;
            _cumulativeAttempts += _attempts;
        }

        private static int SpeedBonusFor(long elapsedMs, int totalPairs)
        {
            long expectedMs = totalPairs * CardsGameContract.ExpectedMsPerPair;
            if (elapsedMs <= expectedMs * 0.6) return 10;
            if (elapsedMs <= expectedMs) return 5;
            return 0;
        }

        /// <summary>3 parejas erradas en el tablero actual -- 1ra vez baja un nivel, 2da
        /// vez seguida repite ese mismo nivel (ya bajado), 3ra vez termina la partida.</summary>
        private IEnumerator HandleStageFailure()
        {
            _stageFailures++;
            if (_stageFailures >= CardsGameContract.StageFailuresBeforeGameOver)
            {
                EndSession();
                yield break;
            }
            if (_stageFailures == 1)
                yield return StartCoroutine(TransitionToStage(Mathf.Max(_stage - 1, 1), CountdownReason.Demoted));
            else
                yield return StartCoroutine(TransitionToStage(_stage, CountdownReason.Retry));
        }

        private void EndSession()
        {
            _gameEnded = true;
            _exit.Show();
            int averageScore = Mathf.Clamp(_boardsPlayed > 0 ? _cumulativeScore / _boardsPlayed : 0, 0, 100);
            _phasePill.Set($"Partida terminada · {averageScore} puntos", PhaseGreen);

            var telemetry = new CardsTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new CardsSessionMetrics
                {
                    matched_pairs = _cumulativeMatchedPairs,
                    attempts = _cumulativeAttempts,
                    calculated_score = averageScore,
                    level = _config.config.level,
                    timed = _config.config.timed
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
        }

        // ---- Countdown separado (mismo patrón que SequenceGameController) ----

        private IEnumerator PlayCountdownScreen(int stageNumber, CountdownReason reason)
        {
            _safeAreaContentRect.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play(
                $"Nivel {stageNumber}",
                SubtitleFor(reason),
                () => _safeAreaContentRect.gameObject.SetActive(true)));
            _safeAreaContentRect.gameObject.SetActive(true);
        }

        private static string SubtitleFor(CountdownReason reason)
        {
            switch (reason)
            {
                case CountdownReason.Start: return "¿Listos?";
                case CountdownReason.Advance: return "¡Bien hecho!";
                case CountdownReason.Demoted: return "Con calma, tú puedes";
                case CountdownReason.Retry: return "Otra oportunidad";
                default: return "";
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        // ---- UI construida por código ----

        private void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("CardsCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // ancho fijo (1080): el HUD no cambia de proporción según el alto del teléfono
            canvasGo.AddComponent<GraphicRaycaster>();

            var backgroundGo = new GameObject("Background");
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            var backgroundRect = backgroundGo.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            // Mundo "TwinMoons": cielo nocturno de la app + su elemento propio (ver Shared/WorldBackdrop.cs).
            WorldBackdrop.Build(backgroundRect, GameWorld.TwinMoons);

            var safeAreaGo = new GameObject("SafeAreaContent");
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            _safeAreaContentRect = safeAreaGo.AddComponent<RectTransform>();
            ApplySafeArea(_safeAreaContentRect);

            BuildHud(_safeAreaContentRect);

            var boardGo = new GameObject("BoardPanel");
            boardGo.transform.SetParent(_safeAreaContentRect, false);
            _boardPanelRect = boardGo.AddComponent<RectTransform>();
            var boardImage = boardGo.AddComponent<Image>();
            boardImage.sprite = RoundedRectSprite.Get(CardCornerRadiusPx * 2);
            boardImage.type = Image.Type.Sliced;
            boardImage.color = new Color(DomainMemoriaColor.r, DomainMemoriaColor.g, DomainMemoriaColor.b, 0.14f);

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

            _phasePill = new PhasePill(_safeAreaContentRect, this, UnitsPerDp, 22);
            _toast = new Toast(_safeAreaContentRect, this, UnitsPerDp);
            _toast.SetTopOffset((HudHeightDp - 6f) * UnitsPerDp);
            _exit = new ExitButton(_safeAreaContentRect, this, UnitsPerDp);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private static void AddBackgroundGlow(Transform parent, Vector2 anchor, float size, Color color)
        {
            var go = new GameObject("BackgroundGlow");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.AddComponent<Image>();
            image.sprite = RadialGlowSprite.Get();
            image.raycastTarget = false;
            image.color = color;
        }

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

            _hudStageText = CreateAnchoredText(hudGo.transform, new Vector2(0f, 1f), new Vector2(0.55f, 1f), new Vector2(0f, 1f), new Vector2(marginU, 0f), 20, TextAnchor.UpperLeft);
            _hudMatchedText = CreateAnchoredText(hudGo.transform, new Vector2(0.55f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-marginU, 0f), 20, TextAnchor.UpperRight);
            _hudTimerText = CreateAnchoredText(hudGo.transform, new Vector2(0.55f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-marginU, -34f * UnitsPerDp), 17, TextAnchor.UpperRight);

            // Vidas = fallas restantes (3 corazones ilustrados sobre una píldora, con animación
            // al perder una) -- mismo componente que Secuencia Lumínica.
            _livesHud = new LivesHud(hudGo.transform, this, CardsGameContract.StageFailuresBeforeGameOver,
                alignRight: false, marginU: marginU, topOffsetU: -36f * UnitsPerDp, heartSizeU: 30f * UnitsPerDp);

            var barBgGo = new GameObject("ProgressBarBg");
            barBgGo.transform.SetParent(hudGo.transform, false);
            var barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0f);
            barBgRect.pivot = new Vector2(0.5f, 0f);
            barBgRect.sizeDelta = new Vector2(-marginU * 2f, 10f * UnitsPerDp);
            barBgRect.anchoredPosition = new Vector2(0f, 10f * UnitsPerDp);
            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.sprite = RoundedRectSprite.Get(10);
            barBgImage.type = Image.Type.Sliced;
            barBgImage.color = new Color(1f, 1f, 1f, 0.12f);

            var barFillGo = new GameObject("ProgressBarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            _progressFillImage = barFillGo.AddComponent<Image>();
            _progressFillImage.sprite = RoundedRectSprite.Get(10);
            _progressFillImage.type = Image.Type.Filled;
            _progressFillImage.fillMethod = Image.FillMethod.Horizontal;
            _progressFillImage.fillAmount = 1f;
            _progressFillImage.color = new Color(DomainMemoriaColor.r, DomainMemoriaColor.g, DomainMemoriaColor.b, 0.9f);
        }

        private void UpdateHud()
        {
            _hudStageText.text = $"Nivel {_stage}/{CardsGameContract.TotalStages}";
            _hudMatchedText.text = $"Parejas {_matchedPairs}/{_totalPairs}";
            _hudTimerText.text = _timeLeftSeconds.HasValue ? $"Tiempo {_timeLeftSeconds.Value} s" : $"Intentos {_attempts}";
            _livesHud.SetLives(CardsGameContract.StageFailuresBeforeGameOver - _stageFailures);
        }

        private void SetProgress(float fraction) => _progressFillImage.fillAmount = Mathf.Clamp01(fraction);

        /// <summary><paramref name="cardCount"/> es la cantidad REAL de cartas del mazo
        /// (2 * cantidad de parejas). <paramref name="cols"/>/<paramref name="rows"/> vienen
        /// del perfil de dificultad (VisualWorkingMemoryDDA.GridDimensionsFor), que redondea
        /// filas hacia arriba -- así que cols*rows a veces excede a cardCount (p.ej. 9 parejas
        /// = 18 cartas, pero grilla 4x5 = 20 casillas). Antes se creaban cols*rows casillas
        /// siempre, y las que sobraban quedaban como cartas "fantasma" boca abajo sin symbol
        /// asignado -- Ricardo lo vio como casillas vacías feas en algunos niveles (23-sep).
        /// Se crean como máximo <paramref name="cardCount"/> casillas: la última fila queda
        /// simplemente más corta, sin huecos.</summary>
        private void EnsureGridDimension(int cols, int rows, int cardCount)
        {
            int tileCount = Mathf.Min(cols * rows, cardCount);
            if (cols == _currentGridCols && rows == _currentGridRows && tileCount == _currentTileCount)
            {
                ApplyResponsiveLayout();
                return;
            }
            _currentGridCols = cols;
            _currentGridRows = rows;
            _currentTileCount = tileCount;

            foreach (var button in _cardButtons.Values) Destroy(button.gameObject);
            _cardButtons.Clear();
            _cardImages.Clear();
            _cardSymbolImages.Clear();

            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = cols;

            for (int i = 0; i < tileCount; i++) CreateCardTile(i);

            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            ApplySafeArea(_safeAreaContentRect);

            Rect safeRect = _safeAreaContentRect.rect;
            float marginU = MarginDp * UnitsPerDp;
            float hudReservedU = HudHeightDp * UnitsPerDp;
            float bottomReservedU = BottomReservedDp * UnitsPerDp;
            float paddingU = GridPaddingDp * UnitsPerDp;
            float spacingU = GridSpacingDp * UnitsPerDp;

            float availableW = Mathf.Max(0f, safeRect.width - marginU * 2f);
            float availableH = Mathf.Max(0f, safeRect.height - hudReservedU - bottomReservedU);

            int cols = Mathf.Max(1, _currentGridCols);
            int rows = Mathf.Max(1, _currentGridRows);
            float rawCellW = (availableW - paddingU * 2f - spacingU * (cols - 1)) / cols;
            float rawCellH = (availableH - paddingU * 2f - spacingU * (rows - 1)) / rows;
            float minCellU = MinCellSizeDp * UnitsPerDp;
            float maxCellU = MaxCellSizeDp * UnitsPerDp;
            float cellSize = Mathf.Clamp(Mathf.Min(rawCellW, rawCellH), minCellU, maxCellU);

            _boardContentWidth = cellSize * cols + spacingU * (cols - 1);
            _boardContentHeight = cellSize * rows + spacingU * (rows - 1);
            float panelWidth = _boardContentWidth + paddingU * 2f;
            float panelHeight = _boardContentHeight + paddingU * 2f;
            float gridCenterY = (bottomReservedU - hudReservedU) / 2f;

            _gridLayout.padding = new RectOffset(Mathf.RoundToInt(paddingU), Mathf.RoundToInt(paddingU), Mathf.RoundToInt(paddingU), Mathf.RoundToInt(paddingU));
            _gridLayout.spacing = new Vector2(spacingU, spacingU);
            _gridLayout.cellSize = new Vector2(cellSize, cellSize);

            _gridContainerRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            _gridContainerRect.anchoredPosition = new Vector2(0f, gridCenterY);

            _boardPanelRect.sizeDelta = _gridContainerRect.sizeDelta;
            _boardPanelRect.anchoredPosition = _gridContainerRect.anchoredPosition;

            _distractorLayerRect.sizeDelta = new Vector2(_boardContentWidth, _boardContentHeight);
            _distractorLayerRect.anchoredPosition = _gridContainerRect.anchoredPosition;

            float textY = gridCenterY - panelHeight / 2f - 34f * UnitsPerDp;
            _phasePill.SetPosition(new Vector2(0f, textY));
        }

        private void CreateCardTile(int index)
        {
            var go = new GameObject($"Card_{index}");
            go.transform.SetParent(_gridContainerRect, false);
            go.AddComponent<RectTransform>();

            var image = go.AddComponent<Image>();
            image.sprite = CardSprites.Get(CardSprites.Face.Back);
            image.type = Image.Type.Simple;
            image.color = CardNeutralTint;

            var symbolGo = new GameObject("Symbol");
            symbolGo.transform.SetParent(go.transform, false);
            var symbolRect = symbolGo.AddComponent<RectTransform>();
            // Ícono grande dentro de la cara de la carta (el 8% inferior del sprite es el
            // "grosor" 3D de la ficha, por eso el ícono queda un poco más arriba del
            // centro) -- más legible para personas con baja visión (Ricardo, 23-sep).
            symbolRect.anchorMin = new Vector2(0.16f, 0.21f);
            symbolRect.anchorMax = new Vector2(0.84f, 0.87f);
            symbolRect.offsetMin = Vector2.zero;
            symbolRect.offsetMax = Vector2.zero;
            var symbolImage = symbolGo.AddComponent<Image>();
            symbolImage.raycastTarget = false;
            symbolImage.preserveAspect = true;
            symbolImage.enabled = false;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            int capturedIndex = index;
            button.onClick.AddListener(() => OnCardTapped(capturedIndex));

            _cardButtons[index] = button;
            _cardImages[index] = image;
            _cardSymbolImages[index] = symbolImage;
        }

        private void RenderAllCards()
        {
            foreach (var card in _cards)
            {
                if (!_cardImages.TryGetValue(card.Id, out var image)) continue;
                var symbolImage = _cardSymbolImages[card.Id];
                image.rectTransform.localScale = Vector3.one;
                image.rectTransform.localRotation = Quaternion.identity;
                image.color = CardNeutralTint;
                if (card.IsMatched)
                {
                    image.sprite = CardSprites.Get(CardSprites.Face.Matched);
                    SetSymbolImage(symbolImage, card.Symbol, true);
                    _cardButtons[card.Id].interactable = false;
                }
                else if (card.IsFaceUp)
                {
                    image.sprite = CardSprites.Get(CardSprites.Face.Front);
                    SetSymbolImage(symbolImage, card.Symbol, true);
                    _cardButtons[card.Id].interactable = true;
                }
                else
                {
                    image.sprite = CardSprites.Get(CardSprites.Face.Back);
                    SetSymbolImage(symbolImage, card.Symbol, false);
                    _cardButtons[card.Id].interactable = true;
                }
            }
        }

        private void SetSymbolImage(Image symbolImage, SymbolDef symbol, bool visible)
        {
            symbolImage.enabled = visible;
            if (!visible) return;
            symbolImage.sprite = SymbolSprite.Get(symbol.Shape, symbol.Variant);
            symbolImage.color = Color.white;
        }

        /// <summary>Animación de entrada del tablero: cada carta "salta" desde escala 0
        /// con rebote elástico, escalonadas -- la ventana total se acota (0.3s) para no
        /// comerse la vista previa cuando el piso de exposición es de solo 500ms.</summary>
        private IEnumerator AnimateDealIn(int cardCount)
        {
            float stagger = Mathf.Min(0.035f, 0.30f / Mathf.Max(1, cardCount));
            const float popDuration = 0.28f;

            foreach (var rect in _cardImages.Values) rect.rectTransform.localScale = Vector3.zero;

            float elapsed = 0f;
            float total = stagger * cardCount + popDuration;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < cardCount; i++)
                {
                    if (!_cardImages.TryGetValue(i, out var image)) continue;
                    float t = Mathf.Clamp01((elapsed - i * stagger) / popDuration);
                    image.rectTransform.localScale = Vector3.one * (t <= 0f ? 0f : EaseOutBack(t));
                }
                yield return null;
            }
            foreach (var image in _cardImages.Values) image.rectTransform.localScale = Vector3.one;
        }

        /// <summary>Rebote de la pareja resuelta + chispas doradas que salen del centro de
        /// cada carta.</summary>
        private IEnumerator AnimateMatchPop(int id)
        {
            if (!_cardImages.TryGetValue(id, out var image)) yield break;
            var rect = image.rectTransform;

            const int sparkCount = 6;
            var sparks = new RectTransform[sparkCount];
            var sparkImages = new Image[sparkCount];
            float sparkSize = rect.rect.width * 0.16f;
            for (int i = 0; i < sparkCount; i++)
            {
                var go = new GameObject("Spark");
                go.transform.SetParent(rect, false);
                var r = go.AddComponent<RectTransform>();
                r.sizeDelta = new Vector2(sparkSize, sparkSize);
                var img = go.AddComponent<Image>();
                img.sprite = RadialGlowSprite.Get();
                img.raycastTarget = false;
                img.color = SparkleColor;
                sparks[i] = r;
                sparkImages[i] = img;
            }

            const float duration = 0.45f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = 1f + 0.16f * Mathf.Sin(t * Mathf.PI);
                rect.localScale = new Vector3(pulse, pulse, 1f);

                float reach = rect.rect.width * 0.62f * (1f - Mathf.Pow(1f - t, 2f));
                for (int i = 0; i < sparkCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / sparkCount + 0.4f;
                    sparks[i].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
                    sparkImages[i].color = new Color(SparkleColor.r, SparkleColor.g, SparkleColor.b, 1f - t);
                }
                yield return null;
            }
            rect.localScale = Vector3.one;
            for (int i = 0; i < sparkCount; i++) if (sparks[i] != null) Destroy(sparks[i].gameObject);
        }

        /// <summary>Error: la carta se sacude (rotación, para no pelearle la posición al
        /// GridLayoutGroup) y se tiñe de rojo un instante.</summary>
        private IEnumerator AnimateWobble(int id)
        {
            if (!_cardImages.TryGetValue(id, out var image)) yield break;
            var rect = image.rectTransform;

            const float duration = 0.32f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float angle = Mathf.Sin(t * Mathf.PI * 6f) * 9f * (1f - t);
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                image.color = Color.Lerp(CardErrorTint, CardNeutralTint, t);
                yield return null;
            }
            rect.localRotation = Quaternion.identity;
            image.color = CardNeutralTint;
        }

        private void SetCardFaceUp(int id, bool up)
        {
            var card = _cards.Find(c => c.Id == id);
            if (card == null) return;
            card.IsFaceUp = up;
            StartCoroutine(AnimateFlip(id, up, card.Symbol));
        }

        private void SetCardMatched(int id)
        {
            var card = _cards.Find(c => c.Id == id);
            if (card == null) return;
            card.IsMatched = true;
            card.IsFaceUp = true;
            if (_cardImages.TryGetValue(id, out var image))
            {
                image.sprite = CardSprites.Get(CardSprites.Face.Matched);
                image.color = CardNeutralTint;
            }
            if (_cardButtons.TryGetValue(id, out var button)) button.interactable = false;
            StartCoroutine(AnimateMatchPop(id));
        }

        /// <summary>Pseudo-flip 2D: achica la escala X a 0, cambia cara/sprite en el punto
        /// medio, vuelve a 1 -- da sensación de flip sin necesitar geometría 3D real en
        /// uGUI.</summary>
        private IEnumerator AnimateFlip(int id, bool up, SymbolDef symbol)
        {
            if (!_cardImages.TryGetValue(id, out var image)) yield break;
            var rect = image.rectTransform;
            var symbolImage = _cardSymbolImages[id];

            float elapsed = 0f;
            while (elapsed < FlipHalfSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FlipHalfSeconds);
                rect.localScale = new Vector3(Mathf.Lerp(1f, 0f, t), Mathf.Lerp(1f, 1.06f, t), 1f);
                yield return null;
            }

            image.sprite = CardSprites.Get(up ? CardSprites.Face.Front : CardSprites.Face.Back);
            SetSymbolImage(symbolImage, symbol, up);

            elapsed = 0f;
            while (elapsed < FlipHalfSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FlipHalfSeconds);
                rect.localScale = new Vector3(Mathf.Lerp(0f, 1f, t), Mathf.Lerp(1.06f, 1f, t), 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        /// <summary>Fin de memorización: todas las cartas boca abajo de una, sin
        /// animación de flip individual -- mismo criterio abrupto que Kotlin.</summary>
        private void FlipAllCardsDown()
        {
            foreach (var card in _cards) card.IsFaceUp = false;
            RenderAllCards();
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
            AddLegibilityOutline(go);
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
            AddLegibilityOutline(go);
            return text;
        }

        private static void AddLegibilityOutline(GameObject textGo)
        {
            UiFonts.AddSoftShadow(textGo, 3f, 0.45f);
        }

        // ---- Distractores de fondo (paridad con DistractorBackdrop, Kotlin) ----

        private void RegenerateDistractors(int seed, int count, float opacity)
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
                float radius = minSide * (0.06f + (float)random.NextDouble() * 0.07f);

                var blobGo = new GameObject($"Distractor_{i}");
                blobGo.transform.SetParent(_distractorLayerRect, false);
                var rect = blobGo.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
                rect.anchoredPosition = new Vector2(x, y);
                var image = blobGo.AddComponent<Image>();
                image.sprite = RadialGlowSprite.Get();
                image.raycastTarget = false;
                image.color = new Color(1f, 1f, 1f, opacity);
            }
        }

        // ---- Audio: tonos simples generados por código (misma síntesis que Secuencia, HarmonicTone) ----

        private const float FlipToneHz = 300f;
        private const float MismatchToneHz = 220f;

        private void PlayMatchOrMismatchFeedback(bool matched)
        {
            if (matched)
            {
                PlayTone(659.25f, 0.35f, 0.2f);
            }
            else
            {
                PlayTone(MismatchToneHz, 0.3f, 0.18f);
            }
        }

        private IEnumerator PlayLevelUpDelayed()
        {
            // Mismo desfase de 150ms que Kotlin -- evita que el tono de subida de nivel
            // suene amontonado encima del de acierto/error.
            yield return new WaitForSeconds(0.15f);
            PlayTone(1046.50f, 0.4f, 0.22f);
        }

        private void PlayTone(float hz, float durationSeconds, float volume)
        {
            float key = hz + durationSeconds * 100000f; // separa el cache por duración además de frecuencia
            if (!_toneCache.TryGetValue(key, out var clip))
            {
                clip = HarmonicTone.Build(hz, durationSeconds, volume);
                _toneCache[key] = clip;
            }
            _audioSource.PlayOneShot(clip);
        }
    }
}
