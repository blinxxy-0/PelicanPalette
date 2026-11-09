#if DEBUG
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace ArtStudio.Vale;

// The Gilded Vale (1.4), in development: Debug builds only, like its content
// pack (the user, 2026-11-04: not in any release until they say so).
internal static class GildedVale
{
    public const string PaintSpotAction = ModEntry.Id + "_PaintSpot";
    public const string TakeGuideAction = ModEntry.Id + "_TakeGuide";

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        ValeBus.Register(helper, harmony, monitor);
        ValeResidents.Register(helper, harmony);
        ValeTests.Register(helper, monitor);
        GuideTests.Register(helper, monitor);
        ValeGrounds.Register(helper, monitor);
        ValeOrchard.Register(helper, monitor);
        ValeFish.Register(helper, monitor);
        ValeStudios.Register(helper, harmony, monitor);

        // Lupini's painting spots are found, not easels (the painters' guide design, section 4):
        // touch a stool with the guide to find its view. blinxxy.MuseArt_PaintSpot <spotId>;
        // a tile with no id does nothing. You can still paint any view with an easel you set down.
        PaintersGuide.Monitor = monitor;
        PaintSplash.Register(helper);
        GameLocation.RegisterTileAction(PaintSpotAction, (_, args, who, _) => PaintersGuide.OnTouch(who, args));
        // The frame by the Vale House's guest books holds the guide (ValeHouse.tmx, PaintersGuide.BoardTile).
        GameLocation.RegisterTileAction(TakeGuideAction, (_, _, who, _) => PaintersGuide.OnTake(who));
        PaintersGuide.Register(helper);
        PaintersGuide.Patch(harmony);
        // The Grove the guide's last page leads to, and the frame back (GildedGrove.cs).
        GildedGrove.Register(helper, monitor);
    }
}
#endif
