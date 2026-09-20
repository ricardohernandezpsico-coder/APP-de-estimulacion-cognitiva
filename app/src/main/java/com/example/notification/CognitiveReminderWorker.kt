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
    val userProfile = database.userProfileDao().getUserProfileSync()

    // If notifications are disabled by the user, do not post
    if (userProfile?.notificationsEnabled == false) {
      return Result.success()
    }

    // Check if the user already trained today
    val todayKey = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(Date())
    val dailySession = database.dailySessionDao().getDailySessionSync(todayKey)
    val completedCount = dailySession?.completedCount ?: 0

    // If already finished all daily exercises, send an encouraging or celebratory note if appropriate,
    // or skip to avoid nagging
    val userName = userProfile?.name?.ifBlank { "Explorador" } ?: "Explorador"

    val (title, message) = if (completedCount >= 3) {
      Pair(
        "¡Excelente trabajo, $userName! 🌟",
        "Ya completaste tu entrenamiento de hoy. Tu mente sigue ágil y en forma."
      )
    } else {
      val pending = (3 - completedCount).coerceAtLeast(1)
      val motivationalMessages = listOf(
        "Te faltan $pending ejercicios para completar tu entrenamiento del día. ¡Tu mente te lo agradecerá!",
        "5 minutos de entrenamiento diario marcan la diferencia. Mantén activa tu concentración y memoria.",
        "Tu sesión personalizada de hoy te está esperando. ¡Despierta tus neuronas con un reto divertido!",
        "Constancia y agilidad: completa tu sesión diaria para cuidar tu reserva cognitiva."
      )
      Pair(
        "🧠 Tiempo de activación mental, $userName",
        motivationalMessages.random()
      )
    }

    sendNotification(title, message)
    return Result.success()
  }

  private fun sendNotification(title: String, message: String) {
    val notificationManager = context.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager

    createNotificationChannel(notificationManager)

    val intent = Intent(context, MainActivity::class.java).apply {
      flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
    }

    val pendingIntent = PendingIntent.getActivity(
      context,
      NOTIFICATION_REQUEST_CODE,
      intent,
      PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
    )

    val notification = NotificationCompat.Builder(context, CHANNEL_ID)
      .setSmallIcon(android.R.drawable.ic_popup_reminder)
      .setContentTitle(title)
      .setContentText(message)
      .setStyle(NotificationCompat.BigTextStyle().bigText(message))
      .setPriority(NotificationCompat.PRIORITY_DEFAULT)
      .setContentIntent(pendingIntent)
      .setAutoCancel(true)
      .build()

    notificationManager.notify(NOTIFICATION_ID, notification)
  }

  private fun createNotificationChannel(notificationManager: NotificationManager) {
    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
      val channel = NotificationChannel(
        CHANNEL_ID,
        "Recordatorios de Estimulación Cognitiva",
        NotificationManager.IMPORTANCE_DEFAULT
      ).apply {
        description = "Notificaciones diarias para ejercitar la memoria, atención y agilidad mental."
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
