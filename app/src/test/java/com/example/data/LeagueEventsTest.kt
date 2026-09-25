package com.example.data

import com.example.model.RankTier
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class LeagueEventsTest {
  @Test
  fun `ida y vuelta conserva juego y liga general`() {
    val events = listOf(
      LeagueEvent(1_700_000_000_000, RankTier.PLATA, "secuencia"),
      LeagueEvent(1_700_000_500_000, RankTier.ORO, null)
    )
    assertEquals(events, decodeLeagueEvents(encodeLeagueEvents(events)))
  }

  @Test
  fun `vacio o dañado no rompe`() {
    assertTrue(decodeLeagueEvents(null).isEmpty())
    assertTrue(decodeLeagueEvents("").isEmpty())
    assertEquals(1, decodeLeagueEvents("basura\n12|PLATA|parejas\n13|LIGA_INEXISTENTE|x").size)
  }
}
