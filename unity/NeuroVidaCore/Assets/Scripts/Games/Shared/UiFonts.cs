using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Tipografía compartida por los juegos Unity. Usa <b>Outfit</b> (geométrica, limpia,
    /// licencia OFL -- ver <c>Assets/Resources/Fonts/Outfit-OFL.txt</c>), cercana al estilo
    /// de las apps de entrenamiento mental tipo Lumosity, en vez de la fuente legacy de
    /// Unity (Arial genérica). Sin la fuente presente, cae a la legacy para no romper nada.
    ///
    /// Se usa siempre la variante Bold ya diseñada como tal: NO combinar con
    /// <c>FontStyle.Bold</c> (Unity la "engordaría" otra vez sintéticamente).
    /// </summary>
    public static class UiFonts
    {
        private static Font _bold;
        private static Font _regular;

        public static Font Bold => _bold != null ? _bold : (_bold = Load("Fonts/Outfit-Bold"));
        public static Font Regular => _regular != null ? _regular : (_regular = Load("Fonts/Outfit-Regular"));

        private static Font Load(string path)
        {
            var font = Resources.Load<Font>(path);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Sombra suave debajo del texto (mejor lectura que el contorno negro duro
        /// de las primeras pasadas; se atenúa junto con el alfa del texto).</summary>
        public static void AddSoftShadow(GameObject textGo, float distance = 3f, float alpha = 0.38f)
        {
            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
            shadow.useGraphicAlpha = true;
        }
    }
}
