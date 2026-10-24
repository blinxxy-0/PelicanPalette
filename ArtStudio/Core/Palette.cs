using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

// How a pigment's "true hue" marker moves on the mixing bar. Same families
// as the fishing minigame's fish, so a player who has learned fishing reads
// these at a glance: earth tones are smooth, minerals dart, rare pigments
// mix it up.
public enum MixMotion { Mixed, Dart, Smooth, Sinker, Floater }

public sealed record Pigment(string Id, string Name, uint Rgb, int Difficulty, MixMotion Motion, bool Starter, bool Rare = false)
{
    public byte R => (byte)(Rgb >> 16);
    public byte G => (byte)(Rgb >> 8);
    public byte B => (byte)Rgb;
}

public static class Palette
{
    // Indices are saved inside every painting, so this list is append-only:
    // never reorder or remove an entry.
    public static readonly Pigment[] All =
    {
        new("charcoal", "Charcoal", 0x2E2430, 15, MixMotion.Smooth, true),
        new("umber", "Umber", 0x6E4A2F, 15, MixMotion.Smooth, true),
        new("sienna", "Sienna", 0xA85D33, 25, MixMotion.Smooth, false),
        new("ochre", "Ochre", 0xDCA03E, 20, MixMotion.Smooth, true),
        new("cream", "Cream", 0xF3E5C2, 20, MixMotion.Floater, true),
        new("stone", "Stone", 0x8F8A86, 20, MixMotion.Smooth, true),
        new("crimson", "Crimson", 0xC23C3C, 35, MixMotion.Mixed, true),
        new("rose", "Rose", 0xE8899E, 40, MixMotion.Floater, false),
        new("marigold", "Marigold", 0xF4CB3A, 35, MixMotion.Mixed, false),
        new("moss", "Moss", 0x4E7A31, 25, MixMotion.Smooth, true),
        new("leaf", "Leaf", 0x8DC044, 35, MixMotion.Mixed, false),
        new("slate", "Slate", 0x4A6894, 30, MixMotion.Sinker, true),
        new("cerulean", "Cerulean", 0x5FB0E3, 45, MixMotion.Dart, false),
        new("plum", "Plum", 0x7A3E77, 45, MixMotion.Sinker, false),
        new("snow", "Snow", 0xF8FBFF, 30, MixMotion.Floater, false),
        new("violet", "Iridium Violet", 0x8E5DDB, 75, MixMotion.Dart, false, true),
        new("aquamarine", "Aquamarine", 0x3CC7B4, 70, MixMotion.Mixed, false, true),
        new("ruby", "Ruby Red", 0xE3204F, 80, MixMotion.Dart, false, true),
    };

    // Pigments close enough that a viewer would call one "a bit off" for the
    // other rather than simply wrong. Derived from color distance so new
    // pigments slot in without a hand-kept pair list.
    private const double SimilarDistance = 120;
    private static readonly bool[,] Similar = BuildSimilar();

    public static int IndexOf(string id) => Array.FindIndex(All, p => p.Id == id);

    public static bool AreSimilar(int a, int b) => a != b && Similar[a, b];

    // Nearest allowed pigment by "redmean" distance, a cheap perceptual
    // weighting that keeps skin tones and greens from snapping to grey.
    public static int Nearest(byte r, byte g, byte b, IReadOnlyList<int> allowed)
    {
        int best = allowed[0];
        double bestDist = double.MaxValue;
        foreach (int i in allowed)
        {
            double d = Distance(r, g, b, All[i].R, All[i].G, All[i].B);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    public static double Distance(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        double rm = (r1 + r2) / 2.0;
        int dr = r1 - r2, dg = g1 - g2, db = b1 - b2;
        return Math.Sqrt((2 + rm / 256) * dr * dr + 4 * dg * dg + (2 + (255 - rm) / 256) * db * db);
    }

    private static bool[,] BuildSimilar()
    {
        int n = All.Length;
        var similar = new bool[n, n];
        for (int a = 0; a < n; a++)
        for (int b = 0; b < n; b++)
            similar[a, b] = a != b && Distance(All[a].R, All[a].G, All[a].B, All[b].R, All[b].G, All[b].B) < SimilarDistance;
        return similar;
    }
}
