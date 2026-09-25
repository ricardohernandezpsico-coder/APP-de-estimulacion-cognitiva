using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Píldora de estado ("Observa", "Tu turno", "¡Correcto!") con un punto de color, que
    /// reemplaza al texto plano de estado. Cada cambio de fase hace un pequeño "pop", así
    /// el jugador percibe el cambio sin tener que leer. Compartida por Secuencia Lumínica y
    /// Parejas Ocultas.
    /// </summary>
    public sealed class PhasePill
    {
        private readonly MonoBehaviour _runner;
        private readonly RectTransform _rect;
        private readonly Image _bg;
        private readonly Image _dot;
        private readonly Text _text;
        private readonly float _fontPx;
        private Coroutine _anim;

        public PhasePill(Transform parent, MonoBehaviour runner, float unitsPerDp, int fontDp = 24)
        {
            _runner = runner;
            _fontPx = fontDp * unitsPerDp;

            var go = new GameObject("PhasePill");
            go.transform.SetParent(parent, false);
            _rect = go.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);

            _bg = go.AddComponent<Image>();
            _bg.sprite = RoundedRectSprite.Get(64);
            _bg.type = Image.Type.Sliced;
            _bg.raycastTarget = false;

            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(go.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            float dotSize = _fontPx * 0.5f;
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
            dotRect.anchoredPosition = new Vector2(_fontPx * 0.95f, 0f);
            _dot = dotGo.AddComponent<Image>();
            _dot.sprite = DiscSprite.Get();
            _dot.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(_fontPx * 1.55f, 0f);
            textRect.offsetMax = new Vector2(-_fontPx * 0.6f, 0f);
            _text = textGo.AddComponent<Text>();
            _text.font = UiFonts.Bold;
            _text.fontSize = Mathf.RoundToInt(_fontPx);
            _text.alignment = TextAnchor.MiddleLeft;
            _text.color = Color.white;
            _text.raycastTarget = false;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            UiFonts.AddSoftShadow(textGo, 2f, 0.35f);
        }

        public RectTransform Rect => _rect;

        public void SetPosition(Vector2 anchoredPosition) => _rect.anchoredPosition = anchoredPosition;

        public void Set(string text, Color accent)
        {
            _text.text = text;
            _dot.color = accent;
            _bg.color = new Color(
                Mathf.Lerp(0.06f, accent.r, 0.22f),
                Mathf.Lerp(0.09f, accent.g, 0.22f),
                Mathf.Lerp(0.16f, accent.b, 0.22f),
                0.90f);

            // Textos largos (p. ej. la explicación de una serie): primero se achica un poco la letra
            // y, si aun así no cabe en el ancho máximo, se parte en dos renglones.
            const float maxWidth = 1000f;
            float scale = 1f;
            int lines = 1;
            float natural = text.Length * _fontPx * 0.56f + _fontPx * 2.6f;
            if (natural > maxWidth)
            {
                scale = Mathf.Max(0.74f, maxWidth / natural);
                float shrunk = text.Length * _fontPx * scale * 0.56f + _fontPx * scale * 2.6f;
                if (shrunk > maxWidth) lines = 2;
            }
            _text.fontSize = Mathf.RoundToInt(_fontPx * scale);
            _text.horizontalOverflow = lines == 2 ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            float width = Mathf.Clamp(text.Length * _fontPx * scale * 0.56f + _fontPx * 2.6f, _fontPx * 7f, maxWidth);
            if (lines == 2) width = maxWidth;
            _rect.sizeDelta = new Vector2(width, _fontPx * 2.15f * (lines == 2 ? 1.5f : 1f));

            if (_anim != null) _runner.StopCoroutine(_anim);
            _anim = _runner.StartCoroutine(Pop());
        }

        private IEnumerator Pop()
        {
            const float seconds = 0.22f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                _rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, UiFx.EaseOutBack(t));
                yield return null;
            }
            _rect.localScale = Vector3.one;
        }
    }
}
