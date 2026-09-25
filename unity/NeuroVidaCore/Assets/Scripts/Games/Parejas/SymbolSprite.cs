using System.Collections.Generic;
using NeuroVida.Games.Shared;
using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Íconos de las cartas de Parejas Ocultas: una colección propia del CIELO NOCTURNO (el mundo de la app y de
    /// Parejas, "lunas gemelas"), en arcilla. Objetos que se pueden nombrar ("el cohete", "la luna"), porque
    /// ponerles nombre ayuda a recordarlos, y con siluetas bien distintas entre sí (redonda, alargada, en
    /// diagonal, con puntas...). Cada ícono tiene <see cref="SymbolSprite.VariantCount"/> variantes de color; la
    /// 3 es siempre un tono análogo de la 0, para los niveles de máxima interferencia.
    /// <see cref="Drop"/> no va en las cartas: es la gota de tinta del cartel de Tinta o Palabra.
    /// </summary>
    public enum ShapeKind
    {
        Planet,
        Rocket,
        Comet,
        Star,
        Moon,
        Ufo,
        Satellite,
        Sun,
        Helmet,
        Asteroid,
        Telescope,
        Crystal,
        Constellation,
        Drop
    }

    /// <summary>
    /// Generador procedural de los íconos (sin assets). Mismo sello que los íconos de la app
    /// (<c>GameIcon.kt</c>): relleno plano de la paleta, borde tinta grueso (también entre las piezas de cada
    /// objeto), sombra dura tinta hacia abajo y un brillo nítido. La textura ya trae los colores finales: el
    /// <c>Image.color</c> que la use debe ser blanco.
    ///
    /// Historia: emojis en un <c>Text</c> (invisibles con la fuente de Unity) → siluetas geométricas ("muy
    /// básicas") → objetos ilustrados tipo sticker con degradé y sombra difusa (manzana, hongo, pez con cara...),
    /// que Ricardo sintió "bien tradicionales", fuera del estilo de la app (25-sep) → esta colección.
    /// </summary>
    public static class SymbolSprite
    {
        public const int VariantCount = 4;

        private const int SizePx = 256;
        private const float Zoom = 1.2f;     // el dibujo va en ±0.95: margen para el borde y la sombra
        private const float Aa = 0.02f;
        private const float Line = 0.075f;   // borde tinta
        private const float ThinLine = 0.05f;
        private const float Drop = 0.085f;   // sombra dura
        private static readonly Color Visor = Hex(0x2E3B8F);

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite Get(ShapeKind kind, int variant)
        {
            variant = Mathf.Clamp(variant, 0, VariantCount - 1);
            int key = (int)kind * 16 + variant;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var sprite = ToSprite(Render(kind, variant, SizePx), SizePx, SizePx);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Píxeles del ícono (fila 0 = abajo). Separado de <see cref="Get"/> para poder previsualizarlo fuera de Unity.</summary>
        public static Color32[] Render(ShapeKind kind, int variant, int size)
        {
            variant = Mathf.Clamp(variant, 0, VariantCount - 1);
            Color main = MainColors[(int)kind][variant];
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * Zoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * Zoom;

                    // Sombra dura: la silueta completa (con su borde) corrida hacia abajo.
                    var shadow = new Pen { ShapeOnly = true, Silhouette = 1e6f };
                    Draw(ref shadow, kind, main, x, y + Drop);
                    var pen = new Pen();
                    pen.Px.Over(Ink, Cover(shadow.Silhouette - Line, Aa));

                    Draw(ref pen, kind, main, x, y);
                    pixels[py * size + px] = pen.Px.ToColor32();
                }
            }
            return pixels;
        }

        /// <summary>
        /// Pincel de dos modos: <see cref="ShapeOnly"/> solo junta la silueta (para la sombra); si no, pinta.
        /// </summary>
        private struct Pen
        {
            public bool ShapeOnly;
            public float Silhouette;
            public float Scale;   // tamaño del ícono (las SDF se calculan en coordenadas divididas por Scale)
            public Px Px;

            /// <summary>Pieza de arcilla: borde tinta alrededor y relleno plano.</summary>
            public void Part(float sdf, Color fill, float line = Line)
            {
                sdf *= Scale;
                if (ShapeOnly) { Silhouette = Mathf.Min(Silhouette, sdf); return; }
                Px.Over(Ink, Cover(sdf - line, Aa));
                Px.Over(fill, Cover(sdf, Aa));
            }

            /// <summary>Pieza recortada por <paramref name="clip"/> SIN dibujar borde en el corte (p. ej. la mitad
            /// de adelante del anillo de un planeta: el borde del anillo sigue, el corte no se ve).</summary>
            public void PartClipped(float sdf, float clip, Color fill)
            {
                sdf *= Scale;
                clip *= Scale;
                if (ShapeOnly) { Silhouette = Mathf.Min(Silhouette, Mathf.Max(sdf, clip)); return; }
                Px.Over(Ink, Cover(Mathf.Max(sdf - Line, clip), Aa));
                Px.Over(fill, Cover(Mathf.Max(sdf, clip), Aa));
            }

            /// <summary>Detalle plano sin borde (manchas, facetas, reflejos).</summary>
            public void Fill(float sdf, Color c)
            {
                if (ShapeOnly) return;
                Px.Over(c, Cover(sdf * Scale, Aa));
            }

            /// <summary>Brillo de arcilla: óvalo blanco translúcido de borde nítido, dentro de <paramref name="body"/>.</summary>
            public void Gloss(float x, float y, float cx, float cy, float rx, float ry, float body)
            {
                if (ShapeOnly) return;
                Px.Over(new Color(1f, 1f, 1f, 0.45f), Cover(Ellipse(x, y, cx, cy, rx, ry) * Scale, Aa) * Cover(body * Scale + 0.03f, Aa));
            }
        }

        // Colores por ícono y variante (0..3); la 3 es un tono análogo de la 0.
        private static readonly Color[][] MainColors =
        {
            /* Planet        */ new[] { Grape, Lime, Coral, Orchid },
            /* Rocket        */ new[] { Coral, Sky, Lime, Orange },
            /* Comet         */ new[] { Sky, Sun, Pink, Blue },
            /* Star          */ new[] { Sun, Sky, Pink, Amber },
            /* Moon          */ new[] { Hex(0xFFE38A), Grape, Sky, Sun },
            /* Ufo           */ new[] { Lime, Coral, Grape, Mint },
            /* Satellite     */ new[] { Sun, Coral, Lime, Amber },
            /* Sun           */ new[] { Orange, Sun, Coral, Hex(0xFFA64D) },
            /* Helmet        */ new[] { Cream, Sun, Sky, Hex(0xFFE3C4) },
            /* Asteroid      */ new[] { Hex(0xD4A373), Hex(0x9AA5D6), Hex(0xE58A6B), Hex(0xC08A5B) },
            /* Telescope     */ new[] { Sky, Coral, Grape, Blue },
            /* Crystal       */ new[] { Sky, Pink, Lime, Blue },
            /* Constellation */ new[] { Sun, Sky, Pink, Amber },
            /* Drop          */ new[] { Blue, Mint, Grape, Sky },
        };

        /// <summary>
        /// Encuadre de cada ícono (escala, corrimiento x, y): centrado en la carta y con el mismo tamaño aparente
        /// (el lado mayor, con borde y sombra, ocupa ~88% del sprite; los que van en diagonal, un poco menos porque
        /// llenan más). Medido con tools/art-preview.
        /// </summary>
        private static readonly float[][] Fit =
        {
            /* Planet        */ new[] { 1.049f, 0.000f, 0.042f },
            /* Rocket        */ new[] { 1.340f, 0.234f, 0.170f },
            /* Comet         */ new[] { 1.400f, 0.133f, 0.171f },
            /* Star          */ new[] { 1.080f, 0.000f, 0.000f },
            /* Moon          */ new[] { 1.159f, 0.192f, 0.042f },
            /* Ufo           */ new[] { 1.133f, 0.000f, -0.019f },
            /* Satellite     */ new[] { 1.062f, 0.000f, 0.037f },
            /* Sun           */ new[] { 1.036f, 0.000f, 0.042f },
            /* Helmet        */ new[] { 1.184f, 0.000f, 0.070f },
            /* Asteroid      */ new[] { 1.264f, -0.075f, 0.122f },
            /* Telescope     */ new[] { 1.179f, -0.047f, 0.225f },
            /* Crystal       */ new[] { 1.133f, 0.000f, 0.089f },
            /* Constellation */ new[] { 1.167f, -0.005f, 0.014f },
            /* Drop          */ new[] { 1.184f, 0.000f, 0.131f },
        };

        private static void Draw(ref Pen p, ShapeKind kind, Color main, float x, float y)
        {
            var fit = Fit[(int)kind];
            p.Scale = fit[0];
            x = (x - fit[1]) / p.Scale;
            y = (y - fit[2]) / p.Scale;
            switch (kind)
            {
                case ShapeKind.Planet: DrawPlanet(ref p, x, y, main); break;
                case ShapeKind.Rocket: DrawRocket(ref p, x, y, main); break;
                case ShapeKind.Comet: DrawComet(ref p, x, y, main); break;
                case ShapeKind.Star: DrawStar(ref p, x, y, main); break;
                case ShapeKind.Moon: DrawMoon(ref p, x, y, main); break;
                case ShapeKind.Ufo: DrawUfo(ref p, x, y, main); break;
                case ShapeKind.Satellite: DrawSatellite(ref p, x, y, main); break;
                case ShapeKind.Sun: DrawSun(ref p, x, y, main); break;
                case ShapeKind.Helmet: DrawHelmet(ref p, x, y, main); break;
                case ShapeKind.Asteroid: DrawAsteroid(ref p, x, y, main); break;
                case ShapeKind.Telescope: DrawTelescope(ref p, x, y, main); break;
                case ShapeKind.Crystal: DrawCrystal(ref p, x, y, main); break;
                case ShapeKind.Constellation: DrawConstellation(ref p, x, y, main); break;
                default: DrawDrop(ref p, x, y, main); break;
            }
        }

        // ------------------------------------------------------------------ íconos

        /// <summary>Planeta con anillo inclinado: la mitad de atrás del anillo pasa detrás del planeta.</summary>
        private static void DrawPlanet(ref Pen p, float x, float y, Color main)
        {
            Rotate(x, y, 0f, 0f, 0.32f, out float rx, out float ry);
            float ring = Mathf.Abs(Ellipse(rx, ry, 0f, 0f, 0.84f, 0.25f)) - 0.085f;
            Color ringColor = Sun;
            p.PartClipped(ring, -ry, ringColor);                         // mitad de atrás

            float body = Circle(x, y, 0f, 0f, 0.52f);
            p.Part(body, main);
            p.Fill(Circle(x, y, -0.20f, -0.22f, 0.11f), Shade(main, 0.86f));
            p.Fill(Circle(x, y, 0.24f, 0.24f, 0.07f), Shade(main, 0.86f));
            p.Gloss(x, y, -0.22f, 0.30f, 0.17f, 0.09f, body);

            p.PartClipped(ring, ry, ringColor);                          // mitad de adelante
            p.Fill(Mathf.Max(Mathf.Abs(Ellipse(rx, ry, 0f, 0f, 0.84f, 0.25f)) - 0.02f, ry + 0.01f),
                WithAlpha(Color.white, 0.35f));                          // canto de luz del anillo
        }

        /// <summary>Cohete en diagonal con ventanilla, aletas y llama.</summary>
        private static void DrawRocket(ref Pen p, float x, float y, Color main)
        {
            Rotate(x, y, 0f, 0f, 0.55f, out float u, out float v);
            float flameAxis = -(v + 0.46f);
            p.Part(UnevenCapsule(u, flameAxis, 0.16f, 0.04f, 0.36f), Sun);
            p.Fill(UnevenCapsule(u, flameAxis, 0.07f, 0.02f, 0.22f), Coral);

            p.Part(Polygon(Mathf.Abs(u), v, RocketFin), main);

            float body = Intersect(Intersect(Circle(u, v, -0.60f, -0.04f, 0.90f), Circle(u, v, 0.60f, -0.04f, 0.90f)), -0.48f - v);
            p.Part(body, Cream);
            p.Part(Intersect(body, 0.30f - v), main);                  // punta
            p.Gloss(u, v, -0.13f, -0.06f, 0.05f, 0.24f, body);

            float window = Circle(u, v, 0f, 0.04f, 0.15f);
            p.Part(window, Sky, ThinLine);
            p.Gloss(u, v, -0.04f, 0.09f, 0.06f, 0.04f, window);
        }

        /// <summary>Cometa: cabeza abajo a la izquierda y cola que se abre hacia arriba a la derecha.</summary>
        private static void DrawComet(ref Pen p, float x, float y, Color main)
        {
            const float hx = -0.36f, hy = -0.36f, k = 0.70710678f;
            float dx = x - hx, dy = y - hy;
            float across = dx * k - dy * k;
            float along = dx * k + dy * k;
            p.Part(UnevenCapsule(across, along, 0.34f, 0.08f, 1.12f), Tint(main, 0.5f));
            p.Fill(UnevenCapsule(across, along, 0.20f, 0.04f, 0.94f), Tint(main, 0.78f));
            float head = Circle(x, y, hx, hy, 0.34f);
            p.Part(head, main);
            p.Gloss(x, y, hx - 0.10f, hy + 0.13f, 0.12f, 0.07f, head);
        }

        private static void DrawStar(ref Pen p, float x, float y, Color main)
        {
            float body = Star(x, y, 0f, -0.04f, 5, 0.94f, 3.0f, 0.11f);
            p.Part(body, main);
            p.Fill(Star(x, y, 0f, -0.04f, 5, 0.46f, 3.0f, 0.06f), Tint(main, 0.3f));
            p.Gloss(x, y, -0.20f, 0.20f, 0.11f, 0.07f, body);
        }

        /// <summary>Luna creciente con cráteres.</summary>
        private static void DrawMoon(ref Pen p, float x, float y, Color main)
        {
            // Media luna exacta con las puntas redondeadas (radio 0.05): sin puntas filosas ni bordes estirados.
            Rotate(x + 0.06f, y, 0f, 0f, -0.405f, out float mx, out float my);
            float body = Crescent(mx, my, 0.457f, 0.75f, 0.71f) - 0.05f;
            p.Part(body, main);
            Color crater = Shade(main, 0.84f);
            p.Fill(Circle(x, y, -0.46f, 0.02f, 0.12f), crater);
            p.Fill(Circle(x, y, -0.22f, -0.46f, 0.08f), crater);
            p.Fill(Circle(x, y, -0.36f, 0.46f, 0.065f), crater);
            p.Gloss(x, y, -0.60f, -0.10f, 0.06f, 0.18f, body);
        }

        /// <summary>
        /// Media luna (sdMoon de Quilez, distancia exacta): círculo de radio <paramref name="ra"/> en el origen
        /// menos otro de radio <paramref name="rb"/> corrido <paramref name="d"/> sobre el eje x.
        /// </summary>
        private static float Crescent(float x, float y, float d, float ra, float rb)
        {
            y = Mathf.Abs(y);
            float a = (ra * ra - rb * rb + d * d) / (2f * d);
            float b = Mathf.Sqrt(Mathf.Max(ra * ra - a * a, 0f));
            if (d * (x * b - y * a) > d * d * Mathf.Max(b - y, 0f))
                return Mathf.Sqrt((x - a) * (x - a) + (y - b) * (y - b));
            return Mathf.Max(Mathf.Sqrt(x * x + y * y) - ra, -(Mathf.Sqrt((x - d) * (x - d) + y * y) - rb));
        }

        /// <summary>Platillo volador: cúpula, disco con luces y panza.</summary>
        private static void DrawUfo(ref Pen p, float x, float y, Color main)
        {
            float dome = Intersect(Circle(x, y, 0f, 0.06f, 0.40f), 0.02f - y);
            p.Part(dome, Tint(Sky, 0.45f));
            p.Gloss(x, y, -0.14f, 0.28f, 0.10f, 0.06f, dome);
            p.Part(Ellipse(x, y, 0f, -0.20f, 0.40f, 0.15f), Shade(main, 0.78f));
            float disc = Ellipse(x, y, 0f, 0.0f, 0.86f, 0.24f);
            p.Part(disc, main);
            p.Gloss(x, y, -0.42f, 0.08f, 0.22f, 0.045f, disc);
            p.Part(Circle(x, y, -0.50f, -0.04f, 0.07f), Sun, ThinLine);
            p.Part(Circle(x, y, 0f, -0.08f, 0.07f), Sun, ThinLine);
            p.Part(Circle(x, y, 0.50f, -0.04f, 0.07f), Sun, ThinLine);
        }

        /// <summary>Satélite inclinado: cuerpo, dos paneles solares con su grilla y antena.</summary>
        private static void DrawSatellite(ref Pen p, float x, float y, Color main)
        {
            Rotate(x, y, 0f, 0f, 0.42f, out float u, out float v);
            p.Part(Capsule(u, v, -0.62f, 0f, 0.62f, 0f, 0.05f), Cream, ThinLine);
            float au = Mathf.Abs(u);
            float panel = RoundBox(au, v, 0.64f, 0f, 0.28f, 0.22f, 0.05f);
            p.Part(panel, Blue);
            float grid = Mathf.Min(Mathf.Abs(au - 0.64f), Mathf.Abs(v)) - 0.02f;
            p.Fill(Intersect(grid, panel + 0.02f), WithAlpha(Ink, 0.85f));
            p.Gloss(au, v, 0.56f, 0.12f, 0.12f, 0.04f, panel);

            p.Part(Capsule(u, v, 0f, 0.20f, 0f, 0.50f, 0.035f), Cream, ThinLine);
            p.Part(Circle(u, v, 0f, 0.56f, 0.08f), Coral, ThinLine);
            float body = RoundBox(u, v, 0f, 0f, 0.24f, 0.28f, 0.08f);
            p.Part(body, main);
            p.Part(Circle(u, v, 0f, -0.02f, 0.09f), Sky, ThinLine);
            p.Gloss(u, v, -0.10f, 0.16f, 0.06f, 0.05f, body);
        }

        /// <summary>Sol con 8 rayos redondeados (sin cara).</summary>
        private static void DrawSun(ref Pen p, float x, float y, Color main)
        {
            float ang = Mathf.Atan2(y, x);
            float r = Mathf.Sqrt(x * x + y * y);
            const float seg = Mathf.PI / 4f;
            float a = Mathf.Repeat(ang + seg / 2f, seg) - seg / 2f;
            float along = r * Mathf.Cos(a), across = r * Mathf.Sin(a);
            p.Part(UnevenCapsule(across, along - 0.60f, 0.13f, 0.06f, 0.24f), Tint(main, 0.25f));
            float body = Circle(x, y, 0f, 0f, 0.52f);
            p.Part(body, main);
            p.Fill(Circle(x, y, 0.04f, -0.04f, 0.33f), Shade(main, 0.93f));
            p.Gloss(x, y, -0.22f, 0.26f, 0.15f, 0.08f, body);
        }

        /// <summary>Casco de astronauta con visor oscuro y su reflejo.</summary>
        private static void DrawHelmet(ref Pen p, float x, float y, Color main)
        {
            Color trim = Shade(main, 0.80f);
            p.Part(RoundBox(Mathf.Abs(x), y, 0.68f, 0.06f, 0.08f, 0.18f, 0.07f), trim);
            float shell = Circle(x, y, 0f, 0.08f, 0.68f);
            p.Part(shell, main);
            p.Part(RoundBox(x, y, 0f, -0.66f, 0.50f, 0.15f, 0.14f), trim);
            p.Gloss(x, y, -0.36f, 0.58f, 0.15f, 0.07f, shell);
            float visor = RoundBox(x, y, 0f, 0.10f, 0.46f, 0.32f, 0.28f);
            p.Part(visor, Visor);
            p.Fill(Ellipse(x, y, -0.18f, 0.26f, 0.15f, 0.07f), WithAlpha(Color.white, 0.8f));
            p.Fill(Circle(x, y, 0.22f, -0.06f, 0.045f), WithAlpha(Color.white, 0.6f));
        }

        /// <summary>Asteroide: roca irregular con cráteres.</summary>
        private static void DrawAsteroid(ref Pen p, float x, float y, Color main)
        {
            float ang = Mathf.Atan2(y, x);
            float len = Mathf.Sqrt(x * x + y * y);
            float radius = 0.70f + 0.06f * Mathf.Sin(3f * ang + 0.8f) + 0.04f * Mathf.Sin(5f * ang + 2.1f) + 0.02f * Mathf.Sin(7f * ang);
            float body = (len - radius) * 0.85f;
            p.Part(body, main);
            Crater(ref p, x, y, -0.26f, 0.18f, 0.16f, main);
            Crater(ref p, x, y, 0.30f, -0.22f, 0.13f, main);
            Crater(ref p, x, y, 0.24f, 0.38f, 0.08f, main);
            Crater(ref p, x, y, -0.30f, -0.38f, 0.075f, main);
            p.Gloss(x, y, -0.36f, 0.48f, 0.14f, 0.06f, body);
        }

        private static void Crater(ref Pen p, float x, float y, float cx, float cy, float r, Color main)
        {
            p.Fill(Circle(x, y, cx, cy - r * 0.18f, r), Tint(main, 0.3f));   // canto de luz abajo
            p.Fill(Circle(x, y, cx, cy, r), Shade(main, 0.78f));
        }

        /// <summary>Telescopio sobre trípode, apuntando arriba a la derecha.</summary>
        private static void DrawTelescope(ref Pen p, float x, float y, Color main)
        {
            p.Part(Capsule(x, y, -0.02f, -0.16f, -0.48f, -0.86f, 0.045f), Cream, ThinLine);
            p.Part(Capsule(x, y, 0.02f, -0.16f, 0.46f, -0.86f, 0.045f), Cream, ThinLine);
            p.Part(Capsule(x, y, 0f, -0.16f, 0f, -0.90f, 0.045f), Cream, ThinLine);

            Rotate(x, y, 0f, 0.10f, -0.52f, out float u, out float v);
            Color trim = Shade(main, 0.78f);
            p.Part(RoundBox(u, v, -0.60f, 0f, 0.09f, 0.10f, 0.04f), trim);
            float tube = RoundBox(u, v, 0.04f, 0f, 0.56f, 0.17f, 0.08f);
            p.Part(tube, main);
            p.Gloss(u, v, 0.06f, 0.08f, 0.34f, 0.04f, tube);
            p.Part(RoundBox(u, v, -0.14f, 0f, 0.05f, 0.18f, 0.02f), Sun, ThinLine);
            p.Part(RoundBox(u, v, 0.62f, 0f, 0.09f, 0.23f, 0.06f), trim);
            p.Part(Circle(x, y, 0f, -0.14f, 0.09f), trim, ThinLine);
        }

        // Polígonos fijos (fuera del bucle de píxeles: sin basura por píxel).
        private static readonly float[] RocketFin = { 0.22f, 0.02f, 0.50f, -0.36f, 0.50f, -0.56f, 0.22f, -0.42f };
        private static readonly float[] CrystalOutline = { -0.80f, 0.30f, -0.46f, 0.78f, 0.46f, 0.78f, 0.80f, 0.30f, 0f, -0.86f };
        private static readonly float[] CrystalTable = { -0.46f, 0.78f, 0.46f, 0.78f, 0.28f, 0.30f, -0.28f, 0.30f };
        private static readonly float[] CrystalCrownL = { -0.80f, 0.30f, -0.46f, 0.78f, -0.28f, 0.30f };
        private static readonly float[] CrystalCrownR = { 0.80f, 0.30f, 0.46f, 0.78f, 0.28f, 0.30f };
        private static readonly float[] CrystalPavilionL = { -0.28f, 0.30f, 0.28f, 0.30f, 0f, -0.86f };
        private static readonly float[] CrystalPavilionR = { 0.28f, 0.30f, 0.80f, 0.30f, 0f, -0.86f };

        /// <summary>Cristal tallado con facetas planas y un destello.</summary>
        private static void DrawCrystal(ref Pen p, float x, float y, Color main)
        {
            float body = Polygon(x, y, CrystalOutline);
            p.Part(body, main);
            p.Fill(Polygon(x, y, CrystalTable), Tint(main, 0.38f));
            p.Fill(Polygon(x, y, CrystalCrownL), Tint(main, 0.18f));
            p.Fill(Polygon(x, y, CrystalCrownR), Shade(main, 0.88f));
            p.Fill(Polygon(x, y, CrystalPavilionL), Tint(main, 0.2f));
            p.Fill(Polygon(x, y, CrystalPavilionR), Shade(main, 0.8f));
            p.Fill(Intersect(Mathf.Abs(y - 0.30f) - 0.014f, body + 0.01f), WithAlpha(Ink, 0.45f));
            p.Fill(Star(x, y, -0.26f, 0.54f, 4, 0.15f, 2.6f, 0.01f), Color.white);
        }

        /// <summary>Constelación: tres estrellas unidas por trazos.</summary>
        private static void DrawConstellation(ref Pen p, float x, float y, Color main)
        {
            const float ax = -0.56f, ay = -0.44f, bx = -0.10f, by = 0.42f, cx = 0.58f, cy = -0.10f;
            p.Part(Capsule(x, y, ax, ay, bx, by, 0.05f), Cream, ThinLine);
            p.Part(Capsule(x, y, bx, by, cx, cy, 0.05f), Cream, ThinLine);
            StarPart(ref p, x, y, ax, ay, 0.28f, main);
            StarPart(ref p, x, y, bx, by, 0.32f, main);
            StarPart(ref p, x, y, cx, cy, 0.27f, main);
        }

        private static void StarPart(ref Pen p, float x, float y, float cx, float cy, float r, Color main)
        {
            float s = Star(x, y, cx, cy, 5, r, 3.0f, 0.06f);
            p.Part(s, main, ThinLine * 1.2f);
            p.Gloss(x, y, cx - r * 0.2f, cy + r * 0.24f, r * 0.14f, r * 0.09f, s);
        }

        /// <summary>Gota (la tinta del cartel de Tinta o Palabra).</summary>
        private static void DrawDrop(ref Pen p, float x, float y, Color main)
        {
            float body = UnevenCapsule(x, y + 0.34f, 0.52f, 0.07f, 0.98f);
            p.Part(body, main);
            p.Gloss(x, y, -0.22f, -0.20f, 0.09f, 0.17f, body);
        }
    }
}
