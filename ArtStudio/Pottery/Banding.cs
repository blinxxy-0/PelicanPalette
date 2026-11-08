#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ArtStudio.Pottery;

// Banding (docs/design/pottery_minigame.md, section 5): the thrown pot turns on
// the banding wheel. Dip it in a base glaze, then touch a brush to it at a
// height and hold: one turn of the wheel paints a ring. Drag to widen the
// band. Your hand drifts the longer you hold, so a band painted without
// watching the brush comes out wavy. Matching the design's bands (colour,
// height, width) is the grade.
//
// The glaze is kept on a grid wrapped round the pot: slices up the wall (three
// to a row of clay) by buckets round it (32), so a wavy band really is wavy,
// and you see its waves come round as the wheel turns.
internal sealed record Glaze(string Name, string Mineral, Color Colour, bool Speckled = false);

internal static class Glazes
{
    public static readonly Glaze[] All =
    {
        new("Clear", "Quartz", new Color(196, 108, 66)),
        new("Celadon", "Jade", new Color(124, 176, 146)),
        new("Tenmoku", "Iron Ore", new Color(78, 48, 36)),
        new("Cobalt", "Aquamarine", new Color(54, 88, 172)),
        new("Ash", "Coal", new Color(176, 160, 128), Speckled: true),
        new("Copper red", "Fire Quartz", new Color(172, 42, 40)),
    };

    public static int Index(string name) => Array.FindIndex(All, g => g.Name == name);
}

// A pot's design: a base coat, and bands given as fractions of the pot's height,
// so it fits the pot you actually threw.
internal sealed record GlazeBand(int Glaze, float From, float To);

internal sealed record GlazeDesign(int Base, GlazeBand[] Bands)
{
    public static GlazeDesign For(string shape) => shape switch
    {
        "bowl" => new(G("Tenmoku"), new[] { new GlazeBand(G("Ash"), 0.52f, 0.78f), new GlazeBand(G("Celadon"), 0.9f, 1f) }),
        "vase" => new(G("Celadon"), new[] { new GlazeBand(G("Tenmoku"), 0.72f, 0.86f), new GlazeBand(G("Cobalt"), 0.4f, 0.47f) }),
        "jug" => new(G("Ash"), new[] { new GlazeBand(G("Cobalt"), 0.62f, 0.71f), new GlazeBand(G("Copper red"), 0.28f, 0.35f) }),
        _ => new(G("Clear"), new[] { new GlazeBand(G("Cobalt"), 0.78f, 0.9f), new GlazeBand(G("Tenmoku"), 0.24f, 0.31f) }),
    };

    private static int G(string name) => Glazes.Index(name);

    // A band's slices on a pot of so many slices: those whose middles fall inside it.
    public static (int From, int To) Slices(GlazeBand band, int slices)
    {
        int from = (int)MathF.Ceiling(band.From * slices - 0.5f);
        int to = (int)MathF.Floor(band.To * slices - 0.5f);
        from = Math.Clamp(from, 0, slices - 1);
        to = Math.Clamp(Math.Max(from, to), 0, slices - 1);
        return (from, to);
    }

    // Where a band sits, in a potter's words.
    public static string Where(GlazeBand band)
    {
        float mid = (band.From + band.To) / 2f;
        string at = band.To >= 0.97f ? "at the rim" : mid > 0.66f ? "high" : mid > 0.36f ? "middle" : "low";
        return $"{(band.To - band.From < 0.1f ? "thin" : "wide")}, {at}";
    }
}

internal sealed class PotGlaze
{
    public const int SlicesPerRow = 3, Buckets = 32;
    public const float BrushRows = 1f;        // the brush's width, in rows of clay

    public int Slices { get; }
    public int Base { get; private set; } = -1;       // -1: bisque, not dipped yet
    private readonly int[] _band;                      // -1 where there's only the base

    public PotGlaze(int rows)
    {
        Slices = Math.Max(1, rows * SlicesPerRow);
        _band = new int[Slices * Buckets];
        Array.Fill(_band, -1);
    }

    public int Band(int slice, int bucket) => _band[slice * Buckets + bucket];
    public int At(int slice, int bucket) => Band(slice, bucket) is var g && g >= 0 ? g : Base;
    public bool AnyBands => _band.Any(g => g >= 0);

    // Dipping coats the whole pot: any bands under it are covered.
    public void Dip(int glaze)
    {
        Base = glaze;
        Sponge();
    }

    public void Sponge() => Array.Fill(_band, -1);

    // The brush at a height (in rows from the foot) while the wheel turns from one
    // bucket to the next: every bucket it passed, at every slice under the brush.
    public void Paint(int glaze, float height, float fromBucket, float toBucket)
    {
        float half = BrushRows / 2f;
        int s0 = Math.Max(0, (int)MathF.Ceiling((height - half) * SlicesPerRow - 0.5f));
        int s1 = Math.Min(Slices - 1, (int)MathF.Floor((height + half) * SlicesPerRow - 0.5f));
        if (s1 < s0)
            return;
        int b0 = (int)MathF.Floor(fromBucket), b1 = (int)MathF.Floor(toBucket);
        if (b1 < b0)
            (b0, b1) = (b1, b0);
        b1 = Math.Min(b1, b0 + Buckets - 1);
        for (int b = b0; b <= b1; b++)
        {
            int k = ((b % Buckets) + Buckets) % Buckets;
            for (int s = s0; s <= s1; s++)
                _band[s * Buckets + k] = glaze;
        }
    }
}

