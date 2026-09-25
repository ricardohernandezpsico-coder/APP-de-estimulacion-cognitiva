package com.example.ui.components

import android.provider.Settings
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
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
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.GameDefinition
import com.example.model.LevelTier
import com.example.ui.theme.Clay
import com.example.ui.theme.FredokaFamily
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin

/**
 * Pantalla de espera mientras arranca un juego de Unity, con el sello "noche + arcilla": el planeta del juego
 * (el mismo de la biblioteca) con una luna de arcilla que lo orbita (la órbita ES el indicador de carga), el
 * nombre, nivel y modo, y "Cómo se juega" para que la espera sirva de algo.
 *
 * Se muestra en DOS lugares con el mismo aspecto, para que el paso app -> Unity no se note:
 * - en la app (`UnityGameHost`), sobre el cielo de la app, mientras Android levanta la Activity de Unity;
 * - encima de la Activity de Unity (`bridge/UnityLoadingOverlay`, con [withSky] = true) mientras el motor arranca
 *   en frío, hasta que el juego pinta su primer cuadro (antes: pantalla negra o el indicador genérico 5-8 s).
 *
 * [fadeInDelayMs]: el contenido aparece con un fundido corto después de esa espera. En la app se usa una espera
 * breve para que un arranque en caliente (Unity ya vivo, ~1 s) no destelle texto que nadie alcanza a leer.
 * Con "quitar animaciones" del sistema, todo queda quieto y visible.
 */
@Composable
fun GameLoadingScreen(
  game: GameDefinition,
  level: Int?,
  timed: Boolean?,
  modifier: Modifier = Modifier,
  withSky: Boolean = false,
  fadeInDelayMs: Long = 0L
) {
  val context = LocalContext.current
  val reduceMotion = remember {
    Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) == 0f
  }
  val appear = remember { Animatable(if (reduceMotion || fadeInDelayMs <= 0L) 1f else 0f) }
  LaunchedEffect(Unit) {
    if (appear.value >= 1f) return@LaunchedEffect
    kotlinx.coroutines.delay(fadeInDelayMs)
    appear.animateTo(1f, tween(320))
  }

  val transition = rememberInfiniteTransition(label = "loading")
  val orbit by transition.animateFloat(
    initialValue = 0f, targetValue = 1f,
    animationSpec = infiniteRepeatable(tween(2600, easing = LinearEasing)), label = "orbit"
  )
  val breathe by transition.animateFloat(
    initialValue = 0f, targetValue = 1f,
    animationSpec = infiniteRepeatable(tween(1800), RepeatMode.Reverse), label = "breathe"
  )
  val dots by transition.animateFloat(
    initialValue = 0f, targetValue = 3.999f,
    animationSpec = infiniteRepeatable(tween(1400, easing = LinearEasing)), label = "dots"
  )
  val angle = if (reduceMotion) 0.62f else orbit
  val pulse = if (reduceMotion) 0.5f else breathe

  Box(modifier = modifier.fillMaxSize()) {
    if (withSky) CosmosBackground()
    Column(
      modifier = Modifier
        .fillMaxSize()
        .alpha(appear.value)
        .padding(horizontal = 32.dp),
      horizontalAlignment = Alignment.CenterHorizontally,
      verticalArrangement = Arrangement.Center
    ) {
      LoadingPlanet(game = game, angle = angle, pulse = pulse)

      Spacer(Modifier.height(18.dp))
      Text(
        text = game.title,
        fontFamily = FredokaFamily,
        fontWeight = FontWeight.Bold,
        fontSize = 32.sp,
        lineHeight = 36.sp,
        color = Color.White,
        textAlign = TextAlign.Center
      )
      val details = buildList {
        if (level != null) add("Nivel $level · ${LevelTier.fromLevel(level).tierName}")
        if (timed != null) add(if (timed) "Reto" else "Precisión")
      }
      if (details.isNotEmpty()) {
        Text(
          text = details.joinToString("  ·  "),
          fontFamily = FredokaFamily,
          fontWeight = FontWeight.SemiBold,
          fontSize = 16.sp,
          color = Clay.Sun,
          textAlign = TextAlign.Center,
          modifier = Modifier.padding(top = 6.dp)
        )
      }

      Spacer(Modifier.height(34.dp))
      Text(
        text = "Cómo se juega",
        fontFamily = FredokaFamily,
        fontWeight = FontWeight.SemiBold,
        fontSize = 13.sp,
        letterSpacing = 1.5.sp,
        color = Color(0xFFB4BFEA)
      )
      Text(
        text = game.instruction,
        fontFamily = FredokaFamily,
        fontSize = 18.sp,
        lineHeight = 26.sp,
        color = Color.White.copy(alpha = 0.92f),
        textAlign = TextAlign.Center,
        modifier = Modifier
          .fillMaxWidth()
          .padding(top = 8.dp)
      )

      Spacer(Modifier.height(40.dp))
      val shown = if (reduceMotion) 3 else dots.toInt()
      Text(
        // Los puntos que faltan van transparentes: el texto no "baila" al cambiar de largo.
        text = androidx.compose.ui.text.buildAnnotatedString {
          append("Preparando el juego")
          for (i in 0 until 3) {
            pushStyle(androidx.compose.ui.text.SpanStyle(color = if (i < shown) Color.Unspecified else Color.Transparent))
            append(".")
            pop()
          }
        },
        fontFamily = FredokaFamily,
        fontSize = 15.sp,
        color = Color.White.copy(alpha = 0.62f)
      )
    }
  }
}

