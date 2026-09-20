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
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Speed
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material.icons.outlined.Check
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
import androidx.compose.ui.window.Dialog
import com.example.model.DomainType
import com.example.model.GameDefinition
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.model.LevelTier
import com.example.ui.components.DomainChip
import com.example.ui.theme.*
import com.example.viewmodel.NeuroVidaViewModel

@Composable
fun GamesLibraryScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val gameLevels by viewModel.gameLevels.collectAsState()
  val gameRanks by viewModel.gameRanks.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val userSettings by viewModel.userSettings.collectAsState()

  var selectedDomainFilter by remember { mutableStateOf<DomainType?>(null) }
  var gameToIntro by remember { mutableStateOf<GameDefinition?>(null) }

  val filteredGames = remember(selectedDomainFilter) {
    if (selectedDomainFilter == null) {
      GameRegistry.allGames
    } else {
      GameRegistry.allGames.filter { it.domain == selectedDomainFilter }
    }
  }

  Box(
    modifier = modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
  ) {
    LazyColumn(
      modifier = Modifier
        .fillMaxSize()
        .padding(horizontal = 20.dp),
      contentPadding = PaddingValues(top = 16.dp, bottom = 96.dp),
      verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
      item {
        Column {
          Text(
            text = "Biblioteca de Juegos",
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Black,
            color = MaterialTheme.colorScheme.onBackground
          )
          Text(
            text = "9 entrenamientos en 6 dominios cognitivos",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }
      }

      // Domain Filter Chips
      item {
        LazyRow(
          horizontalArrangement = Arrangement.spacedBy(8.dp),
          modifier = Modifier.fillMaxWidth()
        ) {
          item {
            FilterChip(
              selected = selectedDomainFilter == null,
              onClick = { selectedDomainFilter = null },
              label = { Text("Todos") },
              shape = RoundedCornerShape(14.dp),
              colors = FilterChipDefaults.filterChipColors(
                selectedContainerColor = TealPrimary,
                selectedLabelColor = Color.White
              ),
              modifier = Modifier.testTag("filter_all")
            )
          }
          items(DomainType.values()) { domain ->
            val isSelected = selectedDomainFilter == domain
            FilterChip(
              selected = isSelected,
              onClick = { selectedDomainFilter = if (isSelected) null else domain },
              label = { Text(domain.displayName) },
              shape = RoundedCornerShape(14.dp),
              colors = FilterChipDefaults.filterChipColors(
                selectedContainerColor = domain.color,
                selectedLabelColor = Color.White
              ),
              modifier = Modifier.testTag("filter_${domain.name.lowercase()}")
            )
          }
        }
      }

      // Games List
      items(filteredGames) { game ->
        val level = gameLevels[game.id] ?: 1
        val levelTier = LevelTier.fromLevel(level)
        val rankInfo = gameRanks.find { it.gameId == game.id } ?: GameRankInfo(game.id, 0)
        val bestScore = history.filter { it.gameId == game.id }.maxOfOrNull { it.score }

        Card(
          modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(22.dp))
            .clickable { gameToIntro = game }
            .testTag("game_card_${game.id}"),
          shape = RoundedCornerShape(22.dp),
          colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
          elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
        ) {
          Row(
            modifier = Modifier
              .fillMaxWidth()
              .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
          ) {
            // Icon
            Surface(
              modifier = Modifier.size(60.dp),
              shape = RoundedCornerShape(18.dp),
              color = game.domain.color.copy(alpha = 0.14f)
            ) {
              Box(contentAlignment = Alignment.Center) {
                Text(text = game.iconEmoji, fontSize = 28.sp)
              }
            }

            Spacer(modifier = Modifier.width(16.dp))

            Column(modifier = Modifier.weight(1f)) {
              Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp)
              ) {
                Text(
                  text = game.title,
                  style = MaterialTheme.typography.titleMedium,
                  fontWeight = FontWeight.Bold,
                  color = MaterialTheme.colorScheme.onSurface
                )
              }

              Text(
                text = game.subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                modifier = Modifier.padding(top = 2.dp)
              )

              Spacer(modifier = Modifier.height(8.dp))

              Row(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalAlignment = Alignment.CenterVertically
              ) {
                Surface(
                  shape = RoundedCornerShape(8.dp),
                  color = game.domain.color.copy(alpha = 0.12f)
                ) {
                  Text(
                    text = "Nivel $level · ${levelTier.tierName}",
                    style = MaterialTheme.typography.labelSmall,
                    fontWeight = FontWeight.Bold,
                    color = game.domain.color,
                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
                  )
                }

                Surface(
                  shape = RoundedCornerShape(8.dp),
                  color = rankInfo.tier.color.copy(alpha = 0.14f)
                ) {
                  Text(
                    text = "${rankInfo.tier.icon} ${rankInfo.label}",
                    style = MaterialTheme.typography.labelSmall,
                    fontWeight = FontWeight.Bold,
                    color = rankInfo.tier.color,
                    modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
                  )
                }
              }

              if (bestScore != null) {
                Text(
                  text = "Mejor: $bestScore",
                  style = MaterialTheme.typography.labelSmall,
                  color = MaterialTheme.colorScheme.onSurfaceVariant,
                  modifier = Modifier.padding(top = 4.dp)
                )
              }
            }

            IconButton(
              onClick = { gameToIntro = game },
              modifier = Modifier.testTag("btn_play_${game.id}")
            ) {
              Icon(
                imageVector = Icons.Default.PlayArrow,
                contentDescription = "Jugar ${game.title}",
                tint = game.domain.color,
                modifier = Modifier.size(28.dp)
              )
            }
          }
        }
      }
    }

    // Game Intro Dialog
    gameToIntro?.let { game ->
      // OJO: antes esto leía gameLevels[game.id] directo, que es el nivel adaptativo
      // guardado sin importar el modo de dificultad elegido en Ajustes — significaba
      // que "Avanzado" (nivel 5 fijo) nunca se aplicaba de verdad al jugar. Ahora usa
      // el nivel efectivo del ViewModel (respeta Principiante/Intermedio/Avanzado/
      // Personalizada) como base, y Suave/Desafío siguen ajustando ±1 sobre esa base.
      val currentLevel = viewModel.getEffectiveLevelForGame(game.id)
      var levelOffset by remember { mutableStateOf(0) } // -1 (Suave), 0 (Equilibrado), +1 (Desafío)
      var isTimedMode by remember { mutableStateOf(userSettings.defaultTimed) }

      val effectiveLevel = (currentLevel + levelOffset).coerceIn(1, 5)
      val effectiveTier = LevelTier.fromLevel(effectiveLevel)

      Dialog(onDismissRequest = { gameToIntro = null }) {
        Card(
          modifier = Modifier
            .fillMaxWidth()
            .padding(16.dp),
          shape = RoundedCornerShape(24.dp),
          colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
          elevation = CardDefaults.cardElevation(defaultElevation = 6.dp)
        ) {
          Column(
            modifier = Modifier
              .fillMaxWidth()
              .padding(22.dp),
            horizontalAlignment = Alignment.CenterHorizontally
          ) {
            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.SpaceBetween,
              verticalAlignment = Alignment.CenterVertically
            ) {
              DomainChip(domain = game.domain)
              IconButton(onClick = { gameToIntro = null }) {
                Icon(Icons.Default.Close, contentDescription = "Cerrar")
              }
            }

            Spacer(modifier = Modifier.height(12.dp))

            Text(text = game.iconEmoji, fontSize = 48.sp)
            Spacer(modifier = Modifier.height(8.dp))

            Text(
              text = game.title,
              style = MaterialTheme.typography.headlineSmall,
              fontWeight = FontWeight.Bold,
              color = MaterialTheme.colorScheme.onSurface
            )

            Text(
              text = game.instruction,
              style = MaterialTheme.typography.bodyMedium,
              color = MaterialTheme.colorScheme.onSurfaceVariant,
              modifier = Modifier.padding(top = 8.dp),
              lineHeight = 20.sp
            )

            Spacer(modifier = Modifier.height(16.dp))
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            Spacer(modifier = Modifier.height(16.dp))

            // Difficulty adjustment selector: Suave / Equilibrado / Desafío
            Text(
              text = "Ajuste de Dificultad: Nivel $effectiveLevel (${effectiveTier.tierName})",
              style = MaterialTheme.typography.labelMedium,
              fontWeight = FontWeight.Bold,
              color = MaterialTheme.colorScheme.onSurface
            )

            Spacer(modifier = Modifier.height(8.dp))

            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
              val options = listOf(
                Pair(-1, "Suave"),
                Pair(0, "Equilibrado"),
                Pair(1, "Desafío")
              )
              options.forEach { (offset, label) ->
                val isSelected = levelOffset == offset
                Surface(
                  modifier = Modifier
                    .weight(1f)
                    .clip(RoundedCornerShape(12.dp))
                    .clickable { levelOffset = offset },
                  shape = RoundedCornerShape(12.dp),
                  color = if (isSelected) game.domain.color else MaterialTheme.colorScheme.surfaceVariant
                ) {
                  Text(
                    text = label,
                    modifier = Modifier.padding(vertical = 10.dp),
                    style = MaterialTheme.typography.labelSmall,
                    fontWeight = FontWeight.Bold,
                    color = if (isSelected) Color.White else MaterialTheme.colorScheme.onSurface,
                    textAlign = androidx.compose.ui.text.style.TextAlign.Center
                  )
                }
              }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Mode Selector: Precisión vs Reto
            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
              Surface(
                modifier = Modifier
                  .weight(1f)
                  .clip(RoundedCornerShape(12.dp))
                  .clickable { isTimedMode = false },
                shape = RoundedCornerShape(12.dp),
                color = if (!isTimedMode) TealPrimary else MaterialTheme.colorScheme.surfaceVariant
              ) {
                Text(
                  text = "Precisión (Sin reloj)",
                  modifier = Modifier.padding(vertical = 10.dp),
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.Bold,
                  color = if (!isTimedMode) Color.White else MaterialTheme.colorScheme.onSurface,
                  textAlign = androidx.compose.ui.text.style.TextAlign.Center
                )
              }

              Surface(
                modifier = Modifier
                  .weight(1f)
                  .clip(RoundedCornerShape(12.dp))
                  .clickable { isTimedMode = true },
                shape = RoundedCornerShape(12.dp),
                color = if (isTimedMode) DomainVelocidad else MaterialTheme.colorScheme.surfaceVariant
              ) {
                Text(
                  text = "Contra el reloj",
                  modifier = Modifier.padding(vertical = 10.dp),
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.Bold,
                  color = if (isTimedMode) Color.White else MaterialTheme.colorScheme.onSurface,
                  textAlign = androidx.compose.ui.text.style.TextAlign.Center
                )
              }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Button(
              onClick = {
                val g = game
                gameToIntro = null
                viewModel.launchGame(g.id, customLevel = effectiveLevel, customTimed = isTimedMode)
              },
              modifier = Modifier
                .fillMaxWidth()
                .height(50.dp)
                .testTag("btn_intro_start"),
              shape = RoundedCornerShape(14.dp),
              colors = ButtonDefaults.buttonColors(containerColor = game.domain.color)
            ) {
              Text("Empezar", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
              Spacer(modifier = Modifier.width(8.dp))
              Icon(Icons.Default.PlayArrow, contentDescription = null)
            }
          }
        }
      }
    }
  }
}
