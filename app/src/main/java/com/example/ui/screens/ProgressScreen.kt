package com.example.ui.screens

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.BellCurveCard
import com.example.ui.components.DomainRadarCard
import com.example.ui.components.GameLevelsList
import com.example.ui.components.LeagueHero
import com.example.ui.components.ProgressTrendChart
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.SpaceToggle
import com.example.ui.components.overallIndex
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getDomainName
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.TealPrimary
import com.example.viewmodel.NeuroVidaViewModel
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFB4BFEA)

/**
 * Liga (antes "Progreso"), sin tarjetas: el escudo grande arriba y, debajo, secciones sueltas separadas por una
 * linea fina (posicion, perfil, nivel por juego). El detalle secundario (maestria, tendencia, historial) queda
 * plegado detras de "Ver mas detalles" para no saturar.
 */
@Composable
fun ProgressScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val domainMastery by viewModel.domainMasteryInfo.collectAsState()
  val gameRanks by viewModel.gameRanks.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val currentLang = LocalAppLanguage.current
  val dateFormatter = remember { SimpleDateFormat("dd MMM, HH:mm", Locale.getDefault()) }
  var showMore by remember { mutableStateOf(false) }

  val avg = if (gameRanks.isEmpty()) 0 else gameRanks.sumOf { it.rating } / gameRanks.size
  val tier = RankTier.fromRating(avg)
  val prog = if (tier == RankTier.MAESTRO) ((avg - tier.minRating) % 250) / 250f else (avg - tier.minRating) / 250f

  LazyColumn(
    modifier = modifier
      .fillMaxSize()
      .padding(horizontal = 22.dp),
    contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
    verticalArrangement = Arrangement.spacedBy(26.dp)
  ) {
    item {
      Column {
        Text("Tu liga", color = OnNight, fontSize = 28.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(8.dp))
        LeagueHero(tier = tier, rating = avg, progress = prog, index = overallIndex(levels))
      }
    }

    item {
      BellCurveCard(
        levels = levels,
        scoresWithTime = history.map { it.score to it.timestamp },
        accent = TealPrimary
      )
    }

    item { DomainRadarCard(levels = levels) }

    item {
      Column {
        SpaceSectionTitle("Nivel por juego", hint = "Tu liga en cada juego y cómo va tu nivel")
        Spacer(Modifier.height(14.dp))
        GameLevelsList(levels = levels, ranks = gameRanks.associateBy { it.gameId })
      }
    }

    item {
      SpaceToggle(
        text = if (showMore) "Ocultar detalles" else "Ver más detalles",
        onClick = { showMore = !showMore }
      )
    }

    item {
      AnimatedVisibility(visible = showMore) {
        Column(verticalArrangement = Arrangement.spacedBy(26.dp)) {
          // Maestría por dominio (XP que solo crece)
          Column {
            SpaceSectionTitle("Maestría por dominio", hint = "Cada partida suma experiencia a su dominio")
            Spacer(Modifier.height(12.dp))
            Column(verticalArrangement = Arrangement.spacedBy(14.dp)) {
              domainMastery.forEach { info ->
                Column {
                  Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                      Box(Modifier.size(10.dp).clip(CircleShape).background(info.domain.color))
                      Spacer(Modifier.width(8.dp))
                      Text(getDomainName(info.domain, currentLang), color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                    }
                    Text("${info.tier.tierName} · ${info.xpLabel}", color = info.domain.color, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                  }
                  Box(
                    modifier = Modifier
                      .padding(top = 6.dp)
                      .fillMaxWidth()
                      .height(6.dp)
                      .clip(RoundedCornerShape(3.dp))
                      .background(Color.White.copy(alpha = 0.12f))
                  ) {
                    Box(
                      Modifier
                        .fillMaxWidth(info.progressInTier.coerceIn(0.03f, 1f))
                        .height(6.dp)
                        .clip(RoundedCornerShape(3.dp))
                        .background(info.domain.color)
                    )
                  }
                }
              }
            }
          }

          // Tendencia
          ProgressTrendChart(history = history)

          // Historial reciente como filas sueltas
          Column {
            SpaceSectionTitle("Historial reciente")
            Spacer(Modifier.height(10.dp))
            history.take(8).forEach { item ->
              val game = GameRegistry.getById(item.gameId)
              Row(
                modifier = Modifier.fillMaxWidth().padding(vertical = 7.dp),
                verticalAlignment = Alignment.CenterVertically
              ) {
                Box(Modifier.size(10.dp).clip(CircleShape).background(game?.domain?.color ?: TealPrimary))
                Spacer(Modifier.width(10.dp))
                Column(Modifier.weight(1f)) {
                  Text(
                    if (game != null) getGameTitle(game.id, currentLang, game.title) else "Juego",
                    color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold
                  )
                  Text(dateFormatter.format(Date(item.timestamp)), color = OnNightDim, fontSize = 12.sp)
                }
                Text("${item.score} pts", color = game?.domain?.color ?: TealPrimary, fontSize = 15.sp, fontWeight = FontWeight.Bold)
              }
            }
          }
        }
      }
    }
  }
}
