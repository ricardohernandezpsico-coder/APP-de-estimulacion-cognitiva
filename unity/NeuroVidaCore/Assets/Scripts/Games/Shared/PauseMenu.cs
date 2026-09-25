using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Menú de pausa de los 9 juegos (Atrás a mitad de partida, o la app pasa a segundo plano): congela la partida
    /// (<see cref="GameClock"/>) y ofrece Continuar / Reiniciar / Salir. "Salir" vuelve a la app DEJANDO la partida
    /// en pausa: si después se vuelve a abrir ese mismo juego, continúa donde quedó (antes Atrás la abandonaba y
    /// empezaba de cero). Va en un canvas propio encima del juego; su animación usa el reloj real.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private RectTransform _panel;
        private CanvasGroup _group;
        private Action _onResume, _onRestart, _onExit;

        public bool IsShown => gameObject.activeSelf;

        public static PauseMenu Create(Transform parent, Action onResume, Action onRestart, Action onExit)
        {
            var root = new GameObject("PauseMenu");
            root.transform.SetParent(parent, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // encima del juego y de su cuenta regresiva
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            root.AddComponent<GraphicRaycaster>();

            var menu = root.AddComponent<PauseMenu>();
            menu._onResume = onResume;
            menu._onRestart = onRestart;
            menu._onExit = onExit;
            menu.Build(root.GetComponent<RectTransform>());
            root.SetActive(false);
            return menu;
        }

        private void Build(RectTransform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();

            // Velo oscuro: tapa el juego y bloquea sus toques mientras está en pausa.
            var scrim = new GameObject("Scrim");
            scrim.transform.SetParent(root, false);
            var scrimRect = scrim.AddComponent<RectTransform>();
            Stretch(scrimRect);
            scrim.AddComponent<Image>().color = NeuroStyle.WithAlpha(NeuroStyle.NightBottom, 0.78f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(root, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(820f, 940f);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = RoundedRectSprite.Get(64);
            panelImg.type = Image.Type.Sliced;
            panelImg.color = NeuroStyle.Surface;
            NeuroStyle.ClayFrame(panelImg, 7f, 18f);

            var title = MakeText(_panel, "Title", 110, TextAnchor.MiddleCenter, NeuroStyle.Sun, 0f, 0f);
            PlaceCentered(title.rectTransform, new Vector2(0f, 330f), new Vector2(760f, 150f));
            title.text = "Pausa";
            NeuroStyle.ClayText(title, 6f, 12f);

            var sub = MakeText(_panel, "Subtitle", 46, TextAnchor.MiddleCenter, NeuroStyle.Hex(0xB4BFEA), 0f, 0f);
            PlaceCentered(sub.rectTransform, new Vector2(0f, 215f), new Vector2(760f, 70f));
            sub.text = "Tu partida te espera";

            AddButton("Continuar", NeuroStyle.Sun, new Vector2(0f, 55f), () =>
            {
                Hide();
                GameClock.Resume();
                _onResume?.Invoke();
            });
            AddButton("Reiniciar", NeuroStyle.Sky, new Vector2(0f, -135f), () =>
            {
                Hide();
                _onRestart?.Invoke();
            });
            AddButton("Salir", NeuroStyle.Cream, new Vector2(0f, -325f), () => _onExit?.Invoke());
        }

        private void AddButton(string label, Color fill, Vector2 pos, Action onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(_panel, false);
            var rect = go.AddComponent<RectTransform>();
            PlaceCentered(rect, pos, new Vector2(640f, 150f)); // ~50 dp de alto: mayor que el mínimo de 48 dp
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = fill;
            NeuroStyle.ClayFrame(img, 6f, 14f);
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());
            go.AddComponent<PressScale>();

            var text = MakeText(go.transform, "Label", 60, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            text.text = label;
        }

        private static void PlaceCentered(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        /// <summary>Pausa la partida y muestra el menú (con una entrada breve).</summary>
        public void Show()
        {
            GameClock.Pause();
            if (IsShown) return;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StartCoroutine(PopIn());
        }

        /// <summary>Oculta el menú (no reanuda: eso lo decide quien llama).</summary>
        public void Hide() => gameObject.SetActive(false);

        /// <summary>Atrás con el menú abierto = Continuar.</summary>
        public void ResumeFromBack()
        {
            Hide();
            GameClock.Resume();
            _onResume?.Invoke();
        }

        private IEnumerator PopIn()
        {
            float t = 0f;
            const float seconds = 0.22f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime; // reloj real: el de juego está congelado
                float k = Mathf.Clamp01(t / seconds);
                _group.alpha = k;
                _panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, UiFx.EaseOutBack(k));
                yield return null;
            }
            _group.alpha = 1f;
            _panel.localScale = Vector3.one;
        }
    }
}
