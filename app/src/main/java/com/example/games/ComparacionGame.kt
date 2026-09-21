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

/** Auditoría (21-sep): "Modo Reto" no hacía nada acá -- `timed` solo llegaba al
 *  header como ícono cosmético. Es un juego de velocidad por definición, así que
 *  el piso es más bajo que en los demás juegos con temporizador. */
private fun baseTimeForComparacion(level: Int, intensity: Int): Int {
  val byLevel = when (level) { 1 -> 6; 2 -> 6; 3 -> 5; 4 -> 5; else -> 4 }
  val fromMastery = intensity / 10
  return (byLevel - fromMastery).coerceAtLeast(2)
}

@Composable
fun ComparacionGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 12
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }
  // Auditoría (21-9): antes se acumulaba un `scorePoints` con bono de velocidad
  // que NUNCA se usaba para el puntaje final (siempre `correctas/total*100`) ni
  // se mostraba en pantalla -- una funcionalidad a medio implementar. Ahora
  // `speedBonusHits` sí alimenta el puntaje final (ver más abajo), sin que la
  // precisión deje de ser lo que más pesa: acertar todo siempre da 100, el bono
  // de velocidad solo suma cuando la precisión no es perfecta.
  var speedBonusHits by remember { mutableStateOf(0) }

  var currentTrial by remember { mutableStateOf(generateTrial(level, intensity)) }
  // El bono de velocidad exigía <900ms sin importar el nivel; ahora la ventana
  // se acorta con la maestría (piso 500ms) para seguir premiando ser más rápido.
  val speedBonusMs = (900 - intensity * 15).coerceAtLeast(500)
  var selectedSide by remember { mutableStateOf<String?>(null) }
  var timedOut by remember { mutableStateOf(false) }
  var trialStartTime by remember { mutableStateOf(System.currentTimeMillis()) }

  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  val baseTime = remember(level, intensity) { baseTimeForComparacion(level, intensity) }
  var timeLeft by remember { mutableStateOf(if (timed) baseTime else null) }

  LaunchedEffect(currentRound) {
    trialStartTime = System.currentTimeMillis()
  }

  fun handlePick(side: String) {
    if (selectedSide != null || timedOut) return
    selectedSide = side
    val reactionMs = System.currentTimeMillis() - trialStartTime
    val isLeftGreater = currentTrial.left.numericValue >= currentTrial.right.numericValue
    val isCorrect = (side == "LEFT" && isLeftGreater) || (side == "RIGHT" && !isLeftGreater)

    if (isCorrect) {
      correctCount++
      if (reactionMs < speedBonusMs) speedBonusHits++
      flashSuccess = true
    } else {
      flashSuccess = false
    }
    showFlash = true
  }

  LaunchedEffect(currentRound, timed) {
    if (timed) {
      timeLeft = baseTime
      while ((timeLeft ?: 0) > 0) {
        delay(1000)
        timeLeft = (timeLeft ?: 1) - 1
      }
      if (selectedSide == null && !timedOut) {
        timedOut = true
        flashSuccess = false
        showFlash = true
      }
    }
  }

  LaunchedEffect(selectedSide, timedOut) {
    if (selectedSide != null || timedOut) {
      delay(450)
      showFlash = false
      if (currentRound >= totalTrials) {
        val baseScore = correctCount * 100 / totalTrials
        // Bono de velocidad: hasta +10 si TODAS las rondas correctas fueron
        // rápidas -- nunca puede bajar el puntaje, y con 100% de precisión ya se
        // llega a 100 igual (el `coerceIn` lo tapa), así que el bono solo se
        // nota cuando la precisión no fue perfecta.
        val speedBonus = (speedBonusHits * 10 / totalTrials)
        val finalScore = (baseScore + speedBonus).coerceIn(0, 100)
        onFinish(finalScore, correctCount, totalTrials)
      } else {
        currentRound++
        currentTrial = generateTrial(level, intensity)
        selectedSide = null
        timedOut = false
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
        timeLeftSeconds = timeLeft,
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

private fun generateTrial(level: Int, intensity: Int = 0): ComparisonTrial {
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
    // Simple expression vs number. Antes nivel 3, 4 y 5 eran idénticos; ahora
    // `boost` sigue subiendo los productos y acercando los valores (más difícil
    // de distinguir a simple vista) sin límite más allá de nivel 5.
    val boost = (level - 3) + intensity / 5
    val a = Random.nextInt(4 + boost / 2, 9 + boost)
    val b = Random.nextInt(4 + boost / 2, 9 + boost)
    val prod = a * b
    val closeness = (6 - intensity / 6).coerceAtLeast(2)
    // Auditoría (21-sep): `Random.nextInt(-closeness, closeness + 1)` incluye el
    // 0, así que antes podía salir un empate real (compareVal == prod). Con
    // `isLeftGreater = left >= right` en `handlePick`, un empate SIEMPRE se
    // resolvía a favor de "IZQUIERDA" sin que hubiera manera de saberlo mirando
    // la pantalla -- el jugador tenía que adivinar. Se repite el sorteo hasta
    // que no empate, mismo criterio que ya usan los niveles 1 y 2 más arriba.
    var compareVal = prod + Random.nextInt(-closeness, closeness + 1)
    while (compareVal == prod) {
      compareVal = prod + Random.nextInt(-closeness, closeness + 1)
    }
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
