package com.example.bridge

import android.app.Activity
import android.util.Log
import com.example.NeuroVidaApplication
import com.example.model.GamePlayResult
import com.squareup.moshi.JsonClass
import com.squareup.moshi.Moshi
import com.unity3d.player.UnityPlayer
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

/**
 * Receptor Unity -> Nativo, compartido entre todos los juegos migrados (Fase 1: Secuencia
 * Lumínica; Fase 2: Parejas Ocultas -- ver NeuroVida/CLAUDE.md). Unity lo invoca así, desde
 * `unity/NeuroVidaCore/Assets/Scripts/Bridge/NativeBridge.cs`:
 * `AndroidJavaClass("com.example.bridge.NativeReceiver").CallStatic("onGameFinished", json)`
 * -- SIEMPRE el mismo método nativo sea cual sea el juego, así que acá adentro hay que
 * mirar `game_id` para saber qué forma de `session_metrics` esperar antes de parsear en
 * serio (cada juego tiene campos de telemetría propios -- ver `SequenceTelemetry.cs` vs
 * `CardsTelemetry.cs` del lado Unity).
 *
 * Sin `Context` disponible en esa llamada -- de ahí [NeuroVidaApplication], que expone el
 * mismo [com.example.data.NeuroVidaRepository] que usaría
 * `NeuroVidaViewModel.finishActiveGame` si el juego se jugara desde Compose. El mapeo acá
 * es deliberadamente igual de simple que ese método: `recordGameResult` ya hace toda la
 * lógica real (nivel adaptativo, maestría, rank ELO); no hay estado de sesión de la UI
 * (activeGame/lastResult) que replicar porque esta Activity de Unity es standalone, no
 * pasa por el overlay de juego activo del ViewModel.
 */
object NativeReceiver {
  private const val TAG = "NativeReceiver"

  @JsonClass(generateAdapter = true)
  data class GameIdPeekDto(val game_id: String)

  @JsonClass(generateAdapter = true)
  data class SequenceSessionMetricsDto(
    val correct_rounds: Int,
    val total_rounds: Int,
    val calculated_score: Int,
    val average_response_time_ms: Double,
    val final_span_length: Int,
    val level: Int,
    val timed: Boolean
  )

  @JsonClass(generateAdapter = true)
  data class SequenceTelemetryDto(
    val user_id: String,
    val game_id: String,
    val session_metrics: SequenceSessionMetricsDto
  )

  @JsonClass(generateAdapter = true)
  data class CardsSessionMetricsDto(
    val matched_pairs: Int,
    val attempts: Int,
    val calculated_score: Int,
    val level: Int,
    val timed: Boolean
  )

  @JsonClass(generateAdapter = true)
  data class CardsTelemetryDto(
    val user_id: String,
    val game_id: String,
    val session_metrics: CardsSessionMetricsDto
  )

  @JsonClass(generateAdapter = true)
  data class StroopSessionMetricsDto(
    val correct_trials: Int,
    val total_trials: Int,
    val calculated_score: Int,
    val average_response_time_ms: Int,
    val level: Int,
    val timed: Boolean,
    // DDA común (Unity): rating final normalizado 0..1 y nivel máximo alcanzado. Opcionales para
    // no romper versiones anteriores; hoy solo se registran (falta persistirlos entre sesiones).
    val end_rating: Double? = null,
    val peak_level: Int = 0
  )

  @JsonClass(generateAdapter = true)
  data class StroopTelemetryDto(
    val user_id: String,
    val game_id: String,
    val session_metrics: StroopSessionMetricsDto
  )

  private val moshi = Moshi.Builder().build()
  private val peekAdapter = moshi.adapter(GameIdPeekDto::class.java)
  private val sequenceAdapter = moshi.adapter(SequenceTelemetryDto::class.java)
  private val cardsAdapter = moshi.adapter(CardsTelemetryDto::class.java)
  private val stroopAdapter = moshi.adapter(StroopTelemetryDto::class.java)

  // No se reutiliza viewModelScope (no hay ViewModel vivo en este camino) -- ídem al
  // repositoryScope interno de NeuroVidaRepository, pero acá hace falta uno propio
  // porque la llamada entra desde fuera del ciclo de vida de cualquier ViewModel/Activity
  // Compose.
  private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

  const val ACTION_GAME_FINISHED = "com.example.action.UNITY_GAME_FINISHED"
  const val EXTRA_JSON = "json"

