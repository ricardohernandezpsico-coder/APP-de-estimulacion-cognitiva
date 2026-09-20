package com.example.games

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Diamond
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
import com.example.ui.theme.DomainMemoria
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random

@Composable
fun RutaTesoroGame(
  level: Int,
  timed: Boolean,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalRounds = 5
  var currentRound by remember { mutableStateOf(1) }
  var correctRounds by remember { mutableStateOf(0) }

  val gridSize = if (level <= 2) 3 else 4 // 3x3 or 4x4
  val totalCells = gridSize * gridSize
  val treasureCount = when (level) {
    1 -> 3
    2 -> 4
    3 -> 5
    4 -> 6
    else -> 7
  }

  var treasurePositions by remember { mutableStateOf(setOf<Int>()) }
  var discoveredPositions by remember { mutableStateOf(setOf<Int>()) }
  var wrongPositions by remember { mutableStateOf(setOf<Int>()) }
  var isShowingPhase by remember { mutableStateOf(true) }
  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  fun setupNewRound() {
    val positions = mutableSetOf<Int>()
    while (positions.size < treasureCount) {
      positions.add(Random.nextInt(totalCells))
    }
    treasurePositions = positions
    discoveredPositions = emptySet()
    wrongPositions = emptySet()
    isShowingPhase = true
  }

  LaunchedEffect(currentRound) {
    setupNewRound()
    delay(2400) // Show diamonds for 2.4s
    isShowingPhase = false
  }

  fun onCellClicked(index: Int) {
    if (isShowingPhase || discoveredPositions.contains(index) || wrongPositions.contains(index)) return

    if (treasurePositions.contains(index)) {
      val updated = discoveredPositions + index
      discoveredPositions = updated
      if (updated.size == treasurePositions.size) {
        // All treasures recovered in round!
        correctRounds++
        flashSuccess = true
        showFlash = true
      }
    } else {
      // Wrong cell clicked
      wrongPositions = wrongPositions + index
      if (wrongPositions.size >= 2) {
        // Failed round after 2 misses
        flashSuccess = false
        showFlash = true
      }
    }
  }

  LaunchedEffect(showFlash) {
    if (showFlash) {
      delay(700)
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
        title = "Ruta del Tesoro",
        domain = DomainType.MEMORIA,
        currentRound = currentRound,
        totalRounds = totalRounds,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(16.dp))

      Surface(
        shape = RoundedCornerShape(14.dp),
        color = if (isShowingPhase) DomainMemoria.copy(alpha = 0.12f) else EmeraldAccent.copy(alpha = 0.12f)
      ) {
        Text(
          text = if (isShowingPhase) "👀 Memoriza la ubicación de los $treasureCount tesoros" else "💎 Recupera los tesoros (${discoveredPositions.size}/$treasureCount)",
          modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
          style = MaterialTheme.typography.labelLarge,
          fontWeight = FontWeight.Bold,
          color = if (isShowingPhase) DomainMemoria else EmeraldAccent
        )
      }

      Spacer(modifier = Modifier.weight(1f))

      // Square Grid
      Column(
        verticalArrangement = Arrangement.spacedBy(8.dp),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        for (row in 0 until gridSize) {
          Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            for (col in 0 until gridSize) {
              val index = row * gridSize + col
              val isTreasure = treasurePositions.contains(index)
              val isDiscovered = discoveredPositions.contains(index)
              val isWrong = wrongPositions.contains(index)

              val cellSize = if (gridSize == 3) 88.dp else 68.dp

              Surface(
                modifier = Modifier
                  .size(cellSize)
                  .clip(RoundedCornerShape(16.dp))
                  .clickable(enabled = !isShowingPhase && !isDiscovered && !isWrong) {
                    onCellClicked(index)
                  }
                  .testTag("treasure_cell_$index"),
                shape = RoundedCornerShape(16.dp),
                color = when {
                  isShowingPhase && isTreasure -> DomainMemoria.copy(alpha = 0.25f)
                  isDiscovered -> EmeraldAccent.copy(alpha = 0.25f)
                  isWrong -> MaterialTheme.colorScheme.error.copy(alpha = 0.2f)
                  else -> MaterialTheme.colorScheme.surface
                },
                shadowElevation = 2.dp
              ) {
                Box(
                  modifier = Modifier.fillMaxSize(),
                  contentAlignment = Alignment.Center
                ) {
                  if ((isShowingPhase && isTreasure) || isDiscovered) {
                    Text(text = "💠", fontSize = if (gridSize == 3) 34.sp else 26.sp)
                  } else if (isWrong) {
                    Text(text = "❌", fontSize = 24.sp)
                  }
                }
              }
            }
          }
        }
      }

      Spacer(modifier = Modifier.weight(1f))
    }

    ScreenFlashOverlay(visible = showFlash, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
  }
}
