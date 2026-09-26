using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Pantalla de preparación "¿Listos? / 3 - 2 - 1 / ¡Ya!" de los 9 juegos, con el sello "noche + arcilla"
    /// de la app (rediseño 25-sep; antes era un degradé índigo -> magenta -> verde con confeti):
    ///
    /// - Cielo nocturno de la app (mismo degradé y nebulosas celeste / uva / coral que <c>CosmosBackground</c>)
    ///   con un campo de estrellas en perspectiva (<see cref="StarfieldFx"/>) que acelera en cada número.
    /// - Número gigante "de arcilla" (Fredoka, relleno de color, contorno grueso tinta y sombra dura), que
    ///   cambia de color por paso: celeste (3) -> uva (2) -> coral (1) -> sol ("¡Ya!"). Energía creciente, de
    ///   frío a cálido.
    /// - Halo del color del paso detrás del número, anillo de órbita que se vacía en tiempo real con una
    ///   estrella que recorre su borde, y destellos de 4 puntas que brillan alrededor en cada número.
    /// - Título en una píldora de arcilla crema (como las de la app) y subtítulo debajo.
    /// - "¡Ya!": salto al hiperespacio (las estrellas se estiran en estelas), destello, onda y lluvia de
    ///   estrellas de colores; luego la pantalla se desvanece y deja ver el juego ya armado (<c>onRevealStart</c>).
    ///
    /// Es visual puro (sin tonos): los tonos de los números se sacaron porque sonaban igual que tocar una
    /// ficha y confundían (Ricardo, 22-sep).
    /// </summary>
    public sealed class CountdownScreen
    {
        private const float StepSeconds = 0.7f;
        private const float PopSeconds = 0.24f;
        private const int TickSparkles = 6;
        private const int GoSparkles = 26;

        /// <summary>Marca visible (solo en builds de depuración, abajo de la pantalla) para confirmar en el
        /// teléfono que el APK trae esta versión de los juegos: el export de Unity está fuera de git y si no se
        /// reexporta, Gradle empaqueta el viejo sin avisar. Cambiarla junto con cambios visibles de Unity.</summary>
        /// <summary>Muestra la marca de version bajo la cuenta regresiva. Antes dependia de <c>Debug.isDebugBuild</c>,
        /// pero la exportacion de Unity es de produccion y nunca se veia. Poner en false antes de publicar en la tienda.</summary>
        public const bool ShowStyleStamp = true;
        public const string StyleStamp = "estilo 26-sep · g";

        private static readonly Color[] StepColors = { NeuroStyle.Sky, NeuroStyle.Grape, NeuroStyle.Coral };
        private static readonly float[] StepWarp = { 0.07f, 0.15f, 0.27f };
        private static readonly Color GoColor = NeuroStyle.Sun;
        private static readonly Color[] SparkColors =
        {
            NeuroStyle.Sun, NeuroStyle.Coral, NeuroStyle.Sky, NeuroStyle.Grape, NeuroStyle.Lime, NeuroStyle.Cream,
        };

        private readonly float _u;
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly StarfieldFx _stars;
        private readonly Image _centerGlow;
        private readonly RectTransform _centerGlowRect;
        private readonly Image _burst;
        private readonly RectTransform _burstRect;
        private readonly Image _ripple;
        private readonly RectTransform _rippleRect;
        private readonly Image _ringTrack;
        private readonly Image _ringFill;
        private readonly RectTransform _orbitStar;
        private readonly Image _orbitStarImage;
        private readonly float _orbitRadius;
        private readonly Text _number;
        private readonly Text _numberOut;
        private readonly Text _title;
        private readonly RectTransform _titleChipRect;
        private readonly Text _subtitle;
        private readonly RectTransform[] _sparkles;
        private readonly Image[] _sparkleImages;
        private Vector2 _titleChipBase;
        private readonly System.Random _rng = new System.Random(5);

        public CountdownScreen(Transform parent, float unitsPerDp)
        {
            _u = unitsPerDp;

            var rootGo = new GameObject("CountdownScreen");
            rootGo.transform.SetParent(parent, false);
            _root = rootGo.AddComponent<RectTransform>();
            Stretch(_root);
            _group = rootGo.AddComponent<CanvasGroup>();
            var bg = rootGo.AddComponent<Image>(); // también bloquea los toques al juego mientras dura
            bg.sprite = NeuroStyle.NightGradient();

            // Nebulosas de la app: celeste arriba a la derecha, uva al medio a la izquierda, coral abajo.
            AddNebula(new Vector2(0.90f, 0.90f), 1950f, NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.27f));
            AddNebula(new Vector2(0.10f, 0.45f), 1730f, NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.19f));
            AddNebula(new Vector2(0.05f, 0.05f), 1840f, NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.24f));

            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(_root, false);
            var starsRect = starsGo.AddComponent<RectTransform>();
            Stretch(starsRect);
            _stars = starsGo.AddComponent<StarfieldFx>();
            _stars.VanishingPoint = new Vector2(0.5f, 0.52f);
            _stars.Build(starsRect, 90, 4f * _u);

            _centerGlowRect = NewChild("CenterGlow", _root, Vector2.zero, new Vector2(1150f, 1150f));
            _centerGlow = AddImage(_centerGlowRect, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(StepColors[0], 0.35f));

            _burstRect = NewChild("Burst", _root, Vector2.zero, new Vector2(1000f, 1000f));
            _burst = AddImage(_burstRect, RadialGlowSprite.Get(), new Color(1f, 1f, 1f, 0f));

            float ringSize = 250f * _u;
            _orbitRadius = ringSize * 0.435f; // línea media del anillo de RingSprite (radios 0.39..0.48)
            _rippleRect = NewChild("Ripple", _root, Vector2.zero, new Vector2(ringSize, ringSize));
            _ripple = AddImage(_rippleRect, RingSprite.Get(), new Color(1f, 1f, 1f, 0f));

            var trackRect = NewChild("OrbitTrack", _root, Vector2.zero, new Vector2(ringSize, ringSize));
            _ringTrack = AddImage(trackRect, RingSprite.Get(), NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.14f));

            var fillRect = NewChild("OrbitFill", _root, Vector2.zero, new Vector2(ringSize, ringSize));
            _ringFill = AddImage(fillRect, RingSprite.Get(), StepColors[0]);
            _ringFill.type = Image.Type.Filled;
            _ringFill.fillMethod = Image.FillMethod.Radial360;
            _ringFill.fillOrigin = (int)Image.Origin360.Top;
            _ringFill.fillClockwise = true;
            _ringFill.fillAmount = 1f;

            // Estrella que recorre el borde del anillo mientras se vacía (marca "dónde va" el segundo).
            _orbitStar = NewChild("OrbitStar", _root, new Vector2(0f, _orbitRadius), new Vector2(44f * _u, 44f * _u));
            _orbitStarImage = AddImage(_orbitStar, SparkleSprite.Get(), Color.white);

            _numberOut = NewText("NumberOut", _root, Vector2.zero, new Vector2(340f * _u, 200f * _u), 140, TextAnchor.MiddleCenter, clay: true);
            _number = NewText("Number", _root, Vector2.zero, new Vector2(340f * _u, 200f * _u), 140, TextAnchor.MiddleCenter, clay: true);

            // Píldora de arcilla crema con el título (arriba del anillo), como las píldoras de la app.
            _titleChipBase = new Vector2(0f, 262f * _u);
            _titleChipRect = NewChild("TitleChip", _root, _titleChipBase, new Vector2(230f * _u, 62f * _u));
            AddClayLayer(_titleChipRect, "Shadow", NeuroStyle.Ink, new Vector2(0f, -5f * _u), new Vector2(0f, -5f * _u));
            AddClayLayer(_titleChipRect, "Border", NeuroStyle.Ink, Vector2.zero, Vector2.zero);
            float b = 3f * _u;
            AddClayLayer(_titleChipRect, "Fill", NeuroStyle.Cream, new Vector2(b, b), new Vector2(-b, -b));
            _title = NewText("Title", _titleChipRect, Vector2.zero, new Vector2(230f * _u, 62f * _u), 30, TextAnchor.MiddleCenter, clay: false);
            _title.color = NeuroStyle.Ink;
            _title.GetComponent<Shadow>().enabled = false; // texto tinta sobre crema: sin sombra

            _subtitle = NewText("Subtitle", _root, new Vector2(0f, -262f * _u), new Vector2(960f, 90f * _u), 26, TextAnchor.MiddleCenter, clay: false);

            if (ShowStyleStamp)
            {
                var stamp = NewText("StyleStamp", _root, Vector2.zero, new Vector2(600f, 60f), 12, TextAnchor.MiddleCenter, clay: false);
                var sr = stamp.rectTransform;
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0f);
                sr.anchoredPosition = new Vector2(0f, 90f);
                stamp.text = StyleStamp;
                stamp.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.55f);
            }

            _sparkles = new RectTransform[GoSparkles];
            _sparkleImages = new Image[GoSparkles];
            for (int i = 0; i < GoSparkles; i++)
            {
                _sparkles[i] = NewChild("Sparkle", _root, Vector2.zero, new Vector2(60f * _u, 60f * _u));
                _sparkleImages[i] = AddImage(_sparkles[i], SparkleSprite.Get(), new Color(1f, 1f, 1f, 0f));
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
            _stars.Warp = 0.03f;
            _title.text = title;
            FitTitleChip();
            _subtitle.text = subtitle;
            SetAlpha(_subtitle, 0f);
            _number.text = "";
            _numberOut.text = "";
            _number.color = NeuroStyle.WithAlpha(StepColors[0], 0f);
            _numberOut.color = NeuroStyle.WithAlpha(StepColors[0], 0f);
            _ringFill.fillAmount = 1f;
            _ringFill.color = StepColors[0];
            _ringTrack.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.14f);
            _orbitStarImage.color = Color.white;
            _orbitStar.anchoredPosition = new Vector2(0f, _orbitRadius);
            _ripple.color = new Color(1f, 1f, 1f, 0f);
            _burst.color = new Color(1f, 1f, 1f, 0f);
            _centerGlow.color = NeuroStyle.WithAlpha(StepColors[0], 0.30f);
            HideSparkles();

            // Entrada: la pantalla aparece y la píldora del título baja suavemente.
            const float fadeIn = 0.22f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += GameClock.DeltaTime;
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
                var step = Tick(s.ToString(), 3 - s, s < 3);
                while (step.MoveNext()) yield return step.Current;
            }

            var go = Go(onRevealStart);
            while (go.MoveNext()) yield return go.Current;

            _stars.Warp = 0f;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Un paso 3/2/1: el número nuevo entra con rebote en el color del paso mientras el anterior se
        /// agranda y se desvanece; el halo y el anillo toman el color, el anillo se vacía en tiempo real con la
        /// estrella en su borde, las estrellas aceleran un poco y brillan destellos alrededor.</summary>
        private IEnumerator Tick(string label, int stepIndex, bool hasPrevious)
        {
            Color col = StepColors[stepIndex];
            Color fromGlow = _centerGlow.color;
            float fromWarp = _stars.Warp;
            float toWarp = StepWarp[stepIndex];

            _numberOut.text = hasPrevious ? _number.text : "";
            _numberOut.color = _number.color;
            _number.text = label;
            _number.color = NeuroStyle.WithAlpha(col, 0f);
            _ringFill.color = col;

            var delay = new float[TickSparkles];
            for (int k = 0; k < TickSparkles; k++)
            {
                float angle = (float)(_rng.NextDouble() * Mathf.PI * 2.0);
                float dist = _orbitRadius * (0.62f + (float)_rng.NextDouble() * 0.6f);
                _sparkles[k].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                float size = (34f + (float)_rng.NextDouble() * 36f) * _u;
                _sparkles[k].sizeDelta = new Vector2(size, size);
                _sparkleImages[k].color = NeuroStyle.WithAlpha(k % 2 == 0 ? NeuroStyle.Cream : col, 0f);
                delay[k] = (float)_rng.NextDouble() * 0.28f;
            }

            float elapsed = 0f;
            while (elapsed < StepSeconds)
            {
                elapsed += GameClock.DeltaTime;
                float tPop = Mathf.Clamp01(elapsed / PopSeconds);

                _number.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, EaseOutBack(tPop));
                SetAlpha(_number, Mathf.Clamp01(tPop * 1.8f));

                float tOut = Mathf.Clamp01(elapsed / (PopSeconds * 1.1f));
                _numberOut.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, EaseOutCubic(tOut));
                SetAlpha(_numberOut, 1f - tOut);

                _centerGlow.color = Color.Lerp(fromGlow, NeuroStyle.WithAlpha(col, 0.38f), SmoothStep(tPop));
                _stars.Warp = Mathf.Lerp(fromWarp, toWarp, SmoothStep(tPop));

                float fill = elapsed < PopSeconds ? 1f : 1f - Mathf.Clamp01((elapsed - PopSeconds) / (StepSeconds - PopSeconds));
                _ringFill.fillAmount = fill;
                float a = fill * Mathf.PI * 2f;
                _orbitStar.anchoredPosition = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * _orbitRadius;
                _orbitStar.localRotation = Quaternion.Euler(0f, 0f, elapsed * 160f);

                float tR = Mathf.Clamp01(elapsed / 0.55f);
                _rippleRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.9f, EaseOutCubic(tR));
                _ripple.color = NeuroStyle.WithAlpha(col, Mathf.Lerp(0.55f, 0f, tR));

                float breathe = 1f + 0.14f * Mathf.Sin(Mathf.Clamp01(elapsed / StepSeconds) * Mathf.PI);
                _centerGlowRect.localScale = Vector3.one * breathe;

                for (int k = 0; k < TickSparkles; k++)
                {
                    float t = Mathf.Clamp01((elapsed - delay[k]) / 0.42f);
                    float pulse = Mathf.Sin(t * Mathf.PI);
                    _sparkles[k].localScale = Vector3.one * pulse;
                    _sparkles[k].localRotation = Quaternion.Euler(0f, 0f, t * 70f);
                    var c = _sparkleImages[k].color;
                    c.a = pulse;
                    _sparkleImages[k].color = c;
                }
                yield return null;
            }
            _numberOut.text = "";
            _ringFill.fillAmount = 1f;
            _orbitStar.anchoredPosition = new Vector2(0f, _orbitRadius);
            HideSparkles();
        }

        /// <summary>"¡Ya!": salto al hiperespacio, destello y lluvia de estrellas de colores; a mitad de camino
        /// la pantalla empieza a desvanecerse y el juego queda a la vista.</summary>
        private IEnumerator Go(Action onRevealStart)
        {
            Color fromGlow = _centerGlow.color;
            float fromWarp = _stars.Warp;
            _numberOut.text = _number.text;
            _numberOut.color = _number.color;
            _number.text = "¡Ya!";
            _number.color = NeuroStyle.WithAlpha(GoColor, 0f);
            _subtitle.text = "";

            var velocity = new Vector2[GoSparkles];
            var spin = new float[GoSparkles];
            var baseSize = new float[GoSparkles];
            for (int i = 0; i < GoSparkles; i++)
            {
                float angle = (float)(_rng.NextDouble() * Mathf.PI * 2.0);
                float speed = 650f + (float)_rng.NextDouble() * 1000f;
                velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                spin[i] = ((float)_rng.NextDouble() - 0.5f) * 400f;
                baseSize[i] = (36f + (float)_rng.NextDouble() * 44f) * _u;
                _sparkles[i].anchoredPosition = velocity[i].normalized * _orbitRadius * 0.35f;
                _sparkles[i].sizeDelta = new Vector2(baseSize[i], baseSize[i]);
                _sparkles[i].localScale = Vector3.one;
                _sparkleImages[i].color = SparkColors[_rng.Next(SparkColors.Length)];
            }

            const float total = 1.0f;
            const float revealAt = 0.62f;
            bool revealed = false;
            float elapsed = 0f;
            while (elapsed < total)
            {
                float dt = GameClock.DeltaTime;
                elapsed += dt;
                float t = elapsed / total;

                _centerGlow.color = Color.Lerp(fromGlow, NeuroStyle.WithAlpha(GoColor, 0.45f), SmoothStep(Mathf.Clamp01(elapsed / 0.25f)));
                _stars.Warp = Mathf.Lerp(fromWarp, 1f, SmoothStep(Mathf.Clamp01(elapsed / 0.45f)));

                float tPop = Mathf.Clamp01(elapsed / 0.30f);
                _number.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1.22f, EaseOutBack(tPop));
                SetAlpha(_number, Mathf.Clamp01(tPop * 2f));
                float tOut = Mathf.Clamp01(elapsed / 0.22f);
                _numberOut.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.5f, EaseOutCubic(tOut));
                SetAlpha(_numberOut, 1f - tOut);

                float ringA = Mathf.Clamp01(1f - elapsed / 0.2f);
                _ringFill.color = NeuroStyle.WithAlpha(_ringFill.color, ringA);
                _ringTrack.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.14f * ringA);
                _orbitStarImage.color = new Color(1f, 1f, 1f, ringA);

                float tB = Mathf.Clamp01(elapsed / 0.55f);
                _burstRect.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.8f, EaseOutCubic(tB));
                var flash = Color.Lerp(GoColor, Color.white, 0.5f);
                _burst.color = NeuroStyle.WithAlpha(flash, Mathf.Lerp(0.85f, 0f, tB));

                float tR = Mathf.Clamp01(elapsed / 0.6f);
                _rippleRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 2.6f, EaseOutCubic(tR));
                _ripple.color = NeuroStyle.WithAlpha(GoColor, Mathf.Lerp(0.65f, 0f, tR));

                // Lluvia de estrellas: salen disparadas, frenan (sin gravedad: están en el espacio) y se apagan.
                float drag = Mathf.Clamp01(1f - 1.6f * dt);
                for (int i = 0; i < GoSparkles; i++)
                {
                    velocity[i] *= drag;
                    var rect = _sparkles[i];
                    rect.anchoredPosition += velocity[i] * dt;
                    rect.localRotation = Quaternion.Euler(0f, 0f, rect.localEulerAngles.z + spin[i] * dt);
                    rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.45f, t);
                    var col = _sparkleImages[i].color;
                    col.a = Mathf.Clamp01(1f - Mathf.Max(0f, t - 0.5f) / 0.5f) * (0.75f + 0.25f * Mathf.Sin(elapsed * 30f + i));
                    _sparkleImages[i].color = col;
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
            HideSparkles();
        }

        // ---- utilidades ----

        private void HideSparkles()
        {
            for (int i = 0; i < GoSparkles; i++)
            {
                _sparkleImages[i].color = new Color(1f, 1f, 1f, 0f);
                _sparkles[i].localScale = Vector3.one;
            }
        }

        /// <summary>Ajusta el ancho de la píldora al título ("Nivel 3", "Repetimos el nivel 2"...).</summary>
        private void FitTitleChip()
        {
            float w = Mathf.Max(200f * _u, _title.preferredWidth + 56f * _u);
            _titleChipRect.sizeDelta = new Vector2(w, _titleChipRect.sizeDelta.y);
            _title.rectTransform.sizeDelta = new Vector2(w, _title.rectTransform.sizeDelta.y);
        }

        private void AddNebula(Vector2 anchor, float size, Color color)
        {
            var rect = NewChild("Nebula", _root, Vector2.zero, new Vector2(size, size));
            rect.anchorMin = rect.anchorMax = anchor;
            AddImage(rect, RadialGlowSprite.Get(), color);
        }

        /// <summary>Capa de la píldora de arcilla (sombra dura, borde tinta o relleno), estirada con márgenes.</summary>
        private static void AddClayLayer(RectTransform parent, string name, Color color, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            img.color = color;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
        {
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.color = color;
            return img;
        }

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

        /// <summary>Texto Fredoka. <paramref name="clay"/>: contorno tinta y sombra dura (números grandes);
        /// si no, sombra suave (subtítulo).</summary>
        private Text NewText(string name, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSizeDp, TextAnchor anchor, bool clay)
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
            if (clay) NeuroStyle.ClayText(text, 3.5f * _u, 7f * _u);
            else UiFonts.AddSoftShadow(rect.gameObject, 4f);
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
            float dt = GameClock.DeltaTime;
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
