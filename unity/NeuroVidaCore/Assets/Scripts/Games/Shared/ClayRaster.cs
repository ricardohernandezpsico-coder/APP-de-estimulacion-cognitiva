using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Pincel común para el arte procedural "de arcilla" de los juegos (íconos de Parejas, cartas, tesoros,
    /// corazones): el mismo sello que los íconos de la app (<c>ui/components/GameIcon.kt</c>): relleno PLANO del
    /// color de la paleta, borde grueso color tinta, sombra dura tinta hacia abajo (sin desenfoque) y un brillo
    /// ovalado de bordes nítidos. Nada de degradés ni sombras difusas (se leían como "stickers" de emoji).
    ///
    /// Se pinta píxel a píxel con distancias con signo (SDF, negativo = adentro) sobre un acumulador RGB
    /// premultiplicado. Coordenadas: -1..1, y hacia arriba.
    /// </summary>
    public static class ClayRaster
    {
        // Paleta de la app (ui/theme/Clay.kt) más tonos hermanos para variantes.
        public static readonly Color Ink = Hex(0x1A1240);
        public static readonly Color Coral = Hex(0xFF6B4A);
        public static readonly Color Orange = Hex(0xFF8A3D);
        public static readonly Color Sun = Hex(0xFFC93C);
        public static readonly Color Amber = Hex(0xFFA928);
        public static readonly Color Sky = Hex(0x4CC9F0);
        public static readonly Color Blue = Hex(0x5B9BFF);
        public static readonly Color Grape = Hex(0xB8A4FF);
        public static readonly Color Orchid = Hex(0xD98BFF);
        public static readonly Color Lime = Hex(0x9BE564);
        public static readonly Color Mint = Hex(0x5FD68A);
        public static readonly Color Pink = Hex(0xFF7BC0);
        public static readonly Color Cream = Hex(0xFFF8EC);

        /// <summary>Píxel acumulado (RGB premultiplicado + alfa).</summary>
        public struct Px
        {
            public float r, g, b, a;

            public void Over(Color c, float coverage)
            {
                float sa = c.a * coverage;
                if (sa <= 0f) return;
                r = c.r * sa + r * (1f - sa);
                g = c.g * sa + g * (1f - sa);
                b = c.b * sa + b * (1f - sa);
                a = sa + a * (1f - sa);
            }

            public Color32 ToColor32()
            {
                float k = a > 0.0001f ? 1f / a : 0f;
                return new Color32(
                    (byte)(Mathf.Clamp01(r * k) * 255f + 0.5f),
                    (byte)(Mathf.Clamp01(g * k) * 255f + 0.5f),
                    (byte)(Mathf.Clamp01(b * k) * 255f + 0.5f),
                    (byte)(Mathf.Clamp01(a) * 255f + 0.5f));
            }
        }

        public delegate float ShapeFn(float x, float y);
        public delegate void PaintFn(ref Px p, float x, float y);

        /// <summary>
        /// Ícono de arcilla genérico (fila 0 = abajo): sombra dura y borde tinta de la silueta
        /// <paramref name="body"/>, y encima lo que pinte <paramref name="paint"/> (rellenos, detalles).
        /// Coordenadas -1..1 multiplicadas por <paramref name="zoom"/> (margen para borde y sombra).
        /// </summary>
        public static Color32[] RenderClay(int size, float zoom, float line, float drop, float aa, ShapeFn body, PaintFn paint)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * zoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * zoom;
                    var p = new Px();
                    if (drop > 0f) p.Over(Ink, Cover(body(x, y + drop) - line, aa));
                    p.Over(Ink, Cover(body(x, y) - line, aa));
                    paint(ref p, x, y);
                    pixels[py * size + px] = p.ToColor32();
                }
            }
            return pixels;
        }

        /// <summary>Cobertura antialiaseada de una SDF (1 adentro, 0 afuera, rampa de ancho <paramref name="aa"/>).</summary>
        public static float Cover(float sdf, float aa) => Mathf.Clamp01(0.5f - sdf / aa);

        public static Sprite ToSprite(Color32[] pixels, int size, float pixelsPerUnit)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        // ------------------------------------------------------------------ colores

        public static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        /// <summary>Oscurece (k &lt; 1) o aclara multiplicando (k &gt; 1).</summary>
        public static Color Shade(Color c, float k) =>
            new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

        /// <summary>Mezcla hacia blanco (t = 0 igual, 1 blanco).</summary>
        public static Color Tint(Color c, float t) => Color.Lerp(c, Color.white, t);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // ------------------------------------------------------------------ primitivas SDF

        public static float Circle(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>
        /// Elipse. Aproximación por gradiente (Quilez): cerca del borde es casi exacta en todo el contorno, así el
        /// borde tinta tiene el mismo grosor en las puntas que en los costados (con la versión simple, en una
        /// elipse muy aplanada el borde se engordaba en las puntas).
        /// </summary>
        public static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float px = x - cx, py = y - cy;
            float k0x = px / rx, k0y = py / ry;
            float k1x = px / (rx * rx), k1y = py / (ry * ry);
            float k0 = Mathf.Sqrt(k0x * k0x + k0y * k0y);
            float k1 = Mathf.Sqrt(k1x * k1x + k1y * k1y);
            if (k1 < 1e-6f) return -Mathf.Min(rx, ry);
            return k0 * (k0 - 1f) / k1;
        }

        public static float RoundBox(float x, float y, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x - cx) - hx + r;
            float qy = Mathf.Abs(y - cy) - hy + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        public static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        /// <summary>Polígono cerrado (x0, y0, x1, y1, ...), distancia exacta.</summary>
        public static float Polygon(float x, float y, float[] pts)
        {
            int n = pts.Length / 2;
            float d = (x - pts[0]) * (x - pts[0]) + (y - pts[1]) * (y - pts[1]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float ex = pts[j * 2] - pts[i * 2], ey = pts[j * 2 + 1] - pts[i * 2 + 1];
                float wx = x - pts[i * 2], wy = y - pts[i * 2 + 1];
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
                float bx = wx - ex * t, by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = y >= pts[i * 2 + 1];
                bool c2 = y < pts[j * 2 + 1];
                bool c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        /// <summary>Corazón unitario (punta abajo en el origen, ~1.2 de ancho, ~1.1 de alto).</summary>
        public static float HeartUnit(float x, float y)
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

        /// <summary>Cápsula con radios distintos: círculo <paramref name="r1"/> en el origen, punta <paramref name="r2"/> en (0, h).</summary>
        public static float UnevenCapsule(float x, float y, float r1, float r2, float h)
        {
            float px = Mathf.Abs(x);
            float b = (r1 - r2) / h;
            float a = Mathf.Sqrt(1f - b * b);
            float k = px * -b + y * a;
            if (k < 0f) return Mathf.Sqrt(px * px + y * y) - r1;
            if (k > a * h) return Mathf.Sqrt(px * px + (y - h) * (y - h)) - r2;
            return px * a + y * b - r1;
        }

        /// <summary>
        /// Estrella regular de puntas redondeadas (sdStar de Inigo Quilez): <paramref name="points"/> puntas de radio
        /// <paramref name="r"/>; <paramref name="m"/> entre 2 (polígono) y <paramref name="points"/> (muy fina) define
        /// cuán hundidos son los valles. Una punta mira hacia arriba.
        /// </summary>
        public static float Star(float x, float y, float cx, float cy, int points, float r, float m, float round)
        {
            float px = x - cx, py = y - cy;
            float an = Mathf.PI / points;
            float en = Mathf.PI / m;
            float acx = Mathf.Cos(an), acy = Mathf.Sin(an);
            float ecx = Mathf.Cos(en), ecy = Mathf.Sin(en);
            float bn = Mathf.Repeat(Mathf.Atan2(px, py), 2f * an) - an;
            float len = Mathf.Sqrt(px * px + py * py);
            float qx = len * Mathf.Cos(bn), qy = len * Mathf.Abs(Mathf.Sin(bn));
            float rr = r - round;
            qx -= rr * acx;
            qy -= rr * acy;
            float h = Mathf.Clamp(-(qx * ecx + qy * ecy), 0f, rr * acy / ecy);
            qx += ecx * h;
            qy += ecy * h;
            return Mathf.Sqrt(qx * qx + qy * qy) * Mathf.Sign(qx) - round;
        }

        /// <summary>
        /// Media luna (sdMoon de Quilez, distancia exacta): círculo de radio <paramref name="ra"/> en el origen
        /// menos otro de radio <paramref name="rb"/> corrido <paramref name="d"/> sobre el eje x.
        /// </summary>
        public static float Crescent(float x, float y, float d, float ra, float rb)
        {
            y = Mathf.Abs(y);
            float a = (ra * ra - rb * rb + d * d) / (2f * d);
            float b = Mathf.Sqrt(Mathf.Max(ra * ra - a * a, 0f));
            if (d * (x * b - y * a) > d * d * Mathf.Max(b - y, 0f))
                return Mathf.Sqrt((x - a) * (x - a) + (y - b) * (y - b));
            return Mathf.Max(Mathf.Sqrt(x * x + y * y) - ra, -(Mathf.Sqrt((x - d) * (x - d) + y * y) - rb));
        }

        public static float Union(float a, float b) => Mathf.Min(a, b);
        public static float Intersect(float a, float b) => Mathf.Max(a, b);
        public static float Subtract(float a, float b) => Mathf.Max(a, -b);

        /// <summary>Gira el punto (x, y) alrededor de (cx, cy) en <paramref name="radians"/> (sentido antihorario).</summary>
        public static void Rotate(float x, float y, float cx, float cy, float radians, out float rx, out float ry)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            float dx = x - cx, dy = y - cy;
            rx = dx * c - dy * s;
            ry = dx * s + dy * c;
        }
    }
}
