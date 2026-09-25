package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.rotate
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.drawscope.withTransform
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.ui.theme.Clay
import com.example.ui.theme.FredokaFamily
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * Ícono propio de cada juego, dibujado por código con el sello de arcilla (relleno plano, borde tinta grueso,
 * sombra dura hacia abajo y un brillo), en vez de emojis: los emojis cambian según el fabricante del teléfono,
 * no combinan con el estilo y no dicen nada de la mecánica (ui-ux-pro-max: "no emoji as icons"). Cada ícono
 * sale de lo que se hace en el juego:
 * - Secuencia Lumínica: tablero de 4 fichas con una encendida.
 * - Parejas Ocultas: una carta boca abajo y otra dada vuelta.
 * - Ruta del Tesoro: estrella de mar (los tesoros de playa del juego).
 * - Tinta o Palabra: gota de tinta y tarjeta con palabra.
 * - Cambio de Chip: ficha con flecha y dos flechas de cambio.
 * - Detective de Series: lupa sobre una serie que crece.
 * - Anagramas: dos fichas de letras.
 * - Cálculo Sereno: + − × =.
 * - Comparación: círculo grande > círculo chico.
 * Se dibuja pensado para ir sobre el planeta del color del dominio, pero se lee también sobre fondo claro.
 * Id desconocido: cae al emoji de [com.example.model.GameDefinition.iconEmoji].
 */
@Composable
fun GameIcon(gameId: String, size: Dp, modifier: Modifier = Modifier) {
  if (gameId !in DrawnIcons) {
    Box(modifier.size(size), contentAlignment = Alignment.Center) {
      Text(GameRegistry.getById(gameId)?.iconEmoji ?: "🧠", fontSize = (size.value * 0.62f).sp)
    }
    return
  }
  val measurer = rememberTextMeasurer()
  Canvas(modifier.size(size)) {
    val k = this.size.minDimension / 100f
    val dx = (this.size.width - 100f * k) / 2f
    val dy = (this.size.height - 100f * k) / 2f
    withTransform({
      translate(dx, dy)
      scale(k, k, Offset.Zero)
    }) {
      drawGameIcon(gameId, measurer)
    }
  }
}

private val DrawnIcons = setOf(
  "secuencia", "parejas", "rutatesoro", "stroop", "cambiochip", "series", "anagramas", "calculo", "comparacion"
)

// Todo en un lienzo de 100x100 unidades.
private const val Line = 5f       // borde tinta
private const val Drop = 4.5f     // sombra dura
private val Ink = Clay.Ink
private val Cream = Color(0xFFFFFBF2)

private fun DrawScope.drawGameIcon(id: String, measurer: TextMeasurer) {
  when (id) {
    "secuencia" -> {
      val tiles = listOf(Offset(13f, 13f), Offset(53f, 13f), Offset(13f, 53f), Offset(53f, 53f))
      tiles.forEachIndexed { i, o ->
        clay(roundRect(o.x, o.y, 34f, 34f, 9f), if (i == 1) Clay.Sun else Cream, gloss = i == 1)
      }
      sparkle(Offset(84f, 12f), 11f, Color.White)
    }
    "parejas" -> {
      rotate(-12f, Offset(36f, 52f)) {
        clay(roundRect(14f, 20f, 42f, 60f, 8f), Clay.Grape)
        clay(diamond(Offset(35f, 50f), 10f, 14f), Cream, border = 3.5f, shadow = false)
      }
      rotate(10f, Offset(64f, 48f)) {
        clay(roundRect(42f, 16f, 42f, 60f, 8f), Cream)
        clay(star(Offset(63f, 46f), 15f, 7f, 5), Clay.Coral, border = 4f, shadow = false)
      }
    }
    "rutatesoro" -> {
      clay(star(Offset(50f, 53f), 42f, 17f, 5, round = true), Clay.Coral, gloss = false)
      // Textura de puntitos de la estrella de mar
      for (i in 0 until 5) {
        val a = (-90f + i * 72f) * PI.toFloat() / 180f
        drawCircle(Cream, 3.2f, Offset(50f + cos(a) * 20f, 53f + sin(a) * 20f))
      }
      drawCircle(Cream, 4f, Offset(50f, 53f))
    }
    "stroop" -> {
      clay(roundRect(34f, 50f, 54f, 34f, 9f), Cream)
      drawLine(Clay.Sky, Offset(44f, 62f), Offset(78f, 62f), 7f, StrokeCap.Round)
      drawLine(Clay.Coral, Offset(44f, 73f), Offset(66f, 73f), 7f, StrokeCap.Round)
      clay(inkDrop(Offset(30f, 44f), 20f), Clay.Coral, gloss = true)
    }
    "cambiochip" -> {
      clay(roundRect(24f, 24f, 52f, 52f, 12f), Cream)
      arrow(Offset(50f, 50f), 17f, Clay.Coral)
      curvedArrow(Offset(50f, 50f), 44f, 200f, 70f, Clay.Sky)
      curvedArrow(Offset(50f, 50f), 44f, 20f, 70f, Clay.Sky)
    }
    "series" -> {
      // Lupa de detective sobre una serie que crece (tres puntos cada vez más grandes).
      rotate(45f, Offset(40f, 40f)) { clay(roundRect(64f, 31.5f, 34f, 17f, 8.5f), Clay.Sun) }
      clay(circle(Offset(40f, 40f), 31f), Cream, gloss = true)
      clay(circle(Offset(23f, 47f), 5f), Clay.Sky, border = 3.5f, shadow = false)
      clay(circle(Offset(38f, 43f), 7f), Clay.Coral, border = 3.5f, shadow = false)
      clay(circle(Offset(56f, 37f), 9.5f), Clay.Sun, border = 3.5f, shadow = false)
    }
    "anagramas" -> {
      rotate(-10f, Offset(33f, 50f)) {
        clay(roundRect(12f, 28f, 40f, 44f, 10f), Cream)
        letter(measurer, "A", Offset(32f, 50f))
      }
      rotate(9f, Offset(67f, 50f)) {
        clay(roundRect(48f, 28f, 40f, 44f, 10f), Clay.Sun)
        letter(measurer, "Z", Offset(68f, 50f))
      }
    }
    "calculo" -> {
      val bar = 9f
      // + (arriba izquierda)
      clay(plus(Offset(28f, 28f), 34f, bar), Clay.Coral)
      // − (arriba derecha)
      clay(roundRect(56f, 28f - bar / 2f, 34f, bar, bar / 2f), Cream)
      // × (abajo izquierda)
      rotate(45f, Offset(28f, 72f)) { clay(plus(Offset(28f, 72f), 34f, bar), Clay.Sun) }
      // = (abajo derecha)
      clay(roundRect(56f, 64f - bar / 2f, 34f, bar, bar / 2f), Cream)
      clay(roundRect(56f, 80f - bar / 2f, 34f, bar, bar / 2f), Cream)
    }
    "comparacion" -> {
      clay(circle(Offset(22f, 50f), 20f), Cream, gloss = true)
      clay(circle(Offset(87f, 50f), 10f), Cream)
      chevron(Offset(58f, 50f), 13f, Clay.Sun)
    }
  }
}

