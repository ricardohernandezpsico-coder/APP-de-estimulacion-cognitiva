using System;
using NUnit.Framework;
using NeuroVida.Games;

namespace NeuroVida.Games.Secuencia.Tests
{
    /// <summary>
    /// Puerto 1:1 de <c>DdaUserProfileTest.kt</c> (Kotlin). Igual que allá: estas pruebas
    /// no validan los pesos como "verdad clínica" (se adaptaron con criterio propio a la
    /// escala 0..1 del proyecto), sino las invariantes que tienen que cumplirse siempre.
    /// </summary>
    public class DdaUserProfileConfigTests
    {
        private static readonly AgeBand[] AllBands = (AgeBand[])Enum.GetValues(typeof(AgeBand));

        [Test]
        public void AccuracyAndReactionTimeWeightsAlwaysAddUpToOne()
        {
            foreach (var band in AllBands)
            {
                var config = DdaUserProfileConfig.For(band);
                Assert.AreEqual(1f, config.WeightAccuracy + config.WeightReactionTime, 0.001f,
                    $"los pesos de {band} deberían sumar 1 (son las dos únicas fuentes de la fórmula de D(t))");
            }
        }

        [Test]
        public void SeniorWeighsAccuracyMoreAndSpeedLessThanAdult()
        {
            var senior = DdaUserProfileConfig.For(AgeBand.Senior);
            var adult = DdaUserProfileConfig.For(AgeBand.Adult);
            Assert.Greater(senior.WeightAccuracy, adult.WeightAccuracy);
            Assert.Less(senior.WeightReactionTime, adult.WeightReactionTime);
        }

        [Test]
        public void SeniorAndPediatricDisableStrictTimeouts_AdultDoesNot()
        {
            Assert.IsFalse(DdaUserProfileConfig.For(AgeBand.Senior).AllowStrictTimeouts,
                "el material citado es explícito: timeouts estrictos generan ansiedad en adultos mayores");
            Assert.IsFalse(DdaUserProfileConfig.For(AgeBand.Under18).AllowStrictTimeouts);
            Assert.IsTrue(DdaUserProfileConfig.For(AgeBand.Adult).AllowStrictTimeouts);
        }

        [Test]
        public void SeniorHasHigherPreviewExposureFloorThanAdult()
        {
            var senior = DdaUserProfileConfig.For(AgeBand.Senior);
            var adult = DdaUserProfileConfig.For(AgeBand.Adult);
            Assert.Greater(senior.MinPreviewExposureMs, adult.MinPreviewExposureMs);
        }

        [Test]
        public void SeniorHasLargerTouchTargetsAndSpacingThanAdult()
        {
            // Invariante que resuelve la aparente contradicción del 20-sep: el spec de
            // 20mm/3.2-12.5mm no es "el correcto" en abstracto, es el correcto para
            // SENIOR -- ADULT necesita algo más chico.
            var senior = DdaUserProfileConfig.For(AgeBand.Senior);
            var adult = DdaUserProfileConfig.For(AgeBand.Adult);
            Assert.Greater(senior.IdealTouchTargetMm, adult.IdealTouchTargetMm);
            Assert.Greater(senior.MinSpacingMm, adult.MinSpacingMm);
            Assert.Greater(senior.MaxSpacingMm, adult.MaxSpacingMm);
        }

        [Test]
        public void SpacingRangeIsInternallyConsistentForEveryProfile()
        {
            foreach (var band in AllBands)
            {
                var config = DdaUserProfileConfig.For(band);
                Assert.LessOrEqual(config.MinSpacingMm, config.MaxSpacingMm,
                    $"{band}: minSpacingMm ({config.MinSpacingMm}) no debería superar maxSpacingMm ({config.MaxSpacingMm})");
            }
        }

        [Test]
        public void EveryProfilesIdealTouchTargetClearsAndroidsAccessibilityFloor()
        {
            // 48dp equivalen a ~7.6mm -- ningún perfil debería pedir un ideal por debajo
            // de eso, sea cual sea el criterio clínico.
            foreach (var band in AllBands)
            {
                var config = DdaUserProfileConfig.For(band);
                Assert.GreaterOrEqual(config.IdealTouchTargetMm, 7.6f, $"{band}: idealTouchTargetMm={config.IdealTouchTargetMm}");
            }
        }
    }
}
