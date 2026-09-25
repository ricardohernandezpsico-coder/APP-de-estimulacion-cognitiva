package com.example.data

import com.example.model.GamePlayResult
import com.example.model.GameRegistry
import com.example.model.RankTier
import java.util.TimeZone

/** Dibujo central de la medalla de un logro (ver `ui/components/AchievementMedal.kt`). */
enum class AchievementGlyph { PLAY, FLAME, CALENDAR, COMPASS, HEXAGON, STAR, BOLT, CHECK, SHIELD }

/**
 * Logro: [check] dice si está conseguido con las [AchievementStats] actuales; [progress] da (actual, meta) para
 * mostrar "4/7" mientras está bloqueado. [number] se pinta en la medalla (7 días, 100 partidas...). [tier] solo
 * en los logros de liga (la medalla lleva el escudo de esa liga).
 */
data class AchievementDef(
  val id: String,
  val title: String,
  val description: String,
  val glyph: AchievementGlyph,
  val number: Int? = null,
  val tier: RankTier? = null,
  val check: (AchievementStats) -> Boolean,
  val progress: (AchievementStats) -> Pair<Int, Int>
)

/** Todo lo que miran los logros, calculado desde el historial y los trofeos (ver [computeAchievementStats]). */
data class AchievementStats(
  val totalGames: Int,
  val bestStreak: Int,
  val maxGamesInOneDay: Int,
  val distinctGames: Int,
  val distinctDomains: Int,
  val bestScore: Int,
  val timedGames: Int,
  val bestGameRating: Int,
  val globalRating: Int
)

private const val DAY_MS = 24L * 60 * 60 * 1000

private fun localDay(ts: Long): Long = (ts + TimeZone.getDefault().getOffset(ts)) / DAY_MS

/** Racha más larga de días seguidos con al menos una partida (días locales). */
fun longestStreak(days: Collection<Long>): Int {
  if (days.isEmpty()) return 0
  val sorted = days.toSortedSet().toList()
  var best = 1
  var run = 1
  for (i in 1 until sorted.size) {
    run = if (sorted[i] == sorted[i - 1] + 1) run + 1 else 1
    if (run > best) best = run
  }
  return best
}

fun computeAchievementStats(history: List<GamePlayResult>, ratings: Map<String, Int>): AchievementStats {
  val days = history.map { localDay(it.timestamp) }
  val defs = history.mapNotNull { GameRegistry.getById(it.gameId) }
  return AchievementStats(
    totalGames = history.size,
    bestStreak = longestStreak(days),
    maxGamesInOneDay = days.groupingBy { it }.eachCount().values.maxOrNull() ?: 0,
    distinctGames = defs.map { it.id }.toSet().size,
    distinctDomains = defs.map { it.domain }.toSet().size,
    bestScore = history.maxOfOrNull { it.score } ?: 0,
    timedGames = history.count { it.timed },
    bestGameRating = ratings.values.maxOrNull() ?: 0,
    globalRating = GameRegistry.allGames.sumOf { ratings[it.id] ?: 0 } / GameRegistry.allGames.size
  )
}

/** Catálogo de logros, en el orden en que se muestran en el Perfil. */
object Achievements {
  private fun streak(id: String, title: String, days: Int) = AchievementDef(
    id, title, "Juega $days días seguidos", AchievementGlyph.FLAME, number = days,
    check = { it.bestStreak >= days }, progress = { it.bestStreak.coerceAtMost(days) to days }
  )

  private fun gameTier(id: String, tier: RankTier) = AchievementDef(
    id, tier.tierName, "Llega a liga ${tier.tierName} en un juego", AchievementGlyph.SHIELD, tier = tier,
    check = { it.bestGameRating >= tier.minRating },
    progress = { it.bestGameRating.coerceAtMost(tier.minRating) to tier.minRating }
  )

