package com.example.ui.i18n

import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.runtime.staticCompositionLocalOf
import com.example.model.AppLanguage
import com.example.model.DomainType

val LocalAppLanguage = staticCompositionLocalOf { AppLanguage.SPANISH }

data class StringsDefinition(
  // Bottom navigation tabs
  val tabToday: String,
  val tabGames: String,
  val tabProgress: String,
  val tabSettings: String,

  // Home Screen
  val greeting: (name: String) -> String,
  val daySingle: String,
  val dayPlural: String,
  val streakLabel: String,
  val dailySessionTitle: String,
  val dailySessionDesc: String,
  val startDailySession: String,
  val continueDailySession: String,
  val sessionCompletedTitle: String,
  val sessionCompletedDesc: String,
  val quickStatsTitle: String,
  val quickStatsCompleted: String,
  val quickStatsAvgScore: String,
  val weeklyChallengesTitle: String,
  val viewAllGames: String,

  // Games Library
  val gamesLibraryTitle: String,
  val gamesLibrarySubtitle: String,
  val filterAll: String,
  val playButton: String,
  val levelPrefix: String,
  val rulesTitle: String,
  val instructionsTitle: String,
  val startExercise: String,
  val closeButton: String,

  // Progress Screen
  val progressTitle: String,
  val progressSubtitle: String,
  val domainMasteryTitle: String,
  val domainMasterySubtitle: String,
  val gameRankingsTitle: String,
  val gameRankingsSubtitle: String,
  val performanceCurveTitle: String,
  val recentHistoryTitle: String,
  val noGamesPlayedYet: String,
  val pointsShort: String,

  // Settings Screen
  val settingsTitle: String,
  val settingsSubtitle: String,
  val languageCardTitle: String,
  val languageCardSubtitle: String,
  val profilesSectionTitle: String,
  val activeStatus: String,
  val newProfileButton: String,
  val editProfileTitle: String,
  val avatarLabel: String,
  val nameLabel: String,
  val saveProfileButton: String,
  val weeklyGoalTitle: String,
  val daysPerWeek: (days: Int) -> String,
  val cognitiveDifficultyTitle: String,
  val difficultyModeLabel: String,
  val cognitiveAssistanceTitle: String,
  val cognitiveAssistanceDesc: String,
  val tempoTitle: String,
  val saveDifficultyButton: String,
  val gameExperienceTitle: String,
  val timedModeTitle: String,
  val timedModeDescOn: String,
  val timedModeDescOff: String,
  val themeTitle: String,
  val audioHapticsTitle: String,
  val soundLabel: String,
  val hapticsLabel: String,
  val notificationsTitle: String,
  val resetDataTitle: String,
  val resetDataDesc: String,
  val resetDataButton: String,

  // Game UI & Common
  val quitGameConfirm: String,
  val scoreLabel: String,
  val correctLabel: String,
  val resultsTitle: String,
  val nextGame: String,
  val retryGame: String,
  val finishSummary: String,
  val reactionSpeed: String
)

