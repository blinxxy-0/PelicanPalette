using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using StardewValley;

namespace ArtStudio.Painting;

// Paint is a resource (docs/painting_depth.md, 2026-10-25). Knowing a
// colour lets you mix it; painting with it spends paint from your Palette,
// one cell's worth per cell painted. A Palette keeps a well for every
// colour, holding a number of tubes' worth (TubeCells each) set by its tier
// (2026-10-25: Wooden 3, Enamel 6, Iridium 10; half as much again from the
// 15-subject milestone). A new Palette comes filled with the starter
// colours. Relaxed difficulty paints for free and needs no Palette.
//
// Tubes go straight into the best Palette in your bag the moment they reach
// it, for colours you know, while there's room for the whole tube; the rest
// wait in the bag. Placed on the floor, a Palette is storage for paint:
// right-click it to put tubes in or take whole tubes out (PaletteMenu). To
// paint, it has to be in your bag.
//
// The paint lives in the item's modData, so it moves with the Palette (into
// a chest, onto the floor, to another player) like any item's contents.
internal static class PaletteItem
{
    private const string PaintKey = ModEntry.Id + "/Paint";
    public const int TubeCells = 100;

    public sealed record Tier(string Id, string Key, int Tubes, int Sprite, int Price);

    // Cheapest first.
    public static readonly Tier[] Tiers =
    {
        new(ArtItems.PaletteId, "wooden", 3, 9, 40),
        new(ModEntry.Id + "_Palette_Enamel", "enamel", 6, 13, 250),
        new(ModEntry.Id + "_Palette_Iridium", "iridium", 10, 14, 1000),
    };

    // The 15-subject milestone.
    public const float BigWells = 1.5f;
    private static readonly int MostCells = (int)(Tiers[^1].Tubes * TubeCells * BigWells);

    public static bool Required => ModEntry.Config.Difficulty != Difficulty.Relaxed;

    public static Tier? TierOf(Item? item) => item == null ? null : Tiers.FirstOrDefault(t => item.QualifiedItemId == "(O)" + t.Id);

    // How many cells of each colour this Palette holds.
    public static int Capacity(Item palette, Farmer who) =>
        (int)((TierOf(palette)?.Tubes ?? Tiers[0].Tubes) * TubeCells * (Catalogue.HasPerk(who, "palette") ? BigWells : 1f));

    // The Palette you paint from: the biggest one in your bag.
    public static Item? Find(Farmer who) =>
        who.Items.Where(ArtItems.IsPalette).OrderByDescending(p => TierOf(p)!.Tubes).FirstOrDefault();

    public static int[] Read(Item palette)
    {
        var paint = new int[Palette.All.Length];
        if (palette.modData.TryGetValue(PaintKey, out string raw))
            foreach (string entry in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                int i = parts.Length == 2 ? Palette.IndexOf(parts[0]) : -1;
                if (i >= 0 && int.TryParse(parts[1], out int cells))
                    paint[i] = Math.Clamp(cells, 0, MostCells);
            }
        return paint;
    }

    public static void Write(Item palette, int[] paint) =>
        palette.modData[PaintKey] = string.Join(",", paint.Select((cells, i) => (cells, i)).Where(p => p.cells > 0).Select(p => $"{Palette.All[p.i].Id}:{p.cells}"));

    // A Palette that has never held paint comes with a tube's worth of each
    // starter colour.
    public static void Initialize(Item palette)
    {
        if (palette.modData.ContainsKey(PaintKey)) return;
        Write(palette, Enumerable.Range(0, Palette.All.Length).Select(i => Palette.All[i].Starter ? TubeCells : 0).ToArray());
    }

    // Squeezes the tubes in your bag into your Palette, whole tubes only, for
    // colours you know. Returns the tubes used, by pigment.
    public static Dictionary<int, int> Refill(Farmer who, Item palette)
    {
        Initialize(palette);
        var used = new Dictionary<int, int>();
        foreach (var tube in who.Items.Where(i => ArtItems.TubeFor(i) != null).ToList())
        {
            int pigment = ArtItems.TubeFor(tube)!.Value;
            if (!ArtProgress.Owns(who, pigment)) continue;
            int n = Deposit(who, palette, tube, int.MaxValue);
            if (n > 0) used[pigment] = used.GetValueOrDefault(pigment) + n;
        }
        return used;
    }

    // Moves up to `count` tubes from a stack in the bag into the Palette, as
    // many as there's room for. Returns how many went in.
    public static int Deposit(Farmer who, Item palette, Item tube, int count)
    {
        if (ArtItems.TubeFor(tube) is not int pigment) return 0;
        Initialize(palette);
        var paint = Read(palette);
        int room = (Capacity(palette, who) - paint[pigment]) / TubeCells;
        int n = Math.Min(Math.Min(count, tube.Stack), room);
        if (n <= 0) return 0;
        paint[pigment] += n * TubeCells;
        Write(palette, paint);
        if (tube.Stack > n) tube.Stack -= n;
        else who.removeItemFromInventory(tube);
        return n;
    }

    // A whole tube back out of the Palette, if it holds one.
    public static Item? Withdraw(Item palette, int pigment)
    {
        var paint = Read(palette);
        if (paint[pigment] < TubeCells) return null;
        paint[pigment] -= TubeCells;
        Write(palette, paint);
        return ItemRegistry.Create("(O)" + ArtItems.TubeId(pigment));
    }
}
