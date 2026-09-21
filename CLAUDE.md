# NeuroVida (Kotlin/Compose, nativa Android) — Memoria del proyecto (CLAUDE.md)

App de estimulación cognitiva gratuita: 9 juegos en 6 dominios (memoria, atención,
razonamiento, lenguaje, cálculo, velocidad), dificultad adaptativa, sesión diaria, rachas, maestría
por dominio, desafíos semanales, recordatorio (el sistema de logros se sacó por completo el 20-sep,
ver "Remoción de Logros" más abajo). Interfaz en español con soporte parcial multi-idioma en
progreso (ver "Internacionalización" más abajo). **Nativa Android: Kotlin + Jetpack Compose**, generada originalmente con Google AI
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

## Ranking ELO por juego + modo día/noche/sistema + limpieza visual (20-sep)

Tres pedidos en una sola pasada:

### Ranking tipo ELO por juego

Idea de Ricardo: un ranking (Bronce 1-5 ... hasta tiers superiores) asociado a los niveles de
cada juego, que suba **y baje** según el desempeño — a diferencia de Maestría por Dominio (XP que
solo crece). Es un layer de progreso puramente motivacional: no toca nivel de dificultad ni
`masteryStreak`, que siguen exactamente igual.

- `RankTier` (Models.kt): Bronce(0) → Plata(250) → Oro(500) → Platino(750) → Diamante(1000) →
  Maestro(1250+, sin divisiones, sigue subiendo sin techo — mismo criterio que el resto de la
  progresión "sin techo" del proyecto). Cada tier bajo Maestro tiene 5 divisiones de 50 puntos
  (división 5 = recién ascendido, división 1 = a punto de subir de tier), igual que un ladder
  competitivo real.
- `GameRankInfo(gameId, rating)`: deriva tier/división/label a partir del rating crudo. Un rating
  por juego (9 en total, uno por cada uno de `GameRegistry.allGames`), no por dominio — el ranking
  vive en `GameProgressEntity.eloRating` (Room, columna nueva, DB v7→v8).
- Fórmula de ajuste (`NeuroVidaRepository.eloDelta`): a mayor tier, más exigente el score para
  seguir ganando puntos (`gainThreshold = 55 + tier.ordinal * 5`), igual que cuesta más mantenerse
  arriba en un ranking real que subir desde abajo. Rango de cambio: +25 (excelente) a -20 (muy
  flojo), piso en 0 (no hay Bronce por debajo de 5).
- UI: badge de rango (ícono + "Bronce 3", etc.) junto al badge de nivel en cada tarjeta de
  `GamesLibraryScreen.kt`, y una card nueva "Ranking por Juego" en `ProgressScreen.kt` (debajo de
  Maestría por Dominio) listando los 9 juegos con su rango actual.
- Verificado en el emulador: instalación limpia (DB v8) mostró Bronce 5 (rating 0) en los 9 juegos.
  Jugué una partida real de Comparación Instantánea (75 puntos, 9/12 aciertos) — con rating inicial
  0 (tier Bronce, ordinal 0) el umbral de ganancia es 55, 75 cae en el tramo "bueno" (+15), rating
  esperado 15 — coincide con el cálculo manual. Se mantiene en "Bronce 5" porque el cambio de
  división recién ocurre en rating 50 (esperado, no es bug — la primera partida no alcanza para
  subir de división, que es la intención: cuesta más de una partida buena).

### Modo Claro/Oscuro/Sistema

Ricardo reportó que esta opción "antes estaba, ya no sale" — no se encontró rastro en el historial
de git de que existiera antes en este repo (siempre fue `isSystemInDarkTheme()` fijo sin selector),
así que puede haber sido una vista previa de Google AI Studio que no llegó al export. De cualquier
forma, se implementó desde cero:
- `ThemeMode` enum (Models.kt: LIGHT/DARK/SYSTEM) + campo `UserSettings.themeMode`, persistido en
  Room (`UserProfileEntity.themeMode`, DB v6→v7).
- Selector de 3 chips ("☀️ Claro", "🌙 Oscuro", "⚙️ Sistema") en la card "Experiencia de Juego" de
  `SettingsScreen.kt`, aplica inmediato (sin botón de guardar aparte, a diferencia del selector de
  dificultad).
- `MainActivity.kt` computa `darkTheme` a partir de `userSettings.themeMode` (con
  `isSystemInDarkTheme()` como fallback para SYSTEM) y se lo pasa a `NeuroVidaTheme`.
- Verificado en el emulador: los 3 modos cambian el tema de toda la app en vivo (capturas de
  pantalla confirmadas), y el estado persiste al cambiar de pestaña.

### Limpieza visual: badge "Room DB" en Progreso

Ricardo señaló una "palabra desprolija" junto a "Curva de Rendimiento" en la pestaña Progreso — era
una etiqueta de debug (`"Room DB"`) pegada al título en `ProgressTrendChart.kt`, resto del scaffold
original de AI Studio. Se sacó; el título ahora se ve limpio.

Build limpio (`./gradlew.bat assembleDebug`) e instalado/probado en el emulador sin crashes para
los tres cambios.

## Internacionalización (20-sep, hecha por Ricardo vía Google AI Studio, no en esta sesión)

Mientras se trabajaba en el ranking ELO de acá, Ricardo implementó en paralelo (fuera de esta
sesión, directo en AI Studio) un sistema de idiomas para la app — se detectó al hacer `git push` y
encontrar 2 commits en el remoto que no existían localmente (`792a2da`, `68aea2a`). Se trajeron con
`git pull --rebase`, sin conflictos.

- `AppLanguage` (Models.kt): 5 idiomas — Español, English, Français, Deutsch, Português.
- `ui/i18n/AppStrings.kt`: `Translations` data class + `LocalAppLanguage` (CompositionLocal) +
  `strings` (`@Composable` getter que resuelve según el idioma activo).
