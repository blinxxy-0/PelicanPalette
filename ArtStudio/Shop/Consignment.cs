using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Inventories;
using SObject = StardewValley.Object;

namespace ArtStudio.Shop;

// Consignment (2026-10-25): leave up to three paintings with Sable, who
// shows them on the shop's easels. Each finds a buyer in one to three days
// (sooner the better it is) and pays half again what a shop would. You can
// take a painting back any time before it sells.
//
// The paintings wait in a per-player global inventory (it saves with the
// game and is shared in multiplayer), each marked with the day it sells.
internal static class Consignment
{
    public const int MaxConsigned = 3;
    public const float Premium = Market.ConsignPremium;
    private const string SellsOnKey = ModEntry.Id + "/SellsOn";

    // The easels in Sable's corner (tile x, all on row 10) and the picture
    // area of each easel's canvas, in sprite pixels (tools/art/build_shop_furniture.py).
    private static readonly int[] EaselColumns = { 14, 15, 17 };
    private const int EaselRow = 10;
    private static readonly Rectangle CanvasBox = new(3, 5, 10, 11);

    private static IMonitor _monitor = null!;
    private static readonly Random Random = new();

    public static void Apply(Harmony harmony, IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.drawAboveFrontLayer)),
            postfix: new HarmonyMethod(typeof(Consignment), nameof(DrawPostfix)));
    }

    public static Inventory Consigned(Farmer who) =>
        Game1.player.team.GetOrCreateGlobalInventory(ModEntry.Id + "_Consigned_" + who.UniqueMultiplayerID);

    public static IEnumerable<Item> All(Farmer who) => Consigned(who).Where(i => i != null);

    public static bool HasRoom(Farmer who) => All(who).Count() < MaxConsigned;

    // What a buyer pays for it: half again the shop price.
    public static int Price(Item painting) => (int)Math.Round(((SObject)painting).sellToStorePrice() * painting.Stack * Premium);

    public static int SellsOn(Item painting) =>
        painting.modData.TryGetValue(SellsOnKey, out string raw) && int.TryParse(raw, out int day) ? day : Game1.Date.TotalDays + 1;

    public static int DaysLeft(Item painting) => Math.Max(1, SellsOn(painting) - Game1.Date.TotalDays);

    // Iridium sells by tomorrow, gold within two days, the rest in two or three.
    public static bool Consign(Farmer who, Item painting)
    {
        if (!PaintingItems.IsPainting(painting) || !HasRoom(who)) return false;
        int days = painting.Quality switch
        {
            >= 4 => 1,
            2 => Random.Next(1, 3),
            _ => Random.Next(2, 4),
        };
        painting.modData[SellsOnKey] = (Game1.Date.TotalDays + days).ToString();
        who.removeItemFromInventory(painting);
        Consigned(who).Add(painting);
        return true;
    }

    public static bool TakeBack(Farmer who, Item painting)
    {
        if (!who.couldInventoryAcceptThisItem(painting)) return false;
        Consigned(who).Remove(painting);
        Consigned(who).RemoveEmptySlots();
        painting.modData.Remove(SellsOnKey);
        who.addItemToInventory(painting);
        return true;
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e) => SellDue(Game1.player);

    // Pays for every consigned painting whose day has come.
    public static void SellDue(Farmer who)
    {
        var sold = All(who).Where(p => SellsOn(p) <= Game1.Date.TotalDays).ToList();
        foreach (var painting in sold)
        {
            int price = Price(painting);
            Consigned(who).Remove(painting);
            who.Money += price;
            who.totalMoneyEarned += (uint)price;
            ArtProgress.AddXp(who, PaintingRules.SaleXp + 2 * painting.Quality);
            Game1.addHUDMessage(new HUDMessage(I18n.Get("consign.sold", new { painting = painting.DisplayName, price })) { noIcon = true });
            _monitor.Log($"Consigned painting sold: {painting.DisplayName} for {price}g.", LogLevel.Trace);
        }
        if (sold.Count > 0)
            Consigned(who).RemoveEmptySlots();
    }

    // The consigned paintings on the easels in Sable's corner (over the
    // pictures the easels show otherwise), drawn with the Front layer they sit on.
    private static void DrawPostfix(GameLocation __instance, SpriteBatch b)
    {
        try
        {
            if (__instance.Name != ArtShop.LocationName) return;
            int i = 0;
            foreach (var painting in All(Game1.player).Take(EaselColumns.Length))
            {
                var world = new Vector2(EaselColumns[i++] * 64 + CanvasBox.X * 4, EaselRow * 64 + CanvasBox.Y * 4);
                var screen = Game1.GlobalToLocal(Game1.viewport, world);
                var box = new Rectangle((int)screen.X, (int)screen.Y, CanvasBox.Width * 4, CanvasBox.Height * 4);
                b.Draw(Game1.staminaRect, box, null, new Color(243, 236, 220), 0f, Vector2.Zero, SpriteEffects.None, 0.99f);
                PaintingRenderer.DrawArt(b, painting, new Rectangle(box.X, box.Y + 2, box.Width, box.Width), Color.White, 0.991f);
            }
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't draw the consigned paintings: {ex}", LogLevel.Error);
        }
    }
}
