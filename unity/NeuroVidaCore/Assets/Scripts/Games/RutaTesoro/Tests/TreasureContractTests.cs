using NUnit.Framework;

namespace NeuroVida.Games.RutaTesoro.Tests
{
    public class TreasureContractTests
    {
        [Test]
        public void Stages_GrowMonotonicallyInDifficulty()
        {
            int prevTreasures = 0;
            int prevGrid = 0;
            for (int s = 1; s <= TreasureContract.MaxStage; s++)
            {
                var st = TreasureContract.StageFor(s);
                Assert.GreaterOrEqual(st.Treasures, prevTreasures);
                Assert.GreaterOrEqual(st.GridSize, prevGrid);
                Assert.Less(st.Treasures, st.Cells, "siempre quedan casillas vacías");
                Assert.LessOrEqual(st.Treasures * 2, st.Cells + 1 + st.Cells / 2, "no más de ~mitad+ de tesoros");
                prevTreasures = st.Treasures;
                prevGrid = st.GridSize;
            }
        }

        [Test]
        public void StageFor_ClampsOutOfRange()
        {
            Assert.AreEqual(3, TreasureContract.StageFor(0).Treasures);
            Assert.AreEqual(12, TreasureContract.StageFor(99).Treasures);
        }

        [Test]
        public void PickTreasures_ReturnsExactCountOfDistinctCellsInRange()
        {
            var rng = new System.Random(3);
            for (int s = 1; s <= TreasureContract.MaxStage; s++)
            {
                var st = TreasureContract.StageFor(s);
                var set = TreasureContract.PickTreasures(st, rng);
                Assert.AreEqual(st.Treasures, set.Count);
                foreach (int c in set) Assert.That(c, Is.InRange(0, st.Cells - 1));
            }
        }

        [Test]
        public void ShowMs_GrowsWithTreasuresAndShrinksWithMasteryWithFloor()
        {
            Assert.Greater(TreasureContract.ShowMs(10, 0), TreasureContract.ShowMs(1, 0));
            Assert.Less(TreasureContract.ShowMs(5, 20), TreasureContract.ShowMs(5, 0));
            Assert.AreEqual(1300, TreasureContract.ShowMs(1, 500));
        }

        [Test]
        public void FindSeconds_GrowsWithTreasures()
        {
            Assert.Greater(TreasureContract.FindSeconds(8), TreasureContract.FindSeconds(2));
        }

        [Test]
        public void NextStage_UpOnClearDownOnFailWithinBounds()
        {
            Assert.AreEqual(5, TreasureContract.NextStage(4, true));
            Assert.AreEqual(3, TreasureContract.NextStage(4, false));
            Assert.AreEqual(1, TreasureContract.NextStage(1, false));
            Assert.AreEqual(TreasureContract.MaxStage, TreasureContract.NextStage(TreasureContract.MaxStage, true));
        }

        [Test]
        public void Score_IsTenPercentPerClearedRouteCappedAt100()
        {
            Assert.AreEqual(0, TreasureContract.Score(0));
            Assert.AreEqual(50, TreasureContract.Score(5));
            Assert.AreEqual(100, TreasureContract.Score(10));
            Assert.AreEqual(100, TreasureContract.Score(15));
        }
    }
}
