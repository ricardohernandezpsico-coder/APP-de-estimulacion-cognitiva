using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Aviso flotante no bloqueante ("Nivel 4 · ¡Bien hecho!") que baja desde arriba,
    /// se sostiene un momento y sube desvaneciéndose. Reemplaza a las pantallas completas
    /// entre niveles (Ricardo, 23-sep: "van apareciendo pantallazos entre los niveles, falta
    /// fluidez"): el tablero sigue visible y el juego no se detiene por esto.
    /// </summary>
    public sealed class Toast
    {
        private readonly MonoBehaviour _runner;
        private readonly RectTransform _rect;
        private readonly CanvasGroup _group;
        private readonly Image _bg;
        private readonly Image _dot;
        private readonly Text _title;
        private readonly Text _subtitle;
        private readonly float _u;
        private Vector2 _basePosition;
        private Coroutine _anim;

        public Toast(Transform parent, MonoBehaviour runner, float unitsPerDp)
        {
            _runner = runner;
            _u = unitsPerDp;

            var go = new GameObject("Toast");
            go.transform.SetParent(parent, false);
            _rect = go.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.5f, 1f);
            _rect.anchorMax = new Vector2(0.5f, 1f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.sizeDelta = new Vector2(300f * _u, 84f * _u);
            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _bg = go.AddComponent<Image>();
            _bg.sprite = RoundedRectSprite.Get(64);
            _bg.type = Image.Type.Sliced;
            _bg.raycastTarget = false;
            NeuroStyle.ClayFrame(_bg, 4f, 8f);

            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(go.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(30f * _u, 30f * _u);
            dotRect.anchoredPosition = new Vector2(44f * _u, 0f);
            _dot = dotGo.AddComponent<Image>();
            _dot.sprite = DiscSprite.Get();
            _dot.raycastTarget = false;

            _title = NewText("Title", go.transform, 24, new Vector2(0f, 0.5f), new Vector2(1f, 1f));
            _subtitle = NewText("Subtitle", go.transform, 17, new Vector2(0f, 0f), new Vector2(1f, 0.5f));
            _subtitle.color = new Color(1f, 1f, 1f, 0.78f);

            go.SetActive(false);
        }

        private Text NewText(string name, Transform parent, int fontDp, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(76f * _u, 0f);
            rect.offsetMax = new Vector2(-20f * _u, 0f);
            var text = go.AddComponent<Text>();
            text.font = UiFonts.Bold;
            text.fontSize = Mathf.RoundToInt(fontDp * _u);
            text.alignment = anchorMin.y > 0.4f ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>Posición (respecto del borde superior del contenedor) donde aparece.</summary>
        public void SetTopOffset(float topOffsetU) => _basePosition = new Vector2(0f, -topOffsetU);

        public void Show(string title, string subtitle, Color accent, float holdSeconds = 1.2f)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            _subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            _title.alignment = string.IsNullOrEmpty(subtitle) ? TextAnchor.MiddleLeft : TextAnchor.LowerLeft;
            _title.rectTransform.anchorMin = string.IsNullOrEmpty(subtitle) ? Vector2.zero : new Vector2(0f, 0.5f);
            _dot.color = accent;
            _bg.color = new Color(
                Mathf.Lerp(0.06f, accent.r, 0.28f),
                Mathf.Lerp(0.09f, accent.g, 0.28f),
                Mathf.Lerp(0.16f, accent.b, 0.28f),
                0.95f);

            _rect.gameObject.SetActive(true);
            if (_anim != null) _runner.StopCoroutine(_anim);
            _anim = _runner.StartCoroutine(Run(holdSeconds));
        }

        private IEnumerator Run(float hold)
        {
            const float inSeconds = 0.30f;
            const float outSeconds = 0.30f;
            float drop = 46f * _u;

            float elapsed = 0f;
            while (elapsed < inSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / inSeconds);
                _group.alpha = Mathf.Clamp01(t * 1.6f);
                _rect.anchoredPosition = _basePosition + new Vector2(0f, (1f - UiFx.EaseOutBack(t)) * drop);
                yield return null;
            }
            _group.alpha = 1f;
            _rect.anchoredPosition = _basePosition;

            yield return new WaitForSecondsRealtime(hold);

            elapsed = 0f;
            while (elapsed < outSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / outSeconds);
                _group.alpha = 1f - t;
                _rect.anchoredPosition = _basePosition + new Vector2(0f, t * drop * 0.6f);
                yield return null;
            }
            _group.alpha = 0f;
            _rect.gameObject.SetActive(false);
        }
    }
}
