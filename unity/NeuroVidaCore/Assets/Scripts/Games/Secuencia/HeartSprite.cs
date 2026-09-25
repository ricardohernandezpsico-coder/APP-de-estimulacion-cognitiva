using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Corazones de las vidas del HUD (Secuencia Lumínica, Parejas Ocultas y Ruta del Tesoro), en arcilla como
    /// el resto de la app: relleno plano, borde tinta grueso, sombra dura hacia abajo y brillo nítido. Dos
    /// sprites con los colores horneados (<c>Image.color</c> queda en blanco):
    ///
    /// - <see cref="GetFull"/>: corazón coral rosado.
    /// - <see cref="GetLost"/>: corazón apagado (azul noche) con una grieta: se lee como "vida perdida" sin
    ///   depender solo del color.
    ///
    /// Historia: carácter "❤" en un Text (no se veía) → corazón plano tintado → sticker con degradé y sombra
    /// difusa → arcilla (25-sep).
    /// </summary>
    public static class HeartSprite
    {
        private const int SizePx = 128;
        private const float Zoom = 1.12f;
        private const float Aa = 0.035f;
        private const float Line = 0.09f;
        private const float Drop = 0.10f;
        private static readonly Color FullColor = Hex(0xFF5C6C);
        private static readonly Color LostColor = Hex(0x4A5290);
        private static Sprite _full;
        private static Sprite _lost;

        public static Sprite GetFull() => _full != null ? _full : (_full = ToSprite(Render(false, SizePx), SizePx, 100f));
        public static Sprite GetLost() => _lost != null ? _lost : (_lost = ToSprite(Render(true, SizePx), SizePx, 100f));

        /// <summary>Píxeles del corazón (fila 0 = abajo). Separado para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(bool lost, int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) * Zoom;
                    float y = ((py + 0.5f) / size * 2f - 1f) * Zoom;
                    pixels[py * size + px] = Paint(lost, x, y).ToColor32();
                }
            }
            return pixels;
        }

        private static float Body(float x, float y)
        {
            const float s = 1.44f;
            return HeartUnit(x / s, (y + 0.80f) / s) * s;
        }

        private static Px Paint(bool lost, float x, float y)
        {
            var p = new Px();
            float body = Body(x, y);
            p.Over(Ink, Cover(Body(x, y + Drop) - Line, Aa));
            p.Over(Ink, Cover(body - Line, Aa));
            p.Over(lost ? LostColor : FullColor, Cover(body, Aa));

            if (lost)
            {
                // Grieta en zigzag, en tinta.
                float crack = Mathf.Min(
                    Mathf.Min(Capsule(x, y, 0.06f, 0.52f, -0.14f, 0.20f, 0.045f), Capsule(x, y, -0.14f, 0.20f, 0.12f, -0.04f, 0.045f)),
                    Mathf.Min(Capsule(x, y, 0.12f, -0.04f, -0.06f, -0.30f, 0.045f), Capsule(x, y, -0.06f, -0.30f, 0.04f, -0.56f, 0.045f)));
                p.Over(Ink, Cover(crack, Aa) * Cover(body, Aa));
            }
            else
            {
                p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.40f, 0.34f, 0.16f, 0.10f), Aa) * Cover(body + 0.04f, Aa));
            }
            return p;
        }
    }
}
