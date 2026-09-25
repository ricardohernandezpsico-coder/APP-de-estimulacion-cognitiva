using System;

namespace NeuroVida.Games.Comparacion
{
    public readonly struct ComparisonSide
    {
        /// <summary>Texto grande de la tarjeta ("47", "6 × 8", "9 + 14"); vacío en el modo de puntos.</summary>
        public readonly string Display;
        public readonly int Value;
        /// <summary>Cantidad de puntos a dibujar (nivel 1); 0 = no es una tarjeta de puntos.</summary>
        public readonly int DotCount;

        public ComparisonSide(string display, int value, int dotCount = 0)
        {
            Display = display;
            Value = value;
            DotCount = dotCount;
        }

        public bool IsDots => DotCount > 0;

        /// <summary>true si la tarjeta muestra una cuenta (no un número directo ni puntos).</summary>
        public bool IsExpression => !IsDots && Display != Value.ToString();
    }

    public readonly struct ComparisonTrial
    {
        public readonly ComparisonSide Left;
        public readonly ComparisonSide Right;

        public ComparisonTrial(ComparisonSide left, ComparisonSide right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>Nunca hay empate (se repite el sorteo), así que la respuesta siempre se
        /// puede deducir mirando la pantalla.</summary>
        public bool LeftIsGreater => Left.Value > Right.Value;
    }

    /// <summary>
    /// Reglas puras de "Comparación Instantánea". Escalera de dificultad (nivel base + lo que
    /// se sube dentro de la partida, ver <see cref="ComparisonGameController"/>):
    /// <list type="number">
    /// <item>Puntos (4 a 12).</item>
    /// <item>Números de dos cifras cercanos.</item>
    /// <item>Suma o resta contra un número cercano.</item>
    /// <item>Producto contra un número cercano.</item>
    /// <item>Cuenta contra cuenta (suma, resta o producto en cada lado, valores muy cercanos).</item>
    /// <item>Cuentas de tres términos ("6 × 4 + 7") en ambos lados.</item>
    /// </list>
    /// </summary>
    public static class ComparisonContract
    {
        public const string GameId = "comparacion";
        public const int TotalTrials = 12;
        public const int EndlessSeconds = 60;
        /// <summary>Juego de velocidad: 30 ensayos en 60 s (2 s cada uno) = ritmo completo.</summary>
        public const int EndlessTargetTrials = 30;
        public const int MaxLevel = 7;

        /// <summary>Ventana del bono de velocidad: 900 ms, se acorta con la maestría (piso 500).</summary>
        public static int SpeedBonusMs(int intensity) => Math.Max(900 - intensity * 15, 500);

        /// <summary>Aciertos necesarios para subir de nivel dentro de la partida.</summary>
        public static int CorrectPerLevelUp(bool endless) => endless ? 8 : 4;

        public static ComparisonTrial GenerateTrial(int level, int intensity, Random rng)
        {
            level = Math.Max(1, Math.Min(level, MaxLevel));

            if (level == 1)
            {
                int v1 = rng.Next(4, 13);
                int v2 = rng.Next(4, 13);
                while (v2 == v1) v2 = rng.Next(4, 13);
                return new ComparisonTrial(
                    new ComparisonSide(v1.ToString(), v1, v1),
                    new ComparisonSide(v2.ToString(), v2, v2));
            }

            if (level == 2)
            {
                int v1 = rng.Next(15, 99);
                int v2 = v1 + rng.Next(-9, 10);
                if (v2 == v1) v2 = v1 + 3;
                return new ComparisonTrial(
                    new ComparisonSide(v1.ToString(), v1),
                    new ComparisonSide(v2.ToString(), v2));
            }

            int boost = (level - 3) + intensity / 5;
            int closeness = Math.Max(6 - intensity / 6 - Math.Max(0, level - 4) / 2, 2);

            if (level <= 4)
            {
                var expr = level == 3
                    ? (rng.Next(2) == 0 ? Sum(rng, boost) : Difference(rng, boost))
                    : Product(rng, boost);
                int compare = expr.Value + rng.Next(-closeness, closeness + 1);
                while (compare == expr.Value || compare < 1) compare = expr.Value + rng.Next(-closeness, closeness + 1);
                var number = new ComparisonSide(compare.ToString(), compare);
                return rng.Next(2) == 0 ? new ComparisonTrial(expr, number) : new ComparisonTrial(number, expr);
            }

            // Nivel 5+: cuenta contra cuenta, con valores muy cercanos.
            bool threeTerms = level >= 6;
            var a = RandomExpression(rng, boost, threeTerms);
            ComparisonSide b = a;
            bool found = false;
            for (int i = 0; i < 80 && !found; i++)
            {
                b = RandomExpression(rng, boost, threeTerms);
                int diff = Math.Abs(a.Value - b.Value);
                found = diff >= 1 && diff <= closeness + 2 && b.Display != a.Display;
            }
            if (!found)
            {
                int compare = a.Value + rng.Next(1, closeness + 1) * (rng.Next(2) == 0 ? 1 : -1);
                if (compare < 1) compare = a.Value + 2;
                b = new ComparisonSide(compare.ToString(), compare);
            }
            return rng.Next(2) == 0 ? new ComparisonTrial(a, b) : new ComparisonTrial(b, a);
        }

        private static ComparisonSide RandomExpression(Random rng, int boost, bool threeTerms)
        {
            if (!threeTerms)
            {
                switch (rng.Next(3))
                {
                    case 0: return Sum(rng, boost);
                    case 1: return Difference(rng, boost);
                    default: return Product(rng, boost);
                }
            }

            // a × b ± c  (el producto se resuelve primero)
            int x = rng.Next(3, 8 + Math.Min(boost, 4));
            int y = rng.Next(3, 8 + Math.Min(boost, 4));
            int c = rng.Next(2, 12 + boost);
            bool plus = rng.Next(2) == 0;
            int value = plus ? x * y + c : x * y - c;
            if (value < 1) { plus = true; value = x * y + c; }
            return new ComparisonSide(x + " × " + y + (plus ? " + " : " - ") + c, value);
        }

        private static ComparisonSide Sum(Random rng, int boost)
        {
            int a = rng.Next(8 + boost * 3, 40 + boost * 8);
            int b = rng.Next(8 + boost * 3, 40 + boost * 8);
            return new ComparisonSide(a + " + " + b, a + b);
        }

        private static ComparisonSide Difference(Random rng, int boost)
        {
            int a = rng.Next(40 + boost * 4, 95 + boost * 8);
            int b = rng.Next(8 + boost * 2, 40 + boost * 4);
            return new ComparisonSide(a + " - " + b, a - b);
        }

        private static ComparisonSide Product(Random rng, int boost)
        {
            int a = rng.Next(4 + boost / 2, 9 + boost);
            int b = rng.Next(4 + boost / 2, 9 + boost);
            return new ComparisonSide(a + " × " + b, a * b);
        }

        /// <summary>Modo sin reloj (12 ensayos): precisión + hasta +10 por ensayos correctos
        /// rápidos (solo se nota cuando la precisión no es perfecta).</summary>
        public static int PrecisionScore(int correct, int total, int speedHits)
        {
            if (total <= 0) return 0;
            int baseScore = correct * 100 / total;
            int bonus = speedHits * 10 / total;
            return Math.Max(0, Math.Min(100, baseScore + bonus));
        }

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
