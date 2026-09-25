# DDA común de NeuroVida (dificultad adaptativa)

Estado: implementado en Unity el 24-sep-2026 (`Assets/Scripts/Games/AdaptiveDifficulty.cs`), conectado a
Stroop, Comparación, Cambio de Chip, Ruta del Tesoro, Detective de Series, Cálculo Sereno y Anagramas.
Secuencia Lumínica y Parejas Ocultas conservan sus motores propios (ver §6).

> **Alcance y honestidad**: esto es un diseño de ingeniería inspirado en literatura psicométrica y de
> entrenamiento cognitivo. **No está validado clínicamente**, no es una herramienta diagnóstica y sus
> parámetros (pasos, umbrales, objetivos) son valores iniciales razonables que deben calibrarse con datos
> reales de uso. Lumosity, Peak y Elevate no publican sus algoritmos exactos; lo que se toma de ellos es lo
> observable y general (dificultad que se adapta por habilidad, seguimiento por dominio, sesiones cortas
> y diarias, retroalimentación inmediata, puntajes comparables entre juegos).

## 1. Qué problema resuelve

Antes había tres lógicas distintas: `SequenceDDAEngine` (ventana de 3 ensayos), `VisualWorkingMemoryDDA`
(controlador continuo hacia ~80% con Z-score de tiempo de reacción) y, en los otros 7 juegos, contadores
improvisados ("cada N aciertos sube un nivel", "2 errores bajan uno"). Los contadores tienen tres defectos:
no apuntan a una tasa de aciertos concreta, no usan la edad ni el tiempo de reacción, y producen saltos
bruscos. Además el único dato entre sesiones era `masteryStreak` (un entero).

## 2. Fundamentos

| Idea | Fuente | Cómo se usa |
|---|---|---|
| El aprendizaje óptimo ocurre con ~85% de aciertos (error ~15%) | Wilson, Shenhav, Straccia & Cohen (2019), *The Eighty Five Percent Rule for optimal learning*, Nature Communications 10:4646 | Objetivo de aciertos: 0.85 en adultos mayores, 0.80 en el resto (algo más conservador que el 85% teórico para mantener motivación) |
| Métodos adaptativos "up-down" convergen a un porcentaje de aciertos | Levitt (1971), *Transformed up-down methods in psychoacoustics*, JASA 49:467 | Base del control por escalones |
| Up-down **ponderado**: paso de bajada = paso de subida × p/(1−p) converge a la tasa p | Kaernbach (1991), *Simple adaptive testing with the weighted up-down method*, Perception & Psychophysics 49:227 | Regla central: sube δ por acierto, baja δ·p/(1−p) por error (con p=0.8: cuatro veces) |
| Estado de "flow": desafío ajustado a la habilidad | Csikszentmihalyi (1990), *Flow* | Ni aburrimiento (bono cada 6 aciertos seguidos) ni frustración (red anti-frustración) |
| Zona de desarrollo próximo | Vygotsky (1978) | Justifica presentar siempre un nivel apenas por encima de lo dominado |
| Aprendizaje sin error reduce frustración en personas con memoria comprometida o mayores | Baddeley & Wilson (1994), *Neuropsychologia* 32:53 | Objetivo de aciertos más alto y subida más lenta en `Senior`; al fallar se muestra la respuesta correcta |
| Dificultades deseables: algo de error favorece el aprendizaje | Bjork (1994) | Por eso el objetivo no es 100% de aciertos |
| Carga cognitiva | Sweller (1988) | Escalera gradual: cada nivel introduce pocas familias nuevas a la vez |
| Enlentecimiento del procesamiento con la edad | Salthouse (1996), *Psychological Review* 103:403 | El tiempo de reacción pesa menos en `Senior` (0.15) que en `Adult` (0.35) y `Under18` (0.40) |

## 3. Algoritmo (`AdaptiveDifficulty`)

Estado: `Rating` continuo en `[1, MaxLevel+0.99]` sobre la escalera del juego. `Level = floor(Rating)`.

Por cada ensayo `Register(correct, reactionMs)`:

