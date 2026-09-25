package com.example.viewmodel

import android.app.Application
import android.content.Context
import android.os.Build
import android.os.VibrationEffect
import android.os.Vibrator
import android.os.VibratorManager
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.example.data.NeuroVidaRepository
import com.example.model.*
import com.example.notification.CognitiveReminderWorker
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

enum class AppTab(val title: String, val iconName: String) {
  HOY("Hoy", "Today"),
  JUEGOS("Juegos", "SportsEsports"),
  PROGRESO("Progreso", "Insights"),
  AJUSTES("Ajustes", "Settings")
}

data class ActiveGameSession(
  val gameDef: GameDefinition,
  val level: Int,
  val timed: Boolean,
  val isDailyFlow: Boolean = false,
  // Progresión sin techo más allá de nivel 5 (Experto) — ver GameProgressEntity.masteryStreak.
  val intensity: Int = 0,
  // Partida en pausa que se retoma: se relanza Unity con este id de lanzamiento y la partida sigue donde quedó.
  val resumeLaunchId: String? = null
)

data class DomainStats(
  val domain: DomainType,
  val averageScore: Int,
  val totalPlayed: Int,
  val competenceLevel: Int // 1 to 5
)

class NeuroVidaViewModel(application: Application) : AndroidViewModel(application) {
  private val repository = NeuroVidaRepository(application)

  val userSettings = repository.userSettings
  val allProfiles = repository.allProfiles
  val gameHistory = repository.gameHistory
  val gameLevels = repository.gameLevels
  val gameIntensity = repository.gameIntensity
  val weeklyChallengeProgress = repository.weeklyChallengeProgress

  // Meta-progresión por dominio (etapa 4): DomainMasteryInfo deriva tier/label
  // a partir de la XP cruda que guarda el repositorio.
  val domainMasteryInfo: StateFlow<List<DomainMasteryInfo>> = repository.domainMastery.map { xpMap ->
    DomainType.values().map { d -> DomainMasteryInfo(domain = d, xp = xpMap[d] ?: 0) }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, DomainType.values().map { DomainMasteryInfo(it, 0) })

  // Ranking ELO por juego (idea de Ricardo, 20-sep): GameRankInfo deriva tier/división
  // a partir del rating crudo que guarda el repositorio, uno por cada uno de los 9 juegos.
  val gameRanks: StateFlow<List<GameRankInfo>> = repository.gameRanks.map { ratingMap ->
    GameRegistry.allGames.map { g -> GameRankInfo(gameId = g.id, rating = ratingMap[g.id] ?: 0) }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, GameRegistry.allGames.map { GameRankInfo(it.id, 0) })
  val dailySession = repository.dailySession

  /** Nivel (0..1) por juego para Progreso: rating del DDA comun; en Secuencia/Parejas (motores propios) se
   *  aproxima con el nivel 1-5 si ya se jugaron; null = sin medir. */
  val gameLevelsForProgress: StateFlow<Map<String, Float?>> = combine(
    repository.gameDdaRating, gameLevels, gameHistory
  ) { dda, levels, hist ->
    val played = hist.map { it.gameId }.toSet()
    GameRegistry.allGames.associate { g ->
      val r = dda[g.id] ?: -1f
      g.id to when {
        r >= 0f -> r
        g.id in played -> ((levels[g.id] ?: 1) - 1) / 5f + 0.1f
        else -> null
      }
    }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, emptyMap())

  private val _currentTab = MutableStateFlow(AppTab.HOY)
  val currentTab: StateFlow<AppTab> = _currentTab.asStateFlow()

  private val _activeGame = MutableStateFlow<ActiveGameSession?>(null)
  val activeGame: StateFlow<ActiveGameSession?> = _activeGame.asStateFlow()

  private val _lastResult = MutableStateFlow<Pair<GamePlayResult, Boolean>?>(null)
  val lastResult: StateFlow<Pair<GamePlayResult, Boolean>?> = _lastResult.asStateFlow()

