using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Calculo
{
    public readonly struct MathQuestion
    {
        /// <summary>Enunciado terminado en "?" ("7 × 8 = ?", "12 + ? = 31", "25% de 80 = ?").</summary>
        public readonly string Prompt;
        public readonly int Answer;
        /// <summary>Cuatro opciones distintas (incluye la respuesta), ya mezcladas.</summary>
        public readonly int[] Options;

        public MathQuestion(string prompt, int answer, int[] options)
        {
            Prompt = prompt;
            Answer = answer;
            Options = options;
        }
    }

    /// <summary>
    /// Reglas puras de "Cálculo Sereno" (aritmética mental; puerto de <c>CalculoGame.kt</c> más
    /// familias nuevas para que no sea siempre lo mismo). Escalera de niveles:
    /// 1 sumas/restas chicas · 2 sumas/restas de dos cifras y tablas · 3 tablas, divisiones exactas y
    /// porcentajes redondos · 4 cuentas con paréntesis y "número que falta" · 5 productos grandes,
    /// cuadrados y factor que falta · 6 cuentas encadenadas ((a+b)×c, a×b±c×d) · 7 porcentajes
    /// difíciles y mezcla. Las opciones incorrectas imitan errores reales (±1, ±10, cerca).
    /// En Unity el Reto es una ronda de 90 s sin límite de cuentas.
    /// </summary>
    public static class CalculoContract
    {
        public const string GameId = "calculo";
        public const int TotalTrials = 10;
        public const int EndlessSeconds = 90;
        /// <summary>Cuentas que se consideran ritmo completo en 90 s (una cada ~4.5 s).</summary>
        public const int EndlessTargetTrials = 20;
        public const int MaxLevel = 9;

        public static int CorrectPerLevelUp(bool endless) => endless ? 5 : 3;

        public static int LiveIntensity(int intensity, int streak) => intensity + (streak / 3) * 2;

        /// <summary>Segundos que tarda la gota en caer (modo Reto): baja con el nivel y la maestría.</summary>
        public static float FallSeconds(int level, int intensity) =>
            Math.Max(5f, (17 - Math.Min(level, MaxLevel) - intensity / 2) * 0.62f);

        /// <summary>Puntos por acierto: 10 + 2 por cada acierto seguido previo.</summary>
        public static int PointsFor(int streakAfterHit) => 10 + (streakAfterHit - 1) * 2;

        public static MathQuestion Generate(int level, int intensity, Random rng)
        {
            level = Math.Max(1, Math.Min(level, MaxLevel));
            // Magnitudes grandes solo en los últimos niveles; antes de eso los números se mantienen amables.
            int boost = level >= 7 ? intensity * 2 + (level - 7) * 3 : 0;
            // Distractores lejanos al principio y cada vez más cercanos.
            int tight = Math.Max(12 - level - intensity / 4, 3);
            int kind = PickFamily(level, rng);
            _nearMiss = level >= 4; // los errores típicos (±1, ±10) recién desde el nivel 4

            switch (kind)
            {
                case AddSmall:
                {
                    int a = rng.Next(3, 15), b = rng.Next(2, 12);
                    return Make($"{a} + {b} = ?", a + b, tight, rng);
                }
                case SubSmall:
                {
                    int a = rng.Next(3, 15), b = rng.Next(2, 12);
                    int big = Math.Max(a, b), small = Math.Min(a, b);
                    if (big == small) big++;
                    return Make($"{big} - {small} = ?", big - small, tight, rng);
                }
                case AddBig:
                {
                    int a = rng.Next(12, 35), b = rng.Next(9, 25);
                    return Make($"{a} + {b} = ?", a + b, tight, rng);
                }
                case SubBig:
                {
                    int a = rng.Next(20, 50), b = rng.Next(7, 20);
                    return Make($"{a} - {b} = ?", a - b, tight, rng);
                }
                case TimesSmall:
                {
                    int a = rng.Next(3, 9), b = rng.Next(3, 9);
                    return Make($"{a} × {b} = ?", a * b, tight, rng);
                }
                case TimesMid:
                {
                    int a = rng.Next(6, 12), b = rng.Next(4, 12);
                    return Make($"{a} × {b} = ?", a * b, tight, rng);
                }
                case Divide:
                {
                    int d = rng.Next(3, 9), q = rng.Next(4, 12);
                    return Make($"{d * q} ÷ {d} = ?", q, tight, rng);
                }
                case PercentEasy:
                {
                    int p = new[] { 10, 25, 50 }[rng.Next(3)];
                    int baseVal = 20 * rng.Next(2, 13);
                    return Make($"{p}% de {baseVal} = ?", baseVal * p / 100, tight, rng);
                }
                case ParenTimes:
                {
                    int a = rng.Next(5, 12), b = rng.Next(3, 9), c = rng.Next(2, 15);
                    bool sub = rng.Next(2) == 0;
                    return Make($"({a} × {b}) {(sub ? "-" : "+")} {c} = ?", sub ? a * b - c : a * b + c, tight, rng);
                }
                case ParenDivide:
                {
                    int c = rng.Next(2, 6), q = rng.Next(4, 15);
                    int product = c * q;
                    var divisors = new List<int>();
                    for (int i = 2; i <= product / 2; i++) if (product % i == 0) divisors.Add(i);
                    int a = divisors.Count > 0 ? divisors[rng.Next(divisors.Count)] : 1;
                    return Make($"({a} × {product / a}) ÷ {c} = ?", q, tight, rng);
                }
                case MissingAddend:
                {
                    int a = rng.Next(12, 60), x = rng.Next(8, 40);
                    return Make($"{a} + ? = {a + x}", x, tight, rng);
                }
                case TimesMinus:
                {
                    int a = rng.Next(11 + boost, 20 + boost), b = rng.Next(6 + boost / 2, 15 + boost / 2), c = rng.Next(10 + boost, 30 + boost);
                    return Make($"{a} × {b} - {c} = ?", a * b - c, tight, rng);
                }
                case DivideBig:
                {
                    int d = rng.Next(4 + boost / 3, 9 + boost / 2), q = rng.Next(6 + boost / 2, 18 + boost);
                    return Make($"{d * q} ÷ {d} = ?", q, tight, rng);
                }
                case Square:
                {
                    int n = rng.Next(6, 16 + boost / 4);
                    return Make($"{n}² = ?", n * n, tight, rng);
                }
                case MissingFactor:
                {
                    int a = rng.Next(4, 13 + boost / 3), x = rng.Next(3, 13 + boost / 3);
                    return Make($"{a} × ? = {a * x}", x, tight, rng);
                }
                case SumTimes:
                {
                    int a = rng.Next(4, 15 + boost / 2), b = rng.Next(3, 12 + boost / 2), c = rng.Next(3, 10);
                    return Make($"({a} + {b}) × {c} = ?", (a + b) * c, tight, rng);
                }
                case TwoProducts:
                {
                    int a = rng.Next(3, 10 + boost / 3), b = rng.Next(3, 10 + boost / 3), c = rng.Next(3, 9), d = rng.Next(2, 9);
                    bool sub = rng.Next(2) == 0 && a * b > c * d;
                    return Make($"{a} × {b} {(sub ? "-" : "+")} {c} × {d} = ?", sub ? a * b - c * d : a * b + c * d, tight, rng);
                }
                default: // PercentHard
                {
                    int p = new[] { 15, 30, 75, 20, 40 }[rng.Next(5)];
                    int baseVal = 20 * rng.Next(3, 16 + boost / 4);
                    return Make($"{p}% de {baseVal} = ?", baseVal * p / 100, tight, rng);
                }
            }
        }

        private const int AddSmall = 0, SubSmall = 1, AddBig = 2, SubBig = 3, TimesSmall = 4, TimesMid = 5, Divide = 6,
            PercentEasy = 7, ParenTimes = 8, ParenDivide = 9, MissingAddend = 10, TimesMinus = 11, DivideBig = 12,
            Square = 13, MissingFactor = 14, SumTimes = 15, TwoProducts = 16, PercentHard = 17;

        // Escalera GRADUAL: cada nivel introduce familias nuevas de a poco.
        private static readonly int[][] Introduced =
        {
            new[] { AddSmall, SubSmall },                 // 1
            new[] { AddBig, SubBig },                     // 2
            new[] { TimesSmall },                         // 3
            new[] { TimesMid, Divide },                   // 4
            new[] { PercentEasy, MissingAddend },         // 5
            new[] { ParenTimes, ParenDivide },            // 6
            new[] { TimesMinus, Square, MissingFactor },  // 7
            new[] { SumTimes, DivideBig, TwoProducts },   // 8
            new[] { PercentHard },                        // 9
        };

        /// <summary>~65% entre las familias recién introducidas (este nivel y el anterior) y el resto
        /// entre las ya conocidas.</summary>
        private static int PickFamily(int level, Random rng)
        {
            var newest = new List<int>(Introduced[level - 1]);
            if (level >= 2) newest.AddRange(Introduced[level - 2]);
            var older = new List<int>();
            for (int l = 1; l <= level - 2; l++) older.AddRange(Introduced[l - 1]);
            bool useNewest = older.Count == 0 || rng.NextDouble() < 0.65;
            var pool = useNewest ? newest : older;
            return pool[rng.Next(pool.Count)];
        }

        /// <summary>Opciones con distractores "de error humano": ±1, ±10, cifras cercanas o un salto
        /// al azar dentro de <paramref name="range"/>. Siempre 4 distintas y no negativas.</summary>
        [ThreadStatic] private static bool _nearMiss;

        private static MathQuestion Make(string prompt, int answer, int range, Random rng)
        {
            var opts = new List<int> { answer };
            var candidates = new List<int> { answer + 1, answer - 1, answer + 10, answer - 10, answer + 2, answer - 2 };
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
            }
            foreach (int c in candidates)
            {
                if (!_nearMiss) break;
                if (opts.Count >= 4) break;
                if (c >= 0 && !opts.Contains(c) && rng.Next(3) != 0) opts.Add(c);
            }
            int guard = 0;
            while (opts.Count < 4 && guard++ < 500)
            {
                int delta = rng.Next(-range, range + 1);
                int c = answer + delta;
                if (delta != 0 && c >= 0 && !opts.Contains(c)) opts.Add(c);
            }
            for (int k = 1; opts.Count < 4; k++) if (!opts.Contains(answer + k)) opts.Add(answer + k);
            for (int i = opts.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = opts[i]; opts[i] = opts[j]; opts[j] = tmp;
            }
            return new MathQuestion(prompt, answer, opts.ToArray());
        }

        public static int Score(int points, int total) =>
            total <= 0 ? 0 : Math.Max(0, Math.Min(100, points * 100 / (total * 15)));

        public static int EndlessScore(int correct, int total)
        {
            if (total <= 0) return 0;
            float accuracy = (float)correct / total;
            float pace = Math.Min(1f, (float)total / EndlessTargetTrials);
            return Math.Max(0, Math.Min(100, (int)Math.Round(accuracy * pace * 100f)));
        }
    }
}
