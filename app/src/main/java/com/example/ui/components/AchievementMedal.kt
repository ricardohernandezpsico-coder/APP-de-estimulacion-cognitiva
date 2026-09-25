package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.inset
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.text.TextMeasurer
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.drawText
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.rememberTextMeasurer
import androidx.compose.ui.unit.Dp
import com.example.data.AchievementDef
import com.example.data.AchievementGlyph
import com.example.ui.theme.Clay
import com.example.ui.theme.FredokaFamily
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/** Color de la medalla según su dibujo (los de liga usan el color de su liga). */
fun AchievementDef.medalColor(): Color = tier?.palette()?.base ?: when (glyph) {
  AchievementGlyph.PLAY, AchievementGlyph.CALENDAR -> Clay.Sun
  AchievementGlyph.FLAME -> Clay.Coral
  AchievementGlyph.COMPASS, AchievementGlyph.BOLT -> Clay.Sky
  AchievementGlyph.HEXAGON, AchievementGlyph.CHECK -> Clay.Lime
  AchievementGlyph.STAR -> Clay.Grape
  AchievementGlyph.SHIELD -> Clay.Sun
}

/**
 * Medalla de un logro: disco de arcilla del color del logro (borde tinta, sombra dura, brillo), dibujo propio en
 * el centro y, si lleva cifra (7 días, 100 partidas), una píldora sol abajo. Bloqueada: silueta translúcida.
 */
@Composable
fun AchievementMedal(def: AchievementDef, unlocked: Boolean, size: Dp, modifier: Modifier = Modifier) {
  val measurer = rememberTextMeasurer()
  Canvas(modifier.size(size)) { drawAchievementMedal(def, unlocked, measurer) }
}

/** Dibuja la medalla en todo el lienzo (cuadrado). También la usa la tarjeta para compartir. */
fun DrawScope.drawAchievementMedal(def: AchievementDef, unlocked: Boolean, measurer: TextMeasurer?) {
  val w = size.minDimension
  val c = Offset(size.width / 2f, size.height / 2f - w * 0.03f)
  val r = w * 0.42f
  val ink = Clay.Ink
  val color = def.medalColor()

  if (!unlocked) {
    drawCircle(Color.White.copy(alpha = 0.07f), r, c)
    drawCircle(Color.White.copy(alpha = 0.22f), r, c, style = Stroke(w * 0.02f))
    drawGlyph(def, c, r * 1.45f, fill = Color.White.copy(alpha = 0.26f), outline = null)
    return
  }

  drawCircle(ink, r, Offset(c.x, c.y + w * 0.045f))                    // sombra dura
  drawCircle(color, r, c)                                               // aro
  drawCircle(lerp(color, Color.White, 0.30f), r * 0.76f, c)             // cara
  drawCircle(ink.copy(alpha = 0.35f), r * 0.76f, c, style = Stroke(w * 0.012f))
  drawCircle(ink, r - w * 0.017f, c, style = Stroke(w * 0.035f))        // borde tinta
  drawOval(                                                             // brillo
    Color.White.copy(alpha = 0.38f),
    topLeft = Offset(c.x - r * 0.52f, c.y - r * 0.86f),
    size = Size(r * 0.8f, r * 0.26f)
  )
  // Con cifra abajo, el dibujo sube un poco para dejarle lugar a la píldora.
  val glyphCenter = if (def.number != null) Offset(c.x, c.y - r * 0.1f) else c
  drawGlyph(def, glyphCenter, r * (if (def.number != null) 1.3f else 1.45f), fill = Clay.Cream, outline = ink)

  val n = def.number
  if (n != null && measurer != null) {
    val style = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = (w * 0.15f).toSp(), color = ink)
    val layout = measurer.measure("$n", style)
    val pw = layout.size.width + w * 0.12f
    val ph = w * 0.2f
    val top = c.y + r * 0.62f
    val rr = RoundRect(c.x - pw / 2f, top, c.x + pw / 2f, top + ph, CornerRadius(ph / 2f))
    val pill = Path().apply { addRoundRect(rr) }
    translate(0f, w * 0.02f) { drawPath(pill, ink) }
    drawPath(pill, Clay.Sun)
    drawPath(pill, ink, style = Stroke(w * 0.022f))
    drawText(layout, topLeft = Offset(c.x - layout.size.width / 2f, top + (ph - layout.size.height) / 2f))
  }
}

