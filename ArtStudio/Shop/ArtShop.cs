using System;
using System.Linq;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace ArtStudio.Shop;

// The art supply shop in Cindersap Forest. The building, interior and stock
// are in the Content Patcher pack; this is the behaviour: the counter, the
// commission board, buying paintings at a premium, and commission deliveries.
internal static class ArtShop
{
    public const string ShopId = ModEntry.Id + "_ArtShop";
    public const string LocationName = ModEntry.Id + "_ArtShop";

    // The art shop pays more for paintings than Pierre does.
    public const float PaintingPremium = Core.Market.ArtShopPremium;

    // Where Sable stands to serve, behind the counter (the row above it).
    public static readonly Rectangle KeeperArea = new(2, 4, 4, 1);

    // The building and its yard in the Forest, kept clear of weeds, stones,
    // bushes and trees (on existing saves, and wherever the Forest spawns them).
    private static readonly Rectangle Footprint = new(76, 28, 13, 11);

    private static IMonitor _monitor = null!;
    private static IReflectionHelper _reflection = null!;
    private static readonly Random Random = new();

    public static void Apply(Harmony harmony, IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        _reflection = helper.Reflection;

        GameLocation.RegisterTileAction(ModEntry.Id + "_Counter", (_, _, who, _) => Counter(who));
        GameLocation.RegisterTileAction(ModEntry.Id + "_Board", (_, _, who, _) => OpenBoard(who));
        // The display easel in the yard paints like one you set down
        // yourself: outdoors, so it offers the view in front of it.
        // Sable paints at it some mornings; then it's theirs.
        GameLocation.RegisterTileAction(ModEntry.Id + "_YardEasel", (location, _, who, tile) =>
        {
            if (location.characters.Any(npc => npc.Name == "Sable" && npc.TilePoint == new Point(tile.X, tile.Y + 1)))
                Game1.drawObjectDialogue(I18n.Get("shop.yard-easel.busy"));
            else
                ArtHooks.OpenEasel(who, tile.ToVector2());
            return true;
        });
        GameLocation.RegisterTileAction(ModEntry.Id + "_Sign", (_, _, _, _) =>
        {
            Game1.drawObjectDialogue(I18n.Get("shop.sign"));
            return true;
        });

        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.lockedDoorWarp)),
            prefix: new HarmonyMethod(typeof(ArtShop), nameof(TrapdoorPrefix)));
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.tryToReceiveActiveObject)),
            prefix: new HarmonyMethod(typeof(ArtShop), nameof(ReceivePrefix)));
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(ArtShop), nameof(WatchPrefix)));

        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    // ---- The trapdoor down to Sable's room ----

    public const string Basement = ModEntry.Id + "_SableBasement";

    // The trapdoor's tiles are a LockedDoorWarp, so Sable goes down it at
    // night like through any door. Your way down follows the rule for a
    // villager's bedroom door instead of the warp's own: 2 hearts (and once
    // you've been let in, it stays open, as vanilla doors do), or the key to
    // the town.
    private static bool TrapdoorPrefix(Point tile, string locationName)
    {
        if (locationName != Basement)
            return true;
        var who = Game1.player;
        if (MayGoDown(who))
        {
            who.completelyStopAnimatingOrDoingAction();
            Game1.playSound("stairsdown");
            Game1.warpFarmer(locationName, tile.X, tile.Y, Game1.down);
        }
        else
            Game1.drawObjectDialogue(I18n.Get("shop.trapdoor.locked"));
        return false;
    }

    internal static bool MayGoDown(Farmer who)
    {
        if (who.getFriendshipHeartLevelForNPC("Sable") >= 2)
            who.mailReceived.Add("doorUnlockSable");
        return who.mailReceived.Contains("doorUnlockSable") || who.HasTownKey;
    }

    private static bool Counter(Farmer who)
    {
        var location = who.currentLocation;
        location.createQuestionDialogue(I18n.Get("shop.counter"), new[]
        {
            new Response("Shop", I18n.Get("shop.counter.shop")),
            new Response("Board", I18n.Get("shop.counter.board")),
            new Response("Consign", I18n.Get("shop.counter.consign")),
            new Response("Record", I18n.Get("shop.counter.record")),
            new Response("Leave", I18n.Get("shop.counter.leave")),
        }, (farmer, answer) =>
        {
            if (answer == "Shop")
                OpenShop(farmer.currentLocation);
            else if (answer == "Board")
                OpenBoard(farmer);
            else if (answer == "Consign")
                OpenConsign(farmer);
            else if (answer == "Record")
                Game1.activeClickableMenu = new Menus.CollectionMenu(farmer);
        });
        return true;
    }

    // Sable takes paintings in person (or, married, you hang them yourself).
    private static void OpenConsign(Farmer who)
    {
        bool trusted = MarriedToSable(who);
        if (!trusted && !SableAtCounter(who.currentLocation))
        {
            Game1.drawObjectDialogue(I18n.Get("consign.closed"));
            return;
        }
        Game1.activeClickableMenu = new ConsignMenu(who);
    }

    // ---- Watching Sable paint (1.1) ----

    // Sable paints at the yard easel on Friday mornings, standing on this
    // tile (Forest 80, 36; the easel is the tile above).
    public static readonly Point YardEaselSpot = new(80, 36);
    private const string WatchedKey = ModEntry.Id + "/WatchedSable";
    public const int WatchMinutes = 30, WatchXp = 15;

    public static bool SableAtYardEasel(NPC npc) =>
        npc.Name == "Sable" && npc.currentLocation?.Name == "Forest" && npc.TilePoint == YardEaselSpot && Game1.timeOfDay < 930;

    // Once Sable's said what's on the easel, talking to them again offers to
    // stay and watch: half an hour, and a little Painting XP (once a day).
    private static bool WatchPrefix(NPC __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (!SableAtYardEasel(__instance) || who.ActiveObject != null || __instance.CurrentDialogue.Count > 0
                || !who.friendshipData.TryGetValue("Sable", out var friendship) || !friendship.TalkedToToday)
                return true;
            __result = true;
            if (WatchedToday(who))
            {
                Game1.drawObjectDialogue(I18n.Get("yard.watch.again"));
                return false;
            }
            __instance.faceTowardFarmerForPeriod(1000, 3, false, who);
            who.currentLocation.createQuestionDialogue(I18n.Get("yard.watch.ask"), new[]
            {
                new Response("Watch", I18n.Get("yard.watch.yes")),
                new Response("Leave", I18n.Get("yard.watch.no")),
            }, (_, answer) =>
            {
                if (answer == "Watch")
                    Watch(who, __instance);
            });
            return false;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't offer to watch Sable paint: {ex}", LogLevel.Error);
            return true;
        }
    }

    public static bool WatchedToday(Farmer who) =>
        who.modData.TryGetValue(WatchedKey, out string raw) && raw == Game1.Date.TotalDays.ToString();

    public static void Watch(Farmer who, NPC sable)
    {
        who.modData[WatchedKey] = Game1.Date.TotalDays.ToString();
        Game1.globalFadeToBlack(() =>
        {
            for (int i = 0; i < WatchMinutes / 10; i++)
                Game1.performTenMinuteClockUpdate();
            int newLevel = ArtProgress.AddXp(who, WatchXp);
            Game1.globalFadeToClear();
            sable.setNewDialogue(new Dialogue(sable, null, I18n.Get("yard.watch.done")), add: true);
            Game1.drawDialogue(sable);
            if (newLevel > 0)
                Game1.addHUDMessage(new HUDMessage(I18n.Get("results.level-up", new { level = newLevel })) { noIcon = true });
        });
    }

    // Married to (or rooming with) Sable. Read from Sable's friendship rather
    // than Farmer.spouse, which holds only one name: with Polyamory Sweet,
    // Sable may be one of several spouses.
    public static bool MarriedToSable(Farmer who) =>
        who.friendshipData.TryGetValue("Sable", out var friendship) && (friendship.IsMarried() || friendship.IsRoommate());

    // The shop opens while Sable is behind the counter; otherwise the shop
    // data's closed entry tells you the hours. Married to Sable, you're never
    // turned away: when they're out, the till is left on trust for you.
    public static bool OpenShop(GameLocation location)
    {
        if (MarriedToSable(Game1.player) && !SableAtCounter(location))
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("shop.on-trust")) { noIcon = true });
            return Utility.TryOpenShopMenu(ShopId, "None");
        }
        return Utility.TryOpenShopMenu(ShopId, location, KeeperArea);
    }

    public static bool SableAtCounter(GameLocation location) =>
        location.characters.Any(npc => npc.Name == "Sable" && KeeperArea.Contains(npc.TilePoint));

    private static bool OpenBoard(Farmer who)
    {
        Game1.activeClickableMenu = new CommissionBoardMenu(who);
        return true;
    }

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is ShopMenu shop && shop.ShopId == ShopId)
            _reflection.GetField<float>(shop, "sellPercentage").SetValue(PaintingPremium);
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // The world is the host's to tidy; each player has their own board.
        if (Context.IsMainPlayer)
            ClearFootprint();
        Commissions.NewDay(Game1.player, _monitor, Random);
    }

    private static void ClearFootprint()
    {
        var forest = Game1.getLocationFromName("Forest");
        if (forest == null) return;
        for (int x = Footprint.Left; x < Footprint.Right; x++)
        for (int y = Footprint.Top; y < Footprint.Bottom; y++)
        {
            var tile = new Vector2(x, y);
            forest.objects.Remove(tile);
            forest.terrainFeatures.Remove(tile);
        }
        var tileArea = new Rectangle(Footprint.X * 64, Footprint.Y * 64, Footprint.Width * 64, Footprint.Height * 64);
        foreach (var clump in forest.resourceClumps.Where(c => c.getBoundingBox().Intersects(tileArea)).ToList())
            forest.resourceClumps.Remove(clump);
        foreach (var feature in forest.largeTerrainFeatures.Where(f => f.getBoundingBox().Intersects(tileArea)).ToList())
            forest.largeTerrainFeatures.Remove(feature);
    }

    // Handing a commissioned painting to its client delivers it instead of
    // giving it as a gift (so it doesn't use up the week's gifts).
    private static bool ReceivePrefix(NPC __instance, Farmer who, bool probe, ref bool __result)
    {
        try
        {
            if (who.ActiveObject is not SObject painting || !PaintingItems.IsPainting(painting)) return true;
            var commission = Commissions.ActiveFor(who, __instance.Name);
            if (commission == null || !commission.Matches(painting)) return true;

            __result = true;
            if (probe) return false;
            if (painting.Quality < commission.MinQuality)
            {
                __instance.setNewDialogue(new Dialogue(__instance, null,
                    I18n.Get("commission.not-good-enough", new { quality = Commissions.QualityText(commission.MinQuality) })), add: true);
                Game1.drawDialogue(__instance);
                return false;
            }
            if (!commission.MeetsRequirement(painting))
            {
                __instance.setNewDialogue(new Dialogue(__instance, null, I18n.Get("commission.needs." + commission.Requirement)), add: true);
                Game1.drawDialogue(__instance);
                return false;
            }
            Commissions.Deliver(who, __instance, commission, painting);
            return false;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Couldn't check a commission delivery: {ex}", LogLevel.Error);
            return true;
        }
    }
}
