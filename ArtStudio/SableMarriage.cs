using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Inventories;
using StardewValley.Triggers;
using SObject = StardewValley.Object;

namespace ArtStudio;

// Married, Sable keeps running the shop. The game only sends a spouse out on
// their "marriage_<day>" schedule when it isn't raining (a few vanilla
// spouses get a hard-coded job exception); Sable gets the same one, so the
// shop doesn't close every rainy day. (Wednesdays it's closed anyway.)
//
// And married life with a painter (1.1): every so often Sable paints the
// farm from their patio easel and gives you the painting next morning; and
// some mornings they have something to say about a painting of yours you've
// hung in the house.
internal static class SableMarriage
{
    private const string Npc = "Sable";
    private static IMonitor _monitor = null!;

    public const double FarmPaintingChance = 1.0 / 7, WallCommentChance = 0.2;
    public const string GiveGiftAction = ModEntry.Id + "_SableGift";
    public const string PaintsTodayKey = ModEntry.Id + "/SablePaintsFarm";
    private const string LastWallKey = ModEntry.Id + "/SableWallComment";
    private const int WallLines = 4;

    public static void Apply(Harmony harmony, IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.TryLoadSchedule), Type.EmptyTypes),
            postfix: new HarmonyMethod(typeof(SableMarriage), nameof(TryLoadSchedulePostfix)));
        helper.Events.GameLoop.DayStarted += (_, _) => MorningOf(Game1.player, Game1.random);
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        TriggerActionManager.RegisterAction(GiveGiftAction, GiveGiftTrigger);
    }

    private static bool GiveGiftTrigger(string[] args, TriggerActionContext context, out string? error)
    {
        error = null;
        GiveGift(Game1.player);
        return true;
    }

    // Sable's paintings for you, waiting to be handed over (saved with the farm).
    public static Inventory Gifts(Farmer who) =>
        Game1.player.team.GetOrCreateGlobalInventory(ModEntry.Id + "_SableGift_" + who.UniqueMultiplayerID);

    // The morning: a painting from yesterday to give, or a word about one on
    // the wall; and maybe a day for painting the farm.
    public static void MorningOf(Farmer who, Random random)
    {
        try
        {
            if (!Shop.ArtShop.MarriedToSable(who) || Game1.getCharacterFromName(Npc) is not NPC sable) return;
            bool giving = Gifts(who).Any(i => i != null);
            if (giving)
                sable.setNewDialogue(new Dialogue(sable, null, I18n.Get("married.farm-painting") + "#$action " + GiveGiftAction), add: true);
            else if (random.NextDouble() < WallCommentChance)
                CommentOnWall(who, sable, random);
            if (!giving && random.NextDouble() < FarmPaintingChance)
                who.modData[PaintsTodayKey] = Game1.Date.TotalDays.ToString();
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't set up Sable's morning: {ex}", LogLevel.Error);
        }
    }

    // A painting of yours hung in the house (not the last one they mentioned).
    public static bool CommentOnWall(Farmer who, NPC sable, Random random)
    {
        var home = Utility.getHomeOfFarmer(who);
        who.modData.TryGetValue(LastWallKey, out string? last);
        // Each by its own title ("Still Life: Melon"), not the furniture's name.
        var yours = home.furniture.Where(ArtItems.IsFramed).Select(PaintingData.Read)
            .Where(d => d?.Painter == who.Name).Select(d => PaintingItems.Title(d!)).ToList();
        if (yours.Count == 0) return false;
        var fresh = yours.Where(t => t != last).ToList();
        string pick = (fresh.Count > 0 ? fresh : yours)[random.Next(fresh.Count > 0 ? fresh.Count : yours.Count)];
        who.modData[LastWallKey] = pick;
        sable.setNewDialogue(new Dialogue(sable, null, I18n.Get("married.wall." + random.Next(WallLines), new { painting = pick })), add: true);
        return true;
    }

    // On a painting day, the first time you're out on the farm, the view from
    // Sable's patio easel is painted.
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!e.IsMultipleOf(60) || !Context.IsPlayerFree) return;
        var who = Game1.player;
        if (!who.modData.TryGetValue(PaintsTodayKey, out string? day) || day != Game1.Date.TotalDays.ToString()) return;
        if (Game1.currentLocation is not Farm farm) return;
        try
        {
            who.modData.Remove(PaintsTodayKey);
            if (PaintFarm(farm, Game1.random) is SObject painting)
                Gifts(who).Add(painting);
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't paint Sable's view of the farm: {ex}", LogLevel.Error);
        }
    }

    public static SObject? PaintFarm(Farm farm, Random random)
    {
        var capture = Views.Take(farm, farm.spousePatioSpot.ToVector2());
        if (capture == null) return null;
        int size = PaintingRules.CanvasSize(CanvasKind.Medium);
        var all = Enumerable.Range(0, Palette.All.Length).ToList();
        var reference = Views.Reference(capture, size, all);
        int quality = random.NextDouble() < 1 / 3.0 ? PaintingRules.Iridium : PaintingRules.Gold;
        var data = new PaintingData
        {
            Size = size,
            Cells = reference.Cells,
            Purity = new Purity[size, size],
            Subject = capture.Subject,
            Stars = quality == PaintingRules.Iridium ? 5f : 4f,
            Difficulty = reference.Difficulty,
            Painter = Game1.getCharacterFromName(Npc)?.displayName ?? Npc,
            Date = I18n.Get("painting.date", new { season = Utility.getSeasonNameFromNumber(Game1.seasonIndex), day = Game1.dayOfMonth, year = Game1.year }),
        };
        int price = PaintingRules.Value(size, reference.Difficulty, Views.SubjectPrice(capture.Subject), 10);
        return PaintingItems.Create(data, quality, price);
    }

    // The $action in their morning line: the painting goes to you.
    public static void GiveGift(Farmer who)
    {
        var gifts = Gifts(who);
        foreach (var painting in gifts.Where(i => i != null).ToList())
        {
            gifts.Remove(painting);
            who.addItemByMenuIfNecessary(painting);
        }
        gifts.RemoveEmptySlots();
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
