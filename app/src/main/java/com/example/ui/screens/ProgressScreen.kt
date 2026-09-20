package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.ui.components.DomainChip
import com.example.ui.theme.*
import com.example.viewmodel.NeuroVidaViewModel
import java.text.SimpleDateFormat
import java.util.*

@Composable
fun ProgressScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val domainMastery by viewModel.domainMasteryInfo.collectAsState()
  val history by viewModel.gameHistory.collectAsState()

  val dateFormatter = remember { SimpleDateFormat("dd MMM, HH:mm", Locale.getDefault()) }

  LazyColumn(
    modifier = modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
      .padding(horizontal = 20.dp),
    contentPadding = PaddingValues(top = 16.dp, bottom = 96.dp),
    verticalArrangement = Arrangement.spacedBy(20.dp)
  ) {
    // Header
    item {
      Column {
        Text(
          text = "Tu Progreso Cognitivo",
          style = MaterialTheme.typography.headlineMedium,
          fontWeight = FontWeight.Black,
          color = MaterialTheme.colorScheme.onBackground
        )
        Text(
          text = "Evolución integral en las 6 áreas cognitivas",
          style = MaterialTheme.typography.bodyMedium,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )
      }
    }

    // Performance Trend Graph (Room DB backed)
    item {
      com.example.ui.components.ProgressTrendChart(
        history = history
      )
    }

    // Cognitive Domains Competence Cards
    item {
      Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
      ) {
        Column(
          modifier = Modifier.padding(20.dp),
          verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
          ) {
            Text(
              text = "Maestría por Dominio",
              style = MaterialTheme.typography.titleMedium,
              fontWeight = FontWeight.Bold
            )
            Text(
              text = "XP sin límite",
              style = MaterialTheme.typography.labelSmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
          }

          // A diferencia del nivel de juego (tope visible en 5), esto nunca deja
          // de crecer: jugar CUALQUIER juego del dominio suma, así que un dominio
          // con todos sus juegos en Experto sigue dando sensación de avance.
          domainMastery.forEach { info ->
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
              Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
              ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                  Box(
                    modifier = Modifier
                      .size(10.dp)
                      .clip(CircleShape)
                      .background(info.domain.color)
                  )
                  Spacer(modifier = Modifier.width(8.dp))
                  Text(
                    text = info.domain.displayName,
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface
                  )
                }

                Text(
                  text = "${info.tier.icon} ${info.tier.tierName} · ${info.xpLabel}",
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.SemiBold,
                  color = info.domain.color
                )
              }

              LinearProgressIndicator(
                progress = { info.progressInTier.coerceIn(0.03f, 1f) },
                modifier = Modifier
                  .fillMaxWidth()
                  .height(8.dp)
                  .clip(RoundedCornerShape(4.dp)),
                color = info.domain.color,
                trackColor = MaterialTheme.colorScheme.surfaceVariant
              )
            }
          }
        }
      }
    }

    // Recent History
    item {
      Text(
        text = "Historial Reciente",
        style = MaterialTheme.typography.titleLarge,
        fontWeight = FontWeight.Bold,
        modifier = Modifier.padding(top = 8.dp)
      )
    }

    items(history.take(8)) { item ->
      val game = GameRegistry.getById(item.gameId)
      Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
      ) {
        Row(
          modifier = Modifier
            .fillMaxWidth()
            .padding(14.dp),
          verticalAlignment = Alignment.CenterVertically
        ) {
          Text(text = game?.iconEmoji ?: "🧠", fontSize = 24.sp)
          Spacer(modifier = Modifier.width(12.dp))

          Column(modifier = Modifier.weight(1f)) {
            Text(
              text = game?.title ?: "Juego",
              style = MaterialTheme.typography.titleSmall,
              fontWeight = FontWeight.Bold
            )
            Text(
              text = dateFormatter.format(Date(item.timestamp)),
              style = MaterialTheme.typography.labelSmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
          }

          Surface(
            shape = RoundedCornerShape(10.dp),
            color = (game?.domain?.color ?: TealPrimary).copy(alpha = 0.12f)
          ) {
            Text(
              text = "${item.score} pts",
              modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
              style = MaterialTheme.typography.labelMedium,
              fontWeight = FontWeight.ExtraBold,
              color = game?.domain?.color ?: TealPrimary
            )
          }
        }
      }
    }
  }
}
