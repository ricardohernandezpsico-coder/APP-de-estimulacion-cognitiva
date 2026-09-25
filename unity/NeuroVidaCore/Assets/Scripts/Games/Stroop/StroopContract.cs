using System;
using UnityEngine;

namespace NeuroVida.Games.Stroop
{
    public enum StroopRule { Ink, Word }

    public readonly struct StroopTrial
    {
        /// <summary>Índice (en <see cref="StroopContract.Colors"/>) de la palabra escrita.</summary>
        public readonly int WordIndex;
        /// <summary>Índice del color de la tinta.</summary>
        public readonly int InkIndex;
        public readonly StroopRule Rule;

        public StroopTrial(int wordIndex, int inkIndex, StroopRule rule)
        {
            WordIndex = wordIndex;
            InkIndex = inkIndex;
            Rule = rule;
        }

        /// <summary>La respuesta correcta depende de la regla activa de este ensayo.</summary>
        public int CorrectIndex => Rule == StroopRule.Ink ? InkIndex : WordIndex;
    }

    /// <summary>
    /// Reglas puras de "Tinta o Palabra" (puerto 1:1 de <c>StroopGame.kt</c>): 12 ensayos,
    /// tiempo por ensayo que baja con el nivel y la maestría, inversión de regla desde el
    /// nivel 4. Sin dependencias de UI para poder testearlo.
    /// </summary>
    public static class StroopContract
    {
        public const string GameId = "stroop";
        public const int TotalTrials = 12;
        /// <summary>Duración de la ronda del modo Reto (sin límite de ensayos).</summary>
        public const int EndlessSeconds = 60;
        /// <summary>Ensayos que se consideran "ritmo completo" en una ronda de 60 s.</summary>
        public const int EndlessTargetTrials = 24;

        public static readonly string[] Names = { "ROJO", "AZUL", "VERDE", "AMARILLO", "MORADO" };

        public static readonly Color[] Colors =
        {
            new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f),
            new Color(0x3B / 255f, 0x82 / 255f, 0xF6 / 255f),
            new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f),
            new Color(0xFA / 255f, 0xCC / 255f, 0x15 / 255f),
            new Color(0xA8 / 255f, 0x55 / 255f, 0xF7 / 255f),
        };

        /// <summary>Segundos por ensayo (modo Reto): baja de a 1s por nivel y 1s más cada 3
        /// rondas de maestría, con piso de 3s -- el reto nunca deja de crecer.</summary>
        public static int BaseTimeForLevel(int level, int intensity)
        {
            int byLevel;
            switch (level)
            {
                case 1: byLevel = 10; break;
                case 2: byLevel = 9; break;
                case 3: byLevel = 8; break;
                case 4: byLevel = 7; break;
                default: byLevel = 6; break;
            }
            return Math.Max(byLevel - intensity / 3, 3);
        }

        /// <summary>Probabilidad de que el ensayo pida la PALABRA en vez de la tinta. Niveles
        /// 1-3 nunca cambian de regla; desde el 4 varía sin aviso y la maestría lo sube.</summary>
        public static float RuleFlipChance(int level, int intensity)
        {
            if (level < 4) return 0f;
            return Math.Min(0.25f + intensity * 0.02f, 0.5f);
        }

        public static StroopTrial GenerateTrial(int level, int intensity, System.Random rng)
        {
            int word = rng.Next(Names.Length);
            int ink;
            if (level > 1)
            {
                // Siempre incongruente: la tinta nunca coincide con la palabra.
                ink = rng.Next(Names.Length - 1);
                if (ink >= word) ink++;
            }
            else
            {
                ink = rng.Next(Names.Length);
            }
            var rule = rng.NextDouble() < RuleFlipChance(level, intensity) ? StroopRule.Word : StroopRule.Ink;
            return new StroopTrial(word, ink, rule);
        }

        /// <summary>Puntaje 0-100 del modo sin límite: precisión x ritmo (llegar a
        /// <see cref="EndlessTargetTrials"/> ensayos vale el 100% del ritmo). Así ni responder
        /// al azar muy rápido ni acertar todo muy despacio da el máximo.</summary>
        public static int EndlessScore(int correct, int total)
        {
            if (total <= 0) return 0;
            float accuracy = (float)correct / total;
            float pace = Math.Min(1f, (float)total / EndlessTargetTrials);
            return Math.Max(0, Math.Min(100, (int)Math.Round(accuracy * pace * 100f)));
        }

        public static int Score(int correct, int total) =>
            total <= 0 ? 0 : Math.Max(0, Math.Min(100, correct * 100 / total));
    }
}
