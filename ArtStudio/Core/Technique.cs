using System;
using System.Collections.Generic;
using System.Linq;

namespace ArtStudio.Core;

// What sits on top of a cell's paint (2026-10-25). A small canvas is flat
// colour; a medium one adds glazing, a large one highlights as well, so the
// painting grows with the canvas one idea at a time:
//   Glaze     — a thin dark wash over paint that's already down: the shadow
//               side of a melon. Paint the colour first, then glaze it.
//   Highlight — a dab of thick, light paint: the shine on fruit, the light
//               in an eye. It goes on last; painting beside it afterwards
//               smears it away.
// Painting a cell again covers whatever was on it.
public enum Mark : byte { None, Glaze, Highlight }

[Flags]
public enum Techniques { None = 0, Glaze = 1, Highlight = 2 }

public static class Technique
{
    private static readonly (byte R, byte G, byte B) GlazeTint = (52, 34, 28), HighlightTint = (255, 250, 238);
    private const float GlazeStrength = 0.38f, HighlightStrength = 0.5f;

    // A technique is only used where it matches the picture clearly better
    // than any plain colour, so references stay mostly flat paint.
    private const double ClearlyBetter = 0.75;

    public static (byte R, byte G, byte B) Apply(byte r, byte g, byte b, Mark mark) => mark switch
    {
        Mark.Glaze => Lerp((r, g, b), GlazeTint, GlazeStrength),
        Mark.Highlight => Lerp((r, g, b), HighlightTint, HighlightStrength),
        _ => (r, g, b),
    };

    // The nearest of the owned pigments, plain or (where allowed) glazed or highlighted.
    public static (byte Pigment, Mark Mark) Nearest(byte r, byte g, byte b, IReadOnlyList<int> owned, Techniques allowed)
    {
        int plain = Palette.Nearest(r, g, b, owned);
        if (allowed == Techniques.None) return ((byte)plain, Mark.None);
        double plainDist = Distance(r, g, b, plain, Mark.None);
        (int Pigment, Mark Mark, double Dist) best = (plain, Mark.None, plainDist);
        foreach (var mark in new[] { Mark.Glaze, Mark.Highlight })
        {
            if (!Allows(allowed, mark)) continue;
            // Highlights are for the brightest spots only.
            if (mark == Mark.Highlight && 0.299 * r + 0.587 * g + 0.114 * b < 190) continue;
            foreach (int p in owned)
            {
                double d = Distance(r, g, b, p, mark);
                if (d < best.Dist && d < plainDist * ClearlyBetter)
                    best = (p, mark, d);
            }
        }
        return ((byte)best.Pigment, best.Mark);
    }

    public static bool Allows(Techniques allowed, Mark mark) => mark switch
    {
        Mark.Glaze => allowed.HasFlag(Techniques.Glaze),
        Mark.Highlight => allowed.HasFlag(Techniques.Highlight),
        _ => true,
    };

    // Keeps a picture from being all technique: at most a third of it glazed,
    // and a few highlights (the palest kept). The rest go back to plain paint.
    public static void Limit(byte[,] cells, Mark[,] marks)
    {
        int n = cells.GetLength(0), subject = 0;
        var glazes = new List<(int X, int Y)>();
        var highlights = new List<(int X, int Y)>();
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        {
            if (cells[x, y] == PixelCanvas.Blank) continue;
            subject++;
            if (marks[x, y] == Mark.Glaze) glazes.Add((x, y));
            else if (marks[x, y] == Mark.Highlight) highlights.Add((x, y));
        }
        foreach (var (x, y) in glazes.Skip(subject / 3))
            marks[x, y] = Mark.None;
        int maxHighlights = Math.Max(2, subject / 25);
        foreach (var (x, y) in highlights.OrderByDescending(c => Luma(cells[c.X, c.Y])).Skip(maxHighlights))
            marks[x, y] = Mark.None;
    }

    public static int Count(Mark[,] marks, Mark mark)
    {
        int count = 0;
        foreach (var m in marks)
            if (m == mark) count++;
        return count;
    }

    private static double Distance(byte r, byte g, byte b, int pigment, Mark mark)
    {
        var p = Palette.All[pigment];
        var (pr, pg, pb) = Apply(p.R, p.G, p.B, mark);
        return Palette.Distance(r, g, b, pr, pg, pb);
    }

    private static double Luma(byte pigment) =>
        0.299 * Palette.All[pigment].R + 0.587 * Palette.All[pigment].G + 0.114 * Palette.All[pigment].B;

    private static (byte, byte, byte) Lerp((byte R, byte G, byte B) a, (byte R, byte G, byte B) b, float t) =>
        ((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
}
