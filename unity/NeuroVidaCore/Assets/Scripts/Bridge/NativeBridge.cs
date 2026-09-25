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

        /// <summary>Cierra la pantalla de Unity y devuelve al usuario a la app (Activity de Android).
        /// En el Editor solo loguea.</summary>
        public static void CloseGameScreen()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
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

        /// <summary>Se llama al terminar la partida, con el JSON de
        /// <see cref="NeuroVida.Contracts.SequenceTelemetry"/> ya serializado.</summary>
        public static void ForwardTelemetryToPlatform(string jsonTelemetry)
        {
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
