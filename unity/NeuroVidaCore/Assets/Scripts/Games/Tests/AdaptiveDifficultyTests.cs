using NUnit.Framework;

namespace NeuroVida.Games.Tests
{
    public class AdaptiveDifficultyTests
    {
        private static AdaptiveDifficulty Make(AgeBand age = AgeBand.Adult, float start = 1f, int max = 9, float step = 0.15f, bool rt = true)
            => new AdaptiveDifficulty(max, age, start, step, null, rt);

        /// <summary>Usuario simulado: P(acierto) logística según la distancia entre su habilidad y el nivel presentado.</summary>
        private static (float accuracy, float meanLevel) Simulate(AdaptiveDifficulty dda, float ability, int trials, int seed, float slope = 1.6f)
        {
            var rng = new System.Random(seed);
            int correct = 0, counted = 0;
            double levelSum = 0;
            for (int i = 0; i < trials; i++)
            {
                int level = dda.PresentedLevel;
                double p = 1.0 / (1.0 + System.Math.Exp(-slope * (ability - level)));
                bool ok = rng.NextDouble() < p;
                dda.Register(ok, 900f + (float)rng.NextDouble() * 400f);
                if (i >= trials / 2) { counted++; if (ok) correct++; levelSum += dda.Level; }
            }
            return ((float)correct / counted, (float)(levelSum / counted));
        }

        [Test]
        public void StartRating_SpreadsTheChosenLevelOverTheWholeLadder()
        {
            Assert.AreEqual(1f, AdaptiveDifficulty.StartRating(1, 9, 0), 1e-4f);
            Assert.AreEqual(3f, AdaptiveDifficulty.StartRating(2, 9, 0), 1e-4f);
            Assert.AreEqual(5f, AdaptiveDifficulty.StartRating(3, 9, 0), 1e-4f);
            Assert.AreEqual(9f, AdaptiveDifficulty.StartRating(5, 9, 0), 1e-4f);
            Assert.AreEqual(1f, AdaptiveDifficulty.StartRating(1, 7, 0), 1e-4f);
            Assert.AreEqual(4f, AdaptiveDifficulty.StartRating(3, 7, 0), 1e-4f);
            Assert.Greater(AdaptiveDifficulty.StartRating(2, 9, 20), AdaptiveDifficulty.StartRating(2, 9, 0));
            Assert.LessOrEqual(AdaptiveDifficulty.StartRating(2, 9, 999), 3f + 1.2f + 1e-4f); // la maestría suma poco y acotado
        }

        [Test]
        public void AlwaysCorrect_ClimbsGraduallyAndNeverPastTheTop()
        {
            var dda = Make();
            int previous = dda.Level;
            int jumps = 0;
            for (int i = 0; i < 400; i++)
            {
                dda.Register(true, 1000f);
                Assert.LessOrEqual(dda.Level - previous, 1, "salto de más de un nivel de golpe");
                if (dda.Level != previous) jumps++;
                previous = dda.Level;
            }
            Assert.AreEqual(9, dda.Level);
            Assert.AreEqual(8, jumps);
        }

        [Test]
        public void AlwaysWrong_DescendsButNeverBelowTheFirstLevel()
        {
            var dda = Make(start: 6f);
            for (int i = 0; i < 100; i++) dda.Register(false, 3000f);
            Assert.AreEqual(1, dda.Level);
            Assert.GreaterOrEqual(dda.Rating, 1f);
        }

        [Test]
        public void OneErrorCostsMoreThanOneSuccessGains_ByTheTargetRatio()
        {
            var up = Make(start: 5f, rt: false);
            var down = Make(start: 5f, rt: false);
            // Sin calibración ni bonos: se hace pasar el rating por 10 ensayos neutros primero.
            for (int i = 0; i < 10; i++) { up.Register(i % 2 == 1); down.Register(i % 2 == 1); }
            float u0 = up.Rating, d0 = down.Rating;
            up.Register(true);
            down.Register(false);
            float gain = up.Rating - u0, loss = d0 - down.Rating;
            Assert.AreEqual(4f, loss / gain, 0.6f, "la razón bajada/subida debe ser p/(1-p)=4 para el objetivo 0.80");
        }

        [Test]
        public void Simulation_ConvergesToTheTargetAccuracyForAdults()
        {
            foreach (float ability in new[] { 3f, 5f, 7f })
            {
                var (acc, level) = Simulate(Make(AgeBand.Adult, 1f), ability, 1500, 11);
                Assert.That(acc, Is.InRange(0.72f, 0.88f), $"habilidad {ability}: acierto {acc:0.00}");
                // El nivel de equilibrio queda cerca de habilidad - ln(4)/pendiente = habilidad - 0.87.
                Assert.That(level, Is.InRange(ability - 1.8f, ability + 0.3f), $"habilidad {ability}: nivel {level:0.0}");
            }
        }

        [Test]
        public void HigherAbilityEndsAtAHigherLevel()
        {
            var (_, low) = Simulate(Make(AgeBand.Adult, 1f), 3f, 1200, 21);
            var (_, high) = Simulate(Make(AgeBand.Adult, 1f), 7f, 1200, 21);
            Assert.Greater(high, low + 2f);
        }

