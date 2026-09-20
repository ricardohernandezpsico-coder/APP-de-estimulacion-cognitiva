package com.example

import android.app.Application
import android.content.Context
import androidx.test.core.app.ApplicationProvider
import com.example.viewmodel.NeuroVidaViewModel
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config

@RunWith(RobolectricTestRunner::class)
@Config(sdk = [36])
class ExampleRobolectricTest {

  @Test
  fun `read string from context`() {
    val context = ApplicationProvider.getApplicationContext<Context>()
    val appName = context.getString(R.string.app_name)
    assertEquals("NeuroVida", appName)
  }

  @Test
  fun `viewmodel initializes without exception`() {
    val app = ApplicationProvider.getApplicationContext<Application>()
    val viewModel = NeuroVidaViewModel(app)
    assertNotNull(viewModel.userSettings.value)
    assertNotNull(viewModel.dailySession.value)
  }

  @Test
  fun `room database stores and retrieves game results`() = kotlinx.coroutines.test.runTest {
    val context = ApplicationProvider.getApplicationContext<Context>()
    val db = androidx.room.Room.inMemoryDatabaseBuilder(
      context,
      com.example.data.local.NeuroVidaDatabase::class.java
    ).allowMainThreadQueries().build()

    val dao = db.gameResultDao()
    val initialCount = dao.getCount()
    assertEquals(0, initialCount)

    val testResult = com.example.data.local.GameResultEntity(
      id = "test-1",
      gameId = "calculo",
      score = 95,
      correctAnswers = 10,
      totalTrials = 10,
      timed = false,
      level = 2,
      timestamp = System.currentTimeMillis()
    )
    dao.insert(testResult)

    val results = dao.getAllResultsSync()
    assertEquals(1, results.size)
    assertEquals(95, results[0].score)
    assertEquals("calculo", results[0].gameId)

    db.close()
  }

  @Test
  fun `room user profile dao updates settings`() = kotlinx.coroutines.test.runTest {
    val context = ApplicationProvider.getApplicationContext<Context>()
    val db = androidx.room.Room.inMemoryDatabaseBuilder(
      context,
      com.example.data.local.NeuroVidaDatabase::class.java
    ).allowMainThreadQueries().build()

    val profileDao = db.userProfileDao()
    profileDao.insertOrUpdate(
      com.example.data.local.UserProfileEntity(
        id = 1,
        name = "Carlos",
        weeklyGoal = 5,
        defaultTimed = true,
        soundEnabled = false,
        hapticsEnabled = true
      )
    )

    val loaded = profileDao.getUserProfileSync()
    assertNotNull(loaded)
    assertEquals("Carlos", loaded?.name)
    assertEquals(5, loaded?.weeklyGoal)
    assertEquals(true, loaded?.defaultTimed)

    db.close()
  }

  @Test
  fun `room user profile dao manages multiple profiles and difficulty settings`() = kotlinx.coroutines.test.runTest {
    val context = ApplicationProvider.getApplicationContext<Context>()
    val db = androidx.room.Room.inMemoryDatabaseBuilder(
      context,
      com.example.data.local.NeuroVidaDatabase::class.java
    ).allowMainThreadQueries().build()

    val dao = db.userProfileDao()

    // Profile 1: Ana (Adaptive)
    val id1 = dao.insertOrUpdate(
      com.example.data.local.UserProfileEntity(
        name = "Ana",
        avatar = "🧠",
        isActive = true,
        difficultyMode = "ADAPTIVE",
        difficultyMemoria = 2
      )
    )

    // Profile 2: Abuela Rosa (Principiante con asistencia cognitiva)
    val id2 = dao.insertOrUpdate(
      com.example.data.local.UserProfileEntity(
        name = "Abuela Rosa",
        avatar = "👵",
        isActive = false,
        difficultyMode = "PRINCIPIANTE",
        difficultyMemoria = 1,
        cognitiveAssistance = true,
        timeScaleFactor = 1.5f
      )
    )

    val all = dao.getAllProfilesSync()
    assertEquals(2, all.size)

    val activeInitially = dao.getActiveProfileSync()
    assertEquals("Ana", activeInitially?.name)

    // Switch active profile to Rosa
    dao.setActiveProfile(id2)
    val activeAfterSwitch = dao.getActiveProfileSync()
    assertEquals("Abuela Rosa", activeAfterSwitch?.name)
    assertEquals("PRINCIPIANTE", activeAfterSwitch?.difficultyMode)
    assertEquals(true, activeAfterSwitch?.cognitiveAssistance)
    assertEquals(1.5f, activeAfterSwitch?.timeScaleFactor ?: 1f, 0.01f)

    db.close()
  }

  @Test
  fun `room database multiple results compute positive performance trend`() = kotlinx.coroutines.test.runTest {
    val context = ApplicationProvider.getApplicationContext<Context>()
    val db = androidx.room.Room.inMemoryDatabaseBuilder(
      context,
      com.example.data.local.NeuroVidaDatabase::class.java
    ).allowMainThreadQueries().build()

    val dao = db.gameResultDao()
    val now = System.currentTimeMillis()
    val day = 24 * 60 * 60 * 1000L

    // Insert progressing scores: 60, 75, 90
    val session1 = com.example.data.local.GameResultEntity("r1", "calculo", 60, 6, 10, false, 1, now - 2 * day)
    val session2 = com.example.data.local.GameResultEntity("r2", "parejas", 75, 8, 10, false, 1, now - 1 * day)
    val session3 = com.example.data.local.GameResultEntity("r3", "stroop", 90, 9, 10, false, 2, now)

    dao.insertAll(listOf(session1, session2, session3))

    val stored = dao.getAllResultsSync()
    assertEquals(3, stored.size)

    val chronological = stored.sortedBy { it.timestamp }
    val initialScore = chronological.first().score
    val latestScore = chronological.last().score
    val improvement = ((latestScore - initialScore).toFloat() / initialScore) * 100f

    assertEquals(50f, improvement)
    assertTrue(latestScore > initialScore)

    db.close()
  }

  @Test
  fun `workmanager reminder schedules without error`() {
    val context = ApplicationProvider.getApplicationContext<Context>()
    val config = androidx.work.Configuration.Builder()
      .setMinimumLoggingLevel(android.util.Log.DEBUG)
      .setExecutor(java.util.concurrent.Executors.newSingleThreadExecutor())
      .build()
    try {
      androidx.work.WorkManager.initialize(context, config)
    } catch (_: Exception) {}

    com.example.notification.CognitiveReminderWorker.scheduleDailyReminder(context, 19, 0)
    val workManager = androidx.work.WorkManager.getInstance(context)
    val workInfos = workManager.getWorkInfosForUniqueWork(com.example.notification.CognitiveReminderWorker.WORK_NAME).get()
    assertNotNull(workInfos)
    assertTrue(workInfos.isNotEmpty())
  }
}

