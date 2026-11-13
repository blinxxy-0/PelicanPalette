#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Pottery;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace ArtStudio.Vale;

// museart_claytest: Potter's Clay, Tobin's clay pit and his teaching wheel (ValeClay.cs),
// in a running game. Each line is [PASS] or [FAIL]. What it changes is put back: the pit's
// digs, the season, yesterday's rain, the farmer's energy and clay, the debris it dug up.
internal static class ClayTests
{
    private static readonly Point PitTile = new(55, 23);                 // the pit's north face
    private const string Pottery = ModEntry.Id + "_ValePottery";
    private static readonly Point[] WheelTiles = { new(17, 19), new(18, 19) };
    private const string DugKey = ModEntry.Id + "_ClayPitDug";

    private static IMonitor _monitor = null!;
    private static int _pass, _fail;

    private static void Check(string what, bool ok, string detail = "")
    {
        if (ok) _pass++; else _fail++;
        _monitor.Log($"[{(ok ? "PASS" : "FAIL")}] clay: {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);
    }

    public static void Run(IMonitor monitor)
    {
        _monitor = monitor;
        _pass = _fail = 0;
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        Game1.exitActiveMenu();
        CheckItem();
        CheckShop();
        CheckPit();
        CheckWheel();
        CheckTub();
        Game1.exitActiveMenu();
        monitor.Log($"museart_claytest: {_pass} passed, {_fail} failed.", _fail == 0 ? LogLevel.Info : LogLevel.Error);
    }

    // ---- The Slaking Tub: 3 Clay in, 1 Potter's Clay out, 8 hours later ----

    public const string TubId = ModEntry.Id + "_SlakingTub";
    public const string QualifiedTubId = "(BC)" + TubId;

    // An open tile on the farm near the house's front, for a tub.
    public static Vector2? FreeFarmTile(GameLocation farm, Point near)
    {
        for (int r = 1; r < 12; r++)
            for (int y = near.Y - r; y <= near.Y + r; y++)
                for (int x = near.X - r; x <= near.X + r; x++)
                {
                    var tile = new Vector2(x, y);
                    if (farm.CanItemBePlacedHere(tile) && !farm.objects.ContainsKey(tile) && !farm.terrainFeatures.ContainsKey(tile))
                        return tile;
                }
        return null;
    }

    private static void CheckTub()
    {
        var who = Game1.player;
        var farm = Game1.getFarm();
        // put back afterwards: the recipe, and the farmer's wood, copper bars, clay and potter's clay
        bool knew = who.craftingRecipes.ContainsKey(TubId);
        var counts = new[] { "(O)388", "(O)334", "(O)330", ValeClay.QualifiedItemId, QualifiedTubId }.ToDictionary(id => id, id => who.Items.CountId(id));
        Vector2? placed = null;
        try
        {
            Check("the Slaking Tub exists, a big craftable", ItemRegistry.Exists(QualifiedTubId) && ItemRegistry.Create(QualifiedTubId) is StardewValley.Object { bigCraftable.Value: true } t && t.DisplayName == "Slaking Tub",
                ItemRegistry.Create(QualifiedTubId).DisplayName);
            var data = ItemRegistry.GetDataOrErrorItem(QualifiedTubId);
            Check("its sprite loads: 16x32 frames, idle and in use", !data.IsErrorItem && data.GetTexture() is { Width: 32, Height: 32 }, data.TextureName);
            Check("it's a machine in the game's own system (Data/Machines)", DataLoader.Machines(Game1.content).TryGetValue(QualifiedTubId, out var machine)
                && machine.OutputRules.Count == 1 && machine.OutputRules[0].MinutesUntilReady == 480);

            // the recipe at Tobin's counter, for those who don't know it yet
            who.craftingRecipes.Remove(TubId);
            const string shopId = ModEntry.Id + "_ValePottery";
            var entry = DataLoader.Shops(Game1.content)[shopId].Items.FirstOrDefault(i => i.ItemId == QualifiedTubId);
            Check("Tobin's shop sells the tub's recipe", entry is { IsRecipe: true }, entry == null ? "not listed" : $"{entry.Price}g");
            bool opened = Utility.TryOpenShopMenu(shopId, "None", playOpenSound: false) && Game1.activeClickableMenu is ShopMenu;
            var recipe = (Game1.activeClickableMenu as ShopMenu)?.forSale.FirstOrDefault(i => i.QualifiedItemId == QualifiedTubId);
            Check("... and the open shop shows it as a recipe", opened && recipe is { IsRecipe: true }, recipe == null ? "not for sale" : $"{(Game1.activeClickableMenu as ShopMenu)!.itemPriceAndStock[recipe].Price}g");
            Game1.exitActiveMenu();

            // crafting it
            who.craftingRecipes.TryAdd(TubId, 0);
            var craft = new CraftingRecipe(TubId, false);
            Check("the recipe: 30 Wood, 1 Copper Bar, 10 Clay", craft.bigCraftable && craft.recipeList.Count == 3 && craft.recipeList["388"] == 30 && craft.recipeList["334"] == 1 && craft.recipeList["330"] == 10,
                string.Join(", ", craft.recipeList.Select(p => $"{p.Value} {p.Key}")));
            Give(who, "(O)388", 30); Give(who, "(O)334", 1); Give(who, "(O)330", 10);
            int woodBefore = who.Items.CountId("(O)388");
            Check("with the ingredients, it can be crafted", craft.doesFarmerHaveIngredientsInInventory());
            var made = craft.createItem();
            craft.consumeIngredients(null);
            Check("crafting makes a Slaking Tub and takes the ingredients", made.QualifiedItemId == QualifiedTubId && who.Items.CountId("(O)388") == woodBefore - 30, made.QualifiedItemId);

            // set down on the farm
            var tile = FreeFarmTile(farm, farm.GetMainFarmHouseEntry() + new Point(3, 3));
            if (tile is not { } spot)
            {
                Check("a free farm tile for the tub", false);
                return;
            }
            bool put = ((StardewValley.Object)made).placementAction(farm, (int)spot.X * 64, (int)spot.Y * 64, who);
            placed = spot;
            var tub = farm.objects.TryGetValue(spot, out var o) ? o : null;
            Check("it's set down on the farm", put && tub?.QualifiedItemId == QualifiedTubId, $"{spot}");
            if (tub == null)
                return;

            // what it takes
            var two = ItemRegistry.Create("(O)330", 2);
            Check("2 Clay is refused, and kept", !tub.performObjectDropInAction(two, false, who) && two.Stack == 2 && tub.heldObject.Value == null);
            Game1.hudMessages.Clear();
            var stone = ItemRegistry.Create("(O)390", 5);
            Check("other things are refused (Stone)", !tub.performObjectDropInAction(stone, false, who) && stone.Stack == 5 && tub.heldObject.Value == null);
            var pots = ItemRegistry.Create(ValeClay.QualifiedItemId, 5);
            Check("Potter's Clay is refused too", !tub.performObjectDropInAction(pots, false, who) && pots.Stack == 5 && tub.heldObject.Value == null);
            var five = ItemRegistry.Create("(O)330", 5);
            bool took = tub.performObjectDropInAction(five, false, who);
            Check("3 Clay from a stack of 5 go in", took && five.Stack == 2, $"{five.Stack} left");
            Check("... and it's slaking: Potter's Clay in 8 hours, shown in use", tub.heldObject.Value?.QualifiedItemId == ValeClay.QualifiedItemId && tub.MinutesUntilReady == 480 && !tub.readyForHarvest.Value && tub.showNextIndex.Value,
                $"{tub.heldObject.Value?.QualifiedItemId}, {tub.MinutesUntilReady} min");
            Check("a full tub takes no more", !tub.performObjectDropInAction(ItemRegistry.Create("(O)330", 3), false, who));
            tub.minutesElapsed(470);
            Check("not ready after 7 h 50", !tub.readyForHarvest.Value, $"{tub.MinutesUntilReady} min left");
            tub.minutesElapsed(10);
            Check("ready after 8 hours", tub.readyForHarvest.Value && tub.heldObject.Value?.QualifiedItemId == ValeClay.QualifiedItemId);
            Check("one filled at 10pm is ready by 6am (the night counts)", Utility.CalculateMinutesUntilMorning(2200) >= 480, $"{Utility.CalculateMinutesUntilMorning(2200)} min");
            int potters = who.Items.CountId(ValeClay.QualifiedItemId);
            tub.checkForAction(who);
            Check("the Potter's Clay is collected", who.Items.CountId(ValeClay.QualifiedItemId) == potters + 1 && tub.heldObject.Value == null && !tub.readyForHarvest.Value,
                $"{who.Items.CountId(ValeClay.QualifiedItemId) - potters} collected");
        }
        finally
        {
            Game1.exitActiveMenu();
            if (placed is { } p)
                farm.objects.Remove(p);
            if (!knew)
                who.craftingRecipes.Remove(TubId);
            foreach (var (id, n) in counts)
            {
                who.Items.ReduceId(id, 9999);
                if (n > 0)
                    Give(who, id, n);
            }
        }
    }

    private static void Give(Farmer who, string id, int n)
    {
        var item = ItemRegistry.Create(id, n);
        if (!who.addItemToInventoryBool(item))
            _monitor.Log($"clay test: no room for {n} {id}", LogLevel.Warn);
    }

    // ---- The item ----

    private static void CheckItem()
    {
        Check("Potter's Clay exists", ItemRegistry.Exists(ValeClay.QualifiedItemId), ValeClay.QualifiedItemId);
        var item = ItemRegistry.Create(ValeClay.QualifiedItemId);
        Check("it's called Potter's Clay, with a description", item.DisplayName == "Potter's Clay" && item.getDescription().Contains("Tobin"), $"{item.DisplayName}: {item.getDescription()}");
        var data = ItemRegistry.GetDataOrErrorItem(ValeClay.QualifiedItemId);
        Check("its icon loads (not the error item)", !data.IsErrorItem && data.GetTexture() is { Width: 16, Height: 16 }, data.TextureName);
        Check("it isn't the game's Clay", ItemRegistry.Create("(O)330").QualifiedItemId != item.QualifiedItemId && item.Name != "Clay", item.Name);
        Check("it stacks", item.maximumStackSize() > 1, item.maximumStackSize().ToString());
    }

    // ---- Tobin's shop ----

    private static void CheckShop()
    {
        const string shopId = ModEntry.Id + "_ValePottery";
        var shops = DataLoader.Shops(Game1.content);
        var entry = shops.TryGetValue(shopId, out var shop) ? shop.Items.FirstOrDefault(i => i.ItemId == ValeClay.QualifiedItemId) : null;
        Check("Tobin's shop lists Potter's Clay, year-round", entry != null && string.IsNullOrEmpty(entry.Condition), entry == null ? "not listed" : $"{entry.Price}g, condition '{entry.Condition}'");
        Check("a lump at a time (no stack or stock limit)", entry != null && entry.MinStack <= 1 && entry.MaxStack <= 1 && entry.AvailableStock < 0, entry == null ? "" : $"stack {entry.MinStack}-{entry.MaxStack}, stock {entry.AvailableStock}");
        bool opened = Utility.TryOpenShopMenu(shopId, "None", playOpenSound: false) && Game1.activeClickableMenu is ShopMenu;
        var forSale = (Game1.activeClickableMenu as ShopMenu)?.forSale.Select(i => i.QualifiedItemId).ToList() ?? new List<string>();
        Check("the open shop sells it", opened && forSale.Contains(ValeClay.QualifiedItemId), string.Join(", ", forSale));
        Game1.exitActiveMenu();
    }

    // ---- The pit ----

    private static void CheckPit()
    {
        if (Game1.getLocationFromName(ValeBus.Vale) is not { } vale)
        {
            Check("the Gilded Vale exists", false);
            return;
        }
        var who = Game1.player;
        int pitTiles = 0;
        for (int y = 20; y < 30; y++)
            for (int x = 48; x < 62; x++)
                if (ValeClay.IsPit(vale, new Point(x, y)))
                    pitTiles++;
        Check("the clay bank's pit is marked on the map (18 tiles)", pitTiles == 18, pitTiles.ToString());
        Check("the pit's face is solid, with sand to stand on in front of it", !vale.isTilePassable(new xTile.Dimensions.Location(PitTile.X, PitTile.Y), Game1.viewport)
            && vale.isTilePassable(new xTile.Dimensions.Location(PitTile.X, PitTile.Y - 1), Game1.viewport));

        // put back afterwards
        var season = Game1.season;
        bool rainYesterday = Game1.wasRainingYesterday;
        float stamina = who.Stamina;
        string? dugBefore = vale.modData.TryGetValue(DugKey, out string? d) ? d : null;
        var debrisBefore = vale.debris.ToList();
        try
        {
            if (Game1.season == Season.Winter)
                Game1.season = Season.Spring;
            Game1.wasRainingYesterday = false;
            vale.modData.Remove(DugKey);
            int limit = ValeClay.DigsToday(vale);
            Check($"a dry day's digs: {ValeClay.Digs}", vale.IsRainingHere() || limit == ValeClay.Digs, limit.ToString());
            Check("a fresh pit has the day's digs left", ValeClay.DigsLeft(vale) == limit, ValeClay.DigsLeft(vale).ToString());

            // a real swing of the hoe at the pit, from the sand above it
            var hoe = new Hoe();
            who.Stamina = 100f;
            int clayBefore = DebrisClay(vale);
            hoe.DoFunction(vale, PitTile.X * 64 + 32, PitTile.Y * 64 + 32, 1, who);
            Game1.exitActiveMenu();
            int clayDug = DebrisClay(vale) - clayBefore;
            Check("a dig turns up 1 or 2 lumps of Potter's Clay", clayDug is 1 or 2, clayDug.ToString());
            Check($"a dig costs {ValeClay.DigStamina} energy", Math.Abs(100f - who.Stamina - ValeClay.DigStamina) < 0.01f, $"{100f - who.Stamina}");
            Check("a dig counts down the day's digs", ValeClay.DigsLeft(vale) == limit - 1, ValeClay.DigsLeft(vale).ToString());
            Check("the digs are kept in the location's modData (shared in co-op)", vale.modData.TryGetValue(DugKey, out string? kept) && kept == $"{Game1.Date.TotalDays}:1", kept ?? "none");
            Check("the pit isn't tilled", !vale.terrainFeatures.ContainsKey(PitTile.ToVector2()));

            // dig it out
            for (int k = 1; k < limit; k++)
            {
                hoe.DoFunction(vale, PitTile.X * 64 + 32, PitTile.Y * 64 + 32, 1, who);
                Game1.exitActiveMenu();
            }
            Check("the day's digs can all be used", ValeClay.DigsLeft(vale) == 0, ValeClay.DigsLeft(vale).ToString());
            int clayAtLimit = DebrisClay(vale);
            float staminaAtLimit = who.Stamina;
            var outcome = ValeClay.Dig(vale, who, PitTile, hoe);
            string said = (Game1.activeClickableMenu as DialogueBox)?.getCurrentString() ?? "";
            Game1.exitActiveMenu();
            Check("past the limit, a dig turns up nothing and costs nothing", outcome == ValeClay.Outcome.DugOut && DebrisClay(vale) == clayAtLimit && who.Stamina == staminaAtLimit, outcome.ToString());
            Check("... and says the pit's dug out", said.Contains("dug out"), said);
            Check("the limit holds (the count stays at the limit)", ValeClay.Dug(vale) == limit, ValeClay.Dug(vale).ToString());

            // a new day refills it
            vale.modData[DugKey] = $"{Game1.Date.TotalDays - 1}:{limit}";
            Check("the next day the pit is refilled", ValeClay.DigsLeft(vale) == limit, ValeClay.DigsLeft(vale).ToString());
            Game1.wasRainingYesterday = true;
            Check($"after rain, more digs ({ValeClay.RainDigs})", ValeClay.DigsToday(vale) == ValeClay.RainDigs && ValeClay.DigsLeft(vale) == ValeClay.RainDigs, ValeClay.DigsToday(vale).ToString());
            Game1.wasRainingYesterday = false;

            // winter: frozen
            vale.modData.Remove(DugKey);
            Game1.season = Season.Winter;
            int clayWinter = DebrisClay(vale);
            float staminaWinter = who.Stamina;
            hoe.DoFunction(vale, PitTile.X * 64 + 32, PitTile.Y * 64 + 32, 1, who);
            said = (Game1.activeClickableMenu as DialogueBox)?.getCurrentString() ?? "";
            Game1.exitActiveMenu();
            Check("in winter the pit is frozen: no clay, no energy, no dig counted", DebrisClay(vale) == clayWinter && who.Stamina == staminaWinter && ValeClay.Dug(vale) == 0);
            Check("... and a message says so", said.Contains("frozen"), said);
            Game1.season = season == Season.Winter ? Season.Spring : season;

            // what a dig turns up, over many digs
            var random = new Random(4);
            var finds = Enumerable.Range(0, 4000).Select(_ => ValeClay.Finds(random)).ToList();
            Check("every dig gives 1 or 2 lumps", finds.All(f => f[0].QualifiedItemId == ValeClay.QualifiedItemId && f[0].Stack is 1 or 2));
            double perDig = finds.Average(f => f[0].Stack);
            Check("about 1.4 lumps a dig", perDig is > 1.3 and < 1.5, perDig.ToString("0.00"));
            var extras = finds.Where(f => f.Count > 1).Select(f => f[1]).ToList();
            int pebbles = extras.Count(i => i.QualifiedItemId == ValeClay.Pebble), shards = extras.Count(i => i.QualifiedItemId == ValeClay.Shard), artifacts = extras.Count(i => ValeClay.Artifacts.Contains(i.QualifiedItemId));
            Check("now and then a pebble, an old shard or a buried artifact", pebbles > 300 && shards > 100 && artifacts > 30 && pebbles + shards + artifacts == extras.Count, $"of 4000 digs: {pebbles} pebbles, {shards} shards, {artifacts} artifacts");
            var names = new[] { ValeClay.Pebble, ValeClay.Shard }.Concat(ValeClay.Artifacts).Select(id => ItemRegistry.Create(id)).ToList();
            Check("the extras are the game's Stone, Chipped Amphora and artifacts", names[0].Name == "Stone" && names[1].Name == "Chipped Amphora" && names.Skip(1).All(i => i is StardewValley.Object { Type: "Arch" }),
                string.Join(", ", names.Select(i => $"{i.QualifiedItemId} {i.Name} ({(i as StardewValley.Object)?.Type})")));

            // right-click: Tobin's note
            string? action = vale.doesTileHaveProperty(PitTile.X, PitTile.Y, "Action", "Buildings");
            vale.performAction(action, who, new xTile.Dimensions.Location(PitTile.X, PitTile.Y));
            said = (Game1.activeClickableMenu as DialogueBox)?.getCurrentString() ?? "";
            Game1.exitActiveMenu();
            Check("right-clicking the pit shows Tobin's note and the digs left", said.Contains("Take what you need. Fill the holes.") && said.Contains(ValeClay.DigsLeft(vale).ToString()), said);
        }
        finally
        {
            Game1.season = season;
            Game1.wasRainingYesterday = rainYesterday;
            who.Stamina = stamina;
            if (dugBefore is null) vale.modData.Remove(DugKey); else vale.modData[DugKey] = dugBefore;
            vale.debris.RemoveWhere(x => !debrisBefore.Contains(x));
        }
    }

    private static int DebrisClay(GameLocation location) =>
        location.debris.Where(d => d.item?.QualifiedItemId == ValeClay.QualifiedItemId).Sum(d => d.item.Stack);

    // ---- The teaching wheel ----

    private static void CheckWheel()
    {
        if (Game1.getLocationFromName(Pottery) is not { } pottery)
        {
            Check("the pottery exists", false);
            return;
        }
        var who = Game1.player;
        foreach (var t in WheelTiles)
        {
            string? a = pottery.doesTileHaveProperty(t.X, t.Y, "Action", "Buildings");
            Check($"the teaching wheel ({t.X}, {t.Y}) throws", a == ValeClay.WheelAction, a ?? "no action");
        }
        Check("the wheel's open to anyone until Tobin's 2-heart event", ValeClay.WheelOpenTo(who));
        Check("the shapes' clay: cup and bowl 1 lump, vase and jug 2",
            ValeClay.LumpsFor("cup") == 1 && ValeClay.LumpsFor("bowl") == 1 && ValeClay.LumpsFor("vase") == 2 && ValeClay.LumpsFor("jug") == 2);

        int had = who.Items.CountId(ValeClay.QualifiedItemId);
        var location = Game1.currentLocation;
        try
        {
            who.Items.ReduceId(ValeClay.QualifiedItemId, had);

            // no clay: a message, no question, no wheel
            pottery.performAction(ValeClay.WheelAction, who, new xTile.Dimensions.Location(17, 19));
            var box = Game1.activeClickableMenu as DialogueBox;
            Check("with no clay, the wheel gives a message, not a menu", box != null && !box.isQuestion && box.getCurrentString().Contains("Potter's clay"), box?.getCurrentString() ?? Game1.activeClickableMenu?.GetType().Name ?? "nothing");
            Game1.exitActiveMenu();

            // a lump: the question, with every shape
            who.addItemToInventory(ItemRegistry.Create(ValeClay.QualifiedItemId, 1));
            pottery.performAction(ValeClay.WheelAction, who, new xTile.Dimensions.Location(17, 19));
            box = Game1.activeClickableMenu as DialogueBox;
            Check("with clay, the wheel asks what to throw (four shapes and Not now)", box is { isQuestion: true } && box.responses?.Length == 5,
                box == null ? "no question" : string.Join(" | ", box.responses?.Select(r => r.responseText) ?? Array.Empty<string>()));
            Game1.exitActiveMenu();

            // a vase on one lump: a message, no wheel, the lump kept
            bool threw = ValeClay.Throw(who, "vase");
            Check("a vase on one lump: no wheel", !threw && Game1.activeClickableMenu is not PotteryMenu, Game1.activeClickableMenu?.GetType().Name ?? "nothing");
            Check("... a message says why", (Game1.activeClickableMenu as DialogueBox)?.getCurrentString().Contains("2 lumps") == true, (Game1.activeClickableMenu as DialogueBox)?.getCurrentString() ?? "");
            Check("... and the lump is kept", who.Items.CountId(ValeClay.QualifiedItemId) == 1);
            Game1.exitActiveMenu();

            // a cup: the lump goes, the wheel opens
            threw = ValeClay.Throw(who, "cup");
            Check("a cup takes the lump and opens the wheel", threw && Game1.activeClickableMenu is PotteryMenu && who.Items.CountId(ValeClay.QualifiedItemId) == 0, Game1.activeClickableMenu?.GetType().Name ?? "nothing");
            Game1.activeClickableMenu?.exitThisMenuNoSound();
            Game1.exitActiveMenu();

            // a jug: two lumps of three
            who.addItemToInventory(ItemRegistry.Create(ValeClay.QualifiedItemId, 3));
            threw = ValeClay.Throw(who, "jug");
            Check("a jug takes two lumps and opens the wheel", threw && Game1.activeClickableMenu is PotteryMenu && who.Items.CountId(ValeClay.QualifiedItemId) == 1, $"{who.Items.CountId(ValeClay.QualifiedItemId)} left");
            // the kiln at a wheel burns fuel from your bag: one per feed, and none left refuses
            if (Game1.activeClickableMenu is PotteryMenu wheel && wheel.TakeFuel != null && wheel.FuelStock != null)
            {
                who.Items.ReduceId("(O)388", who.Items.CountId("(O)388"));
                who.addItemToInventory(ItemRegistry.Create("(O)388", 2));
                bool first = wheel.TakeFuel(Fuel.Wood), second = wheel.TakeFuel(Fuel.Wood), third = wheel.TakeFuel(Fuel.Wood);
                Check("the wheel's kiln burns Wood from your bag, one a feed, and refuses with none left",
                    first && second && !third && who.Items.CountId("(O)388") == 0 && wheel.FuelStock(Fuel.Wood) == 0, $"{first}/{second}/{third}");
            }
            else
                Check("the wheel's kiln burns Wood from your bag, one a feed, and refuses with none left", false, "no fuel hooks on the wheel");
            Game1.activeClickableMenu?.exitThisMenuNoSound();
            Game1.exitActiveMenu();

            // field clay isn't potter's clay
            who.Items.ReduceId(ValeClay.QualifiedItemId, 99);
            bool hadField = who.Items.ContainsId("(O)330");
            if (!hadField)
                who.addItemToInventory(ItemRegistry.Create("(O)330", 1));
            pottery.performAction(ValeClay.WheelAction, who, new xTile.Dimensions.Location(17, 19));
            string said = (Game1.activeClickableMenu as DialogueBox)?.getCurrentString() ?? "";
            Check("with only the game's Clay, the wheel's note says it's grit and roots", said.Contains("grit and roots") && Game1.activeClickableMenu is not PotteryMenu, said);
            Game1.exitActiveMenu();
            if (!hadField)
                who.Items.ReduceId("(O)330", 1);
        }
        finally
        {
            Game1.exitActiveMenu();
            who.Items.ReduceId(ValeClay.QualifiedItemId, 999);
            if (had > 0)
                who.addItemToInventory(ItemRegistry.Create(ValeClay.QualifiedItemId, had));
        }
    }
}
#endif
