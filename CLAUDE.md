# NeuroVida — memoria del proyecto (comprimida 23-sep)

App de estimulación cognitiva: 9 juegos, 6 dominios, dificultad adaptativa, maestría por
dominio (XP), ranking ELO, desafíos semanales. Android nativo (Kotlin + Compose) +
Unity como motor de juego (migración en curso). Repo GitHub:
`https://github.com/ricardohernandezpsico-coder/APP-de-estimulacion-cognitiva`

---

## Toolchain Android (Windows)

- **Android CLI** en `C:\Users\RURAL7\AppData\AndroidCLI` (PATH).
- **Android SDK** en `C:\Users\RURAL7\AppData\Local\Android\Sdk` (android-36, emulador `medium_phone`).
- **JDK 21 Temurin** en `C:\Users\RURAL7\AppData\Local\Temurin21\jdk-21.0.12.1+1` — NO usar JBR de Android Studio (Java 25).
- **Gradle 9.3.1 standalone** en `C:\Users\RURAL7\AppData\Local\Gradle\gradle-9.3.1` — solo para regenerar el wrapper si AI Studio lo borra (`gradle wrapper --gradle-version 9.3.1`).
- `local.properties`: `sdk.dir=C\:\\Users\\RURAL7\\AppData\\Local\\Android\\Sdk` (gitignored).
- `debug.keystore`: `keytool -genkey ... -storepass android -alias androiddebugkey -keypass android -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=Android Debug,O=Android,C=US"` (gitignored).
- **Compilar Android**: `export JAVA_HOME=".../Temurin21/jdk-21.0.12.1+1"` · `./gradlew.bat assembleDebug`.
- **Unity Editor**: 6000.0.84f1 (LTS) + soporte Android. NDK/SDK/OpenJDK/Gradle en `PlaybackEngines/AndroidPlayer/`. Config-cache de Gradle **desactivado** (incompatible con el `build.gradle` que exporta Unity).

## Estado de los 9 juegos (Android, al 23-sep)

| Juego | Motor DDA | FlowMVI | Migrado a Unity |
|---|---|---|---|
| Secuencia Lumínica | SequenceDDAEngine (3 ejes) | ✓ | ✓ FASE 1 CERRADA |
| Parejas Ocultas | VisualWorkingMemoryDDA (2 ejes) | ✓ | ✓ FASE 2 EN CURSO |
| Stroop (Tinta o Palabra) | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |
| Comparación Instantánea | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |
| Cambio de Chip | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |
| Cálculo Sereno | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |
| Detective de Series | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |
| Ruta del Tesoro | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |
| Anagramas | AdaptiveDifficulty (DDA común) | no | ✓ migrado (24-sep, sin probar en dispositivo) |

Suite Kotlin: 51 tests unitarios en verde (`./gradlew.bat testDebugUnitTest`).
Suite Unity: 108/108 en verde (EditMode: `SequenceDDAEngineTests` + `DdaUserProfileConfigTests` + `VisualWorkingMemoryDDATests` + `StroopContractTests` + `ComparisonContractTests` + `ChipContractTests` + `TreasureContractTests` + `SeriesContractTests` + `CalculoContractTests` + `AnagramContractTests` + `AdaptiveDifficultyTests`).

## Room DB (Android, v10 al 21-sep)

`exportSchema = true`, esquemas en `app/schemas/`. Migraciones destructivas SOLO en debug.
Al cambiar esquema: 1) entidad, 2) subir version, 3) `Migration(N, N+1)` en SQL, 4) compilar → comitear `schemas/<N+1>.json`.

---

## Roadmap Unity

**Decisión**: Unity como motor de juego; Home/Progreso/Ajustes/Onboarding se quedan en Compose. Meta-progresión (Maestría/Rank/Retos) se queda en Kotlin/Room.

- **Fase 1 — Secuencia Lumínica**: ✓ CERRADA (aprobada por Ricardo 23-sep)
- **Fase 2 — Resto de juegos**: EN CURSO → Parejas Ocultas primero (Grupo A), luego los 7 restantes (Grupo B: formalizar DDA en Kotlin antes de portar)
- **Fase 3** — Consolidación nativa + Play Store (juegos migrados estables)
- **Fase 4/5/6** — iOS / Backend / WebGL (no prioritarias)

---

## Unity — Arquitectura y archivos clave

**Proyecto**: `NeuroVida/unity/NeuroVidaCore/`
**Export Android**: `unity/AndroidExport/` (615MB, gitignored, regenerar con `NeuroVida > Exportar como librería Android` o `-executeMethod`)
**Assembly order**: `NeuroVida.Contracts` ← `NeuroVida.Bridge` ← `NeuroVida.Games` ← `NeuroVida.Bootstrap`

### Archivos compartidos (`Assets/Scripts/Games/Shared/`)
- `RoundedRectSprite.cs` — sprite redondeado 9-sliced generado por código (SDF de Quilez)
- `RadialGlowSprite.cs` — blob radial suave (para distractores de fondo)
- `DiscSprite.cs` — círculo antialiaseado
- `RingSprite.cs` — anillo que se vacía en tiempo real (countdown timer)
- `HarmonicTone.cs` — tono fundamental + 2 armónicos, envolvente ADSR, AudioReverbFilter "Room"
- `DistractorDrone.cs` — zumbido grave (Re3) para distractor auditivo (niveles ≥12 de Secuencia)
- `HeartSprite.cs` — corazón lleno (rojo brillante) / roto (gris con grieta), colores horneados
- `LivesHud.cs` — fila de vidas en píldora translúcida, animación "salta y rompe" al perder
- `CountdownScreen.cs` — 3-2-1 completo: degradé por paso, burbujas, anillo, onda, confeti en "¡Ya!"
- `Toast.cs` — aviso flotante no bloqueante
- `PhasePill.cs` — píldora de estado con punto de color y "pop" al cambiar
- `UiFx.cs` — chispas, onda expansiva, sacudida, resplandor de fondo
- `UiFonts.cs` — tipografía Outfit (`Assets/Resources/Fonts/Outfit-*.ttf`, OFL); sombra suave, sin combinar con `FontStyle.Bold`

### Contrato bridge (`Assets/Scripts/Contracts/`)
- `SequenceInitConfig.cs` / `SequenceTelemetry.cs` — config entrada (Nativo→Unity) + telemetría salida. Reusada para Parejas en la config de entrada.
- `CardsTelemetry.cs` — telemetría salida de Parejas (distinto a Secuencia: parejas/intentos, no rondas/span)
- `game_id`: string ("secuencia" / "parejas"), coincide con `GameRegistry` en Kotlin

### Lado Kotlin bridge (`app/src/main/java/com/example/bridge/`)
- `NeuroVidaApplication.kt` — expone repositorio para que el bridge llegue a Room sin Context
- `NativeReceiver.kt` — recibe telemetría vía Moshi, espía `game_id` y elige adapter (`SequenceTelemetryDto` vs `CardsTelemetryDto`), llama `repository.recordGameResult`
- `UnityGameLauncher.kt` — lanza `AppUIGameActivity` con config como Intent extra (patrón estable)
- Botones debug en Ajustes (`BuildConfig.DEBUG`): "Probar Secuencia" y "Probar Parejas" en Unity

### Secuencia Lumínica — CERRADA (`Assets/Scripts/Games/Secuencia/`)
- `SequenceDDAEngine.cs` — motor DDA 3 ejes (intacto, sin instanciarse ya en el juego actual)
- `SequenceLevelConfig.cs` — tabla FIJA de 16 niveles (cols×rows, span, ISI, flags distractor). Niveles 17+ extrapolados.
- `TileSprites.cs` — fichas 3D clay (bisel + brillo + labio + sombra horneada, escala 0.86), `TilePalette` 16 colores
- `SequenceGameController.cs` — controlador principal (reglas finales aprobadas):
  - 2 aciertos consecutivos → sube 1 nivel
  - Cada error → −1 vida + +200ms ISI siguiente secuencia (nivel NO baja)
  - Fin: 3 vidas perdidas
  - Grilla puede ser rectangular (2×3, 3×4); `GridDimensionForSpan` mapea span 3-12 a tabla de 16 niveles
  - HUD: insignia circular con nivel + nombre de fase (5 fases), vidas en píldora, puntos de progreso por paso de secuencia (verde/rojo en respuesta), `PhasePill` de estado
  - Transiciones entre rondas: sobre el mismo tablero (sin pantalla completa), fichas salen/entran con rebote si cambia el tamaño de grilla
  - `CanvasScaler` en `ScaleWithScreenSize` (1080×1920) + `SafeAreaContent` desde `Screen.safeArea`
