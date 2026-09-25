package com.example.model

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class LeaguePromotionTest {
  private fun outcome(gameBefore: Int, gameAfter: Int, globalBefore: Int = 0, globalAfter: Int = 0) =
    RecordOutcome(false, gameBefore, gameAfter, globalBefore, globalAfter)

  @Test
  fun `sin cambio de liga no hay ascenso`() {
    assertNull(outcome(100, 125).promotion("secuencia"))
  }

  @Test
  fun `cruzar 250 en un juego es ascenso a Plata en ese juego`() {
    val p = outcome(240, 255).promotion("secuencia")!!
    assertEquals(RankTier.PLATA, p.tier)
    assertEquals(RankTier.BRONCE, p.previous)
    assertEquals("secuencia", p.gameId)
    assertEquals(255, p.rating)
  }

  @Test
  fun `bajar de liga no se celebra`() {
    assertNull(outcome(255, 240).promotion("secuencia"))
  }

  @Test
  fun `la liga general tiene prioridad sobre la del juego`() {
    val p = outcome(490, 505, globalBefore = 248, globalAfter = 250).promotion("calculo")!!
    assertEquals(RankTier.PLATA, p.tier)
    assertNull(p.gameId)
    assertEquals(250, p.rating)
  }
}
