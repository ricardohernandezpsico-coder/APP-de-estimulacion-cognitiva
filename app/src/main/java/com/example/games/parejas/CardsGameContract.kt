package com.example.games.parejas

import pro.respawn.flowmvi.api.MVIAction
import pro.respawn.flowmvi.api.MVIIntent
import pro.respawn.flowmvi.api.MVIState

/**
 * Contrato reactivo de "Parejas Ocultas" sobre FlowMVI real (`pro.respawn.flowmvi`).
 * Reemplaza al contrato nativo de la iteración anterior (`ParejasOcultasState`/Intent/
 * Action) ahora que el motor pasó a ser un [pro.respawn.flowmvi.api.Container] de
 * verdad — ver [CardsGameContainer].
 */

data class MemoryCardUi(
  val id: Int,
  val pairKey: String,
  val symbol: String,
  val isFaceUp: Boolean = false,
  val isMatched: Boolean = false
)

enum class SoundEffect { FLIP, MATCH, MISMATCH, LEVEL_UP, ROUND_COMPLETE }
enum class HapticPattern { CORRECT, INCORRECT, LEVEL_UP }
enum class CountdownReason { START, ADVANCE, DEMOTED, RETRY }

sealed interface CardsGameState : MVIState {

  data object Stopped : CardsGameState

  /** Precarga (plugin `asyncCache`/`cache`): construir el mazo + seleccionar el banco
   *  de símbolos según la interferencia perceptual vigente, antes de mostrar nada. */
  data class Loading(val progress: Float) : CardsGameState

  /**
   * Pantalla de "prepárate" entre niveles (y al arrancar la partida). Pedido explícito
   * de Ricardo tras jugarlo: "se ve muy estática" -- al completar un tablero, la partida
   * ya no termina ahí, pasa DE INMEDIATO al siguiente nivel con esta cuenta 3-2-1 como
   * transición, hasta [TOTAL_STAGES] niveles. [reason] distingue arranque/avance normal
   * de una bajada o repetición de nivel por fallas (ver [CardsGameContainer.handleStageFailure]),
   * para que el cambio de nivel hacia ATRÁS se lea como mecánica del juego y no como un bug.
   */
  data class Countdown(
    val secondsLeft: Int,
    val stage: Int,
    val totalStages: Int,
    val reason: CountdownReason = CountdownReason.ADVANCE
  ) : CardsGameState

  data class Running(
    val cards: List<MemoryCardUi>,
    val gridDimensions: Pair<Int, Int>, // (columnas, filas)
    val score: Int,
    val matchedPairs: Int,
    val totalPairs: Int,
    val attempts: Int,
    val isMemorizing: Boolean,
    val memorizeMillisLeft: Long,
    /** Nivel actual dentro de esta partida (1..[TOTAL_STAGES]) -- distinto del "Nivel N"
     *  de dificultad adaptativa elegido en el diálogo de la biblioteca, este es el
     *  contador de tableros sucesivos DENTRO de la sesión. */
    val stage: Int = 1,
    val totalStages: Int = TOTAL_STAGES,
    /** Candado de turno visible en UI. La serialización real de golpes casi
     *  simultáneos la garantiza el propio Store de FlowMVI (procesa un MVIIntent a
     *  la vez desde su canal interno) — este flag es la señal para deshabilitar el
     *  `clickable` de las cartas mientras se resuelve la pareja anterior, no el
     *  mecanismo de seguridad en sí. */
    val isTurnLocked: Boolean,
    val currentStreak: Int = 0,
    val difficultyIndex: Float,
    val distractorCount: Int,
    val distractorOpacity: Float,
    val distractorSeed: Int,
    val timeLeftSeconds: Int? = null,
    /** Presupuesto inicial de [timeLeftSeconds] (Modo Reto) — sin esto la barra de
     *  tiempo de la UI no tiene contra qué calcular el porcentaje restante. */
    val totalTimeSeconds: Int? = null,
    /** Cronómetro de tiempo de resolución (desde que termina la memorización, NO desde
     *  que arranca la ronda) — pedido explícito de Ricardo tras jugarlo: "el tiempo en
     *  las actividades también es una métrica relevante", incluso en Precisión (sin
     *  reloj). Alimenta la barra de ritmo en la UI y el bono de velocidad en
     *  [CardsGameContainer.finishRound]. */
    val elapsedMs: Long = 0L
  ) : CardsGameState

  data class Finished(val score: Int, val matchedPairs: Int, val attempts: Int) : CardsGameState

