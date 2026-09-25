package com.example.data

import kotlin.math.abs
import kotlin.math.exp
import kotlin.math.sqrt

/**
 * Posición del usuario respecto de una distribución de referencia (fase A, sin servidor).
 *
 * IMPORTANTE: la distribución inicial ([PROVISIONAL_MEAN], [PROVISIONAL_SD]) es un SUPUESTO de diseño, no un
 * dato medido: la app todavía no tiene usuarios que la calibren. Por eso la interfaz la rotula como
 * "estimación provisional". Cuando exista una muestra real (fase B, servidor) se reemplaza por el histograma
 * observado por juego y grupo de edad. Ver NeuroVida/CLAUDE.md.
 */
object Percentile {
  /** Rating normalizado (0..1) supuesto de un usuario típico y su dispersión. */
  const val PROVISIONAL_MEAN = 0.45f
  const val PROVISIONAL_SD = 0.20f

  /** Función de distribución normal estándar (aprox. de Abramowitz-Stegun 7.1.26 para erf). */
  fun normalCdf(z: Double): Double {
    val x = abs(z) / sqrt(2.0)
    val t = 1.0 / (1.0 + 0.3275911 * x)
    val poly = ((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592
    val erf = 1.0 - poly * t * exp(-x * x)
    return if (z >= 0) 0.5 * (1.0 + erf) else 0.5 * (1.0 - erf)
  }

  /** Percentil (1..99) de un rating 0..1 frente a la distribución de referencia. */
  fun of(rating: Float, mean: Float = PROVISIONAL_MEAN, sd: Float = PROVISIONAL_SD): Int {
    val z = (rating - mean) / sd.toDouble()
    return (normalCdf(z) * 100).toInt().coerceIn(1, 99)
  }

  /** Densidad normal de la referencia en x (para dibujar la campana). */
  fun density(x: Float, mean: Float = PROVISIONAL_MEAN, sd: Float = PROVISIONAL_SD): Float {
    val z = (x - mean) / sd
    return exp(-0.5f * z * z)
  }
}
