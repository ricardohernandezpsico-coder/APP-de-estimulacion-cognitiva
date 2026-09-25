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
  private val userProfileDao = database.userProfileDao()
  private val domainMasteryDao = database.domainMasteryDao()
  private val claimedWeeklyChallengeDao = database.claimedWeeklyChallengeDao()

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

  // 3b. Reactive Mastery Streak Map from Room — progresión sin techo más allá de nivel 5
  // (ver comentario en GameProgressEntity.masteryStreak).
  val gameIntensity: StateFlow<Map<String, Int>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Int>()
      GameRegistry.allGames.forEach { g -> map[g.id] = 0 }
      list.forEach { p -> map[p.gameId] = p.masteryStreak }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to 0 }
    )

  // 3b'. Rating del DDA común por juego (0..1; -1 = sin dato), para arrancar los juegos Unity donde
  // quedó el usuario (ver docs/DDA-comun.md).
  val gameDdaRating: StateFlow<Map<String, Float>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Float>()
      GameRegistry.allGames.forEach { g -> map[g.id] = -1f }
      list.forEach { p -> map[p.gameId] = p.ddaRating }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to -1f }
    )

  // 3c. Reactive ELO Rank Map from Room — ranking competitivo por juego (ver
  // RankTier/GameRankInfo en Models.kt), independiente de nivel/masteryStreak.
  val gameRanks: StateFlow<Map<String, Int>> = gameProgressDao.getAllProgress()
    .map { list ->
      val map = mutableMapOf<String, Int>()
      GameRegistry.allGames.forEach { g -> map[g.id] = 0 }
      list.forEach { p -> map[p.gameId] = p.eloRating }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = GameRegistry.allGames.associate { it.id to 0 }
    )

  // 4b. Reactive Domain Mastery XP Map from Room — meta-progresión sin techo,
  // ver DomainMasteryInfo/MasteryTier en Models.kt.
  val domainMastery: StateFlow<Map<DomainType, Int>> = domainMasteryDao.getAll()
    .map { list ->
      val map = DomainType.values().associateWith { 0 }.toMutableMap()
      list.forEach { e -> runCatching { DomainType.valueOf(e.domain) }.getOrNull()?.let { map[it] = e.xp } }
      map
    }
    .stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = DomainType.values().associateWith { 0 }
    )

  // 4c. Desafíos semanales: progreso derivado EN VIVO del historial de esta
  // semana (no se persiste un contador aparte) + qué se reclamó ya (para no
  // volver a otorgar el premio si se recalcula).
  private val _claimedChallenges = MutableStateFlow<Set<String>>(emptySet())

  val weeklyChallengeProgress: StateFlow<List<WeeklyChallengeProgress>> =
    combine(gameHistory, _claimedChallenges) { history, claimed ->
      computeWeeklyProgress(history, claimed, getWeekKey())
    }.stateIn(
      scope = repositoryScope,
      started = SharingStarted.Eagerly,
      initialValue = WeeklyChallengeRegistry.all.map { WeeklyChallengeProgress(it, 0, false) }
    )

  private fun getWeekKey(timestamp: Long = System.currentTimeMillis()): String {
    val cal = Calendar.getInstance()
    cal.timeInMillis = timestamp
    cal.firstDayOfWeek = Calendar.MONDAY
    cal.minimalDaysInFirstWeek = 4
    return "${cal.get(Calendar.YEAR)}-W${cal.get(Calendar.WEEK_OF_YEAR)}"
  }

  private fun startOfWeekMillis(): Long {
    val cal = Calendar.getInstance()
    cal.firstDayOfWeek = Calendar.MONDAY
    cal.set(Calendar.DAY_OF_WEEK, Calendar.MONDAY)
    cal.set(Calendar.HOUR_OF_DAY, 0)
    cal.set(Calendar.MINUTE, 0)
    cal.set(Calendar.SECOND, 0)
    cal.set(Calendar.MILLISECOND, 0)
    return cal.timeInMillis
  }

  private fun computeWeeklyProgress(
    history: List<GamePlayResult>,
    claimedKeys: Set<String>,
    weekKey: String
  ): List<WeeklyChallengeProgress> {
    val startOfWeek = startOfWeekMillis()
    val thisWeek = history.filter { it.timestamp >= startOfWeek }
    return WeeklyChallengeRegistry.all.map { def ->
      val progress = when (def.key) {
        "dominios3" -> thisWeek.mapNotNull { GameRegistry.getById(it.gameId)?.domain }.toSet().size
        "reto2" -> thisWeek.count { it.timed }
        "precision3" -> thisWeek.count { it.score >= 85 }
        "dias4" -> thisWeek.map { formatDate(it.timestamp) }.toSet().size
        else -> 0
      }
      val claimed = "$weekKey|${def.key}" in claimedKeys
      WeeklyChallengeProgress(def, progress.coerceAtMost(def.target), claimed)
    }
  }

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

    // 4. Domain mastery: sin filas nuevas que crear (arranca en 0 para los 6,
    // ya cubierto por el valor inicial del StateFlow). Solo cargamos lo reclamado.
    _claimedChallenges.value = claimedWeeklyChallengeDao.getAllClaimedSync().toSet()

    // 5. Daily Session in Room
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

  suspend fun recordGameResult(result: GamePlayResult): RecordOutcome = withContext(Dispatchers.IO) {
    // 1. Add to Room game_results table
    gameResultDao.insert(result.toEntity())

    // 2. Adaptive level calculation and progress update in Room
    val currentProgress = gameProgressDao.getProgressForGameSync(result.gameId)
      ?: GameProgressEntity(gameId = result.gameId, currentLevel = 1)
    // Trofeos de los 9 juegos antes de esta partida (liga general = promedio; sin jugar = 0).
    val ratingsBefore = gameProgressDao.getAllProgressSync().associate { it.gameId to it.eloRating }

    val activeProfile = userProfileDao.getActiveProfileSync() ?: userProfileDao.getUserProfileSync()
    val isAdaptive = activeProfile?.difficultyMode == "ADAPTIVE"

    val playedLevel = result.level
    var newLevel = playedLevel
    var newMastery = currentProgress.masteryStreak
    var didLevelUp = false
    if (isAdaptive) {
      if (result.score >= 85) {
        if (playedLevel < 5) {
          newLevel = playedLevel + 1
          didLevelUp = true
        } else {
          // Ya está en el techo de nivel (Experto): seguir rindiendo bien seguirá
          // endureciendo el juego (tiempos, rangos) vía masteryStreak, que no tiene
          // límite superior. didLevelUp se marca igual para celebrar el avance en la UI.
          newMastery += 1
          didLevelUp = true
        }
      } else if (result.score <= 45 && playedLevel > 1) {
        if (playedLevel == 5 && newMastery > 0) {
          // Colchón de gracia: antes de bajar de Experto, primero se consume la
          // racha de maestría acumulada — un mal día no tira por la borda meses
          // de progreso más allá del nivel 5.
          newMastery = (newMastery - 2).coerceAtLeast(0)
        } else {
          newLevel = playedLevel - 1
        }
      }
    }

    val newRating = (currentProgress.eloRating + eloDelta(result.score, currentProgress.eloRating))
      .coerceAtLeast(0)

    val updatedProgress = currentProgress.copy(
      currentLevel = newLevel,
      masteryStreak = newMastery,
      eloRating = newRating,
      ddaRating = result.endRating?.let { blendDdaRating(currentProgress.ddaRating, it) } ?: currentProgress.ddaRating,
      highestScore = maxOf(currentProgress.highestScore, result.score),
      totalGamesPlayed = currentProgress.totalGamesPlayed + 1,
      lastPlayedTimestamp = result.timestamp
    )
    gameProgressDao.insertOrUpdate(updatedProgress)
    val ratingsAfter = ratingsBefore + (result.gameId to newRating)
    val globalOf = { m: Map<String, Int> -> GameRegistry.allGames.sumOf { m[it.id] ?: 0 } / GameRegistry.allGames.size }

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

    val allResults = gameResultDao.getAllResultsSync().map { it.toDomain() }

    // 4. Maestría por dominio (etapa 4): XP sin techo por CUALQUIER partida del
    // dominio, no solo cuando el juego individual mejora de nivel.
    val gameDef = GameRegistry.getById(result.gameId)
    if (gameDef != null) {
      val xpGained = (result.score / 5).coerceAtLeast(1)
      awardDomainXp(gameDef.domain, xpGained)
    }

    // 5. Desafíos semanales: revisa si alguno se completó recién con esta
    // partida y, si no se había reclamado ya, otorga el bono una sola vez.
    val weekKey = getWeekKey()
    val progressNow = computeWeeklyProgress(allResults, _claimedChallenges.value, weekKey)
    progressNow.filter { it.isComplete && !it.claimed }.forEach { wp ->
      val claimId = "$weekKey|${wp.def.key}"
      claimedWeeklyChallengeDao.insert(ClaimedWeeklyChallengeEntity(claimId))
      DomainType.values().forEach { d -> awardDomainXp(d, WeeklyChallengeRegistry.XP_REWARD_PER_DOMAIN) }
      _claimedChallenges.value = _claimedChallenges.value + claimId
    }

    RecordOutcome(
      didLevelUp = didLevelUp,
      gameRatingBefore = currentProgress.eloRating,
      gameRatingAfter = newRating,
      globalBefore = globalOf(ratingsBefore),
      globalAfter = globalOf(ratingsAfter)
    )
  }

  // Cuánto sube o baja el ELO de un juego tras una partida. A mayor tier, más
  // exigente el umbral para seguir ganando puntos (igual que un ladder real: cuesta
  // más mantenerse arriba que subir desde abajo). Piso en 0 (Bronce 5), sin techo.
  private fun eloDelta(score: Int, currentRating: Int): Int {
    val tier = RankTier.fromRating(currentRating)
    val gainThreshold = 55 + tier.ordinal * 5
    val lossThreshold = 40 + tier.ordinal * 5
    return when {
      score >= gainThreshold + 25 -> 25
      score >= gainThreshold -> 15
      score >= lossThreshold -> 2
      score >= lossThreshold - 20 -> -10
      else -> -20
    }
  }

  private suspend fun awardDomainXp(domain: DomainType, amount: Int) {
    val current = domainMasteryDao.getForDomain(domain.name)?.xp ?: 0
    domainMasteryDao.insertOrUpdate(DomainMasteryEntity(domain = domain.name, xp = current + amount))
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

  suspend fun resetData() = withContext(Dispatchers.IO) {
    gameResultDao.deleteAll()
    gameProgressDao.deleteAll()
    dailySessionDao.deleteAll()
    userProfileDao.deleteAll()
    domainMasteryDao.deleteAll()
    claimedWeeklyChallengeDao.deleteAll()
    _claimedChallenges.value = emptySet()
    initializeDatabaseDefaults()
  }
}
