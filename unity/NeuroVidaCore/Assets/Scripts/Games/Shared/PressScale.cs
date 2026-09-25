using UnityEngine;
using UnityEngine.EventSystems;

namespace NeuroVida.Games.Shared
{
    /// <summary>Feedback táctil inmediato: el botón se "hunde" un poco mientras el dedo lo
    /// mantiene apretado y rebota al soltar. Es lo que hace que un botón grande se sienta
    /// físico (el toque se siente respondido antes de que llegue la lógica del juego).</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = 0.93f;
        private RectTransform _rect;
        private float _target = 1f;
        private float _current = 1f;

        private void Awake() => _rect = (RectTransform)transform;

        public void OnPointerDown(PointerEventData eventData) => _target = PressedScale;
        public void OnPointerUp(PointerEventData eventData) => _target = 1f;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void OnDisable()
        {
            _target = _current = 1f;
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime * 3.2f);
            // Solo escala cuando nadie más la anima (otras corrutinas fijan su propia escala).
            _rect.localScale = new Vector3(_current, _current, 1f);
        }
    }
}
