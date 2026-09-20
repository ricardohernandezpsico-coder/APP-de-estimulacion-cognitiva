package com.example.games

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Backspace
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material.icons.filled.SkipNext
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
import com.example.ui.theme.DomainLenguaje
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay

data class AnagramItem(
  val targetWord: String,
  val clue: String
)

val AnagramBank = listOf(
  AnagramItem("SOL", "Astro central"),
  AnagramItem("VIDA", "Lo que disfrutamos y cuidamos"),
  AnagramItem("MENTE", "Capacidad cognitiva humana"),
  AnagramItem("LIBRO", "Contiene historias y saber"),
  AnagramItem("RUTAS", "Caminos para explorar"),
  AnagramItem("CALMA", "Estado de tranquilidad"),
  AnagramItem("TIEMPO", "Pasa segundo a segundo"),
  AnagramItem("SINFONIA", "Armonía de muchas notas")
)

@Composable
fun AnagramasGame(
  level: Int,
  timed: Boolean,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val totalTrials = 6
  var currentRound by remember { mutableStateOf(1) }
  var correctCount by remember { mutableStateOf(0) }

  // Filter words roughly by length based on level
  val availableWords = remember(level) {
    when (level) {
      1 -> AnagramBank.filter { it.targetWord.length <= 4 }
      2 -> AnagramBank.filter { it.targetWord.length in 4..5 }
      else -> AnagramBank.filter { it.targetWord.length >= 5 }
    }.ifEmpty { AnagramBank }
  }

  var currentItem by remember { mutableStateOf(availableWords.random()) }
  var scrambledLetters by remember {
    mutableStateOf(scramble(currentItem.targetWord))
  }
  var usedLetterIndices by remember { mutableStateOf(setOf<Int>()) }
  var currentAssembly by remember { mutableStateOf(listOf<Pair<Int, Char>>()) }
  var showClue by remember { mutableStateOf(false) }

  var showFlash by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  fun onLetterTapped(index: Int, char: Char) {
    if (usedLetterIndices.contains(index)) return
    val updatedAssembly = currentAssembly + Pair(index, char)
    currentAssembly = updatedAssembly
    usedLetterIndices = usedLetterIndices + index

    // If word filled, check answer
    if (updatedAssembly.size == currentItem.targetWord.length) {
      val formedWord = updatedAssembly.map { it.second }.joinToString("")
      if (formedWord == currentItem.targetWord) {
        correctCount++
        flashSuccess = true
      } else {
        flashSuccess = false
      }
      showFlash = true
    }
  }

  fun onBackspace() {
    if (currentAssembly.isNotEmpty()) {
      val last = currentAssembly.last()
      currentAssembly = currentAssembly.dropLast(1)
      usedLetterIndices = usedLetterIndices - last.first
    }
  }

  fun nextWord() {
    if (currentRound >= totalTrials) {
      val finalScore = (correctCount * 100 / totalTrials).coerceIn(0, 100)
      onFinish(finalScore, correctCount, totalTrials)
    } else {
      currentRound++
      currentItem = availableWords.random()
      scrambledLetters = scramble(currentItem.targetWord)
      currentAssembly = emptyList()
      usedLetterIndices = emptySet()
      showClue = false
    }
  }

  LaunchedEffect(showFlash) {
    if (showFlash) {
      delay(700)
      showFlash = false
      nextWord()
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
        title = "Anagramas",
        domain = DomainType.LENGUAJE,
        currentRound = currentRound,
        totalRounds = totalTrials,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(16.dp))

      // Clue / Hint toggle
      if (showClue) {
        Surface(
          shape = RoundedCornerShape(12.dp),
          color = DomainLenguaje.copy(alpha = 0.12f),
          modifier = Modifier.padding(horizontal = 24.dp)
        ) {
          Text(
            text = "💡 Pista: ${currentItem.clue}",
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.SemiBold,
            color = DomainLenguaje,
            modifier = Modifier.padding(horizontal = 14.dp, vertical = 6.dp)
          )
        }
      }

      Spacer(modifier = Modifier.weight(0.4f))

      // Word slots where formed letters go
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .padding(horizontal = 20.dp),
        horizontalArrangement = Arrangement.Center
      ) {
        val wordLen = currentItem.targetWord.length
        for (i in 0 until wordLen) {
          val char = currentAssembly.getOrNull(i)?.second
          Surface(
            modifier = Modifier
              .padding(4.dp)
              .size(if (wordLen > 6) 42.dp else 52.dp),
            shape = RoundedCornerShape(12.dp),
            color = if (char != null) DomainLenguaje.copy(alpha = 0.15f) else MaterialTheme.colorScheme.surface,
            border = androidx.compose.foundation.BorderStroke(
              2.dp,
              if (char != null) DomainLenguaje else MaterialTheme.colorScheme.surfaceVariant
            )
          ) {
            Box(contentAlignment = Alignment.Center) {
              Text(
                text = char?.toString() ?: "",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Black,
                color = DomainLenguaje
              )
            }
          }
        }
      }

      Spacer(modifier = Modifier.weight(0.5f))

      // Available scrambled letter tiles
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .padding(horizontal = 16.dp),
        horizontalArrangement = Arrangement.Center
      ) {
        scrambledLetters.forEachIndexed { index, char ->
          val isUsed = usedLetterIndices.contains(index)
          Surface(
            modifier = Modifier
              .padding(4.dp)
              .size(54.dp)
              .clip(RoundedCornerShape(14.dp))
              .clickable(enabled = !isUsed) { onLetterTapped(index, char) }
              .testTag("btn_letter_$index"),
            shape = RoundedCornerShape(14.dp),
            color = if (isUsed) MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.3f) else MaterialTheme.colorScheme.surface,
            shadowElevation = if (isUsed) 0.dp else 3.dp
          ) {
            Box(contentAlignment = Alignment.Center) {
              Text(
                text = char.toString(),
                fontSize = 24.sp,
                fontWeight = FontWeight.Bold,
                color = if (isUsed) MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.3f) else MaterialTheme.colorScheme.onSurface
              )
            }
          }
        }
      }

      Spacer(modifier = Modifier.height(24.dp))

      // Action buttons: Undo, Clue, Pass
      Row(
        modifier = Modifier
          .fillMaxWidth(0.9f)
          .padding(bottom = 32.dp),
        horizontalArrangement = Arrangement.SpaceBetween
      ) {
        OutlinedButton(
          onClick = { onBackspace() },
          enabled = currentAssembly.isNotEmpty(),
          shape = RoundedCornerShape(12.dp),
          modifier = Modifier.testTag("btn_anagram_backspace")
        ) {
          Icon(imageVector = Icons.AutoMirrored.Filled.Backspace, contentDescription = "Borrar letra")
          Spacer(modifier = Modifier.width(4.dp))
          Text("Borrar")
        }

        OutlinedButton(
          onClick = { showClue = true },
          shape = RoundedCornerShape(12.dp),
          modifier = Modifier.testTag("btn_anagram_clue")
        ) {
          Icon(imageVector = Icons.Default.Lightbulb, contentDescription = "Pista", tint = DomainLenguaje)
          Spacer(modifier = Modifier.width(4.dp))
          Text("Pista")
        }

        OutlinedButton(
          onClick = { nextWord() },
          shape = RoundedCornerShape(12.dp),
          modifier = Modifier.testTag("btn_anagram_skip")
        ) {
          Icon(imageVector = Icons.Default.SkipNext, contentDescription = "Pasar palabra")
          Spacer(modifier = Modifier.width(4.dp))
          Text("Pasar palabra")
        }
      }
    }

    ScreenFlashOverlay(visible = showFlash, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
  }
}

private fun scramble(word: String): List<Char> {
  val chars = word.toList().shuffled()
  return if (chars.joinToString("") == word && word.length > 2) {
    chars.reversed()
  } else {
    chars
  }
}
