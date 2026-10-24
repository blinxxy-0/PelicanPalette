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
    public const float PaintingPremium = 1.2f;

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
        GameLocation.RegisterTileAction(ModEntry.Id + "_Sign", (_, _, _, _) =>
        {
            Game1.drawObjectDialogue("Art Supply Shop^Canvases, pigments, brushes and frames. Paintings bought.^Open 10am to 6pm (Fridays till 5, Sundays from 1). Closed Wednesdays.");
            return true;
        });

        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.tryToReceiveActiveObject)),
            prefix: new HarmonyMethod(typeof(ArtShop), nameof(ReceivePrefix)));

        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    private static bool Counter(Farmer who)
    {
        var location = who.currentLocation;
        location.createQuestionDialogue("Welcome to the art supply shop.", new[]
        {
            new Response("Shop", "Browse supplies (and sell paintings)"),
            new Response("Board", "The commission board"),
            new Response("Leave", "Leave"),
        }, (farmer, answer) =>
        {
            if (answer == "Shop")
                OpenShop(farmer.currentLocation);
            else if (answer == "Board")
                OpenBoard(farmer);
        });
        return true;
    }

    // The shop only opens while Sable is behind the counter; otherwise the
    // shop data's closed entry tells you the hours.
    public static bool OpenShop(GameLocation location) =>
        Utility.TryOpenShopMenu(ShopId, location, KeeperArea);

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
                    $"It's lovely... but I was hoping for {Commissions.QualityText(commission.MinQuality)}. Could you try again?"), add: true);
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
