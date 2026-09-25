using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Sprite procedural de las fichas y botones de TODOS los juegos, con el sello "noche + arcilla" de la app
    /// (rediseño 25-sep; antes era una ficha gris con bisel, labio y sombra difusa): cara con brillo suave
    /// arriba, borde oscuro grueso y sombra dura sólida debajo, igual que los botones de arcilla de la app
    /// (<c>ui/theme/Clay.kt</c>). Está en ESCALA DE GRISES: <c>Image.color</c> la tiñe con el color de cada
    /// ficha (multiplica), así una sola textura sirve para todas; el borde y la sombra son negros, así que
    /// quedan oscuros sea cual sea el color.
    ///
    /// <see cref="GetPressed"/> es la misma ficha "hundida" (la cara baja y la sombra casi desaparece): la usa
    /// <c>PressScale</c> al presionar, así el toque se siente físico sin cambiar el espacio que ocupa el botón.
    /// Ocupa la misma huella que la versión anterior (<see cref="ShapeScale"/>), para no mover ningún layout.
    /// </summary>
    public static class TileSprites
    {
        private const int SizePx = 256;
        private const float Aa = 0.014f;

        // La ficha ocupa solo ShapeScale del sprite; el margen aloja el borde y la sombra dura.
        private const float ShapeScale = 0.86f;

        private const float FaceY = 0.06f;        // centro vertical de la cara (en reposo)
        private const float Border = 0.075f;      // grosor del borde oscuro
        private const float Depth = 0.14f;        // cuánto asoma la sombra dura debajo
        private const float PressTravel = 0.10f;  // cuánto baja la cara al presionar

        private static Sprite _cached;
        private static Sprite _pressed;

        public static Sprite Get() => _cached != null ? _cached : (_cached = Build(0f));

        /// <summary>La misma ficha presionada (cara hundida hacia su sombra).</summary>
        public static Sprite GetPressed() => _pressed != null ? _pressed : (_pressed = Build(PressTravel));

        private static Sprite Build(float press)
        {
            var pixels = new Color32[SizePx * SizePx];
            for (int y = 0; y < SizePx; y++)
            {
                for (int x = 0; x < SizePx; x++)
                {
                    float nx = (x + 0.5f) / SizePx * 2f - 1f;
                    float ny = (y + 0.5f) / SizePx * 2f - 1f;
                    pixels[y * SizePx + x] = Pixel(nx / ShapeScale, ny / ShapeScale, press);
                }
            }
            var tex = new Texture2D(SizePx, SizePx, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SizePx, SizePx), new Vector2(0.5f, 0.5f), SizePx);
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

        private static Color32 Pixel(float nx, float ny, float press)
        {
            float a = 0f, c = 0f; // gris premultiplicado + alfa

            void Over(float gray, float alpha)
            {
                if (alpha <= 0f) return;
                c = gray * alpha + c * (1f - alpha);
                a = alpha + a * (1f - alpha);
            }

            float faceY = FaceY - press;

            // Sombra dura: la silueta con borde, desplazada hacia abajo (siempre en la misma posición, así al
            // hundirse la cara "la tapa").
            float shadow = RoundBox(nx, ny, FaceY - Depth) - Border;
            Over(0f, EdgeStep(shadow));

            // Borde oscuro grueso alrededor de la cara.
            float face = RoundBox(nx, ny, faceY);
            Over(0f, EdgeStep(face - Border));

            // Cara: degradé vertical suave (arriba más luminosa) y una sombra interior en el borde de abajo, que
            // le da volumen de arcilla sin el bisel duro de antes.
            float faceMask = EdgeStep(face);
            float t = Mathf.Clamp01((0.93f - (ny - faceY)) / 1.8f);
            float fill = Mathf.Lerp(0.95f, 0.84f, t);
            float innerShade = Mathf.Clamp01((ny - faceY + 0.86f) / 0.26f); // 0 en el borde inferior
            fill *= Mathf.Lerp(0.80f, 1f, innerShade);
            Over(fill, faceMask);

            // Borde de luz interior en la mitad de arriba (el "canto" de la arcilla).
            float rim = Mathf.Clamp01(1f - Mathf.Abs(face + 0.07f) / 0.05f) * Mathf.Clamp01((ny - faceY) / 0.5f);
            Over(1f, rim * 0.45f * faceMask);

            // Brillo de arcilla: una cápsula clara y difusa arriba a la izquierda.
            float gx = (nx + 0.30f) / 0.46f, gy = (ny - faceY - 0.50f) / 0.17f;
            float gloss = Mathf.Clamp01(1f - (gx * gx + gy * gy));
            Over(1f, Mathf.Sqrt(gloss) * 0.42f * faceMask);

            byte g = (byte)(Mathf.Clamp01(a > 0.0001f ? c / a : 0f) * 255f);
            return new Color32(g, g, g, (byte)(Mathf.Clamp01(a) * 255f));
        }
    }
}
