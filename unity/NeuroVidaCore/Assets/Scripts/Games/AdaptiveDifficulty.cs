using System;
using NeuroVida.Contracts;

namespace NeuroVida.Games
{
    public enum DdaChange { None, Up, Down }

    /// <summary>
    /// Motor de dificultad adaptativa COMÚN (DDA) para los juegos con escalera de niveles
    /// (Stroop, Comparación, Cambio de Chip, Ruta del Tesoro, Series, Cálculo, Anagramas).
    /// Detalle y referencias en <c>NeuroVida/docs/DDA-comun.md</c>.
    ///
    /// Idea central: un "rating" continuo sobre la escalera del juego (1 = lo más fácil,
    /// MaxLevel = lo más difícil) que se mueve con una regla <b>up-down ponderada</b>
    /// (Kaernbach, 1991): sube un paso chico <c>δ</c> por acierto y baja <c>δ·p/(1-p)</c> por error,
    /// lo que hace converger la tasa de aciertos a <c>p</c> (el "punto dulce": ~80% en adultos, ~85% en
    /// adultos mayores, ver Wilson et al., 2019 y la literatura de aprendizaje sin error).
    /// Sobre eso se agregan:
    /// <list type="bullet">
    /// <item>Modulación por tiempo de reacción (Z-score contra la propia historia del usuario, como en
    /// Parejas): un acierto rápido sube un poco más, uno lento un poco menos. El peso viene del perfil
    /// de edad (<see cref="DdaUserProfileConfig.WeightReactionTime"/>): menor en mayores (Salthouse, 1996).</item>
    /// <item>Calibración inicial: los primeros ensayos mueven el rating 1.5 veces más (rating "provisorio").</item>
    /// <item>Calentamiento: los primeros ensayos se presentan un nivel por debajo.</item>
    /// <item>Red anti-frustración: dos errores seguidos bajan un poco extra y marcan <see cref="Struggling"/>
    /// (los juegos muestran un aviso amable).</item>
    /// <item>Red anti-aburrimiento: cada 6 aciertos seguidos suma un empujón extra.</item>
    /// </list>
    /// Sin dependencias de UnityEngine: testeable con NUnit.
    /// </summary>
    public sealed class AdaptiveDifficulty
    {
        public const int WarmupTrials = 2;
        private const int ProvisionalTrials = 6;
        private const float ProvisionalBoost = 1.5f;
        private const float StruggleExtraDrop = 0.15f;
        private const int BoredomStreak = 6;
        private const float BoredomBonus = 0.20f;
        private const float MaxDropPerError = 1.0f;
        private const int MinTrialsForZScore = 3;
        private const double MinStdDevMs = 150.0;
        private const float MaxZScore = 2.5f;

        public readonly int MaxLevel;
        public readonly float TargetAccuracy;
        private readonly float _stepUp;
        private readonly float _weightReaction;
        private readonly bool _useReaction;

        private int _consecutiveOk, _consecutiveErr;
        private int _rtCount;
        private double _rtMean, _rtM2;

        public float Rating { get; private set; }
        public int Trials { get; private set; }
        public int Correct { get; private set; }
        public int PeakLevel { get; private set; }
        /// <summary>true tras 2 o más errores seguidos (se apaga al acertar).</summary>
        public bool Struggling => _consecutiveErr >= 2;

        /// <summary>Nivel "real" del usuario ahora (1..MaxLevel).</summary>
        public int Level => Math.Max(1, Math.Min(MaxLevel, (int)Math.Floor(Rating)));

        /// <summary>Nivel a presentar: durante el calentamiento, uno por debajo.</summary>
        public int PresentedLevel => Trials < WarmupTrials ? Math.Max(1, Level - 1) : Level;

        /// <summary>Avance dentro del nivel actual (0..1) para parámetros continuos.</summary>
        public float Fraction => Rating - (float)Math.Floor(Rating);

        /// <summary>Rating normalizado 0..1 (para guardarlo entre sesiones y compararlo entre juegos).</summary>
        public float RatingNormalized => Math.Max(0f, Math.Min(1f, (Rating - 1f) / MaxLevel));

        public float Accuracy => Trials == 0 ? 0f : (float)Correct / Trials;

