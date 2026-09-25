using System.IO;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.RutaTesoro;
using NeuroVida.Games.Secuencia;
using UnityEditor;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Herramienta de desarrollo: vuelca a PNG las hojas de contacto de los íconos y las
    /// cartas de Parejas Ocultas (generados por código, sin assets) para poder revisarlos
    /// a ojo sin instalar la app en el dispositivo. No forma parte del build.
    ///
    /// Unity.exe -batchmode -nographics -quit -projectPath &lt;path&gt;
    ///   -executeMethod NeuroVida.Bridge.EditorTools.SymbolPreviewExporter.Export
    /// Salida: unity/test-results/preview-symbols.png y preview-cards.png
    /// </summary>
    public static class SymbolPreviewExporter
    {
        public static void Export()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "test-results"));
            Directory.CreateDirectory(outDir);

            ExportSymbols(Path.Combine(outDir, "preview-symbols.png"));
            ExportCards(Path.Combine(outDir, "preview-cards.png"));
            ExportHearts(Path.Combine(outDir, "preview-hearts.png"));
            ExportTiles(Path.Combine(outDir, "preview-tiles.png"));
            ExportTreasures(Path.Combine(outDir, "preview-treasures.png"));
            Debug.Log("[SymbolPreview] OK -> " + outDir);
        }

        private static void ExportSymbols(string path)
        {
            var kinds = (ShapeKind[])System.Enum.GetValues(typeof(ShapeKind));
            const int cell = 160;
            int cols = SymbolSprite.VariantCount;
            int rows = kinds.Length;
            int width = cols * cell;
            int height = rows * cell;
            var sheet = NewSheet(width, height, new Color32(0xF4, 0xF0, 0xFC, 255));

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var sprite = SymbolSprite.Get(kinds[r], c);
                    Blit(sheet, width, sprite.texture, c * cell, (rows - 1 - r) * cell, cell, cell);
                }
            }
            Save(sheet, width, height, path);
        }

        private static void ExportCards(string path)
        {
            const int cell = 256;
            var faces = new[] { CardSprites.Face.Back, CardSprites.Face.Front, CardSprites.Face.Matched };
            int width = (faces.Length + 2) * cell;
            var sheet = NewSheet(width, cell, new Color32(0x0F, 0x17, 0x2A, 255));

            for (int i = 0; i < faces.Length; i++)
            {
                Blit(sheet, width, CardSprites.Get(faces[i]).texture, i * cell, 0, cell, cell);
            }

            // Cartas con un ícono encima (frente y pareja resuelta), como se ven en el juego.
            int iconX = (int)(cell * 0.10f);
            int iconY = (int)(cell * 0.16f);
            int iconW = (int)(cell * 0.80f);
            int iconH = (int)(cell * 0.76f);
            Blit(sheet, width, CardSprites.Get(CardSprites.Face.Front).texture, 3 * cell, 0, cell, cell);
            Blit(sheet, width, SymbolSprite.Get(ShapeKind.Apple, 0).texture, 3 * cell + iconX, iconY, iconW, iconH);
            Blit(sheet, width, CardSprites.Get(CardSprites.Face.Matched).texture, 4 * cell, 0, cell, cell);
            Blit(sheet, width, SymbolSprite.Get(ShapeKind.Fish, 0).texture, 4 * cell + iconX, iconY, iconW, iconH);

            Save(sheet, width, cell, path);
        }

        private static void ExportHearts(string path)
        {
            const int cell = 128;
            int width = 3 * cell;
            var sheet = NewSheet(width, cell, new Color32(0x0F, 0x17, 0x2A, 255));
            Blit(sheet, width, HeartSprite.GetFull().texture, 0, 0, cell, cell);
            Blit(sheet, width, HeartSprite.GetLost().texture, cell, 0, cell, cell);
            Blit(sheet, width, HeartSprite.GetFull().texture, 2 * cell, 0, cell / 2, cell / 2);
            Blit(sheet, width, HeartSprite.GetLost().texture, 2 * cell + cell / 2, 0, cell / 2, cell / 2);
            Save(sheet, width, cell, path);
        }

        private static void ExportTiles(string path)
        {
            const int cell = 160;
            const int cols = 8;
            int width = cols * cell;
            int height = 2 * cell;
            var sheet = NewSheet(width, height, new Color32(0x0F, 0x17, 0x2A, 255));
            var tile = TileSprites.Get().texture.GetPixels32();
            int ts = TileSprites.Get().texture.width;
            for (int i = 0; i < cols; i++)
            {
                var info = TilePalette.Get(i);
                for (int row = 0; row < 2; row++)
                {
                    Color c = row == 0 ? info.LightColor : info.NormalColor; // fila de arriba: encendida
                    for (int y = 0; y < cell; y++)
                    {
                        for (int x = 0; x < cell; x++)
                        {
                            var src = tile[(y * ts / cell) * ts + (x * ts / cell)];
                            var tinted = new Color32(
                                (byte)(src.r * c.r), (byte)(src.g * c.g), (byte)(src.b * c.b), src.a);
                            int di = ((1 - row) * cell + y) * width + (i * cell + x);
                            sheet[di] = Over(tinted, sheet[di]);
                        }
                    }
                }
            }
            Save(sheet, width, height, path);
        }

        private static void ExportTreasures(string path)
        {
            const int cell = 200;
            int width = 6 * cell;
            int height = cell;
            var sheet = NewSheet(width, height, new Color32(0x08, 0x1B, 0x36, 255));
            var tile = TileSprites.Get().texture.GetPixels32();
            int ts = TileSprites.Get().texture.width;
            var sand = new Color(0xF0 / 255f, 0xD9 / 255f, 0xA6 / 255f);
            for (int i = 0; i < 6; i++)
            {
                for (int y = 0; y < cell; y++)
                {
                    for (int x = 0; x < cell; x++)
                    {
                        var src = tile[(y * ts / cell) * ts + (x * ts / cell)];
                        var tinted = new Color32((byte)(src.r * sand.r), (byte)(src.g * sand.g), (byte)(src.b * sand.b), src.a);
                        sheet[y * width + i * cell + x] = Over(tinted, sheet[y * width + i * cell + x]);
                    }
                }
                int pad = cell / 6;
                Blit(sheet, width, TreasureSprites.ForIndex(i).texture, i * cell + pad, pad + cell / 24, cell - pad * 2, cell - pad * 2);
            }
            Save(sheet, width, height, path);
        }

        private static Color32[] NewSheet(int w, int h, Color32 fill)
        {
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
            return px;
        }

        /// <summary>Pega <paramref name="src"/> (escalado al tamaño w x h) sobre la hoja en (ox, oy).</summary>
        private static void Blit(Color32[] dst, int dstW, Texture2D src, int ox, int oy, int w, int h)
        {
            var sp = src.GetPixels32();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int sx = x * src.width / w;
                    int sy = y * src.height / h;
                    int di = (oy + y) * dstW + (ox + x);
                    if (di < 0 || di >= dst.Length) continue;
                    dst[di] = Over(sp[sy * src.width + sx], dst[di]);
                }
            }
        }

        private static Color32 Over(Color32 top, Color32 bottom)
        {
            float a = top.a / 255f;
            return new Color32(
                (byte)(top.r * a + bottom.r * (1f - a)),
                (byte)(top.g * a + bottom.g * (1f - a)),
                (byte)(top.b * a + bottom.b * (1f - a)),
                255);
        }

        private static void Save(Color32[] px, int w, int h, string path)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
