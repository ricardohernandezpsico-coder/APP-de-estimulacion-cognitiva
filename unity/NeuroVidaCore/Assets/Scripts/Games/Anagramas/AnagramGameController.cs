using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / TileSprites / TilePalette / HarmonicTone
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Anagramas
{
    /// <summary>
    /// "Anagramas" en Unity (lenguaje). Las letras desordenadas de una palabra son fichas de arcilla
    /// que se tocan para armarla en las casillas de arriba; las fichas VUELAN a su casilla y una
    /// ficha colocada se puede tocar para devolverla. Al completar la palabra se valida sola: si
    /// acierta, ola de fichas verdes; si falla, las fichas se reordenan solas mostrando la palabra
    /// correcta (se aprende). El cartel superior hace de pizarra (instrucción / pista). Reto = 120 s
    /// sin límite de palabras (el largo sube de a poco: 3 a 11 letras); Precisión = 6 palabras.
    /// Reglas de <c>AnagramasGame.kt</c> (ver <see cref="AnagramContract"/>). Telemetría: reusa
    /// <see cref="StroopTelemetry"/>.
    /// </summary>
    public class AnagramGameController : MonoBehaviour
    {
        public const string GameId = AnagramContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 60f;
        private const float ShapeScale = 0.86f;
        private const int MaxLetters = 11;

        private static readonly Color BackgroundColor = new Color(0x18 / 255f, 0x0F / 255f, 0x2E / 255f);
        private static readonly Color Accent = new Color(0xF4 / 255f, 0x72 / 255f, 0xB6 / 255f);
        private static readonly Color TileCream = new Color(0xFD / 255f, 0xE9 / 255f, 0xC8 / 255f);
        private static readonly Color TilePlaced = new Color(0xFF / 255f, 0xD1 / 255f, 0x7A / 255f);
        private static readonly Color TileGood = new Color(0x86 / 255f, 0xEF / 255f, 0xAC / 255f);
        private static readonly Color TileBad = new Color(0xFC / 255f, 0xA5 / 255f, 0xA5 / 255f);
        private static readonly Color TileReveal = new Color(0xA7 / 255f, 0xF3 / 255f, 0xD0 / 255f);
        private static readonly Color Ink = new Color(0x2A / 255f, 0x17 / 255f, 0x40 / 255f);
        private static readonly Color GoodColor = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        private static readonly Color BadColor = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);
        private static readonly Color AmberColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f);

        private enum WordResult { Waiting, Solved, Failed, Skipped, TimeUp }

        private sealed class Letter
        {
            public RectTransform Rect;
            public Image Image;
            public Text Label;
            public char Char;
            public bool InSlot;
            public Vector2 Target;
        }

        private SequenceInitConfig _config;
        private System.Random _rng;
        private AudioSource _audioSource;
        private readonly Dictionary<float, AudioClip> _toneCache = new Dictionary<float, AudioClip>();

        private int _trialIndex, _correct, _streak, _bestStreak, _totalAttempts, _points, _effLevel;
        private AdaptiveDifficulty _dda; // DDA común (ver docs/DDA-comun.md)
        private long _solveMsSum;
        private int _solveCount;
        private bool _acceptInput, _ended, _hintUsed;
        private WordResult _result;
        private AnagramWord _word;
        private readonly HashSet<string> _used = new HashSet<string>();
        private readonly List<Letter> _letters = new List<Letter>();
        private readonly List<Letter> _assembly = new List<Letter>();
        private float _roundEndsAt;
        private int _lastTickSecond = -1;
        private bool Endless => _config != null && _config.config.timed;

        // UI
        private RectTransform _safe, _boardRect, _streakPill, _bannerRect, _timerBg, _timerFill, _fxRect, _resultRoot, _actionsRoot;
        private Text _titleText, _subText, _streakText, _bannerText;
        private Image _streakDisc, _flash, _timerFillImage, _bannerImage;
        private ProgressDots _dots;
        private Toast _toast;
        private ExitButton _exit;
        private CountdownScreen _countdown;
        private readonly List<RectTransform> _slotRects = new List<RectTransform>();
        private readonly List<Image> _slotImages = new List<Image>();
        private Button _backButton, _hintButton, _skipButton;
        private float _slotSize, _tileSize;
        private float _slotsCenterY;
        private Vector2[] _poolPositions = new Vector2[MaxLetters];
        private Vector2[] _slotPositions = new Vector2[MaxLetters];

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            BuildUi();
            gameObject.SetActive(false);
        }

        // Las fichas se deslizan suavemente hacia su destino (resorte amortiguado, independiente del framerate).
        private void Update()
        {
            float k = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            foreach (var l in _letters)
                if (l != null && l.Rect != null)
                    l.Rect.anchoredPosition = Vector2.Lerp(l.Rect.anchoredPosition, l.Target, k);
        }

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _trialIndex = _correct = _streak = _bestStreak = _totalAttempts = _points = 0;
            _solveMsSum = 0;
            _solveCount = 0;
            _ended = false;
            _acceptInput = false;
            _lastTickSecond = -1;
            _used.Clear();
            // DDA común sobre la escalera de 7 niveles (largo de palabra). Pocas palabras por sesión => pasos más grandes.
            _dda = new AdaptiveDifficulty(AnagramContract.MaxLevel, DdaUserProfileConfig.ParseAgeBand(config.config.age_band),
                AdaptiveDifficulty.StartRating(config.config, AnagramContract.MaxLevel),
                stepUp: 0.35f, useReaction: false);
            _effLevel = _dda.PresentedLevel;

            _dots.Reset();
            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            _bannerRect.gameObject.SetActive(true);
            _actionsRoot.gameObject.SetActive(true);
            foreach (var s in _slotRects) s.gameObject.SetActive(false);
            ClearLetters();
            SetStreak(0);
            _timerBg.gameObject.SetActive(config.config.timed);
            _dots.Rect.gameObject.SetActive(!config.config.timed);

            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Anagramas", "Ordena las letras", () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            LayoutStatic();

            _roundEndsAt = Time.unscaledTime + AnagramContract.EndlessSeconds;
            _trialIndex = 0;
            while (Endless ? Time.unscaledTime < _roundEndsAt : _trialIndex < AnagramContract.TotalTrials)
            {
                yield return StartCoroutine(PlayWord());
                if (_result == WordResult.TimeUp) break;
                _trialIndex++;
            }

            yield return StartCoroutine(FinishGame());
        }

        // ------------------------------------------------------------------ palabra

        private IEnumerator PlayWord()
        {
            _effLevel = _dda.PresentedLevel;
            _word = AnagramContract.Pick(_effLevel, _used, _rng);
            int n = _word.Word.Length;
            _result = WordResult.Waiting;
            _hintUsed = false;
            _assembly.Clear();
            if (!Endless) _dots.MarkCurrent(_trialIndex);
            UpdateHudText();
            SetBanner("Ordena las letras", Accent, 0.30f);

            ClearLetters();
            LayoutWord(n);
            var scrambled = AnagramContract.Scramble(_word.Word, _rng);
            for (int i = 0; i < n; i++) _letters.Add(CreateLetter(scrambled[i], i));
            for (int i = 0; i < n; i++) _slotRects[i].gameObject.SetActive(true);
            for (int i = n; i < MaxLetters; i++) _slotRects[i].gameObject.SetActive(false);
            for (int i = 0; i < n; i++) _slotImages[i].color = SlotColor(false);

            // Casillas y fichas aparecen con rebote escalonado.
            for (int i = 0; i < n; i++)
            {
                StartCoroutine(PopIn(_slotRects[i], 0.18f));
                StartCoroutine(PopIn(_letters[i].Rect, 0.22f));
                yield return new WaitForSeconds(Mathf.Min(0.06f, 0.5f / n));
            }
            PlayTone(587.33f, 0.14f, 0.09f);
            SetActionsInteractable(true);
            _acceptInput = true;

            float startedAt = Time.unscaledTime;
            while (_result == WordResult.Waiting)
            {
                if (Endless && UpdateRoundClock()) _result = WordResult.TimeUp;
                yield return null;
            }
            _acceptInput = false;
            SetActionsInteractable(false);

            if (_result == WordResult.TimeUp)
            {
                yield return StartCoroutine(FadeAll(0.18f));
                yield break;
            }

            _totalAttempts++;
            bool solved = _result == WordResult.Solved;
            if (!Endless) _dots.Mark(_trialIndex, solved);

            if (solved)
            {
                _correct++;
                _streak++;
                _bestStreak = Mathf.Max(_bestStreak, _streak);
                _solveMsSum += (long)((Time.unscaledTime - startedAt) * 1000f);
                _solveCount++;
                _points += AnagramContract.PointsFor(_streak, _hintUsed);
                SetStreak(_streak);
                UpdateHudText();
                SetBanner("¡" + _word.Word + "!", GoodColor, 0.45f);
                foreach (var l in _assembly) l.Image.color = TileGood;
                PlayTone(523.25f * Mathf.Pow(2f, Mathf.Min(_streak - 1, 7) * 2f / 12f), 0.3f, 0.2f);
                StartCoroutine(PlayCelebrationTone());
                StartCoroutine(Flash(GoodColor, 0.08f, 0.28f));
                StartCoroutine(UiFx.SparkBurst(_fxRect, SlotsCenter(), Accent, 16, _slotSize * n * 0.55f, 46f, 0.7f));
                StartCoroutine(UiFx.RingBurst(_fxRect, SlotsCenter(), Color.white, _slotSize * 1.5f, _slotSize * n * 0.9f, 0.6f));
                yield return StartCoroutine(JumpWave());

                var change = _dda.Register(true);
                _effLevel = _dda.Level;
                if (change == DdaChange.Up)
                {
                    UpdateHudText();
                    var (min, max) = AnagramContract.LengthRange(_effLevel);
                    _toast.Show($"Nivel {_effLevel}", $"Palabras de {min} a {max} letras", Accent, 1.1f);
                    PlayTone(1046.5f, 0.35f, 0.2f);
                }
                else if (_streak == 3 || _streak == 6 || _streak == 10)
                {
                    _toast.Show($"Racha de {_streak}", "¡Qué vocabulario!", GoodColor, 0.9f);
                }
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                _streak = 0;
                SetStreak(0);
                var change = _dda.Register(false);
                _effLevel = _dda.Level;
                if (change == DdaChange.Down || _dda.Struggling)
                {
                    UpdateHudText();
                    _toast.Show("Con calma", "Ajustamos la dificultad", AmberColor, 1.0f);
                }
                PlayTone(196f, 0.32f, 0.2f);
                StartCoroutine(Flash(BadColor, 0.10f, 0.3f));
                if (_result == WordResult.Failed)
                {
                    foreach (var l in _assembly) l.Image.color = TileBad;
                    foreach (var l in _assembly) StartCoroutine(UiFx.Shake(18f, 0.4f, l.Rect));
                    yield return new WaitForSeconds(0.55f);
                }
                // Se muestra la palabra correcta: las fichas se reordenan solas.
                SetBanner("Era " + _word.Word, AmberColor, 0.40f);
                ArrangeAsSolution();
                yield return new WaitForSeconds(Endless ? 1.1f : 1.5f);
            }

            yield return StartCoroutine(FadeAll(0.2f));
        }

        // ------------------------------------------------------------------ interacción

        private void OnLetterTapped(Letter letter)
        {
            if (!_acceptInput || _ended) return;
            int idx = _assembly.IndexOf(letter);
            if (idx >= 0)
            {
                // Devolver una ficha colocada: las siguientes se corren una casilla a la izquierda.
                _assembly.RemoveAt(idx);
                letter.InSlot = false;
                letter.Image.color = TileCream;
                PlayTone(330f, 0.08f, 0.10f);
                Retarget();
                StartCoroutine(PopRect(letter.Rect, 1.12f, 0.18f));
                return;
            }
            if (_assembly.Count >= _word.Word.Length) return;

            _assembly.Add(letter);
            letter.InSlot = true;
            letter.Image.color = TilePlaced;
            PlayTone(392f * Mathf.Pow(2f, (_assembly.Count - 1) * 2f / 12f), 0.12f, 0.12f);
            Retarget();
            StartCoroutine(PopRect(letter.Rect, 1.15f, 0.2f));

            if (_assembly.Count == _word.Word.Length)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var l in _assembly) sb.Append(l.Char);
                _acceptInput = false;
                _result = AnagramContract.IsAccepted(sb.ToString(), _word.Word) ? WordResult.Solved : WordResult.Failed;
            }
        }

        private void OnBackspace()
        {
            if (!_acceptInput || _assembly.Count == 0) return;
            OnLetterTapped(_assembly[_assembly.Count - 1]);
        }

        private void OnHint()
        {
            if (!_acceptInput || _hintUsed) return;
            _hintUsed = true;
            SetBanner("Pista: " + _word.Clue, AmberColor, 0.35f);
            PlayTone(659.25f, 0.16f, 0.12f);
        }

        private void OnSkip()
        {
            if (!_acceptInput) return;
            _acceptInput = false;
            _result = WordResult.Skipped;
        }

        /// <summary>Recalcula el destino de cada ficha: las armadas a su casilla, las demás a su lugar del banco.</summary>
        private void Retarget()
        {
            for (int i = 0; i < _letters.Count; i++)
                if (!_letters[i].InSlot) _letters[i].Target = _poolPositions[i];
            for (int i = 0; i < _assembly.Count; i++)
                _assembly[i].Target = _slotPositions[i];
        }

        /// <summary>Reordena las fichas para mostrar la solución (prefiere no mover las que ya están bien).</summary>
        private void ArrangeAsSolution()
        {
            string target = _word.Word;
            var free = new List<Letter>(_letters);
            var placed = new Letter[target.Length];
            for (int i = 0; i < target.Length && i < _assembly.Count; i++)
            {
                if (_assembly[i].Char == target[i]) { placed[i] = _assembly[i]; free.Remove(_assembly[i]); }
            }
            for (int i = 0; i < target.Length; i++)
            {
                if (placed[i] != null) continue;
                var pick = free.Find(l => l.Char == target[i]);
                placed[i] = pick;
                free.Remove(pick);
            }
            _assembly.Clear();
            for (int i = 0; i < target.Length; i++)
            {
                var l = placed[i];
                l.InSlot = true;
                l.Image.color = TileReveal;
                _assembly.Add(l);
            }
            Retarget();
        }

        // ------------------------------------------------------------------ efectos

        private IEnumerator JumpWave()
        {
            // Cada ficha da un saltito, una tras otra.
            for (int i = 0; i < _assembly.Count; i++)
            {
                StartCoroutine(PopRect(_assembly[i].Rect, 1.25f, 0.28f));
                PlayTone(523.25f * Mathf.Pow(2f, i * 2f / 12f), 0.1f, 0.09f);
                yield return new WaitForSeconds(Mathf.Min(0.07f, 0.6f / _assembly.Count));
            }
            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator PlayCelebrationTone()
        {
            yield return new WaitForSeconds(0.2f);
            PlayTone(783.99f, 0.25f, 0.16f);
            yield return new WaitForSeconds(0.14f);
            PlayTone(1046.5f, 0.35f, 0.18f);
        }

        private IEnumerator FadeAll(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                foreach (var l in _letters) if (l.Rect != null) l.Rect.localScale = Vector3.one * (1f - k);
                for (int i = 0; i < _word.Word.Length && i < _slotRects.Count; i++) _slotRects[i].localScale = Vector3.one * (1f - k);
                yield return null;
            }
            ClearLetters();
        }

        private bool UpdateRoundClock()
        {
            float left = _roundEndsAt - Time.unscaledTime;
            float f = Mathf.Clamp01(left / AnagramContract.EndlessSeconds);
            _timerFill.anchorMax = new Vector2(f, 1f);
            _timerFill.offsetMin = _timerFill.offsetMax = Vector2.zero;
            _timerFillImage.color = f > 0.5f ? Color.Lerp(AmberColor, Accent, (f - 0.5f) * 2f) : Color.Lerp(BadColor, AmberColor, f * 2f);
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
            int total = Endless ? _totalAttempts : AnagramContract.TotalTrials;
            int score = Endless ? AnagramContract.EndlessScore(_correct, _totalAttempts) : AnagramContract.Score(_correct, total);
            int avgMs = _solveCount > 0 ? (int)(_solveMsSum / _solveCount) : 0;

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

        // ------------------------------------------------------------------ construcción de UI

        private void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("AnagramCanvas");
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
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.12f, 0.88f), 1500f, new Color(0.96f, 0.45f, 0.71f, 0.17f));
            UiFx.AddBackgroundGlow(bg.transform, new Vector2(0.92f, 0.12f), 1400f, new Color(0.55f, 0.36f, 0.96f, 0.18f));
            bg.AddComponent<CountdownAmbient>().Build(bgRect, 8);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            BuildHud();
            _dots = new ProgressDots(_safe, this, UnitsPerDp, AnagramContract.TotalTrials);
            BuildBanner();
            BuildTimer();

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(_safe, false);
            _boardRect = boardGo.AddComponent<RectTransform>();
            _boardRect.anchorMin = _boardRect.anchorMax = new Vector2(0.5f, 1f);
            _boardRect.pivot = new Vector2(0.5f, 1f);
            _boardRect.sizeDelta = new Vector2(10f, 10f);
            _boardRect.anchoredPosition = Vector2.zero;
            BuildSlots();

            BuildActions();

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
            _titleText.text = "Anagramas";
        }

        private void BuildBanner()
        {
            var go = new GameObject("Banner");
            go.transform.SetParent(_safe, false);
            _bannerRect = go.AddComponent<RectTransform>();
            _bannerRect.anchorMin = _bannerRect.anchorMax = new Vector2(0.5f, 1f);
            _bannerRect.pivot = new Vector2(0.5f, 0.5f);
            _bannerImage = go.AddComponent<Image>();
            _bannerImage.sprite = RoundedRectSprite.Get(64);
            _bannerImage.type = Image.Type.Sliced;
            _bannerImage.raycastTarget = false;
            _bannerText = MakeText(go.transform, "Text", 70, TextAnchor.MiddleCenter, Color.white, 3f, 0.4f);
            _bannerText.rectTransform.offsetMin = new Vector2(30f, 0f);
            _bannerText.rectTransform.offsetMax = new Vector2(-30f, 0f);
            BestFit(_bannerText, 34);
            SetBannerNow("Ordena las letras", Accent, 0.30f);
        }

        private static Color BannerColor(Color accent, float amount) =>
            Color.Lerp(new Color(0.11f, 0.07f, 0.22f, 1f), accent, amount);

        private void SetBannerNow(string text, Color accent, float amount)
        {
            _bannerText.text = text;
            _bannerImage.color = BannerColor(accent, amount);
        }

        private void SetBanner(string text, Color accent, float amount)
        {
            SetBannerNow(text, accent, amount);
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
            _timerFillImage.color = Accent;
        }

        private void BuildSlots()
        {
            for (int i = 0; i < MaxLetters; i++)
            {
                var go = new GameObject("Slot_" + i);
                go.transform.SetParent(_boardRect, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = RoundedRectSprite.Get(36);
                img.type = Image.Type.Sliced;
                img.color = SlotColor(false);
                img.raycastTarget = false;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.16f);
                outline.effectDistance = new Vector2(2.5f, -2.5f);
                go.SetActive(false);
                _slotRects.Add(r);
                _slotImages.Add(img);
            }
        }

        private static Color SlotColor(bool filled) =>
            filled ? new Color(0.35f, 0.25f, 0.55f, 0.6f) : new Color(0.10f, 0.06f, 0.20f, 0.55f);

        private void BuildActions()
        {
            var root = new GameObject("Actions");
            root.transform.SetParent(_safe, false);
            _actionsRoot = root.AddComponent<RectTransform>();
            _actionsRoot.anchorMin = _actionsRoot.anchorMax = new Vector2(0.5f, 0f);
            _actionsRoot.pivot = new Vector2(0.5f, 0.5f);
            _actionsRoot.sizeDelta = new Vector2(10f, 10f);

            _backButton = MakeAction("Borrar", new Color(0.36f, 0.30f, 0.62f), OnBackspace);
            _hintButton = MakeAction("Pista", new Color(0.85f, 0.55f, 0.10f), OnHint);
            _skipButton = MakeAction("Pasar", new Color(0.78f, 0.28f, 0.50f), OnSkip);
        }

        private Button MakeAction(string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(_actionsRoot, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(48);
            img.type = Image.Type.Sliced;
            img.color = color;
            var shadow = go.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -4f);
            var b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(onClick);
            go.AddComponent<PressScale>();
            var text = MakeText(go.transform, "Label", 52, TextAnchor.MiddleCenter, Color.white, 2f, 0.3f);
            BestFit(text, 30);
            text.text = label;
            return b;
        }

        private void SetActionsInteractable(bool on)
        {
            float a = on ? 1f : 0.45f;
            foreach (var b in new[] { _backButton, _hintButton, _skipButton })
            {
                b.interactable = on;
                var img = b.GetComponent<Image>();
                img.color = new Color(img.color.r, img.color.g, img.color.b, a);
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
            img.color = new Color(0.11f, 0.07f, 0.22f, 0.96f);
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
            _bannerRect.gameObject.SetActive(false);
            _actionsRoot.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            foreach (var s in _slotRects) s.gameObject.SetActive(false);

            string title = score >= 90 ? "¡Maestro de las palabras!" : score >= 70 ? "¡Muy bien!" : score >= 50 ? "Buen trabajo" : "Sigue practicando";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = Endless
                ? $"{_correct} palabras de {total} · {_points} puntos"
                : $"{_correct} de {total} palabras · {_points} puntos";
            _resultRoot.Find("Extra").GetComponent<Text>().text = _solveCount > 0
                ? $"Tiempo medio {avgMs / 1000f:0.0} s por palabra · mejor racha {_bestStreak}"
                : $"Mejor racha {_bestStreak}";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            StartCoroutine(UiFx.SparkBurst(_fxRect, Vector2.zero, Accent, 24, 420f, 56f, 0.9f));
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

        // ------------------------------------------------------------------ fichas

        private Letter CreateLetter(char c, int poolIndex)
        {
            var go = new GameObject("Letter_" + c + "_" + poolIndex);
            go.transform.SetParent(_boardRect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_tileSize / ShapeScale, _tileSize / ShapeScale);
            var img = go.AddComponent<Image>();
            img.sprite = TileSprites.Get();
            img.color = TileCream;
            img.alphaHitTestMinimumThreshold = 0.1f;

            var letter = new Letter { Rect = rect, Image = img, Char = c, InSlot = false, Target = _poolPositions[poolIndex] };
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnLetterTapped(letter));
            go.AddComponent<PressScale>();

            letter.Label = MakeText(go.transform, "Char", Mathf.RoundToInt(_tileSize * 0.62f), TextAnchor.MiddleCenter, Ink, 0f, 0f);
            letter.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
            letter.Label.verticalOverflow = VerticalWrapMode.Overflow;
            var lr = letter.Label.rectTransform;
            lr.anchorMin = new Vector2(0.06f, 0.16f);
            lr.anchorMax = new Vector2(0.94f, 0.90f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            letter.Label.text = c.ToString();

            // Las fichas nacen en su lugar del banco (sin volar desde el origen).
            rect.anchoredPosition = letter.Target;
            rect.localScale = Vector3.zero;
            return letter;
        }

        private void ClearLetters()
        {
            foreach (var l in _letters) if (l != null && l.Rect != null) Destroy(l.Rect.gameObject);
            _letters.Clear();
            _assembly.Clear();
        }

        // ------------------------------------------------------------------ layout

        private float _contentW, _safeH, _yAfterBanner, _actionsTopY;

        private void LayoutStatic()
        {
            Rect safe = _safe.rect;
            float sw = Mathf.Max(safe.width, 400f);
            _safeH = Mathf.Max(safe.height, 800f);
            _contentW = sw - MarginU * 2f;

            float y = 190f;
            _dots.Rect.anchoredPosition = new Vector2(0f, -y);
            y += 70f;
            float bannerH = 130f;
            _bannerRect.sizeDelta = new Vector2(_contentW, bannerH);
            _bannerRect.anchoredPosition = new Vector2(0f, -(y + bannerH / 2f));
            y += bannerH + 22f;
            _timerBg.sizeDelta = new Vector2(_contentW, 18f);
            _timerBg.anchoredPosition = new Vector2(0f, -(y + 9f));
            y += 18f + 34f;
            _yAfterBanner = y;

            // Botones de acción abajo: tres pastillas iguales.
            float gap = 20f;
            float bw = (_contentW - gap * 2f) / 3f;
            float bh = 130f;
            float bottomMargin = 36f;
            _actionsRoot.anchoredPosition = new Vector2(0f, bottomMargin + bh / 2f);
            var buttons = new[] { _backButton, _hintButton, _skipButton };
            for (int i = 0; i < 3; i++)
            {
                var r = (RectTransform)buttons[i].transform;
                r.sizeDelta = new Vector2(bw, bh);
                r.anchoredPosition = new Vector2((i - 1) * (bw + gap), 0f);
            }
            _actionsTopY = _safeH - (bottomMargin + bh);
            _toast.SetTopOffset(0f);
        }

        /// <summary>Casillas arriba y banco de fichas debajo, según el largo de la palabra actual.</summary>
        private void LayoutWord(int n)
        {
            float gapSlot = 14f;
            _slotSize = Mathf.Min(150f, (_contentW - (n - 1) * gapSlot) / n);
            int perRow = n <= 6 ? n : Mathf.CeilToInt(n / 2f);
            int rows = n <= 6 ? 1 : 2;
            float gapTile = 18f;
            float availableH = _actionsTopY - _yAfterBanner - _slotSize - 150f;
            _tileSize = Mathf.Clamp(Mathf.Min((_contentW - (perRow - 1) * gapTile) / perRow, (availableH - (rows - 1) * gapTile) / rows), 90f, 175f);

            // Filas de casillas y de banco centradas en el espacio disponible.
            float blockH = _slotSize + 110f + rows * _tileSize + (rows - 1) * gapTile;
            float free = Mathf.Max(0f, _actionsTopY - _yAfterBanner - blockH - 30f);
            float slotsY = _yAfterBanner + free * 0.35f + _slotSize / 2f;
            _slotsCenterY = slotsY;

            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) / 2f) * (_slotSize + gapSlot);
                _slotPositions[i] = new Vector2(x, -slotsY);
                var r = _slotRects[i];
                r.sizeDelta = new Vector2(_slotSize, _slotSize);
                r.anchoredPosition = _slotPositions[i];
                r.localScale = Vector3.one;
            }

            float poolTop = slotsY + _slotSize / 2f + 110f + _tileSize / 2f;
            for (int i = 0; i < n; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = row == 0 ? Mathf.Min(perRow, n) : n - perRow;
                float x = (col - (inRow - 1) / 2f) * (_tileSize + gapTile);
                float y = poolTop + row * (_tileSize + gapTile);
                _poolPositions[i] = new Vector2(x, -y);
            }
        }

        private Vector2 SlotsCenter() => new Vector2(0f, _safeH / 2f - _slotsCenterY);

        // ------------------------------------------------------------------ helpers

        private void UpdateHudText()
        {
            _subText.text = Endless
                ? $"Puntos {_points} · Nivel {_effLevel}"
                : $"Palabra {_trialIndex + 1} de {AnagramContract.TotalTrials} · Nivel {_effLevel}";
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
