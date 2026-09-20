package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.NotificationsActive
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.FitnessCenter
import androidx.compose.material.icons.outlined.Schedule
import androidx.compose.material.icons.outlined.Star
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.ui.components.CircularProgressRing
import com.example.ui.components.DomainChip
import com.example.ui.components.Sparkline
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.i18n.strings
import com.example.ui.theme.*
import com.example.viewmodel.NeuroVidaViewModel

@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  onNavigateToGames: () -> Unit,
  modifier: Modifier = Modifier
) {
  val userSettings by viewModel.userSettings.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  val last7Days by viewModel.last7DaysActivity.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val sessionsSparkline by viewModel.sessionsSparkline.collectAsState()
  val scoresSparkline by viewModel.scoresSparkline.collectAsState()
  val weeklyChallenges by viewModel.weeklyChallengeProgress.collectAsState()

  LazyColumn(
    modifier = modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
      .padding(horizontal = 20.dp),
    contentPadding = PaddingValues(top = 16.dp, bottom = 96.dp),
    verticalArrangement = Arrangement.spacedBy(18.dp)
  ) {
    // Top greeting header
    item {
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Surface(
            shape = CircleShape,
            color = TealPrimary.copy(alpha = 0.12f),
            modifier = Modifier.size(50.dp)
          ) {
            Box(contentAlignment = Alignment.Center) {
              Text(text = userSettings.avatar, fontSize = 24.sp)
            }
          }

          Spacer(modifier = Modifier.width(12.dp))

          Column {
            Text(
              text = strings.greeting(userSettings.name),
              style = MaterialTheme.typography.titleLarge,
              fontWeight = FontWeight.Black,
              color = MaterialTheme.colorScheme.onBackground
            )
            Row(verticalAlignment = Alignment.CenterVertically) {
              Surface(
                shape = RoundedCornerShape(6.dp),
                color = TealPrimary.copy(alpha = 0.12f)
              ) {
                Text(
                  text = userSettings.difficultyMode.label,
                  style = MaterialTheme.typography.labelSmall,
                  color = TealPrimary,
                  fontWeight = FontWeight.Bold,
                  modifier = Modifier.padding(horizontal = 6.dp, vertical = 2.dp)
                )
              }
            }
          }
        }

        // Streak badge pill
        Surface(
          shape = RoundedCornerShape(20.dp),
          color = if (streak > 0) Color(0xFFFEF3C7) else MaterialTheme.colorScheme.surfaceVariant
        ) {
          Row(
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
            verticalAlignment = Alignment.CenterVertically
          ) {
            Icon(
              imageVector = Icons.Default.Whatshot,
              contentDescription = strings.streakLabel,
              tint = if (streak > 0) Color(0xFFD97706) else MaterialTheme.colorScheme.onSurfaceVariant,
              modifier = Modifier.size(20.dp)
            )
            Spacer(modifier = Modifier.width(4.dp))
            Text(
              text = "$streak ${if (streak == 1) strings.daySingle else strings.dayPlural}",
              style = MaterialTheme.typography.labelLarge,
              fontWeight = FontWeight.Bold,
              color = if (streak > 0) Color(0xFFB45309) else MaterialTheme.colorScheme.onSurfaceVariant
            )
          }
        }
      }
    }

    // "Tu sesión de hoy" Main Card
    item {
      val lang = LocalAppLanguage.current
      Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
      ) {
        Column(
          modifier = Modifier
            .fillMaxWidth()
            .padding(20.dp)
        ) {
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
          ) {
            Column(modifier = Modifier.weight(1f)) {
              Text(
                text = strings.dailySessionTitle,
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onSurface
              )
              Text(
                text = when (dailySession.completedCount) {
                  0 -> strings.dailySessionDesc
                  in 1..2 -> "${dailySession.completedCount}/3"
                  else -> strings.sessionCompletedTitle
                },
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(top = 2.dp)
              )
            }

            CircularProgressRing(
              progress = dailySession.completedCount / 3f,
              text = "${dailySession.completedCount}/3",
              color = TealPrimary,
              size = 58.dp
            )
          }

          Spacer(modifier = Modifier.height(16.dp))

          // 3 game chips for today's queue
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
          ) {
            dailySession.gameIds.forEachIndexed { idx, gameId ->
              val def = GameRegistry.getById(gameId)
              val isDone = idx < dailySession.completedCount
              Surface(
                modifier = Modifier.weight(1f),
                shape = RoundedCornerShape(14.dp),
                color = if (isDone) EmeraldAccent.copy(alpha = 0.12f) else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.6f)
              ) {
                Column(
                  modifier = Modifier.padding(8.dp),
                  horizontalAlignment = Alignment.CenterHorizontally
                ) {
                  Text(text = def?.iconEmoji ?: "🧠", fontSize = 20.sp)
                  Spacer(modifier = Modifier.height(2.dp))
                  Text(
                    text = if (def != null) getGameTitle(def.id, lang, def.title) else "Juego",
                    style = MaterialTheme.typography.labelSmall,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    color = if (isDone) EmeraldAccent else MaterialTheme.colorScheme.onSurface
                  )
                }
              }
            }
          }

          Spacer(modifier = Modifier.height(18.dp))

          Button(
            onClick = { viewModel.startDailySession() },
            modifier = Modifier
              .fillMaxWidth()
              .height(50.dp)
              .testTag("btn_start_daily_session"),
            shape = RoundedCornerShape(14.dp),
            colors = ButtonDefaults.buttonColors(containerColor = TealPrimary)
          ) {
            Icon(
              imageVector = Icons.Default.PlayArrow,
              contentDescription = null,
              modifier = Modifier.size(20.dp)
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text(
              text = when {
                dailySession.completedCount == 0 -> strings.startDailySession
                dailySession.completedCount < 3 -> strings.continueDailySession
                else -> strings.startDailySession
              },
              style = MaterialTheme.typography.titleSmall,
              fontWeight = FontWeight.Bold
            )
          }

          if (userSettings.notificationsEnabled) {
            Spacer(modifier = Modifier.height(10.dp))
            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.Center,
              verticalAlignment = Alignment.CenterVertically
            ) {
              Icon(
                imageVector = Icons.Default.NotificationsActive,
                contentDescription = null,
                modifier = Modifier.size(14.dp),
                tint = TealPrimary
              )
              Spacer(modifier = Modifier.width(6.dp))
              Text(
                text = "Recordatorio activo para las %02d:%02d".format(userSettings.reminderHour, userSettings.reminderMinute),
                style = MaterialTheme.typography.labelSmall,
                color = TealPrimary,
                fontWeight = FontWeight.Medium
              )
            }
          }
        }
      }
    }

    // Desafíos de la semana (etapa 4 de gamificación): fijos, no aleatorios,
    // progreso calculado en vivo desde el historial — dan un motivo concreto
    // para variar (jugar otro dominio, probar Reto) más allá de la racha diaria.
    item {
      Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
      ) {
        Column(modifier = Modifier.padding(16.dp)) {
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
          ) {
            Text(
              text = "Desafíos de la semana",
              style = MaterialTheme.typography.titleSmall,
              fontWeight = FontWeight.Bold
            )
            val completedCount = weeklyChallenges.count { it.isComplete }
            Surface(
              shape = RoundedCornerShape(10.dp),
              color = if (completedCount == weeklyChallenges.size && weeklyChallenges.isNotEmpty())
                EmeraldAccent.copy(alpha = 0.15f) else MaterialTheme.colorScheme.surfaceVariant
            ) {
              Text(
                text = "$completedCount/${weeklyChallenges.size}",
                style = MaterialTheme.typography.labelSmall,
                fontWeight = FontWeight.Bold,
                color = if (completedCount == weeklyChallenges.size && weeklyChallenges.isNotEmpty())
                  EmeraldAccent else MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp)
              )
            }
          }

          Spacer(modifier = Modifier.height(12.dp))

          weeklyChallenges.forEachIndexed { idx, wc ->
            if (idx > 0) Spacer(modifier = Modifier.height(10.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
              Text(text = wc.def.iconEmoji, fontSize = 18.sp)
              Spacer(modifier = Modifier.width(10.dp))
              Column(modifier = Modifier.weight(1f)) {
                Text(
                  text = wc.def.title,
                  style = MaterialTheme.typography.labelMedium,
                  fontWeight = FontWeight.SemiBold,
                  color = if (wc.isComplete) EmeraldAccent else MaterialTheme.colorScheme.onSurface
                )
                Spacer(modifier = Modifier.height(4.dp))
                LinearProgressIndicator(
                  progress = { (wc.progress.toFloat() / wc.def.target).coerceIn(0f, 1f) },
                  modifier = Modifier
                    .fillMaxWidth()
                    .height(6.dp)
                    .clip(RoundedCornerShape(3.dp)),
                  color = if (wc.isComplete) EmeraldAccent else TealPrimary,
                  trackColor = MaterialTheme.colorScheme.surfaceVariant
                )
              }
              Spacer(modifier = Modifier.width(10.dp))
              if (wc.isComplete) {
                Icon(
                  imageVector = Icons.Outlined.CheckCircle,
                  contentDescription = "Completado",
                  tint = EmeraldAccent,
                  modifier = Modifier.size(18.dp)
                )
              } else {
                Text(
                  text = "${wc.progress}/${wc.def.target}",
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.Bold,
                  color = MaterialTheme.colorScheme.onSurfaceVariant
                )
              }
            }
          }
        }
      }
    }

    // 7 Days Streak Row
    item {
      Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
      ) {
        Column(modifier = Modifier.padding(16.dp)) {
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
          ) {
            Text(
              text = "Actividad últimos 7 días",
              style = MaterialTheme.typography.titleSmall,
              fontWeight = FontWeight.Bold
            )
            Text(
              text = if (streak >= 1) "🔥 Racha activa" else "Comienza hoy tu racha",
              style = MaterialTheme.typography.labelSmall,
              color = if (streak >= 1) Color(0xFFD97706) else MaterialTheme.colorScheme.onSurfaceVariant
            )
          }

          Spacer(modifier = Modifier.height(12.dp))

          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
          ) {
            last7Days.forEach { (dayLetter, played) ->
              Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(4.dp)
              ) {
                Box(
                  modifier = Modifier
                    .size(36.dp)
                    .clip(CircleShape)
                    .background(if (played) TealPrimary else MaterialTheme.colorScheme.surfaceVariant),
                  contentAlignment = Alignment.Center
                ) {
                  if (played) {
                    Text(text = "✓", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 14.sp)
                  }
                }
                Text(
                  text = dayLetter,
                  style = MaterialTheme.typography.labelSmall,
                  color = MaterialTheme.colorScheme.onSurfaceVariant
                )
              }
            }
          }
        }
      }
    }

    // 3 Metric Cards with Sparklines & Distinct Accent Colors
    item {
      Text(
        text = "Resumen de rendimiento",
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.Bold,
        color = MaterialTheme.colorScheme.onBackground
      )
    }

    item {
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(10.dp)
      ) {
        // Metric 1: Sesiones (Teal)
        Card(
          modifier = Modifier.weight(1f),
          shape = RoundedCornerShape(18.dp),
          colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
          Column(modifier = Modifier.padding(14.dp)) {
            Icon(
              imageVector = Icons.Outlined.FitnessCenter,
              contentDescription = null,
              tint = TealPrimary,
              modifier = Modifier.size(22.dp)
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
              text = "${history.size}",
              style = MaterialTheme.typography.titleLarge,
              fontWeight = FontWeight.Black,
              color = MaterialTheme.colorScheme.onSurface
            )
            Text(
              text = "Sesiones",
              style = MaterialTheme.typography.labelSmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(modifier = Modifier.height(8.dp))
            Sparkline(data = sessionsSparkline, lineColor = TealPrimary)
          }
        }

        // Metric 2: Minutos (Ámbar)
        Card(
          modifier = Modifier.weight(1f),
          shape = RoundedCornerShape(18.dp),
          colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
          Column(modifier = Modifier.padding(14.dp)) {
            Icon(
              imageVector = Icons.Outlined.Schedule,
              contentDescription = null,
              tint = Color(0xFFD97706),
              modifier = Modifier.size(22.dp)
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
              text = "${history.size * 4}",
              style = MaterialTheme.typography.titleLarge,
              fontWeight = FontWeight.Black,
              color = MaterialTheme.colorScheme.onSurface
            )
            Text(
              text = "Minutos aprox.",
              style = MaterialTheme.typography.labelSmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(modifier = Modifier.height(8.dp))
            // Uniform sparkline
            Sparkline(data = sessionsSparkline, lineColor = Color(0xFFD97706))
          }
        }

        // Metric 3: Última puntuación (Esmeralda)
        Card(
          modifier = Modifier.weight(1f),
          shape = RoundedCornerShape(18.dp),
          colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
          val lastScore = history.firstOrNull()?.score ?: 0
          Column(modifier = Modifier.padding(14.dp)) {
            Icon(
              imageVector = Icons.Outlined.Star,
              contentDescription = null,
              tint = EmeraldAccent,
              modifier = Modifier.size(22.dp)
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
              text = "$lastScore",
              style = MaterialTheme.typography.titleLarge,
              fontWeight = FontWeight.Black,
              color = MaterialTheme.colorScheme.onSurface
            )
            Text(
              text = "Última puntuación",
              style = MaterialTheme.typography.labelSmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant,
              maxLines = 1
            )
            Spacer(modifier = Modifier.height(8.dp))
            Sparkline(data = scoresSparkline, lineColor = EmeraldAccent)
          }
        }
      }
    }

    // Quick Games Carousel
    item {
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        Text(
          text = "Explorar juegos",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )
        TextButton(onClick = onNavigateToGames) {
          Text("Ver todos", color = TealPrimary, fontWeight = FontWeight.SemiBold)
          Spacer(modifier = Modifier.width(4.dp))
          Icon(Icons.AutoMirrored.Filled.ArrowForward, contentDescription = null, modifier = Modifier.size(16.dp), tint = TealPrimary)
        }
      }
    }

    item {
      LazyRow(
        horizontalArrangement = Arrangement.spacedBy(14.dp)
      ) {
        items(GameRegistry.allGames.take(4)) { game ->
          Card(
            modifier = Modifier
              .width(200.dp)
              .clip(RoundedCornerShape(20.dp))
              .clickable { viewModel.launchGame(game.id) }
              .testTag("quick_game_${game.id}"),
            shape = RoundedCornerShape(20.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
          ) {
            Column(modifier = Modifier.padding(16.dp)) {
              Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
              ) {
                Text(text = game.iconEmoji, fontSize = 28.sp)
                DomainChip(domain = game.domain)
              }
              Spacer(modifier = Modifier.height(12.dp))
              Text(
                text = game.title,
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.Bold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis
              )
              Text(
                text = game.subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                modifier = Modifier.padding(top = 2.dp)
              )
            }
          }
        }
      }
    }
  }
}
