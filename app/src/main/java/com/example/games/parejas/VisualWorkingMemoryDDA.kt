package com.example.games.parejas

import kotlin.math.ceil
import kotlin.math.sqrt

/**
 * Motor matemático del DDA multidimensional de "Parejas Ocultas" (Memoria de Trabajo
 * Visual). A diferencia de [com.example.games.parejas]'s versión anterior (funciones
 * puras sin estado), esta clase SÍ mantiene estado propio a propósito: el Z-score de
 * tiempo de reacción necesita una media y desvío histórico *personales* del usuario, que
 * solo tienen sentido si se acumulan ensayo a ensayo dentro de una instancia viva.
 *
 * D(t) ∈ [0, 1] es un índice de dificultad continuo, actualizado como un controlador
 * incremental (no un umbral fijo): cada ensayo empuja D(t) hacia arriba o hacia abajo
 * según qué tan lejos está el desempeño reciente del "punto dulce" de Lumosity (~75-85%
 * de precisión) — la misma filosofía de mantener al usuario en la zona de desafío
 * sostenible, no en el techo ni en el piso.
 *
 * Ojo: D(t) YA NO decide la cantidad de parejas del tablero. Ricardo notó jugando que el
 * nivel 2 le salió más fácil (menos parejas) que el nivel 1 -- pasaba porque el tamaño de
 * grilla dependía de D(t), que puede bajar si el desempeño fue malo, rompiendo la
 * progresión "nivel 1 fácil -> nivel 10 difícil" que se espera de una escalera de niveles
 * fija. Ahora la cantidad de parejas es puramente función del nivel (1..10, tabla en
 * [CardsGameContract.pairCountForStage]), y D(t) solo sigue afinando vista previa,
 * interferencia y distractores -- el "ajuste fino" dentro de cada nivel, no la estructura
 * general de la partida.
 */
