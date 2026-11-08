#if DEBUG
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace ArtStudio.Vale;

// Keeps the Vale's buildings clear in saves that already exist. A save keeps every
// tree, bush and stone it ever had, even after the map moves: trees from an older map
// grew inside the east Old Studio's walls and round its door (the user, 2026-11-08:
// "there are a bunch literally in the house that I couldn't break"). The map now has
// no trees near a building and marks the ground (tools/vale_yards.py):
//   blinxxy.MuseArt_KeepClear  a building's footprint and its door's approach: nothing
//                              grows or lies there (also NoSpawn All);
//   NoSpawn Tree               beside a building and in front of it: no trees.
// This sweep, on loading and every morning, takes out what the map says can't be there,
// plus anything standing in a wall or in water. Things you placed yourself stay.
// museart_vale_clear runs it on demand.
internal static class ValeGrounds
{
    private const string KeepClear = ModEntry.Id + "_KeepClear";

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        helper.Events.GameLoop.SaveLoaded += (_, _) => Sweep(monitor, LogLevel.Info);
        helper.Events.GameLoop.DayStarted += (_, _) => Sweep(monitor, LogLevel.Trace);
        DevTools.AddCommand(helper, "museart_vale_clear",
            "Clears trees, bushes and debris off the Gilded Vale's buildings and door approaches (it also runs every morning). "
            + "'museart_vale_clear plant x,y x,y ...' plants grown oaks first (to rebuild an old save's trees); 'museart_vale_clear test' checks the sweep.",
            (_, args) =>
            {
                if (args.FirstOrDefault() == "plant")
                    Plant(args.Skip(1));
                else if (args.FirstOrDefault() == "test")
                    Test(monitor);
                else
                    Sweep(monitor, LogLevel.Info);
            });
    }

    private static void Plant(IEnumerable<string> tiles)
    {
        if (Game1.getLocationFromName(ValeBus.Vale) is not { } vale)
            return;
        foreach (string t in tiles)
        {
            string[] xy = t.Split(',');
            var tile = new Vector2(int.Parse(xy[0]), int.Parse(xy[1]));
            vale.terrainFeatures.Remove(tile);
            vale.terrainFeatures.Add(tile, new Tree(Tree.bushyTree, Tree.treeStage));
        }
    }

    // The east Old Studio (x 88-93, y 29-38): a tree in its wall, one under its roof, one
    // on its door's approach, one in front, one beside it, and one well away that stays.
    private static void Test(IMonitor monitor)
    {
        if (Game1.getLocationFromName(ValeBus.Vale) is not { } vale)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        var gone = new[] { new Vector2(91, 37), new Vector2(92, 33), new Vector2(91, 40), new Vector2(90, 43), new Vector2(95, 35) };
        var away = new Vector2(80, 45);
        bool awayWasFree = !vale.terrainFeatures.ContainsKey(away);
        Plant(gone.Append(away).Select(v => $"{v.X},{v.Y}"));
        Sweep(monitor, LogLevel.Trace);
        foreach (var v in gone)
            Check(monitor, $"Vale grounds: the tree planted at {v.X},{v.Y} is swept away", !vale.terrainFeatures.ContainsKey(v));
        Check(monitor, "Vale grounds: a tree well away from the buildings stays", vale.terrainFeatures.ContainsKey(away));
        if (awayWasFree)
            vale.terrainFeatures.Remove(away);
        Check(monitor, "Vale grounds: no tree stands on a building's footprint or door approach", !vale.terrainFeatures.Pairs.Any(p => p.Value is Tree && NoTrees(vale, p.Key)));
    }

    private static void Check(IMonitor monitor, string what, bool ok) =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}", ok ? LogLevel.Info : LogLevel.Error);

    public static int Sweep(IMonitor monitor, LogLevel level)
    {
        if (!Context.IsMainPlayer || Game1.getLocationFromName(ValeBus.Vale) is not { } vale || vale.map is null)
            return 0;
        int removed = 0;
        void Log(string what, Vector2 tile)
        {
            removed++;
            monitor.Log($"Vale grounds: removed {what} at {tile.X},{tile.Y}", LogLevel.Trace);
        }

        foreach (var (tile, feature) in vale.terrainFeatures.Pairs.ToList())
        {
            bool tree = feature is Tree or FruitTree;
            if ((tree && NoTrees(vale, tile)) || (feature is Bush && Clear(vale, tile)) || ((tree || feature is Grass or Bush) && Blocked(vale, tile)))
            {
                vale.terrainFeatures.Remove(tile);
                Log(feature.GetType().Name, tile);
            }
        }
        foreach (var bush in vale.largeTerrainFeatures.OfType<Bush>().ToList())
        {
            if (Tiles(bush.getBoundingBox()).Any(t => Clear(vale, t) || Blocked(vale, t)))
            {
                vale.largeTerrainFeatures.Remove(bush);
                Log("bush", bush.Tile);
            }
        }
        foreach (var clump in vale.resourceClumps.ToList())
        {
            if (Tiles(clump.getBoundingBox()).Any(t => Clear(vale, t) || Blocked(vale, t)))
            {
                vale.resourceClumps.Remove(clump);
                Log("stump or boulder", clump.Tile);
            }
        }
        foreach (var (tile, obj) in vale.objects.Pairs.ToList())
        {
            if (obj.isDebrisOrForage() && (Clear(vale, tile) || Blocked(vale, tile)))
            {
                vale.objects.Remove(tile);
                Log(obj.Name, tile);
            }
        }
        if (removed > 0)
            monitor.Log($"Vale grounds: cleared {removed} trees, bushes and bits of debris off the buildings and door approaches.", level);
        return removed;
    }

    // A building's footprint or its door's approach.
    private static bool Clear(GameLocation vale, Vector2 tile) =>
        vale.doesTileHaveProperty((int)tile.X, (int)tile.Y, KeepClear, "Back") is not null;

    private static bool NoTrees(GameLocation vale, Vector2 tile) =>
        Clear(vale, tile) || vale.doesTileHaveProperty((int)tile.X, (int)tile.Y, "NoSpawn", "Back") == "Tree";

    // A wall (Buildings without Passable) or water, or off the map.
    private static bool Blocked(GameLocation vale, Vector2 tile) =>
        !vale.isTileOnMap(tile) || !vale.isTilePassable(tile) || vale.isWaterTile((int)tile.X, (int)tile.Y);

    private static IEnumerable<Vector2> Tiles(Rectangle box)
    {
        for (int y = box.Top / 64; y <= (box.Bottom - 1) / 64; y++)
            for (int x = box.Left / 64; x <= (box.Right - 1) / 64; x++)
                yield return new Vector2(x, y);
    }
}
#endif