- **Acierto**: `Rating += δ · calibración · modulación_RT (+ bono)`
  - `δ` = `stepUp` del juego (0.12–0.5 niveles) × factor de edad (Senior ×0.85, Under18 ×1.1).
  - `calibración` = ×1.5 en los primeros 6 ensayos (rating provisorio: converge más rápido al inicio).
  - `modulación_RT` = `1 + w_RT · clamp(−z, −1, 1)`, con `z` = Z-score del tiempo de reacción contra la
    historia del propio usuario (Welford en línea, desvío mínimo 150 ms). Solo en modos con reloj.
  - `bono` = +0.20 cada 6 aciertos seguidos (anti-aburrimiento).
- **Error**: `Rating −= min(δ · p/(1−p), 1.0) · calibración`; si es el 2.º error seguido, −0.15 extra y
  `Struggling = true` (los juegos muestran "Con calma · Ajustamos la dificultad").
- **Calentamiento**: los 2 primeros ensayos se presentan un nivel por debajo (`PresentedLevel`).
- **Fracción** (`Rating − floor`) queda disponible para parámetros continuos (hoy no usada).

Punto de partida: `StartRating(nivelElegido 1-5, MaxLevel, base_intensity)` reparte el nivel elegido por la
app sobre toda la escalera del juego y suma hasta +1.2 niveles según la maestría acumulada.

Parámetros por juego:

| Juego | Escalera | stepUp Reto / Precisión | Objetivo | Usa RT |
|---|---|---|---|---|
| Stroop | 5 | 0.12 / 0.20 | edad | Reto |
| Cambio de Chip | 5 | 0.12 / 0.20 | edad | Reto |
| Comparación | 7 | 0.15 / 0.25 | edad | Reto |
| Detective de Series | 9 | 0.15 / 0.25 | edad | Reto |
| Cálculo Sereno | 9 | 0.15 / 0.25 | edad | Reto |
| Anagramas | 7 | 0.35 | edad | no |
| Ruta del Tesoro | 12 | 0.50 (por ruta) | 0.70 (perder la ruta cuesta una vida) | no |

## 4. Verificación

`AdaptiveDifficultyTests` (13 pruebas) incluye simulaciones con un usuario logístico: con habilidades 3, 5 y
7 la tasa de aciertos converge a 0.72–0.88 (objetivo 0.80) y el nivel de equilibrio queda cerca de
`habilidad − ln(4)/pendiente`; mayor habilidad termina en mayor nivel; `Senior` apunta a más aciertos que
`Adult`; la razón bajada/subida es p/(1−p); el calentamiento, la red anti-frustración, la modulación por
tiempo de reacción y el bono anti-aburrimiento se prueban aisladamente.

## 5. Persistencia entre sesiones (hecho el 24-sep)

`StroopSessionMetrics` (telemetría común de los 7 juegos) lleva `end_rating` (0..1) y `peak_level`.
`NativeReceiver` los recoge y `recordGameResult` guarda el rating por juego en Room (columna `ddaRating`,
esquema v11), suavizado 60% partida / 40% anterior (`blendDdaRating`) para que un mal día no tire el progreso.
Al abrir un juego, `UnityGameLauncher` envía el rating guardado y `AdaptiveDifficulty.StartRating(config, max)`
continúa desde ahí; sin dato, se usa el nivel elegido más la maestría (`masteryStreak`), como antes.
Pendiente: mostrar el rating en la pantalla de Progreso.

## 6. Lo que NO cambió

- `SequenceDDAEngine` (Secuencia): regla anti-frustración por ventana de 3 ensayos, validada en dispositivo.
- `VisualWorkingMemoryDDA` (Parejas): controlador continuo D(t) hacia ~80% con Z-score. Es la misma familia
  de ideas que el motor común; unificarlos es posible pero no urgente. Ambos ya comparten los perfiles de
  edad (`DdaUserProfileConfig`).

## 7. Siguientes pasos sugeridos

1. (Hecho) Persistir el rating entre sesiones. Falta mostrarlo en Progreso.
2. Recoger datos reales (aciertos por nivel, tiempos) y calibrar `stepUp`, objetivos y ratio de calentamiento.
3. Puntaje normativo por percentil y edad (necesita una muestra; es lo que hacen las apps líderes para
   comparar entre juegos).
4. Migrar Secuencia y Parejas al motor común (o alinear sus objetivos).
5. Considerar un modelo tipo Elo / teoría de respuesta al ítem por familia de ítem (no solo por nivel) si se
   quiere elegir cada ítem según su dificultad real medida.
