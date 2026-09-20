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
  val timestamp: Long = System.currentTimeMillis()
)

data class DailySessionState(
  val dateKey: String,
  val gameIds: List<String>,
  val completedCount: Int = 0,
  val scores: List<Int> = emptyList()
)

data class AchievementItem(
  val id: String,
  val title: String,
  val description: String,
  val iconEmoji: String,
  val isUnlocked: Boolean = false
)

enum class DifficultyMode(val label: String, val description: String) {
  ADAPTIVE("Auto-adaptativa", "Ajuste dinámico según tu desempeño en cada sesión"),
  PRINCIPIANTE("Principiante", "Nivel 1 con tiempos generosos y estímulos claros"),
  INTERMEDIO("Intermedio", "Nivel 3 equilibrado para mantener agilidad mental"),
  AVANZADO("Avanzado", "Nivel 5 con máxima exigencia cognitiva y velocidad"),
  CUSTOM("Personalizada", "Nivel asignado a medida por cada dominio cognitivo")
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
  val timeScaleFactor: Float = 1.0f
)
