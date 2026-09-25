package com.example.ui.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext

// Esquema unico "cosmos": fondo transparente (lo dibuja CosmosBackground), superficies azul noche y acentos
// azul electrico + naranja. Ver ui/components/CosmosBackground.kt.
private val CosmosColorScheme =
  darkColorScheme(
    primary = Color(0xFF5B9BFF),
    onPrimary = Color(0xFF07123A),
    primaryContainer = Color(0xFF1E3A8A),
    onPrimaryContainer = Color(0xFFDCE8FF),
    secondary = Color(0xFFFF8A3D),
    onSecondary = Color(0xFF2B1200),
    tertiary = Color(0xFFFFB347),
    background = Color.Transparent,
    onBackground = Color(0xFFEAF0FF),
    surface = Color(0xE6172058),
    onSurface = Color(0xFFEAF0FF),
    surfaceVariant = Color(0xFF243078),
    onSurfaceVariant = Color(0xFFB4BFEA),
    outline = Color(0xFF4B5AA8),
    outlineVariant = Color(0xFF34418A)
  )

@Composable
fun NeuroVidaTheme(
  darkTheme: Boolean = isSystemInDarkTheme(),
  dynamicColor: Boolean = false, // Use intentional brand theme by default
  content: @Composable () -> Unit,
) {
  // La identidad visual es unica (cosmos oscuro): darkTheme y dynamicColor se ignoran.
  val colorScheme = CosmosColorScheme

  MaterialTheme(colorScheme = colorScheme, typography = Typography, content = content)
}