object Translations {
  val Spanish = StringsDefinition(
    tabToday = "Hoy",
    tabGames = "Juegos",
    tabProgress = "Progreso",
    tabSettings = "Ajustes",

    greeting = { name -> "Hola, $name 👋" },
    daySingle = "día",
    dayPlural = "días",
    streakLabel = "Racha",
    dailySessionTitle = "Tu Sesión Diaria",
    dailySessionDesc = "3 ejercicios calibrados para estimular tus dominios clave.",
    startDailySession = "Comenzar Sesión",
    continueDailySession = "Continuar Sesión",
    sessionCompletedTitle = "¡Sesión Completada!",
    sessionCompletedDesc = "Has completado tu rutina de estimulación de hoy. ¡Excelente trabajo!",
    quickStatsTitle = "Resumen Rápido",
    quickStatsCompleted = "Completados",
    quickStatsAvgScore = "Promedio",
    weeklyChallengesTitle = "Desafíos Semanales",
    viewAllGames = "Ver todos los juegos",

    gamesLibraryTitle = "Gimnasio Cerebral",
    gamesLibrarySubtitle = "9 juegos diseñados para ejercitar tus capacidades cognitivas",
    filterAll = "Todos",
    playButton = "Jugar",
    levelPrefix = "Nivel",
    rulesTitle = "¿Cómo se juega?",
    instructionsTitle = "Instrucciones",
    startExercise = "Iniciar Ejercicio",
    closeButton = "Cerrar",

    progressTitle = "Tu Progreso Cognitivo",
    progressSubtitle = "Evolución integral en las 6 áreas cognitivas",
    domainMasteryTitle = "Maestría por Dominio",
    domainMasterySubtitle = "XP acumulada en cada área cognitiva (sin límite de nivel)",
    gameRankingsTitle = "Ranking por Juego",
    gameRankingsSubtitle = "Tu nivel competitivo actual en cada uno de los 9 juegos",
    performanceCurveTitle = "Curva de Rendimiento",
    recentHistoryTitle = "Historial Reciente",
    noGamesPlayedYet = "Aún no has jugado partidas. ¡Comienza una sesión hoy!",
    pointsShort = "pts",

    settingsTitle = "Configuración",
    settingsSubtitle = "Personaliza tu experiencia de entrenamiento cerebral",
    languageCardTitle = "Idioma de la Aplicación",
    languageCardSubtitle = "Selecciona tu idioma preferido para textos y ejercicios",
    profilesSectionTitle = "Perfiles de Usuario",
    activeStatus = "Activo",
    newProfileButton = "Nuevo Perfil",
    editProfileTitle = "Editar Perfil Activo",
    avatarLabel = "Avatar del perfil:",
    nameLabel = "Nombre",
    saveProfileButton = "Guardar Perfil",
    weeklyGoalTitle = "Meta Semanal de Entrenamiento",
    daysPerWeek = { days -> "$days días por semana" },
    cognitiveDifficultyTitle = "Dificultad Cognitiva",
    difficultyModeLabel = "Modo de dificultad:",
    cognitiveAssistanceTitle = "Asistencia cognitiva y pistas",
    cognitiveAssistanceDesc = "Muestra pistas visuales y simplificaciones cuando un ejercicio resulta complejo.",
    tempoTitle = "Ritmo de tiempo en ejercicios:",
    saveDifficultyButton = "Guardar Preferencias de Dificultad",
    gameExperienceTitle = "Experiencia de Juego",
    timedModeTitle = "Modo contra el reloj (Reto)",
    timedModeDescOn = "Partidas con temporizador",
    timedModeDescOff = "Modo Precisión (sin reloj, sin prisa)",
    themeTitle = "Tema de la app",
    audioHapticsTitle = "Sonido y Vibración",
    soundLabel = "Efectos de sonido",
    hapticsLabel = "Respuesta háptica (vibración)",
    notificationsTitle = "Recordatorios Diarios",
    resetDataTitle = "Reiniciar Progreso",
    resetDataDesc = "Borra el historial de partidas, niveles y progreso.",
    resetDataButton = "Restablecer Datos",

    quitGameConfirm = "¿Deseas salir del juego?",
    scoreLabel = "Puntaje",
    correctLabel = "Aciertos",
    resultsTitle = "Resultados de la Partida",
    nextGame = "Siguiente Ejercicio",
    retryGame = "Reintentar",
    finishSummary = "Volver al Inicio",
    reactionSpeed = "Tiempo de reacción promedio"
  )