  data class Error(val error: Throwable) : CardsGameState
}

sealed interface CardsGameIntent : MVIIntent {
  data class CardFlipped(val id: Int) : CardsGameIntent
  data object TimerTicked : CardsGameIntent
  data object RestartGame : CardsGameIntent
}

sealed interface CardsGameAction : MVIAction {
  data class PlaySoundEffect(val effect: SoundEffect) : CardsGameAction
  data class TriggerHapticFeedback(val pattern: HapticPattern) : CardsGameAction
  data object ShowLevelUp : CardsGameAction
}

/** Ritmo de referencia para la barra de tiempo y el bono de velocidad en Precisión: 4s
 *  por pareja es un punto de partida razonable (ronda de 4 parejas "a buen ritmo" en
 *  16s), compartido entre [CardsGameContainer] (bono) y `ParejasGame.kt` (barra) para
 *  que ambos midan lo mismo. */
internal const val EXPECTED_MS_PER_PAIR = 4000L

/** Cuántos tableros sucesivos arma una sola partida antes de mostrar el resultado final.
 *  Ricardo pidió "así sucesivamente, incluso 10 niveles" -- se implementa como tope fijo
 *  (no "sin techo" como `masteryStreak`) para que Precisión, que no tiene otra forma de
 *  terminar, siga teniendo un cierre claro; si se prefiere sin límite, es cuestión de
 *  sacar el chequeo en `CardsGameContainer.finishRound`. */
internal const val TOTAL_STAGES = 10

/**
 * Cuántas parejas tiene el tablero de cada nivel (1..[TOTAL_STAGES]) -- una escalera FIJA
 * y estrictamente creciente, a pedido explícito de Ricardo tras notar que el nivel 2 le
 * había salido más fácil (menos parejas) que el nivel 1: antes la cantidad de parejas
 * salía de `VisualWorkingMemoryDDA` (D(t), que puede BAJAR si el desempeño fue malo), acá
 * en cambio depende solo del número de nivel, nunca del desempeño -- D(t) sigue afinando
 * el resto (vista previa, interferencia, distractores), pero la progresión "empieza fácil,
 * termina difícil" queda garantizada.
 */
private val PAIR_COUNTS_BY_STAGE = intArrayOf(2, 3, 4, 5, 6, 7, 8, 9, 10, 12)

internal fun pairCountForStage(stage: Int): Int =
  PAIR_COUNTS_BY_STAGE[(stage - 1).coerceIn(0, PAIR_COUNTS_BY_STAGE.lastIndex)]

/**
 * Sistema de "3 fallas" pedido por Ricardo: fallar [MISMATCHES_TO_FAIL_STAGE] parejas en
 * el tablero actual corta ese tablero ahí mismo (no espera a que se termine) y cuenta como
 * una falla de nivel. La PRIMERA falla de nivel baja un nivel; la SEGUNDA repite ese mismo
 * nivel (ya bajado) sin bajar más; la TERCERA termina la partida. Ver
 * `CardsGameContainer.handleStageFailure`.
 */
internal const val MISMATCHES_TO_FAIL_STAGE = 3
internal const val STAGE_FAILURES_BEFORE_GAME_OVER = 3

/** Bancos de símbolos graduados por similitud perceptual (0 = muy distintos entre sí,
 *  3 = máxima interferencia visual/semántica) — mismo criterio que la iteración
 *  anterior, ahora impulsado por `interferenceLevel` de [VisualWorkingMemoryDDA]. */
internal object SymbolBank {
  private val TIERS: List<List<String>> = listOf(
    listOf("🐶", "🍕", "🚗", "⭐", "🌈", "🎸", "⚽", "🎁"),
    listOf("🦊", "🐼", "🦁", "🐵", "🐺", "🐹", "🐰", "🐨", "🐸", "🐙"),
    listOf("🍎", "🍒", "🍓", "🍅", "🍇", "🍉", "🍑", "🍊"),
    listOf("😀", "😃", "😄", "😁", "😆", "🙂", "😊", "😇")
  )

  fun select(pairCount: Int, interferenceLevel: Int, random: kotlin.random.Random = kotlin.random.Random.Default): List<String> {
    val level = interferenceLevel.coerceIn(0, TIERS.lastIndex)
    val pool = mutableListOf<String>()
    for (tier in level downTo 0) {
      if (pool.size >= pairCount) break
      pool += TIERS[tier].shuffled(random)
    }
    return pool.distinct().take(pairCount)
  }
}
