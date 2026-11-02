using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// The Framer's Bench (1.3, docs/design/1.3_plan.md section 5): put a framed
// painting in, and the next morning take out the painting as it was and its
// empty frame, so frames can be collected and swapped. A recipe at Painting
// level 4, or bought from Sable. Works with Automate: the painting comes out
// first, then the frame.
//
// It holds the painting (its held object) and, until it's been taken, the
// frame's item id in its modData; the day it went in decides when it's done.
internal static class FramersBench
{
    public const string BenchId = ModEntry.Id + "_FramersBench";
    public const string QualifiedBenchId = "(BC)" + BenchId;
    public const int BenchSprite = 4;
    public const int UnlockLevel = 4;

    private const string StartKey = ModEntry.Id + "/BenchStart";
    private const string FrameKey = ModEntry.Id + "/BenchFrame";
    private const int Waiting = int.MaxValue / 2;

    private static IMonitor _monitor = null!;

    public static void Apply(Harmony harmony, IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.performObjectDropInAction)),
            prefix: new HarmonyMethod(typeof(FramersBench), nameof(DropInPrefix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
            prefix: new HarmonyMethod(typeof(FramersBench), nameof(CheckForActionPrefix)));
        helper.Events.GameLoop.DayStarted += (_, _) =>
        {
            if (!Context.IsMainPlayer) return;
            Utility.ForEachLocation(location =>
            {
                foreach (var bench in location.objects.Values.Where(IsBench))
                    if (IsDone(bench))
                    {
                        bench.MinutesUntilReady = 0;
                        bench.readyForHarvest.Value = true;
                    }
                return true;
            });
        };
    }

    public static bool IsBench(Item? item) => item?.QualifiedItemId == QualifiedBenchId;

    // A framed painting (in any style).
    public static bool IsInput(Item? item) => Frames.StyleOf(item) != null && PaintingData.Read(item!) != null;

    private static int DaysIn(SObject bench) =>
        bench.modData.TryGetValue(StartKey, out string raw) && int.TryParse(raw, out int start) ? Game1.Date.TotalDays - start : 0;

    // Done the morning after it went in.
    public static bool IsDone(SObject bench) => bench.heldObject.Value != null && DaysIn(bench) >= 1;

    public static void Start(SObject bench, Item framed)
    {
        if (ArtHooks.Unframe(framed) is not { } parts) return;
        bench.heldObject.Value = parts.Painting;
        bench.modData[FrameKey] = parts.Frame.QualifiedItemId;
        bench.modData[StartKey] = Game1.Date.TotalDays.ToString();
        bench.MinutesUntilReady = Waiting;
        bench.readyForHarvest.Value = false;
    }

    // What comes out next: the painting, then the frame.
    public static Item? Peek(SObject bench) => bench.heldObject.Value?.getOne();

    // Takes out what's showing: after the painting, the frame takes its place.
    public static void Clear(SObject bench)
    {
        if (bench.heldObject.Value != null && bench.modData.TryGetValue(FrameKey, out string frame) && !IsFrameItem(bench.heldObject.Value))
        {
            bench.heldObject.Value = ItemRegistry.Create<SObject>(frame);
            return;
        }
        bench.heldObject.Value = null;
        bench.modData.Remove(FrameKey);
        bench.modData.Remove(StartKey);
        bench.MinutesUntilReady = 0;
        bench.readyForHarvest.Value = false;
    }

    private static bool IsFrameItem(Item item) => ArtItems.FrameStyles.Any(s => item.QualifiedItemId == "(O)" + s.ItemId);

    private static bool DropInPrefix(SObject __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
    {
        if (!IsBench(__instance)) return true;
        __result = false;
        try
        {
            if (__instance.heldObject.Value != null || !IsInput(dropInItem)) return false;
            __result = true;
            if (probe) return false;
            Start(__instance, dropInItem);
            who.currentLocation?.playSound("woodWhack");
            Game1.addHUDMessage(new HUDMessage(I18n.Get("bench.loaded", new { painting = __instance.heldObject.Value?.DisplayName ?? dropInItem.DisplayName })) { noIcon = true });
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't load the Framer's Bench: {ex}", LogLevel.Error);
        }
        return false;
    }

    // Right-click: the painting and its frame, once it's done.
    private static bool CheckForActionPrefix(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!IsBench(__instance)) return true;
        __result = true;
        if (justCheckingForActivity || !who.IsLocalPlayer) return false;
        if (__instance.heldObject.Value == null)
        {
            Game1.drawObjectDialogue(I18n.Get("bench.empty"));
            return false;
        }
        if (!IsDone(__instance))
        {
            Game1.drawObjectDialogue(I18n.Get("bench.working", new { painting = __instance.heldObject.Value.DisplayName }));
            return false;
        }
        while (__instance.heldObject.Value is SObject item)
        {
            var taken = item.getOne();
            if (!who.addItemToInventoryBool(taken))
                Game1.createItemDebris(taken, __instance.TileLocation * 64f + new Vector2(32f), -1, __instance.Location);
            Clear(__instance);
        }
        who.currentLocation?.playSound("coin");
        return false;
    }

    // For the dev test: as if it went in yesterday.
    internal static void DevAge(SObject bench) => bench.modData[StartKey] = (Game1.Date.TotalDays - 1).ToString();
}