  val English = StringsDefinition(
    tabToday = "Today",
    tabGames = "Games",
    tabProgress = "Progress",
    tabSettings = "Settings",

    greeting = { name -> "Hello, $name 👋" },
    daySingle = "day",
    dayPlural = "days",
    streakLabel = "Streak",
    dailySessionTitle = "Your Daily Session",
    dailySessionDesc = "3 calibrated exercises to stimulate your key domains.",
    startDailySession = "Start Session",
    continueDailySession = "Continue Session",
    sessionCompletedTitle = "Session Completed!",
    sessionCompletedDesc = "You have finished your stimulation routine for today. Great job!",
    quickStatsTitle = "Quick Overview",
    quickStatsCompleted = "Completed",
    quickStatsAvgScore = "Average",
    weeklyChallengesTitle = "Weekly Challenges",
    viewAllGames = "View all games",

    gamesLibraryTitle = "Brain Gym",
    gamesLibrarySubtitle = "9 scientifically inspired games to train your brain",
    filterAll = "All",
    playButton = "Play",
    levelPrefix = "Level",
    rulesTitle = "How to play?",
    instructionsTitle = "Instructions",
    startExercise = "Start Exercise",
    closeButton = "Close",

    progressTitle = "Cognitive Progress",
    progressSubtitle = "Overall evolution across all 6 cognitive domains",
    domainMasteryTitle = "Domain Mastery",
    domainMasterySubtitle = "Cumulative XP across cognitive areas (uncapped)",
    gameRankingsTitle = "Game Rankings",
    gameRankingsSubtitle = "Your competitive rating tier in each of the 9 games",
    performanceCurveTitle = "Performance Trend",
    recentHistoryTitle = "Recent History",
    noGamesPlayedYet = "No games played yet. Start your first session today!",
    pointsShort = "pts",

    settingsTitle = "Settings",
    settingsSubtitle = "Customize your cognitive workout experience",
    languageCardTitle = "App Language",
    languageCardSubtitle = "Choose your preferred language for text and verbal exercises",
    profilesSectionTitle = "User Profiles",
    activeStatus = "Active",
    newProfileButton = "New Profile",
    editProfileTitle = "Edit Active Profile",
    avatarLabel = "Profile avatar:",
    nameLabel = "Name",
    saveProfileButton = "Save Profile",
    weeklyGoalTitle = "Weekly Training Goal",
    daysPerWeek = { days -> "$days days per week" },
    cognitiveDifficultyTitle = "Cognitive Difficulty",
    difficultyModeLabel = "Difficulty mode:",
    cognitiveAssistanceTitle = "Cognitive assistance & hints",
    cognitiveAssistanceDesc = "Shows visual hints and simplified options when an exercise gets tough.",
    tempoTitle = "Pacing in exercises:",
    saveDifficultyButton = "Save Difficulty Preferences",
    gameExperienceTitle = "Game Experience",
    timedModeTitle = "Time Challenge Mode",
    timedModeDescOn = "Games with a countdown timer",
    timedModeDescOff = "Precision Mode (no timer, stress-free)",
    themeTitle = "App Theme",
    audioHapticsTitle = "Sound & Haptics",
    soundLabel = "Sound effects",
    hapticsLabel = "Haptic feedback",
    notificationsTitle = "Daily Reminders",
    resetDataTitle = "Reset Progress",
    resetDataDesc = "Clears all match history, levels and rankings.",
    resetDataButton = "Reset All Data",

    quitGameConfirm = "Do you want to quit the game?",
    scoreLabel = "Score",
    correctLabel = "Correct",
    resultsTitle = "Game Results",
    nextGame = "Next Exercise",
    retryGame = "Retry",
    finishSummary = "Back to Home",
    reactionSpeed = "Average reaction time"
  )