- `SymbolPreviewExporter.cs` (Editor) — vuelca PNG de preview a `unity/test-results/`. Correr tras cualquier cambio de arte procedural.

### Parejas Ocultas — EN CURSO (`Assets/Scripts/Games/Parejas/`)
- `VisualWorkingMemoryDDA.cs` — motor DDA 2 ejes (D(t) continuo: precisión + Z-score tiempo de reacción, pesos por perfil de edad). Puerto 1:1 desde Kotlin.
- `CardsGameContract.cs` — escalera FIJA 10 niveles (2,3,4,5,6,7,8,9,10,12 parejas), sistema 3 fallas, `SymbolBank` por tiers de interferencia, `CountdownReason` (Start/Advance/Demoted/Retry)
- `SymbolSprite.cs` — 14 íconos ilustrados × 4 variantes de color. Capas SDF: contorno + degradé + brillo especular + sombra. `Image.color` blanco (colores horneados).
- `CardSprites.cs` — dorso violeta con rombos + destello; frente blanco (máx contraste); estado "pareja" verde menta; labio 3D
- `CardsGameController.cs` — controlador principal (estado al 23-sep):
  - Memorización (`previewExposureMs`) → flip automático → el jugador busca parejas de a 2 toques
  - Pseudo-flip 2D (squash escala X 1→0→1, cara cambia en el punto medio)
  - Sistema 3 fallas: 3 errores en tablero actual → 1ra baja nivel / 2da repite nivel bajado / 3ra termina
  - Timeout Modo Reto: termina directo si `allowStrictTimeouts`=true (perfil adulto); si false, entra al sistema de 3 fallas
  - Al completar tablero: va al siguiente nivel (hasta 10), animación escalonada out/in, `Toast` con nivel nuevo; pantalla completa 3-2-1 solo al inicio
  - Distractores de fondo con opacidad variable (0.03–0.12 según D(t))
  - Vidas: 3 corazones via `LivesHud`
  - **Bugs ya corregidos**: símbolos eran emojis (invisible con fuente legacy) → reemplazados por `SymbolSprite`; parejas siempre contiguas (Id asignado antes de barajar) → ahora Id se asigna después; `SymbolBank.Select` no completaba mazo de 12 parejas → completa con otros tiers; casillas fantasma en grilla no cuadrada → fila incompleta en vez de celdas vacías

**Pendiente Parejas**: rediseño visual (art + animaciones) NO probado en dispositivo todavía — solo batch + hojas de contacto PNG. Probar jugando antes de cerrar la Fase 2.

---

## Pendientes generales

- **Próximo**: probar Parejas Ocultas en dispositivo real (botón debug en Ajustes)
- Firebase: definir qué partes usar (`firebase-ai` activo, resto comentado) → si se activa, agregar `google-services.json`
- Build release firmado (keystore real) para Play Store
- i18n: juegos hardcodeados en español — extender `Translations` cuando se necesite
- DDA en vivo (`liveIntensity`): faltan Parejas/RutaTesoro/Stroop/Anagramas/Comparación en Android
- Perfiles por edad: piloto activo en Secuencia + Parejas Unity; pendiente extender a los 7 restantes

## Stroop ("Tinta o Palabra") migrado a Unity (24-sep)
- Unity: `Games/Stroop/StroopContract.cs` (reglas puras, puerto 1:1 de `StroopGame.kt`, con tests) y `StroopGameController.cs` (UI por código: tarjeta oscura con palabra neón, cartel de regla que se voltea al cambiar, 5 botones de arcilla, racha con tono ascendente, puntos de avance, barra de tiempo en modo Reto, error que marca la respuesta correcta, panel de resultado). Nuevos compartidos: `Shared/PressScale.cs`, `Shared/ProgressDots.cs`. Contrato de salida `Contracts/StroopTelemetry.cs`.
- Kotlin: `UnityGameLauncher.launchStroop`, `NativeReceiver.parseStroopResult`, botón "[Debug] Probar Tinta o Palabra en Unity" en Ajustes (nivel 4, modo Reto 60 s, para ver el cambio de regla). La ruta normal de la app sigue usando el Stroop Compose.
- Smoke test: `HeadlessPlaymodeSmokeTest.RunStroop` / `RunParejas` (fuerzan el juego vía `EditorPlaytestBootstrap.GameIdOverride`); el pipeline los corre además de Secuencia.
- Pendiente: probar en dispositivo; el diseño no se ha visto en pantalla real.
- Stroop v2 (24-sep, feedback de Ricardo): **Reto = ronda de 60 s sin límite de ensayos** (estilo juegos de velocidad): barra de reloj global, puntos con bonus de racha, tic en los últimos 5 s, ritmo más rápido entre ensayos; puntaje 0-100 = `EndlessScore` (precisión × ritmo, 24 ensayos = ritmo completo). **Precisión (sin reloj) = 12 ensayos**. La regla se lee mejor: cartel con palabra clave enorme TINTA/PALABRA + línea explicativa, etiqueta sobre el borde de la tarjeta y borde de color (azul tinta / verde palabra). Tests: 47.
- Criterio de licencias: mecánica genérica, pero textos/arte/sonidos propios; diferenciar más el juego al cerrarlo (no copiar nombres/gráficos/sonidos de Lumosity).

## Comparación Instantánea migrada a Unity (24-sep)
- `Games/Comparacion/ComparisonContract.cs` (reglas puras: puntos → números cercanos → producto vs número; sin empates; `PrecisionScore` con bono de velocidad, `EndlessScore` con 30 ensayos de ritmo completo) + `ComparisonGameController.cs` (duelo de dos tarjetas de arcilla que entran deslizándose; Reto = 60 s sin límite de ensayos con puntos/racha/bono "¡Rápido!"; Precisión = 12 ensayos; feedback con valores reales "12 > 9"). Reusa `StroopTelemetry` como contrato de salida y los componentes de `Shared/`.
- Kotlin: `UnityGameLauncher.launchComparacion`; `NativeReceiver` parsea "stroop" y "comparacion" con `parseStroopResult`; botón "[Debug] Probar Comparación en Unity (Reto 60 s)" (nivel 2). Smoke: `HeadlessPlaymodeSmokeTest.RunComparacion`.
- Pendiente: probar en dispositivo.

## Cambio de Chip migrado a Unity + Comparación con niveles (24-sep)
- **Comparación v2**: escalera de 7 niveles (puntos → números cercanos → suma/resta vs número → producto vs número → cuenta vs cuenta → cuentas de 3 términos). Dentro de la partida el nivel efectivo sube solo (`CorrectPerLevelUp`: 8 aciertos en Reto, 4 en Precisión) con aviso "Nivel N" y tono. Debug arranca en nivel 2.
- **Cambio de Chip**: `Games/CambioChip/ChipContract.cs` (reglas puras: dirección/posición, `SwitchInterval`, `SurpriseChance`, puntajes) + `ChipGameController.cs` (arena oscura con ficha clara y flecha en uno de 4 bordes, cartel DIRECCIÓN/POSICIÓN que se voltea al cambiar la regla, etiqueta sobre la arena, cruz de 4 botones, Reto 60 s / Precisión 12) + `Shared/ArrowSprite.cs` (flecha procedural). Telemetría reusa `StroopTelemetry`. Kotlin: `launchCambioChip`, `NativeReceiver` ("stroop"/"comparacion"/"cambiochip" → `parseStroopResult`), botón debug (nivel 2, Reto). Smoke: `RunCambioChip`.
- Deuda técnica: los controladores de Stroop, Comparación y Cambio de Chip duplican ~60% (HUD, barra de tiempo, panel de resultado, helpers de layout); candidato a extraer una base común antes de portar más juegos.
- Pendiente: probar en dispositivo.
- Ajustes de UI (24-sep, feedback de Ricardo): Comparación usa layout vertical (tarjetas apiladas a todo el ancho) desde el nivel 3 y el número va en una sola línea con tamaño calculado (`EmWidth`), sin best-fit (que partía "37 - 11" en dos renglones). Los avisos (`Toast`) de Stroop/Comparación/Cambio de Chip van arriba (`SetTopOffset(0)`) para no tapar el tablero. Cambio de Chip refuerza el cambio de regla con `PulseArenaBorder` (borde de la arena late) además del cartel que se voltea.

