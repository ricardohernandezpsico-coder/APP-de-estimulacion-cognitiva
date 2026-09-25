using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia; // RoundedRectSprite
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Marcador común (la franja de arriba) de los juegos del DDA común: antes cada juego armaba el suyo con un
    /// subtítulo de texto corrido ("Puntos 1250 · Nivel 3") y una píldora gris de racha. Ahora todos se leen igual,
    /// con una jerarquía clara (ui-ux-pro-max: una cosa principal por zona):
    /// - Título del juego, grande, a la izquierda.
    /// - Debajo, dos chips de arcilla oscura: "Nivel N" (celeste) y la cifra del modo: puntos que CUENTAN hacia
    ///   arriba al sumar (Reto) o el avance "3 de 12" (Precisión) (sol).
    /// - A la derecha, la racha: número grande y la palabra "racha"; con 3 o más el destello se enciende en coral,
    ///   y en cada múltiplo de 5 la píldora salta con destellos.
    /// Subir de nivel hace saltar y brillar el chip de nivel. Mismo alto que antes (190): no mueve nada de abajo.
    /// </summary>
    public sealed class GameHud
    {
        public const float Height = 190f;
        private const float ChipH = 64f;
        private const float ChipPad = 26f;

        public RectTransform Rect { get; }

        private readonly MonoBehaviour _runner;
        private readonly float _margin;
        private readonly RectTransform _levelChip, _infoChip, _streakPill;
        private readonly Text _levelText, _infoText, _streakNumber, _streakLabel;
        private readonly Image _levelBg, _streakSpark;
        private int _level = -1;
        private int _shownPoints = -1;
        private int _targetPoints;
        private Coroutine _countUp;

        public GameHud(RectTransform parent, string title, float marginU, MonoBehaviour runner, bool withStreak = true, float rightReserve = 0f)
        {
            _runner = runner;
            _margin = marginU;

            var go = new GameObject("Hud");
            go.transform.SetParent(parent, false);
            Rect = go.AddComponent<RectTransform>();
            Rect.anchorMin = new Vector2(0f, 1f);
            Rect.anchorMax = new Vector2(1f, 1f);
            Rect.pivot = new Vector2(0.5f, 1f);
            Rect.sizeDelta = new Vector2(0f, Height);
            Rect.anchoredPosition = Vector2.zero;

            const float pillW = 220f, pillH = 118f;
            float reserve = withStreak ? pillW + 24f : rightReserve;

            var titleText = MakeText(Rect, "Title", 70, TextAnchor.UpperLeft, Color.white, 3f, 0.45f);
            PlaceTopText(titleText, marginU, marginU + reserve, -20f, 92f);
            BestFit(titleText, 44);
            titleText.text = title;

            _levelChip = Chip("LevelChip", out _levelBg, out _levelText, NeuroStyle.Sky);
            _infoChip = Chip("InfoChip", out _, out _infoText, NeuroStyle.Sun);
            _infoChip.gameObject.SetActive(false);

            if (withStreak)
            {
                var pillGo = new GameObject("StreakPill");
                pillGo.transform.SetParent(Rect, false);
                _streakPill = pillGo.AddComponent<RectTransform>();
                _streakPill.anchorMin = _streakPill.anchorMax = _streakPill.pivot = new Vector2(1f, 1f);
                _streakPill.sizeDelta = new Vector2(pillW, pillH);
                _streakPill.anchoredPosition = new Vector2(-marginU, -26f);
                var bg = pillGo.AddComponent<Image>();
                bg.sprite = RoundedRectSprite.Get(64);
                bg.type = Image.Type.Sliced;
                bg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.92f);
                bg.raycastTarget = false;
                NeuroStyle.ClayFrame(bg, 4f, 8f);

                var sparkGo = new GameObject("Spark");
                sparkGo.transform.SetParent(_streakPill, false);
                var sr = sparkGo.AddComponent<RectTransform>();
                sr.anchorMin = sr.anchorMax = new Vector2(0f, 0.5f);
                sr.sizeDelta = new Vector2(64f, 64f);
                sr.anchoredPosition = new Vector2(52f, 0f);
                _streakSpark = sparkGo.AddComponent<Image>();
                _streakSpark.sprite = SparkleSprite.Get();
                _streakSpark.raycastTarget = false;

                _streakNumber = MakeText(_streakPill, "Number", 60, TextAnchor.MiddleCenter, NeuroStyle.Cream, 0f, 0f);
                var nr = _streakNumber.rectTransform;
                nr.offsetMin = new Vector2(92f, 34f);
                nr.offsetMax = new Vector2(-14f, -6f);
                _streakLabel = MakeText(_streakPill, "Label", 28, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.7f), 0f, 0f);
                _streakLabel.font = UiFonts.Regular;
                var lr = _streakLabel.rectTransform;
                lr.offsetMin = new Vector2(92f, 8f);
                lr.offsetMax = new Vector2(-14f, -76f);
                _streakLabel.text = "racha";
                SetStreak(0, animate: false);
            }
        }

        private RectTransform Chip(string name, out Image bg, out Text text, Color textColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Rect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(160f, ChipH);
            rect.anchoredPosition = new Vector2(_margin, -118f);
            bg = go.AddComponent<Image>();
            bg.sprite = RoundedRectSprite.Get(64);
            bg.type = Image.Type.Sliced;
            bg.color = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            bg.raycastTarget = false;
            NeuroStyle.ClayFrame(bg, 3f, 6f);
            text = MakeText(rect, "Text", 40, TextAnchor.MiddleCenter, textColor, 0f, 0f);
            return rect;
        }

        /// <summary>Ajusta el ancho de cada chip a su texto y los pone uno al lado del otro.</summary>
        private void LayoutChips()
        {
            float x = _margin;
            foreach (var (chip, text) in new[] { (_levelChip, _levelText), (_infoChip, _infoText) })
            {
                if (!chip.gameObject.activeSelf) continue;
                chip.sizeDelta = new Vector2(Mathf.Ceil(text.preferredWidth) + ChipPad * 2f, ChipH);
                chip.anchoredPosition = new Vector2(x, -118f);
                x += chip.sizeDelta.x + 14f;
            }
        }

        // ------------------------------------------------------------------ API

        public void SetLevel(int level)
        {
            bool up = _level >= 0 && level > _level;
            _level = level;
            _levelText.text = $"Nivel {level}";
            LayoutChips();
            if (up) _runner.StartCoroutine(LevelUpGlow());
        }

        /// <summary>Puntos del modo Reto: el número cuenta hacia arriba en ~0,3 s.</summary>
        public void SetPoints(int points)
        {
            _infoChip.gameObject.SetActive(true);
            _targetPoints = points;
            if (_shownPoints < 0 || points < _shownPoints)
            {
                _shownPoints = points;
                _infoText.text = FormatPoints(points);
                LayoutChips();
                return;
            }
            if (_countUp != null) _runner.StopCoroutine(_countUp);
            _countUp = _runner.StartCoroutine(CountUp());
        }

        /// <summary>Texto libre del segundo chip ("3 de 12", "Tesoros 2/5").</summary>
        public void SetInfo(string text)
        {
            _infoChip.gameObject.SetActive(true);
            _shownPoints = -1;
            _infoText.text = text;
            LayoutChips();
        }

        public void SetStreak(int streak) => SetStreak(streak, animate: true);

        private void SetStreak(int streak, bool animate)
        {
            if (_streakPill == null) return;
            _streakNumber.text = streak.ToString();
            bool hot = streak >= 3;
            _streakNumber.color = hot ? NeuroStyle.Sun : NeuroStyle.Cream;
            _streakSpark.color = hot ? NeuroStyle.Coral : NeuroStyle.WithAlpha(Color.white, 0.25f);
            if (!animate || streak <= 0) return;
            bool milestone = streak % 5 == 0;
            _runner.StartCoroutine(PopRect(_streakPill, milestone ? 1.22f : 1.1f, milestone ? 0.3f : 0.2f));
            if (milestone)
            {
                var fx = Rect;
                Vector2 c = LocalIn(fx, _streakPill) + new Vector2(-_streakPill.rect.width * 0.5f, -_streakPill.rect.height * 0.5f);
                _runner.StartCoroutine(UiFx.SparkBurst(fx, c, NeuroStyle.Sun, 12, 180f, 30f, 0.55f));
            }
        }

        // ------------------------------------------------------------------ animaciones

        private IEnumerator CountUp()
        {
            int from = _shownPoints;
            float t = 0f;
            const float seconds = 0.3f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                _shownPoints = Mathf.RoundToInt(Mathf.Lerp(from, _targetPoints, k));
                _infoText.text = FormatPoints(_shownPoints);
                LayoutChips();
                yield return null;
            }
            _shownPoints = _targetPoints;
            _infoText.text = FormatPoints(_shownPoints);
            LayoutChips();
            _countUp = null;
        }

        private IEnumerator LevelUpGlow()
        {
            _runner.StartCoroutine(PopRect(_levelChip, 1.25f, 0.32f));
            var baseColor = NeuroStyle.WithAlpha(NeuroStyle.Surface, 0.9f);
            float t = 0f;
            const float seconds = 0.6f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                // Sube rápido al sol y vuelve más despacio (la salida no compite con lo que sigue).
                float glow = k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f;
                _levelBg.color = Color.Lerp(baseColor, NeuroStyle.Sun, glow * 0.85f);
                _levelText.color = Color.Lerp(NeuroStyle.Sky, NeuroStyle.Ink, glow);
                yield return null;
            }
            _levelBg.color = baseColor;
            _levelText.color = NeuroStyle.Sky;
        }

        /// <summary>"1.250 pts" (separador de miles con punto, como se escribe en español).</summary>
        // Sin CultureInfo("es-ES"): en IL2CPP los datos de cultura pueden venir recortados y lanzar excepción.
        private static string FormatPoints(int points) =>
            points.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + " pts";
    }
}
