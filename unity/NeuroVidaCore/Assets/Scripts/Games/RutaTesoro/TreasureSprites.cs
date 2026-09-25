using System.Collections.Generic;
using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.RutaTesoro
{
    /// <summary>
    /// Tesoros de playa (estrella de mar, concha, perla) en dos variantes de color cada uno, en arcilla como el
    /// resto de la app: relleno plano de la paleta, borde tinta grueso, sombra dura hacia abajo y brillo nítido
    /// (antes: degradé con contorno del mismo color, estilo sticker). Siguen siendo objetos de playa, no gemas
    /// doradas (parecían tragamonedas). Sin assets; colores horneados (<c>Image.color</c> en blanco).
    /// </summary>
    public static class TreasureSprites
    {
        public const int KindCount = 3;
        private const int SizePx = 192;
        private const float Zoom = 1.12f;
        private const float Aa = 0.022f;
        private const float Line = 0.075f;
        private const float Drop = 0.09f;
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
            var sprite = ToSprite(Render(kind, variant, SizePx), SizePx, SizePx);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Píxeles del tesoro (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int kind, int variant, int size)
        {
            var px = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int pxi = 0; pxi < size; pxi++)
                {
                    float x = ((pxi + 0.5f) / size * 2f - 1f) * Zoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * Zoom;
                    var p = new Px();
                    p.Over(Ink, Cover(Body(kind, x, y + Drop) - Line, Aa));      // sombra dura
                    p.Over(Ink, Cover(Body(kind, x, y) - Line, Aa));             // borde tinta
                    switch (kind)
                    {
                        case 0: Starfish(ref p, variant, x, y); break;
                        case 1: Shell(ref p, variant, x, y); break;
                        default: Pearl(ref p, variant, x, y); break;
                    }
                    px[py * size + pxi] = p.ToColor32();
                }
            }
            return px;
        }

        /// <summary>Silueta de cada tesoro (para el borde y la sombra).</summary>
        private static float Body(int kind, float x, float y)
        {
            switch (kind)
            {
                case 0: return StarfishSdf(x, y);
                case 1: return ShellSdf(x, y);
                default: return Circle(x, y, 0f, 0.02f, 0.60f);
            }
        }

        private static void Gloss(ref Px p, float x, float y, float cx, float cy, float rx, float ry, float body)
        {
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Ellipse(x, y, cx, cy, rx, ry), Aa) * Cover(body + 0.03f, Aa));
        }

        // ---------------------------------------------------------------- estrella de mar

        private static float StarfishSdf(float x, float y) => Star(x, y, 0f, -0.02f, 5, 0.84f, 3.4f, 0.14f);

        private static void Starfish(ref Px p, int variant, float x, float y)
        {
            Color main = variant == 0 ? Coral : Orange;
            float body = StarfishSdf(x, y);
            p.Over(main, Cover(body, Aa));
            p.Over(Tint(main, 0.22f), Cover(Star(x, y, 0f, -0.02f, 5, 0.40f, 3.4f, 0.08f), Aa));

            // Puntitos crema a lo largo de cada brazo.
            for (int arm = 0; arm < 5; arm++)
            {
                float a = Mathf.PI / 2f + arm * Mathf.PI * 2f / 5f;
                for (int d = 0; d < 3; d++)
                {
                    float r = 0.24f + d * 0.16f;
                    float dot = Circle(x, y, Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.02f, 0.05f - d * 0.009f);
                    p.Over(Cream, Cover(dot, Aa) * Cover(body + 0.04f, Aa));
                }
            }
            Gloss(ref p, x, y, -0.16f, 0.30f, 0.12f, 0.07f, body);
        }

        // ---------------------------------------------------------------- concha (vieira)

        private const float ShellCy = -0.40f;

        private static float ShellFan(float x, float y)
        {
            float dx = x, dy = y - ShellCy;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float scallop = 1.0f + 0.04f * Mathf.Cos(ang * 11f);
            float fan = (len - 0.86f * scallop) * 0.85f;
            float cut = (ShellCy - 0.06f - y) * 0.85f;
            return Mathf.Max(fan, cut);
        }

        private static float ShellSdf(float x, float y) =>
            Mathf.Min(ShellFan(x, y), RoundBox(x, y, 0f, ShellCy - 0.04f, 0.22f, 0.12f, 0.06f));

        private static void Shell(ref Px p, int variant, float x, float y)
        {
            Color main = variant == 0 ? Pink : Grape;
            float fan = ShellFan(x, y);
            float hinge = RoundBox(x, y, 0f, ShellCy - 0.04f, 0.22f, 0.12f, 0.06f);
            p.Over(Shade(main, 0.82f), Cover(hinge, Aa));
            p.Over(Ink, Cover(Mathf.Max(fan - Line * 0.8f, hinge), Aa));   // línea entre bisagra y abanico
            p.Over(main, Cover(fan, Aa));

            // Costillas en tinta que salen de la bisagra.
            float dx = x, dy = y - ShellCy;
            float ang = Mathf.Atan2(dy, dx);
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float rib = Mathf.Abs(Mathf.Repeat(ang * 5.5f / Mathf.PI + 0.5f, 1f) - 0.5f) * Mathf.PI / 5.5f * len - 0.018f;
            p.Over(WithAlpha(Ink, 0.55f), Cover(rib, Aa) * Cover(fan + 0.12f, Aa) * Mathf.Clamp01((len - 0.2f) * 8f));
            Gloss(ref p, x, y, -0.26f, 0.22f, 0.13f, 0.07f, fan);
        }

        // ---------------------------------------------------------------- perla

        private static void Pearl(ref Px p, int variant, float x, float y)
        {
            Color main = variant == 0 ? Cream : Tint(Sky, 0.55f);
            float body = Circle(x, y, 0f, 0.02f, 0.60f);
            p.Over(main, Cover(body, Aa));
            // Sombra propia abajo a la derecha (media luna), plana como en la arcilla de la app.
            p.Over(Shade(main, 0.86f), Cover(Mathf.Max(body, -Circle(x, y, -0.10f, 0.12f, 0.58f)), Aa));
            Gloss(ref p, x, y, -0.20f, 0.26f, 0.17f, 0.10f, body);
            p.Over(new Color(1f, 1f, 1f, 0.9f), Cover(Circle(x, y, 0.08f, 0.36f, 0.05f), Aa));
        }
    }
}
