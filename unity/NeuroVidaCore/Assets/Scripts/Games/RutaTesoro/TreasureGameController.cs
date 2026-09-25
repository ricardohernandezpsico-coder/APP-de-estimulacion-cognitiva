using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite / TileSprites / TilePalette / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.RutaTesoro
{
    /// <summary>
    /// "Ruta del Tesoro" en Unity (memoria espacial). Un mapa de casillas de arena: durante unos
    /// segundos se iluminan los tesoros de playa (estrellas de mar, conchas, perlas), luego se ocultan y hay que encontrarlas.
    /// Con 2 errores la ruta se pierde (cuesta 1 de 3 vidas y se revelan las gemas que faltaban);
    /// al completarla, el mapa crece y hay más gemas. En modo Reto se suma un reloj para
    /// encontrarlas. Nada de pantallas intermedias: los cambios de nivel ocurren sobre el mismo
    /// mapa (ola de casillas + aviso arriba). Telemetría: reusa <see cref="StroopTelemetry"/>.
    /// </summary>
    public class TreasureGameController : GameControllerBase
    {
        public const string GameId = TreasureContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float ShapeScale = 0.86f;

        private static readonly Color BackgroundColor = new Color(0x08 / 255f, 0x1B / 255f, 0x36 / 255f);
        private static readonly Color SandColor = new Color(0xF0 / 255f, 0xD9 / 255f, 0xA6 / 255f);
        private static readonly Color RevealColor = new Color(0x7D / 255f, 0xD3 / 255f, 0xFC / 255f); // celeste suave: nada dorado (parecía tragamonedas)
        private static readonly Color FoundColor = new Color(0x2D / 255f, 0xD4 / 255f, 0xBF / 255f);
        private static readonly Color WrongColor = new Color(0xFB / 255f, 0x71 / 255f, 0x85 / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color TealAccent = new Color(0x2D / 255f, 0xD4 / 255f, 0xBF / 255f);

        private enum TileState { Hidden, Found, Wrong }
        private enum RoundResult { Waiting, Cleared, Lost, TimedOut }

        private sealed class Tile
        {
            public RectTransform Rect;
            public Image Image;
            public Image Gem;
            public RectTransform Mark;
            public Button Button;
            public TileState State;
        }

        private System.Random _rng;

        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private int _stage, _maxStage, _hearts, _cleared, _played, _found, _misses, _treasuresFoundTotal;
        private long _findMsSum;
        private int _findCount;
        private bool _acceptInput, _ended;
        private RoundResult _result;
        private float _roundStartedAt;
        private HashSet<int> _treasures = new HashSet<int>();
        private int _gridN;
        private readonly List<Tile> _tiles = new List<Tile>();

        // UI
        private RectTransform _safe, _boardRect, _timerBg, _timerFill, _fxRect;
        private Text _titleText, _subText;
        private Image _boardPanel, _timerFillImage;
        private ProgressDots _dots;
        private LivesHud _livesHud;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private CountdownScreen _countdown;
        private float _boardSize;
        private Vector2 _boardCenter;

        private bool Timed => _config != null && _config.config.timed;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _dda = new AdaptiveDifficulty(TreasureContract.MaxStage, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, TreasureContract.MaxStage),
                stepUp: 0.5f, targetOverride: 0.70f, useReaction: false);
            _stage = _dda.PresentedLevel;
            _maxStage = _stage;
            _hearts = TreasureContract.Lives;
            _cleared = _played = _found = _misses = _treasuresFoundTotal = 0;
            _findMsSum = 0;
            _findCount = 0;
            _ended = false;
            _acceptInput = false;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _boardRect.gameObject.SetActive(true);
            _pill.Rect.gameObject.SetActive(true);
            _timerBg.gameObject.SetActive(false);
            _livesHud.SetLives(_hearts);
            _gridN = -1;

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Ruta del Tesoro", "Prepárate", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();

            while (_hearts > 0 && _played < TreasureContract.MaxRounds)
            {
                yield return StartCoroutine(PlayRound());
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ ronda

        private IEnumerator PlayRound()
        {
            _stage = _dda.PresentedLevel;
            var spec = TreasureContract.StageFor(_stage);
            _maxStage = Mathf.Max(_maxStage, _stage);
            _found = 0;
            _misses = 0;
            _result = RoundResult.Waiting;
            _acceptInput = false;

            // Mapa: si cambia el tamaño se reconstruye con una ola de entrada; si no, solo se limpia.
            bool rebuilt = spec.GridSize != _gridN;
            if (rebuilt)
            {
                if (_gridN > 0) yield return StartCoroutine(WaveOut());
                BuildBoard(spec.GridSize);
            }
            ResetTiles();

            RebuildDots(spec.Treasures);
            UpdateHud(spec);
            _toast.Show($"Nivel {_stage}", $"{spec.Treasures} tesoros · mapa {spec.GridSize}×{spec.GridSize}", TealAccent, 1.0f);
            if (rebuilt) yield return StartCoroutine(WaveIn());

            _treasures = TreasureContract.PickTreasures(spec, _rng);

            // ---- Memorizar: las gemas se iluminan una a una.
            _pill.Set($"Memoriza los {spec.Treasures} tesoros", AmberColor);
            SetTimerVisible(true, RevealColor);
            var order = new List<int>(_treasures);
            Shuffle(order);
            int showMs = TreasureContract.ShowMs(_stage, _config.config.base_intensity);
            float stagger = Mathf.Min(0.16f, 0.9f / Mathf.Max(1, order.Count));
            for (int i = 0; i < order.Count; i++)
            {
                RevealGem(order[i], i);
                yield return new WaitForSeconds(stagger);
            }
            float shownAt = Time.unscaledTime;
            while (Time.unscaledTime - shownAt < showMs / 1000f)
            {
                SetTimerFraction(1f - (Time.unscaledTime - shownAt) / (showMs / 1000f), RevealColor);
                yield return null;
            }

            // ---- Ocultar y buscar.
            PlayTone(330f, 0.18f, 0.14f);
            yield return StartCoroutine(HideGems(order));
            _pill.Set("Ahora encuéntralos", TealAccent);
            _roundStartedAt = Time.unscaledTime;
            _acceptInput = true;
            float findSeconds = TreasureContract.FindSeconds(_stage);
            SetTimerVisible(Timed, TealAccent);

            while (_result == RoundResult.Waiting)
            {
                if (Timed)
                {
                    float left = 1f - (Time.unscaledTime - _roundStartedAt) / findSeconds;
                    SetTimerFraction(left, left > 0.35f ? TealAccent : (left > 0.15f ? AmberColor : BadColor));
                    if (left <= 0f) _result = RoundResult.TimedOut;
                }
                yield return null;
            }
            _acceptInput = false;
            SetTimerVisible(false, RevealColor);
            _played++;

            if (_result == RoundResult.Cleared)
            {
                _cleared++;
                _pill.Set("¡Ruta completa!", GoodColor);
                PlayTone(659.25f, 0.3f, 0.2f);
                StartCoroutine(PlayCelebrationTone());
                StartCoroutine(UiFx.SparkBurst(_fxRect, _boardCenter, RevealColor, 22, _boardSize * 0.55f, 54f, 0.8f));
                StartCoroutine(UiFx.RingBurst(_fxRect, _boardCenter, RevealColor, _boardSize * 0.3f, _boardSize * 1.2f, 0.7f));
                StartCoroutine(Flash(GoodColor, 0.10f, 0.35f));
                yield return StartCoroutine(CelebrateWave());
                var change = _dda.Register(true);
                _stage = _dda.PresentedLevel;
                if (change == DdaChange.Up && _stage > _maxStage) _toast.Show("¡Nivel completado!", "Más tesoros en el mapa", GoodColor, 1.0f);
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                _hearts--;
                _livesHud.SetLives(_hearts);
                _pill.Set(_result == RoundResult.TimedOut ? "Se acabó el tiempo" : "Se perdió la ruta", AmberColor);
                PlayTone(196f, 0.4f, 0.2f);
                StartCoroutine(Flash(BadColor, 0.14f, 0.35f));
                yield return StartCoroutine(RevealMissed());
                yield return new WaitForSeconds(1.1f);
                if (_hearts > 0)
                {
                    var change = _dda.Register(false);
                    _stage = _dda.PresentedLevel;
                    _toast.Show("Con calma", change == DdaChange.Down ? "Bajamos un nivel" : "Otra oportunidad", AmberColor, 1.0f);
                }
            }
        }

        // ------------------------------------------------------------------ toques

        private void OnTileTapped(int index)
        {
            if (!_acceptInput || _ended || index < 0 || index >= _tiles.Count) return;
            var tile = _tiles[index];
            if (tile.State != TileState.Hidden) return;

            if (_treasures.Contains(index))
            {
                tile.State = TileState.Found;
                _found++;
                _treasuresFoundTotal++;
                _findMsSum += (long)((Time.unscaledTime - _roundStartedAt) * 1000f / _found);
                _findCount++;
                _dots.Mark(_found - 1, true);
                UpdateHud(TreasureContract.StageFor(_stage));
                tile.Image.color = FoundColor;
                StartCoroutine(PopGem(tile, true));
                Vector2 center = LocalIn(_fxRect, tile.Rect);
                StartCoroutine(UiFx.SparkBurst(_fxRect, center, RevealColor, 10, tile.Rect.rect.width * 0.9f, 34f, 0.5f));
                StartCoroutine(UiFx.RingBurst(_fxRect, center, Color.white, tile.Rect.rect.width * 0.4f, tile.Rect.rect.width * 1.5f, 0.45f));
                PlayTone(TilePalette.Get((_found - 1) % 16).ToneHz, 0.28f, 0.2f);
                if (_found >= _treasures.Count) _result = RoundResult.Cleared;
            }
            else
            {
                tile.State = TileState.Wrong;
                _misses++;
                tile.Image.color = WrongColor;
                tile.Mark.gameObject.SetActive(true);
                StartCoroutine(PopRect(tile.Mark, 1.5f, 0.28f));
                StartCoroutine(UiFx.Shake(16f, 0.3f, tile.Rect));
                PlayTone(196f, 0.25f, 0.18f);
                StartCoroutine(Flash(BadColor, 0.08f, 0.22f));
                int left = TreasureContract.MaxMisses - _misses;
                _pill.Set(left > 0 ? $"Ahí no · te queda {left} intento" : "Se perdió la ruta", left > 0 ? AmberColor : BadColor);
                if (_misses >= TreasureContract.MaxMisses) _result = RoundResult.Lost;
            }
        }

        // ------------------------------------------------------------------ efectos de casillas

        private void RevealGem(int index, int orderIndex)
        {
            var tile = _tiles[index];
            tile.Image.color = RevealColor;
            tile.Gem.gameObject.SetActive(true);
            tile.Gem.sprite = TreasureSprites.ForIndex(index);
            StartCoroutine(PopGem(tile, false));
            StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, tile.Rect), RevealColor, tile.Rect.rect.width * 0.4f, tile.Rect.rect.width * 1.4f, 0.45f));
            PlayTone(TilePalette.Get(orderIndex % 16).ToneHz, 0.16f, 0.12f);
        }

        private IEnumerator PopGem(Tile tile, bool keep)
        {
            var r = tile.Gem.rectTransform;
            float t = 0f;
            const float seconds = 0.28f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                r.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            r.localScale = Vector3.one;
            tile.Gem.gameObject.SetActive(true);
        }

        private IEnumerator HideGems(List<int> order)
        {
            float t = 0f;
            const float seconds = 0.22f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                foreach (int i in order)
                {
                    var tile = _tiles[i];
                    tile.Gem.rectTransform.localScale = Vector3.one * (1f - k);
                    tile.Image.color = Color.Lerp(RevealColor, SandColor, k);
                }
                yield return null;
            }
            foreach (int i in order)
            {
                _tiles[i].Gem.gameObject.SetActive(false);
                _tiles[i].Image.color = SandColor;
            }
        }

        /// <summary>Al perder la ruta se muestran las gemas que faltaban (semitransparentes) para
        /// que el jugador vea dónde estaban.</summary>
        private IEnumerator RevealMissed()
        {
            foreach (int i in _treasures)
            {
                var tile = _tiles[i];
                if (tile.State == TileState.Found) continue;
                tile.Image.color = Color.Lerp(SandColor, RevealColor, 0.55f);
                tile.Gem.gameObject.SetActive(true);
                tile.Gem.sprite = TreasureSprites.ForIndex(i);
                StartCoroutine(PopGem(tile, true));
                yield return new WaitForSeconds(0.08f);
            }
        }

        private IEnumerator CelebrateWave()
        {
            int n = _gridN;
            float total = 0.5f;
            float t = 0f;
            while (t < total + 0.3f)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < _tiles.Count; i++)
                {
                    int r = i / n, c = i % n;
                    float delay = (r + c) / (float)(2 * n) * total;
                    float k = Mathf.Clamp01((t - delay) / 0.25f);
                    float bump = Mathf.Sin(k * Mathf.PI) * 0.10f;
                    _tiles[i].Rect.localScale = Vector3.one * (1f + bump);
                }
                yield return null;
            }
            foreach (var tile in _tiles) tile.Rect.localScale = Vector3.one;
        }

        private IEnumerator PlayCelebrationTone()
        {
            yield return new WaitForSeconds(0.14f);
            PlayTone(783.99f, 0.25f, 0.18f);
            yield return new WaitForSeconds(0.14f);
            PlayTone(1046.5f, 0.35f, 0.2f);
        }

        private IEnumerator WaveIn()
        {
            int n = _gridN;
            float total = 0.45f;
            float t = 0f;
            foreach (var tile in _tiles) tile.Rect.localScale = Vector3.zero;
            while (t < total + 0.3f)
            {
                t += Time.unscaledDeltaTime;
                for (int i = 0; i < _tiles.Count; i++)
                {
                    int r = i / n, c = i % n;
                    float delay = (r + c) / (float)(2 * n) * total;
                    float k = Mathf.Clamp01((t - delay) / 0.28f);
                    _tiles[i].Rect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k));
                }
                yield return null;
            }
            foreach (var tile in _tiles) tile.Rect.localScale = Vector3.one;
        }

        private IEnumerator WaveOut()
        {
            float t = 0f;
            const float seconds = 0.25f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                foreach (var tile in _tiles) tile.Rect.localScale = Vector3.one * (1f - k);
                yield return null;
            }
        }

        private void ResetTiles()
        {
            foreach (var tile in _tiles)
            {
                tile.State = TileState.Hidden;
                tile.Image.color = SandColor;
                tile.Gem.gameObject.SetActive(false);
                tile.Mark.gameObject.SetActive(false);
                tile.Rect.localScale = Vector3.one;
                tile.Rect.anchoredPosition = tile.Rect.anchoredPosition; // (las posiciones no cambian)
            }
        }

        // ------------------------------------------------------------------ final

        private IEnumerator FinishGame()
        {
            _ended = true;
            _acceptInput = false;
            int score = TreasureContract.Score(_cleared);
            int avgMs = _findCount > 0 ? (int)(_findMsSum / _findCount) : 0;

            _pill.Set("Partida terminada", GoodColor);
            ShowResult(score);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _cleared,
                    total_trials = Mathf.Max(_played, _cleared),
                    calculated_score = score,
                    average_response_time_ms = avgMs,
                    level = _config.config.level,
                    timed = _config.config.timed,
                    end_rating = _dda.RatingNormalized,
                    peak_level = _dda.PeakLevel
                }
            };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
            PlayTone(659.25f, 0.4f, 0.22f);
            yield break;
        }

        private void ShowResult(int score)
        {
            _exit.Show();
            _boardRect.gameObject.SetActive(false);
            _pill.Rect.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            _dots.Rect.gameObject.SetActive(false);

            string title = score >= 90 ? "¡Tesoro completo!" : score >= 60 ? "¡Gran exploración!" : score >= 30 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = $"{_cleared} rutas completas · nivel máximo {_maxStage}";
            _resultRoot.Find("Extra").GetComponent<Text>().text = $"{_treasuresFoundTotal} tesoros encontrados";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, RevealColor, 24, 420f, 56f, 0.9f));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("TreasureCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo "mar profundo" con dos resplandores (turquesa y dorado) y burbujas.
            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            bg.AddComponent<Image>().color = BackgroundColor;
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.15f, 0.90f), 1500f, new Color(0.18f, 0.83f, 0.75f, 0.20f));
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.90f, 0.10f), 1400f, new Color(0.98f, 0.55f, 0.45f, 0.13f));
            bg.AddComponent<CountdownAmbient>().Build(bgRect, 10);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            BuildTimer();

            var panelGo = new GameObject("Board");
            panelGo.transform.SetParent(_safe, false);
            _boardRect = panelGo.AddComponent<RectTransform>();
            _boardRect.anchorMin = _boardRect.anchorMax = new Vector2(0.5f, 1f);
            _boardRect.pivot = new Vector2(0.5f, 0.5f);
            _boardPanel = panelGo.AddComponent<Image>();
            _boardPanel.sprite = RoundedRectSprite.Get(56);
            _boardPanel.type = Image.Type.Sliced;
            _boardPanel.color = new Color(1f, 1f, 1f, 0.07f);
            _boardPanel.raycastTarget = false;
            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.83f, 0.75f, 0.22f);
            outline.effectDistance = new Vector2(3f, -3f);

            _pill = new PhasePill(_safe, this, UnitsPerDp, 21);

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f); // avisos arriba (zona del título), nunca sobre el mapa

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private void BuildHud()
        {
            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(_safe, false);
            var hudRect = hudGo.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.sizeDelta = new Vector2(0f, 190f);
            hudRect.anchoredPosition = Vector2.zero;

            float heartU = 26f * UnitsPerDp;
            float livesW = 3f * heartU + 2f * heartU * 0.16f + heartU * 0.44f;
            _livesHud = new LivesHud(hudGo.transform, this, TreasureContract.Lives,
                alignRight: true, marginU: MarginU, topOffsetU: -26f, heartSizeU: heartU);

            _titleText = MakeText(hudGo.transform, "Title", 78, TextAnchor.UpperLeft, Color.white, 3f, 0.45f);
            _subText = MakeText(hudGo.transform, "Sub", 48, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.72f), 2f, 0.35f);
            float right = MarginU + livesW + 24f;
            PlaceTopText(_titleText, MarginU, right, -22f, 100f);
            PlaceTopText(_subText, MarginU, right, -112f, 70f);
            BestFit(_titleText, 46);
            BestFit(_subText, 30);
            _titleText.text = "Ruta del Tesoro";

            _dots = new ProgressDots(_safe, this, UnitsPerDp, 3);
        }

        private void RebuildDots(int count)
        {
            if (_dots != null) Destroy(_dots.Rect.gameObject);
            _dots = new ProgressDots(_safe, this, UnitsPerDp, count, 15f);
            _dots.Rect.anchoredPosition = new Vector2(0f, -190f);
        }

        private void BuildTimer()
        {
            var bg = new GameObject("TimerBar");
            bg.transform.SetParent(_safe, false);
            _timerBg = bg.AddComponent<RectTransform>();
            _timerBg.anchorMin = _timerBg.anchorMax = new Vector2(0.5f, 1f);
            _timerBg.pivot = new Vector2(0.5f, 0.5f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = RoundedRectSprite.Get(10);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            bgImg.raycastTarget = false;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bg.transform, false);
            _timerFill = fill.AddComponent<RectTransform>();
            _timerFill.anchorMin = new Vector2(0f, 0f);
            _timerFill.anchorMax = new Vector2(1f, 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage = fill.AddComponent<Image>();
            _timerFillImage.sprite = RoundedRectSprite.Get(10);
            _timerFillImage.type = Image.Type.Sliced;
            _timerFillImage.raycastTarget = false;
            _timerFillImage.color = RevealColor;
        }

        private void SetTimerVisible(bool visible, Color color)
        {
            _timerBg.gameObject.SetActive(visible);
            if (visible) SetTimerFraction(1f, color);
        }

        private void SetTimerFraction(float fraction, Color color)
        {
            _timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage.color = color;
        }

        private void BuildResultPanel()
        {
            var go = new GameObject("Result");
            go.transform.SetParent(_safe, false);
            _resultRoot = go.AddComponent<RectTransform>();
            _resultRoot.anchorMin = _resultRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _resultRoot.pivot = new Vector2(0.5f, 0.5f);
            _resultRoot.sizeDelta = new Vector2(880f, 760f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.06f, 0.14f, 0.26f, 0.96f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            outline.effectDistance = new Vector2(3f, -3f);

            AddResultText("Title", 84, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 260, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 50, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 44, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        // ------------------------------------------------------------------ mapa

        private void BuildBoard(int n)
        {
            foreach (var t in _tiles) if (t.Rect != null) Destroy(t.Rect.gameObject);
            _tiles.Clear();
            _gridN = n;
            LayoutBoard();

            float cell = _boardSize / n;
            float rect = cell * 1.07f / ShapeScale * ShapeScale; // la casilla visible ocupa ~92% de su celda
            rect = cell * (0.94f / ShapeScale);
            for (int i = 0; i < n * n; i++)
            {
                int index = i;
                int r = i / n, c = i % n;
                var go = new GameObject("Tile_" + i);
                go.transform.SetParent(_boardRect, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(rect, rect);
                rt.anchoredPosition = new Vector2((c - (n - 1) / 2f) * cell, ((n - 1) / 2f - r) * cell);
                var img = go.AddComponent<Image>();
                img.sprite = TileSprites.Get();
                img.color = SandColor;
                img.alphaHitTestMinimumThreshold = 0.1f;
                var button = go.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnTileTapped(index));
                go.AddComponent<PressScale>();

                var gemGo = new GameObject("Gem");
                gemGo.transform.SetParent(go.transform, false);
                var gr = gemGo.AddComponent<RectTransform>();
                gr.anchorMin = new Vector2(0.20f, 0.25f);
                gr.anchorMax = new Vector2(0.80f, 0.85f);
                gr.offsetMin = gr.offsetMax = Vector2.zero;
                var gem = gemGo.AddComponent<Image>();
                gem.sprite = TreasureSprites.ForIndex(i);
                gem.raycastTarget = false;
                gemGo.SetActive(false);

                // Cruz de error: dos barras cruzadas.
                var markGo = new GameObject("Mark");
                markGo.transform.SetParent(go.transform, false);
                var mr = markGo.AddComponent<RectTransform>();
                mr.anchorMin = new Vector2(0.28f, 0.33f);
                mr.anchorMax = new Vector2(0.72f, 0.77f);
                mr.offsetMin = mr.offsetMax = Vector2.zero;
                for (int b = 0; b < 2; b++)
                {
                    var bar = new GameObject("Bar" + b);
                    bar.transform.SetParent(markGo.transform, false);
                    var br = bar.AddComponent<RectTransform>();
                    br.anchorMin = new Vector2(0.5f, 0.5f);
                    br.anchorMax = new Vector2(0.5f, 0.5f);
                    br.sizeDelta = new Vector2(rect * 0.46f, rect * 0.09f);
                    br.localRotation = Quaternion.Euler(0f, 0f, b == 0 ? 45f : -45f);
                    var bi = bar.AddComponent<Image>();
                    bi.sprite = RoundedRectSprite.Get(20);
                    bi.type = Image.Type.Sliced;
                    bi.color = new Color(0.55f, 0.08f, 0.16f);
                    bi.raycastTarget = false;
                }
                markGo.SetActive(false);

                _tiles.Add(new Tile { Rect = rt, Image = img, Gem = gem, Mark = mr, Button = button, State = TileState.Hidden });
            }
        }

        private void LayoutBoard()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float y = 190f + 70f; // HUD + fila de puntos
            _dots.Rect.anchoredPosition = new Vector2(0f, -190f);
            _timerBg.sizeDelta = new Vector2(sw - MarginU * 2f, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 28f;

            float pillBlock = 170f;
            float available = sh - y - pillBlock - 40f;
            _boardSize = Mathf.Clamp(Mathf.Min(sw - MarginU * 2f - 40f, available), 300f, 900f);
            float panel = _boardSize + 70f;
            float top = y + Mathf.Max(0f, (sh - y - panel - pillBlock) * 0.3f);
            _boardRect.sizeDelta = new Vector2(panel, panel);
            _boardRect.anchoredPosition = new Vector2(0f, -(top + panel / 2f));
            _boardCenter = new Vector2(0f, sh / 2f - (top + panel / 2f));

            _pill.SetPosition(new Vector2(0f, sh / 2f - (top + panel + pillBlock * 0.42f)));
        }

        private void UpdateHud(TreasureStage spec)
        {
            _subText.text = $"Nivel {_stage} · Tesoros {_found}/{spec.Treasures}";
        }

        private void RebuildDotsIfNeeded() { }

        // ------------------------------------------------------------------ helpers

        private void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
