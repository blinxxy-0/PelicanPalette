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
        Game1.exitActiveMenu();
        monitor.Log($"museart_claytest: {_pass} passed, {_fail} failed.", _fail == 0 ? LogLevel.Info : LogLevel.Error);
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
