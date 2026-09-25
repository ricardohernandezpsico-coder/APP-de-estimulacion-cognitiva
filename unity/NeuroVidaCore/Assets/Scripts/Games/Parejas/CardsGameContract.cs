using System.Collections.Generic;

namespace NeuroVida.Games.Parejas
{
    /// <summary>Puerto 1:1 de <c>CountdownReason</c> (Kotlin) -- distingue arranque/avance
    /// normal de una bajada o repetición de nivel por fallas, para que el cambio de nivel
    /// hacia atrás se lea como mecánica del juego, no como un bug.</summary>
    public enum CountdownReason
    {
        Start,
        Advance,
        Demoted,
        Retry
    }

    /// <summary>
    /// Puerto 1:1 de las constantes de <c>com.example.games.parejas.CardsGameContract</c>
    /// (Kotlin) -- Fase 2 del roadmap de migración. La cantidad de parejas por nivel es
    /// una escalera FIJA (nunca depende de <see cref="VisualWorkingMemoryDDA"/>, que solo
    /// afina vista previa/interferencia/distractores dentro de cada nivel) para que la
    /// progresión "empieza fácil, termina difícil" quede garantizada -- Ricardo notó
    /// jugando la versión Kotlin que el nivel 2 le había salido más fácil que el 1 cuando
    /// la cantidad de parejas todavía salía de D(t).
    /// </summary>
    public static class CardsGameContract
    {
        /// <summary>Ritmo de referencia para el bono de velocidad: 4s por pareja.</summary>
        public const long ExpectedMsPerPair = 4000L;

        /// <summary>Cuántos tableros sucesivos arma una partida antes de mostrar el
        /// resultado final.</summary>
        public const int TotalStages = 10;

        private static readonly int[] PairCountsByStage = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12 };

        public static int PairCountForStage(int stage)
        {
            int index = stage - 1;
            if (index < 0) index = 0;
            if (index > PairCountsByStage.Length - 1) index = PairCountsByStage.Length - 1;
            return PairCountsByStage[index];
        }

