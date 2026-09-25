using UnityEditor;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Exporta el proyecto como módulo Gradle de Android ("Unity as a Library") para
    /// incluirlo en la app Android real (<c>NeuroVida/app/</c>) -- Fase 1 del roadmap,
    /// paso final ("embeber Unity en la app existente"), ver NeuroVida/CLAUDE.md.
    ///
    /// IL2CPP + ARM64 porque Google Play exige soporte de 64 bits -- Mono no compila a
    /// ARM64 en Android. Consecuencia real: el build resultante NO corre en el emulador
    /// x86_64 (`medium_phone`) que usa hoy el proyecto Android para pruebas -- Unity dejó
    /// de soportar arquitecturas x86 en Android hace varias versiones. Probar esto en
    /// vivo requiere un dispositivo físico o un emulador ARM64 (mucho más lento sin
    /// aceleración nativa en un host x86_64) -- decisión explícita de Ricardo (22-sep):
    /// dejar el embedding compilando y probarlo cuando haya un dispositivo real a mano.
    ///
    /// El export produce dos módulos Gradle en <see cref="ExportPath"/>: `launcher/`
    /// (una app standalone, no se usa) y `unityLibrary/` (el módulo de librería que sí se
    /// referencia desde `NeuroVida/app/settings.gradle.kts`).
    /// </summary>
    public static class AndroidLibraryExport
    {
        private const string ExportPath = "../AndroidExport"; // relativo a NeuroVidaCore/ -> unity/AndroidExport

        [MenuItem("NeuroVida/Exportar como librería Android (Unity as a Library)")]
        public static void Export()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // igual que app/build.gradle.kts (minSdk = 24)
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.buildAppBundle = false;

            var scenes = new[] { "Assets/Scenes/SecuenciaPilotoTest.unity" };
            var report = BuildPipeline.BuildPlayer(scenes, ExportPath, BuildTarget.Android, BuildOptions.None);

            Debug.Log($"[AndroidLibraryExport] Resultado: {report.summary.result}, " +
                      $"errores: {report.summary.totalErrors}, tamaño: {report.summary.totalSize / 1024 / 1024}MB");

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
