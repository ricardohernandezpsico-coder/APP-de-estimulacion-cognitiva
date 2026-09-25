using UnityEngine;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Bridge
{
    /// <summary>
    /// Simula la llamada que en el dispositivo real hace la app nativa vía
    /// <c>UnityPlayer.UnitySendMessage</c> — para poder jugar el piloto de "Secuencia
    /// Lumínica" apretando Play en el Editor, sin necesitar todavía la app Android
    /// embebiendo Unity (eso es un paso posterior de la Fase 1, ver roadmap en
    /// NeuroVida/CLAUDE.md).
    ///
    /// Colocar en un GameObject vacío de la escena de prueba junto con
    /// <see cref="GameEntryPoint"/>. Ajustar los valores del Inspector para probar
    /// distintos niveles/perfiles de edad sin tocar código.
    /// </summary>
    public class EditorPlaytestBootstrap : MonoBehaviour
    {
        [SerializeField] private GameEntryPoint gameEntryPoint;
        [SerializeField] private int level = 1;
        [SerializeField] private int baseIntensity = 0;
        [SerializeField] private bool timed = false;
        [SerializeField] private string ageBand = "ADULT"; // SENIOR / ADULT / UNDER_18

        /// <summary>Solo para el smoke test headless: fuerza otro juego sin tocar la escena.</summary>
        public static string GameIdOverride;

        private void Start()
        {
#if UNITY_EDITOR
            // Solo en el Editor -- en un build real (dispositivo), la config llega vía
            // LaunchIntentConfigReader (Intent extra desde UnityGameLauncher.kt del lado
            // nativo). Ambos componentes pueden convivir en la misma escena sin pisarse.
            if (gameEntryPoint == null) gameEntryPoint = FindObjectOfType<GameEntryPoint>();
            if (gameEntryPoint == null)
            {
                Debug.LogError("[EditorPlaytestBootstrap] No se encontró un GameEntryPoint en la escena.");
                return;
            }

            var config = new SequenceInitConfig
            {
                user_id = "editor_playtest",
                game_id = string.IsNullOrEmpty(GameIdOverride) ? SequenceGameController.GameId : GameIdOverride,
                config = new SequenceConfigDetails
                {
                    level = level,
                    base_intensity = baseIntensity,
                    timed = timed,
                    age_band = ageBand,
                    sound_enabled = true
                }
            };

            gameEntryPoint.InitializeGameConfig(JsonUtility.ToJson(config));
#endif
        }
    }
}
