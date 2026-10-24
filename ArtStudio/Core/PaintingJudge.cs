using System;

namespace ArtStudio.Core;

public record PaintingResult(float Shape, float Color, float Neatness, float Overall, float Stars);

// Grades a finished painting against its reference. Ported from Muse, where
// it was calibrated by simulation; the only rule added here is that paint
// counts for less the worse it was mixed.
//
// Three things a viewer would actually notice, each 0-1:
//   Shape    — does the painted silhouette match the reference's (IoU; paint
//              just outside the edge counts half as wrong as paint far away)
//   Color    — F-score of right-pigment-in-the-right-place, both ways. A
//              color within one cell of where the reference has it counts
//              as right: being a cell out of place is a Shape slip. Below 60%
//              counts as nothing, then a 1.5 power curve. Only full credit
//              once at least 60% of the subject is painted.
//   Neatness — stray paint well away from the subject
//
// Overall = (0.4 Shape + 0.6 Color) × a neatness penalty of up to 25%.
// Stars are Overall × 5 rounded to the nearest half.
//
// Where on the canvas the painting sits doesn't matter: every alignment is
// tried and the best one used.
public static class PaintingJudge
{
    private const float SimilarPigmentCredit = 0.4f;

    // How much a cell's color counts for, by how well its paint was mixed.
    public static float PurityCredit(Purity p) => p switch
    {
        Purity.Pure => 1f,
        Purity.Off => 0.6f,
        _ => 0.2f,
    };

    public static PaintingResult Judge(byte[,] painting, Purity[,] purity, Reference reference)
    {
        var refCells = reference.Cells;
        int n = reference.Size;
        int subjectCells = Math.Max(1, reference.SubjectCells);
        var nearSubject = Dilate(refCells, n);

        float bestOverall = -1f, bestShape = 0f, bestColor = 0f, bestNeat = 0f;

        var shifted = new byte[n, n];
        var shiftedPurity = new Purity[n, n];
        int maxShift = n / 2;
        for (int dx = -maxShift; dx <= maxShift; dx++)
        for (int dy = -maxShift; dy <= maxShift; dy++)
        {
            int hits = 0, strayNear = 0, strayFar = 0;

            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                shifted[x, y] = PixelCanvas.Blank;

            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                byte p = painting[x, y];
                if (p == PixelCanvas.Blank) continue;

                int rx = x + dx, ry = y + dy;
                if (rx < 0 || ry < 0 || rx >= n || ry >= n)
                {
                    strayFar++;
                    continue;
                }
                shifted[rx, ry] = p;
                shiftedPurity[rx, ry] = purity[x, y];
                if (refCells[rx, ry] != PixelCanvas.Blank) hits++;
                else if (nearSubject[rx, ry]) strayNear++;
                else strayFar++;
            }

            // Color is an F-score: precision (are the painted cells the right
            // colors?) and recall (does every color in the reference show up
            // where it should?). Recall is what stops one flat color over the
            // whole shape from scoring well.
            float precision = 0f, recall = 0f;
            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                byte r = refCells[x, y];
                if (r == PixelCanvas.Blank) continue;
                byte p = shifted[x, y];
                if (p != PixelCanvas.Blank)
                {
                    float mix = PurityCredit(shiftedPurity[x, y]);
                    precision += CellCredit(p, r, refCells, x, y, n) * mix;
                    recall += CellCredit(r, p, shifted, x, y, n) * mix;
                }
                else if (HasNear(shifted, x, y, r, n))
                    recall += 1f;
            }
            precision = hits > 0 ? precision / hits : 0f;
            recall /= subjectCells;
            float accuracy = precision + recall > 0f ? 2f * precision * recall / (precision + recall) : 0f;

            float shape = hits / (subjectCells + 0.5f * strayNear + strayFar);
            float color = MathF.Pow(Clamp01((accuracy - 0.6f) / 0.4f), 1.5f) * MathF.Min(1f, hits / (0.6f * subjectCells));
            float neat = 1f - Clamp01(strayFar / (0.15f * subjectCells));
            float overall = (0.4f * shape + 0.6f * color) * (1f - 0.25f * (1f - neat));

            if (overall > bestOverall)
            {
                bestOverall = overall;
                bestShape = shape;
                bestColor = color;
                bestNeat = neat;
            }
        }

        float stars = MathF.Round(bestOverall * 10f) / 2f;
        stars = Math.Clamp(stars, 0.5f, 5f);
        return new PaintingResult(bestShape, bestColor, bestNeat, bestOverall, stars);
    }

    public static string Rating(float value) => value switch
    {
        >= 0.9f => "Excellent",
        >= 0.75f => "Good",
        >= 0.5f => "Fair",
        _ => "Poor",
    };

    // A cell one off from where the reference has that color is a placement
    // slip that Shape already accounts for — not a wrong color.
    private static float CellCredit(byte value, byte expected, byte[,] other, int x, int y, int n)
    {
        if (value == expected || HasNear(other, x, y, value, n)) return 1f;
        return Palette.AreSimilar(value, expected) ? SimilarPigmentCredit : 0f;
    }

    private static bool HasNear(byte[,] grid, int x, int y, byte value, int n)
    {
        for (int ox = -1; ox <= 1; ox++)
        for (int oy = -1; oy <= 1; oy++)
        {
            int cx = x + ox, cy = y + oy;
            if (cx >= 0 && cy >= 0 && cx < n && cy < n && grid[cx, cy] == value)
                return true;
        }
        return false;
    }

    private static bool[,] Dilate(byte[,] reference, int n)
    {
        var result = new bool[n, n];
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        {
            if (reference[x, y] == PixelCanvas.Blank) continue;
            for (int ox = -1; ox <= 1; ox++)
            for (int oy = -1; oy <= 1; oy++)
            {
                int cx = x + ox, cy = y + oy;
                if (cx >= 0 && cy >= 0 && cx < n && cy < n)
                    result[cx, cy] = true;
            }
        }
        return result;
    }

    private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);
}
