package com.example.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
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
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.Percentile
import com.example.model.DomainType
import com.example.model.GameRegistry
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * Visualizaciones de la pantalla de Progreso basadas en el rating del DDA común (0..1 por juego, ver
 * docs/DDA-comun.md). Es un indicador interno de entrenamiento, no una medida clínica validada.
 */

/** Rating 0..1 por juego; null = todavía sin dato. */
typealias GameLevels = Map<String, Float?>

fun domainScores(levels: GameLevels): Map<DomainType, Float?> =
  DomainType.values().associateWith { d ->
    val vals = GameRegistry.allGames.filter { it.domain == d }.mapNotNull { levels[it.id] }
    if (vals.isEmpty()) null else vals.average().toFloat()
  }

/** Índice general 0..100: promedio de los juegos con dato; null si no hay ninguno. */
fun overallIndex(levels: GameLevels): Int? {
  val vals = levels.values.filterNotNull()
  return if (vals.isEmpty()) null else (vals.average() * 100).toInt().coerceIn(0, 100)
}

fun levelWord(v: Float): String = when {
  v < 0.2f -> "Inicial"
  v < 0.4f -> "En desarrollo"
  v < 0.6f -> "Intermedio"
  v < 0.8f -> "Avanzado"
  else -> "Experto"
}

@Composable
fun ProgressHeroCard(levels: GameLevels, accent: Color, modifier: Modifier = Modifier) {
  val index = overallIndex(levels)
  val measured = levels.values.count { it != null }
  val track = MaterialTheme.colorScheme.surfaceVariant
  Column(modifier = modifier.fillMaxWidth()) {
    Row(
      modifier = Modifier.padding(22.dp),
      verticalAlignment = Alignment.CenterVertically,
      horizontalArrangement = Arrangement.spacedBy(20.dp)
    ) {
      Box(modifier = Modifier.size(112.dp), contentAlignment = Alignment.Center) {
        Canvas(modifier = Modifier.size(112.dp)) {
          val stroke = 12.dp.toPx()
          val inset = stroke / 2
          val arcSize = Size(size.width - stroke, size.height - stroke)
          drawArc(track, -90f, 360f, false, Offset(inset, inset), arcSize, style = Stroke(stroke, cap = StrokeCap.Round))
          if (index != null) {
            drawArc(accent, -90f, 360f * index / 100f, false, Offset(inset, inset), arcSize, style = Stroke(stroke, cap = StrokeCap.Round))
          }
        }
        Text(
          text = index?.toString() ?: "–",
          fontSize = 34.sp,
          fontWeight = FontWeight.Black,
          color = MaterialTheme.colorScheme.onSurface
        )
      }
      Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        Text("Tu nivel general", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
        Text(
          text = if (index == null) "Juega para medir tu nivel" else levelWord(index / 100f),
          style = MaterialTheme.typography.titleLarge,
          fontWeight = FontWeight.Black,
          color = accent
        )
        Text(
          text = "$measured de ${GameRegistry.allGames.size} juegos medidos",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )
      }
    }
  }
}

