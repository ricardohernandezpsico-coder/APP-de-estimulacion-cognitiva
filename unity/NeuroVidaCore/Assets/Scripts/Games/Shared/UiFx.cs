using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>Efectos de UI reutilizables (chispas, onda, sacudida, resplandor de
    /// fondo) para que Secuencia Lumínica y Parejas Ocultas se sientan igual. Todo
    /// generado por código, sin assets. Las corrutinas se corren desde el controlador
    /// del juego (<c>runner.StartCoroutine(UiFx.SparkBurst(...))</c>).</summary>
    public static class UiFx
    {
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        /// <summary>Resplandor grande y suave anclado a un punto de la pantalla, para que el
        /// fondo oscuro no sea un color plano.</summary>
        public static void AddBackgroundGlow(Transform parent, Vector2 anchor, float size, Color color)
        {
            var go = new GameObject("BackgroundGlow");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.AddComponent<Image>();
            image.sprite = RadialGlowSprite.Get();
            image.raycastTarget = false;
            image.color = color;
        }

        /// <summary>Chispas que salen del centro <paramref name="center"/> (posición
        /// relativa al centro de <paramref name="parent"/>) y se desvanecen.</summary>
        public static IEnumerator SparkBurst(RectTransform parent, Vector2 center, Color color, int count, float reach, float sparkSize, float seconds = 0.55f)
        {
            var rects = new RectTransform[count];
            var images = new Image[count];
            var angles = new float[count];
            var rng = new System.Random(count * 31 + 7);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Spark");
                go.transform.SetParent(parent, false);
                var r = go.AddComponent<RectTransform>();
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                float s = sparkSize * (0.7f + (float)rng.NextDouble() * 0.6f);
                r.sizeDelta = new Vector2(s, s);
                r.anchoredPosition = center;
                var img = go.AddComponent<Image>();
                img.sprite = RadialGlowSprite.Get();
                img.raycastTarget = false;
                img.color = color;
                rects[i] = r;
                images[i] = img;
                angles[i] = i * Mathf.PI * 2f / count + (float)rng.NextDouble() * 0.5f;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float dist = reach * EaseOutCubic(t);
                for (int i = 0; i < count; i++)
                {
                    if (rects[i] == null) continue;
                    rects[i].anchoredPosition = center + new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i])) * dist;
                    images[i].color = new Color(color.r, color.g, color.b, color.a * (1f - t));
                }
                yield return null;
            }
            for (int i = 0; i < count; i++) if (rects[i] != null) Object.Destroy(rects[i].gameObject);
        }

        /// <summary>Onda circular que se expande desde <paramref name="center"/>.</summary>
        public static IEnumerator RingBurst(RectTransform parent, Vector2 center, Color color, float fromSize, float toSize, float seconds = 0.5f)
        {
            var go = new GameObject("RingBurst");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = center;
            var img = go.AddComponent<Image>();
            img.sprite = RingSprite.Get();
            img.raycastTarget = false;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float size = Mathf.Lerp(fromSize, toSize, EaseOutCubic(t));
                r.sizeDelta = new Vector2(size, size);
                img.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
                yield return null;
            }
            Object.Destroy(go);
        }

        /// <summary>Sacudida horizontal amortiguada de varios elementos a la vez; restaura
        /// sus posiciones originales al terminar.</summary>
        public static IEnumerator Shake(float amplitude, float seconds, params RectTransform[] rects)
        {
            var origins = new Vector2[rects.Length];
            for (int i = 0; i < rects.Length; i++) origins[i] = rects[i].anchoredPosition;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float offset = Mathf.Sin(t * Mathf.PI * 7f) * amplitude * (1f - t);
                for (int i = 0; i < rects.Length; i++)
                {
                    if (rects[i] != null) rects[i].anchoredPosition = origins[i] + new Vector2(offset, 0f);
                }
                yield return null;
            }
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null) rects[i].anchoredPosition = origins[i];
            }
        }
    }
}
