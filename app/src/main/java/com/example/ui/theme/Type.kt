package com.example.ui.theme

import androidx.compose.material3.Typography
import androidx.compose.ui.text.ExperimentalTextApi
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontVariation
import androidx.compose.ui.text.font.FontWeight
import com.example.R

/**
 * Tipografia de NeuroVida: Fredoka (OFL), redondeada, la misma familia visual de los juegos. Es una fuente
 * variable: cada peso pide su variacion (en Android < 8 cae al peso por defecto). Fredoka llega a 700, asi que
 * ExtraBold/Black se mapean a 700.
 */
@OptIn(ExperimentalTextApi::class)
private fun fredoka(weight: FontWeight, variation: Int) = Font(
  resId = R.font.fredoka,
  weight = weight,
  variationSettings = FontVariation.Settings(FontVariation.weight(variation))
)

@OptIn(ExperimentalTextApi::class)
val FredokaFamily = FontFamily(
  fredoka(FontWeight.Light, 300),
  fredoka(FontWeight.Normal, 400),
  fredoka(FontWeight.Medium, 500),
  fredoka(FontWeight.SemiBold, 600),
  fredoka(FontWeight.Bold, 700),
  fredoka(FontWeight.ExtraBold, 700),
  fredoka(FontWeight.Black, 700)
)

private val Base = Typography()

val Typography = Typography(
  displayLarge = Base.displayLarge.copy(fontFamily = FredokaFamily),
  displayMedium = Base.displayMedium.copy(fontFamily = FredokaFamily),
  displaySmall = Base.displaySmall.copy(fontFamily = FredokaFamily),
  headlineLarge = Base.headlineLarge.copy(fontFamily = FredokaFamily),
  headlineMedium = Base.headlineMedium.copy(fontFamily = FredokaFamily),
  headlineSmall = Base.headlineSmall.copy(fontFamily = FredokaFamily),
  titleLarge = Base.titleLarge.copy(fontFamily = FredokaFamily),
  titleMedium = Base.titleMedium.copy(fontFamily = FredokaFamily),
  titleSmall = Base.titleSmall.copy(fontFamily = FredokaFamily),
  bodyLarge = Base.bodyLarge.copy(fontFamily = FredokaFamily),
  bodyMedium = Base.bodyMedium.copy(fontFamily = FredokaFamily),
  bodySmall = Base.bodySmall.copy(fontFamily = FredokaFamily),
  labelLarge = Base.labelLarge.copy(fontFamily = FredokaFamily),
  labelMedium = Base.labelMedium.copy(fontFamily = FredokaFamily),
  labelSmall = Base.labelSmall.copy(fontFamily = FredokaFamily)
)
