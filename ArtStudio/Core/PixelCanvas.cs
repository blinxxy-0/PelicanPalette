using System;
using System.Collections.Generic;

namespace ArtStudio.Core;

// How well the paint in a cell was mixed. Pure paint is the exact pigment;
// "off" paint is close but not quite; muddy paint is visibly grimy and a
// viewer would barely call it the right color.
public enum Purity : byte { Pure, Off, Muddy }

// The painting itself: an N×N grid of pigment indices (Blank = unpainted
// canvas), plus how pure the paint in each cell is. Ported from Muse's
// PixelCanvas; every paint call now spends from the brush's load and
// reports how many cells it actually changed.
public class PixelCanvas
{
    public const byte Blank = 255;
    private const int MaxUndo = 64;

    public int Size { get; }
    private byte[,] _cells;
    private Purity[,] _purity;
    private Mark[,] _marks;
    private readonly Stack<(byte[,], Purity[,], Mark[,])> _undo = new();

    // Highlights smeared by painting beside them (the easel tells the painter).
    public int Smeared { get; private set; }

    public PixelCanvas(int size)
    {
        Size = size;
        _cells = NewBlank(size);
        _purity = new Purity[size, size];
        _marks = new Mark[size, size];
    }

    public byte this[int x, int y] => _cells[x, y];
    public Mark MarkAt(int x, int y) => _marks[x, y];
    public Mark[,] MarksSnapshot() => (Mark[,])_marks.Clone();
    public Purity PurityAt(int x, int y) => _purity[x, y];
    public byte[,] Snapshot() => (byte[,])_cells.Clone();
    public Purity[,] PuritySnapshot() => (Purity[,])_purity.Clone();
    public bool CanUndo => _undo.Count > 0;

    public bool IsEmpty
    {
        get
        {
            foreach (var c in _cells)
                if (c != Blank) return false;
            return true;
        }
    }

    // Call once at the start of each user action (stroke, fill, clear) so a
    // single undo reverts the whole action, not one cell of it.
    public void PushUndo()
    {
        _undo.Push((Snapshot(), PuritySnapshot(), MarksSnapshot()));
        if (_undo.Count > MaxUndo)
        {
            var keep = _undo.ToArray();
            _undo.Clear();
            for (int i = MaxUndo - 1; i >= 0; i--)
                _undo.Push(keep[i]);
        }
    }

    // Undo restores the canvas but not the paint: what went on the brush
    // has been used up either way.
    public void Undo()
    {
        if (_undo.Count > 0)
            (_cells, _purity, _marks) = _undo.Pop();
    }

    public void Load(byte[,] cells, Purity[,] purity, Mark[,]? marks = null)
    {
        _cells = (byte[,])cells.Clone();
        _purity = (Purity[,])purity.Clone();
        _marks = marks != null ? (Mark[,])marks.Clone() : new Mark[Size, Size];
        _undo.Clear();
    }

    public void Clear()
    {
        _cells = NewBlank(Size);
        _purity = new Purity[Size, Size];
        _marks = new Mark[Size, Size];
    }

    // Paints one cell if the budget allows. Repainting a cell with exactly
    // what's already there is free; anything else costs one unit of load.
    public bool Set(int x, int y, byte value, Purity purity, ref int budget)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size) return false;
        if (_cells[x, y] == value && (value == Blank || _purity[x, y] == purity)) return false;
        if (value != Blank)
        {
            if (budget <= 0) return false;
            budget--;
        }
        _cells[x, y] = value;
        _purity[x, y] = value == Blank ? Purity.Pure : purity;
        _marks[x, y] = Mark.None;           // new paint covers any glaze or highlight
        if (value != Blank)
            SmearAround(x, y);
        return true;
    }

    // Wet paint beside a highlight drags it into the colour around it.
    private void SmearAround(int x, int y)
    {
        foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            if (nx >= 0 && ny >= 0 && nx < Size && ny < Size && _marks[nx, ny] == Mark.Highlight)
            {
                _marks[nx, ny] = Mark.None;
                Smeared++;
            }
    }

    // Glazes or highlights a painted cell (a bare one can't take either).
    public bool SetMark(int x, int y, Mark mark)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size || _cells[x, y] == Blank || _marks[x, y] == mark) return false;
        _marks[x, y] = mark;
        return true;
    }

    public int MarkLine(int x0, int y0, int x1, int y1, Mark mark)
    {
        int changed = 0;
        foreach (var (x, y) in LinePoints(x0, y0, x1, y1))
            if (SetMark(x, y, mark)) changed++;
        return changed;
    }

    // brushSize 1 = single cell, 2 = 2×2 block anchored at the cell.
    public int Stamp(int x, int y, byte value, Purity purity, int brushSize, ref int budget)
    {
        int changed = 0;
        for (int dx = 0; dx < brushSize; dx++)
        for (int dy = 0; dy < brushSize; dy++)
            if (Set(x + dx, y + dy, value, purity, ref budget)) changed++;
        return changed;
    }

    // Bresenham between the previous and current cell so fast mouse drags
    // leave a continuous line instead of scattered dots.
    public int Line(int x0, int y0, int x1, int y1, byte value, Purity purity, int brushSize, ref int budget)
    {
        int changed = 0;
        foreach (var (x, y) in LinePoints(x0, y0, x1, y1))
            changed += Stamp(x, y, value, purity, brushSize, ref budget);
        return changed;
    }

    public static IEnumerable<(int X, int Y)> LinePoints(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            yield return (x0, y0);
            if (x0 == x1 && y0 == y1) yield break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    // Flood fill outward from the clicked cell, nearest cells first, so a
    // brush that runs dry mid-fill leaves a tidy blob rather than a ragged
    // scan line.
    public int Fill(int x, int y, byte value, Purity purity, ref int budget)
    {
        byte target = _cells[x, y];
        Purity targetPurity = _purity[x, y];
        if (target == value && (value == Blank || targetPurity == purity)) return 0;

        int changed = 0;
        var seen = new bool[Size, Size];
        var queue = new Queue<(int, int)>();
        queue.Enqueue((x, y));
        seen[x, y] = true;
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (!Set(cx, cy, value, purity, ref budget))
                break;
            changed++;
            foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
            {
                if (nx < 0 || ny < 0 || nx >= Size || ny >= Size || seen[nx, ny]) continue;
                if (_cells[nx, ny] != target || (target != Blank && _purity[nx, ny] != targetPurity)) continue;
                seen[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }
        return changed;
    }

    public static byte[,] NewBlank(int size)
    {
        var cells = new byte[size, size];
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
            cells[x, y] = Blank;
        return cells;
    }
}
