package com.example.ui.screens

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
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
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material3.Icon
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
import com.example.model.RankTier
import com.example.ui.components.DomainLegend
import com.example.ui.components.LeagueShield
import com.example.ui.components.SpaceSectionTitle
import com.example.ui.components.levelWord
import com.example.ui.components.overallIndex
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.viewmodel.NeuroVidaViewModel

/**
 * Perfil: quién eres (avatar, liga, racha, cifras clave) y la puerta a los ajustes. Los ajustes dejaron de ser
 * "la pestaña" y viven detrás del botón de este perfil, con su propia flecha de regreso.
 */
@Composable
fun ProfileScreen(viewModel: NeuroVidaViewModel, modifier: Modifier = Modifier) {
  var showSettings by remember { mutableStateOf(false) }

  if (showSettings) {
    BackHandler { showSettings = false }
    Column(modifier = modifier.fillMaxSize()) {
      Row(
        modifier = Modifier
          .fillMaxWidth()
          .padding(horizontal = 12.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
      ) {
        Box(
          modifier = Modifier
            .size(44.dp)
            .clip(CircleShape)
            .background(Clay.Cream)
            .border(Clay.Border, Clay.Ink, CircleShape)
            .clickable { showSettings = false },
          contentAlignment = Alignment.Center
        ) { Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver al perfil", tint = Clay.Ink) }
        Spacer(Modifier.width(12.dp))
        Text("Ajustes", color = Color(0xFFEAF0FF), fontSize = 22.sp, fontWeight = FontWeight.Bold)
      }
      SettingsScreen(viewModel = viewModel, modifier = Modifier.weight(1f))
    }
    return
  }

  val userSettings by viewModel.userSettings.collectAsState()
  val streak by viewModel.currentStreak.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val index = overallIndex(levels)
  val best = history.maxOfOrNull { it.score }

  val prog = if (tier == RankTier.MAESTRO) ((avg - tier.minRating) % 250) / 250f else (avg - tier.minRating) / 250f

  LazyColumn(
    modifier = modifier
      .fillMaxSize()
      .padding(horizontal = 22.dp),
    contentPadding = PaddingValues(top = 16.dp, bottom = 28.dp),
    verticalArrangement = Arrangement.spacedBy(22.dp)
  ) {
    // Tu escudo y tu nombre, sin recuadros
    item {
      Column(modifier = Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
        Box(contentAlignment = Alignment.BottomEnd) {
          LeagueShield(tier = tier, size = 160.dp, pips = ((prog * 5).toInt() + 1).coerceIn(1, 5), glow = true)
          Box(
            modifier = Modifier
              .size(54.dp)
              .clip(CircleShape)
              .background(Clay.Grape)
              .border(Clay.Border, Clay.Ink, CircleShape),
            contentAlignment = Alignment.Center
          ) { Text(userSettings.avatar, fontSize = 26.sp) }
        }
        Text(userSettings.name, color = Color(0xFFEAF0FF), fontSize = 30.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 6.dp))
        Text(
          text = "Liga ${tier.tierName}" + (index?.let { "  ·  nivel ${levelWord(it / 100f).lowercase()}" } ?: ""),
          color = Color(0xFFB4BFEA),
          fontSize = 15.sp
        )
      }
    }

    // Tres cifras como texto grande, separadas por lineas finas
    item {
      Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        StatText("Racha", "$streak", Clay.Sun, Modifier.weight(1f), fire = true)
        Box(Modifier.width(1.dp).height(44.dp).background(Color.White.copy(alpha = 0.14f)))
        StatText("Partidas", "${history.size}", Clay.Sky, Modifier.weight(1f))
        Box(Modifier.width(1.dp).height(44.dp).background(Color.White.copy(alpha = 0.14f)))
        StatText("Mejor", best?.toString() ?: "–", Clay.Lime, Modifier.weight(1f))
      }
    }

    item {
      Column {
        SpaceSectionTitle("Tus dominios", hint = "Cómo vas en cada área")
        Spacer(Modifier.height(12.dp))
        DomainLegend(levels)
      }
    }

    item {
      ClayButton(
        text = "Ajustes",
        onClick = { showSettings = true },
        color = Clay.Cream,
        icon = Icons.Default.Settings
      )
    }
  }
}

@Composable
private fun StatText(label: String, value: String, color: Color, modifier: Modifier, fire: Boolean = false) {
  Column(modifier = modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    Row(verticalAlignment = Alignment.CenterVertically) {
      if (fire) Icon(Icons.Default.Whatshot, contentDescription = null, tint = color, modifier = Modifier.size(24.dp))
      Text(value, color = color, fontSize = 34.sp, fontWeight = FontWeight.Bold)
    }
    Text(label, color = Color(0xFFB4BFEA), fontSize = 13.sp)
  }
}
