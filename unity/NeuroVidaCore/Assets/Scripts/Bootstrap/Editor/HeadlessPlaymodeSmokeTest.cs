using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Smoke test headless: abre la escena de prueba del piloto, entra en Play, deja
    /// correr unos segundos reales (suficiente para countdown + primera presentación de
    /// secuencia) y reporta si hubo errores/excepciones en consola antes de salir. No
    /// simula toques de usuario -- solo confirma que el arranque real (parseo de JSON,
    /// seed del DDA, construcción de UI, síntesis de audio) no explota.
    ///
    /// Invocar con:
    /// Unity.exe -batchmode -nographics -projectPath &lt;path&gt;
    ///   -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.Run
    /// (sin -quit -- este script llama a EditorApplication.Exit cuando termina).
    /// </summary>
    public static class HeadlessPlaymodeSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";
        private static float RunSeconds = 6f;

        private static int _errorCount;
        private static double _enteredPlayAt;
        private static bool _isRunning;
        private static bool _previousDomainReloadDisabled;
        private static EnterPlayModeOptions _previousOptions;

        public static void Run() => RunGame(null, 6f);

        /// <summary>Mismo smoke test pero con "Tinta o Palabra" (Stroop) como juego.</summary>
        public static void RunStroop() => RunGame("stroop", 9f);

        /// <summary>Mismo smoke test pero con Anagramas como juego.</summary>
        public static void RunAnagramas() => RunGame("anagramas", 9f);

        /// <summary>Mismo smoke test pero con Cálculo Sereno como juego.</summary>
        public static void RunCalculo() => RunGame("calculo", 9f);

        /// <summary>Mismo smoke test pero con Detective de Series como juego.</summary>
        public static void RunSeries() => RunGame("series", 9f);

        /// <summary>Mismo smoke test pero con Ruta del Tesoro como juego.</summary>
        public static void RunRutaTesoro() => RunGame("rutatesoro", 9f);

        /// <summary>Mismo smoke test pero con Cambio de Chip como juego.</summary>
        public static void RunCambioChip() => RunGame("cambiochip", 9f);

        /// <summary>Mismo smoke test pero con Comparación Instantánea como juego.</summary>
        public static void RunComparacion() => RunGame("comparacion", 9f);

        /// <summary>Mismo smoke test pero con Parejas Ocultas como juego.</summary>
        public static void RunParejas() => RunGame("parejas", 9f);

        private static void RunGame(string gameId, float seconds)
        {
            RunSeconds = seconds;
            EditorPlaytestBootstrap.GameIdOverride = gameId;
            EditorSceneManager.OpenScene(ScenePath);

            // Entrar a Play dispara por defecto un domain reload -- eso borra estado
            // estático y desuscribe cualquier evento registrado antes de llamar
            // EnterPlaymode(), dejando este smoke test colgado para siempre (bug real
            // encontrado en la primera corrida: el proceso de Unity quedaba girando en
            // vacío sin nunca llamar EditorApplication.Exit). Se desactiva acá, solo
            // para esta corrida, y se restaura al final.
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += OnLog;
            EditorApplication.update += OnUpdate;
            _isRunning = true;
            EditorApplication.EnterPlaymode();
        }

        private static void RestoreDomainReloadSettings()
        {
            EditorSettings.enterPlayModeOptionsEnabled = _previousDomainReloadDisabled;
            EditorSettings.enterPlayModeOptions = _previousOptions;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                _errorCount++;
                Debug.Log($"[SmokeTest] Error capturado: {condition}\n{stackTrace}");
            }
        }

        private static void OnUpdate()
        {
            if (!_isRunning) return;
            if (!EditorApplication.isPlaying) return; // todavía terminando el domain reload de entrar a Play

            if (_enteredPlayAt == 0) _enteredPlayAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - _enteredPlayAt < RunSeconds) return;

            _isRunning = false;
            EditorApplication.update -= OnUpdate;
            Application.logMessageReceived -= OnLog;
            EditorApplication.ExitPlaymode();
            RestoreDomainReloadSettings();

            Debug.Log(_errorCount == 0
                ? "[SmokeTest] OK -- sin errores durante los primeros segundos de partida."
                : $"[SmokeTest] FALLÓ -- {_errorCount} error(es)/excepción(es) durante la partida.");

            EditorApplication.Exit(_errorCount == 0 ? 0 : 1);
        }
    }
}
