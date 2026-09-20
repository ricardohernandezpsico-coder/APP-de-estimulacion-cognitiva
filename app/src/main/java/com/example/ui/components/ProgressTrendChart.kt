package com.example.ui.components

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.TrendingDown
import androidx.compose.material.icons.automirrored.filled.TrendingFlat
import androidx.compose.material.icons.automirrored.filled.TrendingUp
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.TouchApp
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.ui.theme.*
import java.text.SimpleDateFormat
import java.util.*
import kotlin.math.roundToInt

enum class ChartTimeRange(val label: String, val limit: Int) {
  LAST_7("7 sesiones", 7),
  LAST_14("14 sesiones", 14),
  LAST_30("30 sesiones", 30),
  ALL("Todas", Int.MAX_VALUE)
}

@Composable
fun ProgressTrendChart(
  history: List<GamePlayResult>,
  modifier: Modifier = Modifier
) {
  var selectedDomain by remember { mutableStateOf<DomainType?>(null) }
  var selectedRange by remember { mutableStateOf(ChartTimeRange.LAST_14) }
  var selectedIndex by remember { mutableStateOf<Int?>(null) }

  val dateFormatter = remember { SimpleDateFormat("dd MMM", Locale.getDefault()) }
  val fullDateFormatter = remember { SimpleDateFormat("dd MMM, HH:mm", Locale.getDefault()) }

  // Filter and sort chronologically (oldest to newest for X axis)
  val filteredData = remember(history, selectedDomain, selectedRange) {
    val domainFiltered = if (selectedDomain != null) {
      history.filter {
        val game = GameRegistry.getById(it.gameId)
        game?.domain == selectedDomain
      }
    } else {
      history
    }

    // Chronological order
    val sorted = domainFiltered.sortedBy { it.timestamp }
    if (selectedRange.limit == Int.MAX_VALUE) {
      sorted
    } else {
      sorted.takeLast(selectedRange.limit)
    }
  }

  // Auto-select the last point when data changes or resets
  LaunchedEffect(filteredData.size) {
    selectedIndex = if (filteredData.isNotEmpty()) filteredData.lastIndex else null
  }

  // Compute stats: average, highest, improvement percentage, trend direction
  val stats = remember(filteredData) {
    if (filteredData.isEmpty()) {
      ChartStats(avgScore = 0, maxScore = 0, improvementPct = 0f, trendSlope = 0f)
    } else {
      val scores = filteredData.map { it.score }
      val avg = scores.average().roundToInt()
      val max = scores.maxOrNull() ?: 0

      // Improvement rate comparing first half with second half
      val improvement = if (scores.size >= 2) {
        val half = (scores.size / 2).coerceAtLeast(1)
        val firstHalfAvg = scores.take(half).average()
        val secondHalfAvg = scores.takeLast(half).average()
        if (firstHalfAvg > 0) {
          (((secondHalfAvg - firstHalfAvg) / firstHalfAvg) * 100).toFloat()
        } else 0f
      } else 0f

      // Simple linear regression slope for trend line
      val n = scores.size
      val slope = if (n >= 2) {
        val xMean = (n - 1) / 2.0
        val yMean = scores.average()
        var numerator = 0.0
        var denominator = 0.0
        for (i in 0 until n) {
          numerator += (i - xMean) * (scores[i] - yMean)
          denominator += (i - xMean) * (i - xMean)
        }
        if (denominator != 0.0) (numerator / denominator).toFloat() else 0f
      } else 0f

      ChartStats(
        avgScore = avg,
        maxScore = max,
        improvementPct = improvement,
        trendSlope = slope
      )
    }
  }

  val activeColor = selectedDomain?.color ?: TealPrimary

  Card(
    modifier = modifier
      .fillMaxWidth()
      .testTag("progress_trend_card"),
    shape = RoundedCornerShape(24.dp),
    colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
    elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
  ) {
    Column(
      modifier = Modifier
        .fillMaxWidth()
        .padding(20.dp),
      verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
      // 1. Header Title & Badge
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        Column(modifier = Modifier.weight(1f)) {
          Text(
            text = "Curva de Rendimiento",
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold,
            color = MaterialTheme.colorScheme.onSurface
          )
          Text(
            text = "Evolución histórica de tus sesiones cognitivas",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }

        // Improvement badge
        if (filteredData.size >= 2) {
          val badgeColor = when {
            stats.improvementPct > 0.5f -> EmeraldAccent
            stats.improvementPct < -0.5f -> Color(0xFFEF4444)
            else -> Slate600
          }
          Surface(
            shape = RoundedCornerShape(14.dp),
            color = badgeColor.copy(alpha = 0.12f)
          ) {
            Row(
              modifier = Modifier.padding(horizontal = 10.dp, vertical = 6.dp),
              verticalAlignment = Alignment.CenterVertically
            ) {
              Icon(
                imageVector = when {
                  stats.improvementPct > 0.5f -> Icons.AutoMirrored.Filled.TrendingUp
                  stats.improvementPct < -0.5f -> Icons.AutoMirrored.Filled.TrendingDown
                  else -> Icons.AutoMirrored.Filled.TrendingFlat
                },
                contentDescription = null,
                tint = badgeColor,
                modifier = Modifier.size(16.dp)
              )
              Spacer(modifier = Modifier.width(4.dp))
              Text(
                text = String.format(Locale.getDefault(), "%+.1f%%", stats.improvementPct),
                style = MaterialTheme.typography.labelMedium,
                fontWeight = FontWeight.ExtraBold,
                color = badgeColor
              )
            }
          }
        }
      }

      // 2. Metrics Strip (Promedio, Máximo, Total sesiones)
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .clip(RoundedCornerShape(16.dp))
          .background(MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.45f))
          .padding(vertical = 10.dp, horizontal = 14.dp),
        horizontalArrangement = Arrangement.SpaceAround,
        verticalAlignment = Alignment.CenterVertically
      ) {
        MetricItem(label = "Promedio", value = "${stats.avgScore} pts", color = activeColor)
        Box(modifier = Modifier.width(1.dp).height(24.dp).background(MaterialTheme.colorScheme.surfaceVariant))
        MetricItem(label = "Mejor Puntaje", value = "${stats.maxScore} pts", color = Color(0xFFD97706))
        Box(modifier = Modifier.width(1.dp).height(24.dp).background(MaterialTheme.colorScheme.surfaceVariant))
        MetricItem(label = "Sesiones", value = "${filteredData.size}", color = MaterialTheme.colorScheme.onSurface)
      }

      // 3. Domain Filters (Horizontal Scroll)
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .horizontalScroll(rememberScrollState()),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
      ) {
        FilterChip(
          selected = selectedDomain == null,
          onClick = { selectedDomain = null },
          label = { Text("General", style = MaterialTheme.typography.labelMedium) },
          colors = FilterChipDefaults.filterChipColors(
            selectedContainerColor = TealPrimary,
            selectedLabelColor = Color.White
          ),
          border = null
        )

        DomainType.values().forEach { domain ->
          FilterChip(
            selected = selectedDomain == domain,
            onClick = { selectedDomain = if (selectedDomain == domain) null else domain },
            label = { Text(domain.displayName, style = MaterialTheme.typography.labelMedium) },
            leadingIcon = {
              Box(
                modifier = Modifier
                  .size(8.dp)
                  .clip(CircleShape)
                  .background(if (selectedDomain == domain) Color.White else domain.color)
              )
            },
            colors = FilterChipDefaults.filterChipColors(
              selectedContainerColor = domain.color,
              selectedLabelColor = Color.White
            ),
            border = null
          )
        }
      }

      // 4. Time Range Filter (Pills)
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.End
      ) {
        Row(
          modifier = Modifier
            .clip(RoundedCornerShape(10.dp))
            .background(MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f))
            .padding(2.dp),
          horizontalArrangement = Arrangement.spacedBy(2.dp)
        ) {
          ChartTimeRange.values().forEach { range ->
            val isSelected = selectedRange == range
            Box(
              modifier = Modifier
                .clip(RoundedCornerShape(8.dp))
                .background(if (isSelected) MaterialTheme.colorScheme.surface else Color.Transparent)
                .clickable { selectedRange = range }
                .padding(horizontal = 8.dp, vertical = 4.dp),
              contentAlignment = Alignment.Center
            ) {
              Text(
                text = range.label,
                style = MaterialTheme.typography.labelSmall,
                fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal,
                color = if (isSelected) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.onSurfaceVariant
              )
            }
          }
        }
      }

      // 5. The Chart or Empty State
      if (filteredData.size < 2) {
        EmptyChartState(totalStored = history.size, selectedDomain = selectedDomain)
      } else {
        Column(
          modifier = Modifier.fillMaxWidth(),
          verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
          // The Interactive Canvas Chart
          InteractiveTrendCanvas(
            data = filteredData,
            accentColor = activeColor,
            selectedIndex = selectedIndex,
            onSelectIndex = { selectedIndex = it },
            modifier = Modifier
              .fillMaxWidth()
              .height(180.dp)
          )

          // X-Axis Date markers
          Row(
            modifier = Modifier
              .fillMaxWidth()
              .padding(start = 28.dp, end = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween
          ) {
            val firstDate = dateFormatter.format(Date(filteredData.first().timestamp))
            val midDate = dateFormatter.format(Date(filteredData[filteredData.size / 2].timestamp))
            val lastDate = dateFormatter.format(Date(filteredData.last().timestamp))

            val slate400 = Color(0xFF94A3B8)
            Text(text = firstDate, style = MaterialTheme.typography.labelSmall, color = slate400)
            if (filteredData.size > 2) {
              Text(text = midDate, style = MaterialTheme.typography.labelSmall, color = slate400)
            }
            Text(text = lastDate, style = MaterialTheme.typography.labelSmall, color = slate400)
          }

          // 6. Selected Point Detail Card / Tooltip
          val currentItem = selectedIndex?.let { idx -> filteredData.getOrNull(idx) }
          if (currentItem != null) {
            val game = GameRegistry.getById(currentItem.gameId)
            val domain = game?.domain ?: DomainType.MEMORIA
            Card(
              modifier = Modifier
                .fillMaxWidth()
                .padding(top = 4.dp),
              shape = RoundedCornerShape(16.dp),
              colors = CardDefaults.cardColors(
                containerColor = domain.color.copy(alpha = 0.08f)
              )
            ) {
              Row(
                modifier = Modifier
                  .fillMaxWidth()
                  .padding(horizontal = 14.dp, vertical = 10.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
              ) {
                Row(
                  verticalAlignment = Alignment.CenterVertically,
                  modifier = Modifier.weight(1f)
                ) {
                  Text(text = game?.iconEmoji ?: "🧠", fontSize = 22.sp)
                  Spacer(modifier = Modifier.width(10.dp))
                  Column {
                    Text(
                      text = game?.title ?: "Juego",
                      style = MaterialTheme.typography.labelLarge,
                      fontWeight = FontWeight.Bold,
                      maxLines = 1,
                      overflow = TextOverflow.Ellipsis
                    )
                    Text(
                      text = "${fullDateFormatter.format(Date(currentItem.timestamp))} • Nivel ${currentItem.level}",
                      style = MaterialTheme.typography.bodySmall,
                      color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                  }
                }

                Column(horizontalAlignment = Alignment.End) {
                  Surface(
                    shape = RoundedCornerShape(8.dp),
                    color = domain.color
                  ) {
                    Text(
                      text = "${currentItem.score} pts",
                      style = MaterialTheme.typography.labelMedium,
                      fontWeight = FontWeight.ExtraBold,
                      color = Color.White,
                      modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp)
                    )
                  }
                  Text(
                    text = "${currentItem.correctAnswers}/${currentItem.totalTrials} aciertos",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                  )
                }
              }
            }
          } else {
            val slate400 = Color(0xFF94A3B8)
            Row(
              modifier = Modifier
                .fillMaxWidth()
                .padding(top = 4.dp),
              horizontalArrangement = Arrangement.Center,
              verticalAlignment = Alignment.CenterVertically
            ) {
              Icon(
                imageVector = Icons.Default.TouchApp,
                contentDescription = null,
                modifier = Modifier.size(14.dp),
                tint = slate400
              )
              Spacer(modifier = Modifier.width(4.dp))
              Text(
                text = "Toca o desliza sobre el gráfico para examinar cada sesión",
                style = MaterialTheme.typography.labelSmall,
                color = slate400
              )
            }
          }
        }
      }
    }
  }
}

