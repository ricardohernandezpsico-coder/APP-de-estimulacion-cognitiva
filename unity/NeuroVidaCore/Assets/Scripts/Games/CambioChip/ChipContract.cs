using System;

namespace NeuroVida.Games.CambioChip
{
    public enum ChipDirection { Up, Down, Left, Right }

    /// <summary>Qué hay que tocar: hacia dónde APUNTA la flecha, o DÓNDE ESTÁ la ficha.</summary>
    public enum ChipRule { Direction, Position }

    public readonly struct ChipTrial
    {
        public readonly ChipDirection Pointing;
        public readonly ChipDirection Position;
        public readonly ChipRule Rule;

        public ChipTrial(ChipDirection pointing, ChipDirection position, ChipRule rule)
        {
            Pointing = pointing;
            Position = position;
            Rule = rule;
        }

        public ChipDirection Correct => Rule == ChipRule.Direction ? Pointing : Position;
    }

    /// <summary>Reglas puras de "Cambio de Chip" (puerto de <c>CambioChipGame.kt</c>): una ficha con una
    /// flecha aparece en uno de los cuatro bordes de la arena; la regla activa alterna entre
    /// "hacia dónde apunta" y "dónde está".</summary>
    public static class ChipContract
    {
        public const string GameId = "cambiochip";
        public const int TotalTrials = 12;
        public const int EndlessSeconds = 60;
        /// <summary>Con cambios de regla, ~2.5 s por ensayo = ritmo completo.</summary>
        public const int EndlessTargetTrials = 24;

        public static readonly string[] DirectionNames = { "ARRIBA", "ABAJO", "IZQUIERDA", "DERECHA" };

        /// <summary>Cada cuántos ensayos cambia la regla: más seguido a mayor nivel (4 -> 2).</summary>
        public static int SwitchInterval(int level) => Math.Max(2, Math.Min(4, 5 - level));

        /// <summary>Probabilidad de un cambio sorpresa fuera del patrón: sube con la maestría y con
        /// la racha de la partida, se enfría al fallar (racha = 0).</summary>
        public static float SurpriseChance(int intensity, int streak) =>
            Math.Min((intensity + streak) * 0.03f, 0.5f);

        public static ChipTrial GenerateTrial(ChipRule rule, Random rng) =>
            new ChipTrial((ChipDirection)rng.Next(4), (ChipDirection)rng.Next(4), rule);

        public static ChipRule Opposite(ChipRule rule) =>
            rule == ChipRule.Direction ? ChipRule.Position : ChipRule.Direction;

        public static int Score(int correct, int total) =>
            total <= 0 ? 0 : Math.Max(0, Math.Min(100, correct * 100 / total));

        /// <summary>Modo Reto (60 s sin límite de ensayos): precisión x ritmo.</summary>
        public static int EndlessScore(int correct, int total)
        {
            if (total <= 0) return 0;
            float accuracy = (float)correct / total;
            float pace = Math.Min(1f, (float)total / EndlessTargetTrials);
            return Math.Max(0, Math.Min(100, (int)Math.Round(accuracy * pace * 100f)));
        }
    }
}
