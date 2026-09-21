package com.example.games

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.example.games.parejas.SensoryFeedbackManager
import com.example.games.parejas.SoundEffect
import com.example.games.parejas.ddaProfileConfigFor
import com.example.games.secuencia.PadColor
import com.example.games.secuencia.SequenceGameAction
import com.example.games.secuencia.SequenceGameContainer
import com.example.games.secuencia.SequenceGameIntent
import com.example.games.secuencia.SequenceGameState
import com.example.games.secuencia.SequencePhase
import com.example.model.DomainType
import com.example.ui.LocalAgeBand
import com.example.ui.components.GameCountdownBoard
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainMemoria
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import kotlin.random.Random
import pro.respawn.flowmvi.compose.dsl.subscribe

/** 1 mm en dp -- mismo criterio que `ParejasGame.kt` (`dp` = 1/160", 160/25.4 ≈ 6.2992). */
private const val DP_PER_MM = 160f / 25.4f

/**
 * "Secuencia Lumínica" sobre FlowMVI real -- reemplaza la versión anterior, que vivía
 * enteramente en `remember`/`LaunchedEffect` locales (sin Store, sin motor DDA de
 * verdad, sin sonido). Mismo patrón de wiring que `ParejasGame.kt`: el Container se
 * arranca en un `DisposableEffect`, `subscribe` conecta estado + reacciona a las
 * Actions de una sola ejecución (sonido/flash), y `onFinish` se dispara desde un
 * `LaunchedEffect(state)` cuando el Store llega a `Finished`.
 *
 * Origen del rediseño: material que compartió Ricardo sobre un motor DDA
 * multidimensional (longitud de secuencia + velocidad de presentación + distractores,
 * con una regla anti-frustración: ante error, primero desacelerar antes de acortar la
 * secuencia) y tonos "concordantes" (cada pad con su propia frecuencia grave que suena
 * en la demostración Y al tocarlo). Ver [com.example.games.secuencia.SequenceDDAEngine].
 *
 * Segunda vuelta (guía de refactorización específica de este juego, más precisa que el
 * material genérico del 20-sep): extiende a este juego el mismo perfil de accesibilidad
 * táctil por edad que ya usa Parejas Ocultas (`ddaProfileConfigFor`/`LocalAgeBand`) --
 * antes el piloto de perfiles era exclusivo de Parejas Ocultas, pero el material pide
 * explícitamente adaptar los pads al perfil SENIOR (20x20mm, espaciado 3.2-12.5mm), así
 * que el piloto pasa a cubrir 2 juegos. Solo el TAMAÑO/ESPACIADO de los pads usa el
 * perfil acá -- el motor DDA de 3 ejes no varía sus pesos por edad (el material no lo
 * pide para este juego).
 */
