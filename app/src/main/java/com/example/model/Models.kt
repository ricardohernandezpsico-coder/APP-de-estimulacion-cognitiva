package com.example.model

import androidx.compose.ui.graphics.Color
import com.example.ui.theme.*

enum class DomainType(
  val displayName: String,
  val description: String,
  val color: Color
) {
  MEMORIA("Memoria", "Retención visual, espacial y secuencial", DomainMemoria),
  ATENCION("Atención", "Control inhibitorio y flexibilidad mental", DomainAtencion),
  RAZONAMIENTO("Razonamiento", "Lógica inductiva y deducción de patrones", DomainRazonamiento),
  LENGUAJE("Lenguaje", "Fluidez verbal, léxico y ortografía", DomainLenguaje),
  CALCULO("Cálculo", "Agilidad numérica y resolución aritmética", DomainCalculo),
  VELOCIDAD("Velocidad", "Tiempo de reacción y discriminación visual", DomainVelocidad);
}

enum class LevelTier(val tierName: String, val levelNumber: Int) {
  PRINCIPIANTE("Principiante", 1),
  INICIADO("Iniciado", 2),
  INTERMEDIO("Intermedio", 3),
  AVANZADO("Avanzado", 4),
  EXPERTO("Experto", 5);

  companion object {
    fun fromLevel(lvl: Int): LevelTier {
      return when (lvl.coerceIn(1, 5)) {
        1 -> PRINCIPIANTE
        2 -> INICIADO
        3 -> INTERMEDIO
        4 -> AVANZADO
        else -> EXPERTO
      }
    }
  }
}

data class GameDefinition(
  val id: String,
  val title: String,
  val domain: DomainType,
  val subtitle: String,
  val instruction: String,
  val iconEmoji: String,
  val defaultRounds: Int = 10
)

object GameRegistry {
  val allGames = listOf(
    GameDefinition(
      id = "parejas",
      title = "Parejas Ocultas",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria de trabajo visual",
      instruction = "Memoriza la posición de las figuras y encuentra todas las parejas idénticas en el menor número de intentos.",
      iconEmoji = "🃏"
    ),
    GameDefinition(
      id = "secuencia",
      title = "Secuencia Lumínica",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria secuencial a corto plazo",
      instruction = "Observa con atención la secuencia de luces de colores y reprodúcela en el orden exacto.",
      iconEmoji = "💡"
    ),
    GameDefinition(
      id = "rutatesoro",
      title = "Ruta del Tesoro",
      domain = DomainType.MEMORIA,
      subtitle = "Memoria visoespacial",
      instruction = "Memoriza dónde aparecen los tesoros en la cuadrícula antes de que se oculten y recupéralos todos.",
      iconEmoji = "💎"
    ),
    GameDefinition(
      id = "stroop",
      title = "Color o Palabra",
      domain = DomainType.ATENCION,
      subtitle = "Efecto Stroop & Inhibición",
      instruction = "Elige el COLOR con el que está escrita la palabra, ignorando lo que dice el texto.",
      iconEmoji = "🎨"
    ),
    GameDefinition(
      id = "cambiochip",
      title = "Cambio de Chip",
      domain = DomainType.ATENCION,
      subtitle = "Flexibilidad cognitiva",
      instruction = "Atiende a la regla activa en cada momento: responde según hacia dónde apunta la flecha o según en qué cuadrante está situada.",
      iconEmoji = "🔄"
    ),
    GameDefinition(
      id = "series",
      title = "Detective de Series",
      domain = DomainType.RAZONAMIENTO,
      subtitle = "Lógica secuencial",
      instruction = "Descifra la regla matemática o geométrica que gobierna la serie y selecciona la opción que continúa la secuencia.",
      iconEmoji = "🔍"
    ),
    GameDefinition(
      id = "anagramas",
      title = "Anagramas",
      domain = DomainType.LENGUAJE,
      subtitle = "Léxico y procesamiento fonológico",
      instruction = "Descubre la palabra escondida reordenando las letras desordenadas. ¡Puedes pedir una pista si la necesitas!",
      iconEmoji = "🔤"
    ),
    GameDefinition(
      id = "calculo",
      title = "Cálculo Sereno",
      domain = DomainType.CALCULO,
      subtitle = "Aritmética mental",
      instruction = "Resuelve las operaciones matemáticas con precisión y mantén tu racha de aciertos.",
      iconEmoji = "🧮"
    ),
    GameDefinition(
      id = "comparacion",
      title = "Comparación Instantánea",
      domain = DomainType.VELOCIDAD,
      subtitle = "Velocidad perceptiva",
      instruction = "Determina al instante cuál de los dos paneles contiene mayor cantidad o un valor numérico superior.",
      iconEmoji = "⚡"
    )
  )

