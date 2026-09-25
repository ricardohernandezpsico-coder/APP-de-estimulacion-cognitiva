using NUnit.Framework;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// Puerto 1:1 de <c>SequenceDDAEngineTest.kt</c> (Kotlin, NeuroVida Android) — sirve
    /// como especificación exacta del motor portado: debe dar los mismos números que la
    /// versión Kotlin ya validada. Si algún test de acá falla pero el equivalente Kotlin
    /// pasa, el bug está en el puerto a C#, no en la regla de diseño.
    /// </summary>
    public class SequenceDDAEngineTests
    {
        [Test]
        public void DefaultBoundsMatchRicardosRefactoringGuide()
        {
            Assert.AreEqual(3, SequenceDDAEngine.MinSpanDefault);
            Assert.AreEqual(12, SequenceDDAEngine.MaxSpanDefault);
            Assert.AreEqual(300L, SequenceDDAEngine.MinIsiMsDefault);
            Assert.AreEqual(1200L, SequenceDDAEngine.MaxIsiMsDefault);
        }

        [Test]
        public void SeedAnchorsSpanAndIsiWithinBounds()
        {
            var engine = new SequenceDDAEngine();
            var profile = engine.Seed(initialSpan: 4, initialIsiMs: 1200L);
            Assert.AreEqual(4, profile.SpanLength);
            Assert.AreEqual(1200L, profile.InterStimulusIntervalMs);
        }

        [Test]
        public void SeedClampsOutOfRangeValuesToEngineBounds()
        {
            var engine = new SequenceDDAEngine();
            var profile = engine.Seed(initialSpan: 999, initialIsiMs: 5000L);
            Assert.AreEqual(SequenceDDAEngine.MaxSpanDefault, profile.SpanLength);
            Assert.AreEqual(SequenceDDAEngine.MaxIsiMsDefault, profile.InterStimulusIntervalMs);
        }

        [Test]
        public void FewerThanThreeRoundsInWindowNeverAdjustsDifficulty()
        {
            var engine = new SequenceDDAEngine();
            engine.Seed(initialSpan: 4, initialIsiMs: 1000L);
            var afterOne = engine.RegisterRound(wasCorrect: false);
            var afterTwo = engine.RegisterRound(wasCorrect: false);
            Assert.AreEqual(4, afterOne.SpanLength);
            Assert.AreEqual(1000L, afterOne.InterStimulusIntervalMs);
            Assert.AreEqual(4, afterTwo.SpanLength);
            Assert.AreEqual(1000L, afterTwo.InterStimulusIntervalMs);
        }

        [Test]
        public void AntiFrustrationRule_StrugglingSlowsIsiBeforeShrinkingSpan()
        {
            var engine = new SequenceDDAEngine();
            engine.Seed(initialSpan: 4, initialIsiMs: 1000L);
            // Ventana de 3 fallos seguidos (0% de precisión, bajo el piso de 0.60) -- el
            // ISI debería subir (más lento), el span NO debería tocarse todavía (1000
            // está lejos del techo 1200).
            engine.RegisterRound(false);
            engine.RegisterRound(false);
            var profile = engine.RegisterRound(false);
            Assert.AreEqual(4, profile.SpanLength, "el span no debería bajar mientras el ISI todavía puede subir");
            Assert.Greater(profile.InterStimulusIntervalMs, 1000L, "el ISI debería haber subido (más lento) tras la racha de fallos");
        }

        [Test]
        public void AntiFrustrationRule_SpanOnlyShrinksOnceSlowingIsiWouldCrossCeiling()
        {
            // Ventana deslizante de verdad: una vez completa, CADA llamada siguiente
            // reevalúa los últimos 3 resultados (no "cada 3 llamadas").
            var engine = new SequenceDDAEngine(minIsiMs: 400L, maxIsiMs: 1000L); // techo bajo para forzar el caso en pocas rondas
            engine.Seed(initialSpan: 5, initialIsiMs: 700L);
            engine.RegisterRound(false);
            engine.RegisterRound(false);
            var afterThird = engine.RegisterRound(false); // 700*1.2=840, todavía <= 1000 -> solo sube el ISI
            Assert.AreEqual(840L, afterThird.InterStimulusIntervalMs);
            Assert.AreEqual(5, afterThird.SpanLength, "el span no debería bajar mientras el ISI todavía puede subir sin pasarse del techo");

            var afterFourth = engine.RegisterRound(false); // 840*1.2=1008 > 1000 techo -> ya no puede subir más -> baja el span
            Assert.AreEqual(4, afterFourth.SpanLength, "con el ISI ya no pudiendo subir más sin cruzar el techo, el span debería bajar");
        }

        [Test]
        public void SymmetricRule_SuccessSpeedsUpIsiBeforeGrowingSpan()
        {
            var engine = new SequenceDDAEngine();
            engine.Seed(initialSpan: 4, initialIsiMs: 1000L);
            engine.RegisterRound(true);
            engine.RegisterRound(true);
            var profile = engine.RegisterRound(true); // 100% de precisión, sobre el piso de 0.85
            Assert.AreEqual(4, profile.SpanLength, "el span no debería subir mientras el ISI todavía puede bajar");
            Assert.Less(profile.InterStimulusIntervalMs, 1000L, "el ISI debería haber bajado (más rápido) tras la racha de aciertos");
        }

        [Test]
        public void SymmetricRule_SpanOnlyGrowsOnceSpeedingIsiWouldCrossFloor()
        {
            var engine = new SequenceDDAEngine(minIsiMs: 400L, maxIsiMs: 1800L);
            engine.Seed(initialSpan: 4, initialIsiMs: 500L);
            engine.RegisterRound(true);
            engine.RegisterRound(true);
            var afterThird = engine.RegisterRound(true); // 500*0.85=425, todavía >= 400 -> solo baja el ISI
            Assert.AreEqual(425L, afterThird.InterStimulusIntervalMs);
            Assert.AreEqual(4, afterThird.SpanLength, "el span no debería subir mientras el ISI todavía puede bajar sin cruzar el piso");

            var afterFourth = engine.RegisterRound(true); // 425*0.85=361 < 400 -> ya no puede bajar más -> sube el span
            Assert.AreEqual(5, afterFourth.SpanLength, "con el ISI ya no pudiendo bajar más sin cruzar el piso, el span debería subir");
        }

        [Test]
        public void MixedRecentResultsWithinZpdBandLeaveDifficultyUnchanged()
        {
            var engine = new SequenceDDAEngine();
            engine.Seed(initialSpan: 4, initialIsiMs: 1000L);
            engine.RegisterRound(true);
            engine.RegisterRound(true);
            var profile = engine.RegisterRound(false); // 2/3 = 0.667, entre 0.60 y 0.85 -> sin cambios
            Assert.AreEqual(4, profile.SpanLength);
            Assert.AreEqual(1000L, profile.InterStimulusIntervalMs);
        }

        [Test]
        public void SpanNeverDropsBelowMinSpanEvenWithSustainedFailure()
        {
            var engine = new SequenceDDAEngine(minIsiMs: 400L, maxIsiMs: 500L); // rango angosto para llegar rápido al piso
            var profile = engine.Seed(SequenceDDAEngine.MinSpanDefault, 500L);
            for (int i = 0; i < 20; i++)
            {
                engine.RegisterRound(false);
                engine.RegisterRound(false);
                profile = engine.RegisterRound(false);
            }
            Assert.AreEqual(SequenceDDAEngine.MinSpanDefault, profile.SpanLength);
        }

        [Test]
        public void SpanNeverExceedsMaxSpanEvenWithSustainedSuccess()
        {
            var engine = new SequenceDDAEngine(minIsiMs: 400L, maxIsiMs: 500L);
            var profile = engine.Seed(SequenceDDAEngine.MaxSpanDefault, 400L);
            for (int i = 0; i < 20; i++)
            {
                engine.RegisterRound(true);
                engine.RegisterRound(true);
                profile = engine.RegisterRound(true);
            }
            Assert.AreEqual(SequenceDDAEngine.MaxSpanDefault, profile.SpanLength);
        }

        [Test]
        public void DistractorCountGrowsWithDifficultyAndStaysWithinBounds()
        {
            var easyEngine = new SequenceDDAEngine();
            var easyProfile = easyEngine.Seed(SequenceDDAEngine.MinSpanDefault, SequenceDDAEngine.MaxIsiMsDefault);
            Assert.AreEqual(0, easyProfile.DistractorCount, "en el punto más fácil posible no debería haber distractores");

            var hardEngine = new SequenceDDAEngine();
            var hardProfile = hardEngine.Seed(SequenceDDAEngine.MaxSpanDefault, SequenceDDAEngine.MinIsiMsDefault);
            Assert.Greater(hardProfile.DistractorCount, 0, "en el punto más difícil posible debería haber distractores");
            Assert.LessOrEqual(hardProfile.DistractorCount, 4);
        }

        [Test]
        public void RestartingWithSeedClearsRollingWindowFromPreviousGame()
        {
            var engine = new SequenceDDAEngine();
            engine.Seed(initialSpan: 4, initialIsiMs: 1000L);
            engine.RegisterRound(false);
            engine.RegisterRound(false);
            // Si la ventana no se limpiara, este único acierto completaría una ventana de
            // "2 fallos + 1 acierto" (33%) heredada de la partida anterior.
            var profileAfterReseed = engine.Seed(initialSpan: 4, initialIsiMs: 1000L);
            var afterOneRound = engine.RegisterRound(true);
            Assert.AreEqual(4, profileAfterReseed.SpanLength);
            Assert.AreEqual(4, afterOneRound.SpanLength); // 1 sola ronda -> ventana de 3 todavía no se completa
            Assert.AreEqual(1000L, afterOneRound.InterStimulusIntervalMs);
        }
    }
}
