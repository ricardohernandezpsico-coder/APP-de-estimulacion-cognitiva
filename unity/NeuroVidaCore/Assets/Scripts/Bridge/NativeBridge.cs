using UnityEngine;
using System.Runtime.InteropServices;

namespace NeuroVida.Bridge
{
    /// <summary>
    /// Puente Unity -> Nativo, mismo patrón que el "C# Wrapper" del documento de
    /// arquitectura de referencia: en Android delega a una clase Kotlin estática vía
    /// <see cref="AndroidJavaClass"/>, en iOS a una función C exportada
    /// (<c>__Internal</c>), y en el Editor solo loguea (mock) para poder probar sin un
    /// dispositivo.
    ///
    /// La clase Kotlin de destino (<c>com.example.bridge.NativeReceiver.onGameFinished</c>)
    /// todavía NO existe en el proyecto Android — se agrega recién cuando se embeba Unity
    /// as a Library (Fase 1, paso "wiring"), ver plantilla en
    /// <c>unity/NeuroVidaCore/NativeBridgeTemplates/android/NativeReceiver.kt</c>.
    /// </summary>
    public static class NativeBridge
    {
        private const string AndroidReceiverClass = "com.example.bridge.NativeReceiver";

        /// <summary>La partida en curso ya terminó (se envió su resultado). Lo reinicia el lector del Intent al
        /// arrancar cada partida; lo usa el Atrás de <c>GameEntryPoint</c> (terminada = volver; en curso = pausa).</summary>
        public static bool GameFinished;

        // Bug real encontrado compilando el build Android (22-sep): un [DllImport]
        // extern SIN guardia de plataforma genera igual el símbolo nativo en IL2CPP,
        // aunque el método nunca se llame en ese branch -- el linker de Android fallaba
        // ("undefined symbol: _notificarFinJuegoIOS") porque esa función solo existe del
        // lado iOS (Xcode). La declaración misma, no solo el uso, tiene que ir adentro
        // del #if.
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _notificarFinJuegoIOS(string jsonDatos);
#endif

        /// <summary>
        /// Vuelve a la app ("Continuar" o Atrás) SIN cerrar Unity: el lado Kotlin
        /// (<c>NativeReceiver.returnToApp</c>) trae la pantalla de la app al frente con el resultado de la
        /// partida (si terminó) y Unity queda en pausa detrás. Así la próxima partida no vuelve a arrancar el
        /// motor desde cero (eran 5-8 s por juego): solo recarga la escena. La escena se recarga acá mismo para
        /// quedar en reposo (limpia, sin el panel de resultado) mientras está oculta.
        /// En el Editor solo loguea.
        /// </summary>
        public static void CloseGameScreen()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bool returned = false;
            try
            {
                using (var jc = new AndroidJavaClass(AndroidReceiverClass))
                {
                    returned = jc.CallStatic<bool>("returnToApp", false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[NativeBridge] No se pudo volver a la app sin cerrar Unity: " + e.Message);
            }

            if (returned)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                return;
            }

            // Respaldo: el camino anterior (cerrar la Activity de Unity; la próxima partida arranca en frío).
            try
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                // finish() debe correr en el hilo de UI de Android, no en el de Unity.
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() => activity.Call("finish")));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[NativeBridge] No se pudo cerrar la Activity de Unity: " + e.Message);
            }
#else
            Debug.Log("[Mock Editor] CloseGameScreen");
#endif
        }

        /// <summary>
        /// "Salir" desde el menú de pausa: vuelve a la app con la partida EN PAUSA (Unity no recarga nada). Si luego
        /// se abre el mismo juego, la app reusa el mismo id de lanzamiento y la partida sigue donde quedó.
        /// Devuelve false si no pudo (Editor u otra plataforma).
        /// </summary>
        public static bool ReturnToAppPaused()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var jc = new AndroidJavaClass(AndroidReceiverClass))
                {
                    return jc.CallStatic<bool>("returnToApp", true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[NativeBridge] No se pudo volver a la app con la partida en pausa: " + e.Message);
                return false;
            }
#else
            Debug.Log("[Mock Editor] ReturnToAppPaused");
            return false;
#endif
        }

        /// <summary>Se llama al terminar la partida, con el JSON de
        /// <see cref="NeuroVida.Contracts.SequenceTelemetry"/> ya serializado.</summary>
        public static void ForwardTelemetryToPlatform(string jsonTelemetry)
        {
            GameFinished = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var jc = new AndroidJavaClass(AndroidReceiverClass))
            {
                jc.CallStatic("onGameFinished", jsonTelemetry);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _notificarFinJuegoIOS(jsonTelemetry);
#else
            Debug.Log("[Mock Editor] Telemetry Dispatch: " + jsonTelemetry);
#endif
        }
    }
}
