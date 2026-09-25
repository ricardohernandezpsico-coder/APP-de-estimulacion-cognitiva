using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // HarmonicTone
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Base de los 7 juegos del DDA común (Stroop, Comparación, Cambio de Chip, Ruta del Tesoro, Series, Cálculo,
    /// Anagramas): el estado y los comportamientos que cada controlador tenía copiados idénticos. Cada juego arma
    /// su propia UI en <see cref="BuildUi"/> (se llama una vez desde <see cref="Awake"/>) y debe asignar
    /// <see cref="_flash"/> (destello de pantalla completa) y <see cref="_resultRoot"/> (panel de resultado, con un
    /// hijo "Score" de tipo Text). Secuencia y Parejas tienen estructura propia y no heredan de acá.
    /// </summary>
    public abstract class GameControllerBase : MonoBehaviour
    {
        protected SequenceInitConfig _config;
        protected AudioSource _audioSource;
        protected readonly Dictionary<float, AudioClip> _toneCache = new Dictionary<float, AudioClip>();
        protected Image _flash;
        protected RectTransform _resultRoot;

        protected virtual void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            BuildUi();
            gameObject.SetActive(false);
        }

        /// <summary>Arma toda la UI del juego por código (llamado una sola vez, en <see cref="Awake"/>).</summary>
        protected abstract void BuildUi();

        /// <summary>Tono armónico (cacheado por frecuencia y duración); respeta "sonido desactivado" de la app.</summary>
        protected void PlayTone(float hz, float seconds, float volume)
        {
            if (_config != null && _config.config != null && !_config.config.sound_enabled) return;
            float key = Mathf.Round(hz * 10f) + seconds * 100000f;
            if (!_toneCache.TryGetValue(key, out var clip))
            {
                clip = HarmonicTone.Build(hz, seconds, volume);
                _toneCache[key] = clip;
            }
            _audioSource.PlayOneShot(clip);
        }

        /// <summary>Destello de pantalla completa que se desvanece (acierto/error).</summary>
        protected IEnumerator Flash(Color color, float maxAlpha, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _flash.color = new Color(color.r, color.g, color.b, maxAlpha * (1f - k));
                yield return null;
            }
            _flash.color = new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>Entrada del panel de resultado con rebote y el puntaje contando hasta <paramref name="score"/>.</summary>
        protected IEnumerator AnimateResult(int score)
        {
            var scoreText = _resultRoot.Find("Score").GetComponent<Text>();
            float t = 0f;
            const float seconds = 0.9f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _resultRoot.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(k * 2f)));
                scoreText.text = Mathf.RoundToInt(score * UiFx.EaseOutCubic(k)).ToString();
                yield return null;
            }
            scoreText.text = score.ToString();
            _resultRoot.localScale = Vector3.one;
        }

        /// <summary>Texto centrado dentro del panel de resultado.</summary>
        protected Text AddResultText(string name, int size, Vector2 pos, Color color)
        {
            var t = MakeText(_resultRoot, name, size, TextAnchor.MiddleCenter, color, 3f, 0.4f);
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(820f, size * 1.4f);
            r.anchoredPosition = pos;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }
}
