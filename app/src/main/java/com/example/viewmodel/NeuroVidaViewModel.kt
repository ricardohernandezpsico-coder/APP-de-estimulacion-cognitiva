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
  val isDailyFlow: Boolean = false
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
  val dailySession = repository.dailySession

  private val _currentTab = MutableStateFlow(AppTab.HOY)
  val currentTab: StateFlow<AppTab> = _currentTab.asStateFlow()

  private val _activeGame = MutableStateFlow<ActiveGameSession?>(null)
  val activeGame: StateFlow<ActiveGameSession?> = _activeGame.asStateFlow()

  private val _lastResult = MutableStateFlow<Pair<GamePlayResult, Boolean>?>(null)
  val lastResult: StateFlow<Pair<GamePlayResult, Boolean>?> = _lastResult.asStateFlow()

  private val _newAchievementUnlocked = MutableStateFlow<AchievementItem?>(null)
  val newAchievementUnlocked: StateFlow<AchievementItem?> = _newAchievementUnlocked.asStateFlow()

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

  fun launchGame(gameId: String, customLevel: Int? = null, customTimed: Boolean? = null, isDailyFlow: Boolean = false) {
    val def = GameRegistry.getById(gameId) ?: return
    val lvl = customLevel ?: getEffectiveLevelForGame(gameId)
    val timed = customTimed ?: userSettings.value.defaultTimed
    _activeGame.value = ActiveGameSession(def, lvl, timed, isDailyFlow)
    _lastResult.value = null
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
    difficultyMode: DifficultyMode = userSettings.value.difficultyMode
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
        difficultyMode = difficultyMode
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

  fun triggerTestNotification() {
    CognitiveReminderWorker.triggerImmediateTestReminder(getApplication())
  }

  fun getAchievements(): List<AchievementItem> = repository.getAllAchievements()

  fun dismissAchievementBanner() {
    _newAchievementUnlocked.value = null
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
