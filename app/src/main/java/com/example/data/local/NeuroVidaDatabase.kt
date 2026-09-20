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
    UserProfileEntity::class,
    DomainMasteryEntity::class,
    ClaimedWeeklyChallengeEntity::class
  ],
  version = 9,
  exportSchema = false
)
abstract class NeuroVidaDatabase : RoomDatabase() {
  abstract fun gameResultDao(): GameResultDao
  abstract fun gameProgressDao(): GameProgressDao
  abstract fun dailySessionDao(): DailySessionDao
  abstract fun userProfileDao(): UserProfileDao
  abstract fun domainMasteryDao(): DomainMasteryDao
  abstract fun claimedWeeklyChallengeDao(): ClaimedWeeklyChallengeDao

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
