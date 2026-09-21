package com.example.games.parejas

import com.example.model.AgeBand
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Pruebas del piloto de perfiles por edad ([ddaProfileConfigFor]). El punto de partida
 * fue el material que compartió Ricardo sobre perfiles clínicos (adulto mayor/adulto/
 * niño) -- estas pruebas no validan esos números como "verdad clínica" (los pesos se
 * adaptaron con criterio propio a la escala 0..1 del proyecto, ver nota en
 * `DdaUserProfile.kt`), sino las INVARIANTES que tienen que cumplirse pase lo que pase:
 * pesos complementarios, adultos mayores más protegidos que adultos, piso de
 * accesibilidad nunca cruzado.
 */
class DdaUserProfileTest {

  @Test
  fun `accuracy and reaction-time weights always add up to 1 for every profile`() {
    AgeBand.entries.forEach { band ->
      val config = ddaProfileConfigFor(band)
      assertEquals(
        "los pesos de $band deberían sumar 1 (son las dos únicas fuentes de la fórmula de D(t))",
        1f,
        config.weightAccuracy + config.weightReactionTime,
        0.001f
      )
    }
  }

  @Test
  fun `senior profile weighs accuracy more and speed less than the adult profile`() {
    val senior = ddaProfileConfigFor(AgeBand.SENIOR)
    val adult = ddaProfileConfigFor(AgeBand.ADULT)
    assertTrue(senior.weightAccuracy > adult.weightAccuracy)
    assertTrue(senior.weightReactionTime < adult.weightReactionTime)
  }

  @Test
  fun `senior and pediatric profiles disable strict timeouts, adult does not`() {
    assertFalse(
      "el material citado es explícito: timeouts estrictos generan ansiedad en adultos mayores",
      ddaProfileConfigFor(AgeBand.SENIOR).allowStrictTimeouts
    )
    assertFalse(ddaProfileConfigFor(AgeBand.UNDER_18).allowStrictTimeouts)
    assertTrue(ddaProfileConfigFor(AgeBand.ADULT).allowStrictTimeouts)
  }

  @Test
  fun `senior profile has a higher preview exposure floor than the adult profile`() {
    val senior = ddaProfileConfigFor(AgeBand.SENIOR)
    val adult = ddaProfileConfigFor(AgeBand.ADULT)
    assertTrue(senior.minPreviewExposureMs > adult.minPreviewExposureMs)
  }

  @Test
  fun `senior profile has larger touch targets and spacing than the adult profile`() {
    // Esta es la invariante que resuelve la aparente contradicción del mismo día: el
    // spec de 20mm/3.2-12.5mm no es "el correcto" en abstracto, es el correcto para
    // SENIOR -- ADULT necesita algo más chico, como pidió Ricardo jugándolo él mismo.
    val senior = ddaProfileConfigFor(AgeBand.SENIOR)
    val adult = ddaProfileConfigFor(AgeBand.ADULT)
    assertTrue(senior.idealTouchTargetMm > adult.idealTouchTargetMm)
    assertTrue(senior.minSpacingMm > adult.minSpacingMm)
    assertTrue(senior.maxSpacingMm > adult.maxSpacingMm)
  }

  @Test
  fun `spacing range is internally consistent for every profile`() {
    AgeBand.entries.forEach { band ->
      val config = ddaProfileConfigFor(band)
      assertTrue(
        "$band: minSpacingMm (${config.minSpacingMm}) no debería superar maxSpacingMm (${config.maxSpacingMm})",
        config.minSpacingMm <= config.maxSpacingMm
      )
    }
  }

  @Test
  fun `every profile's ideal touch target clears Android's real accessibility floor`() {
    // 48dp equivalen a ~7.6mm -- ningún perfil debería pedir un ideal por debajo de eso,
    // sea cual sea el criterio clínico (el piso real lo aplica ParejasGame.kt aparte,
    // esto solo confirma que el IDEAL declarado ya nace por encima de un valor sensato).
    AgeBand.entries.forEach { band ->
      val config = ddaProfileConfigFor(band)
      assertTrue("$band: idealTouchTargetMm=${config.idealTouchTargetMm}", config.idealTouchTargetMm >= 7.6f)
    }
  }
}