  val all: List<AchievementDef> = listOf(
    AchievementDef(
      "primer_paso", "Primer paso", "Juega tu primera partida", AchievementGlyph.PLAY,
      check = { it.totalGames >= 1 }, progress = { it.totalGames.coerceAtMost(1) to 1 }
    ),
    streak("racha_3", "En marcha", 3),
    streak("racha_7", "Una semana", 7),
    streak("racha_14", "Dos semanas", 14),
    streak("racha_30", "Un mes entero", 30),
    streak("racha_100", "Imparable", 100),
    AchievementDef(
      "dia_completo", "Día completo", "Juega 3 partidas en un mismo día", AchievementGlyph.CALENDAR, number = 3,
      check = { it.maxGamesInOneDay >= 3 }, progress = { it.maxGamesInOneDay.coerceAtMost(3) to 3 }
    ),
    AchievementDef(
      "explorador", "Explorador", "Juega los 9 juegos", AchievementGlyph.COMPASS, number = 9,
      check = { it.distinctGames >= 9 }, progress = { it.distinctGames.coerceAtMost(9) to 9 }
    ),
    AchievementDef(
      "dominios", "Mente completa", "Juega en los 6 dominios", AchievementGlyph.HEXAGON, number = 6,
      check = { it.distinctDomains >= 6 }, progress = { it.distinctDomains.coerceAtMost(6) to 6 }
    ),
    AchievementDef(
      "brillante", "Brillante", "Consigue 90 puntos o más en una partida", AchievementGlyph.STAR, number = 90,
      check = { it.bestScore >= 90 }, progress = { it.bestScore.coerceAtMost(90) to 90 }
    ),
    AchievementDef(
      "perfecto", "Perfecto", "Consigue 100 puntos en una partida", AchievementGlyph.STAR, number = 100,
      check = { it.bestScore >= 100 }, progress = { it.bestScore.coerceAtMost(100) to 100 }
    ),
    AchievementDef(
      "reto_10", "Contrarreloj", "Termina 10 partidas en modo Reto", AchievementGlyph.BOLT, number = 10,
      check = { it.timedGames >= 10 }, progress = { it.timedGames.coerceAtMost(10) to 10 }
    ),
    AchievementDef(
      "partidas_25", "Constante", "Juega 25 partidas", AchievementGlyph.CHECK, number = 25,
      check = { it.totalGames >= 25 }, progress = { it.totalGames.coerceAtMost(25) to 25 }
    ),
    AchievementDef(
      "partidas_100", "Centenario", "Juega 100 partidas", AchievementGlyph.CHECK, number = 100,
      check = { it.totalGames >= 100 }, progress = { it.totalGames.coerceAtMost(100) to 100 }
    ),
    gameTier("liga_plata", RankTier.PLATA),
    gameTier("liga_oro", RankTier.ORO),
    gameTier("liga_platino", RankTier.PLATINO),
    AchievementDef(
      "general_oro", "Oro general", "Tu liga general llega a Oro", AchievementGlyph.SHIELD, tier = RankTier.ORO,
      check = { it.globalRating >= RankTier.ORO.minRating },
      progress = { it.globalRating.coerceAtMost(RankTier.ORO.minRating) to RankTier.ORO.minRating }
    )
  )

  fun byId(id: String): AchievementDef? = all.firstOrNull { it.id == id }

  /** Ids de los logros conseguidos con estas cifras. */
  fun unlocked(stats: AchievementStats): Set<String> = all.filter { it.check(stats) }.map { it.id }.toSet()
}

/** Logros ya conseguidos y cuándo: una línea `id|timestamp`. */
fun encodeUnlocks(unlocks: Map<String, Long>): String = unlocks.entries.joinToString("\n") { "${it.key}|${it.value}" }

fun decodeUnlocks(text: String?): Map<String, Long> =
  text.orEmpty().lines().mapNotNull { line ->
    val parts = line.split('|')
    if (parts.size != 2) return@mapNotNull null
    val ts = parts[1].toLongOrNull() ?: return@mapNotNull null
    parts[0] to ts
  }.toMap()
