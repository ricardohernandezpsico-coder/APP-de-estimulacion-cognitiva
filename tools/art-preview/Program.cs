using System;
using System.IO;
using NeuroVida.Games.Parejas;
using NeuroVida.Games.RutaTesoro;
using NeuroVida.Games.Secuencia;
using UnityEngine;

/// Vuelca cada sprite procedural a <carpeta>/<nombre>.raw (int32 lado + RGBA, fila 0 = abajo).
internal static class Program
{
    private static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : "art-raw";
        Directory.CreateDirectory(dir);

        void Dump(string name, Sprite sprite)
        {
            using var file = File.Create(Path.Combine(dir, name + ".raw"));
            var w = new BinaryWriter(file);
            w.Write(sprite.texture.width);
            foreach (var c in sprite.texture.pixels) { w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a); }
        }

        foreach (ShapeKind kind in Enum.GetValues(typeof(ShapeKind)))
            for (int v = 0; v < SymbolSprite.VariantCount; v++)
                Dump($"sym_{kind}_{v}", SymbolSprite.Get(kind, v));
        foreach (CardSprites.Face face in Enum.GetValues(typeof(CardSprites.Face)))
            Dump("card_" + face, CardSprites.Get(face));
        for (int i = 0; i < 6; i++) Dump("treasure_" + i, TreasureSprites.ForIndex(i));
        Dump("heart_full", HeartSprite.GetFull());
        Dump("heart_lost", HeartSprite.GetLost());
        Console.WriteLine("OK -> " + dir);
    }
}
