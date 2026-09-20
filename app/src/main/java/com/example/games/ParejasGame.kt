package com.example.games

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.HelpOutline
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.DomainType
import com.example.ui.components.GameHeader
import com.example.ui.theme.DomainMemoria
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay

data class MemoryCard(
  val id: Int,
  val pairKey: String,
  val symbol: String,
  val isFaceUp: Boolean = false,
  val isMatched: Boolean = false
)

@Composable
fun ParejasGame(
  level: Int,
  timed: Boolean,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val pairCount = when (level) {
    1 -> 4 // 8 cards (2x4)
    2 -> 6 // 12 cards (3x4)
    else -> 8 // 16 cards (4x4)
  }

  val symbols = listOf("🐶", "🐱", "🦊", "🐼", "🦁", "🐵", "🦄", "🦉", "🐸", "🐙", "🦋", "🐬")

  var cards by remember {
    mutableStateOf(generateCardDeck(pairCount, symbols))
  }

  var isMemorizingPhase by remember { mutableStateOf(true) }
  var memorizeSecondsLeft by remember { mutableStateOf(3) }
  var attemptsCount by remember { mutableStateOf(0) }
  var matchedPairs by remember { mutableStateOf(0) }
  var selectedFirstIndex by remember { mutableStateOf<Int?>(null) }
  var selectedSecondIndex by remember { mutableStateOf<Int?>(null) }
  var isProcessingTurn by remember { mutableStateOf(false) }

  // Initial memorization countdown
  LaunchedEffect(Unit) {
    // Show all cards face up for memorization
    cards = cards.map { it.copy(isFaceUp = true) }
    while (memorizeSecondsLeft > 0) {
      delay(1000)
      memorizeSecondsLeft--
    }
    // Flip all face down to begin playing
    cards = cards.map { it.copy(isFaceUp = false) }
    isMemorizingPhase = false
  }

  fun handleCardClick(index: Int) {
    if (isMemorizingPhase || isProcessingTurn) return
    val card = cards.getOrNull(index) ?: return
    if (card.isFaceUp || card.isMatched) return

    if (selectedFirstIndex == null) {
      selectedFirstIndex = index
      cards = cards.toMutableList().also { it[index] = card.copy(isFaceUp = true) }
    } else if (selectedSecondIndex == null && selectedFirstIndex != index) {
      selectedSecondIndex = index
      cards = cards.toMutableList().also { it[index] = card.copy(isFaceUp = true) }
      attemptsCount++
      isProcessingTurn = true
    }
  }

  // Check matching logic
  LaunchedEffect(selectedSecondIndex) {
    val first = selectedFirstIndex
    val second = selectedSecondIndex
    if (first != null && second != null) {
      delay(700)
      val card1 = cards[first]
      val card2 = cards[second]
      if (card1.pairKey == card2.pairKey) {
        // Matched!
        cards = cards.toMutableList().also {
          it[first] = card1.copy(isMatched = true, isFaceUp = true)
          it[second] = card2.copy(isMatched = true, isFaceUp = true)
        }
        matchedPairs++
        if (matchedPairs >= pairCount) {
          // Finished all pairs
          delay(400)
          val perfectAttempts = pairCount
          val efficiency = (perfectAttempts.toFloat() / attemptsCount.coerceAtLeast(perfectAttempts))
          val finalScore = (efficiency * 100).toInt().coerceIn(40, 100)
          onFinish(finalScore, matchedPairs, attemptsCount)
        }
      } else {
        // Not matched, flip down
        cards = cards.toMutableList().also {
          it[first] = card1.copy(isFaceUp = false)
          it[second] = card2.copy(isFaceUp = false)
        }
      }
      selectedFirstIndex = null
      selectedSecondIndex = null
      isProcessingTurn = false
    }
  }

  Column(
    modifier = Modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
  ) {
    GameHeader(
      title = "Parejas Ocultas",
      domain = DomainType.MEMORIA,
      currentRound = matchedPairs,
      totalRounds = pairCount,
      isTimed = timed,
      onQuit = onQuit
    )

    Spacer(modifier = Modifier.height(16.dp))

    // Status bar
    Row(
      modifier = Modifier
        .fillMaxWidth()
        .padding(horizontal = 20.dp),
      horizontalArrangement = Arrangement.SpaceBetween,
      verticalAlignment = Alignment.CenterVertically
    ) {
      if (isMemorizingPhase) {
        Surface(
          shape = RoundedCornerShape(12.dp),
          color = DomainMemoria.copy(alpha = 0.15f)
        ) {
          Text(
            text = "👀 Memoriza: ${memorizeSecondsLeft}s",
            modifier = Modifier.padding(horizontal = 14.dp, vertical = 6.dp),
            style = MaterialTheme.typography.labelLarge,
            fontWeight = FontWeight.Bold,
            color = DomainMemoria
          )
        }
      } else {
        Surface(
          shape = RoundedCornerShape(12.dp),
          color = MaterialTheme.colorScheme.surfaceVariant
        ) {
          Text(
            text = "Intentos: $attemptsCount",
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
            style = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.SemiBold
          )
        }
      }

      Surface(
        shape = RoundedCornerShape(12.dp),
        color = EmeraldAccent.copy(alpha = 0.12f)
      ) {
        Text(
          text = "Parejas: $matchedPairs/$pairCount",
          modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
          style = MaterialTheme.typography.labelMedium,
          fontWeight = FontWeight.Bold,
          color = EmeraldAccent
        )
      }
    }

    Spacer(modifier = Modifier.height(16.dp))

    // Grid of cards
    val columns = if (pairCount <= 4) 2 else if (pairCount <= 6) 3 else 4
    val rows = cards.chunked(columns)

    Box(
      modifier = Modifier
        .weight(1f)
        .fillMaxWidth()
        .padding(16.dp),
      contentAlignment = Alignment.Center
    ) {
      Column(
        verticalArrangement = Arrangement.spacedBy(10.dp),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        rows.forEachIndexed { rowIndex, rowCards ->
          Row(
            horizontalArrangement = Arrangement.spacedBy(10.dp),
            verticalAlignment = Alignment.CenterVertically
          ) {
            rowCards.forEachIndexed { colIndex, card ->
              val overallIndex = rowIndex * columns + colIndex
              CardView(
                card = card,
                onClick = { handleCardClick(overallIndex) },
                modifier = Modifier
                  .size(if (pairCount <= 4) 96.dp else if (pairCount <= 6) 78.dp else 68.dp)
                  .testTag("card_$overallIndex")
              )
            }
          }
        }
      }
    }
  }
}