        /// <param name="stepUp">Niveles que sube por acierto (0.15 = ~7 aciertos por nivel). Juegos con pocos
        /// ensayos por sesión usan pasos mayores.</param>
        /// <param name="targetOverride">Tasa de aciertos objetivo propia del juego (p. ej. 0.70 en Ruta del
        /// Tesoro, donde perder la ruta cuesta una vida); null = la del perfil de edad.</param>
        /// <param name="useReaction">false en modos sin reloj (Precisión): pensar despacio no se penaliza.</param>
        public AdaptiveDifficulty(int maxLevel, AgeBand ageBand, float startRating, float stepUp = 0.15f,
            float? targetOverride = null, bool useReaction = true)
        {
            MaxLevel = Math.Max(2, maxLevel);
            var profile = DdaUserProfileConfig.For(ageBand);
            TargetAccuracy = targetOverride ?? TargetFor(ageBand);
            // Adultos mayores: subida un poco más lenta (menos frustración; aprendizaje "casi sin error").
            _stepUp = stepUp * (ageBand == AgeBand.Senior ? 0.85f : ageBand == AgeBand.Under18 ? 1.1f : 1f);
            _weightReaction = profile.WeightReactionTime;
            _useReaction = useReaction;
            Rating = Clamp(startRating);
            PeakLevel = Level;
        }

        /// <summary>Tasa de aciertos objetivo por edad.</summary>
        public static float TargetFor(AgeBand ageBand) => ageBand == AgeBand.Senior ? 0.85f : 0.80f;

        /// <summary>Rating inicial: el nivel elegido (1-5) se reparte sobre toda la escalera del juego y la
        /// maestría acumulada entre sesiones (<c>base_intensity</c>) suma un empujón acotado.</summary>
        public static float StartRating(int chosenLevel1To5, int maxLevel, int baseIntensity)
        {
            int lvl = Math.Max(1, Math.Min(5, chosenLevel1To5));
            float start = 1f + (lvl - 1) * (maxLevel - 1) / 4f;
            start += Math.Min(Math.Max(0, baseIntensity) * 0.04f, 1.2f);
            return Math.Max(1f, Math.Min(maxLevel + 0.99f, start));
        }

        /// <summary>Rating inicial de la partida: si la app guardó el rating del juego, se continúa desde ahí
        /// (es el nivel real del usuario); si no, se reparte el nivel elegido sobre la escalera.</summary>
        public static float StartRating(SequenceConfigDetails config, int maxLevel)
        {
            if (config.has_dda_rating)
                return Math.Max(1f, Math.Min(maxLevel + 0.99f, 1f + Math.Max(0f, Math.Min(1f, config.dda_rating)) * maxLevel));
            return StartRating(config.level, maxLevel, config.base_intensity);
        }

        /// <summary>Registra un ensayo. <paramref name="reactionMs"/> &lt; 0 = sin dato de tiempo.
        /// Devuelve si el nivel real subió o bajó.</summary>
        public DdaChange Register(bool correct, float reactionMs = -1f)
        {
            int before = Level;
            Trials++;
            float z = UpdateReactionZ(reactionMs);
            float provisional = Trials <= ProvisionalTrials ? ProvisionalBoost : 1f;
            float delta;

            if (correct)
            {
                Correct++;
                _consecutiveOk++;
                _consecutiveErr = 0;
                float modulation = 1f;
                if (_useReaction && reactionMs >= 0f)
                    modulation = 1f + _weightReaction * Math.Max(-1f, Math.Min(1f, -z));
                delta = _stepUp * provisional * modulation;
                if (_consecutiveOk % BoredomStreak == 0) delta += BoredomBonus;
            }
            else
            {
                _consecutiveOk = 0;
                _consecutiveErr++;
                float ratio = TargetAccuracy / (1f - TargetAccuracy);
                delta = -Math.Min(_stepUp * ratio, MaxDropPerError) * provisional;
                if (_consecutiveErr >= 2) delta -= StruggleExtraDrop;
            }

            Rating = Clamp(Rating + delta);
            PeakLevel = Math.Max(PeakLevel, Level);
            int after = Level;
            return after > before ? DdaChange.Up : after < before ? DdaChange.Down : DdaChange.None;
        }

        private float Clamp(float rating) => Math.Max(1f, Math.Min(MaxLevel + 0.99f, rating));

        // Z-score del tiempo de reacción contra la historia del propio usuario (Welford en línea).
        private float UpdateReactionZ(float reactionMs)
        {
            if (reactionMs < 0f) return 0f;
            _rtCount++;
            double d = reactionMs - _rtMean;
            _rtMean += d / _rtCount;
            _rtM2 += d * (reactionMs - _rtMean);
            if (_rtCount < MinTrialsForZScore) return 0f;
            double sd = Math.Max(Math.Sqrt(_rtM2 / (_rtCount - 1)), MinStdDevMs);
            float z = (float)((reactionMs - _rtMean) / sd);
            return Math.Max(-MaxZScore, Math.Min(MaxZScore, z));
        }
    }
}