  fun getById(id: String): GameDefinition? = allGames.find { it.id == id }
}

data class GamePlayResult(
  val id: String = java.util.UUID.randomUUID().toString(),
  val gameId: String,
  val score: Int, // 0 to 100
  val correctAnswers: Int,
  val totalTrials: Int,
  val timed: Boolean,
  val level: Int,
  val timestamp: Long = System.currentTimeMillis(),
  // Rating final del DDA común (0..1) informado por los juegos Unity; null en los demás.
  val endRating: Float? = null
)

data class DailySessionState(
  val dateKey: String,
  val gameIds: List<String>,
  val completedCount: Int = 0,
  val scores: List<Int> = emptyList()
)


enum class DifficultyMode(val label: String, val description: String) {
  ADAPTIVE("Auto-adaptativa", "Ajuste dinámico según tu desempeño en cada sesión"),
  PRINCIPIANTE("Principiante", "Nivel 1 con tiempos generosos y estímulos claros"),
  INTERMEDIO("Intermedio", "Nivel 3 equilibrado para mantener agilidad mental"),
  AVANZADO("Avanzado", "Nivel 5 con máxima exigencia cognitiva y velocidad"),
  CUSTOM("Personalizada", "Nivel asignado a medida por cada dominio cognitivo")
}

enum class ThemeMode(val label: String) {
  LIGHT("Claro"),
  DARK("Oscuro"),
  SYSTEM("Sistema")
}

/**
 * Piloto de perfiles por edad (20-sep): Ricardo compartió material sobre calibrar el DDA
 * (y la accesibilidad de la UI) según el perfil del usuario en vez de un solo valor
 * global para todos -- ej. el área táctil/espaciado recomendado para adultos mayores es
 * literalmente más grande que lo que un adulto joven encuentra cómodo, no hay un
 * "tamaño correcto" único. Arranca como piloto en Parejas Ocultas
 * (`com.example.games.parejas`, ver `LocalAgeBand`), no en los 9 juegos todavía.
 * `null` en [UserSettings.ageBand] significa "todavía no se preguntó" -- gatilla la
 * pantalla de onboarding una sola vez.
 */
enum class AgeBand(val label: String) {
  UNDER_18("Menos de 18"),
  ADULT("18 a 64"),
  SENIOR("65 o más")
}

/**
 * Maestría por dominio (meta-progresión, etapa 4 de la propuesta de gamificación):
 * a diferencia del nivel de cada juego (tope visible en 5), la XP de dominio no
 * tiene techo — sigue dando sensación de avance aunque todos los juegos de ese
 * dominio ya estén en Experto. Un dominio suma XP jugando CUALQUIER juego suyo.
 */
enum class MasteryTier(val tierName: String, val minXp: Int, val icon: String) {
  BRONCE("Bronce", 0, "🥉"),
  PLATA("Plata", 100, "🥈"),
  ORO("Oro", 300, "🥇"),
  PLATINO("Platino", 600, "💠"),
  DIAMANTE("Diamante", 1000, "💎"),
  MAESTRO("Maestro", 2000, "👑");

  companion object {
    fun fromXp(xp: Int): MasteryTier = entries.lastOrNull { xp >= it.minXp } ?: BRONCE
  }
}

data class DomainMasteryInfo(
  val domain: DomainType,
  val xp: Int
) {
  val tier: MasteryTier get() = MasteryTier.fromXp(xp)
  private val nextTier: MasteryTier? get() = MasteryTier.entries.firstOrNull { it.minXp > xp }
  /** Progreso 0f..1f dentro del tramo actual (para la barra). Si ya es Maestro,
   * sigue avanzando en tramos de 1000 XP para que la barra nunca quede "llena y
   * quieta" — el tope real no existe, solo cambia el tamaño del próximo tramo. */
  val progressInTier: Float get() {
    val next = nextTier
    return if (next != null) {
      ((xp - tier.minXp).toFloat() / (next.minXp - tier.minXp)).coerceIn(0f, 1f)
    } else {
      ((xp - tier.minXp) % 1000) / 1000f
    }
  }
  val xpLabel: String get() {
    val next = nextTier
    return if (next != null) "$xp / ${next.minXp} XP" else "$xp XP"
  }
}

/**
 * Desafíos semanales (etapa 4): fijos y deterministas — no aleatorios — para dar
 * "variedad forzada con motivo". El progreso se calcula en vivo desde el
 * historial de la semana (no se persiste), solo se guarda si ya se reclamó el
 * premio para no volver a otorgarlo al recalcular.
 */
