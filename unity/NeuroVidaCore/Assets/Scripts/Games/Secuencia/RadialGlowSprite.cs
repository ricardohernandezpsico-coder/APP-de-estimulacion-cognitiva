using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Sprite de un blob radial suave (alpha 1 en el centro, degradé a 0 en el borde),
    /// generado por código -- mismo criterio que <see cref="RoundedRectSprite"/> (sin
    /// assets nuevos). Usado para los distractores de fondo (ver
    /// <c>SequenceGameController.RegenerateDistractors</c>), paridad con
    /// <c>DistractorGlow</c> en <c>SecuenciaGame.kt</c> (Kotlin) -- blobs pasivos
    /// detrás de la grilla que crecen en cantidad con la dificultad
    /// (<c>SequenceDDAEngine.DifficultyProfile.DistractorCount</c>), nunca elementos a
    /// identificar o tocar.
    /// </summary>
    public static class RadialGlowSprite
    {
        private static Sprite _cached;

        public static Sprite Get()
        {
            if (_cached != null) return _cached;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float center = size / 2f;
            float maxDist = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - dist / maxDist);
                    alpha *= alpha; // degradé más suave hacia el borde (cuadrático en vez de lineal)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            _cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cached;
        }
    }
}
