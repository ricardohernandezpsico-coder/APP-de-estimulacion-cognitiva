package com.example.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase

@Database(
  entities = [
    GameResultEntity::class,
    GameProgressEntity::class,
    DailySessionEntity::class,
    AchievementEntity::class,
    UserProfileEntity::class
  ],
  version = 3,
  exportSchema = false
)
abstract class NeuroVidaDatabase : RoomDatabase() {
  abstract fun gameResultDao(): GameResultDao
  abstract fun gameProgressDao(): GameProgressDao
  abstract fun dailySessionDao(): DailySessionDao
  abstract fun achievementDao(): AchievementDao
  abstract fun userProfileDao(): UserProfileDao

  companion object {
    @Volatile
    private var INSTANCE: NeuroVidaDatabase? = null

    fun getDatabase(context: Context): NeuroVidaDatabase {
      return INSTANCE ?: synchronized(this) {
        val instance = Room.databaseBuilder(
          context.applicationContext,
          NeuroVidaDatabase::class.java,
          "neurovida_database"
        )
          .fallbackToDestructiveMigration()
          .build()
        INSTANCE = instance
        instance
      }
    }
  }
}
