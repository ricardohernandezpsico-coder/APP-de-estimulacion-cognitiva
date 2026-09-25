package com.example

import android.content.Intent
import android.os.Bundle
import androidx.activity.addCallback
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.togetherWith
import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.BarChart
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.SportsEsports
import androidx.compose.material.icons.outlined.BarChart
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material.icons.outlined.SportsEsports
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.graphics.BlendMode
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.example.bridge.NativeReceiver
import com.example.bridge.UnityGameLauncher
import com.example.games.*
import com.example.ui.LocalAgeBand
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.strings
import com.example.ui.screens.AgeBandOnboardingScreen
import com.example.ui.screens.GamesLibraryScreen
import com.example.ui.screens.HomeScreen
import com.example.ui.screens.ProgressScreen
import com.example.ui.screens.SettingsScreen
import com.example.model.ThemeMode
import com.example.ui.theme.NeuroVidaTheme
import com.example.ui.theme.TealPrimary
import com.example.viewmodel.AppTab
import com.example.viewmodel.NeuroVidaViewModel

class MainActivity : ComponentActivity() {
  private val viewModel: NeuroVidaViewModel by viewModels()

  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)
    if (savedInstanceState == null) handleGameReturn(intent)
    // Atrás en la pantalla raíz: la app pasa a segundo plano en vez de cerrarse. Unity queda vivo DEBAJO de esta
    // Activity entre partidas (ver UnityGameHost); cerrarla lo dejaría a la vista. Los BackHandler de Compose
    // (diálogos, Ajustes) se registran después y tienen prioridad.
    onBackPressedDispatcher.addCallback(this) { moveTaskToBack(true) }
    enableEdgeToEdge(
      statusBarStyle = androidx.activity.SystemBarStyle.dark(android.graphics.Color.TRANSPARENT),
      navigationBarStyle = androidx.activity.SystemBarStyle.dark(android.graphics.Color.TRANSPARENT)
    )
    setContent {
      val userSettings by viewModel.userSettings.collectAsState()
      val darkTheme = when (userSettings.themeMode) {
        ThemeMode.LIGHT -> false
        ThemeMode.DARK -> true
        ThemeMode.SYSTEM -> isSystemInDarkTheme()
      }
      CompositionLocalProvider(
        LocalAppLanguage provides userSettings.language,
        LocalAgeBand provides (userSettings.ageBand ?: com.example.model.AgeBand.ADULT)
      ) {
        NeuroVidaTheme(darkTheme = darkTheme) {
          // Onboarding de edad (piloto de perfiles, 20-sep): mientras no se resuelva
          // `ageBand`, no se monta la app normal (bottom nav, tabs, etc.) -- mismo
          // criterio que Lumosity pidiendo un dato mínimo antes de la primera sesión.
          if (userSettings.ageBand == null) {
            AgeBandOnboardingScreen(onSelected = { band -> viewModel.setAgeBand(band) })
          } else {
            NeuroVidaApp(viewModel = viewModel)
          }
        }
      }
    }
  }

  /** Unity trae la app al frente al terminar o salir de una partida (ver `NativeReceiver.returnToApp`). */
  override fun onNewIntent(intent: Intent) {
    super.onNewIntent(intent)
    setIntent(intent)
    handleGameReturn(intent)
  }

  private fun handleGameReturn(intent: Intent?) {
    if (intent?.getBooleanExtra(NativeReceiver.EXTRA_RETURN_FROM_GAME, false) != true) return
    intent.removeExtra(NativeReceiver.EXTRA_RETURN_FROM_GAME)
    viewModel.onReturnedFromGame(
      launchId = intent.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID),
      resultJson = intent.getStringExtra(NativeReceiver.EXTRA_JSON),
      paused = intent.getBooleanExtra(NativeReceiver.EXTRA_PAUSED, false)
    )
  }
}

