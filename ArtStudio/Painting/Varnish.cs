using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Varnishing (2026-10-25), the painter's cask. Put a finished painting in
// a Varnishing Cabinet (Painting level 7) with a tin of Varnish, and it
// cures over the following weeks: a satin finish after 7 days, gloss after
// 14 and a gallery finish after 28, each worth more. As with a cask, the
// cabinet gives the painting up by itself only once it's fully cured; hit
// it with a tool to take it out sooner, at the finish it has reached (before
// 7 days, unvarnished, and the varnish is wasted). A painting is varnished
// once.
//
// The painting waits as the cabinet's held object; the day it went in is
// in the cabinet's modData, and its finish in the painting's.
internal static class Varnish
{
    public const string VarnishId = ModEntry.Id + "_Varnish";
    public const string CabinetId = ModEntry.Id + "_VarnishCabinet";
    public const string QualifiedCabinetId = "(BC)" + CabinetId;
    public const int CabinetSprite = 2, VarnishSprite = 10;
    public const int UnlockLevel = 7;

    private const string StartKey = ModEntry.Id + "/VarnishStart";
    public const string FinishKey = ModEntry.Id + "/Finish";
    private const int Waiting = int.MaxValue / 2;     // "working" until we say it's ready

    public static (int Days, string Key, float Value)[] Finishes => Core.Market.Finishes;

    // Where the canvas on the cabinet's rack is, in sprite pixels (tools/art/varnish_items.py).
    private static readonly Rectangle RackCanvas = new(4, 13, 8, 10);

    private static IMonitor _monitor = null!;

