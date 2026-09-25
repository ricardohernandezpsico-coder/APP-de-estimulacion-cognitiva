using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>Fila de puntos de avance (uno por ensayo): gris pendiente, verde acierto,
    /// rojo error, con un pequeño "pop" al marcarse. Reemplaza a las barras de progreso.</summary>
    public sealed class ProgressDots
    {
        public static readonly Color Pending = new Color(1f, 1f, 1f, 0.18f);
        public static readonly Color Current = new Color(1f, 1f, 1f, 0.55f);
        public static readonly Color Good = new Color(0x22 / 255f, 0xC5 / 255f, 0x5E / 255f);
        public static readonly Color Bad = new Color(0xEF / 255f, 0x44 / 255f, 0x44 / 255f);

        private readonly MonoBehaviour _runner;
        private readonly RectTransform _rect;
        private readonly List<Image> _dots = new List<Image>();

        public RectTransform Rect => _rect;

        public ProgressDots(Transform parent, MonoBehaviour runner, float unitsPerDp, int count, float dotDp = 15f)
        {
            _runner = runner;
            var go = new GameObject("ProgressDots");
            go.transform.SetParent(parent, false);
            _rect = go.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.sizeDelta = new Vector2(900f, dotDp * 1.6f * unitsPerDp);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = dotDp * 0.75f * unitsPerDp;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float size = dotDp * unitsPerDp;
            for (int i = 0; i < count; i++)
            {
                var dotGo = new GameObject("Dot_" + i);
                dotGo.transform.SetParent(_rect, false);
                var r = dotGo.AddComponent<RectTransform>();
                r.sizeDelta = new Vector2(size, size);
                var image = dotGo.AddComponent<Image>();
                image.sprite = DiscSprite.Get();
                image.raycastTarget = false;
                image.color = Pending;
                _dots.Add(image);
            }
        }

        public void Reset()
        {
            foreach (var d in _dots) d.color = Pending;
        }

        public void MarkCurrent(int index)
        {
            if (index >= 0 && index < _dots.Count) _dots[index].color = Current;
        }

        public void Mark(int index, bool correct)
        {
            if (index < 0 || index >= _dots.Count) return;
            _dots[index].color = correct ? Good : Bad;
            _runner.StartCoroutine(Pop(_dots[index].rectTransform));
        }

        private static IEnumerator Pop(RectTransform rect)
        {
            const float seconds = 0.26f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (rect == null) yield break;
                elapsed += GameClock.DeltaTime;
                rect.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, UiFx.EaseOutCubic(Mathf.Clamp01(elapsed / seconds)));
                yield return null;
            }
            if (rect != null) rect.localScale = Vector3.one;
        }
    }
}
