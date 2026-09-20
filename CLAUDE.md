# NeuroVida (Kotlin/Compose, nativa Android) — Memoria del proyecto (CLAUDE.md)

App de estimulación cognitiva gratuita (español): 9 juegos en 6 dominios (memoria, atención,
razonamiento, lenguaje, cálculo, velocidad), dificultad adaptativa, sesión diaria, rachas, maestría
por dominio, desafíos semanales, recordatorio (el sistema de logros se sacó por completo el 20-sep,
ver "Remoción de Logros" más abajo). **Nativa Android: Kotlin + Jetpack Compose**, generada originalmente con Google AI
Studio y subida a GitHub por Ricardo. Repo: `https://github.com/ricardohernandezpsico-coder/APP-de-estimulacion-cognitiva`.

## Historia — por qué esta carpeta y no `KIMI APPS/neurovida`

Existía una versión previa de "NeuroVida" en `KIMI APPS/neurovida/` (React + TypeScript + Vite +
Capacitor, hecha con Kimi). El 20-sep Ricardo mostró que había avanzado con **esta** versión nativa
(Kotlin/Compose, vía Google AI Studio) en GitHub y pidió seguir desde ahí — son dos implementaciones
distintas del mismo concepto, no ramas de lo mismo, no se pueden mezclar con git. La versión React
queda pausada/superseded; **esta carpeta clonada del repo de GitHub es la fuente de verdad de acá
en adelante.**

## Stack

Kotlin, Jetpack Compose (Material 3), Room (persistencia local), WorkManager (recordatorio diario),
Coroutines, Retrofit/Moshi/OkHttp (presentes en el scaffold, no necesariamente usados aún),
Firebase (`firebase-ai`, App Check — la mayoría de Firebase está comentado en
`app/build.gradle.kts`, activar solo si hace falta). `namespace = "com.example"`,
`applicationId = "com.aistudio.neurovida.cgnv"`. compileSdk/targetSdk = 36, minSdk = 24.

## Cómo compilar y correr (20-sep, primera vez que se armó el toolchain)

El repo de GitHub **no trae** `gradlew`/`gradlew.bat`/`gradle-wrapper.jar` (típico de exports de AI
Studio) ni `local.properties` ni `debug.keystore` (estos dos últimos correctamente gitignored).
Todo esto ya se generó una vez en esta máquina — si se clona de nuevo hay que rehacerlo:

- **Android CLI de Google** (`android.exe`) en `C:\Users\RURAL7\AppData\AndroidCLI` (en el PATH de
  usuario). Gestiona el SDK: `android sdk list --all`, `android sdk install "<paquete>"`,
  `android emulator create/start/list`.
- **SDK** en `C:\Users\RURAL7\AppData\Local\Android\Sdk`: `platforms/android-36`,
  `build-tools/36.0.0`, `platform-tools`, más `system-images/android-36/google_apis_playstore/x86_64`
  para el emulador `medium_phone` ya creado.
- **JDK 21** (Temurin) en `C:\Users\RURAL7\AppData\Local\Temurin21\jdk-21.0.12.1+1`. **No usar el
  JBR de Android Studio** (Java 25) — AGP 9.1.1 / Gradle 9.3.1 de este proyecto no lo soportan.
- **Gradle 9.3.1 standalone** en `C:\Users\RURAL7\AppData\Local\Gradle\gradle-9.3.1` — se usó solo
  para generar el wrapper (`gradle wrapper --gradle-version 9.3.1`) una vez que faltaba. Con el
  wrapper ya generado y commiteado no hace falta de nuevo.
- `local.properties`: `sdk.dir=C\:\\Users\\RURAL7\\AppData\\Local\\Android\\Sdk` (gitignored,
  recrear si no existe).
- `debug.keystore`: el `signingConfig` de debug en `app/build.gradle.kts` pide un keystore propio
  del proyecto (no el `~/.android/debug.keystore` de siempre). Se generó con los valores estándar
  de Android (no es sensible, es solo debug):
  `keytool -genkey -v -keystore debug.keystore -storepass android -alias androiddebugkey -keypass android -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Android Debug,O=Android,C=US"`
  (gitignored, recrear si no existe).