  val French = StringsDefinition(
    tabToday = "Aujourd'hui",
    tabGames = "Jeux",
    tabProgress = "Progrès",
    tabSettings = "Paramètres",

    greeting = { name -> "Bonjour, $name 👋" },
    daySingle = "jour",
    dayPlural = "jours",
    streakLabel = "Série",
    dailySessionTitle = "Votre Session Quotidienne",
    dailySessionDesc = "3 exercices calibrés pour stimuler vos domaines clés.",
    startDailySession = "Commencer la session",
    continueDailySession = "Continuer la session",
    sessionCompletedTitle = "Session Terminée !",
    sessionCompletedDesc = "Vous avez accompli votre entraînement du jour. Excellent travail !",
    quickStatsTitle = "Aperçu Rapide",
    quickStatsCompleted = "Terminés",
    quickStatsAvgScore = "Moyenne",
    weeklyChallengesTitle = "Défis Hebdomadaires",
    viewAllGames = "Voir tous les jeux",

    gamesLibraryTitle = "Gymnastique Cérébrale",
    gamesLibrarySubtitle = "9 jeux stimulants conçus pour entraîner votre esprit",
    filterAll = "Tous",
    playButton = "Jouer",
    levelPrefix = "Niveau",
    rulesTitle = "Comment jouer ?",
    instructionsTitle = "Instructions",
    startExercise = "Démarrer l'exercice",
    closeButton = "Fermer",

    progressTitle = "Progrès Cognitifs",
    progressSubtitle = "Évolution globale dans les 6 domaines cognitifs",
    domainMasteryTitle = "Maîtrise du Domaine",
    domainMasterySubtitle = "XP accumulée par domaine (progression continue)",
    gameRankingsTitle = "Classement par Jeu",
    gameRankingsSubtitle = "Votre rang actuel dans chacun des 9 jeux",
    performanceCurveTitle = "Courbe de Performance",
    recentHistoryTitle = "Historique Récent",
    noGamesPlayedYet = "Aucune partie jouée pour le moment. Commencez dès aujourd'hui !",
    pointsShort = "pts",

    settingsTitle = "Paramètres",
    settingsSubtitle = "Personnalisez votre expérience d'entraînement",
    languageCardTitle = "Langue de l'Application",
    languageCardSubtitle = "Choisissez votre langue préférée pour les textes et exercices",
    profilesSectionTitle = "Profils Utilisateur",
    activeStatus = "Actif",
    newProfileButton = "Nouveau Profil",
    editProfileTitle = "Modifier le Profil Actif",
    avatarLabel = "Avatar du profil :",
    nameLabel = "Nom",
    saveProfileButton = "Enregistrer le Profil",
    weeklyGoalTitle = "Objectif Hebdomadaire",
    daysPerWeek = { days -> "$days jours par semaine" },
    cognitiveDifficultyTitle = "Difficulté Cognitive",
    difficultyModeLabel = "Mode de difficulté :",
    cognitiveAssistanceTitle = "Assistance cognitive & indices",
    cognitiveAssistanceDesc = "Affiche des indices visuels quand un exercice devient exigeant.",
    tempoTitle = "Rythme des exercices :",
    saveDifficultyButton = "Enregistrer la Difficulté",
    gameExperienceTitle = "Expérience de Jeu",
    timedModeTitle = "Mode Contre-la-Montre",
    timedModeDescOn = "Parties avec chronomètre",
    timedModeDescOff = "Mode Précision (sans chrono, sans stress)",
    themeTitle = "Thème de l'application",
    audioHapticsTitle = "Son et Vibrations",
    soundLabel = "Effets sonores",
    hapticsLabel = "Retour haptique",
    notificationsTitle = "Rappels Quotidiens",
    resetDataTitle = "Réinitialiser les Progrès",
    resetDataDesc = "Efface l'historique des parties et les classements.",
    resetDataButton = "Réinitialiser les Données",

    quitGameConfirm = "Voulez-vous quitter la partie ?",
    scoreLabel = "Score",
    correctLabel = "Réussis",
    resultsTitle = "Résultats de la Partie",
    nextGame = "Exercice Suivant",
    retryGame = "Recommencer",
    finishSummary = "Retour à l'Accueil",
    reactionSpeed = "Temps de réaction moyen"
  )

