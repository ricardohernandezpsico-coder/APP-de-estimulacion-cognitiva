package com.example.games

import android.provider.Settings
import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GamePlayResult
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.model.LevelTier
import com.example.ui.components.LeagueShield
import com.example.ui.components.pipCount
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.ClayPill
import com.example.ui.theme.FredokaFamily
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

private val TextSoft = Color(0xFFB4BFEA) // secundario sobre el cielo nocturno (contraste > 7:1)

/**
 * Pantalla de resultado de la app, con el sello "noche + arcilla" (rediseño 25-sep): va sobre el cielo
 * nocturno de la app (`CosmosBackground`, visible porque el fondo del tema es transparente), sin tarjetas: la
 * información es texto suelto y objetos, lo tocable es arcilla.
 *
 * Un solo protagonista animado: el puntaje cuenta hasta su valor, luego aparecen las estrellas con rebote y, si
 * te fue bien (2-3 estrellas), una lluvia de destellos. Con "quitar animaciones" del sistema todo aparece quieto.
 * Atrás = Continuar (comportamiento predecible). Sin promesas de salud en los textos.
 */
@Composable
fun GameResultScreen(
  result: GamePlayResult,
  didLevelUp: Boolean,
  isDailyFlow: Boolean,
  dailyCompletedCount: Int,
  dailyTotalCount: Int = 3,
  onPlayAgain: () -> Unit,
  onContinue: () -> Unit,
  modifier: Modifier = Modifier,
  rank: GameRankInfo? = null
) {
  val gameDef = GameRegistry.getById(result.gameId)
  val domainColor = gameDef?.domain?.color ?: Clay.Sky
  val stars = when {
    result.score >= 90 -> 3
    result.score >= 65 -> 2
    result.score >= 40 -> 1
    else -> 0
  }
  val context = LocalContext.current
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }

  BackHandler(onBack = onContinue)

  val shownScore = remember(result) { Animatable(if (reduceMotion) result.score.toFloat() else 0f) }
  val starScale = remember(result) { List(3) { Animatable(if (reduceMotion) 1f else 0f) } }
  val burst = remember(result) { Animatable(0f) }
  val levelPop = remember(result) { Animatable(if (reduceMotion) 1f else 0f) }
  LaunchedEffect(result) {
    if (reduceMotion) return@LaunchedEffect
    shownScore.animateTo(result.score.toFloat(), tween(900, easing = FastOutSlowInEasing))
    starScale.forEachIndexed { i, anim ->
      launch {
        delay(i * 170L)
        anim.animateTo(1f, spring(dampingRatio = 0.42f, stiffness = 320f))
      }
    }
    if (stars >= 2) launch { burst.animateTo(1f, tween(1500, easing = LinearOutSlowInEasing)) }
    delay(3 * 170L)
    levelPop.animateTo(1f, spring(dampingRatio = 0.5f, stiffness = 300f))
  }

  val halo = if (reduceMotion) 1f else {
    val t = rememberInfiniteTransition(label = "halo")
    val v by t.animateFloat(0.92f, 1.08f, infiniteRepeatable(tween(2200), RepeatMode.Reverse), label = "haloScale")
    v
  }

  Column(
    modifier = modifier
      .fillMaxSize()
      .verticalScroll(rememberScrollState())
      .padding(horizontal = 24.dp)
      .padding(top = 28.dp, bottom = 32.dp),
    horizontalAlignment = Alignment.CenterHorizontally
  ) {
    // Juego y dominio, como texto suelto.
    Text(
      text = gameDef?.title ?: "Partida terminada",
      color = Clay.Cream,
      fontFamily = FredokaFamily,
      fontWeight = FontWeight.Bold,
      fontSize = 26.sp,
      textAlign = TextAlign.Center
    )
    if (gameDef != null) {
      Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 4.dp)) {
        Canvas(Modifier.size(8.dp)) { drawCircle(domainColor) }
        Spacer(Modifier.width(6.dp))
        Text(gameDef.domain.displayName, color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      }
    }

    // Protagonista: halo del dominio, destellos, estrellas y el puntaje gigante de arcilla.
    Box(
      modifier = Modifier
        .fillMaxWidth()
        .height(250.dp),
      contentAlignment = Alignment.Center
    ) {
      Canvas(Modifier.fillMaxSize()) {
        val c = Offset(size.width / 2f, size.height * 0.58f)
        drawCircle(
          Brush.radialGradient(listOf(domainColor.copy(alpha = 0.40f), Color.Transparent), c, size.minDimension * 0.55f * halo),
          radius = size.minDimension * 0.55f * halo,
          center = c
        )
        if (burst.value > 0f && burst.value < 1f) drawBurst(c, burst.value, size.minDimension * 0.62f)
      }
      Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalAlignment = Alignment.Bottom) {
          for (i in 0 until 3) {
            val big = i == 1
            ResultStar(
              earned = i < stars,
              modifier = Modifier
                .size(if (big) 58.dp else 46.dp)
                .offset(y = if (big) (-8).dp else 0.dp)
                .scale(starScale[i].value)
            )
          }
        }
        Spacer(Modifier.height(4.dp))
        ClayNumber(text = shownScore.value.toInt().toString(), color = Clay.Sun, fontSize = 104.sp)
        Text("puntos", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
      }
    }

    Text(
      text = when {
        result.score >= 90 -> "¡Extraordinario!"
        result.score >= 70 -> "¡Muy bien!"
        result.score >= 50 -> "Buen trabajo"
        else -> "Sigue practicando"
      },
      color = Clay.Cream,
      fontFamily = FredokaFamily,
      fontWeight = FontWeight.Bold,
      fontSize = 28.sp,
      textAlign = TextAlign.Center
    )
    Text(
      text = when {
        result.score >= 85 -> "Precisión y ritmo excelentes."
        result.score >= 60 -> "Buen equilibrio entre precisión y velocidad."
        else -> "La dificultad se ajusta a tu ritmo en cada partida."
      },
      color = TextSoft,
      fontSize = 16.sp,
      textAlign = TextAlign.Center,
      modifier = Modifier.padding(top = 4.dp)
    )

    // Datos de la partida, sueltos en una línea.
    Spacer(Modifier.height(18.dp))
    Row(verticalAlignment = Alignment.CenterVertically) {
      Stat("${result.correctAnswers}/${result.totalTrials}", "aciertos")
      Dot()
      Stat("Nivel ${result.level}", LevelTier.fromLevel(result.level).tierName)
      Dot()
      Stat(if (result.timed) "Reto" else "Precisión", "modo")
    }

    if (didLevelUp) {
      Spacer(Modifier.height(18.dp))
      ClayPill(
        text = if (result.level >= 5) "¡Tu maestría sube!" else "¡Subes al nivel ${result.level + 1}!",
        color = Clay.Lime,
        modifier = Modifier.scale(levelPop.value)
      )
    }

    if (rank != null) {
      Spacer(Modifier.height(18.dp))
      Row(verticalAlignment = Alignment.CenterVertically) {
        LeagueShield(tier = rank.tier, size = 40.dp, pips = rank.pipCount())
        Spacer(Modifier.width(10.dp))
        Column {
          Text(rank.label, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 17.sp)
          Text("${rank.rating} trofeos en este juego", color = TextSoft, fontSize = 14.sp)
        }
      }
    }

    if (isDailyFlow) {
      Spacer(Modifier.height(20.dp))
      DailyProgress(completed = dailyCompletedCount, total = dailyTotalCount)
    }

    Spacer(Modifier.height(28.dp))
    val moreToday = isDailyFlow && dailyCompletedCount < dailyTotalCount
    ClayButton(
      text = if (moreToday) "Siguiente juego (${dailyCompletedCount + 1} de $dailyTotalCount)" else "Continuar",
      onClick = onContinue,
      icon = Icons.AutoMirrored.Filled.ArrowForward,
      modifier = Modifier.testTag("btn_result_continue")
    )
    Spacer(Modifier.height(14.dp))
    ClayButton(
      text = "Jugar de nuevo",
      onClick = onPlayAgain,
      color = Clay.Cream,
      icon = Icons.Filled.Refresh,
      modifier = Modifier.testTag("btn_result_replay")
    )
  }
}

