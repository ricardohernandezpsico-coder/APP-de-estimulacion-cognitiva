using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Corazones de las vidas del HUD (Secuencia Lumínica y Parejas Ocultas), generados
    /// por código -- sin assets, mismo criterio que <see cref="RoundedRectSprite"/> /
    /// <see cref="RadialGlowSprite"/>. Son dos sprites con los colores ya horneados
    /// (<c>Image.color</c> queda en blanco):
    ///
    /// - <see cref="GetFull"/>: corazón rojo brillante -- contorno rojo oscuro, degradé
    ///   vertical, brillo especular y sombra proyectada (mismo estilo "sticker" que los
    ///   íconos de las cartas de Parejas).
    /// - <see cref="GetLost"/>: corazón apagado gris azulado con una grieta -- se lee como
    ///   "vida perdida" sin depender solo del color.
    ///
    /// Historia: la primera versión era el carácter Unicode "❤"/"♡" en un <c>Text</c> (no
    /// permitía colores distintos por corazón y el glifo no se veía en el dispositivo), la
    /// segunda un corazón plano blanco tintado por <c>Image.color</c>. Ricardo (23-sep) pidió
    /// que "se vean de una mejor forma".
    /// </summary>
    public static class HeartSprite
    {
        private const int SizePx = 128;
        private const float Aa = 0.03f;
        private const float Stroke = 0.075f;
        private static Sprite _full;
        private static Sprite _lost;

        public static Sprite GetFull() => _full != null ? _full : (_full = Build(lost: false));
        public static Sprite GetLost() => _lost != null ? _lost : (_lost = Build(lost: true));

        private static Sprite Build(bool lost)
        {
            var icon = new float[SizePx * SizePx * 4]; // RGB premultiplicado + A
            for (int py = 0; py < SizePx; py++)
            {
                for (int px = 0; px < SizePx; px++)
                {
                    float x = ((px + 0.5f) / SizePx * 2f - 1f) * 1.08f;
                    float y = ((py + 0.5f) / SizePx * 2f - 1f) * 1.08f;
                    Shade(lost, x, y, out float r, out float g, out float b, out float a);
                    int i = (py * SizePx + px) * 4;
                    icon[i] = r;
                    icon[i + 1] = g;
                    icon[i + 2] = b;
                    icon[i + 3] = a;
                }
            }

            var pixels = new Color32[SizePx * SizePx];
            const int dx = 2, dy = -3, blur = 2;
            for (int py = 0; py < SizePx; py++)
            {
                for (int px = 0; px < SizePx; px++)
                {
                    float sum = 0f;
                    int n = 0;
                    for (int oy = -blur; oy <= blur; oy++)
                    {
                        for (int ox = -blur; ox <= blur; ox++)
                        {
                            n++;
                            int sx = px - dx + ox;
                            int sy = py - dy + oy;
                            if (sx < 0 || sy < 0 || sx >= SizePx || sy >= SizePx) continue;
                            sum += icon[(sy * SizePx + sx) * 4 + 3];
                        }
                    }
                    float shadowA = sum / n * (lost ? 0.18f : 0.30f);
                    int i = (py * SizePx + px) * 4;
                    float a = icon[i + 3];
                    float outA = a + shadowA * (1f - a);
                    float r = icon[i], g = icon[i + 1], b = icon[i + 2];
                    if (outA > 0.0001f)
                    {
                        r /= outA;
                        g /= outA;
                        b /= outA;
                    }
                    pixels[py * SizePx + px] = new Color32(
                        (byte)(Mathf.Clamp01(r) * 255f),
                        (byte)(Mathf.Clamp01(g) * 255f),
                        (byte)(Mathf.Clamp01(b) * 255f),
                        (byte)(Mathf.Clamp01(outA) * 255f));
                }
            }

            var tex = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float EdgeStep(float sdf) => Mathf.Clamp01(0.5f - sdf / Aa);

        /// <summary>Corazón unitario: punta abajo en el origen, ~1.2 de ancho, ~1.1 de alto.</summary>
        private static float HeartUnitSdf(float x, float y)
        {
            float px = Mathf.Abs(x);
            if (y + px > 1f)
            {
                float dx = px - 0.25f, dy = y - 0.75f;
                return Mathf.Sqrt(dx * dx + dy * dy) - 0.35355f;
            }
            float a2 = px * px + (y - 1f) * (y - 1f);
            float m = 0.5f * Mathf.Max(px + y, 0f);
            float b2 = (px - m) * (px - m) + (y - m) * (y - m);
            return Mathf.Sqrt(Mathf.Min(a2, b2)) * (px - y >= 0f ? 1f : -1f);
        }

        private static float SegmentSdf(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float ddx = pax - bax * h, ddy = pay - bay * h;
            return Mathf.Sqrt(ddx * ddx + ddy * ddy) - r;
        }

        private static void Over(ref float r, ref float g, ref float b, ref float a, float cr, float cg, float cb, float ca)
        {
            r = cr * ca + r * (1f - ca);
            g = cg * ca + g * (1f - ca);
            b = cb * ca + b * (1f - ca);
            a = ca + a * (1f - ca);
        }

        private static void Shade(bool lost, float x, float y, out float r, out float g, out float b, out float a)
        {
            r = g = b = a = 0f;

            const float s = 1.5f;
            float sdf = HeartUnitSdf(x / s, (y + 0.86f) / s) * s;

            // Contorno.
            Color outline = lost ? new Color(0.13f, 0.15f, 0.22f) : new Color(0.50f, 0.05f, 0.12f);
            Over(ref r, ref g, ref b, ref a, outline.r, outline.g, outline.b, EdgeStep(sdf - Stroke));

            // Relleno con degradé vertical.
            float t = Mathf.Clamp01((0.85f - y) / 1.7f);
            Color top = lost ? new Color(0.42f, 0.47f, 0.60f) : new Color(1.00f, 0.42f, 0.48f);
            Color bottom = lost ? new Color(0.24f, 0.28f, 0.38f) : new Color(0.83f, 0.07f, 0.18f);
            Color fill = Color.Lerp(top, bottom, t);
            float fillA = EdgeStep(sdf);
            Over(ref r, ref g, ref b, ref a, fill.r, fill.g, fill.b, fillA);

            if (lost)
            {
                // Grieta en zigzag, más oscura que el relleno.
                float crack = Mathf.Min(
                    Mathf.Min(SegmentSdf(x, y, 0.06f, 0.55f, -0.14f, 0.22f, 0.035f), SegmentSdf(x, y, -0.14f, 0.22f, 0.12f, -0.02f, 0.035f)),
                    Mathf.Min(SegmentSdf(x, y, 0.12f, -0.02f, -0.06f, -0.28f, 0.035f), SegmentSdf(x, y, -0.06f, -0.28f, 0.04f, -0.55f, 0.035f)));
                Over(ref r, ref g, ref b, ref a, 0.10f, 0.12f, 0.18f, EdgeStep(crack) * fillA);
            }
            else
            {
                // Brillo especular en el lóbulo izquierdo + destello chico.
                float ex = (x + 0.42f) / 0.20f;
                float ey = (y - 0.38f) / 0.13f;
                float d = Mathf.Sqrt(ex * ex + ey * ey);
                float gloss = Mathf.Clamp01(1f - d);
                gloss = gloss * gloss * (3f - 2f * gloss);
                Over(ref r, ref g, ref b, ref a, 1f, 1f, 1f, gloss * 0.70f * fillA);
                float dot = Mathf.Clamp01(1f - Mathf.Sqrt((x + 0.62f) * (x + 0.62f) + (y - 0.12f) * (y - 0.12f)) / 0.07f);
                Over(ref r, ref g, ref b, ref a, 1f, 1f, 1f, dot * 0.7f * fillA);
            }
        }
    }
}
