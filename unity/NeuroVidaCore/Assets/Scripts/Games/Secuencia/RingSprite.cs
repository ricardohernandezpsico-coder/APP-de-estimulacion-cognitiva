using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Anillo (donut) generado por código -- mismo criterio que <see cref="RoundedRectSprite"/>
    /// / <see cref="RadialGlowSprite"/>, sin asset importado. Pensado para usarse con
    /// <c>Image.Type.Filled</c> + <c>FillMethod.Radial360</c>: al bajar <c>fillAmount</c>
    /// de 1 a 0 el anillo se "vacía" en tiempo real, como un timer -- patrón muy repetido
    /// en apps de fitness/HIIT (revisado en Dribbble antes de implementar esto, spec
    /// "sexta pasada" de countdown en <c>SequenceGameController</c>), mucho más premium
    /// que un círculo pulsante estático.
    /// </summary>
    public static class RingSprite
    {
        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            const int size = 256;
            const float outerRadius = 0.48f;
            const float innerRadius = 0.39f; // grosor del anillo
            const float edge = 0.012f; // antialiaseado del borde interior/exterior

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / size;
                    float outerAlpha = 1f - Mathf.Clamp01((dist - outerRadius) / edge);
                    float innerAlpha = Mathf.Clamp01((dist - innerRadius) / edge);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            _cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cached;
        }
    }
}
