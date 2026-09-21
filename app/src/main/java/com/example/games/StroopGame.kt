package com.example.games

import androidx.compose.foundation.background
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
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainAtencion
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random

enum class StroopRule { INK, WORD }

data class StroopItem(
  val textName: String,
  val inkName: String,
  val inkColor: Color,
  val rule: StroopRule
) {
  /** La respuesta correcta depende de la regla activa en este ensayo, no siempre la tinta. */
  val correctAnswer: String get() = if (rule == StroopRule.INK) inkName else textName
}

val StroopPalette = listOf(
  Pair("ROJO", Color(0xFFDC2626)),
  Pair("AZUL", Color(0xFF2563EB)),
  Pair("VERDE", Color(0xFF16A34A)),
  Pair("AMARILLO", Color(0xFFCA8A04)),
  Pair("MORADO", Color(0xFF9333EA))
)

/**
 * Tiempo base por nivel: baja de forma continua, no solo entre 5 escalones.
 * `intensity` seguirá recortándolo sin límite una vez alcanzado nivel 5 (Experto),
 * para que un usuario que domina el juego nunca deje de sentir presión de tiempo.
 */
private fun baseTimeForLevel(level: Int, intensity: Int): Int {
  val byLevel = when (level) { 1 -> 10; 2 -> 9; 3 -> 8; 4 -> 7; else -> 6 }
  val fromMastery = intensity / 3 // cada 3 rondas de maestría, 1s menos
  return (byLevel - fromMastery).coerceAtLeast(3)
}

/**
 * Probabilidad de que la regla se invierta en este ensayo (responder según la
 * PALABRA en vez de la tinta). En niveles 1-3 nunca cambia — ya es bastante
 * pedirle al cerebro que ignore la palabra. Desde nivel 4 empieza a variar sin
 * aviso (como Cambio de Chip), y la maestría sigue subiendo la probabilidad.
 */
private fun ruleFlipChance(level: Int, intensity: Int): Float = when {
  level < 4 -> 0f
  else -> (0.25f + intensity * 0.02f).coerceAtMost(0.5f)
}

