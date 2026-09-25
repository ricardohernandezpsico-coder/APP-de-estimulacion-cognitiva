package com.example.ui.theme

import androidx.compose.animation.core.animateDpAsState
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/**
 * Lenguaje visual "noche + arcilla" de NeuroVida (ver NeuroVida/CLAUDE.md): fondo nocturno con estrellas
 * ([com.example.ui.components.CosmosBackground]) y, encima, todo lo tocable en arcilla: bordes gruesos color
 * tinta, sombra dura abajo y colores saturados. Es el mismo lenguaje de los juegos de Unity.
 */
object Clay {
  val Ink = Color(0xFF1A1240)
  val Coral = Color(0xFFFF6B4A)
  val Sun = Color(0xFFFFC93C)
  val Sky = Color(0xFF4CC9F0)
  val Grape = Color(0xFFB8A4FF)
  val Lime = Color(0xFF9BE564)
  val Cream = Color(0xFFFFFFFF)
  val InkSoft = Color(0xFF6B6790)
  val Border = 3.dp
}

/** Tarjeta de arcilla: borde grueso, sombra dura abajo y "hundimiento" al presionar (si es clicable). */
@Composable
fun ClayCard(
  modifier: Modifier = Modifier,
  color: Color = Clay.Cream,
  radius: Dp = 24.dp,
  depth: Dp = 5.dp,
  onClick: (() -> Unit)? = null,
  contentPadding: Dp = 16.dp,
  content: @Composable ColumnScope.() -> Unit
) {
  val shape = RoundedCornerShape(radius)
  val source = remember { MutableInteractionSource() }
  val pressed by source.collectIsPressedAsState()
  val press by animateDpAsState(if (pressed && onClick != null) depth else 0.dp, label = "clayPress")
  val click = if (onClick != null) Modifier.clickable(interactionSource = source, indication = null, role = Role.Button, onClick = onClick) else Modifier
  Box(
    modifier = modifier
      .padding(bottom = depth)
      .offset(y = press)
      .drawBehind {
        val d = (depth - press).toPx()
        drawRoundRect(Clay.Ink, topLeft = Offset(0f, d), size = size, cornerRadius = CornerRadius(radius.toPx()))
      }
      .clip(shape)
      .background(color, shape)
      .border(BorderStroke(Clay.Border, Clay.Ink), shape)
      .then(click)
  ) {
    Column(modifier = Modifier.padding(contentPadding), content = content)
  }
}

/** Boton de arcilla (accion principal por defecto en amarillo sol). */
@Composable
fun ClayButton(
  text: String,
  onClick: () -> Unit,
  modifier: Modifier = Modifier,
  color: Color = Clay.Sun,
  textColor: Color = Clay.Ink,
  icon: ImageVector? = null
) {
  ClayCard(
    modifier = modifier.fillMaxWidth(),
    color = color,
    radius = 20.dp,
    depth = 5.dp,
    onClick = onClick,
    contentPadding = 0.dp
  ) {
    Row(
      modifier = Modifier
        .fillMaxWidth()
        .height(52.dp),
      horizontalArrangement = Arrangement.Center,
      verticalAlignment = Alignment.CenterVertically
    ) {
      if (icon != null) {
        Icon(icon, contentDescription = null, tint = textColor, modifier = Modifier.size(22.dp))
        Spacer(Modifier.width(8.dp))
      }
      Text(text, color = textColor, fontSize = 17.sp, fontWeight = FontWeight.Bold)
    }
  }
}

/** Pastilla pequeña con borde de tinta (rachas, etiquetas, contadores). */
@Composable
fun ClayPill(text: String, modifier: Modifier = Modifier, color: Color = Clay.Sun, textColor: Color = Clay.Ink, icon: ImageVector? = null) {
  val shape = RoundedCornerShape(50)
  Row(
    modifier = modifier
      .clip(shape)
      .background(color, shape)
      .border(BorderStroke(Clay.Border, Clay.Ink), shape)
      .padding(horizontal = 12.dp, vertical = 4.dp),
    verticalAlignment = Alignment.CenterVertically
  ) {
    if (icon != null) {
      Icon(icon, contentDescription = null, tint = textColor, modifier = Modifier.size(16.dp))
      Spacer(Modifier.width(4.dp))
    }
    Text(text, color = textColor, fontSize = 14.sp, fontWeight = FontWeight.Bold)
  }
}

private val ClayLightScheme = androidx.compose.material3.lightColorScheme(
  primary = Color(0xFF3D7BFF),
  onPrimary = Color.White,
  secondary = Clay.Coral,
  background = Color.Transparent,
  onBackground = Clay.Ink,
  surface = Clay.Cream,
  onSurface = Clay.Ink,
  surfaceVariant = Color(0xFFEDEBF7),
  onSurfaceVariant = Clay.InkSoft,
  outline = Color(0xFFB9B5D6),
  outlineVariant = Color(0xFFD9D6EE)
)

/**
 * Tarjeta de arcilla clara para contenido existente que usa los colores del tema (texto oscuro sobre crema):
 * envuelve el contenido en un esquema claro, asi los componentes previos se leen bien sin tocar sus colores.
 */
@Composable
fun ClayLightCard(
  modifier: Modifier = Modifier,
  color: Color = Clay.Cream,
  onClick: (() -> Unit)? = null,
  content: @Composable ColumnScope.() -> Unit
) {
  ClayCard(modifier = modifier, color = color, onClick = onClick, contentPadding = 0.dp) {
    androidx.compose.material3.MaterialTheme(
      colorScheme = ClayLightScheme,
      typography = Typography,
      content = { Column(content = content) }
    )
  }
}
