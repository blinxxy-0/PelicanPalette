#if DEBUG
using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
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

        // Test copies run behind whatever the user is doing: keep updating when
        // unfocused, from the title screen on. Only in memory; the shared
        // preferences file is never saved from here.
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            if (Game1.options != null)
                Game1.options.pauseWhenOutOfFocus = false;
        };

        DevTools.AddCommand(helper, "museart_devgame", "Starts a throwaway new farm from the title screen. It is never saved (don't sleep in it).", (_, _) => NewGame(monitor));
        DevTools.AddCommand(helper, "museart_devscene", "Sets up the farmhouse with an easel, a framed painting and supplies, at Painting level 6.", (_, _) => Scene(monitor));
        DevTools.AddCommand(helper, "museart_selftest", "Runs a painting through the real easel flow and checks canvases, XP, brush wear and selling. Then run 'museart_selftest sell' and 'museart_selftest sold', a second apart.", (_, args) => SelfTest(monitor, args));
        DevTools.AddCommand(helper, "museart_export_map", "Writes game maps and their tilesheets to the mod's exports folder. Usage: museart_export_map Forest LeahHouse ...", (_, args) => ExportMaps(helper, monitor, args));
        DevTools.AddCommand(helper, "museart_warp", "Warps to the art shop, or anywhere. Usage: museart_warp shop|outside|<location> <x> <y>", (_, args) => Warp(args));
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
        DevTools.AddCommand(helper, "museart_festspot", "Finds the nearest free, walkable tile to a festival spot (with a festival id, counting other mods' extra villagers too). Usage: museart_festspot Town-Fair 19 64 [fall16]", (_, args) => FestSpot(monitor, args));
        DevTools.AddCommand(helper, "museart_festival", "Jumps a throwaway farm to a festival and walks in, then optionally starts its main event (main), or the Flower Dance with Sable (dance). Usage: museart_festival spring24 dance", (_, args) => Festival(monitor, args));
        DevTools.AddCommand(helper, "museart_advance", "Clicks through the open dialogue box (to run events unattended), picking answer N at questions. Usage: museart_advance [times] [answer]", (_, args) =>
        {
            int times = args.Length > 0 && int.TryParse(args[0], out int n) ? n : 1;
            int answer = args.Length > 1 && int.TryParse(args[1], out int a) ? a : 0;
            for (int i = 0; i < times && Game1.activeClickableMenu is DialogueBox box; i++)
            {
                box.finishTyping();
                bool question = box.isQuestion && box.responses?.Length > answer;
                if (question)
                {
                    monitor.Log($"Answering: {box.responses[answer].responseText}", LogLevel.Info);
                    box.selectedResponse = answer;
                }
                box.receiveLeftClick(0, 0, true);
                if (question)
                    break;                       // the answer closes the box over the next few frames
            }
            monitor.Log($"{Game1.timeOfDay}: event {Game1.CurrentEvent?.id ?? "none"}, menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}; "
                + $"canvases {Game1.player.Items.CountId("(O)" + ModEntry.Id + "_Canvas_Small")}, rose {Game1.player.Items.CountId("(O)" + ModEntry.Id + "_Pigment_rose")}, "
                + $"met Sable {Game1.player.friendshipData.ContainsKey("Sable")}", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_event", "Plays an event by ID wherever it is, ignoring its conditions. Usage: museart_event blinxxy.MuseArt_Sable2h", (_, args) =>
        {
            if (!Context.IsWorldReady || args.Length == 0) return;
            bool found = Game1.PlayEvent(args[0], checkPreconditions: false, checkSeen: false);
            monitor.Log(found ? $"Playing {args[0]}." : $"No event {args[0]}.", found ? LogLevel.Info : LogLevel.Error);
        });
        DevTools.AddCommand(helper, "museart_eventpaths", "Walks every step of Sable's events against the real maps and reports anyone walking through walls, furniture, props or each other.", (_, _) => EventPaths(monitor));
        DevTools.AddCommand(helper, "museart_route", "Prints event 'move' commands for the shortest walkable route. Usage: museart_route JoshHouse 9 23 6 19 [x y ...]", (_, args) => Route(monitor, args));
        DevTools.AddCommand(helper, "museart_tiles", "Says whether each tile can be stood on. Usage: museart_tiles JoshHouse 2 19 5 20", (_, args) => Tiles(monitor, args));
        DevTools.AddCommand(helper, "museart_portraittest", "Portraits of villagers: who'll sit, the reference, painting one, its price and time, gifting it, and portrait commissions.", (_, _) => PortraitTest(monitor));
        DevTools.AddCommand(helper, "museart_painttest", "Paint as a resource: the Palette, tubes, spending paint, and the Pigment Mill. Run 'museart_painttest', then 'museart_painttest check' a second later.", (_, args) => PaintTest(monitor, args));
        DevTools.AddCommand(helper, "museart_knowtest", "Pigments as knowledge, and painting taking time. Run 'museart_knowtest', then 'museart_knowtest close', then 'museart_knowtest check' a few seconds later.", (_, args) => KnowTest(monitor, args));
        DevTools.AddCommand(helper, "museart_dialoguefit", "Checks every page of Sable's dialogue (and their events') fits the portrait dialogue box, so the game never splits a word across boxes.", (_, _) => DialogueFit(monitor));
        DevTools.AddCommand(helper, "museart_portrait", "Makes Sable's portrait of the current farmer and saves it (and the farmer as rendered) to exports/portrait.png.", (_, _) =>
        {
            if (!Context.IsWorldReady) return;
            var painting = FarmerPortrait.Create(Game1.player);
            var data = PaintingData.Read(painting)!;
            using var texture = PaintingRenderer.Build(data.Size, data.Cells, data.Purity, new Color(0xF3, 0xE5, 0xC2));
            string dir = System.IO.Path.Combine(helper.DirectoryPath, "exports");
            System.IO.Directory.CreateDirectory(dir);
            using (var stream = System.IO.File.Create(System.IO.Path.Combine(dir, "portrait.png")))
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            var framed = ArtHooks.Frame(painting);
            monitor.Log($"Portrait: '{painting.DisplayName}', framed as '{framed.DisplayName}': {framed.getDescription().Replace("\n", " ")}", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_marry", "Throwaway farm only: marries the farmer to Sable (house upgraded), lays out the spouse room and patio, and puts Sable in their room. 'patio' puts them at the patio instead.", (_, args) => Marry(monitor, args));
        DevTools.AddCommand(helper, "museart_wedding", "Throwaway farm only: plays the wedding to Sable (as on the wedding morning).", (_, _) => Wedding(monitor));
        DevTools.AddCommand(helper, "museart_export_weddings", "Writes Data/Weddings to the exports folder as JSON.", (_, _) =>
        {
            string dir = System.IO.Path.Combine(helper.DirectoryPath, "exports");
            System.IO.Directory.CreateDirectory(dir);
            var data = DataLoader.Weddings(Game1.content);
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Data_Weddings.json"),
                System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
            monitor.Log("Exported Data/Weddings.", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_marriedtest", "Throwaway farm only: checks a married Sable still runs the shop (rainy days too) and the shop opens on trust when they're out.", (_, _) =>
        {
            if (!Context.IsWorldReady || Constants.CurrentSavePath != null) return;
            Marry(monitor, Array.Empty<string>());
            var sable = Game1.getCharacterFromName("Sable");
            string day = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
            bool wasRaining = Game1.isRaining;
            Game1.isRaining = true;
            bool loaded = sable.TryLoadSchedule();
            Check(monitor, $"married, on a rainy {day} Sable still goes to the shop", loaded && sable.ScheduleKey == "marriage_" + day, sable.ScheduleKey ?? "no schedule");
            Game1.isRaining = wasRaining;
            sable.TryLoadSchedule();
            Check(monitor, $"married, on a dry {day} Sable goes to the shop", sable.ScheduleKey == "marriage_" + day, sable.ScheduleKey ?? "no schedule");

            var shop = Game1.RequireLocation(Shop.ArtShop.LocationName);
            Check(monitor, "with Sable out, the shop opens on trust for their spouse", Shop.ArtShop.OpenShop(shop) && Game1.activeClickableMenu is ShopMenu);
            CloseDialogue();
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
        if (who.MaxItems < 36)
            who.increaseBackpackSize(36 - who.MaxItems);     // room for the test items

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
        who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.PaletteId));
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
            Game1.warpFarmer("Forest", 83, 36, 0);
        else if (args.Length == 3 && int.TryParse(args[1], out int x) && int.TryParse(args[2], out int y))
            Game1.warpFarmer(args[0], x, y, 2);
        else
            Game1.warpFarmer(Shop.ArtShop.LocationName, 9, 11, 0);
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
        string? door = forest?.doesTileHaveProperty(83, 33, "Action", "Buildings");
        Check(monitor, "the Forest has the shop's door", door?.StartsWith("LockedDoorWarp 9 13 " + Shop.ArtShop.LocationName) == true, door ?? "none");
        Check(monitor, "the building blocks movement", forest != null && !forest.isTilePassable(new xTile.Dimensions.Location(80, 32), Game1.viewport));
        Check(monitor, "the yard fence blocks movement", forest != null && !forest.isTilePassable(new xTile.Dimensions.Location(78, 37), Game1.viewport));
        Check(monitor, "the yard gate is open", forest != null && forest.isTilePassable(new xTile.Dimensions.Location(83, 37), Game1.viewport));
        Check(monitor, "the signpost has its action", forest?.doesTileHaveProperty(87, 33, "Action", "Buildings") == ModEntry.Id + "_Sign");
        Check(monitor, "the shop's counter has its action", shop?.doesTileHaveProperty(3, 5, "Action", "Buildings") == ModEntry.Id + "_Counter");
        Check(monitor, "the commission board has its action", shop?.doesTileHaveProperty(7, 3, "Action", "Buildings") == ModEntry.Id + "_Board");
        Check(monitor, "the shop's exit leads back to the Forest", shop?.warps.Any(w => w.TargetName == "Forest" && w.TargetX == 83 && w.TargetY == 34) == true);

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
            if (!who.addItemToInventoryBool(weak))
            {
                Check(monitor, "the bag has room for the test paintings", false);
                return;
            }
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
        // With a festival id, other mods' extra villagers count as taken too.
        var festival = args.Length > 3 ? Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Festivals/" + args[3]) : null;
        foreach (string layerId in new[] { "Set-Up", "MainEvent", "Main-Event" })
        {
            var actors = ActorTiles(location.map, layerId);
            if (festival != null && festival.TryGetValue(layerId + "_additionalCharacters", out string? extra))
                foreach (string entry in extra.Split('/'))
                {
                    string[] parts = entry.Trim().Split(' ');
                    if (parts.Length >= 3 && parts[0] != "Sable" && int.TryParse(parts[1], out int ax) && int.TryParse(parts[2], out int ay))
                        actors.Add(new Point(ax, ay));
                }
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

    // Replays each event's 'move' commands from the actors' starting tiles:
    // every tile a character steps onto (and every starting tile) must be
    // walkable on the map, and not under a prop or another character.
    private static void EventPaths(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        foreach (string locationName in new[] { Shop.ArtShop.LocationName, "Forest", "JoshHouse" })
        {
            var events = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Events/" + locationName);
            foreach (var (key, script) in events.Where(e => e.Key.StartsWith(ModEntry.Id)))
            {
                string id = key.Split('/')[0];
                var commands = Event.ParseCommands(script);
                // An event that moves to a temporary map before anyone walks
                // (the exhibition's gallery) is checked against that map.
                var location = Game1.getLocationFromName(locationName);
                string? temporary = commands.Skip(3).TakeWhile(c => !c.StartsWith("move "))
                    .FirstOrDefault(c => c.StartsWith("changeToTemporaryMap "))?.Split(' ')[1];
                if (temporary != null)
                    location = new GameLocation("Maps\\" + temporary, "Temp");
                var at = new System.Collections.Generic.Dictionary<string, Point>();
                var props = new System.Collections.Generic.HashSet<Point>();
                var problems = new System.Collections.Generic.List<string>();
                string[] actors = commands[2].Split(' ');
                for (int i = 0; i + 2 < actors.Length; i += 4)
                    at[actors[i]] = new Point(int.Parse(actors[i + 1]), int.Parse(actors[i + 2]));

                bool Walkable(Point p) => location.isTilePassable(new xTile.Dimensions.Location(p.X, p.Y), Game1.viewport);
                foreach (var (who, p) in at)
                    if (!Walkable(p))
                        problems.Add($"{who} starts on a blocked tile {p}");

                foreach (string command in commands.Skip(3))
                {
                    string[] args = ArgUtility.SplitBySpaceQuoteAware(command);
                    if (args[0] == "addBigProp" && args.Length > 2)
                        props.Add(new Point(int.Parse(args[1]), int.Parse(args[2])));
                    else if (args[0] == "warp" && args.Length > 3 && at.ContainsKey(args[1]))
                        at[args[1]] = new Point(int.Parse(args[2]), int.Parse(args[3]));
                    else if (args[0] == "move")
                    {
                        for (int i = 1; i + 2 < args.Length; i += 4)
                        {
                            string who = args[i];
                            if (!at.TryGetValue(who, out var p) || !int.TryParse(args[i + 1], out int dx) || !int.TryParse(args[i + 2], out int dy)) continue;
                            var step = new Point(Math.Sign(dx), 0);
                            for (int n = 0; n < Math.Abs(dx) + Math.Abs(dy); n++)
                            {
                                if (n == Math.Abs(dx)) step = new Point(0, Math.Sign(dy));
                                p = new Point(p.X + step.X, p.Y + step.Y);
                                string? blocker = !Walkable(p) ? "a wall or furniture"
                                    : props.Contains(p) ? "a prop"
                                    : at.FirstOrDefault(a => a.Key != who && a.Value == p).Key;
                                if (blocker != null)
                                    problems.Add($"{who} walks into {blocker} at {p} ('{command}')");
                            }
                            at[who] = p;
                        }
                    }
                }
                Check(monitor, $"{id}: everyone walks around walls and furniture", problems.Count == 0, string.Join("; ", problems));
            }
        }
    }

    // Breadth-first search over walkable tiles, printed as event moves (one
    // per straight run). Extra coordinate pairs are tiles to treat as taken
    // (other actors standing there).
    private static void Route(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || args.Length < 5) return;
        var location = Game1.getLocationFromName(args[0]);
        int[] n = args.Skip(1).Select(int.Parse).ToArray();
        var start = new Point(n[0], n[1]);
        var goal = new Point(n[2], n[3]);
        var taken = new System.Collections.Generic.HashSet<Point>();
        for (int i = 4; i + 1 < n.Length; i += 2)
            taken.Add(new Point(n[i], n[i + 1]));
        bool Walkable(Point p) => !taken.Contains(p) && location.isTilePassable(new xTile.Dimensions.Location(p.X, p.Y), Game1.viewport);

        var from = new System.Collections.Generic.Dictionary<Point, Point> { [start] = start };
        var queue = new System.Collections.Generic.Queue<Point>(new[] { start });
        while (queue.Count > 0 && !from.ContainsKey(goal))
        {
            var p = queue.Dequeue();
            foreach (var d in new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) })
            {
                var next = new Point(p.X + d.X, p.Y + d.Y);
                if (from.ContainsKey(next) || !Walkable(next) || Math.Abs(next.X - start.X) > 40 || Math.Abs(next.Y - start.Y) > 40) continue;
                from[next] = p;
                queue.Enqueue(next);
            }
        }
        if (!from.ContainsKey(goal) || !Walkable(goal))
        {
            monitor.Log($"No walkable route in {args[0]} from {start} to {goal}.", LogLevel.Error);
            return;
        }
        var path = new System.Collections.Generic.List<Point> { goal };
        while (path[^1] != start)
            path.Add(from[path[^1]]);
        path.Reverse();
        var moves = new System.Collections.Generic.List<string>();
        for (int i = 1; i < path.Count; )
        {
            var d = new Point(path[i].X - path[i - 1].X, path[i].Y - path[i - 1].Y);
            int run = 0;
            while (i < path.Count && path[i].X - path[i - 1].X == d.X && path[i].Y - path[i - 1].Y == d.Y) { run++; i++; }
            int facing = d.X > 0 ? 1 : d.X < 0 ? 3 : d.Y > 0 ? 2 : 0;
            moves.Add($"move farmer {d.X * run} {d.Y * run} {facing}");
        }
        monitor.Log($"Route in {args[0]} {start} -> {goal}: {string.Join("/", moves)}", LogLevel.Info);
    }

    // Whether each tile can be stood on. Usage: museart_tiles JoshHouse 2 19 5 20
    private static void Tiles(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || args.Length < 3) return;
        var location = Game1.getLocationFromName(args[0]);
        int[] n = args.Skip(1).Select(int.Parse).ToArray();
        for (int i = 0; i + 1 < n.Length; i += 2)
            monitor.Log($"{args[0]} {n[i]},{n[i + 1]}: {(location.isTilePassable(new xTile.Dimensions.Location(n[i], n[i + 1]), Game1.viewport) ? "walkable" : "blocked")}", LogLevel.Info);
    }

    // A dialogue page too tall for the box beside the portrait gets cut into a
    // second box mid-word (DialogueBox.checkDialogue). Measure each page with
    // the game's own text layout for that box (1200x384, minus the portrait).
    // Part one of the paint rework (docs/painting_depth.md): a pigment is
    // learned the moment its jar reaches the bag, the shop stops offering
    // known ones, and finishing paintings moves the clock on.
    private static void KnowTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        int sienna = Palette.IndexOf("sienna");
        if (args.FirstOrDefault() == "close")
        {
            Game1.exitActiveMenu();         // a tick after opening, so the menu change registers
            return;
        }
        if (args.FirstOrDefault() != "check")
        {
            ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter));
            Check(monitor, "the shop condition sees an unknown pigment", !GameStateQuery.CheckConditions(ModEntry.Id + "_KNOWS_PIGMENT Current sienna"));
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.PigmentId(sienna)));
            Game1.timeOfDay = 1000;
            ArtHooks.AddPaintingTime(PaintingRules.Minutes(CanvasKind.Medium));
            Game1.activeClickableMenu = new EaselMenu(who);
            return;
        }
        Check(monitor, "a pigment jar is learned on pickup", ArtProgress.Owns(who, sienna));
        Check(monitor, "the learned jar leaves the bag", !who.Items.Any(i => ArtItems.PigmentFor(i) != null));
        Check(monitor, "the shop condition sees a known pigment", GameStateQuery.CheckConditions(ModEntry.Id + "_KNOWS_PIGMENT Current sienna"));
        Check(monitor, "closing the easel after painting moves the clock on", Game1.timeOfDay == 1200, $"10:00 -> {Game1.timeOfDay}");
        Game1.timeOfDay = 2400;
        Check(monitor, "a large painting started at midnight would run too late", ArtHooks.FinishTime(PaintingRules.Minutes(CanvasKind.Large)) > PaintingRules.LatestFinish);
        Check(monitor, "a small one at midnight still fits", ArtHooks.FinishTime(PaintingRules.Minutes(CanvasKind.Small)) <= PaintingRules.LatestFinish);
        Game1.timeOfDay = 1200;
    }

    private static void PortraitTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        ModEntry.Config.Difficulty = Difficulty.Relaxed;     // portraits, not paint, are under test here
        ArtProgress.SetLevel(who, 6);
        foreach (var (npc, hearts) in new[] { ("Abigail", 5), ("Leah", 2), ("Haley", 0) })
        {
            if (!who.friendshipData.ContainsKey(npc)) who.friendshipData.Add(npc, new Friendship());
            who.friendshipData[npc].Points = hearts * 250;
        }
        var sitters = Sitters.Available(who);
        Check(monitor, "a friend at 4+ hearts will sit for a portrait", sitters.Contains("Abigail"), string.Join(", ", sitters));
        Check(monitor, "nobody under 4 hearts will", !sitters.Contains("Leah") && !sitters.Contains("Haley"));

        var all = Enumerable.Range(0, Palette.All.Length).ToList();
        var large = Sitters.Portrait("Abigail", PaintingRules.CanvasSize(CanvasKind.Large), all);
        var medium = Sitters.Portrait("Abigail", PaintingRules.CanvasSize(CanvasKind.Medium), all);
        int painted = large.Cells.Cast<byte>().Count(c => c != PixelCanvas.Blank);
        Check(monitor, "the portrait reference is a real picture", painted > 300 && large.PigmentsUsed.Count >= 5,
            $"{painted} cells, {large.PigmentsUsed.Count} colours, difficulty {large.Difficulty} (medium {medium.Difficulty})");

        // Through the easel: offered, refused on a small canvas, painted on a medium one.
        var menu = new EaselMenu(who);
        Game1.activeClickableMenu = menu;
        Check(monitor, "the easel offers Abigail as a sitter", menu.DevSitterOffered("Abigail") && !menu.DevSitterOffered("Leah"));
        menu.DevSetCanvas(CanvasKind.Small);
        menu.DevBeginFromSetup(Sitters.SubjectFor("Abigail"));
        Check(monitor, "a portrait won't start on a small canvas", !menu.DevIsPainting);
        int pendingBefore = ArtHooks.PendingMinutes;
        who.addItemToInventory(ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Medium)));
        menu.DevSetCanvas(CanvasKind.Medium);
        menu.DevBeginFromSetup(Sitters.SubjectFor("Abigail"));
        Check(monitor, "a portrait starts on a medium canvas", menu.DevIsPainting);
        menu.DevPaintPartly(1f, 3);
        menu.DevFinish(false);
        var portrait = menu.DevPainting;
        Check(monitor, "it's a portrait of Abigail", portrait?.DisplayName == I18n.Get("painting.title.portrait", new { name = Sitters.DisplayName("Abigail") }), portrait?.DisplayName ?? "none");
        Check(monitor, "a medium portrait takes 3 hours", ArtHooks.PendingMinutes - pendingBefore == 180, $"{ArtHooks.PendingMinutes - pendingBefore} minutes");
        var data = portrait == null ? null : PaintingData.Read(portrait);
        int stillLife = data == null ? 0 : (int)(PaintingRules.Value(PaintingRules.CanvasSize(CanvasKind.Medium), data.Difficulty, 1000, 6));
        Check(monitor, "a portrait sells for more than a still life would", portrait != null && portrait.Price > stillLife, $"{portrait?.Price}g vs {stillLife}g");
        if (portrait != null)
        {
            Check(monitor, "the sitter loves their portrait", ArtHooks.PaintingTaste(Game1.getCharacterFromName("Abigail"), portrait) == NPC.gift_taste_love);
            var commission = new Shop.Commission { Npc = "Abigail", ItemId = Sitters.SubjectFor("Abigail") };
            Check(monitor, "it fills a portrait commission", commission.Matches(portrait));
            Check(monitor, "but not someone else's", !new Shop.Commission { Npc = "Leah", ItemId = Sitters.SubjectFor("Leah") }.Matches(portrait));
        }
        menu.DevKeep();
        Game1.exitActiveMenu();

        // The board: portrait requests only ever come from friends who'd sit.
        Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
        var asked = new System.Collections.Generic.List<Shop.Commission>();
        for (int seed = 0; seed < 60; seed++)
        {
            Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
            Shop.Commissions.NewDay(who, monitor, new Random(seed));
            asked.AddRange(Shop.Commissions.Posted(who).Where(c => c.IsPortrait));
        }
        Check(monitor, "the board posts portrait commissions", asked.Count > 0, $"{asked.Count} in 60 days");
        Check(monitor, "only from friends at 4+ hearts", asked.All(c => Sitters.CanSitFor(who, c.Npc)), string.Join(", ", asked.Select(c => c.Npc).Distinct()));
        Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
        ModEntry.Config.Difficulty = Difficulty.Normal;
    }

    // Part two of the paint rework (docs/painting_depth.md).
    private static void PaintTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        int charcoal = Palette.IndexOf("charcoal"), sienna = Palette.IndexOf("sienna");
        if (args.FirstOrDefault() != "check")
        {
            ModEntry.Config.Difficulty = Difficulty.Normal;
            ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter));
            foreach (var old in who.Items.Where(i => ArtItems.IsPalette(i) || ArtItems.TubeFor(i) != null).ToList())
                who.removeItemFromInventory(old);
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.PaletteId));
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(charcoal), 3));
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(sienna)));
            return;                         // the pickup check runs on the next ticks
        }

        var palette = PaletteItem.Find(who);
        Check(monitor, "a new palette comes with the starter colours", palette != null
            && Enumerable.Range(0, Palette.All.Length).All(i => PaletteItem.Read(palette)[i] == (Palette.All[i].Starter ? PaletteItem.TubeCells : 0)));
        Check(monitor, "a tube of a new colour teaches it", ArtProgress.Owns(who, sienna));
        if (palette == null) return;

        var menu = new EaselMenu(who);
        var paint = PaletteItem.Read(palette);
        Check(monitor, "the easel squeezes tubes into the palette, up to a full well", paint[charcoal] == PaletteItem.WellCells
            && who.Items.FirstOrDefault(i => ArtItems.TubeFor(i) == charcoal)?.Stack == 1, $"charcoal {paint[charcoal]}");
        Check(monitor, "a tube of a colour just learned goes in too", paint[sienna] == PaletteItem.TubeCells, $"sienna {paint[sienna]}");

        int before = paint[sienna];
        int painted = menu.DevFillWith(sienna);
        int after = PaletteItem.Read(palette)[sienna];
        Check(monitor, "painting spends one cell of paint per cell", painted > 0 && after == before - painted, $"{painted} painted, {before} -> {after}");
        Check(monitor, "an empty colour can't be painted with", after == 0 && menu.DevFillWith(sienna) == 0);

        // The Pigment Mill: clay in, two tubes of sienna out.
        var location = Game1.currentLocation;
        var tile = new Vector2(who.TilePoint.X, who.TilePoint.Y - 2);
        location.objects.Remove(tile);
        var mill = (SObject)ItemRegistry.Create(ArtItems.QualifiedMillId);
        location.objects.Add(tile, mill);
        bool accepted = mill.performObjectDropInAction(ItemRegistry.Create("(O)330"), false, who);
        var output = mill.heldObject.Value;
        Check(monitor, "the Pigment Mill grinds clay into sienna tubes", accepted && output?.QualifiedItemId == "(O)" + ArtItems.TubeId(sienna) && output.Stack == 2,
            output == null ? "nothing" : $"{output.Stack} {output.QualifiedItemId}, ready in {mill.MinutesUntilReady} minutes");
        bool refused = !mill.performObjectDropInAction(ItemRegistry.Create("(O)24"), true, who);
        Check(monitor, "the mill turns away things that aren't pigment", refused);
        location.objects.Remove(tile);
    }

    private static void DialogueFit(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var sable = Game1.getCharacterFromName("Sable");
        var texts = new System.Collections.Generic.List<(string Where, string Speaker, string Text)>();
        void AddAll(string asset, string speaker, Func<string, bool>? keep = null)
        {
            foreach (var (key, text) in Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>(asset))
                if (keep == null || keep(key))
                    texts.Add(($"{asset}:{key}", speaker, text));
        }
        AddAll("Characters/Dialogue/Sable", "Sable");
        AddAll("Characters/Dialogue/MarriageDialogueSable", "Sable");
        AddAll("Characters/Dialogue/rainy", "Sable", k => k == "Sable");
        AddAll("Data/EngagementDialogue", "Sable", k => k.StartsWith("Sable"));
        foreach (string festival in new[] { "spring13", "spring24", "summer11", "summer28", "fall16", "fall27", "winter8", "winter25" })
            AddAll("Data/Festivals/" + festival, "Sable", k => k == "Sable");
        string[] tastes = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/NPCGiftTastes")["Sable"].Split('/');
        for (int i = 0; i < tastes.Length; i += 2)
            texts.Add(($"Data/NPCGiftTastes:Sable[{i}]", "Sable", tastes[i]));
        var speak = new System.Text.RegularExpressions.Regex(@"speak (\w+) ""([^""]*)""");
        foreach (string location in new[] { Shop.ArtShop.LocationName, "Forest", "JoshHouse" })
            foreach (var (key, script) in Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Events/" + location))
                if (key.StartsWith(ModEntry.Id))
                    foreach (System.Text.RegularExpressions.Match m in speak.Matches(script))
                        texts.Add(($"event {key.Split('/')[0]}", m.Groups[1].Value, m.Groups[2].Value));

        // The content pack's text comes from its i18n file: every token should
        // have resolved, in the dialogue and in the rest of what it adds.
        var raw = texts.Select(t => (t.Where, t.Text)).ToList();
        foreach (string location in new[] { Shop.ArtShop.LocationName, "Forest", "JoshHouse" })
            foreach (var (key, script) in Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Events/" + location))
                if (key.StartsWith(ModEntry.Id))
                    raw.Add(($"event {key.Split('/')[0]}", script));
        raw.Add(("Data/mail", Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/mail")[ModEntry.Id + "_GrandOpening"]));
        raw.Add(("Data/Weddings", DataLoader.Weddings(Game1.content).EventScript["Sable"]));
        raw.Add(("Data/Locations", DataLoader.Locations(Game1.content)[Shop.ArtShop.LocationName].DisplayName));
        raw.Add(("Data/Characters", DataLoader.Characters(Game1.content)["Sable"].DisplayName));
        foreach (var owner in DataLoader.Shops(Game1.content)[Shop.ArtShop.ShopId].Owners)
        {
            raw.Add(($"shop owner {owner.Id}", owner.ClosedMessage ?? ""));
            foreach (var d in owner.Dialogues ?? new())
                raw.Add(($"shop owner {owner.Id}", d.Dialogue ?? ""));
        }
        var unresolved = raw.Where(t => t.Text.Contains("{{") || t.Text.Contains("no translation")).ToList();
        foreach (var (where, text) in unresolved)
            monitor.Log($"[FAIL] unresolved translation ({where}): \"{text}\"", LogLevel.Error);
        Check(monitor, $"every translated text resolved ({raw.Count} entries)", unresolved.Count == 0, $"{unresolved.Count} unresolved");

        // A line can pick between alternatives at random ($c chance#A#B,
        // chained for more); check every alternative, not just one roll.
        static System.Collections.Generic.IEnumerable<string> Alternatives(string text)
        {
            while (text.StartsWith("$c "))
            {
                string[] parts = text.Split('#', 3);
                yield return parts[1];
                text = parts.Length > 2 ? parts[2] : "";
            }
            yield return text;
        }
        string? random = texts.Select(t => t.Text).FirstOrDefault(t => t.StartsWith("$c "));
        if (random != null)
        {
            var seen = Enumerable.Range(0, 30).Select(_ => new Dialogue(sable, null, random).dialogues.FirstOrDefault()?.Text).Distinct().Count();
            Check(monitor, "a line with random alternatives picks between them", seen > 1, $"{seen} different in 30");
        }
        texts = texts.SelectMany(t => Alternatives(t.Text).Select(alt => (t.Where, t.Speaker, alt))).ToList();

        int pages = 0, bad = 0;
        foreach (var (where, speaker, text) in texts)
        {
            var npc = Game1.getCharacterFromName(speaker) ?? sable;
            // (Items in [brackets] are a gift, taken out before it's shown.)
            var dialogue = new Dialogue(npc, null, System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", ""));
            foreach (var line in dialogue.dialogues)
            {
                if (string.IsNullOrWhiteSpace(line.Text)) continue;
                pages++;
                string overflow = StardewValley.BellsAndWhistles.SpriteText.getSubstringBeyondHeight(line.Text, 1200 - 460 - 20, 384 - 16);
                if (overflow.Length > 0)
                {
                    bad++;
                    monitor.Log($"[FAIL] too long for one box ({where}): \"{line.Text}\" (spills: \"{overflow}\")", LogLevel.Error);
                }
            }
        }
        Check(monitor, $"every dialogue page fits its box ({pages} pages)", bad == 0, bad > 0 ? $"{bad} too long" : "");
    }

    private static void Marry(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || Constants.CurrentSavePath != null) return;      // never on a real save
        var who = Game1.player;
        var sable = Game1.getCharacterFromName("Sable");
        who.friendshipData["Sable"] = new Friendship(2500) { Status = FriendshipStatus.Married, WeddingDate = Game1.Date };
        who.spouse = "Sable";
        who.HouseUpgradeLevel = 1;
        var house = Utility.getHomeOfFarmer(who);
        house.setMapForUpgradeLevel(1);
        house.showSpouseRoom();
        Game1.getFarm().addSpouseOutdoorArea("Sable");
        if (args.FirstOrDefault() == "patio")
        {
            sable.setUpForOutdoorPatioActivity();
            monitor.Log($"Sable is on the patio at {sable.TilePoint} (spot {Game1.getFarm().spousePatioSpot}).", LogLevel.Info);
        }
        else
        {
            var spot = house.GetSpouseRoomSpot();
            Game1.warpCharacter(sable, house, spot.ToVector2());
            monitor.Log($"Married. Sable is in the spouse room at {spot}.", LogLevel.Info);
        }
    }

    private static void Wedding(IMonitor monitor)
    {
        if (!Context.IsWorldReady || Constants.CurrentSavePath != null) return;      // never on a real save
        var who = Game1.player;
        // On the morning of the wedding the game has already switched you from engaged to married.
        who.friendshipData["Sable"] = new Friendship(2500) { Status = FriendshipStatus.Married, WeddingDate = Game1.Date };
        who.spouse = "Sable";
        who.HouseUpgradeLevel = Math.Max(1, who.HouseUpgradeLevel);   // as the pendant requires
        var wedding = Utility.getWeddingEvent(who);
        if (wedding == null)
        {
            monitor.Log("No wedding event.", LogLevel.Error);
            return;
        }
        monitor.Log("Wedding script: " + string.Join(" / ", wedding.eventCommands), LogLevel.Info);
        Game1.weddingsToday.Add(who.UniqueMultiplayerID);
        Game1.warpFarmer("Town", 50, 60, 2);
        monitor.Log("Walking into the wedding.", LogLevel.Info);
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
            case "portrait":
                // The easel with a friend picked to sit (museart_menu portrait Abigail).
                var sitting = new EaselMenu(Game1.player);
                sitting.DevSetCanvas(CanvasKind.Medium);
                sitting.DevSetup(Sitters.SubjectFor(args.ElementAtOrDefault(1) ?? "Abigail"));
                Game1.activeClickableMenu = sitting;
                break;
            case "portraitboard":
                // The board with a portrait request at the top.
                var list = new System.Collections.Generic.List<Shop.Commission>
                {
                    new() { Npc = args.ElementAtOrDefault(1) ?? "Abigail", ItemId = Sitters.SubjectFor(args.ElementAtOrDefault(1) ?? "Abigail"), MinQuality = PaintingRules.Silver, Reward = 1450, ExpiresOn = Game1.Date.TotalDays + 2 },
                    new() { Npc = "Leah", ItemId = "(O)348", MinQuality = PaintingRules.Normal, Reward = 300, ExpiresOn = Game1.Date.TotalDays + 2 },
                };
                Shop.Commissions.Save(Game1.player, list);
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
