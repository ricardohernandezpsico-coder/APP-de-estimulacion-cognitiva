package com.example.games.secuencia

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Pruebas del motor DDA multidimensional de "Secuencia Lumínica" -- en particular, la
 * regla anti-frustración que pidió el material que compartió Ricardo: ante una racha de
 * error, primero se ralentiza la presentación (ISI sube) y solo cuando el ISI YA está en
 * su techo se acorta la secuencia (nunca al revés). Simétricamente para una racha de
 * éxito: primero acelera, luego alarga.
 */
class SequenceDDAEngineTest {

  @Test
  fun `default bounds match the ranges from Ricardo's refactoring guide (span 3-12, ISI 300-1200ms)`() {
    assertEquals(3, SequenceDDAEngine.MIN_SPAN)
    assertEquals(12, SequenceDDAEngine.MAX_SPAN)
    assertEquals(300L, SequenceDDAEngine.MIN_ISI_MS)
    assertEquals(1200L, SequenceDDAEngine.MAX_ISI_MS)
  }

  @Test
  fun `seed anchors span and ISI within bounds`() {
    val engine = SequenceDDAEngine()
    val profile = engine.seed(initialSpan = 4, initialIsiMs = 1200L)
    assertEquals(4, profile.spanLength)
    assertEquals(1200L, profile.interStimulusIntervalMs)
  }

  @Test
  fun `seed clamps out-of-range values to the engine bounds`() {
    val engine = SequenceDDAEngine()
    val profile = engine.seed(initialSpan = 999, initialIsiMs = 5_000L)
    assertEquals(SequenceDDAEngine.MAX_SPAN, profile.spanLength)
    assertEquals(SequenceDDAEngine.MAX_ISI_MS, profile.interStimulusIntervalMs)
  }

  @Test
  fun `fewer than 3 rounds in the window never adjusts difficulty, regardless of outcome`() {
    val engine = SequenceDDAEngine()
    engine.seed(initialSpan = 4, initialIsiMs = 1000L)
    val afterOne = engine.registerRound(wasCorrect = false)
    val afterTwo = engine.registerRound(wasCorrect = false)
    assertEquals(4, afterOne.spanLength)
    assertEquals(1000L, afterOne.interStimulusIntervalMs)
    assertEquals(4, afterTwo.spanLength)
    assertEquals(1000L, afterTwo.interStimulusIntervalMs)
  }

  @Test
  fun `anti-frustration rule -- struggling speeds down ISI before shrinking span`() {
    val engine = SequenceDDAEngine()
    engine.seed(initialSpan = 4, initialIsiMs = 1000L)
    // Ventana de 3 fallos seguidos (0% de precisión, bajo el piso de 0.60) -- ISI debería
    // subir (más lento), NO debería tocarse el span todavía (1000 está lejos del techo 1800).
    engine.registerRound(false)
    engine.registerRound(false)
    val profile = engine.registerRound(false)
    assertEquals("el span no debería bajar mientras el ISI todavía puede subir", 4, profile.spanLength)
    assertTrue("el ISI debería haber subido (más lento) tras la racha de fallos", profile.interStimulusIntervalMs > 1000L)
  }

  @Test
  fun `anti-frustration rule -- span only shrinks once slowing the ISI further would cross the ceiling`() {
    // Nota sobre la ventana: una vez que hay 3 resultados acumulados, CADA llamada
    // siguiente reevalúa una ventana deslizante (no "cada 3 llamadas") -- por eso acá se
    // mira llamada a llamada, no en bloques de 3.
    val engine = SequenceDDAEngine(minIsiMs = 400L, maxIsiMs = 1000L) // techo bajo para forzar el caso en pocas rondas
    engine.seed(initialSpan = 5, initialIsiMs = 700L)
    engine.registerRound(false)
    engine.registerRound(false)
    val afterThird = engine.registerRound(false) // 700*1.2=840, todavía <= 1000 -> solo sube el ISI
    assertEquals(840L, afterThird.interStimulusIntervalMs)
    assertEquals("el span no debería bajar mientras el ISI todavía puede subir sin pasarse del techo", 5, afterThird.spanLength)

    val afterFourth = engine.registerRound(false) // 840*1.2=1008 > 1000 techo -> ya no puede subir más -> baja el span
    assertEquals("con el ISI ya no pudiendo subir más sin cruzar el techo, el span debería bajar", 4, afterFourth.spanLength)
  }