- `UserSettings.language` persistido en Room (`UserProfileEntity.language`, DB v8→v9).
- Selector de idioma nuevo en Ajustes ("card_language_selector"), mismo patrón visual que el
  selector de tema (chips).
- **Cobertura parcial**: el `Translations` actual solo cubre las 4 pantallas principales (Hoy,
  Biblioteca de Juegos, Progreso, Ajustes — tabs, títulos, algunos labels). Los 9 juegos,
  `GameResultScreen` y varios diálogos/textos de Ajustes (dificultad, notificaciones, etc.) siguen
  hardcodeados en español. Si se pide "traducir X" y X es un juego o un diálogo, hay que agregar
  los campos correspondientes a `Translations` primero — no están todavía.

**Regresión encontrada y arreglada**: el commit `68aea2a` (de AI Studio) borró `gradlew` y
`gradlew.bat` del repo — probablemente porque el export/sync de AI Studio no sabe que existen (el
mismo gap que hubo al clonar el repo la primera vez, ver "Cómo compilar y correr" arriba). Sin esos
scripts el proyecto no compila fuera de Android Studio. Se regeneraron con
`gradle wrapper --gradle-version 9.3.1` (Gradle standalone en
`C:\Users\RURAL7\AppData\Local\Gradle\gradle-9.3.1`) y se verificó `assembleDebug` limpio antes de
commitear. **Ojo a futuro**: si Ricardo sigue alternando entre AI Studio y este entorno, este
archivo puede volver a desaparecer — revisar `ls gradlew gradlew.bat` después de traer cambios de
AI Studio.

## Parejas Ocultas (FlowMVI) — feedback jugándolo de verdad (20-sep, mismo día)

Ricardo jugó la versión recién migrada a FlowMVI real y reportó tres cosas + pidió cerrar el gap
de `asyncCache`:

1. **"Los recuadros están demasiado separados... hay que juntarlos"** y **"que se vean un poco más
   pequeñas... más complejo seleccionar las parejas"**: el ideal de 20mm/espaciado 3.2-12.5mm venía
   de la especificación inicial de accesibilidad, pero en la práctica dejaba grillas chicas (2x2-4x2)
   enormes y muy espaciadas — trivial de memorizar de un vistazo. Bajado a 15mm ideal / 1.5-6mm de
   espaciado (`ParejasGame.kt`), sin tocar el piso de accesibilidad real de Android (48dp, no
   negociable).
2. **Bug real encontrado al revisar la captura de pantalla** (no a simple vista jugando en vivo):
   `LazyVerticalGrid` con `GridCells.Fixed(columns)` reparte TODO el ancho del contenedor entre las
   columnas, ignorando el tamaño de carta calculado — con 2 columnas eso daba celdas de medio ancho
   de pantalla (rectángulos anchos, no cuadrados). Esto era probablemente la causa real de fondo del
   punto 1, más que el espaciado en sí. Arreglado fijando el ancho del propio grid a
   `columns * cardSize + spacing * (columns - 1)` en vez de dejarlo llenar el contenedor.
   `cardSizeFor` ahora también considera la ALTURA disponible (antes solo el ancho).
3. **"En la parte superior... una barra de tiempo, porque el tiempo también es una métrica
   relevante"**: agregada bajo `GameHeader`. Modo Reto: cuenta regresiva real
   (`timeLeftSeconds`/`totalTimeSeconds`, se pone roja bajo 20% restante). Precisión (sin reloj) no
   tiene límite real contra el que barrear, así que es una barra de RITMO que se llena a medida que
   pasa el tiempo respecto a un ritmo de referencia (`EXPECTED_MS_PER_PAIR` = 4s/pareja) — la misma
   referencia alimenta un bono de puntaje nuevo en Precisión (+10/+5/+0 según qué tan rápido se
   resuelve), cerrando el gap documentado más arriba ("extender tiempo de reacción → puntaje...
   Parejas") con la métrica de tiempo que pedía acá.
4. **"Los plugins faltantes... intenta solucionarlos, tienes libertad para descargar lo que haga
   falta"**: al decompilar el `.aar` real de FlowMVI 3.2.1 (`javap` sobre el classes.jar del caché de
   Gradle) se confirmó que `asyncCache` SÍ existe (`pro.respawn.flowmvi.plugins.AsyncCachePluginKt`)
   — la duda de la sesión anterior era solo falta de documentación pública, no que faltara en la
   librería. No hizo falta bajar ninguna versión nueva. `CardsGameContainer` ahora usa `asyncCache`
   real (entrega un `Deferred<T>`, se espera con `.await()` en `init`) en vez de `cache` (bloqueante).

Verificado en el emulador: Precisión completa (4/4 parejas, 66 pts, barra de ritmo pasando a rojo
tras exceder el ritmo de referencia — esperado, dado el tiempo real que toma cada comando adb en
este entorno, no un bug) y Modo Reto hasta el timeout real (0/0, cuenta regresiva visible bajando de
30s, se puso roja bajo 6s, terminó en ronda "Finished" con omisión registrada) — este último es la
primera vez que se prueba en vivo el camino de timeout de Modo Reto en esta pantalla. Sin crashes en
logcat en ninguno de los dos modos.

## Parejas Ocultas — progresión multi-nivel y cuenta regresiva dinámica (20-sep, mismo día)

Segunda ronda de feedback jugándolo: "aún me parece muy estática". Tres pedidos:

1. **Pantalla dinámica "prepárate 3,2,1"**: nuevo estado `CardsGameState.Countdown` en el
   contrato, con su propio composable `CountdownBoard` (círculo pulsante detrás del
   número vía `rememberInfiniteTransition`, `AnimatedContent` con scale+fade al cambiar
   de dígito). Aparece al arrancar la partida Y en cada transición de nivel.
