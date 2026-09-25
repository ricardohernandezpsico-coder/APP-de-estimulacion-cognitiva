using System.Collections.Generic;
using UnityEngine;
using static NeuroVida.Games.Shared.ClayRaster;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Cartas de Parejas Ocultas en arcilla, con el sello de la app: cara de color plano, borde tinta grueso,
    /// sombra dura tinta hacia abajo (el "grosor" de la ficha) y brillo nítido.
    ///
    /// - <see cref="Face.Back"/>: uva, con un panel hundido y un destello crema de 4 puntas (el cielo de la app).
    /// - <see cref="Face.Front"/>: crema, sin adornos: el ícono es lo único que se mira.
    /// - <see cref="Face.Matched"/>: lima claro con un sello ✓ en la esquina (resuelta; no depende solo del color).
    ///
    /// La textura ya trae los colores: el <c>Image.color</c> normal es blanco (el tinte se usa solo para
    /// efectos puntuales, como el destello rojo de un error). Misma huella que la versión anterior
    /// (<see cref="ShapeScale"/>): el tablero no cambia de tamaño.
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
        private const float ShapeScale = 0.86f;   // la ficha ocupa este tanto del sprite
        private const float Aa = 0.018f;
        private const float Line = 0.07f;         // borde tinta (coordenadas de la ficha)
        private const float Drop = 0.12f;         // sombra dura
        private const float FaceY = 0.07f, HalfW = 0.90f, HalfH = 0.83f, Corner = 0.28f;

        private static readonly Color BackColor = Grape;
        private static readonly Color FrontColor = Cream;
        private static readonly Color MatchedColor = Hex(0xE2F8C8);

        private static readonly Dictionary<Face, Sprite> Cache = new Dictionary<Face, Sprite>();

        public static Sprite Get(Face face)
        {
            if (Cache.TryGetValue(face, out var cached) && cached != null) return cached;
            var sprite = ToSprite(Render(face, SizePx), SizePx, SizePx);
            Cache[face] = sprite;
            return sprite;
        }

        /// <summary>Píxeles de la carta (fila 0 = abajo). Separado de <see cref="Get"/> para previsualizar fuera de Unity.</summary>
        public static Color32[] Render(Face face, int size)
        {
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float x = ((px + 0.5f) / size * 2f - 1f) / ShapeScale;
                    float y = ((py + 0.5f) / size * 2f - 1f) / ShapeScale;
                    pixels[py * size + px] = PaintCard(face, x, y).ToColor32();
                }
            }
            return pixels;
        }

        private static Px PaintCard(Face face, float x, float y)
        {
            var p = new Px();
            float card = RoundBox(x, y, 0f, FaceY, HalfW, HalfH, Corner);
            float shadow = RoundBox(x, y, 0f, FaceY - Drop, HalfW, HalfH, Corner);
            p.Over(Ink, Cover(shadow - Line, Aa));
            p.Over(Ink, Cover(card - Line, Aa));

            Color fill = face == Face.Back ? BackColor : face == Face.Matched ? MatchedColor : FrontColor;
            p.Over(fill, Cover(card, Aa));

            if (face == Face.Back) BackArt(ref p, x, y);

            // Brillo de arcilla arriba a la izquierda.
            float gloss = Ellipse(x, y, -0.46f, FaceY + 0.62f, 0.26f, 0.07f);
            p.Over(new Color(1f, 1f, 1f, face == Face.Back ? 0.4f : 0.7f), Cover(gloss, Aa) * Cover(card + 0.04f, Aa));

            if (face == Face.Matched) CheckBadge(ref p, x, y);
            return p;
        }

        private static void BackArt(ref Px p, float x, float y)
        {
            // Panel hundido: más oscuro, con un canto de tinta suave arriba (la sombra interior del hueco).
            float panel = RoundBox(x, y, 0f, FaceY - 0.02f, 0.64f, 0.58f, 0.16f);
            float panelDown = RoundBox(x, y, 0f, FaceY - 0.07f, 0.64f, 0.58f, 0.16f);
            p.Over(Shade(BackColor, 0.84f), Cover(panel, Aa));
            p.Over(WithAlpha(Ink, 0.3f), Cover(Mathf.Max(panel, -panelDown), Aa));

            // Puntos de estrellas lejanas.
            float dots = Mathf.Min(
                Mathf.Min(Circle(x, y, -0.42f, 0.44f, 0.03f), Circle(x, y, 0.44f, -0.30f, 0.03f)),
                Mathf.Min(Circle(x, y, 0.38f, 0.50f, 0.022f), Circle(x, y, -0.36f, -0.38f, 0.022f)));
            p.Over(WithAlpha(Cream, 0.8f), Cover(dots, Aa));

            // Destello central de arcilla: sombra dura, borde tinta y relleno crema; uno chico al lado.
            float cy = FaceY + 0.02f;
            float big = Star(x, y, 0f, cy, 4, 0.40f, 2.35f, 0.03f);
            float bigShadow = Star(x, y + 0.05f, 0f, cy, 4, 0.40f, 2.35f, 0.03f);
            p.Over(Ink, Cover(bigShadow - 0.05f, Aa));
            p.Over(Ink, Cover(big - 0.05f, Aa));
            p.Over(Cream, Cover(big, Aa));
            float small = Star(x, y, 0.36f, cy + 0.30f, 4, 0.15f, 2.35f, 0.01f);
            p.Over(Ink, Cover(small - 0.04f, Aa));
            p.Over(Sun, Cover(small, Aa));
        }

        private static void CheckBadge(ref Px p, float x, float y)
        {
            const float bx = 0.70f, by = FaceY + 0.62f, r = 0.19f;
            p.Over(Ink, Cover(Circle(x, y + 0.05f, bx, by, r) - 0.05f, Aa));
            p.Over(Ink, Cover(Circle(x, y, bx, by, r) - 0.05f, Aa));
            p.Over(Lime, Cover(Circle(x, y, bx, by, r), Aa));
            float check = Mathf.Min(
                Capsule(x, y, bx - 0.09f, by + 0.0f, bx - 0.025f, by - 0.07f, 0.035f),
                Capsule(x, y, bx - 0.025f, by - 0.07f, bx + 0.10f, by + 0.08f, 0.035f));
            p.Over(Ink, Cover(check, Aa));
        }
    }
}
