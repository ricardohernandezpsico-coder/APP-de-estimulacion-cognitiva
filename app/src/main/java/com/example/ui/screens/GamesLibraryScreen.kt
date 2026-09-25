package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
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
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
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
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getDomainName
import com.example.ui.i18n.getGameTitle
import com.example.ui.i18n.strings
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

  Box(modifier = modifier.fillMaxSize()) {
    val currentLang = LocalAppLanguage.current
    LazyColumn(
      modifier = Modifier
        .fillMaxSize()
        .padding(horizontal = 16.dp),
      contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
      verticalArrangement = Arrangement.spacedBy(6.dp)
    ) {
      item {
        Column(modifier = Modifier.padding(horizontal = 6.dp)) {
          Text(
            text = strings.gamesLibraryTitle,
            color = Color(0xFFEAF0FF),
            fontSize = 28.sp,
            fontWeight = FontWeight.Bold
          )
          Text(
            text = "Elige un planeta y entrena",
            color = Color(0xFFB4BFEA),
            fontSize = 14.sp
          )
        }
      }

      // Dominios como texto con punto de color (sin recuadros): toca uno para filtrar el mapa
      item {
        LazyRow(
          horizontalArrangement = Arrangement.spacedBy(4.dp),
          modifier = Modifier.fillMaxWidth().padding(top = 8.dp, bottom = 4.dp)
        ) {
          item {
            DomainTab(
              label = strings.filterAll,
              color = Color.White,
              selected = selectedDomainFilter == null,
              onClick = { selectedDomainFilter = null },
              tag = "filter_all"
            )
          }
          items(DomainType.values()) { domain ->
            val isSelected = selectedDomainFilter == domain
            DomainTab(
              label = getDomainName(domain, currentLang),
              color = domain.color,
              selected = isSelected,
              onClick = { selectedDomainFilter = if (isSelected) null else domain },
              tag = "filter_${domain.name.lowercase()}"
            )
          }
        }
      }

      // Mapa de planetas: filas de 3, con la columna central desplazada hacia abajo (constelacion)
      val rows = filteredGames.chunked(3)
      items(rows.size) { r ->
        val rowGames = rows[r]
        Row(
          modifier = Modifier.fillMaxWidth().padding(top = 10.dp),
          horizontalArrangement = Arrangement.SpaceEvenly,
          verticalAlignment = Alignment.Top
        ) {
          for (c in 0 until 3) {
            val game = rowGames.getOrNull(c)
            Box(
              modifier = Modifier
                .weight(1f)
                .padding(top = if (c == 1) 44.dp else 0.dp),
              contentAlignment = Alignment.TopCenter
            ) {
              if (game != null) {
                val level = gameLevels[game.id] ?: 1
                val rankInfo = gameRanks.find { it.gameId == game.id } ?: GameRankInfo(game.id, 0)
                Planet(
                  game = game,
                  title = getGameTitle(game.id, currentLang, game.title),
                  level = level,
                  rank = rankInfo,
                  best = history.filter { it.gameId == game.id }.maxOfOrNull { it.score },
                  onClick = { gameToIntro = game }
                )
              }
            }
          }
        }
        Spacer(Modifier.height(if (rowGames.size == 3) 40.dp else 8.dp))
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
        com.example.ui.theme.ClayLightCard(
          modifier = Modifier
            .fillMaxWidth()
            .padding(16.dp)
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

            com.example.ui.components.GameIcon(game.id, size = 72.dp)
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

@Composable
private fun DomainTab(label: String, color: Color, selected: Boolean, onClick: () -> Unit, tag: String) {
  Row(
    modifier = Modifier
      .clip(RoundedCornerShape(14.dp))
      .clickable(onClick = onClick)
      .padding(horizontal = 10.dp, vertical = 8.dp)
      .testTag(tag),
    verticalAlignment = Alignment.CenterVertically
  ) {
    Box(
      modifier = Modifier
        .size(if (selected) 12.dp else 9.dp)
        .clip(CircleShape)
        .background(color)
    )
    Spacer(Modifier.width(6.dp))
    Text(
      text = label,
      color = if (selected) Color.White else Color(0xFFB4BFEA),
      fontSize = 14.sp,
      fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium
    )
  }
}

/** Un juego como "planeta": esfera de arcilla del color de su dominio con resplandor, nombre y nivel debajo. */
@Composable
private fun Planet(
  game: GameDefinition,
  title: String,
  level: Int,
  rank: GameRankInfo,
  best: Int?,
  onClick: () -> Unit
) {
  val ink = Clay.Ink
  val size = 84.dp
  Column(
    horizontalAlignment = Alignment.CenterHorizontally,
    modifier = Modifier
      .clip(RoundedCornerShape(20.dp))
      .clickable(onClick = onClick)
      .padding(4.dp)
      .testTag("game_card_${game.id}")
  ) {
    Box(
      modifier = Modifier
        .size(size + 28.dp)
        .drawBehind {
          // Resplandor del dominio
          drawCircle(
            Brush.radialGradient(listOf(game.domain.color.copy(alpha = 0.38f), Color.Transparent), center, this.size.minDimension / 2f),
            radius = this.size.minDimension / 2f
          )
          // Sombra dura de arcilla
          drawCircle(ink, radius = size.toPx() / 2f, center = Offset(center.x, center.y + 4.dp.toPx()))
        },
      contentAlignment = Alignment.Center
    ) {
      Box(
        modifier = Modifier
          .size(size)
          .clip(CircleShape)
          .background(game.domain.color)
          .border(3.dp, ink, CircleShape),
        contentAlignment = Alignment.Center
      ) {
        // Brillo superior
        Box(
          modifier = Modifier
            .align(Alignment.TopCenter)
            .padding(top = 7.dp)
            .size(width = 38.dp, height = 12.dp)
            .clip(RoundedCornerShape(50))
            .background(Color.White.copy(alpha = 0.32f))
        )
        com.example.ui.components.GameIcon(game.id, size = 56.dp)
      }
    }
    Text(
      text = title,
      color = Color.White,
      fontSize = 14.sp,
      fontWeight = FontWeight.Bold,
      textAlign = androidx.compose.ui.text.style.TextAlign.Center,
      maxLines = 2,
      overflow = TextOverflow.Ellipsis,
      lineHeight = 16.sp,
      modifier = Modifier.padding(top = 2.dp).width(104.dp)
    )
    Text(
      text = "Nivel $level · ${rank.tier.tierName}",
      color = rank.tier.color,
      fontSize = 11.sp,
      fontWeight = FontWeight.SemiBold
    )
  }
}
