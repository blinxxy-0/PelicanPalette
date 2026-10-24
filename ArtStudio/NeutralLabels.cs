using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio;

// The social page and the profile menu label a villager by gender:
// "(husband)" or "(wife)", "(boyfriend)" or "(girlfriend)", and anyone who
// isn't female gets the male word. Sable is they/them, so while their entry
// is being labelled, those strings come back gender-neutral.
internal static class NeutralLabels
{
    private const string Npc = "Sable";

    private static readonly Dictionary<string, string> Neutral = new()
    {
        ["Strings\\StringsFromCSFiles:SocialPage_Relationship_Husband"] = "(spouse)",
        ["Strings\\StringsFromCSFiles:SocialPage_Relationship_Wife"] = "(spouse)",
        ["Strings\\StringsFromCSFiles:SocialPage_Relationship_Boyfriend"] = "(partner)",
        ["Strings\\StringsFromCSFiles:SocialPage_Relationship_Girlfriend"] = "(partner)",
        ["Strings\\StringsFromCSFiles:SocialPage_Relationship_ExHusband"] = "(ex-spouse)",
        ["Strings\\StringsFromCSFiles:SocialPage_Relationship_ExWife"] = "(ex-spouse)",
        ["Strings\\UI:SocialPage_Relationship_MarriedToOtherPlayer_MaleNpc"] = "(married)",
        ["Strings\\UI:SocialPage_Relationship_MarriedToOtherPlayer_FemaleNpc"] = "(married)",
    };

    private static bool _labellingSable;
    private static IMonitor _monitor = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(AccessTools.Method(typeof(SocialPage), nameof(SocialPage.drawNPCSlot)),
            prefix: new HarmonyMethod(typeof(NeutralLabels), nameof(SlotPrefix)),
            finalizer: new HarmonyMethod(typeof(NeutralLabels), nameof(Done)));
        harmony.Patch(AccessTools.Method(typeof(ProfileMenu), "_SetCharacter"),
            prefix: new HarmonyMethod(typeof(NeutralLabels), nameof(ProfilePrefix)),
            finalizer: new HarmonyMethod(typeof(NeutralLabels), nameof(Done)));
        harmony.Patch(AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString), new[] { typeof(string) }),
            postfix: new HarmonyMethod(typeof(NeutralLabels), nameof(LoadStringPostfix)));
    }

    private static void SlotPrefix(SocialPage __instance, int i) =>
        _labellingSable = __instance.GetSocialEntry(i)?.InternalName == Npc;

    private static void ProfilePrefix(SocialPage.SocialEntry entry) =>
        _labellingSable = entry?.InternalName == Npc;

    private static Exception? Done(Exception? __exception)
    {
        _labellingSable = false;
        return __exception;
    }

    private static void LoadStringPostfix(string path, ref string __result)
    {
        try
        {
            if (_labellingSable && Neutral.TryGetValue(path, out string? neutral))
                __result = neutral;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't relabel {path}: {ex}", LogLevel.Error);
        }
    }
}
