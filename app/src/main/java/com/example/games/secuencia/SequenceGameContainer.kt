package com.example.games.secuencia

import android.os.SystemClock
import com.example.games.parejas.HapticPattern
import com.example.games.parejas.SensoryFeedbackManager
import com.example.games.parejas.SoundEffect
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import pro.respawn.flowmvi.api.Container
import pro.respawn.flowmvi.api.PipelineContext
import pro.respawn.flowmvi.dsl.store
import pro.respawn.flowmvi.dsl.updateState
import pro.respawn.flowmvi.plugins.deinit
import pro.respawn.flowmvi.plugins.init
import pro.respawn.flowmvi.plugins.recover
import pro.respawn.flowmvi.plugins.reduce
import kotlin.random.Random

private typealias Ctx = PipelineContext<SequenceGameState, SequenceGameIntent, SequenceGameAction>

/**
 * Container FlowMVI real de "Secuencia Lumínica" -- reemplaza la versión anterior que
 * vivía enteramente en `remember`/`LaunchedEffect` dentro del Composable (sin Store,
 * sin motor DDA de verdad, sin sonido). Mismo patrón que
 * [com.example.games.parejas.CardsGameContainer]: no-reentrancia real la da el `reduce`
 * de FlowMVI (procesa un intent a la vez desde su cola), la corrutina de presentación
 * se cancela sola al salir del juego (vive en el scope del Store, no en un
 * `LaunchedEffect` externo), y `recover` swallowea excepciones reflejándolas en
 * [SequenceGameState.Error] en vez de tumbar la Activity.
 */
