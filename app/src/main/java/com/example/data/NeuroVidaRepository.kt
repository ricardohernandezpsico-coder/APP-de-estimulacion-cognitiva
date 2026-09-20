package com.example.data

import android.content.Context
import com.example.data.local.*
import com.example.model.*
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.*

class NeuroVidaRepository(
  context: Context,
  private val database: NeuroVidaDatabase = NeuroVidaDatabase.getDatabase(context)
) {
  private val repositoryScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
  private val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())

  private val gameResultDao = database.gameResultDao()
  private val gameProgressDao = database.gameProgressDao()
  private val dailySessionDao = database.dailySessionDao()
  private val achievementDao = database.achievementDao()
  private val userProfileDao = database.userProfileDao()

  private fun getTodayDateKey(): String = synchronized(dateFormat) {
    dateFormat.format(Date())
  }

  private fun formatDate(timestamp: Long): String = synchronized(dateFormat) {
    dateFormat.format(Date(timestamp))
  }

  // 1. Reactive User Profiles & Settings Flows from Room
  val allProfiles: StateFlow<List<UserSettings>> = userProfileDao.getAllProfiles()
    .map { list -> list.map { it.toDomain() } }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = emptyList()
    )

  val userSettings: StateFlow<UserSettings> = userProfileDao.getActiveProfile()
    .map { entity -> entity?.toDomain() ?: userProfileDao.getUserProfileSync()?.toDomain() ?: UserSettings() }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = UserSettings()
    )

  // 2. Reactive Game History Flow from Room
  val gameHistory: StateFlow<List<GamePlayResult>> = gameResultDao.getAllResults()
    .map { list -> list.map { it.toDomain() } }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = defaultSeedHistory()
    )

  // 3. Reactive Game Levels Map from Room
  val gameLevels: StateFlow<Map<String, Int>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Int>()
      GameRegistry.allGames.forEach { g -> map[g.id] = 1 }
      list.forEach { p -> map[p.gameId] = p.currentLevel }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to 1 }
    )

  // 4. Reactive Unlocked Achievements Set from Room
  val unlockedAchievements: StateFlow<Set<String>> = achievementDao.getAllUnlocked()
    .map { list -> list.map { it.id }.toSet() }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = emptySet()
    )

  // 5. Reactive Daily Session State from Room
  private val _dailySession = MutableStateFlow(
    DailySessionState(
      dateKey = getTodayDateKey(),
      gameIds = listOf("calculo", "parejas", "stroop"),
      completedCount = 0
    )
  )
  val dailySession: StateFlow<DailySessionState> = _dailySession.asStateFlow()

  init {
    repositoryScope.launch {
      initializeDatabaseDefaults()
      observeDailySession()
    }
  }

  private suspend fun initializeDatabaseDefaults() {
    // 1. User Profile in Room
    val existingProfiles = userProfileDao.getAllProfilesSync()
    if (existingProfiles.isEmpty()) {
      userProfileDao.insertOrUpdate(
        UserProfileEntity(
          name = "Ana",
          avatar = "🧠",
          isActive = true,
          weeklyGoal = 4,
          difficultyMode = "ADAPTIVE",
          difficultyMemoria = 2,
          difficultyAtencion = 2,
          difficultyRazonamiento = 2,
          difficultyLenguaje = 2,
          difficultyCalculo = 2,
          difficultyVelocidad = 2,
          cognitiveAssistance = true
        )
      )
    }

    // 2. Game Results in Room
    if (gameResultDao.getCount() == 0) {
      gameResultDao.insertAll(defaultSeedHistory().map { it.toEntity() })
    }

    // 3. Game Progress in Room
    val existingProgress = gameProgressDao.getAllProgressSync()
    if (existingProgress.isEmpty()) {
      val initialProgress = GameRegistry.allGames.map { game ->
        GameProgressEntity(
          gameId = game.id,
          currentLevel = 1,
          highestScore = when (game.id) {
            "calculo" -> 80
            "parejas" -> 85
            "stroop" -> 90
            else -> 0
          },
          totalGamesPlayed = if (listOf("calculo", "parejas", "stroop").contains(game.id)) 1 else 0,
          lastPlayedTimestamp = System.currentTimeMillis()
        )
      }
      gameProgressDao.insertAll(initialProgress)
    }

    // 4. Daily Session in Room
    val today = getTodayDateKey()
    val session = dailySessionDao.getDailySessionSync(today)
    if (session == null) {
      val allResults = gameResultDao.getAllResultsSync().map { it.toDomain() }
      val newQueue = pickSessionQueue(allResults)
      val newEntity = DailySessionEntity(
        dateKey = today,
        gameIdsRaw = newQueue.joinToString(","),
        completedCount = 0,
        scoresRaw = ""
      )
      dailySessionDao.insertOrUpdate(newEntity)
      _dailySession.value = newEntity.toDomain()
    } else {
      _dailySession.value = session.toDomain()
    }
  }

  private fun observeDailySession() {
    repositoryScope.launch {
      val today = getTodayDateKey()
      dailySessionDao.getDailySession(today).collect { entity ->
        if (entity != null) {
          _dailySession.value = entity.toDomain()
        }
      }
    }
  }

  suspend fun updateSettings(newSettings: UserSettings) = withContext(Dispatchers.IO) {
    userProfileDao.insertOrUpdate(newSettings.toEntity())
  }

  suspend fun createProfile(
    name: String,
    avatar: String = "🧠",
    difficultyMode: DifficultyMode = DifficultyMode.ADAPTIVE,
    weeklyGoal: Int = 4,
    cognitiveAssistance: Boolean = true
  ): Long = withContext(Dispatchers.IO) {
    val newEntity = UserProfileEntity(
      id = 0L,
      name = name.ifBlank { "Nuevo Perfil" },
      avatar = avatar.ifBlank { "🧠" },
      isActive = true,
      weeklyGoal = weeklyGoal,
      difficultyMode = difficultyMode.name,
      cognitiveAssistance = cognitiveAssistance
    )
    val newId = userProfileDao.insertOrUpdate(newEntity)
    userProfileDao.setActiveProfile(newId)
    newId
  }

  suspend fun switchActiveProfile(profileId: Long) = withContext(Dispatchers.IO) {
    userProfileDao.setActiveProfile(profileId)
  }

  suspend fun deleteProfile(profileId: Long) = withContext(Dispatchers.IO) {
    val all = userProfileDao.getAllProfilesSync()
    if (all.size > 1) {
      userProfileDao.deleteProfileById(profileId)
      val active = userProfileDao.getActiveProfileSync()
      if (active == null) {
        val remaining = userProfileDao.getAllProfilesSync().firstOrNull()
        if (remaining != null) {
          userProfileDao.setActiveProfile(remaining.id)
        }
      }
    }
  }

  suspend fun updateDifficultyPreferences(
    profileId: Long,
    mode: DifficultyMode,
    memoria: Int,
    atencion: Int,
    razonamiento: Int,
    lenguaje: Int,
    calculo: Int,
    velocidad: Int,
    assistance: Boolean,
    timeScale: Float
  ) = withContext(Dispatchers.IO) {
    val existing = userProfileDao.getProfileById(profileId) ?: userProfileDao.getActiveProfileSync()
    if (existing != null) {
      val updated = existing.copy(
        difficultyMode = mode.name,
        difficultyMemoria = memoria.coerceIn(1, 5),
        difficultyAtencion = atencion.coerceIn(1, 5),
        difficultyRazonamiento = razonamiento.coerceIn(1, 5),
        difficultyLenguaje = lenguaje.coerceIn(1, 5),
        difficultyCalculo = calculo.coerceIn(1, 5),
        difficultyVelocidad = velocidad.coerceIn(1, 5),
        cognitiveAssistance = assistance,
        timeScaleFactor = timeScale.coerceIn(0.5f, 2.0f)
      )
      userProfileDao.insertOrUpdate(updated)
    }
  }

  private fun defaultSeedHistory(): List<GamePlayResult> {
    val now = System.currentTimeMillis()
    val day = 24 * 60 * 60 * 1000L
    return listOf(
      GamePlayResult(gameId = "calculo", score = 80, correctAnswers = 8, totalTrials = 10, timed = false, level = 1, timestamp = now - 2 * day),
      GamePlayResult(gameId = "parejas", score = 85, correctAnswers = 9, totalTrials = 10, timed = false, level = 1, timestamp = now - 1 * day),
      GamePlayResult(gameId = "stroop", score = 90, correctAnswers = 9, totalTrials = 10, timed = false, level = 1, timestamp = now - 3 * 3600 * 1000L)
    )
  }

  private fun pickSessionQueue(history: List<GamePlayResult>): List<String> {
    val domainScores = mutableMapOf<DomainType, MutableList<Int>>()
    DomainType.values().forEach { domainScores[it] = mutableListOf() }
    history.forEach { res ->
      val game = GameRegistry.getById(res.gameId)
      if (game != null) {
        domainScores[game.domain]?.add(res.score)
      }
    }

    val sortedDomains = DomainType.values().sortedBy { domain ->
      val scores = domainScores[domain].orEmpty()
      if (scores.isEmpty()) 0.0 else scores.average()
    }

    val selected = mutableListOf<String>()
    sortedDomains.take(3).forEach { domain ->
      val gameInDomain = GameRegistry.allGames.filter { it.domain == domain }.randomOrNull()
      if (gameInDomain != null) {
        selected.add(gameInDomain.id)
      }
    }

    while (selected.size < 3) {
      val candidate = GameRegistry.allGames.random().id
      if (!selected.contains(candidate)) selected.add(candidate)
    }
    return selected
  }

  suspend fun recordGameResult(result: GamePlayResult): Boolean = withContext(Dispatchers.IO) {
    // 1. Add to Room game_results table
    gameResultDao.insert(result.toEntity())

    // 2. Adaptive level calculation and progress update in Room
    val currentProgress = gameProgressDao.getProgressForGameSync(result.gameId)
      ?: GameProgressEntity(gameId = result.gameId, currentLevel = 1)

    val activeProfile = userProfileDao.getActiveProfileSync() ?: userProfileDao.getUserProfileSync()
    val isAdaptive = activeProfile?.difficultyMode == "ADAPTIVE"

    val playedLevel = result.level
    var newLevel = playedLevel
    var didLevelUp = false
    if (isAdaptive) {
      if (result.score >= 85) {
        if (playedLevel < 5) {
          newLevel = playedLevel + 1
          didLevelUp = true
        }
      } else if (result.score <= 45 && playedLevel > 1) {
        newLevel = playedLevel - 1
      }
    }

    val updatedProgress = currentProgress.copy(
      currentLevel = newLevel,
      highestScore = maxOf(currentProgress.highestScore, result.score),
      totalGamesPlayed = currentProgress.totalGamesPlayed + 1,
      lastPlayedTimestamp = result.timestamp
    )
    gameProgressDao.insertOrUpdate(updatedProgress)

    // 3. Advance Daily Session in Room if game matches current queue
    val today = getTodayDateKey()
    val currentSessionEntity = dailySessionDao.getDailySessionSync(today)
    if (currentSessionEntity != null) {
      val domainSession = currentSessionEntity.toDomain()
      if (domainSession.completedCount < domainSession.gameIds.size) {
        val expectedGame = domainSession.gameIds[domainSession.completedCount]
        if (expectedGame == result.gameId) {
          val updatedCount = domainSession.completedCount + 1
          val updatedScores = domainSession.scores + result.score
          val updatedSession = domainSession.copy(completedCount = updatedCount, scores = updatedScores)
          dailySessionDao.insertOrUpdate(updatedSession.toEntity())
          _dailySession.value = updatedSession
        }
      }
    }

    // 4. Check & Unlock Achievements in Room
    val allResults = gameResultDao.getAllResultsSync().map { it.toDomain() }
    val allProgress = gameProgressDao.getAllProgressSync().associate { it.gameId to it.currentLevel }
    checkAchievements(allResults, allProgress)

    didLevelUp
  }

  private suspend fun checkAchievements(history: List<GamePlayResult>, levels: Map<String, Int>) {
    val unlocked = achievementDao.getAllUnlockedSync().map { it.id }.toSet()
    val newlyUnlocked = mutableListOf<String>()

    if (history.isNotEmpty() && !unlocked.contains("primera")) {
      newlyUnlocked.add("primera")
    }

    val uniqueDomains = history.mapNotNull { GameRegistry.getById(it.gameId)?.domain }.toSet()
    if (uniqueDomains.size >= 3 && !unlocked.contains("dominios")) {
      newlyUnlocked.add("dominios")
    }

    val uniqueGames = history.map { it.gameId }.toSet()
    if (uniqueGames.size >= 9 && !unlocked.contains("coleccionista")) {
      newlyUnlocked.add("coleccionista")
    }

    val streak = calculateStreak(history)
    if (streak >= 3 && !unlocked.contains("racha3")) {
      newlyUnlocked.add("racha3")
    }
    if (streak >= 7 && !unlocked.contains("racha7")) {
      newlyUnlocked.add("racha7")
    }

    if (history.any { it.score >= 85 } && !unlocked.contains("puntaje85")) {
      newlyUnlocked.add("puntaje85")
    }
    if (history.any { it.score >= 100 } && !unlocked.contains("perfeccion")) {
      newlyUnlocked.add("perfeccion")
    }

    if (levels.values.any { it >= 3 } && !unlocked.contains("nivel3")) {
      newlyUnlocked.add("nivel3")
    }
    if (levels.values.any { it >= 5 } && !unlocked.contains("nivel5")) {
      newlyUnlocked.add("nivel5")
    }

    if (history.size >= 10 && !unlocked.contains("sesiones10")) {
      newlyUnlocked.add("sesiones10")
    }
    if (history.size >= 50 && !unlocked.contains("sesiones50")) {
      newlyUnlocked.add("sesiones50")
    }

    val sessionsThisWeek = getSessionsThisWeek(history)
    if (sessionsThisWeek >= userSettings.value.weeklyGoal && !unlocked.contains("meta_semanal")) {
      newlyUnlocked.add("meta_semanal")
    }

    if (newlyUnlocked.isNotEmpty()) {
      achievementDao.insertAll(newlyUnlocked.map { AchievementEntity(id = it) })
    }
  }

  fun calculateStreak(history: List<GamePlayResult>): Int {
    if (history.isEmpty()) return 0
    val daysWithSessions = history.map { formatDate(it.timestamp) }.toSet()
    val cal = Calendar.getInstance()

    var streak = 0
    val todayKey = synchronized(dateFormat) { dateFormat.format(cal.time) }
    val playedToday = daysWithSessions.contains(todayKey)

    if (!playedToday) {
      cal.add(Calendar.DAY_OF_YEAR, -1)
      val yesterdayKey = synchronized(dateFormat) { dateFormat.format(cal.time) }
      if (!daysWithSessions.contains(yesterdayKey)) {
        return 0
      }
      streak = 1
      cal.add(Calendar.DAY_OF_YEAR, -1)
    } else {
      streak = 1
      cal.add(Calendar.DAY_OF_YEAR, -1)
    }

    while (true) {
      val prevKey = synchronized(dateFormat) { dateFormat.format(cal.time) }
      if (daysWithSessions.contains(prevKey)) {
        streak++
        cal.add(Calendar.DAY_OF_YEAR, -1)
      } else {
        break
      }
    }
    return streak
  }

  fun getSessionsThisWeek(history: List<GamePlayResult>): Int {
    val cal = Calendar.getInstance()
    cal.firstDayOfWeek = Calendar.MONDAY
    cal.set(Calendar.DAY_OF_WEEK, Calendar.MONDAY)
    cal.set(Calendar.HOUR_OF_DAY, 0)
    cal.set(Calendar.MINUTE, 0)
    cal.set(Calendar.SECOND, 0)
    val startOfWeek = cal.timeInMillis
    return history.count { it.timestamp >= startOfWeek }
  }

  fun getLast7DaysActivity(history: List<GamePlayResult>): List<Pair<String, Boolean>> {
    val daysWithSessions = history.map { formatDate(it.timestamp) }.toSet()
    val cal = Calendar.getInstance()
    val days = mutableListOf<Pair<String, Boolean>>()
    val dayNames = arrayOf("D", "L", "M", "X", "J", "V", "S")

    for (i in 6 downTo 0) {
      val tempCal = Calendar.getInstance()
      tempCal.add(Calendar.DAY_OF_YEAR, -i)
      val key = synchronized(dateFormat) { dateFormat.format(tempCal.time) }
      val dayLetter = dayNames[tempCal.get(Calendar.DAY_OF_WEEK) - 1]
      days.add(Pair(dayLetter, daysWithSessions.contains(key)))
    }
    return days
  }

  fun getAllAchievements(): List<AchievementItem> {
    val unlocked = unlockedAchievements.value
    return listOf(
      AchievementItem("primera", "Primer Paso", "Completa tu primera sesión de entrenamiento", "🌱", unlocked.contains("primera")),
      AchievementItem("dominios", "Mente Integral", "Entrena en al menos 3 dominios cognitivos distintos", "🧩", unlocked.contains("dominios")),
      AchievementItem("coleccionista", "Polímata", "Juega a los 9 juegos de la plataforma", "👑", unlocked.contains("coleccionista")),
      AchievementItem("racha3", "Constancia Activa", "Entrena durante 3 días consecutivos", "🔥", unlocked.contains("racha3")),
      AchievementItem("racha7", "Hábito de Hierro", "Alcanza una racha de 7 días consecutivos", "⚡", unlocked.contains("racha7")),
      AchievementItem("puntaje85", "Agudeza Mental", "Logra 85 puntos o más en cualquier juego", "🎯", unlocked.contains("puntaje85")),
      AchievementItem("perfeccion", "Perfección Serena", "Obtén un puntaje perfecto de 100 puntos", "✨", unlocked.contains("perfeccion")),
      AchievementItem("nivel3", "Mente Avanzada", "Sube al nivel 3 en cualquier juego", "🚀", unlocked.contains("nivel3")),
      AchievementItem("nivel5", "Gran Maestro", "Alcanza el nivel 5 (Experto) en un juego", "🏆", unlocked.contains("nivel5")),
      AchievementItem("sesiones10", "Dedicación", "Completa 10 sesiones de entrenamiento", "📚", unlocked.contains("sesiones10")),
      AchievementItem("sesiones50", "Centinela Cognitivo", "Completa 50 sesiones de entrenamiento", "🛡️", unlocked.contains("sesiones50")),
      AchievementItem("meta_semanal", "Misión Cumplida", "Alcanza tu objetivo semanal de entrenamiento", "🏅", unlocked.contains("meta_semanal"))
    )
  }

  suspend fun resetData() = withContext(Dispatchers.IO) {
    gameResultDao.deleteAll()
    gameProgressDao.deleteAll()
    dailySessionDao.deleteAll()
    achievementDao.deleteAll()
    userProfileDao.deleteAll()
    initializeDatabaseDefaults()
  }
}
