package com.example.games

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AutoAwesome
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.lerp
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.games.parejas.CardsGameAction
import com.example.games.parejas.CardsGameContainer
import com.example.games.parejas.CardsGameIntent
import com.example.games.parejas.CardsGameState
import com.example.games.parejas.CountdownReason
import com.example.games.parejas.MemoryCardUi
import com.example.games.parejas.SensoryFeedbackManager
import com.example.games.parejas.ddaProfileConfigFor
import com.example.model.DomainType
import com.example.ui.LocalAgeBand
import com.example.ui.components.GameCountdownBoard
import com.example.ui.components.GameHeader
import com.example.ui.components.ScreenFlashOverlay
import com.example.ui.theme.DomainAtencion
import com.example.ui.theme.DomainMemoria
import com.example.ui.theme.DomainVelocidad
import com.example.ui.theme.EmeraldAccent
import kotlinx.coroutines.delay
import pro.respawn.flowmvi.compose.dsl.subscribe
import kotlin.random.Random

/** 1 mm en dp: `dp` está definido como 1/160". 160 / 25.4 ≈ 6.2992 dp por mm. */
private const val DP_PER_MM = 160f / 25.4f

/** Piso de accesibilidad real de Android -- NUNCA se cruza, sea cual sea el perfil de
 *  edad (ver [com.example.games.parejas.DdaUserProfileConfig]). El tamaño IDEAL y el
 *  espaciado sí varían por perfil: 20mm/3.2-12.5mm para adultos mayores (el spec
 *  original, pensado para motricidad fina reducida) vs. 15mm/1.5-6mm para adultos (lo
 *  que Ricardo pidió tras jugarlo él mismo) -- no es una contradicción, son perfiles
 *  distintos pidiendo cosas opuestas. */
private val MIN_TOUCH_TARGET_DP = 48.dp

@Composable
fun ParejasGame(
  level: Int,
  timed: Boolean,
  intensity: Int = 0,
  onFinish: (score: Int, correct: Int, total: Int) -> Unit,
  onQuit: () -> Unit
) {
  val context = LocalContext.current
  val ageBand = LocalAgeBand.current
  val profileConfig = remember(ageBand) { ddaProfileConfigFor(ageBand) }
  val sensory = remember { SensoryFeedbackManager(context) }
  val container = remember(level, timed, intensity, ageBand) {
    CardsGameContainer(level = level, timed = timed, baseIntensity = intensity, sensory = sensory, ageBand = ageBand)
  }

  var levelUpVisible by remember { mutableStateOf(false) }

  // Corrección real tras probarlo en el emulador: `subscribe` SOLO conecta la UI al
  // Store (recibe estado, reenvía intents) — no lo arranca. `ImmutableStore.start(scope)`
  // es un método aparte que efectivamente lanza el pipeline (`init`, `reduce`, el timer).
  // Sin este bloque el Store se queda para siempre en `Stopped` y la pantalla del juego
  // queda invisible (se ve literalmente la pantalla de atrás, porque `Stopped -> Unit`
  // no dibuja nada) — así es como se manifestó el bug: tocar "Empezar" parecía no hacer
  // nada.
  val scope = rememberCoroutineScope()
  DisposableEffect(container) {
    val lifecycle = container.store.start(scope)
    onDispose { lifecycle.close() } // dispara `deinit` (ver CardsGameContainer) de verdad
  }

  // `subscribe` va sobre el Store directo (no adentro de un `with`) y devuelve un
  // Compose `State<S>`.
  val state by container.store.subscribe { action ->
    when (action) {
      // El sonido/haptics ya se disparan desde el propio Container (tiene inyectado
      // el SensoryFeedbackManager) — estas Action llegan acá solo por si la UI quiere
      // reaccionar visualmente al mismo evento, que es el caso de ShowLevelUp.
      is CardsGameAction.PlaySoundEffect -> Unit
      is CardsGameAction.TriggerHapticFeedback -> Unit
      CardsGameAction.ShowLevelUp -> {
        levelUpVisible = true
        delay(900)
        levelUpVisible = false
      }
    }
  }

  LaunchedEffect(state) {
    val finished = state as? CardsGameState.Finished ?: return@LaunchedEffect
    onFinish(finished.score, finished.matchedPairs, finished.attempts)
  }

  // `container.store` ya implementa IntentReceiver<CardsGameIntent> directamente, así
  // que despachar un intent es solo `container.store.intent(...)` — no hace falta un
  // receiver de Compose aparte.
  // Se captura en un `val` local porque Kotlin no smart-castea una propiedad delegada
  // (`by`) directamente dentro del `when`.
  val currentState = state
  when (currentState) {
    CardsGameState.Stopped -> Unit
    is CardsGameState.Loading -> LoadingBoard()
    is CardsGameState.Countdown -> CountdownBoard(state = currentState, onQuit = onQuit)
    is CardsGameState.Running -> RunningBoard(
      state = currentState,
      timed = timed,
      levelUpVisible = levelUpVisible,
      idealTouchTargetDp = (profileConfig.idealTouchTargetMm * DP_PER_MM).dp,
      minSpacingDp = (profileConfig.minSpacingMm * DP_PER_MM).dp,
      maxSpacingDp = (profileConfig.maxSpacingMm * DP_PER_MM).dp,
      onCardClick = { id -> container.store.intent(CardsGameIntent.CardFlipped(id)) },
      onQuit = onQuit
    )
    // onFinish ya se disparó con el LaunchedEffect de arriba; el padre reemplaza esta
    // pantalla por la de resultado.
    is CardsGameState.Finished -> Unit
    is CardsGameState.Error -> ErrorBoard(
      onRestart = { container.store.intent(CardsGameIntent.RestartGame) },
      onQuit = onQuit
    )
  }
}

