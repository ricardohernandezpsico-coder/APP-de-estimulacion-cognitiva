using NUnit.Framework;

namespace NeuroVida.Games.Comparacion.Tests
{
    public class ComparisonContractTests
    {
        /// <summary>Evalúa "a + b", "a - b", "a × b" y "a × b ± c" (el producto primero).</summary>
        private static int Eval(string display)
        {
            var t = display.Split(' ');
            int first = int.Parse(t[0]);
            if (t.Length == 1) return first;
            if (t[1] == "×")
            {
                int prod = first * int.Parse(t[2]);
                if (t.Length == 3) return prod;
                int c = int.Parse(t[4]);
                return t[3] == "+" ? prod + c : prod - c;
            }
            int second = int.Parse(t[2]);
            return t[1] == "+" ? first + second : first - second;
        }

        [Test]
        public void GenerateTrial_NeverProducesATie_AtAnyLevel()
        {
            var rng = new System.Random(11);
            for (int level = 1; level <= ComparisonContract.MaxLevel + 1; level++)
            {
                for (int i = 0; i < 500; i++)
                {
                    var t = ComparisonContract.GenerateTrial(level, i % 40, rng);
                    Assert.AreNotEqual(t.Left.Value, t.Right.Value, $"empate en nivel {level}: {t.Left.Display} vs {t.Right.Display}");
                    Assert.Greater(t.Left.Value, 0);
                    Assert.Greater(t.Right.Value, 0);
                }
            }
        }

        [Test]
        public void EveryDisplayedExpression_MatchesItsValue()
        {
            var rng = new System.Random(21);
            for (int level = 2; level <= ComparisonContract.MaxLevel; level++)
            {
                for (int i = 0; i < 300; i++)
                {
                    var t = ComparisonContract.GenerateTrial(level, i % 30, rng);
                    Assert.AreEqual(t.Left.Value, Eval(t.Left.Display), $"{t.Left.Display} nivel {level}");
                    Assert.AreEqual(t.Right.Value, Eval(t.Right.Display), $"{t.Right.Display} nivel {level}");
                }
            }
        }

        [Test]
        public void Level1_UsesDotsBetween4And12()
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 200; i++)
            {
                var t = ComparisonContract.GenerateTrial(1, 0, rng);
                Assert.IsTrue(t.Left.IsDots && t.Right.IsDots);
                Assert.That(t.Left.DotCount, Is.InRange(4, 12));
                Assert.That(t.Right.DotCount, Is.InRange(4, 12));
                Assert.AreEqual(t.Left.Value, t.Left.DotCount);
            }
        }

        [Test]
        public void Level3_HasASumOrDifferenceOnExactlyOneSide()
        {
            var rng = new System.Random(9);
            for (int i = 0; i < 200; i++)
            {
                var t = ComparisonContract.GenerateTrial(3, 0, rng);
                Assert.AreNotEqual(t.Left.IsExpression, t.Right.IsExpression);
                var expr = t.Left.IsExpression ? t.Left : t.Right;
                Assert.IsTrue(expr.Display.Contains("+") || expr.Display.Contains("-"));
            }
        }

        [Test]
        public void Level4_HasAProductOnExactlyOneSide()
        {
            var rng = new System.Random(10);
            for (int i = 0; i < 200; i++)
            {
                var t = ComparisonContract.GenerateTrial(4, 0, rng);
                Assert.AreNotEqual(t.Left.IsExpression, t.Right.IsExpression);
                Assert.IsTrue((t.Left.IsExpression ? t.Left : t.Right).Display.Contains("×"));
            }
        }

        [Test]
        public void Level5Plus_IsMostlyExpressionAgainstExpression()
        {
            var rng = new System.Random(12);
            int both = 0;
            for (int i = 0; i < 200; i++)
            {
                var t = ComparisonContract.GenerateTrial(5, 0, rng);
                if (t.Left.IsExpression && t.Right.IsExpression) both++;
            }
            Assert.Greater(both, 150);
        }

        [Test]
        public void SpeedBonusMs_ShrinksWithMasteryWithFloor()
        {
            Assert.AreEqual(900, ComparisonContract.SpeedBonusMs(0));
            Assert.AreEqual(750, ComparisonContract.SpeedBonusMs(10));
            Assert.AreEqual(500, ComparisonContract.SpeedBonusMs(100));
        }

        [Test]
        public void PrecisionScore_PerfectIs100_AndSpeedOnlyHelpsWhenNotPerfect()
        {
            Assert.AreEqual(100, ComparisonContract.PrecisionScore(12, 12, 12));
            Assert.AreEqual(75, ComparisonContract.PrecisionScore(9, 12, 0));
            Assert.AreEqual(85, ComparisonContract.PrecisionScore(9, 12, 12));
            Assert.AreEqual(0, ComparisonContract.PrecisionScore(0, 0, 0));
        }

        [Test]
        public void EndlessScore_CombinesAccuracyAndPace()
        {
            Assert.AreEqual(100, ComparisonContract.EndlessScore(30, 30));
            Assert.AreEqual(100, ComparisonContract.EndlessScore(50, 50));
            Assert.AreEqual(50, ComparisonContract.EndlessScore(15, 30)); // 50% de precisión, ritmo completo
            Assert.AreEqual(0, ComparisonContract.EndlessScore(0, 0));
            Assert.Less(ComparisonContract.EndlessScore(5, 10), ComparisonContract.EndlessScore(28, 30));
        }
    }
}
