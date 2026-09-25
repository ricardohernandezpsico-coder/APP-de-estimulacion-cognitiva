package com.example.bridge

import android.app.Activity
import android.app.Application
import android.os.Bundle
import android.util.Log
import android.view.View
import android.view.ViewGroup
import android.widget.FrameLayout
import androidx.activity.ComponentActivity
import androidx.compose.ui.platform.ComposeView
import com.example.model.GameRegistry
import com.example.ui.components.GameLoadingScreen
import com.unity3d.player.UnityPlayerGameActivity
import org.json.JSONObject
import java.lang.ref.WeakReference

/**
 * Pantalla de carga ENCIMA de la Activity de Unity mientras el motor arranca en frío (primera partida tras abrir
 * la app, o si Android cerró el proceso `:unity`). Antes, esos segundos se veían en negro o con el indicador
 * genérico de la app. Ahora se ve [GameLoadingScreen], la misma pantalla que la app muestra un instante antes
 * (`UnityGameHost`), así el paso app -> Unity no se nota.
 *
 * Se instala desde `NeuroVidaApplication` (corre en cada proceso; solo actúa sobre la Activity de Unity). La capa
 * se agrega en el primer onStart de cada Activity de Unity (después de que Unity armó su vista; en onCreate
 * Unity la reemplazaría) y se quita con un fundido cuando Unity avisa que ya pintó el juego
 * (`NativeBridge.NotifyGameShown` -> [NativeReceiver.onGameShown]). Si ese aviso no llega (Unity viejo en el
 * APK, error), se quita sola a los [TIMEOUT_MS]. Con Unity ya vivo (REORDER_TO_FRONT) no hay onCreate: no se
 * muestra.
 */
object UnityLoadingOverlay : Application.ActivityLifecycleCallbacks {
  private const val TAG = "UnityLoadingOverlay"
  private const val TIMEOUT_MS = 20_000L
  private const val FADE_MS = 280L
  private const val NIGHT = 0xFF04061C.toInt() // = NeuroStyle.NightBottom / cielo de la app

  /** Activity de Unity recién creada que todavía no tiene la capa (se agrega en su onStart). */
  private var pending: WeakReference<Activity>? = null

  /** Capa visible; se lee desde el hilo de Unity al avisar. */
  @Volatile
  private var overlay: WeakReference<View>? = null

  fun install(app: Application) = app.registerActivityLifecycleCallbacks(this)

  override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {
    if (activity is UnityPlayerGameActivity) pending = WeakReference(activity)
  }

  override fun onActivityStarted(activity: Activity) {
    if (pending?.get() !== activity) return
    pending = null
    try {
      show(activity)
    } catch (e: Exception) {
      // Nunca debe impedir que el juego arranque.
      Log.e(TAG, "No se pudo mostrar la pantalla de carga", e)
    }
  }

  private fun show(activity: Activity) {
    val json = activity.intent?.getStringExtra(UnityGameLauncher.EXTRA_CONFIG_JSON) ?: return
    val root = JSONObject(json)
    val game = GameRegistry.allGames.find { it.id == root.optString("game_id") } ?: return
    val config = root.optJSONObject("config")
    val level = config?.optInt("level", 0)?.takeIf { it > 0 }
    val timed = if (config?.has("timed") == true) config.optBoolean("timed") else null

    val layer = FrameLayout(activity).apply {
      setBackgroundColor(NIGHT)
      isClickable = true // mientras carga, los toques no llegan al juego
    }
    if (activity is ComponentActivity) {
      // ComponentActivity.addContentView deja listos los "dueños" de ciclo de vida que ComposeView necesita.
      layer.addView(
        ComposeView(activity).apply {
          setContent { GameLoadingScreen(game = game, level = level, timed = timed, withSky = true) }
        },
        FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
      )
    }
    activity.addContentView(
      layer,
      ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT)
    )
    overlay = WeakReference(layer)
    layer.postDelayed({ hide(layer) }, TIMEOUT_MS)
  }

  /** Unity ya pintó el juego (cualquier hilo). */
  fun hideFromAnyThread() {
    val layer = overlay?.get() ?: return
    layer.post { hide(layer) }
  }

  private fun hide(layer: View) {
    if (overlay?.get() !== layer) return
    overlay = null
    layer.animate().alpha(0f).setDuration(FADE_MS).withEndAction {
      (layer.parent as? ViewGroup)?.removeView(layer)
    }.start()
  }

  override fun onActivityResumed(activity: Activity) = Unit
  override fun onActivityPaused(activity: Activity) = Unit
  override fun onActivityStopped(activity: Activity) = Unit
  override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) = Unit
  override fun onActivityDestroyed(activity: Activity) {
    if (pending?.get() === activity) pending = null
  }
}