@Composable
private fun LoadingBoard() {
  Box(
    modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.background),
    contentAlignment = Alignment.Center
  ) {
    CircularProgressIndicator(color = DomainMemoria)
  }
}

/**
 * Pantalla "prepárate 3-2-1" -- pedido explícito de Ricardo tras jugarlo: "se ve muy
 * estática... una pantalla dinámica que te diga prepárate, el juego comienza en 3,2,1".
 * Aparece al arrancar la partida Y en cada transición de nivel (ver
 * [CardsGameContainer.playCountdownThenShow]), con un círculo pulsante detrás del número
 * y una animación de escala/fundido al cambiar de dígito para que se sienta como cuenta
 * regresiva real, no un cartel estático. El texto cambia según [CardsGameState.Countdown.reason]
 * para que bajar de nivel tras 3 fallas se lea como parte del juego, no como un bug.
 */
@Composable
private fun CountdownBoard(state: CardsGameState.Countdown, onQuit: () -> Unit) {
  val title = when (state.reason) {
    CountdownReason.START -> "¡Prepárate!"
    CountdownReason.ADVANCE -> "¡Nivel ${state.stage} de ${state.totalStages}!"
    CountdownReason.DEMOTED -> "Bajamos al nivel ${state.stage}"
    CountdownReason.RETRY -> "Repetimos el nivel ${state.stage}"
  }
  val subtitle = when (state.reason) {
    CountdownReason.DEMOTED, CountdownReason.RETRY -> "Vamos con calma, se puede"
    else -> "El juego comienza..."
  }
  // Extraído a `GameCountdownBoard` (ui/components/CommonUi.kt) para que Secuencia
  // Lumínica reuse la misma animación de pulso/cambio de dígito en su propia
  // transición de ronda -- ver `SecuenciaGame.kt`.
  GameCountdownBoard(title = title, subtitle = subtitle, secondsLeft = state.secondsLeft, accentColor = DomainMemoria, onQuit = onQuit)
}

@Composable
private fun ErrorBoard(onRestart: () -> Unit, onQuit: () -> Unit) {
  Column(
    modifier = Modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
      .padding(24.dp),
    horizontalAlignment = Alignment.CenterHorizontally,
    verticalArrangement = Arrangement.Center
  ) {
    Text("Algo no salió bien armando el tablero.", style = MaterialTheme.typography.bodyLarge)
    Spacer(Modifier.height(16.dp))
    Button(onClick = onRestart) { Text("Reintentar") }
    Spacer(Modifier.height(8.dp))
    TextButton(onClick = onQuit) { Text("Salir") }
  }
}

