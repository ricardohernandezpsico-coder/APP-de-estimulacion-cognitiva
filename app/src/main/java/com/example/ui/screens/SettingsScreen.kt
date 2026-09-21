package com.example.ui.screens

import android.os.Build
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.VolumeUp
import androidx.compose.material.icons.filled.*
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.model.AgeBand
import com.example.model.AppLanguage
import com.example.model.DifficultyMode
import com.example.model.DomainType
import com.example.model.ThemeMode
import com.example.model.UserSettings
import com.example.ui.i18n.strings
import com.example.ui.theme.EmeraldAccent
import com.example.ui.theme.TealPrimary
import com.example.viewmodel.NeuroVidaViewModel

@Composable
fun SettingsScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val userSettings by viewModel.userSettings.collectAsState()
  val allProfiles by viewModel.allProfiles.collectAsState()

  var nameInput by remember(userSettings.name) { mutableStateOf(userSettings.name) }
  var selectedAvatar by remember(userSettings.avatar) { mutableStateOf(userSettings.avatar) }
  var weeklyGoal by remember(userSettings.weeklyGoal) { mutableStateOf(userSettings.weeklyGoal) }
  var defaultTimed by remember(userSettings.defaultTimed) { mutableStateOf(userSettings.defaultTimed) }
  var soundEnabled by remember(userSettings.soundEnabled) { mutableStateOf(userSettings.soundEnabled) }
  var hapticsEnabled by remember(userSettings.hapticsEnabled) { mutableStateOf(userSettings.hapticsEnabled) }
  var notificationsEnabled by remember(userSettings.notificationsEnabled) { mutableStateOf(userSettings.notificationsEnabled) }
  var reminderHour by remember(userSettings.reminderHour) { mutableStateOf(userSettings.reminderHour) }
  var reminderMinute by remember(userSettings.reminderMinute) { mutableStateOf(userSettings.reminderMinute) }

  // Granular difficulty states
  var difficultyMode by remember(userSettings.difficultyMode) { mutableStateOf(userSettings.difficultyMode) }
  var diffMemoria by remember(userSettings.difficultyMemoria) { mutableStateOf(userSettings.difficultyMemoria) }
  var diffAtencion by remember(userSettings.difficultyAtencion) { mutableStateOf(userSettings.difficultyAtencion) }
  var diffRazonamiento by remember(userSettings.difficultyRazonamiento) { mutableStateOf(userSettings.difficultyRazonamiento) }
  var diffLenguaje by remember(userSettings.difficultyLenguaje) { mutableStateOf(userSettings.difficultyLenguaje) }
  var diffCalculo by remember(userSettings.difficultyCalculo) { mutableStateOf(userSettings.difficultyCalculo) }
  var diffVelocidad by remember(userSettings.difficultyVelocidad) { mutableStateOf(userSettings.difficultyVelocidad) }
  var cognitiveAssistance by remember(userSettings.cognitiveAssistance) { mutableStateOf(userSettings.cognitiveAssistance) }
  var timeScaleFactor by remember(userSettings.timeScaleFactor) { mutableStateOf(userSettings.timeScaleFactor) }
  var themeMode by remember(userSettings.themeMode) { mutableStateOf(userSettings.themeMode) }
  var appLanguage by remember(userSettings.language) { mutableStateOf(userSettings.language) }
  var ageBand by remember(userSettings.ageBand) { mutableStateOf(userSettings.ageBand ?: AgeBand.ADULT) }

  var showNewProfileDialog by remember { mutableStateOf(false) }
  var profileToDelete by remember { mutableStateOf<UserSettings?>(null) }

  val context = LocalContext.current
  val notificationPermissionLauncher = rememberLauncherForActivityResult(
    contract = ActivityResultContracts.RequestPermission()
  ) { isGranted ->
    if (isGranted) {
      notificationsEnabled = true
      viewModel.updateSettings(
        name = nameInput,
        weeklyGoal = weeklyGoal,
        defaultTimed = defaultTimed,
        sound = soundEnabled,
        haptics = hapticsEnabled,
        notificationsEnabled = true,
        reminderHour = reminderHour,
        reminderMinute = reminderMinute
      )
      Toast.makeText(context, "Recordatorios diarios activados con WorkManager", Toast.LENGTH_SHORT).show()
    } else {
      notificationsEnabled = false
      viewModel.updateSettings(
        name = nameInput,
        weeklyGoal = weeklyGoal,
        defaultTimed = defaultTimed,
        sound = soundEnabled,
        haptics = hapticsEnabled,
        notificationsEnabled = false,
        reminderHour = reminderHour,
        reminderMinute = reminderMinute
      )
      Toast.makeText(context, "Permiso de notificaciones denegado", Toast.LENGTH_SHORT).show()
    }
  }

  var showResetDialog by remember { mutableStateOf(false) }

  Column(
    modifier = modifier
      .fillMaxSize()
      .background(MaterialTheme.colorScheme.background)
      .verticalScroll(rememberScrollState())
      .padding(horizontal = 20.dp),
    verticalArrangement = Arrangement.spacedBy(20.dp)
  ) {
    Spacer(modifier = Modifier.height(16.dp))

    Text(
      text = strings.settingsTitle,
      style = MaterialTheme.typography.headlineMedium,
      fontWeight = FontWeight.Black,
      color = MaterialTheme.colorScheme.onBackground
    )

    // Language Selection Card (Internationalization)
    Card(
      modifier = Modifier.fillMaxWidth().testTag("card_language_selector"),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
      ) {
        Row(
          verticalAlignment = Alignment.CenterVertically,
          horizontalArrangement = Arrangement.SpaceBetween,
          modifier = Modifier.fillMaxWidth()
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Language, contentDescription = null, tint = TealPrimary)
            Spacer(modifier = Modifier.width(10.dp))
            Column {
              Text(
                text = strings.languageCardTitle,
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
              )
              Text(
                text = strings.languageCardSubtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
              )
            }
          }
        }

        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
          AppLanguage.entries.forEach { lang ->
            val isSelected = appLanguage == lang
            FilterChip(
              selected = isSelected,
              onClick = {
                appLanguage = lang
                viewModel.updateSettings(
                  name = nameInput,
                  weeklyGoal = weeklyGoal,
                  defaultTimed = defaultTimed,
                  sound = soundEnabled,
                  haptics = hapticsEnabled,
                  notificationsEnabled = notificationsEnabled,
                  reminderHour = reminderHour,
                  reminderMinute = reminderMinute,
                  themeMode = themeMode,
                  language = lang
                )
              },
              label = {
                Text(
                  text = "${lang.flagEmoji} ${lang.displayName}",
                  fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal
                )
              },
              colors = FilterChipDefaults.filterChipColors(
                selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                selectedLabelColor = TealPrimary
              ),
              modifier = Modifier.testTag("chip_lang_${lang.code}")
            )
          }
        }
      }
    }

    // 1. User Profiles Management Card (Room Local Database)
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(modifier = Modifier.padding(20.dp)) {
        Row(
          verticalAlignment = Alignment.CenterVertically,
          horizontalArrangement = Arrangement.SpaceBetween,
          modifier = Modifier.fillMaxWidth()
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Person, contentDescription = null, tint = TealPrimary)
            Spacer(modifier = Modifier.width(8.dp))
            Text(
              text = "Perfiles de Usuario",
              style = MaterialTheme.typography.titleMedium,
              fontWeight = FontWeight.Bold
            )
          }

          Surface(
            shape = RoundedCornerShape(8.dp),
            color = TealPrimary.copy(alpha = 0.12f)
          ) {
            Text(
              text = "${allProfiles.size} ${if (allProfiles.size == 1) "perfil" else "perfiles"} en Room",
              style = MaterialTheme.typography.labelSmall,
              color = TealPrimary,
              fontWeight = FontWeight.SemiBold,
              modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
            )
          }
        }

        Spacer(modifier = Modifier.height(6.dp))
        Text(
          text = "Gestiona múltiples usuarios o familiares en este dispositivo con preferencias independientes.",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        Spacer(modifier = Modifier.height(14.dp))

        // Profile switcher list
        LazyRow(
          horizontalArrangement = Arrangement.spacedBy(10.dp),
          modifier = Modifier.fillMaxWidth()
        ) {
          items(allProfiles) { profile ->
            val isActive = profile.isActive || profile.id == userSettings.id
            Surface(
              shape = RoundedCornerShape(16.dp),
              color = if (isActive) TealPrimary.copy(alpha = 0.12f) else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
              border = if (isActive) BorderStroke(2.dp, TealPrimary) else BorderStroke(1.dp, Color.Transparent),
              modifier = Modifier
                .clickable {
                  if (!isActive) {
                    viewModel.switchProfile(profile.id)
                    Toast.makeText(context, "Cambiado a perfil: ${profile.name}", Toast.LENGTH_SHORT).show()
                  }
                }
                .testTag("profile_chip_${profile.id}")
            ) {
              Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp)
              ) {
                Text(text = profile.avatar, fontSize = 20.sp)
                Spacer(modifier = Modifier.width(8.dp))
                Column {
                  Text(
                    text = profile.name,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = if (isActive) FontWeight.Bold else FontWeight.Normal,
                    color = if (isActive) TealPrimary else MaterialTheme.colorScheme.onSurface
                  )
                  Text(
                    text = if (isActive) "Activo" else profile.difficultyMode.label,
                    style = MaterialTheme.typography.labelSmall,
                    color = if (isActive) TealPrimary else MaterialTheme.colorScheme.onSurfaceVariant
                  )
                }

                if (!isActive && allProfiles.size > 1) {
                  Spacer(modifier = Modifier.width(4.dp))
                  IconButton(
                    onClick = { profileToDelete = profile },
                    modifier = Modifier.size(24.dp)
                  ) {
                    Icon(
                      imageVector = Icons.Default.Delete,
                      contentDescription = "Eliminar perfil",
                      tint = MaterialTheme.colorScheme.error.copy(alpha = 0.7f),
                      modifier = Modifier.size(16.dp)
                    )
                  }
                }
              }
            }
          }

          item {
            OutlinedButton(
              onClick = { showNewProfileDialog = true },
              shape = RoundedCornerShape(16.dp),
              colors = ButtonDefaults.outlinedButtonColors(contentColor = TealPrimary),
              border = BorderStroke(1.dp, TealPrimary.copy(alpha = 0.5f)),
              contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
              modifier = Modifier.testTag("btn_add_profile")
            ) {
              Icon(Icons.Default.Add, contentDescription = null, modifier = Modifier.size(16.dp))
              Spacer(modifier = Modifier.width(6.dp))
              Text("Nuevo Perfil", style = MaterialTheme.typography.labelMedium)
            }
          }
        }

        Spacer(modifier = Modifier.height(18.dp))
        Divider(color = MaterialTheme.colorScheme.outlineVariant.copy(alpha = 0.5f))
        Spacer(modifier = Modifier.height(14.dp))

        // Edit active profile name & avatar
        Text(
          text = "Editar Perfil Activo",
          style = MaterialTheme.typography.titleSmall,
          fontWeight = FontWeight.Bold
        )

        Spacer(modifier = Modifier.height(8.dp))
        Text(
          text = "Avatar del perfil:",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(6.dp))

        val avatarChoices = listOf("🧠", "👵", "👴", "🌟", "🎯", "🦉", "🌿", "💡", "⚡")
        LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
          items(avatarChoices) { av ->
            val isChosen = selectedAvatar == av
            Surface(
              shape = CircleShape,
              color = if (isChosen) TealPrimary.copy(alpha = 0.2f) else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
              border = if (isChosen) BorderStroke(2.dp, TealPrimary) else BorderStroke(1.dp, Color.Transparent),
              modifier = Modifier
                .size(42.dp)
                .clickable { selectedAvatar = av }
            ) {
              Box(contentAlignment = Alignment.Center) {
                Text(text = av, fontSize = 20.sp)
              }
            }
          }
        }

        Spacer(modifier = Modifier.height(12.dp))

        OutlinedTextField(
          value = nameInput,
          onValueChange = { nameInput = it },
          label = { Text("Nombre del perfil") },
          singleLine = true,
          modifier = Modifier
            .fillMaxWidth()
            .testTag("input_user_name"),
          shape = RoundedCornerShape(14.dp),
          colors = OutlinedTextFieldDefaults.colors(
            focusedBorderColor = TealPrimary,
            focusedLabelColor = TealPrimary
          )
        )

        Spacer(modifier = Modifier.height(12.dp))

        Button(
          onClick = {
            viewModel.updateSettings(
              name = nameInput.trim().ifEmpty { "Usuario" },
              avatar = selectedAvatar,
              weeklyGoal = weeklyGoal,
              defaultTimed = defaultTimed,
              sound = soundEnabled,
              haptics = hapticsEnabled,
              notificationsEnabled = notificationsEnabled,
              reminderHour = reminderHour,
              reminderMinute = reminderMinute,
              difficultyMode = difficultyMode
            )
            Toast.makeText(context, "Perfil guardado en Room", Toast.LENGTH_SHORT).show()
          },
          shape = RoundedCornerShape(12.dp),
          colors = ButtonDefaults.buttonColors(containerColor = TealPrimary),
          modifier = Modifier
            .align(Alignment.End)
            .testTag("btn_save_name")
        ) {
          Icon(Icons.Outlined.Check, contentDescription = null, modifier = Modifier.size(16.dp))
          Spacer(modifier = Modifier.width(6.dp))
          Text("Guardar Perfil")
        }
      }
    }

    // 2. Weekly Training Goal Card
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(modifier = Modifier.padding(20.dp)) {
        Text(
          text = "Meta Semanal de Entrenamiento",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )
        Text(
          text = "$weeklyGoal días por semana",
          style = MaterialTheme.typography.bodyLarge,
          fontWeight = FontWeight.Black,
          color = TealPrimary,
          modifier = Modifier.padding(top = 4.dp)
        )

        Spacer(modifier = Modifier.height(8.dp))

        Slider(
          value = weeklyGoal.toFloat(),
          onValueChange = {
            weeklyGoal = it.toInt()
            viewModel.updateSettings(
              name = nameInput,
              avatar = selectedAvatar,
              weeklyGoal = weeklyGoal,
              defaultTimed = defaultTimed,
              sound = soundEnabled,
              haptics = hapticsEnabled,
              notificationsEnabled = notificationsEnabled,
              reminderHour = reminderHour,
              reminderMinute = reminderMinute,
              difficultyMode = difficultyMode
            )
          },
          valueRange = 1f..7f,
          steps = 5,
          colors = SliderDefaults.colors(
            thumbColor = TealPrimary,
            activeTrackColor = TealPrimary
          ),
          modifier = Modifier.testTag("slider_weekly_goal")
        )
      }
    }

    // 3. Cognitive Difficulty Customization Card (Room Local Database)
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
      ) {
        Row(
          verticalAlignment = Alignment.CenterVertically,
          horizontalArrangement = Arrangement.SpaceBetween,
          modifier = Modifier.fillMaxWidth()
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Tune, contentDescription = null, tint = TealPrimary)
            Spacer(modifier = Modifier.width(8.dp))
            Text(
              text = "Dificultad Cognitiva (Room)",
              style = MaterialTheme.typography.titleMedium,
              fontWeight = FontWeight.Bold
            )
          }

          Surface(
            shape = RoundedCornerShape(8.dp),
            color = EmeraldAccent.copy(alpha = 0.15f)
          ) {
            Text(
              text = difficultyMode.label,
              style = MaterialTheme.typography.labelSmall,
              color = EmeraldAccent,
              fontWeight = FontWeight.Bold,
              modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
            )
          }
        }

        Text(
          text = "Ajusta cómo la aplicación adapta el nivel de desafío y estímulo a las capacidades y objetivos de ${userSettings.name}.",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        // Mode selection chips
        Text(
          text = "Modo de dificultad:",
          style = MaterialTheme.typography.titleSmall,
          fontWeight = FontWeight.Bold
        )

        LazyRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
          items(DifficultyMode.values()) { mode ->
            val isSelected = difficultyMode == mode
            FilterChip(
              selected = isSelected,
              onClick = { difficultyMode = mode },
              label = {
                Text(
                  text = when (mode) {
                    DifficultyMode.ADAPTIVE -> "⚡ Adaptativo"
                    DifficultyMode.PRINCIPIANTE -> "🌱 Principiante"
                    DifficultyMode.INTERMEDIO -> "⚖️ Intermedio"
                    DifficultyMode.AVANZADO -> "🚀 Avanzado"
                    DifficultyMode.CUSTOM -> "🎨 Por Dominio"
                  }
                )
              },
              colors = FilterChipDefaults.filterChipColors(
                selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                selectedLabelColor = TealPrimary
              )
            )
          }
        }

        // Mode description card
        Surface(
          shape = RoundedCornerShape(14.dp),
          color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f),
          modifier = Modifier.fillMaxWidth()
        ) {
          Row(modifier = Modifier.padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
            Icon(
              imageVector = Icons.Default.Info,
              contentDescription = null,
              tint = TealPrimary,
              modifier = Modifier.size(20.dp)
            )
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = when (difficultyMode) {
                DifficultyMode.ADAPTIVE -> "El algoritmo auto-adaptativo evalúa tu precisión en cada sesión y sube o baja de nivel dinámicamente."
                DifficultyMode.PRINCIPIANTE -> "Nivel 1 relajado con estímulos suaves y tiempo generoso. Recomendado para iniciar o estimulación ligera."
                DifficultyMode.INTERMEDIO -> "Nivel 3 equilibrado. Ideal para mantenimiento mental habitual con un grado moderado de reto."
                DifficultyMode.AVANZADO -> "Nivel 5 de alta exigencia mental, velocidad y distractores para usuarios experimentados."
                DifficultyMode.CUSTOM -> "Personalización detallada nivel por nivel para cada uno de los 6 dominios cognitivos."
              },
              style = MaterialTheme.typography.bodySmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
          }
        }

        // Granular Domain Controls
        Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
          Text(
            text = if (difficultyMode == DifficultyMode.CUSTOM) "Nivel por Dominio Cognitivo (1 a 5):" else "Ajuste fino de Dominios (Personalizado):",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold
          )

          // Domain 1: Memoria
          DifficultyDomainSlider(
            title = "Memoria",
            icon = "🧠",
            level = diffMemoria,
            onLevelChange = { diffMemoria = it }
          )

          // Domain 2: Atención
          DifficultyDomainSlider(
            title = "Atención",
            icon = "🎯",
            level = diffAtencion,
            onLevelChange = { diffAtencion = it }
          )

          // Domain 3: Razonamiento
          DifficultyDomainSlider(
            title = "Razonamiento",
            icon = "🧩",
            level = diffRazonamiento,
            onLevelChange = { diffRazonamiento = it }
          )

          // Domain 4: Lenguaje
          DifficultyDomainSlider(
            title = "Lenguaje",
            icon = "📖",
            level = diffLenguaje,
            onLevelChange = { diffLenguaje = it }
          )

          // Domain 5: Cálculo
          DifficultyDomainSlider(
            title = "Cálculo",
            icon = "🔢",
            level = diffCalculo,
            onLevelChange = { diffCalculo = it }
          )

          // Domain 6: Velocidad
          DifficultyDomainSlider(
            title = "Velocidad",
            icon = "⚡",
            level = diffVelocidad,
            onLevelChange = { diffVelocidad = it }
          )
        }

        Divider(color = MaterialTheme.colorScheme.outlineVariant.copy(alpha = 0.5f))

        // Cognitive Assistance Toggle
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Column(modifier = Modifier.weight(1f)) {
            Text(
              text = "Asistencia cognitiva y pistas",
              style = MaterialTheme.typography.bodyLarge,
              fontWeight = FontWeight.SemiBold
            )
            Text(
              text = "Muestra pistas visuales y simplificaciones cuando un ejercicio resulta complejo.",
              style = MaterialTheme.typography.bodySmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
          }
          Switch(
            checked = cognitiveAssistance,
            onCheckedChange = { cognitiveAssistance = it },
            colors = SwitchDefaults.colors(checkedThumbColor = TealPrimary, checkedTrackColor = TealPrimary.copy(alpha = 0.4f)),
            modifier = Modifier.testTag("switch_cognitive_assistance")
          )
        }

        // Time Scale Factor
        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
          Text(
            text = "Ritmo de tiempo en ejercicios:",
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.SemiBold
          )

          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
          ) {
            val scales = listOf(
              Triple(1.0f, "1.0x", "Estándar"),
              Triple(1.25f, "1.25x", "Pausado"),
              Triple(1.5f, "1.5x", "Terapéutico")
            )
            scales.forEach { (factor, label, sub) ->
              val isSelected = (timeScaleFactor - factor).let { if (it < 0) -it else it } < 0.05f
              Surface(
                shape = RoundedCornerShape(12.dp),
                color = if (isSelected) TealPrimary.copy(alpha = 0.15f) else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
                border = if (isSelected) BorderStroke(2.dp, TealPrimary) else BorderStroke(1.dp, Color.Transparent),
                modifier = Modifier
                  .weight(1f)
                  .clickable { timeScaleFactor = factor }
              ) {
                Column(
                  modifier = Modifier.padding(vertical = 10.dp, horizontal = 4.dp),
                  horizontalAlignment = Alignment.CenterHorizontally
                ) {
                  Text(
                    text = label,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Bold,
                    color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.onSurface
                  )
                  Text(
                    text = sub,
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                  )
                }
              }
            }
          }
        }

        // Save Difficulty Preferences Button
        Button(
          onClick = {
            viewModel.updateDifficultyPreferences(
              mode = difficultyMode,
              memoria = diffMemoria,
              atencion = diffAtencion,
              razonamiento = diffRazonamiento,
              lenguaje = diffLenguaje,
              calculo = diffCalculo,
              velocidad = diffVelocidad,
              assistance = cognitiveAssistance,
              timeScale = timeScaleFactor
            )
            Toast.makeText(context, "Preferencias de dificultad guardadas en Room", Toast.LENGTH_SHORT).show()
          },
          shape = RoundedCornerShape(12.dp),
          colors = ButtonDefaults.buttonColors(containerColor = TealPrimary),
          modifier = Modifier
            .fillMaxWidth()
            .testTag("btn_save_difficulty")
        ) {
          Icon(Icons.Outlined.Check, contentDescription = null, modifier = Modifier.size(18.dp))
          Spacer(modifier = Modifier.width(8.dp))
          Text("Guardar Dificultad en Room")
        }
      }
    }

    // Game Mode & Audio/Haptic Switches
    Card(
      modifier = Modifier.fillMaxWidth(),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
      ) {
        Text(
          text = "Experiencia de Juego",
          style = MaterialTheme.typography.titleMedium,
          fontWeight = FontWeight.Bold
        )

        // Default mode toggle
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Column(modifier = Modifier.weight(1f)) {
            Text(
              text = "Modo contra el reloj (Reto)",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
            Text(
              text = if (defaultTimed) "Partidas con temporizador" else "Modo Precisión (sin reloj, sin prisa)",
              style = MaterialTheme.typography.bodySmall,
              color = MaterialTheme.colorScheme.onSurfaceVariant
            )
          }
          Switch(
            checked = defaultTimed,
            onCheckedChange = {
              defaultTimed = it
              viewModel.updateSettings(
                name = nameInput,
                weeklyGoal = weeklyGoal,
                defaultTimed = it,
                sound = soundEnabled,
                haptics = hapticsEnabled,
                notificationsEnabled = notificationsEnabled,
                reminderHour = reminderHour,
                reminderMinute = reminderMinute
              )
            },
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_mode_timed")
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Theme mode selector (Claro / Oscuro / Sistema)
        Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.DarkMode, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Tema de la app",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            ThemeMode.values().forEach { mode ->
              FilterChip(
                selected = themeMode == mode,
                onClick = {
                  themeMode = mode
                  viewModel.updateSettings(
                    name = nameInput,
                    weeklyGoal = weeklyGoal,
                    defaultTimed = defaultTimed,
                    sound = soundEnabled,
                    haptics = hapticsEnabled,
                    notificationsEnabled = notificationsEnabled,
                    reminderHour = reminderHour,
                    reminderMinute = reminderMinute,
                    themeMode = mode
                  )
                },
                label = {
                  Text(
                    text = when (mode) {
                      ThemeMode.LIGHT -> "☀️ Claro"
                      ThemeMode.DARK -> "🌙 Oscuro"
                      ThemeMode.SYSTEM -> "⚙️ Sistema"
                    }
                  )
                },
                colors = FilterChipDefaults.filterChipColors(
                  selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                  selectedLabelColor = TealPrimary
                ),
                modifier = Modifier.testTag("chip_theme_${mode.name.lowercase()}")
              )
            }
          }
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Rango de edad (piloto de perfiles por edad, 20-sep): mismo patrón visual que
        // el selector de tema. Cambia el DDA/tamaño de cartas en Parejas Ocultas
        // únicamente por ahora -- el onboarding prometió "podés cambiarlo cuando
        // quieras desde Ajustes", esto cumple esa promesa.
        Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.Person, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Rango de edad",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            AgeBand.entries.forEach { band ->
              FilterChip(
                selected = ageBand == band,
                onClick = {
                  ageBand = band
                  viewModel.setAgeBand(band)
                },
                label = { Text(text = band.label) },
                colors = FilterChipDefaults.filterChipColors(
                  selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                  selectedLabelColor = TealPrimary
                ),
                modifier = Modifier.testTag("chip_age_band_${band.name.lowercase()}")
              )
            }
          }
          Text(
            text = "Por ahora ajusta la dificultad y el tamaño de las cartas en Parejas Ocultas.",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Sound switch
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
            Icon(Icons.AutoMirrored.Filled.VolumeUp, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Efectos de sonido",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Switch(
            checked = soundEnabled,
            onCheckedChange = {
              soundEnabled = it
              viewModel.updateSettings(
                name = nameInput,
                weeklyGoal = weeklyGoal,
                defaultTimed = defaultTimed,
                sound = it,
                haptics = hapticsEnabled,
                notificationsEnabled = notificationsEnabled,
                reminderHour = reminderHour,
                reminderMinute = reminderMinute
              )
            },
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_sound")
          )
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

        // Haptics switch
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.weight(1f)) {
            Icon(Icons.Default.Vibration, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(modifier = Modifier.width(10.dp))
            Text(
              text = "Vibración y respuesta háptica",
              style = MaterialTheme.typography.bodyMedium,
              fontWeight = FontWeight.SemiBold
            )
          }
          Switch(
            checked = hapticsEnabled,
            onCheckedChange = {
              hapticsEnabled = it
              viewModel.updateSettings(
                name = nameInput,
                weeklyGoal = weeklyGoal,
                defaultTimed = defaultTimed,
                sound = soundEnabled,
                haptics = it,
                notificationsEnabled = notificationsEnabled,
                reminderHour = reminderHour,
                reminderMinute = reminderMinute
              )
            },
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_haptics")
          )
        }
      }
    }

    // Programmed Notifications Card (WorkManager)
    Card(
      modifier = Modifier
        .fillMaxWidth()
        .testTag("card_notifications_workmanager"),
      shape = RoundedCornerShape(22.dp),
      colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
      Column(
        modifier = Modifier.padding(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
      ) {
        Row(
          modifier = Modifier.fillMaxWidth(),
          horizontalArrangement = Arrangement.SpaceBetween,
          verticalAlignment = Alignment.CenterVertically
        ) {
          Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(Icons.Default.NotificationsActive, contentDescription = null, tint = TealPrimary)
            Spacer(modifier = Modifier.width(8.dp))
            Column {
              Text(
                text = "Recordatorios Diarios",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
              )
              Text(
                text = "Programados con Android WorkManager",
                style = MaterialTheme.typography.labelSmall,
                color = TealPrimary
              )
            }
          }

          Switch(
            checked = notificationsEnabled,
            onCheckedChange = { isChecked ->
              if (isChecked) {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                  notificationPermissionLauncher.launch(android.Manifest.permission.POST_NOTIFICATIONS)
                } else {
                  notificationsEnabled = true
                  viewModel.updateSettings(
                    name = nameInput,
                    weeklyGoal = weeklyGoal,
                    defaultTimed = defaultTimed,
                    sound = soundEnabled,
                    haptics = hapticsEnabled,
                    notificationsEnabled = true,
                    reminderHour = reminderHour,
                    reminderMinute = reminderMinute
                  )
                  Toast.makeText(context, "Recordatorios diarios programados", Toast.LENGTH_SHORT).show()
                }
              } else {
                notificationsEnabled = false
                viewModel.updateSettings(
                  name = nameInput,
                  weeklyGoal = weeklyGoal,
                  defaultTimed = defaultTimed,
                  sound = soundEnabled,
                  haptics = hapticsEnabled,
                  notificationsEnabled = false,
                  reminderHour = reminderHour,
                  reminderMinute = reminderMinute
                )
                Toast.makeText(context, "Recordatorios desactivados", Toast.LENGTH_SHORT).show()
              }
            },
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TealPrimary),
            modifier = Modifier.testTag("switch_notifications_enabled")
          )
        }

        Text(
          text = "Recibe un aviso motivador cada día a la hora seleccionada para ejercitar memoria, cálculo y atención sin interrumpir tu rutina.",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant
        )

        if (notificationsEnabled) {
          HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)

          // Schedule time selector
          Text(
            text = "Horario preferido de entrenamiento",
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.SemiBold
          )

          Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
          ) {
            val times = listOf(
              Triple("Mañana", 9, 0),
              Triple("Tarde", 15, 0),
              Triple("Noche", 19, 0),
              Triple("21:00", 21, 0)
            )

            times.forEach { (label, h, m) ->
              val isSelected = reminderHour == h && reminderMinute == m
              OutlinedButton(
                onClick = {
                  reminderHour = h
                  reminderMinute = m
                  viewModel.updateSettings(
                    name = nameInput,
                    weeklyGoal = weeklyGoal,
                    defaultTimed = defaultTimed,
                    sound = soundEnabled,
                    haptics = hapticsEnabled,
                    notificationsEnabled = true,
                    reminderHour = h,
                    reminderMinute = m
                  )
                  Toast.makeText(context, "Recordatorio fijado a las %02d:%02d".format(h, m), Toast.LENGTH_SHORT).show()
                },
                modifier = Modifier.weight(1f).testTag("btn_time_${label.lowercase()}"),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.outlinedButtonColors(
                  containerColor = if (isSelected) TealPrimary.copy(alpha = 0.15f) else Color.Transparent
                ),
                border = androidx.compose.foundation.BorderStroke(
                  width = if (isSelected) 2.dp else 1.dp,
                  color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.outlineVariant
                ),
                contentPadding = PaddingValues(vertical = 8.dp, horizontal = 4.dp)
              ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                  Text(
                    text = label,
                    style = MaterialTheme.typography.labelMedium,
                    fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Normal,
                    color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.onSurface
                  )
                  Text(
                    text = "%02d:%02d".format(h, m),
                    style = MaterialTheme.typography.labelSmall,
                    color = if (isSelected) TealPrimary else MaterialTheme.colorScheme.onSurfaceVariant
                  )
                }
              }
            }
          }

          // Test notification button
          OutlinedButton(
            onClick = {
              viewModel.triggerTestNotification()
              Toast.makeText(context, "¡Notificación de prueba enviada!", Toast.LENGTH_SHORT).show()
            },
            modifier = Modifier
              .fillMaxWidth()
              .testTag("btn_test_notification"),
            shape = RoundedCornerShape(12.dp)
          ) {
            Icon(Icons.Default.Send, contentDescription = null, modifier = Modifier.size(16.dp))
            Spacer(modifier = Modifier.width(8.dp))
            Text("Probar notificación ahora (WorkManager)")
          }
        }
      }
    }

    // Privacy Card
    Surface(
      shape = RoundedCornerShape(18.dp),
      color = TealPrimary.copy(alpha = 0.08f),
      modifier = Modifier.fillMaxWidth()
    ) {
      Row(
        modifier = Modifier.padding(16.dp),
        verticalAlignment = Alignment.CenterVertically
      ) {
        Icon(Icons.Default.Security, contentDescription = null, tint = TealPrimary)
        Spacer(modifier = Modifier.width(12.dp))
        Column {
          Text(
            text = "100% Privado y Seguro",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.Bold,
            color = TealPrimary
          )
          Text(
            text = "Tus puntuaciones y progreso se almacenan únicamente en tu dispositivo. Sin anuncios ni suscripciones.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )
        }
      }
    }

    // Medical Disclaimer
    Surface(
      shape = RoundedCornerShape(18.dp),
      color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.4f),
      modifier = Modifier.fillMaxWidth()
    ) {
      Row(
        modifier = Modifier.padding(16.dp),
        verticalAlignment = Alignment.Top
      ) {
        Icon(Icons.Default.Info, contentDescription = null, tint = MaterialTheme.colorScheme.onSurfaceVariant)
        Spacer(modifier = Modifier.width(12.dp))
        Text(
          text = "NeuroVida es una aplicación para el entretenimiento, agilidad y bienestar cognitivo. No constituye diagnóstico ni reemplazo de consejo médico o profesional.",
          style = MaterialTheme.typography.bodySmall,
          color = MaterialTheme.colorScheme.onSurfaceVariant,
          lineHeight = 16.sp
        )
      }
    }

    // Reset Data button
    OutlinedButton(
      onClick = { showResetDialog = true },
      colors = ButtonDefaults.outlinedButtonColors(contentColor = MaterialTheme.colorScheme.error),
      shape = RoundedCornerShape(14.dp),
      modifier = Modifier
        .fillMaxWidth()
        .testTag("btn_reset_data")
    ) {
      Text("Restablecer datos y puntuaciones")
    }

    Spacer(modifier = Modifier.height(90.dp))
  }

  if (showResetDialog) {
    AlertDialog(
      onDismissRequest = { showResetDialog = false },
      title = { Text("¿Restablecer datos?") },
      text = { Text("Esta acción borrará el historial de partidas y niveles guardados en este dispositivo.") },
      confirmButton = {
        TextButton(
          onClick = {
            viewModel.resetData()
            showResetDialog = false
          }
        ) {
          Text("Restablecer", color = MaterialTheme.colorScheme.error)
        }
      },
      dismissButton = {
        TextButton(onClick = { showResetDialog = false }) {
          Text("Cancelar")
        }
      }
    )
  }

  // Dialog: Create New Profile
  if (showNewProfileDialog) {
    var newName by remember { mutableStateOf("") }
    var newAvatar by remember { mutableStateOf("🧠") }
    var newMode by remember { mutableStateOf(DifficultyMode.ADAPTIVE) }
    val avatarChoices = listOf("🧠", "👵", "👴", "🌟", "🎯", "🦉", "🌿", "💡", "⚡")

    AlertDialog(
      onDismissRequest = { showNewProfileDialog = false },
      title = {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Icon(Icons.Default.Person, contentDescription = null, tint = TealPrimary)
          Spacer(modifier = Modifier.width(8.dp))
          Text("Nuevo Perfil de Usuario")
        }
      },
      text = {
        Column(verticalArrangement = Arrangement.spacedBy(14.dp)) {
          Text(
            text = "Crea un perfil con su propia configuración de dificultad y progreso en Room.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
          )

          OutlinedTextField(
            value = newName,
            onValueChange = { newName = it },
            label = { Text("Nombre (ej. Carmen, Papá, Sofía)") },
            singleLine = true,
            modifier = Modifier
              .fillMaxWidth()
              .testTag("input_new_profile_name"),
            shape = RoundedCornerShape(12.dp),
            colors = OutlinedTextFieldDefaults.colors(
              focusedBorderColor = TealPrimary,
              focusedLabelColor = TealPrimary
            )
          )

          Text(
            text = "Selecciona un avatar:",
            style = MaterialTheme.typography.bodySmall,
            fontWeight = FontWeight.SemiBold
          )

          LazyRow(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
            items(avatarChoices) { av ->
              val isChosen = newAvatar == av
              Surface(
                shape = CircleShape,
                color = if (isChosen) TealPrimary.copy(alpha = 0.2f) else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
                border = if (isChosen) BorderStroke(2.dp, TealPrimary) else BorderStroke(1.dp, Color.Transparent),
                modifier = Modifier
                  .size(38.dp)
                  .clickable { newAvatar = av }
              ) {
                Box(contentAlignment = Alignment.Center) {
                  Text(text = av, fontSize = 18.sp)
                }
              }
            }
          }

          Text(
            text = "Dificultad inicial:",
            style = MaterialTheme.typography.bodySmall,
            fontWeight = FontWeight.SemiBold
          )

          LazyRow(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
            items(DifficultyMode.values()) { mode ->
              FilterChip(
                selected = newMode == mode,
                onClick = { newMode = mode },
                label = { Text(mode.label, style = MaterialTheme.typography.labelSmall) },
                colors = FilterChipDefaults.filterChipColors(
                  selectedContainerColor = TealPrimary.copy(alpha = 0.15f),
                  selectedLabelColor = TealPrimary
                )
              )
            }
          }
        }
      },
      confirmButton = {
        Button(
          onClick = {
            if (newName.isNotBlank()) {
              viewModel.createNewProfile(
                name = newName.trim(),
                avatar = newAvatar,
                difficultyMode = newMode
              )
              showNewProfileDialog = false
              Toast.makeText(context, "Perfil '${newName.trim()}' creado en Room", Toast.LENGTH_SHORT).show()
            }
          },
          enabled = newName.isNotBlank(),
          colors = ButtonDefaults.buttonColors(containerColor = TealPrimary),
          shape = RoundedCornerShape(10.dp),
          modifier = Modifier.testTag("btn_confirm_new_profile")
        ) {
          Text("Crear y Activar")
        }
      },
      dismissButton = {
        TextButton(onClick = { showNewProfileDialog = false }) {
          Text("Cancelar")
        }
      }
    )
  }

  // Dialog: Confirm Delete Profile
  profileToDelete?.let { profile ->
    AlertDialog(
      onDismissRequest = { profileToDelete = null },
      title = { Text("¿Eliminar perfil?") },
      text = {
        Text("Se eliminarán las preferencias y la configuración de dificultad de '${profile.name}' guardadas en Room.")
      },
      confirmButton = {
        Button(
          onClick = {
            viewModel.deleteProfile(profile.id)
            profileToDelete = null
            Toast.makeText(context, "Perfil eliminado", Toast.LENGTH_SHORT).show()
          },
          colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error),
          shape = RoundedCornerShape(10.dp)
        ) {
          Text("Eliminar")
        }
      },
      dismissButton = {
        TextButton(onClick = { profileToDelete = null }) {
          Text("Cancelar")
        }
      }
    )
  }
}

