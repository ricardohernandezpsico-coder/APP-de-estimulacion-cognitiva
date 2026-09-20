package com.example.games

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.*
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.random.Random

enum class PadColor(val id: Int, val normalColor: Color, val lightColor: Color, val label: String) {
  AZUL(0, Color(0xFF1D4ED8), Color(0xFF60A5FA), "Azul"),
  AMBAR(1, Color(0xFFB45309), Color(0xFFFBBF24), "Ámbar"),
  VERDE(2, Color(0xFF047857), Color(0xFF34D399), "Verde"),
  ROSA(3, Color(0xFFBE123C), Color(0xFFFB7185), "Rosa")
}

@Composable
fun SecuenciaGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalRounds = 5
  var currentRound by remember { mutableStateOf(1) }
  var correctRounds by remember { mutableStateOf(0) }

  // ANTES: la secuencia crecía en CADA ronda sin importar si la anterior se
  // acertó o no (ronda 1 = 3 luces, ronda 5 = 7 luces, siempre) y un solo error
  // terminaba la ronda sin reintento — para alguien nuevo eso se siente
  // literalmente como "no me daba tiempo": la dificultad se le venía encima
  // pasara lo que pasara. Ahora `roundLength` sube 1 luz tras un acierto, baja 1
  // tras un error (piso en 2), y solo el punto de partida escala con nivel/
  // intensity — la dificultad reacciona a CÓMO le va, no al número de ronda.
  var roundLength by remember { mutableStateOf(2 + level + intensity / 3) }

  // Velocidad de reproducción: se acelera con la maestría (piso de 220ms/luz para
  // que siga siendo seguible).
  val litMs = (450 - intensity * 8).coerceAtLeast(220)
  val gapMs = (220 - intensity * 4).coerceAtLeast(110)

  var activeSequence by remember { mutableStateOf(listOf<PadColor>()) }
  var playerInput by remember { mutableStateOf(listOf<PadColor>()) }
  var isDemonstrating by remember { mutableStateOf(true) }
  var isReady by remember { mutableStateOf(false) } // breve pausa entre "mostrar" y "tu turno"
  var highlightedPad by remember { mutableStateOf<PadColor?>(null) }
  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  // Métrica de tiempo (pedido explícito): tiempo de reacción por toque, desde
  // que puede responder hasta que toca cada pad. Influye en el puntaje final
  // Y ahora se MUESTRA (antes solo se usaba en silencio para el puntaje —
  // Ricardo no se había dado cuenta de que existía).
  var inputReadyAt by remember { mutableStateOf(0L) }
  var reactionTimesMs by remember { mutableStateOf(listOf<Long>()) }
  var roundFeedback by remember { mutableStateOf<String?>(null) }
  val scope = rememberCoroutineScope()

  // Generate sequence for current round
  fun startNewRoundSequence() {
    val pads = PadColor.values()
    val seq = (1..roundLength).map { pads[Random.nextInt(pads.size)] }
    activeSequence = seq
    playerInput = emptyList()
    isDemonstrating = true
    isReady = false
    roundFeedback = null
  }

  LaunchedEffect(currentRound) {
    startNewRoundSequence()
  }

  // Play sequence demo, luego una pausa breve de "¡Prepárate!" antes de
  // habilitar los toques — sin esa pausa, el cambio de "observa" a "tu turno"
  // era instantáneo y sorprendía al usuario justo cuando más atento debía estar.
  LaunchedEffect(activeSequence) {
    if (activeSequence.isNotEmpty() && isDemonstrating) {
      delay(600)
      for (pad in activeSequence) {
        highlightedPad = pad
        delay(litMs.toLong())
        highlightedPad = null
        delay(gapMs.toLong())
      }
      isDemonstrating = false
      isReady = true
      delay(500)
      isReady = false
      inputReadyAt = System.currentTimeMillis()
    }
  }

  fun onPadClicked(pad: PadColor) {
    if (isDemonstrating || isReady) return
    // ANTES: el pad se quedaba encendido sin apagarse hasta el próximo toque
    // (se sentía "estático" — Ricardo lo notó jugando). Ahora cada toque es un
    // pulso: se enciende y se apaga solo 180ms después, como en la demostración.
    highlightedPad = pad
    scope.launch {
      delay(180)
      if (highlightedPad == pad) highlightedPad = null
    }
    val now = System.currentTimeMillis()
    val reactionMs = now - inputReadyAt
    reactionTimesMs = reactionTimesMs + reactionMs
    inputReadyAt = now
    val nextIndex = playerInput.size
    val expected = activeSequence.getOrNull(nextIndex)

    if (expected == pad) {
      val updated = playerInput + pad
      playerInput = updated
      if (updated.size == activeSequence.size) {
        // Round completed successfully!
        correctRounds++
        roundLength += 1
        val roundAvg = reactionTimesMs.takeLast(activeSequence.size).average().toInt()
        roundFeedback = "⚡ ${roundAvg}ms de reacción promedio"
        flashSuccess = true
        showFlash = true
      }
    } else {
      // Mistake! La próxima ronda es un paso más fácil, no más difícil.
      roundLength = (roundLength - 1).coerceAtLeast(2)
      roundFeedback = null
      flashSuccess = false
      showFlash = true
    }
  }

  LaunchedEffect(showFlash) {
    if (showFlash) {
      delay(400)
      highlightedPad = null
      delay(500)
      showFlash = false
      if (currentRound >= totalRounds) {
        val baseScore = correctRounds * 100 / totalRounds
        val avgReaction = reactionTimesMs.takeIf { it.isNotEmpty() }?.average() ?: 1500.0
        // Bono de velocidad: responder rápido Y bien empuja el puntaje más allá
        // de los saltos de 20 en 20 que da correctRounds por sí solo.
        val speedBonus = when {
          avgReaction < 500 -> 10
          avgReaction < 800 -> 5
          else -> 0
        }
        val finalScore = (baseScore + speedBonus).coerceIn(0, 100)
        onFinish(finalScore, correctRounds, totalRounds)
      } else {
        currentRound++
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
        title = "Secuencia Lumínica",
        domain = DomainType.MEMORIA,
        currentRound = currentRound,
        totalRounds = totalRounds,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(20.dp))

      // Status indicator
      Surface(
        shape = RoundedCornerShape(16.dp),
        color = when {
          isDemonstrating -> DomainMemoria.copy(alpha = 0.12f)
          isReady -> Color(0xFFF59E0B).copy(alpha = 0.15f)
          else -> EmeraldAccent.copy(alpha = 0.12f)
        }
      ) {
        Text(
          text = when {
            isDemonstrating -> "👀 Observa la secuencia (${activeSequence.size} luces)"
            isReady -> "✋ ¡Prepárate!"
            else -> "👉 Tu turno: repite la secuencia (${playerInput.size}/${activeSequence.size})"
          },
          modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
          style = MaterialTheme.typography.labelLarge,
          fontWeight = FontWeight.Bold,
          color = when {
            isDemonstrating -> DomainMemoria
            isReady -> Color(0xFFD97706)
            else -> EmeraldAccent
          }
        )
      }

      AnimatedVisibility(visible = roundFeedback != null, enter = fadeIn(), exit = fadeOut()) {
        Text(
          text = roundFeedback.orEmpty(),
          modifier = Modifier.padding(top = 10.dp),
          style = MaterialTheme.typography.labelMedium,
          fontWeight = FontWeight.Bold,
          color = Color(0xFFD97706)
        )
      }

      Spacer(modifier = Modifier.weight(1f))

      // 2x2 Glowing Pads Grid
      Column(
        modifier = Modifier.padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
          SequencePad(
            pad = PadColor.AZUL,
            isLit = highlightedPad == PadColor.AZUL,
            onClick = { onPadClicked(PadColor.AZUL) }
          )
          SequencePad(
            pad = PadColor.AMBAR,
            isLit = highlightedPad == PadColor.AMBAR,
            onClick = { onPadClicked(PadColor.AMBAR) }
          )
        }
        Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
          SequencePad(
            pad = PadColor.VERDE,
            isLit = highlightedPad == PadColor.VERDE,
            onClick = { onPadClicked(PadColor.VERDE) }
          )
          SequencePad(
            pad = PadColor.ROSA,
            isLit = highlightedPad == PadColor.ROSA,
            onClick = { onPadClicked(PadColor.ROSA) }
          )
        }
      }

      Spacer(modifier = Modifier.weight(1f))
    }

    ScreenFlashOverlay(visible = showFlash, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
  }
}

@Composable
private fun SequencePad(
  pad: PadColor,
  isLit: Boolean,
  onClick: () -> Unit
) {
  val scale by animateFloatAsState(
    targetValue = if (isLit) 1.08f else 1.0f,
    animationSpec = tween(150)
  )
  val color by animateColorAsState(
    targetValue = if (isLit) pad.lightColor else pad.normalColor,
    animationSpec = tween(150)
  )

  Surface(
    modifier = Modifier
      .size(130.dp)
      .scale(scale)
      .clip(RoundedCornerShape(28.dp))
      .clickable { onClick() }
      .testTag("pad_${pad.name.lowercase()}"),
    shape = RoundedCornerShape(28.dp),
    color = color,
    shadowElevation = if (isLit) 12.dp else 4.dp
  ) {
    Box(
      modifier = Modifier.fillMaxSize(),
      contentAlignment = Alignment.Center
    ) {
      Text(
        text = pad.label,
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.Bold,
        color = Color.White.copy(alpha = if (isLit) 1f else 0.8f)
      )
    }
  }
}
