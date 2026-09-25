package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRankInfo
import com.example.model.RankTier
import kotlin.math.cos
import kotlin.math.sin

/** Paleta metalica por liga: luz, base y sombra (para el degradado del escudo). */
data class ShieldPalette(val light: Color, val base: Color, val dark: Color, val glow: Color)

fun RankTier.palette(): ShieldPalette = when (this) {
  RankTier.BRONCE -> ShieldPalette(Color(0xFFF3B27A), Color(0xFFCD7F32), Color(0xFF8A4B14), Color(0xFFFFB86B))
  RankTier.PLATA -> ShieldPalette(Color(0xFFF4F6FA), Color(0xFFB7BFCC), Color(0xFF6E7787), Color(0xFFDDE6F5))
  RankTier.ORO -> ShieldPalette(Color(0xFFFFF0A8), Color(0xFFF5B301), Color(0xFFB37400), Color(0xFFFFD84D))
  RankTier.PLATINO -> ShieldPalette(Color(0xFFE6FBFF), Color(0xFF5FD3E6), Color(0xFF1B8CA3), Color(0xFF7DF0FF))
  RankTier.ESMERALDA -> ShieldPalette(Color(0xFFB9FFD9), Color(0xFF17C97A), Color(0xFF07683E), Color(0xFF4DFFA8))
  RankTier.DIAMANTE -> ShieldPalette(Color(0xFFDCEBFF), Color(0xFF4C8DFF), Color(0xFF1B3FB3), Color(0xFF8FB9FF))
  RankTier.MAESTRO -> ShieldPalette(Color(0xFFF2D4FF), Color(0xFFB04DFF), Color(0xFF5B1B99), Color(0xFFD48CFF))
}

private fun shieldPath(w: Float, h: Float, inset: Float): Path {
  val l = inset
  val r = w - inset
  val t = inset
  val b = h - inset
  val cx = w / 2
  return Path().apply {
    moveTo(cx, t)
    cubicTo(cx + (r - cx) * 0.35f, t + (b - t) * 0.05f, r - (r - cx) * 0.25f, t + (b - t) * 0.06f, r, t + (b - t) * 0.12f)
    lineTo(r, t + (b - t) * 0.50f)
    cubicTo(r, t + (b - t) * 0.76f, cx + (r - cx) * 0.55f, t + (b - t) * 0.90f, cx, b)
    cubicTo(cx - (r - cx) * 0.55f, t + (b - t) * 0.90f, l, t + (b - t) * 0.76f, l, t + (b - t) * 0.50f)
    lineTo(l, t + (b - t) * 0.12f)
    cubicTo(l + (r - cx) * 0.25f, t + (b - t) * 0.06f, cx - (r - cx) * 0.35f, t + (b - t) * 0.05f, cx, t)
    close()
  }
}

private fun DrawScope.drawStar(center: Offset, radius: Float, color: Color) {
  val path = Path()
  for (i in 0 until 10) {
    val rr = if (i % 2 == 0) radius else radius * 0.45f
    val a = -Math.PI / 2 + i * Math.PI / 5
    val p = Offset(center.x + (rr * cos(a)).toFloat(), center.y + (rr * sin(a)).toFloat())
    if (i == 0) path.moveTo(p.x, p.y) else path.lineTo(p.x, p.y)
  }
  path.close()
  drawPath(path, color)
}

/**
 * Escudo de liga dibujado por codigo (arte propio): borde metalico, cara con degradado, bisel, brillo y
 * estrellas segun la division (Maestro lleva corona de 3 estrellas). [pips] = estrellas a mostrar (1..5).
 */
