package com.example.ui.components

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

/**
 * Desplazamiento (en px) de la pantalla que "viaja por el espacio" (el camino de Hoy). El fondo lo lee SOLO al
 * dibujar, asi que actualizarlo no recompone nada: las estrellas se mueven en capas a distinta velocidad
 * (paralaje) y las cercanas dejan una estela cuando el desplazamiento es rapido.
 */
object CosmosScroll {
  var offset by mutableFloatStateOf(0f)
}

private class Star(
  val angle: Float, val p0: Float, val r: Float, val phase: Float, val speed: Float, val warm: Boolean,
  /** Profundidad: 0.25 lejana (casi no se mueve), 0.6 media, 1.1 cercana (se mueve mas y deja estela). */
  val depth: Float
)

/**
 * Fondo "cosmos" de NeuroVida: degradado azul noche con nebulosas azul y coral, ~110 estrellas en 3 capas de
 * profundidad que titilan y se desplazan con [CosmosScroll], y dos olas translucidas abajo (agua). Todo se
 * anima leyendo el estado SOLO dentro del dibujo, sin recomposiciones.
 */
@Composable
fun CosmosBackground(modifier: Modifier = Modifier) {
  val stars = remember {
    val rnd = Random(11)
    val depths = floatArrayOf(0.25f, 0.6f, 1.1f)
    List(150) { i ->
      val depth = depths[i % 3]
      Star(
        angle = rnd.nextFloat() * (2 * PI).toFloat(), p0 = rnd.nextFloat(),
        r = 0.5f + depth * (0.8f + rnd.nextFloat() * 1.2f),
        phase = rnd.nextFloat() * (2 * PI).toFloat(),
        speed = 0.5f + rnd.nextFloat() * 1.5f,
        warm = rnd.nextFloat() < 0.18f,
        depth = depth
      )
    }
  }
  val transition = rememberInfiniteTransition(label = "cosmos")
  val t by transition.animateFloat(
    initialValue = 0f,
    targetValue = (2 * PI).toFloat(),
    animationSpec = infiniteRepeatable(tween(24000, easing = LinearEasing), RepeatMode.Restart),
    label = "cosmosTime"
  )
  // [ultimo desplazamiento, estela suavizada]
  val trail = remember { floatArrayOf(0f, 0f, 0f) }

  Canvas(modifier = modifier.fillMaxSize()) {
    val w = size.width
    val h = size.height
    val time = t
    val scroll = CosmosScroll.offset
    // La primera lectura solo fija la posicion inicial (evita una estela falsa al abrir la pantalla)
    if (trail[2] == 0f) { trail[0] = scroll; trail[2] = 1f }
    val delta = scroll - trail[0]
    trail[0] = scroll
    trail[1] = trail[1] * 0.8f + delta * 0.2f
    val streak = trail[1].coerceIn(-70f, 70f)

    drawRect(
      Brush.verticalGradient(
        listOf(Color(0xFF04061C), Color(0xFF080E3A), Color(0xFF101A58)),
        startY = 0f, endY = h
      )
    )
    // Nebulosas: muy lejanas, se desplazan poco
    val nebulaShift = (scroll * 0.06f) % (h * 0.6f)
    drawCircle(
      Brush.radialGradient(listOf(Color(0x444CC9F0), Color.Transparent), Offset(w * 0.9f, h * 0.10f - nebulaShift), w * 0.9f),
      radius = w * 0.9f, center = Offset(w * 0.9f, h * 0.10f - nebulaShift)
    )
    drawCircle(
      Brush.radialGradient(listOf(Color(0x30B8A4FF), Color.Transparent), Offset(w * 0.1f, h * 0.55f - nebulaShift * 0.6f), w * 0.8f),
      radius = w * 0.8f, center = Offset(w * 0.1f, h * 0.55f - nebulaShift * 0.6f)
    )
    drawCircle(
      Brush.radialGradient(listOf(Color(0x3DFF6B4A), Color.Transparent), Offset(w * 0.05f, h * 0.95f), w * 0.85f),
      radius = w * 0.85f, center = Offset(w * 0.05f, h * 0.95f)
    )

    // Estrellas en perspectiva: nacen en el punto de fuga (el "horizonte" del camino) y se abren hacia los
    // bordes al acercarse. Al deslizar hacia el futuro el camino se aleja y las estrellas convergen al punto
    // de fuga; hacia el pasado emergen de el. Las cercanas (depth alto) van mas rapido y dejan estela.
    val vx = w * 0.5f
    val vy = h * 0.24f
    val reach = maxOf(w, h) * 1.05f
    stars.forEach { s ->
      val p = (((s.p0 - scroll * s.depth * 0.00032f) % 1f) + 1f) % 1f
      val q = p * p                                   // perspectiva: acelera al acercarse
      val dist = reach * q
      val x = vx + cos(s.angle) * dist
      val y = vy + sin(s.angle) * dist * 1.25f
      if (x < -20f || x > w + 20f || y < -20f || y > h + 20f) return@forEach
      val tw = 0.55f + 0.45f * (0.5f + 0.5f * sin(time * s.speed + s.phase))
      val c = if (s.warm) Color(0xFFFFC38A) else Color.White
      val a = (tw * (0.45f + 0.85f * q) * (0.7f + 0.4f * s.depth)).coerceIn(0f, 1f)
      val size = s.r * (0.8f + 1.8f * q)
      if (abs(streak) > 4f && q > 0.15f) {
        val k = (streak * 0.02f * s.depth).coerceIn(-1.2f, 1.2f)
        drawLine(c.copy(alpha = a * 0.5f), Offset(x, y), Offset(x - cos(s.angle) * dist * k * 0.25f, y - sin(s.angle) * dist * 1.25f * k * 0.25f), size * 1.1f, StrokeCap.Round)
      }
      drawCircle(c.copy(alpha = a), size, Offset(x, y))
      if (size > 2.6f) drawCircle(c.copy(alpha = a * 0.16f), size * 4f, Offset(x, y))
    }

    // Olas de agua (dos capas con fases distintas)
    fun wave(baseY: Float, amp: Float, freq: Float, phase: Float, color: Color) {
      val p = Path()
      p.moveTo(0f, h)
      var x = 0f
      while (x <= w + 12f) {
        val y = baseY + amp * sin(x / w * freq * 2 * PI.toFloat() + phase)
        p.lineTo(x, y)
        x += 12f
      }
      p.lineTo(w, h)
      p.close()
      drawPath(p, color)
    }
    wave(h * 0.90f, h * 0.012f, 1.6f, time * 2f, Color(0x334CC9F0))
    wave(h * 0.93f, h * 0.010f, 2.2f, -time * 3f + 1.3f, Color(0x33FF6B4A))
  }
}
