using NUnit.Framework;
using NeuroVida.Games.Parejas;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>Puerto 1:1 de <c>CardsGameContractTest.kt</c> -- la escalera fija de
    /// parejas por nivel (corrección al bug real "el nivel 2 me salió más fácil que el
    /// nivel 1" reportado del lado Kotlin).</summary>
    public class CardsGameContractTests
    {
        [Test]
        public void PairCountForStage_IsTheExactFixedTableForStages1Through10()
        {
            var expected = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12 };
            for (int stage = 1; stage <= 10; stage++)
            {
                Assert.AreEqual(expected[stage - 1], CardsGameContract.PairCountForStage(stage));
            }
        }

        [Test]
        public void PairCountForStage_IsStrictlyIncreasingAcrossAll10Stages()
        {
            int previous = CardsGameContract.PairCountForStage(1);
            for (int stage = 2; stage <= 10; stage++)
            {
                int current = CardsGameContract.PairCountForStage(stage);
                Assert.Greater(current, previous, $"el nivel {stage} ({current} parejas) debería tener MÁS parejas que el nivel {stage - 1} ({previous})");
                previous = current;
            }
        }

        [Test]
        public void PairCountForStage_ClampsStageNumbersOutsideThe1To10Range()
        {
            Assert.AreEqual(CardsGameContract.PairCountForStage(1), CardsGameContract.PairCountForStage(0));
            Assert.AreEqual(CardsGameContract.PairCountForStage(1), CardsGameContract.PairCountForStage(-5));
            Assert.AreEqual(CardsGameContract.PairCountForStage(CardsGameContract.TotalStages), CardsGameContract.PairCountForStage(CardsGameContract.TotalStages + 1));
            Assert.AreEqual(CardsGameContract.PairCountForStage(CardsGameContract.TotalStages), CardsGameContract.PairCountForStage(999));
        }

        [Test]
        public void TotalStages_MatchesTheSizeOfThePairCountTable()
        {
            Assert.AreEqual(10, CardsGameContract.TotalStages);
        }
    }
}
