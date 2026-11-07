#if DEBUG
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ArtStudio.Vale;

// The Gilded Vale (1.4), in development: Debug builds only, like its content
// pack (the user, 2026-11-04: not in any release until they say so).
internal static class GildedVale
{
    public const string PaintSpotAction = ModEntry.Id + "_PaintSpot";
    private const string LocationPrefix = ModEntry.Id + "_Vale";

    public static void Register(IModHelper helper)
    {
        // Lupini's painting spots: sit at one and paint the view in front of it,
        // as at an easel you set down yourself (the user, 2026-11-07: "can we
        // make the palettes usable?").
        GameLocation.RegisterTileAction(PaintSpotAction, (_, _, who, tile) =>
        {
            ArtHooks.OpenEasel(who, tile.ToVector2());
            return true;
        });
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    // The game's interior doors open as you walk into them and stay open until
    // everyone has left the building. In the Vale's houses they swing shut
    // behind you once you've walked on (the user, 2026-11-07: doors should
    // "open and close and open and close forever"); walking up opens them again.
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(10))
            return;
        GameLocation location = Game1.currentLocation;
        if (location == null || !location.Name.StartsWith(LocationPrefix) || !Game1.IsMasterGame)
            return;
        foreach (var pair in location.interiorDoors.Pairs.ToList())
        {
            if (!pair.Value)
                continue;
            Point door = pair.Key;
            bool someoneNear = location.farmers.Any(f => near(f.GetBoundingBox(), door))
                || location.characters.Any(c => near(c.GetBoundingBox(), door));
            if (!someoneNear)
            {
                location.interiorDoors[door] = false;
                location.playSound("doorClose", new Vector2(door.X, door.Y));
            }
        }
    }

    // Within a tile of the door's own tile, or of the tiles either side of it
    // you walk through.
    private static bool near(Rectangle box, Point door) =>
        box.Intersects(new Rectangle((door.X - 1) * 64, (door.Y - 2) * 64, 3 * 64, 5 * 64));
}
#endif