@Composable
private fun RunningBoard(
  state: CardsGameState.Running,
  timed: Boolean,
  levelUpVisible: Boolean,
  idealTouchTargetDp: Dp,
  minSpacingDp: Dp,
  maxSpacingDp: Dp,
  onCardClick: (Int) -> Unit,
  onQuit: () -> Unit
) {
  val (columns, rows) = state.gridDimensions

  Box(modifier = Modifier.fillMaxSize()) {
    Column(modifier = Modifier.fillMaxSize().background(MaterialTheme.colorScheme.background)) {
      GameHeader(
        title = "Parejas Ocultas",
        domain = DomainType.MEMORIA,
        // El chip/barra de arriba ahora reflejan el nivel DENTRO de la partida (1..10,
        // ver CardsGameContainer) en vez de las parejas del tablero actual -- esas ya se
        // ven en el chip "Parejas: X/Y" de StatusRow, más abajo.
        currentRound = state.stage,
        totalRounds = state.totalStages,
        isTimed = timed,
        timeLeftSeconds = state.timeLeftSeconds,
        onQuit = onQuit
      )

      Spacer(Modifier.height(12.dp))
      // Ricardo pidió sacar la barra/línea y dejar solo los segundos, "de una forma
      // establecida y clara" -- ver [TimeDisplay].
      if (!state.isMemorizing) TimeDisplay(state = state, timed = timed)
      Spacer(Modifier.height(8.dp))
      StatusRow(state = state)
      Spacer(Modifier.height(16.dp))

      BoxWithConstraints(
        modifier = Modifier.weight(1f).fillMaxWidth().padding(16.dp),
        contentAlignment = Alignment.Center
      ) {
        DistractorBackdrop(
          count = state.distractorCount,
          opacity = state.distractorOpacity,
          seed = state.distractorSeed
        )

        // `remember(columns, rows, maxWidth, maxHeight)`: el tamaño de carta y el
        // espaciado dependen solo de la grilla y el espacio disponible, NO del resto
        // del estado (que cambia cada 100ms por el timer) -- evita recalcular esto en
        // cada tick cuando en realidad no cambió.
        val (cardSize, spacing) = remember(columns, rows, maxWidth, maxHeight, idealTouchTargetDp, minSpacingDp, maxSpacingDp) {
          resolveCardLayout(
            columns = columns,
            rows = rows,
            availableWidthDp = maxWidth.value,
            availableHeightDp = maxHeight.value,
            idealTargetDp = idealTouchTargetDp.value,
            minSpacingDp = minSpacingDp.value,
            maxSpacingDp = maxSpacingDp.value
          )
        }
        // Bug real encontrado al revisar la captura de pantalla (no a simple vista en
        // el emulador en vivo): `GridCells.Fixed(columns)` reparte TODO el ancho
        // disponible del grid entre las columnas, ignorando el `cardSize` calculado --
        // con solo 2 columnas eso daba celdas de ~medio ancho de pantalla (rectángulos
        // anchos, no cuadrados), exactamente la sensación de "recuadros separados y
        // gigantes" que señaló Ricardo. Fijar el ancho del grid al tamaño real de las
        // cartas hace que `GridCells.Fixed` reparta ESE ancho, no el de la pantalla.
        val gridWidth = cardSize * columns + spacing * (columns - 1)
        val gridHeight = cardSize * rows + spacing * (rows - 1)

        // Segundo pedido de Ricardo tras jugarlo: "el formato de las cartas y la
        // distribución... que no sea el típico juego de parejas ocultas". Antes las
        // cartas flotaban sueltas sobre el fondo liso de la pantalla; este panel con
        // degradé sutil y esquinas bien redondeadas les da un "tablero" real detrás,
        // igual que las cartas de abajo cambian de un rectángulo genérico a algo con
        // más carácter (ver [CardTile]). Es puramente decorativo -- no participa en el
        // cálculo de `resolveCardLayout`, se dimensiona a partir de la grilla ya resuelta.
        val boardPanelInset = 14.dp
        Box(
          modifier = Modifier
            .width(gridWidth + boardPanelInset * 2)
            .height(gridHeight + boardPanelInset * 2)
            .background(
              brush = Brush.verticalGradient(
                colors = listOf(
                  DomainMemoria.copy(alpha = 0.16f),
                  DomainMemoria.copy(alpha = 0.05f)
                )
              ),
              shape = RoundedCornerShape(28.dp)
            )
        )

        LazyVerticalGrid(
          columns = GridCells.Fixed(columns),
          horizontalArrangement = Arrangement.spacedBy(spacing),
          verticalArrangement = Arrangement.spacedBy(spacing),
          modifier = Modifier.width(gridWidth).wrapContentHeight()
        ) {
          // `key = { it.id }`: cada carta conserva su identidad de composición entre
          // recomposiciones. Como `cards.map { if (cond) it.copy(...) else it }` en el
          // Container devuelve la MISMA instancia para las cartas no tocadas, sumado a
          // esta key, Compose salta la recomposición de las celdas que no cambiaron en
          // vez de redibujar las 36 en cada toque.
          items(state.cards, key = { it.id }) { card ->
            CardTile(
              card = card,
              size = cardSize,
              onClick = { onCardClick(card.id) },
              modifier = Modifier.testTag("card_${card.id}")
            )
          }
        }
      }
    }

    // Retroalimentación positiva continua en tono cálido (ámbar = DomainAtencion),
    // distinto a propósito del azul de Memoria: el color de dominio identifica la
    // pantalla, el ámbar cálido identifica "vas bien".
    ScreenFlashOverlay(visible = levelUpVisible, flashColor = DomainAtencion)
  }
}

