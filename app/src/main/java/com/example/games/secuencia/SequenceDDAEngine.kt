package com.example.games.secuencia

/**
 * Motor DDA multidimensional de "Secuencia Lumínica", a partir del material que
 * compartió Ricardo. Regula DOS ejes independientes con datos reales (longitud de
 * secuencia = `spanLength`, velocidad de presentación = `interStimulusIntervalMs`) y
 * deriva un tercero (`distractorCount`) de cuánto se avanzó en esos dos -- el material
 * declaraba "regula tres dimensiones independientes" pero el algoritmo de ejemplo que
 * incluía solo tocaba dos (el campo `distractorLevel` nunca se actualizaba), así que acá
 * sí se resuelve de verdad en vez de dejarlo como campo muerto.
 *
 * Regla clave (tal cual el material, es un principio sólido): ante error, primero se
 * ralentiza la presentación (ISI sube) y solo si el ISI YA está en su techo (más lento
 * posible) se acorta la secuencia. Ante racha de éxito, simétricamente: primero se
 * acelera el ISI, y solo si ya está en su piso (más rápido posible) se alarga la
 * secuencia. Evita el "castigo brusco" de bajar de nivel a la primera duda.
 *
 * A diferencia de [com.example.games.parejas.VisualWorkingMemoryDDA] (controlador
 * incremental con Z-score de tiempo de reacción ensayo a ensayo), acá se usa una
 * ventana deslizante de los últimos 3 resultados (aciertos/fallos), tal cual especifica
 * el material -- es una estrategia distinta a propósito, no una inconsistencia: Secuencia
 * Lumínica evalúa una secuencia COMPLETA por ronda (un solo booleano acierto/fallo), no
 * ensayos discretos como cada volteo de carta en Parejas Ocultas, así que no hay un RT
 * "por ensayo" natural para un Z-score -- una ventana de rondas recientes es lo que
 * corresponde a esta granularidad.
 */
