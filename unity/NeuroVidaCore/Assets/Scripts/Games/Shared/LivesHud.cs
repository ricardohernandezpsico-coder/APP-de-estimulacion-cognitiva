using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Fila de vidas del HUD (corazones ilustrados sobre una píldora translúcida),
    /// compartida por Secuencia Lumínica y Parejas Ocultas para que se vean y se animen
    /// igual. Al perder una vida el corazón "salta" (crece con un temblor), se apaga a
    /// gris con grieta a mitad de la animación y vuelve a su tamaño.
    /// La primera llamada a <see cref="SetLives"/> solo fija el estado (sin animar).
    /// </summary>
    public sealed class LivesHud
    {
        private readonly MonoBehaviour _runner;
        private readonly Image[] _hearts;
        private readonly bool[] _isFull;
        private bool _initialized;

        /// <param name="alignRight">true = anclada arriba a la derecha (el corazón 0 queda
        /// más a la derecha); false = arriba a la izquierda (el 0 queda más a la izquierda).</param>
        public LivesHud(Transform parent, MonoBehaviour runner, int maxLives, bool alignRight,
            float marginU, float topOffsetU, float heartSizeU)
        {
            _runner = runner;
            _hearts = new Image[maxLives];
            _isFull = new bool[maxLives];

            float gap = heartSizeU * 0.16f;
            float pad = heartSizeU * 0.22f;
            float width = maxLives * heartSizeU + (maxLives - 1) * gap + pad * 2f;
            float height = heartSizeU + pad * 2f;

            var pillGo = new GameObject("LivesHud");
            pillGo.transform.SetParent(parent, false);
            var pillRect = pillGo.AddComponent<RectTransform>();
            var anchor = new Vector2(alignRight ? 1f : 0f, 1f);
            pillRect.anchorMin = anchor;
            pillRect.anchorMax = anchor;
            pillRect.pivot = anchor;
            pillRect.sizeDelta = new Vector2(width, height);
            pillRect.anchoredPosition = new Vector2(alignRight ? -marginU : marginU, topOffsetU);
            var pill = pillGo.AddComponent<Image>();
            pill.sprite = RoundedRectSprite.Get(28);
            pill.type = Image.Type.Sliced;
            pill.color = new Color(0f, 0f, 0f, 0.28f);
            pill.raycastTarget = false;

            for (int i = 0; i < maxLives; i++)
            {
                var heartGo = new GameObject($"Heart_{i}");
                heartGo.transform.SetParent(pillGo.transform, false);
                var rect = heartGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(heartSizeU, heartSizeU);
                int slot = alignRight ? (maxLives - 1 - i) : i;
                rect.anchoredPosition = new Vector2(pad + heartSizeU * 0.5f + slot * (heartSizeU + gap), 0f);
                var image = heartGo.AddComponent<Image>();
                image.sprite = HeartSprite.GetFull();
                image.raycastTarget = false;
                _hearts[i] = image;
                _isFull[i] = true;
            }
        }

        public void SetLives(int lives)
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                bool full = i < lives;
                bool justLost = _initialized && _isFull[i] && !full;
                _isFull[i] = full;
                if (justLost)
                {
                    _runner.StartCoroutine(AnimateLost(_hearts[i]));
                }
                else
                {
                    _hearts[i].sprite = full ? HeartSprite.GetFull() : HeartSprite.GetLost();
                    _hearts[i].rectTransform.localScale = Vector3.one;
                    _hearts[i].rectTransform.localRotation = Quaternion.identity;
                }
            }
            _initialized = true;
        }

        private static IEnumerator AnimateLost(Image heart)
        {
            var rect = heart.rectTransform;
            const float grow = 0.16f;
            const float settle = 0.34f;

            float elapsed = 0f;
            while (elapsed < grow)
            {
                elapsed += GameClock.DeltaTime;
                float t = Mathf.Clamp01(elapsed / grow);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.45f, t);
                yield return null;
            }

            heart.sprite = HeartSprite.GetLost();

            elapsed = 0f;
            while (elapsed < settle)
            {
                elapsed += GameClock.DeltaTime;
                float t = Mathf.Clamp01(elapsed / settle);
                rect.localScale = Vector3.one * Mathf.Lerp(1.45f, 1f, t * t);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 5f) * 14f * (1f - t));
                yield return null;
            }
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
