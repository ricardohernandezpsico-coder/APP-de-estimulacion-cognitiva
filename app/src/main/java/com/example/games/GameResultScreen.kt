package com.example.games

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.scaleIn
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.LevelTier
import com.example.ui.components.DomainChip
import com.example.ui.theme.*

@Composable
fun GameResultScreen(
  result: GamePlayResult,
  didLevelUp: Boolean,
  isDailyFlow: Boolean,
  dailyCompletedCount: Int,
  dailyTotalCount: Int = 3,
  onPlayAgain: () -> Unit,
  onContinue: () -> Unit,
  modifier: Modifier = Modifier
) {
  val gameDef = GameRegistry.getById(result.gameId)
  val domainColor = gameDef?.domain?.color ?: TealPrimary
  val levelTier = LevelTier.fromLevel(result.level)
  val stars = when {
    result.score >= 90 -> 3
    result.score >= 65 -> 2
    result.score >= 40 -> 1
    else -> 0
  }

  Box(
    modifier = modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
  ) {
    Column(
      modifier = Modifier
        .fillMaxSize()
        .verticalScroll(rememberScrollState())
        .padding(24.dp)
        .padding(bottom = 32.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      Spacer(modifier = Modifier.height(20.dp))

      // Header badge with game icon
      Surface(
        modifier = Modifier.size(80.dp),
        shape = CircleShape,
        color = domainColor.copy(alpha = 0.15f),
        shadowElevation = 4.dp
      ) {
        Box(contentAlignment = Alignment.Center) {
          Text(text = gameDef?.iconEmoji ?: "🧠", fontSize = 38.sp)
        }
      }

      Spacer(modifier = Modifier.height(16.dp))

      Text(
        text = gameDef?.title ?: "Juego Completado",
        style = MaterialTheme.typography.headlineMedium,
        fontWeight = FontWeight.ExtraBold,
        color = MaterialTheme.colorScheme.onBackground
      )

      if (gameDef != null) {
        DomainChip(domain = gameDef.domain, modifier = Modifier.padding(top = 6.dp))
      }

      Spacer(modifier = Modifier.height(24.dp))

      // Main score card
      Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(24.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        elevation = CardDefaults.cardElevation(defaultElevation = 3.dp)
      ) {
        Column(
          modifier = Modifier
            .fillMaxWidth()
            .padding(24.dp),
          horizontalAlignment = Alignment.CenterHorizontally
        ) {
          // Stars row
          Row(
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = Alignment.CenterVertically
          ) {
            for (i in 1..3) {
              Icon(
                imageVector = Icons.Default.Star,
                contentDescription = null,
                modifier = Modifier.size(36.dp),
                tint = if (i <= stars) DomainAtencion else MaterialTheme.colorScheme.surfaceVariant
              )
            }
          }

          Spacer(modifier = Modifier.height(16.dp))

          Text(
            text = "${result.score}",
            style = MaterialTheme.typography.displayLarge,
            fontWeight = FontWeight.Black,
            color = domainColor
          )
          Text(
            text = "Puntos obtenidos",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )

          Spacer(modifier = Modifier.height(20.dp))
          HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
          Spacer(modifier = Modifier.height(16.dp))

          // Accuracy and level stats
          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly
          ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
              Text(
                text = "${result.correctAnswers}/${result.totalTrials}",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onSurface
              )
              Text(
                text = "Aciertos",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
              )
            }

            Column(horizontalAlignment = Alignment.CenterHorizontally) {
              Text(
                text = "Nivel ${result.level}",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onSurface
              )
              Text(
                text = levelTier.tierName,
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
              )
            }

            Column(horizontalAlignment = Alignment.CenterHorizontally) {
              Text(
                text = if (result.timed) "Reto" else "Precisión",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onSurface
              )
              Text(
                text = "Modalidad",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
              )
            }
          }
        }
      }

      // Level up celebration banner if achieved
      if (didLevelUp) {
        Spacer(modifier = Modifier.height(16.dp))
        Surface(
          modifier = Modifier.fillMaxWidth(),
          shape = RoundedCornerShape(16.dp),
          color = EmeraldAccent.copy(alpha = 0.12f),
          border = androidx.compose.foundation.BorderStroke(1.dp, EmeraldAccent.copy(alpha = 0.4f))
        ) {
          Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
          ) {
            Surface(
              shape = CircleShape,
              color = EmeraldAccent,
              modifier = Modifier.size(36.dp)
            ) {
              Box(contentAlignment = Alignment.Center) {
                Icon(
                  imageVector = Icons.Default.EmojiEvents,
                  contentDescription = null,
                  tint = Color.White,
                  modifier = Modifier.size(20.dp)
                )
              }
            }
            Spacer(modifier = Modifier.width(12.dp))
            Column {
              Text(
                text = "¡Subes de nivel!",
                style = MaterialTheme.typography.titleSmall,
                fontWeight = FontWeight.Bold,
                color = EmeraldAccent
              )
              Text(
                text = "Has alcanzado el Nivel ${(result.level + 1).coerceAtMost(5)} en ${gameDef?.title}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurface
              )
            }
          }
        }
      }

      // Daily session indicator if part of daily flow
      if (isDailyFlow) {
        Spacer(modifier = Modifier.height(16.dp))
        Surface(
          modifier = Modifier.fillMaxWidth(),
          shape = RoundedCornerShape(16.dp),
          color = TealPrimary.copy(alpha = 0.08f)
        ) {
          Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
          ) {
            Column {
              Text(
                text = "Sesión Diaria en progreso",
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.Bold,
                color = TealPrimary
              )
              Text(
                text = "$dailyCompletedCount de $dailyTotalCount juegos completados hoy",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
              )
            }
            Text(
              text = "$dailyCompletedCount/$dailyTotalCount",
              style = MaterialTheme.typography.titleMedium,
              fontWeight = FontWeight.ExtraBold,
              color = TealPrimary
            )
          }
        }
      }

      // Cognitive insight note
      Spacer(modifier = Modifier.height(16.dp))
      Surface(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
      ) {
        Column(modifier = Modifier.padding(16.dp)) {
          Text(
            text = "💡 Qué significa tu resultado",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
            color = MaterialTheme.colorScheme.onSurface
          )
          Spacer(modifier = Modifier.height(4.dp))
          Text(
            text = when {
              result.score >= 85 -> "Excelente rendimiento. Tu cerebro ha mostrado alta eficiencia en procesamiento y exactitud."
              result.score >= 60 -> "Buen entrenamiento. La práctica continua fortalece las conexiones sinápticas de este dominio."
              else -> "¡Gran esfuerzo! El cerebro aprende y se activa precisamente cuando se enfrenta a desafíos que exigen adaptación."
            },
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            lineHeight = 18.sp
          )
        }
      }

      Spacer(modifier = Modifier.height(28.dp))

      // Action buttons
      Button(
        onClick = onContinue,
        modifier = Modifier
          .fillMaxWidth()
          .height(52.dp)
          .testTag("btn_result_continue"),
        shape = RoundedCornerShape(14.dp),
        colors = ButtonDefaults.buttonColors(containerColor = domainColor)
      ) {
        Text(
          text = if (isDailyFlow && dailyCompletedCount < dailyTotalCount) "Siguiente: Juego ${dailyCompletedCount + 1}" else "Continuar",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )
        Spacer(modifier = Modifier.width(8.dp))
        Icon(imageVector = Icons.AutoMirrored.Filled.ArrowForward, contentDescription = null)
      }

      Spacer(modifier = Modifier.height(12.dp))

      OutlinedButton(
        onClick = onPlayAgain,
        modifier = Modifier
          .fillMaxWidth()
          .height(50.dp)
          .testTag("btn_result_replay"),
        shape = RoundedCornerShape(14.dp)
      ) {
        Icon(imageVector = Icons.Default.Refresh, contentDescription = null)
        Spacer(modifier = Modifier.width(8.dp))
        Text(
          text = "Jugar de nuevo",
          style = MaterialTheme.typography.titleSmall,
          fontWeight = FontWeight.SemiBold
        )
      }
    }
  }
}