  // Computed streak
  val currentStreak: StateFlow<Int> = combine(gameHistory) { history ->
    repository.calculateStreak(history[0])
  }.stateIn(viewModelScope, SharingStarted.Eagerly, 0)

  // Last 7 days activity (Day letter to boolean)
  val last7DaysActivity: StateFlow<List<Pair<String, Boolean>>> = combine(gameHistory) { history ->
    repository.getLast7DaysActivity(history[0])
  }.stateIn(viewModelScope, SharingStarted.Eagerly, emptyList())

  // Domain competencies
  val domainStats: StateFlow<List<DomainStats>> = combine(gameHistory, gameLevels) { history, levels ->
    DomainType.values().map { domain ->
      val gamesInDomain = GameRegistry.allGames.filter { it.domain == domain }
      val resultsInDomain = history.filter { r -> gamesInDomain.any { it.id == r.gameId } }
      val avg = if (resultsInDomain.isEmpty()) 50 else resultsInDomain.map { it.score }.average().toInt()
      val highestLvl = gamesInDomain.maxOfOrNull { levels[it.id] ?: 1 } ?: 1
      DomainStats(
        domain = domain,
        averageScore = avg,
        totalPlayed = resultsInDomain.size,
        competenceLevel = highestLvl
      )
    }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, emptyList())

  // Sparkline data for sessions (counts per day over 7 days)
  val sessionsSparkline: StateFlow<List<Float>> = combine(gameHistory) { history ->
    val last7 = repository.getLast7DaysActivity(history[0])
    last7.map { if (it.second) 1f else 0f }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, listOf(0f, 1f, 1f, 0f, 1f, 1f, 1f))

  // Sparkline data for recent scores
  val scoresSparkline: StateFlow<List<Float>> = combine(gameHistory) { history ->
    val recent = history[0].take(7).reversed()
    if (recent.isEmpty()) listOf(50f, 60f, 70f) else recent.map { it.score.toFloat() }
  }.stateIn(viewModelScope, SharingStarted.Eagerly, listOf(60f, 75f, 80f, 85f))

  init {
    viewModelScope.launch { com.example.bridge.UnityResultBus.results.collect { onUnityResult(it) } }
    viewModelScope.launch {
      userSettings.collect { settings ->
        if (settings.notificationsEnabled) {
          CognitiveReminderWorker.scheduleDailyReminder(
            getApplication(),
            settings.reminderHour,
            settings.reminderMinute
          )
        } else {
          CognitiveReminderWorker.cancelReminder(getApplication())
        }
      }
    }
  }

  fun setTab(tab: AppTab) {
    _currentTab.value = tab
    _activeGame.value = null
    _lastResult.value = null
  }

  fun startDailySession() {
    val session = dailySession.value
    val nextGameId = if (session.completedCount < session.gameIds.size) {
      session.gameIds[session.completedCount]
    } else {
      session.gameIds.firstOrNull() ?: "calculo"
    }
    launchGame(nextGameId, isDailyFlow = true)
  }

  fun getEffectiveLevelForGame(gameId: String): Int {
    val settings = userSettings.value
    val gameDef = GameRegistry.getById(gameId) ?: return 1
    return when (settings.difficultyMode) {
      DifficultyMode.ADAPTIVE -> gameLevels.value[gameId] ?: 1
      DifficultyMode.PRINCIPIANTE -> 1
      DifficultyMode.INTERMEDIO -> 3
      DifficultyMode.AVANZADO -> 5
      DifficultyMode.CUSTOM -> {
        when (gameDef.domain) {
          DomainType.MEMORIA -> settings.difficultyMemoria
          DomainType.ATENCION -> settings.difficultyAtencion
          DomainType.RAZONAMIENTO -> settings.difficultyRazonamiento
          DomainType.LENGUAJE -> settings.difficultyLenguaje
          DomainType.CALCULO -> settings.difficultyCalculo
          DomainType.VELOCIDAD -> settings.difficultyVelocidad
        }.coerceIn(1, 5)
      }
    }
  }

