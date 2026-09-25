package com.example.data

import com.example.model.RankTier

/**
 * Ascenso de liga ocurrido en [timestamp]: a [tier] en el juego [gameId], o en la liga general si es null.
 * Se guardan para mostrarlos en el camino de Hoy (escudito en el día en que pasó).
 */
data class LeagueEvent(val timestamp: Long, val tier: RankTier, val gameId: String?)

/** Una línea por evento: `timestamp|TIER|gameId` (gameId vacío = liga general). */
fun encodeLeagueEvents(events: List<LeagueEvent>): String =
  events.joinToString("\n") { "${it.timestamp}|${it.tier.name}|${it.gameId ?: ""}" }

/** Inverso de [encodeLeagueEvents]; ignora líneas dañadas o ligas que ya no existen. */
fun decodeLeagueEvents(text: String?): List<LeagueEvent> =
  text.orEmpty().lines().mapNotNull { line ->
    val parts = line.split('|')
    if (parts.size != 3) return@mapNotNull null
    val ts = parts[0].toLongOrNull() ?: return@mapNotNull null
    val tier = RankTier.entries.firstOrNull { it.name == parts[1] } ?: return@mapNotNull null
    LeagueEvent(ts, tier, parts[2].ifEmpty { null })
  }
