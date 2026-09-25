package com.example.bridge

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

/** Recibe en el proceso principal el resultado que Unity (proceso `:unity`) envía al terminar la partida. */
class UnityResultReceiver : BroadcastReceiver() {
  override fun onReceive(context: Context, intent: Intent) {
    val json = intent.getStringExtra(NativeReceiver.EXTRA_JSON) ?: return
    NativeReceiver.handleFinished(json, intent.getStringExtra(UnityGameLauncher.EXTRA_LAUNCH_ID))
  }
}
