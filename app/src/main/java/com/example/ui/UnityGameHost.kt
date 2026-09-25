package com.example.ui

import android.app.Activity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.example.bridge.UnityGameLauncher
import com.example.model.AgeBand
import com.example.viewmodel.ActiveGameSession

/**
 * Puente entre el flujo de la app (menú, sesión diaria, resultados, todo en Compose) y los juegos, que ahora
 * se juegan en Unity: al montarse lanza la Activity de Unity con la sesión pedida y, cuando Unity se cierra,
 * avisa con [onReturned] si la partida terminó (`true`) o si el usuario salió a mitad / Unity se cayó (`false`).
 *
 * La señal llega como resultado de la Activity (`RESULT_OK` lo pone `NativeReceiver.onGameFinished` al terminar
 * la partida), no adivinando por tiempo al volver a primer plano. El resultado en sí viaja aparte, por
 * broadcast -> `UnityResultBus` -> ViewModel.
 *
 * `launched` es `rememberSaveable` a propósito: si Android recrea MainActivity mientras Unity está al frente
 * (cambio de configuración: idioma, tamaño de fuente, modo oscuro del sistema...), la composición nueva no debe
 * volver a lanzar el juego. Este host sale de la composición entre una sesión y la siguiente (se muestra la
 * pantalla de resultado en medio), así que el valor no se arrastra de una partida a otra.
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
  onReturned: (finished: Boolean) -> Unit
) {
  val context = LocalContext.current
  var launched by rememberSaveable { mutableStateOf(false) }
  val launcher = rememberLauncherForActivityResult(ActivityResultContracts.StartActivityForResult()) { result ->
    onReturned(result.resultCode == Activity.RESULT_OK)
  }

  LaunchedEffect(Unit) {
    if (launched) return@LaunchedEffect
    launched = true
    launcher.launch(
      UnityGameLauncher.buildGameIntent(
        context = context,
        gameId = session.gameDef.id,
        userId = userId,
        level = session.level,
        baseIntensity = session.intensity,
        timed = session.timed,
        ageBand = ageBand,
        soundEnabled = soundEnabled
      )
    )
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
