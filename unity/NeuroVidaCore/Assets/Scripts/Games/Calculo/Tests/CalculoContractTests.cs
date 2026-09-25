using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace NeuroVida.Games.Calculo.Tests
{
    public class CalculoContractTests
    {
        /// <summary>Resuelve el enunciado por su cuenta (independiente del generador) y devuelve la
        /// respuesta esperada; falla si el formato no se reconoce.</summary>
        private static int Solve(string p)
        {
            Match m;
            if ((m = Regex.Match(p, @"^(\d+) ([+\-×÷]) (\d+) = \?$")).Success)
                return Op(int.Parse(m.Groups[1].Value), m.Groups[2].Value, int.Parse(m.Groups[3].Value));
            if ((m = Regex.Match(p, @"^\((\d+) × (\d+)\) ([+\-÷]) (\d+) = \?$")).Success)
                return Op(int.Parse(m.Groups[1].Value) * int.Parse(m.Groups[2].Value), m.Groups[3].Value, int.Parse(m.Groups[4].Value));
            if ((m = Regex.Match(p, @"^(\d+) × (\d+) - (\d+) = \?$")).Success)
                return int.Parse(m.Groups[1].Value) * int.Parse(m.Groups[2].Value) - int.Parse(m.Groups[3].Value);
            if ((m = Regex.Match(p, @"^(\d+)% de (\d+) = \?$")).Success)
            {
                int pct = int.Parse(m.Groups[1].Value), b = int.Parse(m.Groups[2].Value);
                Assert.AreEqual(0, b * pct % 100, "porcentaje no exacto: " + p);
                return b * pct / 100;
            }
            if ((m = Regex.Match(p, @"^(\d+)² = \?$")).Success)
                return int.Parse(m.Groups[1].Value) * int.Parse(m.Groups[1].Value);
            if ((m = Regex.Match(p, @"^(\d+) \+ \? = (\d+)$")).Success)
                return int.Parse(m.Groups[2].Value) - int.Parse(m.Groups[1].Value);
            if ((m = Regex.Match(p, @"^(\d+) × \? = (\d+)$")).Success)
            {
                int a = int.Parse(m.Groups[1].Value), c = int.Parse(m.Groups[2].Value);
                Assert.AreEqual(0, c % a);
                return c / a;
            }
            if ((m = Regex.Match(p, @"^\((\d+) \+ (\d+)\) × (\d+) = \?$")).Success)
                return (int.Parse(m.Groups[1].Value) + int.Parse(m.Groups[2].Value)) * int.Parse(m.Groups[3].Value);
            if ((m = Regex.Match(p, @"^(\d+) × (\d+) ([+\-]) (\d+) × (\d+) = \?$")).Success)
            {
                int x = int.Parse(m.Groups[1].Value) * int.Parse(m.Groups[2].Value);
                int y = int.Parse(m.Groups[4].Value) * int.Parse(m.Groups[5].Value);
                return m.Groups[3].Value == "+" ? x + y : x - y;
            }
            Assert.Fail("formato de enunciado no reconocido: " + p);
            return 0;
        }

        private static int Op(int a, string op, int b)
        {
            switch (op)
            {
                case "+": return a + b;
                case "-": return a - b;
                case "×": return a * b;
                default:
                    Assert.AreEqual(0, a % b, $"división no exacta: {a} ÷ {b}");
                    return a / b;
            }
        }

        [Test]
        public void EveryGeneratedAnswer_MatchesAnIndependentSolution()
        {
            var rng = new System.Random(17);
            for (int level = 1; level <= CalculoContract.MaxLevel; level++)
            {
                for (int i = 0; i < 400; i++)
                {
                    var q = CalculoContract.Generate(level, i % 40, rng);
                    Assert.AreEqual(Solve(q.Prompt), q.Answer, $"nivel {level}: {q.Prompt}");
                }
            }
        }

        [Test]
        public void Options_AreFourDistinctNonNegativeValuesContainingTheAnswer()
        {
            var rng = new System.Random(18);
            for (int level = 1; level <= CalculoContract.MaxLevel; level++)
            {
                for (int i = 0; i < 300; i++)
                {
                    var q = CalculoContract.Generate(level, i % 30, rng);
                    Assert.AreEqual(4, q.Options.Length);
                    Assert.AreEqual(4, new HashSet<int>(q.Options).Count, q.Prompt);
                    CollectionAssert.Contains(q.Options, q.Answer);
                    foreach (int o in q.Options) Assert.GreaterOrEqual(o, 0);
                    Assert.GreaterOrEqual(q.Answer, 0);
                }
            }
        }

        [Test]
        public void Families_AreIntroducedGradually()
        {
            var rng = new System.Random(19);
            bool percent5 = false, missing5 = false, square7 = false, twoProducts8 = false, percentHard9 = false;
            for (int i = 0; i < 1500; i++)
            {
                // Antes de su nivel, la familia no puede aparecer.
                Assert.IsFalse(CalculoContract.Generate(1, 0, rng).Prompt.Contains("%"));
                Assert.IsFalse(CalculoContract.Generate(3, 0, rng).Prompt.Contains("÷"), "división antes del nivel 4");
                Assert.IsFalse(CalculoContract.Generate(4, 0, rng).Prompt.Contains("%"), "porcentaje antes del nivel 5");
                Assert.IsFalse(CalculoContract.Generate(6, 0, rng).Prompt.Contains("²"), "cuadrado antes del nivel 7");
                Assert.IsFalse(Regex.IsMatch(CalculoContract.Generate(7, 0, rng).Prompt, @"× \d+ [+\-] \d+ ×"), "dos productos antes del nivel 8");

                if (CalculoContract.Generate(5, 0, rng).Prompt.Contains("%")) percent5 = true;
                if (CalculoContract.Generate(5, 0, rng).Prompt.Contains("+ ?")) missing5 = true;
                if (CalculoContract.Generate(7, 0, rng).Prompt.Contains("²")) square7 = true;
                if (Regex.IsMatch(CalculoContract.Generate(8, 0, rng).Prompt, @"× \d+ [+\-] \d+ ×")) twoProducts8 = true;
                string p9 = CalculoContract.Generate(9, 0, rng).Prompt;
                if (p9.Contains("%") && (p9.StartsWith("15") || p9.StartsWith("30") || p9.StartsWith("75") || p9.StartsWith("40"))) percentHard9 = true;
            }
            Assert.IsTrue(percent5 && missing5 && square7 && twoProducts8 && percentHard9);
        }

        [Test]
        public void FirstLevel_IsOnlySmallAdditionsAndSubtractionsWithWideDistractors()
        {
            var rng = new System.Random(20);
            for (int i = 0; i < 400; i++)
            {
                var q = CalculoContract.Generate(1, 0, rng);
                Assert.IsTrue(Regex.IsMatch(q.Prompt, @"^\d+ [+\-] \d+ = \?$"), q.Prompt);
                Assert.LessOrEqual(q.Answer, 30);
            }
        }

        [Test]
        public void FallSeconds_ShrinksWithLevelAndMasteryWithFloor()
        {
            Assert.Greater(CalculoContract.FallSeconds(1, 0), CalculoContract.FallSeconds(5, 0));
            Assert.Greater(CalculoContract.FallSeconds(3, 0), CalculoContract.FallSeconds(3, 10));
            Assert.AreEqual(5f, CalculoContract.FallSeconds(7, 200), 1e-4f);
        }

        [Test]
        public void PointsFor_GrowTwoPerStreakStep()
        {
            Assert.AreEqual(10, CalculoContract.PointsFor(1));
            Assert.AreEqual(14, CalculoContract.PointsFor(3));
        }

        [Test]
        public void Scores_BehaveLikeTheOtherGames()
        {
            Assert.AreEqual(100, CalculoContract.Score(150, 10));
            Assert.AreEqual(66, CalculoContract.Score(100, 10));
            Assert.AreEqual(0, CalculoContract.Score(0, 0));
            Assert.AreEqual(100, CalculoContract.EndlessScore(20, 20));
            Assert.AreEqual(50, CalculoContract.EndlessScore(10, 20));
            Assert.AreEqual(0, CalculoContract.EndlessScore(0, 0));
        }
    }
}
