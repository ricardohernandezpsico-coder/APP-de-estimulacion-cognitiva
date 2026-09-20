package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.model.LevelTier
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
  val domainStats by viewModel.domainStats.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val achievements = remember(history) { viewModel.getAchievements() }
  val unlockedCount = achievements.count { it.isUnlocked }

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
          Text(
            text = "Competencia por Área",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold
          )

          domainStats.forEach { stat ->
            val tier = LevelTier.fromLevel(stat.competenceLevel)
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
                      .background(stat.domain.color)
                  )
                  Spacer(modifier = Modifier.width(8.dp))
                  Text(
                    text = stat.domain.displayName,
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface
                  )
                }

                Text(
                  text = "Nivel ${stat.competenceLevel} (${tier.tierName})",
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.SemiBold,
                  color = stat.domain.color
                )
              }

              // Visual proficiency bar
              val progress = (stat.competenceLevel / 5f).coerceIn(0.1f, 1f)
              LinearProgressIndicator(
                progress = { progress },
                modifier = Modifier
                  .fillMaxWidth()
                  .height(8.dp)
                  .clip(RoundedCornerShape(4.dp)),
                color = stat.domain.color,
                trackColor = MaterialTheme.colorScheme.surfaceVariant
              )
            }
          }
        }
      }
    }

    // Achievements Section ("Logros")
    item {
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        Text(
          text = "Logros",
          style = MaterialTheme.typography.titleLarge,
          fontWeight = FontWeight.Bold
        )
        Surface(
          shape = RoundedCornerShape(12.dp),
          color = Color(0xFFFEF3C7)
        ) {
          Text(
            text = "$unlockedCount de 12",
            style = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.Bold,
            color = Color(0xFFB45309),
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
          )
        }
      }
    }

    // Grid of achievements
    val chunkedAchievements = achievements.chunked(2)
    items(chunkedAchievements) { rowItems ->
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(12.dp)
      ) {
        rowItems.forEach { item ->
          Card(
            modifier = Modifier
              .weight(1f)
              .heightIn(min = 120.dp),
            shape = RoundedCornerShape(18.dp),
            colors = CardDefaults.cardColors(
              containerColor = if (item.isUnlocked) Color(0xFFFFFBEB) else MaterialTheme.colorScheme.surface
            ),
            border = androidx.compose.foundation.BorderStroke(
              1.dp,
              if (item.isUnlocked) Color(0xFFFBBF24) else MaterialTheme.colorScheme.surfaceVariant
            )
          ) {
            Column(
              modifier = Modifier.padding(12.dp),
              verticalArrangement = Arrangement.SpaceBetween
            ) {
              Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
              ) {
                Text(text = item.iconEmoji, fontSize = 24.sp)
                if (item.isUnlocked) {
                  Icon(
                    imageVector = Icons.Default.CheckCircle,
                    contentDescription = "Desbloqueado",
                    tint = Color(0xFFD97706),
                    modifier = Modifier.size(18.dp)
                  )
                } else {
                  Text(text = "🔒", fontSize = 16.sp)
                }
              }

              Spacer(modifier = Modifier.height(8.dp))

              Text(
                text = item.title,
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.Bold,
                color = if (item.isUnlocked) Color(0xFF92400E) else MaterialTheme.colorScheme.onSurface
              )

              Text(
                text = item.description,
                style = MaterialTheme.typography.bodySmall,
                color = if (item.isUnlocked) Color(0xFFB45309) else MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
              )
            }
          }
        }
        if (rowItems.size == 1) {
          Spacer(modifier = Modifier.weight(1f))
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