@Composable
fun SecuenciaGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val context = LocalContext.current
  val ageBand = LocalAgeBand.current
  val profileConfig = remember(ageBand) { ddaProfileConfigFor(ageBand) }
  // Piso de accesibilidad real de Android (48dp) -- nunca se cruza, sea cual sea el
  // perfil, mismo criterio que `ParejasGame.MIN_TOUCH_TARGET_DP`.
  val padSizeDp = (profileConfig.idealTouchTargetMm * DP_PER_MM).coerceAtLeast(48f).dp
  val padSpacingDp = (profileConfig.maxSpacingMm * DP_PER_MM).dp
  val sensory = remember { SensoryFeedbackManager(context) }
  val container = remember(level, timed, intensity) {
    SequenceGameContainer(level = level, timed = timed, baseIntensity = intensity, sensory = sensory)
  }

  var levelUpVisible by remember { mutableStateOf(false) }
  var flashVisible by remember { mutableStateOf(false) }
  var flashSuccess by remember { mutableStateOf(true) }

  val scope = rememberCoroutineScope()
  DisposableEffect(container) {
    val lifecycle = container.store.start(scope)
    onDispose { lifecycle.close() }
  }

  val state by container.store.subscribe { action ->
    when (action) {
      // El sonido/haptics ya se disparan desde el propio Container (tiene inyectado el
      // SensoryFeedbackManager) -- acá solo se reacciona VISUALMENTE al mismo evento.
      is SequenceGameAction.PlayConcordantTone -> Unit
      is SequenceGameAction.TriggerHapticFeedback -> Unit
      is SequenceGameAction.PlaySoundEffect -> when (action.effect) {
        SoundEffect.ROUND_COMPLETE -> {
          flashSuccess = true
          flashVisible = true
          delay(400)
          flashVisible = false
        }
        SoundEffect.MISMATCH -> {
          flashSuccess = false
          flashVisible = true
          delay(400)
          flashVisible = false
        }
        SoundEffect.LEVEL_UP -> {
          levelUpVisible = true
          delay(900)
          levelUpVisible = false
        }
        else -> Unit
      }
    }
  }

  LaunchedEffect(state) {
    val finished = state as? SequenceGameState.Finished ?: return@LaunchedEffect
    onFinish(finished.score, finished.correctRounds, finished.totalRounds)
  }

  val currentState = state
  when (currentState) {
    SequenceGameState.Stopped -> Unit
    is SequenceGameState.Countdown -> GameCountdownBoard(
      title = "¡Ronda ${currentState.round} de ${currentState.totalRounds}!",
      subtitle = if (currentState.round == 1) "El juego comienza..." else "Observa con atención",
      secondsLeft = currentState.secondsLeft,
      accentColor = DomainMemoria,
      onQuit = onQuit
    )
    is SequenceGameState.Running -> RunningBoard(
      state = currentState,
      timed = timed,
      levelUpVisible = levelUpVisible,
      flashVisible = flashVisible,
      flashSuccess = flashSuccess,
      padSizeDp = padSizeDp,
      padSpacingDp = padSpacingDp,
      onPadTapped = { pad -> container.store.intent(SequenceGameIntent.PadTapped(pad)) },
      onQuit = onQuit
    )
    is SequenceGameState.Finished -> Unit
    is SequenceGameState.Error -> SequenceErrorBoard(
      onRestart = { container.store.intent(SequenceGameIntent.RestartGame) },
      onQuit = onQuit
    )
  }
}

@Composable
private fun SequenceErrorBoard(onRestart: () -> Unit, onQuit: () -> Unit) {
  Column(
    modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.background).padding(24.dp),
    horizontalAlignment = Alignment.CenterHorizontally,
    verticalArrangement = Arrangement.Center
  ) {
    Text("Algo no salió bien armando la secuencia.", style = MaterialTheme.typography.bodyLarge)
    Spacer(Modifier.height(16.dp))
    Button(onClick = onRestart) { Text("Reintentar") }
    Spacer(Modifier.height(8.dp))
    TextButton(onClick = onQuit) { Text("Salir") }
  }
}