data class WeeklyChallengeDef(
  val key: String,
  val title: String,
  val description: String,
  val iconEmoji: String,
  val target: Int
)

object WeeklyChallengeRegistry {
  val all = listOf(
    WeeklyChallengeDef("dominios3", "Entrenamiento variado", "Jugá en 3 dominios cognitivos distintos esta semana", "🧭", 3),
    WeeklyChallengeDef("reto2", "Modo Reto", "Completá 2 partidas en modo Reto esta semana", "⚡", 2),
    WeeklyChallengeDef("precision3", "Racha de precisión", "Conseguí 85 puntos o más en 3 partidas esta semana", "🎯", 3),
    WeeklyChallengeDef("dias4", "Constancia", "Entrená en 4 días distintos esta semana", "📅", 4)
  )
  const val XP_REWARD_PER_DOMAIN = 10
}

data class WeeklyChallengeProgress(
  val def: WeeklyChallengeDef,
  val progress: Int,
  val claimed: Boolean
) {
  val isComplete: Boolean get() = progress >= def.target
}

/**
 * Ranking tipo ELO por juego (idea de Ricardo, 20-sep): a diferencia de Maestría por
 * Dominio (XP que solo crece), el rating de un juego sube Y baja según el desempeño
 * de cada partida — da la sensación de "rango" que se puede perder, como en un
 * ladder competitivo. Es puramente un layer de progreso/motivación: no reemplaza
 * ni modifica el nivel de dificultad (1-5) ni masteryStreak, que siguen intactos.
 */
enum class RankTier(val tierName: String, val minRating: Int, val icon: String, val color: Color) {
  BRONCE("Bronce", 0, "🥉", Color(0xFFCD7F32)),
  PLATA("Plata", 250, "🥈", Color(0xFF9CA3AF)),
  ORO("Oro", 500, "🥇", Color(0xFFF59E0B)),
  PLATINO("Platino", 750, "💠", Color(0xFF22D3EE)),
  ESMERALDA("Esmeralda", 1000, "🟢", Color(0xFF17C97A)),
  DIAMANTE("Diamante", 1250, "💎", Color(0xFF60A5FA)),
  MAESTRO("Maestro", 1500, "👑", Color(0xFFA855F7));

  companion object {
    const val DIVISION_SIZE = 50
    fun fromRating(rating: Int): RankTier = entries.lastOrNull { rating >= it.minRating } ?: BRONCE
  }
}

data class GameRankInfo(
  val gameId: String,
  val rating: Int
) {
  val tier: RankTier get() = RankTier.fromRating(rating)
  private val intoTier: Int get() = rating - tier.minRating
  /** 1 (a punto de ascender) a 5 (recién ascendido) — como en un ladder competitivo.
   * Maestro no tiene divisiones, sigue subiendo sin techo (mismo criterio que
   * MasteryTier/masteryStreak: el ladder nunca "se llena y queda quieto"). */
  val division: Int? get() =
    if (tier == RankTier.MAESTRO) null
    else (5 - (intoTier / RankTier.DIVISION_SIZE)).coerceIn(1, 5)
  val label: String get() =
    if (tier == RankTier.MAESTRO) "${tier.tierName} · $rating" else "${tier.tierName} $division"
  val progressInDivision: Float get() =
    (intoTier % RankTier.DIVISION_SIZE) / RankTier.DIVISION_SIZE.toFloat()
}

data class UserSettings(
  val id: Long = 1L,
  val name: String = "Ana",
  val avatar: String = "🧠",
  val isActive: Boolean = true,
  val weeklyGoal: Int = 4, // days per week
  val defaultTimed: Boolean = false, // false: Precisión, true: Reto
  val soundEnabled: Boolean = true,
  val hapticsEnabled: Boolean = true,
  val notificationsEnabled: Boolean = true,
  val reminderHour: Int = 19, // 19:00 (7 PM default)
  val reminderMinute: Int = 0,
  // Difficulty and cognitive customization
  val difficultyMode: DifficultyMode = DifficultyMode.ADAPTIVE,
  val difficultyMemoria: Int = 2,
  val difficultyAtencion: Int = 2,
  val difficultyRazonamiento: Int = 2,
  val difficultyLenguaje: Int = 2,
  val difficultyCalculo: Int = 2,
  val difficultyVelocidad: Int = 2,
  val cognitiveAssistance: Boolean = true,
  val timeScaleFactor: Float = 1.0f,
  val themeMode: ThemeMode = ThemeMode.SYSTEM,
  val language: AppLanguage = AppLanguage.SPANISH,
  /** `null` = todavía no completó el onboarding de edad -- ver [AgeBand]. */
  val ageBand: AgeBand? = null
)