## Ruta del Tesoro migrada a Unity (24-sep)
- Rediseñada (no port 1:1): `Games/RutaTesoro/TreasureContract.cs` (escalera de 12 niveles, mapa 3x3 → 5x5 y 3 → 12 gemas; 3 vidas; 2 errores pierden la ruta y cuestan una vida; completar sube un nivel, perder baja uno; `ShowMs` de memorización crece con las gemas y baja con la maestría; Reto añade reloj de búsqueda `FindSeconds`; puntaje = rutas completas × 10 con tope 100 (`ClearedForFullScore`); `MaxRounds` 20) + `TreasureGameController.cs` (mapa de casillas de arena sobre fondo de mar, gemas que se iluminan una a una con nota musical, cruz de error, ola de casillas al completar/entrar, revela las gemas que faltaban al perder). Telemetría reusa `StroopTelemetry` (correct_trials = rutas completas). Kotlin: `launchRutaTesoro` + `parseStroopResult` compartido ("rutatesoro"); botón debug (nivel 1, con reloj). Smoke: `RunRutaTesoro`.
- Pendiente: probar en dispositivo.

## Detective de Series migrado + símbolos nuevos de Ruta del Tesoro (24-sep)
- **Ruta del Tesoro**: las gemas doradas "parecían casino" (feedback de Ricardo) → ahora son tesoros de playa (`RutaTesoro/TreasureSprites.cs`: estrella de mar, concha, perla, 2 variantes de color, procedurales) y el color de revelado es celeste suave (`RevealColor`), sin dorado. `SymbolPreviewExporter` también vuelca `preview-treasures.png`.
- **Detective de Series**: `Games/Series/SeriesContract.cs` (6 tipos de serie: suma, multiplicación, resta, diferencia creciente, cuadrados y cubos desde nivel 4; `StepLabels` calcula "+5"/"×3" entre términos; `LiveIntensity`; Reto = 120 s sin límite de series con `EndlessTargetTrials` 14; el nivel efectivo sube cada 4 aciertos (2 en Precisión); Precisión = 8 series) + `SeriesGameController.cs` (4 fichas que aparecen con tic ascendente + ficha "?" con aro que late; al responder se revela la regla entre las fichas y el "?" se vuelve la respuesta; opciones 2x2 de arcilla; fondo índigo con lupa ámbar). Telemetría reusa `StroopTelemetry`. Kotlin: `launchSeries` + parse compartido ("series"); botón debug (nivel 1, Reto). Smoke: `RunSeries`.
- Quedan por migrar: Cálculo Sereno y Anagramas. Deuda técnica: extraer base común de los controladores de velocidad/razonamiento.
- Pendiente: probar en dispositivo.
- **Series v2 (24-sep, feedback: "monótono, faltan patrones más complejos")**: ahora hay 14 familias de patrones que se van sumando por nivel (L1 suma/multiplicación/resta; L2 + diferencia creciente y cuadrados; L3 + Fibonacci, alternar +a/-b, triangulares; L4 + cubos, ×m±c, cuadrados±c; L5+ + dos series intercaladas, alternar +a/×2, primos, con 60% de probabilidad de familias complejas). Las series tienen de 4 a 6 términos (`MaxTokens` 7, `ApplyRow` reparte las fichas) y `StepLabels` puede ser personalizado ("×2 +1", "+a/×2"). `PhasePill` achica o parte en dos renglones los textos largos. Botón debug de Series ahora arranca en nivel 4. Tests: 78.

## Cálculo Sereno migrado a Unity (24-sep)
- `Games/Calculo/CalculoContract.cs`: 18 familias de cuentas repartidas en 7 niveles (sumas/restas, tablas, divisiones exactas, porcentajes, paréntesis, número que falta, productos grandes, cuadrados, factor que falta, cuentas encadenadas, porcentajes difíciles); distractores de "error humano" (±1, ±10, cerca); `FallSeconds`, `PointsFor` (10 + 2 por racha), `Score` (Precisión = puntos×100/(total×15)), `EndlessScore` (Reto = precisión × ritmo, `EndlessTargetTrials` 20). Los tests resuelven cada enunciado de forma independiente para verificar la respuesta.
- `CalculoGameController.cs`: la cuenta viaja en una burbuja que cae a un estanque; Reto = 90 s sin límite de cuentas (la burbuja cae `FallSeconds`, cambia de color calmo→aviso→peligro, tocar el agua cuenta como error); Precisión = 10 cuentas con la burbuja flotando quieta. El nivel efectivo sube cada 4 aciertos (3 en Precisión) con aviso arriba. Telemetría reusa `StroopTelemetry`. Kotlin: `launchCalculo` + parse compartido; botón debug (nivel 2, Reto 90 s). Smoke: `RunCalculo`.
- Solo queda por migrar Anagramas. Deuda técnica: base común para los controladores.
- Pendiente: probar en dispositivo.

## Rampa de dificultad gradual en Series y Cálculo (24-sep)
- Feedback de Ricardo: había series demasiado difíciles; la dificultad debe ir de lo más fácil a lo más complicado, ajustada al DDA.
- Series y Cálculo tienen ahora una escala interna de 9 pasos (`MaxLevel` 9); el nivel elegido 1-5 se mapea a 1, 3, 5, 7, 9. Cada paso introduce familias nuevas (`Introduced[]`) y `PickFamily` saca ~65% de las recién introducidas y el resto de las conocidas. Los números (boost) crecen a la mitad de velocidad; los distractores empiezan lejos (`tightDelta = 12 - level - intensity/4`) y en Cálculo los errores típicos ±1/±10 solo desde el nivel 4. El nivel sube cada 5 aciertos (3 en Precisión) y baja un paso con 2 errores seguidos (`_levelCorrect` / `_wrongRun`). Botones debug de ambos arrancan en nivel 1. Tests: 86.
- Pendiente (Ricardo: "lo vemos después"): unificar esto con un DDA común para todos los juegos.

## Anagramas migrado + fix de Series — los 9 juegos ya están en Unity (24-sep)
- **Anagramas**: `Games/Anagramas/AnagramContract.cs` (banco de ~95 palabras A-Z sin tildes con pista, 7 niveles por largo de 3 a 11 letras, `Pick` sin repetir, `Scramble`, `IsAccepted` acepta otros anagramas válidos del banco, `PointsFor` con -30 por usar la pista) + `AnagramGameController.cs` (fichas de arcilla que vuelan a las casillas con un resorte en `Update`, una ficha colocada se puede tocar para devolverla, validación automática al completar, al fallar las fichas se reordenan mostrando la solución, el cartel superior hace de pizarra para instrucción/pista, botones Borrar/Pista/Pasar; Reto 120 s sin límite de palabras, Precisión 6; sube cada 2 aciertos y baja un paso con 2 errores seguidos). Telemetría reusa `StroopTelemetry`. Kotlin: `launchAnagramas` + parse compartido; botón debug (nivel 1, Reto). Smoke: `RunAnagramas`.
- **Series**: la explicación de la regla ahora va en el cartel superior (`SetBanner`) y no en la `PhasePill`, que tapaba las etiquetas de los pasos (+5, ×3).
- Estado: los 9 juegos existen en Unity, pero solo se abren desde los botones "[Debug]" de Ajustes; la ruta normal de la app sigue usando las versiones Compose.
- Pendiente: probar todo en dispositivo y ajustar; DDA común; base común para los controladores; conectar la app principal a los juegos Unity.

## DDA común (24-sep)
- Motor único de dificultad adaptativa `Games/AdaptiveDifficulty.cs` (up-down ponderado de Kaernbach: sube δ por acierto, baja δ·p/(1−p) por error → converge a la tasa de aciertos objetivo; 0.80 en adultos, 0.85 en mayores; modulación por Z-score del tiempo de reacción con el peso del perfil de edad; calibración inicial ×1.5, calentamiento un nivel abajo, red anti-frustración y anti-aburrimiento). Conectado a Stroop, Comparación, Cambio de Chip, Ruta del Tesoro (objetivo 0.70), Series, Cálculo y Anagramas. Secuencia y Parejas conservan sus motores.
- Diseño, fundamentos y referencias: [`docs/DDA-comun.md`](docs/DDA-comun.md). Tests: `Games/Tests/AdaptiveDifficultyTests.cs` (asmdef propio `NeuroVida.Games.Common.Tests`, 13 pruebas con simulaciones de un usuario logístico).
- La telemetría común (`StroopSessionMetrics`) ahora lleva `end_rating` (0..1) y `peak_level`; `NativeReceiver` los lee (opcionales) pero solo los registra en el log.
- Se eliminó la lógica propia `_levelCorrect`/`_wrongRun` de cada controlador; `CorrectPerLevelUp` y `TreasureContract.NextStage` quedaron sin uso (candidatos a limpieza).
- Pendiente: mostrar el rating en Progreso, calibrar con datos reales, puntaje normativo por percentil/edad, migrar o alinear Secuencia y Parejas.

