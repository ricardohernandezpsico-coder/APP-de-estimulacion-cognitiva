package com.example.games.secuencia

import androidx.compose.ui.graphics.Color
import com.example.games.parejas.HapticPattern
import com.example.games.parejas.SoundEffect
import pro.respawn.flowmvi.api.MVIAction
import pro.respawn.flowmvi.api.MVIIntent
import pro.respawn.flowmvi.api.MVIState

/**
 * Contrato reactivo FlowMVI de "Secuencia Lumínica" -- mismo patrón que
 * [com.example.games.parejas.CardsGameContract] (estados sellados, `Running` con flags
 * en vez de un estado por sub-fase, `internal` para lo que solo necesita `app/src/test`).
 * Reusa [SoundEffect]/[HapticPattern] de `parejas` a propósito: son ya genéricos
 * (FLIP/MATCH/MISMATCH/LEVEL_UP/ROUND_COMPLETE no son específicos de cartas) y
 * [com.example.games.parejas.SensoryFeedbackManager] es la única fuente de audio/haptics
 * sintetizados del proyecto -- duplicar esa maquinaria (síntesis de onda, vibrator,
 * ciclo de vida) para un segundo juego sería la abstracción prematura que se quiere
 * evitar, no al revés.
 */

/** Cada pad tiene su propia frecuencia grave fija (500-1500Hz, ver material de Ricardo)
 *  para el tono "concordante" -- suena igual en la demostración y al tocarlo. */
enum class PadColor(val toneHz: Int, val normalColor: Color, val lightColor: Color, val label: String) {
  AZUL(520, Color(0xFF1D4ED8), Color(0xFF60A5FA), "Azul"),
  AMBAR(800, Color(0xFFB45309), Color(0xFFFBBF24), "Ámbar"),
  VERDE(1080, Color(0xFF047857), Color(0xFF34D399), "Verde"),
  ROSA(1360, Color(0xFFBE123C), Color(0xFFFB7185), "Rosa")
}

enum class SequencePhase { PRESENTING, GET_READY, AWAITING_INPUT }

sealed interface SequenceGameState : MVIState {

  data object Stopped : SequenceGameState

  /** Mismo "3, 2, 1" que Parejas Ocultas entre rondas -- Ricardo ya validó que le da
   *  sensación de dinamismo, tiene sentido que las dos pantallas de juego se sientan
   *  igual de vivas en esta transición. */
  data class Countdown(val secondsLeft: Int, val round: Int, val totalRounds: Int) : SequenceGameState

  data class Running(
    val phase: SequencePhase,
    val sequence: List<PadColor>,
    val userInput: List<PadColor>,
    val highlightedPad: PadColor?,
    val round: Int,
    val totalRounds: Int,
    val correctRounds: Int,
    val spanLength: Int,
    val interStimulusIntervalMs: Long,
    val distractorCount: Int,
    val roundFeedback: String? = null
  ) : SequenceGameState

  data class Finished(val score: Int, val correctRounds: Int, val totalRounds: Int) : SequenceGameState

  data class Error(val error: Throwable) : SequenceGameState
}

sealed interface SequenceGameIntent : MVIIntent {
  data class PadTapped(val pad: PadColor) : SequenceGameIntent
  data object RestartGame : SequenceGameIntent
}

sealed interface SequenceGameAction : MVIAction {
  data class PlayConcordantTone(val frequencyHz: Int) : SequenceGameAction
  data class PlaySoundEffect(val effect: SoundEffect) : SequenceGameAction
  data class TriggerHapticFeedback(val pattern: HapticPattern) : SequenceGameAction
}

/** Antes 5 rondas fijas sin importar el desempeño. Con un motor DDA real que necesita
 *  una ventana de 3 rondas para el primer ajuste, 5 apenas alcanzaba para 1-2 decisiones
 *  de dificultad en toda la partida -- 8 le da margen para adaptar varias veces. */
internal const val TOTAL_ROUNDS = 8
