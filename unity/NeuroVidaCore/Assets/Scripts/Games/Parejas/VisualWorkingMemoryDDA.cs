using System;

namespace NeuroVida.Games.Parejas
{
    /// <summary>
    /// Puerto 1:1 de <c>com.example.games.parejas.VisualWorkingMemoryDDA</c> (Kotlin,
    /// NeuroVida Android) -- Fase 2 del roadmap de migración (ver NeuroVida/CLAUDE.md).
    /// Motor matemático del DDA multidimensional de "Parejas Ocultas": D(t) ∈ [0,1] es un
    /// índice de dificultad continuo, actualizado como un controlador incremental (no un
    /// umbral fijo) hacia el "punto dulce" de ~80% de precisión, usando precisión Y
    /// Z-score de tiempo de reacción (media/desvío histórico del usuario, Welford online).
    ///
    /// D(t) NO decide la cantidad de parejas del tablero (eso es una escalera fija de 10
    /// niveles, ver <see cref="CardsGameContract.PairCountForStage"/>) -- solo afina vista
    /// previa, densidad de distractores e interferencia perceptual dentro de cada nivel.
    ///
    /// No depende de UnityEngine a propósito: mismo motor puro que en Kotlin, testeable
    /// con NUnit sin levantar una escena.
    /// </summary>
    public class VisualWorkingMemoryDDA
    {
        public const float DefaultWeightAccuracy = 0.65f;
        public const float DefaultWeightReactionTime = 0.35f;
        private const int MinTrialsForZScore = 3;
        private const double MinStdDevMs = 150.0; // evita dividir por un desvío ~0 en los primeros ensayos
        private const float MaxZScore = 2.5f;

        private const float PreviewMaxMs = 3000f;
        public const float DefaultPreviewMinMs = 500f;

        private const int MaxDistractors = 6;
        private const float MinDistractorAlpha = 0.03f;
        private const float MaxDistractorAlpha = 0.12f;

        private readonly float _targetAccuracy;
        private readonly float _stepSize;
        private readonly float _weightAccuracy;
        private readonly float _weightReactionTime;
        private readonly float _previewFloorMs;

        // --- Media/desvío histórico de RT, calculado en línea (Welford) ---
        private int _trialCount;
        private double _rtMean;
        private double _rtM2;

        public float DifficultyIndex { get; private set; }

        public readonly struct TrialResult
        {
            public readonly bool WasCorrect;
            public readonly bool WasOmission;
            public readonly long ReactionTimeMs;

            public TrialResult(bool wasCorrect, bool wasOmission, long reactionTimeMs)
            {
                WasCorrect = wasCorrect;
                WasOmission = wasOmission;
                ReactionTimeMs = reactionTimeMs;
            }
        }

        public readonly struct DifficultyProfile
        {
            public readonly float DifficultyIndex;
            public readonly int GridColumns;
            public readonly int GridRows;
            public readonly int PairCount;
            public readonly long PreviewExposureMs;
            public readonly int DistractorCount;
            public readonly float DistractorOpacity;
            public readonly int InterferenceLevel;

            public DifficultyProfile(float difficultyIndex, int gridColumns, int gridRows, int pairCount, long previewExposureMs, int distractorCount, float distractorOpacity, int interferenceLevel)
            {
                DifficultyIndex = difficultyIndex;
                GridColumns = gridColumns;
                GridRows = gridRows;
                PairCount = pairCount;
                PreviewExposureMs = previewExposureMs;
                DistractorCount = distractorCount;
                DistractorOpacity = distractorOpacity;
                InterferenceLevel = interferenceLevel;
            }
        }

        public VisualWorkingMemoryDDA(
            float targetAccuracy = 0.8f,
            float stepSize = 0.08f,
            float weightAccuracy = DefaultWeightAccuracy,
            float weightReactionTime = DefaultWeightReactionTime,
            float previewFloorMs = DefaultPreviewMinMs)
        {
            _targetAccuracy = targetAccuracy;
            _stepSize = stepSize;
            _weightAccuracy = weightAccuracy;
            _weightReactionTime = weightReactionTime;
            _previewFloorMs = previewFloorMs;
        }

