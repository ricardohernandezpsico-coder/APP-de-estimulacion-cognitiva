using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.RutaTesoro
{
    /// <summary>
    /// Tesoros de playa procedurales (estrella de mar, concha, perla) en dos variantes de color
    /// cada uno. Reemplazan a las gemas doradas, que daban un aire de tragamonedas: estos objetos
    /// son suaves y familiares, con contorno, degradé y un brillo discreto. Sin assets:
    /// distancias con signo + antialiasing. Colores horneados (Image.color en blanco).
    /// </summary>
    public static class TreasureSprites
    {
        public const int KindCount = 3;
        private const int SizePx = 192;
        private const float Aa = 0.022f;
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        /// <summary>Un tesoro distinto según el índice (tipo = i % 3, variante = (i / 3) % 2).</summary>
        public static Sprite ForIndex(int index)
        {
            index = Mathf.Abs(index);
            return Get(index % KindCount, (index / KindCount) % 2);
        }

        public static Sprite Get(int kind, int variant)
        {
            int key = kind * 10 + variant;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var px = new Color32[SizePx * SizePx];
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float nx = (x + 0.5f) / SizePx * 2f - 1f;
                    float ny = (y + 0.5f) / SizePx * 2f - 1f;
                    px[y * SizePx + x] = Shade(kind, variant, nx, ny);
                }
            }
            var tex = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), SizePx);
            Cache[key] = sprite;
            return sprite;
        }

        private static float Edge(float sdf) => Mathf.Clamp01(0.5f - sdf / Aa);
        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private struct Acc
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
                    (byte)(Mathf.Clamp01(r * k) * 255f), (byte)(Mathf.Clamp01(g * k) * 255f),
                    (byte)(Mathf.Clamp01(b * k) * 255f), (byte)(Mathf.Clamp01(a) * 255f));
            }
        }

        private static Color32 Shade(int kind, int variant, float x, float y)
        {
            var acc = new Acc();
            switch (kind)
            {
                case 0: Starfish(ref acc, variant, x, y); break;
                case 1: Shell(ref acc, variant, x, y); break;
                default: Pearl(ref acc, variant, x, y); break;
            }
            return acc.ToColor32();
        }

        // Capa con contorno oscuro + relleno con degradé vertical.
        private static void Layer(ref Acc acc, float sdf, Color outline, Color top, Color bottom, float yTop, float yBottom, float y)
        {
            acc.Over(outline, Edge(sdf - 0.05f));
            float t = Mathf.Clamp01((yTop - y) / Mathf.Max(0.01f, yTop - yBottom));
            acc.Over(Color.Lerp(top, bottom, t), Edge(sdf));
        }

        // ---------------------------------------------------------------- estrella de mar

        private static void Starfish(ref Acc acc, int variant, float x, float y)
        {
            Color top = variant == 0 ? Hex(0xFDBA74) : Hex(0xFDA4AF);
            Color bottom = variant == 0 ? Hex(0xEA580C) : Hex(0xE11D48);
            Color outline = variant == 0 ? Hex(0x9A3412) : Hex(0x9F1239);
            Color dot = variant == 0 ? Hex(0xFFEDD5) : Hex(0xFFE4E6);

            float len = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Atan2(y, x) - Mathf.PI / 2f;
            float wave = 0.5f + 0.5f * Mathf.Cos(5f * ang);            // 1 en las puntas, 0 en los valles
            float radius = 0.34f + 0.46f * Mathf.Pow(wave, 1.35f);
            float sdf = (len - radius) * 0.62f;                         // Lipschitz aproximado
            Layer(ref acc, sdf, outline, top, bottom, 0.8f, -0.8f, y);

            // Puntitos claros a lo largo de cada brazo.
            for (int arm = 0; arm < 5; arm++)
            {
                float a = Mathf.PI / 2f + arm * Mathf.PI * 2f / 5f;
                for (int d = 0; d < 3; d++)
                {
                    float r = 0.22f + d * 0.16f;
                    float px = Mathf.Cos(a) * r, py = Mathf.Sin(a) * r;
                    float dd = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py)) - (0.045f - d * 0.007f);
                    acc.Over(new Color(dot.r, dot.g, dot.b, 0.9f), Edge(dd) * Edge(sdf + 0.03f));
                }
            }
            Gloss(ref acc, x, y, -0.18f, 0.32f, 0.30f, 0.14f, Edge(sdf + 0.02f));
        }

        // ---------------------------------------------------------------- concha (vieira)

        private static void Shell(ref Acc acc, int variant, float x, float y)
        {
            Color top = variant == 0 ? Hex(0xFBCFE8) : Hex(0xDDD6FE);
            Color bottom = variant == 0 ? Hex(0xF472B6) : Hex(0xA78BFA);
            Color outline = variant == 0 ? Hex(0x9D174D) : Hex(0x5B21B6);
            Color ridge = variant == 0 ? Hex(0xBE185D) : Hex(0x6D28D9);

            const float cy = -0.42f;
            float dx = x, dy = y - cy;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);                            // 0..pi en la mitad superior
            float scallop = 1.0f + 0.045f * Mathf.Cos(ang * 11f);
            float fan = (len - 0.90f * scallop) * 0.85f;                // abanico
            float cut = (cy - 0.06f - y) * 0.85f;                        // recorta el borde inferior
            float body = Mathf.Max(fan, cut);
            // Base pequeña (bisagra).
            float bx = Mathf.Abs(x) - 0.20f, by = Mathf.Abs(y - (cy - 0.03f)) - 0.09f;
            float hinge = Mathf.Sqrt(Mathf.Max(bx, 0f) * Mathf.Max(bx, 0f) + Mathf.Max(by, 0f) * Mathf.Max(by, 0f)) + Mathf.Min(Mathf.Max(bx, by), 0f) - 0.05f;
            float sdf = Mathf.Min(body, hinge);
            Layer(ref acc, sdf, outline, top, bottom, 0.6f, -0.5f, y);

            // Costillas: líneas que salen de la bisagra.
            float ribs = Mathf.Abs(Mathf.Cos(ang * 5.5f));
            float ribMask = Mathf.SmoothStep(0.84f, 0.98f, ribs) * Edge(body + 0.09f) * Mathf.Clamp01((len - 0.16f) * 6f);
            acc.Over(new Color(ridge.r, ridge.g, ridge.b, 0.55f), ribMask);
            Gloss(ref acc, x, y, -0.22f, 0.36f, 0.30f, 0.13f, Edge(body + 0.05f));
        }

        // ---------------------------------------------------------------- perla

        private static void Pearl(ref Acc acc, int variant, float x, float y)
        {
            Color light = variant == 0 ? Hex(0xFFFFFF) : Hex(0xECFEFF);
            Color mid = variant == 0 ? Hex(0xFCE7F3) : Hex(0xA5F3FC);
            Color edge = variant == 0 ? Hex(0xE9B7D0) : Hex(0x67C7DA);
            Color outline = variant == 0 ? Hex(0xA8688A) : Hex(0x2B7A90);

            float len = Mathf.Sqrt(x * x + y * y);
            float sdf = len - 0.62f;
            acc.Over(outline, Edge(sdf - 0.05f));
            // Esfera: degradé radial con la luz arriba-izquierda.
            float lx = x + 0.20f, ly = y - 0.22f;
            float t = Mathf.Clamp01(Mathf.Sqrt(lx * lx + ly * ly) / 0.95f);
            Color c = t < 0.55f ? Color.Lerp(light, mid, t / 0.55f) : Color.Lerp(mid, edge, (t - 0.55f) / 0.45f);
            acc.Over(c, Edge(sdf));
            Gloss(ref acc, x, y, -0.24f, 0.26f, 0.20f, 0.12f, Edge(sdf + 0.02f));
            // Reflejo pequeño abajo-derecha.
            float rx = x - 0.24f, ry = y + 0.24f;
            acc.Over(new Color(1f, 1f, 1f, 0.28f), Edge((Mathf.Sqrt(rx * rx + ry * ry) - 0.10f)) * Edge(sdf + 0.03f));
        }

        private static void Gloss(ref Acc acc, float x, float y, float cx, float cy, float rx, float ry, float mask)
        {
            float gx = (x - cx) / rx, gy = (y - cy) / ry;
            float d = Mathf.Sqrt(gx * gx + gy * gy);
            float a = Mathf.Clamp01(1f - d) * 0.62f;
            acc.Over(new Color(1f, 1f, 1f, a), mask);
        }
    }
}