/**
 * Ricardo, tras ver la barra de tiempo anterior: "que no salga la línea... que
 * simplemente aparezcan los segundos, de una forma establecida y clara" -- se saca el
 * `LinearProgressIndicator` y queda solo un reloj de texto centrado, formato m:ss para
 * que se lea bien incluso pasado el minuto (partidas de varios niveles pueden acumular
 * más de 60s). Modo Reto muestra la cuenta regresiva real (roja bajo 20% restante,
 * mismo umbral que el chip de `GameHeader`); Precisión muestra el cronómetro
 * ascendente (`elapsedMs`) sin ningún tinte de urgencia, porque no hay límite real.
 */
@Composable
private fun TimeDisplay(state: CardsGameState.Running, timed: Boolean) {
  val totalTimeSeconds = state.totalTimeSeconds
  val timeLeftSeconds = state.timeLeftSeconds
  val seconds: Int
  val urgent: Boolean
  if (timed && timeLeftSeconds != null && totalTimeSeconds != null) {
    seconds = timeLeftSeconds
    urgent = timeLeftSeconds <= totalTimeSeconds.coerceAtLeast(1) * 0.2f
  } else {
    seconds = (state.elapsedMs / 1000).toInt()
    urgent = false
  }
  val color = if (urgent) DomainVelocidad else MaterialTheme.colorScheme.onSurface

  Row(
    modifier = Modifier.fillMaxWidth(),
    horizontalArrangement = Arrangement.Center,
    verticalAlignment = Alignment.CenterVertically
  ) {
    Icon(Icons.Default.Timer, contentDescription = null, tint = color, modifier = Modifier.size(18.dp))
    Spacer(Modifier.width(6.dp))
    Text(
      text = formatClock(seconds),
      style = MaterialTheme.typography.titleMedium,
      fontWeight = FontWeight.Bold,
      color = color
    )
  }
}

private fun formatClock(totalSeconds: Int): String {
  val safeSeconds = totalSeconds.coerceAtLeast(0)
  val minutes = safeSeconds / 60
  val remainder = safeSeconds % 60
  return if (minutes > 0) "%d:%02d".format(minutes, remainder) else "${remainder}s"
}

@Composable
private fun StatusRow(state: CardsGameState.Running) {
  Row(
    modifier = Modifier.fillMaxWidth().padding(horizontal = 20.dp),
    horizontalArrangement = Arrangement.SpaceBetween,
    verticalAlignment = Alignment.CenterVertically
  ) {
    if (state.isMemorizing) {
      val secondsLeft = ((state.memorizeMillisLeft + 999) / 1000).toInt()
      StatusChip(text = "👀 Memoriza: ${secondsLeft}s", color = DomainMemoria)
    } else {
      StatusChip(text = "Intentos: ${state.attempts}", color = MaterialTheme.colorScheme.onSurfaceVariant)
    }

    if (state.currentStreak >= 2) {
      StatusChip(text = "🔥 Racha: ${state.currentStreak}", color = DomainAtencion)
    } else {
      StatusChip(text = "Parejas: ${state.matchedPairs}/${state.totalPairs}", color = EmeraldAccent)
    }
  }
}

@Composable
private fun StatusChip(text: String, color: Color) {
  Surface(shape = RoundedCornerShape(12.dp), color = color.copy(alpha = 0.14f)) {
    Text(
      text = text,
      modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
      style = MaterialTheme.typography.labelMedium,
      fontWeight = FontWeight.Bold,
      color = color
    )
  }
}

