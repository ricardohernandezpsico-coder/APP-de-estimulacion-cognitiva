package com.example.games.parejas

import com.example.model.AgeBand

/**
 * Piloto de perfiles por edad (20-sep): Ricardo compartió material sobre calibrar el DDA
 * (y la accesibilidad de la UI) según el perfil del usuario en vez de un solo valor
 * global para todos. Insight concreto que motivó esto: el spec de 20mm/3.2-12.5mm que
 * seguimos al pie de la letra en la primera pasada de este juego es EXACTAMENTE el
 * recomendado para adultos mayores/deterioro cognitivo -- pero el mismo día, jugándolo,
 * Ricardo pidió achicarlo a 15mm/1.5-6mm porque le resultaba "muy separado". No es una
 * contradicción: son dos perfiles distintos. Sin este dato no se puede tener las dos
 * cosas bien a la vez.
 *
 * Piloto en "Parejas Ocultas" únicamente (decisión explícita de Ricardo: validar acá
 * antes de tocar los otros 8 juegos) -- ver `CardsGameContainer` (pesos del DDA y
 * política de timeout) y `ParejasGame.kt` (tamaño de carta/espaciado).
 */
data class DdaUserProfileConfig(
  /** Peso de la precisión en `VisualWorkingMemoryDDA` (w1 del material de referencia). */
  val weightAccuracy: Float,
  /** Peso del tiempo de reacción (w2). Mayor en adultos jóvenes: recompensa velocidad
   *  además de precisión. Menor en adultos mayores: la lentitud psicomotora no debería
   *  penalizar tanto como un error real. */
  val weightReactionTime: Float,
  /** Piso de tiempo de vista previa (antes de que las cartas se den vuelta) -- 500ms es
   *  exigente para cualquiera, pero mucho más para alguien con deterioro cognitivo. */
  val minPreviewExposureMs: Float,
  /** Si `false`, quedarse sin tiempo en Modo Reto NO termina toda la partida de una --
   *  entra al mismo sistema de "3 fallas" que un tablero mal resuelto (baja de nivel,
   *  repite, recién ahí termina). El material es explícito: "prohibido expiración
   *  estricta, causa ansiedad" para adultos mayores y niños. */
  val allowStrictTimeouts: Boolean,
  /** Tamaño ideal de carta y espaciado entre cartas, en mm -- ver [ParejasGame.kt] para
   *  cómo se combinan con el piso de accesibilidad real de Android (48dp), que NUNCA se
   *  cruza sea cual sea el perfil. */
  val idealTouchTargetMm: Float,
  val minSpacingMm: Float,
  val maxSpacingMm: Float
)

internal fun ddaProfileConfigFor(ageBand: AgeBand): DdaUserProfileConfig = when (ageBand) {
  AgeBand.SENIOR -> DdaUserProfileConfig(
    weightAccuracy = 0.85f,
    weightReactionTime = 0.15f,
    minPreviewExposureMs = 1500f,
    allowStrictTimeouts = false,
    idealTouchTargetMm = 20f,
    minSpacingMm = 3.2f,
    maxSpacingMm = 12.5f
  )
  AgeBand.ADULT -> DdaUserProfileConfig(
    weightAccuracy = 0.65f,
    weightReactionTime = 0.35f,
    minPreviewExposureMs = 500f,
    allowStrictTimeouts = true,
    idealTouchTargetMm = 15f,
    minSpacingMm = 1.5f,
    maxSpacingMm = 6f
  )
  AgeBand.UNDER_18 -> DdaUserProfileConfig(
    weightAccuracy = 0.60f,
    weightReactionTime = 0.40f,
    minPreviewExposureMs = 800f,
    allowStrictTimeouts = false,
    idealTouchTargetMm = 17f,
    minSpacingMm = 2.5f,
    maxSpacingMm = 8f
  )
}
