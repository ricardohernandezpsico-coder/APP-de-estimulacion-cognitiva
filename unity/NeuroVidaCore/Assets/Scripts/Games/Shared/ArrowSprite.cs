using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>Flecha blanca procedural apuntando ARRIBA (se gira con la rotación del
    /// RectTransform para las otras direcciones). Forma redondeada, con antialiasing por
    /// distancia con signo, sin assets. Se tiñe con <c>Image.color</c>.</summary>
    public static class ArrowSprite
    {
        private const int SizePx = 192;
        private static Sprite _cached;

        private static readonly Vector2[] Poly =
        {
            new Vector2(0f, 0.86f),
            new Vector2(0.62f, 0.10f),
            new Vector2(0.26f, 0.10f),
            new Vector2(0.26f, -0.80f),
            new Vector2(-0.26f, -0.80f),
            new Vector2(-0.26f, 0.10f),
            new Vector2(-0.62f, 0.10f),
        };

        public static Sprite Get()
        {
            if (_cached != null) return _cached;
            var px = new Color32[SizePx * SizePx];
            const float round = 0.06f;
            const float aa = 0.02f;
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float nx = ((x + 0.5f) / SizePx * 2f - 1f);
                    float ny = ((y + 0.5f) / SizePx * 2f - 1f);
                    float sd = PolygonSdf(nx, ny) - round; // dilatar el polígono redondea las puntas
                    float alpha = Mathf.Clamp01(0.5f - sd / aa);
                    px[y * SizePx + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            var tex = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(px);
            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), SizePx);
            return _cached;
        }

        // Distancia con signo al polígono (negativa adentro).
        private static float PolygonSdf(float px, float py)
        {
            float d = float.MaxValue;
            bool inside = false;
            int n = Poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 a = Poly[i], b = Poly[j];
                Vector2 e = b - a;
                Vector2 w = new Vector2(px, py) - a;
                float t = Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                float dist = (w - e * t).magnitude;
                d = Mathf.Min(d, dist);
                if (((a.y > py) != (b.y > py)) && (px < (b.x - a.x) * (py - a.y) / (b.y - a.y) + a.x))
                    inside = !inside;
            }
            return inside ? -d : d;
        }
    }
}
