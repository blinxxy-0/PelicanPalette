using System.Collections.Generic;
using System.Linq;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Locations;

namespace ArtStudio.Social;

// Sable's stall at the Night Market (1.2): a little skiff of their own,
// moored below the submarine's dock, all three nights from 5pm. Built the
// way the game builds the market's own vendors: the skiff and Sable in it are
// tiles on the market's map (the content pack's map patch), with an action on
// Sable that opens the stall. They sell the Moonlit Frame (one a night), and
// buy unusual paintings at half as much again: views of the far places, the
// legendary fish, monsters, and views painted at night or in the rain.
// (Proposed in docs/design/1.2_plan.md and approved 2026-10-31.)
internal static class NightMarketStall
{
    public const string ActionKey = ModEntry.Id + "_SableStall";
    public const string ShopId = ModEntry.Id + "_NightMarketStall";
    public const int Opens = 1700;
    public const float BuyMultiplier = 1.5f;
    public const int BuysPerNight = 3;
    public const int FramePrice = 1500;
    private const string BoughtKey = ModEntry.Id + "/StallBought";

    public static void Register(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(AccessTools.Method(typeof(BeachNightMarket), nameof(BeachNightMarket.draw), new[] { typeof(SpriteBatch) }),
            postfix: new HarmonyMethod(typeof(NightMarketStall), nameof(DrawPostfix)));
        GameLocation.RegisterTileAction(ActionKey, (_, _, who, _) =>
        {
            Open(who);
            return true;
        });
        helper.Events.GameLoop.DayStarted += (_, _) => UpdateSable(Game1.timeOfDay);
        helper.Events.GameLoop.TimeChanged += (_, e) => UpdateSable(e.NewTime);
    }

    public static bool MarketTonight => Utility.IsPassiveFestivalDay("NightMarket");

    // On market evenings Sable's at the stall, so not in town (their
    // schedule takes them out of the shop at 4:30, down to their room).
    internal static void UpdateSable(int time)
    {
        if (!Context.IsMainPlayer || Game1.getCharacterFromName("Sable") is not { } sable) return;
        bool atMarket = MarketTonight && time >= Opens;
        if (atMarket)
            sable.IsInvisible = _hidden = true;
        else if (_hidden)
            sable.IsInvisible = _hidden = false;       // (only undoing our own hiding)
    }

    private static bool _hidden;

    // ---- Sable aboard ----

    // Where the stall's sheet sits on the market (tools/build_night_market_stall.py),
    // and where on it Sable stands, and the hull's front rail starts.
    public static readonly Point SheetTile = new(4, 37);
    private static readonly Point SableAt = new(40, 15);
    private const int RailTop = 41;
    private const string SheetAsset = "Mods/" + ModEntry.Id + "/NightMarketStall";

