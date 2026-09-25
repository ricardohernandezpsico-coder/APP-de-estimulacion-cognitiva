pluginManagement {
  repositories {
    google {
      content {
        includeGroupByRegex("com\\.android.*")
        includeGroupByRegex("com\\.google.*")
        includeGroupByRegex("androidx.*")
      }
    }
    mavenCentral()
    gradlePluginPortal()
  }
}

plugins { id("org.gradle.toolchains.foojay-resolver-convention") version "1.0.0" }

dependencyResolutionManagement {
  repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
  repositories {
    google()
    mavenCentral()
    // "Unity as a Library" (unityLibrary) resuelve sus .jar/.aar sueltos acá -- ver
    // unity/AndroidExport/settings.gradle (generado por Unity, misma declaración). Ruta
    // fija en vez de project(":unityLibrary") porque acá arriba el proyecto todavía no
    // se declaró (include() está más abajo) -- project() en este punto falla.
    flatDir { dirs("unity/AndroidExport/unityLibrary/libs") }
  }
}

rootProject.name = "NeuroVida"

include(":app")

// Fase 1 del roadmap de migración a Unity (ver NeuroVida/CLAUDE.md) -- módulo exportado
// desde unity/NeuroVidaCore vía Unity > Build Settings (Android, "Export Project").
// Reexportar sobreescribe esta carpeta entera, así que no se edita a mano.
include(":unityLibrary")
project(":unityLibrary").projectDir = file("unity/AndroidExport/unityLibrary")
