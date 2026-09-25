using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Destello de estrella de 4 puntas (blanco, se tiñe con <c>Image.color</c>): núcleo brillante, dos rayos
    /// en cruz que se afinan hacia la punta y un halo tenue. Es el "brillo" del sello nocturno de NeuroVida
    /// (cuenta regresiva, celebraciones, aciertos).
    /// </summary>
    public static class SparkleSprite
    {
        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            const int size = 128;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size - 0.5f) * 2f; // -1..1
                    float dy = ((y + 0.5f) / size - 0.5f) * 2f;
                    float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    // Rayos: finos cerca del centro del eje, se afinan (y apagan) hacia la punta.
                    float taperH = Mathf.Pow(Mathf.Clamp01(1f - ax), 1.6f);
                    float taperV = Mathf.Pow(Mathf.Clamp01(1f - ay), 1.6f);
                    float rayH = taperH * Mathf.Exp(-ay * 30f / Mathf.Max(0.15f, taperH));
                    float rayV = taperV * Mathf.Exp(-ax * 30f / Mathf.Max(0.15f, taperV));

                    float core = Mathf.Exp(-r * r * 60f);
                    float halo = 0.22f * Mathf.Pow(Mathf.Clamp01(1f - r), 3f);

                    float a = Mathf.Clamp01(core + 0.95f * (rayH + rayV) + halo);
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