    // From 5pm, Sable in their skiff, facing the bridge, behind the hull's rail.
    private static void DrawPostfix(SpriteBatch b)
    {
        if (Game1.timeOfDay < Opens || Game1.getCharacterFromName("Sable") is not { } sable) return;
        var origin = new Vector2(SheetTile.X * 64, SheetTile.Y * 64);
        float depth = (SheetTile.Y + 3) * 64 / 10000f;
        b.Draw(sable.Sprite.Texture, Game1.GlobalToLocal(Game1.viewport, origin + new Vector2(SableAt.X, SableAt.Y) * 4),
            new Rectangle(0, 0, 16, 32), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
        var sheet = Game1.content.Load<Texture2D>(SheetAsset);
        b.Draw(sheet, Game1.GlobalToLocal(Game1.viewport, origin + new Vector2(0, RailTop) * 4),
            new Rectangle(0, RailTop, sheet.Width, sheet.Height - RailTop), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth + 0.0001f);
    }

    // ---- What Sable buys ----

    private static readonly HashSet<string> UnusualPages = new() { "far-places", "legends" };

    public static bool IsUnusual(Item item)
    {
        if (!PaintingItems.IsPainting(item) || PaintingData.Read(item) is not { } data) return false;
        return data.IsCreature || data.Conditions != null
            || Portfolio.Pages.Where(p => UnusualPages.Contains(p.Id)).Any(p => p.Slots.Any(s => s.Fits(item, data)));
    }

    public static int OfferFor(Item painting) => (int)System.Math.Round(((Object)painting).sellToStorePrice() * painting.Stack * BuyMultiplier);

    private static string Tonight => $"{Game1.year}-{Game1.seasonIndex}-{Game1.dayOfMonth}";

    public static int BoughtTonight(Farmer who) =>
        who.modData.TryGetValue(BoughtKey, out string raw) && raw.Split(':') is [var night, var count] && night == Tonight && int.TryParse(count, out int n) ? n : 0;

    // Sells a painting to Sable: the money, and one fewer they'll take tonight.
    public static int Sell(Farmer who, Item painting)
    {
        int price = OfferFor(painting);
        who.removeItemFromInventory(painting);
        who.Money += price;
        who.modData[BoughtKey] = $"{Tonight}:{BoughtTonight(who) + 1}";
        Game1.playSound("purchaseClick");
        return price;
    }

    // ---- At the stall ----

    private static NPC? Sable => Game1.getCharacterFromName("Sable");

    public static void Open(Farmer who)
    {
        if (Game1.timeOfDay < Opens)
        {
            Game1.drawObjectDialogue(I18n.Get("stall.closed"));
            return;
        }
        var responses = new[]
        {
            new Response("frames", I18n.Get("stall.answer.frames")),
            new Response("sell", I18n.Get("stall.answer.sell")),
            new Response("lupini", I18n.Get("stall.answer.lupini")),
            new Response("leave", I18n.Get("stall.answer.leave")),
        };
        who.currentLocation.createQuestionDialogue(I18n.Get("stall.greeting"), responses, (farmer, answer) => Answer(farmer, answer), Sable);
    }

    private static void Answer(Farmer who, string answer)
    {
        switch (answer)
        {
            case "frames":
                Utility.TryOpenShopMenu(ShopId, "Sable");
                break;
            case "sell":
                OfferPainting(who);
                break;
            case "lupini":
                Say(I18n.Get("stall.lupini"));
                break;
        }
    }

    private static void OfferPainting(Farmer who)
    {
        if (BoughtTonight(who) >= BuysPerNight)
            Say(I18n.Get("stall.full"));
        else if (!who.Items.Any(i => i != null && IsUnusual(i)))
            Say(I18n.Get("stall.nothing"));
        else
            Game1.activeClickableMenu = new PaintingPickerMenu(I18n.Get("stall.pick"), IsUnusual, item =>
            {
                int price = Sell(who, item);
                Say(I18n.Get(BoughtTonight(who) >= BuysPerNight ? "stall.sold-last" : "stall.sold", new { painting = item.DisplayName, price }));
            }, item => I18n.Get("stall.pick-hover", new { painting = item.DisplayName, price = OfferFor(item) }));
    }

    private static void Say(string text)
    {
        if (Sable is { } sable)
        {
            sable.CurrentDialogue.Clear();
            sable.CurrentDialogue.Push(new Dialogue(sable, null, text));
            Game1.drawDialogue(sable);
        }
        else
            Game1.drawObjectDialogue(text);
    }

    // The stall's shop: the Moonlit Frame, one a night.
    public static void AddShop(IDictionary<string, ShopData> shops)
    {
        shops[ShopId] = new ShopData
        {
            Owners = new List<ShopOwnerData>
            {
                new()
                {
                    Name = "Sable",
                    Dialogues = new List<ShopDialogueData> { new() { Id = "Default", Dialogue = I18n.Get("stall.shop") } },
                },
            },
            Items = new List<ShopItemData>
            {
                new()
                {
                    Id = "MoonlitFrame",
                    ItemId = "(O)" + ArtItems.MoonlitFrameItemId,
                    Price = FramePrice,
                    AvailableStock = 1,
                    AvailableStockLimit = LimitedStockMode.Player,
                },
            },
        };
    }
}
