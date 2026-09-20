package com.example.games

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.ArrowDownward
import androidx.compose.material.icons.filled.ArrowUpward
import androidx.compose.material.icons.filled.SwapHoriz
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainAtencion
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random

enum class Direction(val label: String, val icon: ImageVector) {
  ARRIBA("arriba", Icons.Default.ArrowUpward),
  ABAJO("abajo", Icons.Default.ArrowDownward),
  IZQUIERDA("izquierda", Icons.AutoMirrored.Filled.ArrowBack),
  DERECHA("derecha", Icons.AutoMirrored.Filled.ArrowForward)
}

enum class ChipRule(val display: String) {
  DIRECCION("DÓNDE APUNTA"),
  POSICION("DÓNDE ESTÁ")
}

data class ChipTrial(
  val pointing: Direction,
  val position: Direction,
  val rule: ChipRule
)

@Composable
fun CambioChipGame(
  level: Int,
  timed: Boolean,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 12
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }
  var scorePoints by remember { mutableStateOf(0) }

  var activeRule by remember { mutableStateOf(ChipRule.DIRECCION) }
  var currentTrial by remember { mutableStateOf(generateTrial(activeRule)) }
  var selectedDirection by remember { mutableStateOf<Direction?>(null) }
  var showRuleChangeBanner by remember { mutableStateOf(false) }

  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  fun handleAnswer(chosen: Direction) {
    if (selectedDirection != null) return
    selectedDirection = chosen
    val expected = if (currentTrial.rule == ChipRule.DIRECCION) currentTrial.pointing else currentTrial.position
    val isCorrect = chosen == expected

    if (isCorrect) {
      correctCount++
      scorePoints += 10
      flashSuccess = true
    } else {
      flashSuccess = false
    }
    showFlash = true
  }

  LaunchedEffect(selectedDirection) {
    if (selectedDirection != null) {
      delay(550)
      showFlash = false
      if (currentRound >= totalTrials) {
        val finalScore = (correctCount * 100 / totalTrials).coerceIn(0, 100)
        onFinish(finalScore, correctCount, totalTrials)
      } else {
        currentRound++
        // Switch rule every 3-4 trials
        if (currentRound % 3 == 0) {
          activeRule = if (activeRule == ChipRule.DIRECCION) ChipRule.POSICION else ChipRule.DIRECCION
          showRuleChangeBanner = true
          delay(800)
          showRuleChangeBanner = false
        }
        currentTrial = generateTrial(activeRule)
        selectedDirection = null
      }
    }
  }

  Box(
    modifier = Modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
  ) {
    Column(
      modifier = Modifier.fillMaxSize(),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      GameHeader(
        title = "Cambio de Chip",
        domain = DomainType.ATENCION,
        currentRound = currentRound,
        totalRounds = totalTrials,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(16.dp))

      // Rule Chip
      Surface(
        shape = RoundedCornerShape(16.dp),
        color = DomainAtencion.copy(alpha = 0.15f),
        border = androidx.compose.foundation.BorderStroke(1.dp, DomainAtencion.copy(alpha = 0.4f))
      ) {
        Row(
          modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
          verticalAlignment = Alignment.CenterVertically
        ) {
          Icon(
            imageVector = Icons.Default.SwapHoriz,
            contentDescription = null,
            tint = DomainAtencion,
            modifier = Modifier.size(18.dp)
          )
          Spacer(modifier = Modifier.width(6.dp))
          Text(
            text = "REGLA: ${currentTrial.rule.display}",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Black,
            color = DomainAtencion
          )
        }
      }

      // Rule changed alert banner
      AnimatedVisibility(visible = showRuleChangeBanner) {
        Surface(
          shape = RoundedCornerShape(12.dp),
          color = Color(0xFFEF4444).copy(alpha = 0.15f),
          modifier = Modifier.padding(top = 8.dp)
        ) {
          Text(
            text = "⚠️ ¡Cambio de regla!",
            style = MaterialTheme.typography.labelLarge,
            fontWeight = FontWeight.Bold,
            color = Color(0xFFDC2626),
            modifier = Modifier.padding(horizontal = 14.dp, vertical = 4.dp)
          )
        }
      }

      Spacer(modifier = Modifier.weight(0.5f))

      // Arrow Arena box
      Box(
        modifier = Modifier
          .size(240.dp)
          .clip(RoundedCornerShape(28.dp))
          .background(MaterialTheme.colorScheme.surface)
          .border(2.dp, MaterialTheme.colorScheme.surfaceVariant, RoundedCornerShape(28.dp))
          .padding(24.dp)
      ) {
        // Place arrow according to position
        val alignment = when (currentTrial.position) {
          Direction.ARRIBA -> Alignment.TopCenter
          Direction.ABAJO -> Alignment.BottomCenter
          Direction.IZQUIERDA -> Alignment.CenterStart
          Direction.DERECHA -> Alignment.CenterEnd
        }

        Box(
          modifier = Modifier
            .fillMaxSize()
            .align(Alignment.Center)
        ) {
          Surface(
            modifier = Modifier
              .size(68.dp)
              .align(alignment),
            shape = CircleShape,
            color = DomainAtencion.copy(alpha = 0.15f)
          ) {
            Box(contentAlignment = Alignment.Center) {
              Icon(
                imageVector = currentTrial.pointing.icon,
                contentDescription = currentTrial.pointing.label,
                tint = DomainAtencion,
                modifier = Modifier.size(40.dp)
              )
            }
          }
        }
      }

      Spacer(modifier = Modifier.weight(0.6f))

      // 4 Direction Buttons in D-Pad layout
      Column(
        modifier = Modifier
          .padding(bottom = 32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(8.dp)
      ) {
        DirectionButton(Direction.ARRIBA, selectedDirection == Direction.ARRIBA) { handleAnswer(Direction.ARRIBA) }
        Row(horizontalArrangement = Arrangement.spacedBy(36.dp)) {
          DirectionButton(Direction.IZQUIERDA, selectedDirection == Direction.IZQUIERDA) { handleAnswer(Direction.IZQUIERDA) }
          DirectionButton(Direction.DERECHA, selectedDirection == Direction.DERECHA) { handleAnswer(Direction.DERECHA) }
        }
        DirectionButton(Direction.ABAJO, selectedDirection == Direction.ABAJO) { handleAnswer(Direction.ABAJO) }
      }
    }

    ScreenFlashOverlay(visible = showFlash, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
  }
}

@Composable
private fun DirectionButton(
  direction: Direction,
  isSelected: Boolean,
  onClick: () -> Unit
) {
  IconButton(
    onClick = onClick,
    modifier = Modifier
      .size(64.dp)
      .clip(CircleShape)
      .background(MaterialTheme.colorScheme.surface)
      .border(1.dp, MaterialTheme.colorScheme.surfaceVariant, CircleShape)
      .testTag("btn_dir_${direction.label}")
  ) {
    Icon(
      imageVector = direction.icon,
      contentDescription = direction.label,
      tint = MaterialTheme.colorScheme.onSurface,
      modifier = Modifier.size(28.dp)
    )
  }
}

private fun generateTrial(rule: ChipRule): ChipTrial {
  val dirs = Direction.values()
  val pointing = dirs.random()
  val position = dirs.random()
  return ChipTrial(pointing, position, rule)
}
