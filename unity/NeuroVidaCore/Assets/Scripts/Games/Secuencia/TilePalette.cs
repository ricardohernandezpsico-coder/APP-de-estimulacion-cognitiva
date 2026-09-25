using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Color/tono de cada ficha por ÍNDICE (la grilla puede tener 4, 6, 9, 12 o 16
    /// fichas según el nivel, ver <see cref="SequenceLevelDatabase"/>).
    ///
    /// Colores: paleta curada de 16 tonos vívidos y bien separados entre sí (las primeras
    /// cuatro fichas son azul / rojo / ámbar / verde, el clásico de los juegos de secuencia),
    /// en vez del degradé de matiz por índice de la versión anterior, que dejaba fichas
    /// vecinas parecidas y bastante apagadas. En reposo la ficha se ve un poco atenuada y
    /// al iluminarse pasa a un tono claro casi blanco -- el contraste "apagada/encendida"
    /// es lo que hace que la secuencia se lea de un vistazo.
    ///
    /// Tonos: escala pentatónica de Do Mayor (Do-Re-Mi-Sol-La) repetida en octavas
    /// sucesivas a partir de Do5 (523.25Hz) -- generaliza el mapeo Do5/Mi5/Sol5/Do6 de
    /// la primera versión (pensada para exactamente 4 fichas) a cualquier cantidad.
    /// </summary>
    public readonly struct TileInfo
    {
        public readonly float ToneHz;
        public readonly Color BaseColor;
        public readonly Color NormalColor;
        public readonly Color LightColor;

        public TileInfo(float toneHz, Color baseColor, Color normalColor, Color lightColor)
        {
            ToneHz = toneHz;
            BaseColor = baseColor;
            NormalColor = normalColor;
            LightColor = lightColor;
        }
    }

    public static class TilePalette
    {
        private const float BaseHz = 523.25f; // Do5
        // Semitonos de Do-Re-Mi-Sol-La respecto de Do, repetidos +12 (una octava) por vuelta.
        private static readonly int[] PentatonicSemitones = { 0, 2, 4, 7, 9 };

        private static readonly int[] Colors =
        {
            0x3B82F6, // azul
            0xEF4444, // rojo
            0xF59E0B, // ámbar
            0x22C55E, // verde
            0xA855F7, // violeta
            0x06B6D4, // cian
            0xEC4899, // rosa
            0x84CC16, // lima
            0xF97316, // naranja
            0x6366F1, // índigo
            0x14B8A6, // turquesa
            0xFB7185, // coral
            0x0EA5E9, // celeste
            0xEAB308, // amarillo
            0xD946EF, // fucsia
            0x10B981, // esmeralda
        };

        public static TileInfo Get(int index)
        {
            int octave = index / PentatonicSemitones.Length;
            int semitone = PentatonicSemitones[index % PentatonicSemitones.Length] + octave * 12;
            float hz = BaseHz * Mathf.Pow(2f, semitone / 12f);

            int rgb = Colors[index % Colors.Length];
            var baseColor = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
            var normal = new Color(baseColor.r * 0.94f, baseColor.g * 0.94f, baseColor.b * 0.94f, 1f);
            var light = Color.Lerp(baseColor, Color.white, 0.55f);

            return new TileInfo(hz, baseColor, normal, light);
        }
    }
}
