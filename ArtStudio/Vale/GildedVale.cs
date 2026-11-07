#if DEBUG
using ArtStudio.Painting;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace ArtStudio.Vale;

// The Gilded Vale (1.4), in development: Debug builds only, like its content
// pack (the user, 2026-11-04: not in any release until they say so).
internal static class GildedVale
{
    public const string PaintSpotAction = ModEntry.Id + "_PaintSpot";

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        ValeBus.Register(helper, harmony, monitor);

        // Lupini's painting spots: sit at one and paint the view in front of it,
        // as at an easel you set down yourself (the user, 2026-11-07: "can we
        // make the palettes usable?").
        GameLocation.RegisterTileAction(PaintSpotAction, (_, _, who, tile) =>
        {
            ArtHooks.OpenEasel(who, tile.ToVector2());
            return true;
        });
    }
}
#endif