class VisualWorkingMemoryDDA(
  private val targetAccuracy: Float = 0.8f,
  private val stepSize: Float = 0.08f,
  /** Antes constantes fijas (`W_ACCURACY`/`W_REACTION_TIME`) -- ahora parámetros para
   *  que cada [DdaUserProfileConfig] pueda pesar distinto precisión vs. velocidad (ver
   *  `CardsGameContainer`, piloto de perfiles por edad). */
  private val weightAccuracy: Float = DEFAULT_WEIGHT_ACCURACY,
  private val weightReactionTime: Float = DEFAULT_WEIGHT_REACTION_TIME,
  /** Piso de `previewExposureMsFor` -- antes fijo en 500ms para todos, ahora depende del
   *  perfil (un adulto mayor no debería llegar nunca a una vista previa tan corta). */
  private val previewFloorMs: Float = DEFAULT_PREVIEW_MIN_MS
) {

  // --- Media/desvío histórico de RT, calculado en línea (Welford) ---
  // No se guarda el historial completo de RTs: alcanza con contar cuántos ensayos hubo y
  // llevar la media y la suma de cuadrados de las diferencias (M2), que es la forma
  // numéricamente estable de actualizar mean/varianza sin reprocesar todo cada vez.
  private var trialCount: Int = 0
  private var rtMean: Double = 0.0
  private var rtM2: Double = 0.0

  var difficultyIndex: Float = 0f
    private set

  /**
   * Resultado de un ensayo: qué tan preciso fue (comisión/omisión) y cuánto tardó.
   * `wasOmission = true` cuando el usuario dejó pasar el tiempo de turno sin
   * completar el intento (ver `timeLeftSeconds` en el Container) — un tipo de error
   * DISTINTO al de comisión (elegir la carta equivocada), porque en la literatura de
   * funciones ejecutivas ambos pesan distinto: omisión suele reflejar desatención,
   * comisión suele reflejar impulsividad o fallo real de memoria.
   */
  data class TrialResult(
    val wasCorrect: Boolean,
    val wasOmission: Boolean,
    val reactionTimeMs: Long
  )

  data class DifficultyProfile(
    val difficultyIndex: Float,
    val gridDimensions: Pair<Int, Int>, // (columnas, filas)
    val pairCount: Int,
    val previewExposureMs: Long,
    val distractorCount: Int,
    val distractorOpacity: Float,
    val interferenceLevel: Int
  )

  /**
   * Actualiza D(t) con un nuevo ensayo y devuelve el perfil de dificultad resultante
   * (todas las dimensiones ya resueltas a valores concretos, listas para que el
   * Container arme la siguiente ronda).
   *
   * Fórmula: D(t) = D(t-1) + stepSize × ( wP·errorP + wRT·(-z) )
   *  - errorP: distancia entre la precisión observada en ESTE ensayo (0 o 1, con
   *    omisión contando como error) y `targetAccuracy` — positivo empuja para arriba,
   *    negativo empuja para abajo.
   *  - z: Z-score del tiempo de reacción vs. la media histórica del usuario. Negativo
   *    (más rápido que su propio promedio) empuja D(t) hacia arriba; positivo
   *    (más lento) lo empuja hacia abajo. Se usa `-z` para que ambos términos compartan
   *    signo cuando el desempeño es bueno.
   *  - Pesos: 0.65 para precisión, 0.35 para velocidad — la precisión pesa más porque
   *    un usuario rápido pero impreciso NO debería subir de dificultad (eso premiaría
   *    adivinar rápido en vez de reconocer bien).
   */
  fun registerTrial(result: TrialResult, pairCount: Int): DifficultyProfile {
    val z = updateAndComputeReactionZScore(result.reactionTimeMs, result.wasOmission)
    val observedAccuracy = if (result.wasCorrect && !result.wasOmission) 1f else 0f
    val errorP = observedAccuracy - targetAccuracy

    val weightedDelta = (weightAccuracy * errorP) + (weightReactionTime * -z)
    difficultyIndex = (difficultyIndex + stepSize * weightedDelta).coerceIn(0f, 1f)

    return buildProfile(pairCount)
  }

  /**
   * Perfil inicial. `seedDifficulty` es lo que ancla esta partida al nivel/maestría
   * elegidos en el diálogo de la biblioteca de juegos (`level`/`baseIntensity` en
   * [CardsGameContainer]) — sin esto, D(t) arrancaría siempre en 0 sin importar si la
   * persona venía jugando en "Nivel 4" o tenía maestría acumulada, ignorando por completo
   * la selección de dificultad y el progreso entre sesiones. `pairCount` es aparte, a
   * cargo del nivel de la partida (1..10, ver [CardsGameContract.pairCountForStage]) —
   * D(t) ya NO decide cuántas parejas hay, sólo el resto de las dimensiones (vista
   * previa, interferencia, distractores); ver nota en la clase.
   */
  fun initialProfile(seedDifficulty: Float, pairCount: Int): DifficultyProfile {
    difficultyIndex = seedDifficulty.coerceIn(0f, 1f)
    return buildProfile(pairCount)
  }

  /**
   * Perfil ACTUAL, sin tocar `difficultyIndex` ni el historial de RT. Se usa para armar
   * el tablero del siguiente nivel dentro de la misma partida: D(t) sigue afinando vista
   * previa/interferencia/distractores según el desempeño acumulado, pero `pairCount` lo
   * fija el Container según el nivel (1..10) al que corresponda ese tablero.
   */
  fun currentProfile(pairCount: Int): DifficultyProfile = buildProfile(pairCount)

  private fun updateAndComputeReactionZScore(reactionTimeMs: Long, wasOmission: Boolean): Float {
    // Una omisión no tiene un RT real que promediar (el usuario nunca respondió) —
    // no contamina la media histórica, pero SÍ cuenta como el peor RT posible a los
    // fines del z-score de este ensayo puntual (empuja fuerte hacia "bajar dificultad").
    if (wasOmission) return MAX_Z_SCORE

    trialCount++
    val delta = reactionTimeMs - rtMean
    rtMean += delta / trialCount
    val delta2 = reactionTimeMs - rtMean
    rtM2 += delta * delta2

    if (trialCount < MIN_TRIALS_FOR_ZSCORE) return 0f // sin historial suficiente -> neutral

    val variance = rtM2 / (trialCount - 1)
    val stdDev = sqrt(variance).coerceAtLeast(MIN_STD_DEV_MS)
    val z = ((reactionTimeMs - rtMean) / stdDev).toFloat()
    return z.coerceIn(-MAX_Z_SCORE, MAX_Z_SCORE)
  }

  // --- a) Dimensión de matriz: la cantidad de parejas ya NO sale de D(t) (ver
  // CardsGameContract.pairCountForStage) -- acá solo queda resolver filas/columnas
  // para el pairCount que el Container decida. ---

  private fun columnsFor(pairCount: Int): Int = when {
    pairCount <= 2 -> 2
    pairCount <= 4 -> 2
    pairCount <= 6 -> 3
    pairCount <= 9 -> 4
    pairCount <= 12 -> 4
    pairCount <= 15 -> 5
    else -> 6
  }

  private fun gridDimensionsFor(pairCount: Int): Pair<Int, Int> {
    val columns = columnsFor(pairCount)
    val rows = ceil((pairCount * 2) / columns.toFloat()).toInt()
    return columns to rows
  }

  // --- b) Tiempo de vista previa: interpola 3000ms (D=0) -> 500ms (D=1) ---

  private fun previewExposureMsFor(d: Float): Long {
    val ms = PREVIEW_MAX_MS - d * (PREVIEW_MAX_MS - previewFloorMs)
    return ms.toLong()
  }

  // --- c) Densidad de distractores: frecuencia (cantidad) y opacidad ---

  private fun distractorCountFor(d: Float): Int = (d * MAX_DISTRACTORS).toInt()

  private fun distractorOpacityFor(d: Float): Float = MIN_DISTRACTOR_ALPHA + d * (MAX_DISTRACTOR_ALPHA - MIN_DISTRACTOR_ALPHA)

  // --- Interferencia perceptual (ver ParejasOcultasDda original: 4 bancos de símbolos) ---

  private fun interferenceLevelFor(d: Float): Int = (d * 3).toInt().coerceIn(0, 3)

  private fun buildProfile(pairCount: Int): DifficultyProfile {
    return DifficultyProfile(
      difficultyIndex = difficultyIndex,
      gridDimensions = gridDimensionsFor(pairCount),
      pairCount = pairCount,
      previewExposureMs = previewExposureMsFor(difficultyIndex),
      distractorCount = distractorCountFor(difficultyIndex),
      distractorOpacity = distractorOpacityFor(difficultyIndex),
      interferenceLevel = interferenceLevelFor(difficultyIndex)
    )
  }

  companion object {
    const val DEFAULT_WEIGHT_ACCURACY = 0.65f
    const val DEFAULT_WEIGHT_REACTION_TIME = 0.35f
    private const val MIN_TRIALS_FOR_ZSCORE = 3
    private const val MIN_STD_DEV_MS = 150.0 // evita dividir por un desvío ~0 en los primeros ensayos
    private const val MAX_Z_SCORE = 2.5f

    private const val PREVIEW_MAX_MS = 3000f
    // 500ms es una exigencia real para el perfil ADULT (menos que los 900ms del piso
    // anterior). Se alcanza recién con D(t) cerca de 1.0, que ya exige precisión
    // sostenida ≥80% Y velocidad por encima del propio promedio histórico del usuario —
    // no es un piso que un principiante pueda tocar por accidente. Otros perfiles usan
    // un piso más alto (ver DdaUserProfileConfig.minPreviewExposureMs).
    const val DEFAULT_PREVIEW_MIN_MS = 500f

    private const val MAX_DISTRACTORS = 6
    private const val MIN_DISTRACTOR_ALPHA = 0.03f
    private const val MAX_DISTRACTOR_ALPHA = 0.12f
  }
}
