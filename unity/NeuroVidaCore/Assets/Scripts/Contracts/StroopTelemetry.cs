using System;

namespace NeuroVida.Contracts
{
    /// <summary>
    /// Contrato JSON Unity -> Nativo al terminar "Tinta o Palabra" (Stroop). El contrato de
    /// ENTRADA reusa <see cref="SequenceInitConfig"/> como el resto de los juegos.
    /// </summary>
    [Serializable]
    public class StroopTelemetry
    {
        public string user_id;
        public string game_id;
        public StroopSessionMetrics session_metrics;
    }

    [Serializable]
    public class StroopSessionMetrics
    {
        public int correct_trials;
        public int total_trials;
        public int calculated_score;
        /// <summary>Tiempo medio de respuesta de los ensayos respondidos, en milisegundos.</summary>
        public int average_response_time_ms;
        public int level;
        public bool timed;
        /// <summary>Rating final del DDA común normalizado 0..1 (0 = lo más fácil de la escalera del juego).
        /// Para que la app lo guarde entre sesiones (hoy solo se registra).</summary>
        public float end_rating;
        /// <summary>Nivel más alto alcanzado en la partida.</summary>
        public int peak_level;
    }
}
