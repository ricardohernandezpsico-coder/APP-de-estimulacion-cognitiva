package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.model.AgeBand
import com.example.ui.theme.TealPrimary

/**
 * Onboarding de edad, una sola vez (piloto de perfiles, 20-sep): mientras
 * `UserSettings.ageBand == null`, `MainActivity` muestra esta pantalla en vez de la app
 * normal (bottom nav, etc.) -- mismo criterio que Lumosity pidiendo un dato mínimo antes
 * de la primera sesión. Deliberadamente se pide un RANGO de edad, no la edad exacta: es
 * lo mínimo que hace falta para elegir el perfil de dificultad (ver `AgeBand` y
 * `com.example.games.parejas.DdaUserProfile`), no un dato personal de más.
 */
@Composable
fun AgeBandOnboardingScreen(onSelected: (AgeBand) -> Unit) {
  Box(
    modifier = Modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
      .padding(24.dp),
    contentAlignment = Alignment.Center
  ) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
      Box(
        modifier = Modifier
          .size(72.dp)
          .background(TealPrimary.copy(alpha = 0.12f), shape = RoundedCornerShape(20.dp)),
        contentAlignment = Alignment.Center
      ) {
        Icon(Icons.Default.Person, contentDescription = null, tint = TealPrimary, modifier = Modifier.size(36.dp))
      }
      Spacer(Modifier.height(20.dp))
      Text(
        text = "¿En qué rango de edad estás?",
        style = MaterialTheme.typography.headlineSmall,
        fontWeight = FontWeight.Bold,
        textAlign = androidx.compose.ui.text.style.TextAlign.Center
      )
      Spacer(Modifier.height(10.dp))
      Text(
        text = "Con esto ajustamos el ritmo y el tamaño de los juegos a lo que te resulta más cómodo. Podés cambiarlo cuando quieras desde Ajustes.",
        style = MaterialTheme.typography.bodyMedium,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
        textAlign = androidx.compose.ui.text.style.TextAlign.Center
      )
      Spacer(Modifier.height(32.dp))
      Column(
        modifier = Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(12.dp)
      ) {
        AgeBand.entries.forEach { band ->
          OutlinedButton(
            onClick = { onSelected(band) },
            modifier = Modifier
              .fillMaxWidth()
              .height(56.dp)
              .testTag("age_band_${band.name.lowercase()}"),
            shape = RoundedCornerShape(14.dp),
            colors = ButtonDefaults.outlinedButtonColors(contentColor = TealPrimary)
          ) {
            Text(text = band.label, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
          }
        }
      }
    }
  }
}
