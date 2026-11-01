using System;

namespace ArtStudio.Core;

// Grades a restoration (1.3): only the damaged cells count, against the old
// painting as it was. The right pigment there is full credit, a near one
// ("a bit off") half, each scaled by how well the paint was mixed. A cell
// that survived but was painted over in another colour costs half a cell.
// There's no shifting, unlike a fresh painting: the picture is already
// where it is.
//
// Overall is the share of the damage put right; stars are Overall × 5,
// rounded to the nearest half, as for any painting.
public static class RestorationJudge
{
    private const float NearCredit = 0.5f, OverpaintCost = 0.5f;

    public static PaintingResult Judge(byte[,] painting, Purity[,] purity, byte[,] original, bool[,] damaged)
    {
        int n = original.GetLength(0), damagedCount = 0, overpainted = 0;
        float credit = 0f;
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        {
            byte want = original[x, y], got = painting[x, y];
            if (damaged[x, y])
            {
                damagedCount++;
                if (got == PixelCanvas.Blank) continue;
                float match = got == want ? 1f : want != PixelCanvas.Blank && Palette.AreSimilar(got, want) ? NearCredit : 0f;
                credit += match * PaintingJudge.PurityCredit(purity[x, y]);
            }
            else if (got != want)
                overpainted++;
        }
        if (damagedCount == 0) return new PaintingResult(1f, 1f, 1f, 1f, 5f);
        float overall = Math.Clamp((credit - OverpaintCost * overpainted) / damagedCount, 0f, 1f);
        float neat = 1f - Math.Clamp(overpainted / (float)damagedCount, 0f, 1f);
        float stars = Math.Clamp(MathF.Round(overall * 10f) / 2f, 0.5f, 5f);
        return new PaintingResult(overall, overall, neat, overall, stars);
    }
}
