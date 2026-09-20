package com.example.model

enum class AppLanguage(
  val code: String,
  val displayName: String,
  val flagEmoji: String
) {
  SPANISH("es", "Español", "🇪🇸"),
  ENGLISH("en", "English", "🇺🇸"),
  FRENCH("fr", "Français", "🇫🇷"),
  GERMAN("de", "Deutsch", "🇩🇪"),
  PORTUGUESE("pt", "Português", "🇧🇷");

  companion object {
    fun fromCode(code: String): AppLanguage {
      return entries.find { it.code.equals(code, ignoreCase = true) } ?: SPANISH
    }
  }
}
