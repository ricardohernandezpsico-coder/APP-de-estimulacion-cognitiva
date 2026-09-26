using System.Collections.Generic;
using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.RutaTesoro
{
    /// <summary>
    /// Tesoros ESPACIALES de Ruta del Tesoro, en arcilla como el resto de la app (relleno plano de la paleta,
    /// borde tinta grueso, sombra dura hacia abajo, brillo nítido):
    /// - Cristal estelar: tres prismas sobre una roca lunar (uva / rosa).
    /// - Estrella en órbita: estrella con un anillo inclinado que pasa por detrás (sol / coral).
    /// - Meteorito: roca con vetas que brillan (lima / celeste).
    /// Cada uno con un destello crema. Historia: gemas doradas ("parecían casino") → tesoros de playa (estrella
    /// de mar, concha, perla; Ricardo, 26-sep: "poco adecuado a la dirección espacial") → esto.
    /// Sin assets; colores horneados (<c>Image.color</c> en blanco).
    /// </summary>
    public static class TreasureSprites
    {
        public const int KindCount = 3;
        private const int SizePx = 192;
        private const float Zoom = 1.12f;
        private const float Aa = 0.022f;
        private const float Line = 0.075f;
        private const float Thin = 0.05f;
        private const float Drop = 0.09f;
        private static readonly Color Rock = Hex(0x8E86C8);
        private static readonly Color MeteorRock = Hex(0x7A74B0);
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
            switch (kind)
            {
                case 0: return RenderClay(size, Zoom, Line, Drop, Aa, CrystalBody, (ref Px p, float x, float y) => Crystal(ref p, variant, x, y));
                case 1: return RenderClay(size, Zoom, Line, Drop, Aa, OrbitStarBody, (ref Px p, float x, float y) => OrbitStar(ref p, variant, x, y));
                default: return RenderClay(size, Zoom, Line, Drop, Aa, MeteorBody, (ref Px p, float x, float y) => Meteor(ref p, variant, x, y));
            }
        }

        private static void Part(ref Px p, float sdf, Color fill, float line = Line)
        {
            p.Over(Ink, Cover(sdf - line, Aa));
            p.Over(fill, Cover(sdf, Aa));
        }

        private static void Gloss(ref Px p, float x, float y, float cx, float cy, float rx, float ry, float body)
        {
            p.Over(new Color(1f, 1f, 1f, 0.5f), Cover(Ellipse(x, y, cx, cy, rx, ry), Aa) * Cover(body + 0.03f, Aa));
        }

        private static void Sparkle(ref Px p, float x, float y, float cx, float cy, float r)
        {
            float s = Star(x, y, cx, cy, 4, r, 2.3f, 0.01f);
            p.Over(Ink, Cover(s - 0.045f, Aa));
            p.Over(Cream, Cover(s, Aa));
        }

        // ---------------------------------------------------------------- cristal estelar

        private static readonly float[] CrystalMid = { -0.19f, -0.50f, -0.19f, 0.36f, 0f, 0.74f, 0.19f, 0.36f, 0.19f, -0.50f };
        private static readonly float[] CrystalLeft = { -0.58f, -0.44f, -0.62f, 0.02f, -0.46f, 0.30f, -0.26f, 0.08f, -0.24f, -0.50f };
        private static readonly float[] CrystalRight = { 0.24f, -0.50f, 0.26f, -0.02f, 0.46f, 0.20f, 0.62f, -0.06f, 0.56f, -0.46f };

        private static float CrystalBody(float x, float y) =>
            Mathf.Min(Mathf.Min(Polygon(x, y, CrystalMid), Polygon(x, y, CrystalLeft)),
                Mathf.Min(Polygon(x, y, CrystalRight), Ellipse(x, y, 0f, -0.56f, 0.66f, 0.2f)));

        private static void Crystal(ref Px p, int variant, float x, float y)
        {
            Color main = variant == 0 ? Grape : Pink;
            // Facetas: la mitad derecha de cada prisma un poco más oscura.
            float left = Polygon(x, y, CrystalLeft);
            Part(ref p, left, Tint(main, 0.15f));
            p.Over(Shade(main, 0.86f), Cover(Mathf.Max(left, -(x + 0.43f) * 0.9f + (y + 0.1f) * 0.2f), Aa));
            float right = Polygon(x, y, CrystalRight);
            Part(ref p, right, Tint(main, 0.15f));
            p.Over(Shade(main, 0.86f), Cover(Mathf.Max(right, 0.44f - x), Aa));

            float mid = Polygon(x, y, CrystalMid);
            Part(ref p, mid, main);
            p.Over(Tint(main, 0.35f), Cover(Mathf.Max(mid, x), Aa));
            p.Over(WithAlpha(Ink, 0.5f), Cover(Mathf.Max(mid + 0.03f, Mathf.Abs(x) - 0.012f), Aa));
            Gloss(ref p, x, y, -0.10f, 0.20f, 0.04f, 0.18f, mid);

            float rock = Ellipse(x, y, 0f, -0.56f, 0.66f, 0.2f);
            Part(ref p, rock, Rock);
            p.Over(Shade(Rock, 0.8f), Cover(Ellipse(x, y, 0.30f, -0.60f, 0.09f, 0.045f), Aa));
            p.Over(Shade(Rock, 0.8f), Cover(Ellipse(x, y, -0.34f, -0.54f, 0.07f, 0.035f), Aa));
            Sparkle(ref p, x, y, 0.52f, 0.52f, 0.17f);
        }

        // ---------------------------------------------------------------- estrella en órbita

        private static float Ring(float x, float y, out float ry)
        {
            Rotate(x, y, 0f, -0.02f, 0.38f, out float rx, out ry);
            return Mathf.Abs(Ellipse(rx, ry, 0f, 0f, 0.84f, 0.24f)) - 0.07f;
        }

        private static float StarShape(float x, float y) => Star(x, y, 0f, -0.02f, 5, 0.66f, 3.0f, 0.09f);

        private static float OrbitStarBody(float x, float y) => Mathf.Min(StarShape(x, y), Ring(x, y, out _));

        private static void OrbitStar(ref Px p, int variant, float x, float y)
        {
            Color main = variant == 0 ? Sun : Coral;
            Color ringColor = variant == 0 ? Grape : Sky;
            float ring = Ring(x, y, out float ry);
            // Mitad de atrás del anillo, estrella, mitad de adelante (el corte no lleva borde).
            p.Over(Ink, Cover(Mathf.Max(ring - Line, -ry), Aa));
            p.Over(ringColor, Cover(Mathf.Max(ring, -ry), Aa));
            float star = StarShape(x, y);
            Part(ref p, star, main);
            p.Over(Tint(main, 0.3f), Cover(Star(x, y, 0f, -0.02f, 5, 0.32f, 3.0f, 0.05f), Aa));
            Gloss(ref p, x, y, -0.14f, 0.16f, 0.09f, 0.055f, star);
            p.Over(Ink, Cover(Mathf.Max(ring - Line, ry), Aa));
            p.Over(ringColor, Cover(Mathf.Max(ring, ry), Aa));
            Sparkle(ref p, x, y, -0.62f, 0.52f, 0.15f);
        }

        // ---------------------------------------------------------------- meteorito

        private static float MeteorBody(float x, float y)
        {
            float ang = Mathf.Atan2(y, x);
            float len = Mathf.Sqrt(x * x + y * y);
            float r = 0.64f + 0.06f * Mathf.Sin(3f * ang + 0.4f) + 0.04f * Mathf.Sin(5f * ang + 1.7f) + 0.02f * Mathf.Sin(8f * ang);
            return (len - r) * 0.85f;
        }

        private static void Meteor(ref Px p, int variant, float x, float y)
        {
            Color vein = variant == 0 ? Lime : Sky;
            float body = MeteorBody(x, y);
            p.Over(MeteorRock, Cover(body, Aa));
            // Vetas brillantes en zigzag: borde tinta fino, color y un centro claro.
            float veins = Mathf.Min(
                Mathf.Min(Capsule(x, y, -0.50f, 0.18f, -0.14f, 0.02f, 0.06f), Capsule(x, y, -0.14f, 0.02f, 0.08f, 0.30f, 0.06f)),
                Mathf.Min(Capsule(x, y, -0.14f, 0.02f, 0.12f, -0.30f, 0.06f), Capsule(x, y, 0.12f, -0.30f, 0.46f, -0.18f, 0.06f)));
            float inside = Cover(body + 0.05f, Aa);
            p.Over(Ink, Cover(veins - Thin, Aa) * inside);
            p.Over(vein, Cover(veins, Aa) * inside);
            p.Over(Tint(vein, 0.7f), Cover(veins + 0.035f, Aa) * inside);
            p.Over(Shade(MeteorRock, 0.8f), Cover(Circle(x, y, 0.22f, 0.36f, 0.09f), Aa));
            p.Over(Shade(MeteorRock, 0.8f), Cover(Circle(x, y, -0.28f, -0.34f, 0.07f), Aa));
            Gloss(ref p, x, y, -0.30f, 0.42f, 0.14f, 0.06f, body);
            Sparkle(ref p, x, y, 0.56f, 0.54f, 0.16f);
        }
    }
}
