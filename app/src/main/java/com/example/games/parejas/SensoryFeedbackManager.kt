package com.example.games.parejas

import android.content.Context
import android.media.AudioAttributes
import android.media.AudioFormat
import android.media.AudioTrack
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import kotlin.math.PI
import kotlin.math.sin

/**
 * Refuerzo multisensorial de "Parejas Ocultas": tonos sintetizados en el momento (sin
 * assets de audio, así que no hace falta agregar archivos a `res/raw`) + patrones
 * hápticos diferenciados.
 *
 * Nada acá lee `UserSettings.hapticsEnabled`/`soundEnabled` todavía — ningún juego del
 * proyecto los consume hoy (son settings guardados pero sin efecto real, ver
 * `SettingsScreen.kt`/`Models.kt`). `setSoundEnabled`/`setHapticsEnabled` quedan
 * expuestos para que el día que se decida respetarlos de verdad, alcance con pasar el
 * valor de `viewModel.userSettings` al construir este manager — no hace falta tocar
 * nada de esta clase.
 */
class SensoryFeedbackManager(context: Context) {

  private val appContext = context.applicationContext

  private var soundEnabled = true
  private var hapticsEnabled = true

  fun setSoundEnabled(enabled: Boolean) { soundEnabled = enabled }
  fun setHapticsEnabled(enabled: Boolean) { hapticsEnabled = enabled }

  private val vibrator: Vibrator? by lazy { resolveVibrator() }

  // Tonos pre-sintetizados una sola vez (MODE_STATIC): re-dispararlos es solo
  // stop() + reloadStaticData() + play(), sin volver a generar la onda ni asignar
  // memoria en cada toque — importante para no meter jank en la UI cuando el usuario
  // encadena aciertos rápido. Se declaran como `Lazy<AudioTrack?>` explícito (no
  // `by lazy` directo) para poder chequear `isInitialized()` en `release()` sin forzar
  // la construcción de un tono que nunca se llegó a usar.
  private val matchToneLazy: Lazy<AudioTrack?> = lazy {
    // Segunda corrección de Ricardo tras jugarlo: "los sonidos... me parecen
    // inadecuados, iría por unos más suaves". El intervalo de quinta (700->1050Hz)
    // sonaba a "premio/arcade" -- se cambia a una tercera mayor (600->750Hz, un
    // salto más chico y consonante, menos "triunfal"), con menos volumen (0.6->0.30)
    // y una envolvente de entrada/salida bastante más larga (8ms->20ms) para que el
    // ataque se sienta redondeado en vez de percusivo.
    buildStaticTrack(
      concatenate(
        sineBuffer(600, 110, amplitude = 0.30, fadeMs = 20.0),
        sineBuffer(750, 150, amplitude = 0.26, fadeMs = 25.0)
      )
    )
  }
  private val mismatchToneLazy: Lazy<AudioTrack?> = lazy {
    // Mismo criterio: volumen bajado a más de la mitad (0.35->0.16) y envolvente
    // larga (25ms) -- una confirmación neutra y casi imperceptible de "no era esa",
    // nunca un sonido de error/castigo.
    buildStaticTrack(sineBuffer(500, 120, amplitude = 0.16, fadeMs = 25.0))
  }
  private val levelUpToneLazy: Lazy<AudioTrack?> = lazy {
    // Arpegio de 3 notas, comprimido a un rango más chico (700-1000Hz en vez de
    // 700-1200Hz) y con menos volumen (0.6->0.32) -- sigue siendo el momento más
    // "especial" de los tres sonidos, pero ya no debería sonar a feria.
    buildStaticTrack(
      concatenate(
        sineBuffer(700, 90, amplitude = 0.32, fadeMs = 20.0),
        sineBuffer(850, 90, amplitude = 0.32, fadeMs = 20.0),
        sineBuffer(1000, 130, amplitude = 0.32, fadeMs = 20.0)
      )
    )
  }
  private val matchTone: AudioTrack? by matchToneLazy
  private val mismatchTone: AudioTrack? by mismatchToneLazy
  private val levelUpTone: AudioTrack? by levelUpToneLazy

  /**
   * Tonos "concordantes" para Secuencia Lumínica: cada pad de color tiene su propia
   * frecuencia fija (grave, 500-1500Hz según el material que compartió Ricardo) que
   * suena tanto durante la demostración como cuando el usuario lo toca -- la
   * concordancia audio-visual es el punto (redes neuronales cruzadas, más rápido de
   * aprender que solo-visual). Cache por frecuencia en vez de un tono fijo por
   * `SoundEffect`, porque acá el número de tonos depende del juego que llame, no de un
   * enum cerrado -- reutiliza igual `buildStaticTrack`/`sineBuffer`/`replay`.
   */
  private val concordantTones = mutableMapOf<Int, AudioTrack?>()

  fun playConcordantTone(frequencyHz: Int) {
    if (!soundEnabled) return
    val track = concordantTones.getOrPut(frequencyHz) {
      buildStaticTrack(sineBuffer(frequencyHz, durationMs = 140, amplitude = 0.22, fadeMs = 18.0))
    }
    replay(track)
  }

  fun play(effect: SoundEffect) {
    if (!soundEnabled) return
    when (effect) {
      SoundEffect.FLIP -> Unit // deliberadamente silencioso: un tono por cada volteo sería ruido, no señal
      SoundEffect.MATCH -> replay(matchTone)
      SoundEffect.MISMATCH -> replay(mismatchTone)
      SoundEffect.LEVEL_UP -> replay(levelUpTone)
      SoundEffect.ROUND_COMPLETE -> replay(levelUpTone)
    }
  }

