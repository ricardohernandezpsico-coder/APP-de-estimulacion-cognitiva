package com.example.games.parejas

import android.os.SystemClock
import com.example.model.AgeBand
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import pro.respawn.flowmvi.api.Container
import pro.respawn.flowmvi.api.PipelineContext
import pro.respawn.flowmvi.dsl.store
import pro.respawn.flowmvi.dsl.updateState
import pro.respawn.flowmvi.plugins.asyncCache
import pro.respawn.flowmvi.plugins.deinit
import pro.respawn.flowmvi.plugins.init
import pro.respawn.flowmvi.plugins.recover
import pro.respawn.flowmvi.plugins.reduce
import kotlin.random.Random

private typealias Ctx = PipelineContext<CardsGameState, CardsGameIntent, CardsGameAction>

/**
 * Container real de FlowMVI para "Parejas Ocultas" — reemplaza al `ParejasOcultasEngine`
 * nativo de la iteración anterior. Con la librería puesta, la mayor parte de lo que
 * antes había que reconstruir a mano ya viene dado por el framework:
 *
 *  - **No reentrancia / atomicidad real**: el plugin `reduce` procesa un
 *    [CardsGameIntent] por vez desde su cola interna. Un segundo `CardFlipped` que
 *    llegue mientras el primero sigue resolviéndose (con su `delay` adentro) queda
 *    ENCOLADO, no se intercala — no puede haber dos resoluciones de pareja pisándose.
 *    `isTurnLocked` en el estado sigue existiendo, pero ahora es una señal para la UI
 *    (deshabilitar el toque mientras se ve la pareja resuelta), no el mecanismo de
 *    seguridad en sí — eso ya lo da el Store.
 *  - **Corrutina temporizadora cancelable ligada al Store**: [runTimer] se lanza dentro
 *    de `init { }`, en el scope propio del Container. Se cancela sola cuando el Store
 *    se detiene (al salir del juego) — no depende de que la Activity/Composable se
 *    acuerde de cancelarla, a diferencia del `LaunchedEffect` externo de la iteración
 *    anterior.
 *  - **`recover`**: cualquier excepción no capturada en `reduce` cae acá en vez de
 *    tumbar la Activity; se refleja como [CardsGameState.Error] y se swallowea
 *    (`recover` devuelve `null` a propósito, ver abajo).
 *
 * `asyncCache`: lanza el armado del primer tablero en paralelo mientras el Store termina
 * de configurarse, entregando un `Deferred<T>` que se espera (`.await()`) recién al
 * mostrar el primer tablero (ver [playCountdownThenShow]).
 *
 * **Rendimiento (feedback de Ricardo, 20-sep)**: "se me hace muy lento la selección de
 * los recuadros... a una persona que responda rápido se le va a pegar". La causa real no
 * era el toque en sí (cada intent se procesa en cuanto llega, no hay nada bloqueando la
 * cola) sino [runTimer]: antes hacía `updateState` cada 100ms durante TODA la partida
 * (incluso solo para el cronómetro ascendente de Precisión), lo que recomponía
 * `GameHeader`/`StatusRow`/`TimeDisplay` 10 veces por segundo sin parar — de fondo,
 * compitiendo por el hilo principal con el toque real del usuario. Ahora solo se
 * actualiza cada 100ms durante la memorización (necesario para el conteo suave) y una
 * vez por segundo el resto del tiempo, que es la única granularidad que la UI llega a
 * mostrar de todas formas.
 *
 * **Piloto de perfiles por edad (20-sep)**: `ageBand` decide los pesos del DDA (más peso
 * a precisión que a velocidad en adultos mayores), el piso de vista previa, y si un
 * timeout en Modo Reto corta la partida entera o solo cuenta como una falla de nivel más
 * -- ver [DdaUserProfileConfig] y [ddaProfileConfigFor]. Piloto en este juego únicamente,
 * los otros 8 siguen sin perfil.
 */
