#!/bin/bash
# Verificacion completa de NeuroVida en el PC de Ricardo (Windows + Git Bash).
# Uso (desde cualquier carpeta del repo):   bash tools/verificar-todo.sh [--instalar]
#
# Orden que hace, y que SIEMPRE hay que respetar: pruebas Unity -> arranque de los 9 juegos ->
# REEXPORTAR la libreria Android -> compilar con Gradle. Si no se reexporta, assembleDebug empaqueta la
# exportacion vieja (unity/AndroidExport/ esta fuera de git) sin avisar.
#
# Requisitos ya instalados en ese PC (ver CLAUDE.md, "Toolchain Android"):
#   Unity 6000.0.84f1 con soporte Android, JDK 21 Temurin, Android SDK, adb en el PATH del SDK.
# Solo funciona en el PC de Ricardo: una sesion en la nube no tiene Unity ni el SDK.
set -e

REPO="$(git rev-parse --show-toplevel)"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.0.84f1/Editor/Unity.exe}"
PROJ="$(cygpath -w "$REPO/unity/NeuroVidaCore")"
RESULTS="$REPO/unity/test-results"
export JAVA_HOME="${JAVA_HOME:-/c/Users/RURAL7/AppData/Local/Temurin21/jdk-21.0.12.1+1}"
export PATH="$JAVA_HOME/bin:$PATH:/c/Users/RURAL7/AppData/Local/Android/Sdk/platform-tools"
DEVICE="${DEVICE:-ZY22LPRRWS}"   # telefono de Ricardo (adb devices)
mkdir -p "$RESULTS"

echo "=== 1/5: Recrear la escena piloto ==="
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" \
  -executeMethod NeuroVida.Bridge.EditorTools.CreatePilotTestScene.Create -logFile "$RESULTS/v-1-scene.log"

echo "=== 2/5: Pruebas EditMode (esperado: 108/108) ==="
"$UNITY" -batchmode -nographics -projectPath "$PROJ" -runTests -testPlatform EditMode \
  -testResults "$RESULTS/v-2-tests.xml" -logFile "$RESULTS/v-2-tests.log" || true
grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' "$RESULTS/v-2-tests.xml" | head -1

echo "=== 3/5: Arranque de los 9 juegos (smoke) ==="
for G in Run RunStroop RunComparacion RunCambioChip RunRutaTesoro RunSeries RunCalculo RunAnagramas RunParejas; do
  "$UNITY" -batchmode -nographics -projectPath "$PROJ" \
    -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.$G -logFile "$RESULTS/v-3-$G.log" || true
  echo "$G: $(grep '\[SmokeTest\]' "$RESULTS/v-3-$G.log" | tail -1)"
done

echo "=== 4/5: Reexportar la libreria Android ==="
"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" \
  -executeMethod NeuroVida.Bridge.EditorTools.AndroidLibraryExport.Export -logFile "$RESULTS/v-4-export.log"
grep "Resultado:" "$RESULTS/v-4-export.log"

echo "=== 5/5: Gradle (compilar y probar Kotlin) ==="
cd "$REPO"
./gradlew.bat testDebugUnitTest assembleDebug --console=plain > "$RESULTS/v-5-gradle.log" 2>&1 || { tail -n 40 "$RESULTS/v-5-gradle.log"; exit 1; }
tail -n 4 "$RESULTS/v-5-gradle.log"

if [ "$1" = "--instalar" ]; then
  echo "=== Instalando en $DEVICE ==="
  adb -s "$DEVICE" install -r app/build/outputs/apk/debug/app-debug.apk
fi
echo "=== TODO OK ==="