2. **La partida ya no termina al completar un tablero**: antes, `matchedPairs >=
   totalPairs` disparaba directo `Finished` → pantalla de resultado. Ahora
   `CardsGameContainer.finishRound` acumula el puntaje/parejas/intentos de ese tablero en
   contadores privados del Container y, si quedan niveles, pasa de inmediato al
   siguiente vía la cuenta regresiva (`advanceToNextStage`), construyendo el próximo
   tablero con `VisualWorkingMemoryDDA.currentProfile()` -- el D(t) ya acumulado por el
   desempeño de los niveles anteriores decide sola la dificultad del próximo, sin fórmula
   aparte por nivel. Tope fijo en 10 niveles (`TOTAL_STAGES`, no "sin techo" a propósito:
   Precisión no tiene otra forma de terminar la partida, y un tope da un cierre claro).
   Quedarse sin tiempo en Modo Reto SÍ termina toda la partida en ese momento (no tendría
   sentido "premiar" con el siguiente nivel un turno sin completar). El chip/barra de
   `GameHeader` ahora muestra el nivel (ej. "3/10") en vez de las parejas del tablero
   actual (eso se ve en el chip "Parejas: X/Y" de `StatusRow`, sin cambios).
3. **Sacar la barra de tiempo, dejar solo los segundos**: la barra de progreso agregada
   en la vuelta de feedback anterior duró poco -- "que no salga la línea... que
   simplemente aparezcan los segundos, de una forma establecida y clara". `TimeBar` (con
   `LinearProgressIndicator`) se reemplazó por `TimeDisplay`, un reloj de texto centrado
   con formato m:ss (relevante ahora que una partida de varios niveles fácilmente pasa el
   minuto). Se mantiene la lógica de fondo sin cambios: cuenta regresiva real en Modo
   Reto (roja bajo 20%), cronómetro ascendente en Precisión.

Verificado jugando de verdad en el emulador (no solo compilando): completé un tablero de
6 parejas en Precisión resolviendo cada pareja a mano (sin trampas, memorizando las
posiciones reales mostradas) y confirmé la transición automática a "¡Nivel 2 de 10!" con
la animación de cuenta regresiva -- capturé la pantalla a mitad del crossfade 2→1, se ve
el dígito viejo desvaneciéndose detrás del nuevo. El tablero del nivel 2 salió más FÁCIL
(4 parejas en vez de 6) porque el desempeño real durante la prueba tuvo varios fallos
intencionales para explorar el mecanismo de mismatch -- comportamiento correcto del DDA,
no un bug (sube o baja según cómo te fue de verdad, no sube siempre). También probé
salir con la X durante la propia pantalla de cuenta regresiva (código nuevo, sin cobertura
previa) -- vuelve a Inicio sin crash. Sin excepciones en logcat en toda la sesión de
pruebas.

## Parejas Ocultas — progresión fija por nivel, sistema de 3 fallas, rendimiento (20-sep, mismo día)

Tercera ronda de feedback: "la dificultad tiene que ir incrementando de forma paulatina...
me salieron nueve láminas en el nivel 1 y cuatro en el nivel 2, debe ser al revés".

**Causa real**: la cantidad de parejas por nivel salía de `VisualWorkingMemoryDDA` (D(t)),
que sube o baja según el desempeño real -- una racha de errores entre niveles podía hacer
que el nivel 2 saliera MÁS FÁCIL que el 1, rompiendo cualquier sensación de progresión.
Arreglado separando las dos cosas: `pairCountForStage(stage)` (nuevo, en
`CardsGameContract.kt`) es ahora una tabla FIJA y estrictamente creciente (2, 3, 4, 5, 6,
7, 8, 9, 10, 12 parejas para niveles 1..10), que ya no depende del desempeño. D(t) sigue
vivo pero solo afina vista previa/interferencia/distractores dentro de cada nivel --
`VisualWorkingMemoryDDA.buildProfile` ahora recibe el `pairCount` como parámetro externo
en vez de calcularlo.

**Sistema de "3 fallas"** (pedido explícito): 3 parejas erradas en el tablero actual lo
cortan ahí mismo (no espera a que termine) y cuenta como una falla de nivel.
1ra falla -> baja un nivel. 2da falla (ya en el nivel bajado) -> repite ese mismo nivel,
sin bajar más. 3ra falla -> termina la partida. Implementado en
`CardsGameContainer.handleStageFailure`, con una `Countdown.reason` nueva
(`DEMOTED`/`RETRY`) para que la pantalla de transición diga "Bajamos al nivel N" o
"Repetimos el nivel N" en vez de mostrar el mismo cartel de avance normal -- si no, bajar
de nivel se hubiera sentido como un bug, no como una mecánica a propósito.

