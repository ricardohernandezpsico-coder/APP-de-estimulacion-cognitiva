package com.example.bridge

import android.content.Context
import com.example.NeuroVidaApplication

/**
 * El resultado de una partida llega a la app por dos caminos (el Intent de vuelta a la app y el broadcast de
 * respaldo, ver [NativeReceiver]); acá se anota qué lanzamientos ya se procesaron para guardarlo una sola vez.
 * Persistido en SharedPreferences (los últimos [MAX] ids) porque el broadcast puede llegar con la app recién
 * recreada. Solo se usa en el proceso principal.
 */
object UnityResultInbox {
  private const val PREFS = "unity_results"
  private const val KEY = "handled_launch_ids"
  private const val MAX = 30

  /** true si es la primera vez que llega este lanzamiento (y lo marca); false si ya se procesó. */
  @Synchronized
  fun claim(launchId: String?): Boolean {
    if (launchId.isNullOrEmpty()) return true // lanzamientos sin id (versión vieja): sin deduplicar
    val prefs = NeuroVidaApplication.instance.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
    val ids = prefs.getString(KEY, "").orEmpty().split(',').filter { it.isNotEmpty() }
    if (launchId in ids) return false
    prefs.edit().putString(KEY, (ids + launchId).takeLast(MAX).joinToString(",")).apply()
    return true
  }
}
