using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Franja de la superficie lunar del mundo de Ruta del Tesoro (se estira a lo ancho, abajo): arco de una luna
    /// enorme en arcilla (borde tinta, relleno plano apagado hacia la noche, canto de luz) con cráteres achatados
    /// por la perspectiva. Se dibuja en "unidades de canvas" de una franja de 1080 x 326 (15% + 2% de 1920).
    /// </summary>
    public static class LunarSurfaceSprite
    {
        public const int Width = 540, Height = 163;
        private static readonly Color NightMid = ClayRaster.Hex(0x080E3A);
        private static Sprite _cached;

        public static Sprite Get(Color color)
        {
            if (_cached != null) return _cached;
            var pixels = Render(color);
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pixels);
            tex.Apply();
            return _cached = Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Píxeles de la franja (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(Color color)
        {
            const float uw = 1080f, uh = 326f, top = 290f, radius = 1700f, aa = 2.4f;
            var fill = Color.Lerp(NightMid, color, 0.55f);
            var crater = ClayRaster.Shade(fill, 0.78f);
            var craterRim = ClayRaster.Tint(fill, 0.18f);
            var craters = new[]
            {
                new Vector4(190f, 170f, 74f, 17f), new Vector4(770f, 215f, 52f, 12f), new Vector4(930f, 90f, 96f, 22f),
                new Vector4(390f, 70f, 62f, 15f), new Vector4(620f, 130f, 34f, 8f), new Vector4(80f, 60f, 40f, 10f),
            };
            var pixels = new Color32[Width * Height];
            for (int py = 0; py < Height; py++)
            {
                for (int px = 0; px < Width; px++)
                {
                    float x = (px + 0.5f) / Width * uw, y = (py + 0.5f) / Height * uh;
                    float body = ClayRaster.Circle(x, y, uw * 0.5f, top - radius, radius);
                    var p = new ClayRaster.Px();
                    p.Over(ClayRaster.Ink, ClayRaster.Cover(body - 9f, aa));
                    p.Over(fill, ClayRaster.Cover(body, aa));
                    // Canto de luz justo bajo el borde.
                    p.Over(ClayRaster.WithAlpha(ClayRaster.Cream, 0.22f), ClayRaster.Cover(Mathf.Max(body, -(body + 7f)), aa));
                    foreach (var c in craters)
                    {
                        float inside = ClayRaster.Cover(body + 6f, aa);
                        p.Over(craterRim, ClayRaster.Cover(ClayRaster.Ellipse(x, y, c.x, c.y - c.w * 0.3f, c.z, c.w), aa) * inside);
                        p.Over(crater, ClayRaster.Cover(ClayRaster.Ellipse(x, y, c.x, c.y, c.z, c.w), aa) * inside);
                    }
                    pixels[py * Width + px] = p.ToColor32();
                }
            }
            return pixels;
        }
    }
}
