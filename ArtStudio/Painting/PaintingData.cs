using System;
using ArtStudio.Core;
using StardewValley;

namespace ArtStudio.Painting;

// Everything about one finished painting that has to survive saving: the
// pixels, how well each was mixed, and the provenance shown in its
// description. Stored in the item's modData, so paintings stay vanilla
// objects (save-safe, mod-removal-safe) while each one carries its own art.
internal sealed class PaintingData
{
    private const string CellsKey = ModEntry.Id + "/Cells";
    private const string SubjectKey = ModEntry.Id + "/Subject";
    private const string StarsKey = ModEntry.Id + "/Stars";
    private const string DifficultyKey = ModEntry.Id + "/Difficulty";
    private const string PainterKey = ModEntry.Id + "/Painter";
    private const string DateKey = ModEntry.Id + "/Date";

    public const string OriginalSubject = "original";

    public int Size { get; init; }
    public byte[,] Cells { get; init; } = new byte[0, 0];
    public Purity[,] Purity { get; init; } = new Purity[0, 0];

    // Qualified item IDs joined with '|' for a still life, or OriginalSubject.
    public string Subject { get; init; } = OriginalSubject;
    public float Stars { get; init; }
    public int Difficulty { get; init; }
    public string Painter { get; init; } = "";
    public string Date { get; init; } = "";

    public bool IsOriginal => Subject == OriginalSubject;

    public string[] SubjectItems => IsOriginal ? Array.Empty<string>() : Subject.Split('|');

    public void WriteTo(Item item)
    {
        item.modData[CellsKey] = EncodeCells();
        item.modData[SubjectKey] = Subject;
        item.modData[StarsKey] = Stars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        item.modData[DifficultyKey] = Difficulty.ToString();
        item.modData[PainterKey] = Painter;
        item.modData[DateKey] = Date;
    }

    public static string? RawCells(Item item) => item.modData.TryGetValue(CellsKey, out string raw) ? raw : null;

    public static PaintingData? Read(Item item)
    {
        string? raw = RawCells(item);
        if (raw == null || !TryDecodeCells(raw, out int size, out var cells, out var purity))
            return null;
        item.modData.TryGetValue(SubjectKey, out string subject);
        item.modData.TryGetValue(StarsKey, out string stars);
        item.modData.TryGetValue(DifficultyKey, out string difficulty);
        item.modData.TryGetValue(PainterKey, out string painter);
        item.modData.TryGetValue(DateKey, out string date);
        return new PaintingData
        {
            Size = size,
            Cells = cells,
            Purity = purity,
            Subject = string.IsNullOrEmpty(subject) ? OriginalSubject : subject,
            Stars = float.TryParse(stars, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s) ? s : 0f,
            Difficulty = int.TryParse(difficulty, out int d) ? d : 0,
            Painter = painter ?? "",
            Date = date ?? "",
        };
    }

    // Size byte, then size² pigment indices, then size² purity values.
    private string EncodeCells()
    {
        int n = Size;
        var bytes = new byte[1 + 2 * n * n];
        bytes[0] = (byte)n;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            bytes[1 + y * n + x] = Cells[x, y];
            bytes[1 + n * n + y * n + x] = (byte)Purity[x, y];
        }
        return Convert.ToBase64String(bytes);
    }

    public static bool TryDecodeCells(string raw, out int size, out byte[,] cells, out Purity[,] purity)
    {
        size = 0;
        cells = new byte[0, 0];
        purity = new Purity[0, 0];
        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); }
        catch (FormatException) { return false; }
        if (bytes.Length == 0) return false;

        int n = bytes[0];
        if (n == 0 || bytes.Length != 1 + 2 * n * n) return false;
        size = n;
        cells = new byte[n, n];
        purity = new Purity[n, n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            byte c = bytes[1 + y * n + x];
            cells[x, y] = c < Palette.All.Length ? c : PixelCanvas.Blank;
            purity[x, y] = (Purity)Math.Min((int)bytes[1 + n * n + y * n + x], (int)Core.Purity.Muddy);
        }
        return true;
    }
}
