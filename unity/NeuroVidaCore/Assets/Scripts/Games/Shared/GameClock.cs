using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Reloj de juego que se puede PAUSAR. Los juegos medían todo con el reloj real de Unity
    /// (<c>Time.unscaledTime</c>/<c>unscaledDeltaTime</c>), que no se detiene nunca: pausar la partida no
    /// congelaba los relojes de ronda ni los tiempos de respuesta. Ahora usan <see cref="Time"/> y
    /// <see cref="DeltaTime"/>: en pausa el tiempo no avanza (y al reanudar se descuenta lo que duró la pausa),
    /// <c>Time.timeScale</c> = 0 congela los <c>WaitForSeconds</c> y el audio se detiene.
    /// El fondo animado (estrellas, mundos) sigue usando el reloj real a propósito: la pausa no se ve "colgada".
    /// </summary>
    public static class GameClock
    {
        private static float s_pausedTotal;
        private static float s_pausedAt = -1f;

        public static bool Paused => s_pausedAt >= 0f;

        /// <summary>Segundos de juego (sin contar las pausas).</summary>
        public static float Time => (Paused ? s_pausedAt : UnityEngine.Time.unscaledTime) - s_pausedTotal;

        /// <summary>Duración del cuadro en segundos de juego (0 en pausa).</summary>
        public static float DeltaTime => Paused ? 0f : UnityEngine.Time.unscaledDeltaTime;

        public static void Pause()
        {
            if (Paused) return;
            s_pausedAt = UnityEngine.Time.unscaledTime;
            UnityEngine.Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        public static void Resume()
        {
            if (!Paused) return;
            s_pausedTotal += UnityEngine.Time.unscaledTime - s_pausedAt;
            s_pausedAt = -1f;
            UnityEngine.Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        /// <summary>Estado limpio para una partida nueva (cada carga de escena).</summary>
        public static void Reset()
        {
            s_pausedTotal = 0f;
            s_pausedAt = -1f;
            UnityEngine.Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
