using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArtStudio.Core;
using ArtStudio.Menus;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace ArtStudio;

// Developer commands that work from the title screen (no save needed):
// rendering the easel's screens to PNG for layout review, and exporting
// real item references for ArtStudio.Sim to calibrate scoring against.
internal static class DevTools
{
    private static readonly Dictionary<string, Action<string, string[]>> Commands = new();

    // Registers a console command, remembering it so dev_commands.txt can run it too.
    public static void AddCommand(IModHelper helper, string name, string doc, Action<string, string[]> callback)
    {
        Commands[name] = callback;
        helper.ConsoleCommands.Add(name, doc, callback);
    }

    public static void Register(IModHelper helper, IMonitor monitor)
    {
#if DEBUG
        AddCommand(helper, "museart_snap", "Renders the easel screens to PNGs in the mod's snapshots folder. Usage: museart_snap [level] [relaxed|normal|hard]", (_, args) =>
            Snap(helper.DirectoryPath, monitor, args));
        AddCommand(helper, "museart_export_refs", "Writes still-life references for many items to refs.json (for ArtStudio.Sim).", (_, _) =>
            ExportRefs(helper.DirectoryPath, monitor));

        DevGame.Register(helper, monitor);
        AddCommand(helper, "museart_snapmenu", "In a game: renders whatever menu is open to dialogue_snapshots/<name>.png. Usage: museart_snapmenu <name>", (_, args) =>
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu == null) return;
            string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
            Directory.CreateDirectory(dir);
            var menu = Game1.activeClickableMenu;
            Render(menu, Path.Combine(dir, (args.FirstOrDefault() ?? "menu") + ".png"));     // at the window's size, where the menu was laid out
            monitor.Log("Menu rendered.", LogLevel.Info);
        });
        AddCommand(helper, "museart_snapsocial", "In a game: renders the social page (and Sable's profile) to dialogue_snapshots/social_*.png.", (_, _) =>
        {
            if (!Context.IsWorldReady) return;
            string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
            Directory.CreateDirectory(dir);
            AtViewport(1280, 720, () => Render(new GameMenu(GameMenu.socialTab), Path.Combine(dir, "social_1280.png")));
            monitor.Log("Social page rendered.", LogLevel.Info);
        });
        AddCommand(helper, "museart_pos", "Moves the player to a tile where they are, even during a festival. Usage: museart_pos <x> <y>", (_, args) =>
        {
            if (Context.IsWorldReady && args.Length == 2 && int.TryParse(args[0], out int x) && int.TryParse(args[1], out int y))
                Game1.player.Position = new Vector2(x, y) * 64f;
        });
        AddCommand(helper, "museart_snapcontest", "In a game: renders the Fair contest's painting picker and Julian's contest lines at both UI sizes, to dialogue_snapshots/contest_*.png.", (_, _) =>
        {
            if (!Context.IsWorldReady || Game1.getCharacterFromName(Social.Julian.Name) is not { } julian) return;
            string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
            Directory.CreateDirectory(dir);
            var painting = FarmerPortrait.Create(Game1.player);
            Game1.player.addItemToInventory(painting);
            string[] keys = { "invite", "thanks", "entered", "no-painting", "no-entry", "result.1", "result.2", "result.3", "result.4", "after" };
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
            {
                AtViewport(w, h, () => Render(new Social.ContestMenu(Game1.player, julian), Path.Combine(dir, $"contest_menu_{w}.png")));
                foreach (string key in keys)
                foreach (var (page, i) in I18n.Get("contest." + key, new { painting = painting.DisplayName, score = 74, tokens = 500, rival = "Elliott" })
                    .Split(new[] { "#$b#" }, StringSplitOptions.None).Select((p, i) => (p, i)))
                    AtViewport(w, h, () =>
                    {
                        var box = new DialogueBox(new Dialogue(julian, null, page)) { transitioning = false, transitionInitialized = true };
                        box.finishTyping();
                        Render(box, Path.Combine(dir, $"contest_{key}_{i}_{w}.png"));
                    });
            }
            Game1.player.removeItemFromInventory(painting);
            monitor.Log("Contest snapshots written.", LogLevel.Info);
        });
        AddCommand(helper, "museart_gallerynight", "Sable's gallery evening (1.2): who finds what in your portfolio book, for an empty book, portraits, and a gold painting; and renders each line at both UI sizes to dialogue_snapshots/gallery_*.png.", (_, _) =>
        {
            if (!Context.IsWorldReady) return;
            var book = Portfolio.Items;
            var saved = book.ToList();
            string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
            Directory.CreateDirectory(dir);
            void Empty() { for (int i = 0; i < book.Count; i++) book[i] = null; }
            void Check(string what, bool ok, string detail) => monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}: {detail}", ok ? LogLevel.Info : LogLevel.Error);
            string Show((string Narration, (string Speaker, string Line)[] Lines) scene) => string.Join(" / ", scene.Lines.Select(l => $"{l.Speaker}: {l.Line}"));
            var shown = new List<(string Speaker, string Text)>();
            void Keep((string Narration, (string Speaker, string Line)[] Lines) scene)
            {
                shown.Add(("", scene.Narration));
                shown.AddRange(scene.Lines.Select(l => (l.Speaker, l.Line)));
            }

            Empty();
            var scene = SableEvents.GalleryPortfolio();
            Check("an empty book: Leah finds your sketches, and Sable answers", scene.Lines.Select(l => l.Speaker).SequenceEqual(new[] { "Leah", "Sable" }), Show(scene));
            Keep(scene);

            Empty();
            book[0] = TestPainting("sitter:George", PaintingRules.Normal);
            book[1] = TestPainting("sitter:Robin", PaintingRules.Normal);
            scene = SableEvents.GalleryPortfolio();
            Check("portraits of George and Robin: Robin finds hers first", scene.Lines.Length == 1 && scene.Lines[0].Speaker == "Robin", Show(scene));
            foreach (string guest in SableEvents.GalleryGuests)
            {
                Empty();
                book[0] = TestPainting("sitter:" + guest, PaintingRules.Normal);
                Keep(SableEvents.GalleryPortfolio());
            }

            Empty();
            book[0] = TestPainting("(O)24", PaintingRules.Gold);
            book[1] = TestPainting("(O)24", PaintingRules.Normal);
            scene = SableEvents.GalleryPortfolio();
            Check("no portraits: Leah names your best painting", scene.Lines.Length == 1 && scene.Lines[0].Speaker == "Leah" && scene.Lines[0].Line.Contains("Look at that light"), Show(scene));
            Keep(scene);
            book[0] = TestPainting("(O)24", PaintingRules.Iridium);
            Keep(SableEvents.GalleryPortfolio());
            Empty();
            book[0] = TestPainting("(O)24", PaintingRules.Normal);
            Keep(SableEvents.GalleryPortfolio());

            for (int i = 0; i < book.Count; i++) book[i] = i < saved.Count ? saved[i] : null;

            int n = 0;
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
            foreach (var (speaker, text) in shown.Distinct())
            foreach (string page in text.Split(new[] { "#$b#" }, StringSplitOptions.None))
            {
                var box = speaker == "" ? new DialogueBox(page) : new DialogueBox(new Dialogue(Game1.getCharacterFromName(speaker), null, page));
                box.transitioning = false;
                box.transitionInitialized = true;
                box.finishTyping();
                int index = n++;
                AtViewport(w, h, () => Render(box, Path.Combine(dir, $"gallery_{index:00}_{(speaker == "" ? "narration" : speaker)}_{w}.png")));
            }
            monitor.Log($"Gallery lines rendered ({n}).", LogLevel.Info);
        });
        AddCommand(helper, "museart_snapstall", "In a game: renders Sable's Night Market stall (its question, shop, painting picker and lines) at both UI sizes, to dialogue_snapshots/stall_*.png.", (_, _) =>
        {
            if (!Context.IsWorldReady || Game1.getCharacterFromName("Sable") is not { } sable) return;
            string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
            Directory.CreateDirectory(dir);
            var painting = TestPainting("view:Desert:winter", PaintingRules.Gold);
            Game1.player.addItemToInventory(painting);
            var responses = new[] { "frames", "sell", "lupini", "leave" }.Select(k => new Response(k, I18n.Get("stall.answer." + k))).ToArray();
            string[] lines = { "lupini", "nothing", "full", "sold", "sold-last", "closed" };
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
            {
                AtViewport(w, h, () => Render(Opened(new DialogueBox(I18n.Get("stall.greeting"), responses)), Path.Combine(dir, $"stall_question_{w}.png")));
                AtViewport(w, h, () =>
                {
                    Utility.TryOpenShopMenu(Social.NightMarketStall.ShopId, "Sable", playOpenSound: false);
                    if (Game1.activeClickableMenu is { } shop)
                        Render(shop, Path.Combine(dir, $"stall_shop_{w}.png"));
                    Game1.activeClickableMenu = null;
                    // (Pierre's, to compare: how the game lays out its own shops at this size.)
                    Utility.TryOpenShopMenu("SeedShop", "Pierre", playOpenSound: false);
                    if (Game1.activeClickableMenu is { } pierre)
                        Render(pierre, Path.Combine(dir, $"stall_compare_pierre_{w}.png"));
                    Game1.activeClickableMenu = null;
                });
                AtViewport(w, h, () => Render(new PaintingPickerMenu(I18n.Get("stall.pick"), Social.NightMarketStall.IsUnusual, _ => { },
                    item => I18n.Get("stall.pick-hover", new { painting = item.DisplayName, price = Social.NightMarketStall.OfferFor(item) })), Path.Combine(dir, $"stall_picker_{w}.png")));
                foreach (string key in lines)
                foreach (var (page, i) in I18n.Get("stall." + key, new { painting = painting.DisplayName, price = 1350 })
                    .Split(new[] { "#$b#" }, StringSplitOptions.None).Select((p, i) => (p, i)))
                    AtViewport(w, h, () => Render(Opened(key == "closed" ? new DialogueBox(page) : new DialogueBox(new Dialogue(sable, null, page))), Path.Combine(dir, $"stall_{key}_{i}_{w}.png")));
            }
            Game1.player.removeItemFromInventory(painting);
            monitor.Log("Stall snapshots written.", LogLevel.Info);

            static DialogueBox Opened(DialogueBox box)
            {
                box.transitioning = false;
                box.transitionInitialized = true;
                box.finishTyping();
                return box;
            }
        });
        AddCommand(helper, "museart_snapdialogue", "In a game: renders the narration boxes and the longest spoken pages of Sable's events at both UI sizes, to dialogue_snapshots.", (_, _) =>
            SnapDialogue(helper.DirectoryPath, monitor));

        // Debug builds also run console commands dropped into dev_commands.txt
        // (one per line), so tests can drive a game launched without a console.
        string inbox = Path.Combine(helper.DirectoryPath, "dev_commands.txt");
        helper.Events.GameLoop.OneSecondUpdateTicked += (_, _) =>
        {
            if (!File.Exists(inbox)) return;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(inbox);
                File.Delete(inbox);
            }
            catch (IOException)
            {
                return;     // still being written; try again next second
            }
            foreach (string line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                monitor.Log($"dev_commands: {line}", LogLevel.Info);
                if (Commands.TryGetValue(parts[0], out var command))
                    command(parts[0], parts.Skip(1).ToArray());
                else
                    monitor.Log($"Unknown command '{parts[0]}'.", LogLevel.Warn);
            }
        };
