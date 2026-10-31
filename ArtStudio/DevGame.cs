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
    private static IModHelper _helper = null!;

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
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

        DevMultiplayer.Register(helper, monitor);
        DevMedia.Register(helper, monitor);
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
        DevTools.AddCommand(helper, "museart_export_worldmap", "Writes Data/WorldMap (as the game sees it, with every mod's edits) to exports/worldmap.json.", (_, _) =>
        {
            helper.Data.WriteJsonFile("exports/worldmap.json", Game1.content.Load<System.Collections.Generic.Dictionary<string, StardewValley.GameData.WorldMaps.WorldMapRegionData>>("Data/WorldMap"));
            monitor.Log("Exported Data/WorldMap.", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_season", "Sets the season, for checking seasonal art. Usage: museart_season spring|summer|fall|winter", (_, args) =>
        {
            Game1.season = Enum.Parse<Season>(args[0], ignoreCase: true);
            Game1.setGraphicsForSeason();
            monitor.Log($"Season is now {Game1.season}.", LogLevel.Info);
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
        DevTools.AddCommand(helper, "museart_outfittest", "Checks Sable dresses for the weather: the coat outdoors in winter, everyday clothes indoors and in other seasons, the linen shirt on Ginger Island.", (_, args) => OutfitTest(monitor, args));
        DevTools.AddCommand(helper, "museart_portraittest", "Portraits of villagers: who'll sit, the reference, painting one, its price and time, gifting it, and portrait commissions.", (_, _) => PortraitTest(monitor));
        DevTools.AddCommand(helper, "museart_painttest", "Paint as a resource: the Palette, tubes, spending paint, and the Pigment Mill. Run 'museart_painttest', then 'museart_painttest check' a second later.", (_, args) => PaintTest(monitor, args));
        DevTools.AddCommand(helper, "museart_knowtest", "Pigments as knowledge, and painting taking time. Run 'museart_knowtest', then 'museart_knowtest close', then 'museart_knowtest check' a few seconds later.", (_, args) => KnowTest(monitor, args));
        DevTools.AddCommand(helper, "museart_gallerytest", "The painting record, consignment and plein air. Usage: museart_gallerytest record|consign|view (view: stand outdoors first, e.g. museart_warp Farm 64 15).", (_, args) => GalleryTest(monitor, args));
        DevTools.AddCommand(helper, "museart_marriedlifetest", "Throwaway farm only: Sable paints the farm and gives it to you; Sable comments on your paintings on the wall. Run it, warp onto the farm, then 'museart_marriedlifetest farm'.", (_, args) => MarriedLifeTest(monitor, args));
        DevTools.AddCommand(helper, "museart_watchtest", "Sable at the yard easel on a Friday: the line, and watching. Run it, then 'museart_watchtest check' a few seconds later.", (_, args) => WatchTest(monitor, args));
        DevTools.AddCommand(helper, "museart_modetest", "The easel's mode button: cycling, saving, and the rules it sets. Then 'museart_modetest restore'.", (_, args) => ModeTest(monitor, args));
        DevTools.AddCommand(helper, "museart_blanktest", "A painting with no picture (as CJB Item Spawner makes): names, gift tastes, price, drawing, consigning, varnishing.", (_, _) => BlankTest(monitor));
        DevTools.AddCommand(helper, "museart_cjbtest", "CJB Cheats Menu: the Painting skill button and professions on its Skills tab. Stages: (none) or 'at <level>', 'click', 'check', 'profession'.", (_, args) => CjbTest(helper, monitor, args));
        DevTools.AddCommand(helper, "museart_trapdoortest", "Sable's room under the shop: the trapdoor's lock (2 hearts, or the key to the town), Sable's way down, and a trip down. Then 'museart_trapdoortest down', then 'check'.", (_, args) => TrapdoorTest(monitor, args));
        DevTools.AddCommand(helper, "museart_polytest", "Sable as one of several spouses (Polyamory Sweet): the 14-heart event and the shop's trust.", (_, _) => PolyTest(monitor));
        DevTools.AddCommand(helper, "museart_automatetest", "Automate and the Varnishing Cabinet (needs Automate). Run it, then 'museart_automatetest check' and 'museart_automatetest cured' a few seconds apart.", (_, args) => AutomateTest(monitor, args));
        DevTools.AddCommand(helper, "museart_varnishtest", "The Varnishing Cabinet: loading a painting, the finishes, taking it out early, and a cured one. Run in the farmhouse after museart_devscene.", (_, _) => VarnishTest(monitor));
        DevTools.AddCommand(helper, "museart_techtest", "Techniques that grow with the canvas (glazes, highlights), and Masterpieces on linen.", (_, _) => TechTest(monitor));
        DevTools.AddCommand(helper, "museart_palettetest", "Palette tiers, tubes going straight in, and a placed Palette as storage. Run 'museart_palettetest' in the farmhouse, then 'museart_palettetest check' a second later.", (_, args) => PaletteTest(monitor, args));
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
    internal static void NewGame(IMonitor monitor)
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
            Check(monitor, $"recipe {key} crafts its item", made != null && made.ItemId == raw.Split('/')[2].Split(' ')[0], made?.QualifiedItemId ?? "null");
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

        // Commissions asking for more (1.1): a varnish, or glazing.
        if (pierre != null)
        {
            var cells = ReferenceBuilder.StillLife(new[] { "(O)24" }, 28, ArtProgress.OwnedPigments(who)).Cells;
            SObject Painting(Mark[,]? marks = null) => PaintingItems.Create(new PaintingData { Size = 28, Cells = cells, Purity = new Purity[28, 28], Marks = marks, Subject = "(O)24" }, PaintingRules.Gold, 200);
            bool Delivered(string requirement, SObject painting)
            {
                var c = new Shop.Commission { Npc = "Pierre", ItemId = "(O)24", MinQuality = PaintingRules.Silver, Reward = 100, ExpiresOn = Game1.Date.TotalDays + 5, Accepted = true, Requirement = requirement };
                Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission> { c });
                who.addItemToInventory(painting);
                who.CurrentToolIndex = who.Items.IndexOf(painting);
                pierre.tryToReceiveActiveObject(who);
                CloseDialogue();
                bool gone = !who.Items.Contains(painting);
                who.removeItemFromInventory(painting);
                return gone;
            }
            Check(monitor, "a varnished request turns down an unvarnished painting", !Delivered(Shop.Commissions.Varnished, Painting()));
            var satin = Painting();
            satin.modData[Varnish.FinishKey] = "satin";
            Check(monitor, "...and takes a varnished one", Delivered(Shop.Commissions.Varnished, satin));
            Check(monitor, "a glazed request turns down a painting without glazes", !Delivered(Shop.Commissions.Glazed, Painting()));
            var marks = new Mark[28, 28];
            marks[10, 10] = Mark.Glaze;
            Check(monitor, "...and takes a glazed one", Delivered(Shop.Commissions.Glazed, Painting(marks)));

            // Late in the skill, some postings ask for them, for more, with time to cure.
            int oldLevel = ArtProgress.Level(who);
            ArtProgress.SetLevel(who, 9);
            var posted = new System.Collections.Generic.List<Shop.Commission>();
            for (int seed = 0; seed < 40; seed++)
            {
                Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
                Shop.Commissions.NewDay(who, monitor, new Random(seed));
                posted.AddRange(Shop.Commissions.Posted(who));
            }
            Check(monitor, "some postings ask for a varnish, some for glazing",
                posted.Any(c => c.Requirement == Shop.Commissions.Varnished) && posted.Any(c => c.Requirement == Shop.Commissions.Glazed),
                $"{posted.Count(c => c.Requirement == Shop.Commissions.Varnished)} varnished, {posted.Count(c => c.Requirement == Shop.Commissions.Glazed)} glazed of {posted.Count}");
            var asksVarnish = posted.First(c => c.Requirement == Shop.Commissions.Varnished);
            Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission> { asksVarnish });
            Shop.Commissions.Accept(who, asksVarnish);
            Check(monitor, "a varnished request gives time to cure", Shop.Commissions.Accepted(who).First().DaysLeft >= Shop.Commissions.DaysToDeliverVarnished);
            Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
            ArtProgress.SetLevel(who, oldLevel);
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

    private static void CjbTest(IModHelper helper, IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        System.Collections.Generic.List<OptionsElement> Options() =>
            HarmonyLib.Traverse.Create(Game1.activeClickableMenu).Field("Options").GetValue<System.Collections.IList>()?.Cast<OptionsElement>().ToList() ?? new();
        OptionsElement? Painting() => Options().FirstOrDefault(o => o.label.Contains(PaintingSkill.Instance.GetName()) && o.GetType().Name == "CheatButton");
        switch (args.FirstOrDefault())
        {
            case null:
            case "at":
            {
                var cjb = helper.ModRegistry.Get(Integrations.CjbCheatsIntegration.ModId);
                Check(monitor, "CJB Cheats Menu is installed", cjb != null);
                if (cjb == null) return;
                ArtProgress.SetLevel(who, args.Length > 1 ? int.Parse(args[1]) : 0);
                var mod = HarmonyLib.Traverse.Create(cjb).Property("Mod").GetValue<object>();
                var tab = Enum.Parse(HarmonyLib.AccessTools.TypeByName("CJBCheatsMenu.Framework.MenuTab"), "Skills");
                HarmonyLib.AccessTools.Method(mod.GetType(), "OpenCheatsMenu", new[] { tab.GetType(), typeof(bool) }).Invoke(mod, new[] { tab, (object)true });
                var options = Options();
                int button = options.IndexOf(Painting()!);
                Check(monitor, "the Skills tab has a Painting button after the vanilla skills", button == 6, $"at {button}: {(button >= 0 ? options[button].label : "none")}");
                int professions = options.Count(o => o.GetType().Name == "CheatCheckbox" && o.label.Contains(PaintingSkill.Instance.GetName()));
                Check(monitor, "the Professions list has the six Painting professions", professions == 6, $"{professions}");
                return;
            }
            case "click":
            {
                var button = Painting();
                if (button == null) return;
                var bounds = HarmonyLib.Traverse.Create(button).Field("SetButtonBounds").GetValue<Microsoft.Xna.Framework.Rectangle>();
                button.receiveLeftClick(bounds.Center.X, bounds.Center.Y);
                return;
            }
            case "check":
                Check(monitor, "the button raises Painting a level", ArtProgress.Level(who) == 1, $"{ArtProgress.Level(who)}");
                Check(monitor, "with Painting's own level-up screen", Game1.activeClickableMenu is SpaceCore.Interface.SkillLevelUpMenu, Game1.activeClickableMenu?.GetType().Name);
                var queued = HarmonyLib.Traverse.Create(typeof(SpaceCore.Skills)).Property("NewLevels").GetValue<System.Collections.IList>();
                Check(monitor, "and not again at the end of the day", queued.Count == 0, $"{queued.Count} queued");
                return;
            case "profession":
            {
                var gallerist = Options().FirstOrDefault(o => o.label.Contains(PaintingSkill.Instance.Gallerist.GetName()));
                gallerist?.receiveLeftClick(gallerist.bounds.X + 4, gallerist.bounds.Y + 4);
                Check(monitor, "ticking Gallerist gives you the profession", PaintingSkill.Has(who, PaintingSkill.Instance.Gallerist));
                gallerist?.receiveLeftClick(gallerist.bounds.X + 4, gallerist.bounds.Y + 4);
                Check(monitor, "unticking takes it away", !PaintingSkill.Has(who, PaintingSkill.Instance.Gallerist));
                return;
            }
        }
    }

    private static void TrapdoorTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var shop = Game1.getLocationFromName(Shop.ArtShop.LocationName);
        var room = Game1.getLocationFromName(Shop.ArtShop.Basement);
        if (args.FirstOrDefault() == "down")
        {
            shop!.performAction("LockedDoorWarp 1 2 " + Shop.ArtShop.Basement + " 600 2600 Sable 500", who, new xTile.Dimensions.Location(18, 9));
            return;
        }
        if (args.FirstOrDefault() == "check")
        {
            Check(monitor, "the trapdoor takes you down to the foot of Sable's stairs", who.currentLocation == room && who.TilePoint == new Point(1, 2), $"{who.currentLocation?.Name} {who.TilePoint}");
            Check(monitor, "once let in, the trapdoor stays open", who.mailReceived.Contains("doorUnlockSable"));
            return;
        }
        Check(monitor, "Sable's room exists", room != null);
        if (shop == null || room == null) return;
        Check(monitor, "the trapdoor is a door down to Sable's room", shop.doors.TryGetValue(new Point(18, 9), out var to) && to == Shop.ArtShop.Basement);
        Check(monitor, "the gallery evening has no trapdoor in the way", Game1.content.Load<xTile.Map>("Maps/" + ModEntry.Id + "_ArtShopGallery").GetLayer("Buildings").Tiles[18, 9] == null);
        var stairs = room.warps.FirstOrDefault();
        Check(monitor, "the stairs lead up to beside the trapdoor", stairs != null && stairs.TargetName == Shop.ArtShop.LocationName && stairs.TargetX == 17 && stairs.TargetY == 9 && shop.isTilePassable(new Vector2(17, 9)), $"{stairs?.TargetName} {stairs?.TargetX},{stairs?.TargetY}");
        Check(monitor, "the foot of the stairs is open to arrive on", !room.isCollidingPosition(new Rectangle(64 + 8, 2 * 64 + 8, 48, 48), Game1.viewport, true, 0, false, who));
        Check(monitor, "Sable's bed is free to lie in, and reachable from beside it", room.isTilePassable(new Vector2(10, 5)) && room.isTilePassable(new Vector2(9, 5)));
        var route = StardewValley.Pathfinding.WarpPathfindingCache.GetLocationRoute("Forest", Shop.ArtShop.Basement, StardewValley.Gender.Undefined);
        Check(monitor, "Sable can walk from the Forest down to their room", route != null, route == null ? "no route" : string.Join(" > ", route));
        var sable = Game1.getCharacterFromName("Sable");
        Check(monitor, "Sable lives in their room", sable?.DefaultMap == Shop.ArtShop.Basement && sable.DefaultPosition / 64f == new Vector2(10, 5), $"{sable?.DefaultMap} {sable?.DefaultPosition / 64f}");

        // The lock, as a stranger, with the key to the town, and as a friend.
        int points = who.friendshipData.TryGetValue("Sable", out var f) ? f.Points : 0;
        bool hadKey = who.HasTownKey;
        who.friendshipData["Sable"] = new Friendship(0);
        who.mailReceived.Remove("doorUnlockSable");
        who.HasTownKey = false;
        Check(monitor, "a stranger can't go down", !Shop.ArtShop.MayGoDown(who));
        shop.performAction("LockedDoorWarp 1 2 " + Shop.ArtShop.Basement + " 600 2600 Sable 500", who, new xTile.Dimensions.Location(18, 9));
        Check(monitor, "a stranger is told it's bolted", Game1.activeClickableMenu is DialogueBox && Game1.locationRequest == null);
        CloseDialogue();
        Check(monitor, "the key to the town opens it", (who.HasTownKey = true) && Shop.ArtShop.MayGoDown(who) && !who.mailReceived.Contains("doorUnlockSable"));
        who.HasTownKey = hadKey;
        who.friendshipData["Sable"] = new Friendship(500);
        // (Not into the shop first: at 2 hearts, Sable's event there would start.)
        monitor.Log($"(Sable's friendship was {points}; now 2 hearts. Next: 'museart_trapdoortest down'.)", LogLevel.Info);
    }

    // A villager walks a tile in 32 ticks (2 pixels a tick), and ten game
    // minutes are 7 seconds: about 13 tiles every ten minutes.
    private static int WalkMinutes(int tiles) => (int)Math.Ceiling(tiles * 32 / 60.0 / (Game1.realMilliSecondsPerGameTenMinutes / 10000.0));

    private static void NpcTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var sable = Game1.getCharacterFromName("Sable");
        Check(monitor, "Sable exists", sable != null);
        if (sable == null) return;
        Check(monitor, "Sable lives under the art shop", sable.DefaultMap == Shop.ArtShop.Basement, sable.DefaultMap);
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
                int tiles = 0;
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
                        var path = StardewValley.Pathfinding.PathFindController.findPathForNPCSchedules(start, end, location, 30000, sable);
                        if (path == null)
                            problems.Add($"{stop.time}: no path in {location.Name} from {start} to {end}");
                        else
                            tiles += path.Count;
                        if (i < locations.Length - 1)
                            start = location.getWarpPointTarget(end, sable);
                    }
                }
                // Sable sets off at the stop's time; at the counter they must
                // be there by opening, not leave for it then.
                if (stop.targetLocationName == Shop.ArtShop.LocationName && Shop.ArtShop.KeeperArea.Contains(stop.targetTile))
                {
                    int opens = key == "Sun" ? 1300 : 1000;
                    int arrives = Utility.ModifyTime(stop.time, WalkMinutes(tiles));
                    Check(monitor, $"schedule '{key}': Sable reaches the counter by {opens}", arrives <= opens, $"leaves {stop.time}, {tiles} tiles, there at {arrives}");
                }
                from = stop.targetLocationName;
                at = stop.targetTile;
            }
            Check(monitor, $"schedule '{key}' is walkable", problems.Count == 0,
                problems.Count == 0 ? $"{parsed.Count} stops" : string.Join("; ", problems));
        }
        // Married, the game starts their day at the Bus Stop (10, 23), as
        // for any spouse going out.
        foreach (var (key, script) in raw.Where(r => r.Key.StartsWith("marriage")))
        {
            var first = script.Split('/')[0].Split(' ');
            if (first[1] != Shop.ArtShop.LocationName)
                continue;           // (Sundays, from the beach: as when single)
            int leaves = int.Parse(first[0]);
            var at = new Point(10, 23);
            var locations = StardewValley.Pathfinding.WarpPathfindingCache.GetLocationRoute("BusStop", Shop.ArtShop.LocationName, sable.Gender);
            int tiles = 0;
            for (int i = 0; locations != null && i < locations.Length; i++)
            {
                var location = Game1.RequireLocation(locations[i]);
                var end = i < locations.Length - 1 ? location.getWarpPointTo(locations[i + 1]) : new Point(3, 4);
                tiles += StardewValley.Pathfinding.PathFindController.findPathForNPCSchedules(at, end, location, 30000, sable)?.Count ?? 9999;
                if (i < locations.Length - 1)
                    at = location.getWarpPointTarget(end, sable);
            }
            int opens = key == "marriage_Sun" ? 1300 : 1000;
            int arrives = Utility.ModifyTime(leaves, WalkMinutes(tiles));
            Check(monitor, $"schedule '{key}': Sable reaches the counter from the farm by {opens}", locations != null && arrives <= opens, $"leaves {leaves}, {tiles} tiles, there at {arrives}");
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
        if (args.FirstOrDefault() == null)
            FreshStart(who);
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
        // (The clock may have ticked on once by itself while the test waited.)
        Check(monitor, "closing the easel after painting moves the clock on", Game1.timeOfDay is 1200 or 1210, $"10:00 -> {Game1.timeOfDay}");
        Game1.timeOfDay = 2400;
        Check(monitor, "a large painting started at midnight would run too late", ArtHooks.FinishTime(PaintingRules.Minutes(CanvasKind.Large)) > PaintingRules.LatestFinish);
        Check(monitor, "a small one at midnight still fits", ArtHooks.FinishTime(PaintingRules.Minutes(CanvasKind.Small)) <= PaintingRules.LatestFinish);
        Game1.timeOfDay = 1200;
    }

    private static void OutfitTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        if (args.FirstOrDefault() == "festival")
        {
            // Run during a festival: the festival's own Sable.
            var actor = Game1.CurrentEvent?.actors.FirstOrDefault(a => a.Name == "Sable");
            Check(monitor, $"at the festival Sable wears {args.ElementAtOrDefault(1) ?? "the coat"}", actor != null && actor.Sprite.textureName.Value.EndsWith(args.ElementAtOrDefault(2) ?? "Sable_Winter"),
                actor == null ? "no Sable here" : actor.Sprite.textureName.Value);
            return;
        }
        var sable = Game1.getCharacterFromName("Sable");
        if (sable == null) return;
        var home = sable.currentLocation;
        var season = Game1.season;
        string? Wearing(string location, Season s, bool island = false)
        {
            Game1.season = s;
            Game1.warpCharacter(sable, location, sable.TilePoint);
            if (island) sable.wearIslandAttire(); else sable.wearNormalClothes();
            sable.ChooseAppearance();
            return sable.LastAppearanceId;
        }
        Check(monitor, "the winter coat outdoors in winter", Wearing("Forest", Season.Winter) == "Winter", Wearing("Forest", Season.Winter) ?? "everyday");
        Check(monitor, "everyday clothes indoors in winter", Wearing(Shop.ArtShop.LocationName, Season.Winter) == null);
        Check(monitor, "everyday clothes outdoors in spring", Wearing("Forest", Season.Spring) == null);
        Check(monitor, "the linen shirt on Ginger Island", Wearing("IslandSouth", Season.Summer, island: true) == "Island");
        string sprite = sable.Sprite.textureName.Value, portrait = sable.Portrait?.Name ?? "";
        Check(monitor, "the island outfit loads its own sheets", sprite.EndsWith("Sable_Beach") && portrait.EndsWith("Sable_Beach"), $"{sprite}, {portrait}");
        sable.wearNormalClothes();
        Game1.season = season;
        Game1.warpCharacter(sable, home.NameOrUniqueName, sable.TilePoint);
        sable.ChooseAppearance();
    }

    private static void PortraitTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        FreshStart(who);
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
        int stillLife = data == null ? 0 : (int)(PaintingRules.Value(PaintingRules.CanvasSize(CanvasKind.Medium), data.Difficulty, PaintingRules.PortraitSubjectPrice, 6));
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

    // Palettes (2026-10-25): tiers, tubes stored on pickup, storage when placed.
    private static void PaletteTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        int charcoal = Palette.IndexOf("charcoal"), crimson = Palette.IndexOf("crimson");
        if (args.FirstOrDefault() != "check")
        {
            FreshStart(who);
            ModEntry.Config.Difficulty = Difficulty.Normal;
            foreach (var old in who.Items.Where(i => ArtItems.IsPalette(i) || ArtItems.TubeFor(i) != null).ToList())
                who.removeItemFromInventory(old);
            // A new wooden Palette holds a tube of charcoal; five more arrive.
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.PaletteId));
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(charcoal), 5));
            return;
        }

        var wooden = who.Items.First(ArtItems.IsPalette);
        int tubesLeft = who.Items.Where(i => ArtItems.TubeFor(i) == charcoal).Sum(i => i.Stack);
        Check(monitor, "tubes go straight into the palette, up to its 3 tubes of a colour", PaletteItem.Read(wooden)[charcoal] == 300 && tubesLeft == 3,
            $"{PaletteItem.Read(wooden)[charcoal]} cells in, {tubesLeft} tubes left in the bag");

        var enamel = ItemRegistry.Create("(O)" + PaletteItem.Tiers[1].Id);
        var iridium = ItemRegistry.Create("(O)" + PaletteItem.Tiers[2].Id);
        Check(monitor, "the tiers hold 3, 6 and 10 tubes of a colour",
            PaletteItem.Capacity(wooden, who) == 300 && PaletteItem.Capacity(enamel, who) == 600 && PaletteItem.Capacity(iridium, who) == 1000);
        who.addItemToInventory(iridium);
        Check(monitor, "the easel paints from the biggest palette in the bag", PaletteItem.Find(who) == iridium);

        // Not a gift: handing it to a villager does nothing, and it stays in the bag.
        Check(monitor, "palettes can't be given as gifts", !((SObject)wooden).canBeGivenAsGift() && !((SObject)iridium).canBeGivenAsGift());
        var leah = Game1.getCharacterFromName("Leah");
        who.CurrentToolIndex = who.Items.IndexOf(iridium);
        int points = who.getFriendshipLevelForNPC("Leah");
        bool taken = leah.tryToReceiveActiveObject(who, probe: true);
        Check(monitor, "a villager won't take one", !taken && who.Items.Contains(iridium) && who.getFriendshipLevelForNPC("Leah") == points);
        who.removeItemFromInventory(iridium);

        // Set down on the floor: storage, paint and all.
        var house = who.currentLocation;
        var tile = who.Tile + new Vector2(-2, 0);
        house.objects.Remove(tile);
        var toPlace = (SObject)wooden;
        Check(monitor, "a palette can be set down", toPlace.placementAction(house, (int)tile.X * 64, (int)tile.Y * 64, who));
        who.removeItemFromInventory(wooden);
        var placed = house.objects.TryGetValue(tile, out var obj) ? obj : null;
        Check(monitor, "the placed palette keeps its paint", placed != null && PaletteItem.Read(placed)[charcoal] == 300);
        if (placed == null) return;

        placed.checkForAction(who);
        Check(monitor, "right-clicking it opens its storage", Game1.activeClickableMenu is PaletteMenu);
        Game1.exitActiveMenu();
        var out1 = PaletteItem.Withdraw(placed, charcoal);
        Check(monitor, "a whole tube comes out", out1 != null && PaletteItem.Read(placed)[charcoal] == 200);
        who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(crimson), 4));
        var crimsonTubes = who.Items.First(i => ArtItems.TubeFor(i) == crimson);
        int went = PaletteItem.Deposit(who, placed, crimsonTubes, int.MaxValue);
        Check(monitor, "tubes from the bag go in, as many as fit", went == 2 && PaletteItem.Read(placed)[crimson] == 300, $"{went} went in");

        placed.performToolAction(ItemRegistry.Create<Tool>("(T)Pickaxe"));
        var dropped = house.debris.Select(d => d.item).FirstOrDefault(ArtItems.IsPalette);
        Check(monitor, "a tool picks it up with its paint", !house.objects.ContainsKey(tile) && dropped != null
            && PaletteItem.Read(dropped)[charcoal] == 200 && PaletteItem.Read(dropped)[crimson] == 300);
        house.debris.Clear();
    }

    // Glazes, highlights and Masterpieces (2026-10-25).
    private static void TechTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        FreshStart(who);
        ModEntry.Config.Difficulty = Difficulty.Normal;
        ArtProgress.SetLevel(who, 10);
        ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length));
        foreach (var item in who.Items.Where(PaintingItems.IsPainting).ToList())
            who.removeItemFromInventory(item);
        var all = ArtProgress.OwnedPigments(who);
        int Size(CanvasKind k) => PaintingRules.CanvasSize(k);

        // References: flat on a small canvas, glazed on a medium one, highlights on a large one.
        string[] subjects = { "(O)254", "(O)276", "(O)72", "(O)66", "(O)613", "(O)400", "(O)252", "(O)268" };
        Reference Ref(string id, CanvasKind k, Difficulty d = Difficulty.Normal) =>
            ReferenceBuilder.StillLife(new[] { id }, Size(k), all, PaintingRules.TechniquesFor(Size(k), d == Difficulty.Relaxed));
        int glazedMedium = subjects.Count(id => Technique.Count(Ref(id, CanvasKind.Medium).Marks, Mark.Glaze) > 0);
        int highlightedLarge = subjects.Count(id => Technique.Count(Ref(id, CanvasKind.Large).Marks, Mark.Highlight) > 0);
        Check(monitor, "a small canvas is flat colour", subjects.All(id => !Ref(id, CanvasKind.Small).UsesTechniques));
        Check(monitor, "medium references have glazes", glazedMedium >= subjects.Length / 2, $"{glazedMedium} of {subjects.Length}");
        Check(monitor, "only large ones have highlights", highlightedLarge > 0 && subjects.All(id => Technique.Count(Ref(id, CanvasKind.Medium).Marks, Mark.Highlight) == 0),
            $"{highlightedLarge} of {subjects.Length} large");
        Check(monitor, "relaxed paints flat", subjects.All(id => !Ref(id, CanvasKind.Large, Difficulty.Relaxed).UsesTechniques));
        foreach (string id in subjects.Take(4))
        {
            var r = Ref(id, CanvasKind.Large);
            monitor.Log($"  {ItemRegistry.GetDataOrErrorItem(id).DisplayName}: {r.SubjectCells} cells, {Technique.Count(r.Marks, Mark.Glaze)} glazed, {Technique.Count(r.Marks, Mark.Highlight)} highlights, difficulty {r.Difficulty} (small {Ref(id, CanvasKind.Small).Difficulty})", LogLevel.Info);
        }

        // The canvas: a glaze needs paint under it, paint covers it, wet paint smears a highlight.
        var canvas = new PixelCanvas(10);
        int budget = 100;
        Check(monitor, "no glaze on bare canvas", !canvas.SetMark(2, 2, Mark.Glaze));
        canvas.Set(2, 2, 3, Purity.Pure, ref budget);
        Check(monitor, "a glaze goes over paint", canvas.SetMark(2, 2, Mark.Glaze) && canvas.MarkAt(2, 2) == Mark.Glaze);
        canvas.Set(2, 2, 4, Purity.Pure, ref budget);
        Check(monitor, "painting over a glaze covers it", canvas.MarkAt(2, 2) == Mark.None);
        canvas.SetMark(2, 2, Mark.Highlight);
        canvas.Set(3, 2, 5, Purity.Pure, ref budget);
        Check(monitor, "painting beside a highlight smears it", canvas.MarkAt(2, 2) == Mark.None && canvas.Smeared == 1);

        // The judge: the techniques count for about a star.
        var medium = Ref("(O)254", CanvasKind.Medium);
        var cells = medium.Cells;
        var purity = new Purity[Size(CanvasKind.Medium), Size(CanvasKind.Medium)];
        var with = PaintingJudge.Judge(cells, purity, medium, medium.Marks);
        var without = PaintingJudge.Judge(cells, purity, medium, null);
        Check(monitor, "a perfect copy with its glazes gets five stars", with.Stars == 5f && with.Technique == 1f, $"{with.Stars} stars, technique {with.Technique}");
        Check(monitor, "without them, it loses about a star", without.Technique == 0f && with.Stars - without.Stars is >= 0.5f and <= 1.5f, $"{without.Stars} stars");

        // Through the easel: the tools come with the canvas.
        var smallMenu = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { "(O)254" });
        var largeMenu = EaselMenu.StartStillLife(who, CanvasKind.Large, new[] { "(O)254" });
        Check(monitor, "the glaze and highlight tools come with the canvas",
            !smallMenu.DevToolAvailable(PaintTool.Glaze) && largeMenu.DevToolAvailable(PaintTool.Glaze) && largeMenu.DevToolAvailable(PaintTool.Highlight)
            && !EaselMenu.StartStillLife(who, CanvasKind.Medium, new[] { "(O)254" }).DevToolAvailable(PaintTool.Highlight));

        // Saved with the painting.
        largeMenu.DevPaintPartly(1f, 3);
        largeMenu.DevFinish(false);
        var painted = largeMenu.DevPainting;
        var reread = painted == null ? null : PaintingData.Read(painted);
        Check(monitor, "the glazes and highlights are saved with the painting", reread?.Marks != null
            && Technique.Count(reread.Marks, Mark.Glaze) == Technique.Count(largeMenu.DevReference!.Marks, Mark.Glaze),
            reread?.Marks == null ? "none" : $"{Technique.Count(reread.Marks, Mark.Glaze)} glazes, {Technique.Count(reread.Marks, Mark.Highlight)} highlights");
        Check(monitor, "results score the technique", largeMenu.DevResult?.Technique is >= 0.9f, largeMenu.DevResult?.Technique.ToString() ?? "none");

        // Masterpieces: only on linen, only when everything's right.
        SObject? Paint(bool linen, bool techniques)
        {
            FreshStart(who);
            who.addItemToInventory(ItemRegistry.Create(linen ? ArtItems.QualifiedLinenId(CanvasKind.Medium) : ArtItems.QualifiedCanvasId(CanvasKind.Medium)));
            var m = new EaselMenu(who);
            Game1.activeClickableMenu = m;
            m.DevSetCanvas(CanvasKind.Medium);
            m.DevSetLinen(linen);
            m.DevBeginFromSetup("(O)254");
            if (!m.DevIsPainting) return null;
            m.DevPaintPerfect(techniques);
            m.DevFinish(false);
            var p = m.DevPainting;
            m.DevKeep();
            Game1.exitActiveMenu();
            return p;
        }
        var master = Paint(linen: true, techniques: true);
        var plain = Paint(linen: false, techniques: true);
        var sloppy = Paint(linen: true, techniques: false);
        Check(monitor, "a perfect painting on linen is a Masterpiece", master != null && PaintingData.IsMasterpiece(master), master == null ? "didn't start" : $"quality {master.Quality}");
        Check(monitor, "the same on plain canvas isn't", plain != null && !PaintingData.IsMasterpiece(plain) && plain.Quality == PaintingRules.Iridium);
        Check(monitor, "nor on linen with the glazes left out", sloppy != null && !PaintingData.IsMasterpiece(sloppy));
        if (master != null && plain != null)
        {
            Check(monitor, "a Masterpiece is worth half as much again", Math.Abs(master.Price - plain.Price * PaintingRules.MasterpieceValue) <= 2f, $"{master.Price}g vs {plain.Price}g");
            Check(monitor, "it says so", master.getDescription().Contains(I18n.Get("painting.provenance.masterpiece")));
            Check(monitor, "a Masterpiece stays iridium to the game", master.Quality == PaintingRules.Iridium);
        }
        Check(monitor, "using linen takes a linen canvas", !who.Items.Any(i => i?.QualifiedItemId == ArtItems.QualifiedLinenId(CanvasKind.Medium)));
    }

    // Varnishing (2026-10-25): the cabinet works like a cask.
    // Tests begin in the morning, with an empty painting record, so what ran
    // before (time spent painting, novelty, milestone rewards) can't sway them.
    private static void FreshStart(Farmer who)
    {
        ArtHooks.DevFreshMorning();
        who.modData.Remove(ModEntry.Id + "/Catalogue");
        who.modData.Remove(ModEntry.Id + "/CollectionPerks");
    }

    // Married life with Sable (1.1): run it (it marries you and sets a
    // painting day), warp onto the farm, then run 'museart_marriedlifetest farm'.
    private static void MarriedLifeTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || Constants.CurrentSavePath != null) return;      // never on a real save
        var who = Game1.player;
        var sable = Game1.getCharacterFromName("Sable");
        if (args.FirstOrDefault() != "farm")
        {
            FreshStart(who);
            Marry(monitor, new[] { "patio" });
            SableMarriage.Gifts(who).Clear();
            who.modData[SableMarriage.PaintsTodayKey] = Game1.Date.TotalDays.ToString();
            monitor.Log("Married, and today Sable paints the farm. Warp onto the farm, then run 'museart_marriedlifetest farm'.", LogLevel.Info);
            return;
        }
        var gift = SableMarriage.Gifts(who).FirstOrDefault(i => i != null);
        var data = gift == null ? null : PaintingData.Read(gift);
        Check(monitor, "out on the farm on a painting day, Sable paints it", data != null && Views.IsView(data.Subject) && Views.PlaceOf(data.Subject) == "Farm",
            data == null ? "nothing" : $"{gift!.DisplayName} by {data.Painter}");
        Check(monitor, "...signed by Sable", data?.Painter == sable.displayName);

        sable.CurrentDialogue.Clear();
        SableMarriage.MorningOf(who, new Random(1));
        Check(monitor, "next morning Sable offers it", sable.CurrentDialogue.Count > 0
            && sable.CurrentDialogue.Peek().dialogues.Any(l => l.Text.Contains("farm")));
        Game1.drawDialogue(sable);
        // (Left up for four seconds, for a screenshot.)
        int ticks = 0;
        _helper.Events.GameLoop.UpdateTicked += Shot;
        void Shot(object? s, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (++ticks < 240) return;
            _helper.Events.GameLoop.UpdateTicked -= Shot;
            CloseDialogue();
            sable.CurrentDialogue.Clear();
            int before = who.Items.Count(PaintingItems.IsPainting);
            StardewValley.Triggers.TriggerActionManager.TryRunAction(SableMarriage.GiveGiftAction, out _, out _);
            Check(monitor, "...and hands it over", who.Items.Count(PaintingItems.IsPainting) == before + 1 && !SableMarriage.Gifts(who).Any(i => i != null));

            // A painting of yours on the wall gets a word.
            var home = Utility.getHomeOfFarmer(who);
            var m = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { "(O)254" });
            m.DevPaintPartly(1f, 3);
            m.DevFinish(false);
            var framed = ArtHooks.Frame(m.DevPainting!);
            who.removeItemFromInventory(m.DevPainting!);
            framed.TileLocation = new Vector2(4, 1);
            home.furniture.Add(framed);
            Check(monitor, "Sable comments on a painting of yours on the wall", SableMarriage.CommentOnWall(who, sable, new Random(2))
                && sable.CurrentDialogue.Peek().dialogues.Any(l => l.Text.Contains(PaintingItems.Title(PaintingData.Read(framed)!)) && !l.Text.Contains("(")),
                string.Join(" ", sable.CurrentDialogue.Peek().dialogues.Select(l => l.Text)));
            home.furniture.Remove(framed);
            sable.CurrentDialogue.Clear();
        }
    }

    // Watching Sable at the yard easel (1.1): run it, then 'museart_watchtest
    // check' a few seconds later (watching fades out and back).
    private static int _watchStartTime, _watchStartXp;
    private static void WatchTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var sable = Game1.getCharacterFromName("Sable");
        if (args.FirstOrDefault() == "check")
        {
            Check(monitor, "watching passes half an hour", Game1.timeOfDay == Utility.ModifyTime(_watchStartTime, Shop.ArtShop.WatchMinutes), $"{_watchStartTime} -> {Game1.timeOfDay}");
            Check(monitor, "...and teaches a little", ArtProgress.Xp(who) == _watchStartXp + Shop.ArtShop.WatchXp, $"{_watchStartXp} -> {ArtProgress.Xp(who)}");
            CloseDialogue();
            sable.CurrentDialogue.Clear();
            sable.checkAction(who, sable.currentLocation);
            Check(monitor, "only once a day", Game1.activeClickableMenu is DialogueBox box && !box.isQuestion);
            CloseDialogue();
            return;
        }
        FreshStart(who);
        string line = Game1.content.LoadString("Strings\\schedules\\Sable:yardEasel");
        var lines = Enumerable.Range(0, 6).Select(i => I18nCp("yard.painting." + i)).ToList();
        Check(monitor, "the yard easel line is one of Sable's", lines.Contains(line), line);
        sable.TryLoadSchedule("Fri");
        Check(monitor, "Friday at the yard easel carries it", sable.Schedule?.TryGetValue(800, out var stop) == true
            && stop.endOfRouteMessage == "Strings\\schedules\\Sable:yardEasel" && stop.targetTile == Shop.ArtShop.YardEaselSpot);
        sable.ClearSchedule();

        // Sable at the easel, having said their piece; talking again offers to watch.
        Game1.timeOfDay = 800;
        var forest = Game1.getLocationFromName("Forest");
        Game1.warpCharacter(sable, forest, Shop.ArtShop.YardEaselSpot.ToVector2());
        if (!who.friendshipData.ContainsKey("Sable")) who.friendshipData.Add("Sable", new Friendship(500));
        who.friendshipData["Sable"].TalkedToToday = true;
        sable.CurrentDialogue.Clear();
        who.modData.Remove(ModEntry.Id + "/WatchedSable");
        who.CurrentToolIndex = 0;   // empty-handed (holding something, talking is offering it)
        sable.checkAction(who, forest);
        Check(monitor, "talking again at the easel offers to watch", Game1.activeClickableMenu is DialogueBox q && q.isQuestion);
        CloseDialogue();
        _watchStartTime = Game1.timeOfDay;
        _watchStartXp = ArtProgress.Xp(who);
        Shop.ArtShop.Watch(who, sable);
    }

    // A line from the content pack's translations, as the game would show it.
    private static string I18nCp(string key)
    {
        string path = System.IO.Path.Combine(_helper.DirectoryPath, "..", "[CP] Pelican Palette", "i18n", "default.json");
        using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
        return doc.RootElement.TryGetProperty(key, out var value) ? value.GetString() ?? key : key;
    }

    // The mode button on the easel's setup screen (1.1.1). Run it, take a
    // screenshot if wanted, then 'museart_modetest restore'.
    private static Difficulty? _modeBefore;
    private static void ModeTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        if (args.FirstOrDefault() == "restore")
        {
            if (_modeBefore is Difficulty before) { ModEntry.Config.Difficulty = before; ModEntry.SaveConfig(); }
            Game1.exitActiveMenu();
            return;
        }
        _modeBefore = ModEntry.Config.Difficulty;
        ModEntry.Config.Difficulty = Difficulty.Relaxed;
        var menu = new EaselMenu(Game1.player);
        menu.DevSetup("(O)24");
        Game1.activeClickableMenu = menu;
        void Draw()
        {
            Game1.spriteBatch.Begin();
            menu.draw(Game1.spriteBatch);
            Game1.spriteBatch.End();
        }
        Draw();
        var button = menu.DevModeButton;
        Check(monitor, "with nothing to warn about, the mode is a button", button != Rectangle.Empty);
        Check(monitor, "Relaxed has no light meter", !menu.DevTimed);
        menu.receiveLeftClick(button.Center.X, button.Center.Y);
        Check(monitor, "clicking it moves on to Normal", ModEntry.Config.Difficulty == Difficulty.Normal);
        Check(monitor, "...which the painting about to begin uses", menu.DevTimed);
        string saved = System.IO.File.ReadAllText(System.IO.Path.Combine(_helper.DirectoryPath, "config.json"));
        Check(monitor, "...and it's saved", saved.Contains("\"Difficulty\": \"Normal\""));
        // Back to Relaxed for the screenshot, hovering the button.
        ModEntry.Config.Difficulty = Difficulty.Relaxed;
        menu = new EaselMenu(Game1.player);
        menu.DevSetup("(O)24");
        Game1.activeClickableMenu = menu;
        Draw();
        menu.DevHoverAt(menu.DevModeButton.Center);
        monitor.Log($"Staged: {ModEntry.Config.Difficulty}, button at {menu.DevModeButton}.", LogLevel.Info);
    }

    // A painting with no picture, as an item spawner (CJB Item Spawner)
    // makes one: everything that handles paintings must cope with it.
    private static void BlankTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        FreshStart(who);
        bool Safe(string what, Action act)
        {
            try { act(); return true; }
            catch (Exception ex) { monitor.Log($"{what}: {ex}", LogLevel.Error); return false; }
        }
        SObject Blank() => ItemRegistry.Create<SObject>(PaintingItems.QualifiedPaintingId);
        var blank = Blank();
        Check(monitor, "a blank painting has a name and description", Safe("text", () => { _ = blank.DisplayName; _ = blank.getDescription(); }));
        Check(monitor, "...gift tastes don't break on it", Safe("taste", () =>
        {
            foreach (string name in new[] { "Leah", "Pierre", "Sable" })
                Game1.getCharacterFromName(name)?.getGiftTasteForThisItem(blank);
        }));
        Check(monitor, "...it has a sale price", Safe("price", () => _ = blank.sellToStorePrice()) && blank.sellToStorePrice() > 0, $"{blank.sellToStorePrice()}g");
        Check(monitor, "...it draws as a bare canvas", Safe("draw", () =>
        {
            Game1.spriteBatch.Begin();
            blank.drawInMenu(Game1.spriteBatch, Vector2.Zero, 1f);
            blank.drawWhenHeld(Game1.spriteBatch, Vector2.Zero, who);
            Game1.spriteBatch.End();
        }));
        Check(monitor, "...it can't be framed (nothing to frame)", PaintingData.Read(blank) == null);
        Check(monitor, "...it can be consigned and sold", Safe("consign", () =>
        {
            var c = Blank();
            who.addItemToInventory(c);
            Shop.Consignment.Consign(who, c);
            c.modData[ModEntry.Id + "/SellsOn"] = Game1.Date.TotalDays.ToString();
            Shop.Consignment.SellDue(who);
        }));
        Check(monitor, "...a varnishing cabinet takes and returns it", Safe("varnish", () =>
        {
            var cab = ItemRegistry.Create<SObject>(Varnish.QualifiedCabinetId);
            Varnish.Start(cab, Blank());
            cab.modData[ModEntry.Id + "/VarnishStart"] = (Game1.Date.TotalDays - 28).ToString();
            _ = Varnish.TakeOut(cab);
        }));
        foreach (var item in who.Items.Where(PaintingItems.IsPainting).ToList())
            who.removeItemFromInventory(item);
        who.addItemToInventory(Blank());
    }

    // Sable as one of several spouses (as Polyamory Sweet allows): married to
    // Sable while the farmer's one spouse field names someone else.
    private static void PolyTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var location = who.currentLocation;
        string oldSpouse = who.spouse;
        who.friendshipData.TryGetValue("Sable", out var oldSable);
        who.friendshipData.TryGetValue("Abigail", out var oldAbigail);
        try
        {
            who.spouse = "Abigail";
            who.friendshipData["Abigail"] = new Friendship(2500) { Status = FriendshipStatus.Married, WeddingDate = Game1.Date };
            who.friendshipData["Sable"] = new Friendship(3500) { Status = FriendshipStatus.Married, WeddingDate = Game1.Date };
            Check(monitor, "a second spouse: the 14-heart event's married condition holds",
                location.checkEventPrecondition("test/G PLAYER_NPC_RELATIONSHIP Current Sable Married", check_seen: false) != "-1");
            Game1.exitActiveMenu();
            Shop.ArtShop.OpenShop(location);
            Check(monitor, "a second spouse: the shop opens on trust", Game1.activeClickableMenu is ShopMenu);
            Game1.exitActiveMenu();

            who.friendshipData["Sable"] = new Friendship(3500) { Status = FriendshipStatus.Dating };
            Check(monitor, "only dating: the married condition doesn't hold",
                location.checkEventPrecondition("test/G PLAYER_NPC_RELATIONSHIP Current Sable Married", check_seen: false) == "-1");
            Shop.ArtShop.OpenShop(location);
            Check(monitor, "only dating: no shop on trust away from the counter", Game1.activeClickableMenu is not ShopMenu);
            Game1.exitActiveMenu();
        }
        finally
        {
            who.spouse = oldSpouse;
            if (oldSable != null) who.friendshipData["Sable"] = oldSable; else who.friendshipData.Remove("Sable");
            // (Left as a plain entry rather than removed: Polyamory Sweet keeps
            // a list of spouses and looks each one up.)
            who.friendshipData["Abigail"] = oldAbigail ?? new Friendship(0);
        }
    }

    // Automate and the Varnishing Cabinet (needs Automate installed): run
    // 'museart_automatetest', then 'check' a few seconds later, then 'cured'
    // a few seconds after that.
    private static Vector2 _autoCabinet, _autoChest;
    private static void AutomateTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var house = who.currentLocation;
        string stage = args.FirstOrDefault() ?? "";
        if (stage == "")
        {
            Check(monitor, "Automate is installed", _helper.ModRegistry.IsLoaded(Integrations.AutomateIntegration.ModId));
            FreshStart(who);
            _autoCabinet = who.Tile + new Vector2(2, 0);
            _autoChest = _autoCabinet + new Vector2(1, 0);
            house.objects.Remove(_autoCabinet);
            house.objects.Remove(_autoChest);
            var cabinet = ItemRegistry.Create<SObject>(Varnish.QualifiedCabinetId);
            cabinet.placementAction(house, (int)_autoCabinet.X * 64, (int)_autoCabinet.Y * 64, who);
            var chest = new Chest(true) { TileLocation = _autoChest };
            house.objects.Add(_autoChest, chest);
            var m = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { "(O)254" });
            m.DevPaintPartly(1f, 3);
            m.DevFinish(false);
            var painting = m.DevPainting!;
            who.removeItemFromInventory(painting);
            chest.addItem(painting);
            chest.addItem(ItemRegistry.Create("(O)" + Varnish.VarnishId, 1));
            // And a Pigment Mill on the chest's other side, with clay to grind.
            var millTile = _autoChest + new Vector2(1, 0);
            house.objects.Remove(millTile);
            ItemRegistry.Create<SObject>(ArtItems.QualifiedMillId).placementAction(house, (int)millTile.X * 64, (int)millTile.Y * 64, who);
            chest.addItem(ItemRegistry.Create("(O)330", 1));
            monitor.Log($"Cabinet at {_autoCabinet}, chest at {_autoChest} with a painting, a tin and clay, mill at {millTile}.", LogLevel.Info);
            return;
        }
        var cab = house.objects.TryGetValue(_autoCabinet, out var c) ? c : null;
        var box = house.objects.TryGetValue(_autoChest, out var b) ? b as Chest : null;
        if (cab == null || box == null)
        {
            monitor.Log("Run museart_automatetest first.", LogLevel.Error);
            return;
        }
        if (stage == "check")
        {
            Check(monitor, "Automate loads the cabinet from the chest", cab.heldObject.Value != null && PaintingItems.IsPainting(cab.heldObject.Value));
            Check(monitor, "...spending the chest's painting and tin", !box.Items.Any(i => i != null && (PaintingItems.IsPainting(i) || Varnish.IsVarnish(i))),
                string.Join(", ", box.Items.Where(i => i != null).Select(i => i.DisplayName)));
            var mill = house.objects.TryGetValue(_autoChest + new Vector2(1, 0), out var mo) ? mo : null;
            Check(monitor, "Automate runs the Pigment Mill too", mill?.heldObject.Value != null, mill?.heldObject.Value?.DisplayName ?? "empty");
            // Four weeks on.
            cab.modData[ModEntry.Id + "/VarnishStart"] = (Game1.Date.TotalDays - 28).ToString();
        }
        else if (stage == "cured")
        {
            var back = box.Items.FirstOrDefault(PaintingItems.IsPainting);
            Check(monitor, "a cured painting goes back to the chest", back != null && cab.heldObject.Value == null);
            Check(monitor, "...with the gallery finish", back != null && Varnish.FinishOf(back) == "gallery", back == null ? "none" : Varnish.FinishOf(back) ?? "no finish");
        }
    }

    private static void VarnishTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        FreshStart(who);
        var house = who.currentLocation;
        foreach (var item in who.Items.Where(PaintingItems.IsPainting).ToList())
            who.removeItemFromInventory(item);

        // The cabinet beside the farmer.
        var tile = who.Tile + new Vector2(2, 0);
        house.objects.Remove(tile);
        var cabinet = ItemRegistry.Create<SObject>(Varnish.QualifiedCabinetId);
        Check(monitor, "the cabinet can be placed", cabinet.placementAction(house, (int)tile.X * 64, (int)tile.Y * 64, who));
        cabinet = house.objects[tile];

        SObject Painting()
        {
            var m = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { "(O)254" });
            m.DevPaintPartly(1f, 3);
            m.DevFinish(false);
            return m.DevPainting!;
        }
        int Debris(Func<Item, bool> match) => house.debris.Count(d => d.item != null && match(d.item));

        // Without a tin it won't take the painting.
        foreach (var tin in who.Items.Where(Varnish.IsVarnish).ToList())
            who.removeItemFromInventory(tin);
        var first = Painting();
        who.addItemToInventory(first);
        who.CurrentToolIndex = who.Items.IndexOf(first);
        house.checkAction(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport, who);
        Game1.exitActiveMenu();
        Check(monitor, "no varnish, no varnishing", cabinet.heldObject.Value == null && who.Items.Contains(first));

        // With one, a right-click while holding the painting puts it in.
        who.addItemToInventory(ItemRegistry.Create("(O)" + Varnish.VarnishId, 2));
        who.CurrentToolIndex = who.Items.IndexOf(first);
        int basePrice = first.Price;
        house.checkAction(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport, who);
        Check(monitor, "holding a painting, the cabinet takes it and a tin", cabinet.heldObject.Value != null && !who.Items.Contains(first)
            && who.Items.Where(Varnish.IsVarnish).Sum(i => i.Stack) == 1, $"{who.Items.Where(Varnish.IsVarnish).Sum(i => i.Stack)} tins left");

        // Ten days on: satin; a right-click only tells you so; a pickaxe takes it out.
        cabinet.modData[ModEntry.Id + "/VarnishStart"] = (Game1.Date.TotalDays - 10).ToString();
        cabinet.checkForAction(who);
        Game1.exitActiveMenu();
        Check(monitor, "before it's cured a right-click leaves it in", cabinet.heldObject.Value != null);
        cabinet.performToolAction(ItemRegistry.Create<Tool>("(T)Pickaxe"));
        Check(monitor, "a tool takes it out early and leaves the cabinet", cabinet.heldObject.Value == null && house.objects.ContainsKey(tile));
        var satin = house.debris.Select(d => d.item).OfType<SObject>().LastOrDefault(PaintingItems.IsPainting);
        Check(monitor, "after 10 days it has a satin finish, worth more", satin != null && Varnish.FinishOf(satin) == "satin" && satin.Price == (int)Math.Round(basePrice * Varnish.ValueOf("satin")),
            satin == null ? "nothing dropped" : $"{Varnish.FinishOf(satin)}, {basePrice}g -> {satin.Price}g");
        house.debris.Clear();
        if (satin != null)
            Check(monitor, "a varnished painting can't be varnished again", !cabinet.performObjectDropInAction(satin, true, who));

        // Taken out on day 3: no finish, the tin is wasted.
        var early = Painting();
        Varnish.Load(cabinet, early, who);
        cabinet.modData[ModEntry.Id + "/VarnishStart"] = (Game1.Date.TotalDays - 3).ToString();
        var unvarnished = Varnish.TakeOut(cabinet);
        Check(monitor, "out before 7 days, it's unvarnished", unvarnished != null && Varnish.FinishOf(unvarnished) == null && unvarnished.Price == early.Price);

        // Four weeks: ready, and a right-click hands it over.
        who.addItemToInventory(ItemRegistry.Create("(O)" + Varnish.VarnishId, 1));
        var last = Painting();
        Varnish.Load(cabinet, last, who);
        cabinet.modData[ModEntry.Id + "/VarnishStart"] = (Game1.Date.TotalDays - 28).ToString();
        Varnish.MarkCured();
        Check(monitor, "after 28 days it shows as ready", cabinet.readyForHarvest.Value);
        cabinet.checkForAction(who);
        var gallery = who.Items.OfType<SObject>().FirstOrDefault(i => PaintingItems.IsPainting(i) && Varnish.FinishOf(i) == "gallery");
        Check(monitor, "the cured painting comes out with a gallery finish, worth more again", gallery != null && gallery.Price == (int)Math.Round(last.Price * Varnish.ValueOf("gallery")) && cabinet.heldObject.Value == null,
            gallery == null ? "none in the bag" : $"{last.Price}g -> {gallery.Price}g");
        if (gallery != null)
        {
            Check(monitor, "its description says so", string.Join(" ", gallery.getDescription().Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries)).Contains(I18n.Get("varnish.finish.gallery")));
            Check(monitor, "framing keeps the finish", Varnish.FinishOf(ArtHooks.Frame(gallery)) == "gallery");
        }

        // Leave one curing for the screenshot.
        who.addItemToInventory(ItemRegistry.Create("(O)" + Varnish.VarnishId, 1));
        Varnish.Load(cabinet, Painting(), who);
    }

    // The record, consignment and plein air (2026-10-25).
    private static void GalleryTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        FreshStart(who);
        ModEntry.Config.Difficulty = Difficulty.Relaxed;     // no paint needed for these
        ArtProgress.SetLevel(who, 6);
        string parsnip = "(O)24", melon = "(O)254";
        // Room in the bag: the paintings these make pile up.
        void ClearPaintings()
        {
            foreach (var item in who.Items.Where(PaintingItems.IsPainting).ToList())
                who.removeItemFromInventory(item);
        }
        ClearPaintings();
        switch (args.FirstOrDefault())
        {
            case "record":
            {
                who.modData.Remove(ModEntry.Id + "/Catalogue");
                who.modData.Remove(ModEntry.Id + "/CollectionPerks");
                int Paint(params string[] ids)
                {
                    var m = EaselMenu.StartStillLife(who, CanvasKind.Small, ids);
                    Game1.activeClickableMenu = m;
                    m.DevPaintPartly(1f, 3);
                    m.DevFinish(false);
                    int price = m.DevPainting?.Price ?? 0;
                    m.DevKeep();
                    Game1.exitActiveMenu();
                    ClearPaintings();
                    return price;
                }
                int first = Paint(parsnip), second = Paint(parsnip), third = Paint(parsnip), fourth = Paint(parsnip);
                Check(monitor, "a new subject pays 25% more than the second time", Math.Abs(first - second * 1.25f) <= 2, $"{first}g then {second}g");
                Check(monitor, "the record counts every painting", Catalogue.TimesPainted(who, parsnip) == 4, Catalogue.TimesPainted(who, parsnip).ToString());
                Check(monitor, "the fourth of the same in a week pays less", fourth < second, $"{fourth}g vs {second}g");
                // Next week, it's full price again.
                var record = Catalogue.All(who);
                record[parsnip].Week -= 1;
                who.modData[ModEntry.Id + "/Catalogue"] = System.Text.Json.JsonSerializer.Serialize(record);
                int nextWeek = Paint(parsnip);
                Check(monitor, "a new week resets it to full price", nextWeek == second, $"{nextWeek}g vs {second}g");
                Paint(melon, parsnip);
                Check(monitor, "a still life in another order is the same subject", Catalogue.TimesPainted(who, parsnip + "|" + melon) == 1 && Catalogue.All(who).Count == 2);

                int tubesBefore = who.Items.Where(i => ArtItems.TubeFor(i) == Palette.IndexOf("aquamarine")).Sum(i => i.Stack);
                foreach (string id in new[] { "(O)188", "(O)190" })
                    Paint(id);
                Check(monitor, "no milestone before five subjects", !Catalogue.HasPerk(who, "tubes"));
                Paint("(O)192");
                int tubesAfter = who.Items.Where(i => ArtItems.TubeFor(i) == Palette.IndexOf("aquamarine")).Sum(i => i.Stack);
                Check(monitor, "five subjects bring three tubes of aquamarine", Catalogue.HasPerk(who, "tubes") && tubesAfter - tubesBefore == 3, $"{tubesAfter - tubesBefore} tubes");

                // The later milestones, by filling the record by hand.
                var all = Catalogue.All(who);
                for (int i = all.Count; i < 14; i++)
                    all["(O)" + (300 + i)] = new CatalogueEntry { Count = 1, FirstDay = 1 };
                who.modData[ModEntry.Id + "/Catalogue"] = System.Text.Json.JsonSerializer.Serialize(all);
                Paint("(O)400");
                var wooden = ItemRegistry.Create("(O)" + ArtItems.PaletteId);
                Check(monitor, "fifteen subjects make the palette's wells bigger", PaletteItem.Capacity(wooden, who) == (int)(3 * PaletteItem.TubeCells * PaletteItem.BigWells), PaletteItem.Capacity(wooden, who).ToString());
                Game1.activeClickableMenu = new Menus.CollectionMenu(who);
                Check(monitor, "the record page opens", Game1.activeClickableMenu is Menus.CollectionMenu);
                break;
            }
            case "consign":
            {
                foreach (var item in Shop.Consignment.All(who).ToList())
                    Shop.Consignment.Consigned(who).Remove(item);
                var m = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { melon });
                m.DevPaintPartly(1f, 3);
                m.DevFinish(false);
                var painting = m.DevPainting!;
                who.addItemToInventory(painting);
                int shopPrice = painting.sellToStorePrice();
                Check(monitor, "consigning takes it from the bag", Shop.Consignment.Consign(who, painting) && !who.Items.Contains(painting) && Shop.Consignment.All(who).Contains(painting));
                Check(monitor, "a buyer pays more than the shop price", Shop.Consignment.Price(painting) == (int)Math.Round(shopPrice * Market.ConsignPremium), $"{Shop.Consignment.Price(painting)}g vs {shopPrice}g");
                Check(monitor, "it sells within three days", Shop.Consignment.DaysLeft(painting) is >= 1 and <= 3, Shop.Consignment.DaysLeft(painting).ToString());
                bool took = Shop.Consignment.TakeBack(who, painting);
                Check(monitor, "you can take it back", took && who.Items.Contains(painting) && !Shop.Consignment.All(who).Any(),
                    $"took {took}, in bag {who.Items.Contains(painting)}, still consigned {Shop.Consignment.All(who).Count()}, free slots {who.freeSpotsInInventory()}");
                for (int i = 0; i < 4; i++)
                {
                    var copy = painting.getOne();
                    who.addItemToInventory(copy);
                    Shop.Consignment.Consign(who, copy);
                }
                Check(monitor, "three easels at most", Shop.Consignment.All(who).Count() == Shop.Consignment.MaxConsigned);
                foreach (var item in Shop.Consignment.All(who))
                    item.modData[ModEntry.Id + "/SellsOn"] = Game1.Date.TotalDays.ToString();
                int money = who.Money, expected = Shop.Consignment.All(who).Sum(Shop.Consignment.Price);
                Shop.Consignment.SellDue(who);
                Check(monitor, "when the day comes they sell and pay", who.Money - money == expected && !Shop.Consignment.All(who).Any(), $"+{who.Money - money}g of {expected}g");
                var last = painting.getOne();
                who.addItemToInventory(last);
                Shop.Consignment.Consign(who, last);
                Game1.activeClickableMenu = new Shop.ConsignMenu(who);
                Check(monitor, "the consign menu opens", Game1.activeClickableMenu is Shop.ConsignMenu);
                break;
            }
            case "view":
            {
                var tile = who.Tile + new Vector2(0, -1);
                var location = who.currentLocation;
                Check(monitor, "standing outdoors", location.IsOutdoors, location.NameOrUniqueName);
                var m = new EaselMenu(who, easelTile: tile);
                Game1.activeClickableMenu = m;
                Check(monitor, "an easel outdoors offers the view", m.DevSitterOffered(Views.Prefix));
                Check(monitor, "an easel indoors doesn't", !new EaselMenu(who).DevSitterOffered(Views.Prefix));
                m.DevPickView();
                var preview = m.DevPreview;
                Check(monitor, "the view is taken and becomes the reference", m.DevViewSubject == Views.SubjectFor(location) && preview != null,
                    m.DevViewSubject ?? "none");
                if (preview != null)
                    Check(monitor, "it's a real picture filling the canvas", preview.PigmentsUsed.Count >= 4 && preview.SubjectCells == preview.Size * preview.Size,
                        $"{preview.PigmentsUsed.Count} colours, difficulty {preview.Difficulty}");
                int pending = ArtHooks.PendingMinutes;
                m.DevSetCanvas(CanvasKind.Small);
                who.addItemToInventory(ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Small)));
                m.DevBeginFromSetup(Views.SubjectFor(location));
                Check(monitor, "the view starts painting", m.DevIsPainting);
                m.DevPaintPartly(1f, 3);
                m.DevFinish(false);
                var painting = m.DevPainting;
                Check(monitor, "a view takes an hour longer", ArtHooks.PendingMinutes - pending == 120, $"{ArtHooks.PendingMinutes - pending} minutes");
                Check(monitor, "it's named for the place and season", painting?.DisplayName == I18n.Get("painting.title.view", new { place = location.DisplayName, season = Utility.getSeasonNameFromNumber(Game1.seasonIndex) }), painting?.DisplayName ?? "none");
                Check(monitor, "and recorded", Catalogue.TimesPainted(who, Views.SubjectFor(location)) >= 1);
                monitor.Log($"View painting: {painting?.DisplayName}, {painting?.Price}g, quality {painting?.Quality}.", LogLevel.Info);
                break;
            }
        }
        ModEntry.Config.Difficulty = Difficulty.Normal;
    }

    // Part two of the paint rework (docs/painting_depth.md).
    private static void PaintTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        if (args.FirstOrDefault() != "check")
            FreshStart(Game1.player);
        var who = Game1.player;
        int charcoal = Palette.IndexOf("charcoal"), sienna = Palette.IndexOf("sienna");
        if (args.FirstOrDefault() != "check")
        {
            ModEntry.Config.Difficulty = Difficulty.Normal;
            ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter));
            foreach (var old in who.Items.Where(i => ArtItems.IsPalette(i) || ArtItems.TubeFor(i) != null).ToList())
                who.removeItemFromInventory(old);
            // A fresh palette, checked before any tubes arrive.
            var fresh = ItemRegistry.Create("(O)" + ArtItems.PaletteId);
            PaletteItem.Initialize(fresh);
            Check(monitor, "a new palette comes with the starter colours",
                Enumerable.Range(0, Palette.All.Length).All(i => PaletteItem.Read(fresh)[i] == (Palette.All[i].Starter ? PaletteItem.TubeCells : 0)));
            who.addItemToInventory(fresh);
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(charcoal), 3));
            who.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(sienna)));
            return;                         // the pickup check runs on the next ticks
        }

        var palette = PaletteItem.Find(who);
        Check(monitor, "a tube of a new colour teaches it", ArtProgress.Owns(who, sienna));
        if (palette == null) return;

        var menu = new EaselMenu(who);
        var paint = PaletteItem.Read(palette);
        Check(monitor, "tubes go into the palette, up to a full well", paint[charcoal] == PaletteItem.Capacity(palette, who)
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
        // What's on the yard easel (each of the six, not just today's), and
        // Sable's lines from the mod itself (watching them, married life).
        for (int i = 0; i < 6; i++)
            texts.Add(($"Strings/schedules/Sable:yardEasel[{i}]", "Sable", I18nCp("yard.painting." + i)));
        texts.Add(("yard.watch.done", "Sable", I18n.Get("yard.watch.done")));
        texts.Add(("married.farm-painting", "Sable", I18n.Get("married.farm-painting")));
        for (int i = 0; i < 4; i++)
            texts.Add(($"married.wall.{i}", "Sable", I18n.Get("married.wall." + i, new { painting = "Still Life: Pomegranate and Fairy Rose" })));
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
        // 'museart_marry npc:Abigail' marries a vanilla villager instead, to
        // compare how other mods treat Sable's room with a vanilla one.
        string name = args.FirstOrDefault(a => a.StartsWith("npc:"))?.Substring(4) ?? "Sable";
        var sable = Game1.getCharacterFromName(name);
        who.friendshipData[name] = new Friendship(2500) { Status = FriendshipStatus.Married, WeddingDate = Game1.Date };
        who.spouse = name;
        who.HouseUpgradeLevel = 1;
        var house = Utility.getHomeOfFarmer(who);
        house.setMapForUpgradeLevel(1);
        house.showSpouseRoom();
        Game1.getFarm().addSpouseOutdoorArea(name);
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
            case "askboard":
                // The board with requests asking for a varnish and glazing,
                // the longest texts they make (posted and taken).
                Shop.Commissions.Save(Game1.player, new System.Collections.Generic.List<Shop.Commission>
                {
                    new() { Npc = "Demetrius", ItemId = "(O)637", MinQuality = PaintingRules.Silver, Reward = 1890, ExpiresOn = Game1.Date.TotalDays + 2, Requirement = Shop.Commissions.Varnished },
                    new() { Npc = "Elliott", ItemId = "(O)595", MinQuality = PaintingRules.Iridium, Reward = 2440, ExpiresOn = Game1.Date.TotalDays + 2, Requirement = Shop.Commissions.Glazed },
                    new() { Npc = "Caroline", ItemId = "(O)637", MinQuality = PaintingRules.Silver, Reward = 1890, ExpiresOn = Game1.Date.TotalDays + 13, Accepted = true, Requirement = Shop.Commissions.Varnished },
                    new() { Npc = "Maru", ItemId = "(O)595", MinQuality = PaintingRules.Silver, Reward = 2440, ExpiresOn = Game1.Date.TotalDays + 6, Accepted = true, Requirement = Shop.Commissions.Glazed },
                });
                Game1.activeClickableMenu = new Shop.CommissionBoardMenu(Game1.player);
                break;
            case "record":
                Game1.activeClickableMenu = new Menus.CollectionMenu(Game1.player);
                break;
            case "palette":
                // A Palette's storage, as if it were set down (museart_menu palette).
                if (PaletteItem.Find(Game1.player) is Item palette)
                    Game1.activeClickableMenu = new PaletteMenu(Game1.player, palette);
                break;
            case "consign":
                Game1.activeClickableMenu = new Shop.ConsignMenu(Game1.player);
                break;
            case "map":
                // The world map page (museart_menu map).
                Game1.activeClickableMenu = new GameMenu(GameMenu.mapTab);
                break;
            case "view":
                // The easel outdoors, the view picked.
                var outdoors = new EaselMenu(Game1.player, easelTile: Game1.player.Tile + new Vector2(0, -1));
                outdoors.DevPickView();
                Game1.activeClickableMenu = outdoors;
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
