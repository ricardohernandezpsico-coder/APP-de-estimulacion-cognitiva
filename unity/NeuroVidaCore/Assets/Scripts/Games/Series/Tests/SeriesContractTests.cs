using System.Collections.Generic;
using NUnit.Framework;

namespace NeuroVida.Games.Series.Tests
{
    public class SeriesContractTests
    {
        [Test]
        public void Options_AreFourDistinctNonNegativeValuesContainingTheAnswer()
        {
            var rng = new System.Random(7);
            for (int level = 1; level <= SeriesContract.MaxLevel; level++)
            {
                for (int i = 0; i < 300; i++)
                {
                    var item = SeriesContract.Generate(level, i % 30, rng);
                    Assert.AreEqual(4, item.Options.Length);
                    Assert.AreEqual(4, new HashSet<int>(item.Options).Count, $"repetidas en nivel {level}");
                    CollectionAssert.Contains(item.Options, item.Answer);
                    foreach (int o in item.Options) Assert.GreaterOrEqual(o, 0);
                    Assert.That(item.Terms.Length, Is.InRange(4, 6));
                    Assert.AreEqual(item.Terms.Length, item.StepLabels.Length);
                }
            }
        }

        // Aplica una etiqueta ("+5", "-3", "×2", "×3 +1") a un valor.
        private static int Apply(int value, string label)
        {
            foreach (var part in label.Split(' '))
            {
                int n = int.Parse(part.Substring(1));
                value = part[0] == '×' ? value * n : part[0] == '+' ? value + n : value - n;
            }
            return value;
        }

        [Test]
        public void StepLabels_AppliedToTermsProduceTheNextTerm()
        {
            var rng = new System.Random(8);
            for (int level = 1; level <= SeriesContract.MaxLevel; level++)
            {
                for (int i = 0; i < 300; i++)
                {
                    var item = SeriesContract.Generate(level, i % 20, rng);
                    var all = new List<int>(item.Terms) { item.Answer };
                    for (int k = 0; k < item.StepLabels.Length; k++)
                        Assert.AreEqual(all[k + 1], Apply(all[k], item.StepLabels[k]),
                            $"paso {k} ({item.StepLabels[k]}) en {string.Join(",", all)} [{item.Rule}]");
                }
            }
        }

        [Test]
        public void HighLevels_ProduceManyDifferentPatternFamilies()
        {
            var rng = new System.Random(31);
            var rules = new HashSet<string>();
            for (int i = 0; i < 600; i++)
            {
                string rule = SeriesContract.Generate(9, 0, rng).Rule;
                // Se quitan los números para contar familias, no instancias.
                rules.Add(System.Text.RegularExpressions.Regex.Replace(rule, "[0-9]+", "#"));
            }
            Assert.GreaterOrEqual(rules.Count, 10, string.Join(" | ", rules));
        }

        [Test]
        public void ComplexFamilies_AreIntroducedGradually_AndUseLongerSeriesOnlyAtTheTop()
        {
            var rng = new System.Random(32);
            bool fibAt8 = false, longAt9 = false;
            for (int i = 0; i < 800; i++)
            {
                if (SeriesContract.Generate(8, 0, rng).Rule.StartsWith("Cada término es la suma")) fibAt8 = true;
                if (SeriesContract.Generate(9, 0, rng).Terms.Length >= 6) longAt9 = true;
            }
            Assert.IsTrue(fibAt8);
            Assert.IsTrue(longAt9);

            // Los niveles bajos no traen familias complejas.
            for (int i = 0; i < 600; i++)
            {
                Assert.IsFalse(SeriesContract.Generate(5, 0, rng).Rule.StartsWith("Cada término es la suma"), "Fibonacci en nivel 5");
                var early = SeriesContract.Generate(3, 0, rng);
                Assert.AreEqual(4, early.Terms.Length, "series largas en nivel 3");
                StringAssert.DoesNotContain("alterna", early.Rule);
                StringAssert.DoesNotContain("intercaladas", early.Rule);
            }
        }

        [Test]
        public void FirstLevel_OnlyHasTheSimplestFamiliesWithWideDistractors()
        {
            var rng = new System.Random(33);
            double totalSpread = 0;
            for (int i = 0; i < 400; i++)
            {
                var item = SeriesContract.Generate(1, 0, rng);
                Assert.IsTrue(item.Rule.StartsWith("Suma fija") || item.Rule.StartsWith("Resta constante"), item.Rule);
                foreach (int o in item.Options) totalSpread += System.Math.Abs(o - item.Answer);
            }
            // Distractores lejanos: en promedio las 3 incorrectas suman bastante distancia a la correcta.
            Assert.Greater(totalSpread / 400.0, 12.0);
        }

        [Test]
        public void Cubes_OnlyAppearFromLevel8()
        {
            var rng = new System.Random(9);
            for (int i = 0; i < 500; i++)
                Assert.AreNotEqual("Cubos perfectos consecutivos", SeriesContract.Generate(7, 0, rng).Rule);

            bool seen = false;
            for (int i = 0; i < 800 && !seen; i++)
                seen = SeriesContract.Generate(8, 0, rng).Rule == "Cubos perfectos consecutivos";
            Assert.IsTrue(seen);
        }

        [Test]
        public void GeometricSeries_ShowMultiplicationSteps()
        {
            var labels = SeriesContract.StepLabels(new[] { 2, 6, 18, 54 }, 162);
            CollectionAssert.AreEqual(new[] { "×3", "×3", "×3", "×3" }, labels);
        }

        [Test]
        public void LinearAndCreasingSeries_ShowSignedDifferences()
        {
            CollectionAssert.AreEqual(new[] { "+5", "+5", "+5", "+5" }, SeriesContract.StepLabels(new[] { 3, 8, 13, 18 }, 23));
            CollectionAssert.AreEqual(new[] { "-4", "-4", "-4", "-4" }, SeriesContract.StepLabels(new[] { 60, 56, 52, 48 }, 44));
            CollectionAssert.AreEqual(new[] { "+3", "+5", "+7", "+9" }, SeriesContract.StepLabels(new[] { 4, 7, 12, 19 }, 28));
        }

        [Test]
        public void LiveIntensity_AddsTwoPerThreeConsecutiveCorrect()
        {
            Assert.AreEqual(5, SeriesContract.LiveIntensity(5, 2));
            Assert.AreEqual(7, SeriesContract.LiveIntensity(5, 3));
            Assert.AreEqual(9, SeriesContract.LiveIntensity(5, 7));
        }

        [Test]
        public void Scores_BehaveLikeTheOtherGames()
        {
            Assert.AreEqual(75, SeriesContract.Score(6, 8));
            Assert.AreEqual(100, SeriesContract.EndlessScore(14, 14));
            Assert.AreEqual(50, SeriesContract.EndlessScore(7, 14));
            Assert.AreEqual(0, SeriesContract.EndlessScore(0, 0));
        }
    }
}