  val German = StringsDefinition(
    tabToday = "Heute",
    tabGames = "Spiele",
    tabProgress = "Fortschritt",
    tabSettings = "Optionen",

    greeting = { name -> "Hallo, $name 👋" },
    daySingle = "Tag",
    dayPlural = "Tage",
    streakLabel = "Serie",
    dailySessionTitle = "Deine Tägliche Einheit",
    dailySessionDesc = "3 abgestimmte Übungen zur gezielten kognitiven Förderung.",
    startDailySession = "Einheit Starten",
    continueDailySession = "Einheit Fortsetzen",
    sessionCompletedTitle = "Einheit Abgeschlossen!",
    sessionCompletedDesc = "Du hast dein heutiges Gehirntraining gemeistert. Hervorragend!",
    quickStatsTitle = "Schnellübersicht",
    quickStatsCompleted = "Abgeschlossen",
    quickStatsAvgScore = "Durchschnitt",
    weeklyChallengesTitle = "Wöchentliche Herausforderungen",
    viewAllGames = "Alle Spiele ansehen",

    gamesLibraryTitle = "Gehirn-Fitness",
    gamesLibrarySubtitle = "9 wissenschaftlich inspirierte Spiele für mentale Stärke",
    filterAll = "Alle",
    playButton = "Spielen",
    levelPrefix = "Stufe",
    rulesTitle = "Spielanleitung",
    instructionsTitle = "Anweisungen",
    startExercise = "Übung Beginnen",
    closeButton = "Schließen",

    progressTitle = "Kognitiver Fortschritt",
    progressSubtitle = "Umfassende Entwicklung in allen 6 kognitiven Bereichen",
    domainMasteryTitle = "Bereichs-Meisterschaft",
    domainMasterySubtitle = "Gesammelte XP in jedem Bereich (ohne Obergrenze)",
    gameRankingsTitle = "Rangliste nach Spiel",
    gameRankingsSubtitle = "Deine aktuelle Wettkampfstufe in jedem der 9 Spiele",
    performanceCurveTitle = "Leistungskurve",
    recentHistoryTitle = "Verlauf",
    noGamesPlayedYet = "Noch keine Spiele absolviert. Starte noch heute!",
    pointsShort = "Pkt",

    settingsTitle = "Einstellungen",
    settingsSubtitle = "Passe dein persönliches Trainingserlebnis an",
    languageCardTitle = "Sprache der App",
    languageCardSubtitle = "Wähle deine bevorzugte Sprache für Texte und Übungen",
    profilesSectionTitle = "Benutzerprofile",
    activeStatus = "Aktiv",
    newProfileButton = "Neues Profil",
    editProfileTitle = "Aktives Profil Bearbeiten",
    avatarLabel = "Profil-Avatar:",
    nameLabel = "Name",
    saveProfileButton = "Profil Speichern",
    weeklyGoalTitle = "Wöchentliches Trainingsziel",
    daysPerWeek = { days -> "$days Tage pro Woche" },
    cognitiveDifficultyTitle = "Kognitiver Schwierigkeitsgrad",
    difficultyModeLabel = "Schwierigkeitsmodus:",
    cognitiveAssistanceTitle = "Kognitive Unterstützung & Hinweise",
    cognitiveAssistanceDesc = "Zeigt visuelle Hilfen, wenn eine Übung herausfordernd wird.",
    tempoTitle = "Übungstempo:",
    saveDifficultyButton = "Schwierigkeitseinstellungen Speichern",
    gameExperienceTitle = "Spielerlebnis",
    timedModeTitle = "Gegen die Uhr (Herausforderung)",
    timedModeDescOn = "Runden mit Zeituhr",
    timedModeDescOff = "Präzisionsmodus (entspannt, ohne Zeitdruck)",
    themeTitle = "Erscheinungsbild",
    audioHapticsTitle = "Töne & Vibration",
    soundLabel = "Soundeffekte",
    hapticsLabel = "Haptisches Feedback",
    notificationsTitle = "Tägliche Erinnerungen",
    resetDataTitle = "Fortschritt Zurücksetzen",
    resetDataDesc = "Löscht den gesamten Spielverlauf und erreichte Ränge.",
    resetDataButton = "Daten Zurücksetzen",

    quitGameConfirm = "Möchtest du das Spiel beenden?",
    scoreLabel = "Punktzahl",
    correctLabel = "Richtig",
    resultsTitle = "Spielergebnis",
    nextGame = "Nächste Übung",
    retryGame = "Wiederholen",
    finishSummary = "Zurück zum Start",
    reactionSpeed = "Durchschnittliche Reaktionszeit"
  )

