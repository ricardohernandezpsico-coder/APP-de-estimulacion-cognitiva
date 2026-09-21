package com.example.games.parejas

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Pruebas unitarias del motor DDA real de "Parejas Ocultas".
 *
 * Nota de honestidad técnica: Ricardo pegó un ejemplo de pruebas escrito contra una API
 * ficticia (`MultidimensionalDDAEngine`, `PlayerPerformance`, `DDADifficultyState`,
 * JUnit 5 + MockK) que no existe en este proyecto -- acá NO se usa nada de eso, se
 * escribieron pruebas nuevas contra la clase real ([VisualWorkingMemoryDDA]), con el
 * framework que el proyecto ya tiene configurado (JUnit 4, sin mocks porque la clase no
 * tiene dependencias externas que mockear). El objetivo del material pegado sí aplica:
 * validar que D(t) suba/baje en la dirección correcta, que los pesos por perfil
 * modulen esa respuesta, y que cada dimensión de dificultad (vista previa, distractores,
 * interferencia, grilla) se resuelva bien a partir de D(t).
 */
class VisualWorkingMemoryDDATest {

  private fun correctTrial(reactionTimeMs: Long) =
    VisualWorkingMemoryDDA.TrialResult(wasCorrect = true, wasOmission = false, reactionTimeMs = reactionTimeMs)

  private fun wrongTrial(reactionTimeMs: Long) =
    VisualWorkingMemoryDDA.TrialResult(wasCorrect = false, wasOmission = false, reactionTimeMs = reactionTimeMs)

  private fun omissionTrial() =
    VisualWorkingMemoryDDA.TrialResult(wasCorrect = false, wasOmission = true, reactionTimeMs = 0L)

  // --- D(t) arranca en 0 y queda anclado por initialProfile ---

  @Test
  fun `initialProfile clamps seed difficulty into 0 to 1`() {
    assertEquals(0f, VisualWorkingMemoryDDA().initialProfile(-0.5f, pairCount = 4).difficultyIndex)
    assertEquals(1f, VisualWorkingMemoryDDA().initialProfile(1.5f, pairCount = 4).difficultyIndex)
    assertEquals(0.42f, VisualWorkingMemoryDDA().initialProfile(0.42f, pairCount = 4).difficultyIndex, 0.0001f)
  }

  @Test
  fun `currentProfile does not mutate difficultyIndex`() {
    val dda = VisualWorkingMemoryDDA()
    dda.initialProfile(0.3f, pairCount = 4)
    val before = dda.currentProfile(pairCount = 4).difficultyIndex
    dda.currentProfile(pairCount = 8) // pairCount distinto, D(t) no debería cambiar
    val after = dda.currentProfile(pairCount = 2).difficultyIndex
    assertEquals(before, after, 0.0001f)
  }

  // --- Dirección del ajuste: acierto empuja para arriba, error empuja para abajo ---

  @Test
  fun `two correct trials during RT warm-up increase difficulty by exactly the accuracy term`() {
    // Durante los primeros MIN_TRIALS_FOR_ZSCORE - 1 ensayos, el z-score es neutral (0f)
    // por falta de historial -- el único término que mueve D(t) acá es la precisión.
    // targetAccuracy=0.8, weightAccuracy=0.65, stepSize=0.08:
    // delta por ensayo correcto = stepSize * weightAccuracy * (1 - 0.8) = 0.08*0.65*0.2 = 0.0104
    val dda = VisualWorkingMemoryDDA()
    dda.registerTrial(correctTrial(500L), pairCount = 4)
    assertEquals(0.0104f, dda.difficultyIndex, 0.0005f)
    dda.registerTrial(correctTrial(500L), pairCount = 4)
    assertEquals(0.0208f, dda.difficultyIndex, 0.0005f)
  }