@Composable
private fun CardView(
  card: MemoryCard,
  onClick: () -> Unit,
  modifier: Modifier = Modifier
) {
  val rotation by animateFloatAsState(
    targetValue = if (card.isFaceUp || card.isMatched) 180f else 0f,
    animationSpec = tween(durationMillis = 350)
  )

  Card(
    modifier = modifier
      .graphicsLayer {
        rotationY = rotation
        cameraDistance = 12f * density
      }
      .clickable(enabled = !card.isFaceUp && !card.isMatched) { onClick() },
    shape = RoundedCornerShape(16.dp),
    colors = CardDefaults.cardColors(
      containerColor = if (card.isMatched) {
        EmeraldAccent.copy(alpha = 0.18f)
      } else if (card.isFaceUp) {
        MaterialTheme.colorScheme.surface
      } else {
        DomainMemoria
      }
    ),
    elevation = CardDefaults.cardElevation(defaultElevation = if (card.isFaceUp) 2.dp else 4.dp)
  ) {
    Box(
      modifier = Modifier.fillMaxSize(),
      contentAlignment = Alignment.Center
    ) {
      if (rotation > 90f) {
        // Face up content (mirrored back so emoji isn't backwards)
        Box(
          modifier = Modifier.graphicsLayer { rotationY = 180f }
        ) {
          Text(
            text = card.symbol,
            fontSize = 32.sp
          )
        }
      } else {
        // Face down back
        Icon(
          imageVector = Icons.Default.HelpOutline,
          contentDescription = "Carta oculta",
          tint = Color.White.copy(alpha = 0.7f),
          modifier = Modifier.size(24.dp)
        )
      }
    }
  }
}

private fun generateCardDeck(pairCount: Int, symbols: List<String>): List<MemoryCard> {
  val chosenSymbols = symbols.shuffled().take(pairCount)
  val deck = mutableListOf<MemoryCard>()
  var id = 0
  chosenSymbols.forEach { sym ->
    val pairKey = sym
    deck.add(MemoryCard(id = id++, pairKey = pairKey, symbol = sym))
    deck.add(MemoryCard(id = id++, pairKey = pairKey, symbol = sym))
  }
  return deck.shuffled()
}
