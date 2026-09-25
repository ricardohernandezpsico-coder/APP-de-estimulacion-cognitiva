package com.example.notification

/** Lo que sabe el recordatorio sobre el día del usuario (se arma en [CognitiveReminderWorker]). */
data class ReminderInput(
  val name: String,
  /** Días seguidos con partidas terminando AYER (si hoy juega, la racha pasa a este número + 1). */
  val streakUntilYesterday: Int,
  val playedToday: Boolean,
  /** Juegos de la sesión de hoy ya completados (0..3). */
  val completedToday: Int,
  /** Días con partidas esta semana (lunes a hoy) y la meta elegida en el onboarding / Ajustes. */
  val daysThisWeek: Int,
  val weeklyGoal: Int,
  /** Próximo juego del camino de hoy, si se sabe. */
  val nextGameTitle: String?,
  /** Días desde la última partida (null = nunca jugó). */
  val daysSinceLastPlay: Int?,
  /** Para variar el texto día a día sin azar (así se puede probar). */
  val dayOfYear: Int
)

data class ReminderMessage(val title: String, val text: String)

private val StreakMilestones = listOf(3, 7, 14, 30, 50, 100, 200, 365)

/**
 * Texto del recordatorio diario: personal, corto y sin culpa ni promesas de salud (nada de "cuida tu reserva
 * cognitiva"). Devuelve null si no hay que avisar (ya completó la sesión de hoy: no se insiste).
 * Prioridad: sesión a medias > racha en juego > volver después de varios días > meta semanal > invitación.
 */
fun buildReminder(input: ReminderInput): ReminderMessage? {
  if (input.completedToday >= 3) return null
  val name = input.name.trim().ifEmpty { null }
  val hi = name?.let { "$it, " } ?: ""
  val weekLine = if (input.weeklyGoal > 0 && input.daysThisWeek < input.weeklyGoal)
    " Llevas ${input.daysThisWeek} de ${input.weeklyGoal} días esta semana." else ""

  // 1) Empezó hoy y le falta poco
  if (input.playedToday && input.completedToday in 1..2) {
    val left = 3 - input.completedToday
    return ReminderMessage(
      title = "${hi}ya casi".replaceFirstChar { it.uppercase() },
      text = "Te ${if (left == 1) "falta 1 juego" else "faltan $left juegos"} para completar tu camino de hoy." +
        (input.nextGameTitle?.let { " Sigue $it." } ?: "")
    )
  }
  if (input.playedToday) return null // jugó hoy fuera de la sesión: no molestar

  // 2) Racha en juego
  val streak = input.streakUntilYesterday
  if (streak >= 2) {
    val next = streak + 1
    val milestone = next in StreakMilestones
    return ReminderMessage(
      title = if (milestone) "Hoy llegas a $next días seguidos" else "${hi}tu racha de $streak días te espera".replaceFirstChar { it.uppercase() },
      text = (if (milestone) "Juega hoy y consigues la medalla de $next días." else "Juega hoy y sube a $next días. Son unos 5 minutos.") + weekLine
    )
  }

  // 3) Vuelve después de varios días (sin reproches)
  val gap = input.daysSinceLastPlay
  if (gap != null && gap >= 3) {
    return ReminderMessage(
      title = "${hi}tu camino sigue aquí".replaceFirstChar { it.uppercase() },
      text = "Retoma cuando quieras: 3 juegos cortos y vuelves a sumar trofeos." +
        (input.nextGameTitle?.let { " Hoy empieza con $it." } ?: "")
    )
  }

  // 4) Invitación del día (varía según el día)
  val next = input.nextGameTitle
  val options = buildList {
    add(ReminderMessage("${hi}tu camino de hoy está listo".replaceFirstChar { it.uppercase() }, "3 juegos, unos 5 minutos.$weekLine"))
    if (next != null) add(ReminderMessage("Hoy toca $next", "¿Te animas? Es el primero de tu camino de hoy.$weekLine"))
    add(ReminderMessage("Un rato corto hoy", "Cada partida suma trofeos para tu liga.$weekLine"))
  }
  return options[input.dayOfYear.mod(options.size)]
}