        /// <summary>Registra el resultado de un ensayo y devuelve el perfil resultante.
        /// D(t) = D(t-1) + stepSize * (wAccuracy*errorP + wReactionTime*(-z)).</summary>
        public DifficultyProfile RegisterTrial(TrialResult result, int pairCount)
        {
            float z = UpdateAndComputeReactionZScore(result.ReactionTimeMs, result.WasOmission);
            float observedAccuracy = (result.WasCorrect && !result.WasOmission) ? 1f : 0f;
            float errorP = observedAccuracy - _targetAccuracy;

            float weightedDelta = (_weightAccuracy * errorP) + (_weightReactionTime * -z);
            DifficultyIndex = Clamp01(DifficultyIndex + _stepSize * weightedDelta);

            return BuildProfile(pairCount);
        }

        /// <summary>Perfil inicial -- ancla la partida al nivel/maestría elegidos fuera de
        /// este motor. <paramref name="pairCount"/> lo decide el nivel (1..10), no D(t).</summary>
        public DifficultyProfile InitialProfile(float seedDifficulty, int pairCount)
        {
            DifficultyIndex = Clamp01(seedDifficulty);
            return BuildProfile(pairCount);
        }

        /// <summary>Perfil ACTUAL sin tocar difficultyIndex ni el historial de RT -- para
        /// armar el tablero del siguiente nivel dentro de la misma partida.</summary>
        public DifficultyProfile CurrentProfile(int pairCount) => BuildProfile(pairCount);

        private float UpdateAndComputeReactionZScore(long reactionTimeMs, bool wasOmission)
        {
            // Una omisión no tiene un RT real que promediar -- no contamina la media
            // histórica, pero SÍ cuenta como el peor RT posible para este ensayo puntual.
            if (wasOmission) return MaxZScore;

            _trialCount++;
            double delta = reactionTimeMs - _rtMean;
            _rtMean += delta / _trialCount;
            double delta2 = reactionTimeMs - _rtMean;
            _rtM2 += delta * delta2;

            if (_trialCount < MinTrialsForZScore) return 0f; // sin historial suficiente -> neutral

            double variance = _rtM2 / (_trialCount - 1);
            double stdDev = Math.Max(Math.Sqrt(variance), MinStdDevMs);
            float z = (float)((reactionTimeMs - _rtMean) / stdDev);
            return Math.Max(-MaxZScore, Math.Min(MaxZScore, z));
        }

        private static int ColumnsFor(int pairCount)
        {
            if (pairCount <= 2) return 2;
            if (pairCount <= 4) return 2;
            if (pairCount <= 6) return 3;
            if (pairCount <= 9) return 4;
            if (pairCount <= 12) return 4;
            if (pairCount <= 15) return 5;
            return 6;
        }

        private static (int columns, int rows) GridDimensionsFor(int pairCount)
        {
            int columns = ColumnsFor(pairCount);
            int rows = (int)Math.Ceiling((pairCount * 2) / (double)columns);
            return (columns, rows);
        }

        private long PreviewExposureMsFor(float d)
        {
            float ms = PreviewMaxMs - d * (PreviewMaxMs - _previewFloorMs);
            return (long)ms;
        }

        private static int DistractorCountFor(float d) => (int)(d * MaxDistractors);

        private static float DistractorOpacityFor(float d) => MinDistractorAlpha + d * (MaxDistractorAlpha - MinDistractorAlpha);

        private static int InterferenceLevelFor(float d) => Math.Max(0, Math.Min(3, (int)(d * 3)));

        private DifficultyProfile BuildProfile(int pairCount)
        {
            var (columns, rows) = GridDimensionsFor(pairCount);
            return new DifficultyProfile(
                DifficultyIndex,
                columns,
                rows,
                pairCount,
                PreviewExposureMsFor(DifficultyIndex),
                DistractorCountFor(DifficultyIndex),
                DistractorOpacityFor(DifficultyIndex),
                InterferenceLevelFor(DifficultyIndex));
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