class CardsGameContainer(
  private val level: Int,
  private val timed: Boolean,
  private val baseIntensity: Int,
  private val sensory: SensoryFeedbackManager,
  private val ageBand: AgeBand
) : Container<CardsGameState, CardsGameIntent, CardsGameAction> {

  private val profileConfig = ddaProfileConfigFor(ageBand)

  private fun newDda() = VisualWorkingMemoryDDA(
    weightAccuracy = profileConfig.weightAccuracy,
    weightReactionTime = profileConfig.weightReactionTime,
    previewFloorMs = profileConfig.minPreviewExposureMs
  )

  private var dda = newDda()
  private var firstFlipElapsedMs: Long = 0L
  private var lastReportedTier = -1

  // --- Progresión multi-nivel dentro de la misma partida ---
  private var stage = 1
  private var cumulativeScore = 0
  private var cumulativeMatchedPairs = 0
  private var cumulativeAttempts = 0
  private var boardsPlayed = 0

  // --- Sistema de "3 fallas" (pedido de Ricardo): 3 parejas erradas en el tablero
  // actual cortan ESE tablero ya mismo. La 1ra vez que pasa, baja un nivel; la 2da,
  // repite ese mismo nivel (ya bajado); la 3ra, termina la partida. Ver
  // [handleStageFailure]. `mismatchesThisAttempt` se resetea con cada tablero nuevo
  // (avance, bajada o repetición); `stageFailures` solo se resetea al completar un
  // tablero de verdad (ver [settleAndAdvance]).
  private var mismatchesThisAttempt = 0
  private var stageFailures = 0

  /**
   * Ancla D(t) al nivel elegido en el diálogo de la biblioteca + la maestría entre
   * sesiones (`baseIntensity`, lo que hoy llega como `intensity` desde
   * `NeuroVidaViewModel.getEffectiveIntensityForGame`). Ya NO decide cuántas parejas
   * arrancan en pantalla (eso es siempre [pairCountForStage] del nivel 1, fijo y fácil) —
   * solo afina qué tan exigentes salen la vista previa/interferencia/distractores desde
   * el arranque.
   */
  private fun seedDifficultyIndex(): Float {
    val fromLevel = ((level - 1).coerceIn(0, 4) / 4f) * 0.7f // nivel 1 -> 0.0, nivel 5 -> 0.7
    val fromMastery = (baseIntensity / 30f).coerceIn(0f, 0.3f) // tope 0.3 de aporte por maestría
    return (fromLevel + fromMastery).coerceIn(0f, 1f)
  }

  override val store = store<CardsGameState, CardsGameIntent, CardsGameAction>(CardsGameState.Stopped) {
    configure { name = "CardsGameContainer" }

    val initialRoundDeferred by asyncCache {
      buildRound(dda.initialProfile(seedDifficultyIndex(), pairCountForStage(1)), stageNumber = 1)
    }

    init {
      // Pantalla "prepárate 3-2-1" también al arrancar la partida (antes iba directo a
      // `Running`) -- pedido explícito: "que aparezca... prepárate, el juego comienza en
      // 3, 2, 1", no solo entre niveles.
      launch { playCountdownThenShow(stageNumber = 1, reason = CountdownReason.START) { initialRoundDeferred.await() } }
      launch { runTimer() }
    }

    reduce { intent ->
      when (intent) {
        is CardsGameIntent.CardFlipped -> handleCardFlipped(intent.id)
        // El timer de `init` ya escribe directo sobre el estado con su propio
        // `updateState` -- este intent está en el contrato porque así lo pediste (una
        // sola fuente de verdad de "qué puede pasarle al juego"), pero como depende
        // del reloj y no de un toque, lo resuelve el timer, no `reduce`.
        CardsGameIntent.TimerTicked -> Unit
        CardsGameIntent.RestartGame -> {
          lastReportedTier = -1
          stage = 1
          cumulativeScore = 0
          cumulativeMatchedPairs = 0
          cumulativeAttempts = 0
          boardsPlayed = 0
          mismatchesThisAttempt = 0
          stageFailures = 0
          dda = newDda() // reinicia también el historial de RT, no solo D(t)
          playCountdownThenShow(stageNumber = 1, reason = CountdownReason.START) {
            buildRound(dda.initialProfile(seedDifficultyIndex(), pairCountForStage(1)), stageNumber = 1)
          }
        }
      }
    }

    recover { e ->
      updateState { CardsGameState.Error(e) }
      null // se swallowea: ya quedó reflejado en el estado, no hace falta relanzar
    }

    deinit { sensory.release() }
  }

  private fun buildRound(profile: VisualWorkingMemoryDDA.DifficultyProfile, stageNumber: Int): CardsGameState.Running {
    val symbols = SymbolBank.select(profile.pairCount, profile.interferenceLevel)
    val cards = buildShuffledDeck(symbols).map { it.copy(isFaceUp = true) }
    val timeBudget = if (timed) (profile.pairCount * 6).coerceAtLeast(30) else null
    return CardsGameState.Running(
      cards = cards,
      gridDimensions = profile.gridDimensions,
      score = 0,
      matchedPairs = 0,
      totalPairs = profile.pairCount,
      attempts = 0,
      isMemorizing = true,
      memorizeMillisLeft = profile.previewExposureMs,
      isTurnLocked = false,
      stage = stageNumber,
      totalStages = TOTAL_STAGES,
      difficultyIndex = profile.difficultyIndex,
      distractorCount = profile.distractorCount,
      distractorOpacity = profile.distractorOpacity,
      distractorSeed = Random.nextInt(),
      timeLeftSeconds = timeBudget,
      totalTimeSeconds = timeBudget,
      elapsedMs = 0L
    )
  }

  /**
   * Cuenta regresiva "3, 2, 1" compartida entre el arranque de la partida y cada
   * transición de nivel (avance, bajada o repetición por fallas) -- el "dinamismo" que
   * pedía Ricardo en vez de saltar directo de un tablero al siguiente. `roundBuilder` es
   * `suspend` porque el primer nivel espera el `asyncCache` en paralelo; los niveles
   * siguientes arman el tablero sync (es CPU trivial) pero comparten la misma firma para
   * no duplicar el conteo.
   */
  private suspend fun Ctx.playCountdownThenShow(
    stageNumber: Int,
    reason: CountdownReason,
    roundBuilder: suspend () -> CardsGameState.Running
  ) {
    for (secondsLeft in 3 downTo 1) {
      updateState { CardsGameState.Countdown(secondsLeft = secondsLeft, stage = stageNumber, totalStages = TOTAL_STAGES, reason = reason) }
      delay(700)
    }
    mismatchesThisAttempt = 0
    updateState { roundBuilder() }
  }

  private fun buildShuffledDeck(symbols: List<String>): List<MemoryCardUi> {
    var id = 0
    val deck = mutableListOf<MemoryCardUi>()
    symbols.forEach { symbol ->
      deck += MemoryCardUi(id = id++, pairKey = symbol, symbol = symbol)
      deck += MemoryCardUi(id = id++, pairKey = symbol, symbol = symbol)
    }
    return deck.shuffled()
  }

  /**
   * Timer cancelable en pasos de 100ms (resolución necesaria para exposiciones tan
   * cortas como el piso de 500ms del DDA durante la memorización). Fuera de la
   * memorización, solo escribe estado una vez por segundo -- ver nota de rendimiento en
   * el doc de la clase.
   */
  private suspend fun Ctx.runTimer() {
    val tickMs = 100L
    var msSinceLastSecond = 0L
    while (true) {
      delay(tickMs)
      msSinceLastSecond += tickMs
      val isFullSecond = msSinceLastSecond >= 1000
      var timedOut = false
      var totalPairsAtTimeout = 0

      updateState<CardsGameState.Running, _> {
        when {
          isMemorizing -> {
            val left = (memorizeMillisLeft - tickMs).coerceAtLeast(0)
            if (left > 0) copy(memorizeMillisLeft = left)
            else copy(cards = cards.map { it.copy(isFaceUp = false) }, isMemorizing = false, memorizeMillisLeft = 0)
          }
          // A partir de acá el jugador ya puede tocar cartas. El cronómetro de
          // "tiempo de resolución" (elapsedMs) y la cuenta regresiva de Modo Reto solo
          // necesitan granularidad de 1s -- actualizar cada 100ms acá era puro gasto de
          // recomposición sin ningún beneficio visible (ver nota de rendimiento arriba).
          isFullSecond -> {
            val withElapsed = copy(elapsedMs = elapsedMs + msSinceLastSecond)
            if (timeLeftSeconds != null) {
              val newLeft = timeLeftSeconds - 1
              if (newLeft <= 0) {
                timedOut = true
                totalPairsAtTimeout = totalPairs
                withElapsed.copy(timeLeftSeconds = 0)
              } else {
                withElapsed.copy(timeLeftSeconds = newLeft)
              }
            } else {
              withElapsed
            }
          }
          else -> this // ni memorizando ni segundo completo -> sin cambios, sin recomposición
        }
      }

      if (isFullSecond) msSinceLastSecond = 0
      if (timedOut) {
        // Se acabó el turno sin que la persona completara el intento -> error de
        // OMISIÓN (distinto del de comisión, ver VisualWorkingMemoryDDA): no hubo
        // respuesta, no hubo un RT real que promediar.
        dda.registerTrial(
          VisualWorkingMemoryDDA.TrialResult(wasCorrect = false, wasOmission = true, reactionTimeMs = 0),
          pairCount = totalPairsAtTimeout
        )
        settleCurrentBoard()
        if (profileConfig.allowStrictTimeouts) {
          // Perfil adulto: quedarse sin tiempo termina TODA la partida ahí mismo -- no
          // tendría sentido "premiar" con el siguiente nivel un turno sin completar.
          endSession()
          return
        } else {
          // Perfiles sin timeouts estrictos (adulto mayor / menores de 18, ver
          // DdaUserProfileConfig -- el material citado por Ricardo es explícito:
          // "prohibido expiración estricta, causa ansiedad"): un timeout entra al MISMO
          // sistema de 3 fallas que un tablero mal resuelto, en vez de cortar de una. El
          // timer sigue corriendo (no hay `return`) para seguir el próximo tablero.
          handleStageFailure()
        }
      }
    }
  }

  private suspend fun Ctx.handleCardFlipped(id: Int) {
    var justFlippedSecond = false
    val now = SystemClock.elapsedRealtime()

    updateState<CardsGameState.Running, _> {
      if (isMemorizing || isTurnLocked) return@updateState this
      val card = cards.find { it.id == id } ?: return@updateState this
      if (card.isFaceUp || card.isMatched) return@updateState this

      when (cards.count { it.isFaceUp && !it.isMatched }) {
        0 -> {
          firstFlipElapsedMs = now
          copy(cards = cards.map { if (it.id == id) it.copy(isFaceUp = true) else it })
        }
        1 -> {
          justFlippedSecond = true
          copy(
            cards = cards.map { if (it.id == id) it.copy(isFaceUp = true) else it },
            // Se bloquea en la MISMA transacción que registra el segundo volteo. No es
            // el candado que evita la condición de carrera (eso ya lo da `reduce`
            // procesando un intent a la vez) — es la señal para que la UI deshabilite
            // el toque mientras se resuelve.
            isTurnLocked = true
          )
        }
        else -> this
      }
    }

    sensory.play(SoundEffect.FLIP)
    action(CardsGameAction.PlaySoundEffect(SoundEffect.FLIP))
    if (justFlippedSecond) resolveTurn(now)
  }

  private suspend fun Ctx.resolveTurn(secondFlipMs: Long) {
    // Bajado de 700 a 500ms -- pedido de Ricardo: alguien que responde rápido sentía que
    // "se pegaba". Sigue siendo suficiente para ver ambas cartas antes de que se resuelvan.
    delay(500)
    var matched = false
    var justFinished = false
    var profile: VisualWorkingMemoryDDA.DifficultyProfile? = null

    updateState<CardsGameState.Running, _> {
      val faceUp = cards.filter { it.isFaceUp && !it.isMatched }
      if (faceUp.size != 2) return@updateState this // idempotencia: nada que resolver
      val (a, b) = faceUp
      matched = a.pairKey == b.pairKey

      val reactionMs = (secondFlipMs - firstFlipElapsedMs).coerceAtLeast(0)
      profile = dda.registerTrial(
        VisualWorkingMemoryDDA.TrialResult(wasCorrect = matched, wasOmission = false, reactionTimeMs = reactionMs),
        pairCount = totalPairs
      )

      val newAttempts = attempts + 1
      val newStreak = if (matched) currentStreak + 1 else 0
      val newMatchedPairs = matchedPairs + if (matched) 1 else 0
      justFinished = newMatchedPairs >= totalPairs

      val updatedCards = if (matched) {
        cards.map { if (it.id == a.id || it.id == b.id) it.copy(isMatched = true) else it }
      } else {
        cards.map { if (it.id == a.id || it.id == b.id) it.copy(isFaceUp = false) else it }
      }

      copy(
        cards = updatedCards,
        attempts = newAttempts,
        matchedPairs = newMatchedPairs,
        currentStreak = newStreak,
        isTurnLocked = false,
        difficultyIndex = profile?.difficultyIndex ?: difficultyIndex,
        distractorCount = profile?.distractorCount ?: distractorCount,
        distractorOpacity = profile?.distractorOpacity ?: distractorOpacity
      )
    }

    val soundEffect = if (matched) SoundEffect.MATCH else SoundEffect.MISMATCH
    val haptic = if (matched) HapticPattern.CORRECT else HapticPattern.INCORRECT
    sensory.play(soundEffect)
    sensory.vibrate(haptic)
    action(CardsGameAction.PlaySoundEffect(soundEffect))
    action(CardsGameAction.TriggerHapticFeedback(haptic))

    val tier = ((profile?.difficultyIndex ?: 0f) * 10).toInt()
    if (tier > lastReportedTier) {
      lastReportedTier = tier
      // Lanzado aparte (no bloquea el procesamiento del próximo toque) y con un pequeño
      // desfase -- pedido de Ricardo, "los sonidos se escuchan mal": el tono de subida de
      // nivel sonando ENCIMA del de acierto/error (dos AudioTrack independientes
      // reproduciendo a la vez) se escuchaba amontonado. 150ms alcanza para que el tono
      // corto de match/mismatch (90-130ms) ya haya terminado.
      launch {
        delay(150)
        sensory.play(SoundEffect.LEVEL_UP)
        sensory.vibrate(HapticPattern.LEVEL_UP)
        action(CardsGameAction.ShowLevelUp)
      }
    }

    if (!matched) {
      mismatchesThisAttempt++
      if (mismatchesThisAttempt >= MISMATCHES_TO_FAIL_STAGE) {
        settleCurrentBoard()
        handleStageFailure()
        return
      }
    }

    // Tablero completo -> NO se corta acá (era el comportamiento viejo). Pasa de
    // inmediato al siguiente nivel dentro de la misma partida.
    if (justFinished) {
      settleCurrentBoard()
      sensory.play(SoundEffect.ROUND_COMPLETE)
      action(CardsGameAction.PlaySoundEffect(SoundEffect.ROUND_COMPLETE))
      stageFailures = 0 // tablero superado de verdad -> las fallas no se arrastran al próximo nivel
      if (stage >= TOTAL_STAGES) endSession() else transitionToStage(stage + 1, CountdownReason.ADVANCE)
    }
  }

  /**
   * Suma el puntaje/parejas/intentos del tablero ACTUAL a los acumulados de la partida.
   * Se llama tanto al completar un tablero de verdad como al cortarlo por 3 fallas o por
   * timeout -- en todos los casos hay progreso real que debería reflejarse en el
   * resultado final, no solo en los tableros que se llegan a terminar.
   */
  private suspend fun Ctx.settleCurrentBoard() {
    var stageScore = 0
    var stageMatched = 0
    var stageAttempts = 0

    updateState<CardsGameState.Running, _> {
      val efficiency = if (attempts == 0) 0f else matchedPairs.toFloat() / attempts.coerceAtLeast(matchedPairs)
      val baseScore = (efficiency * 100).toInt()
      // Modo Reto ya presiona por tiempo con la cuenta regresiva -- el bono acá es solo
      // para Precisión, donde antes el tiempo no influía en el puntaje en absoluto.
      val speedBonus = if (!timed && matchedPairs > 0) speedBonusFor(elapsedMs, totalPairs) else 0
      stageScore = (baseScore + speedBonus).coerceIn(if (matchedPairs > 0) 40 else 0, 100)
      stageMatched = matchedPairs
      stageAttempts = attempts
      this // la transición de estado real (Countdown/Finished) la decide el código que llama
    }

    boardsPlayed++
    cumulativeScore += stageScore
    cumulativeMatchedPairs += stageMatched
    cumulativeAttempts += stageAttempts
  }

  private suspend fun Ctx.transitionToStage(targetStage: Int, reason: CountdownReason) {
    stage = targetStage
    playCountdownThenShow(stageNumber = targetStage, reason = reason) {
      buildRound(dda.currentProfile(pairCountForStage(targetStage)), stageNumber = targetStage)
    }
  }

  /**
   * 3 parejas erradas en el tablero actual (ver [resolveTurn]) -- pedido explícito de
   * Ricardo. Primera vez: baja un nivel. Segunda vez seguida: repite ese mismo nivel
   * (ya bajado), sin bajar más. Tercera vez: termina la partida entera.
   */
  private suspend fun Ctx.handleStageFailure() {
    stageFailures++
    when {
      stageFailures >= STAGE_FAILURES_BEFORE_GAME_OVER -> endSession()
      stageFailures == 1 -> transitionToStage((stage - 1).coerceAtLeast(1), CountdownReason.DEMOTED)
      else -> transitionToStage(stage, CountdownReason.RETRY)
    }
  }

  private suspend fun Ctx.endSession() {
    val averageScore = (cumulativeScore / boardsPlayed.coerceAtLeast(1)).coerceIn(0, 100)
    updateState {
      CardsGameState.Finished(score = averageScore, matchedPairs = cumulativeMatchedPairs, attempts = cumulativeAttempts)
    }
  }

  private fun speedBonusFor(elapsedMs: Long, totalPairs: Int): Int {
    val expectedMs = totalPairs * EXPECTED_MS_PER_PAIR
    return when {
      elapsedMs <= expectedMs * 0.6 -> 10 // resolvió bastante más rápido que el ritmo de referencia
      elapsedMs <= expectedMs -> 5
      else -> 0
    }
  }
}
