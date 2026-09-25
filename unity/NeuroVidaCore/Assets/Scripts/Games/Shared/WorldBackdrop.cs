using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// "Mundo" de fondo de un juego. Todos comparten el sello de la app: el mismo cielo nocturno
    /// (<see cref="NeuroStyle.NightGradient"/>), nebulosas y estrellas en perspectiva. Encima, cada juego tiene
    /// UN elemento propio ligado a su mecánica (lunas gemelas en Parejas, mar en Ruta del Tesoro, órbitas en
    /// Cambio de Chip...), para que jugar varios seguidos no se sienta repetitivo. Es decoración pura: todo va
    /// detrás del contenido y nada recibe toques.
    /// </summary>
    public sealed class GameWorld
    {
        public string Name;
        public Color NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.22f);
        public Vector2 NebulaAPos = new Vector2(0.12f, 0.88f);
        public Color NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.18f);
        public Vector2 NebulaBPos = new Vector2(0.92f, 0.12f);
        public int Stars = 50;
        public Vector2 VanishingPoint = new Vector2(0.5f, 0.62f);
        public int Bokeh;

        /// <summary>Lunas: posición normalizada (x, y) y tamaño en unidades del canvas (z).</summary>
        public Vector3[] Moons = new Vector3[0];
        public Color[] MoonTints = new Color[0];

        /// <summary>Mar en la parte baja (0 = sin mar), como fracción del alto.</summary>
        public float SeaHeight;
        public Color SeaTop = NeuroStyle.Hex(0x0E3B5C);
        public Color SeaBottom = NeuroStyle.Hex(0x04142A);

        public int OrbitRings;
        public Vector2 OrbitCenter = new Vector2(0.5f, 0.45f);

        /// <summary>Planetas de arcilla: posición normalizada (x, y) y diámetro (z); color en <see cref="PlanetColors"/>.</summary>
        public Vector3[] Planets = new Vector3[0];
        public Color[] PlanetColors = new Color[0];

        public int Constellations;
        public float MeteorEverySeconds;
        public string FloatingGlyphs;

        // ---- los mundos de los 9 juegos ----

        public static GameWorld Constellation => new GameWorld
        {
            Name = "Constelación", Constellations = 3,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f),
        };

        public static GameWorld TwinMoons => new GameWorld
        {
            Name = "Lunas gemelas", Bokeh = 5,
            Moons = new[] { new Vector3(0.74f, 0.845f, 150f), new Vector3(0.885f, 0.80f, 96f) },
            MoonTints = new[] { NeuroStyle.Hex(0xFFF4D6), NeuroStyle.Hex(0xE4DCFF) },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.26f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.16f),
        };

        public static GameWorld MoonlitIsland => new GameWorld
        {
            Name = "Isla bajo la luna", SeaHeight = 0.2f, Stars = 40, VanishingPoint = new Vector2(0.5f, 0.3f),
            Moons = new[] { new Vector3(0.80f, 0.83f, 170f) }, MoonTints = new[] { NeuroStyle.Hex(0xFFF4D6) },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.12f),
            NebulaBPos = new Vector2(0.2f, 0.3f),
        };

        public static GameWorld Neon => new GameWorld
        {
            Name = "Neón", Bokeh = 9,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.26f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.26f),
        };

        public static GameWorld PlanetDuel => new GameWorld
        {
            Name = "Duelo de planetas",
            Planets = new[] { new Vector3(-0.02f, 0.06f, 620f), new Vector3(1.03f, 0.93f, 520f) },
            PlanetColors = new[] { NeuroStyle.Sky, NeuroStyle.Coral },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.18f), NebulaAPos = new Vector2(0.9f, 0.85f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f), NebulaBPos = new Vector2(0.1f, 0.15f),
        };

        public static GameWorld Orbits => new GameWorld
        {
            Name = "Órbitas", OrbitRings = 4,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.14f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f),
        };

        public static GameWorld MeteorShower => new GameWorld
        {
            Name = "Lluvia de meteoros", MeteorEverySeconds = 2.4f,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.12f),
        };

        public static GameWorld MoonPond => new GameWorld
        {
            Name = "Estanque bajo la luna", Stars = 40, VanishingPoint = new Vector2(0.5f, 0.35f),
            Moons = new[] { new Vector3(0.18f, 0.80f, 150f) }, MoonTints = new[] { NeuroStyle.Hex(0xFFF4D6) },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.22f), NebulaAPos = new Vector2(0.85f, 0.85f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.14f),
        };

        public static GameWorld SkyLetters => new GameWorld
        {
            Name = "Letras del cielo", FloatingGlyphs = "AEMNORSLTUVIPCDGBÑ",
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.18f),
        };
    }

    /// <summary>Arma el fondo de un <see cref="GameWorld"/> dentro de un RectTransform que ocupa la pantalla.</summary>
    public static class WorldBackdrop
    {
        public static void Build(RectTransform bgRect, GameWorld world)
        {
            var bg = bgRect.gameObject.GetComponent<Image>();
            if (bg == null) bg = bgRect.gameObject.AddComponent<Image>();
            bg.sprite = NeuroStyle.NightGradient();
            bg.color = Color.white;
            bg.raycastTarget = false;

            UiFx.AddBackgroundGlow(bgRect, world.NebulaAPos, 1600f, world.NebulaA);
            UiFx.AddBackgroundGlow(bgRect, world.NebulaBPos, 1500f, world.NebulaB);

            var starsRect = Layer(bgRect, "Stars");
            var stars = starsRect.gameObject.AddComponent<StarfieldFx>();
            stars.VanishingPoint = world.VanishingPoint;
            stars.Build(starsRect, world.Stars, 10f, world.Name.GetHashCode());

            var ambient = bgRect.gameObject.AddComponent<WorldAmbient>();
            ambient.Init(bgRect);

            if (world.SeaHeight > 0f) AddSea(bgRect, ambient, world); // antes que las lunas: su reflejo va encima del mar
            for (int i = 0; i < world.Moons.Length; i++)
                AddMoon(bgRect, ambient, world.Moons[i], i < world.MoonTints.Length ? world.MoonTints[i] : NeuroStyle.Cream, world.SeaHeight);
            if (world.OrbitRings > 0) AddOrbits(bgRect, ambient, world);
            for (int i = 0; i < world.Planets.Length; i++)
                AddPlanet(bgRect, world.Planets[i], i < world.PlanetColors.Length ? world.PlanetColors[i] : NeuroStyle.Grape);
            if (world.Constellations > 0) AddConstellations(bgRect, ambient, world.Constellations, world.Name.GetHashCode());
            if (world.MeteorEverySeconds > 0f) ambient.EnableMeteors(world.MeteorEverySeconds);
            if (!string.IsNullOrEmpty(world.FloatingGlyphs)) ambient.AddGlyphs(world.FloatingGlyphs, 14);
            if (world.Bokeh > 0) bgRect.gameObject.AddComponent<CountdownAmbient>().Build(bgRect, world.Bokeh);
        }

        // ---- piezas ----

        private static void AddMoon(RectTransform parent, WorldAmbient ambient, Vector3 moon, Color tint, float seaHeight)
        {
            var anchor = new Vector2(moon.x, moon.y);
            float size = moon.z;
            var glow = Node(parent, "MoonGlow", anchor, new Vector2(size * 3.6f, size * 3.6f));
            var glowImg = Img(glow, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(tint, 0.26f));
            ambient.Breathe(glowImg, 0.26f, 0.08f);

            var disc = Node(parent, "Moon", anchor, new Vector2(size, size));
            Img(disc, DiscSprite.Get(), tint);
            // Cráteres suaves: dan volumen sin dibujar una cara.
            var craters = new[] { new Vector3(-0.18f, 0.12f, 0.26f), new Vector3(0.2f, -0.14f, 0.18f), new Vector3(0.05f, 0.26f, 0.12f) };
            foreach (var c in craters)
            {
                var r = Node(disc, "Crater", new Vector2(0.5f, 0.5f), new Vector2(size * c.z, size * c.z));
                r.anchoredPosition = new Vector2(c.x * size, c.y * size);
                Img(r, DiscSprite.Get(), new Color(0.55f, 0.5f, 0.7f, 0.16f));
            }

            // Reflejo en el mar: una columna de luz que titila bajo la luna.
            if (seaHeight > 0f)
            {
                var refl = Node(parent, "MoonReflection", new Vector2(moon.x, 0f), new Vector2(size * 0.9f, 0f));
                refl.anchorMin = new Vector2(moon.x, seaHeight * 0.06f);
                refl.anchorMax = new Vector2(moon.x, seaHeight * 0.94f); // se estira con el alto real del mar
                var reflImg = Img(refl, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(tint, 0.22f));
                ambient.Breathe(reflImg, 0.22f, 0.09f, 2.3f);
            }
        }

        private static void AddSea(RectTransform parent, WorldAmbient ambient, GameWorld world)
        {
            var sea = Layer(parent, "Sea");
            sea.anchorMax = new Vector2(1f, world.SeaHeight); // se agrega después de las estrellas: las tapa
            Img(sea, VerticalGradient(world.SeaTop, world.SeaBottom), Color.white);

            // Línea del horizonte y destellos horizontales que se mecen.
            var line = Node(sea, "Horizon", new Vector2(0.5f, 1f), new Vector2(0f, 6f));
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            Img(line, null, NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.35f));
            for (int i = 0; i < 5; i++)
            {
                float y = 0.2f + i * 0.15f;
                var s = Node(sea, "Shimmer", new Vector2(0.15f + (i * 0.37f) % 0.7f, y), new Vector2(160f + i * 30f, 6f));
                var sImg = Img(s, RoundedRectSprite.Get(4), NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.10f));
                sImg.type = Image.Type.Sliced;
                ambient.Sway(s, 40f + i * 8f, 0.35f + i * 0.07f);
                ambient.Breathe(sImg, 0.10f, 0.06f, 1.2f + i * 0.3f);
            }
        }

        private static void AddOrbits(RectTransform parent, WorldAmbient ambient, GameWorld world)
        {
            var satColors = new[] { NeuroStyle.Lime, NeuroStyle.Sky, NeuroStyle.Sun, NeuroStyle.Grape };
            for (int i = 0; i < world.OrbitRings; i++)
            {
                float d = 820f + i * 420f;
                var ring = Node(parent, "Orbit", world.OrbitCenter, new Vector2(d, d));
                Img(ring, ThinRingSprite(), NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.09f - i * 0.012f));
                // Un satélite por órbita, en sentidos alternos y a distinta velocidad.
                var sat = Node(parent, "Satellite", world.OrbitCenter, new Vector2(26f + i * 4f, 26f + i * 4f));
                Img(sat, DiscSprite.Get(), NeuroStyle.WithAlpha(satColors[i % satColors.Length], 0.8f));
                var satGlow = Node(sat, "Glow", new Vector2(0.5f, 0.5f), new Vector2(110f, 110f));
                Img(satGlow, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(satColors[i % satColors.Length], 0.35f));
                ambient.Orbit(sat, d * 0.5f * 0.985f, (i % 2 == 0 ? 1f : -1f) * (0.22f - i * 0.03f), i * 1.7f);
            }
        }

        private static void AddPlanet(RectTransform parent, Vector3 planet, Color color)
        {
            // Planeta de arcilla semi oculto en una esquina: borde tinta, relleno apagado hacia la noche (es fondo,
            // no debe competir con el juego) y un brillo arriba a la izquierda.
            var anchor = new Vector2(planet.x, planet.y);
            float size = planet.z;
            var glow = Node(parent, "PlanetGlow", anchor, new Vector2(size * 1.9f, size * 1.9f));
            Img(glow, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(color, 0.22f));
            var border = Node(parent, "Planet", anchor, new Vector2(size + 18f, size + 18f));
            Img(border, DiscSprite.Get(), NeuroStyle.Ink);
            var fill = Node(border, "Fill", new Vector2(0.5f, 0.5f), new Vector2(size, size));
            Img(fill, DiscSprite.Get(), Color.Lerp(NeuroStyle.NightMid, color, 0.62f));
            var band = Node(fill, "Band", new Vector2(0.5f, 0.42f), new Vector2(size * 0.95f, size * 0.12f));
            var bandImg = Img(band, RoundedRectSprite.Get(16), NeuroStyle.WithAlpha(NeuroStyle.Ink, 0.18f));
            bandImg.type = Image.Type.Sliced;
            var shine = Node(fill, "Shine", new Vector2(0.33f, 0.68f), new Vector2(size * 0.32f, size * 0.32f));
            Img(shine, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.35f));
        }

        private static void AddConstellations(RectTransform parent, WorldAmbient ambient, int count, int seed)
        {
            var rng = new System.Random(seed);
            // En los márgenes (arriba-izquierda, abajo-derecha, medio-izquierda), lejos del tablero central.
            var zones = new[] { new Rect(0.05f, 0.70f, 0.35f, 0.14f), new Rect(0.60f, 0.06f, 0.35f, 0.14f), new Rect(0.04f, 0.30f, 0.18f, 0.2f) };
            for (int c = 0; c < count && c < zones.Length; c++)
            {
                var zone = zones[c];
                int n = 4 + rng.Next(3);
                var points = new List<Vector2>();
                for (int i = 0; i < n; i++)
                    points.Add(new Vector2(zone.x + (float)rng.NextDouble() * zone.width, zone.y + (float)rng.NextDouble() * zone.height));
                points.Sort((a, b) => a.x.CompareTo(b.x));
                for (int i = 0; i < n - 1; i++) AddLine(parent, ambient, points[i], points[i + 1]);
                for (int i = 0; i < n; i++)
                {
                    var star = Node(parent, "ConstellationStar", points[i], new Vector2(46f, 46f));
                    var img = Img(star, SparkleSprite.Get(), NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.75f));
                    ambient.Breathe(img, 0.6f, 0.35f, 1.1f + (float)rng.NextDouble() * 1.4f);
                }
            }
        }

        private static void AddLine(RectTransform parent, WorldAmbient ambient, Vector2 a, Vector2 b)
        {
            // Largo y ángulo dependen del tamaño real de la pantalla (varía según el alto del teléfono):
            // los calcula WorldAmbient cuando ese tamaño ya se conoce.
            var line = Node(parent, "ConstellationLine", (a + b) * 0.5f, new Vector2(0f, 3f));
            Img(line, null, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.16f));
            ambient.Connect(line, a, b);
        }

        // ---- utilidades ----

        private static RectTransform Layer(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Node(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Img(RectTransform rect, Sprite sprite, Color color)
        {
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static readonly Dictionary<long, Sprite> Gradients = new Dictionary<long, Sprite>();

        private static Sprite VerticalGradient(Color top, Color bottom)
        {
            long key = ((long)ColorUtility.ToHtmlStringRGB(top).GetHashCode() << 32) ^ (uint)ColorUtility.ToHtmlStringRGB(bottom).GetHashCode();
            if (Gradients.TryGetValue(key, out var cached) && cached != null) return cached;
            const int h = 128;
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 100f);
            Gradients[key] = sprite;
            return sprite;
        }

        private static Sprite _thinRing;

        /// <summary>Anillo fino (órbita), blanco: el <see cref="RingSprite"/> es demasiado grueso para esto.</summary>
        private static Sprite ThinRingSprite()
        {
            if (_thinRing != null) return _thinRing;
            const int size = 512;
            const float radius = 0.492f, half = 0.0035f, edge = 0.002f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius);
                    float a = 1f - Mathf.Clamp01((d - half) / edge);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pixels);
            tex.Apply();
            _thinRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _thinRing;
        }
    }

    /// <summary>Animaciones lentas del fondo de un mundo (respirar, mecerse, orbitar, meteoros, letras que suben).
    /// Todo en tiempo sin escala y sin tocar nada del juego.</summary>
    public sealed class WorldAmbient : MonoBehaviour
    {
        private RectTransform _area;
        private float _time;

        private readonly List<(Image img, float baseA, float amp, float speed)> _breathers = new List<(Image, float, float, float)>();
        private readonly List<(RectTransform rect, Vector2 home, float amp, float speed)> _swayers = new List<(RectTransform, Vector2, float, float)>();
        private readonly List<(RectTransform rect, float radius, float speed, float phase)> _orbiters = new List<(RectTransform, float, float, float)>();
        private readonly List<(RectTransform rect, Vector2 a, Vector2 b)> _lines = new List<(RectTransform, Vector2, Vector2)>();
        private Vector2 _linesLaidFor;

        private float _meteorEvery, _nextMeteor;
        private RectTransform[] _meteors;
        private Image[] _meteorImages;
        private float[] _meteorAge;
        private Vector2[] _meteorFrom, _meteorDir;

        private RectTransform[] _glyphs;
        private Vector2[] _glyphNorm;
        private float[] _glyphSpeed;

        public void Init(RectTransform area) => _area = area;

        public void Breathe(Image img, float baseAlpha, float amplitude, float speed = 0.8f) =>
            _breathers.Add((img, baseAlpha, amplitude, speed));

        public void Sway(RectTransform rect, float amplitude, float speed) =>
            _swayers.Add((rect, rect.anchoredPosition, amplitude, speed));

        public void Orbit(RectTransform rect, float radius, float speed, float phase) =>
            _orbiters.Add((rect, radius, speed, phase));

        /// <summary>Línea entre dos puntos normalizados del área (constelaciones).</summary>
        public void Connect(RectTransform line, Vector2 a, Vector2 b) => _lines.Add((line, a, b));

        public void EnableMeteors(float everySeconds)
        {
            _meteorEvery = everySeconds;
            _nextMeteor = 0.8f;
            _meteors = new RectTransform[2];
            _meteorImages = new Image[2];
            _meteorAge = new[] { 99f, 99f };
            _meteorFrom = new Vector2[2];
            _meteorDir = new Vector2[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Meteor");
                go.transform.SetParent(_area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0f); // la cabeza va adelante; la estela queda atrás
                rect.sizeDelta = new Vector2(14f, 300f);
                var img = go.AddComponent<Image>();
                img.sprite = RadialGlowSprite.Get();
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f);
                _meteors[i] = rect;
                _meteorImages[i] = img;
            }
        }

        public void AddGlyphs(string letters, int count)
        {
            var rng = new System.Random(3);
            var colors = new[] { NeuroStyle.Grape, NeuroStyle.Coral, NeuroStyle.Cream, NeuroStyle.Sky };
            _glyphs = new RectTransform[count];
            _glyphNorm = new Vector2[count];
            _glyphSpeed = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Glyph");
                go.transform.SetParent(_area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(220f, 220f);
                rect.localRotation = Quaternion.Euler(0f, 0f, ((float)rng.NextDouble() - 0.5f) * 40f);
                var text = go.AddComponent<Text>();
                text.font = UiFonts.Bold;
                text.text = letters[rng.Next(letters.Length)].ToString();
                text.fontSize = 70 + rng.Next(90);
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.color = NeuroStyle.WithAlpha(colors[i % colors.Length], 0.07f + (float)rng.NextDouble() * 0.07f);
                _glyphs[i] = rect;
                _glyphNorm[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                _glyphSpeed[i] = 0.012f + (float)rng.NextDouble() * 0.02f;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            if (_area == null) return;
            Vector2 size = _area.rect.size;

            foreach (var b in _breathers)
            {
                var c = b.img.color;
                c.a = Mathf.Max(0f, b.baseA + b.amp * Mathf.Sin(_time * b.speed * Mathf.PI * 2f * 0.25f));
                b.img.color = c;
            }
            foreach (var s in _swayers)
                s.rect.anchoredPosition = s.home + new Vector2(Mathf.Sin(_time * s.speed) * s.amp, 0f);
            foreach (var o in _orbiters)
            {
                float a = o.phase + _time * o.speed;
                o.rect.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * o.radius;
            }

            if (_lines.Count > 0 && size != _linesLaidFor && size.x > 0f)
            {
                _linesLaidFor = size;
                foreach (var l in _lines)
                {
                    var pa = Vector2.Scale(l.a, size);
                    var pb = Vector2.Scale(l.b, size);
                    l.rect.sizeDelta = new Vector2(Vector2.Distance(pa, pb), 3f);
                    l.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
                }
            }

            if (_meteors != null) UpdateMeteors(dt, size);

            if (_glyphs != null)
            {
                for (int i = 0; i < _glyphs.Length; i++)
                {
                    _glyphNorm[i].y += _glyphSpeed[i] * dt;
                    if (_glyphNorm[i].y > 1.1f) _glyphNorm[i].y = -0.1f;
                    float sway = Mathf.Sin(_time * 0.4f + i) * 0.015f;
                    _glyphs[i].anchoredPosition = new Vector2((_glyphNorm[i].x + sway) * size.x, _glyphNorm[i].y * size.y);
                }
            }
        }

        private void UpdateMeteors(float dt, Vector2 size)
        {
            _nextMeteor -= dt;
            if (_nextMeteor <= 0f)
            {
                _nextMeteor = _meteorEvery; // ritmo regular a propósito: el mundo de las series tiene un patrón
                int slot = _meteorAge[0] > _meteorAge[1] ? 0 : 1;
                _meteorAge[slot] = 0f;
                float x = Random.Range(0.25f, 1.05f) * size.x;
                _meteorFrom[slot] = new Vector2(x, size.y * Random.Range(0.72f, 0.98f));
                _meteorDir[slot] = new Vector2(-0.82f, -0.57f); // siempre en diagonal hacia abajo-izquierda
                _meteors[slot].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_meteorDir[slot].y, _meteorDir[slot].x) * Mathf.Rad2Deg + 90f);
            }
            for (int i = 0; i < _meteors.Length; i++)
            {
                _meteorAge[i] += dt;
                const float life = 0.9f;
                float t = _meteorAge[i] / life;
                if (t > 1f)
                {
                    _meteorImages[i].color = new Color(1f, 1f, 1f, 0f);
                    continue;
                }
                _meteors[i].anchoredPosition = _meteorFrom[i] + _meteorDir[i] * (t * 900f);
                _meteorImages[i].color = new Color(1f, 0.95f, 0.85f, Mathf.Sin(t * Mathf.PI) * 0.85f);
            }
        }
    }
}
