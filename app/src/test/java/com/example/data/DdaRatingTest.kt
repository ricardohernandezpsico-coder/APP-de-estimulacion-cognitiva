package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class DdaRatingTest {
  @Test
  fun `sin dato previo toma el rating de la partida`() {
    assertEquals(0.42f, blendDdaRating(-1f, 0.42f), 1e-5f)
  }

  @Test
  fun `mezcla 60 40 con lo anterior`() {
    assertEquals(0.6f * 0.8f + 0.4f * 0.4f, blendDdaRating(0.4f, 0.8f), 1e-5f)
  }

  @Test
  fun `un mal dia no tira todo el progreso`() {
    val next = blendDdaRating(0.8f, 0.2f)
    assertTrue("cayó demasiado: $next", next > 0.4f)
  }

  @Test
  fun `valores fuera de rango se acotan a 0 y 1`() {
    assertEquals(1f, blendDdaRating(-1f, 3f), 1e-5f)
    assertEquals(0f, blendDdaRating(0f, -2f), 1e-5f)
  }
}
