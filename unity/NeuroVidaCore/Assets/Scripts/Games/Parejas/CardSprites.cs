using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Sprites procedurales de las cartas de Parejas Ocultas, estilo "clay" (ficha 3D
    /// gruesa: cara con degradé + borde claro + labio inferior más oscuro que da el
    /// grosor). Recomendado por la skill ui-ux-pro-max para juegos casuales/educativos y
    /// alineado con lo que hacen las apps de entrenamiento mental de la Play Store.
    ///
    /// - <see cref="Face.Back"/>: dorso violeta con patrón de rombos y un destello central.
    /// - <see cref="Face.Front"/>: frente casi blanco -- deja que los íconos ilustrados
    ///   resalten con máximo contraste (importante para baja visión; antes la cara
    ///   frontal era azul pizarra oscuro y los símbolos se perdían).
    /// - <see cref="Face.Matched"/>: verde menta, pareja resuelta.
    ///
    /// La textura ya trae los colores: el <c>Image.color</c> normal es blanco (se usa un
    /// tinte solo para efectos puntuales, como el destello rojo de un error).
    /// </summary>
    public static class CardSprites
    {
        public enum Face
        {
            Back,
            Front,
            Matched
        }

        private const int SizePx = 256;
        private const float Aa = 0.014f;
        private static readonly Dictionary<Face, Sprite> Cache = new Dictionary<Face, Sprite>();

        public static Sprite Get(Face face)
        {
            if (Cache.TryGetValue(face, out var cached)) return cached;

            var pixels = new Color32[SizePx * SizePx];
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float nx = (x + 0.5f) / SizePx * 2f - 1f;
                    float ny = (y + 0.5f) / SizePx * 2f - 1f;
                    pixels[y * SizePx + x] = PixelFor(face, nx, ny);
                }
            }

            var texture = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), SizePx);
            Cache[face] = sprite;
            return sprite;
        }

        private static float EdgeStep(float sdf) => Mathf.Clamp01(0.5f - sdf / Aa);

        private static float RoundBoxSdf(float x, float y, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x - cx) - hx + r;
            float qy = Mathf.Abs(y - cy) - hy + r;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        // Compositor "over" sobre RGB premultiplicado.
        private struct Acc
        {
            public float r, g, b, a;

            public void Over(Color c, float coverage)
            {
                float sa = c.a * coverage;
                if (sa <= 0f) return;
                r = c.r * sa + r * (1f - sa);
                g = c.g * sa + g * (1f - sa);
                b = c.b * sa + b * (1f - sa);
                a = sa + a * (1f - sa);
            }

            public Color32 ToColor32()
            {
                float rr = a > 0.0001f ? r / a : 0f;
                float gg = a > 0.0001f ? g / a : 0f;
                float bb = a > 0.0001f ? b / a : 0f;
                return new Color32(
                    (byte)(Mathf.Clamp01(rr) * 255f),
                    (byte)(Mathf.Clamp01(gg) * 255f),
                    (byte)(Mathf.Clamp01(bb) * 255f),
                    (byte)(Mathf.Clamp01(a) * 255f));
            }
        }

        // La ficha ocupa solo ShapeScale del sprite: el resto es margen para la sombra
        // suave horneada (reemplaza al componente Shadow de uGUI, que duplicaba la malla y
        // se veía con el borde duro/pixeleado).
        private const float ShapeScale = 0.86f;

        private static Color32 PixelFor(Face face, float nx, float ny)
        {
            float sx = nx / ShapeScale, sy = ny / ShapeScale;
            Color32 shape = ShapePixel(face, sx, sy);
            float shadowSd = RoundBoxSdf(sx, sy + 0.16f, 0f, -0.05f, 0.93f, 0.86f, 0.30f) * ShapeScale;
            float sh = 0.40f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((shadowSd + 0.03f) / 0.24f)));
            float sa = shape.a / 255f;
            float outA = sa + sh * (1f - sa);
            if (outA <= 0.0001f) return new Color32(0, 0, 0, 0);
            float k = sa / outA; // el sombreado es negro: solo aporta alfa
            return new Color32((byte)(shape.r * k), (byte)(shape.g * k), (byte)(shape.b * k), (byte)(outA * 255f));
        }

        private static Color32 ShapePixel(Face face, float nx, float ny)
        {
            Color faceTop, faceBottom, border, lip;
            switch (face)
            {
                case Face.Back:
                    faceTop = Hex(0x9A8CFF);
                    faceBottom = Hex(0x5B45E0);
                    border = Hex(0xDCD4FF);
                    lip = Hex(0x33208F);
                    break;
                case Face.Matched:
                    faceTop = Hex(0xEAFFF1);
                    faceBottom = Hex(0x8CEDAE);
                    border = Hex(0x22C55E);
                    lip = Hex(0x15803D);
                    break;
                default:
                    faceTop = Hex(0xFFFFFF);
                    faceBottom = Hex(0xEDE8FB);
                    border = Hex(0xC7BAF3);
                    lip = Hex(0x9A8DD4);
                    break;
            }

            var acc = new Acc();

            // Labio inferior (el "grosor" de la ficha), desplazado hacia abajo.
            float lipSdf = RoundBoxSdf(nx, ny, 0f, -0.05f, 0.93f, 0.86f, 0.30f);
            acc.Over(lip, EdgeStep(lipSdf));

            // Cara: borde claro + interior con degradé vertical.
            float faceSdf = RoundBoxSdf(nx, ny, 0f, 0.07f, 0.93f, 0.86f, 0.30f);
            acc.Over(border, EdgeStep(faceSdf));

            const float borderWidth = 0.06f;
            float innerSdf = faceSdf + borderWidth;
            float t = Mathf.Clamp01((0.93f - ny) / 1.7f);
            var fill = Color.Lerp(faceTop, faceBottom, t);
            float innerMask = EdgeStep(innerSdf);
            acc.Over(fill, innerMask);

            // Brillo suave en el tercio superior de la cara.
            float gloss = Mathf.Clamp01((ny - 0.38f) / 0.5f);
            acc.Over(new Color(1f, 1f, 1f, 0.22f), gloss * innerMask);

            if (face == Face.Back)
            {
                // Patrón de rombos sutil.
                float u = (nx + ny) * 2.2f;
                float v = (nx - ny) * 2.2f;
                float lineU = Mathf.Abs(u - Mathf.Floor(u) - 0.5f);
                float lineV = Mathf.Abs(v - Mathf.Floor(v) - 0.5f);
                float line = Mathf.Min(lineU, lineV);
                float lineAlpha = Mathf.Clamp01(1f - line / 0.03f) * 0.10f;
                acc.Over(Color.white, lineAlpha * innerMask);

                // Destello central: halo + estrella de 4 puntas.
                float cx = nx;
                float cy = ny - 0.07f;
                float radial = Mathf.Sqrt(cx * cx + cy * cy);
                float halo = Mathf.Clamp01(1f - radial / 0.55f);
                acc.Over(Color.white, halo * halo * 0.22f * innerMask);

                float dx = Mathf.Abs(cx) / 0.42f;
                float dy = Mathf.Abs(cy) / 0.42f;
                float star = Mathf.Pow(dx, 0.55f) + Mathf.Pow(dy, 0.55f) - 1f;
                acc.Over(new Color(1f, 1f, 1f, 0.95f), EdgeStep(star * 0.30f) * innerMask);
            }

            return acc.ToColor32();
        }
    }
}
