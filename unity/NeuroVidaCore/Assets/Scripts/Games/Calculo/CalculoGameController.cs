using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / TileSprites / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Calculo
{
    /// <summary>
    /// "Cálculo Sereno" en Unity (aritmética mental). La cuenta viaja dentro de una burbuja que
    /// desciende despacio hacia un estanque; hay que tocar el resultado entre cuatro opciones antes
    /// de que la burbuja toque el agua. Al acertar se hunde con una ondulación; al fallar (o si toca
    /// el agua) se tiñe, muestra el resultado correcto y se hunde más despacio.
    /// Reto = ronda de 90 s sin límite de cuentas (la burbuja cae y el nivel sube al acertar);
    /// Precisión = 10 cuentas sin reloj (la burbuja flota quieta). Reglas de <c>CalculoGame.kt</c>
    /// más familias nuevas (ver <see cref="CalculoContract"/>). Telemetría: reusa
    /// <see cref="StroopTelemetry"/>.
    /// </summary>
    public class CalculoGameController : GameControllerBase
    {
        public const string GameId = CalculoContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int NoAnswer = -2;
        private const int TimeUp = -3;
        private const int Splashed = -4; // la burbuja tocó el agua
        private const float ShapeScale = 0.86f;

        private static readonly Color BackgroundColor = new Color(0x06 / 255f, 0x22 / 255f, 0x36 / 255f);
        private static readonly Color Aqua = new Color(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f);
        private static readonly Color BubbleCalm = new Color(0xE0 / 255f, 0xF2 / 255f, 0xFE / 255f);
        private static readonly Color BubbleWarn = new Color(0xFE / 255f, 0xF3 / 255f, 0xC7 / 255f);
        private static readonly Color BubbleDanger = new Color(0xFE / 255f, 0xCA / 255f, 0xCA / 255f);
        private static readonly Color InkColor = new Color(0x0B / 255f, 0x2A / 255f, 0x3F / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color[] OptionColors =
        {
            new Color(0x7D / 255f, 0xD3 / 255f, 0xFC / 255f),
            new Color(0x6E / 255f, 0xE7 / 255f, 0xB7 / 255f),
            new Color(0xC4 / 255f, 0xB5 / 255f, 0xFD / 255f),
            new Color(0xFD / 255f, 0xBA / 255f, 0x74 / 255f),
        };
        private static readonly Color OptionInk = new Color(0x0B / 255f, 0x2A / 255f, 0x3F / 255f);

        private System.Random _rng;

        private int _trialIndex, _correct, _streak, _bestStreak, _totalAnswered, _points, _effLevel;
        private long _responseMsSum;
        private int _responseCount;
        private int _answerIndex = NoAnswer;
        private float _answerAt;
        private bool _acceptInput, _ended;
        private MathQuestion _question;
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe, _streakPill, _timerBg, _timerFill, _fxRect, _bubbleRect, _waterRect, _optionsRoot, _waveA, _waveB;
        private Text _titleText, _subText, _streakText, _bubbleText;
        private Image _streakDisc, _timerFillImage, _bubbleFill, _bubbleBorder;
        private CanvasGroup _bubbleGroup;
        private ProgressDots _dots;
        private Toast _toast;
        private ExitButton _exit;
        private CountdownScreen _countdown;
        private readonly List<RectTransform> _optionRects = new List<RectTransform>();
        private readonly List<Image> _optionImages = new List<Image>();
        private readonly List<Text> _optionLabels = new List<Text>();
        private float _bubbleTopY, _bubbleWaterY, _bubbleRestY, _waterSurfaceY, _contentW, _bubbleH;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _trialIndex = _correct = _streak = _bestStreak = _totalAnswered = _points = 0;
            _responseMsSum = 0;
            _responseCount = 0;
            _ended = false;
            _acceptInput = false;
            _lastTickSecond = -1;
            _dda = new AdaptiveDifficulty(CalculoContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, CalculoContract.MaxLevel),
                stepUp: config.config.timed ? 0.15f : 0.25f, useReaction: config.config.timed);
            _effLevel = _dda.PresentedLevel;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _bubbleRect.gameObject.SetActive(true);
            _waterRect.gameObject.SetActive(true);
            _optionsRoot.gameObject.SetActive(true);
            SetStreak(0);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
            StartCoroutine(WaveLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Cálculo Sereno", "Respira y empieza", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _roundEndsAt = Time.unscaledTime + CalculoContract.EndlessSeconds;
            _trialIndex = 0;
            while (Endless ? Time.unscaledTime < _roundEndsAt : _trialIndex < CalculoContract.TotalTrials)
            {
                yield return StartCoroutine(PresentQuestion());

                float startedAt = Time.unscaledTime;
                float fall = CalculoContract.FallSeconds(_effLevel, _config.config.base_intensity);
                float lastWarn = 0f;
                while (_answerIndex == NoAnswer)
                {
                    float t = Time.unscaledTime - startedAt;
                    if (Endless)
                    {
                        if (UpdateRoundClock()) { _answerIndex = TimeUp; break; }
                        float progress = Mathf.Clamp01(t / fall);
                        SetBubble(progress);
                        if (progress > 0.72f && Time.unscaledTime - lastWarn > 0.7f)
                        {
                            lastWarn = Time.unscaledTime;
                            PlayTone(660f, 0.06f, 0.07f);
                        }
                        if (progress >= 1f) { _answerIndex = Splashed; break; }
                    }
                    else
                    {
                        // Sin reloj: la burbuja flota suavemente en su sitio.
                        _bubbleRect.anchoredPosition = new Vector2(0f, _bubbleRestY + Mathf.Sin(Time.unscaledTime * 1.6f) * 10f);
                    }
                    yield return null;
                }
                _acceptInput = false;

                if (_answerIndex == TimeUp)
                {
                    yield return StartCoroutine(FadeBubble(0.18f));
                    break;
                }

                bool tapped = _answerIndex >= 0;
                bool correct = tapped && _question.Options[_answerIndex] == _question.Answer;
                _totalAnswered++;
                if (tapped)
                {
                    _responseMsSum += (long)((_answerAt - startedAt) * 1000f);
                    _responseCount++;
                }
                yield return StartCoroutine(ResolveQuestion(correct, tapped, tapped ? (_answerAt - startedAt) * 1000f : -1f));
                _trialIndex++;
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ cuenta

        private IEnumerator PresentQuestion()
        {
            _answerIndex = NoAnswer;
            _effLevel = _dda.PresentedLevel;
            int live = CalculoContract.LiveIntensity(_config.config.base_intensity, _streak);
            _question = CalculoContract.Generate(_effLevel, live, _rng);
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();

            _bubbleText.text = _question.Prompt;
            FitBubbleText(_question.Prompt);
            _bubbleText.color = InkColor;
            _bubbleFill.color = BubbleCalm;
            _bubbleBorder.color = new Color(1f, 1f, 1f, 0.75f);
            for (int i = 0; i < 4; i++)
            {
                _optionLabels[i].text = _question.Options[i].ToString();
                FitOption(i);
                _optionImages[i].color = OptionColors[i];
                _optionRects[i].localScale = Vector3.zero;
            }

            float startY = Endless ? _bubbleTopY : _bubbleRestY;
            _bubbleRect.anchoredPosition = new Vector2(0f, startY);
            _bubbleRect.localScale = Vector3.zero;
            _bubbleGroup.alpha = 0f;

            float t = 0f;
            const float seconds = 0.26f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _bubbleRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, UiFx.EaseOutBack(k));
                _bubbleGroup.alpha = Mathf.Clamp01(k * 2.5f);
                yield return null;
            }
            _bubbleRect.localScale = Vector3.one;
            _bubbleGroup.alpha = 1f;
            PlayTone(587.33f, 0.14f, 0.09f);

            for (int i = 0; i < 4; i++)
            {
                StartCoroutine(PopIn(_optionRects[i], 0.2f));
                yield return new WaitForSeconds(0.04f);
            }
            _acceptInput = true;
        }

        private void SetBubble(float progress)
        {
            _bubbleRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(_bubbleTopY, _bubbleWaterY, progress));
            // Del calmo al aviso al peligro: el color de la burbuja lo dice sin números.
            Color c = progress < 0.6f ? BubbleCalm
                : progress < 0.85f ? Color.Lerp(BubbleCalm, BubbleWarn, (progress - 0.6f) / 0.25f)
                : Color.Lerp(BubbleWarn, BubbleDanger, (progress - 0.85f) / 0.15f);
            _bubbleFill.color = c;
        }

        private IEnumerator ResolveQuestion(bool correct, bool tapped, float reactionMs)
        {
            var change = _dda.Register(correct, reactionMs);
            _effLevel = _dda.Level;
            int chosen = _answerIndex;
            int answerIndex = System.Array.IndexOf(_question.Options, _question.Answer);
            if (!Endless) _dots.Mark(_trialIndex, correct);

            // El signo "?" se reemplaza por la respuesta.
            _bubbleText.text = _question.Prompt.Replace("?", _question.Answer.ToString());
            FitBubbleText(_bubbleText.text);
            Vector2 surface = new Vector2(0f, _waterSurfaceY);

            if (correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += CalculoContract.PointsFor(_streak);
                SetStreak(_streak);
                UpdateHudText();
                _bubbleFill.color = new Color(0.78f, 0.97f, 0.85f);
                _optionImages[chosen].color = GoodColor;
                PlayTone(523.25f * Mathf.Pow(2f, Mathf.Min(_streak - 1, 7) * 2f / 12f), 0.3f, 0.2f);
                StartCoroutine(Flash(GoodColor, 0.08f, 0.26f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _optionRects[chosen]), Color.white, 120f, 420f, 0.45f));

                if (change == DdaChange.Up)
                {
                    UpdateHudText();
                    _toast.Show($"Nivel {_effLevel}", LevelHint(_effLevel), Aqua, 1.1f);
                    PlayTone(1046.5f, 0.35f, 0.2f);
                }
                else if (_streak == 4 || _streak == 8 || _streak == 12)
                {
                    _toast.Show($"Racha de {_streak}", "Mente serena", GoodColor, 0.9f);
                }
                yield return StartCoroutine(SinkBubble(0.32f, true));
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                if (change == DdaChange.Down || _dda.Struggling)
                {
                    UpdateHudText();
                    _toast.Show("Con calma", "Ajustamos la dificultad", AmberColor, 1.0f);
                }
                _bubbleFill.color = BubbleDanger;
                _bubbleBorder.color = new Color(BadColor.r, BadColor.g, BadColor.b, 0.9f);
                PlayTone(196f, 0.34f, 0.2f);
                StartCoroutine(Flash(BadColor, 0.12f, 0.3f));
                StartCoroutine(UiFx.Shake(20f, 0.4f, _bubbleRect));
                for (int i = 0; i < 4; i++)
                {
                    if (i == answerIndex) continue;
                    if (i == chosen) _optionImages[i].color = new Color(BadColor.r, BadColor.g, BadColor.b, 1f);
                    else _optionImages[i].color = Color.Lerp(OptionColors[i], new Color(0.2f, 0.28f, 0.38f), 0.7f);
                }
                StartCoroutine(PopRect(_optionRects[answerIndex], 1.12f, 0.3f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _optionRects[answerIndex]), Color.white, 140f, 460f, 0.55f));
                if (_answerIndex == Splashed)
                    StartCoroutine(UiFx.RingBurst(_fxRect, surface + new Vector2(0f, 0f), Aqua, 200f, 800f, 0.7f));
                yield return new WaitForSeconds(Endless ? 0.9f : 1.3f);
                yield return StartCoroutine(SinkBubble(0.4f, false));
            }
        }

        /// <summary>La burbuja se hunde en el estanque: ondulación en la superficie y se desvanece.</summary>
        private IEnumerator SinkBubble(float seconds, bool happy)
        {
            Vector2 from = _bubbleRect.anchoredPosition;
            float targetY = _bubbleWaterY - _bubbleH * 0.55f;
            bool rippled = false;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                _bubbleRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(from.y, targetY, k));
                _bubbleRect.localScale = Vector3.one * (1f - 0.25f * k);
                _bubbleGroup.alpha = 1f - Mathf.Clamp01((k - 0.35f) / 0.65f);
                if (!rippled && k > 0.5f)
                {
                    rippled = true;
                    StartCoroutine(UiFx.RingBurst(_fxRect, new Vector2(0f, _waterSurfaceY), happy ? Color.white : Aqua, 160f, 700f, 0.6f));
                    if (happy) StartCoroutine(UiFx.SparkBurst(_fxRect, new Vector2(0f, _waterSurfaceY), Aqua, 10, 240f, 34f, 0.5f));
                }
                yield return null;
            }
            _bubbleGroup.alpha = 0f;
        }

        private IEnumerator FadeBubble(float seconds)
        {
            float t = 0f;
            float from = _bubbleGroup.alpha;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _bubbleGroup.alpha = from * (1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _bubbleGroup.alpha = 0f;
        }

        private static string LevelHint(int level)
        {
            switch (level)
            {
                case 2: return "Sumas y restas más grandes";
                case 3: return "Tablas de multiplicar";
                case 4: return "Multiplicar y dividir";
                case 5: return "Porcentajes y números que faltan";
                case 6: return "Paréntesis";
                case 7: return "Cuentas grandes y cuadrados";
                case 8: return "Cuentas encadenadas";
                default: return "Porcentajes difíciles";
            }
        }

        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - Time.unscaledTime;
            float f = Mathf.Clamp01(left / CalculoContract.EndlessSeconds);
            _timerFill.anchorMax = new Vector2(f, 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage.color = f > 0.5f ? Color.Lerp(AmberColor, Aqua, (f - 0.5f) * 2f) : Color.Lerp(BadColor, AmberColor, f * 2f);
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
            int total = Endless ? _totalAnswered : CalculoContract.TotalTrials;
            int score = Endless ? CalculoContract.EndlessScore(_correct, _totalAnswered) : CalculoContract.Score(_points, total);
            int avgMs = _responseCount > 0 ? (int)(_responseMsSum / _responseCount) : 0;

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

        private void OnOptionTapped(int index)
        {
            if (!_acceptInput || _ended) return;
            _acceptInput = false;
            _answerAt = Time.unscaledTime;
            _answerIndex = index;
        }

        // ------------------------------------------------------------------ agua

        private IEnumerator WaveLoop()
        {
            // Dos ondas lentas de luz que se cruzan sobre el estanque.
            while (true)
            {
                float t = Time.unscaledTime;
                if (_waveA != null) _waveA.anchoredPosition = new Vector2(Mathf.Sin(t * 0.5f) * 160f, _waveA.anchoredPosition.y);
                if (_waveB != null) _waveB.anchoredPosition = new Vector2(Mathf.Sin(t * 0.37f + 2f) * -200f, _waveB.anchoredPosition.y);
                yield return null;
            }
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

            var canvasGo = new GameObject("CalculoCanvas");
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
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.15f, 0.90f), 1500f, new Color(0.22f, 0.74f, 0.97f, 0.20f));
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.90f, 0.15f), 1400f, new Color(0.65f, 0.55f, 0.98f, 0.14f));
            bg.AddComponent<CountdownAmbient>().Build(bgRect, 10);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            _dots = new ProgressDots(_safe, this, UnitsPerDp, CalculoContract.TotalTrials);
            BuildTimer();
            BuildWater();
            BuildBubble();
            BuildOptions();

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f);

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
            _titleText.text = "Cálculo Sereno";
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
            _timerFillImage.color = Aqua;
        }

        private void BuildWater()
        {
            var go = new GameObject("Water");
            go.transform.SetParent(_safe, false);
            _waterRect = go.AddComponent<RectTransform>();
            _waterRect.anchorMin = _waterRect.anchorMax = new Vector2(0.5f, 1f);
            _waterRect.pivot = new Vector2(0.5f, 1f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(56);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.22f, 0.74f, 0.97f, 0.26f);
            img.raycastTarget = false;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.65f, 0.9f, 1f, 0.35f);
            outline.effectDistance = new Vector2(3f, -3f);

            // Dos manchas de luz que se deslizan sobre el agua.
            _waveA = MakeWave(go.transform, new Color(0.7f, 0.93f, 1f, 0.16f), 0.68f);
            _waveB = MakeWave(go.transform, new Color(0.55f, 0.85f, 1f, 0.12f), 0.32f);
        }

        private static RectTransform MakeWave(Transform parent, Color color, float heightFraction)
        {
            var go = new GameObject("Wave");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, heightFraction);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(760f, 46f);
            var img = go.AddComponent<Image>();
            img.sprite = RadialGlowSprite.Get();
            img.color = color;
            img.raycastTarget = false;
            return r;
        }

        private void BuildBubble()
        {
            var go = new GameObject("Bubble");
            go.transform.SetParent(_safe, false);
            _bubbleRect = go.AddComponent<RectTransform>();
            _bubbleRect.anchorMin = _bubbleRect.anchorMax = new Vector2(0.5f, 1f);
            _bubbleRect.pivot = new Vector2(0.5f, 0.5f);
            _bubbleGroup = go.AddComponent<CanvasGroup>();
            _bubbleGroup.blocksRaycasts = false;

            var shadowGo = new GameObject("Glow");
            shadowGo.transform.SetParent(go.transform, false);
            var sr = shadowGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(-0.08f, -0.55f);
            sr.anchorMax = new Vector2(1.08f, 1.25f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            var sImg = shadowGo.AddComponent<Image>();
            sImg.sprite = RadialGlowSprite.Get();
            sImg.color = new Color(0.4f, 0.85f, 1f, 0.28f);
            sImg.raycastTarget = false;

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(go.transform, false);
            Stretch(borderGo.AddComponent<RectTransform>());
            _bubbleBorder = borderGo.AddComponent<Image>();
            _bubbleBorder.sprite = RoundedRectSprite.Get(64);
            _bubbleBorder.type = Image.Type.Sliced;
            _bubbleBorder.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fr = fillGo.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(7f, 7f);
            fr.offsetMax = new Vector2(-7f, -7f);
            _bubbleFill = fillGo.AddComponent<Image>();
            _bubbleFill.sprite = RoundedRectSprite.Get(60);
            _bubbleFill.type = Image.Type.Sliced;
            _bubbleFill.raycastTarget = false;

            // Brillo suave arriba-izquierda para que parezca una burbuja y no un rectángulo.
            var shineGo = new GameObject("Shine");
            shineGo.transform.SetParent(go.transform, false);
            var shr = shineGo.AddComponent<RectTransform>();
            shr.anchorMin = new Vector2(0.04f, 0.58f);
            shr.anchorMax = new Vector2(0.46f, 0.96f);
            shr.offsetMin = shr.offsetMax = Vector2.zero;
            var shImg = shineGo.AddComponent<Image>();
            shImg.sprite = RadialGlowSprite.Get();
            shImg.color = new Color(1f, 1f, 1f, 0.55f);
            shImg.raycastTarget = false;

            _bubbleText = MakeText(go.transform, "Equation", 130, TextAnchor.MiddleCenter, InkColor, 0f, 0f);
            _bubbleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _bubbleText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void BuildOptions()
        {
            var root = new GameObject("Options");
            root.transform.SetParent(_safe, false);
            _optionsRoot = root.AddComponent<RectTransform>();
            _optionsRoot.anchorMin = _optionsRoot.anchorMax = new Vector2(0.5f, 0f);
            _optionsRoot.pivot = new Vector2(0.5f, 0.5f);
            _optionsRoot.sizeDelta = new Vector2(10f, 10f);

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var go = new GameObject("Option_" + i);
                go.transform.SetParent(_optionsRoot, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = TileSprites.Get();
                img.color = OptionColors[i];
                img.alphaHitTestMinimumThreshold = 0.1f;
                var button = go.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnOptionTapped(index));
                go.AddComponent<PressScale>();

                var label = MakeText(go.transform, "Value", 120, TextAnchor.MiddleCenter, OptionInk, 0f, 0f);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                var lr = label.rectTransform;
                lr.anchorMin = new Vector2(0.06f, 0.16f);
                lr.anchorMax = new Vector2(0.94f, 0.86f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;

                _optionRects.Add(rect);
                _optionImages.Add(img);
                _optionLabels.Add(label);
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
            img.color = new Color(0.05f, 0.16f, 0.26f, 0.96f);
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
            _bubbleRect.gameObject.SetActive(false);
            _waterRect.gameObject.SetActive(false);
            _optionsRoot.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);

            string title = score >= 90 ? "¡Mente brillante!" : score >= 70 ? "¡Muy bien!" : score >= 50 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = Endless
                ? $"{_correct} cuentas correctas de {total} · {_points} puntos"
                : $"{_correct} de {total} correctas · {_points} puntos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _responseCount > 0
                ? $"Respuesta media {avgMs / 1000f:0.0} s · mejor racha {_bestStreak}"
                : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, Aqua, 24, 420f, 56f, 0.9f));
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;
            _contentW = contentW;

            float y = 190f;
            _dots.Rect.anchoredPosition = new Vector2(0f, -y);
            y += 70f;
            _timerBg.sizeDelta = new Vector2(contentW, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 30f;
            float remaining = sh - y;

            // Opciones 2x2 abajo, el estanque justo encima y la burbuja cayendo entre el título y el agua.
            float waterH = 130f;
            float cell = Mathf.Clamp(Mathf.Min((contentW - 20f) / 2f * 0.98f, (remaining - waterH - 380f) / 2.05f), 150f, 300f);
            float optH = cell * 2f * 0.98f;
            float bottomMargin = 30f;
            float optCenterFromBottom = bottomMargin + optH / 2f;
            _optionsRoot.anchoredPosition = new Vector2(0f, optCenterFromBottom);
            float g = cell * 0.98f;
            for (int i = 0; i < 4; i++)
            {
                int r = i / 2, c = i % 2;
                _optionRects[i].sizeDelta = new Vector2(cell, cell);
                _optionRects[i].anchoredPosition = new Vector2((c - 0.5f) * g, (0.5f - r) * g);
            }

            float waterTop = sh - bottomMargin - optH - 26f - waterH; // distancia desde arriba
            _waterRect.sizeDelta = new Vector2(contentW, waterH);
            _waterRect.anchoredPosition = new Vector2(0f, -waterTop);
            _waterSurfaceY = sh / 2f - waterTop; // en coordenadas centradas para los efectos

            _bubbleH = Mathf.Clamp((waterTop - y) * 0.34f, 150f, 230f);
            _bubbleRect.sizeDelta = new Vector2(contentW * 0.9f, _bubbleH);
            // Posiciones (y anclada arriba): comienza justo bajo el reloj y llega al agua.
            _bubbleTopY = -(y + _bubbleH / 2f + 14f);
            _bubbleWaterY = -(waterTop - _bubbleH * 0.15f);
            _bubbleRestY = -(y + (waterTop - y) * 0.42f);
            _toast.SetTopOffset(0f);
        }

        private void FitBubbleText(string text)
        {
            float availW = _bubbleRect.sizeDelta.x * 0.86f;
            float size = Mathf.Min(150f, availW / Mathf.Max(3f, EmWidth(text)), _bubbleH * 0.55f);
            _bubbleText.fontSize = Mathf.RoundToInt(Mathf.Max(48f, size));
        }

        private void FitOption(int i)
        {
            string text = _optionLabels[i].text;
            float cell = _optionRects[i].sizeDelta.x;
            float availW = cell * ShapeScale * 0.88f;
            float size = Mathf.Min(150f, availW / Mathf.Max(1.2f, text.Length * 0.62f), cell * 0.42f);
            _optionLabels[i].fontSize = Mathf.RoundToInt(Mathf.Max(40f, size));
        }

        private static float EmWidth(string s)
        {
            float w = 0f;
            foreach (char c in s) w += char.IsDigit(c) ? 0.62f : c == ' ' ? 0.28f : 0.6f;
            return w;
        }

        // ------------------------------------------------------------------ helpers

        private void UpdateHudText()
        {
            _subText.text = Endless
                ? $"Puntos {_points} · Nivel {_effLevel}"
                : $"Cuenta {_trialIndex + 1} de {CalculoContract.TotalTrials} · {_points} pts";
        }

        private void SetStreak(int streak)
        {
            _streakText.text = $"Racha {streak}";
            _streakDisc.color = streak >= 3 ? AmberColor : new Color(1f, 1f, 1f, 0.30f);
            if (streak > 0) StartCoroutine(PopRect(_streakPill, 1.12f, 0.22f));
        }
    }
}