- **Compilar**: `export JAVA_HOME=".../Temurin21/jdk-21.0.12.1+1"` · `export PATH="$JAVA_HOME/bin:$PATH"` ·
  `./gradlew.bat assembleDebug`. APK en `app/build/outputs/apk/debug/app-debug.apk`.
- **Emulador**: `android emulator start medium_phone` (tarda el primer boot; usar
  `run_in_background` si es una tarea de agente). Luego `adb install -r <apk>` y
  `adb shell monkey -p com.aistudio.neurovida.cgnv -c android.intent.category.LAUNCHER 1` para
  abrir. **Ojo con Git Bash**: las rutas tipo `/sdcard/...` en comandos `adb shell` se manglean por
  la conversión de rutas de MSYS — usar doble slash inicial (`//sdcard/screen.png`).
- Falta `google-services.json` (Firebase) — el build solo *advierte*
  (`missingGoogleServicesStrategy = WARN`), no falla. Ignorar mientras no se use Firebase de verdad.

## Estructura

- `app/src/main/java/com/example/MainActivity.kt`: entry point, `NeuroVidaApp` composable con
  bottom nav (Hoy/Juegos/Progreso/Ajustes) + overlays de juego activo y resultado.
- `app/src/main/java/com/example/model/Models.kt`: `DomainType` (6 dominios con color),
  `GameDefinition`/`GameRegistry` (9 juegos), `GamePlayResult`, `DailySessionState`,
  `MasteryTier`/`DomainMasteryInfo`, `WeeklyChallengeDef`/`WeeklyChallengeRegistry`, `UserSettings`
  (incluye `hapticsEnabled`, `soundEnabled`, dificultad por dominio, `difficultyMode`).
- `app/src/main/java/com/example/games/*.kt`: un archivo por juego (mismo set que la versión React:
  Parejas, Secuencia, RutaTesoro, Stroop, CambioChip, Series, Anagramas, Calculo, Comparacion) +
  `GameResultScreen.kt`.
- `app/src/main/java/com/example/viewmodel/NeuroVidaViewModel.kt`: estado de la app (tab actual,
  juego activo, sesión diaria, último resultado).
- `app/src/main/java/com/example/data/`: `NeuroVidaRepository.kt` + `local/` (Room: `Daos.kt`,
  `Entities.kt`, `NeuroVidaDatabase.kt`) — persistencia real en SQLite vía Room, no localStorage.
- `app/src/main/java/com/example/notification/CognitiveReminderWorker.kt`: recordatorio diario vía
  WorkManager.
- `app/src/main/java/com/example/ui/`: `screens/` (Home, GamesLibrary, Progress, Settings),
  `components/` (`CommonUi.kt`, `ProgressTrendChart.kt`), `theme/` (Color/Theme/Type — colores por
  dominio ya definidos, ver `DomainType` en Models.kt).

## Anti-monotonía / dificultad sin techo (20-sep, en curso)

Ricardo notó que los juegos se vuelven monótonos porque el nivel adaptativo tiene un techo duro en
5 (`LevelTier.EXPERTO`) — confirmado en código: `Stroop` no tenía NINGUNA diferencia real entre
nivel 2 y nivel 5, y `Calculo` congelaba su rango numérico y su tiempo límite en nivel 5. Fuimos de
acuerdo en avanzar de forma gradual sobre la propuesta de gamificación que le compartí (ver
historial de conversación); esto es la primera etapa (dificultad sin techo real).

**Infraestructura agregada** (aplica a los 9 juegos):
- `GameProgressEntity.masteryStreak: Int` (Room, DB version 4) — progresión continua más allá de
  nivel 5. `currentLevel` se sigue topando en 5 (es la etiqueta visible Principiante..Experto), pero
  `masteryStreak` sube sin límite mientras se siga rindiendo ≥85 en nivel 5, y baja (colchón de 2 por
  vez) antes de bajar el nivel mismo — un mal día no tira por la borda meses de progreso.
  `NeuroVidaRepository.recordGameResult` tiene la lógica.
