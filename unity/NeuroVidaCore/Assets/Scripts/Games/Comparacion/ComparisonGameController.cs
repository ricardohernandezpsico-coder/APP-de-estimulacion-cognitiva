using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / TileSprites / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Comparacion
{
    /// <summary>
    /// "Comparación Instantánea" en Unity: dos tarjetas enfrentadas (puntos, números o un
    /// producto) y hay que tocar la MAYOR lo más rápido posible. Reglas de
    /// <c>ComparacionGame.kt</c> (ver <see cref="ComparisonContract"/>).
    /// <list type="bullet">
    /// <item>Modo Reto: ronda de 60 s sin límite de ensayos, con puntos, racha y bono por rapidez.</item>
    /// <item>Modo Precisión (sin reloj): 12 ensayos.</item>
    /// <item>Las tarjetas entran deslizándose desde los lados; la elegida se agranda al acertar y,
    /// al fallar, se marca cuál era la mayor con su valor real ("12 &gt; 9").</item>
    /// </list>
    /// La telemetría de salida reusa <see cref="StroopTelemetry"/> (mismos campos: aciertos,
    /// ensayos, puntaje, tiempo medio, nivel, modo).
    /// </summary>
    public class ComparisonGameController : GameControllerBase
    {
        public const string GameId = ComparisonContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int NoAnswer = -2;
        private const int TimeUp = -3;
        private const float ShapeScale = 0.86f; // la ficha ocupa 86% del sprite (el resto es sombra)

        private static readonly Color BackgroundColor = new Color(0x0F / 255f, 0x17 / 255f, 0x2A / 255f);
        private static readonly Color AccentColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color[] SideColors =
        {
            new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f),
            new Color(0xA8 / 255f, 0x55 / 255f, 0xF7 / 255f),
        };

        private System.Random _rng;

        private int _trialIndex, _correct, _streak, _bestStreak, _totalAnswered, _points, _speedHits;
        private long _responseMsSum;
        private int _responseCount;
        private int _answerSide = NoAnswer;
        private float _answerAt;
        private bool _acceptInput, _ended;
        private ComparisonTrial _trial;
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        /// <summary>Nivel efectivo: arranca en el nivel elegido y sube durante la partida.</summary>
        private int _effLevel = 1;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        /// <summary>Desde el nivel 3 (cuentas) las tarjetas se apilan una sobre otra, a todo el ancho,
        /// para que "37 - 11" o "12 × 7 + 9" quepan en un solo renglón y grandes.</summary>
        private bool _verticalLayout;

        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe, _streakPill, _timerBg, _timerFill, _fxRect, _bannerRect;
        private Text _titleText, _subText, _streakText;
        private Image _streakDisc;
        private ProgressDots _dots;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private CountdownScreen _countdown;

        private sealed class CardView
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Image Image;
            public Text Number;
            public Text Value;
            public readonly List<Image> Dots = new List<Image>();
            public Vector2 RestPos;
            public Vector2 VisibleSize;
        }

        private readonly CardView[] _cards = new CardView[2];

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _trialIndex = _correct = _streak = _bestStreak = _totalAnswered = _points = _speedHits = 0;
            _responseMsSum = 0;
            _responseCount = 0;
            _ended = false;
            _acceptInput = false;
            _lastTickSecond = -1;
            _dda = new AdaptiveDifficulty(ComparisonContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, ComparisonContract.MaxLevel),
                stepUp: config.config.timed ? 0.15f : 0.25f, useReaction: config.config.timed);
            _effLevel = _dda.PresentedLevel;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _bannerRect.gameObject.SetActive(true);
            _pill.Rect.gameObject.SetActive(true);
            foreach (var c in _cards) c.Rect.gameObject.SetActive(true);
            SetStreak(0);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Comparación", "Prepárate", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _roundEndsAt = Time.unscaledTime + ComparisonContract.EndlessSeconds;
            _trialIndex = 0;
            while (Endless ? Time.unscaledTime < _roundEndsAt : _trialIndex < ComparisonContract.TotalTrials)
            {
                yield return StartCoroutine(PresentTrial());

                float startedAt = Time.unscaledTime;
                while (_answerSide == NoAnswer)
                {
                    if (Endless && UpdateRoundClock()) _answerSide = TimeUp;
                    yield return null;
                }
                _acceptInput = false;

                if (_answerSide == TimeUp)
                {
                    yield return StartCoroutine(FadeCardsOut());
                    break;
                }

                float reactionMs = (_answerAt - startedAt) * 1000f;
                bool correct = (_answerSide == 0) == _trial.LeftIsGreater;
                _totalAnswered++;
                _responseMsSum += (long)reactionMs;
                _responseCount++;
                bool fast = correct && reactionMs < ComparisonContract.SpeedBonusMs(_config.config.base_intensity);
                if (fast) _speedHits++;
                yield return StartCoroutine(ResolveTrial(correct, fast, reactionMs));
                _trialIndex++;
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ ensayo

        private IEnumerator PresentTrial()
        {
            _answerSide = NoAnswer;
            _effLevel = _dda.PresentedLevel;
            if ((_effLevel >= 3) != _verticalLayout) Layout();
            _trial = ComparisonContract.GenerateTrial(_effLevel, _config.config.base_intensity, _rng);
            FillCard(_cards[0], _trial.Left, SideColors[0]);
            FillCard(_cards[1], _trial.Right, SideColors[1]);
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();
            _pill.Set("Toca la tarjeta MAYOR", AccentColor);

            // Las tarjetas entran deslizándose desde los lados con rebote.
            float t = 0f;
            const float seconds = 0.22f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float e = UiFx.EaseOutBack(k);
                for (int i = 0; i < 2; i++)
                {
                    var c = _cards[i];
                    float from = i == 0 ? -280f : 280f;
                    c.Rect.anchoredPosition = c.RestPos + new Vector2(Mathf.LerpUnclamped(from, 0f, e), 0f);
                    c.Rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, e);
                    c.Group.alpha = Mathf.Clamp01(k * 2.5f);
                }
                yield return null;
            }
            foreach (var c in _cards)
            {
                c.Rect.anchoredPosition = c.RestPos;
                c.Rect.localScale = Vector3.one;
                c.Group.alpha = 1f;
            }
            _acceptInput = true;
        }

        private void FillCard(CardView card, ComparisonSide side, Color color)
        {
            card.Image.color = color;
            card.Value.gameObject.SetActive(false);
            bool dots = side.IsDots;
            card.Number.gameObject.SetActive(!dots);
            if (!dots)
            {
                card.Number.text = side.Display;
                float availW = card.VisibleSize.x * 0.86f;
                float availH = card.VisibleSize.y * (_verticalLayout ? 0.50f : 0.56f);
                float size = Mathf.Min(230f, availW / Mathf.Max(1f, EmWidth(side.Display)), availH);
                card.Number.fontSize = Mathf.RoundToInt(Mathf.Max(40f, size));
            }

            int n = dots ? side.DotCount : 0;
            const int cols = 4;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)cols));
            float dot = Mathf.Min(card.VisibleSize.x * 0.19f, card.VisibleSize.y * 0.15f);
            float step = dot * 1.32f;
            for (int i = 0; i < card.Dots.Count; i++)
            {
                var d = card.Dots[i];
                d.gameObject.SetActive(i < n);
                if (i >= n) continue;
                int row = i / cols, col = i % cols;
                int inRow = Mathf.Min(cols, n - row * cols);
                float x = (col - (inRow - 1) / 2f) * step;
                float y = ((rows - 1) / 2f - row) * step + card.VisibleSize.y * 0.02f;
                d.rectTransform.sizeDelta = new Vector2(dot, dot);
                d.rectTransform.anchoredPosition = new Vector2(x, y);
            }
        }

        /// <summary>Ancho aproximado (en "em") de un texto con dígitos y operadores.</summary>
        private static float EmWidth(string s)
        {
            float w = 0f;
            foreach (char c in s) w += char.IsDigit(c) ? 0.62f : c == ' ' ? 0.28f : 0.6f;
            return w;
        }

        private IEnumerator ResolveTrial(bool correct, bool fast, float reactionMs)
        {
            var change = _dda.Register(correct, reactionMs);
            _effLevel = _dda.Level;
            int chosen = _answerSide;
            int winner = _trial.LeftIsGreater ? 0 : 1;
            if (!Endless) _dots.Mark(_trialIndex, correct);

            // Muestra los valores reales (útil sobre todo con puntos y productos).
            for (int i = 0; i < 2; i++)
            {
                var side = i == 0 ? _trial.Left : _trial.Right;
                if (side.IsDots || side.IsExpression)
                {
                    _cards[i].Value.text = side.Value.ToString();
                    _cards[i].Value.gameObject.SetActive(true);
                }
            }
            int max = Mathf.Max(_trial.Left.Value, _trial.Right.Value);
            int min = Mathf.Min(_trial.Left.Value, _trial.Right.Value);

            if (correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 100 + 25 * Mathf.Min(_streak - 1, 8) + (fast ? 50 : 0);
                SetStreak(_streak);
                UpdateHudText();
                _pill.Set(fast ? $"¡Rápido! {max} > {min}" : $"¡Correcto! {max} > {min}", GoodColor);
                PlayTone(523.25f * Mathf.Pow(2f, Mathf.Min(_streak - 1, 7) * 2f / 12f), 0.3f, 0.2f);
                _cards[chosen].Image.color = GoodColor;
                StartCoroutine(Flash(GoodColor, 0.10f, 0.28f));
                StartCoroutine(UiFx.SparkBurst(_fxRect, LocalIn(_fxRect, _cards[chosen].Rect), Color.white, 16, 280f, 44f, 0.6f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _cards[chosen].Rect), GoodColor, 200f, 640f, 0.5f));
                if (change == DdaChange.Up)
                {
                    UpdateHudText();
                    _toast.Show($"Nivel {_effLevel}", LevelHint(_effLevel), AccentColor, 1.1f);
                    PlayTone(1046.5f, 0.35f, 0.2f);
                }
                else if (_streak == 4 || _streak == 8 || _streak == 12 || _streak == 20)
                {
                    _toast.Show($"Racha de {_streak}", "Sigue así", GoodColor, 0.9f);
                }

                yield return StartCoroutine(ExitCards(chosen, Endless ? 0.2f : 0.3f));
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                if (change == DdaChange.Down || _dda.Struggling)
                {
                    UpdateHudText();
                    _toast.Show("Con calma", "Ajustamos la dificultad", AccentColor, 1.0f);
                }
                _pill.Set($"Era {max} > {min}", AccentColor);
                PlayTone(196f, 0.32f, 0.2f);
                _cards[chosen].Image.color = BadColor;
                StartCoroutine(Flash(BadColor, 0.14f, 0.32f));
                StartCoroutine(UiFx.Shake(24f, 0.4f, _cards[chosen].Rect));
                StartCoroutine(PopRect(_cards[winner].Rect, 1.08f, 0.3f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _cards[winner].Rect), Color.white, 200f, 620f, 0.55f));
                yield return new WaitForSeconds(Endless ? 0.6f : 0.85f);
                yield return StartCoroutine(ExitCards(-1, 0.16f));
            }
        }

        private static string LevelHint(int level)
        {
            switch (level)
            {
                case 2: return "Números cercanos";
                case 3: return "Sumas y restas";
                case 4: return "Multiplicaciones";
                case 5: return "Cuenta contra cuenta";
                default: return "Cuentas combinadas";
            }
        }

        private IEnumerator ExitCards(int grown, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                for (int i = 0; i < 2; i++)
                {
                    var c = _cards[i];
                    c.Group.alpha = 1f - k;
                    c.Rect.localScale = Vector3.one * (i == grown ? 1f + 0.10f * k : 1f - 0.06f * k);
                }
                yield return null;
            }
            foreach (var c in _cards) c.Group.alpha = 0f;
        }

        private IEnumerator FadeCardsOut()
        {
            float t = 0f;
            const float seconds = 0.15f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / seconds);
                foreach (var c in _cards) c.Group.alpha = Mathf.Min(c.Group.alpha, a);
                yield return null;
            }
            foreach (var c in _cards) c.Group.alpha = 0f;
        }

        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - Time.unscaledTime;
            SetTimerFraction(Mathf.Clamp01(left / ComparisonContract.EndlessSeconds));
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
            int total = Endless ? _totalAnswered : ComparisonContract.TotalTrials;
            int score = Endless
                ? ComparisonContract.EndlessScore(_correct, _totalAnswered)
                : ComparisonContract.PrecisionScore(_correct, total, _speedHits);
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

        private void OnCardTapped(int side)
        {
            if (!_acceptInput || _ended) return;
            _acceptInput = false;
            _answerAt = Time.unscaledTime;
            _answerSide = side;
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

            var canvasGo = new GameObject("ComparisonCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            bg.AddComponent<Image>().color = BackgroundColor;
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.12f, 0.88f), 1500f, new Color(0.96f, 0.62f, 0.04f, 0.16f));
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.92f, 0.10f), 1400f, new Color(0.23f, 0.51f, 0.96f, 0.22f));
            bg.AddComponent<CountdownAmbient>().Build(bgRect, 8);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            _dots = new ProgressDots(_safe, this, UnitsPerDp, ComparisonContract.TotalTrials);
            BuildBanner();
            BuildTimer();
            _cards[0] = BuildCard(0);
            _cards[1] = BuildCard(1);
            _pill = new PhasePill(_safe, this, UnitsPerDp, 21);

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f); // los avisos van arriba (zona del título), nunca sobre las tarjetas

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
            _titleText.text = "Comparación";
        }

        private void BuildBanner()
        {
            var go = new GameObject("QuestionBanner");
            go.transform.SetParent(_safe, false);
            _bannerRect = go.AddComponent<RectTransform>();
            _bannerRect.anchorMin = _bannerRect.anchorMax = new Vector2(0.5f, 1f);
            _bannerRect.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = Color.Lerp(new Color(0.09f, 0.13f, 0.24f, 1f), AccentColor, 0.30f);
            img.raycastTarget = false;

            var text = MakeText(go.transform, "Question", 76, TextAnchor.MiddleCenter, Color.white, 3f, 0.4f);
            text.rectTransform.offsetMin = new Vector2(30f, 0f);
            text.rectTransform.offsetMax = new Vector2(-30f, 0f);
            BestFit(text, 44);
            text.text = "¿Cuál es MAYOR?";
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
            img.color = fraction > 0.5f ? Color.Lerp(AccentColor, GoodColor, (fraction - 0.5f) * 2f)
                                        : Color.Lerp(BadColor, AccentColor, fraction * 2f);
        }

        private CardView BuildCard(int side)
        {
            var view = new CardView();
            var go = new GameObject(side == 0 ? "Card_Left" : "Card_Right");
            go.transform.SetParent(_safe, false);
            view.Rect = go.AddComponent<RectTransform>();
            view.Rect.anchorMin = view.Rect.anchorMax = new Vector2(0.5f, 1f);
            view.Rect.pivot = new Vector2(0.5f, 0.5f);
            view.Group = go.AddComponent<CanvasGroup>();
            view.Image = go.AddComponent<Image>();
            view.Image.sprite = TileSprites.Get();
            view.Image.color = SideColors[side];
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            int captured = side;
            button.onClick.AddListener(() => OnCardTapped(captured));
            go.AddComponent<PressScale>();

            // Puntos (hasta 12), reposicionados en cada ensayo.
            for (int i = 0; i < 12; i++)
            {
                var d = new GameObject("Dot_" + i);
                d.transform.SetParent(go.transform, false);
                var r = d.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                var img = d.AddComponent<Image>();
                img.sprite = DiscSprite.Get();
                img.color = new Color(1f, 1f, 1f, 0.96f);
                img.raycastTarget = false;
                view.Dots.Add(img);
            }

            view.Number = MakeText(go.transform, "Number", 230, TextAnchor.MiddleCenter, Color.white, 5f, 0.35f);
            var nr = view.Number.rectTransform;
            nr.anchorMin = new Vector2(0.10f, 0.20f);
            nr.anchorMax = new Vector2(0.90f, 0.86f);
            nr.offsetMin = nr.offsetMax = Vector2.zero;
            // Sin best-fit: el ajuste de Unity partía "37 - 11" en dos renglones. El tamaño se calcula
            // en FillCard según el ancho real de la tarjeta y siempre queda en una sola línea.
            view.Number.horizontalOverflow = HorizontalWrapMode.Overflow;
            view.Number.verticalOverflow = VerticalWrapMode.Overflow;

            view.Value = MakeText(go.transform, "Value", 64, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.92f), 2f, 0.35f);
            var vr = view.Value.rectTransform;
            vr.anchorMin = new Vector2(0.10f, 0.08f);
            vr.anchorMax = new Vector2(0.90f, 0.22f);
            vr.offsetMin = vr.offsetMax = Vector2.zero;
            BestFit(view.Value, 36);
            view.Value.gameObject.SetActive(false);
            return view;
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
            AddResultText("Detail", 52, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 44, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        private void ShowResult(int score, int avgMs, int total)
        {
            _exit.Show();
            _acceptInput = false;
            foreach (var c in _cards) c.Rect.gameObject.SetActive(false);
            _bannerRect.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            _pill.Rect.gameObject.SetActive(false);

            string title = score >= 90 ? "¡Extraordinario!" : score >= 70 ? "¡Muy bien!" : score >= 50 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = Endless
                ? $"{_correct} aciertos de {total} · {_points} puntos"
                : $"{_correct} de {total} aciertos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _responseCount > 0
                ? $"Respuesta media {avgMs / 1000f:0.0} s · mejor racha {_bestStreak}"
                : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, AccentColor, 24, 420f, 56f, 0.9f));
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = 190f;
            _dots.Rect.anchoredPosition = new Vector2(0f, -y);
            y += 70f;

            float bannerH = 130f;
            _bannerRect.sizeDelta = new Vector2(contentW, bannerH);
            _bannerRect.anchoredPosition = new Vector2(0f, -(y + bannerH / 2f));
            y += bannerH + 22f;

            _timerBg.sizeDelta = new Vector2(contentW, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 30f;

            float pillBlock = 150f;
            float remaining = sh - y;
            float gap = 24f;
            _verticalLayout = _effLevel >= 3;
            float top, blockH;
            if (_verticalLayout)
            {
                // Dos tarjetas apiladas a todo el ancho.
                float visW = contentW;
                float visH = Mathf.Clamp((remaining - pillBlock - 60f - gap) / 2f, 220f, 430f);
                blockH = visH * 2f + gap;
                top = y + Mathf.Max(0f, (remaining - blockH - pillBlock) * 0.3f);
                for (int i = 0; i < 2; i++)
                {
                    var c = _cards[i];
                    c.VisibleSize = new Vector2(visW, visH);
                    c.Rect.sizeDelta = new Vector2(visW / ShapeScale, visH / ShapeScale);
                    c.RestPos = new Vector2(0f, -(top + visH / 2f + i * (visH + gap)));
                    c.Rect.anchoredPosition = c.RestPos;
                }
            }
            else
            {
                float visW = (contentW - gap) / 2f;
                float visH = Mathf.Clamp(remaining - pillBlock - 60f, 380f, Mathf.Min(760f, visW * 1.45f));
                blockH = visH;
                top = y + Mathf.Max(0f, (remaining - visH - pillBlock) * 0.35f);
                for (int i = 0; i < 2; i++)
                {
                    var c = _cards[i];
                    c.VisibleSize = new Vector2(visW, visH);
                    c.Rect.sizeDelta = new Vector2(visW / ShapeScale, visH / ShapeScale);
                    float cx = (i == 0 ? -1f : 1f) * (visW / 2f + gap / 2f);
                    c.RestPos = new Vector2(cx, -(top + visH / 2f));
                    c.Rect.anchoredPosition = c.RestPos;
                }
            }

            // Zonas internas de cada tarjeta: número grande y, abajo, el valor real (al responder).
            foreach (var c in _cards)
            {
                var nr = c.Number.rectTransform;
                var vr = c.Value.rectTransform;
                if (_verticalLayout)
                {
                    nr.anchorMin = new Vector2(0.05f, 0.22f); nr.anchorMax = new Vector2(0.95f, 0.92f);
                    vr.anchorMin = new Vector2(0.05f, 0.04f); vr.anchorMax = new Vector2(0.95f, 0.24f);
                }
                else
                {
                    nr.anchorMin = new Vector2(0.10f, 0.20f); nr.anchorMax = new Vector2(0.90f, 0.86f);
                    vr.anchorMin = new Vector2(0.10f, 0.08f); vr.anchorMax = new Vector2(0.90f, 0.22f);
                }
                nr.offsetMin = nr.offsetMax = Vector2.zero;
                vr.offsetMin = vr.offsetMax = Vector2.zero;
            }

            float pillCenterFromTop = top + blockH + pillBlock * 0.45f;
            _pill.SetPosition(new Vector2(0f, sh / 2f - pillCenterFromTop));
        }

        // ------------------------------------------------------------------ helpers

        private void UpdateHudText()
        {
            _subText.text = Endless
                ? $"Puntos {_points} · Nivel {_effLevel}"
                : $"Ensayo {_trialIndex + 1} de {ComparisonContract.TotalTrials} · Nivel {_effLevel}";
        }

        private void SetStreak(int streak)
        {
            _streakText.text = $"Racha {streak}";
            _streakDisc.color = streak >= 3 ? AccentColor : new Color(1f, 1f, 1f, 0.30f);
            if (streak > 0) StartCoroutine(PopRect(_streakPill, 1.12f, 0.22f));
        }
    }
}