/** Número "de arcilla": relleno de color, contorno tinta grueso y sombra dura tinta (como en los juegos). */
@Composable
private fun ClayNumber(text: String, color: Color, fontSize: TextUnit) {
  val base = TextStyle(fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = fontSize)
  val stroke = Stroke(width = 16f, join = StrokeJoin.Round)
  Box {
    Text(text, style = base.copy(color = Clay.Ink, drawStyle = stroke), modifier = Modifier.offset(y = 6.dp))
    Text(text, style = base.copy(color = Clay.Ink, drawStyle = stroke))
    Text(text, style = base.copy(color = color))
  }
}

/** Estrella de 5 puntas de arcilla: sol con borde tinta si se ganó, contorno tenue si no. */
@Composable
private fun ResultStar(earned: Boolean, modifier: Modifier = Modifier) {
  Canvas(modifier) {
    val path = starPath(size.width / 2f, size.height / 2f, size.minDimension * 0.48f, size.minDimension * 0.21f, 5)
    if (earned) {
      drawPath(path, Clay.Ink, style = Stroke(width = size.minDimension * 0.12f, join = StrokeJoin.Round))
      drawPath(path, Clay.Sun)
      // Brillo arriba a la izquierda.
      drawCircle(Color.White.copy(alpha = 0.55f), radius = size.minDimension * 0.07f, center = Offset(size.width * 0.40f, size.height * 0.36f))
    } else {
      drawPath(path, Color.White.copy(alpha = 0.10f))
      drawPath(path, Color.White.copy(alpha = 0.30f), style = Stroke(width = size.minDimension * 0.05f, join = StrokeJoin.Round))
    }
  }
}

