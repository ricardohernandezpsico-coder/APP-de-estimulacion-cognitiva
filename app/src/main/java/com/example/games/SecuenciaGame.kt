package com.example.games

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
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
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalRounds = 5
  var currentRound by remember { mutableStateOf(1) }
  var correctRounds by remember { mutableStateOf(0) }

  val sequenceLength = remember(currentRound, level) { 2 + level + (currentRound - 1) }

  var activeSequence by remember { mutableStateOf(listOf<PadColor>()) }
  var playerInput by remember { mutableStateOf(listOf<PadColor>()) }
  var isDemonstrating by remember { mutableStateOf(true) }
  var highlightedPad by remember { mutableStateOf<PadColor?>(null) }
  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  // Generate sequence for current round
  fun startNewRoundSequence() {
    val pads = PadColor.values()
    val seq = (1..sequenceLength).map { pads[Random.nextInt(pads.size)] }
    activeSequence = seq
    playerInput = emptyList()
    isDemonstrating = true
  }

  LaunchedEffect(currentRound) {
    startNewRoundSequence()
  }

  // Play sequence demo
  LaunchedEffect(activeSequence) {
    if (activeSequence.isNotEmpty() && isDemonstrating) {
      delay(600)
      for (pad in activeSequence) {
        highlightedPad = pad
        delay(450)
        highlightedPad = null
        delay(220)
      }
      isDemonstrating = false
    }
  }

  fun onPadClicked(pad: PadColor) {
    if (isDemonstrating) return
    highlightedPad = pad
    val nextIndex = playerInput.size
    val expected = activeSequence.getOrNull(nextIndex)

    if (expected == pad) {
      val updated = playerInput + pad
      playerInput = updated
      if (updated.size == activeSequence.size) {
        // Round completed successfully!
        correctRounds++
        flashSuccess = true
        showFlash = true
      }
    } else {
      // Mistake!
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
        val finalScore = (correctRounds * 100 / totalRounds).coerceIn(0, 100)
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
        color = if (isDemonstrating) DomainMemoria.copy(alpha = 0.12f) else EmeraldAccent.copy(alpha = 0.12f)
      ) {
        Text(
          text = if (isDemonstrating) "👀 Observa la secuencia (${activeSequence.size} luces)" else "👉 Tu turno: repite la secuencia (${playerInput.size}/${activeSequence.size})",
          modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
          style = MaterialTheme.typography.labelLarge,
          fontWeight = FontWeight.Bold,
          color = if (isDemonstrating) DomainMemoria else EmeraldAccent
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
