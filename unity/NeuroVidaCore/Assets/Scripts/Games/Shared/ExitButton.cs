using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>Botón "Continuar" que aparece al terminar la partida y devuelve al usuario a la app
    /// (cierra la pantalla de Unity). Compartido por los 9 juegos.</summary>
    public sealed class ExitButton
    {
        private readonly GameObject _root;
        private readonly RectTransform _rect;
        private readonly MonoBehaviour _runner;

        public ExitButton(Transform parent, MonoBehaviour runner, float unitsPerDp)
        {
            _runner = runner;
            _root = new GameObject("ExitButton");
            _root.transform.SetParent(parent, false);
            _rect = _root.AddComponent<RectTransform>();
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(560f, 132f);
            _rect.anchoredPosition = new Vector2(0f, 50f + 66f);

            var img = _root.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = NeuroStyle.Sun; // acción principal en sol con texto tinta, como el botón "Entrenar" de la app
            NeuroStyle.ClayFrame(img, 6f, 14f);

            var button = _root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(NativeBridge.CloseGameScreen);
            _root.AddComponent<PressScale>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(_root.transform, false);
            var tr = textGo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = UiFonts.Bold;
            text.fontSize = 64;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = NeuroStyle.Ink;
            text.raycastTarget = false;
            text.text = "Continuar";

            _root.SetActive(false);
        }

        public void Hide() => _root.SetActive(false);

        public void Show()
        {
            _root.SetActive(true);
            _root.transform.SetAsLastSibling(); // por encima de paneles y efectos
            _runner.StartCoroutine(PopIn());
        }

        private IEnumerator PopIn()
        {
            float t = 0f;
            const float delay = 0.6f; // aparece después de que el panel de resultado termina de animar
            _rect.localScale = Vector3.zero;
            while (t < delay) { t += GameClock.DeltaTime; yield return null; }
            t = 0f;
            const float seconds = 0.3f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                _rect.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(Mathf.Clamp01(t / seconds)));
                yield return null;
            }
            _rect.localScale = Vector3.one;
        }
    }
}