class SequenceGameContainer(
  private val level: Int,
  private val timed: Boolean,
  private val baseIntensity: Int,
  private val sensory: SensoryFeedbackManager
) : Container<SequenceGameState, SequenceGameIntent, SequenceGameAction> {

  private val dda = SequenceDDAEngine()
  private var round = 1
  private var correctRounds = 0
  private val reactionTimesMs = mutableListOf<Long>()
  private var inputReadyAt = 0L
  private var lastReportedSpan = 0

  /** Mismo criterio que `CardsGameContainer.seedDifficultyIndex`: ancla el punto de
   *  partida al nivel elegido en el diálogo de la biblioteca + la maestría entre
   *  sesiones, sin que ninguno de los dos decida el resto de la progresión (de ahí en
   *  más manda el DDA de ventana deslizante). */
  private fun seedSpan(): Int {
    val fromLevel = 3 + (level - 1).coerceIn(0, 4) // nivel 1 -> 3, nivel 5 -> 7
    val fromMastery = (baseIntensity / 20).coerceIn(0, 3) // tope +3 por maestría
    return (fromLevel + fromMastery).coerceIn(SequenceDDAEngine.MIN_SPAN, SequenceDDAEngine.MAX_SPAN)
  }

  private fun seedIsiMs(): Long {
    val fromLevel = 1000L - (level - 1).coerceIn(0, 4) * 100L // nivel 1 -> 1000ms, nivel 5 -> 600ms
    val fromMastery = (baseIntensity * 3L).coerceAtMost(200L) // acelera hasta 200ms extra
    return (fromLevel - fromMastery).coerceIn(SequenceDDAEngine.MIN_ISI_MS, SequenceDDAEngine.MAX_ISI_MS)
  }

  override val store = store<SequenceGameState, SequenceGameIntent, SequenceGameAction>(SequenceGameState.Stopped) {
    configure { name = "SequenceGameContainer" }

    init {
      val profile = dda.seed(seedSpan(), seedIsiMs())
      lastReportedSpan = profile.spanLength
      launch { playCountdownThenPresent(roundNumber = 1, profile = profile) }
    }

    reduce { intent ->
      when (intent) {
        is SequenceGameIntent.PadTapped -> handlePadTapped(intent.pad)
        SequenceGameIntent.RestartGame -> {
          round = 1
          correctRounds = 0
          reactionTimesMs.clear()
          val profile = dda.seed(seedSpan(), seedIsiMs())
          lastReportedSpan = profile.spanLength
          playCountdownThenPresent(roundNumber = 1, profile = profile)
        }
      }
    }

    recover { e ->
      updateState { SequenceGameState.Error(e) }
      null
    }

    deinit { sensory.release() }
  }

  private suspend fun Ctx.playCountdownThenPresent(roundNumber: Int, profile: SequenceDDAEngine.DifficultyProfile) {
    for (secondsLeft in 3 downTo 1) {
      updateState { SequenceGameState.Countdown(secondsLeft = secondsLeft, round = roundNumber, totalRounds = TOTAL_ROUNDS) }
      delay(700)
    }
    presentSequence(roundNumber, profile)
  }

  /** Reproduce la secuencia completa (un pad a la vez, `interStimulusIntervalMs` por
   *  paso: ~60% encendido / ~40% pausa) y deja el estado en `AWAITING_INPUT` al
   *  terminar. El tono concordante de cada pad suena en el mismo instante que se
   *  enciende -- ver `playConcordantTone` en `SensoryFeedbackManager`. */
  private suspend fun Ctx.presentSequence(roundNumber: Int, profile: SequenceDDAEngine.DifficultyProfile) {
    val sequence = (1..profile.spanLength).map { PadColor.entries[Random.nextInt(PadColor.entries.size)] }

    updateState {
      SequenceGameState.Running(
        phase = SequencePhase.PRESENTING,
        sequence = sequence,
        userInput = emptyList(),
        highlightedPad = null,
        round = roundNumber,
        totalRounds = TOTAL_ROUNDS,
        correctRounds = correctRounds,
        spanLength = profile.spanLength,
        interStimulusIntervalMs = profile.interStimulusIntervalMs,
        distractorCount = profile.distractorCount,
        roundFeedback = null
      )
    }

    delay(600) // pausa antes de arrancar, tiempo de "ubicarse" en la nueva ronda
    val litMs = profile.interStimulusIntervalMs * 6 / 10
    val gapMs = profile.interStimulusIntervalMs - litMs
    for (pad in sequence) {
      updateState<SequenceGameState.Running, _> { copy(highlightedPad = pad) }
      sensory.playConcordantTone(pad.toneHz)
      action(SequenceGameAction.PlayConcordantTone(pad.toneHz))
      delay(litMs)
      updateState<SequenceGameState.Running, _> { copy(highlightedPad = null) }
      delay(gapMs)
    }

    updateState<SequenceGameState.Running, _> { copy(phase = SequencePhase.GET_READY) }
    delay(500)
    updateState<SequenceGameState.Running, _> { copy(phase = SequencePhase.AWAITING_INPUT) }
    inputReadyAt = SystemClock.elapsedRealtime()
  }

  private suspend fun Ctx.handlePadTapped(pad: PadColor) {
    val now = SystemClock.elapsedRealtime()
    // `roundResult`: null mientras la ronda sigue en curso (toque intermedio correcto);
    // true/false en cuanto la ronda queda resuelta (secuencia completa o primer error).
    var roundResult: Boolean? = null

    updateState<SequenceGameState.Running, _> {
      if (phase != SequencePhase.AWAITING_INPUT) return@updateState this

      val reactionMs = (now - inputReadyAt).coerceAtLeast(0)
      reactionTimesMs += reactionMs
      inputReadyAt = now

      val nextIndex = userInput.size
      val expected = sequence.getOrNull(nextIndex)
      if (expected != pad) {
        roundResult = false
        copy(highlightedPad = pad, phase = SequencePhase.GET_READY, roundFeedback = null)
      } else {
        val updatedInput = userInput + pad
        if (updatedInput.size == sequence.size) {
          roundResult = true
          val avg = reactionTimesMs.takeLast(sequence.size).average().toInt()
          copy(highlightedPad = pad, userInput = updatedInput, phase = SequencePhase.GET_READY, roundFeedback = "⚡ ${avg}ms de reacción promedio")
        } else {
          copy(highlightedPad = pad, userInput = updatedInput)
        }
      }
    }

    // Pulso visual del toque (se apaga solo, como en la demostración) -- independiente
    // de si el toque resultó correcto o no.
    sensory.playConcordantTone(pad.toneHz)
    action(SequenceGameAction.PlayConcordantTone(pad.toneHz))
    launch {
      delay(180)
      updateState<SequenceGameState.Running, _> { if (highlightedPad == pad) copy(highlightedPad = null) else this }
    }

    val finished = roundResult ?: return // toque intermedio correcto -> nada más que hacer todavía
    finishRound(wasCorrect = finished, roundNumber = round)
  }

  private suspend fun Ctx.finishRound(wasCorrect: Boolean, roundNumber: Int) {
    val profile = dda.registerRound(wasCorrect)
    if (wasCorrect) correctRounds++

    val soundEffect = if (wasCorrect) SoundEffect.ROUND_COMPLETE else SoundEffect.MISMATCH
    val haptic = if (wasCorrect) HapticPattern.CORRECT else HapticPattern.INCORRECT
    sensory.play(soundEffect)
    sensory.vibrate(haptic)
    action(SequenceGameAction.PlaySoundEffect(soundEffect))
    action(SequenceGameAction.TriggerHapticFeedback(haptic))

    if (profile.spanLength > lastReportedSpan) {
      lastReportedSpan = profile.spanLength
      // Mismo desfase de 150ms que Parejas Ocultas para que el tono de acierto/error
      // no le pise el sonido al de "subida de nivel" (dos AudioTrack sonando a la vez
      // se escuchaba amontonado, feedback real de Ricardo).
      launch {
        delay(150)
        sensory.play(SoundEffect.LEVEL_UP)
        sensory.vibrate(HapticPattern.LEVEL_UP)
        action(SequenceGameAction.PlaySoundEffect(SoundEffect.LEVEL_UP))
      }
    }

    delay(700) // tiempo para ver el flash/feedback de la ronda antes de pasar a la próxima
    if (roundNumber >= TOTAL_ROUNDS) {
      endSession()
    } else {
      round = roundNumber + 1
      playCountdownThenPresent(roundNumber = round, profile = profile)
    }
  }

  private suspend fun Ctx.endSession() {
    val baseScore = correctRounds * 100 / TOTAL_ROUNDS
    val avgReaction = reactionTimesMs.takeIf { it.isNotEmpty() }?.average() ?: 1500.0
    val speedBonus = when {
      avgReaction < 500 -> 10
      avgReaction < 800 -> 5
      else -> 0
    }
    val finalScore = (baseScore + speedBonus).coerceIn(0, 100)
    updateState { SequenceGameState.Finished(score = finalScore, correctRounds = correctRounds, totalRounds = TOTAL_ROUNDS) }
  }
}
