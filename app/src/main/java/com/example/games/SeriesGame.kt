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

/** Auditoría (21-sep): "Modo Reto" no hacía nada acá -- `timed` solo llegaba al
 *  header como ícono cosmético. Piso más alto que en los demás juegos con
 *  temporizador porque razonar una regla lógica lleva más tiempo que reconocer
 *  un color o una dirección. */
private fun baseTimeForSeries(level: Int, intensity: Int): Int {
  val byLevel = when (level) { 1 -> 14; 2 -> 13; 3 -> 12; 4 -> 11; else -> 10 }
  val fromMastery = intensity / 4
  return (byLevel - fromMastery).coerceAtLeast(6)
}

@Composable
fun SeriesGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 8
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }
  var currentStreak by remember { mutableStateOf(0) }

  // DDA: igual criterio que Calculo — 3 aciertos seguidos EN ESTA PARTIDA
  // adelantan dificultad que normalmente solo llegaría con masteryStreak entre
  // sesiones. Se resetea a 0 apenas se falla una serie.
  val liveIntensity = intensity + (currentStreak / 3) * 2

  var currentItem by remember { mutableStateOf(generateSeries(level, liveIntensity)) }
  var selectedChoice by remember { mutableStateOf<String?>(null) }
  var showExplanation by remember { mutableStateOf(false) }

  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  val baseTime = remember(level, intensity) { baseTimeForSeries(level, intensity) }
  var timeLeft by remember { mutableStateOf(if (timed) baseTime else null) }

  fun handleChoice(choice: String) {
    if (selectedChoice != null) return
    selectedChoice = choice
    val isCorrect = choice == currentItem.answer

    if (isCorrect) {
      correctCount++
      currentStreak++
      flashSuccess = true
    } else {
      currentStreak = 0
      flashSuccess = false
    }
    showExplanation = true
    showFlash = true
  }

  LaunchedEffect(currentRound, timed) {
    if (timed) {
      timeLeft = baseTime
      while ((timeLeft ?: 0) > 0) {
        delay(1000)
        timeLeft = (timeLeft ?: 1) - 1
      }
      if (selectedChoice == null) {
        handleChoice("TIMEOUT")
      }
    }
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
        currentItem = generateSeries(level, liveIntensity)
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
        timeLeftSeconds = timeLeft,
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

/**
 * Antes solo el tipo "multiplicación" reaccionaba al nivel (y solo 2 escalones);
 * los otros 4 tipos eran idénticos del nivel 1 al 5. Ahora `boost` (nivel +
 * maestría) empuja pasos, magnitudes y complejidad en los 5 tipos, y sigue
 * subiendo sin límite más allá de nivel 5 vía `intensity`.
 */
private fun generateSeries(level: Int, intensity: Int = 0): SeriesItem {
  val boost = level + intensity / 3
  // Del nivel 4 en adelante se suma un 6to tipo (cubos) — más difícil de
  // reconocer que los cuadrados y crece mucho más rápido.
  val typeCount = if (level >= 4) 6 else 5
  val type = Random.nextInt(typeCount)
  val tightDelta = (8 - intensity / 4).coerceAtLeast(3)
  return when (type) {
    0 -> {
      // Linear step (+k), paso y arranque crecen con boost
      val step = Random.nextInt(2 + boost / 2, 6 + boost)
      val start = Random.nextInt(2, 20 + boost * 2)
      val s1 = start
      val s2 = s1 + step
      val s3 = s2 + step
      val s4 = s3 + step
      val ans = s4 + step
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Suma fija de +$step en cada término", tightDelta)
    }
    1 -> {
      // Multiplication: el multiplicador ya no se topa en 3
      val mult = when {
        level <= 2 -> 2
        level == 3 -> 3
        level == 4 -> 4
        else -> (4 + intensity / 6).coerceAtMost(7)
      }
      val start = Random.nextInt(2, 5)
      val s1 = start
      val s2 = s1 * mult
      val s3 = s2 * mult
      val s4 = s3 * mult
      val ans = s4 * mult
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Multiplicación por $mult en cada paso", tightDelta)
    }
    2 -> {
      // Decreasing (-k), paso crece con boost
      val step = Random.nextInt(3 + boost / 2, 8 + boost)
      val start = 50 + boost * 3 + step * 4
      val s1 = start
      val s2 = s1 - step
      val s3 = s2 - step
      val s4 = s3 - step
      val ans = s4 - step
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Resta constante de -$step en cada paso", tightDelta)
    }
    3 -> {
      // Increasing difference: la unidad de incremento crece con boost
      // (antes siempre +2,+4,+6,+8 sin importar el nivel)
      val incUnit = 2 + boost / 2
      val start = Random.nextInt(1, 10 + boost)
      val s1 = start
      val s2 = s1 + incUnit
      val s3 = s2 + incUnit * 2
      val s4 = s3 + incUnit * 3
      val ans = s4 + incUnit * 4
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Diferencia creciente: +$incUnit, +${incUnit * 2}, +${incUnit * 3}, +${incUnit * 4}...", tightDelta)
    }
    4 -> {
      // Squares: el punto de partida crece con boost (números más grandes,
      // menos evidente que son cuadrados a simple vista)
      val offset = Random.nextInt(1, 4 + boost / 3)
      val s1 = (offset) * (offset)
      val s2 = (offset + 1) * (offset + 1)
      val s3 = (offset + 2) * (offset + 2)
      val s4 = (offset + 3) * (offset + 3)
      val ans = (offset + 4) * (offset + 4)
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Cuadrados perfectos consecutivos", tightDelta)
    }
    else -> {
      // Cubes: solo desde nivel 4 — crecen mucho más rápido que los cuadrados
      val offset = Random.nextInt(1, 3)
      val s1 = offset * offset * offset
      val s2 = (offset + 1).let { it * it * it }
      val s3 = (offset + 2).let { it * it * it }
      val s4 = (offset + 3).let { it * it * it }
      val ans = (offset + 4).let { it * it * it }
      createSeriesItem("$s1,  $s2,  $s3,  $s4,  ?", ans.toString(), "Cubos perfectos consecutivos", tightDelta)
    }
  }
}

private fun createSeriesItem(sequenceText: String, answer: String, explanation: String, deltaRange: Int = 8): SeriesItem {
  val intAns = answer.toIntOrNull() ?: 20
  val opts = mutableSetOf(answer)
  while (opts.size < 4) {
    val delta = Random.nextInt(-deltaRange, deltaRange + 1)
    if (delta != 0) {
      opts.add((intAns + delta).toString())
    }
  }
  return SeriesItem(sequenceText, answer, opts.shuffled(), explanation)
}
