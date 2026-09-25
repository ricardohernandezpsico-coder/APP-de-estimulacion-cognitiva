using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Parejas;   // SymbolSprite (gota de tinta del cartel de regla)
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / TileSprites / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Stroop
{
    /// <summary>
    /// "Tinta o Palabra" (Stroop) en Unity: 12 ensayos; se muestra el NOMBRE de un color
    /// escrito con la tinta de otro y el cartel superior dice qué hay que tocar (el color de
    /// la tinta o lo que dice la palabra). Reglas 1:1 con <c>StroopGame.kt</c> (ver
    /// <see cref="StroopContract"/>); lo nuevo es la interfaz y el ritmo:
    /// <list type="bullet">
    /// <item>Tarjeta central oscura donde la palabra brilla como neón; sale volando al acertar.</item>
    /// <item>Cartel de regla que se VOLTEA cuando la regla cambia (el cambio se ve, no solo se lee).</item>
    /// <item>Cinco botones grandes de arcilla con el color y su nombre; se hunden al tocarlos.</item>
    /// <item>Racha (combo) con tono ascendente, puntos de avance y error que señala la respuesta correcta.</item>
    /// <item>Barra de tiempo por ensayo (modo Reto) en vez de un número.</item>
    /// </list>
    /// La UI se arma por código (mismo criterio que Secuencia/Parejas); todo dentro del Safe Area.
    /// </summary>
    public class StroopGameController : GameControllerBase
    {
        public const string GameId = StroopContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float NoAnswer = -2f;

        private static readonly Color CardFill = new Color(0x1B / 255f, 0x27 / 255f, 0x40 / 255f);
        private static readonly Color InkAccent = new Color(0x60 / 255f, 0xA5 / 255f, 0xFA / 255f);
        private static readonly Color WordAccent = new Color(0x34 / 255f, 0xD3 / 255f, 0x99 / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color DarkLabel = new Color(0x1F / 255f, 0x29 / 255f, 0x37 / 255f);

        private System.Random _rng;

        // Estado de partida
        private int _trialIndex;
        private int _correct;
        private int _streak;
        private int _bestStreak;
        private long _responseMsSum;
        private int _responseCount;
        private int _answerIndex = (int)NoAnswer;
        private float _answerAt;
        private bool _acceptInput;
        private bool _ended;
        private StroopTrial _trial;
        private StroopRule _shownRule = (StroopRule)(-1);
        private int _totalAnswered;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private int _effLevel = 1;
        private int _points;
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        private const int TimeUp = -3;

        /// <summary>Modo Reto = ronda contra el reloj SIN límite de ensayos (estilo de los juegos
        /// de velocidad de las apps de estimulación cognitiva): se responde todo lo posible en
        /// <see cref="StroopContract.EndlessSeconds"/>. Modo Precisión (sin reloj) = 12 ensayos.</summary>
        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe;
        private Text _titleText, _subText, _streakText, _wordText, _bannerText, _bannerSub, _bannerAw, _chipText;
        private Image _streakDisc, _bannerBg, _bannerDisc, _bannerDrop, _wordGlow, _cardBorder, _chipBg;
        private Color _ruleAccent = Color.white;
        private RectTransform _hudRect, _streakPill, _bannerRect, _timerBg, _timerFill, _cardRect, _fxRect;
        private CanvasGroup _cardGroup;
        private readonly List<RectTransform> _buttonRects = new List<RectTransform>();
        private readonly List<Image> _buttonImages = new List<Image>();
        private readonly List<Color> _buttonColors = new List<Color>();
        private ProgressDots _dots;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private CountdownScreen _countdown;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _trialIndex = 0;
            _correct = 0;
            _streak = 0;
            _bestStreak = 0;
            _responseMsSum = 0;
            _responseCount = 0;
            _ended = false;
            _acceptInput = false;
            _shownRule = (StroopRule)(-1);
            _totalAnswered = 0;
            _points = 0;
            _dda = new AdaptiveDifficulty(5, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, 5),
                stepUp: config.config.timed ? 0.12f : 0.2f, useReaction: config.config.timed);
            _effLevel = _dda.PresentedLevel;
            _lastTickSecond = -1;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _bannerRect.gameObject.SetActive(true);
            _pill.Rect.gameObject.SetActive(true);
            foreach (var r in _buttonRects) r.gameObject.SetActive(true);
            _cardRect.gameObject.SetActive(true);
            SetStreak(0);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Tinta o Palabra", "Prepárate", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null; // deja que el Canvas calcule el rect del Safe Area antes de medir
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _roundEndsAt = Time.unscaledTime + StroopContract.EndlessSeconds;
            _trialIndex = 0;
            while (Endless ? Time.unscaledTime < _roundEndsAt : _trialIndex < StroopContract.TotalTrials)
            {
                yield return StartCoroutine(PresentTrial());

                float startedAt = Time.unscaledTime;
                while (_answerIndex == (int)NoAnswer)
                {
                    if (Endless && UpdateRoundClock()) _answerIndex = TimeUp;
                    yield return null;
                }
                _acceptInput = false;

                if (_answerIndex == TimeUp)
                {
                    yield return StartCoroutine(FadeCardOut());
                    break;
                }

                bool correct = _answerIndex == _trial.CorrectIndex;
                _totalAnswered++;
                _responseMsSum += (long)((_answerAt - startedAt) * 1000f);
                _responseCount++;
                var change = _dda.Register(correct, (_answerAt - startedAt) * 1000f);
                yield return StartCoroutine(ResolveTrial(correct));
                if (change == DdaChange.Up) _toast.Show($"Nivel {_dda.Level}", _dda.Level >= 4 ? "Cambian las reglas" : "Más difícil", GoodColor, 1.0f);
                else if (change == DdaChange.Down || _dda.Struggling) _toast.Show("Con calma", "Ajustamos la dificultad", AmberColor, 1.0f);
                _trialIndex++;
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ ensayo

        private IEnumerator PresentTrial()
        {
            _answerIndex = (int)NoAnswer;
            _effLevel = _dda.PresentedLevel;
            _trial = StroopContract.GenerateTrial(_effLevel, _config.config.base_intensity, _rng);

            _wordText.text = StroopContract.Names[_trial.WordIndex];
            var ink = StroopContract.Colors[_trial.InkIndex];
            _wordText.color = ink;
            _wordGlow.color = new Color(ink.r, ink.g, ink.b, 0.20f);
            _cardBorder.color = new Color(_ruleAccent.r, _ruleAccent.g, _ruleAccent.b, 0.95f);
            ResetButtonColors();
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();
            if (!Endless) SetTimerFraction(1f);

            bool changed = _shownRule != _trial.Rule;
            if (_shownRule == (StroopRule)(-1))
            {
                ApplyBannerVisuals(_trial.Rule);
            }
            else if (changed)
            {
                StartCoroutine(FlipBanner(_trial.Rule));
                PlayTone(392f, 0.12f, 0.12f);
            }
            _shownRule = _trial.Rule;

            _pill.Set(_trial.Rule == StroopRule.Ink ? "Elige el color de la tinta" : "Elige lo que dice la palabra",
                _trial.Rule == StroopRule.Ink ? InkAccent : WordAccent);

            // Entrada de la tarjeta: crece con rebote mientras aparece.
            float t = 0f;
            const float seconds = 0.24f;
            _cardRect.anchoredPosition = _cardRestPos;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _cardRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.78f, 1f, UiFx.EaseOutBack(k));
                _cardGroup.alpha = Mathf.Clamp01(k * 2.2f);
                yield return null;
            }
            _cardRect.localScale = Vector3.one;
            _cardGroup.alpha = 1f;
            _acceptInput = true;
        }

        private IEnumerator ResolveTrial(bool correct)
        {
            if (!Endless) _dots.Mark(_trialIndex, correct);
            Vector2 cardCenter = Vector2.zero;
            var ink = StroopContract.Colors[_trial.InkIndex];

            if (correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 100 + 25 * Mathf.Min(_streak - 1, 8);
                UpdateHudText();
                SetStreak(_streak);
                _pill.Set("¡Correcto!", GoodColor);
                PlayTone(523.25f * Mathf.Pow(2f, Mathf.Min(_streak - 1, 7) * 2f / 12f), 0.3f, 0.2f);
                StartCoroutine(Flash(GoodColor, 0.10f, 0.28f));
                StartCoroutine(UiFx.SparkBurst(_fxRect, LocalIn(_fxRect, _cardRect), ink, 16, 300f, 46f, 0.6f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _buttonRects[_answerIndex]), Color.white, 120f, 420f, 0.45f));
                if (_streak == 4 || _streak == 8 || _streak == 12)
                    _toast.Show($"Racha de {_streak}", "Sigue así", GoodColor, 0.9f);

                // La tarjeta sale volando hacia arriba mientras se desvanece.
                float t = 0f;
                float seconds = Endless ? 0.17f : 0.24f;
                Vector2 from = _cardRestPos;
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                    _cardRect.anchoredPosition = from + new Vector2(0f, 150f * k);
                    _cardRect.localScale = Vector3.one * (1f + 0.08f * k);
                    _cardGroup.alpha = 1f - k;
                    yield return null;
                }
                if (!Endless) yield return new WaitForSeconds(0.08f);
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                int correctIndex = _trial.CorrectIndex;
                _pill.Set($"Era {StroopContract.Names[correctIndex]}", AmberColor);
                PlayTone(196f, 0.32f, 0.2f);
                _cardBorder.color = new Color(BadColor.r, BadColor.g, BadColor.b, 0.85f);
                StartCoroutine(Flash(BadColor, 0.14f, 0.32f));
                StartCoroutine(UiFx.Shake(24f, 0.4f, _cardRect));
                DimButtonsExcept(correctIndex);
                StartCoroutine(PopRect(_buttonRects[correctIndex], 1.14f, 0.3f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _buttonRects[correctIndex]), Color.white, 140f, 460f, 0.55f));
                yield return new WaitForSeconds(Endless ? 0.6f : 0.85f);

                float t = 0f;
                const float seconds = 0.16f;
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    _cardGroup.alpha = 1f - Mathf.Clamp01(t / seconds);
                    yield return null;
                }
            }
            _cardGroup.alpha = 0f;
        }

        private IEnumerator FadeCardOut()
        {
            float t = 0f;
            const float seconds = 0.15f;
            float from = _cardGroup.alpha;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _cardGroup.alpha = from * (1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _cardGroup.alpha = 0f;
        }

        /// <summary>Actualiza la barra del reloj global; devuelve true cuando se acabó el tiempo.
        /// En los últimos 5 segundos suena un tic por segundo.</summary>
        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - Time.unscaledTime;
            SetTimerFraction(Mathf.Clamp01(left / StroopContract.EndlessSeconds));
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                PlayTone(880f, 0.08f, 0.10f);
            }
            return left <= 0f;
        }

        private IEnumerator FinishGame()
        {
            _ended = true;
            int total = Endless ? _totalAnswered : StroopContract.TotalTrials;
            int score = Endless ? StroopContract.EndlessScore(_correct, _totalAnswered) : StroopContract.Score(_correct, total);
            int avgMs = _responseCount > 0 ? (int)(_responseMsSum / _responseCount) : 0;

            _pill.Set("Partida terminada", GoodColor);
            ShowResult(score, avgMs, total);

            var telemetry = new StroopTelemetry
            {
                user_id = _config.user_id,
                game_id = _config.game_id,
                session_metrics = new StroopSessionMetrics
                {
                    correct_trials = _correct,
                    total_trials = total,
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

        // ------------------------------------------------------------------ entrada

        private void OnColorTapped(int index)
        {
            if (!_acceptInput || _ended) return;
            _acceptInput = false;
            _answerAt = Time.unscaledTime;
            _answerIndex = index;
        }

        // ------------------------------------------------------------------ construcción de UI

        private Vector2 _cardRestPos;

        protected override void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("StroopCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // ancho fijo (1080)
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            // Mundo "Neon": cielo nocturno de la app + su elemento propio (ver Shared/WorldBackdrop.cs).
            WorldBackdrop.Build(bgRect, GameWorld.Neon);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            _dots = new ProgressDots(_safe, this, UnitsPerDp, StroopContract.TotalTrials);
            BuildBanner();
            BuildTimer();
            BuildCard();
            _pill = new PhasePill(_safe, this, UnitsPerDp, 21);
            BuildButtons();

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(560f);

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
            _hudRect = hudGo.AddComponent<RectTransform>();
            _hudRect.anchorMin = new Vector2(0f, 1f);
            _hudRect.anchorMax = new Vector2(1f, 1f);
            _hudRect.pivot = new Vector2(0.5f, 1f);
            _hudRect.sizeDelta = new Vector2(0f, 190f);
            _hudRect.anchoredPosition = Vector2.zero;

            // Píldora de racha (derecha).
            const float pillW = 300f, pillH = 100f;
            var pillGo = new GameObject("StreakPill");
            pillGo.transform.SetParent(hudGo.transform, false);
            _streakPill = pillGo.AddComponent<RectTransform>();
            _streakPill.anchorMin = _streakPill.anchorMax = _streakPill.pivot = new Vector2(1f, 1f);
            _streakPill.sizeDelta = new Vector2(pillW, pillH);
            _streakPill.anchoredPosition = new Vector2(-MarginU, -30f);
            var pillImg = pillGo.AddComponent<Image>();
            pillImg.sprite = RoundedRectSprite.Get(64);
            pillImg.type = Image.Type.Sliced;
            pillImg.color = new Color(0f, 0f, 0f, 0.30f);
            pillImg.raycastTarget = false;

            var discGo = new GameObject("Disc");
            discGo.transform.SetParent(pillGo.transform, false);
            var discRect = discGo.AddComponent<RectTransform>();
            discRect.anchorMin = discRect.anchorMax = new Vector2(0f, 0.5f);
            discRect.pivot = new Vector2(0.5f, 0.5f);
            discRect.sizeDelta = new Vector2(52f, 52f);
            discRect.anchoredPosition = new Vector2(50f, 0f);
            _streakDisc = discGo.AddComponent<Image>();
            _streakDisc.sprite = DiscSprite.Get();
            _streakDisc.raycastTarget = false;

            _streakText = MakeText(pillGo.transform, "StreakText", 58, TextAnchor.MiddleLeft, Color.white, 2f, 0.3f);
            var sr = _streakText.rectTransform;
            sr.offsetMin = new Vector2(96f, 0f);
            sr.offsetMax = new Vector2(-24f, 0f);
            BestFit(_streakText, 36);

            _titleText = MakeText(hudGo.transform, "Title", 78, TextAnchor.UpperLeft, Color.white, 3f, 0.45f);
            _subText = MakeText(hudGo.transform, "Sub", 48, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.72f), 2f, 0.35f);
            float right = MarginU + pillW + 20f;
            PlaceTopText(_titleText, MarginU, right, -22f, 100f);
            PlaceTopText(_subText, MarginU, right, -112f, 70f);
            BestFit(_titleText, 46);
            BestFit(_subText, 30);
            _titleText.text = "Tinta o Palabra";
        }

        private void BuildBanner()
        {
            var go = new GameObject("RuleBanner");
            go.transform.SetParent(_safe, false);
            _bannerRect = go.AddComponent<RectTransform>();
            _bannerRect.anchorMin = _bannerRect.anchorMax = new Vector2(0.5f, 1f);
            _bannerRect.pivot = new Vector2(0.5f, 0.5f);
            _bannerBg = go.AddComponent<Image>();
            _bannerBg.sprite = RoundedRectSprite.Get(64);
            _bannerBg.type = Image.Type.Sliced;
            _bannerBg.raycastTarget = false;

            var discGo = new GameObject("Icon");
            discGo.transform.SetParent(go.transform, false);
            var dr = discGo.AddComponent<RectTransform>();
            dr.anchorMin = dr.anchorMax = new Vector2(0f, 0.5f);
            dr.pivot = new Vector2(0.5f, 0.5f);
            dr.sizeDelta = new Vector2(96f, 96f);
            dr.anchoredPosition = new Vector2(72f, 0f);
            _bannerDisc = discGo.AddComponent<Image>();
            _bannerDisc.sprite = DiscSprite.Get();
            _bannerDisc.raycastTarget = false;

            var dropGo = new GameObject("Drop");
            dropGo.transform.SetParent(discGo.transform, false);
            var dropRect = dropGo.AddComponent<RectTransform>();
            dropRect.anchorMin = new Vector2(0.14f, 0.12f);
            dropRect.anchorMax = new Vector2(0.86f, 0.88f);
            dropRect.offsetMin = dropRect.offsetMax = Vector2.zero;
            _bannerDrop = dropGo.AddComponent<Image>();
            _bannerDrop.sprite = SymbolSprite.Get(ShapeKind.Drop, 0);
            _bannerDrop.raycastTarget = false;

            _bannerAw = MakeText(discGo.transform, "Aa", 50, TextAnchor.MiddleCenter, Color.white, 2f, 0.3f);
            Stretch(_bannerAw.rectTransform);
            _bannerAw.text = "Aa";

            _bannerText = MakeText(go.transform, "Keyword", 84, TextAnchor.MiddleLeft, Color.white, 3f, 0.4f);
            var kr = _bannerText.rectTransform;
            kr.anchorMin = new Vector2(0f, 0.36f);
            kr.anchorMax = new Vector2(1f, 1f);
            kr.offsetMin = new Vector2(150f, 0f);
            kr.offsetMax = new Vector2(-30f, -4f);
            BestFit(_bannerText, 50);

            _bannerSub = MakeText(go.transform, "Explain", 40, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.88f), 2f, 0.3f);
            var er = _bannerSub.rectTransform;
            er.anchorMin = new Vector2(0f, 0f);
            er.anchorMax = new Vector2(1f, 0.40f);
            er.offsetMin = new Vector2(150f, 6f);
            er.offsetMax = new Vector2(-30f, 0f);
            BestFit(_bannerSub, 26);
        }

        private void ApplyBannerVisuals(StroopRule rule)
        {
            bool ink = rule == StroopRule.Ink;
            var accent = ink ? InkAccent : WordAccent;
            _bannerBg.color = Color.Lerp(new Color(0.09f, 0.13f, 0.24f, 1f), accent, 0.32f);
            _bannerDisc.color = accent;
            _bannerDrop.gameObject.SetActive(ink);
            _bannerAw.gameObject.SetActive(!ink);
            _bannerAw.color = new Color(0.06f, 0.09f, 0.16f);
            _bannerText.text = ink ? "TINTA" : "PALABRA";
            _bannerSub.text = ink ? "Toca el color con que está escrita" : "Toca lo que dice, sin importar el color";
            _ruleAccent = accent;
            if (_chipBg != null)
            {
                _chipBg.color = accent;
                _chipText.text = ink ? "TINTA" : "PALABRA";
            }
            if (_cardBorder != null) _cardBorder.color = new Color(accent.r, accent.g, accent.b, 0.95f);
        }

        private IEnumerator FlipBanner(StroopRule rule)
        {
            float t = 0f;
            const float half = 0.11f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                _bannerRect.localScale = new Vector3(1f - Mathf.Clamp01(t / half), 1f, 1f);
                yield return null;
            }
            ApplyBannerVisuals(rule);
            t = 0f;
            const float back = 0.2f;
            while (t < back)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / back);
                _bannerRect.localScale = new Vector3(Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k)), 1f, 1f);
                yield return null;
            }
            _bannerRect.localScale = Vector3.one;
            StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _bannerRect), rule == StroopRule.Ink ? InkAccent : WordAccent, 200f, 700f, 0.5f));
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
            var img = fill.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(10);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = GoodColor;
        }

        private void SetTimerFraction(float fraction)
        {
            _timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            var img = _timerFill.GetComponent<Image>();
            img.color = fraction > 0.5f ? Color.Lerp(AmberColor, GoodColor, (fraction - 0.5f) * 2f)
                                        : Color.Lerp(BadColor, AmberColor, fraction * 2f);
        }

        private void BuildCard()
        {
            var root = new GameObject("Card");
            root.transform.SetParent(_safe, false);
            _cardRect = root.AddComponent<RectTransform>();
            _cardRect.anchorMin = _cardRect.anchorMax = new Vector2(0.5f, 1f);
            _cardRect.pivot = new Vector2(0.5f, 0.5f);
            _cardGroup = root.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;

            // Sombra suave (mancha radial, sin bordes duros) debajo de la tarjeta.
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            var sr = shadowGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(-0.08f, -0.42f);
            sr.anchorMax = new Vector2(1.08f, 0.96f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            var shImg = shadowGo.AddComponent<Image>();
            shImg.sprite = RadialGlowSprite.Get();
            shImg.color = new Color(0f, 0f, 0f, 0.55f);
            shImg.raycastTarget = false;

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(root.transform, false);
            Stretch(borderGo.AddComponent<RectTransform>());
            _cardBorder = borderGo.AddComponent<Image>();
            _cardBorder.sprite = RoundedRectSprite.Get(56);
            _cardBorder.type = Image.Type.Sliced;
            _cardBorder.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(9f, 9f);
            fr.offsetMax = new Vector2(-9f, -9f);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = RoundedRectSprite.Get(52);
            fillImg.type = Image.Type.Sliced;
            fillImg.color = CardFill;
            fillImg.raycastTarget = false;

            // Brillo de la palabra (neón) detrás del texto.
            var glowGo = new GameObject("WordGlow");
            glowGo.transform.SetParent(root.transform, false);
            var gr = glowGo.AddComponent<RectTransform>();
            gr.anchorMin = new Vector2(0.02f, -0.15f);
            gr.anchorMax = new Vector2(0.98f, 1.15f);
            gr.offsetMin = gr.offsetMax = Vector2.zero;
            _wordGlow = glowGo.AddComponent<Image>();
            _wordGlow.sprite = RadialGlowSprite.Get();
            _wordGlow.raycastTarget = false;

            _wordText = MakeText(root.transform, "Word", 190, TextAnchor.MiddleCenter, Color.white, 4f, 0.5f);
            var wr = _wordText.rectTransform;
            wr.offsetMin = new Vector2(30f, 10f);
            wr.offsetMax = new Vector2(-30f, -10f);
            BestFit(_wordText, 80);
            _wordText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Etiqueta de la regla montada sobre el borde superior de la tarjeta: es donde
            // mira el jugador, así que la regla se lee sin subir la vista al cartel.
            var chipGo = new GameObject("RuleChip");
            chipGo.transform.SetParent(root.transform, false);
            var cr = chipGo.AddComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 1f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(330f, 84f);
            cr.anchoredPosition = Vector2.zero;
            _chipBg = chipGo.AddComponent<Image>();
            _chipBg.sprite = RoundedRectSprite.Get(64);
            _chipBg.type = Image.Type.Sliced;
            _chipBg.raycastTarget = false;
            _chipText = MakeText(chipGo.transform, "ChipText", 52, TextAnchor.MiddleCenter, new Color(0.06f, 0.09f, 0.16f), 0f, 0f);
            BestFit(_chipText, 30);
        }

        private void BuildButtons()
        {
            for (int i = 0; i < StroopContract.Names.Length; i++)
            {
                int index = i;
                var color = StroopContract.Colors[i];
                var go = new GameObject("Btn_" + StroopContract.Names[i]);
                go.transform.SetParent(_safe, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = TileSprites.Get();
                img.color = color;
                var button = go.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnColorTapped(index));
                go.AddComponent<PressScale>();

                bool lightFill = (0.299f * color.r + 0.587f * color.g + 0.114f * color.b) > 0.62f;
                var label = MakeText(go.transform, "Label", 52, TextAnchor.MiddleCenter, lightFill ? DarkLabel : Color.white, 2f, lightFill ? 0f : 0.4f);
                var lr = label.rectTransform;
                lr.anchorMin = new Vector2(0.12f, 0.12f);
                lr.anchorMax = new Vector2(0.88f, 0.88f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                BestFit(label, 28);
                label.text = StroopContract.Names[i];

                _buttonRects.Add(rect);
                _buttonImages.Add(img);
                _buttonColors.Add(color);
            }
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
            img.color = new Color(0.08f, 0.12f, 0.22f, 0.96f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.14f);
            outline.effectDistance = new Vector2(3f, -3f);

            AddResultText("Title", 84, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 260, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 56, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 44, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        private void ShowResult(int score, int avgMs, int total)
        {
            _exit.Show();
            _acceptInput = false;
            foreach (var r in _buttonRects) r.gameObject.SetActive(false);
            _cardRect.gameObject.SetActive(false);
            _bannerRect.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            _pill.Rect.gameObject.SetActive(false);

            string title = score >= 90 ? "¡Extraordinario!" : score >= 70 ? "¡Muy bien!" : score >= 50 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = Endless ? $"{_correct} aciertos de {total} · {_points} puntos" : $"{_correct} de {total} aciertos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _responseCount > 0
                ? $"Respuesta media {avgMs / 1000f:0.0} s · mejor racha {_bestStreak}"
                : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, WordAccent, 24, 420f, 56f, 0.9f));
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = 190f; // debajo del HUD
            _dots.Rect.anchoredPosition = new Vector2(0f, -y);
            y += 70f;

            float bannerH = 156f;
            _bannerRect.sizeDelta = new Vector2(contentW, bannerH);
            _bannerRect.anchoredPosition = new Vector2(0f, -(y + bannerH / 2f));
            y += bannerH + 22f;

            _timerBg.sizeDelta = new Vector2(contentW, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 58f; // deja lugar a la etiqueta de regla que sobresale de la tarjeta

            float cardH = Mathf.Clamp(sh * 0.27f, 320f, 520f);
            _cardRect.sizeDelta = new Vector2(contentW, cardH);
            _cardRestPos = new Vector2(0f, -(y + cardH / 2f));
            _cardRect.anchoredPosition = _cardRestPos;
            y += cardH;

            // Botones: 3 arriba + 2 abajo, centrados en el espacio que queda bajo la tarjeta.
            float remaining = sh - y;
            float gap = 6f;
            float cell = Mathf.Clamp(Mathf.Min((sw - 60f) / 3f, (remaining - 200f - gap) / 2f), 150f, 340f);
            float block = cell * 2f + gap;
            float bottom = Mathf.Max(24f, (remaining - block) * 0.28f);
            float rowBottomY = bottom + cell / 2f;
            float rowTopY = rowBottomY + cell + gap;
            for (int i = 0; i < _buttonRects.Count; i++)
            {
                var r = _buttonRects[i];
                r.sizeDelta = new Vector2(cell, cell);
                if (i < 3)
                    r.anchoredPosition = new Vector2((i - 1) * (cell + gap), rowTopY);
                else
                    r.anchoredPosition = new Vector2((i - 3 - 0.5f) * (cell + gap), rowBottomY);
            }

            // Píldora de estado en el hueco entre la tarjeta y los botones (la píldora ancla al
            // centro de su padre, por eso el y se mide desde el centro del Safe Area).
            float gapH = Mathf.Max(0f, sh - y - block - bottom);
            _pill.SetPosition(new Vector2(0f, sh / 2f - (y + gapH / 2f)));
            _toast.SetTopOffset(0f); // avisos arriba (zona del título), nunca sobre la tarjeta
        }

        // ------------------------------------------------------------------ helpers de UI

        private void UpdateHudText()
        {
            _subText.text = Endless
                ? $"Puntos {_points} · Nivel {_effLevel}"
                : $"Ensayo {_trialIndex + 1} de {StroopContract.TotalTrials} · Nivel {_config.config.level}";
        }

        private void SetStreak(int streak)
        {
            _streakText.text = $"Racha {streak}";
            _streakDisc.color = streak >= 3 ? AmberColor : new Color(1f, 1f, 1f, 0.30f);
            if (streak > 0) StartCoroutine(PopRect(_streakPill, 1.12f, 0.22f));
        }

        private void ResetButtonColors()
        {
            for (int i = 0; i < _buttonImages.Count; i++) _buttonImages[i].color = _buttonColors[i];
        }

        private void DimButtonsExcept(int keep)
        {
            for (int i = 0; i < _buttonImages.Count; i++)
            {
                if (i == keep) continue;
                var c = _buttonColors[i];
                _buttonImages[i].color = Color.Lerp(new Color(c.r, c.g, c.b, 1f), new Color(0.25f, 0.28f, 0.36f, 1f), 0.72f);
            }
        }

        // ------------------------------------------------------------------ audio
    }
}