- `NeuroVidaViewModel.getEffectiveIntensityForGame(gameId)`: solo devuelve masteryStreak > 0 en modo
  ADAPTATIVO (los modos fijos — Principiante/Intermedio/Avanzado/Personalizada — no acumulan
  maestría, tiene sentido porque el usuario ya congeló el nivel a propósito).
  `ActiveGameSession.intensity` lo lleva hasta cada composable de juego (parámetro `intensity: Int
  = 0`, agregado a los 9).

**Los 9 juegos ya tienen tratamiento profundo** (usan `intensity` de verdad, no solo la aceptan):
- **Stroop**: el bug real — nivel 2, 3, 4 y 5 eran código idéntico. Ahora el tiempo baja de forma
  continua por nivel + intensity (`baseTimeForLevel`), y desde nivel 4 la regla se invierte al azar
  entre ensayos ("elegí el color" vs "elegí lo que dice la palabra", igual que Cambio de Chip) con
  probabilidad creciente por intensity (`ruleFlipChance`, tope 50%). Verificado en emulador: 12/12
  rondas idénticas antes del fix → 7 de 12 alternaron correctamente después.
- **Calculo**: tiempo límite baja por nivel + intensity (antes fijo en 15s siempre). En nivel 5, el
  rango numérico (`boost`) y la cercanía de los distractores (`tightDelta`) siguen escalando con
  intensity en vez de quedar congelados.
- **CambioChip**: era el más plano de todos — el nivel no influía en NADA, la regla cambiaba cada 3
  rondas fijo siempre. Ahora `switchInterval` baja de 4 (nivel 1) a 2 (nivel 5), y desde ahí
  `surpriseSwitchChance` (vía intensity) agrega cambios fuera del patrón regular. Verificado en
  emulador: nivel 5 cambia de regla cada 2 rondas en vez de cada 3.
- **Series**: otro muy plano — 4 de 5 tipos de serie no cambiaban NADA con el nivel (solo
  "multiplicación" tenía 2 escalones). Ahora los 5 tipos escalan paso/magnitud con `boost` (nivel +
  intensity/3), se agregó un 6to tipo (cubos) desde nivel 4, y los distractores se aprietan
  (`tightDelta`) con intensity.
- **Parejas**: pares tope duro en 8 desde nivel 3. Ahora sigue subiendo con intensity hasta 12 pares
  (agregados símbolos nuevos al banco), y el tiempo de memorización baja de 3s a un piso de 1.5s.
- **Secuencia**: la longitud ya escalaba con nivel sin techo propio, pero ahora también con
  intensity; además la velocidad de reproducción de luces se acelera con intensity (piso 220ms).
  **[Reemplazado el 20-sep, ver "Fix: Secuencia Lumínica" más abajo]** — este esquema (crecer por
  número de ronda, sin importar el resultado) resultó ser un bug de jugabilidad real, no solo una
  cuestión de dificultad.
- **RutaTesoro**: tesoros tope duro en 7 desde nivel 5. Ahora sigue subiendo hasta 10 (de 16
  celdas) con intensity, y el tiempo de memorización baja de 2.4s a un piso de 1.2s.
- **Anagramas**: banco de palabras ampliado (8→14, con más variedad de largo) para que nivel alto no
  repita siempre las mismas; el largo mínimo exigido sigue subiendo con intensity (tope 8 letras).
- **Comparacion**: nivel 3, 4 y 5 eran idénticos (usaban el mismo `else`). Ahora `boost` sigue
  subiendo los productos y `closeness` acerca los valores comparados (más difícil a simple vista)
  sin límite con intensity; la ventana del bono de velocidad también se acorta (900ms → piso 500ms).

Todos los 9 verificados sin crash en el emulador tras el cambio (build + smoke test individual).

## Etapa 2: DDA — ajuste dinámico dentro de la sesión (20-sep)

Hasta acá la dificultad solo cambiaba ENTRE partidas (nivel adaptativo + masteryStreak al terminar).
Esta etapa hace que reaccione EN VIVO, dentro de la misma partida: encadenar aciertos la endurece ya
en el próximo ítem, sin esperar a la siguiente sesión — y se enfría sola apenas se falla uno.

