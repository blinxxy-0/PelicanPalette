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
        DevTools.AddCommand(helper, "museart_margottest", "Margot Pell (1.3): her arrival after the first old painting, her data, sheets, gifts, dialogue and schedule walks.", (_, _) => MargotTest(monitor));
        DevTools.AddCommand(helper, "museart_npctest", "Checks Sable: data, sprites, gift tastes, and that every schedule entry finds its routes.", (_, _) => NpcTest(monitor));
        DevTools.AddCommand(helper, "museart_where", "Logs where Sable (or another villager) is and what they're doing. Usage: museart_where [JulianVane]", (_, args) =>
        {
            var sable = Game1.getCharacterFromName(args.FirstOrDefault() ?? "Sable");
            if (sable == null) return;
            monitor.Log($"{Game1.timeOfDay}: {sable.Name} in {sable.currentLocation?.Name} at {sable.TilePoint}, facing {sable.FacingDirection}, "
                + $"{(sable.isMoving() ? "walking" : "still")}, schedule '{sable.ScheduleKey}', path {(sable.controller?.pathToEndPoint?.Count.ToString() ?? "none")}, "
                + $"queued {sable.queuedSchedulePaths.Count}, tried {sable.lastAttemptedSchedule}, invisible {sable.IsInvisible}, "
                + $"warps here: {string.Join(" ", sable.currentLocation?.warps.Select(w => $"{w.X},{w.Y}>{w.TargetName}") ?? Array.Empty<string>())}", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_schedule", "Logs a villager's schedule legs and whether the first one's path can be found from where they stand. Usage: museart_schedule JulianVane", (_, args) =>
        {
            if (Game1.getCharacterFromName(args.FirstOrDefault() ?? "Sable") is not { } npc || npc.Schedule == null) return;
            foreach (var (time, leg) in npc.Schedule)
                monitor.Log($"{time}: {leg.targetLocationName} {leg.targetTile} via {string.Join(">", leg.route ?? new System.Collections.Generic.Stack<Point>())}", LogLevel.Info);
            var first = npc.Schedule.Values.First();
            var controller = new StardewValley.Pathfinding.PathFindController(first.route, npc, npc.currentLocation);
            monitor.Log($"From {npc.currentLocation?.Name} {npc.TilePoint}: first path {(controller.pathToEndPoint?.Count.ToString() ?? "not found")} steps; "
                + $"standing tile passable {npc.currentLocation?.isTilePassable(npc.Tile)}", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_day", "Early morning only: makes today another day of the month and reloads Sable's (or another villager's) schedule from their home. Usage: museart_day 3 [JulianVane]", (_, args) =>
        {
            var sable = Game1.getCharacterFromName(args.ElementAtOrDefault(1) ?? "Sable");
            if (sable == null || args.Length == 0 || !int.TryParse(args[0], out int day)) return;
            Game1.dayOfMonth = day;
            Game1.warpCharacter(sable, sable.DefaultMap, sable.DefaultPosition / 64f);
            sable.TryLoadSchedule();
            if (sable.Name == Social.Julian.Name)
                Social.Julian.StartVisit(day);          // (as the mod does on a visit morning)
            monitor.Log($"Now {Game1.shortDayNameFromDayOfSeason(day)} {day}; {sable.Name}'s schedule '{sable.ScheduleKey}'.", LogLevel.Info);
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
        DevTools.AddCommand(helper, "museart_recordtest", "The painting record's tabs (1.2): what goes where, silhouettes, and tab rewards.", (_, _) => RecordTest(monitor));
        DevTools.AddCommand(helper, "museart_portfoliotest", "The portfolio book and the plan chest (1.2): slots, swaps, finished pages, and the locks.", (_, args) => PortfolioTest(monitor, args));
        DevTools.AddCommand(helper, "museart_collectiontest", "Painting Mastery's bar and easel, and the museum's collection of restored paintings (1.3). 'view' opens the collection; 'frames' the framing screen with every style.", (_, args) => CollectionTest(monitor, args));
        DevTools.AddCommand(helper, "museart_oldtest", "Old paintings (1.3): the 21 pictures and their damage, finding them, and restoring one at the easel. With 'show', opens a restoration to look at.", (_, args) => OldTest(monitor, args));
        DevTools.AddCommand(helper, "museart_ordertest", "Special orders (1.3): the painting tags, the orders' data, and Lewis's portraits delivered.", (_, _) => OrderTest(monitor));
        DevTools.AddCommand(helper, "museart_festtest", "Festivals, round two (1.3): run at Spirit's Eve or the Jellies (museart_festival fall27 / summer28) to paint there; with 'gift', Sable's Winter Star painting.", (_, args) => FestTest(helper, monitor, args));
        DevTools.AddCommand(helper, "museart_booktest", "The finished portfolio (1.3): Faces of the Valley, the gold-leaf frame, the Summit page and scene, and Julian buying a painting. Then 'museart_booktest scene' to watch the Summit scene.", (_, args) => BookTest(monitor, args));
        DevTools.AddCommand(helper, "museart_animaltest", "Animals: your pet, horse and farm animals as subjects, and the record's Animals tab (1.3).", (_, _) => AnimalTest(monitor));
        DevTools.AddCommand(helper, "museart_creaturetest", "Creatures (monsters, Junimos), Krobus and the Wizard, and the far places (1.2).", (_, _) => CreatureTest(monitor));
        DevTools.AddCommand(helper, "museart_stalltest", "Sable's Night Market stall (1.2). Throwaway farm: 'museart_stalltest' goes to the market on Winter 15 at 6pm, then 'museart_stalltest check' a few seconds later.", (_, args) => StallTest(monitor, args));
        DevTools.AddCommand(helper, "museart_festactors", "At a festival: logs every villager and where they stand, for finding a free spot.", (_, _) =>
        {
            if (Game1.CurrentEvent is not { } ev) return;
            monitor.Log("Festival actors: " + string.Join(", ", ev.actors.OrderBy(a => a.TilePoint.Y).ThenBy(a => a.TilePoint.X).Select(a => $"{a.Name} {a.TilePoint.X},{a.TilePoint.Y}")), LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_contesttest", "The Fair painting contest (1.2): scoring, rivals, prizes. Run at the Fair (museart_festival fall16) to test entering and judging there too.", (_, args) => ContestTest(monitor, args));
        DevTools.AddCommand(helper, "museart_juliantest", "Julian Vane (1.2): his data, art, visits, and comings and goings.", (_, _) => JulianTest(monitor));
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
        DevTools.GivePalette(who);
        if (Count(canvasId) == 0)
            who.addItemToInventory(ItemRegistry.Create(canvasId, 2));
        int canvasesBefore = Count(canvasId), xpBefore = ArtProgress.Xp(who);
        var brushItem = who.Items.FirstOrDefault(i => ArtItems.BrushFor(i) != null);
        int usesBefore = brushItem == null ? -1 : ArtHooks.UsesLeft(brushItem, ArtItems.BrushFor(brushItem)!);
        var menu = new Menus.EaselMenu(who);
        Game1.activeClickableMenu = menu;
        menu.DevBeginFromSetup("(O)24");
        Check(monitor, "Begin starts painting", menu.DevIsPainting, menu.DevWhyNot);
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
            Check(monitor, "...and takes a glazed one (from before 1.1.5)", Delivered(Shop.Commissions.Glazed, Painting(marks)));
            var stray = Painting(marks);
            stray.modData[ModEntry.Id + "/Glazed"] = "0";
            Check(monitor, "a stray glaze isn't a glazed painting", !Delivered(Shop.Commissions.Glazed, stray));
            var glazed = Painting(marks);
            glazed.modData[ModEntry.Id + "/Glazed"] = "1";
            Check(monitor, "one glazed to its reference is", Delivered(Shop.Commissions.Glazed, glazed));

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
        var back = Game1.getLocationFromName(Shop.ArtShop.BackRoom);
        var room = Game1.getLocationFromName(Shop.ArtShop.Basement);
        if (args.FirstOrDefault() == "down")
        {
            back!.performAction("LockedDoorWarp 1 2 " + Shop.ArtShop.Basement + " 600 2600 Sable 500", who, new xTile.Dimensions.Location(9, 7));
            return;
        }
        if (args.FirstOrDefault() == "check")
        {
            Check(monitor, "the trapdoor takes you down to the foot of Sable's stairs", who.currentLocation == room && who.TilePoint == new Point(1, 2), $"{who.currentLocation?.Name} {who.TilePoint}");
            Check(monitor, "once let in, the trapdoor stays open", who.mailReceived.Contains("doorUnlockSable"));
            return;
        }
        Check(monitor, "Sable's room and the back room exist", room != null && back != null);
        if (shop == null || room == null || back == null) return;
        Check(monitor, "the shop's back wall has a door to the back room", shop.doors.TryGetValue(new Point(12, 3), out var through) && through == Shop.ArtShop.BackRoom);
        Check(monitor, "the back room's doorway leads back into the shop", back.warps.Any(w => w.TargetName == Shop.ArtShop.LocationName && w.TargetX == 12 && w.TargetY == 4) && shop.isTilePassable(new Vector2(12, 4)));
        Check(monitor, "the trapdoor is in the back room, a door down to Sable's room", back.doors.TryGetValue(new Point(9, 7), out var to) && to == Shop.ArtShop.Basement);
        Check(monitor, "the shop floor has no trapdoor any more", shop.map.GetLayer("Buildings").Tiles[18, 9] == null);
        var stairs = room.warps.FirstOrDefault();
        Check(monitor, "the stairs lead up to beside the trapdoor", stairs != null && stairs.TargetName == Shop.ArtShop.BackRoom && stairs.TargetX == 8 && stairs.TargetY == 7 && back.isTilePassable(new Vector2(8, 7)), $"{stairs?.TargetName} {stairs?.TargetX},{stairs?.TargetY}");
        Check(monitor, "the book and the plan chest have their actions",
            back.doesTileHaveProperty(6, 4, "Action", "Buildings")?.Contains("_Portfolio") == true && back.doesTileHaveProperty(3, 4, "Action", "Buildings")?.Contains("_PlanChest") == true);
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
        back.performAction("LockedDoorWarp 1 2 " + Shop.ArtShop.Basement + " 600 2600 Sable 500", who, new xTile.Dimensions.Location(9, 7));
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

    // Margot Pell (1.3, step 6).
    private static void MargotTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        // Her arrival: nothing before a find, the letter after one, then her a week on.
        Social.Margot.DevReset();
        string found = who.modData.TryGetValue(ModEntry.Id + "/OldFound", out string f) ? f : null;
        who.modData.Remove(ModEntry.Id + "/OldFound");
        Social.Margot.Update();
        Check(monitor, "no letter before any old painting is found", !who.mailForTomorrow.Contains(Social.Margot.LetterFlag) && Social.Margot.Npc == null);
        who.modData[ModEntry.Id + "/OldFound"] = "valley_mill";
        Social.Margot.Update();
        Check(monitor, "Gunther's letter comes the morning after a find", who.mailForTomorrow.Contains(Social.Margot.LetterFlag)
            && DataLoader.Mail(Game1.content).ContainsKey(Social.Margot.LetterFlag));
        Social.Margot.Update();
        Check(monitor, "...and she isn't here yet", Social.Margot.Npc == null && !Social.Margot.Arrived);
        Game1.MasterPlayer.modData[ModEntry.Id + "/MargotDue"] = Game1.Date.TotalDays.ToString();
        Social.Margot.Update();
        var margot = Social.Margot.Npc;
        Check(monitor, "a week after the letter, she arrives", margot != null && Social.Margot.Arrived);
        if (found == null) who.modData.Remove(ModEntry.Id + "/OldFound"); else who.modData[ModEntry.Id + "/OldFound"] = found;
        if (margot == null) return;

        Check(monitor, "she lives in her room down the Saloon storeroom's steps", margot.DefaultMap == "blinxxy.MuseArt_MargotRoom", $"{margot.DefaultMap} {margot.DefaultPosition / 64}");
        Check(monitor, "her sheets: sprites 64x384, portraits 128x256", margot.Sprite.Texture.Width == 64 && margot.Sprite.Texture.Height == 384
            && margot.Portrait?.Width == 128 && margot.Portrait?.Height == 256);
        Check(monitor, "she's romanceable, she/her", margot.datable.Value && margot.Gender == StardewValley.Gender.Female);
        var outfits = new[] { "Winter", "Beach", "Wedding" }.Where(o =>
            Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Characters/MargotPell_" + o).Height == 384 && Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Portraits/MargotPell_" + o).Height == 256).ToList();
        Check(monitor, "her winter, beach and wedding outfits load", outfits.Count() == 3, string.Join(", ", outfits));
        int Taste(string id) => margot.getGiftTasteForThisItem(ItemRegistry.Create(id));
        Check(monitor, "she loves green tea, likes coffee, hates slime", Taste("(O)614") == NPC.gift_taste_love && Taste("(O)395") == NPC.gift_taste_like && Taste("(O)766") == NPC.gift_taste_hate);
        var dialogue = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Characters/Dialogue/MargotPell");
        Check(monitor, "she has an introduction and a line for every day", dialogue.ContainsKey("Introduction") && new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }.All(dialogue.ContainsKey));

        // Every schedule entry is walkable, and she's at the museum by 9.
        var home = margot.currentLocation;
        var homeTile = margot.TilePoint;
        foreach (var (key, script) in margot.getMasterScheduleRawData())
        {
            var problems = new System.Collections.Generic.List<string>();
            string from = margot.DefaultMap;
            var at = (margot.DefaultPosition / 64).ToPoint();
            foreach (var stop in margot.parseMasterSchedule(key, script).OrderBy(p => p.Key).Select(p => p.Value))
            {
                int tiles = 0;
                var locations = from == stop.targetLocationName ? new[] { from }
                    : StardewValley.Pathfinding.WarpPathfindingCache.GetLocationRoute(from, stop.targetLocationName, margot.Gender);
                if (locations == null) { problems.Add($"{stop.time}: no way from {from} to {stop.targetLocationName}"); continue; }
                var start = at;
                for (int i = 0; i < locations.Length; i++)
                {
                    var location = Game1.RequireLocation(locations[i]);
                    var end = i < locations.Length - 1 ? location.getWarpPointTo(locations[i + 1]) : stop.targetTile;
                    var path = StardewValley.Pathfinding.PathFindController.findPathForNPCSchedules(start, end, location, 30000, margot);
                    if (path == null) problems.Add($"{stop.time}: no path in {location.Name} from {start} to {end}");
                    else tiles += path.Count;
                    if (i < locations.Length - 1) start = location.getWarpPointTarget(end, margot);
                }
                if (stop.targetLocationName == "ArchaeologyHouse")
                {
                    int arrives = Utility.ModifyTime(stop.time, WalkMinutes(tiles));
                    Check(monitor, $"schedule '{key}': at the museum by 9", arrives <= 900, $"leaves {stop.time}, {tiles} tiles, there at {arrives}");
                }
                from = stop.targetLocationName;
                at = stop.targetTile;
            }
            Check(monitor, $"schedule '{key}' is walkable", problems.Count == 0, string.Join("; ", problems));
        }
        Game1.warpCharacter(margot, home, homeTile.ToVector2());
        margot.TryLoadSchedule();

        // Her move (4 hearts): Robin's three days, the flag the night before,
        // then the cottage is home.
        Social.MargotEvents.DevReset();
        Social.MargotEvents.Commission();
        Social.MargotEvents.FinishBuilding();
        Check(monitor, "the cottage isn't built the day it's commissioned", !Social.MargotEvents.CottageBuilt);
        Game1.MasterPlayer.modData[ModEntry.Id + "/MargotCottageDue"] = (Game1.Date.TotalDays + 1).ToString();
        Social.MargotEvents.FinishBuilding();
        Check(monitor, "the night before it's done, it's built, and Robin writes", Social.MargotEvents.CottageBuilt
            && who.mailForTomorrow.Contains(Social.MargotEvents.RobinLetter) && DataLoader.Mail(Game1.content).ContainsKey(Social.MargotEvents.RobinLetter));
        Social.MargotEvents.MoveIn();
        Check(monitor, "...and the cottage is her home", margot.DefaultMap == Social.MargotEvents.CottageHome, margot.DefaultMap);
        foreach (string answer in new[] { "Report", "Talk", "NotMine" })
        {
            foreach (string a in new[] { "Report", "Talk", "NotMine" }) who.mailReceived.Remove(ModEntry.Id + "_Margot" + a);
            who.mailReceived.Add(ModEntry.Id + "_Margot" + answer);
            string line = Social.MargotEvents.ErrandLine(who);
            Check(monitor, $"her 8-heart line follows the 6-heart answer '{answer}'", line.Length > 20 && !line.Contains("margot."), line.Substring(0, Math.Min(40, line.Length)));
        }
        Check(monitor, "the 14-heart card names a painting", Social.MargotEvents.CardLine(who).Contains("Restored by Margot Pell"), Social.MargotEvents.CardLine(who));
        // Her 10-heart paintings, by your answer.
        string eachOther = ModEntry.Id + "_MargotEachOther", window = ModEntry.Id + "_MargotWindow";
        who.mailReceived.Remove(window);
        who.mailReceived.Add(eachOther);
        var portraits = Social.MargotEvents.Paintings(who);
        var hers = portraits.FirstOrDefault(ArtItems.IsFramed);
        var herData = hers != null ? PaintingData.Read(hers) : null;
        Check(monitor, "'paint each other': her portrait by you, and yours by her, painted from you",
            portraits.Count == 2 && portraits[0].QualifiedItemId == "(F)" + ModEntry.Id + "_MargotByYou" && portraits[0].DisplayName == "Margot, by You"
            && herData != null && herData.IsPortrait && herData.Painter == I18n.Get("margot.name") && herData.Stars == 4,
            string.Join(", ", portraits.Select(p => p.DisplayName)) + $"; painter {herData?.Painter}, {herData?.Stars} stars");
        who.mailReceived.Remove(eachOther);
        who.mailReceived.Add(window);
        var windows = Social.MargotEvents.Paintings(who);
        Check(monitor, "'paint the window': the north window", windows.Count == 1 && windows[0].DisplayName == "North Window, by Margot and You",
            string.Join(", ", windows.Select(p => p.DisplayName)));
        who.mailReceived.Remove(window);
        // Her mastery test: offered with the bar full, passed at gold, and she
        // keeps the painting.
        bool hadMastery = who.mailReceived.Contains(OldPaintings.MasteryFlag);
        int level = ArtProgress.Level(who), masteryXp = ArtProgress.MasteryXp(who);
        who.mailReceived.Remove(OldPaintings.MasteryFlag);
        Social.MargotMastery.DevReset(who);
        ArtProgress.SetLevel(who, 9);
        Check(monitor, "no test before Painting 10", !Social.MargotMastery.CanOffer(who));
        ArtProgress.SetLevel(who, PaintingRules.MaxLevel);
        ArtProgress.SetMasteryXp(who, ArtProgress.MasteryXpNeeded);
        Check(monitor, "with the mastery bar full, she offers the test", Social.MargotMastery.CanOffer(who));
        int easels = who.Items.CountId("(BC)" + ArtItems.MastersEaselId);
        // Leaving the damage as it was: no mastery, and she keeps it.
        var failed = Social.MargotMastery.StartTest(who);
        failed.DevFinish(false);
        Game1.delayedActions.ToList().ForEach(a => a.behavior?.Invoke());
        Game1.delayedActions.Clear();
        failed.DevKeep();
        Game1.exitActiveMenu();
        Social.MargotMastery.TakeBackTestPainting(who);
        CloseDialogue();
        Check(monitor, "below gold, no mastery, and she keeps the painting", !ArtProgress.HasMastery(who)
            && !who.Items.Any(i => i != null && (OldPaintings.IsDamaged(i) || PaintingData.Read(i)?.IsOld == true)));
        Check(monitor, "one try a day", !Social.MargotMastery.CanOffer(who));
        Social.MargotMastery.DevReset(who);
        var test = Social.MargotMastery.StartTest(who);
        test.DevRestorePerfectly();
        test.DevFinish(false);
        Game1.delayedActions.ToList().ForEach(a => a.behavior?.Invoke());
        Game1.delayedActions.Clear();
        Check(monitor, "a gold restoration makes you a conservator, with the Master's Easel",
            ArtProgress.HasMastery(who) && who.Items.CountId("(BC)" + ArtItems.MastersEaselId) == easels + 1);
        test.DevKeep();
        Game1.exitActiveMenu();
        Social.MargotMastery.TakeBackTestPainting(who);
        Check(monitor, "...and she keeps the painting", !who.Items.Any(i => i != null && (OldPaintings.IsDamaged(i) || PaintingData.Read(i)?.IsOld == true)));
        Check(monitor, "once a conservator, she doesn't offer it again", !Social.MargotMastery.CanOffer(who));
        foreach (var e in who.Items.Where(i => i?.QualifiedItemId == "(BC)" + ArtItems.MastersEaselId).ToList()) who.removeItemFromInventory(e);
        if (!hadMastery) who.mailReceived.Remove(OldPaintings.MasteryFlag);
        ArtProgress.SetLevel(who, level);
        ArtProgress.SetMasteryXp(who, masteryXp);
        Social.MargotMastery.DevReset(who);
        CloseDialogue();

        // Her fee service (before Mastery): the set's fee, three days, silver.
        Social.MargotService.DevReset(who);
        var wreck = OldPaintings.All.First(p => p.Set == "wreck");
        var damagedWreck = OldPaintings.CreateDamaged(wreck);
        who.addItemToInventory(damagedWreck);
        int money = who.Money;
        who.Money = 100;
        Check(monitor, "without the fee, she keeps nothing", !Social.MargotService.TakeIn(margot, who, damagedWreck, wreck) && Social.MargotService.Job(who) == null);
        CloseDialogue();
        who.Money = 1000;
        Check(monitor, "she takes it for 800g (the wreck's fee)", Social.MargotService.TakeIn(margot, who, damagedWreck, wreck)
            && who.Money == 200 && Social.MargotService.Job(who)?.Ready == Game1.Date.TotalDays + Social.MargotService.Days
            && !who.Items.Any(i => i != null && OldPaintings.IsDamaged(i)));
        CloseDialogue();
        var restoredWreck = Social.MargotService.Restored(wreck);
        Check(monitor, "...and gives it back restored, silver, titled, worth the set's silver value",
            restoredWreck.Quality == PaintingRules.Silver && restoredWreck.DisplayName == OldPaintings.Title(wreck)
            && restoredWreck.salePrice() > 0 && PaintingData.Read(restoredWreck)?.IsOld == true, $"{restoredWreck.DisplayName} q{restoredWreck.Quality}");
        Check(monitor, "...when collected, and the job is done", Social.MargotService.HandBack(margot, who, wreck) && Social.MargotService.Job(who) == null);
        CloseDialogue();
        foreach (var item in who.Items.Where(i => i != null && PaintingData.Read(i)?.IsOld == true).ToList()) who.removeItemFromInventory(item);
        who.Money = money;
        Social.MargotService.DevReset(who);

        // Back as she was, at the Saloon.
        Social.MargotEvents.DevReset();
        margot.DefaultMap = home.Name;
        margot.DefaultPosition = homeTile.ToVector2() * 64f;
        Game1.warpCharacter(margot, home, homeTile.ToVector2());
    }

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
    // Where the mod's events are: Sable's, and Julian's (1.2).
    private static readonly string[] EventLocations =
        { Shop.ArtShop.LocationName, "Forest", "JoshHouse", Shop.ArtShop.BackRoom, "ArchaeologyHouse", "BusStop", "Saloon", "ScienceHouse", Social.MargotEvents.CottageHome };

    private static void EventPaths(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        foreach (string locationName in EventLocations)
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

    // The painting record's tabs (1.2).
    private static void RecordTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        string catalogueKey = ModEntry.Id + "/Catalogue", perksKey = ModEntry.Id + "/CollectionPerks";
        who.modData.TryGetValue(catalogueKey, out string? savedCatalogue);
        who.modData.TryGetValue(perksKey, out string? savedPerks);
        var savedOwned = ArtProgress.OwnedPigments(who).ToList();
        void SetCatalogue(System.Collections.Generic.IEnumerable<string> subjects) =>
            who.modData[catalogueKey] = System.Text.Json.JsonSerializer.Serialize(subjects.Distinct().ToDictionary(s => s, _ => new CatalogueEntry { Count = 1, BestQuality = 1, FirstDay = 1 }));
        try
        {
            // What goes where.
            Check(monitor, "a parsnip is a crop", RecordTabs.TabOfItem("(O)24") == RecordTab.Crops);
            Check(monitor, "wild horseradish and salmonberries are forage", RecordTabs.TabOfItem("(O)16") == RecordTab.Forage && RecordTabs.TabOfItem("(O)296") == RecordTab.Forage);
            Check(monitor, "a sunfish is a fish, trash isn't", RecordTabs.TabOfItem("(O)145") == RecordTab.Fish && RecordTabs.TabOfItem("(O)168") != RecordTab.Fish);
            Check(monitor, "an amethyst and a geode mineral are minerals", RecordTabs.TabOfItem("(O)66") == RecordTab.Minerals && RecordTabs.TabOfItem("(O)538") == RecordTab.Minerals);
            Check(monitor, "a fried egg is cooking", RecordTabs.TabOfItem("(O)194") == RecordTab.Cooking);
            Check(monitor, "honey goes in everything else", RecordTabs.TabOfItem("(O)340") == RecordTab.Other);
            Check(monitor, "portraits and views have their own tabs",
                RecordTabs.TabOfSubject("sitter:Leah") == RecordTab.Portraits && RecordTabs.TabOfSubject("view:Beach:summer") == RecordTab.Views);

            // What each tab holds.
            SetCatalogue(Array.Empty<string>());
            foreach (var tab in RecordTabs.Completable)
            {
                var (done, total) = RecordTabs.Progress(who, tab);
                monitor.Log($"  {tab}: {total} to paint", LogLevel.Info);
                Check(monitor, $"the {tab} tab has things to paint, none painted yet", total > 3 && done == 0, $"{done} of {total}");
            }
            var crops = RecordTabs.Members(who, RecordTab.Crops).Select(m => m.Key).ToList();
            Check(monitor, "the crops tab holds the parsnip but no fish", crops.Contains("(O)24") && !crops.Contains("(O)145"));
            var sitters = RecordTabs.Members(who, RecordTab.Portraits).Select(m => m.Key).ToList();
            Check(monitor, "the portraits tab holds Leah and Sable", sitters.Contains("sitter:Leah") && sitters.Contains("sitter:Sable"));
            var views = RecordTabs.Members(who, RecordTab.Views).Select(m => m.Key).ToList();
            Check(monitor, "the views tab holds the beach, the desert and the island, but not the mines", views.Contains("view:Beach") && views.Contains("view:Desert")
                && views.Any(v => v.StartsWith("view:Island")) && !views.Any(v => v.StartsWith("view:UndergroundMine")));

            // A still life counts for each item; a view for its place in each season.
            SetCatalogue(new[] { "(O)24|(O)145", "view:Beach:summer", "view:Beach:winter", "(O)340" });
            Check(monitor, "a still life of two counts for both", RecordTabs.Progress(who, RecordTab.Crops).Done == 1 && RecordTabs.Progress(who, RecordTab.Fish).Done == 1);
            var beach = RecordTabs.Members(who, RecordTab.Views).First(m => m.Key == "view:Beach");
            Check(monitor, "a place painted in two seasons is one view, with both seasons", beach.Painted?.Count == 2 && beach.Seasons.Count == 2, string.Join(",", beach.Seasons));
            Check(monitor, "everything else shows only what's painted", RecordTabs.Members(who, RecordTab.Other).Select(m => m.Key).SequenceEqual(new[] { "(O)340" }));

            // Rewards: a quarter, a half, all of it.
            who.modData[perksKey] = "";
            var forage = RecordTabs.Members(who, RecordTab.Forage).Select(m => m.Key).ToList();
            int moss = Palette.IndexOf("moss");
            ArtProgress.SetOwned(who, ArtProgress.OwnedPigments(who).Where(p => p != moss).ToList());
            string tube = "(O)" + ArtItems.TubeId(moss);
            int tubesBefore = who.Items.Where(i => i?.QualifiedItemId == tube).Sum(i => i.Stack);
            SetCatalogue(forage.Take((forage.Count + 3) / 4));
            var first = RecordTabs.GrantRewards(who);
            Check(monitor, "a quarter of a tab brings its first reward", first.Count == 1 && RecordTabs.HasReward(who, RecordTab.Forage, 25), string.Join(" / ", first));
            Check(monitor, "...three tubes of its colour, and the colour",
                who.Items.Where(i => i?.QualifiedItemId == tube).Sum(i => i.Stack) == tubesBefore + RecordTabs.RewardTubes && ArtProgress.Owns(who, moss));
            Check(monitor, "a reward comes once", RecordTabs.GrantRewards(who).Count == 0);
            Check(monitor, "a part-painted tab doesn't sell for more", RecordTabs.ValueFor(who, forage[0]) == 1f);
            SetCatalogue(forage);
            var rest = RecordTabs.GrantRewards(who);
            Check(monitor, "the whole tab brings the half and the whole", rest.Count == 2 && RecordTabs.HasReward(who, RecordTab.Forage, 100), string.Join(" / ", rest));
            Check(monitor, "then its paintings sell for more, and only its own", RecordTabs.ValueFor(who, forage[0]) == RecordTabs.SpecialistValue && RecordTabs.ValueFor(who, "(O)24") == 1f);
            foreach (var item in who.Items.Where(i => i?.QualifiedItemId == tube || i?.QualifiedItemId == "(O)" + ArtItems.FrameItemId).ToList())
                who.removeItemFromInventory(item);

            // The menu, on every tab.
            bool opened = true;
            foreach (var tab in Enum.GetValues<RecordTab>())
                try { _ = new Menus.CollectionMenu(who, tab); }
                catch (Exception ex) { opened = false; monitor.Log($"  {tab}: {ex}", LogLevel.Error); }
            Check(monitor, "the record opens on every tab", opened);
        }
        finally
        {
            if (savedCatalogue != null) who.modData[catalogueKey] = savedCatalogue; else who.modData.Remove(catalogueKey);
            if (savedPerks != null) who.modData[perksKey] = savedPerks; else who.modData.Remove(perksKey);
            ArtProgress.SetOwned(who, savedOwned);
        }
    }

    // The portfolio book and the plan chest (1.2). A shared lock is handed
    // over on the next tick, so the menus are checked a step at a time: run
    // it, then 'museart_portfoliotest book', 'openchest', 'chest' and 'closed', a second apart.
    private static void PortfolioTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        switch (args.FirstOrDefault())
        {
            case "book":
                Check(monitor, "the book opens and is kept for whoever opened it", Game1.activeClickableMenu is Menus.PortfolioMenu && Portfolio.Mutex.IsLockHeld());
                Game1.exitActiveMenu();
                return;
            case "openchest":
                Check(monitor, "closing the book lets it go", !Portfolio.Mutex.IsLockHeld());
                Shop.PlanChest.Open(who);
                return;
            case "chest":
                var grab = Game1.activeClickableMenu as ItemGrabMenu;
                Check(monitor, "the plan chest opens as a chest", grab?.context is Shop.PlanChest, $"{Game1.activeClickableMenu?.GetType().Name ?? "no menu"}, lock {Game1.player.team.GetOrCreateGlobalInventoryMutex(Shop.PlanChest.InventoryId).IsLocked()}");
                Check(monitor, "...holding seventy", grab?.ItemsToGrabMenu.capacity == 70, $"{grab?.ItemsToGrabMenu.capacity}");
                Check(monitor, "...and taking paintings, not parsnips", grab != null && grab.inventory.highlightMethod(DevTools.TestPainting("(O)24", 1)) && !grab.inventory.highlightMethod(ItemRegistry.Create("(O)24")));
                Game1.exitActiveMenu();
                return;
            case "closed":
                Check(monitor, "closing it lets the chest go", !Game1.player.team.GetOrCreateGlobalInventoryMutex(Shop.PlanChest.InventoryId).IsLockHeld());
                return;
        }
        var book = Portfolio.Items;
        void Reset()
        {
            for (int i = 0; i < book.Count; i++) book[i] = null;
            foreach (var page in Portfolio.Pages)
            {
                who.mailReceived.Remove(Portfolio.PageFlag(page));
                who.mailForTomorrow.Remove(Portfolio.PageFlag(page) + "_Letter");
            }
            who.mailReceived.Remove(Portfolio.BookFlag);
            who.mailForTomorrow.Remove(Portfolio.BookFlag + "_Letter");
            foreach (var item in who.Items.Where(Portfolio.IsKeepable).ToList())
                who.removeItemFromInventory(item);
        }
        SObject Give(string subject, int quality, float stars = 4f)
        {
            var painting = DevTools.TestPainting(subject, quality, stars);
            who.addItemToInventory(painting);
            return painting;
        }
        Reset();
        try
        {
            Check(monitor, "the book has a slot for every page's slots", book.Count == Portfolio.SlotCount && Portfolio.SlotCount == 31, $"{book.Count}");

            // Into a slot, kept; a better one swaps in; a worse one doesn't fit.
            var spring = Give("view:Town:spring", PaintingRules.Silver);
            var (slot, replaced) = Portfolio.Place(who, 0, spring);
            Check(monitor, "a spring view of town goes in its slot and leaves the bag", slot == 0 && replaced == null && Portfolio.In(0) == spring && !who.Items.Contains(spring));
            var better = Give("view:Town:spring", PaintingRules.Gold);
            (slot, replaced) = Portfolio.Place(who, 0, better);
            Check(monitor, "a better one swaps in, and the old one comes back", slot == 0 && replaced == spring && Portfolio.In(0) == better && who.Items.Contains(spring));
            Check(monitor, "a worse one has nowhere to go", Portfolio.SlotFor(0, spring) < 0);
            var winterBeach = Give("view:Beach:winter", PaintingRules.Iridium);
            Check(monitor, "a view of somewhere else doesn't fit the town's slots", Portfolio.SlotFor(0, winterBeach) < 0);

            // The hardest slot a painting fits, first.
            int harvest = Portfolio.FirstIndex(1), fine = Portfolio.FirstIndex(2);
            Check(monitor, "a parsnip still life is a spring crop", Portfolio.SlotFor(1, Give("(O)24", PaintingRules.Silver)) == harvest);
            Check(monitor, "three crops together go in three crops", Portfolio.SlotFor(1, Give("(O)24|(O)190|(O)276", PaintingRules.Silver)) == harvest + 4);
            Check(monitor, "an apple goes in the orchard slot", Portfolio.SlotFor(1, Give("(O)613", PaintingRules.Silver)) == harvest + 3);
            var master = Give("(O)24", PaintingRules.Iridium);
            master.modData[PaintingData.MasterpieceKey] = "1";
            master.modData[Varnish.FinishKey] = "gallery";
            Check(monitor, "a Masterpiece goes in the Masterpiece slot, not the gold one", Portfolio.SlotFor(2, master) == fine + 4);
            Check(monitor, "a plain painting fits none of fine work", Portfolio.SlotFor(2, Give("(O)24", PaintingRules.Silver)) < 0);

            // A finished page: everyone's flag, and Sable's letter.
            foreach (string season in new[] { "summer", "fall", "winter" })
                Portfolio.Place(who, 0, Give("view:Town:" + season, PaintingRules.Silver));
            Check(monitor, "four of five isn't finished", !Portfolio.IsFinished(Portfolio.Pages[0]));
            Portfolio.Place(who, 0, Give("view:Farm:spring", PaintingRules.Silver));
            Check(monitor, "the last slot finishes the page", Portfolio.IsFinished(Portfolio.Pages[0]) && who.mailForTomorrow.Contains(Portfolio.PageFlag(Portfolio.Pages[0]) + "_Letter"));
            Check(monitor, "the letter is in the game's mail", DataLoader.Mail(Game1.content).ContainsKey(Portfolio.PageFlag(Portfolio.Pages[0]) + "_Letter"));

        }
        finally
        {
            Reset();
        }
        // The book's menu, checked next step.
        Menus.PortfolioMenu.Open(who);
    }

    // Creatures, Krobus and the Wizard, and the far places (1.2, step 3).
    private static void CreatureTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var kills = new System.Collections.Generic.Dictionary<string, int>(who.stats.specificMonstersKilled);
        try
        {
            // Every creature's first frame is found, and sized like a sprite.
            var bad = Creatures.All.Where(c => Creatures.Frame(c.Name) is not { } f || f.Source.Width < 6 || f.Source.Height < 6 || f.Source.Width > 32 || f.Source.Height > 32)
                .Select(c => c.Name + (Creatures.Frame(c.Name) is { } f ? $" {f.Source.Width}x{f.Source.Height}" : " none")).ToList();
            Check(monitor, "every creature's first frame is found", bad.Count == 0, string.Join(", ", bad));
            Check(monitor, "creatures have their game names", Creatures.DisplayName("Green Slime").Length > 0 && Creatures.DisplayName("Skeleton") != "");

            // Met only once slain; a Junimo once the Community Center (or Joja's) is done.
            who.stats.specificMonstersKilled.Clear();
            Check(monitor, "no creatures before any are slain", Creatures.Available(who).All(n => n == Creatures.Junimo));
            who.stats.specificMonstersKilled["Green Slime"] = 3;
            who.stats.specificMonstersKilled["Skeleton"] = 1;
            Check(monitor, "a slain monster can be painted", Creatures.Available(who).Contains("Green Slime") && Creatures.Available(who).Contains("Skeleton"));
            Check(monitor, "a Junimo only after the Community Center", Creatures.Unlocked(who, Creatures.Junimo) == (Game1.MasterPlayer.hasCompletedCommunityCenter() || Utility.hasFinishedJojaRoute()));

            // References: a slime fits the small canvas; a skeleton needs the large one.
            int small = PaintingRules.CanvasSize(CanvasKind.Small), large = PaintingRules.CanvasSize(CanvasKind.Large);
            var owned = Enumerable.Range(0, Palette.All.Length).ToList();
            var slime = Creatures.Reference("Green Slime", small, owned);
            Check(monitor, "a slime is painted green", Creatures.Fits("Green Slime", small) && slime.SubjectCells > 40
                && slime.PigmentsUsed.Any(p => Palette.All[p].Id is "moss" or "leaf"), $"{slime.SubjectCells} cells");
            Check(monitor, "a skeleton needs a bigger canvas", !Creatures.Fits("Skeleton", small) && Creatures.Fits("Skeleton", large));
            Check(monitor, "tougher monsters are worth more", Creatures.SubjectPrice(Creatures.SubjectFor("Pepper Rex")) > Creatures.SubjectPrice(Creatures.SubjectFor("Green Slime")));

            // Through the easel: a study, kept in the record's Creatures tab.
            FreshStart(who);
            var menu = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { Creatures.SubjectFor("Green Slime") });
            menu.DevPaintPartly(1f, 3);
            menu.DevFinish(false);
            var painting = menu.DevPainting;
            var data = painting == null ? null : PaintingData.Read(painting);
            Check(monitor, "a creature paints as a study", data?.IsCreature == true && data.SubjectItems.Length == 0 && painting!.DisplayName.Contains(Creatures.DisplayName("Green Slime")), painting?.DisplayName ?? "none");
            Check(monitor, "...and goes in the record's Creatures tab", RecordTabs.TabOfSubject(Creatures.SubjectFor("Green Slime")) == RecordTab.Creatures
                && RecordTabs.Members(who, RecordTab.Creatures).Any(m => m.Key == Creatures.SubjectFor("Green Slime") && m.Painted != null));

            // Krobus and the Wizard sit once their stories are done.
            bool hadInk = who.mailReceived.Contains("hasPickedUpMagicInk");
            who.mailReceived.Remove("hasPickedUpMagicInk");
            Check(monitor, "the Wizard won't sit before the magic ink", !Sitters.SitsWithoutHearts(who, "Wizard"));
            who.mailReceived.Add("hasPickedUpMagicInk");
            Check(monitor, "...and will after it, hearts or not", Sitters.SitsWithoutHearts(who, "Wizard") && (Sitters.PortraitTexture("Wizard") == null || Sitters.CanSitFor(who, "Wizard") || !who.friendshipData.ContainsKey("Wizard")));
            if (!hadInk) who.mailReceived.Remove("hasPickedUpMagicInk");
            Check(monitor, "Krobus sits once met", Sitters.SitsWithoutHearts(who, "Krobus") == who.friendshipData.ContainsKey("Krobus"));

            // The far places: the Skull Cavern's entry room counts; the mines don't.
            var cave = Game1.getLocationFromName("SkullCave");
            Check(monitor, "the Skull Cavern's entry room is a place to paint", cave != null && Views.CanPaintHere(cave) && !cave.IsOutdoors);
            Check(monitor, "...and in the record's views", RecordTabs.Members(who, RecordTab.Views).Any(m => m.Key == "view:SkullCave"));
            int far = Portfolio.FirstIndex(3), legends = Portfolio.FirstIndex(4);
            Check(monitor, "a view inside the Skull Cavern goes on Far Places", Portfolio.SlotFor(3, DevTools.TestPainting("view:SkullCave:fall", PaintingRules.Silver)) == far + 1);
            Check(monitor, "the Legend goes on Legends", Portfolio.SlotFor(4, DevTools.TestPainting("(O)163", PaintingRules.Silver)) == legends + 2);
        }
        finally
        {
            who.stats.specificMonstersKilled.Clear();
            foreach (var (name, count) in kills)
                who.stats.specificMonstersKilled[name] = count;
        }
    }

    // Painting Mastery and the museum collection (1.3, step 5, parts 4 and 5).
    private static void CollectionTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        SObject Restored(OldPainting old) => PaintingItems.Create(new PaintingData
        {
            Size = old.Size, Cells = OldPaintings.Original(old), Purity = new Purity[old.Size, old.Size],
            Subject = OldPaintings.SubjectFor(old.Id), Stars = 5f, Painter = who.Name,
        }, PaintingRules.Gold, 1000);
        void ClearDonations()
        {
            foreach (var p in OldPaintings.All) who.mailReceived.Remove(ModEntry.Id + "_Donated_" + p.Id);
            foreach (var (key, _) in Social.MuseumCollection.Rewards)
            {
                who.mailReceived.Remove(Social.MuseumCollection.RewardLetter(key));
                who.mailForTomorrow.Remove(Social.MuseumCollection.RewardLetter(key));
            }
        }
        if (args.FirstOrDefault() == "view")
        {
            ClearDonations();
            foreach (var p in OldPaintings.All.Where(p => p.Set is "wreck" || p.Id is "valley_mill" or "calico_queen" or "dwarvish_king"))
                who.mailReceived.Add(ModEntry.Id + "_Donated_" + p.Id);
            if (!who.friendshipData.ContainsKey("Gunther")) who.friendshipData.Add("Gunther", new Friendship());
            who.friendshipData["Gunther"].TalkedToToday = true;
            var gunther = Game1.getCharacterFromName("Gunther");
            gunther.CurrentDialogue.Clear();
            gunther.checkAction(who, gunther.currentLocation);
            return;
        }
        if (args.FirstOrDefault() == "frames")
        {
            foreach (var style in ArtItems.FrameStyles)
                who.addItemToInventory(ItemRegistry.Create("(O)" + style.ItemId, 2));
            var toFrame = DevTools.TestPainting("view:Town:spring", PaintingRules.Gold);
            who.addItemToInventory(toFrame);
            var m = new EaselMenu(who);
            m.DevFraming(toFrame);
            m.DevFrameStyle(ArtItems.Sandstone);
            Game1.activeClickableMenu = m;
            return;
        }
        int level = ArtProgress.Level(who), mxp = ArtProgress.MasteryXp(who);
        try
        {
            // The mastery bar fills with Painting XP past level 10.
            ArtProgress.SetLevel(who, 10);
            ArtProgress.SetMasteryXp(who, 0);
            ArtProgress.AddXp(who, 9000);
            bool partway = !ArtProgress.MasteryReady(who) && ArtProgress.MasteryXp(who) == 9000;
            ArtProgress.AddXp(who, 6000);
            Check(monitor, "Painting XP past level 10 fills the mastery bar", partway && ArtProgress.MasteryReady(who), $"{ArtProgress.MasteryXp(who)} XP");

            // The Master's Easel gives more light.
            var farm = who.currentLocation;
            var tile = who.Tile + new Vector2(2, 0);
            farm.objects.Remove(tile);
            farm.objects.Add(tile, ItemRegistry.Create<SObject>("(BC)" + ArtItems.EaselId));
            float plain = new EaselMenu(who, easelTile: tile).DevLightMultiplier;
            farm.objects.Remove(tile);
            farm.objects.Add(tile, ItemRegistry.Create<SObject>(ArtItems.QualifiedMastersEaselId));
            float master = new EaselMenu(who, easelTile: tile).DevLightMultiplier;
            farm.objects.Remove(tile);
            Check(monitor, "the Master's Easel gives more light", Math.Abs(master / plain - ArtItems.MastersEaselLight) < 0.001f, $"{plain:0.00} -> {master:0.00}");

            // Donating the Wreck's five brings its frames.
            ClearDonations();
            var gunther = Game1.getCharacterFromName("Gunther");
            foreach (var old in OldPaintings.All.Where(p => p.Set == "wreck"))
            {
                var painting = Restored(old);
                who.addItemToInventory(painting);
                Social.MuseumCollection.Donate(who, gunther, painting, old);
                (Game1.activeClickableMenu as StardewValley.Menus.DialogueBox)?.closeDialogue();
                gunther.CurrentDialogue.Clear();
            }
            Check(monitor, "five donated, and the Wreck is complete", Social.MuseumCollection.DonatedCount == 5 && Social.MuseumCollection.SetDone("wreck"));
            Check(monitor, "...which brings Gunther's letter with driftwood frames", who.mailForTomorrow.Contains(Social.MuseumCollection.RewardLetter("set-wreck"))
                && DataLoader.Mail(Game1.content)[Social.MuseumCollection.RewardLetter("set-wreck")].Contains(ArtItems.Driftwood.ItemId));
            Check(monitor, "...and not the Miners'", !who.mailForTomorrow.Contains(Social.MuseumCollection.RewardLetter("set-miners")));
            var framed = ArtHooks.Frame(DevTools.TestPainting("view:Beach:summer", PaintingRules.Gold), ArtItems.Driftwood);
            Check(monitor, "a painting framed in driftwood hangs in the driftwood frame", framed.QualifiedItemId == "(F)" + ArtItems.Driftwood.SmallId && ArtItems.IsFramed(framed));
        }
        finally
        {
            ClearDonations();
            ArtProgress.SetLevel(who, level);
            ArtProgress.SetMasteryXp(who, mxp);
        }
    }

    // Old paintings and restoring them (1.3, step 5).
    private static void OldTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        bool hadMastery = who.mailReceived.Contains(OldPaintings.MasteryFlag);
        if (args.FirstOrDefault() == "show")
        {
            // A restoration on the easel, half done, to look at ('museart_frame' it).
            who.mailReceived.Add(OldPaintings.MasteryFlag);
            var mill = OldPaintings.Get(args.ElementAtOrDefault(1) ?? "valley_mill")!;
            who.addItemToInventory(OldPaintings.CreateDamaged(mill));
            var easel = EaselMenu.StartStillLife(who, mill.Size > 28 ? CanvasKind.Large : CanvasKind.Medium, new[] { OldPaintings.SubjectFor(mill.Id) });
            Game1.activeClickableMenu = easel;
            return;
        }
        try
        {
            // The pictures: whole canvases, damaged by about a third, the same every time.
            var bad = OldPaintings.All.Where(p =>
            {
                var cells = OldPaintings.Original(p);
                int painted = cells.Cast<byte>().Count(c => c != PixelCanvas.Blank);
                return cells.GetLength(0) != p.Size || painted < p.Size * p.Size * 9 / 10;
            }).Select(p => p.Id).ToList();
            Check(monitor, "all 21 old paintings load as whole pictures", OldPaintings.All.Length == 21 && bad.Count == 0, string.Join(", ", bad));
            var shares = OldPaintings.All.Select(p => OldPaintings.Damage(p).Cast<bool>().Count(d => d) * 100 / (p.Size * p.Size)).ToList();
            Check(monitor, "each is about a third damaged", shares.All(s => s >= 28 && s <= 42), string.Join(" ", shares));
            Check(monitor, "...and named", OldPaintings.All.All(p => !OldPaintings.Title(p).StartsWith("old.")));

            // A find from a set becomes one of that set, one not found before.
            who.modData.Remove(ModEntry.Id + "/OldFound");
            var found = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 5; i++)
            {
                var item = ItemRegistry.Create("(O)" + OldPaintings.ItemIdFor("wreck"));
                OldPaintings.Assign(who, item);
                if (OldPaintings.Of(item) is { } p) found.Add(p.Id);
            }
            Check(monitor, "five wreck finds are the five wreck paintings", found.Count == 5 && found.All(id => id.StartsWith("wreck_")), string.Join(", ", found));
            var damaged = OldPaintings.CreateDamaged(OldPaintings.Get("valley_mill")!);
            Check(monitor, "a damaged painting isn't a painting to anything else", OldPaintings.IsDamaged(damaged) && !PaintingItems.IsPainting(damaged)
                && !Portfolio.IsKeepable(damaged) && !Social.JulianPurchase.CanSell(damaged));

            // The first is guaranteed from Painting level 3.
            who.modData.Remove(ModEntry.Id + "/OldFound");
            who.mailReceived.Remove(ModEntry.Id + "_OldPaintingFirst");
            int level = ArtProgress.Level(who);
            ArtProgress.SetLevel(who, 2);
            bool early = OldPaintingFinds.FirstDue(who);
            ArtProgress.SetLevel(who, 3);
            var farm = Game1.getFarm();
            int before = farm.debris.Count;
            farm.digUpArtifactSpot(10, 10, who);
            bool dug = farm.debris.Skip(before).Any(d => d.item != null && OldPaintings.IsDamaged(d.item));
            ArtProgress.SetLevel(who, level);
            Check(monitor, "the first old painting comes with the first dig from level 3", !early && dug);
            farm.debris.Clear();

            // Restoring (at Painting 10, as Mastery comes): perfectly, then not at all.
            who.mailReceived.Add(OldPaintings.MasteryFlag);
            FreshStart(who);
            ArtProgress.SetLevel(who, 10);
            var mill = OldPaintings.Get("valley_mill")!;
            who.addItemToInventory(OldPaintings.CreateDamaged(mill));
            Check(monitor, "with Mastery, the easel offers the old painting", Studies.Available(who, OldPaintings.Prefix).Contains(OldPaintings.SubjectFor(mill.Id)));
            var easel = EaselMenu.StartStillLife(who, CanvasKind.Medium, new[] { OldPaintings.SubjectFor(mill.Id) });
            easel.DevRestorePerfectly();
            easel.DevFinish(false);
            var restored = easel.DevPainting;
            var data = restored == null ? null : PaintingData.Read(restored);
            Check(monitor, "a perfect restoration is iridium, and titled as the old painting", restored?.Quality == PaintingRules.Iridium && data?.IsOld == true
                && restored.DisplayName == OldPaintings.Title(mill), $"{restored?.DisplayName} q{restored?.Quality}");
            Check(monitor, "...worth its set's value", restored != null && restored.Price == OldPaintings.RestoredValue(mill, PaintingRules.Iridium), $"{restored?.Price}g");
            Check(monitor, "...and the damaged one is used up", !who.Items.Any(i => i != null && OldPaintings.Of(i)?.Id == mill.Id));
            who.addItemToInventory(OldPaintings.CreateDamaged(mill));
            var lazy = EaselMenu.StartStillLife(who, CanvasKind.Medium, new[] { OldPaintings.SubjectFor(mill.Id) });
            lazy.DevFinish(false);
            Check(monitor, "leaving the damage as it was is poor", lazy.DevPainting == null || lazy.DevPainting.Quality <= PaintingRules.Silver, $"q{lazy.DevPainting?.Quality}");
            foreach (var item in who.Items.Where(i => i != null && (OldPaintings.IsDamaged(i) || PaintingData.Read(i)?.IsOld == true)).ToList())
                who.removeItemFromInventory(item);
        }
        finally
        {
            if (!hadMastery) who.mailReceived.Remove(OldPaintings.MasteryFlag);
        }
    }

    // Special orders (1.3, step 4).
    private static void OrderTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        SObject Tagged(string subject, int quality, string? conditions = null, bool masterpiece = false)
        {
            var painting = DevTools.TestPainting(subject, quality);
            if (conditions != null) painting.modData[ModEntry.Id + "/Conditions"] = conditions;
            if (masterpiece) painting.modData[PaintingData.MasterpieceKey] = "1";
            painting.MarkContextTagsDirty();
            return painting;
        }
        // The tags follow the painting.
        var portrait = Tagged("sitter:Leah", PaintingRules.Silver);
        Check(monitor, "a portrait is tagged as one, and whose", portrait.HasContextTag("muse_subject_portrait") && portrait.HasContextTag("muse_sitter_leah") && portrait.HasContextTag("muse_painting"));
        var night = Tagged("view:Beach:summer", PaintingRules.Silver, "night");
        Check(monitor, "a night view of the beach is tagged so", night.HasContextTag("muse_view_beach") && night.HasContextTag("muse_night") && !night.HasContextTag("muse_subject_portrait"));
        var dish = Tagged("(O)194|(O)24", PaintingRules.Gold);
        Check(monitor, "a still life with a dish is food, gold", dish.HasContextTag("muse_subject_food") && dish.HasContextTag("quality_gold"));
        Check(monitor, "a still life of wine is food (a drink)", Tagged("(O)348", PaintingRules.Silver).HasContextTag("muse_subject_food"));
        Check(monitor, "a parsnip isn't", !Tagged("(O)24", PaintingRules.Gold).HasContextTag("muse_subject_food"));
        var legend = Tagged("(O)163", PaintingRules.Iridium, masterpiece: true);
        Check(monitor, "a Masterpiece of the Legend is tagged for Qi", legend.HasContextTag("muse_masterpiece") && legend.HasContextTag("muse_legendary_fish"));

        // The orders, their letters, and the level they appear from.
        var orders = DataLoader.SpecialOrders(Game1.content);
        string[] ids = { "LewisPortraits", "GusSaloon", "LeahWoods", "ElliottBeach", "QiLegend" };
        Check(monitor, "the five orders are in the game", ids.All(id => orders.ContainsKey(ModEntry.Id + "_" + id)));
        var mail = DataLoader.Mail(Game1.content);
        Check(monitor, "...with their reward letters", new[] { "Lewis", "Gus", "Leah", "Elliott" }.All(n => mail.ContainsKey($"{ModEntry.Id}_Order{n}_Reward")));
        int level = ArtProgress.Level(who);
        ArtProgress.SetLevel(who, 2);
        bool low = GameStateQuery.CheckConditions(orders[ModEntry.Id + "_LewisPortraits"].Condition);
        ArtProgress.SetLevel(who, 3);
        bool high = GameStateQuery.CheckConditions(orders[ModEntry.Id + "_LewisPortraits"].Condition);
        ArtProgress.SetLevel(who, level);
        Check(monitor, "Lewis's order appears from Painting level 3", !low && high);

        // Lewis takes three portraits, and the order's done.
        string lewisOrder = ModEntry.Id + "_LewisPortraits";
        who.team.specialOrders.RemoveWhere(o => o.questKey.Value == lewisOrder);
        who.team.AddSpecialOrder(lewisOrder);
        var lewis = Game1.getCharacterFromName("Lewis");
        int toolIndex = who.CurrentToolIndex;
        // (An order's objectives hook in on the next update, so a moment later.)
        DelayedAction.functionAfterDelay(() =>
        {
            var order = who.team.specialOrders.FirstOrDefault(o => o.questKey.Value == lewisOrder);
            try
            {
                Check(monitor, "the order can be taken", order != null && lewis != null);
                if (order == null || lewis == null) return;
                var parsnip = Tagged("(O)24", PaintingRules.Gold);
                who.addItemToInventory(parsnip);
                who.CurrentToolIndex = who.Items.IndexOf(parsnip);
                Check(monitor, "Lewis won't take a still life for it", !lewis.tryToReceiveActiveObject(who, probe: true) || order.objectives[0].GetCount() == 0);
                who.removeItemFromInventory(parsnip);
                foreach (string sitter in new[] { "Abigail", "Gus", "Robin" })
                {
                    var p = Tagged("sitter:" + sitter, PaintingRules.Silver);
                    who.addItemToInventory(p);
                    who.CurrentToolIndex = who.Items.IndexOf(p);
                    lewis.tryToReceiveActiveObject(who);
                    (Game1.activeClickableMenu as StardewValley.Menus.DialogueBox)?.closeDialogue();
                    lewis.CurrentDialogue.Clear();
                }
                Check(monitor, "three portraits complete it", order.objectives[0].GetCount() == 3 && order.objectives[0].IsComplete(), $"{order.objectives[0].GetCount()} of 3");
            }
            finally
            {
                who.team.specialOrders.RemoveWhere(o => o.questKey.Value == lewisOrder);
                who.CurrentToolIndex = toolIndex;
            }
        }, 1000);
    }

    // Festivals, round two (1.3, step 3).
    private static void FestTest(IModHelper helper, IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        if (args.FirstOrDefault() == "gift")
        {
            var shipped = new System.Collections.Generic.Dictionary<string, int>(who.basicShipped.Pairs.ToDictionary(kv => kv.Key, kv => kv.Value));
            try
            {
                who.basicShipped.Clear();
                Check(monitor, "with nothing shipped, Sable paints a parsnip", Social.FestivalPainting.Favourite(who) == "(O)24");
                who.basicShipped["190"] = 40;     // cauliflower
                who.basicShipped["24"] = 12;
                var gift = Social.FestivalPainting.SablesPainting(who);
                var data = PaintingData.Read(gift);
                Check(monitor, "Sable's gift is their painting of what you ship most", data?.Subject == "(O)190" && data.Painter == "Sable" && gift.Quality == PaintingRules.Iridium, gift.DisplayName);
                Check(monitor, "Sable has Winter Star lines", Game1.getCharacterFromName("Sable")?.Dialogue.ContainsKey("WinterStar_GiveGift_Before") == true);
            }
            finally
            {
                who.basicShipped.Clear();
                foreach (var (id, n) in shipped) who.basicShipped[id] = n;
            }
            return;
        }
        if (Social.FestivalPainting.Current is not { } spot)
        {
            Check(monitor, "at a festival with an easel", false, "start one with museart_festival fall27 or summer28");
            return;
        }
        var festival = Game1.CurrentEvent!;
        if (args.FirstOrDefault() == "all")
        {
            // Every pigment and canvas, to see the view at its best ('museart_frame' it next).
            ArtProgress.SetLevel(who, 10);
            ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length));
        }
        Check(monitor, $"{spot.Place}: the easel is set up", festival.festivalProps.Count > 0);
        bool used = festival.checkAction(new xTile.Dimensions.Location(spot.Easel.X, spot.Easel.Y), Game1.viewport, who);
        Check(monitor, "...and opens the easel", used && Game1.activeClickableMenu is EaselMenu);
        if (Game1.activeClickableMenu is not EaselMenu menu) return;
        menu.DevPickView();
        Check(monitor, "...with the festival's own view", menu.DevViewSubject == $"view:{spot.Place}:{Game1.season.ToString().ToLowerInvariant()}", menu.DevViewSubject ?? "none");
        Check(monitor, "...named for the festival", !Views.PlaceName(menu.DevViewSubject ?? "view:x").StartsWith("view.place"), Views.PlaceName(menu.DevViewSubject ?? "view:x"));
        if (menu.DevViewThumbnail is { } thumb)
        {
            string dir = System.IO.Path.Combine(helper.DirectoryPath, "media");
            System.IO.Directory.CreateDirectory(dir);
            using var stream = System.IO.File.Create(System.IO.Path.Combine(dir, "festview_" + spot.Place + ".png"));
            thumb.SaveAsPng(stream, thumb.Width, thumb.Height);
        }
        Check(monitor, "...and in the record's views", RecordTabs.Members(who, RecordTab.Views).Any(m => m.Key == "view:" + spot.Place));
        if (menu.DevViewSubject is { } view)
        {
            menu.DevSetCanvas(args.FirstOrDefault() == "all" ? CanvasKind.Large : CanvasKind.Medium);
            menu.DevSetup(view);
            var cells = menu.DevPreview?.Cells;
            int dark = 0, total = 0;
            if (cells != null)
                foreach (byte c in cells) { total++; if (Palette.All.ElementAtOrDefault(c)?.Id == "charcoal") dark++; }
            Check(monitor, "...and isn't mostly charcoal", total > 0 && dark * 100 / total < 40, $"{dark * 100 / Math.Max(1, total)}% charcoal");
        }
    }

    // Finishing the portfolio (1.3, step 2).
    private static void BookTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var book = Portfolio.Items;
        if (args.FirstOrDefault() == "where")
        {
            monitor.Log($"After the scene: {who.currentLocation?.Name} {who.TilePoint}, health {who.health}, event {Game1.CurrentEvent?.id ?? "none"}, menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}", LogLevel.Info);
            return;
        }
        if (args.FirstOrDefault() == "click")
        {
            // Clicks through the scene's dialogue, as a player would.
            if (Game1.activeClickableMenu is DialogueBox box)
            {
                box.finishTyping();
                box.receiveLeftClick(box.xPositionOnScreen + 10, box.yPositionOnScreen + 10);
            }
            return;
        }
        if (args.FirstOrDefault() == "scene")
        {
            Game1.activeClickableMenu?.exitThisMenu();
            who.mailReceived.Add(Portfolio.BookFlag);
            who.eventsSeen.Remove(SableEvents.SummitEventId);
            // The Summit as players reach it, at Perfection. (Without it, the
            // game plays Qi's scene, then knocks you out on every visit after,
            // even the end of an event there.) Its own first-visit scene is
            // marked seen.
            Game1.player.team.farmPerfect.Value = true;
            who.mailReceived.Add("Summit_event");
            Game1.warpFarmer("Summit", 11, 13, 0);
            DelayedAction.functionAfterDelay(() =>
            {
                Check(monitor, "the Summit scene is due after painting the Summit", SableEvents.SummitSceneDue(who, "view:Summit:spring"));
                DelayedAction.functionAfterDelay(() => SableEvents.StartSummitScene(who), 1500);
                DelayedAction.functionAfterDelay(() =>
                    Check(monitor, "...and plays, with Sable", Game1.CurrentEvent?.id == SableEvents.SummitEventId && Game1.CurrentEvent.getActorByName("Sable") != null,
                        Game1.CurrentEvent?.id ?? "no event"), 3000);
            }, 2000);
            return;
        }
        void Reset()
        {
            for (int i = 0; i < book.Count; i++) book[i] = null;
            foreach (var page in Portfolio.Pages)
            {
                who.mailReceived.Remove(Portfolio.PageFlag(page));
                who.mailForTomorrow.Remove(Portfolio.PageFlag(page) + "_Letter");
            }
            who.mailReceived.Remove(Portfolio.BookFlag);
            who.mailForTomorrow.Remove(Portfolio.BookFlag + "_Letter");
            foreach (var item in who.Items.Where(Portfolio.IsKeepable).ToList())
                who.removeItemFromInventory(item);
        }
        SObject Give(string subject, int quality = PaintingRules.Silver)
        {
            var painting = DevTools.TestPainting(subject, quality);
            who.addItemToInventory(painting);
            return painting;
        }
        int Page(string id) => Array.FindIndex(Portfolio.Pages, p => p.Id == id);
        Reset();
        try
        {
            // Faces of the Valley: any one of a group fills its slot.
            int faces = Page("faces"), first = Portfolio.FirstIndex(faces);
            Check(monitor, "Shane fills the Saloon regulars", Portfolio.SlotFor(faces, Give("sitter:Shane")) == first);
            Check(monitor, "Maru fills the family", Portfolio.SlotFor(faces, Give("sitter:Maru")) == first + 1);
            Check(monitor, "Julian fills the visitor", Portfolio.SlotFor(faces, Give("sitter:" + Social.Julian.Name)) == first + 4);
            Check(monitor, "Leah has no slot on Faces", Portfolio.SlotFor(faces, Give("sitter:Leah")) < 0);

            // The Summit page stays hidden until the rest is finished.
            int summit = Page("summit");
            Check(monitor, "the Summit page is the closing one, hidden at first", Portfolio.Pages[summit].Closing && !Portfolio.IsShown(Portfolio.Pages[summit]));
            foreach (string id in new[] { "seasons", "harvest", "fine-work", "far-places", "legends", "faces" })
                for (int i = 0; i < Portfolio.Pages[Page(id)].Slots.Length; i++)
                    book[Portfolio.FirstIndex(Page(id)) + i] = DevTools.TestPainting("(O)24", PaintingRules.Silver);
            book[Portfolio.FirstIndex(faces) + 4] = null;
            Portfolio.Place(who, faces, Give("sitter:" + Social.Julian.Name));
            Check(monitor, "the last page finished finishes the book", Portfolio.BookFinished && who.mailForTomorrow.Contains(Portfolio.BookFlag + "_Letter"));
            Check(monitor, "...and the Summit page shows", Portfolio.IsShown(Portfolio.Pages[summit]));
            var mail = DataLoader.Mail(Game1.content);
            Check(monitor, "Sable's letters are in the game's mail", mail.ContainsKey(Portfolio.BookFlag + "_Letter") && mail[Portfolio.BookFlag + "_Letter"].Contains(ArtItems.GoldLeafFrameItemId)
                && mail.ContainsKey(Portfolio.PageFlag(Portfolio.Pages[faces]) + "_Letter"));
            Check(monitor, "a Summit view goes on its page", Portfolio.SlotFor(summit, Give("view:Summit:fall")) == Portfolio.FirstIndex(summit));
            Portfolio.Place(who, summit, Give("view:Summit:fall"));
            Check(monitor, "...without a letter of its own", Portfolio.IsFinished(Portfolio.Pages[summit]) && !who.mailForTomorrow.Contains(Portfolio.PageFlag(Portfolio.Pages[summit]) + "_Letter"));

            // The gold-leaf frame.
            var framed = ArtHooks.Frame(Give("view:Town:spring"), ArtItems.GoldLeaf);
            Check(monitor, "a painting framed in gold leaf hangs in the gold-leaf frame", framed.QualifiedItemId == "(F)" + ArtItems.GoldLeafSmallId && ArtItems.IsFramed(framed)
                && ItemRegistry.Exists("(O)" + ArtItems.GoldLeafFrameItemId));

            // Julian buys one painting a season, at twice the price.
            Game1.dayOfMonth = Social.Julian.FirstVisitDay;
            var julian = Social.Julian.Npc!;
            julian.IsInvisible = false;
            who.modData.Remove(ModEntry.Id + "/JulianBought");
            if (!who.friendshipData.ContainsKey(Social.Julian.Name)) who.friendshipData.Add(Social.Julian.Name, new Friendship());
            who.friendshipData[Social.Julian.Name].TalkedToToday = false;
            julian.CurrentDialogue.Clear();
            Check(monitor, "Julian doesn't offer before you've talked", !Social.JulianPurchase.Offers(julian, who));
            who.friendshipData[Social.Julian.Name].TalkedToToday = true;
            var sold = Give("(O)24", PaintingRules.Gold);
            Check(monitor, "...and does after, with a painting in your bag", Social.JulianPurchase.Offers(julian, who));
            int money = who.Money, expected = Social.JulianPurchase.OfferFor(sold);
            int paid = Social.JulianPurchase.Buy(julian, who, sold);
            Check(monitor, "he pays twice the shop price", paid == expected && who.Money == money + paid && paid == (int)Math.Round(sold.sellToStorePrice() * 2f) && !who.Items.Contains(sold), $"{paid}g");
            Check(monitor, "...and says what he thinks of it", Game1.activeClickableMenu is DialogueBox);
            (Game1.activeClickableMenu as DialogueBox)?.closeDialogue();
            Give("(O)24", PaintingRules.Gold);
            Check(monitor, "...once a season", !Social.JulianPurchase.Offers(julian, who));
            Check(monitor, "a Masterpiece gets his best word", Social.JulianPurchase.Verdict(MasterOf(Give("(O)24", PaintingRules.Iridium))) == "julian.buy.masterpiece");
        }
        finally
        {
            Reset();
            who.modData.Remove(ModEntry.Id + "/JulianBought");
        }
    }

    private static SObject MasterOf(SObject painting)
    {
        painting.modData[PaintingData.MasterpieceKey] = "1";
        return painting;
    }

    // Animals (1.3, step 1): pets, the horse and farm animals as subjects.
    private static void AnimalTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var farm = Game1.getFarm();
        var added = new System.Collections.Generic.List<NPC>();
        var addedAnimals = new System.Collections.Generic.List<long>();
        try
        {
            // Every animal's side-on frame is found, and fits the large canvas.
            int small = PaintingRules.CanvasSize(CanvasKind.Small), large = PaintingRules.CanvasSize(CanvasKind.Large);
            var sizes = Animals.AllTypes().Select(t => (t, f: Animals.Frame(t))).ToList();
            var bad = sizes.Where(x => x.f is not { } f || f.Source.Width < 8 || f.Source.Height < 8 || !Animals.Fits(x.t, large)).Select(x => x.t + (x.f is { } f ? $" {f.Source.Width}x{f.Source.Height}" : " none")).ToList();
            Check(monitor, "every animal's frame is found and fits a large canvas", bad.Count == 0, string.Join(", ", bad));
            monitor.Log("Animal frames: " + string.Join(", ", sizes.Select(x => x.f is { } f ? $"{x.t} {f.Source.Width}x{f.Source.Height}" : x.t + " none")), LogLevel.Info);
            Check(monitor, "animals have their game names", Animals.DisplayName("White Cow") != "White Cow" || Animals.DisplayName("White Cow").Length > 0);

            // Only the ones you own: none on a fresh farm, then a cow, a cat and a horse.
            bool hadAny = Animals.Available().Count > 0;
            var cow = new FarmAnimal("White Cow", Game1.Multiplayer.getNewID(), who.UniqueMultiplayerID);
            cow.Position = new Vector2(60, 15) * 64f;
            farm.animals.Add(cow.myID.Value, cow);
            addedAnimals.Add(cow.myID.Value);
            var cat = new StardewValley.Characters.Pet(62, 15, "2", "Cat");
            farm.characters.Add(cat);
            added.Add(cat);
            var horse = new StardewValley.Characters.Horse(Guid.NewGuid(), 63, 15);
            farm.characters.Add(horse);
            added.Add(horse);
            var available = Animals.Available();
            Check(monitor, "your animals can be painted", available.Contains("White Cow") && available.Contains("Cat") && available.Contains(Animals.Horse), string.Join(", ", available));
            Check(monitor, "...and not ones you don't own", hadAny || (!available.Contains("Pig") && !available.Contains("Dinosaur")));
            Check(monitor, "a pet is painted as your own pet's breed", Animals.Frame("Cat") is { } catFrame && catFrame.Texture.Name?.Contains("cat2", StringComparison.OrdinalIgnoreCase) != false, Animals.Frame("Cat")?.Texture.Name ?? "none");

            // References: a chicken fits the small canvas; a cow needs a bigger one.
            var owned = Enumerable.Range(0, Palette.All.Length).ToList();
            var chicken = Animals.Reference("White Chicken", small, owned);
            Check(monitor, "a chicken fits a small canvas", Animals.Fits("White Chicken", small) && chicken.SubjectCells > 40, $"{chicken.SubjectCells} cells");
            int medium = PaintingRules.CanvasSize(CanvasKind.Medium);
            Check(monitor, "a cow needs a bigger canvas", !Animals.Fits("White Cow", small) && Animals.Fits("White Cow", large));
            var goatRef = Animals.Reference("Goat", medium, owned);
            var goatBig = Animals.Reference("Goat", large, owned);
            Check(monitor, "a goat shrinks to fit a medium canvas", Animals.Fits("Goat", medium) && Animals.Fits("Sheep", medium) && Animals.Fits("Ostrich", medium)
                && goatRef.SubjectCells > 150 && goatRef.SubjectCells < goatBig.SubjectCells, $"{goatRef.SubjectCells} of {goatBig.SubjectCells} cells");
            Check(monitor, "the cows, the pig and the horse are large only", new[] { "White Cow", "Brown Cow", "Pig", Animals.Horse }.All(t => !Animals.Fits(t, medium) && Animals.Fits(t, large)));
            Check(monitor, "rarer animals are worth more", Animals.SubjectPrice(Animals.SubjectFor("Ostrich")) > Animals.SubjectPrice(Animals.SubjectFor("White Chicken")));

            // Through the easel: a study, kept in the record's Animals tab.
            FreshStart(who);
            var menu = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { Animals.SubjectFor("White Chicken") });
            menu.DevPaintPartly(1f, 3);
            menu.DevFinish(false);
            var painting = menu.DevPainting;
            var data = painting == null ? null : PaintingData.Read(painting);
            Check(monitor, "an animal paints as a study", data?.IsAnimal == true && data.SubjectItems.Length == 0 && painting!.DisplayName.Contains(Animals.DisplayName("White Chicken")), painting?.DisplayName ?? "none");
            Check(monitor, "...and goes in the record's Animals tab", RecordTabs.TabOfSubject(Animals.SubjectFor("White Chicken")) == RecordTab.Animals
                && RecordTabs.Members(who, RecordTab.Animals).Any(m => m.Key == Animals.SubjectFor("White Chicken") && m.Painted != null)
                && !RecordTabs.Members(who, RecordTab.Other).Any(m => Animals.IsAnimal(m.Key)));
            Check(monitor, "the record lists every animal", RecordTabs.Progress(who, RecordTab.Animals).Total == Animals.AllTypes().Count());
        }
        finally
        {
            foreach (var c in added) farm.characters.Remove(c);
            foreach (long id in addedAnimals) farm.animals.Remove(id);
        }
    }

    // Julian Vane (1.2, step 4): data, art, visits, and his comings and goings.
    private static void StallTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || Constants.CurrentSavePath != null) return;
        var who = Game1.player;
        if (args.FirstOrDefault() != "check")
        {
            Game1.season = Season.Winter;
            Game1.dayOfMonth = 15;
            Game1.timeOfDay = 1800;
            // (The game sets the day's festivals each morning; this skipped it.)
            if (!Game1.netWorldState.Value.ActivePassiveFestivals.Contains("NightMarket"))
                Game1.netWorldState.Value.ActivePassiveFestivals.Add("NightMarket");
            Game1.warpFarmer("BeachNightMarket", 7, 36, 2);
            monitor.Log("Off to the Night Market; run 'museart_stalltest check' next.", LogLevel.Info);
            return;
        }

        var market = Game1.currentLocation;
        Check(monitor, "at the Night Market, on market night", market?.Name == "BeachNightMarket" && Social.NightMarketStall.MarketTonight, market?.Name ?? "nowhere");
        Check(monitor, "the stall's action is below the bridge, where you face the boat",
            Enumerable.Range(4, 6).All(x => market?.doesTileHaveProperty(x, 37, "Action", "Buildings") == Social.NightMarketStall.ActionKey));
        Check(monitor, "the lanterns that were under the boat are gone", market?.getTileIndexAt(5, 39, "Buildings", "night market") is -1 or > 1367 || market?.getTileIndexAt(5, 39, "Buildings") != 926);
        Social.NightMarketStall.UpdateSable(1800);
        var sable = Game1.getCharacterFromName("Sable");
        Check(monitor, "Sable's not in town on market evenings", sable?.IsInvisible == true);
        var schedule = sable?.getMasterScheduleRawData();
        Check(monitor, "their schedule leaves the shop at 4:30 on market days", schedule != null && new[] { 15, 16, 17 }.All(d => schedule.ContainsKey($"winter_{d}") && schedule[$"winter_{d}"].Contains("1630")));

        // What Sable buys.
        var cells = new byte[20, 20];
        SObject Painting(string subject, string? conditions = null) =>
            PaintingItems.Create(new PaintingData { Size = 20, Cells = cells, Purity = new Purity[20, 20], Subject = subject, Conditions = conditions }, PaintingRules.Gold, 400);
        var ordinary = Painting("(O)24");
        var desert = Painting("view:Desert:winter");
        var night = Painting("view:Town:winter", "night");
        var monster = Painting(Creatures.SubjectFor("Green Slime"));
        var legend = Painting("(O)163");
        Check(monitor, "Sable buys the unusual: the desert, a night view, a monster, a legendary fish",
            new[] { desert, night, monster, legend }.All(Social.NightMarketStall.IsUnusual));
        Check(monitor, "but not an ordinary still life, or a day view of town",
            !Social.NightMarketStall.IsUnusual(ordinary) && !Social.NightMarketStall.IsUnusual(Painting("view:Town:winter")));
        Check(monitor, "a night view says so", night.getDescription().Contains("by night"), night.getDescription().Replace("\n", " "));
        Check(monitor, "at half as much again", Social.NightMarketStall.OfferFor(desert) == (int)Math.Round(desert.sellToStorePrice() * 1.5f), $"{Social.NightMarketStall.OfferFor(desert)}g for a {desert.sellToStorePrice()}g painting");

        who.modData.Remove(ModEntry.Id + "/StallBought");
        int money = who.Money;
        var bag = new[] { desert, night, monster, legend };
        foreach (var p in bag) who.addItemToInventory(p);
        for (int i = 0; i < Social.NightMarketStall.BuysPerNight; i++)
            Social.NightMarketStall.Sell(who, bag[i]);
        Check(monitor, "three sold: paid, and gone from the bag", who.Money > money && !who.Items.Contains(desert) && Social.NightMarketStall.BoughtTonight(who) == 3, $"+{who.Money - money}g");
        who.removeItemFromInventory(legend);
        who.Money = money;
        who.modData.Remove(ModEntry.Id + "/StallBought");

        // The frame for sale, and framing in it.
        var shops = DataLoader.Shops(Game1.content);
        Check(monitor, "the stall sells the Moonlit Frame, one a night", shops.TryGetValue(Social.NightMarketStall.ShopId, out var shop)
            && shop.Items.Any(i => i.ItemId == "(O)" + ArtItems.MoonlitFrameItemId && i.AvailableStock == 1));
        var framed = ArtHooks.Frame(desert, moonlit: true);
        Check(monitor, "framed in it, a painting hangs in the moonlit frame", framed.QualifiedItemId == "(F)" + ArtItems.MoonlitSmallId && ArtItems.IsFramed(framed));
        Check(monitor, "the conditions follow it into the frame", PaintingData.ConditionsOf(ArtHooks.Frame(night, moonlit: true)) == "night");
        Check(monitor, "views painted now are marked by the hour and the weather",
            ArtStudio.Painting.Views.ConditionsNow("view:Town:winter") == (Game1.timeOfDay >= ArtStudio.Painting.Views.NightFrom ? "night" : Game1.IsRainingHere(Game1.getLocationFromName("Town")) ? "rain" : null));

        // Talking to Sable from the bridge.
        who.setTileLocation(new Vector2(7, 36));
        who.FacingDirection = 2;
        Social.NightMarketStall.Open(who);
        Check(monitor, "talking to Sable from the bridge opens the stall", Game1.activeClickableMenu is DialogueBox { isQuestion: true }, Game1.activeClickableMenu?.GetType().Name ?? "nothing");
        CloseDialogue();
    }

    private static void ContestTest(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var cells = new byte[20, 20];
        SObject Painting(int quality, float stars, int difficulty)
        {
            var painting = PaintingItems.Create(new PaintingData { Size = 20, Cells = cells, Purity = new Purity[20, 20], Subject = "(O)24", Stars = stars, Difficulty = difficulty }, quality, 200);
            return painting;
        }

        // Scoring, out of 100.
        var plain = Painting(PaintingRules.Normal, 1f, 0);
        Check(monitor, "a plain one-star painting scores 33", Social.PaintingContest.Score(plain) == 33, Social.PaintingContest.Score(plain).ToString());
        var best = Painting(PaintingRules.Iridium, 5f, 100);
        best.modData[Varnish.FinishKey] = "gallery";
        best.modData[PaintingData.MasterpieceKey] = "true";
        Check(monitor, "an iridium five-star gallery-varnished Masterpiece scores 100", Social.PaintingContest.Score(best) == 100, Social.PaintingContest.Score(best).ToString());
        var gold = Painting(PaintingRules.Gold, 4f, 40);
        Check(monitor, "a gold four-star painting scores 74", Social.PaintingContest.Score(gold) == 74, Social.PaintingContest.Score(gold).ToString());

        // Rivals: in their ranges, and the same all Fair.
        var rivals = Social.PaintingContest.RivalScores(who);
        Check(monitor, "three rivals, each in range, the same each time asked", rivals.Count == 3
            && rivals.All(r => r.Score is >= 45 and <= 78) && rivals.SequenceEqual(Social.PaintingContest.RivalScores(who)),
            string.Join(", ", rivals.Select(r => $"{r.Npc} {r.Score}")));
        Check(monitor, "placings: first beats all, ties go to the farmer, last is fourth",
            Social.PaintingContest.Placing(80, new[] { 70, 60, 50 }) == 1 && Social.PaintingContest.Placing(70, new[] { 70, 60, 50 }) == 1
            && Social.PaintingContest.Placing(55, new[] { 70, 60, 50 }) == 3 && Social.PaintingContest.Placing(10, new[] { 70, 60, 50 }) == 4);
        Check(monitor, "the three ribbons exist", Enumerable.Range(1, 3).All(n => ItemRegistry.Exists($"(F){ModEntry.Id}_Ribbon{n}")));
        var festival = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Festivals/fall16");
        Check(monitor, "Julian is set up at the Fair, with a line", festival.ContainsKey(Social.Julian.Name)
            && festival["Set-Up_additionalCharacters"].Contains($"{Social.Julian.Name} {Social.PaintingContest.JulianTile.X} {Social.PaintingContest.JulianTile.Y} 2"));

        var mapStrings = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Strings/StringsFromMaps");
        Check(monitor, Social.PaintingContest.WithSve ? "with SVE, the tourist (far from his table) keeps their own line" : "the tourist by his table talks about the contest",
            mapStrings.TryGetValue("Town-Fair.4", out string? tourist) && tourist.Contains("Julian") != Social.PaintingContest.WithSve, tourist ?? "missing");

        // At the Fair: enter, have the grange judged, collect.
        if (Game1.CurrentEvent is not { isFestival: true } fair || !fair.isSpecificFestival("fall16"))
        {
            monitor.Log("Not at the Fair: run 'museart_festival fall16' first to test entering and judging.", LogLevel.Info);
            return;
        }
        var julian = fair.getActorByName(Social.Julian.Name);
        Check(monitor, "Julian is at the Fair, at his table", julian?.TilePoint == Social.PaintingContest.JulianTile, julian?.TilePoint.ToString() ?? "missing");
        if (julian == null) return;
        var easelTile = Social.PaintingContest.EaselTile;
        monitor.Log("Near his table: " + string.Join(", ", fair.actors.Where(a => Math.Abs(a.TilePoint.X - julian.TilePoint.X) <= 4 && Math.Abs(a.TilePoint.Y - julian.TilePoint.Y) <= 4).Select(a => $"{a.Name} {a.TilePoint}")), LogLevel.Info);
        Check(monitor, "his easel is solid", fair.checkForCollision(new Rectangle(easelTile.X * 64 + 16, easelTile.Y * 64 + 16, 32, 32), who));
        who.modData.Remove(ModEntry.Id + "/ContestYear");
        Social.PaintingContest.Entry(who).Clear();
        var entry = Painting(PaintingRules.Gold, 4f, 40);
        who.addItemToInventory(entry);
        var talkAt = new xTile.Dimensions.Location(julian.TilePoint.X, julian.TilePoint.Y);
        fair.grangeJudged = false;
        bool talked = fair.checkAction(talkAt, Game1.viewport, who);
        Check(monitor, "talking to him before the judging invites an entry", talked && Game1.activeClickableMenu is DialogueBox);
        (Game1.activeClickableMenu as DialogueBox)?.closeDialogue();      // (as clicking through it does, so the picker follows)
        Check(monitor, "then a painting can be picked", Game1.activeClickableMenu is Social.ContestMenu, Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        Game1.activeClickableMenu = null;
        Social.PaintingContest.Enter(who, entry);
        Check(monitor, "entered: it's on his easel, not in the bag", Social.PaintingContest.EntryOf(who) == entry && !who.Items.Contains(entry));
        if (args.FirstOrDefault() == "show")
        {
            // Leave it on the easel, and stand in front of it, for a screenshot.
            // (A real painting, a portrait of the farmer, so it shows.)
            Social.PaintingContest.Entry(who).Clear();
            Social.PaintingContest.Entry(who).Add(FarmerPortrait.Create(who));
            who.setTileLocation(new Vector2(easelTile.X - 1, easelTile.Y + 1));
            // (And the camera on the table, held there for the shot.)
            Game1.viewportFreeze = true;
            Game1.viewport.Location = new xTile.Dimensions.Location(easelTile.X * 64 - Game1.viewport.Width / 2, easelTile.Y * 64 - Game1.viewport.Height / 2);
            who.FacingDirection = 0;
            return;
        }

        int tokens = who.festivalScore;
        int placing = Social.PaintingContest.Placing(74, rivals.Select(r => r.Score));
        fair.grangeJudged = true;
        fair.checkAction(talkAt, Game1.viewport, who);
        CloseDialogue();
        Check(monitor, $"after the judging: placed {placing}, {Social.PaintingContest.Prizes[placing - 1]} tokens", who.festivalScore - tokens == Social.PaintingContest.Prizes[placing - 1], $"{who.festivalScore - tokens} tokens");
        Check(monitor, "the painting's back in the bag, the entry empty", who.Items.Contains(entry) && Social.PaintingContest.EntryOf(who) == null);
        Check(monitor, placing <= 3 ? "a ribbon for placing" : "no ribbon for fourth",
            who.Items.Any(i => i?.QualifiedItemId == $"(F){ModEntry.Id}_Ribbon{placing}") == placing <= 3);
        int after = who.festivalScore;
        fair.checkAction(talkAt, Game1.viewport, who);
        CloseDialogue();
        Check(monitor, "talking again pays nothing more", who.festivalScore == after);
        who.festivalScore = tokens;
    }

    private static void JulianTest(IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var julian = Social.Julian.Npc;
        Check(monitor, "Julian exists", julian != null);
        if (julian == null) return;
        Check(monitor, "he's called Julian, he/him, and not marriageable", julian.displayName == "Julian" && julian.Gender == StardewValley.Gender.Male && !julian.datable.Value);
        Check(monitor, "his sprites are 64x128 and his portraits 128x256", julian.Sprite.Texture.Width == 64 && julian.Sprite.Texture.Height == 128 && julian.Portrait?.Width == 128 && julian.Portrait?.Height == 256);
        Check(monitor, "his home between visits exists", Game1.getLocationFromName("blinxxy.MuseArt_JulianHome") != null);
        Check(monitor, "he loves wine and hates Joja Cola", julian.getGiftTasteForThisItem(ItemRegistry.Create("(O)348")) == NPC.gift_taste_love && julian.getGiftTasteForThisItem(ItemRegistry.Create("(O)167")) == NPC.gift_taste_hate);
        var lines = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Characters/Dialogue/JulianVane");
        var sableLines = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Characters/Dialogue/Sable");
        Check(monitor, "he has an introduction and a line for every visit day", lines.ContainsKey("Introduction")
            && new[] { "spring", "summer", "fall", "winter" }.All(s => Enumerable.Range(22, 3).All(d => lines.ContainsKey($"{s}_{d}"))), $"{lines.Count} lines");
        Check(monitor, "Sable has a word for his visit days", sableLines.ContainsKey("fall_23"));

        // Every visit day, leg by leg: from the bus, to each stop, and back.
        const string home = ModEntry.Id + "_JulianHome";
        var raw = julian.getMasterScheduleRawData();
        foreach (string day in new[] { "22", "23", "24" })
        {
            bool ok = raw.TryGetValue(day, out string? schedule);
            string from = "BusStop", problems = "";
            foreach (string leg in (schedule ?? "").Split('/'))
            {
                string[] f = leg.Split(' ');
                if (f.Length < 4) continue;
                string to = f[1];
                var location = Game1.getLocationFromName(to);
                if (location == null) { problems += $" no {to};"; continue; }
                if (!location.isTilePassable(new Vector2(int.Parse(f[2]), int.Parse(f[3])))) problems += $" {to} {f[2]},{f[3]} blocked;";
                if (to != from && StardewValley.Pathfinding.WarpPathfindingCache.GetLocationRoute(from, to, julian.Gender) == null) problems += $" no way {from}>{to};";
                from = to;
            }
            Check(monitor, $"day {day}: every stop is reachable and open, ending at the bus", ok && from == "BusStop" && problems == "", problems);
        }

        // The morning of a visit, as the game has it: he wakes at home (chosen
        // the night before), and the mod moves him to the bus door and routes
        // his day from there.
        int dayBefore = Game1.dayOfMonth;
        var homeLocation = Game1.getLocationFromName(home);
        try
        {
            Game1.dayOfMonth = 22;
            if (homeLocation != null)
                Game1.warpCharacter(julian, homeLocation, new Vector2(1, 1));
            julian.TryLoadSchedule();
            Social.Julian.StartVisit(22);
            Check(monitor, "on the 22nd he wakes at home, and the mod starts his day at the bus", homeLocation != null && julian.currentLocation?.Name == "BusStop" && julian.ScheduleKey == "22"
                && julian.Schedule?.Count > 0 && julian.Schedule.Values.First().route?.Count > 0,
                $"{julian.currentLocation?.Name}, schedule {julian.ScheduleKey ?? "none"}, {julian.Schedule?.Count ?? 0} legs");
        }
        finally
        {
            Game1.dayOfMonth = dayBefore;
            julian.TryLoadSchedule();
        }

        // Out of sight before the morning bus, after the evening one, and between visits.
        int day0 = Game1.dayOfMonth;
        try
        {
            Game1.dayOfMonth = 22;
            // (Away from the bus door, where standing after 11 means he's caught the evening bus.)
            Game1.warpCharacter(julian, "Town", new Vector2(30, 60));
            Social.Julian.UpdateVisibility(900);
            bool beforeBus = julian.IsInvisible;
            Social.Julian.UpdateVisibility(950);
            bool busPullsIn = julian.IsInvisible;
            Social.Julian.UpdateVisibility(1300);
            bool midday = julian.IsInvisible;
            Social.Julian.UpdateVisibility(2100);
            bool night = julian.IsInvisible;
            Game1.dayOfMonth = 10;
            Social.Julian.UpdateVisibility(1300);
            bool away = julian.IsInvisible;
            Check(monitor, "he's seen only while he's visiting (from 9:50, as the bus pulls in)", beforeBus && !busPullsIn && !midday && night && away,
                $"9am {beforeBus}, 9:50 {busPullsIn}, 1pm {midday}, 9pm {night}, the 10th {away}");
        }
        finally
        {
            Game1.dayOfMonth = day0;
            Social.Julian.UpdateVisibility(Game1.timeOfDay);
        }

        // Friendship holds between visits, not while he's in town.
        who.friendshipData[Social.Julian.Name] = new Friendship(500);
        Social.Julian.RememberFriendship(who);
        who.friendshipData[Social.Julian.Name].Points = 480;
        Social.Julian.KeepFriendship(who, 11);
        bool held = who.friendshipData[Social.Julian.Name].Points == 500;
        who.friendshipData[Social.Julian.Name].Points = 480;
        Social.Julian.KeepFriendship(who, 23);
        Check(monitor, "friendship holds between visits, but not after a day in town", held && who.friendshipData[Social.Julian.Name].Points == 480);
        who.friendshipData.Remove(Social.Julian.Name);
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
        Check(monitor, "it counts as glazed", painted != null && PaintingData.GlazedOf(painted) == true);
        var bare = EaselMenu.StartStillLife(who, CanvasKind.Medium, new[] { "(O)254" });
        bare.DevPaintPartly(1f, 3, techniques: false);
        bare.DevFinish(false);
        Check(monitor, "with the glazes left out, it doesn't", bare.DevPainting != null && PaintingData.GlazedOf(bare.DevPainting) == false);

        // Relaxed (1.1.5): flat references, but the Glaze tool from a medium
        // canvas, and a glazed commission's reference asks for its glazes.
        ModEntry.Config.Difficulty = Difficulty.Relaxed;
        Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
        var relaxedMedium = EaselMenu.StartStillLife(who, CanvasKind.Medium, new[] { "(O)254" });
        var relaxedLarge = EaselMenu.StartStillLife(who, CanvasKind.Large, new[] { "(O)254" });
        Check(monitor, "relaxed: flat references", !relaxedMedium.DevReference!.UsesTechniques && !relaxedLarge.DevReference!.UsesTechniques);
        Check(monitor, "relaxed: the Glaze tool from a medium canvas, no highlights",
            relaxedMedium.DevToolAvailable(PaintTool.Glaze) && !relaxedLarge.DevToolAvailable(PaintTool.Highlight)
            && !EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { "(O)254" }).DevToolAvailable(PaintTool.Glaze));
        relaxedMedium.DevPaintPartly(1f, 3);
        relaxedMedium.DevFinish(false);
        Check(monitor, "relaxed: a flat painting isn't a glazed one", relaxedMedium.DevPainting != null && PaintingData.GlazedOf(relaxedMedium.DevPainting) == false);
        Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission> {
            new() { Npc = "Pierre", ItemId = "(O)254", MinQuality = PaintingRules.Silver, Reward = 100, ExpiresOn = Game1.Date.TotalDays + 5, Accepted = true, Requirement = Shop.Commissions.Glazed } });
        var forPierre = EaselMenu.StartStillLife(who, CanvasKind.Large, new[] { "(O)254" });
        Check(monitor, "relaxed: a glazed commission asks for glazes, not highlights",
            Technique.Count(forPierre.DevReference!.Marks, Mark.Glaze) > 0 && Technique.Count(forPierre.DevReference.Marks, Mark.Highlight) == 0);
        forPierre.DevPaintPartly(1f, 3);
        forPierre.DevFinish(false);
        Check(monitor, "relaxed: glazed to it, it's a glazed painting", forPierre.DevPainting != null && PaintingData.GlazedOf(forPierre.DevPainting) == true);
        Shop.Commissions.Save(who, new System.Collections.Generic.List<Shop.Commission>());
        ModEntry.Config.Difficulty = Difficulty.Normal;

        // Masterpieces: only on linen, only when everything's right.
        SObject? Paint(bool linen, bool techniques)
        {
            FreshStart(who);
            DevTools.GivePalette(who);
            who.addItemToInventory(ItemRegistry.Create(linen ? ArtItems.QualifiedLinenId(CanvasKind.Medium) : ArtItems.QualifiedCanvasId(CanvasKind.Medium)));
            var m = new EaselMenu(who);
            Game1.activeClickableMenu = m;
            m.DevSetCanvas(CanvasKind.Medium);
            m.DevSetLinen(linen);
            m.DevBeginFromSetup("(O)254");
            if (!m.DevIsPainting) { monitor.Log("  didn't start: " + m.DevWhyNot, LogLevel.Info); return null; }
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
        if (!Game1.player.Items.Any(i => i?.QualifiedItemId == ArtItems.QualifiedCanvasId(CanvasKind.Small)))
            Game1.player.addItemToInventory(ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Small)));     // (no warning in the button's place)
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
        AddAll("Characters/Dialogue/JulianVane", Social.Julian.Name);
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
        foreach (string location in EventLocations)
            foreach (var (key, script) in Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Events/" + location))
                if (key.StartsWith(ModEntry.Id))
                    foreach (System.Text.RegularExpressions.Match m in speak.Matches(script))
                        texts.Add(($"event {key.Split('/')[0]}", m.Groups[1].Value, m.Groups[2].Value));

        // The content pack's text comes from its i18n file: every token should
        // have resolved, in the dialogue and in the rest of what it adds.
        var raw = texts.Select(t => (t.Where, t.Text)).ToList();
        foreach (string location in EventLocations)
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
            case "portfolio":
                // The book with a page half kept, and paintings in the bag that fit (museart_menu portfolio).
                {
                    var book = Portfolio.Items;
                    for (int i = 0; i < book.Count; i++) book[i] = null;
                    book[0] = DevTools.TestPainting("view:Town:spring", PaintingRules.Gold);
                    book[1] = DevTools.TestPainting("view:Town:summer", PaintingRules.Silver);
                    book[4] = DevTools.TestPainting("view:Farm:fall", PaintingRules.Iridium);
                    Game1.player.addItemToInventory(DevTools.TestPainting("view:Town:fall", PaintingRules.Silver));
                    Game1.player.addItemToInventory(DevTools.TestPainting("(O)24", PaintingRules.Gold));
                    Menus.PortfolioMenu.Open(Game1.player);
                }
                break;
            case "planchest":
                // The plan chest with a few paintings in it (museart_menu planchest).
                {
                    var drawer = Game1.player.team.GetOrCreateGlobalInventory(Shop.PlanChest.InventoryId);
                    drawer.Clear();
                    foreach (string subject in new[] { "(O)24", "view:Beach:summer", "(O)613", "sitter:Leah" })
                        drawer.Add(DevTools.TestPainting(subject, PaintingRules.Silver));
                    Shop.PlanChest.Open(Game1.player);
                }
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