@Composable
fun DomainRadarCard(levels: GameLevels, modifier: Modifier = Modifier) {
  val scores = domainScores(levels)
  val domains = DomainType.values().toList()
  val grid = MaterialTheme.colorScheme.outlineVariant
  val fill = MaterialTheme.colorScheme.primary
  Column(modifier = modifier.fillMaxWidth()) {
    Column(modifier = Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
      Text("Perfil cognitivo", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
      Canvas(modifier = Modifier.fillMaxWidth().aspectRatio(1.3f).padding(12.dp)) {
        val cx = size.width / 2
        val cy = size.height / 2
        val r = minOf(cx, cy)
        val n = domains.size
        fun pt(i: Int, f: Float): Offset {
          val a = -PI / 2 + 2 * PI * i / n
          return Offset(cx + (r * f * cos(a)).toFloat(), cy + (r * f * sin(a)).toFloat())
        }
        for (ring in 1..4) {
          val path = Path()
          for (i in 0 until n) {
            val p = pt(i, ring / 4f)
            if (i == 0) path.moveTo(p.x, p.y) else path.lineTo(p.x, p.y)
          }
          path.close()
          drawPath(path, grid, style = Stroke(1.dp.toPx()))
        }
        for (i in 0 until n) drawLine(grid, Offset(cx, cy), pt(i, 1f), 1.dp.toPx())
        val poly = Path()
        for (i in 0 until n) {
          val f = (scores[domains[i]] ?: 0f).coerceIn(0.04f, 1f)
          val p = pt(i, f)
          if (i == 0) poly.moveTo(p.x, p.y) else poly.lineTo(p.x, p.y)
        }
        poly.close()
        drawPath(poly, fill.copy(alpha = 0.25f))
        drawPath(poly, fill, style = Stroke(2.5.dp.toPx()))
        for (i in 0 until n) {
          val f = scores[domains[i]] ?: continue
          drawCircle(domains[i].color, 5.dp.toPx(), pt(i, f.coerceIn(0.04f, 1f)))
        }
      }
      DomainLegend(levels)
    }
  }
}

@Composable
fun DomainLegend(levels: GameLevels, modifier: Modifier = Modifier) {
  val scores = domainScores(levels)
  Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(6.dp)) {
    DomainType.values().forEach { d ->
      val v = scores[d]
      Row(verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.size(10.dp).clip(CircleShape).background(d.color))
        Spacer(Modifier.width(8.dp))
        Text(d.displayName, style = MaterialTheme.typography.labelLarge, modifier = Modifier.weight(1f))
        Text(
          text = if (v == null) "Sin medir" else "${levelWord(v)} · P${Percentile.of(v)}",
          style = MaterialTheme.typography.labelMedium,
          color = if (v == null) MaterialTheme.colorScheme.onSurfaceVariant else d.color,
          fontWeight = FontWeight.SemiBold
        )
      }
    }
  }
}

@Composable
fun GameLevelsCard(levels: GameLevels, modifier: Modifier = Modifier) {
  Column(modifier = modifier.fillMaxWidth()) {
    Column(modifier = Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(14.dp)) {
      Text("Nivel por juego", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
      GameRegistry.allGames.forEach { g ->
        val v = levels[g.id]
        Column(verticalArrangement = Arrangement.spacedBy(5.dp)) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Text(g.title, style = MaterialTheme.typography.labelLarge, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
            Text(
              text = if (v == null) "Sin medir" else "${levelWord(v)} · P${Percentile.of(v)}",
              style = MaterialTheme.typography.labelSmall,
              color = if (v == null) MaterialTheme.colorScheme.onSurfaceVariant else g.domain.color,
              fontWeight = FontWeight.Bold
            )
          }
          Box(
            modifier = Modifier
              .fillMaxWidth()
              .height(8.dp)
              .clip(RoundedCornerShape(4.dp))
              .background(MaterialTheme.colorScheme.surfaceVariant)
          ) {
            if (v != null) {
              Box(
                modifier = Modifier
                  .fillMaxWidth(v.coerceIn(0.04f, 1f))
                  .height(8.dp)
                  .clip(RoundedCornerShape(4.dp))
                  .background(g.domain.color)
              )
            }
          }
        }
      }
      Text(
        "Estimación interna según cómo se adapta la dificultad a ti; no es una medida clínica.",
        style = MaterialTheme.typography.labelSmall,
        color = MaterialTheme.colorScheme.onSurfaceVariant
      )
    }
  }
}

