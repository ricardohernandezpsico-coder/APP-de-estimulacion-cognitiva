using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>Círculo sólido antialiaseado (blanco, se tiñe con <c>Image.color</c>).
    /// Mismo criterio de "sin assets" que <c>RingSprite</c>/<c>RoundedRectSprite</c>. Sirve
    /// para insignias, puntos de progreso y el punto de color de las píldoras.</summary>
    public static class DiscSprite
    {
        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            const int size = 128;
            const float radius = 0.485f;
            const float edge = 0.012f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f;
                    float dy = (y + 0.5f) / size - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - Mathf.Clamp01((d - radius) / edge);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cached;
        }
    }
}
