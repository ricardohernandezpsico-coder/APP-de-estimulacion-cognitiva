using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Utilidades de UI por código que los 7 juegos del DDA común (Stroop, Comparación, Cambio de Chip, Ruta del
    /// Tesoro, Series, Cálculo, Anagramas) tenían copiadas idénticas en cada controlador. Se usan con
    /// <c>using static NeuroVida.Games.Shared.UiKit;</c>, así las llamadas siguen igual (<c>Stretch(rect)</c>,
    /// <c>StartCoroutine(PopRect(...))</c>). Cambiar algo acá (p. ej. la tipografía de <see cref="MakeText"/>) lo
    /// cambia en los 7 juegos a la vez. Secuencia y Parejas tienen versiones propias distintas y no las usan.
    /// </summary>
    public static class UiKit
    {
        /// <summary>Ajusta <paramref name="target"/> al Safe Area de la pantalla (notch, barra de gestos).</summary>

        public static void ApplySafeArea(RectTransform target)
        {
            Rect safeArea = Screen.safeArea;
            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        /// <summary>Anclas 0..1 y offsets en cero: el rect ocupa todo su padre.</summary>

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Texto de UI con la tipografía de los juegos (<see cref="UiFonts.Bold"/>) y sombra suave opcional; ocupa todo su padre.</summary>

        public static Text MakeText(Transform parent, string name, int fontPx, TextAnchor align, Color color, float shadowDistance, float shadowAlpha)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            Stretch(rect);
            var text = go.AddComponent<Text>();
            text.font = UiFonts.Bold;
            text.fontSize = fontPx;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            if (shadowAlpha > 0f) UiFonts.AddSoftShadow(go, shadowDistance, shadowAlpha);
            return text;
        }

        /// <summary>Achica la fuente hasta <paramref name="minSize"/> si el texto no entra (tope = tamaño actual).</summary>

        public static void BestFit(Text text, int minSize)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
        }

        /// <summary>Coloca un texto anclado arriba, con márgenes laterales y una altura fija.</summary>

        public static void PlaceTopText(Text text, float left, float right, float topY, float height)
        {
            var r = text.rectTransform;
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.offsetMin = new Vector2(left, topY - height);
            r.offsetMax = new Vector2(-right, topY);
        }

        /// <summary>"Pop": el rect arranca en escala <paramref name="peak"/> y vuelve a 1.</summary>

        public static IEnumerator PopRect(RectTransform rect, float peak, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (rect == null) yield break;
                t += Time.unscaledDeltaTime;
                rect.localScale = Vector3.one * Mathf.Lerp(peak, 1f, UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }

        /// <summary>Aparece creciendo desde 0 con rebote.</summary>

        public static IEnumerator PopIn(RectTransform rect, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (rect == null) yield break;
                t += Time.unscaledDeltaTime;
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }

        /// <summary>Posición de <paramref name="target"/> en el espacio local de <paramref name="space"/>.</summary>

        public static Vector2 LocalIn(RectTransform space, RectTransform target)
        {
            Vector3 local = space.InverseTransformPoint(target.position);
            return new Vector2(local.x, local.y);
        }
    }
}
