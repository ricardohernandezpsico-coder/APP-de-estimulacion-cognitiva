package com.example.ui.components

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.Spring
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.SportsEsports
import androidx.compose.material.icons.outlined.EmojiEvents
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.SportsEsports
import androidx.compose.material3.Icon
import androidx.compose.material3.Surface
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
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.role
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.viewmodel.AppTab

private val BarColor = Color(0xFFFFFFFF)
private val Ink = Color(0xFF1A1240)
private val InkDim = Color(0xFF6B6790)
private val Blue = Color(0xFFFF6B4A)
private val Orange = Color(0xFFFFC93C)
private val Amber = Color(0xFFFFC93C)

private data class NavItem(val tab: AppTab, val label: String, val on: ImageVector, val off: ImageVector)

private val leftItems = listOf(
  NavItem(AppTab.HOY, "Hoy", Icons.Filled.Home, Icons.Outlined.Home),
  NavItem(AppTab.JUEGOS, "Juegos", Icons.Filled.SportsEsports, Icons.Outlined.SportsEsports)
)
private val rightItems = listOf(
  NavItem(AppTab.PROGRESO, "Liga", Icons.Filled.EmojiEvents, Icons.Outlined.EmojiEvents),
  NavItem(AppTab.AJUSTES, "Perfil", Icons.Filled.Person, Icons.Outlined.Person)
)

/**
 * Barra inferior flotante de NeuroVida: 4 destinos (Hoy, Juegos, Liga, Perfil) y, al centro, el boton
 * "Entrenar" (inicia la sesion diaria) elevado, naranja y brillante. Pensada para no saturar: solo icono y
 * etiqueta corta; el seleccionado se resalta con una pastilla azul.
 */
@Composable
fun NeuroNavBar(current: AppTab, onSelect: (AppTab) -> Unit, onTrain: () -> Unit, modifier: Modifier = Modifier) {
  Box(
    modifier = modifier
      .fillMaxWidth()
      .height(96.dp)
      .padding(horizontal = 14.dp)
      .testTag("bottom_nav_bar")
  ) {
    Surface(
      modifier = Modifier
        .align(Alignment.BottomCenter)
        .padding(bottom = 4.dp)
        .fillMaxWidth()
        .height(68.dp)
        .drawBehind {
          drawRoundRect(Ink, topLeft = Offset(0f, 4.dp.toPx()), size = size, cornerRadius = CornerRadius(30.dp.toPx()))
        },
      shape = RoundedCornerShape(30.dp),
      color = BarColor,
      border = BorderStroke(3.dp, Ink),
      shadowElevation = 0.dp
    ) {
      Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically
      ) {
        leftItems.forEach { NavSlot(it, current == it.tab, { onSelect(it.tab) }, Modifier.weight(1f)) }
        // Hueco para el boton central
        Box(modifier = Modifier.weight(1f))
        rightItems.forEach { NavSlot(it, current == it.tab, { onSelect(it.tab) }, Modifier.weight(1f)) }
      }
    }

    TrainButton(
      onClick = onTrain,
      modifier = Modifier
        .align(Alignment.TopCenter)
        .offset(y = 2.dp)
    )
  }
}

@Composable
private fun NavSlot(item: NavItem, selected: Boolean, onClick: () -> Unit, modifier: Modifier = Modifier) {
  val tint by animateColorAsState(if (selected) Ink else InkDim, label = "navTint")
  val pill by animateColorAsState(if (selected) Blue.copy(alpha = 0.18f) else Color.Transparent, label = "navPill")
  val scale by animateFloatAsState(
    if (selected) 1.08f else 1f,
    spring(dampingRatio = Spring.DampingRatioMediumBouncy, stiffness = Spring.StiffnessMedium),
    label = "navScale"
  )
  Column(
    modifier = modifier
      .height(68.dp)
      .clickable(interactionSource = remember { MutableInteractionSource() }, indication = null, onClick = onClick)
      .semantics { role = Role.Tab; contentDescription = item.label }
      .testTag("nav_item_${item.tab.name.lowercase()}"),
    horizontalAlignment = Alignment.CenterHorizontally,
    verticalArrangement = Arrangement.Center
  ) {
    Box(
      modifier = Modifier
        .clip(RoundedCornerShape(16.dp))
        .background(pill)
        .padding(horizontal = 16.dp, vertical = 5.dp)
        .scale(scale),
      contentAlignment = Alignment.Center
    ) {
      Icon(if (selected) item.on else item.off, contentDescription = null, tint = if (selected) Blue else tint, modifier = Modifier.size(24.dp))
    }
    Text(item.label, fontSize = 11.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium, color = tint)
  }
}

@Composable
private fun TrainButton(onClick: () -> Unit, modifier: Modifier = Modifier) {
  Column(modifier = modifier, horizontalAlignment = Alignment.CenterHorizontally) {
    Box(
      modifier = Modifier
        .size(64.dp)
        .drawBehind { drawCircle(Ink, radius = size.minDimension / 2, center = Offset(size.width / 2, size.height / 2 + 4.dp.toPx())) }
        .clip(CircleShape)
        .background(Amber)
        .border(3.dp, Ink, CircleShape)
        .clickable(onClick = onClick)
        .semantics { role = Role.Button; contentDescription = "Entrenar: iniciar sesión diaria" }
        .testTag("nav_train_button"),
      contentAlignment = Alignment.Center
    ) {
      // Brillo superior
      Box(
        modifier = Modifier
          .align(Alignment.TopCenter)
          .padding(top = 5.dp)
          .size(width = 34.dp, height = 12.dp)
          .clip(RoundedCornerShape(50))
          .background(Color.White.copy(alpha = 0.55f))
      )
      Icon(Icons.Filled.PlayArrow, contentDescription = null, tint = Ink, modifier = Modifier.size(34.dp))
    }
  }
}
