package com.example.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.model.RankTier

private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFB4BFEA)

/** Titulo de seccion sin recuadro: texto claro con una linea fina encima que separa del bloque anterior. */
@Composable
fun SpaceSectionTitle(text: String, modifier: Modifier = Modifier, hint: String? = null) {
  Column(modifier = modifier.fillMaxWidth().padding(top = 8.dp)) {
    Box(
      modifier = Modifier
        .fillMaxWidth()
        .height(1.dp)
        .background(Color.White.copy(alpha = 0.10f))
    )
    Text(text, color = OnNight, fontSize = 20.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 14.dp))
    if (hint != null) Text(hint, color = OnNightDim, fontSize = 13.sp, modifier = Modifier.padding(top = 2.dp))
  }
}

/**
 * Cabecera de la Liga sin tarjeta: el escudo grande y brillante en el centro, el nombre de la liga, los trofeos y
 * una barra fina hasta la siguiente liga.
 */
@Composable
fun LeagueHero(tier: RankTier, rating: Int, progress: Float, index: Int?, modifier: Modifier = Modifier) {
  val pal = tier.palette()
  val next = RankTier.entries.getOrNull(tier.ordinal + 1)
  Column(modifier = modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
    LeagueShield(tier = tier, size = 150.dp, pips = ((progress * 5).toInt() + 1).coerceIn(1, 5), glow = true)
    Text(tier.tierName, color = pal.base, fontSize = 34.sp, fontWeight = FontWeight.Bold, modifier = Modifier.padding(top = 4.dp))
    Text(
      text = buildString {
        append("$rating trofeos")
        if (index != null) append("  ·  nivel $index")
      },
      color = OnNightDim, fontSize = 14.sp
    )
    if (next != null) {
      Spacer(Modifier.height(12.dp))
      Box(
        modifier = Modifier
          .width(220.dp)
          .height(8.dp)
          .clip(RoundedCornerShape(4.dp))
          .background(Color.White.copy(alpha = 0.14f))
      ) {
        Box(
          modifier = Modifier
            .fillMaxWidth(progress.coerceIn(0.03f, 1f))
            .height(8.dp)
            .clip(RoundedCornerShape(4.dp))
            .background(pal.base)
        )
      }
      Text(
        "faltan ${(next.minRating - rating).coerceAtLeast(0)} para ${next.tierName}",
        color = OnNightDim, fontSize = 12.sp, textAlign = TextAlign.Center, modifier = Modifier.padding(top = 6.dp)
      )
    }
  }
}

/** Nivel por juego como filas sueltas: escudo de la liga del juego, nombre, barra fina y etiqueta de nivel. */
@Composable
fun GameLevelsList(levels: GameLevels, ranks: Map<String, GameRankInfo>, modifier: Modifier = Modifier) {
  Column(modifier = modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(14.dp)) {
    GameRegistry.allGames.forEach { g ->
      val v = levels[g.id]
      val rank = ranks[g.id]
      Row(verticalAlignment = Alignment.CenterVertically) {
        LeagueShield(tier = rank?.tier ?: RankTier.BRONCE, size = 34.dp, pips = rank?.pipCount() ?: 0)
        Spacer(Modifier.width(12.dp))
        Column(modifier = Modifier.weight(1f)) {
          Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
            Text(g.title, color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, maxLines = 1)
            Text(
              text = if (v == null) "Sin medir" else "${levelWord(v)} · P${com.example.data.Percentile.of(v)}",
              color = if (v == null) OnNightDim else g.domain.color,
              fontSize = 12.sp, fontWeight = FontWeight.Bold
            )
          }
          Box(
            modifier = Modifier
              .padding(top = 6.dp)
              .fillMaxWidth()
              .height(6.dp)
              .clip(RoundedCornerShape(3.dp))
              .background(Color.White.copy(alpha = 0.12f))
          ) {
            if (v != null) {
              Box(
                modifier = Modifier
                  .fillMaxWidth(v.coerceIn(0.04f, 1f))
                  .height(6.dp)
                  .clip(RoundedCornerShape(3.dp))
                  .background(g.domain.color)
              )
            }
          }
          Text(
            rank?.label ?: "",
            color = OnNightDim, fontSize = 11.sp, modifier = Modifier.padding(top = 3.dp)
          )
        }
      }
    }
    Text(
      "Estimación interna según cómo se adapta la dificultad a ti; no es una medida clínica.",
      color = OnNightDim, fontSize = 11.sp
    )
  }
}

/** Fila de texto clicable para expandir/ocultar detalles (sin recuadro). */
@Composable
fun SpaceToggle(text: String, onClick: () -> Unit, modifier: Modifier = Modifier) {
  Text(
    text = text,
    color = Color(0xFFFFC93C),
    fontSize = 15.sp,
    fontWeight = FontWeight.Bold,
    modifier = modifier
      .clip(RoundedCornerShape(12.dp))
      .clickable(onClick = onClick)
      .padding(vertical = 10.dp, horizontal = 4.dp)
  )
}
