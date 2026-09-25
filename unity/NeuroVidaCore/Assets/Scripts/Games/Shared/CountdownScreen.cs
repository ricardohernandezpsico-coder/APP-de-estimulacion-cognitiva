using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Pantalla de preparación "¿Listos? / 3 - 2 - 1 / ¡Ya!" compartida por Secuencia
    /// Lumínica y Parejas Ocultas (antes cada juego tenía su copia). Rediseñada (23-sep)
    /// tomando como referencia las apps de entrenamiento mental tipo Lumosity y las de
    /// temporizador/fitness:
    ///
    /// - Fondo con degradé vertical que cambia de color por paso (índigo -> violeta ->
    ///   magenta -> verde) y burbujas de luz flotando lentamente ("bokeh").
    /// - Halo detrás del número que "respira" y una onda que se expande en cada tick.
    /// - El número saliente se agranda y se desvanece mientras el nuevo entra con rebote
    ///   (transición cruzada, no un cambio seco de dígito).
    /// - Anillo que se vacía en tiempo real (la vuelta completa ES el segundo).
    /// - Título ("Nivel X") en una píldora translúcida y subtítulo debajo.
    /// - "¡Ya!" con destello radial, onda grande y confeti; luego la pantalla se
    ///   desvanece y deja ver el juego ya armado detrás (<c>onRevealStart</c>).
    ///
    /// Es visual puro (sin tonos): los tonos de los números se sacaron porque sonaban
    /// igual que tocar una ficha y confundían (Ricardo, 22-sep).
    /// </summary>
    public sealed class CountdownScreen
    {
        private const float StepSeconds = 0.7f;
        private const float PopSeconds = 0.24f;

        private static readonly Color[] StepColors =
        {
            new Color(0x4C / 255f, 0x1D / 255f, 0x95 / 255f),
            new Color(0x7C / 255f, 0x3A / 255f, 0xED / 255f),
            new Color(0xDB / 255f, 0x27 / 255f, 0x77 / 255f),
        };
        private static readonly Color GoColor = new Color(0x16 / 255f, 0xA3 / 255f, 0x4A / 255f);
        private static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.85f, 0.25f), new Color(1f, 0.45f, 0.70f), new Color(0.35f, 0.85f, 1f),
            Color.white, new Color(1f, 0.60f, 0.20f), new Color(0.65f, 0.55f, 1f),
        };

        private readonly float _u;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _bg;
        private readonly Image _centerGlow;
        private readonly RectTransform _centerGlowRect;
        private readonly Image _ripple;
        private readonly RectTransform _rippleRect;
        private readonly Image _ringTrack;
        private readonly Image _ringFill;
        private readonly Text _number;
        private readonly Text _numberOut;
        private readonly Text _title;
        private readonly RectTransform _titleChipRect;
        private readonly Text _subtitle;
        private readonly Image _burst;
        private readonly RectTransform _burstRect;
        private readonly RectTransform[] _confetti;
        private readonly Image[] _confettiImages;
        private Vector2 _titleChipBase;

        private const int ConfettiCount = 22;

        public CountdownScreen(Transform parent, float unitsPerDp)
        {
            _u = unitsPerDp;

            var rootGo = new GameObject("CountdownScreen");
            rootGo.transform.SetParent(parent, false);
            _root = rootGo.AddComponent<RectTransform>();
            Stretch(_root);
            _group = rootGo.AddComponent<CanvasGroup>();
            _bg = rootGo.AddComponent<Image>();
            _bg.sprite = VerticalGradient();
            _bg.color = StepColors[0];

            // Burbujas de luz flotantes (las mueve CountdownAmbient en cada frame).
            var ambient = rootGo.AddComponent<CountdownAmbient>();
            ambient.Build(_root, 14);

            _centerGlowRect = NewChild("CenterGlow", _root, Vector2.zero, new Vector2(1100f, 1100f));
            _centerGlow = _centerGlowRect.gameObject.AddComponent<Image>();
            _centerGlow.sprite = RadialGlowSprite.Get();
            _centerGlow.raycastTarget = false;
            _centerGlow.color = new Color(1f, 1f, 1f, 0.26f);

            _burstRect = NewChild("Burst", _root, Vector2.zero, new Vector2(900f, 900f));
            _burst = _burstRect.gameObject.AddComponent<Image>();
            _burst.sprite = RadialGlowSprite.Get();
            _burst.raycastTarget = false;
            _burst.color = new Color(1f, 1f, 1f, 0f);

            float ringSize = 250f * _u;
            _rippleRect = NewChild("Ripple", _root, Vector2.zero, new Vector2(ringSize, ringSize));
            _ripple = _rippleRect.gameObject.AddComponent<Image>();
            _ripple.sprite = RingSprite.Get();
            _ripple.raycastTarget = false;
            _ripple.color = new Color(1f, 1f, 1f, 0f);

            var trackRect = NewChild("RingTrack", _root, Vector2.zero, new Vector2(ringSize, ringSize));
            _ringTrack = trackRect.gameObject.AddComponent<Image>();
            _ringTrack.sprite = RingSprite.Get();
            _ringTrack.raycastTarget = false;
            _ringTrack.color = new Color(1f, 1f, 1f, 0.20f);

            var fillRect = NewChild("RingFill", _root, Vector2.zero, new Vector2(ringSize, ringSize));
            _ringFill = fillRect.gameObject.AddComponent<Image>();
            _ringFill.sprite = RingSprite.Get();
            _ringFill.raycastTarget = false;
            _ringFill.type = Image.Type.Filled;
            _ringFill.fillMethod = Image.FillMethod.Radial360;
            _ringFill.fillOrigin = (int)Image.Origin360.Top;
            _ringFill.fillClockwise = true;
            _ringFill.fillAmount = 1f;
            _ringFill.color = Color.white;

            _numberOut = NewText("NumberOut", _root, Vector2.zero, new Vector2(340f * _u, 200f * _u), 118, TextAnchor.MiddleCenter);
            _number = NewText("Number", _root, Vector2.zero, new Vector2(340f * _u, 200f * _u), 118, TextAnchor.MiddleCenter);

            // Píldora del título (arriba del anillo).
            _titleChipBase = new Vector2(0f, 255f * _u);
            _titleChipRect = NewChild("TitleChip", _root, _titleChipBase, new Vector2(230f * _u, 60f * _u));
            var chip = _titleChipRect.gameObject.AddComponent<Image>();
            chip.sprite = RoundedRectSprite.Get(48);
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;
            chip.color = new Color(1f, 1f, 1f, 0.20f);
            _title = NewText("Title", _titleChipRect, Vector2.zero, new Vector2(230f * _u, 60f * _u), 30, TextAnchor.MiddleCenter);

            _subtitle = NewText("Subtitle", _root, new Vector2(0f, -250f * _u), new Vector2(900f, 90f * _u), 26, TextAnchor.MiddleCenter);

            _confetti = new RectTransform[ConfettiCount];
            _confettiImages = new Image[ConfettiCount];
            for (int i = 0; i < ConfettiCount; i++)
            {
                var r = NewChild("Confetti", _root, Vector2.zero, new Vector2(22f, 12f));
                var img = r.gameObject.AddComponent<Image>();
                img.sprite = RoundedRectSprite.Get(4);
                img.type = Image.Type.Sliced;
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f);
                _confetti[i] = r;
                _confettiImages[i] = img;
            }

            rootGo.SetActive(false);
        }

        public bool IsActive => _root.gameObject.activeSelf;

        /// <summary>Reproduce la pantalla completa. <paramref name="onRevealStart"/> se llama
        /// cuando empieza el desvanecimiento final -- ahí el juego debe volver a mostrar su
        /// contenido (queda visible debajo mientras la pantalla se desvanece).</summary>
        public IEnumerator Play(string title, string subtitle, Action onRevealStart)
        {
            _root.gameObject.SetActive(true);
            _group.alpha = 0f;
            _bg.color = StepColors[0];
            _title.text = title;
            _subtitle.text = subtitle;
            SetAlpha(_subtitle, 0f);
            SetAlpha(_number, 0f);
            SetAlpha(_numberOut, 0f);
            _ringFill.fillAmount = 1f;
            _ringFill.color = Color.white;
            _ringTrack.color = new Color(1f, 1f, 1f, 0.20f);
            _ripple.color = new Color(1f, 1f, 1f, 0f);
            _burst.color = new Color(1f, 1f, 1f, 0f);
            _centerGlow.color = new Color(1f, 1f, 1f, 0.26f);
            for (int i = 0; i < ConfettiCount; i++) _confettiImages[i].color = new Color(1f, 1f, 1f, 0f);
            _number.text = "";
            _numberOut.text = "";

            // Entrada: la pantalla aparece y la píldora del título baja suavemente.
            const float fadeIn = 0.22f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeIn);
                _group.alpha = t;
                _titleChipRect.anchoredPosition = _titleChipBase + new Vector2(0f, (1f - EaseOutCubic(t)) * 40f);
                SetAlpha(_subtitle, t);
                yield return null;
            }
            _group.alpha = 1f;
            _titleChipRect.anchoredPosition = _titleChipBase;

            for (int s = 3; s >= 1; s--)
            {
                var step = Tick(s.ToString(), StepColors[3 - s], s < 3);
                while (step.MoveNext()) yield return step.Current;
            }

            var go = Go(onRevealStart);
            while (go.MoveNext()) yield return go.Current;

            _root.gameObject.SetActive(false);
        }

        /// <summary>Un paso 3/2/1: transición cruzada de número, cambio de color de fondo,
        /// onda expansiva, halo que respira y anillo que se vacía en tiempo real.</summary>
        private IEnumerator Tick(string label, Color toColor, bool hasPrevious)
        {
            Color fromColor = _bg.color;
            _numberOut.text = hasPrevious ? _number.text : "";
            _number.text = label;

            float elapsed = 0f;
            while (elapsed < StepSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float tPop = Mathf.Clamp01(elapsed / PopSeconds);

                _number.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, EaseOutBack(tPop));
                SetAlpha(_number, Mathf.Clamp01(tPop * 1.8f));

                float tOut = Mathf.Clamp01(elapsed / (PopSeconds * 1.1f));
                _numberOut.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, EaseOutCubic(tOut));
                SetAlpha(_numberOut, 1f - tOut);

                _bg.color = Color.Lerp(fromColor, toColor, SmoothStep(tPop));

                _ringFill.fillAmount = elapsed < PopSeconds ? 1f : 1f - Mathf.Clamp01((elapsed - PopSeconds) / (StepSeconds - PopSeconds));

                float tR = Mathf.Clamp01(elapsed / 0.55f);
                _rippleRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.9f, EaseOutCubic(tR));
                _ripple.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 0f, tR));

                float breathe = 1f + 0.14f * Mathf.Sin(Mathf.Clamp01(elapsed / StepSeconds) * Mathf.PI);
                _centerGlowRect.localScale = Vector3.one * breathe;
                yield return null;
            }
            _numberOut.text = "";
            _ringFill.fillAmount = 1f;
        }

        private IEnumerator Go(Action onRevealStart)
        {
            Color fromColor = _bg.color;
            _numberOut.text = _number.text;
            _number.text = "¡Ya!";
            _subtitle.text = "";

            var rng = new System.Random(7);
            var velocity = new Vector2[ConfettiCount];
            var spin = new float[ConfettiCount];
            for (int i = 0; i < ConfettiCount; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float speed = 550f + (float)rng.NextDouble() * 850f;
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spin[i] = ((float)rng.NextDouble() - 0.5f) * 900f;
                _confetti[i].anchoredPosition = Vector2.zero;
                _confetti[i].sizeDelta = new Vector2(20f + (float)rng.NextDouble() * 14f, 10f + (float)rng.NextDouble() * 8f);
                var c = ConfettiColors[rng.Next(ConfettiColors.Length)];
                _confettiImages[i].color = c;
            }

            const float total = 0.95f;
            const float revealAt = 0.62f;
            bool revealed = false;
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / total;

                _bg.color = Color.Lerp(fromColor, GoColor, SmoothStep(Mathf.Clamp01(elapsed / 0.25f)));

                float tPop = Mathf.Clamp01(elapsed / 0.30f);
                _number.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1.22f, EaseOutBack(tPop));
                SetAlpha(_number, Mathf.Clamp01(tPop * 2f));
                float tOut = Mathf.Clamp01(elapsed / 0.22f);
                _numberOut.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, EaseOutCubic(tOut));
                SetAlpha(_numberOut, 1f - tOut);

                float ringA = Mathf.Clamp01(1f - elapsed / 0.2f);
                _ringFill.color = new Color(1f, 1f, 1f, ringA);
                _ringTrack.color = new Color(1f, 1f, 1f, 0.20f * ringA);

                float tB = Mathf.Clamp01(elapsed / 0.55f);
                _burstRect.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.7f, EaseOutCubic(tB));
                _burst.color = new Color(GoColor.r * 0.6f + 0.4f, GoColor.g * 0.6f + 0.4f, GoColor.b * 0.6f + 0.4f, Mathf.Lerp(0.85f, 0f, tB));

                float tR = Mathf.Clamp01(elapsed / 0.6f);
                _rippleRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 2.6f, EaseOutCubic(tR));
                _ripple.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.6f, 0f, tR));

                float dt = Time.unscaledDeltaTime;
                for (int i = 0; i < ConfettiCount; i++)
                {
                    velocity[i].y -= 1500f * dt;
                    var rect = _confetti[i];
                    rect.anchoredPosition += velocity[i] * dt;
                    rect.localRotation = Quaternion.Euler(0f, 0f, rect.localEulerAngles.z + spin[i] * dt);
                    var col = _confettiImages[i].color;
                    col.a = Mathf.Clamp01(1f - Mathf.Max(0f, t - 0.55f) / 0.45f);
                    _confettiImages[i].color = col;
                }

                if (!revealed && elapsed >= revealAt)
                {
                    revealed = true;
                    onRevealStart?.Invoke();
                }
                if (elapsed >= revealAt)
                {
                    _group.alpha = 1f - Mathf.Clamp01((elapsed - revealAt) / (total - revealAt));
                }
                yield return null;
            }
            if (!revealed) onRevealStart?.Invoke();
            _group.alpha = 0f;
            for (int i = 0; i < ConfettiCount; i++) _confettiImages[i].color = new Color(1f, 1f, 1f, 0f);
        }

        // ---- utilidades ----

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform NewChild(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return rect;
        }

        private Text NewText(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSizeDp, TextAnchor anchor)
        {
            var rect = NewChild(name, parent, anchoredPos, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UiFonts.Bold;
            text.fontSize = Mathf.RoundToInt(fontSizeDp * _u);
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            UiFonts.AddSoftShadow(rect.gameObject, 4f);
            return text;
        }

        private static void SetAlpha(Text text, float a)
        {
            var c = text.color;
            c.a = a;
            text.color = c;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float SmoothStep(float t) => t * t * (3f - 2f * t);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static Sprite _gradient;

        /// <summary>Degradé vertical blanco (arriba) -> gris medio (abajo): al multiplicarlo
        /// por el color del paso da un fondo más luminoso arriba y más profundo abajo.</summary>
        private static Sprite VerticalGradient()
        {
            if (_gradient != null) return _gradient;
            const int h = 128;
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1); // 0 abajo, 1 arriba
                float v = Mathf.Lerp(0.55f, 1.12f, t);
                var c = new Color(Mathf.Clamp01(v), Mathf.Clamp01(v), Mathf.Clamp01(v), 1f);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            _gradient = Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 100f);
            return _gradient;
        }
    }

    /// <summary>Mueve las burbujas de luz de fondo de <see cref="CountdownScreen"/> (suben
    /// despacio con un leve balanceo y reaparecen abajo al salir por arriba).</summary>
    public sealed class CountdownAmbient : MonoBehaviour
    {
        private RectTransform _area;
        private RectTransform[] _bubbles;
        private Vector2[] _norm;      // posición normalizada (0..1)
        private float[] _speed;       // fracción de pantalla por segundo
        private float[] _phase;

        public void Build(RectTransform area, int count)
        {
            _area = area;
            _bubbles = new RectTransform[count];
            _norm = new Vector2[count];
            _speed = new float[count];
            _phase = new float[count];
            var rng = new System.Random(21);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Bubble");
                go.transform.SetParent(area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                float size = 120f + (float)rng.NextDouble() * 300f;
                rect.sizeDelta = new Vector2(size, size);
                var img = go.AddComponent<UnityEngine.UI.Image>();
                img.sprite = RadialGlowSprite.Get();
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0.07f + (float)rng.NextDouble() * 0.09f);
                _bubbles[i] = rect;
                _norm[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                _speed[i] = 0.03f + (float)rng.NextDouble() * 0.05f;
                _phase[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
            }
        }

        private void Update()
        {
            if (_bubbles == null) return;
            var size = _area.rect.size;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _bubbles.Length; i++)
            {
                _norm[i].y += _speed[i] * dt;
                if (_norm[i].y > 1.15f) _norm[i].y = -0.15f;
                _phase[i] += dt * 0.6f;
                float sway = Mathf.Sin(_phase[i]) * 0.02f;
                _bubbles[i].anchoredPosition = new Vector2((_norm[i].x + sway) * size.x, _norm[i].y * size.y);
            }
        }
    }
}
