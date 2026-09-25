using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Campo de estrellas en perspectiva, el mismo efecto del fondo de la app (<c>CosmosBackground.kt</c>): las
    /// estrellas nacen cerca de un punto de fuga y se abren hacia los bordes, creciendo y acelerando como si se
    /// viajara hacia ellas. <see cref="Warp"/> (0..1) controla la velocidad: en 0 derivan despacio y titilan; al
    /// subir se estiran en estelas radiales ("hiperespacio"), que es como la cuenta regresiva arranca el juego.
    /// Se agrega a un RectTransform que ocupa la pantalla; solo corre mientras su GameObject está activo.
    /// </summary>
    public sealed class StarfieldFx : MonoBehaviour
    {
        /// <summary>0 = deriva tranquila, 1 = hiperespacio.</summary>
        public float Warp;

        /// <summary>Punto de fuga en coordenadas normalizadas del área (0,0 abajo-izquierda).</summary>
        public Vector2 VanishingPoint = new Vector2(0.5f, 0.55f);

        private RectTransform _area;
        private RectTransform[] _stars;
        private Image[] _images;
        private float[] _dist;   // 0 = en el punto de fuga, 1 = borde de la pantalla
        private Vector2[] _dir;
        private float[] _speed;
        private float[] _phase;
        private float[] _baseAlpha;
        private float[] _baseSize;
        private System.Random _rng;

        public void Build(RectTransform area, int count, float starSize, int seed = 11)
        {
            _area = area;
            _rng = new System.Random(seed);
            _stars = new RectTransform[count];
            _images = new Image[count];
            _dist = new float[count];
            _dir = new Vector2[count];
            _speed = new float[count];
            _phase = new float[count];
            _baseAlpha = new float[count];
            _baseSize = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Star");
                go.transform.SetParent(area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.sprite = DiscSprite.Get();
                img.raycastTarget = false;
                // Una de cada seis estrellas es cálida (como en la app); el resto, blancas.
                img.color = i % 6 == 0 ? NeuroStyle.StarWarm : Color.white;
                _stars[i] = rect;
                _images[i] = img;
                _baseSize[i] = starSize * (0.45f + (float)_rng.NextDouble() * 0.75f);
                _baseAlpha[i] = 0.45f + (float)_rng.NextDouble() * 0.55f;
                Respawn(i, true);
            }
        }

        private void Respawn(int i, bool anywhere)
        {
            float angle = (float)(_rng.NextDouble() * Mathf.PI * 2.0);
            _dir[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _dist[i] = anywhere ? 0.05f + (float)_rng.NextDouble() * 1.1f : 0.03f + (float)_rng.NextDouble() * 0.12f;
            _speed[i] = 0.6f + (float)_rng.NextDouble() * 0.8f;
            _phase[i] = (float)_rng.NextDouble() * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (_stars == null) return;
            float dt = Time.unscaledDeltaTime;
            Vector2 size = _area.rect.size;
            Vector2 vp = new Vector2(VanishingPoint.x * size.x, VanishingPoint.y * size.y);
            float reach = size.magnitude * 0.6f;
            float warp = Mathf.Clamp01(Warp);

            // Perspectiva: la distancia crece en proporción a sí misma (lejos = lento, cerca = rápido).
            float growth = Mathf.Lerp(0.10f, 2.6f, warp * warp);

            for (int i = 0; i < _stars.Length; i++)
            {
                _dist[i] += (_dist[i] + 0.04f) * growth * _speed[i] * dt;
                if (_dist[i] > 1.25f) Respawn(i, false);

                float d = _dist[i];
                Vector2 pos = vp + _dir[i] * d * reach;
                float s = _baseSize[i] * (0.35f + d * 1.3f);
                float streak = s * warp * (4f + d * 26f);

                var rect = _stars[i];
                rect.anchoredPosition = pos - _dir[i] * streak * 0.5f; // la estela queda detrás de la estrella
                rect.sizeDelta = new Vector2(s, s + streak);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_dir[i].y, _dir[i].x) * Mathf.Rad2Deg - 90f);

                _phase[i] += dt * (1.5f + _speed[i]);
                float twinkle = Mathf.Lerp(0.55f + 0.45f * Mathf.Sin(_phase[i]), 1f, warp);
                float fadeIn = Mathf.Clamp01(d / 0.12f);
                var c = _images[i].color;
                c.a = _baseAlpha[i] * twinkle * fadeIn;
                _images[i].color = c;
            }
        }
    }
}