@Composable
private fun InteractiveTrendCanvas(
  data: List<GamePlayResult>,
  accentColor: Color,
  selectedIndex: Int?,
  onSelectIndex: (Int) -> Unit,
  modifier: Modifier = Modifier
) {
  val n = data.size
  val scores = remember(data) { data.map { it.score.toFloat() } }

  Canvas(
    modifier = modifier
      .pointerInput(data) {
        detectTapGestures { offset ->
          val stepX = (size.width - 70f) / (n - 1).coerceAtLeast(1)
          val relativeX = (offset.x - 50f).coerceIn(0f, size.width - 70f)
          val closestIndex = (relativeX / stepX).roundToInt().coerceIn(0, n - 1)
          onSelectIndex(closestIndex)
        }
      }
      .pointerInput(data) {
        detectDragGestures { change, _ ->
          val stepX = (size.width - 70f) / (n - 1).coerceAtLeast(1)
          val relativeX = (change.position.x - 50f).coerceIn(0f, size.width - 70f)
          val closestIndex = (relativeX / stepX).roundToInt().coerceIn(0, n - 1)
          onSelectIndex(closestIndex)
        }
      }
  ) {
    val paddingLeft = 45f
    val paddingRight = 20f
    val paddingTop = 15f
    val paddingBottom = 25f

    val chartWidth = size.width - paddingLeft - paddingRight
    val chartHeight = size.height - paddingTop - paddingBottom

    val minY = 0f
    val maxY = 100f

    // 1. Draw Grid Lines (at 100, 75, 50, 25, 0)
    val gridValues = listOf(100f, 75f, 50f, 25f)
    val dashEffect = PathEffect.dashPathEffect(floatArrayOf(10f, 10f), 0f)

    gridValues.forEach { gridVal ->
      val y = paddingTop + chartHeight * (1f - (gridVal - minY) / (maxY - minY))

      // Dotted line
      drawLine(
        color = Color.LightGray.copy(alpha = 0.45f),
        start = Offset(paddingLeft, y),
        end = Offset(size.width - paddingRight, y),
        strokeWidth = 1.dp.toPx(),
        pathEffect = dashEffect
      )
    }

    // Benchmark line at 80 pts (Mastery Zone)
    val benchmarkY = paddingTop + chartHeight * (1f - (80f - minY) / (maxY - minY))
    drawLine(
      color = EmeraldAccent.copy(alpha = 0.35f),
      start = Offset(paddingLeft, benchmarkY),
      end = Offset(size.width - paddingRight, benchmarkY),
      strokeWidth = 1.5.dp.toPx(),
      pathEffect = PathEffect.dashPathEffect(floatArrayOf(6f, 6f), 0f)
    )

    if (n < 2) return@Canvas

    val stepX = chartWidth / (n - 1)

    // Calculate coordinate points
    val points = scores.mapIndexed { idx, score ->
      val x = paddingLeft + idx * stepX
      val y = paddingTop + chartHeight * (1f - (score - minY) / (maxY - minY))
      Offset(x, y)
    }

    // 2. Smooth Cubic Bézier Curve Path
    val curvePath = Path().apply {
      moveTo(points.first().x, points.first().y)
      for (i in 0 until points.size - 1) {
        val p0 = points[i]
        val p1 = points[i + 1]
        val controlX1 = p0.x + (p1.x - p0.x) / 2f
        val controlY1 = p0.y
        val controlX2 = p0.x + (p1.x - p0.x) / 2f
        val controlY2 = p1.y
        cubicTo(controlX1, controlY1, controlX2, controlY2, p1.x, p1.y)
      }
    }

    // 3. Fill Area Under the Curve with Gradient
    val fillPath = Path().apply {
      addPath(curvePath)
      lineTo(points.last().x, paddingTop + chartHeight)
      lineTo(points.first().x, paddingTop + chartHeight)
      close()
    }

    drawPath(
      path = fillPath,
      brush = Brush.verticalGradient(
        colors = listOf(
          accentColor.copy(alpha = 0.30f),
          accentColor.copy(alpha = 0.04f),
          Color.Transparent
        ),
        startY = paddingTop,
        endY = paddingTop + chartHeight
      )
    )

    // 4. Linear Regression Trend Line (dashed, subtle)
    val xMean = (n - 1) / 2f
    val yMean = scores.average().toFloat()
    var num = 0f
    var den = 0f
    for (i in 0 until n) {
      num += (i - xMean) * (scores[i] - yMean)
      den += (i - xMean) * (i - xMean)
    }
    if (den != 0f) {
      val slope = num / den
      val intercept = yMean - slope * xMean
      val trendY0 = (intercept).coerceIn(0f, 100f)
      val trendY1 = (slope * (n - 1) + intercept).coerceIn(0f, 100f)

      val startTrendOffset = Offset(
        paddingLeft,
        paddingTop + chartHeight * (1f - (trendY0 - minY) / (maxY - minY))
      )
      val endTrendOffset = Offset(
        size.width - paddingRight,
        paddingTop + chartHeight * (1f - (trendY1 - minY) / (maxY - minY))
      )

      drawLine(
        color = if (slope >= 0f) EmeraldAccent.copy(alpha = 0.6f) else Color(0xFFEF4444).copy(alpha = 0.6f),
        start = startTrendOffset,
        end = endTrendOffset,
        strokeWidth = 2.dp.toPx(),
        pathEffect = PathEffect.dashPathEffect(floatArrayOf(12f, 8f), 0f)
      )
    }

    // 5. Draw Main Curve Stroke
    drawPath(
      path = curvePath,
      color = accentColor,
      style = Stroke(
        width = 3.dp.toPx(),
        cap = StrokeCap.Round,
        join = StrokeJoin.Round
      )
    )

    // 6. Draw Selected Point Guideline and Indicator
    if (selectedIndex != null && selectedIndex in points.indices) {
      val selectedPoint = points[selectedIndex]

      // Vertical guideline
      drawLine(
        color = accentColor.copy(alpha = 0.5f),
        start = Offset(selectedPoint.x, paddingTop),
        end = Offset(selectedPoint.x, paddingTop + chartHeight),
        strokeWidth = 1.5.dp.toPx(),
        pathEffect = PathEffect.dashPathEffect(floatArrayOf(6f, 6f), 0f)
      )

      // Outer glow
      drawCircle(
        color = accentColor.copy(alpha = 0.25f),
        radius = 12.dp.toPx(),
        center = selectedPoint
      )

      // Outer circle
      drawCircle(
        color = Color.White,
        radius = 7.dp.toPx(),
        center = selectedPoint
      )

      // Center filled dot
      drawCircle(
        color = accentColor,
        radius = 4.5.dp.toPx(),
        center = selectedPoint
      )
    }

    // 7. Regular Data Point Markers
    points.forEachIndexed { index, pt ->
      if (index != selectedIndex) {
        drawCircle(
          color = Color.White,
          radius = 4.dp.toPx(),
          center = pt
        )
        drawCircle(
          color = accentColor,
          radius = 2.5.dp.toPx(),
          center = pt
        )
      }
    }
  }
}