  // Solo el modo adaptativo acumula masteryStreak (juego real, historial real);
  // los modos de dificultad fija (Principiante/Intermedio/Avanzado/Personalizada)
  // no tienen ese concepto porque el usuario ya eligió congelar el nivel.
  fun getEffectiveIntensityForGame(gameId: String): Int {
    if (userSettings.value.difficultyMode != DifficultyMode.ADAPTIVE) return 0
    return gameIntensity.value[gameId] ?: 0
  }

  fun launchGame(gameId: String, customLevel: Int? = null, customTimed: Boolean? = null, isDailyFlow: Boolean = false) {
    // Si ese juego quedó en pausa ("Salir" del menú de pausa), se retoma en vez de empezar de cero. "Jugar de
    // nuevo" (customLevel) siempre es una partida nueva. Abrir otro juego descarta la pausa (Unity recarga).
    val paused = pausedGame
    pausedGame = null
    if (paused != null && paused.first.gameDef.id == gameId && customLevel == null) {
      _activeGame.value = paused.first.copy(
        isDailyFlow = isDailyFlow || paused.first.isDailyFlow,
        resumeLaunchId = paused.second
      )
      _lastResult.value = null
      return
    }
    val def = GameRegistry.getById(gameId) ?: return
    val lvl = customLevel ?: getEffectiveLevelForGame(gameId)
    val timed = customTimed ?: userSettings.value.defaultTimed
    val intensity = if (lvl >= 5) getEffectiveIntensityForGame(gameId) else 0
    _activeGame.value = ActiveGameSession(def, lvl, timed, isDailyFlow, intensity)
    _lastResult.value = null
  }

  /**
   * Resultado de una partida jugada en Unity (llega por [com.example.bridge.UnityResultBus]). Se guarda una sola
   * vez en Room; si coincide con la sesión activa se muestra la pantalla de resultado de la app.
   * Las partidas lanzadas desde los botones de depuración (sin sesión activa) solo se guardan.
   */
  fun onUnityResult(result: GamePlayResult) {
    val current = _activeGame.value
    viewModelScope.launch {
      val levelUp = repository.recordGameResult(result)
      if (current != null && current.gameDef.id == result.gameId) {
        _lastResult.value = Pair(result, levelUp)
        _activeGame.value = null
        triggerHapticFeedback(if (result.score >= 70) HapticType.SUCCESS else HapticType.LIGHT)
      }
    }
  }

  /** true entre que se lanza Unity y que vuelve la app (ver [onReturnedFromGame] / [onHostResumed]). */
  private var awaitingUnityReturn = false

  /** Partida que quedó en pausa dentro de Unity (sesión + id de lanzamiento), para retomarla en [launchGame]. */
  //
  // Se guarda en disco (SharedPreferences), no solo en memoria: al salir al escritorio Android puede destruir la
  // app (y este ViewModel) mientras el proceso de Unity, que es aparte, sigue vivo con la partida en pausa. Sin
  // esto, al volver y tocar Play se generaba un id de lanzamiento nuevo y Unity empezaba una partida desde cero.
  private val pausePrefs by lazy {
    getApplication<Application>().getSharedPreferences("paused_game", android.content.Context.MODE_PRIVATE)
  }

  private var pausedGame: Pair<ActiveGameSession, String>?
    get() {
      val gameId = pausePrefs.getString("gameId", null) ?: return null
      val launchId = pausePrefs.getString("launchId", null) ?: return null
      val def = GameRegistry.getById(gameId) ?: return null
      return ActiveGameSession(
        gameDef = def,
        level = pausePrefs.getInt("level", 1),
        timed = pausePrefs.getBoolean("timed", false),
        isDailyFlow = pausePrefs.getBoolean("daily", false),
        intensity = pausePrefs.getInt("intensity", 0)
      ) to launchId
    }
    set(value) {
      val e = pausePrefs.edit()
      if (value == null) {
        e.clear()
      } else {
        val (session, launchId) = value
        e.putString("gameId", session.gameDef.id)
          .putString("launchId", launchId)
          .putInt("level", session.level)
          .putBoolean("timed", session.timed)
          .putBoolean("daily", session.isDailyFlow)
          .putInt("intensity", session.intensity)
      }
      e.apply()
    }