@Composable
fun StroopGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 12
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }

  var currentTrial by remember { mutableStateOf(generateStroopTrial(level, intensity)) }
  var selectedChoice by remember { mutableStateOf<String?>(null) }
  var showFeedbackFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  // Fixed order of buttons so they are 100% stable during the game session
  val stableColors = remember { StroopPalette }

  val baseTime = remember(level, intensity) { baseTimeForLevel(level, intensity) }
  var timeLeft by remember { mutableStateOf(if (timed) baseTime else null) }

  fun handleChoice(choice: String) {
    if (selectedChoice != null) return
    selectedChoice = choice
    val isCorrect = choice == currentTrial.correctAnswer

    if (isCorrect) {
      correctCount++
      flashSuccess = true
    } else {
      flashSuccess = false
    }
    showFeedbackFlash = true
  }

  LaunchedEffect(currentRound, timed) {
    if (timed) {
      timeLeft = baseTime
      while (timeLeft != null && timeLeft!! > 0) {
        delay(1000)
        timeLeft = timeLeft!! - 1
      }
      if (selectedChoice == null) {
        handleChoice("TIMEOUT")
      }
    }
  }

  LaunchedEffect(selectedChoice) {
    if (selectedChoice != null) {
      delay(600)
      showFeedbackFlash = false
      if (currentRound >= totalTrials) {
        val finalScore = (correctCount * 100 / totalTrials).coerceIn(0, 100)
        onFinish(finalScore, correctCount, totalTrials)
      } else {
        currentRound++
        currentTrial = generateStroopTrial(level, intensity)
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
        title = "Color o Palabra",
        domain = DomainType.ATENCION,
        currentRound = currentRound,
        totalRounds = totalTrials,
        isTimed = timed,
        timeLeftSeconds = timeLeft,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(16.dp))

      Text(
        text = if (currentTrial.rule == StroopRule.INK) "Selecciona el COLOR DE LA TINTA" else "Selecciona LO QUE DICE LA PALABRA",
        style = MaterialTheme.typography.labelLarge,
        fontWeight = FontWeight.Bold,
        color = if (currentTrial.rule == StroopRule.INK) MaterialTheme.colorScheme.onSurfaceVariant else EmeraldAccent
      )

      Spacer(modifier = Modifier.weight(0.4f))

      // Stimulus card
      Card(
        modifier = Modifier
          .fillMaxWidth(0.9f)
          .height(180.dp),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
      ) {
        Box(
          modifier = Modifier.fillMaxSize(),
          contentAlignment = Alignment.Center
        ) {
          Text(
            text = currentTrial.textName,
            style = MaterialTheme.typography.displayMedium,
            fontWeight = FontWeight.Black,
            color = currentTrial.inkColor,
            letterSpacing = 4.sp,
            textAlign = TextAlign.Center
          )
        }
      }

      Spacer(modifier = Modifier.weight(0.6f))

      // Stable 5 buttons
      Column(
        modifier = Modifier
          .fillMaxWidth(0.9f)
          .padding(bottom = 36.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
      ) {
        // First row of 3
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
          stableColors.take(3).forEach { (name, color) ->
            StroopColorButton(
              name = name,
              color = color,
              isSelected = selectedChoice == name,
              isCorrect = selectedChoice != null && name == currentTrial.correctAnswer,
              isEnabled = selectedChoice == null,
              onClick = { handleChoice(name) },
              modifier = Modifier.weight(1f)
            )
          }
        }
        // Second row of 2
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
          stableColors.drop(3).forEach { (name, color) ->
            StroopColorButton(
              name = name,
              color = color,
              isSelected = selectedChoice == name,
              isCorrect = selectedChoice != null && name == currentTrial.correctAnswer,
              isEnabled = selectedChoice == null,
              onClick = { handleChoice(name) },
              modifier = Modifier.weight(1f)
            )
          }
        }
      }
    }

    ScreenFlashOverlay(
      visible = showFeedbackFlash,
      flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error
    )
  }
}

@Composable
private fun StroopColorButton(
  name: String,
  color: Color,
  isSelected: Boolean,
  isCorrect: Boolean,
  isEnabled: Boolean,
  onClick: () -> Unit,
  modifier: Modifier = Modifier
) {
  Button(
    onClick = onClick,
    enabled = isEnabled,
    modifier = modifier
      .height(56.dp)
      .testTag("btn_stroop_$name"),
    shape = RoundedCornerShape(16.dp),
    colors = ButtonDefaults.buttonColors(
      containerColor = MaterialTheme.colorScheme.surface,
      disabledContainerColor = if (isCorrect) EmeraldAccent else if (isSelected) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.surface.copy(alpha = 0.6f)
    ),
    elevation = ButtonDefaults.buttonElevation(defaultElevation = 2.dp)
  ) {
    Row(verticalAlignment = Alignment.CenterVertically) {
      Box(
        modifier = Modifier
          .size(14.dp)
          .clip(CircleShape)
          .background(color)
      )
      Spacer(modifier = Modifier.width(6.dp))
      Text(
        text = name,
        style = MaterialTheme.typography.labelLarge,
        fontWeight = FontWeight.Bold,
        color = if (isCorrect) Color.White else MaterialTheme.colorScheme.onSurface,
        maxLines = 1
      )
    }
  }
}

private fun generateStroopTrial(level: Int, intensity: Int = 0): StroopItem {
  val names = StroopPalette.map { it.first }
  val textName = names.random()
  // At level 1, occasionally match, at higher levels almost always incongruent
  val ink = if (level > 1) {
    StroopPalette.filter { it.first != textName }.random()
  } else {
    StroopPalette.random()
  }
  val rule = if (Random.nextFloat() < ruleFlipChance(level, intensity)) StroopRule.WORD else StroopRule.INK
  return StroopItem(textName = textName, inkName = ink.first, inkColor = ink.second, rule = rule)
}