@Composable
fun NeuroVidaApp(viewModel: NeuroVidaViewModel) {
  val userSettings by viewModel.userSettings.collectAsState()
  val currentTab by viewModel.currentTab.collectAsState()
  val activeGame by viewModel.activeGame.collectAsState()
  val lastResult by viewModel.lastResult.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()
  val gameRanks by viewModel.gameRanks.collectAsState()

  Box(modifier = Modifier.fillMaxSize()) {
    com.example.ui.components.CosmosBackground()
    Scaffold(
      modifier = Modifier
        .fillMaxSize()
        .safeDrawingPadding(),
      containerColor = androidx.compose.ui.graphics.Color.Transparent,
      bottomBar = {
        // Show bottom bar only when not playing a game or looking at results
        if (activeGame == null && lastResult == null) {
          com.example.ui.components.NeuroNavBar(
            current = currentTab,
            onSelect = { viewModel.setTab(it) },
            onTrain = { viewModel.startDailySession() }
          )
        }
      }
    ) { innerPadding ->
      Box(
        modifier = Modifier
          .padding(innerPadding)
          .graphicsLayer { compositingStrategy = CompositingStrategy.Offscreen }
          .drawWithContent {
            drawContent()
            // Las pantallas se desvanecen en los bordes en vez de cortarse en seco
            drawRect(
              Brush.verticalGradient(0f to Color.Transparent, 0.025f to Color.Black, 0.955f to Color.Black, 1f to Color.Transparent),
              blendMode = BlendMode.DstIn
            )
          }
      ) {
        // Content based on tab. Mientras hay un juego o un resultado encima no se compone: esas pantallas van
        // sobre el cielo transparente (se vería la pestaña detrás) y no deben dejar pasar toques a ella.
        if (activeGame == null && lastResult == null) AnimatedContent(
          targetState = currentTab,
          transitionSpec = { fadeIn() togetherWith fadeOut() },
          label = "TabTransition"
        ) { tab ->
          when (tab) {
            AppTab.HOY -> HomeScreen(
              viewModel = viewModel,
              onNavigateToGames = { viewModel.setTab(AppTab.JUEGOS) }
            )
            AppTab.JUEGOS -> GamesLibraryScreen(viewModel = viewModel)
            AppTab.PROGRESO -> ProgressScreen(viewModel = viewModel)
            AppTab.AJUSTES -> com.example.ui.screens.ProfileScreen(viewModel = viewModel)
          }
        }

        // Active Game Screen Overlay
        activeGame?.let { session ->
          Box(modifier = Modifier.fillMaxSize()) {
            // Los 9 juegos se juegan ahora en Unity (ver docs y UnityGameHost): se lanza la Activity de Unity con
            // esta sesión y el resultado vuelve por UnityResultBus al ViewModel.
            com.example.ui.UnityGameHost(
              session = session,
              userId = userSettings.id.toString(),
              ageBand = userSettings.ageBand ?: com.example.model.AgeBand.ADULT,
              soundEnabled = userSettings.soundEnabled,
              onLaunched = { viewModel.onUnityLaunched() },
              onHostResumed = { viewModel.onHostResumed() }
            )
          }
        }

        // Last Result Screen Overlay
        lastResult?.let { (result, didLevelUp) ->
          GameResultScreen(
            result = result,
            didLevelUp = didLevelUp,
            isDailyFlow = activeGame?.isDailyFlow ?: (dailySession.completedCount in 1..3),
            dailyCompletedCount = dailySession.completedCount,
            dailyTotalCount = 3,
            rank = gameRanks.firstOrNull { it.gameId == result.gameId },
            onPlayAgain = {
              viewModel.launchGame(result.gameId, customLevel = result.level, customTimed = result.timed)
            },
            onContinue = {
              if (dailySession.completedCount < 3) {
                viewModel.continueDailyFlow()
              } else {
                viewModel.closeGameOrResult()
              }
            }
          )
        }
      }
    }
  }
}
