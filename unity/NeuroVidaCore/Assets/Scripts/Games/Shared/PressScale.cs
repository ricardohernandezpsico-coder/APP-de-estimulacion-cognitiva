using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>Feedback táctil inmediato: mientras el dedo lo mantiene apretado, el botón de arcilla se HUNDE
    /// (su ficha cambia a <see cref="TileSprites.GetPressed"/>: la cara baja hacia su sombra dura) y se encoge
    /// apenas; al soltar vuelve a subir. Es lo que hace que un botón grande se sienta físico (el toque se siente
    /// respondido antes de que llegue la lógica del juego) sin cambiar el espacio que ocupa. Botones con otro
    /// sprite solo se encogen.</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = 0.97f;
        private RectTransform _rect;
        private Image _image;
        private float _target = 1f;
        private float _current = 1f;
        private bool _sunk;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _image = GetComponent<Image>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _target = PressedScale;
            SetSunk(true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            _target = 1f;
            SetSunk(false);
        }

        /// <summary>Solo cambia la ficha si el botón usa la de arcilla (el juego puede haberle puesto otra).</summary>
        private void SetSunk(bool sunk)
        {
            if (_image == null || sunk == _sunk) return;
            if (sunk && _image.sprite == TileSprites.Get())
            {
                _image.sprite = TileSprites.GetPressed();
                _sunk = true;
            }
            else if (!sunk)
            {
                if (_image.sprite == TileSprites.GetPressed()) _image.sprite = TileSprites.Get();
                _sunk = false;
            }
        }

        private void OnDisable()
        {
            _target = _current = 1f;
            if (_rect != null) _rect.localScale = Vector3.one;
            SetSunk(false);
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target)) return;
            _current = Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime * 1.2f);
            // Solo escala cuando nadie más la anima (otras corrutinas fijan su propia escala).
            _rect.localScale = new Vector3(_current, _current, 1f);
        }
    }
}
