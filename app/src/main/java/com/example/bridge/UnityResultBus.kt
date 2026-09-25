package com.example.bridge

import com.example.model.GamePlayResult
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow

/**
 * Canal por donde llega a la UI (ViewModel) el resultado de una partida jugada en Unity.
 *
 * [NativeReceiver.onGameFinished] lo publica; el `NeuroVidaViewModel` lo recoge, lo guarda en Room y muestra
 * la pantalla de resultado de la app. Si nadie está escuchando (la app fue destruida mientras Unity estaba
 * al frente), [publish] devuelve false y el receptor guarda el resultado directamente para no perderlo.
 */
object UnityResultBus {
  private val _results = MutableSharedFlow<GamePlayResult>(
    replay = 0,
    extraBufferCapacity = 8,
    onBufferOverflow = BufferOverflow.DROP_OLDEST
  )
  val results: SharedFlow<GamePlayResult> = _results.asSharedFlow()

  /** true si hay al menos un suscriptor que recibirá el resultado. */
  fun publish(result: GamePlayResult): Boolean {
    val hasSubscribers = _results.subscriptionCount.value > 0
    if (hasSubscribers) _results.tryEmit(result)
    return hasSubscribers
  }
}
