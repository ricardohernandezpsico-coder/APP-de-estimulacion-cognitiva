using System;
using System.Collections.Generic;

namespace NeuroVida.Games.RutaTesoro
{
    public readonly struct TreasureStage
    {
        public readonly int GridSize;
        public readonly int Treasures;

        public TreasureStage(int gridSize, int treasures)
        {
            GridSize = gridSize;
            Treasures = treasures;
        }

        public int Cells => GridSize * GridSize;
    }

    /// <summary>
    /// Reglas puras de "Ruta del Tesoro" (memoria espacial). Se muestran unos tesoros en el
    /// mapa, se ocultan y hay que encontrarlos; con 2 errores la ruta se pierde y cuesta una
    /// vida. Versión Unity de <c>RutaTesoroGame.kt</c>, pero en vez de 5 rondas fijas es una
    /// escalera de niveles (3x3 -> 5x5, de 3 a 12 tesoros) que sigue hasta quedarse sin vidas.
    /// </summary>
    public static class TreasureContract
    {
        public const string GameId = "rutatesoro";
        public const int MaxStage = 12;
        public const int Lives = 3;
        public const int MaxMisses = 2;
        /// <summary>Tope de rutas jugadas en una partida (evita partidas interminables).</summary>
        public const int MaxRounds = 20;
        /// <summary>Rutas completadas que valen el 100% del puntaje.</summary>
        public const int ClearedForFullScore = 10;

        private static readonly TreasureStage[] Stages =
        {
            new TreasureStage(3, 3),   // 1
            new TreasureStage(3, 4),   // 2
            new TreasureStage(4, 4),   // 3
            new TreasureStage(4, 5),   // 4
            new TreasureStage(4, 6),   // 5
            new TreasureStage(4, 7),   // 6
            new TreasureStage(5, 7),   // 7
            new TreasureStage(5, 8),   // 8
            new TreasureStage(5, 9),   // 9
            new TreasureStage(5, 10),  // 10
            new TreasureStage(5, 11),  // 11
            new TreasureStage(5, 12),  // 12
        };

        public static TreasureStage StageFor(int stage) =>
            Stages[Math.Max(1, Math.Min(stage, MaxStage)) - 1];

        /// <summary>Milisegundos para memorizar: más tesoros = más tiempo, la maestría lo acorta
        /// (piso 1300 ms).</summary>
        public static int ShowMs(int stage, int intensity)
        {
            int treasures = StageFor(stage).Treasures;
            return Math.Max(1300, 900 + treasures * 260 - intensity * 40);
        }

        /// <summary>Segundos para encontrarlos en modo Reto.</summary>
        public static float FindSeconds(int stage) => 5f + StageFor(stage).Treasures * 1.4f;

        public static HashSet<int> PickTreasures(TreasureStage stage, Random rng)
        {
            var set = new HashSet<int>();
            while (set.Count < stage.Treasures) set.Add(rng.Next(stage.Cells));
            return set;
        }

        /// <summary>Completar sube un nivel; perder la ruta baja uno (mínimo 1).</summary>
        public static int NextStage(int stage, bool cleared) =>
            cleared ? Math.Min(stage + 1, MaxStage) : Math.Max(stage - 1, 1);

        public static int Score(int cleared) =>
            Math.Max(0, Math.Min(100, cleared * 100 / ClearedForFullScore));
    }
}