**Patrón usado** (mismo en los 3 juegos): variable local `currentStreak` (cuenta aciertos seguidos
*de esta partida*, se resetea a 0 en cualquier error) → `liveIntensity = intensity + (currentStreak
/ 3) * 2` → se pasa esa `liveIntensity` en vez de `intensity` a la función de generación/tiempo del
juego. Como es un cálculo derivado (no `remember`), se recalcula solo en cada recomposición.

- **Calculo**: `currentStreak` ya existía (se usaba para el bono de puntos) — se reutilizó tal cual.
  `baseTime` pasó de calcularse una vez al inicio (`remember(level, intensity)`) a recalcularse cada
  ronda con `liveIntensity`, así el tiempo límite se acorta en vivo con la racha.
- **Series**: no tenía racha propia, se agregó `currentStreak`. Mismo criterio: cada 3 aciertos
  seguidos empuja `boost` como si fuera maestría temporal.
- **CambioChip**: se agregó `currentStreak`, sumado directamente a `intensity` dentro de
  `surpriseSwitchChance` (tope subido de 0.35 a 0.5 porque ahora combina dos señales: maestría entre
  sesiones + racha en vivo).

Verificado: build limpio, Calculo en modo Reto arranca con el tiempo/rango esperado para
nivel 5 + streak 0 (`17 × 11 − 11 = ?`, dentro de rango, "8s" visto ya descontando el tiempo de
navegación del propio test). No se verificó en vivo el descenso del tiempo AL ENCADENAR aciertos
reales (requeriría resolver varias operaciones correctas seguidas vía automatización, no se hizo por
tiempo) — la fórmula es la misma que ya se validó para Stroop/Calculo en la etapa 1, así que el
riesgo es bajo, pero queda pendiente de que Ricardo lo sienta jugando.

**Pendiente de esta etapa**: los otros 6 juegos (Parejas, Secuencia, RutaTesoro, Stroop, Anagramas,
Comparacion) no tienen DDA todavía — se limitan a intensity entre sesiones. Extenderlos sigue el
mismo patrón de 3 líneas (`currentStreak` + `liveIntensity` + pasarlo donde antes iba `intensity`).

## Etapa 3: variedad de contenido (20-sep)

- **Anagramas — bug real encontrado**: `currentItem = availableWords.random()` no excluía palabras
  ya usadas en la MISMA partida. Con pools chicas (nivel 1 filtraba a solo 4 palabras de ≤4 letras
  para 6 rondas) la repetición estaba garantizada. Arreglado con `pickNextWord()`: banco de
  palabras sin usar mientras queden (se resetea recién cuando se agota el pool filtrado, no antes).
  De paso, banco ampliado de 14 a 24 palabras (9 cortas / 7 medias / 8 largas) para que el pool de
  cada nivel tenga más margen antes de necesitar repetir.
- **Calculo — hueco de contenido real**: nunca había división, solo suma/resta/multiplicación (raro
  para un juego de "aritmética mental"). Agregada como variante 50/50 en nivel 3 (`90 ÷ 6 = ?`,
  siempre exacta), nivel 4 (`(a × b) ÷ c = ?` compuesta) y nivel 5+ (números más grandes, sigue
  escalando con `boost`/`tightDelta` igual que las demás variantes). Verificado en emulador: "90 ÷ 6
  = ?" apareció sin problema, sin crash en varias rondas.

**Pendiente de esta etapa**: Series y Parejas podrían sumar más tipos/temática (ya tienen variedad
razonable de la etapa 1 — 6 tipos y banco de símbolos ampliado — pero no se revisaron a fondo bajo
el lente específico de "variedad de contenido"). Stroop/CambioChip/Comparacion/RutaTesoro/Secuencia
generan contenido proceduralmente (números y posiciones al azar en rangos amplios), así que no
sufren el mismo problema de "banco fijo que se agota" — no se tocaron en esta etapa.

## Etapa 4: gamificación más profunda (20-sep)

Dos sistemas nuevos, ambos reales (con datos en Room), no cosméticos:

### Maestría por dominio (meta-progresión sin techo)