## Rating del DDA persistido entre sesiones (24-sep)
- **Room v11** (`Migration(10, 11)`: `ALTER TABLE game_progress ADD COLUMN ddaRating REAL NOT NULL DEFAULT -1`; `GameProgressEntity.ddaRating`, -1 = sin dato; esquema `app/schemas/.../11.json`). Flow `repository.gameDdaRating`.
- `GamePlayResult.endRating` (opcional) lo llena `NativeReceiver` desde `end_rating` de la telemetría; `recordGameResult` lo guarda con `blendDdaRating` (`data/DdaRating.kt`, 60% partida / 40% anterior; 4 pruebas en `DdaRatingTest`).
- `UnityGameLauncher` envía `has_dda_rating` + `dda_rating` en la config; en Unity `AdaptiveDifficulty.StartRating(config, max)` continúa desde ese rating (si no hay, usa el nivel elegido + maestría). El nivel elegido en la app solo cuenta la primera vez.
- Suites: Unity 108/108, Kotlin 55/55 (`./gradlew.bat testDebugUnitTest`).
- Pendiente: instalar en el teléfono y verificar que la migración 10→11 no rompe la app (el teléfono estaba desconectado por USB al terminar); mostrar el rating en Progreso.

## Menú principal conectado a Unity (24-sep)
- Los 9 juegos del menú, la sesión diaria y "Jugar de nuevo" ahora abren la versión Unity (reemplazan a los juegos Compose). `MainActivity` monta `ui/UnityGameHost.kt` en vez de los 9 composables: lanza la Activity de Unity (`UnityGameLauncher.launchGame`, con nivel, modo Reto/Precisión, edad, sonido y rating guardado) y, al volver (ON_RESUME), llama a `viewModel.onUnityGameClosed()`.
- Resultado: Unity → `NativeReceiver.onGameFinished` → `UnityResultBus` (SharedFlow) → `NeuroVidaViewModel.onUnityResult` (guarda una sola vez en Room y muestra la pantalla de resultado de la app con `didLevelUp`, sesión diaria, etc.). Si no hay suscriptor (ViewModel muerto) el receptor guarda directo. Partidas lanzadas desde los botones Debug de Ajustes (sin sesión activa) solo se guardan.
- Unity: `Shared/ExitButton.cs` ("Continuar") aparece al terminar en los 9 controladores y llama a `NativeBridge.CloseGameScreen()` (Android: `runOnUiThread { finish() }`). Salir a mitad de partida: botón Atrás de Android (cierra la sesión sin guardar).
- Código Compose de juegos (`app/src/main/java/com/example/games/*Game.kt`, `games/parejas/`, `games/secuencia/`) quedó SIN USO (candidato a borrar cuando Ricardo valide los juegos Unity en el teléfono; los tests Kotlin de contratos/DDA de esos paquetes siguen verdes).
- Verificado: pipeline Unity completo (108 tests, 9 smoke tests, export) + `assembleDebug` + 55 tests Kotlin + instalación. NO verificado en pantalla: el teléfono estaba bloqueado (huella) al intentar la prueba con adb.
- Pendiente: probar el flujo completo en el teléfono (menú → juego → Continuar → resultado → sesión diaria); botón de salir a mitad de partida dentro de Unity si Atrás no cierra bien; borrar el código Compose muerto.

## Fix: la app se cerraba al tocar "Continuar" (24-sep)
- Causa: al cerrarse la Activity de Unity, `UnityPlayer.destroy()` mata el proceso; compartiendo proceso con la app, se cerraba todo.
- Solución: `AppUIGameActivity` corre en `android:process=":unity"` (manifest de `app`). `NativeReceiver.onGameFinished` (proceso Unity) ahora envía un broadcast explícito (`ACTION_GAME_FINISHED`) a `bridge/UnityResultReceiver` (proceso principal), que llama a `NativeReceiver.handleFinished` (parsea y publica al bus / guarda). Compila y Kotlin tests OK; falta confirmar en el teléfono.

## Limpieza y Progreso nuevo (24-sep)
- El código Compose de los 9 juegos (y `ParejasGameLayoutTest`) se movió a `NeuroVida/_respaldo_juegos_compose/` (fuera del código; recuperable, borrar la carpeta cuando se valide). Se conservan contratos y motores DDA de Kotlin con sus tests.
- Progreso: `ui/components/ProgressInsights.kt` (nivel general en anillo, radar de 6 dominios con leyenda, barras por juego) alimentado por `viewModel.gameLevelsForProgress` (rating DDA común; Secuencia/Parejas aproximados con su nivel 1-5 hasta que usen el motor común). Estimación interna, no clínica. Texto en español fijo (falta i18n).

## Posición en campana / percentil (fase A, 24-sep)
- `data/Percentile.kt` (+ `PercentileTest`): percentil de un rating 0..1 frente a una distribución de referencia PROVISIONAL (media 0.45, sd 0.20: supuesto de diseño, no dato). `ProgressInsights.kt`: `BellCurveCard` (campana, punto "tú estás aquí", percentil, delta semanal de puntajes) y "P##" junto al nivel en la leyenda de dominios y en las barras por juego. Rotulado "estimación provisional".
- Decisión de Ricardo: todo lo de servidores/conectores/requisitos de publicación (Play Store/App Store) se deja para el FINAL. Fase B (histograma agregado anónimo por juego y banda de edad que reemplace la referencia, con consentimiento y sin identificadores; muestra mínima ~30) queda documentada, no implementada. Enfoque actual: app, métricas y visual.

## Ligas con escudos (24-sep) — primer paso del sistema de recompensa
- Decisión de Ricardo: la app debe ser masiva, social y motivadora (no clínica): ligas/trofeos compartibles como motor; celebraciones, logros y compartir por historias vienen después. Servidores al final.
- `RankTier` ahora tiene 7 ligas: Bronce, Plata, Oro, Platino, Esmeralda, Diamante, Maestro (250 trofeos cada una, 5 divisiones de 50; Maestro desde 1500 sin techo). `ui/components/LeagueShield.kt`: escudo metálico dibujado por código (paleta por liga, bisel, brillo, estrellas de división) + `GlobalLeagueCard`. Progreso muestra la liga global (promedio de trofeos de los 9 juegos) y un escudo por juego.
- Pendiente: pantalla/animación de ascenso, celebraciones de fin de juego/sesión/hitos de racha, tarjeta compartible (hoja de compartir de Android), logros, y REMODELACIÓN COMPLETA de la interfaz (barra inferior, perfil separado de ajustes).

## Remodelación de interfaz — fase 1: identidad y navegación (24-sep)
- Sistema de diseño (skill ui-ux-pro-max: indigo + naranja energético, estilo arcilla): tema ÚNICO "cosmos" oscuro (`ui/theme/Theme.kt`, `background = Transparent`, superficies azul noche translúcidas); marca azul eléctrico `TealPrimary` (0x5B9BFF) + acento naranja `TealAccent` (0xFF8A3D) (se reutilizaron los nombres viejos de constantes). El modo claro/sistema quedó ignorado.
- `ui/components/CosmosBackground.kt`: fondo animado (degradado noche, resplandores azul/naranja, 72 estrellas que titilan, 2 olas de agua). `ui/components/NeuroNavBar.kt`: barra flotante con Hoy · Juegos · [Entrenar naranja central = sesión diaria] · Liga (=PROGRESO) · Perfil (=AJUSTES). `MainActivity` usa ambos.
- Principios acordados con Ricardo: no saturar (poca información por pantalla, jerarquía clara, revelar detalle bajo demanda), colores llamativos, distintivo. Pendiente: rediseñar cada pantalla (Hoy, Juegos, Liga, Perfil separado de Ajustes), celebraciones, compartir, logros; comprobar contraste de textos con colores fijos en pantallas antiguas.
- No verificado en pantalla (teléfono bloqueado).

## Remodelación — fase 2: "noche + arcilla" aplicada (24-sep)
- Dirección elegida por Ricardo (boceto 24-sep): fondo nocturno animado (`CosmosBackground`, azul más vivo, resplandores celeste/coral) + arcilla en todo lo tocable (borde grueso tinta `Clay.Ink` 0x1A1240, sombra dura, colores Coral/Sun/Sky/Grape/Lime). Tipografía Fredoka (`res/font/fredoka.ttf`, variable, OFL; `ui/theme/Type.kt`). Componentes en `ui/theme/Clay.kt`: `ClayCard`, `ClayButton`, `ClayPill`, `ClayLightCard` (envuelve contenido antiguo en esquema claro para que se lea sobre crema).
- Pantallas: Hoy (`HomeScreen.kt` reescrita: saludo+racha, sesión de hoy en coral, liga+nivel, semana, desafíos plegables, atajo a juegos), barra `NeuroNavBar` en arcilla con botón central amarillo, Juegos y Liga (Progreso) con tarjetas `ClayLightCard`, y NUEVA `ProfileScreen.kt` (pestaña Perfil = AppTab.AJUSTES): avatar, liga, racha/partidas/mejor y botón Ajustes que abre `SettingsScreen` con flecha y BackHandler.
- Pendiente: llevar la MISMA paleta/tipografía a los 9 juegos de Unity (Fredoka + Ink/Coral/Sun/Sky/Grape/Lime, fondo noche); rediseñar Juegos/Liga más a fondo (hoy solo cambian de envoltorio); celebraciones, compartir, logros. Sin verificar en pantalla (teléfono bloqueado).

