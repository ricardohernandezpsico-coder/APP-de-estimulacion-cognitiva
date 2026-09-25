using UnityEngine;
using NeuroVida.Contracts;

namespace NeuroVida.Bridge
{
    /// <summary>
    /// Lee la configuración inicial desde el Intent que lanzó la Activity de Unity, en
    /// vez de esperar a que el lado nativo llame a <c>UnitySendMessage</c> sobre una
    /// instancia del player ya corriendo. Contraparte de
    /// <c>com.example.bridge.UnityGameLauncher</c> (Kotlin), que pone el JSON en el
    /// extra <c>neurovida_game_config_json</c> antes de arrancar la Activity.
    ///
    /// Se apoya en <c>UnityPlayer.currentActivity</c> -- API pública estable de Unity
    /// para Android, no en la clase concreta de la Activity generada (que cambió de
    /// nombre entre versiones de Unity: <c>UnityPlayerActivity</c> en versiones viejas,
    /// <c>AppUIGameActivity</c> en Unity 6). Esto no se pudo probar en un dispositivo
    /// real en esta sesión (sin hardware ARM64 a mano, ver NeuroVida/CLAUDE.md) -- es el
    /// patrón documentado para este caso, pero queda pendiente de confirmación cuando
    /// haya un dispositivo.
    /// </summary>
    public class LaunchIntentConfigReader : MonoBehaviour
    {
        private const string ExtraKey = "neurovida_game_config_json";
        private const string LaunchIdKey = "neurovida_launch_id"; // = UnityGameLauncher.EXTRA_LAUNCH_ID (Kotlin)

        [SerializeField] private GameEntryPoint gameEntryPoint;

        /// <summary>Id del lanzamiento que ya se jugó (o se está jugando). Estático para sobrevivir a la recarga
        /// de escena: Unity queda vivo entre partidas y cada partida nueva llega como un Intent con otro id.</summary>
        private static string s_startedLaunchId;

        /// <summary>Esta instancia de la escena ya arrancó una partida (una escena "usada" se recarga antes de la
        /// siguiente, para que el juego nuevo arranque limpio).</summary>
        private bool _startedHere;

        private void Start() => TryStart();

        // Unity vuelve al frente con el Intent de la partida nueva (FLAG_ACTIVITY_REORDER_TO_FRONT): la Activity
        // actualiza getIntent() en onNewIntent y acá se detecta por el cambio de id.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) TryStart();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) TryStart();
        }

        private void TryStart()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string json, launchId;
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var launchIntent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    json = launchIntent.Call<string>("getStringExtra", ExtraKey);
                    launchId = launchIntent.Call<string>("getStringExtra", LaunchIdKey);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LaunchIntentConfigReader] No se pudo leer la config del Intent: " + e);
                return;
            }

            if (string.IsNullOrEmpty(json))
            {
                if (!_startedHere) Debug.LogError("[LaunchIntentConfigReader] El Intent de lanzamiento no traía el extra " + ExtraKey);
                return;
            }
            // Ya jugada o en curso (volver de segundo plano a mitad de partida no la reinicia). Sin id = versión
            // vieja de la app: una sola partida por escena, como antes.
            if (string.IsNullOrEmpty(launchId) ? _startedHere : launchId == s_startedLaunchId) return;

            if (_startedHere)
            {
                // Partida nueva sobre una escena ya usada: se recarga y la instancia nueva la arranca en su Start.
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (gameEntryPoint == null) gameEntryPoint = FindObjectOfType<GameEntryPoint>();
            if (gameEntryPoint == null)
            {
                Debug.LogError("[LaunchIntentConfigReader] No se encontró un GameEntryPoint en la escena.");
                return;
            }
            s_startedLaunchId = launchId;
            _startedHere = true;
            gameEntryPoint.InitializeGameConfig(json);
#endif
        }
    }
}
