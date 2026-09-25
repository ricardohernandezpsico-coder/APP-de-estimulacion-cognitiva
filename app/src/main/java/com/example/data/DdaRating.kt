package com.example.data

/**
 * Suavizado del rating del DDA común entre sesiones (ver docs/DDA-comun.md §5).
 *
 * El rating final de una partida ya arranca desde el rating guardado, así que es una continuación y no una
 * medición independiente; se promedia 60/40 con lo anterior para que un mal día (cansancio, distracción) no
 * tire por la borda el progreso ni un buen día lo dispare. -1 = todavía sin dato (se toma el de la partida).
 */
fun blendDdaRating(previous: Float, sessionEnd: Float): Float {
  val end = sessionEnd.coerceIn(0f, 1f)
  if (previous < 0f) return end
  return (0.6f * end + 0.4f * previous.coerceIn(0f, 1f)).coerceIn(0f, 1f)
}