## Hoy = camino estrellado con historial (24-sep)
- Ricardo rechazó el look de "recuadros/plantilla" (se nota IA): las pantallas deben presentar la información de forma distinta, con objetos y texto suelto en vez de tarjetas. Elegida la propuesta C (camino) + historial deslizable + fondo estrellado con profundidad.
- `ui/screens/HomeScreen.kt` (la versión de tarjetas quedó en `_respaldo_juegos_compose/HomeScreen_tarjetas.kt.txt`): `LazyColumn` de nodos día a día (pasado arriba, hoy grande con pulso y `btn_start_daily_session`, futuro con bandera de hito de racha 3/7/14/30/50/100/200/365). El trazo sinuoso (`fx(i)`) es continuo entre filas (curvas con tangente vertical en los bordes). Tocar un día con partidas abre detalle (juego + puntaje). Cabecera fija de una línea (liga · nivel · racha) y "Desafíos x/y" abre un diálogo. Botón "Volver a hoy" si hoy no está a la vista. Los días sin partidas se ven apagados, sin castigo visual.
- Paralaje: `CosmosScroll.offset` (ui/components/CosmosBackground.kt) lo actualiza el camino; el fondo dibuja 110 estrellas en 3 capas de profundidad, nebulosas lejanas y estelas en las cercanas al deslizar rápido (se lee solo al dibujar, sin recomposición).
- Pendiente: los ascensos de liga no se guardan como evento (no aparecen en el camino del pasado); usar estos hitos para logros/compartir; aplicar la misma idea "sin recuadros" a Juegos (mapa de islas), Liga (órbitas) y Perfil; llevar paleta y fondo a los juegos Unity. Sin verificar en pantalla (teléfono bloqueado).

## Perspectiva "Star Wars" del camino + fondo más oscuro (24-sep)
- El camino de Hoy se inclina hacia el horizonte (`graphicsLayer rotationX = PathTilt 32°`, origen abajo, `cameraDistance`, máscara de desvanecido arriba y abajo con `BlendMode.DstIn`). El fondo (`CosmosBackground`) es más oscuro (0x04061C→0x101A58) y sus 150 estrellas ya NO caen en vertical: nacen en un punto de fuga (50% ancho, 24% alto) y se abren hacia los bordes con perspectiva; al deslizar convergen/emergen de él y las cercanas dejan estelas radiales (efecto hiperespacio). La primera lectura de `CosmosScroll` solo fija la posición inicial (sin estela falsa).
- Verificación visual: por primera vez se probó en el EMULADOR (`emulator -avd medium_phone -no-window -no-audio -gpu swiftshader_indirect`, esperar ~1 min tras el arranque, instalar y `adb -s emulator-5554 exec-out screencap -p`), porque el teléfono real queda bloqueado. Sirve para revisar la UI de Compose (no los juegos Unity, que son arm64).

## Juegos, Liga y Perfil sin tarjetas (24-sep)
- Regla de diseño: ninguna pantalla principal usa recuadros para informar; la información va como texto suelto, objetos (planetas, escudos, nodos) y secciones separadas por una línea fina (`SpaceSectionTitle`). La arcilla queda para lo que se toca (botones, planetas, nodos). Los diálogos modales (intro de juego, detalle de día, desafíos) sí usan tarjeta de arcilla.
- Juegos (`GamesLibraryScreen.kt`): mapa de "planetas" (esfera de arcilla del color del dominio, resplandor, sombra dura, emoji, nombre y "Nivel N · Liga") en filas de 3 con la columna central desplazada; dominios como texto con punto de color que filtran. Tocar un planeta abre el diálogo de intro existente (nivel y modo).
- Liga (`ProgressScreen.kt`, tab PROGRESO): `LeagueHero` (escudo 150dp con brillo, liga, trofeos, barra fina a la siguiente), campana, radar sin tarjeta, `GameLevelsList` (escudo de la liga de cada juego + barra de nivel + percentil), y "Ver más detalles" plegable (maestría por dominio, tendencia, historial). Componentes en `ui/components/SpaceSections.kt`.
- Perfil (`ProfileScreen.kt`, tab AJUSTES): escudo grande con avatar, nombre, tres cifras (racha/partidas/mejor) como texto, "Tus dominios" y botón Ajustes.
- `MainActivity`: el contenido de las pestañas se desvanece arriba y abajo (máscara `DstIn`) en vez de cortarse en seco.
- Verificado en el emulador (capturas de Juegos, Liga, Perfil, Hoy). Pendiente: logros, tarjeta compartible, celebraciones, guardar ascensos de liga, pantalla de resultado y sesión con el mismo estilo, y llevar paleta/fondo a los juegos Unity.

## Rotación de pantalla + cierre robusto de Unity + limpieza (25-sep)
- Bug reportado por Ricardo: girar el teléfono en cualquier juego desarmaba la interfaz, se pegaba o se caía. Causas: Unity en autorrotación (`defaultScreenOrientation: 4`) con controladores diseñados solo para 1080x1920 vertical; MainActivity sin orientación fija, y al recrearse `UnityGameHost` relanzaba el juego (`launched` volvía a false).
- Fix: Player Settings = Portrait (sin autorrotación), `GameEntryPoint.LockPortrait` (`RuntimeInitializeOnLoadMethod BeforeSceneLoad`), `screenOrientation="portrait"` en el manifest para MainActivity y `AppUIGameActivity` (+ `PROPERTY_COMPAT_ALLOW_RESTRICTED_RESIZABILITY` para que Android 16 lo respete en tablets). `launched` es `rememberSaveable`.
- Cierre de sesión: el juego se lanza con `startActivityForResult` (`UnityGameLauncher.buildGameIntent` + `rememberLauncherForActivityResult`). `NativeReceiver.onGameFinished` hace `UnityPlayer.currentActivity.setResult(RESULT_OK)`; `onUnityGameClosed(finished)` espera el resultado (hasta 10 s) solo si terminó; salir a mitad o una caída de Unity (RESULT_CANCELED) cierra la sesión al instante. Reemplaza la espera fija de 1,5 s.
- Unity: quitados paquetes sin uso (purchasing, analytics, ai.*, microsoft.gdk, multiplayer, timeline, xr, tilemap y módulos de terreno/vehículos/tela/viento/video/umbra/web request) y `BillingMode.json`; splash "Made with Unity" desactivado (aparecía en cada partida porque el proceso `:unity` arranca de cero).
- Ajustes: quitado el selector Claro/Oscuro/Sistema (el tema es único; `themeMode` sigue en Room).
- NO compilado ni probado (hecho en un entorno sin SDK de Android ni Unity). Para probar: abrir Unity (resuelve paquetes), reexportar la librería Android, `assembleDebug`, instalar y girar el teléfono en cada juego; salir con Atrás a mitad de partida; terminar una partida y verificar la pantalla de resultado y la sesión diaria.

## Build tras quitar paquetes de Unity (24-sep)
- `AppUIGameActivity` venía del paquete App UI, que llegaba transitivamente con `com.unity.ai.assistant`; al quitarlo dejó de existir. Los juegos usan `com.unity3d.player.UnityPlayerGameActivity` (manifest del plugin en `Assets/Plugins/Android`, manifest de la app y `UnityGameLauncher`). `app/build.gradle.kts` agrega `compileOnly(files(".../unity-classes.jar"))` porque en `unityLibrary` ese jar es `implementation` y la app no ve `UnityPlayer`. Tras esto el export baja a 388 MB y el APK a 115 MB. Probado por Ricardo en el teléfono: giro, Atrás a mitad de partida y resultado, todo OK.

## Base común de controladores Unity — tramo 1: `UiKit` (25-sep)
- `Games/Shared/UiKit.cs` (clase estática): `ApplySafeArea`, `Stretch`, `MakeText`, `BestFit`, `PlaceTopText`, `PopRect`, `PopIn`, `LocalIn`, copiadas tal cual. Los 7 controladores del DDA común borraron sus copias idénticas (50 funciones, ~570 líneas; un script comparó cada cuerpo con la versión compartida antes de borrarlo) y las usan con `using static NeuroVida.Games.Shared.UiKit;` (las llamadas no cambian). Secuencia y Parejas tienen versiones propias distintas y no se tocaron.
- Sin cambio de comportamiento. NO compilado acá (sin Unity): correr la suite EditMode (108) y los 9 smoke tests antes de dar por bueno.
- Tramo 1 verificado por Ricardo (25-sep): 108/108 EditMode, 9 smoke tests, export y `assembleDebug` OK.