    public static void Apply(Harmony harmony, IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.performObjectDropInAction)),
            prefix: new HarmonyMethod(typeof(Varnish), nameof(DropInPrefix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
            prefix: new HarmonyMethod(typeof(Varnish), nameof(CheckForActionPrefix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.performToolAction)),
            prefix: new HarmonyMethod(typeof(Varnish), nameof(ToolActionPrefix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            postfix: new HarmonyMethod(typeof(Varnish), nameof(DrawPostfix)));
        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    public static bool IsCabinet(Item? item) => item?.QualifiedItemId == QualifiedCabinetId;

    public static bool IsVarnish(Item? item) => item?.QualifiedItemId == "(O)" + VarnishId;

    public static string? FinishOf(Item painting) => painting.modData.TryGetValue(FinishKey, out string finish) ? finish : null;

    public static float ValueOf(string? finish) => Finishes.FirstOrDefault(f => f.Key == finish).Value is float v and > 0 ? v : 1f;

    public static int DaysIn(SObject cabinet) =>
        cabinet.modData.TryGetValue(StartKey, out string raw) && int.TryParse(raw, out int start) ? Game1.Date.TotalDays - start : 0;

    // The finish a painting has reached after so many days (null before satin).
    public static (int Days, string Key, float Value)? Reached(int days) =>
        Finishes.LastOrDefault(f => days >= f.Days) is { Key: not null } f ? f : null;

    // Puts the painting in (as the game does for any machine, the caller takes it from the hand).
    public static bool Load(SObject cabinet, Item painting, Farmer who)
    {
        var tin = who.Items.FirstOrDefault(IsVarnish);
        if (tin == null || cabinet.heldObject.Value != null) return false;
        who.Items.ReduceId(tin.QualifiedItemId, 1);
        Start(cabinet, painting);
        return true;
    }

    // A varnished painting starts curing (the tin's already been spent).
    public static void Start(SObject cabinet, Item painting)
    {
        cabinet.heldObject.Value = (SObject)painting.getOne();
        cabinet.modData[StartKey] = Game1.Date.TotalDays.ToString();
        cabinet.MinutesUntilReady = Waiting;
        cabinet.readyForHarvest.Value = false;
    }

    // Whether a painting can go in: not yet varnished.
    public static bool CanVarnish(Item? item) => PaintingItems.IsPainting(item) && FinishOf(item!) == null;

    public static bool IsCured(SObject cabinet) => cabinet.heldObject.Value != null && DaysIn(cabinet) >= Finishes[^1].Days;

    // The painting as it would come out now, at the finish it has reached
    // (a copy: the cabinet still holds it).
    public static SObject? Peek(SObject cabinet)
    {
        if (cabinet.heldObject.Value is not SObject held) return null;
        var painting = (SObject)held.getOne();
        if (Reached(DaysIn(cabinet)) is { } finish)
        {
            painting.modData[FinishKey] = finish.Key;
            painting.Price = (int)Math.Round(painting.Price * finish.Value);
        }
        return painting;
    }

    // Empties the cabinet.
    public static void Clear(SObject cabinet)
    {
        cabinet.heldObject.Value = null;
        cabinet.modData.Remove(StartKey);
        cabinet.MinutesUntilReady = 0;
        cabinet.readyForHarvest.Value = false;
    }

    // Takes the painting out at the finish it has reached.
    public static SObject? TakeOut(SObject cabinet)
    {
        var painting = Peek(cabinet);
        if (painting != null)
            Clear(cabinet);
        return painting;
    }

    private static bool DropInPrefix(SObject __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
    {
        if (!IsCabinet(__instance)) return true;
        __result = false;
        try
        {
            if (__instance.heldObject.Value != null || !PaintingItems.IsPainting(dropInItem)) return false;
            if (FinishOf(dropInItem) != null)
            {
                if (!probe) Game1.showRedMessage(I18n.Get("varnish.already"));
                return false;
            }
            if (!who.Items.Any(IsVarnish))
            {
                if (!probe) Game1.showRedMessage(I18n.Get("varnish.need-tin"));
                return false;
            }
            __result = true;
            if (probe) return false;
            Load(__instance, dropInItem, who);
            who.currentLocation?.playSound("bubbles");
            Game1.addHUDMessage(new HUDMessage(I18n.Get("varnish.loaded", new { painting = dropInItem.DisplayName })) { noIcon = true });
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't load the varnishing cabinet: {ex}", LogLevel.Error);
        }
        return false;
    }

    // Right-click: the fully cured painting comes out; before then, how it's coming along.
    private static bool CheckForActionPrefix(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!IsCabinet(__instance)) return true;
        __result = true;
        if (justCheckingForActivity || !who.IsLocalPlayer) return false;
        if (__instance.heldObject.Value is not SObject painting)
        {
            Game1.drawObjectDialogue(I18n.Get("varnish.empty"));
            return false;
        }
        int days = DaysIn(__instance);
        if (IsCured(__instance))
        {
            var cured = TakeOut(__instance)!;
            if (!who.addItemToInventoryBool(cured))
                Game1.createItemDebris(cured, __instance.TileLocation * 64f + new Vector2(32f), -1, __instance.Location);
            who.currentLocation?.playSound("coin");
            return false;
        }
        var next = Finishes.First(f => days < f.Days);
        string reached = Reached(days) is { } f ? I18n.Get("varnish.finish." + f.Key) : I18n.Get("varnish.tacky");
        Game1.drawObjectDialogue(I18n.Get("varnish.status", new
        {
            painting = painting.DisplayName,
            finish = reached,
            next = I18n.Get("varnish.finish." + next.Key),
            days = next.Days - days,
        }));
        return false;
    }

    // A tool takes the painting out early (the cabinet stays where it is).
    private static bool ToolActionPrefix(SObject __instance, Tool t, ref bool __result)
    {
        if (!IsCabinet(__instance) || __instance.heldObject.Value == null) return true;
        __result = false;
        var painting = TakeOut(__instance);
        if (painting != null)
        {
            Game1.createItemDebris(painting, __instance.TileLocation * 64f + new Vector2(32f), -1, __instance.Location);
            __instance.Location?.playSound("woodWhack");
        }
        return false;
    }

    // The painting on the rack behind the glass.
    private static void DrawPostfix(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!IsCabinet(__instance) || __instance.heldObject.Value is not SObject painting) return;
        try
        {
            var screen = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + RackCanvas.X * 4, y * 64 - 64 + RackCanvas.Y * 4));
            var area = new Rectangle((int)screen.X, (int)screen.Y, RackCanvas.Width * 4, RackCanvas.Height * 4);
            float depth = Math.Max(0f, ((y + 1) * 64 - 24) / 10000f) + x * 1E-05f + 2E-05f;
            PaintingRenderer.DrawArt(spriteBatch, painting, area, Color.White * alpha, depth);
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't draw the painting in a varnishing cabinet: {ex}", LogLevel.Error);
        }
    }

    // A painting fully cured shows the "ready" bubble, as a cask's does.
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer)
            MarkCured();
    }

    public static void MarkCured()
    {
        Utility.ForEachLocation(location =>
        {
            foreach (var obj in location.objects.Values)
                if (IsCabinet(obj) && obj.heldObject.Value != null && DaysIn(obj) >= Finishes[^1].Days && !obj.readyForHarvest.Value)
                {
                    obj.readyForHarvest.Value = true;
                    obj.MinutesUntilReady = 0;
                }
            return true;
        });
    }
}