// ---------- Primitivas de arcilla ----------

/** Figura de arcilla: sombra dura tinta hacia abajo, relleno, borde tinta y (opcional) brillo arriba a la izquierda. */
private fun DrawScope.clay(
  path: Path,
  fill: Color,
  gloss: Boolean = false,
  border: Float = Line,
  shadow: Boolean = true
) {
  if (shadow) translate(0f, Drop) { drawPath(path, Ink) }
  drawPath(path, fill)
  drawPath(path, Ink, style = Stroke(border, join = StrokeJoin.Round, cap = StrokeCap.Round))
  if (gloss) {
    val b = path.getBounds()
    drawOval(
      Color.White.copy(alpha = 0.55f),
      topLeft = Offset(b.left + b.width * 0.18f, b.top + b.height * 0.14f),
      size = androidx.compose.ui.geometry.Size(b.width * 0.34f, b.height * 0.16f)
    )
  }
}

private fun roundRect(x: Float, y: Float, w: Float, h: Float, r: Float) =
  Path().apply { addRoundRect(RoundRect(x, y, x + w, y + h, CornerRadius(r))) }

private fun circle(c: Offset, r: Float) = Path().apply { addOval(Rect(c.x - r, c.y - r, c.x + r, c.y + r)) }

private fun diamond(c: Offset, w: Float, h: Float) = Path().apply {
  moveTo(c.x, c.y - h); lineTo(c.x + w, c.y); lineTo(c.x, c.y + h); lineTo(c.x - w, c.y); close()
}

private fun plus(c: Offset, len: Float, bar: Float) = Path().apply {
  addRoundRect(RoundRect(c.x - len / 2f, c.y - bar / 2f, c.x + len / 2f, c.y + bar / 2f, CornerRadius(bar / 2f)))
  addRoundRect(RoundRect(c.x - bar / 2f, c.y - len / 2f, c.x + bar / 2f, c.y + len / 2f, CornerRadius(bar / 2f)))
}

/** Estrella de [points] puntas; con [round] las puntas quedan romas (estrella de mar). */
private fun star(c: Offset, outer: Float, inner: Float, points: Int, round: Boolean = false) = Path().apply {
  val n = points * 2
  val pts = (0 until n).map { i ->
    val a = (-90f + i * 360f / n) * PI.toFloat() / 180f
    val r = if (i % 2 == 0) outer else inner
    Offset(c.x + cos(a) * r, c.y + sin(a) * r)
  }
  if (!round) {
    moveTo(pts[0].x, pts[0].y); pts.drop(1).forEach { lineTo(it.x, it.y) }; close()
  } else {
    // Curvas que pasan cerca de cada punta: brazos gorditos.
    val mid = { a: Offset, b: Offset -> Offset((a.x + b.x) / 2f, (a.y + b.y) / 2f) }
    val start = mid(pts[n - 1], pts[0])
    moveTo(start.x, start.y)
    for (i in 0 until n) {
      val p = pts[i]
      val next = mid(p, pts[(i + 1) % n])
      quadraticBezierTo(p.x, p.y, next.x, next.y)
    }
    close()
  }
}

