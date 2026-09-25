package com.example.notification

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ReminderContentTest {
  private fun input(
    streak: Int = 0, playedToday: Boolean = false, completed: Int = 0, week: Int = 0, goal: Int = 4,
    next: String? = "Secuencia Lumínica", gap: Int? = 1, name: String = "Ricardo", day: Int = 0
  ) = ReminderInput(name, streak, playedToday, completed, week, goal, next, gap, day)

  @Test
  fun `sesion completa no avisa`() {
    assertNull(buildReminder(input(playedToday = true, completed = 3)))
  }

  @Test
  fun `sesion a medias dice cuanto falta`() {
    val m = buildReminder(input(playedToday = true, completed = 2))!!
    assertEquals("Ricardo, ya casi", m.title)
    assertTrue(m.text.startsWith("Te falta 1 juego"))
  }

  @Test
  fun `racha en juego y medalla al llegar a un hito`() {
    val normal = buildReminder(input(streak = 4))!!
    assertEquals("Ricardo, tu racha de 4 días te espera", normal.title)
    assertTrue(normal.text.contains("5 días"))
    val milestone = buildReminder(input(streak = 6))!!
    assertEquals("Hoy llegas a 7 días seguidos", milestone.title)
  }

  @Test
  fun `vuelve sin reproches despues de varios dias`() {
    val m = buildReminder(input(gap = 5))!!
    assertEquals("Ricardo, tu camino sigue aquí", m.title)
  }

  @Test
  fun `meta semanal aparece si falta y sin nombre no queda coma`() {
    val m = buildReminder(input(name = "", week = 1, goal = 4, day = 0))!!
    assertEquals("Tu camino de hoy está listo", m.title)
    assertTrue(m.text.contains("Llevas 1 de 4 días esta semana."))
    val done = buildReminder(input(week = 4, goal = 4, day = 0))!!
    assertFalse(done.text.contains("Llevas"))
  }

  @Test
  fun `sin promesas de salud`() {
    val all = (0 until 6).mapNotNull { buildReminder(input(day = it)) } +
      listOfNotNull(buildReminder(input(streak = 3)), buildReminder(input(gap = 9)), buildReminder(input(playedToday = true, completed = 1)))
    all.forEach { m ->
      val t = (m.title + " " + m.text).lowercase()
      listOf("neurona", "cerebro", "reserva cognitiva", "salud", "deterioro").forEach { assertFalse(t, t.contains(it)) }
    }
  }
}