@Composable
private fun MetricItem(
  label: String,
  value: String,
  color: Color
) {
  Column(horizontalAlignment = Alignment.CenterHorizontally) {
    Text(
      text = label,
      style = MaterialTheme.typography.labelSmall,
      color = MaterialTheme.colorScheme.onSurfaceVariant
    )
    Text(
      text = value,
      style = MaterialTheme.typography.titleMedium,
      fontWeight = FontWeight.ExtraBold,
      color = color
    )
  }
}

@Composable
private fun EmptyChartState(
  totalStored: Int,
  selectedDomain: DomainType?
) {
  Surface(
    modifier = Modifier
      .fillMaxWidth()
      .padding(vertical = 12.dp),
    shape = RoundedCornerShape(16.dp),
    color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.35f)
  ) {
    Column(
      modifier = Modifier.padding(20.dp),
      horizontalAlignment = Alignment.CenterHorizontally,
      verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
      Text(text = "📈", fontSize = 32.sp)
      Text(
        text = if (selectedDomain != null) {
          "Pocas sesiones en ${selectedDomain.displayName}"
        } else {
          "Se requieren al menos 2 sesiones"
        },
        style = MaterialTheme.typography.titleSmall,
        fontWeight = FontWeight.Bold,
        textAlign = TextAlign.Center
      )
      Text(
        text = if (selectedDomain != null) {
          "Juega más partidas de ${selectedDomain.displayName} para trazar su curva de aprendizaje individual."
        } else {
          "Completa tu entrenamiento de hoy para ver reflejada tu evolución en tiempo real sobre la base de datos Room."
        },
        style = MaterialTheme.typography.bodySmall,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
        textAlign = TextAlign.Center
      )
    }
  }
}

private data class ChartStats(
  val avgScore: Int,
  val maxScore: Int,
  val improvementPct: Float,
  val trendSlope: Float
)