  @Test
  fun `symmetric rule -- success speeds up ISI before growing span`() {
    val engine = SequenceDDAEngine()
    engine.seed(initialSpan = 4, initialIsiMs = 1000L)
    engine.registerRound(true)
    engine.registerRound(true)
    val profile = engine.registerRound(true) // 100% de precisión, sobre el piso de 0.85
    assertEquals("el span no debería subir mientras el ISI todavía puede bajar", 4, profile.spanLength)
    assertTrue("el ISI debería haber bajado (más rápido) tras la racha de aciertos", profile.interStimulusIntervalMs < 1000L)
  }

  @Test
  fun `symmetric rule -- span only grows once speeding the ISI further would cross the floor`() {
    val engine = SequenceDDAEngine(minIsiMs = 400L, maxIsiMs = 1800L)
    engine.seed(initialSpan = 4, initialIsiMs = 500L)
    engine.registerRound(true)
    engine.registerRound(true)
    val afterThird = engine.registerRound(true) // 500*0.85=425, todavía >= 400 -> solo baja el ISI
    assertEquals(425L, afterThird.interStimulusIntervalMs)
    assertEquals("el span no debería subir mientras el ISI todavía puede bajar sin cruzar el piso", 4, afterThird.spanLength)

    val afterFourth = engine.registerRound(true) // 425*0.85=361 < 400 -> ya no puede bajar más -> sube el span
    assertEquals("con el ISI ya no pudiendo bajar más sin cruzar el piso, el span debería subir", 5, afterFourth.spanLength)
  }

  @Test
  fun `mixed recent results within the ZPD band leave difficulty unchanged`() {
    val engine = SequenceDDAEngine()
    engine.seed(initialSpan = 4, initialIsiMs = 1000L)
    engine.registerRound(true)
    engine.registerRound(true)
    val profile = engine.registerRound(false) // 2/3 = 0.667, entre 0.60 y 0.85 -> sin cambios
    assertEquals(4, profile.spanLength)
    assertEquals(1000L, profile.interStimulusIntervalMs)
  }

  @Test
  fun `span never drops below MIN_SPAN even with sustained failure`() {
    val engine = SequenceDDAEngine(minIsiMs = 400L, maxIsiMs = 500L) // rango angosto para llegar rápido al piso
    engine.seed(initialSpan = SequenceDDAEngine.MIN_SPAN, initialIsiMs = 500L)
    var profile = engine.seed(SequenceDDAEngine.MIN_SPAN, 500L)
    repeat(20) {
      engine.registerRound(false)
      engine.registerRound(false)
      profile = engine.registerRound(false)
    }
    assertEquals(SequenceDDAEngine.MIN_SPAN, profile.spanLength)
  }

  @Test
  fun `span never exceeds MAX_SPAN even with sustained success`() {
    val engine = SequenceDDAEngine(minIsiMs = 400L, maxIsiMs = 500L)
    var profile = engine.seed(SequenceDDAEngine.MAX_SPAN, 400L)
    repeat(20) {
      engine.registerRound(true)
      engine.registerRound(true)
      profile = engine.registerRound(true)
    }
    assertEquals(SequenceDDAEngine.MAX_SPAN, profile.spanLength)
  }

  @Test
  fun `distractor count grows with difficulty and stays within bounds`() {
    val easyEngine = SequenceDDAEngine()
    val easyProfile = easyEngine.seed(initialSpan = SequenceDDAEngine.MIN_SPAN, initialIsiMs = SequenceDDAEngine.MAX_ISI_MS)
    assertEquals("en el punto más fácil posible no debería haber distractores", 0, easyProfile.distractorCount)

    val hardEngine = SequenceDDAEngine()
    val hardProfile = hardEngine.seed(initialSpan = SequenceDDAEngine.MAX_SPAN, initialIsiMs = SequenceDDAEngine.MIN_ISI_MS)
    assertTrue("en el punto más difícil posible debería haber distractores", hardProfile.distractorCount > 0)
    assertTrue(hardProfile.distractorCount <= 4)
  }

  @Test
  fun `restarting with seed clears the rolling window from a previous game`() {
    val engine = SequenceDDAEngine()
    engine.seed(initialSpan = 4, initialIsiMs = 1000L)
    engine.registerRound(false)
    engine.registerRound(false)
    // Si la ventana no se limpiara, este único acierto completaría una ventana de
    // "2 fallos + 1 acierto" (33%) heredada de la partida anterior.
    val profileAfterReseed = engine.seed(initialSpan = 4, initialIsiMs = 1000L)
    val afterOneRound = engine.registerRound(true)
    assertEquals(4, profileAfterReseed.spanLength)
    assertEquals(4, afterOneRound.spanLength) // 1 sola ronda -> ventana de 3 todavía no se completa
    assertEquals(1000L, afterOneRound.interStimulusIntervalMs)
  }
}
