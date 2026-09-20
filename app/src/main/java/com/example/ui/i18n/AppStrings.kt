package com.example.ui.i18n

import androidx.compose.runtime.Composable
import androidx.compose.runtime.compositionLocalOf
import com.example.model.AppLanguage
import com.example.model.DomainType

val LocalAppLanguage = compositionLocalOf { AppLanguage.SPANISH }

val strings: Translations
  @Composable
  get() = getTranslations(LocalAppLanguage.current)

data class Translations(
  val tabToday: String,
  val tabGames: String,
  val tabProgress: String,
  val tabSettings: String,

  // Home screen
  val streakLabel: String,
  val daySingle: String,
  val dayPlural: String,
  val dailySessionTitle: String,
  val dailySessionDesc: String,
  val sessionCompletedTitle: String,
  val startDailySession: String,
  val continueDailySession: String,
  val trainAnotherRound: String,
  val greetingFormat: (String) -> String,

  // Games Library screen
  val gamesLibraryTitle: String,
  val gamesLibrarySubtitle: String,
  val filterAll: String,
  val levelPrefix: String,
  val scoreLabel: String,
  val playButton: String,

  // Progress screen
  val progressTitle: String,
  val progressSubtitle: String,
  val domainMasteryTitle: String,
  val xpLabel: String,
  val gameRankingsTitle: String,
  val gameRankingsSubtitle: String,
  val recentHistoryTitle: String,

  // Settings screen
  val settingsTitle: String,
  val languageCardTitle: String,
  val languageCardSubtitle: String
)

private val spanishTranslations = Translations(
  tabToday = "Hoy",
  tabGames = "Juegos",
  tabProgress = "Progreso",
  tabSettings = "Ajustes",
  streakLabel = "Racha",
  daySingle = "día",
  dayPlural = "días",
  dailySessionTitle = "Tu sesión de hoy",
  dailySessionDesc = "3 juegos seleccionados según tus prioridades",
  sessionCompletedTitle = "¡Excelente! Has completado tu rutina de hoy",
  startDailySession = "Empezar sesión",
  continueDailySession = "Continuar sesión",
  trainAnotherRound = "Entrenar otra ronda",
  greetingFormat = { name -> "Hola, $name 👋" },
  gamesLibraryTitle = "Biblioteca de Juegos",
  gamesLibrarySubtitle = "9 entrenamientos en 6 dominios cognitivos",
  filterAll = "Todos",
  levelPrefix = "Nivel",
  scoreLabel = "Mejor",
  playButton = "Jugar",
  progressTitle = "Tu Progreso Cognitivo",
  progressSubtitle = "Evolución integral en las 6 áreas cognitivas",
  domainMasteryTitle = "Maestría por Dominio",
  xpLabel = "XP sin límite",
  gameRankingsTitle = "Ranking por Juego",
  gameRankingsSubtitle = "Sube y baja según cómo juegues",
  recentHistoryTitle = "Historial Reciente",
  settingsTitle = "Ajustes y Preferencias",
  languageCardTitle = "Idioma de la Aplicación",
  languageCardSubtitle = "Selecciona tu idioma preferido"
)

private val englishTranslations = Translations(
  tabToday = "Today",
  tabGames = "Games",
  tabProgress = "Progress",
  tabSettings = "Settings",
  streakLabel = "Streak",
  daySingle = "day",
  dayPlural = "days",
  dailySessionTitle = "Today's Session",
  dailySessionDesc = "3 games chosen according to your priorities",
  sessionCompletedTitle = "Great! You completed today's workout",
  startDailySession = "Start Session",
  continueDailySession = "Continue Session",
  trainAnotherRound = "Train Another Round",
  greetingFormat = { name -> "Hello, $name 👋" },
  gamesLibraryTitle = "Games Library",
  gamesLibrarySubtitle = "9 brain workouts across 6 cognitive domains",
  filterAll = "All",
  levelPrefix = "Level",
  scoreLabel = "Best",
  playButton = "Play",
  progressTitle = "Your Cognitive Progress",
  progressSubtitle = "Comprehensive growth across 6 brain areas",
  domainMasteryTitle = "Domain Mastery",
  xpLabel = "Unlimited XP",
  gameRankingsTitle = "Game Rankings",
  gameRankingsSubtitle = "Adjusts based on your performance",
  recentHistoryTitle = "Recent History",
  settingsTitle = "Settings & Preferences",
  languageCardTitle = "App Language",
  languageCardSubtitle = "Choose your preferred language"
)

