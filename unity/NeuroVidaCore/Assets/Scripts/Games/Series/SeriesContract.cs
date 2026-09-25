using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Series
{
    public readonly struct SeriesItem
    {
        /// <summary>Los términos visibles (4 a 6, según el patrón).</summary>
        public readonly int[] Terms;
        public readonly int Answer;
        /// <summary>Cuatro opciones distintas (incluye la respuesta), ya mezcladas.</summary>
        public readonly int[] Options;
        /// <summary>Explicación de la regla, se muestra al responder.</summary>
        public readonly string Rule;
        /// <summary>Cuatro etiquetas del paso entre términos consecutivos (los 4 términos + la
        /// respuesta): "+5", "×3", "-4"... Se revelan al responder, entre los términos.</summary>
        public readonly string[] StepLabels;

        public SeriesItem(int[] terms, int answer, int[] options, string rule, string[] stepLabels)
        {
            Terms = terms;
            Answer = answer;
            Options = options;
            Rule = rule;
            StepLabels = stepLabels;
        }
    }

    /// <summary>
    /// Reglas puras de "Detective de Series" (puerto de <c>SeriesGame.kt</c>): series numéricas de
    /// cuatro términos y hay que elegir el quinto entre cuatro opciones. Seis tipos de serie
    /// (suma fija, multiplicación, resta fija, diferencia creciente, cuadrados y, desde el nivel 4,
    /// cubos); el nivel y la maestría empujan pasos y magnitudes. En Unity el Reto es una ronda de
    /// 120 s sin límite de series y el nivel sube dentro de la partida.
    /// </summary>
    public static class SeriesContract
    {
        public const string GameId = "series";
        public const int TotalTrials = 8;
        public const int EndlessSeconds = 120;
        /// <summary>Razonar una serie lleva ~8 s: 14 series en 120 s = ritmo completo.</summary>
        public const int EndlessTargetTrials = 14;
        public const int MaxLevel = 9;

        /// <summary>Aciertos para subir de nivel dentro de la partida (Reto / Precisión).</summary>
        public static int CorrectPerLevelUp(bool endless) => endless ? 5 : 3;

        /// <summary>Maestría "en vivo": cada 3 aciertos seguidos suman 2 puntos de intensidad.</summary>
        public static int LiveIntensity(int intensity, int streak) => intensity + (streak / 3) * 2;

        private static readonly int[] Primes =
            { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79 };

        // Tipos de serie.
        private const int Linear = 0, Multiply = 1, Decrease = 2, GrowingDiff = 3, Squares = 4, Cubes = 5,
            Fibonacci = 6, AlternateAddSub = 7, MultiplyAdd = 8, SquaresPlus = 9, Interleaved = 10,
            AlternateOps = 11, PrimesType = 12, Triangular = 13;

        // Escalera GRADUAL: cada nivel introduce una o dos familias nuevas (no un salto de golpe).
        private static readonly int[][] Introduced =
        {
            new[] { Linear, Decrease },                    // 1
            new[] { Multiply },                            // 2
            new[] { GrowingDiff },                         // 3
            new[] { Squares },                             // 4
            new[] { Triangular },                          // 5
            new[] { AlternateAddSub },                     // 6
            new[] { Fibonacci },                           // 7
            new[] { MultiplyAdd, Cubes },                  // 8
            new[] { SquaresPlus, Interleaved, AlternateOps, PrimesType }, // 9
        };

        /// <summary>Elige la familia: ~65% de las veces entre las recién introducidas (este nivel y el
        /// anterior, para practicarlas) y el resto entre las ya conocidas, así la variedad crece de a poco.</summary>
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

        public static SeriesItem Generate(int level, int intensity, Random rng)
        {
            level = Math.Max(1, Math.Min(level, MaxLevel));
            // Magnitudes: crecen a la mitad de velocidad que el nivel (antes crecían 1 a 1).
            int boost = (level + 1) / 2 + intensity / 3;
            // Distractores lejanos al principio (se distinguen a simple vista) y cada vez más cercanos.
            int tightDelta = Math.Max(12 - level - intensity / 4, 3);
            int type = PickFamily(level, rng);

            switch (type)
            {
                case Linear:
                {
                    int step = rng.Next(2 + boost / 2, 6 + boost);
                    int s1 = rng.Next(2, 20 + boost * 2);
                    return Build(new[] { s1, s1 + step, s1 + 2 * step, s1 + 3 * step }, s1 + 4 * step,
                        $"Suma fija de +{step} en cada término", tightDelta, rng);
                }
                case Multiply:
                {
                    int mult = level <= 4 ? 2 : level <= 6 ? 3 : level <= 8 ? 4 : Math.Min(4 + intensity / 6, 6);
                    int s1 = rng.Next(2, 5);
                    int s2 = s1 * mult, s3 = s2 * mult, s4 = s3 * mult;
                    return Build(new[] { s1, s2, s3, s4 }, s4 * mult,
                        $"Multiplicación por {mult} en cada paso", tightDelta, rng);
                }
                case Decrease:
                {
                    int step = rng.Next(3 + boost / 2, 8 + boost);
                    int s1 = 50 + boost * 3 + step * 4;
                    return Build(new[] { s1, s1 - step, s1 - 2 * step, s1 - 3 * step }, s1 - 4 * step,
                        $"Resta constante de -{step} en cada paso", tightDelta, rng);
                }
                case GrowingDiff:
                {
                    int inc = 2 + boost / 2;
                    int s1 = rng.Next(1, 10 + boost);
                    int s2 = s1 + inc, s3 = s2 + inc * 2, s4 = s3 + inc * 3;
                    return Build(new[] { s1, s2, s3, s4 }, s4 + inc * 4,
                        $"Diferencia creciente: +{inc}, +{inc * 2}, +{inc * 3}, +{inc * 4}...", tightDelta, rng);
                }
                case Squares:
                {
                    int o = rng.Next(1, 4 + boost / 3);
                    return Build(new[] { o * o, (o + 1) * (o + 1), (o + 2) * (o + 2), (o + 3) * (o + 3) }, (o + 4) * (o + 4),
                        "Cuadrados perfectos consecutivos", tightDelta, rng);
                }
                case Cubes:
                {
                    int o = rng.Next(1, 3);
                    return Build(new[] { o * o * o, Cube(o + 1), Cube(o + 2), Cube(o + 3) }, Cube(o + 4),
                        "Cubos perfectos consecutivos", tightDelta, rng);
                }
                case Fibonacci:
                {
                    int a = rng.Next(1, 5), b = rng.Next(2, 7);
                    int t2 = a + b, t3 = b + t2, t4 = t2 + t3;
                    return Build(new[] { a, b, t2, t3, t4 }, t3 + t4,
                        "Cada término es la suma de los dos anteriores", tightDelta, rng);
                }
                case AlternateAddSub:
                {
                    int up = rng.Next(6 + boost / 2, 12 + boost);
                    int down = rng.Next(2, up - 1);
                    int s = rng.Next(5, 25);
                    return Build(new[] { s, s + up, s + up - down, s + 2 * up - down, s + 2 * up - 2 * down }, s + 3 * up - 2 * down,
                        $"Se alterna: +{up} y -{down}", tightDelta, rng,
                        new[] { "+" + up, "-" + down, "+" + up, "-" + down, "+" + up });
                }
                case MultiplyAdd:
                {
                    int m = rng.Next(2, 4);
                    int c = new[] { -1, 1, 2, 3 }[rng.Next(4)];
                    int s = rng.Next(2, 6);
                    int t2 = s * m + c, t3 = t2 * m + c, t4 = t3 * m + c;
                    string op = "×" + m + (c > 0 ? " +" + c : " -" + Math.Abs(c));
                    return Build(new[] { s, t2, t3, t4 }, t4 * m + c,
                        c > 0 ? $"Se multiplica por {m} y se suma {c}" : $"Se multiplica por {m} y se resta {Math.Abs(c)}",
                        tightDelta, rng, new[] { op, op, op, op });
                }
                case SquaresPlus:
                {
                    int o = rng.Next(1, 4 + boost / 3);
                    int c = new[] { -1, 1, 2, 3 }[rng.Next(4)];
                    return Build(new[] { o * o + c, (o + 1) * (o + 1) + c, (o + 2) * (o + 2) + c, (o + 3) * (o + 3) + c },
                        (o + 4) * (o + 4) + c,
                        c > 0 ? $"Cuadrados consecutivos más {c}" : $"Cuadrados consecutivos menos {Math.Abs(c)}",
                        tightDelta, rng);
                }
                case Interleaved:
                {
                    int a0 = rng.Next(3, 16), da = rng.Next(2, 7);
                    int b0 = rng.Next(40, 71), db = rng.Next(2, 7);
                    return Build(new[] { a0, b0, a0 + da, b0 - db, a0 + 2 * da, b0 - 2 * db }, a0 + 3 * da,
                        $"Dos series intercaladas: una sube +{da} y la otra baja -{db}", tightDelta, rng);
                }
                case AlternateOps:
                {
                    int s = rng.Next(2, 7), a = rng.Next(2, 8);
                    int t1 = s + a, t2 = t1 * 2, t3 = t2 + a, t4 = t3 * 2;
                    return Build(new[] { s, t1, t2, t3, t4 }, t4 + a,
                        $"Se alterna: +{a} y ×2", tightDelta, rng,
                        new[] { "+" + a, "×2", "+" + a, "×2", "+" + a });
                }
                case PrimesType:
                {
                    int i = rng.Next(0, Primes.Length - 6);
                    return Build(new[] { Primes[i], Primes[i + 1], Primes[i + 2], Primes[i + 3], Primes[i + 4] }, Primes[i + 5],
                        "Números primos consecutivos", tightDelta, rng);
                }
                default: // Triangular
                {
                    int n = rng.Next(1, 5);
                    return Build(new[] { Tri(n), Tri(n + 1), Tri(n + 2), Tri(n + 3) }, Tri(n + 4),
                        "Números triangulares: se suma 1, 2, 3, 4... más cada vez", tightDelta, rng);
                }
            }
        }

        private static int Tri(int n) => n * (n + 1) / 2;

        private static int Cube(int n) => n * n * n;

        private static SeriesItem Build(int[] terms, int answer, string rule, int deltaRange, Random rng, string[] labels = null)
        {
            var opts = new List<int> { answer };
            int guard = 0;
            while (opts.Count < 4 && guard++ < 500)
            {
                int delta = rng.Next(-deltaRange, deltaRange + 1);
                int candidate = answer + delta;
                if (delta != 0 && candidate >= 0 && !opts.Contains(candidate)) opts.Add(candidate);
            }
            // Si el rango era demasiado estrecho (no debería pasar), completa hacia arriba.
            for (int k = 1; opts.Count < 4; k++) if (!opts.Contains(answer + k)) opts.Add(answer + k);
            // Mezcla Fisher-Yates.
            for (int i = opts.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = opts[i]; opts[i] = opts[j]; opts[j] = tmp;
            }
            return new SeriesItem(terms, answer, opts.ToArray(), rule, labels ?? StepLabels(terms, answer));
        }

        /// <summary>Etiquetas de paso: si la razón es constante y entera, "×r"; si no, la diferencia.</summary>
        public static string[] StepLabels(int[] terms, int answer)
        {
            var all = new int[terms.Length + 1];
            Array.Copy(terms, all, terms.Length);
            all[terms.Length] = answer;

            bool geometric = all[0] > 0;
            int ratio = geometric && all[0] != 0 ? all[1] / all[0] : 0;
            for (int i = 0; i + 1 < all.Length && geometric; i++)
                if (all[i] == 0 || all[i + 1] != all[i] * ratio) geometric = false;

            var labels = new string[all.Length - 1];
            for (int i = 0; i < labels.Length; i++)
            {
                if (geometric && ratio > 1) labels[i] = "×" + ratio;
                else
                {
                    int d = all[i + 1] - all[i];
                    labels[i] = d >= 0 ? "+" + d : "-" + Math.Abs(d);
                }
            }
            return labels;
        }

        public static int Score(int correct, int total) =>
            total <= 0 ? 0 : Math.Max(0, Math.Min(100, correct * 100 / total));

        public static int EndlessScore(int correct, int total)
        {
            if (total <= 0) return 0;
            float accuracy = (float)correct / total;
            float pace = Math.Min(1f, (float)total / EndlessTargetTrials);
            return Math.Max(0, Math.Min(100, (int)Math.Round(accuracy * pace * 100f)));
        }
    }
}
