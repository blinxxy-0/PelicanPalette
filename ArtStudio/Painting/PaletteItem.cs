using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using StardewValley;

namespace ArtStudio.Painting;

// Paint is a resource (docs/painting_depth.md, 2026-10-25). Knowing a
// colour lets you mix it; painting with it spends paint from your Palette,
// one cell's worth per cell painted. The Palette keeps a well for every
// colour, up to WellCells each (more from the 15-subject milestone:
// Catalogue.WellCells), refilled from tubes (TubeCells each) when
// you sit down at the easel. A new Palette comes filled with the starter
// colours. Relaxed difficulty paints for free and needs no Palette.
//
// The paint lives in the item's modData, so it moves with the Palette (into
// a chest, to another player) like any item's contents.
internal static class PaletteItem
{
    private const string PaintKey = ModEntry.Id + "/Paint";
    public const int WellCells = 300;
    public const int TubeCells = 100;

    public static bool Required => ModEntry.Config.Difficulty != Difficulty.Relaxed;

    public static Item? Find(Farmer who) => who.Items.FirstOrDefault(ArtItems.IsPalette);

    public static int[] Read(Item palette)
    {
        var paint = new int[Palette.All.Length];
        if (palette.modData.TryGetValue(PaintKey, out string raw))
            foreach (string entry in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                int i = parts.Length == 2 ? Palette.IndexOf(parts[0]) : -1;
                if (i >= 0 && int.TryParse(parts[1], out int cells))
                    paint[i] = Math.Clamp(cells, 0, Catalogue.BigWellCells);
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

    // Squeezes tubes from the bag into wells that have room for a whole tube,
    // for colours the painter knows. Returns the tubes used, by pigment.
    public static Dictionary<int, int> Refill(Farmer who, Item palette)
    {
        Initialize(palette);
        var paint = Read(palette);
        var used = new Dictionary<int, int>();
        foreach (var tube in who.Items.Where(i => ArtItems.TubeFor(i) != null).ToList())
        {
            int pigment = ArtItems.TubeFor(tube)!.Value;
            if (!ArtProgress.Owns(who, pigment)) continue;
            while (tube.Stack > 0 && paint[pigment] + TubeCells <= Catalogue.WellCells(who))
            {
                paint[pigment] += TubeCells;
                used[pigment] = used.GetValueOrDefault(pigment) + 1;
                if (tube.Stack > 1) tube.Stack--;
                else
                {
                    who.removeItemFromInventory(tube);
                    break;
                }
            }
        }
        Write(palette, paint);
        return used;
    }
}
