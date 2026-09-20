package com.example.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow

@Dao
interface GameResultDao {
  @Query("SELECT * FROM game_results ORDER BY timestamp DESC")
  fun getAllResults(): Flow<List<GameResultEntity>>

  @Query("SELECT * FROM game_results ORDER BY timestamp DESC")
  suspend fun getAllResultsSync(): List<GameResultEntity>

  @Query("SELECT * FROM game_results WHERE gameId = :gameId ORDER BY timestamp DESC")
  fun getResultsForGame(gameId: String): Flow<List<GameResultEntity>>

  @Query("SELECT COUNT(*) FROM game_results")
  suspend fun getCount(): Int

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insert(result: GameResultEntity)

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insertAll(results: List<GameResultEntity>)

  @Query("DELETE FROM game_results")
  suspend fun deleteAll()
}

@Dao
interface GameProgressDao {
  @Query("SELECT * FROM game_progress")
  fun getAllProgress(): Flow<List<GameProgressEntity>>

  @Query("SELECT * FROM game_progress")
  suspend fun getAllProgressSync(): List<GameProgressEntity>

  @Query("SELECT * FROM game_progress WHERE gameId = :gameId")
  fun getProgressForGame(gameId: String): Flow<GameProgressEntity?>

  @Query("SELECT * FROM game_progress WHERE gameId = :gameId")
  suspend fun getProgressForGameSync(gameId: String): GameProgressEntity?

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insertOrUpdate(progress: GameProgressEntity)

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insertAll(progressList: List<GameProgressEntity>)

  @Query("DELETE FROM game_progress")
  suspend fun deleteAll()
}

@Dao
interface DailySessionDao {
  @Query("SELECT * FROM daily_sessions WHERE dateKey = :dateKey")
  fun getDailySession(dateKey: String): Flow<DailySessionEntity?>

  @Query("SELECT * FROM daily_sessions WHERE dateKey = :dateKey")
  suspend fun getDailySessionSync(dateKey: String): DailySessionEntity?

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insertOrUpdate(session: DailySessionEntity)

  @Query("DELETE FROM daily_sessions")
  suspend fun deleteAll()
}

@Dao
interface DomainMasteryDao {
  @Query("SELECT * FROM domain_mastery")
  fun getAll(): Flow<List<DomainMasteryEntity>>

  @Query("SELECT * FROM domain_mastery")
  suspend fun getAllSync(): List<DomainMasteryEntity>

  @Query("SELECT * FROM domain_mastery WHERE domain = :domain")
  suspend fun getForDomain(domain: String): DomainMasteryEntity?

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insertOrUpdate(entity: DomainMasteryEntity)

  @Query("DELETE FROM domain_mastery")
  suspend fun deleteAll()
}

@Dao
interface ClaimedWeeklyChallengeDao {
  @Query("SELECT id FROM claimed_weekly_challenges")
  suspend fun getAllClaimedSync(): List<String>

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insert(entity: ClaimedWeeklyChallengeEntity)

  @Query("DELETE FROM claimed_weekly_challenges")
  suspend fun deleteAll()
}

@Dao
interface UserProfileDao {
  @Query("SELECT * FROM user_profile ORDER BY id ASC")
  fun getAllProfiles(): Flow<List<UserProfileEntity>>

  @Query("SELECT * FROM user_profile ORDER BY id ASC")
  suspend fun getAllProfilesSync(): List<UserProfileEntity>

  @Query("SELECT * FROM user_profile WHERE isActive = 1 LIMIT 1")
  fun getActiveProfile(): Flow<UserProfileEntity?>

  @Query("SELECT * FROM user_profile WHERE isActive = 1 LIMIT 1")
  suspend fun getActiveProfileSync(): UserProfileEntity?

  @Query("SELECT * FROM user_profile WHERE id = :id LIMIT 1")
  suspend fun getProfileById(id: Long): UserProfileEntity?

  @Query("SELECT * FROM user_profile WHERE isActive = 1 OR id = 1 LIMIT 1")
  fun getUserProfile(): Flow<UserProfileEntity?>

  @Query("SELECT * FROM user_profile WHERE isActive = 1 OR id = 1 LIMIT 1")
  suspend fun getUserProfileSync(): UserProfileEntity?

  @Insert(onConflict = OnConflictStrategy.REPLACE)
  suspend fun insertOrUpdate(profile: UserProfileEntity): Long

  @Query("UPDATE user_profile SET isActive = CASE WHEN id = :profileId THEN 1 ELSE 0 END")
  suspend fun setActiveProfile(profileId: Long)

  @Query("DELETE FROM user_profile WHERE id = :id")
  suspend fun deleteProfileById(id: Long)

  @Query("DELETE FROM user_profile")
  suspend fun deleteAll()
}
