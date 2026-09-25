using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// "Sensación" común de los 9 juegos: los mismos sonidos propios de NeuroVida y la misma vibración para
    /// acertar, fallar, subir de nivel, el tic del reloj y el final de la partida. Antes cada juego tocaba sus
    /// propios tonos sueltos y ninguno vibraba; ahora acertar se siente igual de bien en todos (criterio de
    /// ui-ux-pro-max: "motion/feedback consistency").
    ///
    /// Sonidos sintetizados por código (sin archivos), cálidos y cortos:
    /// - Acierto: "pling" de marimba que sube por la escala pentatónica con la racha (1ª nota do5, 8ª mi6):
    ///   la racha se OYE crecer.
    /// - Error: dos notas graves suaves que bajan (sin chicharra ni castigo).
    /// - Subir de nivel: arpegio mayor rápido. Final: acorde de campana. Tic: golpecito de madera.
    ///
    /// Vibración (regla haptic-feedback: solo confirmaciones importantes, nunca en cada toque): error (doble
    /// toque leve), racha múltiplo de 5, subir de nivel y final (golpe firme). Respeta los ajustes de sonido y
    /// vibración de la app (<see cref="SoundOn"/>/<see cref="HapticsOn"/>, los fija GameEntryPoint con la config).
    /// Todo usa el reloj real de audio: con la pausa (<c>AudioListener.pause</c>) también se calla.
    /// </summary>
    public static class GameFeel
    {
        public static bool SoundOn = true;
        public static bool HapticsOn = true;

        private const int Rate = 44100;
        // Pentatónica mayor desde do5: suena "bien" en cualquier orden, nunca desafina con la anterior.
        private static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f };

        private static AudioSource _source;
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        // ------------------------------------------------------------------ API

        /// <summary>Respuesta correcta. <paramref name="streak"/> = racha actual (1 = primer acierto).</summary>
        public static void Correct(int streak)
        {
            int step = Mathf.Clamp(streak - 1, 0, Pentatonic.Length - 1);
            Play("pling" + step, () => Marimba(Pentatonic[step], 0.26f), 0.55f);
            if (streak > 0 && streak % 5 == 0) Haptic(HapticKind.Light);
        }

        /// <summary>Respuesta incorrecta: suave, sin castigo.</summary>
        public static void Wrong()
        {
            Play("wrong", WrongClip, 0.5f);
            Haptic(HapticKind.Double);
        }

        /// <summary>Sube el nivel dentro de la partida.</summary>
        public static void LevelUp()
        {
            Play("levelup", () => Arpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.065f, 0.5f), 0.5f);
            Haptic(HapticKind.Firm);
        }

        /// <summary>Último tramo del reloj (los 5 segundos finales).</summary>
        public static void Tick() => Play("tick", TickClip, 0.45f);

        /// <summary>Fin de la partida.</summary>
        public static void Finish()
        {
            Play("finish", FinishClip, 0.55f);
            Haptic(HapticKind.Firm);
        }

        // ------------------------------------------------------------------ audio

        private static void Play(string key, System.Func<AudioClip> build, float volume)
        {
            if (!SoundOn) return;
            if (!Clips.TryGetValue(key, out var clip) || clip == null)
            {
                clip = build();
                Clips[key] = clip;
            }
            if (_source == null)
            {
                // Vive en su propio objeto: la escena se recarga entre partidas y el objeto se vuelve a crear.
                var go = new GameObject("GameFeelAudio");
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }
            _source.PlayOneShot(clip, volume);
        }

        private static AudioClip Make(string name, float seconds, System.Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            // Fundido de 4 ms al final: sin "clic" de corte.
            int fade = Mathf.Min(n, Rate / 250);
            for (int i = 0; i < fade; i++) data[n - 1 - i] *= i / (float)fade;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        /// <summary>Envolvente: ataque de <paramref name="attack"/> s y caída exponencial con constante <paramref name="tau"/>.</summary>
        private static float Env(float t, float attack, float tau) =>
            (t < attack ? t / attack : 1f) * Mathf.Exp(-Mathf.Max(0f, t - attack) / tau);

        /// <summary>Marimba: fundamental + parcial 4x (el "tok" de madera) que se apaga más rápido.</summary>
        private static AudioClip Marimba(float hz, float seconds) => Make("marimba", seconds, t =>
            0.8f * Sin(hz, t) * Env(t, 0.004f, 0.11f) + 0.22f * Sin(hz * 4f, t) * Env(t, 0.002f, 0.025f));

        private static AudioClip WrongClip() => Make("wrong", 0.34f, t =>
        {
            // "bu-bum": la3 y luego fa#3, con un poco de tercer armónico (redondo, no áspero).
            float hz = t < 0.11f ? 220f : 185f;
            float local = t < 0.11f ? t : t - 0.11f;
            float env = Env(local, 0.006f, 0.09f) * (t < 0.11f ? 0.75f : 1f);
            return 0.7f * env * (Sin(hz, t) + 0.18f * Sin(hz * 3f, t));
        });

        private static AudioClip Arpeggio(float[] notes, float gap, float seconds) => Make("arp", seconds, t =>
        {
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * gap;
                if (t < start) break;
                float lt = t - start;
                s += 0.42f * Sin(notes[i], t) * Env(lt, 0.004f, 0.14f) + 0.1f * Sin(notes[i] * 4f, t) * Env(lt, 0.002f, 0.03f);
            }
            return s;
        });

        private static AudioClip FinishClip() => Make("finish", 1.1f, t =>
        {
            // Acorde de campana (do-mi-sol-do) con un brillo que entra un poco después.
            float s = 0f;
            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.50f };
            for (int i = 0; i < chord.Length; i++)
                s += 0.26f * Sin(chord[i], t) * Env(t, 0.006f, 0.42f) + 0.06f * Sin(chord[i] * 2.76f, t) * Env(t, 0.003f, 0.12f);
            if (t > 0.12f) s += 0.12f * Sin(2093f, t) * Env(t - 0.12f, 0.004f, 0.2f);
            return s;
        });

        private static AudioClip TickClip() => Make("tick", 0.06f, t =>
            0.7f * (Sin(1250f, t) + 0.4f * Sin(2500f, t)) * Env(t, 0.001f, 0.012f));

        // ------------------------------------------------------------------ vibración

        public enum HapticKind { Light, Double, Firm }

        public static void Haptic(HapticKind kind)
        {
            if (!HapticsOn) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                EnsureVibrator();
                if (_vibrator == null) return;
                // Efectos predefinidos del sistema (API 29+): se sienten "nativos" y los ajusta el fabricante.
                // EFFECT_TICK = 2, EFFECT_DOUBLE_CLICK = 1, EFFECT_HEAVY_CLICK = 5.
                if (_sdk >= 29)
                {
                    int effect = kind == HapticKind.Light ? 2 : kind == HapticKind.Double ? 1 : 5;
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = ve.CallStatic<AndroidJavaObject>("createPredefined", effect))
                        _vibrator.Call("vibrate", e);
                }
                else if (_sdk >= 26)
                {
                    long ms = kind == HapticKind.Light ? 12 : kind == HapticKind.Double ? 22 : 35;
                    int amp = kind == HapticKind.Light ? 60 : kind == HapticKind.Double ? 110 : 200;
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = ve.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                        _vibrator.Call("vibrate", e);
                }
                else
                {
                    _vibrator.Call("vibrate", kind == HapticKind.Light ? 12L : 30L);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GameFeel] Sin vibración: " + e.Message);
                HapticsOn = false; // no insistir en cada acierto si el equipo no la soporta
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdk;

        private static void EnsureVibrator()
        {
            if (_vibrator != null) return;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                _sdk = version.GetStatic<int>("SDK_INT");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
#endif
    }
}
