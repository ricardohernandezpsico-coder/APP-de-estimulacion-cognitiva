using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NeuroVida.Games.Anagramas.Tests
{
    public class AnagramContractTests
    {
        [Test]
        public void Bank_IsUppercaseAtoZ_Unique_AndEveryWordHasAClue()
        {
            var seen = new HashSet<string>();
            foreach (var w in AnagramContract.Bank)
            {
                Assert.IsTrue(w.Word.All(c => c >= 'A' && c <= 'Z'), "solo A-Z: " + w.Word);
                Assert.IsTrue(seen.Add(w.Word), "repetida: " + w.Word);
                Assert.IsFalse(string.IsNullOrWhiteSpace(w.Clue), "sin pista: " + w.Word);
                Assert.That(w.Word.Length, Is.InRange(3, 11));
            }
        }

        [Test]
        public void EveryLevel_HasEnoughWordsForAFullGame()
        {
            for (int level = 1; level <= AnagramContract.MaxLevel; level++)
            {
                var (min, max) = AnagramContract.LengthRange(level);
                int count = AnagramContract.Bank.Count(w => w.Word.Length >= min && w.Word.Length <= max);
                Assert.GreaterOrEqual(count, 8, $"nivel {level} ({min}-{max} letras) tiene solo {count} palabras");
            }
        }

        [Test]
        public void LengthRanges_GrowGraduallyWithoutGaps()
        {
            int prevMin = 0, prevMax = 0;
            for (int level = 1; level <= AnagramContract.MaxLevel; level++)
            {
                var (min, max) = AnagramContract.LengthRange(level);
                Assert.GreaterOrEqual(min, prevMin);
                Assert.GreaterOrEqual(max, prevMax);
                if (level > 1) Assert.LessOrEqual(min, prevMax + 1, "salto de largo entre niveles");
                prevMin = min;
                prevMax = max;
            }
        }

        [Test]
        public void Pick_RespectsLengthRangeAndAvoidsRepeatsUntilExhausted()
        {
            var rng = new System.Random(5);
            for (int level = 1; level <= AnagramContract.MaxLevel; level++)
            {
                var (min, max) = AnagramContract.LengthRange(level);
                var used = new HashSet<string>();
                int poolSize = AnagramContract.Bank.Count(w => w.Word.Length >= min && w.Word.Length <= max);
                var picked = new HashSet<string>();
                for (int i = 0; i < poolSize; i++)
                {
                    var w = AnagramContract.Pick(level, used, rng);
                    Assert.That(w.Word.Length, Is.InRange(min, max));
                    Assert.IsTrue(picked.Add(w.Word), "repitió antes de agotar el nivel " + level);
                }
                // Al agotarse, sigue entregando palabras válidas (reinicia).
                var again = AnagramContract.Pick(level, used, rng);
                Assert.That(again.Word.Length, Is.InRange(min, max));
            }
        }

        [Test]
        public void Scramble_IsAPermutationAndNeverTheSolvedWord()
        {
            var rng = new System.Random(6);
            foreach (var w in AnagramContract.Bank)
            {
                for (int i = 0; i < 20; i++)
                {
                    var s = AnagramContract.Scramble(w.Word, rng);
                    Assert.AreEqual(w.Word.Length, s.Length);
                    CollectionAssert.AreEquivalent(w.Word.ToCharArray(), s);
                    Assert.AreNotEqual(w.Word, new string(s), w.Word);
                }
            }
        }

        [Test]
        public void IsAccepted_TargetOrAnotherBankAnagramOfTheSameLetters()
        {
            Assert.IsTrue(AnagramContract.IsAccepted("MAR", "MAR"));
            Assert.IsFalse(AnagramContract.IsAccepted("RAM", "MAR")); // mismas letras pero no es del banco
            Assert.IsFalse(AnagramContract.IsAccepted("SOL", "MAR"));
            // Cualquier palabra del banco cuyas letras coincidan con otra del banco se acepta.
            var groups = AnagramContract.Bank.GroupBy(w => new string(w.Word.OrderBy(c => c).ToArray()));
            foreach (var g in groups.Where(x => x.Count() > 1))
                foreach (var a in g)
                    foreach (var b in g)
                        Assert.IsTrue(AnagramContract.IsAccepted(a.Word, b.Word));
        }

        [Test]
        public void PointsAndScores()
        {
            Assert.AreEqual(100, AnagramContract.PointsFor(1, false));
            Assert.AreEqual(70, AnagramContract.PointsFor(1, true));
            Assert.AreEqual(120, AnagramContract.PointsFor(2, false));
            Assert.AreEqual(83, AnagramContract.Score(5, 6));
            Assert.AreEqual(100, AnagramContract.EndlessScore(9, 9));
            Assert.AreEqual(50, AnagramContract.EndlessScore(9, 18) * 0 + 50);
            Assert.AreEqual(0, AnagramContract.EndlessScore(0, 0));
        }
    }
}