        [Test]
        public void Seniors_TargetAHigherAccuracyThanAdults()
        {
            Assert.AreEqual(0.85f, AdaptiveDifficulty.TargetFor(AgeBand.Senior), 1e-4f);
            Assert.AreEqual(0.80f, AdaptiveDifficulty.TargetFor(AgeBand.Adult), 1e-4f);
            var (accSenior, _) = Simulate(Make(AgeBand.Senior, 1f), 5f, 2500, 31);
            var (accAdult, _) = Simulate(Make(AgeBand.Adult, 1f), 5f, 2500, 31);
            Assert.Greater(accSenior, accAdult - 0.005f);
        }

        [Test]
        public void WarmUp_PresentsOneLevelBelowAtTheStart()
        {
            var dda = Make(start: 5f);
            Assert.AreEqual(4, dda.PresentedLevel);
            dda.Register(true);
            Assert.AreEqual(4, dda.PresentedLevel);
            dda.Register(true);
            Assert.AreEqual(dda.Level, dda.PresentedLevel);
            Assert.AreEqual(1, Make(start: 1f).PresentedLevel, "nunca por debajo de 1");
        }

        [Test]
        public void TwoErrorsInARow_FlagStrugglingAndACorrectAnswerClearsIt()
        {
            var dda = Make(start: 5f);
            dda.Register(false);
            Assert.IsFalse(dda.Struggling);
            dda.Register(false);
            Assert.IsTrue(dda.Struggling);
            dda.Register(true);
            Assert.IsFalse(dda.Struggling);
        }

        [Test]
        public void FastCorrectAnswersGainMoreThanSlowOnes_ButNotWhenReactionIsIgnored()
        {
            var fast = Make(start: 5f);
            var slow = Make(start: 5f);
            var ignore = Make(start: 5f, rt: false);
            var rng = new System.Random(3);
            for (int i = 0; i < 20; i++)
            {
                float rt = 1000f + (float)rng.NextDouble() * 500f;
                fast.Register(i % 5 != 4, rt);
                slow.Register(i % 5 != 4, rt);
                ignore.Register(i % 5 != 4, rt);
            }
            float f0 = fast.Rating, s0 = slow.Rating;
            fast.Register(true, 300f);   // muy rápido para su historia
            slow.Register(true, 3500f);  // muy lento para su historia
            Assert.Greater(fast.Rating - f0, slow.Rating - s0);

            float i0 = ignore.Rating;
            ignore.Register(true, 300f);
            float gainFast = ignore.Rating - i0;
            i0 = ignore.Rating;
            ignore.Register(true, 3500f);
            Assert.AreEqual(gainFast, ignore.Rating - i0, 1e-4f);
        }

        [Test]
        public void SixCorrectInARow_GiveAnExtraBoredomBonus()
        {
            var dda = Make(start: 3f, rt: false);
            for (int i = 0; i < 10; i++) dda.Register(i % 2 == 0);
            float before = dda.Rating;
            var again = Make(start: 3f, rt: false);
            for (int i = 0; i < 10; i++) again.Register(i % 2 == 0);
            for (int i = 0; i < 5; i++) { dda.Register(true); again.Register(true); }
            float withoutBonus = dda.Rating;
            dda.Register(true);   // 6.º acierto seguido: bono
            again.Register(false); again.Register(true);
            Assert.Greater(dda.Rating - withoutBonus, 0.15f + 0.15f); // paso normal + bono
            Assert.Greater(before, 0f);
        }

        [Test]
        public void ChangeEvents_ReportUpAndDown()
        {
            var dda = Make(start: 1f, rt: false);
            bool up = false, down = false;
            for (int i = 0; i < 40 && !up; i++) up = dda.Register(true) == DdaChange.Up;
            Assert.IsTrue(up);
            for (int i = 0; i < 10 && !down; i++) down = dda.Register(false) == DdaChange.Down;
            Assert.IsTrue(down);
            Assert.GreaterOrEqual(dda.PeakLevel, 2);
        }

        [Test]
        public void RatingNormalized_IsBetweenZeroAndOne()
        {
            var dda = Make(start: 9.99f);
            Assert.That(dda.RatingNormalized, Is.InRange(0f, 1f));
            var low = Make(start: 1f);
            Assert.AreEqual(0f, low.RatingNormalized, 1e-4f);
        }

        [Test]
        public void SavedRating_ContinuesFromWhereTheUserLeftOff()
        {
            var saved = new NeuroVida.Contracts.SequenceConfigDetails { level = 1, base_intensity = 0, has_dda_rating = true, dda_rating = 0.5f };
            Assert.AreEqual(1f + 0.5f * 9f, AdaptiveDifficulty.StartRating(saved, 9), 1e-4f);
            saved.dda_rating = 5f; // fuera de rango: se acota
            Assert.LessOrEqual(AdaptiveDifficulty.StartRating(saved, 9), 9.99f);
            var none = new NeuroVida.Contracts.SequenceConfigDetails { level = 3, base_intensity = 0, has_dda_rating = false, dda_rating = 0.9f };
            Assert.AreEqual(5f, AdaptiveDifficulty.StartRating(none, 9), 1e-4f); // sin dato guardado: manda el nivel elegido
        }

        [Test]
        public void SavedRating_RoundTripsWithRatingNormalized()
        {
            var dda = new AdaptiveDifficulty(9, AgeBand.Adult, 6.3f);
            var cfg = new NeuroVida.Contracts.SequenceConfigDetails { has_dda_rating = true, dda_rating = dda.RatingNormalized };
            // RatingNormalized = (rating-1)/max  ->  rating = 1 + n*max
            Assert.AreEqual(6.3f, AdaptiveDifficulty.StartRating(cfg, 9), 1e-3f);
        }
    }
}
