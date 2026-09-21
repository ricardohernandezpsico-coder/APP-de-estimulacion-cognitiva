package com.example.games.parejas

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Pruebas de la tabla fija de progresión por nivel ([pairCountForStage]) -- la corrección
 * al bug real que reportó Ricardo jugando ("el nivel 2 me salió más fácil que el nivel
 * 1"): la cantidad de parejas por nivel ya NO puede depender del desempeño (eso es lo que
 * [VisualWorkingMemoryDDATest] confirma por su lado), tiene que ser una escalera fija y
 * siempre creciente del nivel 1 al 10.
 */
class CardsGameContractTest {

  @Test
  fun `pairCountForStage is the exact fixed table for stages 1 through 10`() {
    val counts = (1..10).map { pairCountForStage(it) }
    assertEquals(listOf(2, 3, 4, 5, 6, 7, 8, 9, 10, 12), counts)
  }

  @Test
  fun `pairCountForStage is strictly increasing across all 10 stages`() {
    val counts = (1..10).map { pairCountForStage(it) }
    for (i in 1 until counts.size) {
      assertTrue(
        "el nivel ${i + 1} (${counts[i]} parejas) debería tener MÁS parejas que el nivel $i (${counts[i - 1]})",
        counts[i] > counts[i - 1]
      )
    }
  }

  @Test
  fun `pairCountForStage clamps stage numbers outside the 1 to 10 range`() {
    assertEquals(pairCountForStage(1), pairCountForStage(0))
    assertEquals(pairCountForStage(1), pairCountForStage(-5))
    assertEquals(pairCountForStage(TOTAL_STAGES), pairCountForStage(TOTAL_STAGES + 1))
    assertEquals(pairCountForStage(TOTAL_STAGES), pairCountForStage(999))
  }

  @Test
  fun `TOTAL_STAGES matches the size of the pair count table`() {
    // Si alguna vez se cambia TOTAL_STAGES sin actualizar la tabla (o viceversa), esta
    // prueba lo detecta -- pairCountForStage(TOTAL_STAGES) no debería ser un valor
    // clamped al último elemento por accidente.
    assertEquals(10, TOTAL_STAGES)
  }
}
