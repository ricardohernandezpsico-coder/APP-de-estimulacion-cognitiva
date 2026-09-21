package com.example.games

import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Prueba de regresión para el bug real que reportó Ricardo jugando: en Nivel 7 con
 * perfil "65 o más" (adulto mayor), las cartas se veían muy pequeñas. Causa real:
 * tamaño de carta y espaciado se resolvían por separado -- el espaciado generoso del
 * perfil adulto mayor (hasta 12.5mm) por sí solo ya casi llenaba el ancho disponible en
 * una grilla densa, exprimiendo la carta hasta el piso de accesibilidad (48dp), lo
 * opuesto a la intención del perfil. Ver [resolveCardLayout].
 */
class ParejasGameLayoutTest {

  // Perfil "65 o más" (ver DdaUserProfileConfig.SENIOR): ideal 20mm, espaciado 3.2-12.5mm.
  private val seniorIdealDp = 125.98f // 20mm
  private val seniorMinSpacingDp = 20.16f // 3.2mm
  private val seniorMaxSpacingDp = 78.74f // 12.5mm

  @Test
  fun `senior profile at a dense grid (level 7, 4x4) no longer collapses cards to the bare accessibility floor`() {
    // Ancho/alto típicos de un teléfono para el área del tablero (dp), Nivel 7 = 8
    // parejas = grilla 4x4 (ver CardsGameContractTest / VisualWorkingMemoryDDATest).
    val (cardSize, spacing) = resolveCardLayout(
      columns = 4,
      rows = 4,
      availableWidthDp = 340f,
      availableHeightDp = 550f,
      idealTargetDp = seniorIdealDp,
      minSpacingDp = seniorMinSpacingDp,
      maxSpacingDp = seniorMaxSpacingDp
    )

    assertTrue(
      "la carta (${cardSize.value}dp) debería quedar bastante por encima del piso de 48dp -- si no, el espaciado le está comiendo el espacio",
      cardSize.value > 60f
    )
    assertTrue(
      "el espaciado (${spacing.value}dp) debería haber cedido hacia el piso del perfil (${seniorMinSpacingDp}dp) para liberarle espacio a la carta",
      spacing.value <= seniorMinSpacingDp + 0.5f
    )
  }

  @Test
  fun `senior profile at a loose grid (level 1, 2x2) keeps the generous preferred spacing`() {
    val (cardSize, spacing) = resolveCardLayout(
      columns = 2,
      rows = 2,
      availableWidthDp = 340f,
      availableHeightDp = 550f,
      idealTargetDp = seniorIdealDp,
      minSpacingDp = seniorMinSpacingDp,
      maxSpacingDp = seniorMaxSpacingDp
    )

    // Con solo 2 columnas hay espacio de sobra -- la carta debería llegar a su tamaño
    // ideal completo y el espaciado no debería haber tenido que ceder nada.
    assertTrue(cardSize.value >= seniorIdealDp - 0.5f)
    assertTrue(spacing.value >= seniorMaxSpacingDp - 0.5f)
  }

  @Test
  fun `card size never drops below Android's real 48dp accessibility floor, no matter how dense the grid`() {
    val (cardSize, _) = resolveCardLayout(
      columns = 6,
      rows = 6,
      availableWidthDp = 340f,
      availableHeightDp = 550f,
      idealTargetDp = seniorIdealDp,
      minSpacingDp = seniorMinSpacingDp,
      maxSpacingDp = seniorMaxSpacingDp
    )
    assertTrue(cardSize.value >= 48f)
  }
}
