package com.example.games

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.tween
import androidx.compose.animation.scaleIn
import androidx.compose.animation.scaleOut
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
import com.example.model.GameRegistry
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainCalculo
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random

data class MathQuestion(
  val prompt: String,
  val answer: Int,
  val options: List<Int>
)

/**
 * Tiempo base por nivel, con recorte continuo por `intensity` (maestría más allá de
 * nivel 5) — igual filosofía que Stroop: el reto nunca deja de crecer.
 */
private fun baseTimeForCalculo(level: Int, intensity: Int): Int {
  val byLevel = 16 - level // 15..11
  val fromMastery = intensity / 2
  return (byLevel - fromMastery).coerceAtLeast(6)
}

@Composable
fun CalculoGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 10
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }
  var currentStreak by remember { mutableStateOf(0) }
  var scorePoints by remember { mutableStateOf(0) }

  // DDA: cada 3 aciertos seguidos DENTRO de esta partida suman dureza extra
  // (como si fuera intensity temporal) — se autocorrige solo, porque vuelve a 0
  // apenas se falla una. Así encadenar respuestas correctas no se siente "más
  // de lo mismo": el próximo ítem ya viene un poco más exigente sin esperar a
  // la siguiente sesión.
  val liveIntensity = intensity + (currentStreak / 3) * 2

  var currentQuestion by remember { mutableStateOf(generateQuestion(level, liveIntensity)) }
  var selectedAnswer by remember { mutableStateOf<Int?>(null) }
  var showFeedbackFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }
  var streakPopupText by remember { mutableStateOf<String?>(null) }

  val baseTime = baseTimeForCalculo(level, liveIntensity)
  // Timer countdown if timed mode
  var timeLeft by remember { mutableStateOf(if (timed) baseTime else null) }

  fun handleSelection(chosen: Int, correct: Int) {
    if (selectedAnswer != null) return
    selectedAnswer = chosen
    val isCorrect = chosen == correct

    if (isCorrect) {
      correctCount++
      currentStreak++
      val streakBonus = (currentStreak - 1) * 2
      scorePoints += (10 + streakBonus)
      flashSuccess = true
      if (currentStreak >= 2) {
        streakPopupText = "¡Racha x$currentStreak! +${10 + streakBonus}"
      }
    } else {
      currentStreak = 0
      flashSuccess = false
      streakPopupText = null
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
      // Time expired counts as incorrect
      if (selectedAnswer == null) {
        handleSelection(-999, currentQuestion.answer)
      }
    }
  }

  LaunchedEffect(selectedAnswer) {
    if (selectedAnswer != null) {
      delay(700)
      showFeedbackFlash = false
      streakPopupText = null
      if (currentRound >= totalTrials) {
        val finalScore = (scorePoints * 100 / (totalTrials * 15)).coerceIn(0, 100)
        onFinish(finalScore, correctCount, totalTrials)
      } else {
        currentRound++
        currentQuestion = generateQuestion(level, liveIntensity)
        selectedAnswer = null
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
        title = "Cálculo Sereno",
        domain = DomainType.CALCULO,
        currentRound = currentRound,
        totalRounds = totalTrials,
        isTimed = timed,
        timeLeftSeconds = timeLeft,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(24.dp))

      // Streak & score HUD
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .padding(horizontal = 24.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        Surface(
          shape = RoundedCornerShape(12.dp),
          color = DomainCalculo.copy(alpha = 0.12f)
        ) {
          Text(
            text = "Puntos: $scorePoints",
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
            style = MaterialTheme.typography.labelLarge,
            fontWeight = FontWeight.Bold,
            color = DomainCalculo
          )
        }

        if (currentStreak >= 2) {
          Surface(
            shape = RoundedCornerShape(12.dp),
            color = Color(0xFFF59E0B).copy(alpha = 0.15f)
          ) {
            Text(
              text = "🔥 Racha: $currentStreak",
              modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
              style = MaterialTheme.typography.labelLarge,
              fontWeight = FontWeight.Bold,
              color = Color(0xFFD97706)
            )
          }
        }
      }

      Spacer(modifier = Modifier.weight(0.4f))

      // Equation prompt card
      Card(
        modifier = Modifier
          .fillMaxWidth(0.9f)
          .heightIn(min = 150.dp),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
      ) {
        Box(
          modifier = Modifier
            .fillMaxWidth()
            .padding(24.dp),
          contentAlignment = Alignment.Center
        ) {
          Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text(
              text = currentQuestion.prompt,
              style = MaterialTheme.typography.displayMedium,
              fontWeight = FontWeight.Black,
              color = MaterialTheme.colorScheme.onSurface,
              textAlign = TextAlign.Center
            )

            AnimatedVisibility(
              visible = streakPopupText != null,
              enter = scaleIn(tween(250)),
              exit = scaleOut()
            ) {
              Text(
                text = streakPopupText.orEmpty(),
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.ExtraBold,
                color = EmeraldAccent,
                modifier = Modifier.padding(top = 8.dp)
              )
            }
          }
        }
      }

      Spacer(modifier = Modifier.weight(0.6f))

      // 2x2 grid of options
      Column(
        modifier = Modifier
          .fillMaxWidth(0.92f)
          .padding(bottom = 32.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
      ) {
        val options = currentQuestion.options
        for (row in 0..1) {
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(14.dp)
          ) {
            for (col in 0..1) {
              val index = row * 2 + col
              if (index < options.size) {
                val optionVal = options[index]
                val isSelected = selectedAnswer == optionVal
                val isCorrectAnswer = optionVal == currentQuestion.answer

                val btnColor = when {
                  selectedAnswer == null -> MaterialTheme.colorScheme.surface
                  isSelected && isCorrectAnswer -> EmeraldAccent
                  isSelected && !isCorrectAnswer -> MaterialTheme.colorScheme.error
                  isCorrectAnswer -> EmeraldAccent.copy(alpha = 0.8f)
                  else -> MaterialTheme.colorScheme.surface.copy(alpha = 0.5f)
                }

                val contentColor = when {
                  selectedAnswer == null -> MaterialTheme.colorScheme.onSurface
                  isSelected || (selectedAnswer != null && isCorrectAnswer) -> Color.White
                  else -> MaterialTheme.colorScheme.onSurface.copy(alpha = 0.5f)
                }

                Button(
                  onClick = { handleSelection(optionVal, currentQuestion.answer) },
                  enabled = selectedAnswer == null,
                  modifier = Modifier
                    .weight(1f)
                    .height(68.dp)
                    .testTag("btn_calculo_opt_$index"),
                  shape = RoundedCornerShape(18.dp),
                  colors = ButtonDefaults.buttonColors(
                    containerColor = btnColor,
                    disabledContainerColor = btnColor
                  ),
                  elevation = ButtonDefaults.buttonElevation(defaultElevation = 2.dp)
                ) {
                  Text(
                    text = "$optionVal",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    color = contentColor
                  )
                }
              }
            }
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

private fun generateQuestion(level: Int, intensity: Int = 0): MathQuestion {
  val r = Random
  // Más allá de nivel 5 (Experto), cada tramo de maestría sigue empujando los
  // números hacia arriba y los distractores hacia números más parecidos entre sí
  // — el nivel 5 deja de ser un techo real.
  val boost = if (level >= 5) intensity * 2 else 0
  val tightDelta = (7 - intensity / 4).coerceAtLeast(3)
  return when (level) {
    1 -> {
      val isAdd = r.nextBoolean()
      val a = r.nextInt(3, 15)
      val b = r.nextInt(2, 12)
      if (isAdd) {
        createQuestion("$a + $b = ?", a + b)
      } else {
        val big = maxOf(a, b)
        val small = minOf(a, b)
        createQuestion("$big − $small = ?", big - small)
      }
    }
    2 -> {
      val type = r.nextInt(3)
      when (type) {
        0 -> {
          val a = r.nextInt(12, 35)
          val b = r.nextInt(9, 25)
          createQuestion("$a + $b = ?", a + b)
        }
        1 -> {
          val a = r.nextInt(20, 50)
          val b = r.nextInt(7, 20)
          createQuestion("$a − $b = ?", a - b)
        }
        else -> {
          val a = r.nextInt(3, 9)
          val b = r.nextInt(3, 9)
          createQuestion("$a × $b = ?", a * b)
        }
      }
    }
    3 -> {
      // Antes solo multiplicación acá; ahora alterna con división (limpia, sin
      // resto) — "aritmética mental" sin dividir nunca era un hueco de contenido.
      if (r.nextBoolean()) {
        val a = r.nextInt(6, 12)
        val b = r.nextInt(4, 12)
        createQuestion("$a × $b = ?", a * b)
      } else {
        val divisor = r.nextInt(3, 9)
        val quotient = r.nextInt(4, 12)
        createQuestion("${divisor * quotient} ÷ $divisor = ?", quotient)
      }
    }
    4 -> {
      if (r.nextBoolean()) {
        val a = r.nextInt(5, 12)
        val b = r.nextInt(3, 9)
        val c = r.nextInt(2, 15)
        val isSub = r.nextBoolean()
        val ans = if (isSub) a * b - c else a * b + c
        val sign = if (isSub) "−" else "+"
        createQuestion("($a × $b) $sign $c = ?", ans)
      } else {
        // División compuesta: (a × b) ÷ c, siempre exacta
        val c = r.nextInt(2, 6)
        val quotient = r.nextInt(4, 15)
        val product = c * quotient
        val a = (2..product / 2).filter { product % it == 0 }.randomOrNull() ?: 1
        val b = product / a
        createQuestion("($a × $b) ÷ $c = ?", quotient)
      }
    }
    else -> {
      if (r.nextBoolean()) {
        val a = r.nextInt(11 + boost, 20 + boost)
        val b = r.nextInt(6 + boost / 2, 15 + boost / 2)
        val c = r.nextInt(10 + boost, 30 + boost)
        createQuestion("$a × $b − $c = ?", a * b - c, tightDelta)
      } else {
        // División con números más grandes que en nivel 3, sigue creciendo con boost
        val divisor = r.nextInt(4 + boost / 3, 9 + boost / 2)
        val quotient = r.nextInt(6 + boost / 2, 18 + boost)
        createQuestion("${divisor * quotient} ÷ $divisor = ?", quotient, tightDelta)
      }
    }
  }
}

private fun createQuestion(prompt: String, answer: Int, deltaRange: Int = 6): MathQuestion {
  val options = mutableSetOf(answer)
  while (options.size < 4) {
    val delta = Random.nextInt(-deltaRange, deltaRange + 1)
    if (delta != 0) {
      val candidate = (answer + delta).coerceAtLeast(0)
      options.add(candidate)
    }
  }
  return MathQuestion(prompt, answer, options.shuffled())
}