@Composable
private fun DifficultyDomainSlider(
  title: String,
  icon: String,
  level: Int,
  onLevelChange: (Int) -> Unit
) {
  val levelLabel = when (level) {
    1 -> "Nivel 1 (Inicial)"
    2 -> "Nivel 2 (Básico)"
    3 -> "Nivel 3 (Medio)"
    4 -> "Nivel 4 (Avanzado)"
    else -> "Nivel 5 (Experto)"
  }

  Surface(
    shape = RoundedCornerShape(14.dp),
    color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.35f),
    modifier = Modifier.fillMaxWidth()
  ) {
    Column(modifier = Modifier.padding(horizontal = 14.dp, vertical = 10.dp)) {
      Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
      ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
          Text(text = icon, fontSize = 18.sp)
          Spacer(modifier = Modifier.width(8.dp))
          Text(
            text = title,
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.Bold
          )
        }

        Surface(
          shape = RoundedCornerShape(6.dp),
          color = TealPrimary.copy(alpha = 0.12f)
        ) {
          Text(
            text = levelLabel,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.Bold,
            color = TealPrimary,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
          )
        }
      }

      Slider(
        value = level.toFloat(),
        onValueChange = { onLevelChange(it.toInt()) },
        valueRange = 1f..5f,
        steps = 3,
        colors = SliderDefaults.colors(
          thumbColor = TealPrimary,
          activeTrackColor = TealPrimary
        ),
        modifier = Modifier.testTag("slider_diff_${title.lowercase()}")
      )
    }
  }
}