  @Test
  fun `a single omission from a mid-range difficulty pulls it down hard`() {
    val dda = VisualWorkingMemoryDDA()
    dda.initialProfile(0.5f, pairCount = 4)
    dda.registerTrial(omissionTrial(), pairCount = 4)
    // errorP = 0 - 0.8 = -0.8; z = MAX_Z_SCORE = 2.5 (omisión siempre cuenta como el peor
    // RT posible) -> delta = 0.08 * (0.65*-0.8 + 0.35*-2.5) = 0.08 * -1.395 = -0.1116
    assertEquals(0.5f - 0.1116f, dda.difficultyIndex, 0.001f)
  }

  @Test
  fun `sustained correct trials never push difficulty above 1`() {
    val dda = VisualWorkingMemoryDDA()
    repeat(100) { dda.registerTrial(correctTrial(reactionTimeMs = 50L), pairCount = 4) }
    assertTrue(dda.difficultyIndex <= 1f)
    assertTrue("un desempeño perfecto y rápido sostenido debería acercarse al techo", dda.difficultyIndex > 0.9f)
  }

  @Test
  fun `sustained wrong trials never push difficulty below 0`() {
    val dda = VisualWorkingMemoryDDA()
    dda.initialProfile(0.5f, pairCount = 4)
    repeat(100) { dda.registerTrial(wrongTrial(reactionTimeMs = 2000L), pairCount = 4) }
    assertEquals(0f, dda.difficultyIndex, 0.0001f)
  }

  // --- El componente de tiempo de reacción realmente influye (no solo la precisión) ---

  @Test
  fun `a correct trial faster than the user's own history raises difficulty more than one at baseline speed`() {
    // Dos instancias reciben EXACTAMENTE el mismo calentamiento (mismo historial de RT),
    // así que llegan al mismo D(t) y a la misma media/desvío -- después, una recibe un
    // ensayo a la velocidad del promedio (z≈0) y la otra mucho más rápido (z muy
    // negativo, empuja para arriba). Esto aísla el efecto del término de velocidad sin
    // depender de cálculos manuales de Welford a través de muchos pasos.
    val warmup = List(5) { correctTrial(1000L) }
    val ddaBaselineSpeed = VisualWorkingMemoryDDA()
    val ddaMuchFaster = VisualWorkingMemoryDDA()
    warmup.forEach {
      ddaBaselineSpeed.registerTrial(it, pairCount = 4)
      ddaMuchFaster.registerTrial(it, pairCount = 4)
    }
    assertEquals(ddaBaselineSpeed.difficultyIndex, ddaMuchFaster.difficultyIndex, 0.0001f)

    ddaBaselineSpeed.registerTrial(correctTrial(1000L), pairCount = 4) // misma velocidad que su historial
    ddaMuchFaster.registerTrial(correctTrial(150L), pairCount = 4) // mucho más rápido que su historial

    assertTrue(ddaMuchFaster.difficultyIndex > ddaBaselineSpeed.difficultyIndex)
  }

  // --- Los pesos por perfil (piloto de perfiles por edad) modulan la respuesta ---

  @Test
  fun `a higher reaction-time weight amplifies the speed-driven adjustment`() {
    val warmup = List(5) { correctTrial(1000L) }
    val lowRtWeight = VisualWorkingMemoryDDA(weightAccuracy = 0.9f, weightReactionTime = 0.1f)
    val highRtWeight = VisualWorkingMemoryDDA(weightAccuracy = 0.1f, weightReactionTime = 0.9f)
    warmup.forEach {
      lowRtWeight.registerTrial(it, pairCount = 4)
      highRtWeight.registerTrial(it, pairCount = 4)
    }

    lowRtWeight.registerTrial(correctTrial(150L), pairCount = 4)
    highRtWeight.registerTrial(correctTrial(150L), pairCount = 4)

    val lowRtWeightGain = lowRtWeight.difficultyIndex
    val highRtWeightGain = highRtWeight.difficultyIndex
    assertTrue(
      "un perfil que pesa más la velocidad (ej. adulto joven) debería subir más de dificultad ante un ensayo rápido que uno que la pesa poco (ej. adulto mayor)",
      highRtWeightGain > lowRtWeightGain
    )
  }