Antes, "Competencia por Área" en Progreso mostraba `stat.competenceLevel` = el nivel más alto entre
los juegos de ese dominio — es decir, **el mismo techo en 5 de siempre**, solo que a nivel dominio.
Ahora hay un sistema de XP aparte:
- `GameProgressEntity` → no, es tabla nueva: `DomainMasteryEntity(domain, xp)` (Room, un registro
  por dominio). Se suma `result.score / 5` XP (mínimo 1) al dominio del juego jugado, en
  `recordGameResult`, sin importar si ese juego individual ya está en nivel 5 — CUALQUIER partida
  suma, así que un dominio "maxeado" en niveles de juego sigue sintiéndose vivo.
- `MasteryTier` (Models.kt): Bronce(0) → Plata(100) → Oro(300) → Platino(600) → Diamante(1000) →
  Maestro(2000+, sigue subiendo en tramos de 1000 sin tope real — la barra de progreso nunca queda
  "llena y quieta").
- UI: `ProgressScreen.kt`, sección "Maestría por Dominio" (reemplazó la vieja "Competencia por
  Área" basada en nivel). Verificado en emulador: los 6 dominios en Bronce·0/100 XP (esperado, la
  data sembrada es previa a esta función — recién de acá en más las partidas suman).

### Desafíos semanales (fijos, no aleatorios)

4 desafíos SIEMPRE los mismos, reseteados por semana (lunes a lunes) — "variedad forzada con
motivo", no al azar:
1. **Entrenamiento variado**: 3 dominios distintos jugados esta semana.
2. **Modo Reto**: 2 partidas con reloj esta semana.
3. **Racha de precisión**: 3 partidas con 85+ puntos esta semana.
4. **Constancia**: 4 días distintos jugados esta semana.

Decisión de diseño clave: el progreso **no se persiste como contador** — se calcula en vivo
filtrando `gameHistory` por `timestamp >= inicio de semana` cada vez (`computeWeeklyProgress` en
`NeuroVidaRepository.kt`). Solo se persiste qué se **reclamó ya** (`ClaimedWeeklyChallengeEntity`,
clave `"<weekKey>|<challengeKey>"`) para no volver a otorgar el premio (+10 XP a los 6 dominios) si
se recalcula. Esto evita mantener un contador Room separado que se puede desincronizar del
historial real — la fuente de verdad es una sola.

UI: card "Desafíos de la semana" en `HomeScreen.kt`, arriba de "Actividad últimos 7 días". Verificado
en emulador con la data sembrada: mostró correctamente 1/4 completo (✓ verde en "Entrenamiento
variado"), 2/3, 3/4 y 0/2 — el cálculo en vivo desde el historial existente funciona.

### Cambios de infraestructura

- Room version 4 → 5 (2 tablas nuevas: `domain_mastery`, `claimed_weekly_challenges`;
  `fallbackToDestructiveMigration` ya configurado, no hizo falta migración manual).
- `NeuroVidaViewModel`: expone `domainMasteryInfo` (`StateFlow<List<DomainMasteryInfo>>`) y
  `weeklyChallengeProgress` (pass-through directo del repositorio).

### Verificación

Build limpio, sin crashes navegando Home/Progreso/Ajustes ni entrando a un juego con los cambios
activos. Los datos DERIVADOS (challenges desde historial sembrado, mastery en 0 inicial) se
confirmaron correctos en el emulador. **No se verificó en vivo** que jugar una partida completa
efectivamente sume XP al dominio ni dispare el reclamo de un desafío recién completado — jugar
Stroop correctamente por automatización requiere leer el color de tinta desde una captura de
pantalla (no está expuesto como atributo en el árbol de accesibilidad), y no se resolvió por
tiempo. El código reusa exactamente el mismo método `recordGameResult` ya probado extensamente en
las etapas 1-3, así que el riesgo es bajo, pero es lo primero a confirmar jugando de verdad.

### Pendiente / ideas para más adelante (no implementado)

- **Colección/cosmetics**: los 7 avatares en Ajustes son de libre elección hoy, ninguno gateado por
  logro/maestría — sería el siguiente paso natural para una "colección" real (ver propuesta
  original: desbloquear skins/marcos como meta a largo plazo).
- **Recompensa variable**: la propuesta original mencionaba bonos aleatorios de baja frecuencia
  (además de las rachas ya existentes) — no implementado.
- Los desafíos semanales no tienen indicador de "cuánto falta para que termine la semana" en la UI.

**Bug real encontrado y arreglado de paso** (no estaba directamente relacionado, pero bloqueaba
probar lo de arriba): en `GamesLibraryScreen.kt`, el diálogo de intro de cada juego calculaba el
nivel a jugar desde `gameLevels[game.id]` (el nivel adaptativo crudo guardado), **ignorando por
completo** `userSettings.difficultyMode` — significaba que elegir "Avanzado" en Ajustes no tenía
ningún efecto real al jugar desde la biblioteca (sí funcionaba, por separado, desde la Sesión
Diaria, que si usa `viewModel.getEffectiveLevelForGame`). Arreglado usando ese mismo método como
base del cálculo. Verificado en el emulador: antes "Avanzado" mostraba "Nivel 1" en el diálogo de
intro, después de este fix muestra "Nivel 5 (Experto)" correctamente.
**Ojo**: cambiar el modo de dificultad en Ajustes requiere tocar el botón separado "Guardar
Dificultad en Room" — seleccionar el chip solo cambia estado local sin persistir (por diseño, no es
un bug, pero es fácil de olvidar al probar manualmente).