private val frenchTranslations = Translations(
  tabToday = "Aujourd'hui",
  tabGames = "Jeux",
  tabProgress = "Progrès",
  tabSettings = "Paramètres",
  streakLabel = "Série",
  daySingle = "jour",
  dayPlural = "jours",
  dailySessionTitle = "Session du jour",
  dailySessionDesc = "3 jeux ciblés selon vos priorités",
  sessionCompletedTitle = "Excellent ! Entraînement du jour terminé",
  startDailySession = "Démarrer la session",
  continueDailySession = "Continuer la session",
  trainAnotherRound = "Nouvelle session",
  greetingFormat = { name -> "Bonjour, $name 👋" },
  gamesLibraryTitle = "Bibliothèque de Jeux",
  gamesLibrarySubtitle = "9 entraînements dans 6 domaines cognitifs",
  filterAll = "Tous",
  levelPrefix = "Niveau",
  scoreLabel = "Meilleur",
  playButton = "Jouer",
  progressTitle = "Votre Progrès Cognitif",
  progressSubtitle = "Évolution complète sur les 6 domaines",
  domainMasteryTitle = "Maîtrise par Domaine",
  xpLabel = "XP illimité",
  gameRankingsTitle = "Classement par Jeu",
  gameRankingsSubtitle = "Évolue selon vos performances",
  recentHistoryTitle = "Historique Récent",
  settingsTitle = "Paramètres et Préférences",
  languageCardTitle = "Langue de l'application",
  languageCardSubtitle = "Choisissez votre langue préférée"
)

private val germanTranslations = Translations(
  tabToday = "Heute",
  tabGames = "Spiele",
  tabProgress = "Fortschritt",
  tabSettings = "Einstellungen",
  streakLabel = "Serie",
  daySingle = "Tag",
  dayPlural = "Tage",
  dailySessionTitle = "Heutige Trainingseinheit",
  dailySessionDesc = "3 personalisierte Spiele für deine Ziele",
  sessionCompletedTitle = "Super! Heutiges Training abgeschlossen",
  startDailySession = "Training starten",
  continueDailySession = "Training fortsetzen",
  trainAnotherRound = "Weitere Runde",
  greetingFormat = { name -> "Hallo, $name 👋" },
  gamesLibraryTitle = "Spiele-Bibliothek",
  gamesLibrarySubtitle = "9 Übungen in 6 kognitiven Bereichen",
  filterAll = "Alle",
  levelPrefix = "Level",
  scoreLabel = "Rekord",
  playButton = "Spielen",
  progressTitle = "Kognitiver Fortschritt",
  progressSubtitle = "Ganzheitliche Entwicklung über 6 Bereiche",
  domainMasteryTitle = "Bereichs-Meisterschaft",
  xpLabel = "Unbegrenzte XP",
  gameRankingsTitle = "Spiel-Rangliste",
  gameRankingsSubtitle = "Passt sich deiner Leistung an",
  recentHistoryTitle = "Verlauf",
  settingsTitle = "Einstellungen",
  languageCardTitle = "App-Sprache",
  languageCardSubtitle = "Wähle deine bevorzugte Sprache"
)

private val portugueseTranslations = Translations(
  tabToday = "Hoje",
  tabGames = "Jogos",
  tabProgress = "Progresso",
  tabSettings = "Ajustes",
  streakLabel = "Sequência",
  daySingle = "dia",
  dayPlural = "dias",
  dailySessionTitle = "Sessão de Hoje",
  dailySessionDesc = "3 jogos escolhidos segundo suas prioridades",
  sessionCompletedTitle = "Excelente! Treino de hoje concluído",
  startDailySession = "Começar Sessão",
  continueDailySession = "Continuar Sessão",
  trainAnotherRound = "Mais Uma Rodada",
  greetingFormat = { name -> "Olá, $name 👋" },
  gamesLibraryTitle = "Biblioteca de Jogos",
  gamesLibrarySubtitle = "9 treinos em 6 domínios cognitivos",
  filterAll = "Todos",
  levelPrefix = "Nível",
  scoreLabel = "Melhor",
  playButton = "Jogar",
  progressTitle = "Seu Progresso Cognitivo",
  progressSubtitle = "Evolução integral nas 6 áreas cognitivas",
  domainMasteryTitle = "Domínio Cognitivo",
  xpLabel = "XP sem limite",
  gameRankingsTitle = "Ranking por Jogo",
  gameRankingsSubtitle = "Ajusta conforme seu desempenho",
  recentHistoryTitle = "Histórico Recente",
  settingsTitle = "Ajustes e Preferências",
  languageCardTitle = "Idioma do Aplicativo",
  languageCardSubtitle = "Selecione seu idioma de preferência"
)

fun getTranslations(lang: AppLanguage): Translations = when (lang) {
  AppLanguage.SPANISH -> spanishTranslations
  AppLanguage.ENGLISH -> englishTranslations
  AppLanguage.FRENCH -> frenchTranslations
  AppLanguage.GERMAN -> germanTranslations
  AppLanguage.PORTUGUESE -> portugueseTranslations
}

