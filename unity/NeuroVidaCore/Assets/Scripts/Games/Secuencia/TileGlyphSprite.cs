using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Símbolo de arcilla (crema con borde tinta) que lleva cada ficha de Secuencia Lumínica en el centro: estrella,
    /// luna, planeta, destello... uno distinto por ficha (hasta 16). Le da al juego un arte propio del cielo
    /// nocturno y hace que las fichas se distingan también por forma, no solo por color (accesibilidad para
    /// daltonismo, regla de ui-ux-pro-max). Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class TileGlyphSprite
    {
        public const int Count = 16;
        private const int SizePx = 128;
        private const float Zoom = 1.2f;
        private const float Aa = 0.04f;
        private const float Line = 0.1f;
        private const float Drop = 0.09f;
        private static readonly Sprite[] Cache = new Sprite[Count];

        public static Sprite Get(int index)
        {
            index = ((index % Count) + Count) % Count;
            if (Cache[index] != null) return Cache[index];
            return Cache[index] = ToSprite(Render(index, SizePx), SizePx, SizePx);
        }

        /// <summary>Píxeles del símbolo (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int index, int size) =>
            RenderClay(size, Zoom, Line, Drop, Aa, (x, y) => Shape(index, x, y), (ref Px p, float x, float y) => Paint(index, ref p, x, y));

        private static void Paint(int index, ref Px p, float x, float y)
        {
            float body = Shape(index, x, y);
            p.Over(Cream, Cover(body, Aa));
            if (index == 2)
            {
                // Planeta: los dos cantos del anillo cruzan por delante del planeta.
                Rotate(x, y, 0f, 0f, 0.35f, out float rx, out float ry);
                float edge = Mathf.Abs(Mathf.Abs(Ellipse(rx, ry, 0f, 0f, 0.96f, 0.28f)) - 0.09f) - 0.035f;
                p.Over(Ink, Cover(Mathf.Max(edge, ry), Aa) * Cover(body, Aa));
            }
        }

        private static readonly float[] Diamond = { 0f, 0.80f, 0.56f, 0f, 0f, -0.80f, -0.56f, 0f };
        private static readonly float[] Triangle = { 0f, 0.66f, 0.70f, -0.56f, -0.70f, -0.56f };
        private static readonly float[] Bolt = { 0.14f, 0.86f, -0.46f, -0.04f, -0.04f, -0.04f, -0.18f, -0.86f, 0.46f, 0.10f, 0.04f, 0.10f };

        private static float Shape(int index, float x, float y)
        {
            switch (index)
            {
                case 0: return Star(x, y, 0f, -0.04f, 5, 0.86f, 3.0f, 0.1f);
                case 1:
                    Rotate(x + 0.08f, y, 0f, 0f, -0.4f, out float mx, out float my);
                    return Crescent(mx, my, 0.44f, 0.72f, 0.66f) - 0.05f;
                case 2:
                {
                    Rotate(x, y, 0f, 0f, 0.35f, out float rx, out float ry);
                    float ring = Mathf.Abs(Ellipse(rx, ry, 0f, 0f, 0.96f, 0.28f)) - 0.09f;
                    return Mathf.Min(Circle(x, y, 0f, 0f, 0.52f), ring);
                }
                case 3: return Star(x, y, 0f, 0f, 4, 0.88f, 2.3f, 0.05f);
                case 4: return Polygon(x, y, Diamond) - 0.06f;
                case 5: return Polygon(x, y, Triangle) - 0.1f;
                case 6:
                {
                    float r = 0.66f;
                    float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                    // Hexágono regular con puntas arriba y abajo, redondeado.
                    float d = Mathf.Max(ax * 0.866f + ay * 0.5f, ay) - r * 0.866f;
                    return d - 0.08f;
                }
                case 7:
                {
                    const float s = 1.3f;
                    return HeartUnit(x / s, (y + 0.72f) / s) * s;
                }
                case 8: return Polygon(x, y, Bolt) - 0.03f;
                case 9: return UnevenCapsule(x, y + 0.34f, 0.5f, 0.07f, 0.96f);
                case 10: return Mathf.Min(Mathf.Abs(Circle(x, y, -0.1f, -0.08f, 0.58f)) - 0.1f, Circle(x, y, 0.42f, 0.44f, 0.24f));
                case 11:
                    return Mathf.Min(Mathf.Min(Circle(x, y, 0.38f, 0f, 0.4f), Circle(x, y, -0.38f, 0f, 0.4f)),
                        Mathf.Min(Circle(x, y, 0f, 0.38f, 0.4f), Circle(x, y, 0f, -0.38f, 0.4f)));
                case 12: return Mathf.Min(RoundBox(x, y, 0f, 0f, 0.80f, 0.26f, 0.2f), RoundBox(x, y, 0f, 0f, 0.26f, 0.80f, 0.2f));
                case 13: return Mathf.Max(Mathf.Abs(Circle(x, y, 0f, -0.36f, 0.66f)) - 0.2f, -0.36f - y);
                case 14: return RoundBox(x, y, 0f, 0f, 0.62f, 0.62f, 0.2f);
                default:
                {
                    const float k = 0.70710678f;
                    float dx = x + 0.30f, dy = y + 0.30f;
                    return Mathf.Min(Circle(x, y, -0.30f, -0.30f, 0.44f), UnevenCapsule(dx * k - dy * k, dx * k + dy * k, 0.4f, 0.1f, 1.12f));
                }
            }
        }
    }
}