  val Portuguese = StringsDefinition(
    tabToday = "Hoje",
    tabGames = "Jogos",
    tabProgress = "Progresso",
    tabSettings = "Ajustes",

    greeting = { name -> "Olá, $name 👋" },
    daySingle = "dia",
    dayPlural = "dias",
    streakLabel = "Sequência",
    dailySessionTitle = "Sua Sessão Diária",
    dailySessionDesc = "3 exercícios calibrados para exercitar suas áreas principais.",
    startDailySession = "Começar Sessão",
    continueDailySession = "Continuar Sessão",
    sessionCompletedTitle = "Sessão Concluída!",
    sessionCompletedDesc = "Você completou seu treino mental de hoje. Excelente trabalho!",
    quickStatsTitle = "Resumo Rápido",
    quickStatsCompleted = "Concluídos",
    quickStatsAvgScore = "Média",
    weeklyChallengesTitle = "Desafios Semanais",
    viewAllGames = "Ver todos os jogos",

    gamesLibraryTitle = "Ginásio Cerebral",
    gamesLibrarySubtitle = "9 jogos projetados para exercitar suas capacidades cognitivas",
    filterAll = "Todos",
    playButton = "Jogar",
    levelPrefix = "Nível",
    rulesTitle = "Como jogar?",
    instructionsTitle = "Instruções",
    startExercise = "Iniciar Exercício",
    closeButton = "Fechar",

    progressTitle = "Seu Progresso Cognitivo",
    progressSubtitle = "Evolução integrada nas 6 áreas cognitivas",
    domainMasteryTitle = "Maestria por Domínio",
    domainMasterySubtitle = "XP acumulada por área cognitiva (sem limite fixo)",
    gameRankingsTitle = "Classificação por Jogo",
    gameRankingsSubtitle = "Seu ranking atual em cada um dos 9 jogos",
    performanceCurveTitle = "Curva de Desempenho",
    recentHistoryTitle = "Histórico Recente",
    noGamesPlayedYet = "Nenhuma partida jogada ainda. Comece hoje mesmo!",
    pointsShort = "pts",

    settingsTitle = "Configurações",
    settingsSubtitle = "Personalize sua experiência de treino cognitivo",
    languageCardTitle = "Idioma do Aplicativo",
    languageCardSubtitle = "Selecione o idioma preferido para textos e exercícios",
    profilesSectionTitle = "Perfis de Usuário",
    activeStatus = "Ativo",
    newProfileButton = "Novo Perfil",
    editProfileTitle = "Editar Perfil Ativo",
    avatarLabel = "Avatar do perfil:",
    nameLabel = "Nome",
    saveProfileButton = "Salvar Perfil",
    weeklyGoalTitle = "Meta Semanal de Treino",
    daysPerWeek = { days -> "$days dias por semana" },
    cognitiveDifficultyTitle = "Dificuldade Cognitiva",
    difficultyModeLabel = "Modo de dificuldade:",
    cognitiveAssistanceTitle = "Assistência cognitiva e dicas",
    cognitiveAssistanceDesc = "Exibe dicas visuais e simplificações quando um exercício é exigente.",
    tempoTitle = "Ritmo de tempo nos exercícios:",
    saveDifficultyButton = "Salvar Preferências de Dificuldade",
    gameExperienceTitle = "Experiência de Jogo",
    timedModeTitle = "Modo Contra o Tempo (Desafio)",
    timedModeDescOn = "Partidas com temporizador",
    timedModeDescOff = "Modo Precisão (sem relógio, sem pressa)",
    themeTitle = "Tema do aplicativo",
    audioHapticsTitle = "Som e Vibração",
    soundLabel = "Efeitos sonoros",
    hapticsLabel = "Resposta tátil (vibração)",
    notificationsTitle = "Lembretes Diários",
    resetDataTitle = "Reiniciar Progresso",
    resetDataDesc = "Apaga todo o histórico de partidas, níveis e ranking.",
    resetDataButton = "Redefinir Dados",

    quitGameConfirm = "Deseja sair do jogo?",
    scoreLabel = "Pontuação",
    correctLabel = "Acertos",
    resultsTitle = "Resultados da Partida",
    nextGame = "Próximo Exercício",
    retryGame = "Repetir",
    finishSummary = "Voltar ao Início",
    reactionSpeed = "Tempo de reação médio"
  )

