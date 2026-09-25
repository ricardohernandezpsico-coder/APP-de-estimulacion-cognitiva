package com.example.bridge

import android.content.Context
import android.content.Intent
import com.example.model.AgeBand
import com.squareup.moshi.JsonClass
import com.squareup.moshi.Moshi
import com.unity3d.player.UnityPlayerGameActivity

/**
 * Lanza el piloto de Secuencia Lumínica en Unity (Fase 1 del roadmap, ver
 * NeuroVida/CLAUDE.md). En vez de que el lado nativo llame a
 * `UnityPlayer.UnitySendMessage` sobre una instancia del player ya corriendo (frágil de
 * verificar sin poder probarlo en un dispositivo real -- depende de detalles internos de
 * la arquitectura GameActivity de Unity 6 que no se pudieron validar en esta sesión), el
 * JSON de configuración viaja como extra del propio Intent que arranca la Activity.
 * Unity lo lee solo al arrancar la escena
 * (`Assets/Scripts/Bootstrap/LaunchIntentConfigReader.cs`) vía
 * `UnityPlayer.currentActivity.getIntent()` -- un patrón mucho más estable porque no
 * depende de la clase Activity específica que genera cada versión de Unity, solo de la
 * API pública y estable de `UnityPlayer`.
 */
object UnityGameLauncher {
  const val EXTRA_CONFIG_JSON = "neurovida_game_config_json"

  /** Id único de cada partida lanzada. Unity queda vivo entre partidas: con este id detecta que llegó una nueva
   *  (Intent nuevo con REORDER_TO_FRONT) y la app sabe a qué partida pertenece el resultado que vuelve. */
  const val EXTRA_LAUNCH_ID = "neurovida_launch_id"

  @JsonClass(generateAdapter = true)
  data class ConfigDetailsDto(
    val level: Int,
    val base_intensity: Int,
    val timed: Boolean,
    val age_band: String,
    val sound_enabled: Boolean,
    // DDA común: rating guardado del juego (0..1). `has_dda_rating` evita confundir "sin dato" con 0.
    val has_dda_rating: Boolean = false,
    val dda_rating: Double = 0.0
  )

  @JsonClass(generateAdapter = true)
  data class InitConfigDto(
    val user_id: String,
    val game_id: String,
    val config: ConfigDetailsDto
  )

  private val moshi = Moshi.Builder().build()
  private val adapter = moshi.adapter(InitConfigDto::class.java)

  fun launchSecuenciaLuminica(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "secuencia", level, baseIntensity, timed, ageBand, soundEnabled)

  /** Piloto de la Fase 2 (ver NeuroVida/CLAUDE.md) -- mismo mecanismo de Intent extra que
   *  Secuencia Lumínica, ver [launchSecuenciaLuminica]. */
  fun launchParejasOcultas(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "parejas", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Tinta o Palabra" (Stroop) en Unity -- mismo mecanismo de Intent extra. */
  fun launchStroop(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "stroop", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Comparación Instantánea" en Unity -- mismo mecanismo de Intent extra. */
  fun launchComparacion(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "comparacion", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Cambio de Chip" en Unity -- mismo mecanismo de Intent extra. */
  fun launchCambioChip(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "cambiochip", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Ruta del Tesoro" en Unity -- mismo mecanismo de Intent extra. */
  fun launchRutaTesoro(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "rutatesoro", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Detective de Series" en Unity -- mismo mecanismo de Intent extra. */
  fun launchSeries(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "series", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Cálculo Sereno" en Unity -- mismo mecanismo de Intent extra. */
  fun launchCalculo(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "calculo", level, baseIntensity, timed, ageBand, soundEnabled)

  /** "Anagramas" en Unity -- mismo mecanismo de Intent extra. */
  fun launchAnagramas(
    context: Context,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true
  ) = launch(context, userId, "anagramas", level, baseIntensity, timed, ageBand, soundEnabled)

  /**
   * Intent para jugar cualquiera de los 9 juegos en Unity desde el menú principal / sesión diaria. Trae al
   * frente la Activity de Unity si ya está viva (FLAG_ACTIVITY_REORDER_TO_FRONT: sin volver a arrancar el motor)
   * con un [EXTRA_LAUNCH_ID] nuevo. La vuelta a la app llega a `MainActivity.onNewIntent` (ver
   * `NativeReceiver.returnToApp`).
   */
  fun buildGameIntent(
    context: Context,
    gameId: String,
    userId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean = true,
    launchId: String? = null // una partida en pausa se retoma con SU id (Unity no la reinicia)
  ): Intent = buildIntent(context, userId, gameId, level, baseIntensity, timed, ageBand, soundEnabled, launchId)

  private fun launch(
    context: Context,
    userId: String,
    gameId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean
  ) {
    context.startActivity(buildIntent(context, userId, gameId, level, baseIntensity, timed, ageBand, soundEnabled))
  }

  private fun buildIntent(
    context: Context,
    userId: String,
    gameId: String,
    level: Int,
    baseIntensity: Int,
    timed: Boolean,
    ageBand: AgeBand,
    soundEnabled: Boolean,
    launchId: String? = null
  ): Intent {
    val savedRating = com.example.NeuroVidaApplication.instance.repository.gameDdaRating.value[gameId] ?: -1f
    val config = InitConfigDto(
      user_id = userId,
      game_id = gameId, // mismo id de texto que GameRegistry (Models.kt), no un int
      config = ConfigDetailsDto(
        level = level,
        base_intensity = baseIntensity,
        timed = timed,
        age_band = ageBand.name, // "SENIOR"/"ADULT"/"UNDER_18" -- coincide 1:1 con DdaUserProfileConfig.ParseAgeBand en C#
        sound_enabled = soundEnabled,
        has_dda_rating = savedRating >= 0f,
        dda_rating = savedRating.coerceAtLeast(0f).toDouble()
      )
    )
    val json = adapter.toJson(config)

    return Intent(context, UnityPlayerGameActivity::class.java)
      .addFlags(Intent.FLAG_ACTIVITY_REORDER_TO_FRONT)
      .putExtra(EXTRA_CONFIG_JSON, json)
      .putExtra(EXTRA_LAUNCH_ID, launchId ?: java.util.UUID.randomUUID().toString())
  }
}