#endif
    }

    // Every screen and popup the easel can show, at the two common UI sizes.
    // Each render is also audited for overlapping or spilling text; the
    // findings go to snapshots/layout_report.txt and the console.
    // Normal paints from a Palette (1.1): a wooden one, full of every colour.
    // A painting for tests and staged menus: any picture, the subject and
    // quality asked for.
    internal static SObject TestPainting(string subject, int quality, float stars = 4f, string painter = "Tester")
    {
        int size = PaintingRules.CanvasSize(CanvasKind.Small);
        var cells = ReferenceBuilder.StillLife(new[] { "(O)24" }, size, Enumerable.Range(0, Palette.All.Length).ToList()).Cells;
        return PaintingItems.Create(new PaintingData { Size = size, Cells = cells, Purity = new Purity[size, size], Subject = subject, Stars = stars, Painter = painter }, quality, 100);
    }

    internal static void GivePalette(Farmer who)
    {
        var palette = PaletteItem.Find(who);
        if (palette == null)
        {
            palette = ItemRegistry.Create("(O)" + PaletteItem.Tiers[0].Id);
            if (!who.addItemToInventoryBool(palette))
                who.Items[who.Items.Count - 1] = palette;     // (a full bag)
        }
        int full = PaletteItem.Capacity(palette, who);
        PaletteItem.Write(palette, Enumerable.Repeat(full, Palette.All.Length).ToArray());
    }

    private static void Snap(string modDir, IMonitor monitor, string[] args)
    {
        string dir = Path.Combine(modDir, "snapshots");
        if (Directory.Exists(dir))
            foreach (string old in Directory.GetFiles(dir))
                File.Delete(old);
        Directory.CreateDirectory(dir);
        int level = args.Length > 0 && int.TryParse(args[0], out int l) ? l : 4;
        string? only = args.Length > 1 ? args[1] : null;

        var savedDifficulty = ModEntry.Config.Difficulty;
        var bag = new[] { "(O)24", "(O)254", "(O)276", "(O)190", "(O)192", "(O)66", "(O)128", "(O)72", "(O)395", "(O)424" }
            .Select(id => ItemRegistry.Create(id)).ToList();
        foreach (var item in bag) Game1.player.addItemToInventory(item);
        ArtProgress.SetOwned(Game1.player, Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter || i % 3 == 0));

        var report = new List<string>();
        try
        {
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
            foreach (var (name, make) in Scenarios(level))
            {
                if (only != null && !name.Contains(only)) continue;
                AtViewport(w, h, () =>
                {
                    ModEntry.Config.Difficulty = Difficulty.Normal;
                    ArtProgress.SetLevel(Game1.player, level);
                    var menu = make();
                    Ui.Audit = new List<Ui.AuditEntry>();
                    Render(menu, Path.Combine(dir, $"{name}_{w}.png"));
                    foreach (string problem in Ui.AuditProblems())
                        report.Add($"{name} @{w}x{h}: {problem}");
                    Ui.Audit = null;
                });
            }
        }
        finally
        {
            Ui.Audit = null;
            ModEntry.Config.Difficulty = savedDifficulty;
            foreach (var item in bag) Game1.player.removeItemFromInventory(item);
        }

        File.WriteAllLines(Path.Combine(dir, "layout_report.txt"), report.Count > 0 ? report : new List<string> { "No layout problems found." });
        foreach (string line in report) monitor.Log(line, LogLevel.Warn);
        monitor.Log($"Snapshots written to {dir}; {report.Count} layout problem(s).", report.Count > 0 ? LogLevel.Warn : LogLevel.Info);
    }

    // Event dialogue as the player sees it: every 'message' box and every
    // spoken page, rendered (not measured) at both sizes.
    private static void SnapDialogue(string modDir, IMonitor monitor)
    {
        if (!Context.IsWorldReady) return;
        string dir = Path.Combine(modDir, "dialogue_snapshots");
        if (Directory.Exists(dir))
            foreach (string old in Directory.GetFiles(dir))
                File.Delete(old);
        Directory.CreateDirectory(dir);

        var message = new System.Text.RegularExpressions.Regex(@"message ""([^""]*)""");
        var speak = new System.Text.RegularExpressions.Regex(@"speak (\w+) ""([^""]*)""");
        var boxes = new List<(string Name, Func<IClickableMenu> Make)>();
        int n = 0;
        foreach (string location in new[] { Shop.ArtShop.LocationName, "Forest", "JoshHouse" })
        foreach (var (key, script) in Game1.content.Load<Dictionary<string, string>>("Data/Events/" + location))
        {
            if (!key.StartsWith(ModEntry.Id)) continue;
            foreach (System.Text.RegularExpressions.Match m in message.Matches(script))
            {
                string text = m.Groups[1].Value;
                boxes.Add(($"message_{n++}", () => Opened(new DialogueBox(Game1.parseText(text)))));
            }
            foreach (string command in Event.ParseCommands(script).Where(c => c.StartsWith("quickQuestion ")))
            {
                string[] parts = command.Substring("quickQuestion ".Length).Split("(break)")[0].Split('#');
                var responses = parts.Skip(1).Select((text, i) => new Response(i.ToString(), text)).ToArray();
                boxes.Add(($"question_{n++}", () => Opened(new DialogueBox(parts[0], responses))));
            }
            foreach (System.Text.RegularExpressions.Match m in speak.Matches(script))
            {
                string speaker = m.Groups[1].Value;
                foreach (string page in m.Groups[2].Value.Split(new[] { "#$b#", "#$e#" }, StringSplitOptions.None))
                    boxes.Add(($"speak_{n++}_{speaker}", () => Opened(new DialogueBox(new Dialogue(Game1.getCharacterFromName(speaker), null, page)))));
            }
        }

        // As it looks once open and fully typed out (it opens with a grow
        // animation that draws only the frame).
        static DialogueBox Opened(DialogueBox box)
        {
            box.transitioning = false;
            box.transitionInitialized = true;
            box.finishTyping();
            return box;
        }

        foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
        foreach (var (name, make) in boxes)
            AtViewport(w, h, () => Render(make(), Path.Combine(dir, $"{name}_{w}.png")));
        monitor.Log($"Dialogue snapshots written to {dir} ({boxes.Count} boxes).", LogLevel.Info);
    }

    internal static IEnumerable<(string Name, Func<IClickableMenu> Make)> Scenarios(int level)
    {
        string parsnip = "(O)24", melon = "(O)254", pumpkin = "(O)276";
        int moss = Palette.IndexOf("moss");

        EaselMenu Paint(CanvasKind kind, params string[] items)
        {
            GivePalette(Game1.player);            // (Normal paints from one)
            var menu = EaselMenu.StartStillLife(Game1.player, kind, items);
            menu.DevPaintPartly(0.55f, 1);
            return menu;
        }

        EaselMenu Hard(Func<EaselMenu> make)
        {
            ModEntry.Config.Difficulty = Difficulty.Hard;
            return make();
        }

        yield return ("board", () =>
        {
            Game1.player.modData[ModEntry.Id + "/Commissions"] = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new Shop.Commission { Npc = "Gus", ItemId = "(O)206", MinQuality = 2, Reward = 640, ExpiresOn = Game1.Date.TotalDays + 2 },
                new Shop.Commission { Npc = "Leah", ItemId = "(O)196", MinQuality = 1, Reward = 380, ExpiresOn = Game1.Date.TotalDays + 1 },
                new Shop.Commission { Npc = "Emily", ItemId = "(O)72", MinQuality = 4, Reward = 1520, ExpiresOn = Game1.Date.TotalDays },
                new Shop.Commission { Npc = "Pierre", ItemId = "(O)24", MinQuality = 0, Reward = 220, ExpiresOn = Game1.Date.TotalDays + 5, Accepted = true },
            });
            return new Shop.CommissionBoardMenu(Game1.player);
        });
        yield return ("setup_bag", () => { var m = new EaselMenu(Game1.player); m.DevSetup(parsnip); return m; });
        yield return ("setup_creatures", () =>
        {
            foreach (string name in new[] { "Green Slime", "Bug", "Fly", "Bat", "Rock Crab", "Dust Spirit", "Frost Bat", "Ghost", "Skeleton", "Metal Head" })
                Game1.player.stats.specificMonstersKilled[name] = 2;
            var m = new EaselMenu(Game1.player);
            m.DevShowCreatures("Dust Spirit");
            return m;
        });
        yield return ("setup_animals", () =>
        {
            var farm = Game1.getFarm();
            foreach (string type in new[] { "White Chicken", "Duck", "Rabbit", "Brown Cow", "Goat", "Sheep", "Pig", "Ostrich", "Dinosaur" })
                if (!farm.animals.Values.Any(a => a.type.Value == type))
                {
                    var animal = new FarmAnimal(type, Game1.Multiplayer.getNewID(), Game1.player.UniqueMultiplayerID) { Position = new Vector2(50, 20) * 64f };
                    farm.animals.Add(animal.myID.Value, animal);
                }
            if (!farm.characters.Any(c => c is StardewValley.Characters.Pet))
                farm.characters.Add(new StardewValley.Characters.Pet(52, 20, "1", "Dog"));
            if (!farm.characters.Any(c => c is StardewValley.Characters.Horse))
                farm.characters.Add(new StardewValley.Characters.Horse(Guid.NewGuid(), 53, 20));
            var m = new EaselMenu(Game1.player);
            m.DevShowStudies(Painting.Animals.Prefix, Painting.Animals.SubjectFor("Goat"));
            m.DevSetCanvas(CanvasKind.Medium);
            return m;
        });
        yield return ("setup_free", () => { var m = new EaselMenu(Game1.player); m.DevSetupFree(); return m; });
        yield return ("paint_normal", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevMixedPalette(); return m; });
        yield return ("paint_hard", () => Hard(() => Paint(CanvasKind.Small, parsnip)));
        yield return ("paint_large", () => Paint(CanvasKind.Large, melon, pumpkin, parsnip));
        yield return ("paint_lv10", () => { ArtProgress.SetLevel(Game1.player, 10); return Paint(CanvasKind.Medium, melon, parsnip); });
        yield return ("paint_free", () => EaselMenu.StartOriginal(Game1.player, CanvasKind.Small));
        yield return ("tech_glaze", () => { var m = Paint(CanvasKind.Medium, pumpkin); m.DevPaintPartly(0.7f, 5); m.DevSelectTool(PaintTool.Glaze); return m; });
        yield return ("tech_glaze_relaxed", () => { ModEntry.Config.Difficulty = Difficulty.Relaxed; var m = Paint(CanvasKind.Medium, pumpkin); m.DevPaintPartly(0.7f, 5); m.DevSelectTool(PaintTool.Glaze); return m; });
        yield return ("spark_canvas", () => { var m = Paint(CanvasKind.Medium, pumpkin); m.DevPaintPartly(0.5f, 5); m.DevHover(9, 12, false); return m; });
        yield return ("spark_reference", () => { var m = Paint(CanvasKind.Medium, pumpkin); m.DevPaintPartly(0.5f, 5); m.DevHover(9, 12, true); return m; });
        // The Nexus page's painting shots (1.1.5): the spark ring, and a cell
        // pointed at, framed on both grids and named under the reference.
        yield return ("shot_glaze", () => { var m = Paint(CanvasKind.Medium, pumpkin); m.DevPaintPartly(0.7f, 5); m.DevSelectTool(PaintTool.Glaze); m.DevHoldSpark(); m.DevHover(10, 12, false); return m; });
        yield return ("shot_highlight", () => { var m = Paint(CanvasKind.Large, "(O)72"); m.DevPaintPartly(0.9f, 5); m.DevSelectTool(PaintTool.Highlight); m.DevHoldSpark(); m.DevHover(16, 15, false); return m; });
        yield return ("shot_mixing", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevOpenMixWell(moss, 0.7f); m.DevHoldSpark(); m.DevFreeze(); return m; });
        yield return ("tech_highlight", () => { var m = Paint(CanvasKind.Large, "(O)72"); m.DevPaintPartly(0.9f, 5); m.DevSelectTool(PaintTool.Highlight); return m; });
        yield return ("tech_results", () => { ArtProgress.SetLevel(Game1.player, 10); var m = Paint(CanvasKind.Medium, pumpkin); m.DevPaintPartly(1f, 5); m.DevFinish(false); return m; });
        yield return ("tech_masterpiece", () =>
        {
            ArtProgress.SetLevel(Game1.player, 10);
            Game1.player.addItemToInventory(ItemRegistry.Create(ArtItems.QualifiedLinenId(CanvasKind.Medium)));
            var m = new EaselMenu(Game1.player);
            m.DevSetCanvas(CanvasKind.Medium);
            m.DevSetLinen(true);
            m.DevBeginFromSetup(pumpkin);
            m.DevPaintPerfect();
            m.DevFinish(false);
            return m;
        });
        yield return ("tech_linen", () => { ArtProgress.SetLevel(Game1.player, 8); var m = new EaselMenu(Game1.player); m.DevSetCanvas(CanvasKind.Medium); m.DevSetLinen(true); m.DevSetup(pumpkin); return m; });
        yield return ("toast_long", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevToast("Spark! You found Iridium Violet pigment!"); return m; });
        yield return ("toast_tool", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevToast("Dropper unlocks at Painting level 6."); return m; });
        yield return ("mix_start", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevOpenMix(moss, 0, armed: false); return m; });
        yield return ("mix_normal", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevOpenMix(moss, 50, armed: true); return m; });
        yield return ("mix_hard_topup", () => Hard(() => { var m = Paint(CanvasKind.Small, parsnip); m.DevOpenMix(-1, 50, armed: true); return m; }));
        yield return ("leave_prompt", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevLeavePrompt(); return m; });
        yield return ("results_good", () => { var m = Paint(CanvasKind.Small, parsnip); m.DevPaintPartly(1f, 3); m.DevFinish(false); m.DevLevelUp(5); return m; });
        yield return ("results_lightgone", () => Hard(() => { var m = Paint(CanvasKind.Small, parsnip); m.DevFinish(true); return m; }));
        yield return ("results_large", () => { var m = Paint(CanvasKind.Large, melon, pumpkin, parsnip); m.DevPaintPartly(1f, 4); m.DevFinish(false); return m; });
        yield return ("results_free", () => { var m = EaselMenu.StartOriginal(Game1.player, CanvasKind.Small); m.DevScribble(); m.DevFinish(false); return m; });
        yield return ("results_empty", () => { var m = EaselMenu.StartStillLife(Game1.player, CanvasKind.Small, new[] { parsnip }); m.DevFinish(true); return m; });
        // From 2 hearts with Sable, they write the critique.
        EaselMenu WithSable(Func<EaselMenu> make)
        {
            var friends = Game1.player.friendshipData;
            bool had = friends.TryGetValue("Sable", out var before);
            friends["Sable"] = new Friendship(500);
            try { return make(); }
            finally { if (had) friends["Sable"] = before; else friends.Remove("Sable"); }
        }
        yield return ("results_sable", () => WithSable(() => { var m = Paint(CanvasKind.Small, parsnip); m.DevPaintPartly(1f, 3); m.DevFinish(false); m.DevLevelUp(5); return m; }));
        yield return ("results_sable_large", () => WithSable(() => { var m = Paint(CanvasKind.Large, melon, pumpkin, parsnip); m.DevPaintPartly(0.6f, 4); m.DevFinish(false); return m; }));
        SObject FinishedPainting(CanvasKind kind, params string[] items)
        {
            var m = Paint(kind, items);
            m.DevPaintPartly(1f, 3);
            m.DevFinish(false);
            return m.DevPainting!;
        }

        yield return ("record", () =>
        {
            // A record with a still life, a pairing, a portrait and a view, and a milestone reached.
            var entries = new Dictionary<string, CatalogueEntry>
            {
                ["(O)24"] = new() { Count = 7, BestQuality = 2, FirstDay = 3 },
                ["(O)24|(O)254"] = new() { Count = 1, BestQuality = 1, FirstDay = 5 },
                ["sitter:Abigail"] = new() { Count = 2, BestQuality = 4, FirstDay = 9 },
                ["view:Forest:spring"] = new() { Count = 1, BestQuality = 0, FirstDay = 12 },
            };
            foreach (string id in new[] { "(O)276", "(O)190", "(O)192", "(O)66", "(O)128", "(O)72", "(O)395", "(O)424", "(O)60", "(O)62", "(O)64", "(O)68", "(O)70" })
                entries[id] = new CatalogueEntry { Count = 1, BestQuality = 1, FirstDay = 14 };
            Game1.player.modData[ModEntry.Id + "/Catalogue"] = System.Text.Json.JsonSerializer.Serialize(entries);
            Game1.player.modData[ModEntry.Id + "/CollectionPerks"] = "tubes,palette";
            return new CollectionMenu(Game1.player);
        });
        foreach (var tab in new[] { RecordTab.Crops, RecordTab.Fish, RecordTab.Portraits, RecordTab.Views, RecordTab.Creatures, RecordTab.Animals, RecordTab.Other })
            yield return ("record_" + tab.ToString().ToLowerInvariant(), () =>
            {
                var entries = new Dictionary<string, CatalogueEntry>
                {
                    ["(O)24"] = new() { Count = 7, BestQuality = 2, FirstDay = 3 },
                    ["(O)24|(O)145"] = new() { Count = 1, BestQuality = 1, FirstDay = 5 },
                    ["(O)190"] = new() { Count = 2, BestQuality = 4, FirstDay = 6 },
                    ["(O)192"] = new() { Count = 1, BestQuality = 0, FirstDay = 6 },
                    ["sitter:Abigail"] = new() { Count = 2, BestQuality = 4, FirstDay = 9 },
                    ["sitter:Leah"] = new() { Count = 1, BestQuality = 1, FirstDay = 10 },
                    ["view:Forest:spring"] = new() { Count = 1, BestQuality = 0, FirstDay = 12 },
                    ["view:Forest:fall"] = new() { Count = 1, BestQuality = 1, FirstDay = 70 },
                    ["view:Beach:summer"] = new() { Count = 1, BestQuality = 2, FirstDay = 40 },
                    ["(O)340"] = new() { Count = 1, BestQuality = 1, FirstDay = 20 },
                    ["(O)428"] = new() { Count = 3, BestQuality = 2, FirstDay = 21 },
                    ["creature:Green Slime"] = new() { Count = 2, BestQuality = 2, FirstDay = 22 },
                    ["creature:Skeleton"] = new() { Count = 1, BestQuality = 1, FirstDay = 30 },
                };
                Game1.player.modData[ModEntry.Id + "/Catalogue"] = System.Text.Json.JsonSerializer.Serialize(entries);
                Game1.player.modData[ModEntry.Id + "/CollectionPerks"] = "tubes,tab-crops-25";
                return new CollectionMenu(Game1.player, tab);
            });
        yield return ("consign", () => new Shop.ConsignMenu(Game1.player));
        for (int page = 0; page < Portfolio.Pages.Length; page++)
        {
            int p = page;
            yield return ("portfolio_" + Portfolio.Pages[p].Id, () =>
            {
                var book = Portfolio.Items;
                for (int i = 0; i < book.Count; i++) book[i] = null;
                book[Portfolio.FirstIndex(p)] = TestPainting(p == 0 ? "view:Town:spring" : "(O)24", PaintingRules.Gold);
                if (Portfolio.Pages[p].Closing)
                    Game1.player.mailReceived.Add(Portfolio.BookFlag);      // (its tab shows once the book's finished)
                return Menus.PortfolioMenu.DevCreate(Game1.player, p);
            });
        }
        yield return ("palette", () =>
        {
            var palette = ItemRegistry.Create("(O)" + PaletteItem.Tiers[1].Id);
            PaletteItem.Initialize(palette);
            Game1.player.addItemToInventory(ItemRegistry.Create("(O)" + ArtItems.TubeId(Palette.IndexOf("rose")), 4));
            return new PaletteMenu(Game1.player, palette);
        });
        yield return ("setup_framing", () =>
        {
            var painting = FinishedPainting(CanvasKind.Small, parsnip);
            Game1.player.addItemToInventory(painting);
            var m = new EaselMenu(Game1.player);
            m.DevFraming(painting);
            Game1.player.removeItemFromInventory(painting);
            return m;
        });
        yield return ("items", () =>
        {
            var painting = FinishedPainting(CanvasKind.Small, parsnip);
            var large = FinishedPainting(CanvasKind.Large, melon, pumpkin, parsnip);
            var sable = ItemRegistry.Create("(O)" + ArtItems.BrushId(Brushes.All[1]));
            ArtHooks.WearBrush(Game1.player, sable, Brushes.All[1]);
            for (int i = 0; i < 7; i++) ArtHooks.WearBrush(Game1.player, sable, Brushes.All[1]);
            return new ItemPreview(new List<Item>
            {
                painting,
                ArtHooks.Frame(painting),
                ArtHooks.Frame(large),
                ItemRegistry.Create(ArtItems.QualifiedEaselId),
                ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Small), 5),
                sable,
                ItemRegistry.Create("(O)" + ArtItems.FrameItemId),
                ItemRegistry.Create("(O)" + ArtItems.PigmentId(Palette.IndexOf("rose")), 3),
                // The Moonlit Frame (1.2) and paintings framed in it.
                ItemRegistry.Create("(O)" + ArtItems.MoonlitFrameItemId),
                ArtHooks.Frame(painting, moonlit: true),
                ArtHooks.Frame(large, moonlit: true),
            });
        });
        // Framing with a Moonlit Frame in the bag: the two styles to choose from.
        yield return ("setup_framing_moonlit", () =>
        {
            var painting = FinishedPainting(CanvasKind.Small, parsnip);
            Game1.player.addItemToInventory(painting);
            // (The last two slots of the bag, which is full by now.)
            var items = Game1.player.Items;
            items[items.Count - 1] = ItemRegistry.Create("(O)" + ArtItems.FrameItemId, 2);
            items[items.Count - 2] = ItemRegistry.Create("(O)" + ArtItems.MoonlitFrameItemId, 1);
            var m = new EaselMenu(Game1.player);
            m.DevFraming(painting);
            m.DevFrameStyle(moonlit: true);
            Game1.player.removeItemFromInventory(painting);
            return m;
        });
        // 1.3's frame picker with every style in the bag, a rare one chosen.
        yield return ("setup_framing_all", () =>
        {
            if (Game1.player.MaxItems < 36) Game1.player.increaseBackpackSize(36 - Game1.player.MaxItems);
            var painting = FinishedPainting(CanvasKind.Small, parsnip);
            Game1.player.addItemToInventory(painting);
            var items = Game1.player.Items;
            for (int i = 0; i < ArtItems.FrameStyles.Length; i++)
                items[items.Count - 1 - i] = ItemRegistry.Create("(O)" + ArtItems.FrameStyles[i].ItemId, 1 + i % 3);
            var m = new EaselMenu(Game1.player);
            m.DevFraming(painting);
            m.DevFrameStyle(ArtItems.Sandstone);
            Game1.player.removeItemFromInventory(painting);
            return m;
        });
    }

    private static void AtViewport(int w, int h, Action action)
    {
        var old = Game1.uiViewport;
        Game1.uiViewport = new xTile.Dimensions.Rectangle(0, 0, w, h);
        try { action(); }
        finally { Game1.uiViewport = old; }
    }

    private static void Render(IClickableMenu menu, string path)
    {
        var device = Game1.graphics.GraphicsDevice;
        int w = Game1.uiViewport.Width, h = Game1.uiViewport.Height;
        using var target = new RenderTarget2D(device, w, h);
        // The game's own batch: some vanilla menus draw parts with it directly.
        var batch = Game1.spriteBatch;
        var previous = device.GetRenderTargets();
        device.SetRenderTarget(target);
        device.Clear(new Color(90, 120, 80));
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        menu.draw(batch);
        batch.End();
        device.SetRenderTargets(previous);
        using var stream = File.Create(path);
        target.SaveAsPng(stream, w, h);
    }

    // Every object with a 16×16 sprite, quantized to the starter pigments and
    // to all pigments, as palette-index rows ('.' = blank).
    private static void ExportRefs(string modDir, IMonitor monitor)
    {
        var starter = Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter).ToList();
        var all = Enumerable.Range(0, Palette.All.Length).ToList();
        var output = new List<object>();
        foreach (string id in ItemRegistry.GetObjectTypeDefinition().GetAllIds())
        {
            var item = ItemRegistry.Create("(O)" + id, allowNull: true);
            if (item == null || !ReferenceBuilder.CanPaint(item) || item.Category == -999 || item.Name.Contains("Error"))
                continue;
            output.Add(new
            {
                id = item.QualifiedItemId,
                name = item.Name,
                price = item.salePrice(),
                starter = Rows(ReferenceBuilder.Subject(item.QualifiedItemId, starter)),
                all = Rows(ReferenceBuilder.Subject(item.QualifiedItemId, all)),
            });
        }
        string path = Path.Combine(modDir, "refs.json");
        File.WriteAllText(path, JsonSerializer.Serialize(output));
        monitor.Log($"Exported {output.Count} references to {path}.", LogLevel.Info);
    }

    private static string[] Rows(byte[,] cells)
    {
        int w = cells.GetLength(0), h = cells.GetLength(1);
        var rows = new string[h];
        for (int y = 0; y < h; y++)
        {
            var chars = new char[w];
            for (int x = 0; x < w; x++)
                chars[x] = cells[x, y] == PixelCanvas.Blank ? '.' : (char)('a' + cells[x, y]);
            rows[y] = new string(chars);
        }
        return rows;
    }

    // Draws one item at inventory size and as held, with its hover tooltip.
    // A row of items in inventory slots, the first two also at double size,
    // with the first item's tooltip.
    private sealed class ItemPreview : IClickableMenu
    {
        private readonly List<Item> _items;
        public ItemPreview(List<Item> items) => _items = items;

        public override void draw(SpriteBatch b)
        {
            drawTextureBox(b, 80, 80, _items.Count * 72 + 40, 120, Color.White);
            for (int i = 0; i < _items.Count; i++)
            {
                var pos = new Vector2(100 + i * 72, 108);
                b.Draw(Game1.menuTexture, new Rectangle((int)pos.X, (int)pos.Y, 64, 64), Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
                _items[i].drawInMenu(b, pos, 1f);
            }
            _items[0].drawInMenu(b, new Vector2(120, 260), 2f);
            _items[1].drawInMenu(b, new Vector2(300, 260), 2.5f);
            drawToolTip(b, _items[1].getDescription(), _items[1].DisplayName, _items[1]);
        }
    }
}