@Composable
private fun CardTile(card: MemoryCardUi, size: Dp, onClick: () -> Unit, modifier: Modifier = Modifier) {
  val rotation by animateFloatAsState(
    targetValue = if (card.isFaceUp || card.isMatched) 180f else 0f,
    animationSpec = tween(durationMillis = 350),
    label = "cardFlip"
  )

  // Tercer pedido de Ricardo, mismo día que el de los sonidos: "el formato de las
  // cartas... que no sea el típico juego de parejas ocultas" -- un rebote de escala al
  // resolverse la pareja, para que se sienta como un pequeño festejo y no solo un
  // cambio de color. `Animatable` + `LaunchedEffect(card.isMatched)` porque solo debe
  // dispararse UNA vez, en la transición false->true (si usara `animateFloatAsState`
  // con target=1.18f, quedaría agrandada para siempre en vez de asentarse).
  val matchPop = remember { Animatable(1f) }
  LaunchedEffect(card.isMatched) {
    if (card.isMatched) {
      matchPop.animateTo(1.18f, animationSpec = tween(150, easing = FastOutSlowInEasing))
      matchPop.animateTo(1f, animationSpec = tween(220, easing = FastOutSlowInEasing))
    }
  }

  // Esquinas proporcionales al tamaño de carta (squircle) en vez de un radio fijo de
  // 16dp -- a tamaños grandes (perfil adulto mayor) un radio fijo se ve casi cuadrado;
  // proporcional mantiene la misma sensación de forma en todos los perfiles/niveles.
  val cornerRadius = (size.value * 0.24f).dp
  // Degradé en vez de color plano para la cara oculta: mismo azul de dominio
  // (DomainMemoria) en la identidad, pero con un poco más de profundidad/carácter que
  // un bloque sólido.
  val backBrush = Brush.verticalGradient(
    colors = listOf(DomainMemoria, lerp(DomainMemoria, Color.Black, 0.22f))
  )

  Card(
    modifier = modifier
      .size(size)
      .graphicsLayer {
        rotationY = rotation
        cameraDistance = 12f * density
        scaleX = matchPop.value
        scaleY = matchPop.value
      }
      .clickable(enabled = !card.isFaceUp && !card.isMatched) { onClick() },
    shape = RoundedCornerShape(cornerRadius),
    colors = CardDefaults.cardColors(
      containerColor = when {
        card.isMatched -> EmeraldAccent.copy(alpha = 0.18f)
        card.isFaceUp -> MaterialTheme.colorScheme.surface
        else -> Color.Transparent
      }
    ),
    elevation = CardDefaults.cardElevation(defaultElevation = if (card.isFaceUp) 2.dp else 4.dp)
  ) {
    Box(
      modifier = Modifier
        .fillMaxSize()
        .then(if (!card.isFaceUp && !card.isMatched) Modifier.background(backBrush) else Modifier),
      contentAlignment = Alignment.Center
    ) {
      if (rotation > 90f) {
        Box(modifier = Modifier.graphicsLayer { rotationY = 180f }) {
          Text(text = card.symbol, fontSize = (size.value / 2.6f).sp)
        }
      } else {
        // Antes un ícono de "?" -- leía a examen/trivia genérico. Un destello
        // (AutoAwesome) encaja mejor con "acá hay algo que vas a descubrir".
        Icon(
          imageVector = Icons.Default.AutoAwesome,
          contentDescription = "Carta oculta",
          tint = Color.White.copy(alpha = 0.75f),
          modifier = Modifier.size(size / 3.2f)
        )
      }
    }
  }
}

