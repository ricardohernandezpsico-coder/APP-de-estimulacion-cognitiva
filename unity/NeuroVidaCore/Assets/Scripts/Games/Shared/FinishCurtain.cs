using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Momento de cierre de la partida (≈1,2 s): el cielo nocturno tapa el juego, las estrellas aceleran hasta
    /// hiperespacio y aparece "¡Listo!" en arcilla sol con una lluvia de destellos; debajo, "Veamos cómo te fue",
    /// que anuncia lo que viene (la pantalla de resultado de la app). Canvas propio por encima del juego y por
    /// debajo del menú de pausa. Tiempos: entrada con rebote (spring) y sin salida propia: la app aparece encima.
    /// </summary>
    public sealed class FinishCurtain : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _title;
        private CanvasGroup _subGroup;
        private StarfieldFx _stars;
        private RectTransform _fx;

        public static FinishCurtain Create(Transform parent)
        {
            var root = new GameObject("FinishCurtain");
            root.transform.SetParent(parent, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450; // sobre el juego y su cuenta regresiva; bajo el menú de pausa (500)
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            root.AddComponent<GraphicRaycaster>(); // bloquea toques al juego durante el cierre

            var curtain = root.AddComponent<FinishCurtain>();
            curtain.Build(root.GetComponent<RectTransform>());
            return curtain;
        }

        private void Build(RectTransform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            var sky = new GameObject("Sky");
            sky.transform.SetParent(root, false);
            var skyRect = sky.AddComponent<RectTransform>();
            Stretch(skyRect);
            var skyImg = sky.AddComponent<Image>();
            skyImg.sprite = NeuroStyle.NightGradient();
            skyImg.color = Color.white;

            var starsGo = new GameObject("Stars");
            starsGo.transform.SetParent(root, false);
            var starsRect = starsGo.AddComponent<RectTransform>();
            Stretch(starsRect);
            _stars = starsGo.AddComponent<StarfieldFx>();
            _stars.VanishingPoint = new Vector2(0.5f, 0.55f);
            _stars.Build(starsRect, 70, 10f, 29);

            var fxGo = new GameObject("Fx");
            fxGo.transform.SetParent(root, false);
            _fx = fxGo.AddComponent<RectTransform>();
            _fx.anchorMin = _fx.anchorMax = new Vector2(0.5f, 0.55f);
            _fx.sizeDelta = Vector2.zero;

            var title = MakeText(root, "Title", 170, TextAnchor.MiddleCenter, NeuroStyle.Sun, 0f, 0f);
            _title = title.rectTransform;
            _title.anchorMin = _title.anchorMax = new Vector2(0.5f, 0.55f);
            _title.sizeDelta = new Vector2(1000f, 240f);
            _title.anchoredPosition = Vector2.zero;
            title.font = UiFonts.Bold;
            title.text = "¡Listo!";
            NeuroStyle.ClayText(title, 8f, 16f);

            var sub = MakeText(root, "Sub", 54, TextAnchor.MiddleCenter, NeuroStyle.Cream, 0f, 0f);
            var subRect = sub.rectTransform;
            subRect.anchorMin = subRect.anchorMax = new Vector2(0.5f, 0.55f);
            subRect.sizeDelta = new Vector2(1000f, 90f);
            subRect.anchoredPosition = new Vector2(0f, -170f);
            sub.text = "Veamos cómo te fue";
            _subGroup = sub.gameObject.AddComponent<CanvasGroup>();
            _subGroup.alpha = 0f;

            _title.localScale = Vector3.zero;
        }

        /// <summary>Anima el cierre completo (≈1,2 s) con el reloj real.</summary>
        public IEnumerator Play(MonoBehaviour runner)
        {
            runner.StartCoroutine(UiFx.SparkBurst(_fx, Vector2.zero, NeuroStyle.Sun, 22, 520f, 60f, 0.9f));
            float t = 0f;
            const float total = 1.2f;
            while (t < total)
            {
                t += Time.unscaledDeltaTime;
                // Cielo: entra rápido (0,22 s) para tapar el juego.
                _group.alpha = Mathf.Clamp01(t / 0.22f);
                // "¡Listo!": rebote de 0,4 s, empieza con el cielo.
                float k = Mathf.Clamp01((t - 0.05f) / 0.4f);
                _title.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, UiFx.EaseOutBack(k));
                // Subtítulo: después del título.
                _subGroup.alpha = Mathf.Clamp01((t - 0.35f) / 0.25f);
                // Estrellas: aceleran hasta hiperespacio en la segunda mitad (el viaje "hacia" el resultado).
                _stars.Warp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.7f));
                yield return null;
            }
        }
    }
}
