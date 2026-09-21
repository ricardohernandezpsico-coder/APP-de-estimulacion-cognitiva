package com.example.ui

import androidx.compose.runtime.compositionLocalOf
import com.example.model.AgeBand

/**
 * Piloto de perfiles por edad (20-sep, ver [AgeBand]): se provee una vez en
 * `MainActivity` (mismo patrón que `LocalAppLanguage`) para que "Parejas Ocultas" pueda
 * leerlo sin agregar un parámetro nuevo a la firma compartida de los 9 juegos
 * (`(level, timed, intensity, onFinish, onQuit)`). Default `ADULT` solo para
 * previews/tests que no pasan por `MainActivity` -- en la app real, `NeuroVidaApp` nunca
 * se monta sin haber resuelto `ageBand` primero (ver el gate de onboarding).
 */
val LocalAgeBand = compositionLocalOf { AgeBand.ADULT }
