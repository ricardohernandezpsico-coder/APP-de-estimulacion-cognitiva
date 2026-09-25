using NUnit.Framework;
using NeuroVida.Games.Parejas;

namespace NeuroVida.Games.Parejas.Tests
{
    /// <summary>
    /// Puerto 1:1 de <c>VisualWorkingMemoryDDATest.kt</c> -- mismos casos, mismos números
    /// esperados (comentados con la fórmula igual que el original), para validar que el
    /// motor C# se comporta idéntico al ya probado en producción del lado Kotlin.
    /// </summary>
    public class VisualWorkingMemoryDDATests
    {
        private static VisualWorkingMemoryDDA.TrialResult CorrectTrial(long reactionTimeMs) =>
            new VisualWorkingMemoryDDA.TrialResult(true, false, reactionTimeMs);

        private static VisualWorkingMemoryDDA.TrialResult WrongTrial(long reactionTimeMs) =>
            new VisualWorkingMemoryDDA.TrialResult(false, false, reactionTimeMs);

        private static VisualWorkingMemoryDDA.TrialResult OmissionTrial() =>
            new VisualWorkingMemoryDDA.TrialResult(false, true, 0L);

        [Test]
        public void InitialProfile_ClampsSeedDifficultyInto0To1()
        {
            Assert.AreEqual(0f, new VisualWorkingMemoryDDA().InitialProfile(-0.5f, 4).DifficultyIndex);
            Assert.AreEqual(1f, new VisualWorkingMemoryDDA().InitialProfile(1.5f, 4).DifficultyIndex);
            Assert.AreEqual(0.42f, new VisualWorkingMemoryDDA().InitialProfile(0.42f, 4).DifficultyIndex, 0.0001f);
        }

        [Test]
        public void CurrentProfile_DoesNotMutateDifficultyIndex()
        {
            var dda = new VisualWorkingMemoryDDA();
            dda.InitialProfile(0.3f, 4);
            float before = dda.CurrentProfile(4).DifficultyIndex;
            dda.CurrentProfile(8); // pairCount distinto, D(t) no debería cambiar
            float after = dda.CurrentProfile(2).DifficultyIndex;
            Assert.AreEqual(before, after, 0.0001f);
        }

        [Test]
        public void TwoCorrectTrialsDuringRtWarmup_IncreaseDifficultyByExactlyTheAccuracyTerm()
        {
            // Durante los primeros MIN_TRIALS_FOR_ZSCORE-1 ensayos, z-score neutral (0f).
            // targetAccuracy=0.8, weightAccuracy=0.65, stepSize=0.08:
            // delta = 0.08*0.65*0.2 = 0.0104
            var dda = new VisualWorkingMemoryDDA();
            dda.RegisterTrial(CorrectTrial(500L), 4);
            Assert.AreEqual(0.0104f, dda.DifficultyIndex, 0.0005f);
            dda.RegisterTrial(CorrectTrial(500L), 4);
            Assert.AreEqual(0.0208f, dda.DifficultyIndex, 0.0005f);
        }

        [Test]
        public void ASingleOmissionFromMidRangeDifficulty_PullsItDownHard()
        {
            var dda = new VisualWorkingMemoryDDA();
            dda.InitialProfile(0.5f, 4);
            dda.RegisterTrial(OmissionTrial(), 4);
            // errorP = 0-0.8 = -0.8; z = MAX_Z_SCORE = 2.5 -> delta = 0.08*(0.65*-0.8 + 0.35*-2.5) = -0.1116
            Assert.AreEqual(0.5f - 0.1116f, dda.DifficultyIndex, 0.001f);
        }

        [Test]
        public void SustainedCorrectTrials_NeverPushDifficultyAbove1()
        {
            var dda = new VisualWorkingMemoryDDA();
            for (int i = 0; i < 100; i++) dda.RegisterTrial(CorrectTrial(50L), 4);
            Assert.LessOrEqual(dda.DifficultyIndex, 1f);
            Assert.Greater(dda.DifficultyIndex, 0.9f, "un desempeño perfecto y rápido sostenido debería acercarse al techo");
        }

        [Test]
        public void SustainedWrongTrials_NeverPushDifficultyBelow0()
        {
            var dda = new VisualWorkingMemoryDDA();
            dda.InitialProfile(0.5f, 4);
            for (int i = 0; i < 100; i++) dda.RegisterTrial(WrongTrial(2000L), 4);
            Assert.AreEqual(0f, dda.DifficultyIndex, 0.0001f);
        }

        [Test]
        public void ACorrectTrialFasterThanHistory_RaisesDifficultyMoreThanOneAtBaselineSpeed()
        {
            var ddaBaselineSpeed = new VisualWorkingMemoryDDA();
            var ddaMuchFaster = new VisualWorkingMemoryDDA();
            for (int i = 0; i < 5; i++)
            {
                ddaBaselineSpeed.RegisterTrial(CorrectTrial(1000L), 4);
                ddaMuchFaster.RegisterTrial(CorrectTrial(1000L), 4);
            }
            Assert.AreEqual(ddaBaselineSpeed.DifficultyIndex, ddaMuchFaster.DifficultyIndex, 0.0001f);

            ddaBaselineSpeed.RegisterTrial(CorrectTrial(1000L), 4); // misma velocidad que su historial
            ddaMuchFaster.RegisterTrial(CorrectTrial(150L), 4); // mucho más rápido que su historial

            Assert.Greater(ddaMuchFaster.DifficultyIndex, ddaBaselineSpeed.DifficultyIndex);
        }

