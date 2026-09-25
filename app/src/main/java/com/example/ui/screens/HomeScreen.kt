package com.example.ui.screens

import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Flag
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Whatshot
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.PathEffect
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import com.example.ui.components.CosmosScroll
import com.example.ui.components.LeagueShield
import com.example.ui.components.palette
import com.example.data.LeagueEvent
import com.example.ui.components.overallIndex
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getGameTitle
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayCard
import com.example.ui.theme.ClayPill
import com.example.viewmodel.NeuroVidaViewModel
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.TimeZone
import kotlin.math.roundToInt
import kotlin.math.sin
import kotlinx.coroutines.launch

private const val DAY_MS = 86_400_000L
private const val PathTilt = 32f
private val RowHeight = 120.dp
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)
private val Milestones = listOf(3, 7, 14, 30, 50, 100, 200, 365)

private fun dayIndex(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

private sealed class PathItem {
  data class Past(val day: Long, val results: List<GamePlayResult>) : PathItem()
  data class Today(val day: Long, val results: List<GamePlayResult>) : PathItem()
  /** [flagTarget] != null: hito de racha (bandera) con [remaining] días por delante. */
  data class Future(val flagTarget: Int?, val remaining: Int) : PathItem()
}

/** Posición horizontal (0..1) del nodo i: una sinusoide suave, así el trazo es continuo entre filas. */
private fun fx(i: Int): Float = 0.5f + 0.27f * sin(i * 0.9f)

/**
 * Inicio ("Hoy") como un CAMINO por el espacio: cada día es un nodo de un sendero sinuoso; hoy es el nodo grande
 * (toca para entrenar), lo anterior queda hacia arriba (desliza para ver tu recorrido) y adelante hay un hito
 * de racha. El fondo de estrellas viaja con el desplazamiento (paralaje), ver [CosmosScroll].
 */
@Composable
fun HomeScreen(
  viewModel: NeuroVidaViewModel,
  onNavigateToGames: () -> Unit,
  modifier: Modifier = Modifier
) {
  val streak by viewModel.currentStreak.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val ranks by viewModel.gameRanks.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val weeklyChallenges by viewModel.weeklyChallengeProgress.collectAsState()
  val leagueEvents by viewModel.leagueEvents.collectAsState()
  val eventsByDay = remember(leagueEvents) { leagueEvents.groupBy { dayIndex(it.timestamp) } }
  val lang = LocalAppLanguage.current
  val scope = rememberCoroutineScope()

  var dayDetail by remember { mutableStateOf<PathItem?>(null) }
  var showChallenges by remember { mutableStateOf(false) }

  val avg = if (ranks.isEmpty()) 0 else ranks.sumOf { it.rating } / ranks.size
  val tier = RankTier.fromRating(avg)
  val index = overallIndex(levels)

  val items = remember(history, streak) {
    val today = dayIndex(System.currentTimeMillis())
    val byDay = history.groupBy { dayIndex(it.timestamp) }
    val first = minOf(byDay.keys.minOrNull() ?: today, today - 13)
    val list = mutableListOf<PathItem>()
    for (d in first until today) list += PathItem.Past(d, byDay[d].orEmpty())
    list += PathItem.Today(today, byDay[today].orEmpty())
    val target = Milestones.firstOrNull { it > streak } ?: (streak + 30)
    val remaining = (target - streak).coerceAtLeast(1)
    val ahead = minOf(remaining, 3)
    for (k in 1 until ahead) list += PathItem.Future(null, remaining)
    list += PathItem.Future(target, remaining)
    list
  }
  val todayIdx = items.indexOfFirst { it is PathItem.Today }

  val listState = rememberLazyListState(initialFirstVisibleItemIndex = (todayIdx - 3).coerceAtLeast(0))
  val rowPx = with(LocalDensity.current) { RowHeight.toPx() }
  LaunchedEffect(listState, rowPx) {
    snapshotFlow { listState.firstVisibleItemIndex * rowPx + listState.firstVisibleItemScrollOffset }
      .collect { CosmosScroll.offset = it }
  }
  val todayVisible by remember {
    derivedStateOf { listState.layoutInfo.visibleItemsInfo.any { it.index == todayIdx } }
  }

  Column(modifier = modifier.fillMaxSize()) {
    // Cabecera fija: liga, nivel y racha en una sola línea (sin recuadros)
    Row(
      modifier = Modifier
        .fillMaxWidth()
        .padding(start = 20.dp, end = 20.dp, top = 8.dp),
      verticalAlignment = Alignment.CenterVertically
    ) {
      LeagueShield(tier = tier, size = 26.dp)
      Spacer(Modifier.width(8.dp))
      Text(tier.tierName, color = OnNight, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      Text("  ·  ", color = OnNightDim, fontSize = 15.sp)
      Text(index?.let { "$it nivel" } ?: "sin nivel", color = OnNight, fontSize = 15.sp)
      Text("  ·  ", color = OnNightDim, fontSize = 15.sp)
      Icon(Icons.Default.Whatshot, contentDescription = null, tint = Clay.Sun, modifier = Modifier.size(18.dp))
      Text("$streak", color = Clay.Sun, fontSize = 15.sp, fontWeight = FontWeight.Bold, modifier = Modifier.testTag("streak_pill"))
      Spacer(Modifier.weight(1f))
      Text(
        text = "Desafíos ${weeklyChallenges.count { it.isComplete }}/${weeklyChallenges.size}",
        color = Clay.Sun,
        fontSize = 13.sp,
        fontWeight = FontWeight.SemiBold,
        modifier = Modifier
          .clip(RoundedCornerShape(10.dp))
          .clickable { showChallenges = true }
          .padding(6.dp)
      )
    }
    Text(
      text = "Tu camino",
      color = OnNight,
      fontSize = 28.sp,
      fontWeight = FontWeight.Bold,
      modifier = Modifier.padding(start = 20.dp, top = 6.dp, bottom = 4.dp)
    )

    BoxWithConstraints(modifier = Modifier.weight(1f).fillMaxWidth()) {
      val widthPx = with(LocalDensity.current) { maxWidth.toPx() }
      val camera = with(LocalDensity.current) { 14.dp.toPx() * 8f }
      LazyColumn(
        state = listState,
        modifier = Modifier
          .fillMaxSize()
          .graphicsLayer {
            // Perspectiva "texto de Star Wars": el camino se inclina hacia el horizonte
            rotationX = PathTilt
            transformOrigin = TransformOrigin(0.5f, 1f)
            cameraDistance = camera
            compositingStrategy = CompositingStrategy.Offscreen
          }
          .drawWithContent {
            drawContent()
            // Lo lejano se desvanece en las estrellas
            drawRect(
              Brush.verticalGradient(0f to Color.Transparent, 0.30f to Color.Black, 0.92f to Color.Black, 1f to Color.Transparent),
              blendMode = BlendMode.DstIn
            )
          },
        contentPadding = PaddingValues(bottom = 24.dp)
      ) {
        items(items.size) { i ->
          PathRow(
            index = i,
            item = items[i],
            widthPx = widthPx,
            gameIds = dailySession.gameIds,
            completedToday = dailySession.completedCount,
            lang = lang,
            onStart = { viewModel.startDailySession() },
            onOpenDay = { dayDetail = it },
            events = when (val row = items[i]) {
              is PathItem.Past -> eventsByDay[row.day].orEmpty()
              is PathItem.Today -> eventsByDay[row.day].orEmpty()
              else -> emptyList()
            }
          )
        }
      }
      if (!todayVisible) {
        ClayPill(
          text = "Volver a hoy",
          color = Clay.Sun,
          modifier = Modifier
            .align(Alignment.BottomCenter)
            .padding(bottom = 12.dp)
            .clickable { scope.launch { listState.animateScrollToItem((todayIdx - 3).coerceAtLeast(0)) } }
        )
      }
    }
  }

  dayDetail?.let { item ->
    val results = when (item) {
      is PathItem.Past -> item.results
      is PathItem.Today -> item.results
      else -> emptyList()
    }
    val day = when (item) {
      is PathItem.Past -> item.day
      is PathItem.Today -> item.day
      else -> 0L
    }
    Dialog(onDismissRequest = { dayDetail = null }) {
      ClayCard(modifier = Modifier.fillMaxWidth().padding(8.dp), color = Clay.Cream, radius = 28.dp, contentPadding = 20.dp) {
        Text(dateLabel(day), color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(10.dp))
        // Ascensos de liga de ese día, arriba de las partidas: son lo más importante que pasó.
        eventsByDay[day].orEmpty().forEach { ev ->
          val gameName = ev.gameId?.let { id -> GameRegistry.getById(id)?.let { getGameTitle(it.id, lang, it.title) } }
          Row(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
            LeagueShield(tier = ev.tier, size = 30.dp, pips = 1)
            Spacer(Modifier.width(10.dp))
            Column {
              Text("Subiste a ${ev.tier.tierName}", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold)
              Text(gameName ?: "Liga general", color = Clay.InkSoft, fontSize = 13.sp)
            }
          }
        }
        if (eventsByDay[day].orEmpty().isNotEmpty()) Spacer(Modifier.height(8.dp))
        if (results.isEmpty()) {
          Text("Sin partidas este día.", color = Clay.InkSoft, fontSize = 15.sp)
        } else {
          results.sortedBy { it.timestamp }.forEach { r ->
            val def = GameRegistry.getById(r.gameId)
            Row(modifier = Modifier.fillMaxWidth().padding(vertical = 4.dp), horizontalArrangement = Arrangement.SpaceBetween) {
              Text(
                text = if (def != null) getGameTitle(def.id, lang, def.title) else r.gameId,
                color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.SemiBold
              )
              Text("${r.score} pts", color = Clay.Ink, fontSize = 16.sp, fontWeight = FontWeight.Bold)
            }
          }
        }
      }
    }
  }

  if (showChallenges) {
    Dialog(onDismissRequest = { showChallenges = false }) {
      ClayCard(modifier = Modifier.fillMaxWidth().padding(8.dp), color = Clay.Grape, radius = 28.dp, contentPadding = 20.dp) {
        Text("Desafíos de la semana", color = Clay.Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold)
        Spacer(Modifier.height(12.dp))
        weeklyChallenges.forEach { wc ->
          Column(modifier = Modifier.padding(vertical = 6.dp)) {
            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
              Text(wc.def.title, color = Clay.Ink, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
              Text(if (wc.isComplete) "Listo" else "${wc.progress}/${wc.def.target}", color = Clay.Ink, fontSize = 14.sp, fontWeight = FontWeight.Bold)
            }
            Box(
              modifier = Modifier
                .padding(top = 4.dp)
                .fillMaxWidth()
                .height(10.dp)
                .clip(RoundedCornerShape(5.dp))
                .background(Color.White.copy(alpha = 0.55f))
                .border(2.dp, Clay.Ink, RoundedCornerShape(5.dp))
            ) {
              Box(
                modifier = Modifier
                  .fillMaxWidth((wc.progress.toFloat() / wc.def.target).coerceIn(0.04f, 1f))
                  .height(10.dp)
                  .background(if (wc.isComplete) Clay.Lime else Clay.Sun)
              )
            }
          }
        }
      }
    }
  }
}

private fun dateLabel(day: Long): String {
  val f = SimpleDateFormat("EEE d MMM", Locale("es")).apply { timeZone = TimeZone.getTimeZone("UTC") }
  return f.format(Date(day * DAY_MS)).replaceFirstChar { it.uppercase() }
}

@Composable
private fun PathRow(
  index: Int,
  item: PathItem,
  widthPx: Float,
  gameIds: List<String>,
  completedToday: Int,
  lang: com.example.model.AppLanguage,
  onStart: () -> Unit,
  onOpenDay: (PathItem) -> Unit,
  events: List<LeagueEvent> = emptyList()
) {
  val density = LocalDensity.current
  val hPx = with(density) { RowHeight.toPx() }
  val x = fx(index) * widthPx
  val xTop = (fx(index - 1) + fx(index)) / 2f * widthPx
  val xBot = (fx(index) + fx(index + 1)) / 2f * widthPx
  val onLeftHalf = x < widthPx / 2f
  val done = (item is PathItem.Past && item.results.isNotEmpty()) || (item is PathItem.Today && completedToday >= 3)
  val trail = when {
    item is PathItem.Future -> Color.White.copy(alpha = 0.25f)
    item is PathItem.Past && item.results.isNotEmpty() -> Clay.Lime.copy(alpha = 0.65f)
    item is PathItem.Today -> Clay.Sun.copy(alpha = 0.8f)
    else -> Color.White.copy(alpha = 0.16f)
  }

  Box(modifier = Modifier.fillMaxWidth().height(RowHeight)) {
    Canvas(modifier = Modifier.matchParentSize()) {
      val p = Path().apply {
        moveTo(xTop, 0f)
        cubicTo(xTop, hPx * 0.25f, x, hPx * 0.25f, x, hPx / 2f)
        cubicTo(x, hPx * 0.75f, xBot, hPx * 0.75f, xBot, hPx)
      }
      drawPath(
        p, trail,
        style = Stroke(
          width = 5.dp.toPx(), cap = StrokeCap.Round,
          pathEffect = PathEffect.dashPathEffect(floatArrayOf(1f, 11.dp.toPx()))
        )
      )
    }

    // Nodo
    val nodeR = when {
      item is PathItem.Today -> 32.dp
      item is PathItem.Future && item.flagTarget != null -> 24.dp
      done -> 20.dp
      else -> 7.dp
    }
    val nodeRpx = with(density) { nodeR.toPx() }
    Box(
      modifier = Modifier
        .offset { IntOffset((x - nodeRpx).roundToInt(), (hPx / 2f - nodeRpx).roundToInt()) }
        .size(nodeR * 2),
      contentAlignment = Alignment.Center
    ) {
      when {
        item is PathItem.Today -> TodayNode(done = done, onClick = onStart)
        item is PathItem.Future && item.flagTarget != null ->
          Box(
            modifier = Modifier.fillMaxSize().clip(CircleShape).background(Clay.Coral).border(3.dp, Clay.Ink, CircleShape),
            contentAlignment = Alignment.Center
          ) { Icon(Icons.Default.Flag, contentDescription = "Hito de racha", tint = Color.White, modifier = Modifier.size(24.dp)) }
        item is PathItem.Future ->
          Box(modifier = Modifier.fillMaxSize().clip(CircleShape).background(Color.White.copy(alpha = 0.18f)).border(2.dp, Color.White.copy(alpha = 0.3f), CircleShape))
        done ->
          Box(
            modifier = Modifier
              .fillMaxSize()
              .clip(CircleShape)
              .background(Clay.Lime)
              .border(3.dp, Clay.Ink, CircleShape)
              .clickable { onOpenDay(item) },
            contentAlignment = Alignment.Center
          ) { Icon(Icons.Default.Check, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(22.dp)) }
        else ->
          Box(modifier = Modifier.fillMaxSize().clip(CircleShape).background(Color.White.copy(alpha = 0.22f)))
      }
    }

    // Ascenso de liga ese día: escudito pegado arriba del nodo (la liga general manda sobre la de un juego).
    val best = events.maxWithOrNull(compareBy<LeagueEvent>({ it.gameId == null }, { it.tier.ordinal }))
    if (best != null) {
      val badge = 26.dp
      val badgePx = with(density) { badge.toPx() }
      Box(
        modifier = Modifier.offset {
          IntOffset((x + nodeRpx * 0.55f - badgePx / 2f).roundToInt(), (hPx / 2f - nodeRpx - badgePx * 0.75f).roundToInt())
        }
      ) { LeagueShield(tier = best.tier, size = badge, pips = 1) }
    }

    // Etiqueta al lado del nodo (hacia el lado con más espacio)
    val labelW = 150.dp
    val gap = 14.dp
    val labelWpx = with(density) { labelW.toPx() }
    val gapPx = with(density) { gap.toPx() }
    val lx = if (onLeftHalf) x + nodeRpx + gapPx else x - nodeRpx - gapPx - labelWpx
    Column(
      modifier = Modifier
        .offset { IntOffset(lx.roundToInt(), (hPx / 2f - with(density) { 24.dp.toPx() }).roundToInt()) }
        .width(labelW),
      horizontalAlignment = if (onLeftHalf) Alignment.Start else Alignment.End
    ) {
      val align = if (onLeftHalf) TextAlign.Start else TextAlign.End
      when (item) {
        is PathItem.Today -> {
          val next = if (completedToday < 3) gameIds.getOrNull(completedToday) else null
          val def = next?.let { GameRegistry.getById(it) }
          Text(if (done) "Hoy" else "Ahora", color = OnNightDim, fontSize = 12.sp, textAlign = align)
          Text(
            text = if (done) "¡Sesión completa!" else if (def != null) getGameTitle(def.id, lang, def.title) else "Entrenar",
            color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.Bold, lineHeight = 22.sp,
            maxLines = 2, overflow = TextOverflow.Ellipsis, textAlign = align
          )
          Text(
            text = if (done) "Toca para otra ronda" else "Falta${if (3 - completedToday == 1) "" else "n"} ${3 - completedToday} de 3",
            color = OnNightDim, fontSize = 12.sp, textAlign = align
          )
        }
        is PathItem.Past -> if (item.results.isNotEmpty()) {
          Text(dateLabel(item.day), color = OnNightDim, fontSize = 12.sp, textAlign = align)
          Text(
            "${item.results.size} ${if (item.results.size == 1) "juego" else "juegos"}",
            color = Color.White, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, textAlign = align
          )
          if (best != null) {
            Text(
              "Subiste a ${best.tier.tierName}",
              color = best.tier.palette().light, fontSize = 13.sp, fontWeight = FontWeight.Bold, textAlign = align
            )
          }
        }
        is PathItem.Future -> if (item.flagTarget != null) {
          Text("Racha de ${item.flagTarget} días", color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Bold, textAlign = align)
          Text("faltan ${item.remaining}", color = Clay.Sun, fontSize = 13.sp, textAlign = align)
        }
      }
    }
  }
}

@Composable
private fun TodayNode(done: Boolean, onClick: () -> Unit) {
  val pulse = rememberInfiniteTransition(label = "todayPulse")
  val s by pulse.animateFloat(
    initialValue = 1f, targetValue = 1.28f,
    animationSpec = infiniteRepeatable(tween(1400), RepeatMode.Reverse), label = "s"
  )
  Box(contentAlignment = Alignment.Center, modifier = Modifier.fillMaxSize()) {
    if (!done) {
      Box(modifier = Modifier.fillMaxSize().scale(s).border(3.dp, Clay.Sun.copy(alpha = 0.55f * (1.4f - s)), CircleShape))
    }
    Box(
      modifier = Modifier
        .fillMaxSize()
        .clip(CircleShape)
        .background(if (done) Clay.Lime else Clay.Sun)
        .border(3.5.dp, Clay.Ink, CircleShape)
        .clickable(onClick = onClick)
        .testTag("btn_start_daily_session"),
      contentAlignment = Alignment.Center
    ) {
      Icon(
        if (done) Icons.Default.Check else Icons.Default.PlayArrow,
        contentDescription = "Entrenar",
        tint = Clay.Ink,
        modifier = Modifier.size(36.dp)
      )
    }
  }
}