fun getDomainName(domain: DomainType, lang: AppLanguage): String = when (lang) {
  AppLanguage.SPANISH -> domain.displayName
  AppLanguage.ENGLISH -> when (domain) {
    DomainType.MEMORIA -> "Memory"
    DomainType.ATENCION -> "Attention"
    DomainType.RAZONAMIENTO -> "Reasoning"
    DomainType.LENGUAJE -> "Language"
    DomainType.CALCULO -> "Calculation"
    DomainType.VELOCIDAD -> "Speed"
  }
  AppLanguage.FRENCH -> when (domain) {
    DomainType.MEMORIA -> "Mémoire"
    DomainType.ATENCION -> "Attention"
    DomainType.RAZONAMIENTO -> "Raisonnement"
    DomainType.LENGUAJE -> "Langage"
    DomainType.CALCULO -> "Calcul"
    DomainType.VELOCIDAD -> "Vitesse"
  }
  AppLanguage.GERMAN -> when (domain) {
    DomainType.MEMORIA -> "Gedächtnis"
    DomainType.ATENCION -> "Aufmerksamkeit"
    DomainType.RAZONAMIENTO -> "Logik"
    DomainType.LENGUAJE -> "Sprache"
    DomainType.CALCULO -> "Rechnen"
    DomainType.VELOCIDAD -> "Geschwindigkeit"
  }
  AppLanguage.PORTUGUESE -> when (domain) {
    DomainType.MEMORIA -> "Memória"
    DomainType.ATENCION -> "Atenção"
    DomainType.RAZONAMIENTO -> "Raciocínio"
    DomainType.LENGUAJE -> "Linguagem"
    DomainType.CALCULO -> "Cálculo"
    DomainType.VELOCIDAD -> "Velocidade"
  }
}

fun getGameTitle(gameId: String, lang: AppLanguage, defaultTitle: String): String {
  if (lang == AppLanguage.SPANISH) return defaultTitle
  return when (gameId) {
    "mem_secuencias" -> when (lang) {
      AppLanguage.ENGLISH -> "Sequences"
      AppLanguage.FRENCH -> "Séquences"
      AppLanguage.GERMAN -> "Sequenzen"
      AppLanguage.PORTUGUESE -> "Sequências"
      else -> defaultTitle
    }
    "mem_parejas" -> when (lang) {
      AppLanguage.ENGLISH -> "Card Pairs"
      AppLanguage.FRENCH -> "Paires de Cartes"
      AppLanguage.GERMAN -> "Kartenpaare"
      AppLanguage.PORTUGUESE -> "Pares de Cartas"
      else -> defaultTitle
    }
    "atn_stroop" -> when (lang) {
      AppLanguage.ENGLISH -> "Stroop Focus"
      AppLanguage.FRENCH -> "Focus Stroop"
      AppLanguage.GERMAN -> "Stroop Fokus"
      AppLanguage.PORTUGUESE -> "Foco Stroop"
      else -> defaultTitle
    }
    "atn_intruso" -> when (lang) {
      AppLanguage.ENGLISH -> "Find the Intruder"
      AppLanguage.FRENCH -> "Trouve l'Intrus"
      AppLanguage.GERMAN -> "Finde den Eindringling"
      AppLanguage.PORTUGUESE -> "Encontre o Intruso"
      else -> defaultTitle
    }
    "raz_patrones" -> when (lang) {
      AppLanguage.ENGLISH -> "Visual Patterns"
      AppLanguage.FRENCH -> "Motifs Visuels"
      AppLanguage.GERMAN -> "Visuelle Muster"
      AppLanguage.PORTUGUESE -> "Padrões Visuais"
      else -> defaultTitle
    }
    "raz_anagramas" -> when (lang) {
      AppLanguage.ENGLISH -> "Anagrams"
      AppLanguage.FRENCH -> "Anagrammes"
      AppLanguage.GERMAN -> "Anagramme"
      AppLanguage.PORTUGUESE -> "Anagramas"
      else -> defaultTitle
    }
    "len_pasapalabra" -> when (lang) {
      AppLanguage.ENGLISH -> "Alphabet Rosette"
      AppLanguage.FRENCH -> "Le Rosier des Mots"
      AppLanguage.GERMAN -> "Alphabet-Rad"
      AppLanguage.PORTUGUESE -> "Roda de Letras"
      else -> defaultTitle
    }
    "cal_mental" -> when (lang) {
      AppLanguage.ENGLISH -> "Mental Math"
      AppLanguage.FRENCH -> "Calcul Mental"
      AppLanguage.GERMAN -> "Kopfrechnen"
      AppLanguage.PORTUGUESE -> "Cálculo Mental"
      else -> defaultTitle
    }
    "vel_reaccion" -> when (lang) {
      AppLanguage.ENGLISH -> "Flash Reaction"
      AppLanguage.FRENCH -> "Réaction Éclair"
      AppLanguage.GERMAN -> "Blitz-Reaktion"
      AppLanguage.PORTUGUESE -> "Reação Relâmpago"
      else -> defaultTitle
    }
    else -> defaultTitle
  }
}
