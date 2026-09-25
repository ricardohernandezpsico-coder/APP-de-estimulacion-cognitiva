using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Síntesis de tono armónico y cálido (rediseño 22-sep, reemplaza el tono seno puro
    /// "tipo arcade" que reportó Ricardo jugando en el dispositivo). En vez de una sola
    /// onda seno, suma la fundamental con 2 armónicos atenuados (2x y 3x la frecuencia,
    /// mismo principio que un tono de campana/xilófono simple) para un timbre más
    /// acústico, y aplica una envolvente ADSR simplificada: ataque rápido (15ms) con
    /// curva de coseno elevado (no clic), decaimiento exponencial (200ms) en vez de
    /// sostener el volumen -- son tonos de toque puntual, no notas sostenidas, así que
    /// no hace falta una fase de "sustain" real.
    ///
    /// La reverb corta pedida en el spec NO se sintetiza acá (mezclar una cola de reverb
    /// a mano en una sola AudioClip corta suena artificial) -- se aplica con
    /// <see cref="UnityEngine.AudioReverbFilter"/>, un componente real de Unity, sobre el
    /// AudioSource que reproduce el clip (ver <c>SequenceGameController.BuildUi</c>).
    /// </summary>
    public static class HarmonicTone
    {
        private static readonly float[] HarmonicMultipliers = { 1f, 2f, 3f };
        private static readonly float[] HarmonicWeights = { 1f, 0.28f, 0.12f };

        public static AudioClip Build(float baseHz, float durationSeconds = 0.5f, float volume = 0.22f, float attackSeconds = 0.015f, float decaySeconds = 0.2f)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
            var samples = new float[sampleCount];
            int attackSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * attackSeconds));

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;

                float envelope;
                if (i < attackSamples)
                {
                    float x = i / (float)attackSamples;
                    envelope = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * x); // ataque suave, sin clic
                }
                else
                {
                    float sinceAttack = (i - attackSamples) / (float)sampleRate;
                    envelope = Mathf.Exp(-sinceAttack / decaySeconds); // decaimiento armónico
                }

                float sample = 0f;
                for (int h = 0; h < HarmonicMultipliers.Length; h++)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * baseHz * HarmonicMultipliers[h] * t) * HarmonicWeights[h];
                }
                samples[i] = sample * volume * envelope;
            }

            var clip = AudioClip.Create($"tone_{baseHz:0}hz", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