  fun vibrate(pattern: HapticPattern) {
    if (!hapticsEnabled) return
    val v = vibrator ?: return
    when (pattern) {
      // Doble pulso corto y firme -> "sí, así".
      HapticPattern.CORRECT -> vibrateWaveform(v, timings = longArrayOf(0, 30, 40, 30), amplitudes = intArrayOf(0, 180, 0, 180))
      // Un solo pulso más largo y suave -> negativo pero no punitivo.
      HapticPattern.INCORRECT -> vibrateWaveform(v, timings = longArrayOf(0, 90), amplitudes = intArrayOf(0, 90))
      // Tres pulsos crecientes -> "subiendo de nivel".
      HapticPattern.LEVEL_UP -> vibrateWaveform(v, timings = longArrayOf(0, 25, 35, 35, 45, 45), amplitudes = intArrayOf(0, 100, 0, 160, 0, 220))
    }
  }

  /** Se llama desde el plugin `deinit` del Container — libera los AudioTrack para no
   *  dejar recursos nativos de audio abiertos cuando se sale del juego. */
  fun release() {
    if (matchToneLazy.isInitialized()) matchToneLazy.value?.release()
    if (mismatchToneLazy.isInitialized()) mismatchToneLazy.value?.release()
    if (levelUpToneLazy.isInitialized()) levelUpToneLazy.value?.release()
    concordantTones.values.forEach { it?.release() }
    concordantTones.clear()
  }

  private fun replay(track: AudioTrack?) {
    val t = track ?: return
    try {
      t.stop()
      t.reloadStaticData()
      t.play()
    } catch (_: IllegalStateException) {
      // AudioTrack en un estado inválido (p.ej. liberado a destiempo) -> se ignora,
      // un sonido de feedback perdido no debe interrumpir el juego.
    }
  }

  private fun vibrateWaveform(vibrator: Vibrator, timings: LongArray, amplitudes: IntArray) {
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
      vibrator.vibrate(VibrationEffect.createWaveform(timings, amplitudes, -1))
    } else {
      @Suppress("DEPRECATION")
      vibrator.vibrate(timings, -1)
    }
  }

  private fun resolveVibrator(): Vibrator? = try {
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
      val manager = appContext.getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager
      manager?.defaultVibrator
    } else {
      @Suppress("DEPRECATION")
      appContext.getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
    }
  } catch (_: Throwable) {
    null // dispositivo sin vibrador o servicio no disponible -> sin haptics, sin crash
  }

  private fun buildStaticTrack(pcm: ShortArray): AudioTrack? = try {
    val bytes = pcm.size * 2 // PCM_16BIT = 2 bytes por muestra
    val track = AudioTrack.Builder()
      .setAudioAttributes(
        AudioAttributes.Builder()
          // SONIFICATION, no GAME/MEDIA: es semánticamente lo correcto para un
          // sonido de UI corto (acierto/error), no música ni efectos de juego.
          .setUsage(AudioAttributes.USAGE_ASSISTANCE_SONIFICATION)
          .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
          .build()
      )
      .setAudioFormat(
        AudioFormat.Builder()
          .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
          .setSampleRate(SAMPLE_RATE_HZ)
          .setChannelMask(AudioFormat.CHANNEL_OUT_MONO)
          .build()
      )
      .setBufferSizeInBytes(bytes)
      .setTransferMode(AudioTrack.MODE_STATIC)
      .build()
    track.write(pcm, 0, pcm.size, AudioTrack.WRITE_BLOCKING)
    track
  } catch (_: Throwable) {
    null // dispositivo/emulador sin salida de audio utilizable -> se degrada a silencio
  }

  /**
   * Onda senoidal simple con envolvente de fade-in/out configurable (`fadeMs`) para
   * evitar el "click"/pop audible que deja un corte abrupto de amplitud al principio/
   * final del buffer — un detalle chico pero es la diferencia entre sonar "pulido" o
   * "casero". Envolventes más largas (20-25ms, ver tonos arriba) también suavizan el
   * ataque percibido del sonido, no solo el pop.
   */
  private fun sineBuffer(frequencyHz: Int, durationMs: Int, amplitude: Double = 0.6, fadeMs: Double = 8.0): ShortArray {
    val sampleCount = (SAMPLE_RATE_HZ * durationMs / 1000.0).toInt()
    val angularStep = 2.0 * PI * frequencyHz / SAMPLE_RATE_HZ
    val fadeSamples = (SAMPLE_RATE_HZ * fadeMs / 1000.0).toInt().coerceAtLeast(1)
    return ShortArray(sampleCount) { i ->
      val envelope = when {
        i < fadeSamples -> i.toDouble() / fadeSamples
        i > sampleCount - fadeSamples -> (sampleCount - i).toDouble() / fadeSamples
        else -> 1.0
      }
      (sin(angularStep * i) * amplitude * envelope * Short.MAX_VALUE).toInt().toShort()
    }
  }

  private fun concatenate(vararg buffers: ShortArray): ShortArray {
    val gapSamples = (SAMPLE_RATE_HZ * 0.015).toInt() // 15ms de silencio entre notas
    val gap = ShortArray(gapSamples)
    val total = buffers.sumOf { it.size } + gap.size * (buffers.size - 1).coerceAtLeast(0)
    val result = ShortArray(total)
    var offset = 0
    buffers.forEachIndexed { index, buffer ->
      buffer.copyInto(result, offset)
      offset += buffer.size
      if (index != buffers.lastIndex) {
        gap.copyInto(result, offset)
        offset += gap.size
      }
    }
    return result
  }

  companion object {
    private const val SAMPLE_RATE_HZ = 44_100
  }
}