@Composable
fun LeagueShield(tier: RankTier, modifier: Modifier = Modifier, size: Dp = 72.dp, pips: Int = 0, glow: Boolean = false) {
  val pal = tier.palette()
  Canvas(modifier = modifier.size(width = size, height = size * 1.12f)) {
    val w = this.size.width
    val h = this.size.height
    val edge = w * 0.03f
    if (glow) {
      drawCircle(
        Brush.radialGradient(listOf(pal.glow.copy(alpha = 0.55f), Color.Transparent), Offset(w / 2, h * 0.5f), w * 0.85f),
        radius = w * 0.85f, center = Offset(w / 2, h * 0.5f)
      )
    }
    // Sombra
    drawPath(shieldPath(w, h, edge), Color.Black.copy(alpha = 0.18f))
    // Borde metalico
    drawPath(shieldPath(w, h, edge), Brush.verticalGradient(listOf(pal.light, pal.dark), 0f, h))
    // Cara
    val faceInset = w * 0.085f
    val face = shieldPath(w, h, faceInset)
    drawPath(face, Brush.verticalGradient(listOf(pal.light, pal.base, pal.dark), 0f, h))
    // Bisel interior
    drawPath(shieldPath(w, h, w * 0.13f), Brush.verticalGradient(listOf(pal.dark.copy(alpha = 0.35f), pal.base.copy(alpha = 0.10f)), 0f, h))
    drawPath(shieldPath(w, h, w * 0.13f), pal.light.copy(alpha = 0.55f), style = Stroke(w * 0.012f))
    // Brillo especular arriba-izquierda
    drawPath(
      Path().apply {
        moveTo(w * 0.22f, h * 0.16f)
        cubicTo(w * 0.32f, h * 0.10f, w * 0.60f, h * 0.10f, w * 0.72f, h * 0.15f)
        cubicTo(w * 0.55f, h * 0.24f, w * 0.34f, h * 0.30f, w * 0.22f, h * 0.36f)
        close()
      },
      Color.White.copy(alpha = 0.35f)
    )
    // Emblema: estrella central grande
    val c = Offset(w / 2, h * 0.47f)
    drawStar(Offset(c.x, c.y + w * 0.012f), w * 0.20f, pal.dark.copy(alpha = 0.55f))
    drawStar(c, w * 0.20f, Color.White.copy(alpha = 0.92f))
    // Divisiones: pequenas estrellas bajo el emblema
    val n = pips.coerceIn(0, 5)
    if (n > 0) {
      val gap = w * 0.13f
      val startX = w / 2 - gap * (n - 1) / 2f
      for (i in 0 until n) drawStar(Offset(startX + i * gap, h * 0.72f), w * 0.05f, Color.White.copy(alpha = 0.95f))
    }
  }
}

/** Division 1..5 -> estrellas encendidas (5 = recien ascendido, 1 = a punto de ascender, como en el ladder). */
fun GameRankInfo.pipCount(): Int = if (tier == RankTier.MAESTRO) 3 else (6 - (division ?: 5)).coerceIn(1, 5)

@Composable
fun GlobalLeagueCard(tier: RankTier, rating: Int, progress: Float, modifier: Modifier = Modifier) {
  val pal = tier.palette()
  val next = RankTier.entries.getOrNull(tier.ordinal + 1)
  com.example.ui.theme.ClayLightCard(modifier = modifier.fillMaxWidth()) {
    Row(
      modifier = Modifier.padding(22.dp),
      verticalAlignment = Alignment.CenterVertically,
      horizontalArrangement = Arrangement.spacedBy(20.dp)
    ) {
      LeagueShield(tier = tier, size = 104.dp, pips = ((progress * 5).toInt() + 1).coerceIn(1, 5), glow = true)
      Column(verticalArrangement = Arrangement.spacedBy(6.dp), modifier = Modifier.weight(1f)) {
        Text("Tu liga", style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(tier.tierName, fontSize = 30.sp, fontWeight = FontWeight.Black, color = pal.base)
        Text(
          text = "$rating trofeos" + if (next != null) " · faltan ${(next.minRating - rating).coerceAtLeast(0)} para ${next.tierName}" else "",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Box(
          modifier = Modifier
            .fillMaxWidth()
            .height(8.dp)
            .clip(RoundedCornerShape(4.dp))
            .background(MaterialTheme.colorScheme.surfaceVariant)
        ) {
          Box(
            modifier = Modifier
              .fillMaxWidth(progress.coerceIn(0.03f, 1f))
              .height(8.dp)
              .clip(RoundedCornerShape(4.dp))
              .background(pal.base)
          )
        }
      }
    }
  }
}
