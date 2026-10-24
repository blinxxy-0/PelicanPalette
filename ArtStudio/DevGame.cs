#if DEBUG
using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace ArtStudio;

// Debug-only helpers for testing in the world without touching real saves:
// a throwaway farm that is never written to disk, and a scene with the
// easel and a framed painting set up in the farmhouse.
internal static class DevGame
{
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        // SpaceCore loads skill XP from the save folder, which a never-saved
        // farm doesn't have; give it an empty XP table instead, right after
        // its own save-loaded handler gives up (and before the day starts).
        helper.Events.GameLoop.SaveLoaded += EnsureSkillTable;

        DevTools.AddCommand(helper, "museart_devgame", "Starts a throwaway new farm from the title screen. It is never saved (don't sleep in it).", (_, _) => NewGame(monitor));
        DevTools.AddCommand(helper, "museart_devscene", "Sets up the farmhouse with an easel, a framed painting and supplies, at Painting level 6.", (_, _) => Scene(monitor));
        DevTools.AddCommand(helper, "museart_selftest", "Runs a painting through the real easel flow and checks canvases, XP, brush wear and selling. Then run 'museart_selftest sell' and 'museart_selftest sold', a second apart.", (_, args) => SelfTest(monitor, args));
        DevTools.AddCommand(helper, "museart_export_map", "Writes game maps and their tilesheets to the mod's exports folder. Usage: museart_export_map Forest LeahHouse ...", (_, args) => ExportMaps(helper, monitor, args));
        DevTools.AddCommand(helper, "museart_warp", "Warps to the art shop. Usage: museart_warp shop|outside", (_, args) => Warp(args));
        DevTools.AddCommand(helper, "museart_shoptest", "Checks the art shop: location, door, counter, stock, premium and commission deliveries. Run 'museart_shoptest premium' a second later to finish.", (_, args) => ShopTest(helper, monitor, args));
        DevTools.AddCommand(helper, "museart_export_tex", "Writes game textures to the mod's exports folder. Usage: museart_export_tex Portraits/Leah Characters/Leah ...", (_, args) =>
        {
            string dir = System.IO.Path.Combine(helper.DirectoryPath, "exports");
            System.IO.Directory.CreateDirectory(dir);
            foreach (string asset in args)
            {
                var texture = Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(asset);
                using var stream = System.IO.File.Create(System.IO.Path.Combine(dir, asset.Replace('/', '_') + ".png"));
                texture.SaveAsPng(stream, texture.Width, texture.Height);
                monitor.Log($"Exported {asset} ({texture.Width}x{texture.Height}).", LogLevel.Info);
            }
        });
        DevTools.AddCommand(helper, "museart_export_data", "Writes string-dictionary game data (schedules, dialogue) to the mod's exports folder as JSON. Usage: museart_export_data Characters/schedules/Leah ...", (_, args) =>
        {
            string dir = System.IO.Path.Combine(helper.DirectoryPath, "exports");
            System.IO.Directory.CreateDirectory(dir);
            foreach (string asset in args)
            {
                var data = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>(asset);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, asset.Replace('/', '_') + ".json"),
                    System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                monitor.Log($"Exported {asset} ({data.Count} entries).", LogLevel.Info);
            }
        });
        DevTools.AddCommand(helper, "museart_npctest", "Checks Sable: data, sprites, gift tastes, and that every schedule entry finds its routes.", (_, _) => NpcTest(monitor));
        DevTools.AddCommand(helper, "museart_where", "Logs where Sable is and what they're doing.", (_, _) =>
        {
            var sable = Game1.getCharacterFromName("Sable");
            if (sable == null) return;
            monitor.Log($"{Game1.timeOfDay}: Sable in {sable.currentLocation?.Name} at {sable.TilePoint}, facing {sable.FacingDirection}, "
                + $"{(sable.isMoving() ? "walking" : "still")}, schedule '{sable.ScheduleKey}'", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_day", "Early morning only: makes today another day of the month and reloads Sable's schedule. Usage: museart_day 3", (_, args) =>
        {
            var sable = Game1.getCharacterFromName("Sable");
            if (sable == null || args.Length == 0 || !int.TryParse(args[0], out int day)) return;
            Game1.dayOfMonth = day;
            Game1.warpCharacter(sable, sable.DefaultMap, sable.DefaultPosition / 64f);
            sable.TryLoadSchedule();
            monitor.Log($"Now {Game1.shortDayNameFromDayOfSeason(day)} {day}; Sable's schedule '{sable.ScheduleKey}'.", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_festspot", "Finds the nearest free, walkable tile to a festival spot. Usage: museart_festspot Town-Fair 19 64", (_, args) => FestSpot(monitor, args));
        DevTools.AddCommand(helper, "museart_festival", "Jumps a throwaway farm to a festival and walks in, then optionally starts its main event (main), or the Flower Dance with Sable (dance). Usage: museart_festival spring24 dance", (_, args) => Festival(monitor, args));
        DevTools.AddCommand(helper, "museart_advance", "Clicks through the open dialogue box (to run events unattended). Usage: museart_advance [times]", (_, args) =>
        {
            int times = args.Length > 0 && int.TryParse(args[0], out int n) ? n : 1;
            for (int i = 0; i < times && Game1.activeClickableMenu is DialogueBox box; i++)
            {
                box.finishTyping();
                box.receiveLeftClick(0, 0, true);
            }
            monitor.Log($"{Game1.timeOfDay}: event {Game1.CurrentEvent?.id ?? "none"}, menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}; "
                + $"canvases {Game1.player.Items.CountId("(O)" + ModEntry.Id + "_Canvas_Small")}, rose {Game1.player.Items.CountId("(O)" + ModEntry.Id + "_Pigment_rose")}, "
                + $"met Sable {Game1.player.friendshipData.ContainsKey("Sable")}", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_menu", "Opens a game menu for screenshots. Usage: museart_menu skills|easel|inventory", (_, args) => OpenMenu(args));
    }

    [EventPriority(EventPriority.Low)]
    private static void EnsureSkillTable(object? sender, SaveLoadedEventArgs e)
    {
        if (Constants.CurrentSavePath != null) return;
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        object? state = typeof(SpaceCore.Skills).GetProperty("State", flags)?.GetValue(null);
        var exp = state?.GetType().GetField("Exp");
        if (state != null && exp != null && exp.GetValue(state) == null)
            exp.SetValue(state, new System.Collections.Generic.Dictionary<long, System.Collections.Generic.Dictionary<string, int>>());
    }

    // The same steps the title screen takes for a new character with the
    // intro skipped, except the new day doesn't save.
    private static void NewGame(IMonitor monitor)
    {
        if (Context.IsWorldReady)
        {
            monitor.Log("Already in a game.", LogLevel.Warn);
            return;
        }
        // Keep running in the background (the test copy sits behind whatever
        // the user is doing). Only in memory: the preferences file isn't saved.
        Game1.options.pauseWhenOutOfFocus = false;
        Game1.player.Name = "Tester";
        Game1.player.displayName = "Tester";
        Game1.player.farmName.Value = "Devfarm";
        Game1.player.favoriteThing.Value = "Painting";
        Game1.whichFarm = 0;
        Game1.game1.loadForNewGame();
        Game1.saveOnNewDay = false;
        Game1.player.eventsSeen.Add("60367");
        Game1.player.currentLocation = Utility.getHomeOfFarmer(Game1.player);
        Game1.player.Position = new Vector2(9f, 9f) * 64f;
        Game1.player.isInBed.Value = true;
        Game1.NewDay(0f);
        Game1.exitActiveMenu();
        Game1.setGameMode(3);
        monitor.Log("Throwaway farm started (not saved).", LogLevel.Info);
    }

    private static void Scene(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        ArtProgress.SetLevel(who, 6);
        ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length));

        var house = Utility.getHomeOfFarmer(who);
        if (who.currentLocation != house)
            Game1.warpFarmer(house.NameOrUniqueName, 6, 6, 2);

        // An easel on the first free floor tile near the middle of the room.
        var easelTile = Vector2.Zero;
        var easel = ItemRegistry.Create<SObject>(ArtItems.QualifiedEaselId);
        foreach (var tile in new[] { new Vector2(5, 7), new Vector2(4, 7), new Vector2(7, 7), new Vector2(5, 8), new Vector2(3, 6) })
            if (house.CanItemBePlacedHere(tile) && easel.placementAction(house, (int)tile.X * 64, (int)tile.Y * 64, who))
            {
                easelTile = tile;
                break;
            }

        // A finished painting, framed, on the first free bit of wall.
        var data = new PaintingData
        {
            Size = 20,
            Cells = ReferenceBuilder.StillLife(new[] { "(O)24" }, 20, ArtProgress.OwnedPigments(who)).Cells,
            Purity = new Purity[20, 20],
            Subject = "(O)24",
            Stars = 5f,
            Painter = who.Name,
            Date = "Spring 1, Year 1",
        };
        var painting = PaintingItems.Create(data, PaintingRules.Gold, 300);
        bool hung = false;
        for (int y = 0; y < 6 && !hung; y++)
        for (int x = 1; x < 12 && !hung; x++)
        {
            var framed = ArtHooks.Frame(painting);
            hung = framed.canBePlacedHere(house, new Vector2(x, y)) && framed.placementAction(house, x * 64, y * 64, who);
        }

        who.addItemToInventory(painting);
        who.addItemToInventory(ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Small), 5));
        who.addItemToInventory(ItemRegistry.Create("(O)24", 3));
        who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.FrameItemId, 2));
        who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.BrushId(Brushes.All[1])));
        who.Position = new Vector2(easelTile.X, easelTile.Y + 1) * 64f;
        who.FacingDirection = 0;
        monitor.Log($"Scene ready: easel at {easelTile}, framed painting {(hung ? "hung" : "NOT hung (no free wall)")}.", LogLevel.Info);
    }

    private static int _xpBeforeSale;

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static void SelfTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        string canvasId = ArtItems.QualifiedCanvasId(CanvasKind.Small);
        int Count(string id) => who.Items.Where(i => i?.QualifiedItemId == id).Sum(i => i.Stack);

        if (args.FirstOrDefault() == "sell")
        {
            // Pierre buys paintings; sell one by taking it out of the bag while his shop is open.
            var forSale = who.Items.FirstOrDefault(PaintingItems.IsPainting);
            if (forSale != null && Utility.TryOpenShopMenu("SeedShop", "Pierre") && Game1.activeClickableMenu is ShopMenu shop)
            {
                Check(monitor, "Pierre's shop accepts paintings", shop.highlightItemToSell(forSale));
                _xpBeforeSale = ArtProgress.Xp(who);
                who.removeItemFromInventory(forSale);
            }
            else
                Check(monitor, "Pierre's shop opens with a painting in the bag", false);
            return;
        }

        if (args.FirstOrDefault() == "sold")
        {
            int xpNow = ArtProgress.Xp(who);
            Check(monitor, "selling a painting at Pierre's gives Painting XP", xpNow > _xpBeforeSale, $"{_xpBeforeSale} -> {xpNow}");
            Game1.exitActiveMenu();
            return;
        }

        // 0. Recipes make what they say, and pigment jars join the palette.
        foreach (var (key, raw) in DataLoader.CraftingRecipes(Game1.content).Where(r => r.Key.StartsWith(ModEntry.Id)))
        {
            var made = new CraftingRecipe(key).createItem();
            Check(monitor, $"recipe {key} crafts its item", made != null && made.ItemId == raw.Split('/')[2], made?.QualifiedItemId ?? "null");
        }
        Check(monitor, "the easel and small canvas recipes are known", ArtItems.StarterRecipes.All(r => who.craftingRecipes.ContainsKey(r)));
        int rose = Palette.IndexOf("rose");
        ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter));
        who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.PigmentId(rose)));
        ArtHooks.OpenEasel(who);
        Check(monitor, "opening the easel adds pigment jars to the palette", ArtProgress.Owns(who, rose)
            && !who.Items.Any(i => ArtItems.PigmentFor(i) == rose));
        Game1.exitActiveMenu();

        // 1. Paint through the real easel flow.
        int canvasesBefore = Count(canvasId), xpBefore = ArtProgress.Xp(who);
        var brushItem = who.Items.FirstOrDefault(i => ArtItems.BrushFor(i) != null);
        int usesBefore = brushItem == null ? -1 : ArtHooks.UsesLeft(brushItem, ArtItems.BrushFor(brushItem)!);
        var menu = new Menus.EaselMenu(who);
        Game1.activeClickableMenu = menu;
        menu.DevBeginFromSetup("(O)24");
        Check(monitor, "Begin starts painting", menu.DevIsPainting);
        Check(monitor, "Begin uses up one small canvas", Count(canvasId) == canvasesBefore - 1, $"{canvasesBefore} -> {Count(canvasId)}");
        menu.DevPaintPartly(1f, 7);
        menu.DevFinish(false);
        var painting = menu.DevPainting;
        Check(monitor, "finishing makes a painting", painting != null, painting == null ? "" : $"{painting.DisplayName}, quality {painting.Quality}, sells for {painting.sellToStorePrice()}g");
        Check(monitor, "finishing gives Painting XP", ArtProgress.Xp(who) > xpBefore, $"{xpBefore} -> {ArtProgress.Xp(who)}");
        if (brushItem != null)
        {
            int usesAfter = ArtHooks.UsesLeft(brushItem, ArtItems.BrushFor(brushItem)!);
            Check(monitor, "the brush wears by one painting", usesAfter == usesBefore - 1, $"{usesBefore} -> {usesAfter}");
        }
        menu.DevKeep();
        Check(monitor, "the painting goes into the bag", painting != null && who.Items.Contains(painting));
        Check(monitor, "the painting keeps its pixels after leaving the easel", painting != null && PaintingData.Read(painting)?.Size == 20);

        // 2. Gift tastes follow the rules.
        var pierre = Game1.getCharacterFromName("Pierre");
        var leah = Game1.getCharacterFromName("Leah");
        if (painting != null && pierre != null && leah != null)
        {
            int quality = painting.Quality;
            painting.Quality = PaintingRules.Normal;
            Check(monitor, "normal painting: neutral for Pierre", pierre.getGiftTasteForThisItem(painting) == NPC.gift_taste_neutral);
            Check(monitor, "normal painting: liked by Leah (art lover)", leah.getGiftTasteForThisItem(painting) == NPC.gift_taste_like);
            painting.Quality = PaintingRules.Gold;
            Check(monitor, "gold painting: liked by Pierre", pierre.getGiftTasteForThisItem(painting) == NPC.gift_taste_like);
            painting.Quality = PaintingRules.Iridium;
            Check(monitor, "iridium painting: loved by Pierre", pierre.getGiftTasteForThisItem(painting) == NPC.gift_taste_love);
            painting.Quality = quality;
        }

        monitor.Log("Now run 'museart_selftest sell', then 'museart_selftest sold'.", LogLevel.Info);
    }

    // Maps as TMX (CSV layers, easy to read from scripts) plus each tilesheet
    // as a PNG, for planning map edits against the real layout.
    private static void ExportMaps(IModHelper helper, IMonitor monitor, string[] names)
    {
        string dir = System.IO.Path.Combine(helper.DirectoryPath, "exports");
        System.IO.Directory.CreateDirectory(dir);
        var format = new TMXTile.TMXFormat(Game1.tileSize / 4, Game1.tileSize / 4, Game1.pixelZoom, Game1.pixelZoom);
        foreach (string name in names)
        {
            var map = Game1.content.Load<xTile.Map>("Maps/" + name);
            format.Store(map, System.IO.Path.Combine(dir, name + ".tmx"), TMXTile.DataEncodingType.CSV);
            foreach (var sheet in map.TileSheets)
            {
                string png = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(sheet.ImageSource) + ".png");
                if (System.IO.File.Exists(png)) continue;
                var texture = Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(sheet.ImageSource);
                using var stream = System.IO.File.Create(png);
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
            monitor.Log($"Exported {name} ({map.Layers[0].LayerWidth}x{map.Layers[0].LayerHeight}, sheets: {string.Join(", ", map.TileSheets.Select(t => t.Id + "=" + t.ImageSource))}).", LogLevel.Info);
        }
    }

    private static void Warp(string[] args)
    {
        if (!Context.IsWorldReady) return;
        if (args.FirstOrDefault() == "outside")
            Game1.warpFarmer("Forest", 82, 36, 0);
        else
            Game1.warpFarmer(Shop.ArtShop.LocationName, 7, 7, 0);
    }

    private static void ShopTest(IModHelper helper, IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;

        if (args.FirstOrDefault() == "premium")
        {
            bool premium = Game1.activeClickableMenu is ShopMenu shopMenu
                && Math.Abs(helper.Reflection.GetField<float>(shopMenu, "sellPercentage").GetValue() - Shop.ArtShop.PaintingPremium) < 0.001f;
            Check(monitor, "the art shop pays the painting premium", premium);
            Game1.exitActiveMenu();
            return;
        }

        var shop = Game1.getLocationFromName(Shop.ArtShop.LocationName);
        Check(monitor, "the art shop location exists", shop != null);
        var forest = Game1.getLocationFromName("Forest");
        string? door = forest?.doesTileHaveProperty(82, 33, "Action", "Buildings");
        Check(monitor, "the Forest has the shop's door", door?.StartsWith("LockedDoorWarp 7 9 " + Shop.ArtShop.LocationName) == true, door ?? "none");
        Check(monitor, "the building blocks movement", forest != null && !forest.isTilePassable(new xTile.Dimensions.Location(79, 32), Game1.viewport));
        Check(monitor, "the yard fence blocks movement", forest != null && !forest.isTilePassable(new xTile.Dimensions.Location(78, 37), Game1.viewport));
        Check(monitor, "the yard gate is open", forest != null && forest.isTilePassable(new xTile.Dimensions.Location(82, 37), Game1.viewport));
        Check(monitor, "the signpost has its action", forest?.doesTileHaveProperty(87, 33, "Action", "Buildings") == ModEntry.Id + "_Sign");
        Check(monitor, "the shop's counter has its action", shop?.doesTileHaveProperty(3, 5, "Action", "Buildings") == ModEntry.Id + "_Counter");
        Check(monitor, "the commission board has its action", shop?.doesTileHaveProperty(9, 3, "Action", "Buildings") == ModEntry.Id + "_Board");
        Check(monitor, "the shop's exit leads back to the Forest", shop?.warps.Any(w => w.TargetName == "Forest" && w.TargetX == 82 && w.TargetY == 34) == true);

        var stock = DataLoader.Shops(Game1.content).TryGetValue(Shop.ArtShop.ShopId, out var data) ? data : null;
        Check(monitor, "the shop has stock", stock?.Items?.Count > 10, $"{stock?.Items?.Count ?? 0} entries");

        // A fresh farm hasn't met anyone; commissions only come from people you know.
        foreach (var villager in Utility.getAllVillagers().Where(n => n.CanSocialize))
            if (!who.friendshipData.ContainsKey(villager.Name))
                who.friendshipData.Add(villager.Name, new Friendship(0));

        // Commissions: deliver a matching painting, and have a weak one turned down.
        var pierre = Game1.getCharacterFromName("Pierre");
        if (pierre != null)
        {
            var commission = new Shop.Commission { Npc = "Pierre", ItemId = "(O)24", MinQuality = PaintingRules.Silver, Reward = 333, ExpiresOn = Game1.Date.TotalDays + 5, Accepted = true };
            Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission> { commission });

            var cells = ReferenceBuilder.StillLife(new[] { "(O)24" }, 20, ArtProgress.OwnedPigments(who)).Cells;
            SObject Painting(int quality) => PaintingItems.Create(new PaintingData { Size = 20, Cells = cells, Purity = new Purity[20, 20], Subject = "(O)24" }, quality, 200);

            var weak = Painting(PaintingRules.Normal);
            who.addItemToInventory(weak);
            who.CurrentToolIndex = who.Items.IndexOf(weak);
            pierre.tryToReceiveActiveObject(who);
            Check(monitor, "a painting below the asked quality is turned down", who.Items.Contains(weak) && Shop.Commissions.Accepted(who).Any());
            CloseDialogue();
            who.removeItemFromInventory(weak);

            var good = Painting(PaintingRules.Gold);
            who.addItemToInventory(good);
            who.CurrentToolIndex = who.Items.IndexOf(good);
            int money = who.Money, points = who.getFriendshipLevelForNPC("Pierre");
            pierre.tryToReceiveActiveObject(who);
            Check(monitor, "delivering pays the reward", who.Money == money + 333, $"{money} -> {who.Money}");
            Check(monitor, "delivering raises friendship", who.getFriendshipLevelForNPC("Pierre") > points);
            Check(monitor, "the painting is handed over and the commission closed", !who.Items.Contains(good) && !Shop.Commissions.Accepted(who).Any());
            CloseDialogue();
        }

        // Fill the board as a morning would, then open the shop for the premium check.
        Shop.Commissions.NewDay(who, monitor, new Random(3));
        Check(monitor, "the board fills with postings", Shop.Commissions.Posted(who).Count() == Shop.Commissions.MaxPosted,
            string.Join("; ", Shop.Commissions.Posted(who).Select(c => $"{c.Npc}: {c.ItemName} ({Shop.Commissions.QualityText(c.MinQuality)}) {c.Reward}g")));
        Utility.TryOpenShopMenu(Shop.ArtShop.ShopId, "AnyOrNone");
        monitor.Log("Now run 'museart_shoptest premium'.", LogLevel.Info);
    }

    // Closing a dialogue box by hand leaves the game thinking one is up (time
    // stops and events can't start); undo that too.
    private static void CloseDialogue()
    {
        Game1.exitActiveMenu();
        Game1.dialogueUp = false;
        Game1.currentSpeaker = null;
        Game1.player.CanMove = true;
    }

    private static void NpcTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var sable = Game1.getCharacterFromName("Sable");
        Check(monitor, "Sable exists", sable != null);
        if (sable == null) return;
        Check(monitor, "Sable lives at the art shop", sable.DefaultMap == Shop.ArtShop.LocationName, sable.DefaultMap);
        Check(monitor, "Sable's sprite sheet is 64x384 (walking, then the Flower Dance at 40-47)", sable.Sprite.Texture.Width == 64 && sable.Sprite.Texture.Height == 384);
        Check(monitor, "Sable's portraits are 128x256", sable.Portrait?.Width == 128 && sable.Portrait?.Height == 256);
        Check(monitor, "Sable is romanceable and they/them", sable.datable.Value && sable.Gender == StardewValley.Gender.Undefined);

        int Taste(string id) => sable.getGiftTasteForThisItem(ItemRegistry.Create(id));
        Check(monitor, "Sable loves coffee and amethyst", Taste("(O)395") == NPC.gift_taste_love && Taste("(O)66") == NPC.gift_taste_love);
        Check(monitor, "Sable likes clay", Taste("(O)330") == NPC.gift_taste_like);
        Check(monitor, "Sable dislikes hay", Taste("(O)178") == NPC.gift_taste_dislike);
        Check(monitor, "Sable hates slime", Taste("(O)766") == NPC.gift_taste_hate);

        var dialogue = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Characters/Dialogue/Sable");
        Check(monitor, "Sable has an introduction", dialogue.ContainsKey("Introduction"), $"{dialogue.Count} lines");

        // Every schedule entry, leg by leg: each stop must be reachable from
        // the one before it (across locations, through their warps and doors).
        var home = sable.currentLocation;
        var homeTile = sable.TilePoint;
        var raw = sable.getMasterScheduleRawData();
        foreach (var (key, script) in raw)
        {
            if (key.StartsWith("marriage")) continue;
            var parsed = sable.parseMasterSchedule(key, script);
            string from = sable.currentLocation.Name;
            var at = sable.TilePoint;
            var problems = new System.Collections.Generic.List<string>();
            if (parsed.Count == 0 && !script.StartsWith("GOTO") && !script.StartsWith("NOT"))
                problems.Add("nothing parsed");
            foreach (var stop in parsed.OrderBy(p => p.Key).Select(p => p.Value))
            {
                var locations = from == stop.targetLocationName
                    ? new[] { from }
                    : StardewValley.Pathfinding.WarpPathfindingCache.GetLocationRoute(from, stop.targetLocationName, sable.Gender);
                if (locations == null)
                {
                    problems.Add($"{stop.time}: no way from {from} to {stop.targetLocationName}");
                }
                else
                {
                    var start = at;
                    for (int i = 0; i < locations.Length; i++)
                    {
                        var location = Game1.RequireLocation(locations[i]);
                        var end = i < locations.Length - 1 ? location.getWarpPointTo(locations[i + 1]) : stop.targetTile;
                        if (StardewValley.Pathfinding.PathFindController.findPathForNPCSchedules(start, end, location, 30000, sable) == null)
                            problems.Add($"{stop.time}: no path in {location.Name} from {start} to {end}");
                        if (i < locations.Length - 1)
                            start = location.getWarpPointTarget(end, sable);
                    }
                }
                from = stop.targetLocationName;
                at = stop.targetTile;
            }
            Check(monitor, $"schedule '{key}' is walkable", problems.Count == 0,
                problems.Count == 0 ? $"{parsed.Count} stops" : string.Join("; ", problems));
        }
        Game1.warpCharacter(sable, home, homeTile.ToVector2());
        sable.TryLoadSchedule();

        // The counter: open with Sable behind it, closed without.
        var shop = Game1.RequireLocation(Shop.ArtShop.LocationName);
        Game1.warpCharacter(sable, shop, new Vector2(3, 4));
        Check(monitor, "the shop opens with Sable at the counter", Shop.ArtShop.OpenShop(shop) && Game1.activeClickableMenu is ShopMenu);
        Game1.exitActiveMenu();
        Game1.warpCharacter(sable, shop, new Vector2(9, 8));
        Check(monitor, "the shop is closed without them", !Shop.ArtShop.OpenShop(shop) && Game1.activeClickableMenu is not ShopMenu);
        CloseDialogue();
        Game1.warpCharacter(sable, home, homeTile.ToVector2());
    }

    // A festival map's villager spots (the Set-Up and MainEvent layers).
    private static System.Collections.Generic.HashSet<Point> ActorTiles(xTile.Map map, string layerId)
    {
        var tiles = new System.Collections.Generic.HashSet<Point>();
        var layer = map.GetLayer(layerId);
        if (layer == null) return tiles;
        for (int x = 0; x < layer.LayerWidth; x++)
        for (int y = 0; y < layer.LayerHeight; y++)
            if (layer.Tiles[x, y] != null)
                tiles.Add(new Point(x, y));
        return tiles;
    }

    private static void FestSpot(IMonitor monitor, string[] args)
    {
        if (args.Length < 3 || !int.TryParse(args[1], out int px) || !int.TryParse(args[2], out int py)) return;
        var location = new GameLocation("Maps\\" + args[0], "MuseArtFestSpot");
        foreach (string layerId in new[] { "Set-Up", "MainEvent", "Main-Event" })
        {
            var actors = ActorTiles(location.map, layerId);
            if (actors.Count == 0) { monitor.Log($"{args[0]} has no {layerId} layer.", LogLevel.Info); continue; }
            bool Free(int x, int y) =>
                location.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport) && !location.isWaterTile(x, y)
                && location.map.GetLayer("Back")?.Tiles[x, y] != null
                && !actors.Any(a => Math.Abs(a.X - x) <= 1 && Math.Abs(a.Y - y) <= 1);
            Point? best = null;
            for (int r = 0; r <= 6 && best == null; r++)
            for (int dx = -r; dx <= r && best == null; dx++)
            for (int dy = -r; dy <= r && best == null; dy++)
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && Free(px + dx, py + dy))
                    best = new Point(px + dx, py + dy);
            monitor.Log($"{args[0]} {layerId}: asked {px},{py} -> {(best.HasValue ? $"{best.Value.X} {best.Value.Y}" : "nothing free nearby")}", LogLevel.Info);
        }
    }

    private static void Festival(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || args.Length == 0) return;
        string id = args[0];
        var data = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Festivals/" + id);
        string[] conditions = data["conditions"].Split('/');
        int start = Convert.ToInt32(conditions[1].Split(' ')[0]);
        string season = new string(id.TakeWhile(char.IsLetter).ToArray());
        Game1.season = Utility.TryParseEnum<Season>(season, out var parsed) ? parsed : Season.Spring;
        Game1.dayOfMonth = int.Parse(id.Substring(season.Length));
        Game1.weatherIcon = 1;
        Game1.whereIsTodaysFest = conditions[0];
        Game1.timeOfDay = start;
        foreach (var npc in Utility.getAllVillagers())
            npc.TryLoadSchedule();
        Game1.warpFarmer(conditions[0], Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 2);
        monitor.Log($"Walking into {id} at {conditions[0]} ({start}).", LogLevel.Info);
        string then = args.Length > 1 ? args[1] : "";
        DelayedAction.functionAfterDelay(() =>
        {
            var ev = Game1.CurrentEvent;
            var sable = ev?.actors.FirstOrDefault(a => a.Name == "Sable");
            monitor.Log(ev?.isFestival == true
                ? $"Festival {id} is on; Sable {(sable != null ? $"is at {sable.TilePoint}, facing {sable.FacingDirection}" : "is missing")}."
                : $"Festival {id} didn't start.", sable != null ? LogLevel.Info : LogLevel.Error);
            if (ev == null || sable == null || then == "") return;

            // 'dance': ask Sable to the Flower Dance (as the dialogue choice
            // does, at 4+ hearts); then 'main' or 'dance' has Lewis start the
            // main event, as answering him does.
            if (then == "dance")
            {
                Game1.player.friendshipData["Sable"] = new Friendship(1000);
                ev.answerDialogueQuestion(sable, "danceAsk");
                CloseDialogue();
                monitor.Log($"Dance partner: {Game1.player.dancePartner.TryGetVillager()?.Name ?? "none"}.", LogLevel.Info);
            }
            ev.answerDialogueQuestion(ev.getActorByName("Lewis"), "yes");
            DelayedAction.functionAfterDelay(() =>
            {
                var actor = Game1.CurrentEvent?.actors.FirstOrDefault(a => a.Name == "Sable");
                monitor.Log($"Main event of {id}: Sable {(actor != null ? $"is at {actor.TilePoint}, frame {actor.Sprite.currentFrame}" : "is missing")}.", actor != null ? LogLevel.Info : LogLevel.Error);
            }, 6000);
        }, 3000);
    }

    private static void OpenMenu(string[] args)
    {
        if (!Context.IsWorldReady) return;
        switch (args.FirstOrDefault())
        {
            case "skills":
                Game1.activeClickableMenu = new GameMenu(GameMenu.skillsTab);
                break;
            case "scroll":
                // SpaceCore's skills page (swapped in after the menu opens) scrolls;
                // custom skills sit below the vanilla five.
                if (Game1.activeClickableMenu is GameMenu gameMenu)
                    for (int i = 0; i < 1; i++)
                        HarmonyLib.Traverse.Create(gameMenu.GetCurrentPage()).Method("scrollSkillsDown").GetValue();
                break;
            case "inventory":
                Game1.activeClickableMenu = new GameMenu(GameMenu.inventoryTab);
                break;
            case "easel":
                ArtHooks.OpenEasel(Game1.player);
                break;
            case "mapshot":
                // The game's own whole-location screenshot, rendered off-screen
                // (saved to %AppData%/StardewValley/Screenshots).
                Game1.game1.takeMapScreenshot(args.Length > 1 && float.TryParse(args[1], out float scale) ? scale : 0.5f,
                    "museart_" + Game1.currentLocation.NameOrUniqueName, () => { });
                break;
            case "board":
                Game1.activeClickableMenu = new Shop.CommissionBoardMenu(Game1.player);
                break;
            case "useeasel":
                // The same call a right-click on the placed easel makes.
                var easel = Game1.currentLocation.objects.Values.FirstOrDefault(ArtItems.IsEasel);
                easel?.checkForAction(Game1.player);
                break;
            default:
                Game1.exitActiveMenu();
                break;
        }
    }
}
#endif
