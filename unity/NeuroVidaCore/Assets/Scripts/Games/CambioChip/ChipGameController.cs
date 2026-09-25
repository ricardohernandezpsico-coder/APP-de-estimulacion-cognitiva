using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite / TileSprites / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.CambioChip
{
    /// <summary>
    /// "Cambio de Chip" en Unity (flexibilidad cognitiva): una ficha con una flecha aparece en
    /// uno de los cuatro bordes de la arena; según la regla activa hay que tocar HACIA DÓNDE
    /// APUNTA la flecha o DÓNDE ESTÁ la ficha. La regla cambia cada cierto número de ensayos
    /// (y a veces por sorpresa), y el cambio se ve: el cartel se voltea y suena un aviso.
    /// Reglas de <c>CambioChipGame.kt</c> (ver <see cref="ChipContract"/>). Modo Reto = 60 s
    /// sin límite de ensayos; Precisión = 12 ensayos. Telemetría: reusa <see cref="StroopTelemetry"/>.
    /// </summary>
    public class ChipGameController : MonoBehaviour
    {
        public const string GameId = ChipContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const int NoAnswer = -2;
        private const int TimeUp = -3;
        private const float ShapeScale = 0.86f;

        private static readonly Color BackgroundColor = new Color(0x0F / 255f, 0x17 / 255f, 0x2A / 255f);
        private static readonly Color DirAccent = new Color(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f);
        private static readonly Color PosAccent = new Color(0xF4 / 255f, 0x72 / 255f, 0xB6 / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);
        private static readonly Color ArenaFill = new Color(0x1B / 255f, 0x27 / 255f, 0x40 / 255f);
        private static readonly Color ChipColor = new Color(0xF8 / 255f, 0xFA / 255f, 0xFC / 255f);
        private static readonly Color ChipArrow = new Color(0x0F / 255f, 0x17 / 255f, 0x2A / 255f);
        // Orden de los botones = orden de ChipDirection: Up, Down, Left, Right.
        private static readonly Color[] PadColors =
        {
            new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f),
            new Color(0xA8 / 255f, 0x55 / 255f, 0xF7 / 255f),
            new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f),
            new Color(0xF9 / 255f, 0x73 / 255f, 0x16 / 255f),
        };

        private SequenceInitConfig _config;
        private System.Random _rng;
        private AudioSource _audioSource;
        private readonly Dictionary<float, AudioClip> _toneCache = new Dictionary<float, AudioClip>();

        private int _trialIndex, _correct, _streak, _bestStreak, _totalAnswered, _points;
        private long _responseMsSum;
        private int _responseCount;
        private int _answer = NoAnswer;
        private float _answerAt;
        private bool _acceptInput, _ended;
        private ChipTrial _trial;
        private ChipRule _activeRule = ChipRule.Direction;
        private bool _ruleShown;
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private int _effLevel = 1;
        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe, _streakPill, _bannerRect, _timerBg, _timerFill, _arenaRect, _fxRect, _resultRoot, _chipRect;
        private Text _titleText, _subText, _streakText, _bannerText, _bannerSub, _chipLabel;
        private Image _streakDisc, _bannerBg, _bannerDisc, _bannerArrow, _bannerRing, _arenaBorder, _labelBg, _chipImage, _chipArrowImage, _flash;
        private CanvasGroup _arenaGroup, _chipGroup;
        private readonly List<RectTransform> _padRects = new List<RectTransform>();
        private readonly List<Image> _padImages = new List<Image>();
        private readonly List<RectTransform> _slotRects = new List<RectTransform>();
        private ProgressDots _dots;
        private PhasePill _pill;
        private Toast _toast;
        private ExitButton _exit;
        private CountdownScreen _countdown;
        private Vector2 _arenaRestPos;
        private float _arenaSize;
        private float _chipSize;
        private Color _ruleAccent = Color.white;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            BuildUi();
            gameObject.SetActive(false);
        }

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
            _activeRule = ChipRule.Direction;
            _ruleShown = false;
            _dda = new AdaptiveDifficulty(5, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, 5),
                stepUp: config.config.timed ? 0.12f : 0.2f, useReaction: config.config.timed);
            _effLevel = _dda.PresentedLevel;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _bannerRect.gameObject.SetActive(true);
            _pill.Rect.gameObject.SetActive(true);
            _arenaRect.gameObject.SetActive(true);
            foreach (var r in _padRects) r.gameObject.SetActive(true);
            SetStreak(0);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Cambio de Chip", "Prepárate", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();

            _roundEndsAt = Time.unscaledTime + ChipContract.EndlessSeconds;
            _trialIndex = 0;
            while (Endless ? Time.unscaledTime < _roundEndsAt : _trialIndex < ChipContract.TotalTrials)
            {
                yield return StartCoroutine(PresentTrial());

                float startedAt = Time.unscaledTime;
                while (_answer == NoAnswer)
                {
                    if (Endless && UpdateRoundClock()) _answer = TimeUp;
                    yield return null;
                }
                _acceptInput = false;

                if (_answer == TimeUp)
                {
                    yield return StartCoroutine(FadeOutBoard());
                    break;
                }

                bool correct = _answer == (int)_trial.Correct;
                _totalAnswered++;
                _responseMsSum += (long)((_answerAt - startedAt) * 1000f);
                _responseCount++;
                var change = _dda.Register(correct, (_answerAt - startedAt) * 1000f);
                _effLevel = _dda.PresentedLevel;
                yield return StartCoroutine(ResolveTrial(correct));
                _trialIndex++;
                if (change == DdaChange.Up) _toast.Show($"Nivel {_dda.Level}", "La regla cambia más seguido", GoodColor, 1.0f);
                else if (change == DdaChange.Down || _dda.Struggling) _toast.Show("Con calma", "Ajustamos la dificultad", AmberColor, 1.0f);

                // Cambio de regla: fijo cada N ensayos o sorpresa según maestría y racha.
                int round = _trialIndex + 1; // 1-based, como en la versión Kotlin
                bool forced = round % ChipContract.SwitchInterval(_effLevel) == 0;
                bool surprise = !forced && (float)_rng.NextDouble() < ChipContract.SurpriseChance(_config.config.base_intensity, _streak);
                if (forced || surprise) _activeRule = ChipContract.Opposite(_activeRule);
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ ensayo

        private IEnumerator PresentTrial()
        {
            _answer = NoAnswer;
            _trial = ChipContract.GenerateTrial(_activeRule, _rng);
            _ruleAccent = _trial.Rule == ChipRule.Direction ? DirAccent : PosAccent;
            ResetPadColors();
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();

            bool ruleChanged = _ruleShown && _trial.Rule != _shownRule;
            if (!_ruleShown)
            {
                ApplyRuleVisuals(_trial.Rule);
            }
            else if (ruleChanged)
            {
                StartCoroutine(FlipBanner(_trial.Rule));
                PlayTone(392f, 0.14f, 0.14f);
                _toast.Show("¡Cambio de regla!", _trial.Rule == ChipRule.Direction ? "Ahora: hacia dónde apunta" : "Ahora: dónde está", _ruleAccent, 0.9f);
                StartCoroutine(PulseArenaBorder());
            }
            _ruleShown = true;
            _shownRule = _trial.Rule;

            _pill.Set(_trial.Rule == ChipRule.Direction ? "Toca hacia dónde APUNTA" : "Toca dónde ESTÁ", _ruleAccent);

            // La ficha aparece en su borde y la flecha se orienta.
            Vector2 slot = SlotPosition(_trial.Position);
            _chipRect.anchoredPosition = slot;
            _chipArrowImage.rectTransform.localRotation = Rotation(_trial.Pointing);
            _chipImage.color = ChipColor;
            _arenaBorder.color = new Color(_ruleAccent.r, _ruleAccent.g, _ruleAccent.b, 0.95f);
            _arenaRect.anchoredPosition = _arenaRestPos;
            _arenaRect.localScale = Vector3.one;
            _arenaGroup.alpha = 1f;

            float t = 0f;
            const float seconds = 0.2f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _chipRect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k));
                _chipGroup.alpha = Mathf.Clamp01(k * 3f);
                yield return null;
            }
            _chipRect.localScale = Vector3.one;
            _chipGroup.alpha = 1f;
            _acceptInput = true;
        }

        private ChipRule _shownRule;

        /// <summary>El borde de la arena late dos veces con el color de la regla nueva: refuerza el
        /// cambio justo donde mira el jugador, sin tapar nada del tablero.</summary>
        private IEnumerator PulseArenaBorder()
        {
            var accent = _ruleAccent;
            for (int i = 0; i < 2; i++)
            {
                float t = 0f;
                const float seconds = 0.22f;
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / seconds);
                    float pulse = Mathf.Sin(k * Mathf.PI);
                    _arenaBorder.color = Color.Lerp(new Color(accent.r, accent.g, accent.b, 0.95f), Color.white, pulse * 0.85f);
                    _arenaRect.localScale = Vector3.one * (1f + 0.03f * pulse);
                    yield return null;
                }
            }
            _arenaBorder.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            _arenaRect.localScale = Vector3.one;
        }

        private IEnumerator ResolveTrial(bool correct)
        {
            int chosen = _answer;
            int expected = (int)_trial.Correct;
            if (!Endless) _dots.Mark(_trialIndex, correct);

            if (correct)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _points += 100 + 25 * Mathf.Min(_streak - 1, 8);
                SetStreak(_streak);
                UpdateHudText();
                _pill.Set("¡Correcto!", GoodColor);
                PlayTone(523.25f * Mathf.Pow(2f, Mathf.Min(_streak - 1, 7) * 2f / 12f), 0.3f, 0.2f);
                StartCoroutine(Flash(GoodColor, 0.10f, 0.28f));
                StartCoroutine(UiFx.SparkBurst(_fxRect, LocalIn(_fxRect, _chipRect), Color.white, 14, 260f, 42f, 0.55f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _padRects[chosen]), Color.white, 120f, 420f, 0.45f));
                if (_streak == 4 || _streak == 8 || _streak == 12 || _streak == 20)
                    _toast.Show($"Racha de {_streak}", "Sigue así", GoodColor, 0.9f);

                // La ficha sale volando hacia el lado que se eligió.
                float seconds = Endless ? 0.17f : 0.24f;
                float t = 0f;
                Vector2 from = _chipRect.anchoredPosition;
                Vector2 dir = Vector(_trial.Correct == _trial.Position ? _trial.Position : _trial.Pointing);
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                    _chipRect.anchoredPosition = from + dir * (_arenaSize * 0.35f * k);
                    _chipRect.localScale = Vector3.one * (1f + 0.15f * k);
                    _chipGroup.alpha = 1f - k;
                    yield return null;
                }
                if (!Endless) yield return new WaitForSeconds(0.08f);
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                _pill.Set($"Era {ChipContract.DirectionNames[expected]}", AmberColor);
                PlayTone(196f, 0.32f, 0.2f);
                _chipImage.color = BadColor;
                _arenaBorder.color = new Color(BadColor.r, BadColor.g, BadColor.b, 0.9f);
                StartCoroutine(Flash(BadColor, 0.14f, 0.32f));
                StartCoroutine(UiFx.Shake(24f, 0.4f, _arenaRect));
                DimPadsExcept(expected);
                StartCoroutine(PopRect(_padRects[expected], 1.14f, 0.3f));
                StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _padRects[expected]), Color.white, 140f, 460f, 0.55f));
                yield return new WaitForSeconds(Endless ? 0.6f : 0.85f);

                float t = 0f;
                const float seconds = 0.16f;
                while (t < seconds)
                {
                    t += Time.unscaledDeltaTime;
                    _chipGroup.alpha = 1f - Mathf.Clamp01(t / seconds);
                    yield return null;
                }
            }
            _chipGroup.alpha = 0f;
        }

        private IEnumerator FadeOutBoard()
        {
            float t = 0f;
            const float seconds = 0.15f;
            float from = _chipGroup.alpha;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                _chipGroup.alpha = from * (1f - Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _chipGroup.alpha = 0f;
        }

        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - Time.unscaledTime;
            SetTimerFraction(Mathf.Clamp01(left / ChipContract.EndlessSeconds));
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
            int total = Endless ? _totalAnswered : ChipContract.TotalTrials;
            int score = Endless ? ChipContract.EndlessScore(_correct, _totalAnswered) : ChipContract.Score(_correct, total);
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

        private void OnPadTapped(int index)
        {
            if (!_acceptInput || _ended) return;
            _acceptInput = false;
            _answerAt = Time.unscaledTime;
            _answer = index;
        }

        // ------------------------------------------------------------------ direcciones

        private static Vector2 Vector(ChipDirection d)
        {
            switch (d)
            {
                case ChipDirection.Up: return new Vector2(0f, 1f);
                case ChipDirection.Down: return new Vector2(0f, -1f);
                case ChipDirection.Left: return new Vector2(-1f, 0f);
                default: return new Vector2(1f, 0f);
            }
        }

        private static Quaternion Rotation(ChipDirection d)
        {
            switch (d)
            {
                case ChipDirection.Up: return Quaternion.identity;
                case ChipDirection.Down: return Quaternion.Euler(0f, 0f, 180f);
                case ChipDirection.Left: return Quaternion.Euler(0f, 0f, 90f);
                default: return Quaternion.Euler(0f, 0f, -90f);
            }
        }

        private Vector2 SlotPosition(ChipDirection d)
        {
            float reach = _arenaSize * 0.5f - _chipSize * 0.5f - _arenaSize * 0.10f;
            return Vector(d) * reach;
        }

        // ------------------------------------------------------------------ cartel de regla

        private void ApplyRuleVisuals(ChipRule rule)
        {
            bool dir = rule == ChipRule.Direction;
            _ruleAccent = dir ? DirAccent : PosAccent;
            _bannerBg.color = Color.Lerp(new Color(0.09f, 0.13f, 0.24f, 1f), _ruleAccent, 0.32f);
            _bannerDisc.color = _ruleAccent;
            _bannerArrow.gameObject.SetActive(dir);
            _bannerRing.gameObject.SetActive(!dir);
            _bannerText.text = dir ? "DIRECCIÓN" : "POSICIÓN";
            _bannerSub.text = dir ? "Toca hacia dónde apunta la flecha" : "Toca dónde está la ficha";
            _labelBg.color = _ruleAccent;
            _chipLabel.text = dir ? "DIRECCIÓN" : "POSICIÓN";
            _arenaBorder.color = new Color(_ruleAccent.r, _ruleAccent.g, _ruleAccent.b, 0.95f);
        }

        private IEnumerator FlipBanner(ChipRule rule)
        {
            float t = 0f;
            const float half = 0.11f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                _bannerRect.localScale = new Vector3(1f - Mathf.Clamp01(t / half), 1f, 1f);
                yield return null;
            }
            ApplyRuleVisuals(rule);
            t = 0f;
            const float back = 0.2f;
            while (t < back)
            {
                t += Time.unscaledDeltaTime;
                _bannerRect.localScale = new Vector3(Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(t / back))), 1f, 1f);
                yield return null;
            }
            _bannerRect.localScale = Vector3.one;
            StartCoroutine(UiFx.RingBurst(_fxRect, LocalIn(_fxRect, _bannerRect), _ruleAccent, 200f, 700f, 0.5f));
        }

        // ------------------------------------------------------------------ construcción de UI

        private void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("ChipCanvas");
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
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.12f, 0.88f), 1500f, new Color(0.22f, 0.74f, 0.97f, 0.18f));
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.92f, 0.10f), 1400f, new Color(0.96f, 0.45f, 0.71f, 0.16f));
            bg.AddComponent<CountdownAmbient>().Build(bgRect, 8);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            _dots = new ProgressDots(_safe, this, UnitsPerDp, ChipContract.TotalTrials);
            BuildBanner();
            BuildTimer();
            BuildArena();
            _pill = new PhasePill(_safe, this, UnitsPerDp, 21);
            BuildPad();

            var fxGo = new GameObject("FxLayer");
            fxGo.transform.SetParent(_safe, false);
            _fxRect = fxGo.AddComponent<RectTransform>();
            Stretch(_fxRect);

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);

            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(620f);

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
            _titleText.text = "Cambio de Chip";
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

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(discGo.transform, false);
            var ar = arrowGo.AddComponent<RectTransform>();
            ar.anchorMin = new Vector2(0.2f, 0.2f);
            ar.anchorMax = new Vector2(0.8f, 0.8f);
            ar.offsetMin = ar.offsetMax = Vector2.zero;
            ar.localRotation = Quaternion.Euler(0f, 0f, -45f);
            _bannerArrow = arrowGo.AddComponent<Image>();
            _bannerArrow.sprite = ArrowSprite.Get();
            _bannerArrow.color = new Color(0.06f, 0.09f, 0.16f);
            _bannerArrow.raycastTarget = false;

            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(discGo.transform, false);
            var rr = ringGo.AddComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.16f, 0.16f);
            rr.anchorMax = new Vector2(0.84f, 0.84f);
            rr.offsetMin = rr.offsetMax = Vector2.zero;
            _bannerRing = ringGo.AddComponent<Image>();
            _bannerRing.sprite = RingSprite.Get();
            _bannerRing.color = new Color(0.06f, 0.09f, 0.16f);
            _bannerRing.raycastTarget = false;

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

        private void BuildArena()
        {
            var root = new GameObject("Arena");
            root.transform.SetParent(_safe, false);
            _arenaRect = root.AddComponent<RectTransform>();
            _arenaRect.anchorMin = _arenaRect.anchorMax = new Vector2(0.5f, 1f);
            _arenaRect.pivot = new Vector2(0.5f, 0.5f);
            _arenaGroup = root.AddComponent<CanvasGroup>();
            _arenaGroup.blocksRaycasts = false;

            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            var sr = shadowGo.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(-0.10f, -0.30f);
            sr.anchorMax = new Vector2(1.10f, 0.98f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;
            var shImg = shadowGo.AddComponent<Image>();
            shImg.sprite = RadialGlowSprite.Get();
            shImg.color = new Color(0f, 0f, 0f, 0.5f);
            shImg.raycastTarget = false;

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(root.transform, false);
            Stretch(borderGo.AddComponent<RectTransform>());
            _arenaBorder = borderGo.AddComponent<Image>();
            _arenaBorder.sprite = RoundedRectSprite.Get(56);
            _arenaBorder.type = Image.Type.Sliced;
            _arenaBorder.raycastTarget = false;

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
            fillImg.color = ArenaFill;
            fillImg.raycastTarget = false;

            // Cuatro marcas tenues en los bordes: dejan claro dónde puede aparecer la ficha.
            var slotNames = new[] { "SlotUp", "SlotDown", "SlotLeft", "SlotRight" };
            for (int i = 0; i < 4; i++)
            {
                var g = new GameObject(slotNames[i]);
                g.transform.SetParent(root.transform, false);
                var r = g.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                var img = g.AddComponent<Image>();
                img.sprite = RingSprite.Get();
                img.color = new Color(1f, 1f, 1f, 0.10f);
                img.raycastTarget = false;
                _slotRects.Add(r);
            }

            // La ficha: cuadrado de arcilla claro con la flecha oscura.
            var chipGo = new GameObject("Chip");
            chipGo.transform.SetParent(root.transform, false);
            _chipRect = chipGo.AddComponent<RectTransform>();
            _chipRect.anchorMin = _chipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _chipRect.pivot = new Vector2(0.5f, 0.5f);
            _chipGroup = chipGo.AddComponent<CanvasGroup>();
            _chipImage = chipGo.AddComponent<Image>();
            _chipImage.sprite = TileSprites.Get();
            _chipImage.color = ChipColor;
            _chipImage.raycastTarget = false;

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(chipGo.transform, false);
            var ar = arrowGo.AddComponent<RectTransform>();
            ar.anchorMin = new Vector2(0.25f, 0.27f);
            ar.anchorMax = new Vector2(0.75f, 0.77f);
            ar.offsetMin = ar.offsetMax = Vector2.zero;
            _chipArrowImage = arrowGo.AddComponent<Image>();
            _chipArrowImage.sprite = ArrowSprite.Get();
            _chipArrowImage.color = ChipArrow;
            _chipArrowImage.raycastTarget = false;

            // Etiqueta de la regla montada sobre el borde superior de la arena.
            var labelGo = new GameObject("RuleChip");
            labelGo.transform.SetParent(root.transform, false);
            var lr = labelGo.AddComponent<RectTransform>();
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 1f);
            lr.pivot = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(360f, 84f);
            lr.anchoredPosition = Vector2.zero;
            _labelBg = labelGo.AddComponent<Image>();
            _labelBg.sprite = RoundedRectSprite.Get(64);
            _labelBg.type = Image.Type.Sliced;
            _labelBg.raycastTarget = false;
            _chipLabel = MakeText(labelGo.transform, "LabelText", 52, TextAnchor.MiddleCenter, new Color(0.06f, 0.09f, 0.16f), 0f, 0f);
            BestFit(_chipLabel, 30);
        }

        private void BuildPad()
        {
            var arrowRotations = new[] { ChipDirection.Up, ChipDirection.Down, ChipDirection.Left, ChipDirection.Right };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var go = new GameObject("Pad_" + ChipContract.DirectionNames[i]);
                go.transform.SetParent(_safe, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = TileSprites.Get();
                img.color = PadColors[i];
                var button = go.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnPadTapped(index));
                go.AddComponent<PressScale>();

                var arrowGo = new GameObject("Arrow");
                arrowGo.transform.SetParent(go.transform, false);
                var ar = arrowGo.AddComponent<RectTransform>();
                ar.anchorMin = new Vector2(0.23f, 0.25f);
                ar.anchorMax = new Vector2(0.77f, 0.79f);
                ar.offsetMin = ar.offsetMax = Vector2.zero;
                ar.localRotation = Rotation(arrowRotations[i]);
                var arrow = arrowGo.AddComponent<Image>();
                arrow.sprite = ArrowSprite.Get();
                arrow.color = Color.white;
                arrow.raycastTarget = false;
                UiFonts.AddSoftShadow(arrowGo, 3f, 0.35f);

                _padRects.Add(rect);
                _padImages.Add(img);
            }
        }

        private void ResetPadColors()
        {
            for (int i = 0; i < _padImages.Count; i++) _padImages[i].color = PadColors[i];
        }

        private void DimPadsExcept(int keep)
        {
            for (int i = 0; i < _padImages.Count; i++)
            {
                if (i == keep) continue;
                _padImages[i].color = Color.Lerp(PadColors[i], new Color(0.25f, 0.28f, 0.36f, 1f), 0.72f);
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
            AddResultText("Detail", 52, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 44, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        private void AddResultText(string name, int size, Vector2 pos, Color color)
        {
            var t = MakeText(_resultRoot, name, size, TextAnchor.MiddleCenter, color, 3f, 0.4f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(820f, size * 1.4f);
            r.anchoredPosition = pos;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void ShowResult(int score, int avgMs, int total)
        {
            _exit.Show();
            _acceptInput = false;
            foreach (var r in _padRects) r.gameObject.SetActive(false);
            _arenaRect.gameObject.SetActive(false);
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
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, DirAccent, 24, 420f, 56f, 0.9f));
        }

        private IEnumerator AnimateResult(int score)
        {
            var scoreText = _resultRoot.Find("Score").GetComponent<Text>();
            float t = 0f;
            const float seconds = 0.9f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _resultRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(k * 2f)));
                scoreText.text = Mathf.RoundToInt(score * UiFx.EaseOutCubic(k)).ToString();
                yield return null;
            }
            scoreText.text = score.ToString();
            _resultRoot.localScale = Vector3.one;
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

            float bannerH = 156f;
            _bannerRect.sizeDelta = new Vector2(contentW, bannerH);
            _bannerRect.anchoredPosition = new Vector2(0f, -(y + bannerH / 2f));
            y += bannerH + 22f;

            _timerBg.sizeDelta = new Vector2(contentW, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 58f;

            // Reparto del alto que queda: arena (cuadrada) + píldora + cruz de 3 botones.
            float remaining = sh - y;
            float pillBlock = 130f;
            float arena = Mathf.Clamp(Mathf.Min(contentW * 0.82f, remaining * 0.40f), 300f, 620f);
            float padCell = Mathf.Clamp((remaining - arena - pillBlock - 40f) / 2.9f, 130f, Mathf.Min(300f, contentW / 3.1f));
            float padBlock = padCell * 2.9f;
            float free = Mathf.Max(0f, remaining - arena - pillBlock - padBlock);
            float top = y + free * 0.25f;

            _arenaSize = arena;
            _chipSize = arena * 0.30f / ShapeScale;
            _arenaRect.sizeDelta = new Vector2(arena, arena);
            _arenaRestPos = new Vector2(0f, -(top + arena / 2f));
            _arenaRect.anchoredPosition = _arenaRestPos;
            _chipRect.sizeDelta = new Vector2(_chipSize, _chipSize);
            float slotSize = _chipSize * 0.5f;
            var dirs = new[] { ChipDirection.Up, ChipDirection.Down, ChipDirection.Left, ChipDirection.Right };
            for (int i = 0; i < 4; i++)
            {
                _slotRects[i].sizeDelta = new Vector2(slotSize, slotSize);
                _slotRects[i].anchoredPosition = SlotPosition(dirs[i]);
            }

            float pillCenterFromTop = top + arena + pillBlock * 0.5f;
            _pill.SetPosition(new Vector2(0f, sh / 2f - pillCenterFromTop));

            // Cruz de direcciones anclada abajo: centro de la cruz a 1.45 celdas del fondo.
            float g = padCell * 0.98f;
            float cy = Mathf.Max(20f, free * 0.35f) + padCell * 1.45f;
            for (int i = 0; i < 4; i++)
            {
                var r = _padRects[i];
                r.sizeDelta = new Vector2(padCell, padCell);
                Vector2 v = Vector(dirs[i]);
                r.anchoredPosition = new Vector2(v.x * g, cy + v.y * g);
            }
            _toast.SetTopOffset(0f); // avisos arriba (zona del título), nunca sobre la arena
        }

        // ------------------------------------------------------------------ helpers

        private void UpdateHudText()
        {
            _subText.text = Endless
                ? $"Puntos {_points} · Nivel {_effLevel}"
                : $"Ensayo {_trialIndex + 1} de {ChipContract.TotalTrials} · Nivel {_effLevel}";
        }

        private void SetStreak(int streak)
        {
            _streakText.text = $"Racha {streak}";
            _streakDisc.color = streak >= 3 ? AmberColor : new Color(1f, 1f, 1f, 0.30f);
            if (streak > 0) StartCoroutine(PopRect(_streakPill, 1.12f, 0.22f));
        }

        private IEnumerator Flash(Color color, float maxAlpha, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _flash.color = new Color(color.r, color.g, color.b, maxAlpha * (1f - k));
                yield return null;
            }
            _flash.color = new Color(0f, 0f, 0f, 0f);
        }

        private void PlayTone(float hz, float seconds, float volume)
        {
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            float key = Mathf.Round(hz * 10f) + seconds * 100000f;
            if (!_toneCache.TryGetValue(key, out var clip))
            {
                clip = HarmonicTone.Build(hz, seconds, volume);
                _toneCache[key] = clip;
            }
            _audioSource.PlayOneShot(clip);
        }
    }
}
