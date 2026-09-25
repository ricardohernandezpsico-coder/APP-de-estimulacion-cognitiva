using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Sello visual "noche + arcilla" de NeuroVida en los juegos de Unity: los mismos colores que la app
    /// (<c>ui/theme/Clay.kt</c> y <c>ui/components/CosmosBackground.kt</c>) para que pasar del menú al juego no
    /// se sienta como cambiar de app. Cada juego conserva su propio mundo (mar, estanque, neón...), pero lo
    /// dibuja bajo el mismo cielo nocturno, con la misma tipografía (Fredoka) y con lo tocable en arcilla:
    /// borde grueso color tinta y sombra dura hacia abajo.
    /// </summary>
    public static class NeuroStyle
    {
        // Arcilla (Clay.kt)
        public static readonly Color Ink = Hex(0x1A1240);
        public static readonly Color InkSoft = Hex(0x6B6790);
        public static readonly Color Coral = Hex(0xFF6B4A);
        public static readonly Color Sun = Hex(0xFFC93C);
        public static readonly Color Sky = Hex(0x4CC9F0);
        public static readonly Color Grape = Hex(0xB8A4FF);
        public static readonly Color Lime = Hex(0x9BE564);
        public static readonly Color Cream = Hex(0xFFFFFF);

        // Cielo nocturno (CosmosBackground.kt): abajo -> medio -> arriba
        public static readonly Color NightBottom = Hex(0x04061C);
        public static readonly Color NightMid = Hex(0x080E3A);
        public static readonly Color NightTop = Hex(0x101A58);
        public static readonly Color StarWarm = Hex(0xFFC38A);

        public static Color Hex(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>
        /// Texto "de arcilla": contorno tinta grueso y sombra dura tinta hacia abajo (sin desenfoque), como los
        /// títulos y botones de la app. <paramref name="outline"/> y <paramref name="drop"/> en unidades del canvas.
        /// </summary>
        public static void ClayText(Text text, float outline, float drop)
        {
            var go = text.gameObject;
            // La sombra va primero: los efectos de Unity se aplican en orden y así la sombra también lleva contorno.
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = Ink;
            shadow.effectDistance = new Vector2(0f, -drop);
            shadow.useGraphicAlpha = true;
            var o = go.AddComponent<Outline>();
            o.effectColor = Ink;
            o.effectDistance = new Vector2(outline, -outline);
            o.useGraphicAlpha = true;
            // Un segundo contorno en la diagonal opuesta cierra los huecos de Outline en trazos gruesos.
            var o2 = go.AddComponent<Outline>();
            o2.effectColor = Ink;
            o2.effectDistance = new Vector2(outline, outline);
            o2.useGraphicAlpha = true;
        }

        private static Sprite _night;

        /// <summary>Degradé vertical del cielo nocturno de la app (NightBottom -> NightMid -> NightTop).</summary>
        public static Sprite NightGradient()
        {
            if (_night != null) return _night;
            const int h = 256;
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1); // 0 abajo, 1 arriba
                Color c = t < 0.5f ? Color.Lerp(NightBottom, NightMid, t * 2f) : Color.Lerp(NightMid, NightTop, (t - 0.5f) * 2f);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            _night = Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 100f);
            return _night;
        }
    }
}
