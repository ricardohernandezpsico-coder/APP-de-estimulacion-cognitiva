package com.example.ui

import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.example.bridge.UnityGameLauncher
import com.example.model.AgeBand
import com.example.ui.components.GameLoadingScreen
import com.example.viewmodel.ActiveGameSession

/**
 * Puente entre el flujo de la app (menú, sesión diaria, resultados, todo en Compose) y los juegos, que se
 * juegan en Unity. Al montarse trae al frente la pantalla de Unity con la sesión pedida. Unity queda VIVO entre
 * partidas: al terminar ("Continuar") o salir (Atrás) no se cierra, sino que devuelve la app al frente con un
 * Intent (ver `NativeReceiver.returnToApp` -> `MainActivity.onNewIntent` -> `NeuroVidaViewModel.onReturnedFromGame`),
 * así la siguiente partida no vuelve a arrancar el motor (antes, 5-8 s de carga por juego).
 *
 * [onHostResumed]: si la app vuelve a primer plano sin esa vuelta (Unity se cerró o se cayó), el ViewModel cierra
 * la sesión. El observador de ciclo de vida recibe un ON_RESUME inmediato al registrarse (antes de lanzar): por
 * eso el ViewModel solo lo tiene en cuenta después de [onLaunched].
 *
 * `launched` es `rememberSaveable` a propósito: si Android recrea MainActivity mientras Unity está al frente
 * (cambio de configuración: idioma, tamaño de fuente, modo oscuro del sistema...), la composición nueva no debe
 * volver a lanzar el juego. Este host sale de la composición entre una sesión y la siguiente (se muestra la
 * pantalla de resultado en medio), así que el valor no se arrastra de una partida a otra.
 */
@Composable
fun UnityGameHost(
  session: ActiveGameSession,
  userId: String,
  ageBand: AgeBand,
  soundEnabled: Boolean,
  onLaunched: () -> Unit,
  onHostResumed: () -> Unit
) {
  val context = LocalContext.current
  var launched by rememberSaveable { mutableStateOf(false) }

  val lifecycleOwner = LocalLifecycleOwner.current
  DisposableEffect(lifecycleOwner) {
    val observer = LifecycleEventObserver { _, event ->
      if (event == Lifecycle.Event.ON_RESUME) onHostResumed()
    }
    lifecycleOwner.lifecycle.addObserver(observer)
    onDispose { lifecycleOwner.lifecycle.removeObserver(observer) }
  }

  LaunchedEffect(Unit) {
    if (launched) return@LaunchedEffect
    launched = true
    context.startActivity(
      UnityGameLauncher.buildGameIntent(
        context = context,
        gameId = session.gameDef.id,
        userId = userId,
        level = session.level,
        baseIntensity = session.intensity,
        timed = session.timed,
        ageBand = ageBand,
        soundEnabled = soundEnabled,
        launchId = session.resumeLaunchId
      )
    )
    onLaunched()
  }

  // Mismo aspecto que la capa de carga encima de Unity (bridge/UnityLoadingOverlay): el paso app -> Unity no se
  // nota. El cielo ya lo pinta la app detrás. Con Unity vivo (arranque en caliente) esto se ve una fracción de
  // segundo: el contenido espera un poco antes de aparecer para no destellar.
  GameLoadingScreen(
    game = session.gameDef,
    level = session.level,
    timed = session.timed,
    fadeInDelayMs = 250L
  )
}
