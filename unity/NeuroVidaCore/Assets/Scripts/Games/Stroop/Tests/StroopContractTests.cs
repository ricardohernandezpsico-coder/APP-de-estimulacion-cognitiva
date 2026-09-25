using NUnit.Framework;

namespace NeuroVida.Games.Stroop.Tests
{
    /// <summary>Puerto de las reglas de <c>StroopGame.kt</c>.</summary>
    public class StroopContractTests
    {
        [Test]
        public void BaseTimeForLevel_DropsWithLevelAndMasteryWithFloorOf3()
        {
            Assert.AreEqual(10, StroopContract.BaseTimeForLevel(1, 0));
            Assert.AreEqual(9, StroopContract.BaseTimeForLevel(2, 0));
            Assert.AreEqual(6, StroopContract.BaseTimeForLevel(5, 0));
            Assert.AreEqual(5, StroopContract.BaseTimeForLevel(5, 3));
            Assert.AreEqual(3, StroopContract.BaseTimeForLevel(5, 300));
        }

        [Test]
        public void RuleFlipChance_IsZeroBelowLevel4AndCappedAtHalf()
        {
            Assert.AreEqual(0f, StroopContract.RuleFlipChance(3, 50));
            Assert.AreEqual(0.25f, StroopContract.RuleFlipChance(4, 0), 1e-5f);
            Assert.AreEqual(0.5f, StroopContract.RuleFlipChance(5, 100), 1e-5f);
        }

        [Test]
        public void GenerateTrial_IsAlwaysIncongruentFromLevel2()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 500; i++)
            {
                var trial = StroopContract.GenerateTrial(2, 0, rng);
                Assert.AreNotEqual(trial.WordIndex, trial.InkIndex);
            }
        }

        [Test]
        public void GenerateTrial_NeverFlipsRuleBelowLevel4()
        {
            var rng = new System.Random(2);
            for (int i = 0; i < 300; i++)
                Assert.AreEqual(StroopRule.Ink, StroopContract.GenerateTrial(3, 40, rng).Rule);
        }

        [Test]
        public void GenerateTrial_FlipsRuleSometimesAtLevel5()
        {
            var rng = new System.Random(3);
            int words = 0;
            for (int i = 0; i < 400; i++)
                if (StroopContract.GenerateTrial(5, 0, rng).Rule == StroopRule.Word) words++;
            Assert.Greater(words, 40);
            Assert.Less(words, 200);
        }

        [Test]
        public void CorrectIndex_FollowsTheActiveRule()
        {
            Assert.AreEqual(1, new StroopTrial(0, 1, StroopRule.Ink).CorrectIndex);
            Assert.AreEqual(0, new StroopTrial(0, 1, StroopRule.Word).CorrectIndex);
        }

        [Test]
        public void Score_IsPercentageOfTrialsClamped()
        {
            Assert.AreEqual(75, StroopContract.Score(9, 12));
            Assert.AreEqual(100, StroopContract.Score(12, 12));
            Assert.AreEqual(0, StroopContract.Score(0, 0));
        }

        [Test]
        public void EndlessScore_CombinesAccuracyAndPace()
        {
            Assert.AreEqual(100, StroopContract.EndlessScore(24, 24));
            Assert.AreEqual(100, StroopContract.EndlessScore(40, 40));
            Assert.AreEqual(25, StroopContract.EndlessScore(6, 12)); // 50% de precisión x 50% de ritmo
            Assert.AreEqual(0, StroopContract.EndlessScore(0, 0));
            Assert.Less(StroopContract.EndlessScore(5, 10), StroopContract.EndlessScore(20, 24));
        }
    }
}