/** Puntaje promedio de los ultimos 7 dias menos el de los 7 anteriores; null si falta alguno de los dos. */
fun weeklyDelta(scoresWithTime: List<Pair<Int, Long>>, now: Long = System.currentTimeMillis()): Int? {
  val week = 7L * 24 * 60 * 60 * 1000
  val recent = scoresWithTime.filter { now - it.second in 0..week }.map { it.first }
  val before = scoresWithTime.filter { now - it.second in (week + 1)..(2 * week) }.map { it.first }
  if (recent.isEmpty() || before.isEmpty()) return null
  return (recent.average() - before.average()).toInt()
}

/**
 * Campana de referencia con la posicion del usuario ("tu estas aqui") y su percentil. La distribucion es
 * PROVISIONAL (supuesto de diseno, ver [Percentile]); se rotula asi hasta que haya datos reales.
 */
@Composable
fun BellCurveCard(levels: GameLevels, scoresWithTime: List<Pair<Int, Long>>, accent: Color, modifier: Modifier = Modifier) {
  val index = overallIndex(levels)
  val rating = index?.let { it / 100f }
  val percentile = rating?.let { Percentile.of(it) }
  val delta = weeklyDelta(scoresWithTime)
  val line = MaterialTheme.colorScheme.outline
  Column(modifier = modifier.fillMaxWidth()) {
    Column(modifier = Modifier.padding(20.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
      Text("Tu posición", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
      Text(
        text = if (percentile == null) "Juega para ver dónde estás"
        else "Por sobre el $percentile % de la referencia",
        style = MaterialTheme.typography.titleLarge,
        fontWeight = FontWeight.Black,
        color = accent
      )
      Canvas(modifier = Modifier.fillMaxWidth().height(120.dp)) {
        val w = size.width
        val h = size.height
        val base = h - 10.dp.toPx()
        val top = 10.dp.toPx()
        fun xOf(v: Float) = v * w
        fun yOf(v: Float) = base - (base - top) * Percentile.density(v)
        val steps = 80
        val curve = Path()
        for (i in 0..steps) {
          val v = i / steps.toFloat()
          if (i == 0) curve.moveTo(xOf(v), yOf(v)) else curve.lineTo(xOf(v), yOf(v))
        }
        if (rating != null) {
          val filled = Path()
          filled.moveTo(0f, base)
          val upTo = (rating * steps).toInt()
          for (i in 0..upTo) {
            val v = i / steps.toFloat()
            filled.lineTo(xOf(v), yOf(v))
          }
          filled.lineTo(xOf(rating), yOf(rating))
          filled.lineTo(xOf(rating), base)
          filled.close()
          drawPath(filled, accent.copy(alpha = 0.22f))
        }
        drawLine(line, Offset(0f, base), Offset(w, base), 1.dp.toPx())
        drawPath(curve, line, style = Stroke(2.dp.toPx(), cap = StrokeCap.Round))
        if (rating != null) {
          drawLine(accent, Offset(xOf(rating), base), Offset(xOf(rating), yOf(rating)), 2.dp.toPx())
          drawCircle(accent, 7.dp.toPx(), Offset(xOf(rating), yOf(rating)))
          drawCircle(Color.White, 3.dp.toPx(), Offset(xOf(rating), yOf(rating)))
        }
      }
      Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        Text("Inicial", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text("Experto", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
      }
      if (delta != null) {
        Text(
          text = when {
            delta > 0 -> "Esta semana: +$delta pts frente a la anterior"
            delta < 0 -> "Esta semana: $delta pts frente a la anterior"
            else -> "Esta semana: igual que la anterior"
          },
          style = MaterialTheme.typography.bodySmall,
          fontWeight = FontWeight.SemiBold,
          color = if (delta >= 0) accent else MaterialTheme.colorScheme.onSurfaceVariant
        )
      }
      Text(
        "Estimación provisional: la referencia es un supuesto de diseño y se irá ajustando con datos reales de uso. No es una medida clínica.",
        style = MaterialTheme.typography.labelSmall,
        color = MaterialTheme.colorScheme.onSurfaceVariant
      )
    }
  }
}
