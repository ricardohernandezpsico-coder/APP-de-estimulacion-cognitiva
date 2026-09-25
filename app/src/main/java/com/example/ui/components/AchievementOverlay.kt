package com.example.ui.components

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
import androidx.compose.runtime.rememberCoroutineScope
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
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.data.AchievementDef
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
 * Celebración de un logro conseguido ("¡Logro! Una semana"): más liviana que el ascenso de liga, pero con la
 * misma familia visual. La medalla cae con rebote sobre rayos de su color, destellos, vibración; debajo el nombre,
 * qué se hizo y cuántos logros llevas; "¡Genial!" y "Compartir" (tarjeta con la medalla, [ShareCard]).
 * Si hay varios logros seguidos se muestran de a uno. Atrás = "¡Genial!". Respeta "quitar animaciones".
 */
@Composable
fun AchievementOverlay(
  def: AchievementDef,
  unlockedCount: Int,
  totalCount: Int,
  streak: Int,
  onDismiss: () -> Unit,
  delayMs: Long = 1700L
) {
  val context = LocalContext.current
  val haptics = LocalHapticFeedback.current
  val scope = rememberCoroutineScope()
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }
  var visible by remember(def.id) { mutableStateOf(reduceMotion || delayMs <= 0L) }
  LaunchedEffect(def.id) {
    if (!visible) {
      delay(delayMs)
      visible = true
    }
  }
  if (!visible) return

  BackHandler(onBack = onDismiss)

  val scrim = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  val medal = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  val burst = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  val texts = remember(def.id) { Animatable(if (reduceMotion) 1f else 0f) }
  LaunchedEffect(def.id) {
    if (reduceMotion) return@LaunchedEffect
    scrim.animateTo(1f, tween(240))
    haptics.performHapticFeedback(HapticFeedbackType.LongPress)
    launch { burst.animateTo(1f, tween(1400, easing = LinearOutSlowInEasing)) }
    launch { medal.animateTo(1f, spring(dampingRatio = 0.45f, stiffness = 280f)) }
    delay(300)
    texts.animateTo(1f, tween(320))
  }
  val spin = if (reduceMotion) 0f else {
    val t = rememberInfiniteTransition(label = "achRays")
    val v by t.animateFloat(0f, 360f, infiniteRepeatable(tween(18000, easing = LinearEasing)), label = "achSpin")
    v
  }

  val color = def.medalColor()
  val light = lerp(color, Color.White, 0.45f)
  val sparks = remember(def.id) {
    val rnd = Random(def.id.hashCode())
    List(18) { Triple(rnd.nextFloat() * 2f * PI.toFloat(), 0.45f + rnd.nextFloat() * 0.55f, 7f + rnd.nextFloat() * 10f) }
  }
  val sparkColors = listOf(light, Clay.Sun, Clay.Sky, Clay.Coral, Clay.Grape, Color.White)

  Box(
    modifier = Modifier
      .fillMaxSize()
      .alpha(scrim.value)
      .background(Color(0xF204061C))
      .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null) {}
      .testTag("achievement_overlay"),
    contentAlignment = Alignment.Center
  ) {
    Column(
      modifier = Modifier.fillMaxWidth().padding(horizontal = 28.dp),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      Text(
        text = "¡LOGRO!",
        fontFamily = FredokaFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 16.sp,
        letterSpacing = 3.sp,
        color = Clay.Sun
      )
      Box(modifier = Modifier.size(270.dp), contentAlignment = Alignment.Center) {
        Canvas(Modifier.fillMaxSize()) {
          val c = center
          val reach = size.minDimension / 2f
          val a = medal.value.coerceIn(0f, 1f)
          drawCircle(Brush.radialGradient(listOf(color.copy(alpha = 0.45f * a), Color.Transparent), c, reach), reach, c)
          for (i in 0 until 10) {
            val ang = (spin + i * 36f) * PI.toFloat() / 180f
            val p = Path().apply {
              moveTo(c.x, c.y)
              lineTo(c.x + cos(ang - 0.1f) * reach, c.y + sin(ang - 0.1f) * reach)
              lineTo(c.x + cos(ang + 0.1f) * reach, c.y + sin(ang + 0.1f) * reach)
              close()
            }
            drawPath(p, Brush.radialGradient(listOf(light.copy(alpha = 0.30f * a), Color.Transparent), c, reach))
          }
          val b = burst.value
          if (b > 0f && b < 1f) {
            sparks.forEachIndexed { i, (ang, speed, s) ->
              val d = reach * speed * (1f - (1f - b) * (1f - b))
              drawFourPointStar(
                Offset(c.x + cos(ang) * d, c.y + sin(ang) * d),
                s.dp.toPx() * (1f - b * 0.5f),
                sparkColors[i % sparkColors.size].copy(alpha = 1f - b)
              )
            }
          }
        }
        AchievementMedal(
          def = def,
          unlocked = true,
          size = 170.dp,
          modifier = Modifier.graphicsLayer {
            val k = medal.value
            scaleX = k
            scaleY = k
            alpha = k.coerceIn(0f, 1f)
            translationY = (1f - k.coerceIn(0f, 1f)) * -120f
          }
        )
      }
      Column(
        modifier = Modifier.alpha(texts.value),
        horizontalAlignment = Alignment.CenterHorizontally
      ) {
        Text(
          text = def.title,
          style = TextStyle(
            fontFamily = FredokaFamily, fontWeight = FontWeight.Bold, fontSize = 36.sp, lineHeight = 40.sp,
            color = light, textAlign = TextAlign.Center, shadow = Shadow(Clay.Ink, Offset(0f, 8f), 0f)
          )
        )
        Text(
          text = def.description,
          fontFamily = FredokaFamily,
          fontWeight = FontWeight.SemiBold,
          fontSize = 18.sp,
          color = Color.White,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(top = 4.dp)
        )
        Text(
          text = "$unlockedCount de $totalCount logros",
          fontFamily = FredokaFamily,
          fontSize = 15.sp,
          color = Color(0xFFB4BFEA),
          modifier = Modifier.padding(top = 10.dp)
        )
        Spacer(Modifier.height(28.dp))
        ClayButton(text = "¡Genial!", onClick = onDismiss, modifier = Modifier.testTag("btn_achievement_ok"))
        Spacer(Modifier.height(14.dp))
        ClayButton(
          text = "Compartir",
          color = Clay.Cream,
          onClick = {
            scope.launch {
              try {
                ShareCard.share(
                  context,
                  ShareCard.Content(
                    headline = def.title,
                    subtitle = def.description,
                    achievement = def,
                    stats = buildList {
                      add("$unlockedCount" to "logros")
                      if (streak > 0) add("$streak" to if (streak == 1) "día de racha" else "días de racha")
                    }
                  ),
                  "¡Conseguí el logro \"${def.title}\" en NeuroVida!"
                )
              } catch (e: Exception) {
                android.util.Log.e("AchievementOverlay", "No se pudo compartir el logro", e)
              }
            }
          }
        )
      }
    }
  }
}
