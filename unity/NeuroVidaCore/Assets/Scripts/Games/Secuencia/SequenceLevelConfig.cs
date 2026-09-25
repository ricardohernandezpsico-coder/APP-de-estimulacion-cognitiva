using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Cuarta pasada de rediseño (22-sep, spec detallado de Ricardo: "esquema extendido
    /// de progresión multinivel", 16 niveles en 5 fases). Reemplaza a <see cref="SequenceDDAEngine"/>
    /// como motor de dificultad de ESTE juego -- ese motor sigue existiendo intacto y
    /// testeado 1:1 con Kotlin (ver sus tests), pero su ajuste CONTINUO por ventana
    /// deslizante no es compatible con una tabla de niveles con nombre y parámetros
    /// fijos por peldaño. <c>SequenceGameController</c> ya no lo instancia.
    ///
    /// Cada nivel fija cuadrícula (rows x cols, no necesariamente cuadrada), longitud de
    /// secuencia (span) y velocidad de presentación (ISI) -- valores tomados literal del
    /// esquema que compartió Ricardo. Los niveles 17+ (no definidos explícitamente en el
    /// spec, que dice "16+") se generan por extrapolación en <see cref="SequenceLevelDatabase.Get"/>.
    /// </summary>
    public readonly struct SequenceLevelConfig
    {
        public readonly int LevelIndex;
        public readonly int GridRows;
        public readonly int GridCols;
        public readonly int SequenceLength;
        public readonly float IsiMs;
        public readonly bool HasAudioDistractor;
        public readonly bool HasVisualDistractor;

        public int TotalTiles => GridRows * GridCols;

        public SequenceLevelConfig(int levelIndex, int gridRows, int gridCols, int sequenceLength, float isiMs, bool hasAudioDistractor = false, bool hasVisualDistractor = false)
        {
            LevelIndex = levelIndex;
            GridRows = gridRows;
            GridCols = gridCols;
            SequenceLength = sequenceLength;
            IsiMs = isiMs;
            HasAudioDistractor = hasAudioDistractor;
            HasVisualDistractor = hasVisualDistractor;
        }
    }

    public static class SequenceLevelDatabase
    {
        /// <summary>Último nivel definido literal en la tabla del spec -- de acá para
        /// arriba, <see cref="Get"/> extrapola (ver comentario ahí).</summary>
        public const int MaxDefinedLevel = 16;

        private static readonly SequenceLevelConfig[] Levels =
        {
            // Fase 1: Calentamiento y ritmo base
            new SequenceLevelConfig(1, 2, 2, 3, 1000f),
            new SequenceLevelConfig(2, 2, 2, 3, 800f),
            new SequenceLevelConfig(3, 2, 2, 4, 900f),
            // Fase 2: Consolidación y expansión del campo visual
            new SequenceLevelConfig(4, 2, 3, 4, 800f),
            new SequenceLevelConfig(5, 2, 3, 5, 850f),
            new SequenceLevelConfig(6, 2, 3, 5, 700f),
            // Fase 3: Desafío visoespacial intermedio
            new SequenceLevelConfig(7, 3, 3, 5, 750f),
            new SequenceLevelConfig(8, 3, 3, 6, 800f),
            new SequenceLevelConfig(9, 3, 3, 6, 650f),
            // Fase 4: Carga alta de memoria de trabajo
            new SequenceLevelConfig(10, 3, 4, 6, 700f),
            new SequenceLevelConfig(11, 3, 4, 7, 650f),
            new SequenceLevelConfig(12, 3, 4, 7, 550f, hasAudioDistractor: true), // tono de fondo
            // Fase 5: Nivel experto / gran maestro
            new SequenceLevelConfig(13, 4, 4, 7, 500f, hasAudioDistractor: true), // tono de fondo
            new SequenceLevelConfig(14, 4, 4, 8, 450f, hasAudioDistractor: true), // interferencia auditiva
            new SequenceLevelConfig(15, 4, 4, 8, 400f, hasVisualDistractor: true), // destello ambiental
            new SequenceLevelConfig(16, 4, 4, 9, 350f, hasAudioDistractor: true, hasVisualDistractor: true), // múltiples distractores
        };

        private const int ExtrapolatedIsiFloorMs = 350; // "velocidad límite de procesamiento perceptivo" (spec)
        private const int ExtrapolatedMaxSequenceLength = 16; // techo propio (no pedido), evita secuencias absurdas

        /// <summary>Nivel 1-16 literal de la tabla; 17+ extrapola manteniendo la 4x4 y el
        /// piso de ISI del nivel 16, sumando 1 luz cada 2 niveles (mismo espíritu que
        /// "Nivel 16+: Secuencia de 9+ luces" del spec, que no define una tabla exacta
        /// más allá de ahí).</summary>
        public static SequenceLevelConfig Get(int levelIndex)
        {
            if (levelIndex <= MaxDefinedLevel) return Levels[Mathf.Clamp(levelIndex, 1, MaxDefinedLevel) - 1];

            int stepsBeyond = levelIndex - MaxDefinedLevel;
            int sequenceLength = Mathf.Min(9 + stepsBeyond / 2, ExtrapolatedMaxSequenceLength);
            return new SequenceLevelConfig(levelIndex, 4, 4, sequenceLength, ExtrapolatedIsiFloorMs, hasAudioDistractor: true, hasVisualDistractor: true);
        }
    }
}
