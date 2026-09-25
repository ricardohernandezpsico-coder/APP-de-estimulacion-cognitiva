package com.example.data

import com.example.model.GamePlayResult
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class AchievementsTest {
  private val day = 24L * 60 * 60 * 1000
  private val base = 1_700_000_000_000L

  private fun game(id: String, dayOffset: Int, score: Int = 70, timed: Boolean = false) =
    GamePlayResult(gameId = id, score = score, correctAnswers = 5, totalTrials = 10, timed = timed, level = 1, timestamp = base + dayOffset * day)

  @Test
  fun `racha mas larga cuenta dias seguidos`() {
    assertEquals(0, longestStreak(emptyList()))
    assertEquals(3, longestStreak(listOf(1L, 2L, 3L, 5L, 6L)))
    assertEquals(1, longestStreak(listOf(4L, 4L)))
  }

  @Test
  fun `sin partidas no hay logros`() {
    assertTrue(Achievements.unlocked(computeAchievementStats(emptyList(), emptyMap())).isEmpty())
  }

  @Test
  fun `primera partida y racha de 3`() {
    val history = (0 until 3).map { game("secuencia", it) }
    val got = Achievements.unlocked(computeAchievementStats(history, emptyMap()))
    assertTrue("primer_paso" in got)
    assertTrue("racha_3" in got)
    assertFalse("racha_7" in got)
  }

  @Test
  fun `puntaje, dia completo, reto y ligas`() {
    val history = listOf(game("secuencia", 0, 100, timed = true), game("parejas", 0), game("stroop", 0))
    val got = Achievements.unlocked(computeAchievementStats(history, mapOf("secuencia" to 520)))
    assertTrue("perfecto" in got)
    assertTrue("brillante" in got)
    assertTrue("dia_completo" in got)
    assertTrue("liga_plata" in got)
    assertTrue("liga_oro" in got)
    assertFalse("liga_platino" in got)
    assertFalse("general_oro" in got) // 520 / 9 juegos = 57: la liga general sigue en Bronce
    assertFalse("reto_10" in got)
  }

  @Test
  fun `progreso de un logro bloqueado`() {
    val stats = computeAchievementStats((0 until 4).map { game("secuencia", it) }, emptyMap())
    assertEquals(4 to 7, Achievements.byId("racha_7")!!.progress(stats))
  }

  @Test
  fun `registro de logros ida y vuelta`() {
    val unlocks = mapOf("primer_paso" to 10L, "racha_3" to 20L)
    assertEquals(unlocks, decodeUnlocks(encodeUnlocks(unlocks)))
    assertTrue(decodeUnlocks("basura").isEmpty())
  }
}
