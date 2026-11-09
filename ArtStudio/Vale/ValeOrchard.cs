#if DEBUG
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace ArtStudio.Vale;

// The old orchard's fruit trees (tools/vale_decor.py, zone E) stand among the
// wild trees, so a sapling there is always "blocked" and the game tells the
// player every morning that it "wasn't able to grow" (FruitTree.dayUpdate; the
// user, 2026-11-08). The map now spawns and regrows them fully grown
// ("SpawnTree: fruit <id> 4 4"), which never sends that message; this grows any
// sapling an older Vale left in a save (regrown at the game's default stage 2
// before that) to the full-grown tree a new save gets.
internal static class ValeOrchard
{
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        helper.Events.GameLoop.DayStarted += (_, _) =>
        {
            if (!Context.IsMainPlayer || Game1.getLocationFromName(ValeBus.Vale) is not { } vale)
                return;
            int grown = 0;
            foreach (var (tile, tree) in vale.terrainFeatures.Pairs
                         .Where(p => p.Value is FruitTree { growthStage.Value: < FruitTree.treeStage })
                         .Select(p => (p.Key, (FruitTree)p.Value)).ToList())
            {
                string? spawn = vale.doesTileHaveProperty((int)tile.X, (int)tile.Y, "SpawnTree", "Paths");
                if (spawn?.StartsWith("fruit ") != true)
                    continue;
                tree.growthStage.Value = FruitTree.treeStage;
                tree.daysUntilMature.Value = FruitTree.GrowthStageToDaysUntilMature(FruitTree.treeStage);
                grown++;
            }
            if (grown > 0)
                monitor.Log($"Gilded Vale: grew {grown} orchard sapling(s) to full trees.", LogLevel.Trace);
        };
    }
}
#endif
