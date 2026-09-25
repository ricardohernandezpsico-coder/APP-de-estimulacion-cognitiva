using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NeuroVida.Bridge;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Arma por código la escena mínima de prueba del piloto de Fase 1 (Secuencia
    /// Lumínica): un GameObject "GameBridge" con <see cref="GameEntryPoint"/> y otro con
    /// <see cref="EditorPlaytestBootstrap"/> que lo alimenta con un JSON de ejemplo al
    /// entrar en Play. Invocable desde el menú (`NeuroVida > Crear escena de prueba del
    /// piloto`) o headless vía
    /// <c>Unity.exe -batchmode -executeMethod NeuroVida.Bridge.EditorTools.CreatePilotTestScene.Create</c>.
    /// </summary>
    public static class CreatePilotTestScene
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";

        [MenuItem("NeuroVida/Crear escena de prueba del piloto")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // AudioListener -- sin esto el smoke test headless avisaba "no audio
            // listeners in the scene" en cada frame; no rompe nada pero conviene
            // resolverlo antes de que esta escena sea la que se compila para el
            // dispositivo real.
            var cameraGo = new GameObject("Main Camera");
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            cameraGo.tag = "MainCamera";

            var bridgeGo = new GameObject("GameBridge");
            var entryPoint = bridgeGo.AddComponent<GameEntryPoint>();

            var bootstrapGo = new GameObject("PlaytestBootstrap");
            var bootstrap = bootstrapGo.AddComponent<EditorPlaytestBootstrap>();
            var bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("gameEntryPoint").objectReferenceValue = entryPoint;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            // Convive con PlaytestBootstrap sin pisarse: uno corre solo en el Editor
            // (#if UNITY_EDITOR), el otro solo en un build Android real
            // (#if UNITY_ANDROID && !UNITY_EDITOR) -- ver ambos scripts.
            var intentReaderGo = new GameObject("LaunchIntentConfigReader");
            var intentReader = intentReaderGo.AddComponent<LaunchIntentConfigReader>();
            var intentReaderSo = new SerializedObject(intentReader);
            intentReaderSo.FindProperty("gameEntryPoint").objectReferenceValue = entryPoint;
            intentReaderSo.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[CreatePilotTestScene] Escena de prueba guardada en {ScenePath}");
        }
    }
}
