# NeuroVida — proyecto Unity (Fase 0/1 del roadmap de migración)

Ver el roadmap completo en [`NeuroVida/CLAUDE.md`](../CLAUDE.md), sección "Roadmap
acordado: migración a Unity" y "Estado Unity — Fase 0/1". Esta carpeta es el proyecto
Unity nuevo (el core de juego), separado del proyecto Android existente en
`NeuroVida/app/`.

## Estado (22-sep)

Unity 6000.0.84f1 (LTS) instalado con soporte Android. El piloto de "Secuencia Lumínica"
está **embebido en la app Android real** (`app-debug.apk` compila con `unityLibrary`
incluido, 51/51 tests de Kotlin en verde) — pero **sin probar en un dispositivo todavía**:
Unity ya no soporta x86_64 en Android (el emulador `medium_phone` que usa el proyecto es
x86_64), así que embeber requiere IL2CPP + ARM64, que solo corre en un dispositivo físico
o un emulador ARM64 (mucho más lento sin aceleración nativa). Decisión de Ricardo: dejar
todo compilando y probarlo en vivo cuando haya un Android real a mano.

- `NeuroVidaCore/`: el proyecto Unity en sí (Assets/Scripts, escena de prueba,
  Packages/manifest.json). Esto es lo que se versiona en git.
- `AndroidExport/`: proyecto Gradle exportado (615MB, **gitignored**, regenerable) —
  nunca se edita a mano. Se regenera con el menú `NeuroVida > Exportar como librería
  Android` en el Editor, o headless:
  `Unity.exe -batchmode -nographics -quit -projectPath NeuroVidaCore -executeMethod NeuroVida.Bridge.EditorTools.AndroidLibraryExport.Export`.
- `test-results/`: logs de las corridas de verificación (**gitignored**, reproducibles).

## Cómo probar en un dispositivo real, cuando haya uno

1. Conectar el dispositivo Android (USB debugging habilitado) y confirmar que `adb
   devices` lo lista.
2. Compilar e instalar: `./gradlew.bat :app:installDebug` desde `NeuroVida/` (con
   `JAVA_HOME` apuntando a Temurin 21, ver sección de toolchain arriba en CLAUDE.md).
3. Abrir la app → pestaña Ajustes → botón "[Debug] Probar Secuencia Lumínica en Unity"
   (solo visible en builds debug).
4. Esto es la PRIMERA prueba real de toda la cadena: confirma que
   `LaunchIntentConfigReader.cs` lee de verdad el extra del Intent que arma
   `UnityGameLauncher.kt` (patrón documentado pero nunca probado en un dispositivo real
   en esta sesión), que el juego se ve/siente bien, y que el resultado vuelve y se
   persiste en Room vía `NativeReceiver.kt`.
5. Antes de dar esto por cerrado del todo: resolver los 2 `TODO` explícitos en
   `NativeReceiver.kt` (`timed`/`level` van *hardcodeados* como placeholder hoy, el
   contrato de telemetría de vuelta no los lleva todavía).
6. **Medir tamaño de build** (riesgo señalado en el roadmap): comparar el APK resultante
   contra el umbral de ~40MB que menciona el documento de referencia para el núcleo de
   Unity solo (el `app-debug.apk` completo hoy pesa ~115MB, incluye debug symbols y no
   está minificado — no es la cifra comparable, hay que medir con un build release).

## Si hace falta volver a tocar el proyecto Unity desde cero

1. Abrir `NeuroVidaCore/` en Unity Hub -> Open. Primera vez tarda (genera `Library/`).
2. Tests: `Window > General > Test Runner` -> EditMode -> Run All. Deberían pasar 20/20
   (12 del motor DDA + 7 del perfil por edad + 1 de ejemplo del paquete de Unity). Si
   alguno falla, comparar contra el resultado de la misma prueba en Kotlin
   (`./gradlew.bat testDebugUnitTest` en `NeuroVida/`) — el bug está en el puerto a C#,
   no en la regla.
3. Jugar: abrir `Assets/Scenes/SecuenciaPilotoTest.unity`, apretar Play. Ajustar
   `level`/`ageBand` en el Inspector de `PlaytestBootstrap`.
4. Después de cualquier cambio de código: re-exportar (`AndroidLibraryExport`) y
   recompilar la app (`./gradlew.bat :app:assembleDebug` desde `NeuroVida/`) antes de
   asumir que el cambio llegó al lado Android.
