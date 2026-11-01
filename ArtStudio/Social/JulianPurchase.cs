using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Social;

// Julian buys a painting a season (1.3): on a visit day, once you've talked
// to him, talking to him again with no gift in hand offers him a painting
// from your bag. He pays twice what a shop would (somewhere for a
// Masterpiece to go), and says what he thinks of it, as his review did.
internal static class JulianPurchase
{
    public const float PriceMultiplier = 2f;
    private const string BoughtKey = ModEntry.Id + "/JulianBought";

    public static void Register(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(JulianPurchase), nameof(CheckActionPrefix)));
    }

    private static string ThisSeason => $"{Game1.year}-{Game1.seasonIndex}";

    public static bool BoughtThisSeason(Farmer who) => who.modData.TryGetValue(BoughtKey, out string raw) && raw == ThisSeason;

    public static bool CanSell(Item? item) => item != null && PaintingItems.IsPainting(item) && PaintingData.Read(item) != null;

    public static int OfferFor(Item painting) => (int)Math.Round(((SObject)painting).sellToStorePrice() * PriceMultiplier);

    // Whether talking to him now offers him a painting.
    public static bool Offers(NPC npc, Farmer who) =>
        npc.Name == Julian.Name && Julian.IsVisitDay(Game1.dayOfMonth) && !npc.IsInvisible
        && who.ActiveObject == null && npc.CurrentDialogue.Count == 0
        && who.friendshipData.TryGetValue(Julian.Name, out var friendship) && friendship.TalkedToToday
        && !BoughtThisSeason(who) && who.Items.Any(CanSell);

    private static bool CheckActionPrefix(NPC __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (!Offers(__instance, who)) return true;
            __result = true;
            Ask(__instance, who);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static void Ask(NPC julian, Farmer who)
    {
        who.currentLocation.createQuestionDialogue(I18n.Get("julian.buy.ask"), new[]
        {
            new Response("yes", I18n.Get("julian.buy.yes")),
            new Response("no", I18n.Get("julian.buy.no")),
        }, (_, answer) =>
        {
            if (answer == "yes")
                Game1.activeClickableMenu = new PaintingPickerMenu(I18n.Get("julian.buy.pick"), CanSell, item => Buy(julian, who, item),
                    item => I18n.Get("julian.buy.pick-hover", new { painting = item.DisplayName, price = OfferFor(item) }));
        });
    }

    // He buys it: the money, his word on it, and no more this season.
    public static int Buy(NPC julian, Farmer who, Item painting)
    {
        int price = OfferFor(painting);
        string verdict = Verdict(painting);
        who.removeItemFromInventory(painting);
        who.Money += price;
        who.modData[BoughtKey] = ThisSeason;
        Game1.playSound("purchaseClick");
        julian.CurrentDialogue.Clear();
        julian.CurrentDialogue.Push(new Dialogue(julian, null, I18n.Get(verdict, new { painting = painting.DisplayName, price })));
        Game1.drawDialogue(julian);
        return price;
    }

    // What he says depends on how good it is.
    public static string Verdict(Item painting) =>
        PaintingData.IsMasterpiece(painting) ? "julian.buy.masterpiece"
        : painting.Quality >= PaintingRules.Iridium ? "julian.buy.iridium"
        : painting.Quality >= PaintingRules.Gold ? "julian.buy.gold"
        : "julian.buy.plain";
}