  fun get(lang: AppLanguage): StringsDefinition = when (lang) {
    AppLanguage.SPANISH -> Spanish
    AppLanguage.ENGLISH -> English
    AppLanguage.FRENCH -> French
    AppLanguage.GERMAN -> German
    AppLanguage.PORTUGUESE -> Portuguese
  }
}

val strings: StringsDefinition
  @Composable
  @ReadOnlyComposable
  get() = Translations.get(LocalAppLanguage.current)

fun getDomainName(domain: DomainType, lang: AppLanguage): String = when (lang) {
  AppLanguage.SPANISH -> when (domain) {
    DomainType.MEMORIA -> "Memoria"
    DomainType.ATENCION -> "Atención"
    DomainType.RAZONAMIENTO -> "Razonamiento"
    DomainType.LENGUAJE -> "Lenguaje"
    DomainType.CALCULO -> "Cálculo"
    DomainType.VELOCIDAD -> "Velocidad"
  }
  AppLanguage.ENGLISH -> when (domain) {
    DomainType.MEMORIA -> "Memory"
    DomainType.ATENCION -> "Attention"
    DomainType.RAZONAMIENTO -> "Reasoning"
    DomainType.LENGUAJE -> "Language"
    DomainType.CALCULO -> "Math"
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
    DomainType.VELOCIDAD -> "Tempo"
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

fun getGameTitle(gameId: String, lang: AppLanguage, defaultTitle: String): String = when (lang) {
  AppLanguage.SPANISH -> defaultTitle
  AppLanguage.ENGLISH -> when (gameId) {
    "parejas" -> "Hidden Pairs"
    "secuencia" -> "Light Sequence"
    "rutatesoro" -> "Treasure Path"
    "stroop" -> "Color or Word"
    "cambiochip" -> "Focus Switch"
    "series" -> "Series Detective"
    "anagramas" -> "Anagrams"
    "calculo" -> "Calm Math"
    "comparacion" -> "Instant Compare"
    else -> defaultTitle
  }
  AppLanguage.FRENCH -> when (gameId) {
    "parejas" -> "Paires Cachées"
    "secuencia" -> "Séquence Lumineuse"
    "rutatesoro" -> "Route du Trésor"
    "stroop" -> "Couleur ou Mot"
    "cambiochip" -> "Changement de Cap"
    "series" -> "Détective de Séries"
    "anagramas" -> "Anagrammes"
    "calculo" -> "Calcul Serein"
    "comparacion" -> "Comparaison Éclair"
    else -> defaultTitle
  }
  AppLanguage.GERMAN -> when (gameId) {
    "parejas" -> "Versteckte Paare"
    "secuencia" -> "Lichtsequenz"
    "rutatesoro" -> "Schatzroute"
    "stroop" -> "Farbe oder Wort"
    "cambiochip" -> "Fokus-Wechsel"
    "series" -> "Zahlenreihen-Detektiv"
    "anagramas" -> "Anagramme"
    "calculo" -> "Kopfrechnen"
    "comparacion" -> "Blitzvergleich"
    else -> defaultTitle
  }
  AppLanguage.PORTUGUESE -> when (gameId) {
    "parejas" -> "Pares Ocultos"
    "secuencia" -> "Sequência de Luzes"
    "rutatesoro" -> "Rota do Tesouro"
    "stroop" -> "Cor ou Palavra"
    "cambiochip" -> "Mudar o Chip"
    "series" -> "Detetive de Séries"
    "anagramas" -> "Anagramas"
    "calculo" -> "Cálculo Sereno"
    "comparacion" -> "Comparação Rápida"
    else -> defaultTitle
  }
}
