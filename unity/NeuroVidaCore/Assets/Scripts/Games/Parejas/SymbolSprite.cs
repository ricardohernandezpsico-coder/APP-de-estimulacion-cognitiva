using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Parejas
{
    /// <summary>Íconos ilustrados de las cartas de Parejas Ocultas. Estilo "sticker"
    /// de juego casual premium (contorno grueso oscuro, relleno con degradé, brillo
    /// especular, sombra proyectada) -- objetos reconocibles y de colores naturales, no
    /// siluetas abstractas. Cada ícono tiene <see cref="SymbolSprite.VariantCount"/>
    /// variantes de color (la 3 es siempre un tono análogo de la 0, para los niveles de
    /// máxima interferencia perceptual).</summary>
    public enum ShapeKind
    {
        Apple,
        Star,
        Heart,
        Sun,
        Flower,
        Clover,
        Drop,
        Cloud,
        Fish,
        Mushroom,
        Balloon,
        Crown,
        Moon,
        Gem
    }

    /// <summary>
    /// Generador procedural de íconos (sin assets importados, mismo criterio que
    /// <c>RoundedRectSprite</c>/<c>RingSprite</c>). Cada ícono se arma pintando capas
    /// (contorno + relleno) con campos de distancia con signo (SDF) sobre un buffer
    /// RGBA, y una segunda pasada agrega la sombra proyectada suavizada. La textura ya
    /// trae los colores finales: el <c>Image.color</c> que la use debe ser blanco.
    ///
    /// Historia (23-sep): la primera versión eran emojis en un <c>Text</c> (la fuente
    /// legacy de Unity no tiene esos glifos -> cartas vacías); la segunda, siluetas
    /// geométricas tintadas, que Ricardo consideró "muy básicas, poco llamativas". Esta
    /// versión ilustrada busca el nivel visual de las apps de entrenamiento mental de la
    /// Play Store.
    /// </summary>
    public static class SymbolSprite
    {
        public const int VariantCount = 4;

        private const int SizePx = 256;
        private const float Aa = 0.02f;       // ancho del antialiasing (coordenadas -1..1)
        private const float Stroke = 0.055f;  // grosor del contorno oscuro de cada capa
        private const float Zoom = 1.12f;     // deja margen para la sombra dentro del sprite

        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite Get(ShapeKind kind, int variant)
        {
            variant = Mathf.Clamp(variant, 0, VariantCount - 1);
            int key = (int)kind * 16 + variant;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var icon = new Px[SizePx * SizePx];
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float nx = ((x + 0.5f) / SizePx * 2f - 1f) * Zoom;
                    float ny = ((y + 0.5f) / SizePx * 2f - 1f) * Zoom;
                    var p = new Px();
                    DrawIcon(ref p, kind, variant, nx, ny);
                    icon[y * SizePx + x] = p;
                }
            }

            var pixels = new Color32[SizePx * SizePx];
            const int shadowDx = 5;   // sombra abajo-derecha (luz arriba-izquierda)
            const int shadowDy = -6;
            const int blur = 5;
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float shadowSum = 0f;
                    int samples = 0;
                    for (int oy = -blur; oy <= blur; oy++)
                    {
                        for (int ox = -blur; ox <= blur; ox++)
                        {
                            int sx = x - shadowDx + ox;
                            int sy = y - shadowDy + oy;
                            samples++;
                            if (sx < 0 || sy < 0 || sx >= SizePx || sy >= SizePx) continue;
                            shadowSum += icon[sy * SizePx + sx].a;
                        }
                    }
                    float shadowA = shadowSum / samples * 0.30f;

                    var px = icon[y * SizePx + x];
                    float outA = px.a + shadowA * (1f - px.a);
                    float outR = px.r;
                    float outG = px.g;
                    float outB = px.b;
                    if (outA > 0.0001f)
                    {
                        outR /= outA;
                        outG /= outA;
                        outB /= outA;
                    }
                    pixels[y * SizePx + x] = new Color32(
                        (byte)(Mathf.Clamp01(outR) * 255f),
                        (byte)(Mathf.Clamp01(outG) * 255f),
                        (byte)(Mathf.Clamp01(outB) * 255f),
                        (byte)(Mathf.Clamp01(outA) * 255f));
                }
            }

            var texture = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), SizePx);
            Cache[key] = sprite;
            return sprite;
        }

        // ---- Buffer de pintura (RGB premultiplicado) ----

        private struct Px
        {
            public float r, g, b, a;
        }

        private static float EdgeStep(float sdf) => Mathf.Clamp01(0.5f - sdf / Aa);

        private static void Paint(ref Px p, float sdf, Color c, float alphaScale = 1f)
        {
            float a = EdgeStep(sdf) * c.a * alphaScale;
            if (a <= 0f) return;
            p.r = c.r * a + p.r * (1f - a);
            p.g = c.g * a + p.g * (1f - a);
            p.b = c.b * a + p.b * (1f - a);
            p.a = a + p.a * (1f - a);
        }

        /// <summary>Capa con contorno oscuro + relleno con degradé vertical (más claro arriba).</summary>
        private static void Layer(ref Px p, float y, float sdf, Color fill)
        {
            var outline = new Color(fill.r * 0.40f, fill.g * 0.40f, fill.b * 0.40f, 1f);
            Paint(ref p, sdf - Stroke, outline);
            float grad = Mathf.Lerp(1.18f, 0.80f, Mathf.Clamp01((0.95f - y) / 1.9f));
            var f = new Color(
                Mathf.Clamp01(fill.r * grad),
                Mathf.Clamp01(fill.g * grad),
                Mathf.Clamp01(fill.b * grad), 1f);
            Paint(ref p, sdf, f);
        }

        /// <summary>Brillo especular: mancha elíptica blanca suave, recortada al cuerpo.</summary>
        private static void Gloss(ref Px p, float x, float y, float cx, float cy, float rx, float ry, float bodySdf, float alpha)
        {
            float ex = (x - cx) / rx;
            float ey = (y - cy) / ry;
            float d = Mathf.Sqrt(ex * ex + ey * ey);
            float a = Mathf.Clamp01(1f - d);
            a = a * a * (3f - 2f * a);
            Paint(ref p, bodySdf + 0.02f, new Color(1f, 1f, 1f, alpha * a));
        }

        private static void Sparkle(ref Px p, float x, float y, float cx, float cy, float size, float alpha)
        {
            float dx = Mathf.Abs(x - cx) / size;
            float dy = Mathf.Abs(y - cy) / size;
            float d = Mathf.Pow(dx, 0.5f) + Mathf.Pow(dy, 0.5f) - 1f;
            Paint(ref p, d * size * 0.9f, new Color(1f, 1f, 1f, alpha));
        }

        // ---- Primitivas SDF (negativo = adentro) ----

        private static float CircleSdf(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        private static float EllipseSdf(float x, float y, float cx, float cy, float rx, float ry)
        {
            float ex = (x - cx) / rx;
            float ey = (y - cy) / ry;
            return (Mathf.Sqrt(ex * ex + ey * ey) - 1f) * Mathf.Min(rx, ry);
        }

        private static float RoundBoxSdf(float x, float y, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x - cx) - hx + r;
            float qy = Mathf.Abs(y - cy) - hy + r;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float CapsuleSdf(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay;
            float bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h;
            float dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        private static float PolygonSdf(float x, float y, float[] pts)
        {
            int n = pts.Length / 2;
            float d = (x - pts[0]) * (x - pts[0]) + (y - pts[1]) * (y - pts[1]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float ex = pts[j * 2] - pts[i * 2];
                float ey = pts[j * 2 + 1] - pts[i * 2 + 1];
                float wx = x - pts[i * 2];
                float wy = y - pts[i * 2 + 1];
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * t;
                float by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = y >= pts[i * 2 + 1];
                bool c2 = y < pts[j * 2 + 1];
                bool c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        private static float Union(float a, float b) => Mathf.Min(a, b);
        private static float Intersect(float a, float b) => Mathf.Max(a, b);
        private static float Subtract(float a, float b) => Mathf.Max(a, -b);

        private static void Rotate(float x, float y, float radians, out float rx, out float ry)
        {
            float c = Mathf.Cos(radians);
            float s = Mathf.Sin(radians);
            rx = x * c - y * s;
            ry = x * s + y * c;
        }

        private static float VesicaSdf(float x, float y, float halfSpan, float r)
        {
            return Intersect(CircleSdf(x, y, -halfSpan, 0f, r), CircleSdf(x, y, halfSpan, 0f, r));
        }

        /// <summary>Corazón unitario (punta abajo en el origen, ~1.2 de ancho, ~1.1 de alto).</summary>
        private static float HeartUnitSdf(float x, float y)
        {
            float px = Mathf.Abs(x);
            float py = y;
            if (py + px > 1f)
            {
                float dx = px - 0.25f, dy = py - 0.75f;
                return Mathf.Sqrt(dx * dx + dy * dy) - 0.35355f;
            }
            float a2 = px * px + (py - 1f) * (py - 1f);
            float m = 0.5f * Mathf.Max(px + py, 0f);
            float b2 = (px - m) * (px - m) + (py - m) * (py - m);
            return Mathf.Sqrt(Mathf.Min(a2, b2)) * (px - py >= 0f ? 1f : -1f);
        }

        /// <summary>Cápsula con radios distintos (gota): círculo grande en el origen, punta en (0,h).</summary>
        private static float UnevenCapsuleSdf(float x, float y, float r1, float r2, float h)
        {
            float px = Mathf.Abs(x);
            float b = (r1 - r2) / h;
            float a = Mathf.Sqrt(1f - b * b);
            float k = px * -b + y * a;
            if (k < 0f) return Mathf.Sqrt(px * px + y * y) - r1;
            if (k > a * h) return Mathf.Sqrt(px * px + (y - h) * (y - h)) - r2;
            return px * a + y * b - r1;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        private static Color Shade(Color c, float k)
        {
            return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f);
        }

        // Colores principales por ícono y variante (0..3). La variante 3 es siempre un tono
        // análogo de la 0 (para el nivel de máxima interferencia).
        private static readonly int[][] MainColors =
        {
            /* Apple    */ new[] { 0xE11D2E, 0x6BBF1F, 0xF7B500, 0xF0562B },
            /* Star     */ new[] { 0xFFC107, 0x26C6DA, 0xFF6FB5, 0xFF9800 },
            /* Heart    */ new[] { 0xE11D48, 0xFF6FB5, 0x9B5DE5, 0xFF4D6D },
            /* Sun      */ new[] { 0xFF9800, 0xFFD93D, 0xFF5E3A, 0xFFB300 },
            /* Flower   */ new[] { 0xFF5FA2, 0x9B5DE5, 0xFF8A3D, 0xFF7BC0 },
            /* Clover   */ new[] { 0x2FA84F, 0x14B8A6, 0x8BC34A, 0x43B86A },
            /* Drop     */ new[] { 0x2196F3, 0x14B8A6, 0x7C4DFF, 0x42A5F5 },
            /* Cloud    */ new[] { 0x9CCBFF, 0xC7B8FF, 0xFFD1E8, 0x7FB6F5 },
            /* Fish     */ new[] { 0xFF8A3D, 0x26C6DA, 0xFFD93D, 0xFF6F3D },
            /* Mushroom */ new[] { 0xE53935, 0x8E5BEA, 0xFF9800, 0xF4511E },
            /* Balloon  */ new[] { 0xE91E63, 0x2196F3, 0x8E44AD, 0xFF4F8B },
            /* Crown    */ new[] { 0xFFC107, 0xC0C8D8, 0xFF8FB1, 0xFFB300 },
            /* Moon     */ new[] { 0xFFD54F, 0xB79CFF, 0x7FDBFF, 0xFFC107 },
            /* Gem      */ new[] { 0x26C6DA, 0xE040FB, 0x43D17A, 0x42A5F5 },
        };

        private static void DrawIcon(ref Px p, ShapeKind kind, int variant, float x, float y)
        {
            Color main = Hex(MainColors[(int)kind][variant]);
            switch (kind)
            {
                case ShapeKind.Apple: DrawApple(ref p, x, y, main); break;
                case ShapeKind.Star: DrawStar(ref p, x, y, main); break;
                case ShapeKind.Heart: DrawHeart(ref p, x, y, main); break;
                case ShapeKind.Sun: DrawSun(ref p, x, y, main); break;
                case ShapeKind.Flower: DrawFlower(ref p, x, y, main); break;
                case ShapeKind.Clover: DrawClover(ref p, x, y, main); break;
                case ShapeKind.Drop: DrawDrop(ref p, x, y, main); break;
                case ShapeKind.Cloud: DrawCloud(ref p, x, y, main); break;
                case ShapeKind.Fish: DrawFish(ref p, x, y, main); break;
                case ShapeKind.Mushroom: DrawMushroom(ref p, x, y, main); break;
                case ShapeKind.Balloon: DrawBalloon(ref p, x, y, main); break;
                case ShapeKind.Crown: DrawCrown(ref p, x, y, main, variant); break;
                case ShapeKind.Moon: DrawMoon(ref p, x, y, main); break;
                case ShapeKind.Gem: DrawGem(ref p, x, y, main); break;
            }
        }

        // ---- Íconos ----

        private static void DrawApple(ref Px p, float x, float y, Color main)
        {
            Layer(ref p, y, CapsuleSdf(x, y, 0.02f, 0.38f, 0.10f, 0.74f, 0.05f), Hex(0x8A5A2B));
            Rotate(x - 0.34f, y - 0.62f, -0.9f, out float lx, out float ly);
            Layer(ref p, y, VesicaSdf(lx, ly, 0.13f, 0.27f), Hex(0x43A02F));

            float body = Union(CircleSdf(x, y, -0.24f, -0.10f, 0.56f), CircleSdf(x, y, 0.24f, -0.10f, 0.56f));
            body = Subtract(body, CircleSdf(x, y, 0f, 0.55f, 0.14f));
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.36f, 0.12f, 0.13f, 0.24f, body, 0.60f);
        }

        private static void DrawStar(ref Px p, float x, float y, Color main)
        {
            float angle = Mathf.Atan2(y, x);
            float radius = Mathf.Sqrt(x * x + y * y);
            const int points = 5;
            float seg = Mathf.PI * 2f / points;
            float half = seg / 2f;
            float a = Mathf.Repeat(angle - Mathf.PI / 2f + half, seg);
            float t = Mathf.Abs(a - half) / half;
            float body = radius - Mathf.Lerp(0.98f, 0.40f, Mathf.Pow(t, 0.85f)) + 0.03f;
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.16f, 0.30f, 0.14f, 0.22f, body, 0.55f);
        }

        private static void DrawHeart(ref Px p, float x, float y, Color main)
        {
            const float s = 1.5f;
            float body = HeartUnitSdf(x / s, (y + 0.86f) / s) * s;
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.40f, 0.38f, 0.16f, 0.22f, body, 0.60f);
        }

        private static void DrawSun(ref Px p, float x, float y, Color main)
        {
            float angle = Mathf.Atan2(y, x);
            float r = Mathf.Sqrt(x * x + y * y);
            float seg = Mathf.PI / 4f;
            float a = Mathf.Repeat(angle + seg / 2f, seg) - seg / 2f;
            float rx = r * Mathf.Cos(a);
            float ry = r * Mathf.Sin(a);
            float rays = CapsuleSdf(rx, ry, 0.66f, 0f, 0.90f, 0f, 0.085f);
            Layer(ref p, y, rays, Shade(main, 0.92f));

            float body = CircleSdf(x, y, 0f, 0f, 0.52f);
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.20f, 0.24f, 0.16f, 0.12f, body, 0.55f);

            var dark = Hex(0x5A2E12);
            Paint(ref p, CircleSdf(x, y, -0.18f, 0.08f, 0.055f), dark);
            Paint(ref p, CircleSdf(x, y, 0.18f, 0.08f, 0.055f), dark);
            float smile = Mathf.Abs(CircleSdf(x, y, 0f, 0.02f, 0.24f)) - 0.028f;
            smile = Mathf.Max(smile, y + 0.03f);
            Paint(ref p, smile, dark);
        }

        private static void DrawFlower(ref Px p, float x, float y, Color main)
        {
            float angle = Mathf.Atan2(y, x);
            float r = Mathf.Sqrt(x * x + y * y);
            const int petals = 5;
            float seg = Mathf.PI * 2f / petals;
            float a = Mathf.Repeat(angle - Mathf.PI / 2f + seg / 2f, seg) - seg / 2f;
            float rx = r * Mathf.Cos(a);
            float ry = r * Mathf.Sin(a);
            float body = CircleSdf(rx, ry, 0.50f, 0f, 0.36f);
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.05f, 0.60f, 0.16f, 0.10f, body, 0.45f);

            float center = CircleSdf(x, y, 0f, 0f, 0.26f);
            Layer(ref p, y, center, Hex(0xFFD54F));
            Gloss(ref p, x, y, -0.08f, 0.08f, 0.10f, 0.08f, center, 0.6f);
        }

        private static void DrawClover(ref Px p, float x, float y, Color main)
        {
            Layer(ref p, y, CapsuleSdf(x, y, 0.03f, -0.04f, 0.26f, -0.86f, 0.045f), Hex(0x2E7D32));

            float angle = Mathf.Atan2(y, x);
            float r = Mathf.Sqrt(x * x + y * y);
            float seg = Mathf.PI / 2f;
            float a = Mathf.Repeat(angle - Mathf.PI / 2f + seg / 2f, seg) - seg / 2f;
            float qx = r * Mathf.Cos(a);
            float qy = r * Mathf.Sin(a);
            const float k = 0.72f;
            float body = HeartUnitSdf(qy / k, (qx - 0.05f) / k) * k;
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.30f, 0.42f, 0.13f, 0.10f, body, 0.45f);
        }

        private static void DrawDrop(ref Px p, float x, float y, Color main)
        {
            float body = UnevenCapsuleSdf(x, y + 0.30f, 0.52f, 0.07f, 0.95f);
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.20f, -0.22f, 0.10f, 0.20f, body, 0.65f);
        }

        private static void DrawCloud(ref Px p, float x, float y, Color main)
        {
            float body = Union(
                Union(CircleSdf(x, y, -0.42f, -0.10f, 0.30f), CircleSdf(x, y, -0.04f, 0.14f, 0.42f)),
                Union(CircleSdf(x, y, 0.38f, -0.06f, 0.32f), RoundBoxSdf(x, y, 0f, -0.28f, 0.68f, 0.22f, 0.22f)));
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.16f, 0.32f, 0.20f, 0.10f, body, 0.55f);
        }

        private static void DrawFish(ref Px p, float x, float y, Color main)
        {
            var tail = new[] { -0.45f, 0f, -0.96f, 0.44f, -0.96f, -0.44f };
            Layer(ref p, y, PolygonSdf(x, y, tail), Shade(main, 0.85f));
            var fin = new[] { -0.05f, 0.36f, 0.30f, 0.78f, 0.42f, 0.32f };
            Layer(ref p, y, PolygonSdf(x, y, fin), Shade(main, 0.85f));

            float body = EllipseSdf(x, y, 0.08f, 0f, 0.70f, 0.48f);
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, 0.05f, 0.26f, 0.32f, 0.10f, body, 0.5f);

            Paint(ref p, CircleSdf(x, y, 0.42f, 0.10f, 0.12f), Color.white);
            Paint(ref p, CircleSdf(x, y, 0.45f, 0.10f, 0.06f), Hex(0x1B1B1B));
            Paint(ref p, Mathf.Max(Mathf.Abs(CircleSdf(x, y, 0.52f, -0.06f, 0.14f)) - 0.02f, y + 0.06f), Hex(0x5A2E12));
        }

        private static void DrawMushroom(ref Px p, float x, float y, Color main)
        {
            Layer(ref p, y, RoundBoxSdf(x, y, 0f, -0.46f, 0.27f, 0.34f, 0.13f), Hex(0xF5E6C8));
            float cap = Intersect(CircleSdf(x, y, 0f, 0.02f, 0.80f), -(y + 0.06f));
            Layer(ref p, y, cap, main);
            Gloss(ref p, x, y, -0.34f, 0.34f, 0.18f, 0.12f, cap, 0.5f);
            Paint(ref p, CircleSdf(x, y, -0.34f, 0.10f, 0.13f), Color.white, 0.95f);
            Paint(ref p, CircleSdf(x, y, 0.06f, 0.44f, 0.12f), Color.white, 0.95f);
            Paint(ref p, CircleSdf(x, y, 0.40f, 0.14f, 0.11f), Color.white, 0.95f);
        }

        private static void DrawBalloon(ref Px p, float x, float y, Color main)
        {
            float stringSdf = Mathf.Max(Mathf.Abs(x - 0.07f * Mathf.Sin(y * 11f)) - 0.02f, Mathf.Max(y + 0.58f, -0.96f - y));
            Paint(ref p, stringSdf, Hex(0x616161));
            var knot = new[] { 0f, -0.46f, -0.12f, -0.64f, 0.12f, -0.64f };
            Layer(ref p, y, PolygonSdf(x, y, knot), Shade(main, 0.85f));
            float body = EllipseSdf(x, y, 0f, 0.20f, 0.60f, 0.72f);
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.26f, 0.44f, 0.11f, 0.22f, body, 0.60f);
        }

        private static void DrawCrown(ref Px p, float x, float y, Color main, int variant)
        {
            var poly = new[]
            {
                -0.72f, -0.55f, 0.72f, -0.55f, 0.78f, 0.32f, 0.38f, -0.10f,
                0f, 0.50f, -0.38f, -0.10f, -0.78f, 0.32f
            };
            Layer(ref p, y, PolygonSdf(x, y, poly), main);
            Layer(ref p, y, RoundBoxSdf(x, y, 0f, -0.46f, 0.72f, 0.10f, 0.05f), Shade(main, 0.82f));

            Color jewel = variant == 1 ? Hex(0x2563EB) : Hex(0xE11D48);
            Layer(ref p, y, CircleSdf(x, y, -0.78f, 0.38f, 0.10f), Color.white);
            Layer(ref p, y, CircleSdf(x, y, 0f, 0.56f, 0.11f), Color.white);
            Layer(ref p, y, CircleSdf(x, y, 0.78f, 0.38f, 0.10f), Color.white);
            Layer(ref p, y, CircleSdf(x, y, 0f, 0.08f, 0.10f), jewel);
            Paint(ref p, CircleSdf(x, y, -0.42f, -0.46f, 0.055f), jewel);
            Paint(ref p, CircleSdf(x, y, 0.42f, -0.46f, 0.055f), jewel);
            Paint(ref p, CircleSdf(x, y, 0f, -0.46f, 0.055f), Color.white);
        }

        private static void DrawMoon(ref Px p, float x, float y, Color main)
        {
            float body = Subtract(CircleSdf(x, y, -0.05f, 0f, 0.84f), CircleSdf(x, y, 0.36f, 0.14f, 0.72f));
            Layer(ref p, y, body, main);
            Gloss(ref p, x, y, -0.50f, 0.30f, 0.12f, 0.22f, body, 0.55f);
            Sparkle(ref p, x, y, 0.50f, -0.20f, 0.10f, 0.9f);
        }

        private static void DrawGem(ref Px p, float x, float y, Color main)
        {
            var silhouette = new[] { -0.86f, 0.28f, -0.50f, 0.80f, 0.50f, 0.80f, 0.86f, 0.28f, 0f, -0.88f };
            Layer(ref p, y, PolygonSdf(x, y, silhouette), main);

            var table = new[] { -0.50f, 0.80f, 0.50f, 0.80f, 0.32f, 0.28f, -0.32f, 0.28f };
            Paint(ref p, PolygonSdf(x, y, table), Shade(main, 1.30f));
            var crownL = new[] { -0.86f, 0.28f, -0.50f, 0.80f, -0.32f, 0.28f };
            Paint(ref p, PolygonSdf(x, y, crownL), Shade(main, 1.05f));
            var crownR = new[] { 0.86f, 0.28f, 0.50f, 0.80f, 0.32f, 0.28f };
            Paint(ref p, PolygonSdf(x, y, crownR), Shade(main, 0.82f));
            var pavL = new[] { -0.86f, 0.28f, -0.32f, 0.28f, 0f, -0.88f };
            Paint(ref p, PolygonSdf(x, y, pavL), Shade(main, 0.88f));
            var pavC = new[] { -0.32f, 0.28f, 0.32f, 0.28f, 0f, -0.88f };
            Paint(ref p, PolygonSdf(x, y, pavC), Shade(main, 1.10f));
            var pavR = new[] { 0.32f, 0.28f, 0.86f, 0.28f, 0f, -0.88f };
            Paint(ref p, PolygonSdf(x, y, pavR), Shade(main, 0.68f));

            Sparkle(ref p, x, y, -0.30f, 0.52f, 0.13f, 0.95f);
        }
    }
}