internal class SequenceDDAEngine(
  private val minSpan: Int = MIN_SPAN,
  private val maxSpan: Int = MAX_SPAN,
  private val minIsiMs: Long = MIN_ISI_MS,
  private val maxIsiMs: Long = MAX_ISI_MS,
  private val successThreshold: Float = SUCCESS_THRESHOLD,
  private val strugglingThreshold: Float = STRUGGLING_THRESHOLD
) {

  private val recentResults = ArrayDeque<Boolean>()

  var spanLength: Int = minSpan
    private set
  var interStimulusIntervalMs: Long = maxIsiMs
    private set

  data class DifficultyProfile(
    val spanLength: Int,
    val interStimulusIntervalMs: Long,
    val distractorCount: Int
  )

  /** Ancla el motor a un punto de partida (nivel/maestría elegidos fuera de este motor,
   *  ver [SequenceGameContainer.seedSpan]/`seedIsiMs`) y limpia la ventana de resultados
   *  -- una partida nueva no debería arrastrar la racha/rachas de errores de la anterior. */
  fun seed(initialSpan: Int, initialIsiMs: Long): DifficultyProfile {
    spanLength = initialSpan.coerceIn(minSpan, maxSpan)
    interStimulusIntervalMs = initialIsiMs.coerceIn(minIsiMs, maxIsiMs)
    recentResults.clear()
    return currentProfile()
  }

  /** Registra el resultado (acierto/fallo) de la ronda que acaba de terminar y devuelve
   *  el perfil de dificultad para la SIGUIENTE ronda. Con menos de [WINDOW_SIZE]
   *  resultados todavía no hay ventana completa -> no se ajusta nada (igual que el
   *  calentamiento de Parejas Ocultas antes de confiar en un Z-score).
   *
   *  Ojo: una vez que la ventana tiene [WINDOW_SIZE] resultados, es una ventana
   *  DESLIZANTE de verdad -- cada ronda siguiente vuelve a evaluar los últimos 3
   *  resultados (no "cada 3 rondas"). Esto significa que, ante una racha sostenida, el
   *  ISI puede seguir moviéndose ronda a ronda hasta chocar con su piso/techo, y recién
   *  ahí el span cambia -- ver los tests de `speedUpThenGrow`/`slowDownThenShrink`. */
  fun registerRound(wasCorrect: Boolean): DifficultyProfile {
    recentResults.addLast(wasCorrect)
    if (recentResults.size > WINDOW_SIZE) recentResults.removeFirst()

    if (recentResults.size >= WINDOW_SIZE) {
      val accuracy = recentResults.count { it } / WINDOW_SIZE.toFloat()
      when {
        accuracy >= successThreshold -> speedUpThenGrow()
        accuracy < strugglingThreshold -> slowDownThenShrink()
        else -> Unit // Zona de Desarrollo Próximo -- sin cambios
      }
    }
    return currentProfile()
  }

  private fun speedUpThenGrow() {
    val faster = (interStimulusIntervalMs * ISI_SPEED_UP_FACTOR).toLong()
    if (faster >= minIsiMs) {
      interStimulusIntervalMs = faster
    } else if (spanLength < maxSpan) {
      spanLength += 1
      interStimulusIntervalMs = COMFORT_ISI_AFTER_SPAN_CHANGE_MS.coerceIn(minIsiMs, maxIsiMs)
    } else {
      interStimulusIntervalMs = minIsiMs // techo de span Y de velocidad ya alcanzados
    }
  }

  private fun slowDownThenShrink() {
    val slower = (interStimulusIntervalMs * ISI_SLOW_DOWN_FACTOR).toLong()
    if (slower <= maxIsiMs) {
      interStimulusIntervalMs = slower
    } else if (spanLength > minSpan) {
      spanLength -= 1
      interStimulusIntervalMs = COMFORT_ISI_AFTER_SPAN_CHANGE_MS.coerceIn(minIsiMs, maxIsiMs)
    } else {
      interStimulusIntervalMs = maxIsiMs // piso de span Y de velocidad ya alcanzados
    }
  }

  /** Ambos ejes normalizados 0..1 y promediados -- un solo "cuánto está costando esto"
   *  que alimenta la densidad de distractores. No es D(t) de Parejas Ocultas (ese es un
   *  controlador con estado propio); acá es una lectura directa de dónde están
   *  `spanLength`/`interStimulusIntervalMs` ahora mismo. */
  private fun difficultyFraction(): Float {
    val spanFraction = (spanLength - minSpan).toFloat() / (maxSpan - minSpan).coerceAtLeast(1)
    val speedFraction = (maxIsiMs - interStimulusIntervalMs).toFloat() / (maxIsiMs - minIsiMs).coerceAtLeast(1)
    return ((spanFraction + speedFraction) / 2f).coerceIn(0f, 1f)
  }

  private fun distractorCountFor(): Int = (difficultyFraction() * MAX_DISTRACTORS).toInt().coerceIn(0, MAX_DISTRACTORS)

  private fun currentProfile() = DifficultyProfile(
    spanLength = spanLength,
    interStimulusIntervalMs = interStimulusIntervalMs,
    distractorCount = distractorCountFor()
  )

  companion object {
    // Rangos tal cual el "Master Prompt" que compartió Ricardo (guía de refactorización
    // específica de este juego, más precisa que el material genérico del 20-sep): span
    // 3-12, ISI 300-1200ms. Antes eran 2-9 / 400-1800ms (un punto de partida propio sin
    // una cita concreta detrás) -- se reemplazan por los del material citado.
    const val MIN_SPAN = 3
    const val MAX_SPAN = 12
    const val MIN_ISI_MS = 300L
    const val MAX_ISI_MS = 1200L
    const val WINDOW_SIZE = 3
    const val SUCCESS_THRESHOLD = 0.85f
    const val STRUGGLING_THRESHOLD = 0.60f
    private const val ISI_SPEED_UP_FACTOR = 0.85f
    private const val ISI_SLOW_DOWN_FACTOR = 1.20f
    private const val COMFORT_ISI_AFTER_SPAN_CHANGE_MS = 700L
    // Deliberadamente bajo (no 6, como los puntos de fondo de Parejas Ocultas): el
    // material es explícito en evitar "animaciones invasivas... que compitan por la
    // memoria de trabajo" -- acá los distractores son ruido AMBIENTE detrás de la
    // grilla de pads (ver DistractorGlow en SecuenciaGame.kt), no elementos que haya
    // que identificar o ignorar activamente.
    private const val MAX_DISTRACTORS = 4
  }
}
