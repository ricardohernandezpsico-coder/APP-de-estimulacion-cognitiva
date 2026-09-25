using System;

namespace NeuroVida.Contracts
{
    /// <summary>
    /// Contrato JSON Nativo -> Unity para "Secuencia Lumínica" (Fase 0/1 del roadmap de
    /// migración, ver NeuroVida/CLAUDE.md). Fijado desde el día 1 a propósito: si no
    /// cambia entre fases, lo que se construya ahora sigue sirviendo cuando se agregue
    /// iOS/backend más adelante.
    ///
    /// Compatible con <c>UnityEngine.JsonUtility.FromJson</c> (solo campos públicos,
    /// sin colecciones genéricas anidadas) — mismo criterio simple que usa el ejemplo de
    /// arquitectura que compartió Ricardo.
    /// </summary>
    [Serializable]
    public class SequenceInitConfig
    {
        public string user_id;
        /// <summary>Id de texto tal cual <c>GameRegistry</c> del lado Kotlin (p. ej.
        /// "secuencia") -- NO un id numérico. El documento de arquitectura de referencia
        /// usaba un int genérico; se corrigió acá para no necesitar una tabla de
        /// traducción entre Unity y la app real.</summary>
        public string game_id;
        public SequenceConfigDetails config;
    }

    [Serializable]
    public class SequenceConfigDetails
    {
        /// <summary>Nivel elegido (1-5) — ancla el punto de partida del DDA, ver
        /// <c>SequenceGameContainer.seedSpan/seedIsiMs</c> en la versión Kotlin.</summary>
        public int level;
        /// <summary>Maestría acumulada entre sesiones (masteryStreak), mismo rol que
        /// <c>baseIntensity</c> en el Container Kotlin.</summary>
        public int base_intensity;
        /// <summary>Modo Reto (con reloj) vs. Precisión (sin reloj).</summary>
        public bool timed;
        /// <summary>"SENIOR" / "ADULT" / "UNDER_18" — ver <see cref="DdaUserProfileConfig"/>.
        /// Nunca la edad exacta: es el mismo rango mínimo necesario que ya captura el
        /// onboarding de la app nativa (dato de salud/personal reducido a lo imprescindible).</summary>
        public string age_band;
        public bool sound_enabled;
        /// <summary>Vibración (Ajustes > Vibración de la app). Por defecto activada: si el JSON no la trae (app
        /// vieja), <c>JsonUtility</c> conserva este valor inicial.</summary>
        public bool haptics_enabled = true;
        /// <summary>Rating guardado del DDA común (0..1) si la app tiene uno para este juego.</summary>
        public bool has_dda_rating;
        public float dda_rating;
    }
}
