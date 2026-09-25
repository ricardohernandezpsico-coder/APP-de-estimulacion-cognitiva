package com.example.ui.components

import android.content.Intent
import android.provider.Settings
import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shadow
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameRegistry
import com.example.model.LeaguePromotion
import com.example.model.RankTier
import com.example.ui.theme.Clay
import com.example.ui.theme.ClayButton
import com.example.ui.theme.FredokaFamily
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

/**
 * Celebración de ascenso de liga ("¡Subiste a Plata!"), encima de la pantalla de resultado. Es el momento de
 * recompensa más fuerte de la app, así que va a pantalla completa y con una sola idea: el escudo viejo tiembla y
 * estalla, el nuevo aparece con rebote sobre rayos de luz del color de la liga y una lluvia de destellos.
 * Debajo: la liga nueva, dónde (liga general o de un juego), trofeos y la próxima meta; "¡Genial!" y "Compartir"
 * (hoja de compartir de Android con un texto corto).
 *
 * Aparece después de [delayMs] para que primero se vea el puntaje de la partida. Atrás = "¡Genial!". Con
 * "quitar animaciones" del sistema aparece quieta, ya con el escudo nuevo.
 */
@Composable
fun LeaguePromotionOverlay(
  promotion: LeaguePromotion,
  onDismiss: () -> Unit,
  delayMs: Long = 1700L
) {
  val context = LocalContext.current
  val haptics = LocalHapticFeedback.current
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }
  var visible by remember(promotion) { mutableStateOf(reduceMotion) }
  LaunchedEffect(promotion) {
    if (!reduceMotion) {
      delay(delayMs)
      visible = true
    }
  }
  if (!visible) return

  BackHandler(onBack = onDismiss)

  val scrim = remember(promotion) { Animatable(if (reduceMotion) 1f else 0f) }
  val shake = remember(promotion) { Animatable(0f) }            // 0..1: el escudo viejo tiembla
  val oldGone = remember(promotion) { Animatable(if (reduceMotion) 1f else 0f) }  // 0..1: estalla y se va
  val newIn = remember(promotion) { Animatable(if (reduceMotion) 1f else 0f) }    // 0..1 (con rebote)
  val flash = remember(promotion) { Animatable(0f) }
  val burst = remember(promotion) { Animatable(if (reduceMotion) 1f else 0f) }
  val texts = remember(promotion) { Animatable(if (reduceMotion) 1f else 0f) }

  LaunchedEffect(promotion) {
    if (reduceMotion) return@LaunchedEffect
    scrim.animateTo(1f, tween(280))
    delay(250)
    shake.animateTo(1f, tween(520, easing = LinearEasing))
    oldGone.animateTo(1f, tween(160))
    haptics.performHapticFeedback(HapticFeedbackType.LongPress)
    launch { flash.animateTo(1f, tween(90)); flash.animateTo(0f, tween(420)) }
    launch { burst.animateTo(1f, tween(1500, easing = LinearOutSlowInEasing)) }
    launch { newIn.animateTo(1f, spring(dampingRatio = 0.42f, stiffness = 260f)) }
    delay(380)
    texts.animateTo(1f, tween(360))
  }

  val spin = if (reduceMotion) 0f else {
    val t = rememberInfiniteTransition(label = "rays")
    val v by t.animateFloat(0f, 360f, infiniteRepeatable(tween(16000, easing = LinearEasing)), label = "raysSpin")
    v
  }

  val pal = promotion.tier.palette()
  val next = RankTier.entries.getOrNull(promotion.tier.ordinal + 1)
  val game = promotion.gameId?.let { GameRegistry.getById(it) }
  val sparks = remember(promotion) {
    val rnd = Random(promotion.rating)
    List(22) { Triple(rnd.nextFloat() * 2f * PI.toFloat(), 0.45f + rnd.nextFloat() * 0.55f, 8f + rnd.nextFloat() * 12f) }
  }
  val sparkColors = listOf(pal.light, Clay.Sun, Clay.Sky, Clay.Coral, Clay.Grape, Color.White)

  Box(
    modifier = Modifier
      .fillMaxSize()
      .alpha(scrim.value)
      .background(Color(0xF204061C))
      // Tapa la pantalla de resultado: sus botones no reciben toques mientras se celebra.
      .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null) {}
      .testTag("league_promotion"),
    contentAlignment = Alignment.Center
  ) {
    Column(
      modifier = Modifier
        .fillMaxWidth()
        .padding(horizontal = 28.dp),
      horizontalAlignment = Alignment.CenterHorizontally,
      verticalArrangement = Arrangement.Center
    ) {
      Text(
        text = "¡ASCENSO!",
        fontFamily = FredokaFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 16.sp,
        letterSpacing = 3.sp,
        color = Clay.Sun,
        modifier = Modifier.alpha(0.4f + 0.6f * scrim.value)
      )
      Spacer(Modifier.height(8.dp))

      Box(modifier = Modifier.size(300.dp), contentAlignment = Alignment.Center) {
        // Rayos de luz del color de la liga (giran lento) + resplandor + destellos que salen del centro.
        Canvas(Modifier.fillMaxSize()) {
          val c = center
          val reach = size.minDimension / 2f
          val a = newIn.value.coerceIn(0f, 1f)
          if (a > 0f) {
            drawCircle(
              Brush.radialGradient(listOf(pal.glow.copy(alpha = 0.55f * a), Color.Transparent), c, reach),
              radius = reach, center = c
            )
            val rays = 12
            for (i in 0 until rays) {
              val ang = (spin + i * 360f / rays) * PI.toFloat() / 180f
              val half = 0.09f
              val p = Path().apply {
                moveTo(c.x, c.y)
                lineTo(c.x + cos(ang - half) * reach, c.y + sin(ang - half) * reach)
                lineTo(c.x + cos(ang + half) * reach, c.y + sin(ang + half) * reach)
                close()
              }
              drawPath(
                p,
                Brush.radialGradient(listOf(pal.light.copy(alpha = 0.34f * a), Color.Transparent), c, reach)
              )
            }
          }
          val b = burst.value
          if (b > 0f && b < 1f) {
            sparks.forEachIndexed { i, (ang, speed, s) ->
              val d = reach * speed * (1f - (1f - b) * (1f - b))
              val p = Offset(c.x + cos(ang) * d, c.y + sin(ang) * d)
              val r = s.dp.toPx() * (1f - b * 0.5f)
              drawFourPointStar(p, r, sparkColors[i % sparkColors.size].copy(alpha = 1f - b))
            }
          }
          if (flash.value > 0f) {
            drawCircle(Color.White.copy(alpha = 0.75f * flash.value), radius = reach * 0.7f, center = c)
          }
        }

        // Escudo viejo: tiembla y estalla.
        if (oldGone.value < 1f) {
          val s = shake.value
          LeagueShield(
            tier = promotion.previous,
            size = 150.dp,
            pips = 5,
            modifier = Modifier.graphicsLayer {
              rotationZ = sin(s * 40f) * 7f * s
              val k = 1f + 0.1f * s + 0.4f * oldGone.value
              scaleX = k
              scaleY = k
              alpha = 1f - oldGone.value
            }
          )
        }
        // Escudo nuevo: aparece con rebote.
        LeagueShield(
          tier = promotion.tier,
          size = 170.dp,
          pips = 1,
          glow = true,
          modifier = Modifier.graphicsLayer {
            val k = newIn.value
            scaleX = k
            scaleY = k
            alpha = k.coerceIn(0f, 1f)
          }
        )
      }

      Column(
        modifier = Modifier.alpha(texts.value).graphicsLayer { translationY = (1f - texts.value) * 24f },
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text(
          text = "Subiste a ${promotion.tier.tierName}",
          style = TextStyle(
            fontFamily = FredokaFamily,
            fontWeight = FontWeight.Bold,
            fontSize = 38.sp,
            lineHeight = 42.sp,
            color = pal.light,
            textAlign = TextAlign.Center,
            shadow = Shadow(Clay.Ink, Offset(0f, 8f), 0f)
          )
        )
        Text(
          text = if (game != null) "en ${game.title}" else "Tu liga general",
          fontFamily = FredokaFamily,
          fontWeight = FontWeight.SemiBold,
          fontSize = 18.sp,
          color = Color.White,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(top = 4.dp)
        )
        Text(
          text = buildString {
            append("${promotion.rating} trofeos")
            if (next != null) append("  ·  próxima: ${next.tierName} (${next.minRating})")
            else append("  ·  la liga más alta")
          },
          fontFamily = FredokaFamily,
          fontSize = 15.sp,
          color = Color(0xFFB4BFEA),
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(top = 10.dp)
        )

        Spacer(Modifier.height(30.dp))
        ClayButton(text = "¡Genial!", onClick = onDismiss, modifier = Modifier.testTag("btn_promotion_ok"))
        Spacer(Modifier.height(14.dp))
        ClayButton(
          text = "Compartir",
          color = Clay.Cream,
          onClick = {
            val where = if (game != null) " en ${game.title}" else ""
            val send = Intent(Intent.ACTION_SEND)
              .setType("text/plain")
              .putExtra(Intent.EXTRA_TEXT, "¡Subí a la liga ${promotion.tier.tierName}$where en NeuroVida!")
            context.startActivity(Intent.createChooser(send, "Compartir"))
          }
        )
      }
    }
  }
}

private fun androidx.compose.ui.graphics.drawscope.DrawScope.drawFourPointStar(c: Offset, r: Float, color: Color) {
  val p = Path().apply {
    moveTo(c.x, c.y - r)
    quadraticBezierTo(c.x, c.y, c.x + r, c.y)
    quadraticBezierTo(c.x, c.y, c.x, c.y + r)
    quadraticBezierTo(c.x, c.y, c.x - r, c.y)
    quadraticBezierTo(c.x, c.y, c.x, c.y - r)
    close()
  }
  drawPath(p, color)
}