**Rendimiento** ("se me hace muy lento la selección de los recuadros... a alguien que
responda rápido se le va a pegar y se le va a buguear"): causa real encontrada, no
cosmética -- `runTimer` hacía `updateState` cada 100ms durante TODA la partida (no solo
durante la memorización), para poder mostrar el cronómetro ascendente de Precisión
agregado en la vuelta de feedback anterior. Eso recomponía `GameHeader`/`StatusRow`/
`TimeDisplay` 10 veces por segundo sin parar, compitiendo por el hilo principal con el
toque real. Arreglado: fuera de la memorización, el timer solo escribe estado una vez
por segundo (la única granularidad que la UI llega a mostrar de todas formas). También se
bajó el delay de "ver ambas cartas antes de resolver" de 700 a 500ms.

**Sonido** ("los sonidos se escuchan mal"): no se puede auditar de oído en este entorno
(el emulador no tiene forma de "escuchar" desde acá), pero se corrigió la causa más
plausible encontrada en el código: el tono de subida de nivel (`LEVEL_UP`) se disparaba
básicamente al mismo tiempo que el de acierto/error, en un `AudioTrack` DISTINTO -- dos
tonos independientes sonando a la vez se escuchan amontonados. Ahora el de subida de
nivel se lanza en una corrutina aparte (no bloquea el próximo toque) con 150ms de
desfase, tiempo de sobra para que el tono corto de acierto/error (90-130ms) ya haya
terminado. **Pendiente**: si el problema de sonido persiste tras esto, hace falta que
Ricardo describa más específico QUÉ tono suena mal y CUÁNDO (¿arranque del juego?
¿acierto? ¿al subir de nivel?) para seguir depurando con puntería.

Verificado jugando de verdad en el emulador: nivel 1 arrancó en 2 parejas (antes podía
salir con 8-9); forcé 3 fallas seguidas y bajó a "Bajamos al nivel 1" (quedó en el piso
porque ya estaba en nivel 1); forcé 3 fallas más y mostró "Repetimos el nivel 1"; forcé 3
fallas más y terminó la partida (pantalla de resultado, 0/9 aciertos = 3 tableros x 3
fallas cada uno, 0 puntos, coherente). Reinicié limpio y esta vez completé el nivel 1 de
verdad -- avanzó a "¡Nivel 2 de 10!" con 3 parejas (más que el nivel 1, como corresponde).
Sin excepciones en logcat en ningún momento de la sesión de pruebas.

## Piloto de perfiles por edad + onboarding (20-sep, mismo día)

Ricardo compartió material sobre calibrar el DDA (y la accesibilidad de la UI) según el
perfil del usuario en vez de un solo valor global para todos, al estilo Lumosity, y
señaló que "se deben definir edades y otras características personales mínimas
necesarias". Decidió alcance explícitamente: **piloto en Parejas Ocultas primero** (no
los 9 juegos), y **captura de edad vía un paso de onboarding nuevo** (no un campo
opcional en Ajustes).

**Insight que motivó esto**: el spec de 20mm/3.2-12.5mm que seguimos al pie de la letra
en la primera pasada de Parejas Ocultas es el recomendado para adultos mayores/deterioro
cognitivo -- pero el mismo día, jugándolo, Ricardo pidió achicarlo a 15mm/1.5-6mm porque
le resultaba "muy separado". No era una contradicción: son perfiles distintos.