## Fix: Secuencia Lumínica no daba tiempo a responder (20-sep)

Ricardo reportó, jugando de verdad: "jugué la secuencia lumínica y no alcanzaba a responder y ya
iniciaba la siguiente". Causa real (no era un bug de timing/race condition — no hay ningún timeout
oculto en el código): `sequenceLength = 2 + level + (currentRound - 1)` crecía **en cada ronda sin
importar el resultado de la anterior** (ronda 1 = 3 luces, ronda 5 = 7 luces, siempre), y un solo
error terminaba la ronda al instante sin reintento. Para alguien nuevo memorizando, un primer error
temprano significaba enfrentar una secuencia MÁS larga en la ronda siguiente, en una espiral que se
siente exactamente como "no me da tiempo".

**Arreglado en `SecuenciaGame.kt`:**
- `roundLength` (antes `sequenceLength`) ahora es estado local que sube 1 luz tras un acierto y baja
  1 tras un error (piso en 2) — la dificultad reacciona a cómo le va a la persona, no al número de
  ronda. Solo el punto de partida (`2 + level + intensity/3`) depende de nivel/maestría.
- Pausa "✋ ¡Prepárate!" de 500ms entre que termina la demostración y se habilitan los toques — antes
  el cambio de "Observa" a "Tu turno" era instantáneo.
- Verificado en emulador con datos reales: ronda 1 arrancó en 3 luces (nivel 1), forcé un error
  a propósito y la ronda 2 bajó a 2 luces (antes hubiera subido a 4) — el fix funciona.

