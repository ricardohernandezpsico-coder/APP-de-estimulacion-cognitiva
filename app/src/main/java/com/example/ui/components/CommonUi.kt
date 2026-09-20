package com.example.ui.components

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material.icons.filled.TrendingUp
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.theme.*

@Composable
fun Sparkline(
  data: List<Float>,
  lineColor: Color = TealAccent,
  modifier: Modifier = Modifier.height(28.dp).fillMaxWidth()
) {
  if (data.size < 2) return

  Canvas(modifier = modifier) {
    val minVal = data.minOrNull() ?: 0f
    val maxVal = (data.maxOrNull() ?: 1f).coerceAtLeast(minVal + 0.1f)
    val range = maxVal - minVal
    val stepX = size.width / (data.size - 1)

    val path = Path()
    data.forEachIndexed { index, value ->
      val x = index * stepX
      val y = size.height - ((value - minVal) / range) * (size.height * 0.8f) - (size.height * 0.1f)
      if (index == 0) path.moveTo(x, y) else path.lineTo(x, y)
    }

    drawPath(
      path = path,
      color = lineColor,
      style = Stroke(width = 2.5.dp.toPx(), cap = StrokeCap.Round)
    )

    // Draw point at last entry
    val lastX = (data.size - 1) * stepX
    val lastY = size.height - ((data.last() - minVal) / range) * (size.height * 0.8f) - (size.height * 0.1f)
    drawCircle(
      color = lineColor,
      radius = 3.5.dp.toPx(),
      center = androidx.compose.ui.geometry.Offset(lastX, lastY)
    )
  }
}

@Composable
fun CircularProgressRing(
  progress: Float, // 0.0 to 1.0
  text: String,
  color: Color = TealPrimary,
  trackColor: Color = Slate200,
  strokeWidth: Dp = 6.dp,
  size: Dp = 60.dp
) {
  Box(
    modifier = Modifier.size(size),
    contentAlignment = Alignment.Center
  ) {
    Canvas(modifier = Modifier.fillMaxSize()) {
      // Track
      drawArc(
        color = trackColor,
        startAngle = 0f,
        sweepAngle = 360f,
        useCenter = false,
        style = Stroke(width = strokeWidth.toPx(), cap = StrokeCap.Round)
      )
      // Progress
      drawArc(
        color = color,
        startAngle = -90f,
        sweepAngle = (progress * 360f).coerceIn(0f, 360f),
        useCenter = false,
        style = Stroke(width = strokeWidth.toPx(), cap = StrokeCap.Round)
      )
    }
    Text(
      text = text,
      style = MaterialTheme.typography.labelLarge,
      fontWeight = FontWeight.Bold,
      color = MaterialTheme.colorScheme.onSurface
    )
  }
}

@Composable
fun DomainChip(
  domain: DomainType,
  modifier: Modifier = Modifier
) {
  Surface(
    modifier = modifier,
    shape = RoundedCornerShape(12.dp),
    color = domain.color.copy(alpha = 0.14f)
  ) {
    Row(
      modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
      verticalAlignment = Alignment.CenterVertically
    ) {
      Box(
        modifier = Modifier
          .size(8.dp)
          .clip(CircleShape)
          .background(domain.color)
      )
      Spacer(modifier = Modifier.width(6.dp))
      Text(
        text = domain.displayName,
        style = MaterialTheme.typography.labelSmall,
        fontWeight = FontWeight.SemiBold,
        color = domain.color
      )
    }
  }
}

@Composable
fun GameHeader(
  title: String,
  domain: DomainType,
  currentRound: Int,
  totalRounds: Int,
  isTimed: Boolean,
  timeLeftSeconds: Int? = null,
  onQuit: () -> Unit,
  modifier: Modifier = Modifier
) {
  Surface(
    modifier = modifier.fillMaxWidth(),
    color = MaterialTheme.colorScheme.surface,
    tonalElevation = 2.dp
  ) {
    Column(modifier = Modifier.fillMaxWidth()) {
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .padding(horizontal = 16.dp, vertical = 12.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        IconButton(
          onClick = onQuit,
          modifier = Modifier.testTag("btn_quit_game")
        ) {
          Icon(
            imageVector = Icons.Default.Close,
            contentDescription = "Salir del juego",
            tint = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }

        Column(horizontalAlignment = Alignment.CenterHorizontally) {
          Text(
            text = title,
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis
          )
          DomainChip(domain = domain)
        }

        Row(verticalAlignment = Alignment.CenterVertically) {
          if (isTimed && timeLeftSeconds != null) {
            Surface(
              shape = RoundedCornerShape(8.dp),
              color = if (timeLeftSeconds <= 5) DomainVelocidad.copy(alpha = 0.15f) else MaterialTheme.colorScheme.surfaceVariant
            ) {
              Row(
                modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically
              ) {
                Icon(
                  imageVector = Icons.Default.Timer,
                  contentDescription = null,
                  modifier = Modifier.size(16.dp),
                  tint = if (timeLeftSeconds <= 5) DomainVelocidad else MaterialTheme.colorScheme.onSurface
                )
                Spacer(modifier = Modifier.width(4.dp))
                Text(
                  text = "${timeLeftSeconds}s",
                  style = MaterialTheme.typography.labelMedium,
                  fontWeight = FontWeight.Bold,
                  color = if (timeLeftSeconds <= 5) DomainVelocidad else MaterialTheme.colorScheme.onSurface
                )
              }
            }
          } else {
            Surface(
              shape = RoundedCornerShape(8.dp),
              color = domain.color.copy(alpha = 0.12f)
            ) {
              Text(
                text = "$currentRound/$totalRounds",
                modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                style = MaterialTheme.typography.labelMedium,
                fontWeight = FontWeight.Bold,
                color = domain.color
              )
            }
          }
        }
      }

      // Linear round progress bar
      val progress = (currentRound.toFloat() / totalRounds.coerceAtLeast(1)).coerceIn(0f, 1f)
      LinearProgressIndicator(
        progress = { progress },
        modifier = Modifier.fillMaxWidth().height(3.dp),
        color = domain.color,
        trackColor = MaterialTheme.colorScheme.surfaceVariant
      )
    }
  }
}

@Composable
fun ScreenFlashOverlay(
  visible: Boolean,
  flashColor: Color = EmeraldAccent
) {
  AnimatedVisibility(
    visible = visible,
    enter = fadeIn(),
    exit = fadeOut()
  ) {
    Box(
      modifier = Modifier
        .fillMaxSize()
        .background(flashColor.copy(alpha = 0.15f))
    )
  }
}
