package com.example

import android.os.Bundle
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
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.unit.dp
import com.example.games.*
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
    enableEdgeToEdge()
    setContent {
      val userSettings by viewModel.userSettings.collectAsState()
      val darkTheme = when (userSettings.themeMode) {
        ThemeMode.LIGHT -> false
        ThemeMode.DARK -> true
        ThemeMode.SYSTEM -> isSystemInDarkTheme()
      }
      NeuroVidaTheme(darkTheme = darkTheme) {
        NeuroVidaApp(viewModel = viewModel)
      }
    }
  }
}

@Composable
fun NeuroVidaApp(viewModel: NeuroVidaViewModel) {
  val currentTab by viewModel.currentTab.collectAsState()
  val activeGame by viewModel.activeGame.collectAsState()
  val lastResult by viewModel.lastResult.collectAsState()
  val dailySession by viewModel.dailySession.collectAsState()

  Box(
    modifier = Modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
  ) {
    Scaffold(
      modifier = Modifier
        .fillMaxSize()
        .safeDrawingPadding(),
      bottomBar = {
        // Show bottom bar only when not playing a game or looking at results
        if (activeGame == null && lastResult == null) {
          NavigationBar(
            modifier = Modifier
              .fillMaxWidth()
              .testTag("bottom_nav_bar"),
            containerColor = MaterialTheme.colorScheme.surface,
            tonalElevation = 6.dp
          ) {
            val tabs = listOf(
              Triple(AppTab.HOY, Icons.Filled.Home, Icons.Outlined.Home),
              Triple(AppTab.JUEGOS, Icons.Filled.SportsEsports, Icons.Outlined.SportsEsports),
              Triple(AppTab.PROGRESO, Icons.Filled.BarChart, Icons.Outlined.BarChart),
              Triple(AppTab.AJUSTES, Icons.Filled.Settings, Icons.Outlined.Settings)
            )

            tabs.forEach { (tab, filledIcon, outlinedIcon) ->
              val isSelected = currentTab == tab
              NavigationBarItem(
                selected = isSelected,
                onClick = { viewModel.setTab(tab) },
                icon = {
                  Icon(
                    imageVector = if (isSelected) filledIcon else outlinedIcon,
                    contentDescription = tab.title
                  )
                },
                label = { Text(text = tab.title) },
                colors = NavigationBarItemDefaults.colors(
                  selectedIconColor = TealPrimary,
                  selectedTextColor = TealPrimary,
                  indicatorColor = TealPrimary.copy(alpha = 0.15f)
                ),
                modifier = Modifier.testTag("nav_item_${tab.name.lowercase()}")
              )
            }
          }
        }
      }
    ) { innerPadding ->
      Box(modifier = Modifier.padding(innerPadding)) {
        // Content based on tab
        AnimatedContent(
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
            AppTab.AJUSTES -> SettingsScreen(viewModel = viewModel)
          }
        }

        // Active Game Screen Overlay
        activeGame?.let { session ->
          Box(modifier = Modifier.fillMaxSize()) {
            when (session.gameDef.id) {
              "calculo" -> CalculoGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "parejas" -> ParejasGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "stroop" -> StroopGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "secuencia" -> SecuenciaGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "rutatesoro" -> RutaTesoroGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "cambiochip" -> CambioChipGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "series" -> SeriesGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "anagramas" -> AnagramasGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              "comparacion" -> ComparacionGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
              else -> CalculoGame(
                level = session.level,
                timed = session.timed,
                intensity = session.intensity,
                onFinish = { score, correct, total -> viewModel.finishActiveGame(score, correct, total) },
                onQuit = { viewModel.closeGameOrResult() }
              )
            }
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