  @Test
  fun `a higher accuracy weight amplifies the correctness-driven adjustment`() {
    val lowAccuracyWeight = VisualWorkingMemoryDDA(weightAccuracy = 0.1f, weightReactionTime = 0.9f)
    val highAccuracyWeight = VisualWorkingMemoryDDA(weightAccuracy = 0.9f, weightReactionTime = 0.1f)

    // Mismo ensayo (correcto, RT neutral durante el calentamiento -> z=0) para ambas: el
    // único término que puede diferir es el de precisión.
    lowAccuracyWeight.registerTrial(correctTrial(500L), pairCount = 4)
    highAccuracyWeight.registerTrial(correctTrial(500L), pairCount = 4)

    assertTrue(
      "un perfil que pesa más la precisión (ej. adulto mayor) debería reaccionar más a un acierto que uno que la pesa poco",
      highAccuracyWeight.difficultyIndex > lowAccuracyWeight.difficultyIndex
    )
  }

  // --- Vista previa: interpola entre 3000ms y el piso del perfil activo ---

  @Test
  fun `preview exposure is at its maximum when difficulty is zero, regardless of profile floor`() {
    val adultProfileDda = VisualWorkingMemoryDDA(previewFloorMs = 500f)
    val seniorProfileDda = VisualWorkingMemoryDDA(previewFloorMs = 1500f)
    assertEquals(3000L, adultProfileDda.initialProfile(0f, pairCount = 4).previewExposureMs)
    assertEquals(3000L, seniorProfileDda.initialProfile(0f, pairCount = 4).previewExposureMs)
  }

  @Test
  fun `preview exposure hits the profile-specific floor when difficulty is maxed out`() {
    val adultProfileDda = VisualWorkingMemoryDDA(previewFloorMs = 500f)
    val seniorProfileDda = VisualWorkingMemoryDDA(previewFloorMs = 1500f)
    assertEquals(500L, adultProfileDda.initialProfile(1f, pairCount = 4).previewExposureMs)
    assertEquals(1500L, seniorProfileDda.initialProfile(1f, pairCount = 4).previewExposureMs)
  }

  // --- Distractores e interferencia: 0 en D=0, máximo en D=1 ---

  @Test
  fun `distractors and interference are at their minimum when difficulty is zero`() {
    val profile = VisualWorkingMemoryDDA().initialProfile(0f, pairCount = 4)
    assertEquals(0, profile.distractorCount)
    assertEquals(0.03f, profile.distractorOpacity, 0.0001f)
    assertEquals(0, profile.interferenceLevel)
  }

  @Test
  fun `distractors and interference reach their cap when difficulty is maxed out`() {
    val profile = VisualWorkingMemoryDDA().initialProfile(1f, pairCount = 4)
    assertEquals(6, profile.distractorCount)
    assertEquals(0.12f, profile.distractorOpacity, 0.0001f)
    assertEquals(3, profile.interferenceLevel)
  }

  // --- Grilla: columnas/filas resueltas a partir del pairCount que decide el nivel ---

  @Test
  fun `grid dimensions are resolved correctly for representative pair counts`() {
    val dda = VisualWorkingMemoryDDA()
    assertEquals(2 to 2, dda.initialProfile(0f, pairCount = 2).gridDimensions)
    assertEquals(3 to 4, dda.currentProfile(pairCount = 6).gridDimensions)
    assertEquals(4 to 5, dda.currentProfile(pairCount = 9).gridDimensions)
    assertEquals(4 to 6, dda.currentProfile(pairCount = 12).gridDimensions)
  }

  @Test
  fun `pairCount from the level is reflected as-is in the profile, independent of difficulty`() {
    val dda = VisualWorkingMemoryDDA()
    dda.initialProfile(0.9f, pairCount = 10)
    assertEquals(10, dda.currentProfile(pairCount = 10).pairCount)
    // Confirma que subir D(t) a mano NO cambia cuántas parejas hay -- eso ahora lo
    // decide únicamente el nivel (ver CardsGameContract.pairCountForStage), no D(t).
    assertFalse(dda.currentProfile(pairCount = 10).pairCount == 18)
  }
}
