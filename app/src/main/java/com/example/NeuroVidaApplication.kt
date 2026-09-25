package com.example

import android.app.Application
import com.example.data.NeuroVidaRepository

/**
 * Fase 1 del roadmap de migración a Unity (ver CLAUDE.md): el puente que recibe
 * telemetría desde Unity ([com.example.bridge.NativeReceiver]) lo invoca Unity vía
 * `AndroidJavaClass.CallStatic`, sin pasar ningún `Context` -- necesita una forma propia
 * de llegar al repositorio. Antes de esto el proyecto no tenía una `Application` propia
 * (cada `NeuroVidaViewModel` construye su propia instancia de `NeuroVidaRepository`, que
 * comparte igual la misma base Room subyacente vía `NeuroVidaDatabase.getDatabase`).
 */
class NeuroVidaApplication : Application() {
  val repository: NeuroVidaRepository by lazy { NeuroVidaRepository(this) }

  override fun onCreate() {
    super.onCreate()
    instance = this
  }

  companion object {
    lateinit var instance: NeuroVidaApplication
      private set
  }
}
