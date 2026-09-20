package com.example.games

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainVelocidad
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random

data class ComparisonSide(
  val displayValue: String,
  val numericValue: Int,
  val dotCount: Int? = null // if dots mode
)

data class ComparisonTrial(
  val left: ComparisonSide,
  val right: ComparisonSide
)

@Composable
fun ComparacionGame(
  level: Int,
  timed: Boolean,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 12
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }
  var scorePoints by remember { mutableStateOf(0) }

  var currentTrial by remember { mutableStateOf(generateTrial(level)) }
  var selectedSide by remember { mutableStateOf<String?>(null) }
  var trialStartTime by remember { mutableStateOf(System.currentTimeMillis()) }

  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  LaunchedEffect(currentRound) {
    trialStartTime = System.currentTimeMillis()
  }

  fun handlePick(side: String) {
    if (selectedSide != null) return
    selectedSide = side
    val reactionMs = System.currentTimeMillis() - trialStartTime
    val isLeftGreater = currentTrial.left.numericValue >= currentTrial.right.numericValue
    val isCorrect = (side == "LEFT" && isLeftGreater) || (side == "RIGHT" && !isLeftGreater)

    if (isCorrect) {
      correctCount++
      val speedBonus = if (reactionMs < 900) 5 else 0
      scorePoints += (10 + speedBonus)
      flashSuccess = true
    } else {
      flashSuccess = false
    }
    showFlash = true
  }

  LaunchedEffect(selectedSide) {
    if (selectedSide != null) {
      delay(450)
      showFlash = false
      if (currentRound >= totalTrials) {
        val finalScore = (correctCount * 100 / totalTrials).coerceIn(0, 100)
        onFinish(finalScore, correctCount, totalTrials)
      } else {
        currentRound++
        currentTrial = generateTrial(level)
        selectedSide = null
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
        title = "Comparación Instantánea",
        domain = DomainType.VELOCIDAD,
        currentRound = currentRound,
        totalRounds = totalTrials,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(16.dp))

      Text(
        text = "¿Cuál lado es MAYOR?",
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.Bold,
        color = DomainVelocidad
      )

      Spacer(modifier = Modifier.weight(1f))

      // Left vs Right panels
      Row(
        modifier = Modifier
          .fillMaxWidth(0.92f)
          .height(240.dp),
        horizontalArrangement = Arrangement.spacedBy(16.dp)
      ) {
        // Left Side
        Card(
          modifier = Modifier
            .weight(1f)
            .fillMaxHeight()
            .clip(RoundedCornerShape(24.dp))
            .clickable(enabled = selectedSide == null) { handlePick("LEFT") }
            .testTag("btn_compare_left"),
          shape = RoundedCornerShape(24.dp),
          colors = CardDefaults.cardColors(
            containerColor = if (selectedSide == "LEFT") {
              if (flashSuccess) EmeraldAccent.copy(alpha = 0.2f) else MaterialTheme.colorScheme.error.copy(alpha = 0.2f)
            } else {
              MaterialTheme.colorScheme.surface
            }
          ),
          elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
        ) {
          Box(
            modifier = Modifier.fillMaxSize().padding(16.dp),
            contentAlignment = Alignment.Center
          ) {
            if (currentTrial.left.dotCount != null) {
              DotsGrid(count = currentTrial.left.dotCount!!)
            } else {
              Text(
                text = currentTrial.left.displayValue,
                style = MaterialTheme.typography.displayMedium,
                fontWeight = FontWeight.Black,
                color = MaterialTheme.colorScheme.onSurface
              )
            }
          }
        }

        // Right Side
        Card(
          modifier = Modifier
            .weight(1f)
            .fillMaxHeight()
            .clip(RoundedCornerShape(24.dp))
            .clickable(enabled = selectedSide == null) { handlePick("RIGHT") }
            .testTag("btn_compare_right"),
          shape = RoundedCornerShape(24.dp),
          colors = CardDefaults.cardColors(
            containerColor = if (selectedSide == "RIGHT") {
              if (flashSuccess) EmeraldAccent.copy(alpha = 0.2f) else MaterialTheme.colorScheme.error.copy(alpha = 0.2f)
            } else {
              MaterialTheme.colorScheme.surface
            }
          ),
          elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
        ) {
          Box(
            modifier = Modifier.fillMaxSize().padding(16.dp),
            contentAlignment = Alignment.Center
          ) {
            if (currentTrial.right.dotCount != null) {
              DotsGrid(count = currentTrial.right.dotCount!!)
            } else {
              Text(
                text = currentTrial.right.displayValue,
                style = MaterialTheme.typography.displayMedium,
                fontWeight = FontWeight.Black,
                color = MaterialTheme.colorScheme.onSurface
              )
            }
          }
        }
      }

      Spacer(modifier = Modifier.weight(1f))
    }

    ScreenFlashOverlay(visible = showFlash, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
  }
}

@Composable
private fun DotsGrid(count: Int) {
  Column(
    verticalArrangement = Arrangement.spacedBy(6.dp),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    val rows = (1..count).chunked(4)
    rows.forEach { rowDots ->
      Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
        rowDots.forEach {
          Box(
            modifier = Modifier
              .size(14.dp)
              .clip(CircleShape)
              .background(DomainVelocidad)
          )
        }
      }
    }
  }
}

private fun generateTrial(level: Int): ComparisonTrial {
  if (level == 1) {
    // Dot patterns
    val v1 = Random.nextInt(4, 13)
    var v2 = Random.nextInt(4, 13)
    while (v2 == v1) {
      v2 = Random.nextInt(4, 13)
    }
    return ComparisonTrial(
      left = ComparisonSide(v1.toString(), v1, dotCount = v1),
      right = ComparisonSide(v2.toString(), v2, dotCount = v2)
    )
  } else if (level == 2) {
    // Numeric values
    val v1 = Random.nextInt(15, 99)
    var v2 = v1 + Random.nextInt(-9, 10)
    if (v2 == v1) v2 = v1 + 3
    return ComparisonTrial(
      left = ComparisonSide(v1.toString(), v1),
      right = ComparisonSide(v2.toString(), v2)
    )
  } else {
    // Simple expression vs number
    val a = Random.nextInt(4, 9)
    val b = Random.nextInt(4, 9)
    val prod = a * b
    val compareVal = prod + Random.nextInt(-6, 7)
    val leftIsExpr = Random.nextBoolean()

    return if (leftIsExpr) {
      ComparisonTrial(
        left = ComparisonSide("$a × $b", prod),
        right = ComparisonSide(compareVal.toString(), compareVal)
      )
    } else {
      ComparisonTrial(
        left = ComparisonSide(compareVal.toString(), compareVal),
        right = ComparisonSide("$a × $b", prod)
      )
    }
  }
}
