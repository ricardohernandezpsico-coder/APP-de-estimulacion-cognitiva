using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Sprite procedural de la ficha de Secuencia Lumínica, estilo "clay" (ficha 3D
    /// gruesa: cara con degradé, bisel con luz arriba-izquierda, brillo suave y un labio
    /// inferior más oscuro que da el grosor) -- el mismo lenguaje visual que las cartas de
    /// Parejas Ocultas (<c>CardSprites</c>). Está en ESCALA DE GRISES: <c>Image.color</c> la
    /// tiñe con el color de cada ficha (multiplica), así una sola textura sirve para todas.
    /// </summary>
    public static class TileSprites
    {
        private const int SizePx = 256;
        private const float Aa = 0.014f;
        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            var pixels = new Color32[SizePx * SizePx];
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float nx = (x + 0.5f) / SizePx * 2f - 1f;
                    float ny = (y + 0.5f) / SizePx * 2f - 1f;
                    pixels[y * SizePx + x] = Pixel(nx, ny);
                }
            }
            var tex = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), SizePx);
            return _cached;
        }

        private static float EdgeStep(float sdf) => Mathf.Clamp01(0.5f - sdf / Aa);

        private static float RoundBox(float x, float y, float cy)
        {
            const float hx = 0.93f, hy = 0.86f, r = 0.32f;
            float qx = Mathf.Abs(x) - hx + r;
            float qy = Mathf.Abs(y - cy) - hy + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        // La ficha ocupa solo ShapeScale del sprite; el margen aloja la sombra suave horneada
        // (en vez del componente Shadow de uGUI, que se veía duro/pixeleado).
        private const float ShapeScale = 0.86f;

        private static Color32 Pixel(float nx, float ny)
        {
            float sx = nx / ShapeScale, sy = ny / ShapeScale;
            Color32 shape = ShapePixel(sx, sy);
            float shadowSd = RoundBox(sx, sy + 0.16f, -0.05f) * ShapeScale;
            float sh = 0.42f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((shadowSd + 0.03f) / 0.24f)));
            float sa = shape.a / 255f;
            float outA = sa + sh * (1f - sa);
            if (outA <= 0.0001f) return new Color32(0, 0, 0, 0);
            float k = sa / outA;
            byte g = (byte)(shape.r * k);
            return new Color32(g, g, g, (byte)(outA * 255f));
        }

        private static Color32 ShapePixel(float nx, float ny)
        {
            float a = 0f, c = 0f; // gris premultiplicado + alfa

            void Over(float gray, float alpha)
            {
                if (alpha <= 0f) return;
                c = gray * alpha + c * (1f - alpha);
                a = alpha + a * (1f - alpha);
            }

            // Labio inferior (grosor de la ficha).
            float lip = RoundBox(nx, ny, -0.05f);
            Over(0.50f, EdgeStep(lip));

            // Cara.
            float face = RoundBox(nx, ny, 0.07f);
            float faceMask = EdgeStep(face);

            // Bisel: normal aproximada por diferencias finitas del SDF; luz desde arriba-izquierda.
            const float e = 0.01f;
            float gx = RoundBox(nx + e, ny, 0.07f) - RoundBox(nx - e, ny, 0.07f);
            float gy = RoundBox(nx, ny + e, 0.07f) - RoundBox(nx, ny - e, 0.07f);
            float gl = Mathf.Sqrt(gx * gx + gy * gy) + 1e-5f;
            float nxn = gx / gl, nyn = gy / gl;
            float lit = Mathf.Clamp01((-nxn * 0.6f + nyn * 0.8f) * 0.5f + 0.5f); // 1 = mira a la luz
            float bevelShade = Mathf.Lerp(0.74f, 1.0f, lit);
            Over(bevelShade, faceMask);

            // Interior de la cara (más chico que el bisel) con degradé vertical.
            const float bevelWidth = 0.11f;
            float inner = EdgeStep(face + bevelWidth);
            float t = Mathf.Clamp01((0.93f - ny) / 1.7f);
            float fill = Mathf.Lerp(1.0f, 0.90f, t);
            Over(fill, inner);

            // Brillo suave en el tercio superior.
            float gloss = Mathf.Clamp01((ny - 0.30f) / 0.55f);
            Over(1f, gloss * gloss * 0.20f * inner);

            byte g = (byte)(Mathf.Clamp01(a > 0.0001f ? c / a : 0f) * 255f);
            return new Color32(g, g, g, (byte)(Mathf.Clamp01(a) * 255f));
        }
    }
}
