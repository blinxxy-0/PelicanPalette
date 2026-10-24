using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace ArtStudio;

// Married, Sable keeps running the shop. The game only sends a spouse out on
// their "marriage_<day>" schedule when it isn't raining (a few vanilla
// spouses get a hard-coded job exception); Sable gets the same one, so the
// shop doesn't close every rainy day. (Wednesdays it's closed anyway.)
internal static class SableMarriage
{
    private const string Npc = "Sable";
    private static IMonitor _monitor = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.TryLoadSchedule), Type.EmptyTypes),
            postfix: new HarmonyMethod(typeof(SableMarriage), nameof(TryLoadSchedulePostfix)));
    }

    private static void TryLoadSchedulePostfix(NPC __instance, ref bool __result)
    {
        try
        {
            if (__result || __instance.Name != Npc || !__instance.isMarried() || !Game1.isRaining || Utility.isFestivalDay())
                return;
            __result = __instance.TryLoadSchedule("marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth));
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't send Sable to the shop on a rainy day: {ex}", LogLevel.Error);
        }
    }
}