  /** `UnityGameHost` acaba de traer al frente la pantalla de juego (Unity). */
  fun onUnityLaunched() {
    awaitingUnityReturn = true
  }

  /**
   * Unity devolvió la app al frente ("Continuar" o Atrás) -- llega por `MainActivity.onNewIntent`, antes de
   * `onResume`. Si la partida terminó, [resultJson] trae su resultado y se muestra al instante (sin esperar al
   * broadcast de respaldo; [com.example.bridge.UnityResultInbox] evita procesarlo dos veces). Sin resultado =
   * salió a mitad de partida: se cierra la sesión.
   */
  fun onReturnedFromGame(launchId: String?, resultJson: String?, paused: Boolean = false) {
    awaitingUnityReturn = false
    if (paused) {
      // "Salir" del menú de pausa: la partida sigue viva (en pausa) en Unity; se vuelve al menú de la app.
      val session = _activeGame.value
      if (session != null && launchId != null) pausedGame = session to launchId
      _activeGame.value = null
      return
    }
    if (resultJson != null) {
      if (com.example.bridge.UnityResultInbox.claim(launchId)) {
        val result = com.example.bridge.NativeReceiver.parse(resultJson)
        if (result != null) {
          onUnityResult(result)
          return
        }
      } else {
        return // ya llegó por el broadcast: onUnityResult lo está mostrando
      }
    }
    if (_lastResult.value == null) _activeGame.value = null
  }

  /**
   * La app volvió a primer plano. Si se esperaba la vuelta de Unity y no llegó (Unity se cerró o se cayó en
   * vez de devolver la app), se cierra la sesión para no dejar la pantalla de carga colgada.
   */
  fun onHostResumed() {
    if (!awaitingUnityReturn) return
    awaitingUnityReturn = false
    if (_lastResult.value == null) _activeGame.value = null
  }

  fun finishActiveGame(score: Int, correct: Int, total: Int) {
    val current = _activeGame.value ?: return
    val result = GamePlayResult(
      gameId = current.gameDef.id,
      score = score.coerceIn(0, 100),
      correctAnswers = correct,
      totalTrials = total,
      timed = current.timed,
      level = current.level
    )
    viewModelScope.launch {
      val levelUp = repository.recordGameResult(result)
      _lastResult.value = Pair(result, levelUp)
      _activeGame.value = null
    }

    triggerHapticFeedback(if (score >= 70) HapticType.SUCCESS else HapticType.LIGHT)
  }

  fun continueDailyFlow() {
    _lastResult.value = null
    val session = dailySession.value
    if (session.completedCount < session.gameIds.size) {
      val nextId = session.gameIds[session.completedCount]
      launchGame(nextId, isDailyFlow = true)
    } else {
      // Session fully finished, go home
      setTab(AppTab.HOY)
    }
  }

  fun closeGameOrResult() {
    _activeGame.value = null
    _lastResult.value = null
  }

  fun createNewProfile(
    name: String,
    avatar: String = "🧠",
    difficultyMode: DifficultyMode = DifficultyMode.ADAPTIVE,
    weeklyGoal: Int = 4,
    cognitiveAssistance: Boolean = true
  ) {
    viewModelScope.launch {
      repository.createProfile(name, avatar, difficultyMode, weeklyGoal, cognitiveAssistance)
      triggerHapticFeedback(HapticType.SUCCESS)
    }
  }

  fun switchProfile(profileId: Long) {
    viewModelScope.launch {
      repository.switchActiveProfile(profileId)
      triggerHapticFeedback(HapticType.LIGHT)
    }
  }

  fun deleteProfile(profileId: Long) {
    viewModelScope.launch {
      repository.deleteProfile(profileId)
      triggerHapticFeedback(HapticType.MEDIUM)
    }
  }

