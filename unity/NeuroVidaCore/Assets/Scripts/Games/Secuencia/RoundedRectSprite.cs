using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Genera un sprite de rectángulo redondeado por código (SDF de caja redondeada,
    /// antialiaseado, 9-sliced vía el parámetro <c>border</c> de <see cref="Sprite.Create"/>)
    /// para las fichas estilo Lumosity del rediseño (22-sep) -- deliberadamente SIN
    /// depender de ningún asset/sprite importado: un recurso "built-in" de Unity al que
    /// solo se accede desde código (como <c>UI/Skin/UISprite.psd</c>) no está garantizado
    /// de viajar al build de un dispositivo real, y ya hubo dos vueltas de bugs
    /// encontrados recién al probar en un dispositivo -- generar la textura en runtime
    /// evita ese riesgo por completo.
    ///
    /// El <c>border</c> 9-slice hace que las esquinas se vean iguales sea cual sea el
    /// tamaño final de la ficha (2x2 hoy, pero el layout responsivo puede agrandarlas en
    /// pantallas grandes) -- sin 9-slice, una textura estirada distorsiona el radio de
    /// esquina.
    /// </summary>
    public static class RoundedRectSprite
    {
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        /// <param name="radiusPx">Radio de esquina en píxeles de LA TEXTURA generada
        /// (no de la ficha final -- el 9-slice lo preserva al escalar). 24-30px pedido
        /// en el spec de rediseño.</param>
        public static Sprite Get(int radiusPx = 28)
        {
            if (Cache.TryGetValue(radiusPx, out var cached) && cached != null) return cached;

            int size = radiusPx * 4; // suficiente resolución para que el borde no se vea dentado
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedBoxCoverage(x + 0.5f, y + 0.5f, half, half, half, half, radiusPx);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            var border = new Vector4(radiusPx, radiusPx, radiusPx, radiusPx);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            Cache[radiusPx] = sprite;
            return sprite;
        }

        /// <summary>SDF estándar de "caja redondeada" (Inigo Quilez), con antialiasing de
        /// ~1px en el borde en vez de un corte binario duro.</summary>
        private static float RoundedBoxCoverage(float px, float py, float cx, float cy, float bx, float by, float r)
        {
            float qx = Mathf.Abs(px - cx) - (bx - r);
            float qy = Mathf.Abs(py - cy) - (by - r);
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            float dist = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            return Mathf.Clamp01(0.5f - dist);
        }
    }
}
