using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Anagramas
{
    public readonly struct AnagramWord
    {
        /// <summary>Palabra objetivo en MAYÚSCULAS, sin tildes ni Ñ (solo A-Z), para que las fichas
        /// sean letras simples y no haya ambigüedad de acentos al armarla.</summary>
        public readonly string Word;
        public readonly string Clue;

        public AnagramWord(string word, string clue)
        {
            Word = word;
            Clue = clue;
        }
    }

    /// <summary>
    /// Reglas puras de "Anagramas" (lenguaje): se muestran las letras desordenadas de una palabra y hay que
    /// armarla. Puerto de <c>AnagramasGame.kt</c> con un banco mucho más grande (para que el Reto
    /// de 120 s no repita palabras) y una escala de 7 niveles por largo de palabra, de 3 a 11 letras.
    /// </summary>
    public static class AnagramContract
    {
        public const string GameId = "anagramas";
        public const int TotalTrials = 6;
        public const int EndlessSeconds = 120;
        /// <summary>Una palabra cada ~13 s: 9 palabras en 120 s = ritmo completo.</summary>
        public const int EndlessTargetTrials = 9;
        public const int MaxLevel = 7;

        public static int CorrectPerLevelUp(bool endless) => 2;

        /// <summary>Largo (mínimo, máximo) de las palabras de cada nivel: crece de a poco.</summary>
        public static (int min, int max) LengthRange(int level)
        {
            switch (Math.Max(1, Math.Min(level, MaxLevel)))
            {
                case 1: return (3, 4);
                case 2: return (4, 5);
                case 3: return (5, 6);
                case 4: return (6, 7);
                case 5: return (7, 8);
                case 6: return (8, 9);
                default: return (9, 11);
            }
        }

        /// <summary>Puntos por palabra: 100 + bonus de racha, menos 30 si se usó la pista.</summary>
        public static int PointsFor(int streakAfterHit, bool usedHint) =>
            Math.Max(20, 100 + Math.Min(streakAfterHit - 1, 6) * 20 - (usedHint ? 30 : 0));

        public static readonly AnagramWord[] Bank =
        {
            // 3 letras
            new AnagramWord("SOL", "Astro central del día"),
            new AnagramWord("MAR", "Masa de agua salada"),
            new AnagramWord("PAZ", "Ausencia de conflicto"),
            new AnagramWord("VOZ", "Sonido al hablar"),
            new AnagramWord("LUZ", "Lo que ilumina"),
            new AnagramWord("PAN", "Alimento de la panadería"),
            new AnagramWord("SAL", "Condimento blanco del mar"),
            new AnagramWord("REY", "Gobierna un reino"),
            new AnagramWord("OJO", "Órgano de la vista"),
            new AnagramWord("PIE", "Con él caminamos"),
            // 4 letras
            new AnagramWord("VIDA", "Lo que disfrutamos y cuidamos"),
            new AnagramWord("LUNA", "Satélite de la Tierra"),
            new AnagramWord("AGUA", "Recurso vital, se bebe"),
            new AnagramWord("ROSA", "Flor con espinas"),
            new AnagramWord("RIOS", "Corrientes de agua dulce"),
            new AnagramWord("CASA", "Lugar donde vivimos"),
            new AnagramWord("GATO", "Felino doméstico"),
            new AnagramWord("MESA", "Mueble con cuatro patas"),
            new AnagramWord("LAGO", "Agua dulce rodeada de tierra"),
            new AnagramWord("NUBE", "Flota en el cielo"),
            new AnagramWord("ISLA", "Tierra rodeada de mar"),
            new AnagramWord("TREN", "Va sobre rieles"),
            new AnagramWord("IDEA", "Pensamiento nuevo"),
            new AnagramWord("ARTE", "Pintura, música, escultura"),
            new AnagramWord("SOPA", "Plato caliente con caldo"),
            new AnagramWord("PATO", "Ave que nada y hace cuac"),
            // 5 letras
            new AnagramWord("MENTE", "Capacidad cognitiva humana"),
            new AnagramWord("LIBRO", "Contiene historias y saber"),
            new AnagramWord("RUTAS", "Caminos para explorar"),
            new AnagramWord("CALMA", "Estado de tranquilidad"),
            new AnagramWord("PLAYA", "Arena junto al mar"),
            new AnagramWord("TIGRE", "Felino rayado"),
            new AnagramWord("NOCHE", "Cuando sale la luna"),
            new AnagramWord("CIELO", "Sobre nuestras cabezas"),
            new AnagramWord("MUNDO", "El planeta y su gente"),
            new AnagramWord("RADIO", "Se escucha con antena"),
            new AnagramWord("CAMPO", "Lugar de cultivos"),
            new AnagramWord("VERDE", "Color de las hojas"),
            new AnagramWord("RELOJ", "Marca la hora"),
            new AnagramWord("FELIZ", "Contento, alegre"),
            new AnagramWord("VIAJE", "Ir de un lugar a otro"),
            // 6 letras
            new AnagramWord("FUERZA", "Capacidad de ejercer energía"),
            new AnagramWord("TIEMPO", "Pasa segundo a segundo"),
            new AnagramWord("JARDIN", "Espacio con plantas y flores"),
            new AnagramWord("BOSQUE", "Muchos árboles juntos"),
            new AnagramWord("CAMINO", "Sendero para andar"),
            new AnagramWord("AMIGOS", "Personas queridas que nos acompañan"),
            new AnagramWord("MUSICA", "Arte de los sonidos"),
            new AnagramWord("ESPEJO", "Refleja tu imagen"),
            new AnagramWord("PUENTE", "Cruza un río"),
            new AnagramWord("VOLCAN", "Montaña que echa lava"),
            new AnagramWord("DELFIN", "Mamífero marino juguetón"),
            // 7 letras
            new AnagramWord("SONRISA", "Gesto de alegría en la cara"),
            new AnagramWord("MEMORIA", "Capacidad de recordar"),
            new AnagramWord("PLANETA", "Como la Tierra o Marte"),
            new AnagramWord("PALABRA", "Unidad del lenguaje"),
            new AnagramWord("VENTANA", "Se abre para mirar afuera"),
            new AnagramWord("AMISTAD", "Cariño entre amigos"),
            new AnagramWord("CULTURA", "Costumbres y saberes de un pueblo"),
            new AnagramWord("COCINERO", "Prepara la comida"),
            // 8 letras
            new AnagramWord("SINFONIA", "Armonía de muchas notas"),
            new AnagramWord("GUITARRA", "Instrumento de seis cuerdas"),
            new AnagramWord("MARIPOSA", "Insecto de alas de colores"),
            new AnagramWord("ESTRELLA", "Brilla en el cielo nocturno"),
            new AnagramWord("LIBERTAD", "Poder elegir sin cadenas"),
            new AnagramWord("ELEFANTE", "Animal enorme con trompa"),
            new AnagramWord("PRINCESA", "Hija de un rey"),
            new AnagramWord("AVENTURA", "Viaje lleno de sorpresas"),
            new AnagramWord("GRATITUD", "Sentimiento de agradecimiento"),
            new AnagramWord("MEDICINA", "Ciencia que cura enfermedades"),
            // 9 letras
            new AnagramWord("PACIENCIA", "Virtud de esperar sin frustrarse"),
            new AnagramWord("CHOCOLATE", "Dulce hecho de cacao"),
            new AnagramWord("HORIZONTE", "Línea donde el cielo toca la tierra"),
            new AnagramWord("ESPERANZA", "Confianza en que algo bueno llegará"),
            new AnagramWord("SABIDURIA", "Conocimiento y buen juicio"),
            new AnagramWord("CALENDARIO", "Muestra los días del año"),
            new AnagramWord("SOLIDARIDAD", "Ayuda mutua entre las personas"),
            // 10-11 letras
            new AnagramWord("CONCIENCIA", "Percepción de uno mismo"),
            new AnagramWord("EQUILIBRIO", "Estado de estabilidad y balance"),
            new AnagramWord("BIBLIOTECA", "Lugar con muchos libros"),
            new AnagramWord("TELESCOPIO", "Sirve para mirar las estrellas"),
            new AnagramWord("ARQUITECTO", "Diseña edificios"),
            new AnagramWord("CREATIVIDAD", "Capacidad de imaginar cosas nuevas"),
            new AnagramWord("RESILIENCIA", "Capacidad de adaptarse tras la adversidad"),
            new AnagramWord("PROFESIONAL", "Persona con oficio o carrera"),
        };

        /// <summary>Elige una palabra del rango de largo del nivel que no se haya usado; si ya se
        /// agotaron, reinicia el registro de usadas (el Reto puede pedir muchas).</summary>
        public static AnagramWord Pick(int level, HashSet<string> used, Random rng)
        {
            var (min, max) = LengthRange(level);
            var candidates = new List<AnagramWord>();
            foreach (var w in Bank)
                if (w.Word.Length >= min && w.Word.Length <= max && !used.Contains(w.Word)) candidates.Add(w);
            if (candidates.Count == 0)
            {
                used.Clear();
                foreach (var w in Bank)
                    if (w.Word.Length >= min && w.Word.Length <= max) candidates.Add(w);
            }
            var picked = candidates[rng.Next(candidates.Count)];
            used.Add(picked.Word);
            return picked;
        }

        /// <summary>Mezcla las letras; nunca devuelve la palabra ya armada (salvo palabras de 1-2 letras).</summary>
        public static char[] Scramble(string word, Random rng)
        {
            var letters = word.ToCharArray();
            if (letters.Length < 3) return letters;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                for (int i = letters.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    char t = letters[i]; letters[i] = letters[j]; letters[j] = t;
                }
                if (new string(letters) != word) return letters;
            }
            Array.Reverse(letters);
            return letters;
        }

        /// <summary>Acierta si arma la palabra objetivo o cualquier otra palabra del banco que sea un
        /// anagrama de las mismas letras (p. ej. dos palabras válidas con las mismas letras).</summary>
        public static bool IsAccepted(string formed, string target)
        {
            if (formed == target) return true;
            if (SortedLetters(formed) != SortedLetters(target)) return false;
            foreach (var w in Bank) if (w.Word == formed) return true;
            return false;
        }

        private static string SortedLetters(string s)
        {
            var arr = s.ToCharArray();
            Array.Sort(arr);
            return new string(arr);
        }

        public static int Score(int correct, int total) =>
            total <= 0 ? 0 : Math.Max(0, Math.Min(100, correct * 100 / total));

        public static int EndlessScore(int correct, int total)
        {
            if (total <= 0) return 0;
            float accuracy = (float)correct / total;
            float pace = Math.Min(1f, (float)total / EndlessTargetTrials);
            return Math.Max(0, Math.Min(100, (int)Math.Round(accuracy * pace * 100f)));
        }
    }
}