internal sealed record GlazeResult(float Base, float Bands, float Neatness, float Overall, int[] BandScoresPct);

internal static class GlazeJudge
{
    // Each band, judged a column at a time round the pot: how far its painted edges sit
    // from the design's, with gaps in it counted as error. Wavy bands lose on every
    // column they wander in. Glaze painted where the design has none costs neatness.
    public static GlazeResult Judge(PotGlaze glaze, GlazeDesign design)
    {
        int n = glaze.Slices;
        float baseScore = glaze.Base == design.Base ? 1f : glaze.Base < 0 ? 0f : 0.3f;
        var target = new int[n];
        Array.Fill(target, -1);
        var spans = design.Bands.Select(b => GlazeDesign.Slices(b, n)).ToArray();
        for (int i = 0; i < design.Bands.Length; i++)
            for (int s = spans[i].From; s <= spans[i].To; s++)
                target[s] = design.Bands[i].Glaze;

        var scores = new float[design.Bands.Length];
        for (int i = 0; i < design.Bands.Length; i++)
        {
            int g = design.Bands[i].Glaze;
            var (from, to) = spans[i];
            int window = 2 * PotGlaze.SlicesPerRow;
            float sum = 0f;
            for (int k = 0; k < PotGlaze.Buckets; k++)
            {
                int lo = int.MaxValue, hi = int.MinValue, count = 0;
                for (int s = Math.Max(0, from - window); s <= Math.Min(n - 1, to + window); s++)
                    if (glaze.Band(s, k) == g)
                    {
                        lo = Math.Min(lo, s);
                        hi = Math.Max(hi, s);
                        count++;
                    }
                if (count == 0)
                    continue;
                int gaps = hi - lo + 1 - count;
                float err = (MathF.Abs(lo - from) + MathF.Abs(hi - to) + gaps) / (float)PotGlaze.SlicesPerRow;
                err = MathF.Max(0f, err - 1f / PotGlaze.SlicesPerRow);     // a slice's slop is free
                sum += Math.Clamp(1f - err / 1f, 0f, 1f);
            }
            scores[i] = sum / PotGlaze.Buckets;
        }

        // stray glaze: band paint that matches neither the design nor a band's edge slop
        int stray = 0, cells = 0;
        for (int s = 0; s < n; s++)
        {
            if (target[s] >= 0)
                cells += PotGlaze.Buckets;
            for (int k = 0; k < PotGlaze.Buckets; k++)
            {
                int g = glaze.Band(s, k);
                if (g < 0 || g == target[s])
                    continue;
                bool nearOwn = false;
                for (int i = 0; i < design.Bands.Length && !nearOwn; i++)
                    nearOwn = design.Bands[i].Glaze == g && s >= spans[i].From - 1 && s <= spans[i].To + 1;
                if (!nearOwn)
                    stray++;
            }
        }
        float neat = Math.Clamp(1f - stray / (float)Math.Max(1, cells) * 1.5f, 0f, 1f);
        float bands = scores.Length > 0 ? scores.Average() : 1f;
        float overall = 0.15f * baseScore + 0.85f * bands * neat;
        return new GlazeResult(baseScore, bands, neat, overall, scores.Select(s => (int)MathF.Round(s * 100)).ToArray());
    }

    // What cost the most, in Tobin's words.
    public static IEnumerable<string> Tips(PotGlaze glaze, GlazeDesign design, GlazeResult r)
    {
        if (glaze.Base != design.Base)
            yield return glaze.Base < 0
                ? $"Dip it in {Glazes.All[design.Base].Name.ToLowerInvariant()} first."
                : $"Wrong base: it wants {Glazes.All[design.Base].Name.ToLowerInvariant()}.";
        int worst = -1;
        for (int i = 0; i < design.Bands.Length; i++)
            if (r.BandScoresPct[i] < 90 && (worst < 0 || r.BandScoresPct[i] < r.BandScoresPct[worst]))
                worst = i;
        if (worst >= 0)
        {
            var band = design.Bands[worst];
            string name = Glazes.All[band.Glaze].Name.ToLowerInvariant();
            var (from, to) = GlazeDesign.Slices(band, glaze.Slices);
            // where its paint actually sits, on average
            float sum = 0; int count = 0, columns = 0;
            for (int k = 0; k < PotGlaze.Buckets; k++)
            {
                bool any = false;
                for (int s = 0; s < glaze.Slices; s++)
                    if (glaze.Band(s, k) == band.Glaze)
                    {
                        sum += s;
                        count++;
                        any = true;
                    }
                if (any) columns++;
            }
            if (count == 0)
                yield return $"It's missing the {name} band.";
            else if (columns < PotGlaze.Buckets * 0.9f)
                yield return $"The {name} band doesn't go all the way round: hold the brush for a whole turn.";
            else if (sum / count > to + 1.5f)
                yield return $"The {name} band is too high.";
            else if (sum / count < from - 1.5f)
                yield return $"The {name} band is too low.";
            else
                yield return $"The {name} band wanders: watch the brush, not your hand, and keep strokes short.";
        }
        if (r.Neatness < 0.9f)
            yield return "There's glaze where the design has none. Sponge it off and band it again.";
    }
}
#endif
