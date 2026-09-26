using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Series
{
    /// <summary>
    /// Lupa de detective en arcilla (aro sol, vidrio celeste con brillo, mango coral): la insignia de Detective
    /// de Series. Va sobre la ficha "?" (lo que hay que descubrir). Colores horneados: <c>Image.color</c> en blanco.
    /// </summary>
    public static class MagnifierSprite
    {
        private const int SizePx = 160;
        private const float Zoom = 1.12f;
        private const float Aa = 0.03f;
        private const float Line = 0.085f;
        private const float Drop = 0.09f;
        private static Sprite _cached;

        public static Sprite Get() => _cached != null ? _cached : (_cached = ToSprite(Render(SizePx), SizePx, SizePx));

        private static float Lens(float x, float y) => Circle(x, y, -0.18f, 0.18f, 0.56f);
        private static float Handle(float x, float y) => Capsule(x, y, 0.26f, -0.26f, 0.70f, -0.70f, 0.14f);
        private static float Body(float x, float y) => Mathf.Min(Lens(x, y), Handle(x, y));

        /// <summary>Píxeles de la lupa (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(int size) => RenderClay(size, Zoom, Line, Drop, Aa, Body, (ref Px p, float x, float y) =>
        {
            float handle = Handle(x, y);
            p.Over(Coral, Cover(handle, Aa));
            float lens = Lens(x, y);
            p.Over(Ink, Cover(lens - Line, Aa));
            p.Over(Sun, Cover(lens, Aa));
            float glass = Circle(x, y, -0.18f, 0.18f, 0.38f);
            p.Over(Ink, Cover(glass, Aa));
            p.Over(Tint(Sky, 0.35f), Cover(glass + 0.06f, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.8f), Cover(Ellipse(x, y, -0.32f, 0.34f, 0.12f, 0.07f), Aa) * Cover(glass + 0.06f, Aa));
            p.Over(new Color(1f, 1f, 1f, 0.45f), Cover(Ellipse(x, y, -0.36f, 0.55f, 0.14f, 0.05f), Aa) * Cover(lens + 0.02f, Aa) * Cover(-(glass - 0.02f), Aa));
        });
    }
}
