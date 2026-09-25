package com.example.data

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class PercentileTest {
  @Test
  fun `la media cae en el percentil 50`() {
    assertEquals(50, Percentile.of(Percentile.PROVISIONAL_MEAN))
  }

  @Test
  fun `una desviacion arriba ronda el percentil 84`() {
    val p = Percentile.of(Percentile.PROVISIONAL_MEAN + Percentile.PROVISIONAL_SD)
    assertTrue("p=$p", p in 83..85)
  }

  @Test
  fun `es monotono y acotado entre 1 y 99`() {
    var prev = 0
    for (i in 0..10) {
      val p = Percentile.of(i / 10f)
      assertTrue(p >= prev)
      assertTrue(p in 1..99)
      prev = p
    }
  }

  @Test
  fun `cdf normal en valores conocidos`() {
    assertEquals(0.5, Percentile.normalCdf(0.0), 1e-6)
    assertEquals(0.9772, Percentile.normalCdf(2.0), 1e-3)
    assertEquals(0.0228, Percentile.normalCdf(-2.0), 1e-3)
  }
}
