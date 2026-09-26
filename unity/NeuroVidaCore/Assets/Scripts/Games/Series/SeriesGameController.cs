using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite / TileSprites / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Series
{
    /// <summary>
    /// "Detective de Series" en Unity (razonamiento lógico). Cuatro fichas numéricas aparecen una a
    /// una y una quinta, con un "?", late esperando. Se elige el número que sigue entre cuatro
    /// opciones. Al responder se REVELA la regla sobre el mismo tablero: entre las fichas aparecen
    /// los pasos ("+5", "×3"...) y el "?" se convierte en la respuesta, además de la explicación en
    /// texto. Arte propio: una lupa de arcilla (<see cref="MagnifierSprite"/>) "busca" sobre la ficha "?" y los
    /// números de las fichas van en arcilla (contorno tinta y sombra dura). Reto = ronda de 120 s sin límite de series (sube de nivel al ir acertando);
    /// Precisión = 8 series sin reloj. Reglas de <c>SeriesGame.kt</c> (ver <see cref="SeriesContract"/>).
    /// Telemetría: reusa <see cref="StroopTelemetry"/>.
    /// </summary>
    public class SeriesGameController : GameControllerBase
    {
        public const string GameId = SeriesContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int NoAnswer = -2;
        private const int TimeUp = -3;
        private const float ShapeScale = 0.86f;

        private static readonly Color Accent = new Color(0xA7 / 255f, 0x8B / 255f, 0xFA / 255f);
        private static readonly Color LensColor = new Color(0xFB / 255f, 0xBF / 255f, 0x24 / 255f);
        private static readonly Color TermColor = new Color(0x5B / 255f, 0x6C / 255f, 0xF0 / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color[] OptionColors =
        {
            new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f),
            new Color(0xEC / 255f, 0x48 / 255f, 0x99 / 255f),
            new Color(0x14 / 255f, 0xB8 / 255f, 0xA6 / 255f),
            new Color(0xF9 / 255f, 0x73 / 255f, 0x16 / 255f),
        };

        private sealed class Token
        {
            public RectTransform Rect;
            public Image Image;
            public Text Label;
            public RectTransform Chip;
            public Text ChipLabel;
            public Image ChipBg;
        }

        private System.Random _rng;

        private int _trialIndex, _correct, _streak, _bestStreak, _totalAnswered, _points, _effLevel;
        private long _responseMsSum;
        private int _responseCount;
        private int _answerIndex = NoAnswer; // índice de la opción tocada
        private float _answerAt;
        private bool _acceptInput, _ended;
        private SeriesItem _item;
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe, _bannerRect, _timerBg, _timerFill, _fxRect, _rowRect, _optionsRoot;
                private Image _timerFillImage, _lensRing, _bannerImage, _magnifier;
        private Text _bannerText;
        private ProgressDots _dots;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;
        private const int MaxTokens = 7; // hasta 6 términos + la ficha "?"
        private readonly Token[] _tokens = new Token[MaxTokens];
        private int _count = 5;        // fichas en pantalla en la serie actual (términos + "?")
        private float _contentW = 900f;
        private readonly List<RectTransform> _optionRects = new List<RectTransform>();
        private readonly List<Image> _optionImages = new List<Image>();
        private readonly List<Text> _optionLabels = new List<Text>();
        private float _tokenSize;

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
            // DDA común: rating continuo sobre la escalera de 9 pasos (el nivel elegido 1-5 se reparte en toda la escalera).
            _dda = new AdaptiveDifficulty(SeriesContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, SeriesContract.MaxLevel),
                stepUp: config.config.timed ? 0.15f : 0.25f, useReaction: config.config.timed);
            _effLevel = _dda.PresentedLevel;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _bannerRect.gameObject.SetActive(true);
            _rowRect.gameObject.SetActive(true);
            _optionsRoot.gameObject.SetActive(true);
            _pill.Rect.gameObject.SetActive(false); // ya no se usa: la explicación va en el cartel superior
            SetStreak(0);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Detective de Series", "Prepárate", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _roundEndsAt = GameClock.Time + SeriesContract.EndlessSeconds;
            _trialIndex = 0;
            while (Endless ? GameClock.Time < _roundEndsAt : _trialIndex < SeriesContract.TotalTrials)
            {
                yield return StartCoroutine(PresentSeries());

                float startedAt = GameClock.Time;
                while (_answerIndex == NoAnswer)
                {
                    if (Endless && UpdateRoundClock()) _answerIndex = TimeUp;
                    yield return null;
                }
                _acceptInput = false;

                if (_answerIndex == TimeUp)
                {
                    yield return StartCoroutine(RowOut(0.15f));
                    break;
                }

                bool correct = _item.Options[_answerIndex] == _item.Answer;
                _totalAnswered++;
                _responseMsSum += (long)((_answerAt - startedAt) * 1000f);
                _responseCount++;
                yield return StartCoroutine(ResolveSeries(correct, (_answerAt - startedAt) * 1000f));
                _trialIndex++;
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ serie

        private IEnumerator PresentSeries()
        {
            _answerIndex = NoAnswer;
            _effLevel = _dda.PresentedLevel;
            int live = SeriesContract.LiveIntensity(_config.config.base_intensity, _streak);
            _item = SeriesContract.Generate(_effLevel, live, _rng);
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();
            SetBanner("¿Qué número sigue?", Accent, 0.30f);

            // Cantidad de fichas según el patrón (4 a 6 términos + "?"): se reparten a lo ancho.
            ApplyRow(_item.Terms.Length + 1);

            // Estado inicial: fichas ocultas, opciones ocultas.
            for (int i = 0; i < MaxTokens; i++)
            {
                var t = _tokens[i];
                t.Chip.gameObject.SetActive(false);
                if (i >= _count)
                {
                    t.Rect.gameObject.SetActive(false);
                    continue;
                }
                bool isTerm = i < _count - 1;
                t.Rect.gameObject.SetActive(true);
                t.Rect.localScale = Vector3.zero;
                t.Image.color = isTerm ? TermColor : LensColor;
                t.Label.color = Color.white;
                t.Label.text = isTerm ? _item.Terms[i].ToString() : "?";
                FitToken(t, t.Label.text);
            }
            _lensRing.color = new Color(LensColor.r, LensColor.g, LensColor.b, 0f);
            _magnifier.gameObject.SetActive(false);
            _rowRect.localScale = Vector3.one;
            _rowRect.anchoredPosition = _rowRest;
            for (int i = 0; i < 4; i++)
            {
                _optionLabels[i].text = _item.Options[i].ToString();
                FitOption(i);
                _optionImages[i].color = OptionColors[i];
                _optionRects[i].localScale = Vector3.zero;
            }

            // Las fichas aparecen de izquierda a derecha con un tic ascendente.
            for (int i = 0; i < _count; i++)
            {
                StartCoroutine(PopIn(_tokens[i].Rect, 0.22f));
                PlayTone(392f * Mathf.Pow(2f, i * 2f / 12f), 0.12f, 0.10f);
                yield return new WaitForSeconds(i == _count - 2 ? 0.14f : 0.09f);
            }
            _magnifier.gameObject.SetActive(true);
            StartCoroutine(PopIn(_magnifier.rectTransform, 0.22f));
            StartCoroutine(PulseLens());

            // Opciones con rebote escalonado.
            for (int i = 0; i < 4; i++)
            {
                StartCoroutine(PopIn(_optionRects[i], 0.2f));
                yield return new WaitForSeconds(0.05f);
            }
            yield return new WaitForSeconds(0.12f);
            _acceptInput = true;
        }

        private IEnumerator PulseLens()
        {
            // Aro que late alrededor de la ficha "?" mientras no se responda.
            while (_answerIndex == NoAnswer && !_ended)
            {
                float t = Mathf.PingPong(GameClock.Time * 1.6f, 1f);
                _lensRing.color = new Color(LensColor.r, LensColor.g, LensColor.b, 0.25f + 0.55f * t);
                _lensRing.rectTransform.localScale = Vector3.one * (1.02f + 0.10f * t);
                _magnifier.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f + 16f * t); // la lupa "busca"
                yield return null;
            }
            _lensRing.color = new Color(LensColor.r, LensColor.g, LensColor.b, 0f);
        }

        private IEnumerator ResolveSeries(bool correct, float reactionMs)
        {
            var change = _dda.Register(correct, reactionMs);
            _effLevel = _dda.Level;
            int chosen = _answerIndex;
            int answerIndex = System.Array.IndexOf(_item.Options, _item.Answer);
            if (!Endless) _dots.Mark(_trialIndex, correct);

            _magnifier.gameObject.SetActive(false);
            var answerToken = _tokens[_count - 1];
            answerToken.Label.text = _item.Answer.ToString();
            answerToken.Label.color = Color.white;
            answerToken.Image.color = GoodColor;
            FitToken(answerToken, answerToken.Label.text);
            StartCoroutine(PopRect(answerToken.Rect, 1.25f, 0.3f));

            if (correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 100 + 25 * Mathf.Min(_streak - 1, 8);
                SetStreak(_streak);
                UpdateHudText();
                _optionImages[chosen].color = GoodColor;
                SetBanner(_item.Rule, GoodColor, 0.45f);
                GameFeel.Correct(_streak);
                StartCoroutine(Flash(GoodColor, 0.10f, 0.28f));
                StartCoroutine(UiFx.SparkBurst(_fxRect, LocalIn(_fxRect, answerToken.Rect), Color.white, 14, 260f, 42f, 0.55f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _optionRects[chosen]), Color.white, 120f, 420f, 0.45f));

                if (change == DdaChange.Up)
                {
                    UpdateHudText();
                    _toast.Show($"Nivel {_effLevel}", "Aparecen patrones nuevos", Accent, 1.1f);
                    GameFeel.LevelUp();
                }
                else if (_streak == 3 || _streak == 6 || _streak == 10)
                {
                    _toast.Show($"Racha de {_streak}", "Buen ojo, detective", GoodColor, 0.9f);
                }
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                if (change == DdaChange.Down || _dda.Struggling)
                {
                    // Dos errores seguidos (o un nivel menos): aviso amable, la dificultad se ajusta sola.
                    UpdateHudText();
                    _toast.Show("Con calma", "Ajustamos la dificultad", AmberColor, 1.0f);
                }
                _optionImages[chosen].color = BadColor;
                SetBanner(_item.Rule, AmberColor, 0.40f);
                GameFeel.Wrong();
                StartCoroutine(Flash(BadColor, 0.14f, 0.32f));
                StartCoroutine(UiFx.Shake(20f, 0.4f, _optionRects[chosen]));
                for (int i = 0; i < 4; i++)
                    if (i != answerIndex && i != chosen)
                        _optionImages[i].color = Color.Lerp(OptionColors[i], new Color(0.25f, 0.27f, 0.36f), 0.7f);
                StartCoroutine(PopRect(_optionRects[answerIndex], 1.12f, 0.3f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _optionRects[answerIndex]), Color.white, 140f, 460f, 0.55f));
            }

            // Se revela la regla: los pasos aparecen entre las fichas, de izquierda a derecha.
            for (int i = 0; i < _count - 1; i++)
            {
                var t = _tokens[i];
                t.Chip.gameObject.SetActive(true);
                t.ChipLabel.text = _item.StepLabels[i];
                t.ChipLabel.fontSize = Mathf.RoundToInt(Mathf.Clamp(t.Chip.sizeDelta.x * 0.9f / (Mathf.Max(2, t.ChipLabel.text.Length) * 0.56f), 26f, 46f));
                t.ChipBg.color = correct ? new Color(GoodColor.r, GoodColor.g, GoodColor.b, 0.95f) : new Color(LensColor.r, LensColor.g, LensColor.b, 0.95f);
                StartCoroutine(PopIn(t.Chip, 0.2f));
                yield return new WaitForSeconds(0.11f);
            }

            // Tiempo para leer la regla: más si se falló (así se aprende), menos si el reloj corre.
            float read = correct ? (Endless ? 0.6f : 1.0f) : (Endless ? 1.3f : 1.9f);
            yield return new WaitForSeconds(read);
            yield return StartCoroutine(RowOut(0.18f));
        }

        private IEnumerator RowOut(float seconds)
        {
            float t = 0f;
            var optionScales = new Vector3[4];
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                _rowRect.anchoredPosition = _rowRest + new Vector2(-220f * k, 0f);
                foreach (var tok in _tokens)
                {
                    tok.Rect.localScale = Vector3.one * (1f - k);
                    tok.Chip.localScale = Vector3.one * (1f - k);
                }
                for (int i = 0; i < 4; i++) _optionRects[i].localScale = Vector3.one * (1f - k);
                yield return null;
            }
            foreach (var tok in _tokens) tok.Rect.localScale = Vector3.zero;
        }

        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - GameClock.Time;
            float f = Mathf.Clamp01(left / SeriesContract.EndlessSeconds);
            _timerFill.anchorMax = new Vector2(f, 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage.color = f > 0.5f ? Color.Lerp(AmberColor, GoodColor, (f - 0.5f) * 2f) : Color.Lerp(BadColor, AmberColor, f * 2f);
            int whole = Mathf.CeilToInt(left);
            if (whole <= 5 && whole >= 1 && whole != _lastTickSecond)
            {
                _lastTickSecond = whole;
                GameFeel.Tick();
            }
            return left <= 0f;
        }

        private IEnumerator FinishGame()
        {
            _ended = true;
            int total = Endless ? _totalAnswered : SeriesContract.TotalTrials;
            int score = Endless ? SeriesContract.EndlessScore(_correct, _totalAnswered) : SeriesContract.Score(_correct, total);
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
            yield break;
        }

        private void OnOptionTapped(int index)
        {
            if (!_acceptInput || _ended) return;
            _acceptInput = false;
            _answerAt = GameClock.Time;
            _answerIndex = index;
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

            var canvasGo = new GameObject("SeriesCanvas");
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
            // Mundo "MeteorShower": cielo nocturno de la app + su elemento propio (ver Shared/WorldBackdrop.cs).
            WorldBackdrop.Build(bgRect, GameWorld.MeteorShower);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            _dots = new ProgressDots(_safe, this, UnitsPerDp, SeriesContract.TotalTrials);
            BuildBanner();
            BuildTimer();
            BuildRow();
            _pill = new PhasePill(_safe, this, UnitsPerDp, 21);
            BuildOptions();

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f); // avisos arriba (zona del título), nunca sobre las fichas

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
            _hud = new GameHud(_safe, "Detective de Series", MarginU, this);
        }

        private void BuildBanner()
        {
            var go = new GameObject("QuestionBanner");
            go.transform.SetParent(_safe, false);
            _bannerRect = go.AddComponent<RectTransform>();
            _bannerRect.anchorMin = _bannerRect.anchorMax = new Vector2(0.5f, 1f);
            _bannerRect.pivot = new Vector2(0.5f, 0.5f);
            _bannerImage = go.AddComponent<Image>();
            _bannerImage.sprite = RoundedRectSprite.Get(64);
            _bannerImage.type = Image.Type.Sliced;
            _bannerImage.color = BannerColor(Accent, 0.30f);
            _bannerImage.raycastTarget = false;

            _bannerText = MakeText(go.transform, "Question", 70, TextAnchor.MiddleCenter, Color.white, 3f, 0.4f);
            _bannerText.rectTransform.offsetMin = new Vector2(30f, 0f);
            _bannerText.rectTransform.offsetMax = new Vector2(-30f, 0f);
            BestFit(_bannerText, 34);
            _bannerText.text = "Descubre la regla de la serie";
        }

        private static Color BannerColor(Color accent, float amount) =>
            Color.Lerp(new Color(0.09f, 0.08f, 0.22f, 1f), accent, amount);

        /// <summary>El cartel superior hace de "pizarra": pregunta al empezar y explica la regla al
        /// responder. Antes la explicación salía en una píldora entre las fichas y las opciones y tapaba
        /// las etiquetas de los pasos (+5, ×3...).</summary>
        private void SetBanner(string text, Color accent, float amount)
        {
            _bannerText.text = text;
            _bannerImage.color = BannerColor(accent, amount);
            StartCoroutine(PopRect(_bannerRect, 1.04f, 0.2f));
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
            _timerFillImage.color = GoodColor;
        }

        private void BuildRow()
        {
            var row = new GameObject("SeriesRow");
            row.transform.SetParent(_safe, false);
            _rowRect = row.AddComponent<RectTransform>();
            _rowRect.anchorMin = _rowRect.anchorMax = new Vector2(0.5f, 1f);
            _rowRect.pivot = new Vector2(0.5f, 0.5f);
            _rowRect.sizeDelta = new Vector2(10f, 10f);

            for (int i = 0; i < MaxTokens; i++)
            {
                var t = new Token();
                var go = new GameObject("Token_" + i);
                go.transform.SetParent(_rowRect, false);
                t.Rect = go.AddComponent<RectTransform>();
                t.Rect.anchorMin = t.Rect.anchorMax = new Vector2(0.5f, 0.5f);
                t.Rect.pivot = new Vector2(0.5f, 0.5f);
                t.Image = go.AddComponent<Image>();
                t.Image.sprite = TileSprites.Get();
                t.Image.color = TermColor;
                t.Image.raycastTarget = false;

                t.Label = MakeText(go.transform, "Number", 90, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
                NeuroStyle.ClayText(t.Label, 4f, 7f); // número "de arcilla": contorno tinta y sombra dura
                t.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.Label.verticalOverflow = VerticalWrapMode.Overflow;
                var lr = t.Label.rectTransform;
                lr.anchorMin = new Vector2(0.04f, 0.14f);
                lr.anchorMax = new Vector2(0.96f, 0.90f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;

                // Pastilla del paso (aparece entre esta ficha y la siguiente al responder).
                var chipGo = new GameObject("Step_" + i);
                chipGo.transform.SetParent(_rowRect, false);
                t.Chip = chipGo.AddComponent<RectTransform>();
                t.Chip.anchorMin = t.Chip.anchorMax = new Vector2(0.5f, 0.5f);
                t.Chip.pivot = new Vector2(0.5f, 0.5f);
                t.Chip.sizeDelta = new Vector2(120f, 64f);
                t.ChipBg = chipGo.AddComponent<Image>();
                t.ChipBg.sprite = RoundedRectSprite.Get(48);
                t.ChipBg.type = Image.Type.Sliced;
                t.ChipBg.raycastTarget = false;
                t.ChipLabel = MakeText(chipGo.transform, "Text", 46, TextAnchor.MiddleCenter, new Color(0.08f, 0.06f, 0.16f), 0f, 0f);
                t.ChipLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.ChipLabel.verticalOverflow = VerticalWrapMode.Overflow;
                chipGo.SetActive(false);

                _tokens[i] = t;
            }

            // Aro pulsante alrededor de la ficha "?" (se reposiciona en cada serie).
            var ringGo = new GameObject("LensRing");
            ringGo.transform.SetParent(_rowRect, false);
            ringGo.transform.SetAsFirstSibling();
            var rr = ringGo.AddComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.pivot = new Vector2(0.5f, 0.5f);
            _lensRing = ringGo.AddComponent<Image>();
            _lensRing.sprite = RadialGlowSprite.Get();
            _lensRing.raycastTarget = false;
            _lensRing.color = new Color(LensColor.r, LensColor.g, LensColor.b, 0f);

            // Lupa de detective sobre la ficha "?": lo que hay que descubrir (se esconde al responder).
            var magGo = new GameObject("Magnifier");
            magGo.transform.SetParent(_rowRect, false);
            var mr = magGo.AddComponent<RectTransform>();
            mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0.5f);
            mr.pivot = new Vector2(0.5f, 0.5f);
            _magnifier = magGo.AddComponent<Image>();
            _magnifier.sprite = MagnifierSprite.Get();
            _magnifier.raycastTarget = false;
            magGo.SetActive(false);
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

                var label = MakeText(go.transform, "Value", 120, TextAnchor.MiddleCenter, Color.white, 4f, 0.4f);
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
            img.color = new Color(0.09f, 0.08f, 0.22f, 0.96f);
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
            _rowRect.gameObject.SetActive(false);
            _optionsRoot.gameObject.SetActive(false);
            _bannerRect.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            string title = score >= 90 ? "¡Detective estrella!" : score >= 70 ? "¡Muy bien!" : score >= 50 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = Endless
                ? $"{_correct} series resueltas de {total} · {_points} puntos"
                : $"{_correct} de {total} series resueltas";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _responseCount > 0
                ? $"Respuesta media {avgMs / 1000f:0.0} s · mejor racha {_bestStreak}"
                : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, Accent, 24, 420f, 56f, 0.9f));
        }

        // ------------------------------------------------------------------ layout

        private Vector2 _rowRest;
        private float _tokenStep;

        private Vector2 TokenPos(int i) => new Vector2((i - (_count - 1) / 2f) * _tokenStep, 0f);

        /// <summary>Reparte <paramref name="count"/> fichas (términos + "?") a lo ancho de la fila y
        /// coloca las pastillas de paso entre ellas y el aro de la ficha "?".</summary>
        private void ApplyRow(int count)
        {
            _count = Mathf.Clamp(count, 5, MaxTokens);
            _tokenStep = _contentW / _count;
            _tokenSize = _tokenStep / ShapeScale * 0.92f;
            for (int i = 0; i < _count; i++)
            {
                var t = _tokens[i];
                t.Rect.sizeDelta = new Vector2(_tokenSize, _tokenSize);
                t.Rect.anchoredPosition = TokenPos(i);
                if (i < _count - 1)
                {
                    t.Chip.sizeDelta = new Vector2(Mathf.Min(150f, _tokenStep * 0.95f), 64f);
                    t.Chip.anchoredPosition = new Vector2((i - (_count - 1) / 2f + 0.5f) * _tokenStep, -(_tokenSize * 0.5f + 34f));
                }
            }
            var ring = _lensRing.rectTransform;
            ring.sizeDelta = new Vector2(_tokenSize * 1.35f, _tokenSize * 1.35f);
            ring.anchoredPosition = TokenPos(_count - 1);
            var mag = _magnifier.rectTransform;
            mag.sizeDelta = new Vector2(_tokenSize * 0.66f, _tokenSize * 0.66f);
            mag.anchoredPosition = TokenPos(_count - 1) + new Vector2(_tokenSize * 0.34f, _tokenSize * 0.38f);
        }

        private void Layout()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            float sh = Mathf.Max(safe.height, 800f);
            float contentW = sw - MarginU * 2f;

            float y = 190f;
            _dots.Rect.anchoredPosition = new Vector2(0f, -y);
            y += 70f;

            float bannerH = 120f;
            _bannerRect.sizeDelta = new Vector2(contentW, bannerH);
            _bannerRect.anchoredPosition = new Vector2(0f, -(y + bannerH / 2f));
            y += bannerH + 22f;

            _timerBg.sizeDelta = new Vector2(contentW, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 30f;

            // Fila de 5 fichas: cada una ocupa 1/5 del ancho.
            float rowH = contentW / 5f * 1.05f;
            float remaining = sh - y;

            // Opciones 2x2 (cuadradas), centradas abajo.
            float pillBlock = 20f;
            float chipBlock = 90f;
            float cell = Mathf.Clamp(Mathf.Min((contentW - 20f) / 2f * 0.98f, (remaining - rowH - chipBlock - pillBlock - 60f) / 2.05f), 150f, 330f);
            float optH = cell * 2f * 0.98f;
            float free = Mathf.Max(0f, remaining - rowH - chipBlock - pillBlock - optH);
            float rowTop = y + free * 0.25f;

            _rowRest = new Vector2(0f, -(rowTop + rowH / 2f));
            _rowRect.anchoredPosition = _rowRest;
            _contentW = contentW;


            float optCenterFromBottom = Mathf.Max(30f, free * 0.4f) + optH / 2f;
            _optionsRoot.anchoredPosition = new Vector2(0f, optCenterFromBottom);
            float g = cell * 0.98f;
            for (int i = 0; i < 4; i++)
            {
                int r = i / 2, c = i % 2;
                _optionRects[i].sizeDelta = new Vector2(cell, cell);
                _optionRects[i].anchoredPosition = new Vector2((c - 0.5f) * g, (0.5f - r) * g);
            }
            _toast.SetTopOffset(0f);
        }

        private void FitToken(Token t, string text)
        {
            float availW = _tokenSize * ShapeScale * 0.94f;
            float size = Mathf.Min(120f, availW / Mathf.Max(1.2f, text.Length * 0.6f));
            t.Label.fontSize = Mathf.RoundToInt(Mathf.Max(34f, size));
        }

        private void FitOption(int i)
        {
            string text = _optionLabels[i].text;
            float cell = _optionRects[i].sizeDelta.x;
            float availW = cell * ShapeScale * 0.88f;
            float size = Mathf.Min(150f, availW / Mathf.Max(1.2f, text.Length * 0.6f), cell * 0.4f);
            _optionLabels[i].fontSize = Mathf.RoundToInt(Mathf.Max(40f, size));
        }

        // ------------------------------------------------------------------ helpers

        private void UpdateHudText()
        {
            // Marcador común (GameHud): nivel + puntos que cuentan (Reto) o avance "3 de 12" (Precisión).
            if (Endless)
            {
                _hud.SetLevel(_effLevel);
                _hud.SetPoints(_points);
            }
            else
            {
                _hud.SetLevel(_effLevel);
                _hud.SetInfo($"{_trialIndex + 1} de {SeriesContract.TotalTrials}");
            }
        }

        private void SetStreak(int streak)
        {
            _hud.SetStreak(streak);
        }
    }
}
