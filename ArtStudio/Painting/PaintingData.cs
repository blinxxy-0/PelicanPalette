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
    // Glazes and highlights; null for a painting without any.
    public Mark[,]? Marks { get; init; }

    // Qualified item IDs joined with '|' for a still life, or OriginalSubject.
    public string Subject { get; init; } = OriginalSubject;
    public float Stars { get; init; }
    public int Difficulty { get; init; }
    public string Painter { get; init; } = "";
    public string Date { get; init; } = "";

    public bool IsOriginal => Subject == OriginalSubject;

    // Its varnish ("satin", "gloss" or "gallery"), if it's been through the cabinet.
    public string? Finish { get; init; }

    // Painted on linen at the very top of the craft (PaintingRules.IsMasterpiece).
    public bool Masterpiece { get; init; }
    public const string MasterpieceKey = ModEntry.Id + "/Masterpiece";
    public static bool IsMasterpiece(Item item) => item.modData.ContainsKey(MasterpieceKey);

    // Painted to a reference with glazes, and glazed well enough for a glazed
    // commission (PaintingRules.IsGlazed). Null when not recorded: paintings
    // from before 1.1.5, and originals.
    public bool? Glazed { get; init; }
    private const string GlazedKey = ModEntry.Id + "/Glazed";

    public static bool? GlazedOf(Item item) =>
        item.modData.TryGetValue(GlazedKey, out string raw) ? raw == "1" : null;

    // A view painted at night (7pm on) or in the rain: "night" or "rain"
    // (1.2; Sable buys these at the Night Market). Null for everything else,
    // and for views from before 1.2.
    public string? Conditions { get; init; }
    private const string ConditionsKey = ModEntry.Id + "/Conditions";

    public static string? ConditionsOf(Item item) =>
        item.modData.TryGetValue(ConditionsKey, out string raw) ? raw : null;

    // A portrait of the farmer, by Sable (the 10-heart event): "portrait:<name>".
    public bool IsPortrait => FarmerPortrait.IsPortrait(Subject);

    // A portrait of a villager, painted by the player: "sitter:<name>".
    public bool IsSitter => Sitters.IsSitter(Subject);

    // A plein-air view: "view:<location>:<season>".
    public bool IsView => Views.IsView(Subject);

    // A study of a creature: "creature:<monster name>" (1.2).
    public bool IsCreature => Creatures.IsCreature(Subject);

    // A study of one of your animals: "animal:<type>" (1.3).
    public bool IsAnimal => Animals.IsAnimal(Subject);

    // Either: painted from a sprite, from memory.
    public bool IsStudy => IsCreature || IsAnimal;

    public string[] SubjectItems => IsOriginal || IsPortrait || IsSitter || IsView || IsStudy ? Array.Empty<string>() : Subject.Split('|');

    public void WriteTo(Item item)
    {
        item.modData[CellsKey] = EncodeCells();
        item.modData[SubjectKey] = Subject;
        item.modData[StarsKey] = Stars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        item.modData[DifficultyKey] = Difficulty.ToString();
        item.modData[PainterKey] = Painter;
        item.modData[DateKey] = Date;
        if (Finish != null)
            item.modData[Varnish.FinishKey] = Finish;
        if (Masterpiece)
            item.modData[MasterpieceKey] = "1";
        if (Glazed is bool glazed)
            item.modData[GlazedKey] = glazed ? "1" : "0";
        if (Conditions != null)
            item.modData[ConditionsKey] = Conditions;
    }

    public static string? RawCells(Item item) => item.modData.TryGetValue(CellsKey, out string raw) ? raw : null;

    public static PaintingData? Read(Item item)
    {
        string? raw = RawCells(item);
        if (raw == null || !TryDecodeCells(raw, out int size, out var cells, out var purity, out var marks))
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
            Marks = marks,
            Subject = string.IsNullOrEmpty(subject) ? OriginalSubject : subject,
            Stars = float.TryParse(stars, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s) ? s : 0f,
            Difficulty = int.TryParse(difficulty, out int d) ? d : 0,
            Painter = painter ?? "",
            Date = date ?? "",
            Finish = Varnish.FinishOf(item),
            Masterpiece = IsMasterpiece(item),
            Glazed = GlazedOf(item),
            Conditions = ConditionsOf(item),
        };
    }

    // Size byte, then size² pigment indices, then size² purity values, then
    // (only when the painting has any) size² marks.
    private string EncodeCells()
    {
        int n = Size;
        bool marked = Marks != null && (Technique.Count(Marks, Mark.Glaze) > 0 || Technique.Count(Marks, Mark.Highlight) > 0);
        var bytes = new byte[1 + (marked ? 3 : 2) * n * n];
        bytes[0] = (byte)n;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            bytes[1 + y * n + x] = Cells[x, y];
            bytes[1 + n * n + y * n + x] = (byte)Purity[x, y];
            if (marked)
                bytes[1 + 2 * n * n + y * n + x] = (byte)Marks![x, y];
        }
        return Convert.ToBase64String(bytes);
    }

    public static bool TryDecodeCells(string raw, out int size, out byte[,] cells, out Purity[,] purity, out Mark[,]? marks)
    {
        size = 0;
        cells = new byte[0, 0];
        purity = new Purity[0, 0];
        marks = null;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); }
        catch (FormatException) { return false; }
        if (bytes.Length == 0) return false;

        int n = bytes[0];
        if (n == 0 || (bytes.Length != 1 + 2 * n * n && bytes.Length != 1 + 3 * n * n)) return false;
        size = n;
        cells = new byte[n, n];
        purity = new Purity[n, n];
        if (bytes.Length == 1 + 3 * n * n)
            marks = new Mark[n, n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            byte c = bytes[1 + y * n + x];
            cells[x, y] = c < Palette.All.Length ? c : PixelCanvas.Blank;
            purity[x, y] = (Purity)Math.Min((int)bytes[1 + n * n + y * n + x], (int)Core.Purity.Muddy);
            if (marks != null)
                marks[x, y] = (Mark)Math.Min((int)bytes[1 + 2 * n * n + y * n + x], (int)Mark.Highlight);
        }
        return true;
    }
}
