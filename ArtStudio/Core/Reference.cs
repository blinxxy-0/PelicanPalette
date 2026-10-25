using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

// What the player is copying: a palette-exact pixel image the same size as
// the canvas (Blank = leave the canvas bare). It's always shown to the
// player — the challenge is reproducing it under pressure, not guessing.
public sealed class Reference
{
    public byte[,] Cells { get; }
    // Glazes and highlights over the paint (Core/Technique.cs); all None on a small canvas.
    public Mark[,] Marks { get; }
    public int Size { get; }
    public int SubjectCells { get; }
    public IReadOnlyList<int> PigmentsUsed { get; }

    // 0-100, shown to the player like a fish's difficulty. More pigments,
    // more cells and more color changes between neighbours all make a
    // subject harder (and worth more).
    public int Difficulty { get; }

    public bool UsesTechniques { get; }

    public Reference(byte[,] cells, Mark[,]? marks = null)
    {
        Cells = cells;
        Size = cells.GetLength(0);
        Marks = marks ?? new Mark[Size, Size];
        int marked = Technique.Count(Marks, Mark.Glaze) + Technique.Count(Marks, Mark.Highlight);
        UsesTechniques = marked > 0;

        var used = new SortedSet<int>();
        int edges = 0;
        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        {
            byte c = cells[x, y];
            if (c == PixelCanvas.Blank) continue;
            SubjectCells++;
            used.Add(c);
            if (x + 1 < Size && cells[x + 1, y] != c) edges++;
            if (y + 1 < Size && cells[x, y + 1] != c) edges++;
        }
        PigmentsUsed = new List<int>(used);

        int hardestPigment = 0;
        foreach (int p in used)
            hardestPigment = Math.Max(hardestPigment, Palette.All[p].Difficulty);

        // Cells and edges are scaled to a small (20×20) canvas, so bigger
        // canvases rate by how busy the picture is, not how big; they get more
        // light and a higher base price for their size separately. Tuned on
        // all 683 paintable items: plain ones (a stone, clay) land near 15-25,
        // a parsnip about 45, busy multi-colored ones 80+.
        double area = Size * Size / 400.0;
        double cellsN = SubjectCells / area, edgesN = edges / area;
        double score = 25 * (cellsN - 60) / 150 + 30 * (edgesN - 60) / 150 + 6 * (used.Count - 2) + 0.25 * hardestPigment
            + (Technique.Count(Marks, Mark.Glaze) > 0 ? 6 : 0) + (Technique.Count(Marks, Mark.Highlight) > 0 ? 6 : 0);
        Difficulty = (int)Math.Clamp(Math.Round(score), 5, 100);
    }

    // Places a sprite-sized grid of pigment indices in the middle of a
    // larger, blank canvas.
    public static Reference Centered(byte[,] subject, int canvasSize, Mark[,]? subjectMarks = null)
    {
        int w = subject.GetLength(0), h = subject.GetLength(1);
        var cells = PixelCanvas.NewBlank(canvasSize);
        var marks = new Mark[canvasSize, canvasSize];
        int ox = (canvasSize - w) / 2, oy = (canvasSize - h) / 2;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            int cx = x + ox, cy = y + oy;
            if (cx >= 0 && cy >= 0 && cx < canvasSize && cy < canvasSize)
            {
                cells[cx, cy] = subject[x, y];
                marks[cx, cy] = subjectMarks?[x, y] ?? Mark.None;
            }
        }
        return new Reference(cells, marks);
    }
}