  fun updateDifficultyPreferences(
    mode: DifficultyMode,
    memoria: Int,
    atencion: Int,
    razonamiento: Int,
    lenguaje: Int,
    calculo: Int,
    velocidad: Int,
    assistance: Boolean,
    timeScale: Float
  ) {
    viewModelScope.launch {
      val profileId = userSettings.value.id
      repository.updateDifficultyPreferences(
        profileId = profileId,
        mode = mode,
        memoria = memoria,
        atencion = atencion,
        razonamiento = razonamiento,
        lenguaje = lenguaje,
        calculo = calculo,
        velocidad = velocidad,
        assistance = assistance,
        timeScale = timeScale
      )
      triggerHapticFeedback(HapticType.SUCCESS)
    }
  }

  fun updateSettings(
    name: String,
    weeklyGoal: Int,
    defaultTimed: Boolean,
    sound: Boolean,
    haptics: Boolean,
    notificationsEnabled: Boolean = userSettings.value.notificationsEnabled,
    reminderHour: Int = userSettings.value.reminderHour,
    reminderMinute: Int = userSettings.value.reminderMinute,
    avatar: String = userSettings.value.avatar,
    difficultyMode: DifficultyMode = userSettings.value.difficultyMode,
    themeMode: ThemeMode = userSettings.value.themeMode,
    language: AppLanguage = userSettings.value.language
  ) {
    viewModelScope.launch {
      val current = userSettings.value
      val updated = current.copy(
        name = name,
        avatar = avatar,
        weeklyGoal = weeklyGoal,
        defaultTimed = defaultTimed,
        soundEnabled = sound,
        hapticsEnabled = haptics,
        notificationsEnabled = notificationsEnabled,
        reminderHour = reminderHour,
        reminderMinute = reminderMinute,
        difficultyMode = difficultyMode,
        themeMode = themeMode,
        language = language
      )
      repository.updateSettings(updated)

      if (notificationsEnabled) {
        CognitiveReminderWorker.scheduleDailyReminder(
          getApplication(),
          reminderHour,
          reminderMinute
        )
      } else {
        CognitiveReminderWorker.cancelReminder(getApplication())
      }
    }
  }

  /**
   * Onboarding de edad (piloto de perfiles por edad, 20-sep) -- función propia en vez de
   * sumarse a `updateSettings` porque es una acción de una sola vez que además decide si
   * se muestra la pantalla de onboarding (`userSettings.ageBand == null`), no una edición
   * más de Ajustes.
   */
  fun setAgeBand(band: AgeBand) {
    viewModelScope.launch {
      repository.updateSettings(userSettings.value.copy(ageBand = band))
    }
  }

  fun triggerTestNotification() {
    CognitiveReminderWorker.triggerImmediateTestReminder(getApplication())
  }

  fun resetData() {
    viewModelScope.launch {
      repository.resetData()
      _activeGame.value = null
      _lastResult.value = null
    }
  }

  enum class HapticType { LIGHT, MEDIUM, SUCCESS, ERROR }

  fun triggerHapticFeedback(type: HapticType = HapticType.LIGHT) {
    if (!userSettings.value.hapticsEnabled) return
    try {
      val vibrator = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
        val vibratorManager = getApplication<Application>().getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? VibratorManager
        vibratorManager?.defaultVibrator
      } else {
        @Suppress("DEPRECATION")
        getApplication<Application>().getSystemService(Context.VIBRATOR_SERVICE) as? Vibrator
      }

      vibrator?.let { v ->
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
          val effect = when (type) {
            HapticType.LIGHT -> VibrationEffect.createPredefined(VibrationEffect.EFFECT_TICK)
            HapticType.MEDIUM -> VibrationEffect.createPredefined(VibrationEffect.EFFECT_CLICK)
            HapticType.SUCCESS -> VibrationEffect.createOneShot(100, VibrationEffect.DEFAULT_AMPLITUDE)
            HapticType.ERROR -> VibrationEffect.createWaveform(longArrayOf(0, 50, 50, 50), -1)
          }
          v.vibrate(effect)
        } else {
          @Suppress("DEPRECATION")
          v.vibrate(40)
        }
      }
    } catch (_: Exception) {}
  }
}