  /**
   * Punto de entrada desde Unity. La Activity de Unity corre en su propio proceso (`:unity`, ver el manifest:
   * al cerrarse Unity mata su proceso y, si compartiera el de la app, se llevaría la app puesta), así que el
   * resultado viaja al proceso principal por un broadcast explícito ([UnityResultReceiver] -> [handleFinished]).
   */
  @JvmStatic
  fun onGameFinished(json: String) {
    // Marca la Activity de Unity como "partida terminada": al cerrarse, MainActivity recibe RESULT_OK (ver
    // UnityGameHost) y espera este resultado. Si el usuario sale a mitad (Atrás) o Unity se cae, nunca pasa por
    // acá y el resultado queda en RESULT_CANCELED. Activity.setResult es synchronized: se puede llamar desde
    // el hilo de Unity.
    UnityPlayer.currentActivity?.setResult(Activity.RESULT_OK)
    val app = NeuroVidaApplication.instance
    app.sendBroadcast(
      android.content.Intent(ACTION_GAME_FINISHED).setPackage(app.packageName).putExtra(EXTRA_JSON, json)
    )
  }

  /** Corre en el proceso principal: parsea la telemetría y la entrega a la UI o la guarda. */
  fun handleFinished(json: String) {
    val gameId = try {
      peekAdapter.fromJson(json)?.game_id
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría inválido (sin game_id legible): $json", e)
      null
    } ?: return

    val result = when (gameId) {
      "secuencia" -> parseSequenceResult(json)
      "parejas" -> parseCardsResult(json)
      // Comparación, Cambio de Chip, Ruta del Tesoro, Series, Cálculo y Anagramas reusan el mismo esquema de telemetría por ensayos que Stroop.
      "stroop", "comparacion", "cambiochip", "rutatesoro", "series", "calculo", "anagramas" -> parseStroopResult(json)
      else -> {
        Log.e(TAG, "game_id \"$gameId\" no tiene un parser de telemetría registrado todavía.")
        null
      }
    } ?: return

    // Si la UI está escuchando (ViewModel vivo), ella guarda el resultado y muestra la pantalla de
    // resultado de la app; si no, se guarda directamente para no perder la partida.
    if (UnityResultBus.publish(result)) {
      Log.i(TAG, "Resultado de Unity entregado a la UI: $result")
    } else {
      scope.launch {
        Log.i(TAG, "Persistiendo resultado de Unity: $result")
        NeuroVidaApplication.instance.repository.recordGameResult(result)
      }
    }
  }

  private fun parseSequenceResult(json: String): GamePlayResult? {
    val telemetry = try {
      sequenceAdapter.fromJson(json)
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría de Secuencia Lumínica inválido: $json", e)
      null
    } ?: return null

    val metrics = telemetry.session_metrics
    return GamePlayResult(
      gameId = telemetry.game_id,
      score = metrics.calculated_score.coerceIn(0, 100),
      correctAnswers = metrics.correct_rounds,
      totalTrials = metrics.total_rounds,
      // Eco de los valores con que se lanzó la partida (ver UnityGameLauncher /
      // SequenceInitConfig.config) -- mismo criterio que
      // NeuroVidaViewModel.finishActiveGame, que tampoco recalcula nada, solo reusa
      // ActiveGameSession.level/.timed.
      timed = metrics.timed,
      level = metrics.level
    )
  }

  private fun parseCardsResult(json: String): GamePlayResult? {
    val telemetry = try {
      cardsAdapter.fromJson(json)
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría de Parejas Ocultas inválido: $json", e)
      null
    } ?: return null

    val metrics = telemetry.session_metrics
    return GamePlayResult(
      gameId = telemetry.game_id,
      score = metrics.calculated_score.coerceIn(0, 100),
      correctAnswers = metrics.matched_pairs,
      totalTrials = metrics.attempts,
      timed = metrics.timed,
      level = metrics.level
    )
  }

  private fun parseStroopResult(json: String): GamePlayResult? {
    val telemetry = try {
      stroopAdapter.fromJson(json)
    } catch (e: Exception) {
      Log.e(TAG, "JSON de telemetría de Tinta o Palabra inválido: $json", e)
      null
    } ?: return null

    val metrics = telemetry.session_metrics
    Log.i(TAG, "DDA ${telemetry.game_id}: end_rating=${metrics.end_rating} peak_level=${metrics.peak_level}")
    return GamePlayResult(
      gameId = telemetry.game_id,
      score = metrics.calculated_score.coerceIn(0, 100),
      correctAnswers = metrics.correct_trials,
      totalTrials = metrics.total_trials,
      timed = metrics.timed,
      level = metrics.level,
      endRating = metrics.end_rating?.toFloat()
    )
  }
}