/** Dibujo central, dentro de un cuadrado de lado [box] centrado en [c]. [outline] null = silueta sin borde. */
private fun DrawScope.drawGlyph(def: AchievementDef, c: Offset, box: Float, fill: Color, outline: Color?) {
  val u = box / 2f // 1 unidad del dibujo = medio lado
  fun p(x: Float, y: Float) = Offset(c.x + x * u, c.y + y * u)
  val strokeW = size.minDimension * 0.03f
  fun shape(path: Path, f: Color = fill) {
    drawPath(path, f)
    if (outline != null) drawPath(path, outline, style = Stroke(strokeW, join = StrokeJoin.Round, cap = StrokeCap.Round))
  }
  when (def.glyph) {
    AchievementGlyph.PLAY -> shape(Path().apply {
      val a = p(-0.32f, -0.5f); moveTo(a.x, a.y)
      p(0.52f, -0.02f).let { lineTo(it.x, it.y) }
      p(-0.32f, 0.46f).let { lineTo(it.x, it.y) }
      close()
    })
    AchievementGlyph.FLAME -> {
      shape(flame(c, u, 1f, lift = -0.02f))
      if (outline != null) shape(flame(c, u, 0.46f, lift = 0.26f), Clay.Sun)
    }
    AchievementGlyph.CALENDAR -> {
      shape(Path().apply { addRoundRect(RoundRect(Rect(p(-0.5f, -0.4f), p(0.5f, 0.5f)), CornerRadius(u * 0.14f))) })
      shape(Path().apply { addRoundRect(RoundRect(Rect(p(-0.5f, -0.4f), p(0.5f, -0.12f)), CornerRadius(u * 0.14f))) },
        if (outline != null) Clay.Coral else fill)
      if (outline != null) {
        drawLine(outline, p(-0.2f, 0.15f), p(-0.02f, 0.32f), strokeW * 1.4f, StrokeCap.Round)
        drawLine(outline, p(-0.02f, 0.32f), p(0.26f, 0.02f), strokeW * 1.4f, StrokeCap.Round)
      }
    }
    AchievementGlyph.COMPASS -> {
      shape(Path().apply { addOval(Rect(p(-0.52f, -0.52f), p(0.52f, 0.52f))) })
      val north = Path().apply {
        p(0f, -0.4f).let { moveTo(it.x, it.y) }; p(0.14f, 0f).let { lineTo(it.x, it.y) }
        p(-0.14f, 0f).let { lineTo(it.x, it.y) }; close()
      }
      val south = Path().apply {
        p(0f, 0.4f).let { moveTo(it.x, it.y) }; p(0.14f, 0f).let { lineTo(it.x, it.y) }
        p(-0.14f, 0f).let { lineTo(it.x, it.y) }; close()
      }
      if (outline != null) { drawPath(north, Clay.Coral); drawPath(south, outline.copy(alpha = 0.55f)) }
    }
    AchievementGlyph.HEXAGON -> {
      fun hex(rad: Float) = Path().apply {
        for (i in 0 until 6) {
          val a = (-90f + i * 60f) * PI.toFloat() / 180f
          val q = Offset(c.x + cos(a) * rad * u, c.y + sin(a) * rad * u)
          if (i == 0) moveTo(q.x, q.y) else lineTo(q.x, q.y)
        }
        close()
      }
      shape(hex(0.56f))
      if (outline != null) shape(hex(0.26f), Clay.Sky)
    }
    AchievementGlyph.STAR -> shape(Path().apply {
      for (i in 0 until 10) {
        val a = (-90f + i * 36f) * PI.toFloat() / 180f
        val rad = if (i % 2 == 0) 0.58f else 0.26f
        val q = Offset(c.x + cos(a) * rad * u, c.y + sin(a) * rad * u - u * 0.04f)
        if (i == 0) moveTo(q.x, q.y) else lineTo(q.x, q.y)
      }
      close()
    })
    AchievementGlyph.BOLT -> shape(Path().apply {
      val pts = listOf(p(0.1f, -0.58f), p(-0.34f, 0.06f), p(-0.02f, 0.06f), p(-0.12f, 0.58f), p(0.34f, -0.1f), p(0.02f, -0.1f))
      moveTo(pts[0].x, pts[0].y); pts.drop(1).forEach { lineTo(it.x, it.y) }; close()
    })
    AchievementGlyph.CHECK -> {
      val check = Path().apply {
        p(-0.4f, 0.02f).let { moveTo(it.x, it.y) }; p(-0.1f, 0.32f).let { lineTo(it.x, it.y) }
        p(0.44f, -0.28f).let { lineTo(it.x, it.y) }
      }
      if (outline != null) drawPath(check, outline, style = Stroke(u * 0.34f, cap = StrokeCap.Round, join = StrokeJoin.Round))
      drawPath(check, fill, style = Stroke(u * 0.2f, cap = StrokeCap.Round, join = StrokeJoin.Round))
    }
    AchievementGlyph.SHIELD -> {
      val tier = def.tier ?: return
      val sw = u * 1.05f
      val sh = sw * 1.12f
      if (outline != null) {
        inset(c.x - sw / 2f, c.y - sh / 2f, size.width - (c.x + sw / 2f), size.height - (c.y + sh / 2f)) {
          drawLeagueShield(tier, pips = 0)
        }
      } else {
        shape(Path().apply { addOval(Rect(p(-0.4f, -0.45f), p(0.4f, 0.45f))) })
      }
    }
  }
}

/** Llama clásica: punta arriba, base redonda y una lengüeta hacia el centro. [k] escala, [lift] la sube o baja. */
private fun flame(c: Offset, u: Float, k: Float, lift: Float) = Path().apply {
  fun q(x: Float, y: Float) = Offset(c.x + x * 1.25f * u * k, c.y + (y * k + lift) * u)
  val start = q(0.04f, -0.64f)
  moveTo(start.x, start.y)
  val segs = listOf(
    floatArrayOf(0.16f, -0.40f, 0.46f, -0.22f, 0.46f, 0.16f),
    floatArrayOf(0.46f, 0.42f, 0.26f, 0.58f, 0f, 0.58f),
    floatArrayOf(-0.26f, 0.58f, -0.46f, 0.42f, -0.46f, 0.16f),
    floatArrayOf(-0.46f, -0.06f, -0.30f, -0.18f, -0.22f, -0.34f),
    floatArrayOf(-0.14f, -0.20f, -0.10f, -0.14f, -0.04f, -0.12f),
    floatArrayOf(0.02f, -0.30f, 0.02f, -0.48f, 0.04f, -0.64f)
  )
  for (sg in segs) {
    val a = q(sg[0], sg[1]); val b = q(sg[2], sg[3]); val e = q(sg[4], sg[5])
    cubicTo(a.x, a.y, b.x, b.y, e.x, e.y)
  }
  close()
}
