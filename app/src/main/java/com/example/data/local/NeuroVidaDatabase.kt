package com.example.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import com.example.BuildConfig

@Database(
  entities = [
    GameResultEntity::class,
    GameProgressEntity::class,
    DailySessionEntity::class,
    UserProfileEntity::class,
    DomainMasteryEntity::class,
    ClaimedWeeklyChallengeEntity::class
  ],
  version = 10,
  exportSchema = true
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

    // Auditoría (21-sep, fase 2): antes esto era `.fallbackToDestructiveMigration()`
    // sin condición -- CUALQUIER futuro cambio de esquema (agregar una columna, una
    // tabla, etc.) habría borrado streaks/scores/rangos/historial de todos los
    // usuarios en producción sin avisar. Ahora, si se sube `version` sin agregar la
    // `Migration` real correspondiente en `MIGRATIONS`, un build de RELEASE falla con
    // un crash (`IllegalStateException`) en vez de destruir los datos en silencio --
    // un crash se detecta en QA antes de publicar; una pérdida de datos silenciosa la
    // descubre el usuario después. Solo en DEBUG se mantiene el fallback destructivo,
    // para no trabar la iteración local mientras se define un esquema nuevo.
    private val MIGRATIONS = arrayOf<androidx.room.migration.Migration>(
      // Cuando se necesite cambiar el esquema: agregar acá un Migration(N, N+1) real
      // con el SQL de la migración, subir `version` arriba, y correr el build una vez
      // para que se genere `schemas/<version>.json` (ya versionado en git desde esta
      // auditoría). Ver NeuroVida/CLAUDE.md para el detalle del proceso.
    )

    fun getDatabase(context: Context): NeuroVidaDatabase {
      return INSTANCE ?: synchronized(this) {
        val builder = Room.databaseBuilder(
          context.applicationContext,
          NeuroVidaDatabase::class.java,
          "neurovida_database"
        ).addMigrations(*MIGRATIONS)

        if (BuildConfig.DEBUG) {
          builder.fallbackToDestructiveMigration(dropAllTables = true)
        }

        val instance = builder.build()
        INSTANCE = instance
        instance
      }
    }
  }
}