## Base común — tramo 2: `GameControllerBase` (25-sep)
- `Games/Shared/GameControllerBase.cs` (abstracta, `: MonoBehaviour`): campos `_config`, `_audioSource`, `_toneCache`, `_flash`, `_resultRoot` (protected) y `Awake` (virtual: crea el AudioSource, llama a `BuildUi()` y se desactiva), `BuildUi()` (abstracto), `PlayTone`, `Flash`, `AnimateResult`, `AddResultText` (devuelve el Text, variante de Stroop; los demás ignoran el valor).
- Los 7 controladores del DDA común heredan de ella (`: GameControllerBase`, `protected override void BuildUi()`), borraron esas 5 funciones y esos 5 campos (~470 líneas; cada cuerpo se comparó con el de la base antes de borrarlo). Sin cambio de comportamiento. Secuencia y Parejas no heredan.
- Juego nuevo del DDA común: heredar de `GameControllerBase`, implementar `BuildUi()` asignando `_flash` y `_resultRoot` (con un hijo "Score" de tipo Text).
- Quedan duplicados que difieren un poco entre juegos (HUD, reloj de ronda, `SetStreak`, `ShowResult`/`BuildResultPanel`): unificarlos cambia detalles visuales, conviene hacerlo junto con la unificación de estilo (paleta/tipografía de la app).
- NO compilado acá: correr EditMode + smoke tests + export + `assembleDebug`.

## Sello "noche + arcilla" en los juegos Unity (25-sep)
Pedido de Ricardo: llevar el estilo de la app a los juegos sin que se vuelvan repetitivos ("cada juego tenga lo suyo, pero con el sello"), y una cuenta regresiva luminosa con estrellas. La skill ui-ux-pro-max no está en el entorno en la nube; se partió del sistema que ella generó y quedó en el código (`Clay.kt`, `CosmosBackground.kt`, Fredoka).
- **Sello común** (`Games/Shared/`): `NeuroStyle.cs` (paleta de la app: Ink/Coral/Sun/Sky/Grape/Lime/Cream + degradé nocturno; `ClayText` = contorno tinta + sombra dura), tipografía **Fredoka** en los 9 juegos (`Resources/Fonts/Fredoka-Bold|SemiBold.ttf`: instancias estáticas 700/600 generadas con fontTools de la fuente variable de la app, que por defecto es Light; Outfit eliminada), `SparkleSprite.cs` (destello de 4 puntas), `StarfieldFx.cs` (estrellas en perspectiva desde un punto de fuga como en la app; `Warp` 0..1 hasta estelas de hiperespacio).
- **Cuenta regresiva** (`CountdownScreen.cs`, misma API): cielo nocturno + nebulosas de la app, estrellas que aceleran en cada número, número de arcilla gigante celeste (3) → uva (2) → coral (1) → sol (¡Ya!), halo del color del paso, anillo de órbita con una estrella que recorre su borde, destellos alrededor, píldora de título crema con borde tinta; "¡Ya!" = hiperespacio + destello + lluvia de estrellas de colores (sin confeti).
- **Mundos** (`WorldBackdrop.cs`: `GameWorld` + `WorldBackdrop.Build` + `WorldAmbient`): todos comparten cielo nocturno, nebulosas y estrellas; cada juego suma UN elemento propio ligado a su mecánica: Secuencia = constelaciones · Parejas = lunas gemelas · Ruta del Tesoro = isla bajo la luna (mar con reflejo y destellos) · Tinta o Palabra = neón (nebulosas coral/uva + luces flotantes) · Comparación = duelo de planetas de arcilla (celeste vs coral, en esquinas opuestas) · Cambio de Chip = órbitas con satélites · Series = lluvia de meteoros a ritmo regular · Cálculo = luna sobre su estanque · Anagramas = letras flotando. Cada controlador reemplazó su fondo (color plano + 2 resplandores + burbujas) por `WorldBackdrop.Build(bgRect, GameWorld.X)`; se borró `BackgroundColor` de los 9.
- NO compilado ni visto en pantalla (sin Unity en el entorno). Revisar en el teléfono: legibilidad del HUD sobre lunas/planetas, que los planetas de Comparación y las órbitas de Cambio de Chip no distraigan, ritmo de los meteoros, y la cuenta regresiva.
- Siguiente paso propuesto: lo tocable de cada juego en arcilla (botones con borde tinta y sombra dura, HUD y panel de resultado comunes), manteniendo los colores propios de cada juego.

## Por qué no se vio el sello en el teléfono + arcilla en lo tocable (25-sep)
- Ricardo probó y vio la cuenta regresiva igual que antes y nada de lo nuevo (solo "puntos blancos muy leves" = las burbujas del fondo viejo). El C# compila limpio (verificado con `tools/unity-compile-check`), así que el APK llevaba el export VIEJO de Unity: `unity/AndroidExport/` está fuera de git y si no se reexporta tras traer la rama, `assembleDebug` empaqueta lo anterior sin avisar. **Siempre: `git pull` → abrir Unity/reexportar → `assembleDebug`.**
- Marca de verificación: en builds de depuración la cuenta regresiva muestra abajo `estilo 25-sep` (`CountdownScreen.StyleStamp`). Si no aparece, el APK trae juegos viejos. Cambiarla con cada cambio visible de Unity.
- `tools/unity-compile-check/UnityCheck.csproj`: compila todo `Assets/Scripts` (sin Editor/Tests) contra UnityEngine 2021.3 + UI 2018.3 de NuGet (`dotnet build tools/unity-compile-check -v q`). Detecta errores de C#; no reemplaza Unity 6. Se validó que detecta un error inyectado.
- `tools/previews/*.py` + `docs/previews/*.png`: réplicas en Python del arte procedural (fichas de arcilla, cuadros de la cuenta regresiva) para ver el diseño sin Unity. Si se cambia `TileSprites.cs` o `CountdownScreen.cs`, actualizar la réplica.
- Skill `ui-ux-pro-max` (ya en `.claude/skills/`): se aplicaron sus reglas de claymorphism (borde grueso, sombra doble, rebote suave), toque (≥48dp, feedback 80-150ms, sin mover el layout al presionar) y contraste (≥4.5:1).
- **Fichas/botones de los 9 juegos** (`Secuencia/TileSprites.cs`, el sprite que usan todos): de ficha gris con bisel y sombra difusa a arcilla: borde oscuro grueso, sombra dura sólida, cara con canto de luz, sombra interior y brillo. Misma huella (`ShapeScale` 0.86): no se mueve ningún layout. `TileSprites.GetPressed()` = cara hundida; `PressScale` cambia a esa ficha al presionar (y escala 0.97) en vez de encoger el botón.
- `NeuroStyle.ClayFrame` (borde tinta + sombra dura con efectos de uGUI) en: botón "Continuar" (ahora sol con texto tinta, como "Entrenar" de la app), `Toast`, `PhasePill`, y el panel de resultado de los 7 juegos del DDA común (`GameControllerBase.StyleResultPanel`: superficie azul noche `NeuroStyle.Surface`, puntaje gigante en sol "de arcilla").
- Cuenta regresiva: número de 118 a 140 dp.

## Flujo de trabajo nube ↔ PC (25-sep)
- Las sesiones en la nube editan, verifican C# con `tools/unity-compile-check` y hacen `git push` a la rama. Todo lo que necesita Unity, JDK, SDK o el teléfono corre en el PC de Ricardo (Git Bash, carpeta del repo): `git pull && bash tools/verificar-todo.sh --instalar` (escena piloto → EditMode 108 → smoke de los 9 juegos → REEXPORTAR → Gradle con tests Kotlin → instalar). El script no hace `git pull` por su cuenta.
- Si algo falla: pegar las últimas 40 líneas del log correspondiente en `unity/test-results/v-*.log`.
- Resultado 25-sep sobre `b6169f3`: 108/108, 9 smoke OK, export 390 MB, BUILD SUCCESSFUL, "estilo 25-sep" presente en el APK. Ricardo: cuenta regresiva aprobada ("muy bien"), mundos bien; detalles de pulido quedan para después.