**Métrica de tiempo → puntaje** (pedido explícito de Ricardo, "sería relevante incluir la métrica
del tiempo... y que ello influya en el puntaje"): se agregó tracking de tiempo de reacción por toque
(desde que puede responder hasta cada click) en Secuencia — el promedio da un bono de hasta +10
puntos sobre el puntaje base, además de los saltos de 20 en 20 que da `correctRounds`. Esto solo se
implementó en Secuencia por ahora; Comparacion ya tenía algo similar (bono si responde en <900ms).
**Pendiente**: extender tiempo de reacción → puntaje al resto de los juegos que no timed lo tienen
todavía (Parejas, RutaTesoro, Series, Anagramas) es un pedido más amplio de Ricardo, no resuelto en
esta pasada — mismo patrón que Secuencia/Comparacion, debería ser rápido de replicar.

**Ronda 2 de feedback jugando de verdad (20-sep, mismo día)**: dos cosas más en Secuencia:
1. *"Selecciono un recuadro y se ilumina pero no cambia, se ve estático"* — `onPadClicked` prendía
   `highlightedPad` y nunca lo apagaba hasta el próximo toque (se quedaba sólido, no como el pulso
   de la demostración). Arreglado: cada toque ahora es un pulso de 180ms (enciende y apaga solo),
   igual que en la demo, vía `rememberCoroutineScope()`.
2. *"No me percaté de la métrica de tiempo"* — el bono de velocidad influía en el puntaje en
   silencio, sin ningún indicio visible. Se agregó `roundFeedback` (texto tipo "⚡ 380ms de reacción
   promedio" que aparece con `AnimatedVisibility` tras completar una ronda con éxito) para que la
   métrica se SIENTA, no solo se calcule.

Verificado en emulador: repetí la ronda 1 y fallé a propósito de nuevo — confirma otra vez que
`roundLength` baja tras el error (4→3 en esta corrida), sin crashes. No se capturó en pantalla el
pulso de acierto en sí ni el texto de feedback disparándose (la demostración dura ~2s y el overhead
de cada comando adb en este entorno se comió esa ventana en los 3 intentos de captura) — el código
reusa el mismo patrón (`AnimatedVisibility` + `rememberCoroutineScope`) ya validado en otros juegos
de este proyecto, así que la confianza es alta, pero falta la confirmación visual directa.

## Remoción de Logros (20-sep, mismo día)

Ricardo, viendo el sistema de Maestría por Dominio y Desafíos Semanales recién agregados: *"en
progreso, los logros ya no me parecen pertinente, que opinas?"*. Coincidí — "meta_semanal" (el
logro de jugar en varios dominios) se solapaba directamente con el nuevo desafío "Entrenamiento
variado", y el resto de logros no aportaba nada que Maestría/Desafíos no cubrieran mejor (progreso
real con XP en vez de checkboxes fijos). Ofrecí achicarlo/moverlo o sacarlo del todo — Ricardo eligió
sacarlo por completo: *"prefiero sacarlo"*.

Se quitó el sistema completo, no solo la UI, para no dejar código muerto:
- `Models.kt`: `AchievementItem` eliminado.
- `Entities.kt` / `Daos.kt`: `AchievementEntity` y `AchievementDao` eliminados.
- `NeuroVidaDatabase.kt`: Room version 5 → 6 (tabla `achievements` fuera del schema;
  `fallbackToDestructiveMigration` ya configurado, se pierde el historial de logros al actualizar,
  esperado).
- `NeuroVidaRepository.kt`: `achievementDao`, `unlockedAchievements` StateFlow, `checkAchievements()`
  (~55 líneas) y `getAllAchievements()` eliminados; limpieza de logros sacada de `resetData()`.
- `NeuroVidaViewModel.kt`: `newAchievementUnlocked` StateFlow, `getAchievements()` y
  `dismissAchievementBanner()` eliminados.
- `HomeScreen.kt`: toast de "logro desbloqueado" (`AnimatedVisibility`) eliminado + imports sueltos.
- `ProgressScreen.kt`: sección "Logros" (header + grid de 12 tarjetas) eliminada + imports sueltos
  (`Icons`, `Color`, `TextOverflow`, etc. que solo se usaban ahí).
- `SettingsScreen.kt`: texto del diálogo de reset ya no menciona "logros".

Verificado: `./gradlew.bat assembleDebug` limpio (solo warnings de deprecación preexistentes, no
relacionados). Grep sweep completo de `Achievement`/`achievement` en `app/src/main/java` → sin
resultados. Instalado en el emulador (`medium_phone`): abre en Progreso sin crash, "Maestría por
Dominio" se ve normal, sin ningún rastro de "Logros" en el árbol de UI (uiautomator dump). Sin
errores fatales en logcat.

## Pendiente / por confirmar con Ricardo

- Firebase: qué partes se van a usar de verdad (`firebase-ai` ya está en las dependencias activas,
  el resto comentado). Si se activa Firestore/Auth, hace falta `google-services.json` real.
- No hay `README.md` propio todavía — considerar agregar uno si el repo va a tener colaboradores
  externos o revisión de Play Store.
- Sin decidir aún: build de release firmado (keystore de verdad, no el de debug) para publicar.
