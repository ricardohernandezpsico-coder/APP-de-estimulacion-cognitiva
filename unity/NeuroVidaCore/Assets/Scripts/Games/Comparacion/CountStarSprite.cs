using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Comparacion
{
    /// <summary>
    /// Estrella de arcilla para contar en Comparación (niveles de puntos): sol con borde tinta, sombra dura y
    /// brillo, en vez de un disco blanco plano. Todas iguales (el tamaño no debe dar pistas de cantidad).
    /// Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class CountStarSprite
    {
        private const int SizePx = 128;
        private const float Zoom = 1.04f;
        private const float Aa = 0.04f;
        private const float Line = 0.1f;
        private const float Drop = 0.1f;
        private static Sprite _cached;

        public static Sprite Get() => _cached != null ? _cached : (_cached = ToSprite(Render(SizePx), SizePx, SizePx));

        /// <summary>Píxeles de la estrella (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int size) => RenderClay(size, Zoom, Line, Drop, Aa, Body, (ref Px p, float x, float y) =>
        {
            float body = Body(x, y);
            p.Over(Sun, Cover(body, Aa));
            p.Over(Tint(Sun, 0.35f), Cover(Star(x, y, 0f, 0.02f, 5, 0.38f, 3.0f, 0.06f), Aa));
            p.Over(new Color(1f, 1f, 1f, 0.55f), Cover(Ellipse(x, y, -0.2f, 0.2f, 0.12f, 0.07f), Aa) * Cover(body + 0.03f, Aa));
        });

        private static float Body(float x, float y) => Star(x, y, 0f, 0.02f, 5, 0.84f, 3.0f, 0.12f);
    }
}
