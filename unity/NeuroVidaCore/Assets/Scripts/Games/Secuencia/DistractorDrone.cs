using UnityEngine;

namespace NeuroVida.Games.Secuencia
{
    /// <summary>
    /// Distractor auditivo pendiente de la tabla de 16 niveles ("tono de fondo" niveles
    /// 12-13, "interferencia auditiva" nivel 14+, <c>SequenceLevelConfig.HasAudioDistractor</c>)
    /// -- un zumbido grave y sostenido, deliberadamente NO afinado con la escala
    /// pentatónica de las fichas (<see cref="TilePalette"/>) para que se perciba como
    /// ruido de fondo y no como parte de la melodía, a volumen bajo para no tapar los
    /// tonos reales que sí hay que memorizar.
    ///
    /// Se genera una vez (loop de ~2s, longitud = número entero de períodos exactos de la
    /// onda para que no haga click al repetirse) y se reproduce/detiene en un
    /// <c>AudioSource</c> aparte según <c>SequenceLevelConfig.HasAudioDistractor</c> del
    /// nivel actual (ver <c>SequenceGameController.SetAudioDistractor</c>).
    /// </summary>
    public static class DistractorDrone
    {
        private const float DroneHz = 146.83f; // Re3 -- fuera de la escala pentatónica de Do usada en los tiles
        private const float Volume = 0.05f;
        private static AudioClip _cached;

        public static AudioClip Get()
        {
            if (_cached != null) return _cached;

            const int sampleRate = 44100;
            int periodsPerLoop = Mathf.RoundToInt(DroneHz * 2f); // ~2 segundos de loop
            int sampleCount = Mathf.RoundToInt(periodsPerLoop * sampleRate / DroneHz);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float tremolo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.3f * t); // respiración lenta, no un zumbido estático
                samples[i] = Mathf.Sin(2f * Mathf.PI * DroneHz * t) * Volume * tremolo;
            }

            _cached = AudioClip.Create("distractor_drone", sampleCount, 1, sampleRate, false);
            _cached.SetData(samples, 0);
            return _cached;
        }
    }
}
