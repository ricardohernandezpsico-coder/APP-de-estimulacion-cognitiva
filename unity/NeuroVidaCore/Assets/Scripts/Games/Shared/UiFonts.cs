using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// Tipografía compartida por los juegos Unity: <b>Fredoka</b>, la misma de la app (sello "noche + arcilla",
    /// 25-sep; antes era Outfit). Redondeada y amable, se lee bien en números grandes y a cualquier edad.
    /// Licencia OFL (<c>Assets/Resources/Fonts/Fredoka-OFL.txt</c>). Los .ttf son instancias estáticas
    /// (Bold 700 y SemiBold 600) generadas de la fuente variable de la app (<c>res/font/fredoka.ttf</c>, que por
    /// defecto es Light): el Text legacy de Unity no elige peso en fuentes variables. Sin la fuente presente,
    /// cae a la legacy para no romper nada.
    ///
    /// Se usa siempre la variante ya diseñada con su peso: NO combinar con <c>FontStyle.Bold</c> (Unity la
    /// "engordaría" otra vez sintéticamente).
    /// </summary>
    public static class UiFonts
    {
        private static Font _bold;
        private static Font _regular;

        public static Font Bold => _bold != null ? _bold : (_bold = Load("Fonts/Fredoka-Bold"));
        public static Font Regular => _regular != null ? _regular : (_regular = Load("Fonts/Fredoka-SemiBold"));

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