/**
 * El planeta del juego (esfera de arcilla del color del dominio, como en la biblioteca, pero grande) con una
 * luna sol que lo orbita en una elipse inclinada: pasa por DETRÁS del planeta en la mitad de arriba y por
 * DELANTE en la de abajo (más grande), así se lee como 3D.
 */
@Composable
private fun LoadingPlanet(game: GameDefinition, angle: Float, pulse: Float) {
  val ink = Clay.Ink
  val color = game.domain.color
  val planet = 132.dp
  Box(modifier = Modifier.size(260.dp, 220.dp), contentAlignment = Alignment.Center) {
    // Detrás: halo que respira, mitad lejana de la órbita y la luna si va por detrás.
    Canvas(Modifier.fillMaxSize()) {
      val r = planet.toPx() / 2f
      drawCircle(
        Brush.radialGradient(
          listOf(color.copy(alpha = 0.30f + 0.14f * pulse), Color.Transparent),
          center, r * (1.95f + 0.12f * pulse)
        ),
        radius = r * (1.95f + 0.12f * pulse)
      )
      drawOrbit(back = true)
      if (moonBehind(angle)) drawMoon(angle, ink)
    }
    // Planeta: sombra dura + esfera con borde tinta + brillo + ícono.
    Canvas(Modifier.size(planet + 8.dp)) {
      val r = planet.toPx() / 2f
      drawCircle(ink, radius = r, center = Offset(center.x, center.y + 6.dp.toPx()))
      drawCircle(color, radius = r, center = center)
      drawCircle(Color.White.copy(alpha = 0.18f), radius = r * 0.78f, center = Offset(center.x - r * 0.18f, center.y - r * 0.2f))
      drawCircle(ink, radius = r - 1.5.dp.toPx(), center = center, style = Stroke(3.5.dp.toPx()))
      drawOval(
        Color.White.copy(alpha = 0.34f),
        topLeft = Offset(center.x - r * 0.42f, center.y - r * 0.8f),
        size = Size(r * 0.84f, r * 0.26f)
      )
    }
    Text(game.iconEmoji, fontSize = 54.sp, modifier = Modifier.scale(1f + 0.03f * pulse))
    // Delante: mitad cercana de la órbita y la luna si va por delante.
    Canvas(Modifier.fillMaxSize()) {
      drawOrbit(back = false)
      if (!moonBehind(angle)) drawMoon(angle, ink)
    }
  }
}

// Elipse de la órbita, en proporción al lienzo (260x220 dp).
private fun DrawScope.orbitRx() = size.width * 0.46f
private fun DrawScope.orbitRy() = size.height * 0.17f
private const val OrbitTilt = -0.28f // radianes: la órbita va un poco inclinada

private fun moonBehind(angle: Float): Boolean = sin(angle * 2f * PI.toFloat()) < 0f

private fun DrawScope.orbitPoint(t: Float): Offset {
  val a = t * 2f * PI.toFloat()
  val x = cos(a) * orbitRx()
  val y = sin(a) * orbitRy()
  return Offset(
    center.x + x * cos(OrbitTilt) - y * sin(OrbitTilt),
    center.y + x * sin(OrbitTilt) + y * cos(OrbitTilt)
  )
}

private fun DrawScope.drawOrbit(back: Boolean) {
  // Mitad de atrás = sin(a) < 0 (arriba); mitad de adelante = sin(a) >= 0. Se dibuja por tramos cortos.
  val steps = 48
  val stroke = 2.dp.toPx()
  for (i in 0 until steps) {
    val t0 = i / steps.toFloat()
    val t1 = (i + 1) / steps.toFloat()
    val mid = sin((t0 + t1) * PI.toFloat())
    if ((mid < 0f) != back) continue
    drawLine(Color.White.copy(alpha = if (back) 0.12f else 0.26f), orbitPoint(t0), orbitPoint(t1), stroke)
  }
}

private fun DrawScope.drawMoon(angle: Float, ink: Color) {
  val p = orbitPoint(angle)
  // Más grande adelante, más chica atrás.
  val depth = (sin(angle * 2f * PI.toFloat()) + 1f) / 2f
  val r = (9f + 5f * depth).dp.toPx()
  drawCircle(ink, radius = r, center = Offset(p.x, p.y + 2.5.dp.toPx()))
  drawCircle(Clay.Sun, radius = r, center = p)
  drawCircle(ink, radius = r - 1.dp.toPx(), center = p, style = Stroke(2.5.dp.toPx()))
  drawCircle(Color.White.copy(alpha = 0.55f), radius = r * 0.28f, center = Offset(p.x - r * 0.3f, p.y - r * 0.32f))
}
