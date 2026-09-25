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

        [SerializeField] private GameEntryPoint gameEntryPoint;

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (gameEntryPoint == null) gameEntryPoint = FindObjectOfType<GameEntryPoint>();
            if (gameEntryPoint == null)
            {
                Debug.LogError("[LaunchIntentConfigReader] No se encontró un GameEntryPoint en la escena.");
                return;
            }

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var launchIntent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    string json = launchIntent.Call<string>("getStringExtra", ExtraKey);
                    if (string.IsNullOrEmpty(json))
                    {
                        Debug.LogError("[LaunchIntentConfigReader] El Intent de lanzamiento no traía el extra " + ExtraKey);
                        return;
                    }
                    gameEntryPoint.InitializeGameConfig(json);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LaunchIntentConfigReader] No se pudo leer la config del Intent: " + e);
            }
#endif
        }
    }
}