/**
 * Resuelve tamaño de carta Y espaciado JUNTOS -- antes se calculaban por separado
 * (espaciado solo según cantidad de columnas, tamaño de carta encogiéndose para
 * absorber lo que sobraba), y eso generaba un bug real que Ricardo notó jugando en
 * Nivel 7 con perfil "65 o más": en grillas densas, el espaciado generoso del perfil
 * adulto mayor (hasta 12.5mm) por sí solo ya casi llenaba el ancho disponible, exprimiendo
 * la carta hasta el piso de accesibilidad (48dp) -- exactamente lo opuesto a la intención
 * del perfil (cartas GRANDES para adultos mayores). El espaciado ahora también cede
 * cuando hace falta: primero se prueba el espaciado "ideal" según la cantidad de
 * columnas (igual que antes); si con ese espaciado la carta no llega al tamaño ideal del
 * perfil, el espaciado se achica (nunca por debajo de `minSpacingDp`, el piso del
 * perfil) hasta el punto justo en el que la carta alcanza su tamaño ideal -- solo si
 * ni así alcanza, recién ahí la carta se achica de verdad (hasta el piso real de
 * accesibilidad de Android, 48dp).
 *
 * También considera la ALTURA disponible (no solo el ancho): en grillas de pocas filas
 * (2x2, 2x3) el ancho por sí solo dejaba cartas gigantes que además de verse mal hacían
 * trivial memorizar de un vistazo dónde está cada pareja. Con el mínimo entre ancho y
 * alto disponibles, una grilla achatada (pocas filas, muchas columnas) también queda
 * acotada por la altura real del tablero, no solo por el ancho del teléfono.
 */
internal fun resolveCardLayout(
  columns: Int,
  rows: Int,
  availableWidthDp: Float,
  availableHeightDp: Float,
  idealTargetDp: Float,
  minSpacingDp: Float,
  maxSpacingDp: Float
): Pair<Dp, Dp> {
  // Espaciado "preferido" según densidad de columnas (grillas angostas más generosas,
  // grillas densas más ajustadas) -- el mismo criterio de siempre, punto de partida.
  val t = ((columns - 2).toFloat() / (6 - 2)).coerceIn(0f, 1f)
  val preferredSpacingDp = maxSpacingDp - t * (maxSpacingDp - minSpacingDp)

  // Cuánto espaciado como máximo se puede usar en cada eje y que la carta SIGA llegando
  // al tamaño ideal -- si da negativo, ni con espaciado cero entraría el ideal en ese
  // eje, así que no limita el espaciado (la carta se va a achicar más abajo, no el
  // espaciado).
  val spacingCapForWidth = if (columns > 1) (availableWidthDp - idealTargetDp * columns) / (columns - 1) else Float.MAX_VALUE
  val spacingCapForHeight = if (rows > 1) (availableHeightDp - idealTargetDp * rows) / (rows - 1) else Float.MAX_VALUE

  val spacingDp = minOf(preferredSpacingDp, spacingCapForWidth, spacingCapForHeight).coerceAtLeast(minSpacingDp)

  val usableWidth = availableWidthDp - spacingDp * (columns - 1)
  val usableHeight = availableHeightDp - spacingDp * (rows - 1)
  val fitDp = minOf(usableWidth / columns, usableHeight / rows)
  val cardSizeDp = fitDp.coerceIn(MIN_TOUCH_TARGET_DP.value, idealTargetDp)

  return cardSizeDp.dp to spacingDp.dp
}

/**
 * Decoración de fondo no interactiva — la opacidad y la cantidad las decide
 * [com.example.games.parejas.VisualWorkingMemoryDDA] según D(t) (más interferencia a
 * medida que sube la dificultad). Nunca compite con los tonos cálidos de
 * retroalimentación ni con el espacio de toque de las cartas.
 *
 * Antes eran círculos grises de borde duro -- parte del mismo pedido de "que no sea el
 * típico juego de parejas ocultas" se aplica también acá: cada punto ahora es un
 * degradé radial que se desvanece hacia afuera (un "blob" suave) en vez de un disco
 * plano, sin depender de `Modifier.blur` (requiere API 31+; el proyecto soporta desde
 * API 24).
 */
@Composable
private fun DistractorBackdrop(count: Int, opacity: Float, seed: Int) {
  if (count == 0) return
  val baseColor = MaterialTheme.colorScheme.onSurfaceVariant
  Canvas(modifier = Modifier.fillMaxSize()) {
    val random = Random(seed)
    repeat(count) {
      val x = random.nextFloat() * size.width
      val y = random.nextFloat() * size.height
      val radius = size.minDimension * (0.06f + random.nextFloat() * 0.07f)
      val center = Offset(x, y)
      drawCircle(
        brush = Brush.radialGradient(
          colors = listOf(baseColor.copy(alpha = opacity), baseColor.copy(alpha = 0f)),
          center = center,
          radius = radius
        ),
        radius = radius,
        center = center
      )
    }
  }
}