private fun starPath(cx: Float, cy: Float, outer: Float, inner: Float, points: Int): Path = Path().apply {
  for (i in 0 until points * 2) {
    val r = if (i % 2 == 0) outer else inner
    val a = -PI / 2 + i * PI / points
    val x = cx + (r * cos(a)).toFloat()
    val y = cy + (r * sin(a)).toFloat()
    if (i == 0) moveTo(x, y) else lineTo(x, y)
  }
  close()
}

private val BurstColors = listOf(Clay.Sun, Clay.Coral, Clay.Sky, Clay.Grape, Clay.Lime, Color.White)

/** Lluvia de destellos de 4 puntas que salen del centro y se apagan (celebración de 2-3 estrellas). */
private fun DrawScope.drawBurst(center: Offset, progress: Float, reach: Float) {
  val rnd = Random(7)
  val fade = 1f - progress
  repeat(26) { i ->
    val angle = rnd.nextFloat() * 2f * PI.toFloat()
    val speed = 0.45f + rnd.nextFloat() * 0.55f
    val d = reach * speed * (1f - (1f - progress) * (1f - progress)) // sale rápido y frena
    val p = Offset(center.x + cos(angle) * d, center.y + sin(angle) * d)
    val s = (10f + rnd.nextFloat() * 16f) * (1f - progress * 0.5f)
    val color = BurstColors[i % BurstColors.size].copy(alpha = fade)
    val sparkle = Path().apply {
      moveTo(p.x, p.y - s); lineTo(p.x + s * 0.22f, p.y - s * 0.22f)
      lineTo(p.x + s, p.y); lineTo(p.x + s * 0.22f, p.y + s * 0.22f)
      lineTo(p.x, p.y + s); lineTo(p.x - s * 0.22f, p.y + s * 0.22f)
      lineTo(p.x - s, p.y); lineTo(p.x - s * 0.22f, p.y - s * 0.22f)
      close()
    }
    drawPath(sparkle, color)
  }
}

@Composable
private fun Stat(value: String, label: String) {
  Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.padding(horizontal = 6.dp)) {
    Text(value, color = Clay.Cream, fontWeight = FontWeight.Bold, fontSize = 20.sp, fontFamily = FredokaFamily)
    Text(label, color = TextSoft, fontSize = 14.sp)
  }
}

@Composable
private fun Dot() {
  Canvas(Modifier.padding(horizontal = 8.dp).size(5.dp)) { drawCircle(TextSoft.copy(alpha = 0.6f)) }
}

/** Avance de la sesión diaria como nodos de arcilla (como el camino de Hoy): hechos en coral con tilde. */
@Composable
private fun DailyProgress(completed: Int, total: Int) {
  Column(horizontalAlignment = Alignment.CenterHorizontally) {
    Text("Sesión de hoy", color = TextSoft, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
    Spacer(Modifier.height(8.dp))
    Row(verticalAlignment = Alignment.CenterVertically) {
      for (i in 0 until total) {
        if (i > 0) {
          Canvas(Modifier.width(26.dp).height(4.dp)) {
            drawLine(
              if (i <= completed) Clay.Coral else Color.White.copy(alpha = 0.18f),
              Offset(0f, size.height / 2), Offset(size.width, size.height / 2), strokeWidth = size.height
            )
          }
        }
        Canvas(Modifier.size(30.dp)) {
          val r = size.minDimension / 2f
          if (i < completed) {
            drawCircle(Clay.Ink, radius = r, center = Offset(r, r + 2.dp.toPx()))
            drawCircle(Clay.Ink, radius = r)
            drawCircle(Clay.Coral, radius = r - 3.dp.toPx())
            val check = Path().apply {
              moveTo(r * 0.55f, r * 1.02f); lineTo(r * 0.88f, r * 1.34f); lineTo(r * 1.45f, r * 0.70f)
            }
            drawPath(check, Clay.Ink, style = Stroke(width = 3.dp.toPx(), join = StrokeJoin.Round))
          } else {
            drawCircle(Color.White.copy(alpha = 0.30f), radius = r - 1.dp.toPx(), style = Stroke(width = 2.dp.toPx()))
          }
        }
      }
    }
  }
}
