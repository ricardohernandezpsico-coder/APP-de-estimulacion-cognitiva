package com.example.notification

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.work.*
import com.example.MainActivity
import com.example.R
import com.example.data.local.toDomain
import com.example.model.GameRegistry
import com.example.data.local.NeuroVidaDatabase
import java.text.SimpleDateFormat
import java.util.*
import java.util.concurrent.TimeUnit

class CognitiveReminderWorker(
  private val context: Context,
  workerParams: WorkerParameters
) : CoroutineWorker(context, workerParams) {

  override suspend fun doWork(): Result {
    val database = NeuroVidaDatabase.getDatabase(context)
    val profile = database.userProfileDao().getActiveProfileSync() ?: database.userProfileDao().getUserProfileSync()
    if (profile?.notificationsEnabled == false) return Result.success()

    val now = System.currentTimeMillis()
    val today = localDay(now)
    val days = database.gameResultDao().getAllResultsSync().map { localDay(it.timestamp) }.toSet()
    var streak = 0
    while ((today - 1 - streak) in days) streak++

    val cal = Calendar.getInstance()
    val mondayOffset = (cal.get(Calendar.DAY_OF_WEEK) + 5) % 7 // lunes = 0
    val weekStart = today - mondayOffset

    val todayKey = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date(now))
    val session = database.dailySessionDao().getDailySessionSync(todayKey)?.toDomain()
    val completed = session?.completedCount ?: 0
    val nextTitle = session?.gameIds?.getOrNull(completed)?.let { GameRegistry.getById(it)?.title }

    val message = buildReminder(
      ReminderInput(
        name = profile?.name.orEmpty(),
        streakUntilYesterday = streak,
        playedToday = today in days,
        completedToday = completed,
        daysThisWeek = days.count { it >= weekStart },
        weeklyGoal = profile?.weeklyGoal ?: 4,
        nextGameTitle = nextTitle,
        daysSinceLastPlay = days.maxOrNull()?.let { (today - it).toInt() },
        dayOfYear = cal.get(Calendar.DAY_OF_YEAR)
      )
    ) ?: return Result.success() // ya completó hoy: no se insiste

    sendNotification(message.title, message.text)
    return Result.success()
  }

  private fun localDay(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / (24L * 60 * 60 * 1000)

  private fun sendNotification(title: String, message: String) {
    val notificationManager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

    createNotificationChannel(notificationManager)

    // REORDER_TO_FRONT (no CLEAR_TOP): trae la app sin recrearla ni cerrar Unity, que vive detrás en la misma tarea.
    val openApp = PendingIntent.getActivity(
      context,
      NOTIFICATION_REQUEST_CODE,
      Intent(context, MainActivity::class.java).apply { flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_REORDER_TO_FRONT },
      PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
    )
    // "Jugar ahora": abre la app y arranca la sesión de hoy (ver MainActivity.EXTRA_START_SESSION).
    val playNow = PendingIntent.getActivity(
      context,
      NOTIFICATION_REQUEST_CODE + 1,
      Intent(context, MainActivity::class.java).apply {
        flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_REORDER_TO_FRONT
        putExtra(MainActivity.EXTRA_START_SESSION, true)
      },
      PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
    )

    val notification = NotificationCompat.Builder(context, CHANNEL_ID)
      .setSmallIcon(R.drawable.ic_stat_neurovida)
      .setColor(0xFFFFC93C.toInt()) // sol de la app
      .setContentTitle(title)
      .setContentText(message)
      .setStyle(NotificationCompat.BigTextStyle().bigText(message))
      .setPriority(NotificationCompat.PRIORITY_DEFAULT)
      .setContentIntent(openApp)
      .addAction(0, "Jugar ahora", playNow)
      .setAutoCancel(true)
      .build()

    notificationManager.notify(NOTIFICATION_ID, notification)
  }

  private fun createNotificationChannel(notificationManager: NotificationManager) {
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
      val channel = NotificationChannel(
        CHANNEL_ID,
        "Recordatorio diario",
        NotificationManager.IMPORTANCE_DEFAULT
      ).apply {
        description = "Un aviso al día para tu camino de juegos. No llega si ya completaste la sesión de hoy."
        enableVibration(true)
      }
      notificationManager.createNotificationChannel(channel)
    }
  }

  companion object {
    const val CHANNEL_ID = "neurovida_daily_reminder"
    const val NOTIFICATION_ID = 1001
    const val NOTIFICATION_REQUEST_CODE = 2001
    const val WORK_NAME = "neurovida_daily_cognitive_reminder"

    fun scheduleDailyReminder(context: Context, hour: Int = 19, minute: Int = 0) {
      val now = Calendar.getInstance()
      val target = Calendar.getInstance().apply {
        set(Calendar.HOUR_OF_DAY, hour)
        set(Calendar.MINUTE, minute)
        set(Calendar.SECOND, 0)
        set(Calendar.MILLISECOND, 0)
      }

      if (target.before(now)) {
        target.add(Calendar.DAY_OF_YEAR, 1)
      }

      val initialDelayMillis = target.timeInMillis - now.timeInMillis

      val workRequest = PeriodicWorkRequestBuilder<CognitiveReminderWorker>(24, TimeUnit.HOURS)
        .setInitialDelay(initialDelayMillis, TimeUnit.MILLISECONDS)
        .setConstraints(
          Constraints.Builder()
            .setRequiresBatteryNotLow(false)
            .build()
        )
        .build()

      WorkManager.getInstance(context).enqueueUniquePeriodicWork(
        WORK_NAME,
        ExistingPeriodicWorkPolicy.UPDATE,
        workRequest
      )
    }

    fun triggerImmediateTestReminder(context: Context) {
      val workRequest = OneTimeWorkRequestBuilder<CognitiveReminderWorker>()
        .build()
      WorkManager.getInstance(context).enqueue(workRequest)
    }

    fun cancelReminder(context: Context) {
      WorkManager.getInstance(context).cancelUniqueWork(WORK_NAME)
    }
  }
}