## Unity persistente entre partidas: fin de los 5-8 s de carga por juego (25-sep)
- Reporte de Ricardo: al abrir cada juego (y al volver) aparecía el indicador de carga 5-8 s. Causa: cada partida arrancaba Unity en frío en un proceso nuevo (`:unity`) y al terminar `finish()` + `UnityPlayer.destroy()` mataba ese proceso; el indicador era el de `UnityGameHost` debajo mientras el motor arrancaba.
- Ahora Unity queda VIVO detrás de la app entre partidas:
  - Lanzar: `UnityGameLauncher.buildIntent` agrega `FLAG_ACTIVITY_REORDER_TO_FRONT` + `EXTRA_LAUNCH_ID` (UUID por partida). La Activity de Unity, si existe, vuelve al frente sin reiniciar el motor (onNewIntent actualiza getIntent).
  - Unity: `LaunchIntentConfigReader.TryStart` (en Start, OnApplicationFocus(true) y OnApplicationPause(false)) arranca la partida si el id del Intent es nuevo (`s_startedLaunchId` estático); si la escena ya se usó, primero la recarga.
  - Volver ("Continuar" / Atrás): `NativeBridge.CloseGameScreen` → Kotlin `NativeReceiver.returnToApp()` (proceso `:unity`) trae `MainActivity` al frente (REORDER_TO_FRONT) con `EXTRA_RETURN_FROM_GAME`, el launch id y el JSON del resultado si la partida terminó; luego recarga la escena en reposo. Si `returnToApp` falla, respaldo: `finish()` como antes.
  - App: `MainActivity.onNewIntent/handleGameReturn` → `NeuroVidaViewModel.onReturnedFromGame(launchId, json)`: resultado al instante (sin esperar broadcast) o, sin resultado, cierra la sesión. `onHostResumed` (ON_RESUME en `UnityGameHost`) cierra la sesión si Unity se cayó y la app volvió sin esa vuelta. Se eliminó `startActivityForResult`/`onUnityGameClosed` y la espera de hasta 10 s.
  - El broadcast `ACTION_GAME_FINISHED` sigue como respaldo (usuario que nunca vuelve, app recreada); `bridge/UnityResultInbox` (SharedPreferences, últimos 30 ids) garantiza que cada partida se guarde una sola vez.
  - Atrás en la pantalla raíz de la app: `moveTaskToBack(true)` (Unity está debajo en la misma tarea; cerrar MainActivity lo dejaría a la vista).
  - La cámara de la escena pinta `NeuroStyle.NightBottom` (antes: cielo por defecto de Unity) para el instante en reposo.
- La PRIMERA partida tras abrir la app sigue siendo arranque en frío (pendiente: pantalla de carga con el estilo de la app). Si Android mata `:unity` en segundo plano por memoria, la siguiente partida arranca en frío (normal).
- Marca de verificación: `estilo 25-sep · b`.
- NO compilado el Kotlin acá (sin SDK). Probar en el teléfono: 1) primera partida (lenta, normal); 2) "Continuar" → resultado al instante; 3) otra partida → debe abrir en ~1 s; 4) Atrás a mitad → vuelve y cierra la sesión; 5) Inicio a mitad de partida y volver con el ícono → sigue el juego; 6) Atrás en Hoy → la app pasa a segundo plano (no se ve Unity).

## Pantalla de resultado de la app con el sello (25-sep)
- `games/GameResultScreen.kt` reescrita ("noche + arcilla", reglas de ui-ux-pro-max): sin tarjetas, sobre el cielo; protagonista único animado = puntaje que cuenta (Fredoka 104sp, relleno sol + contorno y sombra tinta vía `TextStyle.drawStyle = Stroke`), luego 3 estrellas de arcilla con rebote (la del medio más grande) y lluvia de destellos si hay 2-3 estrellas; halo del color del dominio que respira. Debajo: frase según puntaje, fila suelta aciertos · nivel · modo, píldora lima de subida de nivel, liga del juego (`LeagueShield` + "Bronce 3" + trofeos, nuevo parámetro `rank`), avance de la sesión diaria como nodos de arcilla coral, y `ClayButton` sol (Continuar / Siguiente juego) + crema (Jugar de nuevo). Atrás = Continuar. Con "quitar animaciones" del sistema (`ANIMATOR_DURATION_SCALE` = 0) todo aparece quieto.
- Textos sin promesas de salud (se sacó "fortalece las conexiones sinápticas").
- `MainActivity`: la pestaña (Hoy/Juegos/...) ya no se compone mientras hay un juego o un resultado encima (con el fondo transparente se veía detrás y podía recibir toques).
- Vista previa: `tools/previews/pantalla_resultado.py` → `docs/previews/pantalla-resultado.png` (aproximada; el escudo real es el de `LeagueShield`).
- Kotlin NO compilado acá.

## Pausa real y retomar la partida (25-sep)
- Reporte de Ricardo: a mitad de partida, Atrás → cerrar la app → volver con el ícono → Play: el juego empezaba de cero. Causa: Atrás a mitad de partida ABANDONABA (volvía a la app y cerraba la sesión) y la sesión diaria relanzaba el mismo juego como partida nueva.
- Ahora (estilo de las apps de referencia): Atrás a mitad de partida (o la app pasa a segundo plano: `OnApplicationPause(true)`) abre `Games/Shared/PauseMenu.cs` (velo + panel de arcilla: Continuar / Reiniciar / Salir; Atrás con el menú abierto = Continuar). Con la partida terminada o sin partida, Atrás vuelve a la app como antes (`GameEntryPoint.Update`, `NativeBridge.GameFinished`).
- Pausa real: `Games/Shared/GameClock.cs` (Time/DeltaTime pausables; en pausa `timeScale = 0` y `AudioListener.pause`). Se reemplazaron 106 usos de `Time.unscaledTime`/`unscaledDeltaTime` en los 9 controladores y en Shared (cuenta regresiva, FX, HUD), salvo `StarfieldFx`, `WorldBackdrop`, `PressScale`, `PauseMenu` (el fondo sigue vivo en pausa). `GameClock.Reset()` en cada carga de escena (`GameEntryPoint.Awake`). **Código nuevo de juegos: usar `GameClock.Time`/`GameClock.DeltaTime`, no `Time.unscaled*`.**
- Reiniciar: `LaunchIntentConfigReader.RestartCurrentGame()` (misma partida desde cero, escena limpia).
- Salir: `NativeBridge.ReturnToAppPaused()` → Kotlin `NativeReceiver.returnToApp(paused = true)` (`@JvmOverloads`; `EXTRA_PAUSED`) trae la app al frente SIN recargar la escena. `NeuroVidaViewModel.onReturnedFromGame(..., paused = true)` guarda `pausedGame` (sesión + launch id); `launchGame` del mismo juego (sesión diaria, biblioteca; no "Jugar de nuevo") relanza con `ActiveGameSession.resumeLaunchId` → mismo id → Unity no reinicia y se ve el menú de pausa para continuar. Abrir otro juego descarta la pausa.
- Límites: si se cierra la app desde Recientes (o Android mata Unity por memoria), la partida en pausa se pierde y el juego empieza de cero (no se persiste el estado interno de las partidas).
- Marca: `estilo 25-sep · c`. Kotlin NO compilado acá.
- Fix posterior (`ec10b62`, otra sesión, probado por Ricardo): `pausedGame` se persiste en SharedPreferences ("paused_game": juego, nivel, modo, diario, intensidad, launch id). Caso real en su Motorola: al salir al escritorio Android destruía la app mientras el proceso `:unity` quedaba congelado con la partida en pausa; al volver, Play generaba un id nuevo y Unity reiniciaba. Revisado: si Unity murió, el id reusado solo arranca una partida limpia (sin duplicar resultados); abrir cualquier juego borra la pausa guardada.

## Pantalla de carga de los juegos (25-sep)
- `ui/components/GameLoadingScreen.kt`: planeta del juego (esfera de arcilla del color del dominio, como en la biblioteca) con una luna sol que lo orbita en elipse inclinada (pasa por detrás y por delante: la órbita ES el indicador de carga), nombre, "Nivel N · Tier · Reto/Precisión", "Cómo se juega" (`GameDefinition.instruction`) y "Preparando el juego…". Respeta "quitar animaciones".
- Se ve en dos lugares con el mismo aspecto, para que el paso app → Unity no se note: 1) `UnityGameHost` (en la app, mientras Android levanta la Activity de Unity; aparece tras 250 ms para no destellar en el arranque en caliente); 2) `bridge/UnityLoadingOverlay.kt` ENCIMA de la Activity de Unity durante el arranque en frío: `ActivityLifecycleCallbacks` instalados en `NeuroVidaApplication` (corre en cada proceso), agrega la capa (`ComposeView` sobre cielo `CosmosBackground`, bloquea toques) en el primer onStart de `UnityPlayerGameActivity` y la quita con fundido cuando Unity avisa `NativeBridge.NotifyGameShown()` (`LaunchIntentConfigReader`, 2 cuadros después de arrancar la partida) → `NativeReceiver.onGameShown`. Sin aviso, se quita sola a los 20 s. Con Unity vivo no hay onCreate: no aparece.
- Fondo de ventana de la app = `@color/nv_night` (y `windowSplashScreenBackground` en Android 12+): sin destello de otro color al abrir.
- Vista previa: `tools/previews/pantalla_carga.py` → `docs/previews/pantalla-carga.png`.
- Marca: `estilo 25-sep · d`. C# verificado con `tools/unity-compile-check`; Kotlin NO compilado acá. Probar: cerrar la app desde Recientes → abrir → jugar (primera partida: debe verse la pantalla de carga en vez de negro/indicador hasta la cuenta regresiva); segunda partida (rápida, casi no se ve).
- Probado por Ricardo en el teléfono (25-sep): funciona como se describe.