@Composable
private fun RunningBoard(
  state: SequenceGameState.Running,
  timed: Boolean,
  levelUpVisible: Boolean,
  flashVisible: Boolean,
  flashSuccess: Boolean,
  padSizeDp: Dp,
  padSpacingDp: Dp,
  onPadTapped: (PadColor) -> Unit,
  onQuit: () -> Unit
) {
  Box(modifier = Modifier.fillMaxSize()) {
    Column(
      modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.background),
      horizontalAlignment = Alignment.CenterHorizontally
    ) {
      GameHeader(
        title = "Secuencia Lumínica",
        domain = DomainType.MEMORIA,
        currentRound = state.round,
        totalRounds = state.totalRounds,
        isTimed = timed,
        onQuit = onQuit
      )

      Spacer(modifier = Modifier.height(20.dp))

      Surface(
        shape = RoundedCornerShape(16.dp),
        color = when (state.phase) {
          SequencePhase.PRESENTING -> DomainMemoria.copy(alpha = 0.12f)
          SequencePhase.GET_READY -> Color(0xFFF59E0B).copy(alpha = 0.15f)
          SequencePhase.AWAITING_INPUT -> EmeraldAccent.copy(alpha = 0.12f)
        }
      ) {
        Text(
          text = when (state.phase) {
            SequencePhase.PRESENTING -> "👀 Observa la secuencia (${state.sequence.size} luces)"
            SequencePhase.GET_READY -> "✋ ¡Prepárate!"
            SequencePhase.AWAITING_INPUT -> "👉 Tu turno: repite la secuencia (${state.userInput.size}/${state.sequence.size})"
          },
          modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
          style = MaterialTheme.typography.labelLarge,
          fontWeight = FontWeight.Bold,
          color = when (state.phase) {
            SequencePhase.PRESENTING -> DomainMemoria
            SequencePhase.GET_READY -> Color(0xFFD97706)
            SequencePhase.AWAITING_INPUT -> EmeraldAccent
          }
        )
      }

      AnimatedVisibility(visible = state.roundFeedback != null, enter = fadeIn(), exit = fadeOut()) {
        Text(
          text = state.roundFeedback.orEmpty(),
          modifier = Modifier.padding(top = 10.dp),
          style = MaterialTheme.typography.labelMedium,
          fontWeight = FontWeight.Bold,
          color = Color(0xFFD97706)
        )
      }

      Spacer(modifier = Modifier.weight(1f))

      Box(contentAlignment = Alignment.Center) {
        // Ruido ambiente que crece con la dificultad -- deliberadamente sutil (nunca
        // compite por la atención con los pads reales): el material que compartió
        // Ricardo es explícito en evitar "animaciones invasivas... que compitan por la
        // memoria de trabajo", así que acá los distractores son un fondo pasivo, no
        // elementos que haya que identificar o ignorar activamente. Mismo criterio
        // visual (blobs con degradé radial) que `DistractorBackdrop` en Parejas Ocultas.
        DistractorGlow(count = state.distractorCount, seed = state.round, boardSize = padSizeDp * 2 + padSpacingDp)

        Column(
          modifier = Modifier.padding(24.dp),
          verticalArrangement = Arrangement.spacedBy(padSpacingDp),
          horizontalAlignment = Alignment.CenterHorizontally
        ) {
          Row(horizontalArrangement = Arrangement.spacedBy(padSpacingDp)) {
            SequencePad(pad = PadColor.AZUL, size = padSizeDp, isLit = state.highlightedPad == PadColor.AZUL, onClick = { onPadTapped(PadColor.AZUL) })
            SequencePad(pad = PadColor.AMBAR, size = padSizeDp, isLit = state.highlightedPad == PadColor.AMBAR, onClick = { onPadTapped(PadColor.AMBAR) })
          }
          Row(horizontalArrangement = Arrangement.spacedBy(padSpacingDp)) {
            SequencePad(pad = PadColor.VERDE, size = padSizeDp, isLit = state.highlightedPad == PadColor.VERDE, onClick = { onPadTapped(PadColor.VERDE) })
            SequencePad(pad = PadColor.ROSA, size = padSizeDp, isLit = state.highlightedPad == PadColor.ROSA, onClick = { onPadTapped(PadColor.ROSA) })
          }
        }
      }

      Spacer(modifier = Modifier.weight(1f))
    }

    ScreenFlashOverlay(visible = flashVisible, flashColor = if (flashSuccess) EmeraldAccent else MaterialTheme.colorScheme.error)
    ScreenFlashOverlay(visible = levelUpVisible, flashColor = com.example.ui.theme.DomainAtencion)
  }
}

@Composable
private fun SequencePad(pad: PadColor, size: Dp, isLit: Boolean, onClick: () -> Unit) {
  val scale by animateFloatAsState(targetValue = if (isLit) 1.08f else 1.0f, animationSpec = tween(150))
  val color by animateColorAsState(targetValue = if (isLit) pad.lightColor else pad.normalColor, animationSpec = tween(150))

  Surface(
    modifier = Modifier
      .size(size)
      .scale(scale)
      .clip(RoundedCornerShape(28.dp))
      .clickable { onClick() }
      .testTag("pad_${pad.name.lowercase()}"),
    shape = RoundedCornerShape(28.dp),
    color = color,
    shadowElevation = if (isLit) 12.dp else 4.dp
  ) {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
      Text(
        text = pad.label,
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.Bold,
        color = Color.White.copy(alpha = if (isLit) 1f else 0.8f)
      )
    }
  }
}

@Composable
private fun DistractorGlow(count: Int, seed: Int, boardSize: Dp) {
  if (count == 0) return
  val color = MaterialTheme.colorScheme.onSurfaceVariant
  Canvas(modifier = Modifier.size(boardSize)) {
    val random = Random(seed)
    repeat(count) {
      val x = random.nextFloat() * size.width
      val y = random.nextFloat() * size.height
      val radius = size.minDimension * (0.10f + random.nextFloat() * 0.08f)
      val center = Offset(x, y)
      drawCircle(
        brush = Brush.radialGradient(
          colors = listOf(color.copy(alpha = 0.07f), color.copy(alpha = 0f)),
          center = center,
          radius = radius
        ),
        radius = radius,
        center = center
      )
    }
  }
}