        /// <summary>Sistema de "3 fallas": fallar <see cref="MismatchesToFailStage"/>
        /// parejas en el tablero actual corta ese tablero ahí mismo y cuenta como una
        /// falla de nivel. 1ra falla -> baja un nivel; 2da -> repite ese mismo nivel ya
        /// bajado; 3ra -> termina la partida.</summary>
        public const int MismatchesToFailStage = 3;
        public const int StageFailuresBeforeGameOver = 3;
    }

    /// <summary>Un símbolo de carta: ícono ilustrado + variante de color (ver
    /// <see cref="SymbolSprite"/>). <see cref="Key"/> es el identificador estable usado
    /// para detectar pares (equivalente al emoji original, que servía como su propia
    /// clave por ser un string).</summary>
    public readonly struct SymbolDef
    {
        public readonly ShapeKind Shape;
        public readonly int Variant;
        public readonly string Key;

        public SymbolDef(ShapeKind shape, int variant)
        {
            Shape = shape;
            Variant = variant;
            Key = $"{shape}:{variant}";
        }
    }

    /// <summary>Puerto de <c>SymbolBank</c> (Kotlin) -- bancos de símbolos graduados por
    /// similitud perceptual (0 = muy distintos entre sí, 3 = máxima interferencia),
    /// impulsado por <c>DifficultyProfile.InterferenceLevel</c>.
    ///
    /// La versión Kotlin usa emojis de color (el font system de Android los renderiza
    /// nativo). Acá son íconos ilustrados procedurales (<see cref="SymbolSprite"/>): la
    /// fuente legacy de Unity (<c>LegacyRuntime.ttf</c>) no tiene glifos de emoji, así que
    /// las cartas se veían vacías (bug reportado por Ricardo jugando en el dispositivo,
    /// 23-sep).
    ///
    /// Gradiente de dificultad, siempre con colores vívidos (Ricardo: "los símbolos son
    /// muy opacos, oscuros, difíciles de recordar"): tier 0 = objetos y colores muy
    /// distintos; tier 1 = más variedad de objetos; tier 2 = mismo objeto en dos colores
    /// distintos (hay que recordar objeto Y color); tier 3 = mismo objeto en dos tonos
    /// análogos (variantes 0 y 3 de cada ícono).</summary>
    public static class SymbolBank
    {
        private static readonly SymbolDef[][] Tiers =
        {
            // Tier 0: 8 objetos cotidianos, cada uno de un color dominante distinto.
            new[]
            {
                new SymbolDef(ShapeKind.Apple, 0),
                new SymbolDef(ShapeKind.Star, 0),
                new SymbolDef(ShapeKind.Drop, 0),
                new SymbolDef(ShapeKind.Sun, 0),
                new SymbolDef(ShapeKind.Flower, 0),
                new SymbolDef(ShapeKind.Clover, 0),
                new SymbolDef(ShapeKind.Cloud, 0),
                new SymbolDef(ShapeKind.Crown, 0),
            },
            // Tier 1: más objetos, algunos repiten forma con otro color.
            new[]
            {
                new SymbolDef(ShapeKind.Heart, 0),
                new SymbolDef(ShapeKind.Fish, 0),
                new SymbolDef(ShapeKind.Mushroom, 0),
                new SymbolDef(ShapeKind.Balloon, 2),
                new SymbolDef(ShapeKind.Moon, 1),
                new SymbolDef(ShapeKind.Gem, 0),
                new SymbolDef(ShapeKind.Apple, 1),
                new SymbolDef(ShapeKind.Star, 1),
                new SymbolDef(ShapeKind.Flower, 1),
                new SymbolDef(ShapeKind.Drop, 1),
            },
            // Tier 2: pares del mismo objeto en dos colores claramente distintos.
            new[]
            {
                new SymbolDef(ShapeKind.Heart, 0),
                new SymbolDef(ShapeKind.Heart, 1),
                new SymbolDef(ShapeKind.Balloon, 0),
                new SymbolDef(ShapeKind.Balloon, 1),
                new SymbolDef(ShapeKind.Fish, 0),
                new SymbolDef(ShapeKind.Fish, 2),
                new SymbolDef(ShapeKind.Crown, 0),
                new SymbolDef(ShapeKind.Crown, 1),
            },
            // Tier 3 (máxima interferencia): pares del mismo objeto en tonos análogos.
            new[]
            {
                new SymbolDef(ShapeKind.Apple, 0),
                new SymbolDef(ShapeKind.Apple, 3),
                new SymbolDef(ShapeKind.Star, 0),
                new SymbolDef(ShapeKind.Star, 3),
                new SymbolDef(ShapeKind.Drop, 0),
                new SymbolDef(ShapeKind.Drop, 3),
                new SymbolDef(ShapeKind.Flower, 0),
                new SymbolDef(ShapeKind.Flower, 3),
            },
        };

        public static List<SymbolDef> Select(int pairCount, int interferenceLevel, System.Random random)
        {
            int level = interferenceLevel;
            if (level < 0) level = 0;
            if (level > Tiers.Length - 1) level = Tiers.Length - 1;

            var pool = new List<SymbolDef>();
            var seenKeys = new HashSet<string>();
            void AddFromTier(int tier)
            {
                foreach (var symbol in Shuffled(Tiers[tier], random))
                {
                    if (pool.Count >= pairCount) return;
                    if (!seenKeys.Add(symbol.Key)) continue;
                    pool.Add(symbol);
                }
            }

            for (int tier = level; tier >= 0 && pool.Count < pairCount; tier--) AddFromTier(tier);
            // La escalera fija de niveles (CardsGameContract.PairCountForStage) llega a
            // pairCount=12, pero con interferenceLevel bajo (0 o 1) los tiers 0..level solos
            // no siempre alcanzan esa cantidad (tier 0 solo tiene 8 símbolos distintos).
            // Antes esto dejaba el mazo con MENOS pares de los que _totalPairs esperaba --
            // el tablero nunca podía completarse (partida "pegada", Ricardo lo vio cerca del
            // nivel 8) y sobraban casillas de grilla sin carta asignada. Completar con los
            // tiers restantes (aunque sean "más difíciles") garantiza siempre pairCount
            // símbolos -- más de 25 distintos entre los 4 tiers, contra un máximo de 12.
            for (int tier = level + 1; tier < Tiers.Length && pool.Count < pairCount; tier++) AddFromTier(tier);

            return pool;
        }

        private static IEnumerable<SymbolDef> Shuffled(SymbolDef[] source, System.Random random)
        {
            var list = new List<SymbolDef>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}