## Íconos propios de los juegos (25-sep)
- `ui/components/GameIcon.kt` (`GameIcon(gameId, size)`): 9 íconos dibujados por código en arcilla (relleno plano, borde tinta, sombra dura, brillo) en un lienzo de 100x100 unidades, en vez de emojis (cambian según el fabricante y no dicen nada de la mecánica). Secuencia = 4 fichas con una encendida · Parejas = carta boca abajo + carta dada vuelta · Ruta del Tesoro = estrella de mar · Tinta o Palabra = gota de tinta + tarjeta con palabra · Cambio de Chip = ficha con flecha + flechas de cambio · Series = lupa sobre puntos que crecen · Anagramas = fichas A/Z · Cálculo = + − × = · Comparación = círculo grande > chico. Id desconocido → emoji de `iconEmoji` (se conserva el campo).
- Usado en: planetas de la biblioteca, diálogo de inicio del juego, historial (`ProgressTrendChart`) y pantalla de carga. Los desafíos semanales siguen con emoji.
- `GameRegistry`: Stroop se llama "Tinta o Palabra" (como en Unity) e instrucciones reescritas para describir los juegos de Unity actuales (se leen en la pantalla de carga).
- Vista previa: `tools/previews/iconos_juegos.py` → `docs/previews/iconos-juegos.png` (si se cambia `GameIcon.kt`, actualizar la réplica). Kotlin NO compilado acá.

## Celebración de ascenso de liga (25-sep)
- `NeuroVidaRepository.recordGameResult` devuelve `RecordOutcome` (antes `Boolean`): `didLevelUp` + trofeos del juego y de la liga general (promedio de los 9 juegos, sin jugar = 0) antes y después. `RecordOutcome.promotion(gameId)` → `LeaguePromotion` si se cruzó a una liga superior (prioridad: liga general, luego la del juego); bajar no se celebra. Pruebas: `test/.../model/LeaguePromotionTest.kt` (4).
- `NeuroVidaViewModel.promotion` (se fija junto con `lastResult`; `dismissPromotion()`). `MainActivity` pinta `ui/components/LeaguePromotionOverlay.kt` encima de la pantalla de resultado, 1,7 s después (primero se ve el puntaje): velo nocturno, escudo viejo que tiembla y estalla, escudo nuevo con rebote sobre rayos del color de la liga que giran, destello, lluvia de destellos, vibración; "Subiste a Plata" / "en <juego>" o "Tu liga general", trofeos y próxima liga; botones "¡Genial!" (sol) y "Compartir" (hoja de compartir de Android, texto corto). Atrás = "¡Genial!". Respeta "quitar animaciones".
- Límite: si la app no estaba viva al terminar (resultado guardado por el broadcast de respaldo), no se celebra (pero el ascenso sí queda guardado, ver abajo).
- La celebración no depende de que el resultado siga abierto (si se cerró antes de 1,7 s, aparece sobre la pestaña); mientras se muestra se oculta la barra inferior. Botón "[Debug] Ver celebración de ascenso de liga" en Ajustes (`viewModel.debugShowPromotion()`, Bronce → Plata en Secuencia). Probado por Ricardo en el teléfono: aprobado.

## Ascensos en el camino de Hoy + tarjeta para compartir (25-sep)
- **Eventos de ascenso**: `data/LeagueEvents.kt` (`LeagueEvent(timestamp, tier, gameId?)`, `encode/decodeLeagueEvents`: una línea `ts|TIER|gameId`; pruebas en `LeagueEventsTest`). `NeuroVidaRepository.recordGameResult` guarda TODOS los ascensos de la partida (liga general y del juego; `RecordOutcome.globalPromotion()` / `gamePromotion()`) en SharedPreferences "league_events" (máx. 500; sin Room para no migrar) → `repository.leagueEvents` / `viewModel.leagueEvents`. Se guardan también si el resultado llegó por el broadcast de respaldo.
- **Hoy** (`HomeScreen.kt`): el día con ascenso lleva un escudito (26 dp) pegado arriba del nodo (la liga general manda) y la línea "Subiste a Plata" en el color de la liga; el detalle del día lista primero los ascensos (escudo + "Subiste a X" + juego o "Liga general"). Los ascensos anteriores a esta versión no se guardaron.
- **Tarjeta para compartir** (`ui/components/ShareCard.kt`): imagen 1080x1920 (formato historia) dibujada fuera de pantalla con `CanvasDrawScope` + `TextMeasurer` (Fredoka) y el mismo escudo (`LeagueShield.kt` ahora expone `DrawScope.drawLeagueShield`): cielo nocturno con nebulosas y estrellas, marca "NeuroVida", escudo sobre rayos del color de la liga, titular ("Subí a Plata" / "Estoy en liga Oro"), dónde, trofeos y racha, pie "Juega. Entrena. Sube de liga.". Se guarda en `cache/share/` y se comparte con `FileProvider` (`${applicationId}.fileprovider`, `res/xml/share_paths.xml`, declarado en el manifest) + texto corto. Si falla, se comparte solo el texto.
- Se usa en: "Compartir" de la celebración de ascenso y la píldora "Compartir mi liga" bajo el escudo de la pestaña Liga (liga general).
- Vista previa: `tools/previews/tarjeta_compartir.py` → `docs/previews/tarjeta-compartir.png`. Kotlin NO compilado acá.

## Logros + celebración de hitos de racha (25-sep)
- **Catálogo** (`data/Achievements.kt`, lógica pura): 18 logros derivados del historial y los trofeos con `computeAchievementStats` (partidas, racha MÁS LARGA de días seguidos, máx. partidas en un día, juegos y dominios distintos, mejor puntaje, partidas en Reto, mejor trofeo de un juego, liga general). Primer paso · rachas 3/7/14/30/100 (= banderas del camino de Hoy, así llegar a una bandera se celebra) · Día completo (3 en un día) · Explorador (9 juegos) · Mente completa (6 dominios) · Brillante (90+) · Perfecto (100) · Contrarreloj (10 en Reto) · Constante (25) · Centenario (100) · Plata/Oro/Platino en un juego · Oro general. Cada uno con `progress` (actual, meta). Pruebas: `AchievementsTest` (6).
- **Registro**: SharedPreferences "achievements" (`id|timestamp`). `NeuroVidaRepository.seedAchievementsIfNeeded()` (en `init` y al inicio de `recordGameResult`): la primera vez registra lo YA conseguido sin celebrar (si no, la próxima partida dispararía todos los logros viejos). `recordGameResult` → `RecordOutcome.newAchievements` (en orden de catálogo).
- **Medalla** (`ui/components/AchievementMedal.kt`, `DrawScope.drawAchievementMedal`, usada también en la tarjeta para compartir): disco de arcilla del color del logro (racha coral, sesión sol, explorar celeste, puntaje uva, volumen lima, liga = color de la liga) con dibujo propio (play, llama, calendario, brújula, hexágono, estrella, rayo, check, escudo de la liga) y píldora sol con la cifra; bloqueada = silueta translúcida.
- **Celebración** (`ui/components/AchievementOverlay.kt`): cola `viewModel.achievementQueue`; en `MainActivity` va después del ascenso de liga (si lo hubo), de a uno (la primera a 1,7 s, las siguientes a 0,25 s); medalla que cae con rebote sobre rayos, destellos, vibración, "N de 18 logros", "¡Genial!" / "Compartir" (tarjeta con la medalla). La barra inferior se oculta mientras hay celebraciones.
- **Perfil**: sección "Logros" (x de 18) con medallas en filas de 4, nombre y "4/7" en las bloqueadas; tocar abre el detalle (fecha en que se consiguió o barra de avance).
- `ShareCard.Content` generalizado: `tier` o `achievement` + `stats` (hasta 2 cifras; `trophiesAndStreak`).
- Botón "[Debug] Ver celebración de logro" en Ajustes (encola "Una semana").
- Vista previa: `tools/previews/medallas_logros.py` → `docs/previews/medallas-logros.png`. Kotlin NO compilado acá.
