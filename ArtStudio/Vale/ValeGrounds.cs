#if DEBUG
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

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
//
// The overlook's Three Trees stay (tools/vale_overlook.py, KEEP_TREE): their trunk tiles
// carry blinxxy.MuseArt_KeepTree on Back, its value the tree's type. The axe stops short
// of them and bombs leave them standing ("He swears there used to be four": the stump of
// the fourth is beside them), and the sweep replants any that's missing, as in saves made
// before the overlook was built (a map's trees only spawn when the location is created).
internal static class ValeGrounds
{
    private const string KeepClear = ModEntry.Id + "_KeepClear";
    private const string KeepTree = ModEntry.Id + "_KeepTree";
    private const string KeepTreeLine = "Strings\\StringsFromMaps:" + ModEntry.Id + "_Vale.overlook.three_trees";

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(AccessTools.Method(typeof(Tree), nameof(Tree.performToolAction)),
            prefix: new HarmonyMethod(typeof(ValeGrounds), nameof(TreeToolPrefix)));
        helper.Events.GameLoop.SaveLoaded += (_, _) => Sweep(monitor, LogLevel.Info);
        helper.Events.GameLoop.DayStarted += (_, _) => Sweep(monitor, LogLevel.Trace);
        DevTools.AddCommand(helper, "museart_vale_clear",
            "Clears trees, bushes and debris off the Gilded Vale's buildings and door approaches (it also runs every morning). "
            + "'museart_vale_clear plant x,y x,y ...' plants grown oaks first (to rebuild an old save's trees); 'museart_vale_clear test' checks the sweep; 'museart_vale_clear pos px py' puts the farmer at a pixel spot.",
            (_, args) =>
            {
                if (args.FirstOrDefault() == "plant")
                    Plant(args.Skip(1));
                else if (args.FirstOrDefault() == "test")
                    Test(monitor);
                else if (args.FirstOrDefault() == "pos" && args.Length == 3)
                    // the farmer at an exact pixel spot, as mid-step (to look at depth: a
                    // head through a roof only shows between tiles)
                    Game1.player.Position = new Vector2(float.Parse(args[1]), float.Parse(args[2]));
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

        // The Three Trees: a bomb leaves each standing; taken out, each is back after the sweep.
        var kept = KeptTrees(vale).ToList();
        Check(monitor, "Vale grounds: the overlook's Three Trees are marked on the map", kept.Count == 3);
        Sweep(monitor, LogLevel.Trace);
        foreach (var (tile, type) in kept)
        {
            bool standing = vale.terrainFeatures.TryGetValue(tile, out var f) && f is Tree t && t.treeType.Value == type && t.growthStage.Value >= Tree.treeStage && !t.stump.Value;
            Check(monitor, $"Vale grounds: tree {type} stands at {tile.X},{tile.Y}", standing);
            if (f is not Tree tree)
                continue;
            float health = tree.health.Value;
            Check(monitor, $"Vale grounds: a bomb leaves the tree at {tile.X},{tile.Y} standing", !tree.performToolAction(null, 10, tile) && tree.health.Value == health);
            // a real swing of an iridium axe, by the farmer: the tree takes no hit, and says why
            Game1.exitActiveMenu();
            var axe = new Axe { UpgradeLevel = 4 };
            axe.DoFunction(vale, (int)tile.X * 64 + 32, (int)tile.Y * 64 + 32, 1, Game1.player);
            string said = (Game1.activeClickableMenu as StardewValley.Menus.DialogueBox)?.getCurrentString() ?? "";
            Check(monitor, $"Vale grounds: the axe stops short of the tree at {tile.X},{tile.Y}, and says so ({said})",
                vale.terrainFeatures.TryGetValue(tile, out var after) && after == tree && tree.health.Value == health && said.Contains("stump"));
            Game1.exitActiveMenu();
            vale.terrainFeatures.Remove(tile);
        }
        Sweep(monitor, LogLevel.Trace);
        foreach (var (tile, type) in kept)
            Check(monitor, $"Vale grounds: the tree at {tile.X},{tile.Y} is replanted", vale.terrainFeatures.TryGetValue(tile, out var f) && f is Tree t && t.treeType.Value == type && t.growthStage.Value >= Tree.treeStage);

        // The overlook shed's foot (x 93-95, y 64) says its line from Strings/StringsFromMaps.
        var strings = Game1.content.Load<Dictionary<string, string>>("Strings/StringsFromMaps");
        foreach (int x in new[] { 93, 94, 95 })
        {
            string? action = vale.doesTileHaveProperty(x, 64, "Action", "Buildings");
            string key = action?.StartsWith("Message ") == true ? action[8..] : "";
            Check(monitor, $"Vale grounds: the shed at {x},64 has its line ({action})", strings.TryGetValue(key, out string? line) && line.Contains("easel"));
        }
    }

    // The tiles marked KeepTree, with the tree type each wants.
    private static IEnumerable<(Vector2 Tile, string Type)> KeptTrees(GameLocation vale)
    {
        var back = vale.map?.GetLayer("Back");
        if (back is null)
            yield break;
        for (int y = 0; y < back.LayerHeight; y++)
            for (int x = 0; x < back.LayerWidth; x++)
                if (vale.doesTileHaveProperty(x, y, KeepTree, "Back") is { } type)
                    yield return (new Vector2(x, y), type);
    }

    private static bool IsKeptTree(GameLocation? where, Vector2 tile) =>
        where is not null && where.Name == ValeBus.Vale && where.doesTileHaveProperty((int)tile.X, (int)tile.Y, KeepTree, "Back") is not null;

    // The axe stops short of the Three Trees (and says why); a bomb leaves them standing.
    private static bool TreeToolPrefix(Tree __instance, Tool? __0, ref bool __result)
    {
        if (!IsKeptTree(__instance.Location, __instance.Tile))
            return true;
        __result = false;
        if (__0 is Axe axe && axe.getLastFarmerToUse() is { } who && who.IsLocalPlayer)
        {
            __instance.Location.playSound("woodyHit");
            Game1.drawObjectDialogue(Game1.content.LoadString(KeepTreeLine));
        }
        return false;
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
        Replant(vale, monitor);
        return removed;
    }

    // Any of the Three Trees that's missing (or a stump, a sapling, another kind of tree)
    // grows back full-sized overnight. A thing you placed on its tile keeps it away.
    private static void Replant(GameLocation vale, IMonitor monitor)
    {
        foreach (var (tile, type) in KeptTrees(vale))
        {
            if (vale.terrainFeatures.TryGetValue(tile, out var f) && f is Tree t && t.treeType.Value == type && t.growthStage.Value >= Tree.treeStage && !t.stump.Value)
                continue;
            if (vale.objects.TryGetValue(tile, out var obj))
            {
                if (!obj.isDebrisOrForage())
                    continue;
                vale.objects.Remove(tile);
            }
            vale.terrainFeatures.Remove(tile);
            vale.terrainFeatures.Add(tile, new Tree(type, Tree.treeStage));
            monitor.Log($"Vale grounds: replanted the overlook's tree {type} at {tile.X},{tile.Y}", LogLevel.Trace);
        }
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