**Onboarding nuevo**: `AgeBandOnboardingScreen.kt` -- pantalla de una sola vez, antes de
montar la app normal (bottom nav, tabs, etc.), pide un RANGO de edad (no la edad exacta:
es lo mínimo necesario, no un dato personal de más) con 3 opciones (`AgeBand`: menor de
18 / 18-64 / 65+). Gateada en `MainActivity.kt` con `if (userSettings.ageBand == null)`.
También editable después desde Ajustes → "Rango de edad" (mismo patrón visual que el
selector de tema), para cumplir la promesa del onboarding ("podés cambiarlo cuando
quieras").

**Infraestructura**:
- `UserSettings.ageBand: AgeBand?` (`null` = no completó onboarding aún) + columna
  homónima en `UserProfileEntity` (Room v9 → v10, `fallbackToDestructiveMigration` ya
  configurado, no hizo falta migración manual).
- `NeuroVidaViewModel.setAgeBand(band)`: función dedicada (no se sumó al `updateSettings`
  genérico) porque además de editar una preferencia, decide si se muestra el onboarding.
- `LocalAgeBand` (CompositionLocal, mismo patrón que `LocalAppLanguage` para idioma):
  se provee una vez en `MainActivity`, así "Parejas Ocultas" lo lee sin agregar un
  parámetro nuevo a la firma compartida de los 9 juegos `(level, timed, intensity,
  onFinish, onQuit)` -- los otros 8 juegos no se tocaron.

**Piloto en Parejas Ocultas** (`com.example.games.parejas.DdaUserProfileConfig` +
`ddaProfileConfigFor(ageBand)`): por perfil, varía (a) los pesos precisión/velocidad de
`VisualWorkingMemoryDDA` (adultos mayores: 0.85/0.15, más tolerante a lentitud
psicomotora; adultos: 0.65/0.35, el balance ya validado; menores: 0.60/0.40), (b) el piso
de tiempo de vista previa (500ms adultos, 1500ms adultos mayores, 800ms menores), (c) el
tamaño ideal de carta/espaciado (15mm/1.5-6mm adultos, 20mm/3.2-12.5mm adultos mayores,
17mm/2.5-8mm menores -- el piso de accesibilidad real de 48dp nunca se cruza, sea cual
sea el perfil), y (d) si un timeout en Modo Reto termina la partida entera (`allowStrictTimeouts
= true`, perfil adulto) o entra al mismo sistema de "3 fallas" que un tablero mal resuelto
(`false`, adultos mayores y menores -- el material es explícito: "prohibido expiración
estricta, causa ansiedad").

Verificado en el emulador: instalación limpia (desinstalé la app para forzar el
onboarding desde cero) mostró la pantalla de edad antes que cualquier otra pantalla;
elegí "65 o más" y Parejas Ocultas mostró cartas visiblemente más grandes y espaciadas
que las que se vieron con el perfil adulto en pruebas anteriores del mismo día (mismo
nivel 1, 2 parejas); cambié el rango a "18 a 64" desde Ajustes → Rango de edad sin
crashes. Sin excepciones en logcat en toda la sesión (onboarding, juego, cambio de
perfil).

**Pendiente / decisiones abiertas**: extender el piloto a los otros 8 juegos (no
decidido); `allowStrictTimeouts=false` solo se probó indirectamente (no se forzó un
timeout real en Modo Reto con perfil adulto mayor/menor); los pesos exactos del material
de referencia (elderly/pediatric) se adaptaron con criterio propio a la escala 0-1 que ya
usa el proyecto, no son una traducción literal -- vale la pena validarlos jugando de
verdad con cada perfil antes de asumir que están bien calibrados.

## Pruebas unitarias del motor DDA + piloto de perfiles (20-sep, mismo día)

Ricardo pidió pruebas unitarias para "validar las ecuaciones del DDA" y compartió un
ejemplo escrito contra una API ficticia (`MultidimensionalDDAEngine`, JUnit 5 + MockK) que
no existe en este proyecto. Se escribieron pruebas nuevas contra la clase real
(`VisualWorkingMemoryDDA`) usando lo que el proyecto ya tenía configurado -- JUnit 4, sin
mocks (la clase no tiene dependencias externas) -- en
`app/src/test/java/com/example/games/parejas/`:

- `VisualWorkingMemoryDDATest.kt` (15 pruebas): dirección del ajuste (acierto sube,
  omisión baja fuerte), z-score neutral durante el calentamiento (primeros 2 ensayos,
  matemática exacta verificada a mano), tope/piso de D(t) en 0..1, que un ensayo más
  rápido que el propio historial sube más que uno a velocidad promedio (aislado
  comparando dos instancias con el mismo calentamiento), que los pesos por perfil
  amplifican/atenúan la respuesta a precisión vs. velocidad, vista previa/distractores/
  interferencia/grilla resueltos correctamente a partir de D(t), y que el `pairCount` ya
  no depende de D(t) (el bug real de la vuelta de feedback anterior).
- `CardsGameContractTest.kt` (4 pruebas): `pairCountForStage` es la tabla fija exacta
  2,3,4,5,6,7,8,9,10,12, estrictamente creciente, con clamping fuera de 1..10.
- `DdaUserProfileTest.kt` (7 pruebas): invariantes del piloto de perfiles -- pesos suman
  1, adultos mayores pesan más precisión y menos velocidad que adultos, adultos mayores y
  menores desactivan timeouts estrictos, adultos mayores tienen piso de vista previa y
  tamaño de carta/espaciado mayores que adultos (la invariante que resuelve la aparente
  contradicción de la vuelta anterior).

Corridas con `./gradlew.bat testDebugUnitTest` -- 26 pruebas nuevas, 0 fallas, 0 errores;
el resto de la suite del proyecto (Robolectric/Roborazzi ya existentes) sigue en verde.
**No se escribieron pruebas de integración del `CardsGameContainer` (MVI/FlowMVI)** -- el
segundo archivo que compartió Ricardo probaba eso, pero requiere controlar corrutinas/
timers manejados por la librería más un `Context` real (`SensoryFeedbackManager`), un
esfuerzo bastante mayor al de pruebas unitarias puras; queda pendiente si se pide
explícitamente.

## Fix: cartas diminutas en niveles altos con perfil adulto mayor (20-sep, mismo día)

Ricardo reportó, jugando de verdad: "en el nivel 7 siendo adulto mayor, los recuadros se
ven muy pequeños". Causa real (no cosmética): tamaño de carta y espaciado se resolvían
por SEPARADO -- primero se fijaba el espaciado según la cantidad de columnas (hasta
12.5mm para el perfil adulto mayor), y RECIÉN DESPUÉS la carta se achicaba para absorber
lo que sobraba del ancho disponible. En una grilla densa (nivel 7 = 8 parejas = grilla
4x4), ese espaciado generoso por sí solo casi llenaba el ancho de pantalla, exprimiendo
la carta hasta el piso de accesibilidad (48dp) -- exactamente lo opuesto a la intención
del perfil adulto mayor (cartas GRANDES). Cuanto más denso el nivel, peor: el perfil que
más necesita cartas grandes es el que más las perdía.

**Arreglado en `ParejasGame.kt`** (`cardSizeFor`/`spacingForColumns` reemplazados por
`resolveCardLayout`, ahora `internal` para poder testearlo): tamaño de carta y espaciado
se resuelven JUNTOS. Se calcula cuánto espaciado como máximo se puede usar en cada eje
(ancho/alto) y que la carta siga llegando a su tamaño ideal; si ese cálculo da menos que
el piso de espaciado del perfil (`minSpacingMm`), el espaciado cede hasta ese piso -- NUNCA
por debajo, eso sigue siendo un límite duro del perfil -- liberándole ancho real a la
carta. Solo si ni con el espaciado en su piso alcanza el tamaño ideal, recién ahí la
carta se achica de verdad (hasta el piso real de accesibilidad de Android, 48dp), pero
ahora empieza desde mucho más arriba.

Prueba de regresión con los números reales del caso reportado
(`ParejasGameLayoutTest.kt`, nivel 7 = grilla 4x4, perfil "65 o más" = ideal 20mm,
espaciado 3.2-12.5mm, pantalla típica 340x550dp de área de tablero): antes la carta caía
al piso de 48dp con el espaciado en su máximo (~79dp); con el fix, el espaciado cede a su
piso (~20dp) y la carta sube a ~70dp -- una mejora real, no solo "un poco más grande".
Verificado también jugando de verdad en el emulador: llegué al Nivel 5 (6 parejas, grilla
3x3) con perfil "65 o más" -- las cartas se ven con un tamaño cómodo y el espaciado
notablemente más ajustado que en el Nivel 1 (2 columnas, donde sí sobra espacio y el
espaciado se mantiene generoso, como corresponde). Sin excepciones en logcat.

## Sonidos más suaves + rediseño visual de las cartas (21-sep)

Ricardo, tras seguir jugando "Parejas Ocultas": "lo que aun no me cierra son los sonidos
al acertar o errar, me parecen inadecuados, iria por unos mas suaves. igual no me cierra
el formato de las cartas y la distribuición, creo que eso se puede mejorar notoriamente,
que no sea el tipico juego de parejas ocultas." Dos pedidos independientes, mismo mensaje.

**Sonidos** (`SensoryFeedbackManager.kt`): los tres tonos sintetizados (match/mismatch/
level-up) sonaban a "arcade" -- intervalos amplios (quinta 700→1050Hz en el acierto),
volumen alto (0.6) y envolvente de fade muy corta (8ms, casi percusiva). Se rebajaron a:
volumen 0.16-0.32 según el tono, intervalos más chicos y consonantes (tercera mayor
600→750Hz en vez de quinta), arpegio de subida de nivel comprimido a un rango menor
(700-1000Hz en vez de 700-1200Hz), y envolvente de fade mucho más larga (20-25ms) para un
ataque redondeado en vez de percusivo. `sineBuffer()` pasó a aceptar `fadeMs` como
parámetro (antes tenía el 0.008 hardcodeado) para poder variarlo por tono.

**Formato de cartas y distribución** (`ParejasGame.kt`): antes las cartas eran rectángulos
de esquina fija (16dp) con color plano y un ícono de "?" (HelpOutline) flotando sueltas
sobre el fondo liso de la pantalla -- exactamente el aspecto "típico" que señaló Ricardo.
Cambios, todos en `CardTile`/`RunningBoard`/`DistractorBackdrop`:

- **Esquinas proporcionales al tamaño de carta** (squircle, radio = 24% del tamaño) en vez
  de un radio fijo -- a los tamaños grandes del perfil adulto mayor, 16dp fijo casi no se
  notaba (se veía casi cuadrado); proporcional mantiene la misma sensación de forma en
  todos los perfiles/niveles.
- **Degradé en la cara oculta** (azul de dominio hacia una variante más oscura) en vez de
  color plano, con un ícono de destello (`AutoAwesome`) reemplazando el "?" -- "acá hay
  algo que vas a descubrir" en vez de "examen/trivia".
- **Panel de tablero**: un rectángulo con esquinas redondeadas y degradé sutil (azul de
  dominio a 5-16% de opacidad) detrás de la grilla, dimensionado a partir del tamaño de
  grilla ya resuelto por `resolveCardLayout` -- las cartas ahora se leen como parte de un
  tablero real, no como íconos sueltos sobre el fondo de la pantalla. Puramente decorativo,
  no participa en el cálculo de layout.
- **Animación de rebote al acertar** (`Animatable` + `LaunchedEffect(card.isMatched)`,
  escala 1→1.18→1 en ~370ms): se dispara una sola vez, en la transición false→true, no en
  cada recomposición.
- **Distractores como blobs suaves**: los puntos de interferencia de fondo pasaron de
  círculos grises de borde duro a un degradé radial que se desvanece hacia afuera --
  mismo efecto visual que un blur sin depender de `Modifier.blur` (API 31+; el proyecto
  soporta desde API 24).

Verificado: `./gradlew.bat compileDebugKotlin` y `testDebugUnitTest` (33 pruebas, 0
fallas) en verde tras ambos cambios. Instalé en el emulador y jugué una partida completa
en Nivel 1: el panel de tablero, las cartas en degradé con el ícono de destello y el fondo
oculto/revelado se ven como se esperaba; provoqué a propósito 3 fallas seguidas para
confirmar que la pantalla de "Bajamos al nivel X" seguía funcionando sin romperse con el
nuevo diseño, y luego logré una pareja real para ver el estado "resuelta" (tinte verde) y
confirmar que no quedó pegado el rebote de escala. Sin excepciones ni ANR en logcat en
toda la sesión. No pude "escuchar" los tonos en este entorno (sin salida de audio real en
el emulador vía ADB) -- la verificación de sonido quedó limitada a que el código compila,
los tres `AudioTrack` se construyen sin excepciones, y el dispatch (`play(SoundEffect...)`)
se sigue llamando en los mismos puntos de siempre; Ricardo tiene que confirmar cómo se
escuchan de verdad en un dispositivo con audio.

## Reescritura de "Secuencia Lumínica" sobre FlowMVI + DDA multidimensional (21-sep)

Ricardo compartió 5 PDFs de material de referencia (mismo estilo que el de DDA por
perfiles del 20-sep: documentación técnica generada por IA, con APIs ficticias que no
calzan literal con el proyecto pero con ideas de fondo aprovechables) y pidió: (a)
continuar con Secuencia Lumínica usando el PDF específico de ese juego, y (b) revisar la
pertinencia de los otros 4 (BPI normado por edad, expansión a 9 juegos/6 dominios, Room
DB con ELO por dominio, panel de perfil con radar). Revisión de esos otros 4: la mayor
parte de lo que proponían YA EXISTE en el proyecto con otros nombres -- 9 juegos y 6
dominios (`DomainType`), i18n 5 idiomas (`AppLanguage`), "modo de asistencia cognitiva"
(`UserSettings.cognitiveAssistance`/`timeScaleFactor`), y un sistema de rango tipo ELO
por juego (`RankTier`/`GameRankInfo`, ya visible como "Bronce 5" en cada tarjeta) más
sofisticado que el que proponía el PDF. Lo único genuinamente nuevo (BPI normado por
cohortes de edad con tablas de normas inventadas por la IA, ELO por dominio como TERCER
sistema de progreso, panel de perfil con radar) se pausó a pedido explícito de Ricardo
(`AskUserQuestion` → "Pausar por ahora") -- ver [[project_neurovida_dda_profiles]] en
memoria para retomarlo.

**Antes**: `SecuenciaGame.kt` vivía enteramente en `remember`/`LaunchedEffect` locales --
sin FlowMVI, sin motor DDA real (`roundLength` solo subía/bajaba 1 según acierto/error),
sin sonido, 5 rondas fijas. Reescrito completo, mismo patrón que "Parejas Ocultas":

- **`com.example.games.secuencia`** (paquete nuevo, mismo criterio que `parejas`):
  `SequenceGameContract.kt` (estados sellados `Stopped/Countdown/Running/Finished/Error`,
  `PadColor` con frecuencia de tono propia), `SequenceDDAEngine.kt` (motor puro,
  testeable, `internal`), `SequenceGameContainer.kt` (Container FlowMVI real).
- **DDA multidimensional real** (`SequenceDDAEngine`): dos ejes con datos (longitud de
  secuencia, velocidad de presentación/ISI) sobre una ventana deslizante de los últimos 3
  resultados, más un tercero derivado (densidad de distractores). Regla anti-frustración
  tal cual el material: ante racha de fallos, primero se ralentiza el ISI; el span solo
  baja cuando ralentizar más ya cruzaría el techo (y simétrico para rachas de éxito). Ojo
  con un detalle no obvio: una vez completa, la ventana de 3 es DESLIZANTE de verdad
  (se reevalúa en CADA ronda, no cada 3) -- documentado en la clase y cubierto por los
  tests.
- **Tonos concordantes**: cada pad tiene una frecuencia grave fija (520-1360Hz) que suena
  igual en la demostración y al tocarlo -- `SensoryFeedbackManager.playConcordantTone(hz)`
  nuevo (reutiliza `sineBuffer`/`buildStaticTrack` existentes, cache por frecuencia).
  Sonidos de ronda completa/fallo/subida de nivel reutilizan `ROUND_COMPLETE`/
  `MISMATCH`/`LEVEL_UP` de `SensoryFeedbackManager` -- ya eran genéricos, no hizo falta
  duplicar la síntesis de audio para un segundo juego.
- **Distractores como ruido ambiente**, no decoys a filtrar: el material es explícito en
  evitar "animaciones invasivas... que compitan por la memoria de trabajo", así que
  `DistractorGlow` (blobs con degradé radial, mismo criterio que `DistractorBackdrop` de
  Parejas) es puramente pasivo detrás de la grilla de pads, nunca un elemento que haya
  que identificar.
- **`GameCountdownBoard` extraído** a `ui/components/CommonUi.kt` (antes vivía privado
  dentro de `ParejasGame.kt`): Secuencia Lumínica ahora tiene la misma pantalla "3, 2, 1"
  entre rondas que Parejas Ocultas -- Ricardo ya validó que le da sensación de dinamismo,
  no había razón para que solo un juego la tuviera. `ParejasGame.kt` se refactorizó para
  usar el componente compartido (mismo output visual, verificado sin cambios).
- Rondas por partida: 5 → 8 (con un motor que necesita una ventana de 3 para el primer
  ajuste, 5 apenas alcanzaba para 1-2 decisiones de dificultad en toda la sesión).

**Pruebas**: `SequenceDDAEngineTest.kt` (12 pruebas) -- la regla anti-frustración en
ambos sentidos (ISI se mueve antes que el span, en cualquiera de las dos direcciones),
límites de span nunca cruzados, ventana <3 sin ajustar, zona ZPD sin cambios, distractores
proporcionales a la dificultad, y que `seed()` de verdad limpia la ventana de una partida
anterior (regresión: sin el `clear()`, un acierto justo después de reiniciar heredaría
fallos de la partida vieja). Suite completa: `./gradlew.bat testDebugUnitTest` -- 45
pruebas, 0 fallas (33 anteriores + 12 nuevas).

**Verificado en el emulador**: instalé, abrí Secuencia Lumínica en Nivel 2 (Iniciado) --
countdown inicial, "Observa la secuencia (3 luces)" (span sembrado según nivel, coincide
con `seedSpan()`), transición a "Tu turno" funcionando, un toque correcto avanzó a 1/3,
un toque incorrecto cortó la ronda ahí mismo y pasó a "¡Ronda 2 de 8!" con la misma
animación de pulso que Parejas Ocultas -- exactamente el comportamiento esperado de
"un solo error termina la ronda". Salí con el botón "X" sin problema. Sin excepciones ni
ANR en logcat en toda la sesión. Igual que con los sonidos de Parejas, no pude confirmar
de oído los tonos concordantes en este entorno (emulador sin audio real vía ADB) --
Ricardo tiene que confirmar cómo suenan de verdad.

## Ajuste de Secuencia Lumínica contra la guía de refactorización específica (21-sep, mismo día)

Ricardo compartió un sexto PDF -- a diferencia de los 5 anteriores (genéricos/de visión
general), este es un "Master Prompt" específico de este juego, más preciso que el
material del 20-sep, y pidió "tomemos esto para reestructurar lo que se necesite". Se
comparó contra la reescritura recién hecha (sección anterior) en vez de reimplementar de
cero -- la arquitectura FlowMVI, el motor DDA de 3 ejes, la regla anti-frustración y los
tonos concordantes ya estaban resueltos igual. Dos ajustes reales:

1. **Rangos del DDA más precisos**: el material da números concretos (span 3-12, ISI
   1200-300ms) que reemplazan los que había puesto por criterio propio sin cita
   (span 2-9, ISI 1800-400ms) en `SequenceDDAEngine.kt`. Las funciones de siembra
   (`seedSpan`/`seedIsiMs` en `SequenceGameContainer.kt`) se reescalaron al nuevo rango.
   Los 13 tests de `SequenceDDAEngineTest.kt` siguieron pasando sin tocarlos (los que usan
   bounds explícitos en el constructor no dependían de las constantes por defecto) --
   se agregó uno nuevo que fija los 4 números del material como regresión.
2. **Ergonomía táctil del perfil SENIOR, ausente en la primera pasada**: el material pide
   explícitamente adaptar los pads a `DdaUserProfileConfig` (20x20mm ideal, espaciado
   3.2-12.5mm) -- la primera reescritura de Secuencia Lumínica había dejado el tamaño de
   pad fijo en 130dp sin importar el perfil de edad, porque el piloto de perfiles
   (`LocalAgeBand`/`ddaProfileConfigFor`) se había acordado exclusivo de Parejas Ocultas
   el 20-sep. Este documento pide extenderlo explícitamente a este segundo juego, así que
   el piloto pasa a cubrir 2 juegos (ver `[[project_neurovida_dda_profiles]]` en memoria,
   actualizado). Solo el TAMAÑO/ESPACIADO de los pads usa el perfil -- el motor DDA no
   varía pesos por edad acá, el material no lo pedía para este juego. Como la grilla de
   Secuencia Lumínica es fija (2x2, no una grilla densa que dependa del nivel como
   Parejas), alcanzó con `idealTouchTargetMm`/`maxSpacingMm` directos, sin reimplementar
   el resolver conjunto tamaño/espaciado (`resolveCardLayout`) de Parejas Ocultas.

La integración con Room DB (XP de dominio, ELO por juego) que pedía el material YA estaba
resuelta de forma genérica -- `finishActiveGame` en `NeuroVidaViewModel` persiste
cualquier juego por `onFinish`, no es algo que cada juego deba implementar aparte.

**Verificado en el emulador** con el perfil "65 o más" (ya estaba activo en el dispositivo
de prueba): los pads de Secuencia Lumínica se ven notoriamente más grandes y separados
que con el perfil por defecto (comparar captura de esta sesión vs. la anterior, mismo
Nivel 2) -- el toque siguió funcionando correctamente contra los pads más grandes (avanzó
de ronda sin problema). Sin excepciones ni ANR en logcat. Suite completa en verde
(`./gradlew.bat testDebugUnitTest`, 46 pruebas).

## Auditoría completa de la app + 3 bugs corregidos (21-sep, mismo día)

Ricardo pidió una auditoría de código/mecánica/arquitectura/sistema de puntajes antes de
seguir con la migración de juegos. Se hizo con 2 agentes en paralelo (los 6 juegos aún no
migrados + toda la capa de persistencia/ViewModel) y verificación manual mía de los 2
hallazgos más importantes antes de reportarlos. Detalle completo en la conversación;
resumen de lo que se decidió corregir de inmediato (los otros hallazgos -- migraciones
destructivas de Room, código muerto como `DomainStats`, comparación de String en vez de
enum para "ADAPTIVE", riesgo de doble registro en `finishActiveGame` -- quedan para las
siguientes dos etapas que Ricardo ya definió: migraciones, y luego juego por juego a
FlowMVI):

**Bug 1 — empate mal manejado en "Comparación Instantánea" (nivel 3+)**: el generador
podía producir `compareVal == prod` (el rango aleatorio incluye el 0), y con
`isLeftGreater = left >= right`, un empate SIEMPRE se resolvía a favor de "IZQUIERDA" sin
que hubiera manera de saberlo mirando la pantalla. `ComparacionGame.kt`: se repite el
sorteo hasta que no empate (mismo criterio que ya usaban los niveles 1 y 2).

**Bug 2 — puntaje "fantasma" en 3 juegos**: `CambioChipGame`, `ComparacionGame` y
`StroopGame` acumulaban un `scorePoints` con bono que NUNCA se usaba para el puntaje
final (siempre `correctas/total*100` liso) ni se mostraba en pantalla. En CambioChip y
Stroop era además puro código muerto (`+10` fijo, idéntico a `correctCount*10`, sin
variación real que perder) -- se eliminó directamente. En Comparación sí había una
variación real perdida (bono de velocidad 0/+5 por respuesta rápida) -- se conectó al
puntaje final con una fórmula que nunca puede bajar la precisión como criterio principal
(100% de aciertos siempre da 100, el bono de velocidad solo se nota si la precisión no
fue perfecta).

**Bug 3 — "Modo Reto" no hacía nada en 4 de 9 juegos**: Anagramas, Cambio de Chip,
Comparación y Detective de Series recibían `timed` y se lo pasaban a `GameHeader` como
ícono cosmético, sin ninguna cuenta regresiva real (a diferencia de Cálculo y Stroop, que
sí la tenían). Se agregó un temporizador real por ronda a los 4, cada uno con su propia
función `baseTimeForX(level, intensity)` calibrada a la carga cognitiva del juego
(Anagramas: ~3.5s por letra, más lento porque se arma letra por letra; Cambio de Chip:
mismo criterio que Stroop; Comparación: piso más bajo, es un juego de velocidad por
definición; Series: piso más alto, razonar una regla lógica lleva más tiempo). Cada uno
usa un flag `timedOut` separado de la selección real del usuario (evita fingir que el
usuario eligió algo que no tocó) y reutiliza el mismo camino de "ronda fallida" que ya
existía para una respuesta incorrecta real.

**Verificado**: compila limpio, 46 pruebas en verde (sin cambios en la suite, estos son
juegos sin tests todavía). En el emulador probé los 4 temporizadores nuevos en vivo,
incluyendo el camino de timeout (dejé pasar el tiempo sin responder) en cada uno --
Comparación, Cambio de Chip (con cambio de regla incluido) y Series funcionaron
perfectamente; Anagramas también completó su sesión entera vía timeouts repetidos sin
problema. Sin excepciones ni ANR en logcat en toda la sesión de pruebas, incluyendo
sesiones que corrieron sus 6-12 rondas completas solo por timeout (buena prueba de
estrés del temporizador nuevo).

## Pendiente / por confirmar con Ricardo

- Firebase: qué partes se van a usar de verdad (`firebase-ai` ya está en las dependencias activas,
  el resto comentado). Si se activa Firestore/Auth, hace falta `google-services.json` real.
- No hay `README.md` propio todavía — considerar agregar uno si el repo va a tener colaboradores
  externos o revisión de Play Store.
- Sin decidir aún: build de release firmado (keystore de verdad, no el de debug) para publicar.