/** Gota con la punta hacia arriba; [c] = centro de la parte redonda. */
private fun inkDrop(c: Offset, r: Float) = Path().apply {
  moveTo(c.x, c.y - r * 1.9f)
  cubicTo(c.x + r * 0.35f, c.y - r * 1.2f, c.x + r, c.y - r * 0.55f, c.x + r, c.y)
  cubicTo(c.x + r, c.y + r * 0.56f, c.x + r * 0.56f, c.y + r, c.x, c.y + r)
  cubicTo(c.x - r * 0.56f, c.y + r, c.x - r, c.y + r * 0.56f, c.x - r, c.y)
  cubicTo(c.x - r, c.y - r * 0.55f, c.x - r * 0.35f, c.y - r * 1.2f, c.x, c.y - r * 1.9f)
  close()
}

/** Destello de 4 puntas (sin borde): el "brillo" de lo encendido. */
private fun DrawScope.sparkle(c: Offset, r: Float, color: Color) {
  val p = Path().apply {
    moveTo(c.x, c.y - r)
    quadraticBezierTo(c.x, c.y, c.x + r, c.y)
    quadraticBezierTo(c.x, c.y, c.x, c.y + r)
    quadraticBezierTo(c.x, c.y, c.x - r, c.y)
    quadraticBezierTo(c.x, c.y, c.x, c.y - r)
    close()
  }
  drawPath(p, color)
}

/** Trazo grueso "de arcilla": sombra tinta, borde tinta y el color encima. */
private fun DrawScope.clayStroke(path: Path, color: Color, width: Float) {
  translate(0f, Drop) { drawPath(path, Ink, style = Stroke(width + Line * 2f, cap = StrokeCap.Round, join = StrokeJoin.Round)) }
  drawPath(path, Ink, style = Stroke(width + Line * 2f, cap = StrokeCap.Round, join = StrokeJoin.Round))
  drawPath(path, color, style = Stroke(width, cap = StrokeCap.Round, join = StrokeJoin.Round))
}

/** Flecha hacia arriba centrada en [c]. */
private fun DrawScope.arrow(c: Offset, len: Float, color: Color) {
  val p = Path().apply {
    moveTo(c.x, c.y + len); lineTo(c.x, c.y - len)
    moveTo(c.x - len * 0.62f, c.y - len * 0.38f); lineTo(c.x, c.y - len); lineTo(c.x + len * 0.62f, c.y - len * 0.38f)
  }
  drawPath(p, Ink, style = Stroke(9f + 4f, cap = StrokeCap.Round, join = StrokeJoin.Round))
  drawPath(p, color, style = Stroke(9f - 1f, cap = StrokeCap.Round, join = StrokeJoin.Round))
}

/** Arco de [sweep] grados desde [startDeg] (0 = derecha, sentido horario) con punta de flecha al final. */
private fun DrawScope.curvedArrow(c: Offset, r: Float, startDeg: Float, sweep: Float, color: Color) {
  val p = Path().apply { arcTo(Rect(c.x - r, c.y - r, c.x + r, c.y + r), startDeg, sweep, true) }
  val end = (startDeg + sweep) * PI.toFloat() / 180f
  val tip = Offset(c.x + cos(end) * r, c.y + sin(end) * r)
  // Tangente (sentido horario) y normal, para la punta.
  val t = Offset(-sin(end), cos(end))
  val n = Offset(cos(end), sin(end))
  val head = 9f
  p.moveTo(tip.x - t.x * head + n.x * head, tip.y - t.y * head + n.y * head)
  p.lineTo(tip.x + t.x * 2f, tip.y + t.y * 2f)
  p.lineTo(tip.x - t.x * head - n.x * head, tip.y - t.y * head - n.y * head)
  clayStroke(p, color, 6f)
}

/** Signo ">" de arcilla centrado en [c]. */
private fun DrawScope.chevron(c: Offset, r: Float, color: Color) {
  val p = Path().apply {
    moveTo(c.x - r * 0.6f, c.y - r); lineTo(c.x + r * 0.6f, c.y); lineTo(c.x - r * 0.6f, c.y + r)
  }
  clayStroke(p, color, 7f)
}

/** Letra en Fredoka tinta, centrada en [c] (en unidades del lienzo 100x100). */
private fun DrawScope.letter(measurer: TextMeasurer, text: String, c: Offset) {
  // El lienzo está escalado: se pide la fuente en "unidades" (px antes de escalar).
  val style = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = 34f.toSp(), color = Ink)
  val layout = measurer.measure(text, style)
  drawText(layout, topLeft = Offset(c.x - layout.size.width / 2f, c.y - layout.size.height / 2f))
}
