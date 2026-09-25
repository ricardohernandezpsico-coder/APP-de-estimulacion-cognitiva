using System.Collections.Generic;
using System.Linq;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Puerto 1:1 de <c>com.example.games.secuencia.SequenceDDAEngine</c> (Kotlin,
    /// NeuroVida Android). Misma regla anti-frustración: ante un error, primero se
    /// ralentiza la presentación (ISI sube) y solo si el ISI ya está en su techo se
    /// acorta la secuencia; simétrico para una racha de éxito. Ventana deslizante de los
    /// últimos <see cref="WindowSize"/> resultados (no un contador que se resetea cada
    /// N rondas) — ver los tests para el detalle de por qué importa esa distinción.
    ///
    /// No depende de UnityEngine a propósito: es el mismo motor puro que en Kotlin, para
    /// poder testearlo con NUnit sin levantar una escena.
    /// </summary>
    public class SequenceDDAEngine
    {
        public const int MinSpanDefault = 3;
        public const int MaxSpanDefault = 12;
        public const long MinIsiMsDefault = 300L;
        public const long MaxIsiMsDefault = 1200L;
        public const int WindowSize = 3;
        public const float SuccessThresholdDefault = 0.85f;
        public const float StrugglingThresholdDefault = 0.60f;

        private const float IsiSpeedUpFactor = 0.85f;
        private const float IsiSlowDownFactor = 1.20f;
        private const long ComfortIsiAfterSpanChangeMs = 700L;
        // Deliberadamente bajo (no un valor mayor como los puntos de fondo de Parejas
        // Ocultas): el material de referencia es explícito en evitar "animaciones
        // invasivas... que compitan por la memoria de trabajo" — acá los distractores son
        // ruido ambiente detrás de la grilla de pads, no elementos a identificar.
        private const int MaxDistractors = 4;

        private readonly int _minSpan;
        private readonly int _maxSpan;
        private readonly long _minIsiMs;
        private readonly long _maxIsiMs;
        private readonly float _successThreshold;
        private readonly float _strugglingThreshold;

        private readonly LinkedList<bool> _recentResults = new LinkedList<bool>();

        public int SpanLength { get; private set; }
        public long InterStimulusIntervalMs { get; private set; }

        public readonly struct DifficultyProfile
        {
            public readonly int SpanLength;
            public readonly long InterStimulusIntervalMs;
            public readonly int DistractorCount;

            public DifficultyProfile(int spanLength, long interStimulusIntervalMs, int distractorCount)
            {
                SpanLength = spanLength;
                InterStimulusIntervalMs = interStimulusIntervalMs;
                DistractorCount = distractorCount;
            }
        }

        public SequenceDDAEngine(
            int minSpan = MinSpanDefault,
            int maxSpan = MaxSpanDefault,
            long minIsiMs = MinIsiMsDefault,
            long maxIsiMs = MaxIsiMsDefault,
            float successThreshold = SuccessThresholdDefault,
            float strugglingThreshold = StrugglingThresholdDefault)
        {
            _minSpan = minSpan;
            _maxSpan = maxSpan;
            _minIsiMs = minIsiMs;
            _maxIsiMs = maxIsiMs;
            _successThreshold = successThreshold;
            _strugglingThreshold = strugglingThreshold;

            SpanLength = minSpan;
            InterStimulusIntervalMs = maxIsiMs;
        }

        /// <summary>
        /// Ancla el motor a un punto de partida (nivel/maestría decididos fuera de este
        /// motor) y limpia la ventana de resultados — una partida nueva no debería
        /// arrastrar la racha de la anterior.
        /// </summary>
        public DifficultyProfile Seed(int initialSpan, long initialIsiMs)
        {
            SpanLength = Clamp(initialSpan, _minSpan, _maxSpan);
            InterStimulusIntervalMs = Clamp(initialIsiMs, _minIsiMs, _maxIsiMs);
            _recentResults.Clear();
            return CurrentProfile();
        }

        /// <summary>
        /// Registra el resultado de la ronda que acaba de terminar y devuelve el perfil
        /// de dificultad para la siguiente. Con menos de <see cref="WindowSize"/>
        /// resultados todavía no hay ventana completa -> no se ajusta nada. Una vez
        /// completa, es una ventana DESLIZANTE de verdad: cada ronda siguiente reevalúa
        /// los últimos <see cref="WindowSize"/> resultados, no "cada N rondas".
        /// </summary>
        public DifficultyProfile RegisterRound(bool wasCorrect)
        {
            _recentResults.AddLast(wasCorrect);
            if (_recentResults.Count > WindowSize) _recentResults.RemoveFirst();

            if (_recentResults.Count >= WindowSize)
            {
                float accuracy = _recentResults.Count(r => r) / (float)WindowSize;
                if (accuracy >= _successThreshold) SpeedUpThenGrow();
                else if (accuracy < _strugglingThreshold) SlowDownThenShrink();
                // else: Zona de Desarrollo Próximo -- sin cambios.
            }
            return CurrentProfile();
        }

        private void SpeedUpThenGrow()
        {
            long faster = (long)(InterStimulusIntervalMs * IsiSpeedUpFactor);
            if (faster >= _minIsiMs)
            {
                InterStimulusIntervalMs = faster;
            }
            else if (SpanLength < _maxSpan)
            {
                SpanLength += 1;
                InterStimulusIntervalMs = Clamp(ComfortIsiAfterSpanChangeMs, _minIsiMs, _maxIsiMs);
            }
            else
            {
                InterStimulusIntervalMs = _minIsiMs; // techo de span Y de velocidad ya alcanzados
            }
        }

        private void SlowDownThenShrink()
        {
            long slower = (long)(InterStimulusIntervalMs * IsiSlowDownFactor);
            if (slower <= _maxIsiMs)
            {
                InterStimulusIntervalMs = slower;
            }
            else if (SpanLength > _minSpan)
            {
                SpanLength -= 1;
                InterStimulusIntervalMs = Clamp(ComfortIsiAfterSpanChangeMs, _minIsiMs, _maxIsiMs);
            }
            else
            {
                InterStimulusIntervalMs = _maxIsiMs; // piso de span Y de velocidad ya alcanzados
            }
        }

        /// <summary>
        /// Ambos ejes normalizados 0..1 y promediados. No es un controlador con estado
        /// propio: es una lectura directa de dónde están SpanLength/InterStimulusIntervalMs
        /// ahora mismo.
        /// </summary>
        private float DifficultyFraction()
        {
            float spanFraction = (SpanLength - _minSpan) / (float)System.Math.Max(_maxSpan - _minSpan, 1);
            float speedFraction = (_maxIsiMs - InterStimulusIntervalMs) / (float)System.Math.Max(_maxIsiMs - _minIsiMs, 1);
            return Clamp01((spanFraction + speedFraction) / 2f);
        }

        private int DistractorCountFor()
        {
            int raw = (int)(DifficultyFraction() * MaxDistractors);
            return Clamp(raw, 0, MaxDistractors);
        }

        private DifficultyProfile CurrentProfile() => new DifficultyProfile(SpanLength, InterStimulusIntervalMs, DistractorCountFor());

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        private static long Clamp(long value, long min, long max) => value < min ? min : value > max ? max : value;
        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
