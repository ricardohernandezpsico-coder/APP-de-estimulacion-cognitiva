package com.example.games

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainRazonamiento
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random

data class SeriesItem(
  val sequenceText: String,
  val answer: String,
  val options: List<String>,
  val ruleExplanation: String
)

@Composable
fun SeriesGame(
  level: Int,
  timed: Boolean,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 8
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }

  var currentItem by remember { mutableStateOf(generateSeries(level)) }
  var selectedChoice by remember { mutableStateOf<String?>(null) }
  var showExplanation by remember { mutableStateOf(false) }

  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  fun handleChoice(choice: String) {
    if (selectedChoice != null) return
    selectedChoice = choice
    val isCorrect = choice == currentItem.answer

    if (isCorrect) {
      correctCount++
      flashSuccess = true
    } else {
      flashSuccess = false
    }
    showExplanation = true
    showFlash = true
  }

  LaunchedEffect(selectedChoice) {
    if (selectedChoice != null) {
      delay(1600) // Give user time to read the rule explanation
      showFlash = false
      showExplanation = false
      if (currentRound >= totalTrials) {
        val finalScore = (correctCount * 100 / totalTrials).coerceIn(0, 100)
        onFinish(finalScore, correctCount, totalTrials)
      } else {
        currentRound++
        currentItem = generateSeries(level)
        selectedChoice = null
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
        title = "Detective de Series",
        domain = DomainType.RAZONAMIENTO,
        currentRound = currentRound,
        totalRounds = totalTrials,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(20.dp))

      Text(
        text = "¿Qué elemento continúa la serie lógica?",
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant
      )

      Spacer(modifier = Modifier.weight(0.4f))

      // Sequence Card
      Card(
        modifier = Modifier
          .fillMaxWidth(0.9f)
          .heightIn(min = 150.dp),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
      ) {
        Column(
          modifier = Modifier
            .fillMaxWidth()
            .padding(24.dp),
          horizontalAlignment = Alignment.CenterHorizontally
        ) {
          Text(
            text = currentItem.sequenceText,
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Black,
            color = DomainRazonamiento,
            textAlign = TextAlign.Center
          )

          AnimatedVisibility(visible = showExplanation) {
            Surface(
              shape = RoundedCornerShape(12.dp),
              color = DomainRazonamiento.copy(alpha = 0.12f),
              modifier = Modifier.padding(top = 12.dp)
            ) {
              Text(
                text = "💡 ${currentItem.ruleExplanation}",
                style = MaterialTheme.typography.bodySmall,
                fontWeight = FontWeight.SemiBold,
                color = DomainRazonamiento,
                modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
                textAlign = TextAlign.Center
              )
            }
          }
        }
      }

      Spacer(modifier = Modifier.weight(0.6f))

      // 4 Multiple choice options
      Column(
        modifier = Modifier
          .fillMaxWidth(0.9f)
          .padding(bottom = 32.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
      ) {
        val options = currentItem.options
        for (row in 0..1) {
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
          ) {
            for (col in 0..1) {
              val idx = row * 2 + col
              if (idx < options.size) {
                val opt = options[idx]
                val isSelected = selectedChoice == opt
                val isCorrect = opt == currentItem.answer

                val btnBg = when {
                  selectedChoice == null -> MaterialTheme.colorScheme.surface
                  isSelected && isCorrect -> EmeraldAccent
                  isSelected && !isCorrect -> MaterialTheme.colorScheme.error
                  isCorrect -> EmeraldAccent.copy(alpha = 0.8f)
                  else -> MaterialTheme.colorScheme.surface.copy(alpha = 0.5f)
                }

                Button(
                  onClick = { handleChoice(opt) },
                  enabled = selectedChoice == null,
                  modifier = Modifier
                    .weight(1f)
                    .height(64.dp)
                    .testTag("btn_series_opt_$idx"),
                  shape = RoundedCornerShape(18.dp),
                  colors = ButtonDefaults.buttonColors(
                    containerColor = btnBg,
                    disabledContainerColor = btnBg
                  ),
                  elevation = ButtonDefaults.buttonElevation(defaultElevation = 2.dp)
                ) {
                  Text(
                    text = opt,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    color = if (selectedChoice == null) MaterialTheme.colorScheme.onSurface else Color.White
                  )
                }
              }
            }
          }
        }
      }
    }

    ScreenFlashOverlay(visible = showFlash, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
  }
}

private fun generateSeries(level: Int): SeriesItem {
  val type = Random.nextInt(5)
  return when (type) {
    0 -> {
      // Linear step (+k)
      val start = Random.nextInt(2, 20)
      val step = Random.nextInt(2, 6)
      val s1 = start
      val s2 = s1 + step
      val s3 = s2 + step
      val s4 = s3 + step
      val ans = s4 + step
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Suma fija de +$step en cada término")
    }
    1 -> {
      // Multiplication (*2 or *3)
      val mult = if (level <= 2) 2 else 3
      val start = Random.nextInt(2, 5)
      val s1 = start
      val s2 = s1 * mult
      val s3 = s2 * mult
      val s4 = s3 * mult
      val ans = s4 * mult
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Multiplicación por $mult en cada paso")
    }
    2 -> {
      // Decreasing (-k)
      val step = Random.nextInt(3, 8)
      val start = 50 + step * 4
      val s1 = start
      val s2 = s1 - step
      val s3 = s2 - step
      val s4 = s3 - step
      val ans = s4 - step
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Resta constante de -$step en cada paso")
    }
    3 -> {
      // Increasing difference (+1, +2, +3, +4...)
      val start = Random.nextInt(1, 10)
      val s1 = start
      val s2 = s1 + 2
      val s3 = s2 + 4
      val s4 = s3 + 6
      val ans = s4 + 8
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Diferencia creciente: +2, +4, +6, +8...")
    }
    else -> {
      // Squares
      val offset = Random.nextInt(1, 4)
      val s1 = (offset) * (offset)
      val s2 = (offset + 1) * (offset + 1)
      val s3 = (offset + 2) * (offset + 2)
      val s4 = (offset + 3) * (offset + 3)
      val ans = (offset + 4) * (offset + 4)
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Cuadrados perfectos consecutivos")
    }
  }
}

private fun createSeriesItem(sequenceText: String, answer: String, explanation: String): SeriesItem {
  val intAns = answer.toIntOrNull() ?: 20
  val opts = mutableSetOf(answer)
  while (opts.size < 4) {
    val delta = Random.nextInt(-8, 9)
    if (delta != 0) {
      opts.add((intAns + delta).toString())
    }
  }
  return SeriesItem(sequenceText, answer, opts.shuffled(), explanation)
}
