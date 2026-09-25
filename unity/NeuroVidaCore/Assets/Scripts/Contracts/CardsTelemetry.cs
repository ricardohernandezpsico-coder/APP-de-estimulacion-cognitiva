using System;

namespace NeuroVida.Contracts
{
    /// <summary>
    /// Contrato JSON Unity -> Nativo al terminar la partida de "Parejas Ocultas" (Fase 2
    /// del roadmap). El contrato de ENTRADA (Nativo -> Unity) se reusa tal cual de
    /// <see cref="SequenceInitConfig"/> -- mismo esquema (user_id/game_id/config con
    /// level/base_intensity/timed/age_band/sound_enabled) fijado desde el día 1 del
    /// roadmap para no duplicarlo por juego. El de SALIDA sí es propio porque los campos
    /// no son los mismos (parejas emparejadas/intentos, no rondas/span).
    /// </summary>
    [Serializable]
    public class CardsTelemetry
    {
        public string user_id;
        public string game_id;
        public CardsSessionMetrics session_metrics;
    }

    [Serializable]
    public class CardsSessionMetrics
    {
        /// <summary>Acumulado de toda la partida (todos los tableros jugados, no solo el
        /// último) -- mismo criterio que <c>GamePlayResult.correctAnswers</c>.</summary>
        public int matched_pairs;
        public int attempts;
        public int calculated_score;
        /// <summary>Eco de <c>SequenceConfigDetails.level/.timed</c> -- mismo criterio que
        /// <c>NeuroVidaViewModel.finishActiveGame</c>, que reusa los valores de
        /// lanzamiento en vez de recalcularlos.</summary>
        public int level;
        public bool timed;
    }
}
