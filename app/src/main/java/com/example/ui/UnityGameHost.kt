package com.example.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.example.bridge.UnityGameLauncher
import com.example.model.AgeBand
import com.example.viewmodel.ActiveGameSession

/**
 * Puente entre el flujo de la app (menú, sesión diaria, resultados, todo en Compose) y los juegos, que ahora
 * se juegan en Unity: al montarse lanza la Activity de Unity con la sesión pedida y, cuando el usuario vuelve
 * a la app, avisa con [onReturned] (el ViewModel decide si hubo resultado o si se salió sin terminar).
 *
 * Mientras Unity está al frente esta pantalla queda debajo, con un indicador de carga por si la transición
 * tarda un instante.
 */
@Composable
fun UnityGameHost(
  session: ActiveGameSession,
  userId: String,
  ageBand: AgeBand,
  soundEnabled: Boolean,
  onReturned: () -> Unit
) {
  val context = LocalContext.current
  var launched by remember(session) { mutableStateOf(false) }

  LaunchedEffect(session) {
    UnityGameLauncher.launchGame(
      context = context,
      gameId = session.gameDef.id,
      userId = userId,
      level = session.level,
      baseIntensity = session.intensity,
      timed = session.timed,
      ageBand = ageBand,
      soundEnabled = soundEnabled
    )
    launched = true
  }

  val lifecycleOwner = LocalLifecycleOwner.current
  DisposableEffect(lifecycleOwner, session) {
    val observer = LifecycleEventObserver { _, event ->
      // Volver a la app después de haber lanzado Unity = el usuario cerró el juego.
      if (event == Lifecycle.Event.ON_RESUME && launched) onReturned()
    }
    lifecycleOwner.lifecycle.addObserver(observer)
    onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
  }

  Column(
    modifier = Modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background),
    horizontalAlignment = Alignment.CenterHorizontally,
    verticalArrangement = Arrangement.Center
  ) {
    CircularProgressIndicator()
    Text(
      text = session.gameDef.title,
      style = MaterialTheme.typography.titleMedium,
      color = MaterialTheme.colorScheme.onBackground,
      modifier = Modifier.padding(top = 16.dp)
    )
  }
}