        [Test]
        public void AHigherReactionTimeWeight_AmplifiesTheSpeedDrivenAdjustment()
        {
            var lowRtWeight = new VisualWorkingMemoryDDA(weightAccuracy: 0.9f, weightReactionTime: 0.1f);
            var highRtWeight = new VisualWorkingMemoryDDA(weightAccuracy: 0.1f, weightReactionTime: 0.9f);
            for (int i = 0; i < 5; i++)
            {
                lowRtWeight.RegisterTrial(CorrectTrial(1000L), 4);
                highRtWeight.RegisterTrial(CorrectTrial(1000L), 4);
            }

            lowRtWeight.RegisterTrial(CorrectTrial(150L), 4);
            highRtWeight.RegisterTrial(CorrectTrial(150L), 4);

            Assert.Greater(highRtWeight.DifficultyIndex, lowRtWeight.DifficultyIndex,
                "un perfil que pesa más la velocidad debería subir más de dificultad ante un ensayo rápido que uno que la pesa poco");
        }

        [Test]
        public void AHigherAccuracyWeight_AmplifiesTheCorrectnessDrivenAdjustment()
        {
            var lowAccuracyWeight = new VisualWorkingMemoryDDA(weightAccuracy: 0.1f, weightReactionTime: 0.9f);
            var highAccuracyWeight = new VisualWorkingMemoryDDA(weightAccuracy: 0.9f, weightReactionTime: 0.1f);

            lowAccuracyWeight.RegisterTrial(CorrectTrial(500L), 4);
            highAccuracyWeight.RegisterTrial(CorrectTrial(500L), 4);

            Assert.Greater(highAccuracyWeight.DifficultyIndex, lowAccuracyWeight.DifficultyIndex,
                "un perfil que pesa más la precisión debería reaccionar más a un acierto que uno que la pesa poco");
        }

        [Test]
        public void PreviewExposure_IsAtMaximumWhenDifficultyIsZero_RegardlessOfProfileFloor()
        {
            var adultProfileDda = new VisualWorkingMemoryDDA(previewFloorMs: 500f);
            var seniorProfileDda = new VisualWorkingMemoryDDA(previewFloorMs: 1500f);
            Assert.AreEqual(3000L, adultProfileDda.InitialProfile(0f, 4).PreviewExposureMs);
            Assert.AreEqual(3000L, seniorProfileDda.InitialProfile(0f, 4).PreviewExposureMs);
        }

        [Test]
        public void PreviewExposure_HitsTheProfileSpecificFloorWhenDifficultyIsMaxedOut()
        {
            var adultProfileDda = new VisualWorkingMemoryDDA(previewFloorMs: 500f);
            var seniorProfileDda = new VisualWorkingMemoryDDA(previewFloorMs: 1500f);
            Assert.AreEqual(500L, adultProfileDda.InitialProfile(1f, 4).PreviewExposureMs);
            Assert.AreEqual(1500L, seniorProfileDda.InitialProfile(1f, 4).PreviewExposureMs);
        }

        [Test]
        public void DistractorsAndInterference_AreAtMinimumWhenDifficultyIsZero()
        {
            var profile = new VisualWorkingMemoryDDA().InitialProfile(0f, 4);
            Assert.AreEqual(0, profile.DistractorCount);
            Assert.AreEqual(0.03f, profile.DistractorOpacity, 0.0001f);
            Assert.AreEqual(0, profile.InterferenceLevel);
        }

        [Test]
        public void DistractorsAndInterference_ReachTheirCapWhenDifficultyIsMaxedOut()
        {
            var profile = new VisualWorkingMemoryDDA().InitialProfile(1f, 4);
            Assert.AreEqual(6, profile.DistractorCount);
            Assert.AreEqual(0.12f, profile.DistractorOpacity, 0.0001f);
            Assert.AreEqual(3, profile.InterferenceLevel);
        }

        [Test]
        public void GridDimensions_AreResolvedCorrectlyForRepresentativePairCounts()
        {
            var dda = new VisualWorkingMemoryDDA();
            var p2 = dda.InitialProfile(0f, 2);
            Assert.AreEqual(2, p2.GridColumns);
            Assert.AreEqual(2, p2.GridRows);

            var p6 = dda.CurrentProfile(6);
            Assert.AreEqual(3, p6.GridColumns);
            Assert.AreEqual(4, p6.GridRows);

            var p9 = dda.CurrentProfile(9);
            Assert.AreEqual(4, p9.GridColumns);
            Assert.AreEqual(5, p9.GridRows);

            var p12 = dda.CurrentProfile(12);
            Assert.AreEqual(4, p12.GridColumns);
            Assert.AreEqual(6, p12.GridRows);
        }

        [Test]
        public void PairCountFromTheLevel_IsReflectedAsIsInTheProfile_IndependentOfDifficulty()
        {
            var dda = new VisualWorkingMemoryDDA();
            dda.InitialProfile(0.9f, 10);
            Assert.AreEqual(10, dda.CurrentProfile(10).PairCount);
        }
    }
}
