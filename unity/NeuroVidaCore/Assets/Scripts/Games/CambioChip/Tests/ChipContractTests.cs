using NUnit.Framework;

namespace NeuroVida.Games.CambioChip.Tests
{
    public class ChipContractTests
    {
        [Test]
        public void SwitchInterval_ShrinksWithLevelBetween2And4()
        {
            Assert.AreEqual(4, ChipContract.SwitchInterval(1));
            Assert.AreEqual(3, ChipContract.SwitchInterval(2));
            Assert.AreEqual(2, ChipContract.SwitchInterval(3));
            Assert.AreEqual(2, ChipContract.SwitchInterval(5));
            Assert.AreEqual(4, ChipContract.SwitchInterval(0));
        }

        [Test]
        public void SurpriseChance_GrowsWithMasteryAndStreakCappedAtHalf()
        {
            Assert.AreEqual(0f, ChipContract.SurpriseChance(0, 0));
            Assert.AreEqual(0.09f, ChipContract.SurpriseChance(1, 2), 1e-5f);
            Assert.AreEqual(0.5f, ChipContract.SurpriseChance(50, 50), 1e-5f);
        }

        [Test]
        public void Correct_FollowsTheActiveRule()
        {
            var t = new ChipTrial(ChipDirection.Up, ChipDirection.Left, ChipRule.Direction);
            Assert.AreEqual(ChipDirection.Up, t.Correct);
            var p = new ChipTrial(ChipDirection.Up, ChipDirection.Left, ChipRule.Position);
            Assert.AreEqual(ChipDirection.Left, p.Correct);
        }

        [Test]
        public void GenerateTrial_CoversAllFourDirectionsForBothFields()
        {
            var rng = new System.Random(4);
            var pointing = new System.Collections.Generic.HashSet<ChipDirection>();
            var position = new System.Collections.Generic.HashSet<ChipDirection>();
            for (int i = 0; i < 200; i++)
            {
                var t = ChipContract.GenerateTrial(ChipRule.Direction, rng);
                Assert.AreEqual(ChipRule.Direction, t.Rule);
                pointing.Add(t.Pointing);
                position.Add(t.Position);
            }
            Assert.AreEqual(4, pointing.Count);
            Assert.AreEqual(4, position.Count);
        }

        [Test]
        public void Opposite_TogglesTheRule()
        {
            Assert.AreEqual(ChipRule.Position, ChipContract.Opposite(ChipRule.Direction));
            Assert.AreEqual(ChipRule.Direction, ChipContract.Opposite(ChipRule.Position));
        }

        [Test]
        public void Scores_BehaveLikeTheOtherSpeedGames()
        {
            Assert.AreEqual(75, ChipContract.Score(9, 12));
            Assert.AreEqual(0, ChipContract.Score(0, 0));
            Assert.AreEqual(100, ChipContract.EndlessScore(24, 24));
            Assert.AreEqual(25, ChipContract.EndlessScore(6, 12));
            Assert.AreEqual(0, ChipContract.EndlessScore(0, 0));
        }
    }
}
