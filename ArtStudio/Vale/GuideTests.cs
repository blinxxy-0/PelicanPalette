#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Quests;
using SObject = StardewValley.Object;

namespace ArtStudio.Vale;

// museart_guidetest: checks the painters' guide's rules in a running game (Debug only):
// the spots are found only with the guide, once each, and the eighth opens the Grove; then
// the Grove itself, and (over the next few seconds, as real warps) the way in and the way back.
// The player's flags, season and place are saved and put back, so a run changes nothing.
internal static class GuideTests
{
    private static IModHelper? _helper;

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        DevTools.AddCommand(helper, "museart_guidetest",
            "Checks the painters' guide's rules (no guide no find, a first find, a repeat, the eighth completes, an unknown id is refused), then the Gilded Grove: its season, its easels, the warp in and the frame home.",
            (_, _) => Run(monitor));
        DevTools.AddCommand(helper, "museart_foundertest",
            "Co-op checks for the Founders' easel. 'offer': this player offers a test masterpiece for real and keeps the frame and the flag. 'check': every player online has offered and holds their own Founders' Frame.",
            (_, args) => FoundersCoop(monitor, args.FirstOrDefault() ?? ""));
        DevTools.AddCommand(helper, "museart_useguide",
            "Selects the guide in the toolbar and uses it, as a right-click with it held would (for playing the guide's flow by hand).",
            (_, _) =>
            {
                var who = Game1.player;
                int slot = who.Items.ToList().FindIndex(i => i?.QualifiedItemId == "(O)" + PaintersGuide.GuideItemId);
                if (slot is >= 0 and < 12)
                    who.CurrentToolIndex = slot;
                bool used = Context.IsPlayerFree && who.CurrentItem?.QualifiedItemId == "(O)" + PaintersGuide.GuideItemId && PaintersGuide.UseGuide(who);
                monitor.Log($"Used the guide: {used}; menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}.", LogLevel.Info);
            });
        DevTools.AddCommand(helper, "museart_answer",
            "Answers the open question box with its nth answer (0 first), as a click on it would. Usage: museart_answer 0",
            (_, args) =>
            {
                if (Game1.activeClickableMenu is DialogueBox { isQuestion: true } box && int.TryParse(args.FirstOrDefault() ?? "0", out int n))
                {
                    box.selectedResponse = n;
                    box.receiveLeftClick(box.xPositionOnScreen + box.width / 2, box.yPositionOnScreen + box.height / 2);
                }
            });
        DevTools.AddCommand(helper, "museart_playerstate", "Logs whether the farmer can move, and what's holding them.", (_, _) => monitor.Log(PlayerState(), LogLevel.Info));
    }

    internal static string PlayerState()
    {
        var who = Game1.player;
        var sprite = who.FarmerSprite;
        return $"at {who.currentLocation?.Name} {who.TilePoint}; free {Context.IsPlayerFree}, CanMove {who.CanMove}, menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}, "
            + $"dialogueUp {Game1.dialogueUp}, eventUp {Game1.eventUp}, warping {Game1.isWarping}, freezeControls {who.freezePause}, "
            + $"UsingTool {who.UsingTool}, frame {sprite.CurrentFrame} (single anim {sprite.PauseForSingleAnimation}, anim {sprite.CurrentAnimation?.Count ?? 0}), carrying {who.IsCarrying()}, "
            + $"held {who.ActiveObject?.Name ?? "nothing"}, splash {PaintSplash.Playing}, warp pending {GildedGrove.WarpPending}, fade {Game1.globalFade} {Game1.fadeToBlack}";
    }

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static void Run(IMonitor monitor)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        var ours = PaintersGuide.Spots.Select(s => PaintersGuide.FoundFlag(s.Id))
            .Append(PaintersGuide.GuideTakenFlag).Append(PaintersGuide.GroveOpenFlag).ToList();
        var had = ours.Where(who.mailReceived.Contains).ToHashSet();
        void Clear() { foreach (string f in ours) who.mailReceived.Remove(f); }

        // The guide item and its quest, put back as they were (the bag's slots too).
        string itemId = "(O)" + PaintersGuide.GuideItemId;
        int Held() => who.Items.Where(i => i?.QualifiedItemId == itemId).Sum(i => i.Stack);
        void DropGuides() { for (int i = 0; i < who.Items.Count; i++) if (who.Items[i]?.QualifiedItemId == itemId) who.Items[i] = null; }
        Quest? Ours() => who.questLog.FirstOrDefault(q => q.id.Value == PaintersGuide.QuestId);
        int Quests() => who.questLog.Count(q => q.id.Value == PaintersGuide.QuestId);
        var heldBefore = who.Items.Select((it, slot) => (it, slot)).Where(x => x.it?.QualifiedItemId == itemId).ToList();
        var questBefore = who.questLog.Where(q => q.id.Value == PaintersGuide.QuestId).ToList();
        var place = who.currentLocation;
        var seasonBefore = Game1.season;
        var menuBefore = Game1.activeClickableMenu;
        // Whatever box a use opened is closed again, so the player counts as free for the next one.
        void Settle() { Game1.activeClickableMenu = menuBefore; Game1.dialogueUp = false; who.CanMove = true; }
        try
        {
            Clear();
            var r = PaintersGuide.Touch(who, "rooftops", false);
            Check(monitor, "guide: no guide, no find", r == PaintersGuide.TouchResult.NoGuide && PaintersGuide.FoundCount(who) == 0,
                $"{r}, found {PaintersGuide.FoundCount(who)}");

            who.mailReceived.Add(PaintersGuide.GuideTakenFlag);
            r = PaintersGuide.Touch(who, "rooftops", false);
            Check(monitor, "guide: first find", r == PaintersGuide.TouchResult.FirstFind && PaintersGuide.FoundCount(who) == 1,
                $"{r}, found {PaintersGuide.FoundCount(who)}");

            r = PaintersGuide.Touch(who, "rooftops", false);
            Check(monitor, "guide: a second touch doesn't count", r == PaintersGuide.TouchResult.Again && PaintersGuide.FoundCount(who) == 1,
                $"{r}, found {PaintersGuide.FoundCount(who)}");

            var results = PaintersGuide.Spots.Skip(1).Select(s => PaintersGuide.Touch(who, s.Id, false)).ToList();
            bool shape = results.Take(6).All(x => x == PaintersGuide.TouchResult.FirstFind) && results[6] == PaintersGuide.TouchResult.Completed;
            Check(monitor, "guide: the eighth completes",
                shape && who.mailReceived.Contains(PaintersGuide.GroveOpenFlag) && PaintersGuide.FoundCount(who) == 8,
                $"{string.Join(",", results)}, grove {who.mailReceived.Contains(PaintersGuide.GroveOpenFlag)}, found {PaintersGuide.FoundCount(who)}");

            bool threw = false;
            try { PaintersGuide.Touch(who, "nowhere", false); }
            catch (ArgumentException) { threw = true; }
            Check(monitor, "guide: an unknown id is refused", threw);

            // The tile action: an old tile with no id, or an unknown id, does nothing and says false.
            bool noId = PaintersGuide.OnTouch(who, new[] { GildedVale.PaintSpotAction });
            bool unknownId = PaintersGuide.OnTouch(who, new[] { GildedVale.PaintSpotAction, "nowhere" });
            Check(monitor, "guide: the action with no id does nothing", !noId && PaintersGuide.FoundCount(who) == 8, $"returned {noId}");
            Check(monitor, "guide: the action with an unknown id does nothing", !unknownId, $"returned {unknownId}");

            // The guide item, its quest, and using it.
            Clear();
            DropGuides();
            foreach (var q in questBefore) who.questLog.Remove(q);

            PaintersGuide.TakeGuide(who, false);
            Check(monitor, "guide: taking it",
                who.mailReceived.Contains(PaintersGuide.GuideTakenFlag) && Held() == 1 && Quests() == 1,
                $"flag {who.mailReceived.Contains(PaintersGuide.GuideTakenFlag)}, held {Held()}, quests {Quests()}");

            DropGuides();
            int foundBefore = PaintersGuide.FoundCount(who);
            PaintersGuide.TakeGuide(who, false);
            Check(monitor, "guide: a lost guide comes back", Held() == 1 && Quests() == 1 && PaintersGuide.FoundCount(who) == foundBefore,
                $"held {Held()}, quests {Quests()}, found {PaintersGuide.FoundCount(who)}");
            PaintersGuide.TakeGuide(who, false);
            Check(monitor, "guide: a second one isn't given while you hold one", Held() == 1 && Quests() == 1, $"held {Held()}, quests {Quests()}");

            PaintersGuide.Touch(who, "rooftops", false);
            string objective = Ours()?.currentObjective ?? "(no quest)";
            Check(monitor, "guide: the objective counts", objective.Contains('1'), objective);

            var item = ItemRegistry.Create(itemId);
            Check(monitor, "guide: it can't be given, trashed or sold",
                !item.canBeTrashed() && !item.canBeDropped() && !item.canBeShipped() && !(item is SObject o && o.canBeGivenAsGift()),
                $"trash {item.canBeTrashed()}, drop {item.canBeDropped()}, ship {item.canBeShipped()}, gift {(item as SObject)?.canBeGivenAsGift()}");
            var guideIcon = ItemRegistry.GetDataOrErrorItem(itemId);
            string guideTexture = Game1.objectData.TryGetValue(PaintersGuide.GuideItemId, out var guideData) ? guideData.Texture : "(no data)";
            Check(monitor, "guide: its own icon",
                !guideIcon.IsErrorItem && guideIcon.GetTexture() != null && guideIcon.GetSourceRect().Width == 16
                && guideTexture == "Mods/blinxxy.MuseArt.Vale/PaintersGuide",
                guideTexture);

            who.mailReceived.Add(PaintersGuide.GroveOpenFlag);
            var farm = Game1.getLocationFromName("Farm");
            who.currentLocation = farm;
            bool usedOutside = PaintersGuide.UseGuide(who);
            bool stayed = Game1.currentLocation == place && who.currentLocation == farm;
            Settle();
            who.currentLocation = place;
            Check(monitor, "guide: outside the Vale", !usedOutside && stayed, $"returned {usedOutside}");

            who.currentLocation = Game1.getLocationFromName(ValeBus.Vale);
            Game1.activeClickableMenu = new DialogueBox("busy");
            bool usedBusy = PaintersGuide.UseGuide(who);
            bool menuKept = Game1.activeClickableMenu is DialogueBox { isQuestion: false };
            Settle();
            who.currentLocation = place;
            Check(monitor, "guide: not while busy", !usedBusy && menuKept, $"returned {usedBusy}");

            var vale = Game1.getLocationFromName(ValeBus.Vale);
            who.currentLocation = vale;
            bool usedInVale = PaintersGuide.UseGuide(who);
            bool asked = Game1.activeClickableMenu is DialogueBox { isQuestion: true };
            Settle();
            vale.afterQuestion = null;
            who.currentLocation = place;
            Check(monitor, "guide: in the Vale, open", usedInVale && asked, $"returned {usedInVale}, asked {asked}");

            // Anywhere in the Vale, its interiors too: the question.
            var house = Game1.getLocationFromName(ValeResidents.ValeHouse);
            who.currentLocation = house;
            bool usedIndoors = house != null && PaintersGuide.UseGuide(who);
            bool askedIndoors = Game1.activeClickableMenu is DialogueBox { isQuestion: true };
            Settle();
            if (house != null)
                house.afterQuestion = null;
            who.currentLocation = place;
            Check(monitor, "guide: in a Vale interior, open", usedIndoors && askedIndoors && ValeBus.IsInVale(house) && !ValeBus.IsInVale(farm),
                $"{house?.Name ?? "no Vale House"}: returned {usedIndoors}, asked {askedIndoors}");

            // In the Grove itself: not the Vale's line, but its own, and nothing done.
            var groveHere = Game1.getLocationFromName(GildedGrove.Location);
            who.currentLocation = groveHere;
            bool usedInGrove = groveHere != null && PaintersGuide.UseGuide(who);
            var groveBox = (Game1.activeClickableMenu as DialogueBox)?.dialogues.ToList() ?? new List<string>();
            Settle();
            who.currentLocation = place;
            string groveLine = PaintersGuide.Text("vale.guide.in_grove");
            Check(monitor, "guide: in the Grove, its own line", groveHere != null && !usedInGrove && groveBox.Count == 1 && groveBox[0] == groveLine
                && groveLine != PaintersGuide.Text("vale.guide.not_here") && !ValeBus.IsInVale(groveHere),
                $"returned {usedInGrove}, shown '{string.Join(" | ", groveBox)}'");

            // The spots on the Vale's map, and the guide's frame in the Vale House, through their tile actions.
            Clear();
            SpotChecks(monitor, who, vale, Settle);
            Clear();
            DropGuides();
            foreach (var q in who.questLog.Where(q => q.id.Value == PaintersGuide.QuestId).ToList()) who.questLog.Remove(q);
            BoardCheck(monitor, who, Held, Quests, Settle);
            Clear();
            GraveChecks(monitor, who, vale, Settle);

            // ---- The Gilded Grove ----
            var grove = Game1.getLocationFromName(GildedGrove.Location);
            Check(monitor, "grove: the location exists", grove != null);
            if (grove != null)
            {
                var seasons = new List<string>();
                var views = new List<string>();
                foreach (Season s in Enum.GetValues<Season>())
                {
                    Game1.season = s;
                    seasons.Add($"{s}: {grove.GetSeason()}");
                    views.Add(Views.SubjectFor(grove));
                }
                Check(monitor, "grove: always fall", seasons.All(x => x.EndsWith(": Fall")), string.Join(", ", seasons));
                Check(monitor, "grove: one view", views.All(v => v.EndsWith(":fall")), string.Join(", ", views.Distinct()));
                Game1.season = Season.Spring;
                grove.updateSeasonalTileSheets();
                var sheets = grove.Map.TileSheets.Select(t => t.ImageSource).Where(src => src.Contains("outdoorsTileSheet")).ToList();
                Check(monitor, "grove: fall sheets in spring", sheets.Count > 0 && sheets.All(src => src.Contains("fall_")), string.Join(", ", sheets));
                Game1.season = seasonBefore;
                grove.updateSeasonalTileSheets();
                var weather = grove.GetWeather();
                Check(monitor, "grove: always sunny", weather.Weather == "Sun" && !grove.IsRainingHere() && !grove.IsSnowingHere(), weather.Weather);

                // Answering the question happens in its box: with the player not free, WarpIn does nothing.
                who.mailReceived.Add(PaintersGuide.GroveOpenFlag);
                who.currentLocation = vale;
                Game1.activeClickableMenu = new DialogueBox("busy");
                bool warpedBusy = GildedGrove.WarpIn(who, false);
                bool noWarp = Game1.locationRequest == null;
                Settle();
                who.currentLocation = place;
                Check(monitor, "grove: no warp in while busy", !warpedBusy && noWarp, $"returned {warpedBusy}, warp {(noWarp ? "none" : Game1.locationRequest?.Name)}");

                // The warp at the end of the paint wash checks again: not at 2am, not with a box up.
                who.currentLocation = vale;
                int timeBefore = Game1.timeOfDay;
                Game1.timeOfDay = 2600;
                bool late = GildedGrove.FinishWarp(who, true);
                bool lateNone = Game1.locationRequest == null;
                Game1.timeOfDay = timeBefore;
                Game1.activeClickableMenu = new DialogueBox("busy");
                bool busy = GildedGrove.FinishWarp(who, true);
                bool busyNone = Game1.locationRequest == null;
                Settle();
                who.currentLocation = place;
                Check(monitor, "grove: the wash's end checks again", !late && lateNone && !busy && busyNone,
                    $"at 2am returned {late} (warp {(lateNone ? "none" : "started")}), busy returned {busy} (warp {(busyNone ? "none" : "started")})");

                // The frame never sets you down in water, nor on a tile walled in on every side.
                Point? water = null, shut = null;
                var back = vale.Map.GetLayer("Back");
                for (int y = 1; y < back.LayerHeight - 1; y++)
                    for (int x = 1; x < back.LayerWidth - 1; x++)
                    {
                        var v = new Vector2(x, y);
                        if (water == null && vale.isWaterTile(x, y))
                            water = new Point(x, y);
                        else if (shut == null && !vale.isWaterTile(x, y) && vale.isTilePassable(v)
                                 && new[] { new Vector2(x + 1, y), new Vector2(x - 1, y), new Vector2(x, y + 1), new Vector2(x, y - 1) }.All(n => !vale.isTilePassable(n)))
                            shut = new Point(x, y);
                    }
                bool wet = water != null && GildedGrove.Landable(vale, water.Value);
                bool walled = shut != null && GildedGrove.Landable(vale, shut.Value);
                Check(monitor, "grove: the frame never lands in water or walled in", water != null && !wet && !walled,
                    $"water {water?.ToString() ?? "none found"} landable {wet}, walled-in {shut?.ToString() ?? "none found"} landable {walled}");
            }

            if (grove != null)
            {
                FoundersChecks(monitor, who, grove);
                EchoChecks(monitor, who, grove);
                GroveFishChecks(monitor, who, grove);
            }

            PageChecks(monitor, who, Settle);

            var bad = PaintersGuide.StringKeys.Where(k => { string t = PaintersGuide.Text(k); return t.Length == 0 || t == k; }).ToList();
            Check(monitor, "guide: every guide string resolves", bad.Count == 0,
                bad.Count == 0 ? $"{PaintersGuide.StringKeys.Count()} strings" : "missing: " + string.Join(", ", bad));
        }
        finally
        {
            Game1.season = seasonBefore;
            who.currentLocation = place;
            Settle();
            Clear();
            foreach (string f in had) who.mailReceived.Add(f);
            DropGuides();
            foreach (var (it, slot) in heldBefore) who.Items[slot] = it;
            foreach (var q in who.questLog.Where(q => q.id.Value == PaintersGuide.QuestId).ToList()) who.questLog.Remove(q);
            foreach (var q in questBefore) who.questLog.Add(q);
        }
        if (Game1.getLocationFromName(GildedGrove.Location) != null)
            GroveWarps(monitor);
    }

    // ---- The spots on the map ----

    // The eight spots on the map (the painters' guide design, section 4; tools/vale_spots.py).
    private static readonly string[] MapSpots = { "rooftops", "northlight", "wildflowers", "lakewindow", "parterre", "falls", "tree", "stage" };

    // Every PaintSpot tile on the Vale's map names one of the guide's spots, each on both foot tiles of
    // one paint rock, and the four that stand today are there. Then each, through its tile action as the game
    // runs it: without the guide, the plain line and no find; with it, that spot found (its splash
    // starts); again, the line, and still one found.
    private static void SpotChecks(IMonitor monitor, Farmer who, GameLocation vale, Action settle)
    {
        var tiles = new Dictionary<string, List<Point>>();
        var bad = new List<string>();
        var layer = vale.Map.GetLayer("Buildings");
        for (int y = 0; y < layer.LayerHeight; y++)
            for (int x = 0; x < layer.LayerWidth; x++)
            {
                string? action = vale.doesTileHaveProperty(x, y, "Action", "Buildings");
                string[] words = action?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                if (words.Length == 0 || words[0] != GildedVale.PaintSpotAction)
                    continue;
                if (words.Length != 2 || !PaintersGuide.Spots.Any(s => s.Id == words[1]))
                {
                    bad.Add($"'{action}' at ({x}, {y})");
                    continue;
                }
                if (!tiles.TryGetValue(words[1], out var list))
                    tiles[words[1]] = list = new List<Point>();
                list.Add(new Point(x, y));
            }
        // found row by row, so a rock's two foot tiles come left then right
        var notRocks = tiles.Where(kv => kv.Value.Count != 2 || kv.Value[1] != kv.Value[0] + new Point(1, 0)).Select(kv => kv.Key).ToList();
        var missing = MapSpots.Where(id => !tiles.ContainsKey(id)).ToList();
        Check(monitor, "guide: spots on the map", bad.Count == 0 && notRocks.Count == 0 && missing.Count == 0,
            string.Join(", ", tiles.Select(kv => $"{kv.Key} at {kv.Value[0]}"))
            + (bad.Count > 0 ? "; bad: " + string.Join(", ", bad) : "")
            + (notRocks.Count > 0 ? "; not on one rock: " + string.Join(", ", notRocks) : "")
            + (missing.Count > 0 ? "; missing: " + string.Join(", ", missing) : ""));

        var placeBefore = who.currentLocation;
        var detail = new List<string>();
        bool all = true;
        try
        {
            who.currentLocation = vale;
            foreach (var (id, feet) in tiles)
            {
                foreach (var s in PaintersGuide.Spots)
                    who.mailReceived.Remove(PaintersGuide.FoundFlag(s.Id));
                who.mailReceived.Remove(PaintersGuide.GuideTakenFlag);
                var foot = feet[feet.Count - 1];           // the rock's right foot: either tile works
                string action = vale.doesTileHaveProperty(foot.X, foot.Y, "Action", "Buildings")!;
                var at = new xTile.Dimensions.Location(foot.X, foot.Y);

                bool noGuide = vale.performAction(action, who, at);
                bool plainLine = Game1.activeClickableMenu is DialogueBox && !PaintSplash.Playing && PaintersGuide.FoundCount(who) == 0;
                settle();

                who.mailReceived.Add(PaintersGuide.GuideTakenFlag);
                bool first = vale.performAction(action, who, at);
                bool found = PaintSplash.Playing && who.mailReceived.Contains(PaintersGuide.FoundFlag(id)) && PaintersGuide.FoundCount(who) == 1;
                PaintSplash.Stop();
                settle();

                bool again = vale.performAction(action, who, at);
                bool line = Game1.activeClickableMenu is DialogueBox && !PaintSplash.Playing && PaintersGuide.FoundCount(who) == 1;
                settle();

                bool ok = noGuide && plainLine && first && found && again && line;
                all &= ok;
                detail.Add($"{id}: {(ok ? "ok" : $"no guide {noGuide}/{plainLine}, first {first}/{found}, again {again}/{line}")}");
            }
        }
        finally
        {
            PaintSplash.Stop();
            settle();
            who.currentLocation = placeBefore;
        }
        Check(monitor, "guide: each spot's rock, through its tile action", all && tiles.Count > 0, string.Join("; ", detail));
    }

    // The guide's frame in the Vale House: its tile action, by the guest books, with floor in front
    // of it; touched with no guide taken, it gives the guide and its quest.
    private static void BoardCheck(IMonitor monitor, Farmer who, Func<int> held, Func<int> quests, Action settle)
    {
        var house = Game1.getLocationFromName(ValeResidents.ValeHouse);
        var tile = PaintersGuide.BoardTile;
        string? action = house?.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings");
        bool front = house != null && house.isTilePassable(new Vector2(tile.X, tile.Y + 1));
        bool taken = false;
        var placeBefore = who.currentLocation;
        try
        {
            if (house != null && action != null)
            {
                who.currentLocation = house;
                taken = house.performAction(action, who, new xTile.Dimensions.Location(tile.X, tile.Y));
            }
        }
        finally
        {
            Game1.afterDialogues = null;
            settle();
            who.currentLocation = placeBefore;
        }
        bool gave = who.mailReceived.Contains(PaintersGuide.GuideTakenFlag) && held() == 1 && quests() == 1;
        Check(monitor, "guide: the frame in the Vale House", action == GildedVale.TakeGuideAction && front && taken && gave,
            $"{tile}: '{action ?? "(no action)"}', floor in front {front}, handled {taken}, flag {who.mailReceived.Contains(PaintersGuide.GuideTakenFlag)}, held {held()}, quests {quests()}");
    }

    // ---- The graveyard (the design's section 6) ----

    // The seven stones on the Vale's map, each through its tile action as the game runs it: its box
    // shows that stone's own text (vale.grave.<id>, from ValeStrings), page for page, and reading it
    // sets the flags the residents' lines wait for (put back after). Then Lupini's Monday morning:
    // open ground beside the graveyard's bench, reached from the bus stop. And the two lines' wiring:
    // the trigger actions' conditions parse, and the dialogue keys they add are in the two NPCs' files.
    private static void GraveChecks(IMonitor monitor, Farmer who, GameLocation vale, Action settle)
    {
        var found = new Dictionary<string, Point>();
        var bad = new List<string>();
        var layer = vale.Map.GetLayer("Buildings");
        for (int y = 0; y < layer.LayerHeight; y++)
            for (int x = 0; x < layer.LayerWidth; x++)
            {
                string[] words = vale.doesTileHaveProperty(x, y, "Action", "Buildings")?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                if (words.Length == 0 || words[0] != ValeGraves.GraveAction)
                    continue;
                if (words.Length != 2 || !ValeGraves.Ids.Contains(words[1]) || found.ContainsKey(words[1]))
                    bad.Add($"'{string.Join(" ", words)}' at ({x}, {y})");
                else
                    found[words[1]] = new Point(x, y);
            }

        var flags = ValeGraves.Ids.Select(ValeGraves.FlagFor).Append(ValeGraves.ReadFlag).ToList();
        var had = flags.Where(who.mailReceived.Contains).ToHashSet();
        var placeBefore = who.currentLocation;
        var detail = new List<string>();
        bool all = bad.Count == 0 && found.Count == ValeGraves.Ids.Length;
        try
        {
            who.currentLocation = vale;
            foreach (string id in ValeGraves.Ids)
            {
                if (!found.TryGetValue(id, out var p))
                {
                    detail.Add($"{id}: not on the map");
                    continue;
                }
                foreach (string f in flags)
                    who.mailReceived.Remove(f);
                string key = ValeGraves.Key(id), text = PaintersGuide.Text(key);
                string action = vale.doesTileHaveProperty(p.X, p.Y, "Action", "Buildings")!;
                bool handled = vale.performAction(action, who, new xTile.Dimensions.Location(p.X, p.Y));
                var shown = (Game1.activeClickableMenu as DialogueBox)?.dialogues.ToList() ?? new List<string>();
                var want = ValeGraves.Pages(id);
                bool same = shown.Count == 2 && shown.SequenceEqual(want) && text != key && text.Contains(id.ToUpperInvariant());
                bool flagged = who.mailReceived.Contains(ValeGraves.ReadFlag) && who.mailReceived.Contains(ValeGraves.FlagFor(id))
                    && flags.Count(who.mailReceived.Contains) == 2;
                settle();
                bool ok = handled && same && flagged;
                all &= ok;
                detail.Add(ok ? $"{id} ok" : $"{id} at {p}: handled {handled}, pages {shown.Count}/{want.Count} same {same}, flags {flagged}");
            }
            bool unknown = ValeGraves.OnGrave(who, "nobody");
            all &= !unknown && Game1.activeClickableMenu == null;
            settle();
        }
        finally
        {
            settle();
            who.currentLocation = placeBefore;
            foreach (string f in flags)
                who.mailReceived.Remove(f);
            foreach (string f in had)
                who.mailReceived.Add(f);
        }
        Check(monitor, "graves: seven stones read", all,
            string.Join("; ", detail) + (bad.Count > 0 ? "; bad: " + string.Join(", ", bad) : ""));

        // The residents (and so Lupini's schedule and the two lines) are only there with the pack's
        // ValeResidents on and their art in it (data/lupini.json): with them off there's nothing to check.
        if (!Game1.content.DoesAssetExist<Dictionary<string, string>>("Characters/schedules/Lupini"))
        {
            monitor.Log("[INFO] graves: Lupini's Monday and the residents' lines not checked: the residents are switched off (ValeResidents)", LogLevel.Info);
            return;
        }

        // Lupini's Monday: the first leg, in the Vale, on open ground beside the bench's seat.
        string mon = Game1.content.Load<Dictionary<string, string>>("Characters/schedules/Lupini").GetValueOrDefault("Mon") ?? "(no Mon)";
        string[] leg = mon.Split('/')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool standing = false;
        string where = string.Join(" ", leg);
        if (leg.Length >= 5 && leg[1] == ValeBus.Vale && int.TryParse(leg[2], out int lx) && int.TryParse(leg[3], out int ly))
        {
            var tile = new Point(lx, ly);
            var seat = new HashSet<int> { 2362 - 2040, 2363 - 2040, 2367 - 2040 };     // the game's bench, its seat (Town sheet)
            bool beside = new[] { lx - 1, lx + 1 }.Any(nx => layer.Tiles[nx, ly] is { } t && t.TileSheet.Id == "Town" && seat.Contains(t.TileIndex));
            standing = GildedGrove.Landable(vale, tile) && beside;
            where += $": open and reachable {GildedGrove.Landable(vale, tile)}, beside the bench {beside}";
        }
        Check(monitor, "graves: Lupini's Monday tile walkable", standing, where);

        // The two lines: Tobin's about the urns (from 4 hearts, once a stone's been read) and Lupini's about Bram.
        var triggers = Game1.content.Load<List<StardewValley.GameData.TriggerActionData>>("Data/TriggerActions").ToDictionary(t => t.Id);
        var lines = new (string Trigger, string Npc, string Topic)[]
        {
            (ModEntry.Id + "_ValeUrnsTalk", "TobinClay", ModEntry.Id + "_ValeUrns"),
            (ModEntry.Id + "_ValeBramTalk", "Lupini", ModEntry.Id + "_ValeBram"),
            (ModEntry.Id + "_ValeGroveTalk", "BeaThistle", ModEntry.Id + "_ValeGrove"),
        };
        var wiring = new List<string>();
        bool wired = true;
        foreach (var (trigger, npc, topic) in lines)
        {
            bool parses = triggers.TryGetValue(trigger, out var t) && GameStateQuery.Parse(t.Condition).All(q => q.Error == null)
                && t.Actions?.Any(a => a.StartsWith("AddConversationTopic " + topic + " ")) == true;
            bool said;
            try { said = Game1.content.Load<Dictionary<string, string>>("Characters/Dialogue/" + npc).TryGetValue(topic, out string? line) && line.Length > 0 && !line.Contains("{{"); }
            catch (Exception) { said = false; }
            wired &= parses && said;
            wiring.Add($"{npc}: trigger {parses}, line {said}");
        }
        Check(monitor, "graves: Tobin's urns, Lupini's Bram and Bea's Grove lines are wired", wired, string.Join("; ", wiring));

        // Bea's: the topic is added only once the Grove has opened, and the trigger isn't set to repeat (an action
        // marked applied runs once per save, so it isn't added again after she's said it).
        {
            var beaWho = Game1.player;
            bool beaHad = beaWho.mailReceived.Contains(PaintersGuide.GroveOpenFlag);
            try
            {
                var bea = triggers.TryGetValue(ModEntry.Id + "_ValeGroveTalk", out var bt) ? bt : null;
                beaWho.mailReceived.Remove(PaintersGuide.GroveOpenFlag);
                bool closed = bea != null && !GameStateQuery.CheckConditions(bea.Condition);
                beaWho.mailReceived.Add(PaintersGuide.GroveOpenFlag);
                bool open = bea != null && GameStateQuery.CheckConditions(bea.Condition);
                bool once = bea != null && bea.Trigger == "DayStarted" && bea.MarkActionApplied;
                Check(monitor, "graves: Bea's Grove topic waits for the Grove and is added once", closed && open && once,
                    $"closed {closed}, open {open}, once {once}");
            }
            finally
            {
                if (!beaHad)
                    beaWho.mailReceived.Remove(PaintersGuide.GroveOpenFlag);
            }
        }
    }

    // ---- The Grove's two easels ----

    // A painting made straight from its data (no easel session, so no XP or records change).
    private static SObject TestPainting(bool masterpiece)
    {
        int n = Core.PaintingRules.CanvasSize(Core.CanvasKind.Small);
        return PaintingItems.Create(new PaintingData
        {
            Size = n,
            Cells = new byte[n, n],
            Purity = new Core.Purity[n, n],
            Subject = "(O)254",
            Stars = 5f,
            Painter = "Test",
            Masterpiece = masterpiece,
        }, Core.PaintingRules.Iridium, 1000);
    }

    private static FrameStyle? FoundersStyle => ArtItems.MoreFrames.FirstOrDefault(s => s.Id == "founders");

    private static int FoundersFrames(Farmer who) =>
        FoundersStyle is { } style ? who.Items.CountId("(O)" + style.ItemId) : 0;

    // The Founders' easel's rules and the hidden easel's head start. The bag is emptied for the checks
    // and put back after, slot for slot, with the flag, the menu and the player's place.
    private static void FoundersChecks(IMonitor monitor, Farmer who, GameLocation grove)
    {
        var bag = who.Items.ToList();
        bool hadFlag = who.mailReceived.Contains(GildedGrove.FoundersFlag);
        var menuBefore = Game1.activeClickableMenu;
        var placeBefore = who.currentLocation;
        bool Flag() => who.mailReceived.Contains(GildedGrove.FoundersFlag);
        bool Holds(Item item) => who.Items.Contains(item);
        try
        {
            for (int i = 0; i < who.Items.Count; i++)
                who.Items[i] = null;
            who.mailReceived.Remove(GildedGrove.FoundersFlag);

            var style = FoundersStyle;
            var entry = style != null && Frames.Styles.TryGetValue(style.Id, out var e) ? e : default;
            Check(monitor, "founders: in the frames table",
                style != null && entry == (FrameRarity.Epic, FrameTrait.Sparks) && Frames.Of(style) == entry,
                style == null ? "no founders style" : $"{entry.Rarity} {entry.Trait}");

            var plain = TestPainting(false);
            who.addItemToInventoryBool(plain);
            string plainLine = GildedGrove.LineFor(who, plain), emptyLine = GildedGrove.LineFor(who, null);
            bool r = GildedGrove.Offer(who, plain);
            bool emptyHands = GildedGrove.Offer(who, null);
            Check(monitor, "founders: not a masterpiece", !r && !emptyHands && Holds(plain) && FoundersFrames(who) == 0 && !Flag(),
                $"returned {r} (empty hands {emptyHands}), kept {Holds(plain)}, frames {FoundersFrames(who)}, flag {Flag()}");

            var framed = ArtHooks.Frame(TestPainting(true), ArtItems.Plain);
            who.addItemToInventoryBool(framed);
            string framedLine = GildedGrove.LineFor(who, framed);
            r = GildedGrove.Offer(who, framed);
            Check(monitor, "founders: framed", !r && Holds(framed) && FoundersFrames(who) == 0 && !Flag(),
                $"returned {r}, kept {Holds(framed)}, frames {FoundersFrames(who)}, flag {Flag()}");

            var master = TestPainting(true);
            who.addItemToInventoryBool(master);
            string masterLine = GildedGrove.LineFor(who, master);
            bool canvasBefore = GildedGrove.ShowsFinishedCanvas(who);
            r = GildedGrove.Offer(who, master);
            Check(monitor, "founders: a masterpiece", r && !Holds(master) && FoundersFrames(who) == 1 && Flag(),
                $"returned {r}, kept {Holds(master)}, frames {FoundersFrames(who)}, flag {Flag()}");

            var second = TestPainting(true);
            who.addItemToInventoryBool(second);
            string thanksLine = GildedGrove.LineFor(who, second);
            r = GildedGrove.Offer(who, second);
            Check(monitor, "founders: only once", !r && Holds(second) && FoundersFrames(who) == 1,
                $"returned {r}, kept {Holds(second)}, frames {FoundersFrames(who)}");

            string lines = $"plain {plainLine}, empty {emptyLine}, framed {framedLine}, masterpiece {masterLine}, after {thanksLine}";
            Check(monitor, "founders: what the easel says",
                plainLine == "vale.grove.founders.not_masterpiece" && emptyLine == plainLine && framedLine == "vale.grove.founders.framed"
                && masterLine == "vale.grove.founders.ask" && thanksLine == "vale.grove.founders.thanks", lines);

            bool canvasAfter = GildedGrove.ShowsFinishedCanvas(who);
            Check(monitor, "founders: the finished canvas, for that player only",
                !canvasBefore && canvasAfter && GildedGrove.FinishedCanvasTexture(grove) != null,
                $"before {canvasBefore}, after {canvasAfter}, art {(GildedGrove.FinishedCanvasTexture(grove) != null ? "found" : "missing")}");

            // The hidden easel opens the easel with one spark already caught; other easels start at none.
            Game1.activeClickableMenu = null;
            who.currentLocation = grove;
            bool opened = GildedGrove.OnHiddenEasel(who, GildedGrove.HiddenEaselTile);
            var menu = Game1.activeClickableMenu as Menus.EaselMenu;
            int atOpen = menu?.DevSparksCaught ?? -1;
            menu?.DevBeginStillLife(Core.CanvasKind.Small, new[] { "(O)254" });
            int atStart = menu?.DevSparksCaught ?? -1;
            Game1.activeClickableMenu = null;
            ArtHooks.OpenEasel(who);
            int ordinary = (Game1.activeClickableMenu as Menus.EaselMenu)?.DevSparksCaught ?? -1;
            Game1.activeClickableMenu = null;
            Check(monitor, "hidden easel: a spark to start", opened && menu != null && atOpen == 1 && atStart == 1 && ordinary == 0,
                $"opened {opened}, menu {menu != null}, sparks at open {atOpen}, at the start {atStart}, at another easel {ordinary}");
        }
        finally
        {
            Game1.activeClickableMenu = menuBefore;
            who.currentLocation = placeBefore;
            for (int i = 0; i < who.Items.Count; i++)
                who.Items[i] = i < bag.Count ? bag[i] : null;
            who.mailReceived.Remove(GildedGrove.FoundersFlag);
            if (hadFlag)
                who.mailReceived.Add(GildedGrove.FoundersFlag);
        }
    }

    // ---- The painted echoes ----

    // Four echoes on the Grove's map (ids agnes, hollis, mabel, bram), each 2 x 3 tiles with its
    // action on both tiles of its foot, each showing its own memory: two pages, with the page-break
    // marker never on screen.
    private static void EchoChecks(IMonitor monitor, Farmer who, GameLocation grove)
    {
        var feet = new List<string>();
        int animated = 0;
        static bool Animated(xTile.Tiles.Tile? t) => t is xTile.Tiles.AnimatedTile { TileFrames.Length: >= 2 };
        for (int y = 0; y < grove.Map.Layers[0].LayerHeight; y++)
            for (int x = 0; x < grove.Map.Layers[0].LayerWidth; x++)
            {
                string? action = grove.doesTileHaveProperty(x, y, "Action", "Buildings");
                var words = action?.Split(' ');
                if (words != null && words[0] == GildedGrove.EchoAction)
                {
                    feet.Add(words.Length > 1 ? words[1] : "");
                    // the foot tile and the two Front tiles above it
                    if (Animated(grove.Map.GetLayer("Buildings").Tiles[x, y])
                        && Animated(grove.Map.GetLayer("Front").Tiles[x, y - 1])
                        && Animated(grove.Map.GetLayer("Front").Tiles[x, y - 2]))
                        animated++;
                }
            }
        var found = feet.Distinct().ToList();
        bool ids = found.OrderBy(i => i).SequenceEqual(GildedGrove.EchoIds.OrderBy(i => i))
            && found.All(id => feet.Count(f => f == id) == 2);
        var menuBefore = Game1.activeClickableMenu;
        var placeBefore = who.currentLocation;
        var detail = new List<string>();
        bool allShown = true;
        try
        {
            who.currentLocation = grove;
            foreach (string id in GildedGrove.EchoIds)
            {
                Game1.activeClickableMenu = null;
                var pages = GildedGrove.EchoPages(id);
                bool shown = GildedGrove.OnEcho(who, id);
                var box = Game1.activeClickableMenu as DialogueBox;
                var onScreen = box?.dialogues ?? new List<string>();
                bool ok = shown && pages.Count == 2 && onScreen.Count == 2 && onScreen.SequenceEqual(pages)
                    && onScreen.All(p => p.Length > 0 && !p.Contains("#$b#") && !p.Contains("$b"));
                allShown &= ok;
                detail.Add($"{id}: {onScreen.Count} pages{(ok ? "" : " (BAD)")}");
            }
            Game1.activeClickableMenu = null;
            bool unknown = GildedGrove.OnEcho(who, "nobody") || GildedGrove.OnEcho(who, null);
            bool noMenu = Game1.activeClickableMenu == null;
            Check(monitor, "echoes: four, each with a 2-wide foot", found.Count == 4 && ids, $"foot tiles [{string.Join(",", feet)}]");
            Check(monitor, "echoes: each idles (all six tiles animated)", animated == 8, $"{animated} of 8 columns");
            Check(monitor, "echoes: each shows its own memory, two pages, no marker", allShown, string.Join("; ", detail));
            Check(monitor, "echoes: an unknown id shows nothing", !unknown && noMenu);
        }
        finally
        {
            Game1.activeClickableMenu = menuBefore;
            who.currentLocation = placeBefore;
        }
    }

    // ---- The painted fish ----

    // It's in the Grove's fish list and the Vale's isn't; it has its data and its own icon; and the
    // game's own fish picker gives it at the Grove's pool, never in the Vale's water.
    private static void GroveFishChecks(IMonitor monitor, Farmer who, GameLocation grove)
    {
        const string id = "blinxxy.MuseArt.Vale_PaintedFish";
        var locations = DataLoader.Locations(Game1.content);
        bool InList(string loc) => locations.TryGetValue(loc, out var data) && data.Fish.Any(f => f.ItemId == "(O)" + id || f.Id == id);
        Check(monitor, "grove fish: only here", InList(GildedGrove.Location) && !InList(ValeBus.Vale),
            $"grove {InList(GildedGrove.Location)}, vale {InList(ValeBus.Vale)}");

        var obj = Game1.objectData.TryGetValue(id, out var o) ? o : null;
        var icon = ItemRegistry.GetDataOrErrorItem("(O)" + id);
        Check(monitor, "grove fish: item, fishing data and its own icon",
            obj != null && obj.Category == SObject.FishCategory && !obj.ExcludeFromFishingCollection
            && DataLoader.Fish(Game1.content).ContainsKey(id) && !icon.IsErrorItem && icon.GetSourceRect().Width == 16
            && icon.GetTexture() != null && obj.Texture == "Mods/blinxxy.MuseArt.Vale/PaintedFish",
            obj == null ? "no object" : $"{obj.Texture}, price {obj.Price}");

        // The pool: any water tile on the Grove's map.
        Vector2? pool = null;
        for (int y = 0; y < grove.Map.Layers[0].LayerHeight && pool == null; y++)
            for (int x = 0; x < grove.Map.Layers[0].LayerWidth; x++)
                if (grove.isWaterTile(x, y)) { pool = new Vector2(x, y); break; }
        var vale = Game1.getLocationFromName(ValeBus.Vale);
        int level = who.fishingLevel.Value;
        try
        {
            who.fishingLevel.Value = 10;
            int here = 0, there = 0;
            if (pool != null)
                for (int i = 0; i < 400; i++)
                    if (GameLocation.GetFishFromLocationData(grove.Name, pool.Value, 5, who, false, false, grove)?.QualifiedItemId == "(O)" + id)
                        here++;
            foreach (var tile in new[] { new Vector2(61, 18), new Vector2(65, 63) })
                for (int i = 0; i < 300 && vale != null; i++)
                    if (GameLocation.GetFishFromLocationData(vale.Name, tile, 5, who, false, false, vale)?.QualifiedItemId == "(O)" + id)
                        there++;
            Check(monitor, "grove fish: the picker gives it in the Grove's pool, never in the Vale's water",
                pool != null && here > 0 && there == 0, $"pool {pool}, grove {here}/400, vale {there}/600");
        }
        finally { who.fishingLevel.Value = level; }
    }

    // Co-op (tools/test/run_mp.ps1): the host offers, then the farmhand; then each player has their
    // own flag and their own frame. 'offer' keeps what it changes (a throwaway co-op farm).
    private static void FoundersCoop(IMonitor monitor, string mode)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        if (mode == "offer")
        {
            var others = Game1.getOnlineFarmers().Where(f => f != who && f.mailReceived.Contains(GildedGrove.FoundersFlag)).Select(f => f.Name).ToList();
            var master = TestPainting(true);
            who.addItemToInventoryBool(master);
            bool r = GildedGrove.Offer(who, master);
            Check(monitor, $"founders co-op: {(Context.IsMainPlayer ? "the host" : "the farmhand")} offers",
                r && !who.Items.Contains(master) && FoundersFrames(who) == 1 && who.mailReceived.Contains(GildedGrove.FoundersFlag),
                $"returned {r}, frames {FoundersFrames(who)}, already offered: {(others.Count > 0 ? string.Join(", ", others) : "nobody")}");
        }
        else if (mode == "check")
        {
            var farmers = Game1.getOnlineFarmers().ToList();
            string detail = string.Join("; ", farmers.Select(f => $"{f.Name}: flag {f.mailReceived.Contains(GildedGrove.FoundersFlag)}, frames {FoundersFrames(f)}"));
            Check(monitor, "founders co-op: each player has their own frame",
                farmers.Count >= 2 && farmers.All(f => f.mailReceived.Contains(GildedGrove.FoundersFlag) && FoundersFrames(f) == 1), detail);
        }
        else
            monitor.Log("Usage: museart_foundertest offer|check", LogLevel.Warn);
    }

    // ---- What the boxes show ----

    // A page shows a code if it has a page break, a '#', or a $ and a letter or digit (portrait and
    // page codes only work in a villager's own dialogue).
    private static readonly System.Text.RegularExpressions.Regex Codes = new(@"#|\$[A-Za-z0-9]");

    // The pages the open box shows, whatever kind of box it is.
    private static List<string> OnScreen()
    {
        if (Game1.activeClickableMenu is not DialogueBox box)
            return new List<string>();
        if (box.characterDialogue is { } d)
            return d.dialogues.Select(l => l.Text ?? "").ToList();
        return box.dialogues.ToList();
    }

    private static void PageChecks(IMonitor monitor, Farmer who, Action settle)
    {
        var shown = new List<(string Where, string Page)>();
        void Grab(string where)
        {
            var pages = OnScreen();
            if (pages.Count == 0)
                shown.Add((where, "(no box)"));
            shown.AddRange(pages.Select(p => (where, p)));
            settle();
        }
        var ours = PaintersGuide.Spots.Select(s => PaintersGuide.FoundFlag(s.Id)).Append(PaintersGuide.GuideTakenFlag).Append(PaintersGuide.GroveOpenFlag).ToList();
        var placeBefore = who.currentLocation;
        try
        {
            foreach (string f in ours) who.mailReceived.Remove(f);
            // Bea's words when you take the guide: as hers if she's about, and as a plain box if not.
            PaintersGuide.TakeGuide(who);
            Grab("the frame");
            bool beaHere = Game1.getCharacterFromName(ValeResidents.Bea) != null;
            PaintersGuide.BeaSays(PaintersGuide.Text("vale.guide.take"));
            Grab(beaHere ? "Bea" : "Bea (not about)");
            PaintersGuide.BeaSays(PaintersGuide.Text("vale.guide.take"), asObject: true);
            Grab("Bea's words, plain");
            PaintersGuide.TakeGuide(who);
            Grab("the frame again");

            who.currentLocation = Game1.getLocationFromName(ValeBus.Vale);
            PaintersGuide.Touch(who, "rooftops");
            PaintSplash.FinishNow();
            Grab("a first find");
            PaintersGuide.Touch(who, "rooftops");
            Grab("a find again");
            foreach (var s in PaintersGuide.Spots.Skip(1).Take(6))
                who.mailReceived.Add(PaintersGuide.FoundFlag(s.Id));
            PaintersGuide.Touch(who, PaintersGuide.Spots[7].Id);
            PaintSplash.FinishNow();
            Grab("the last page");
            who.mailReceived.Remove(PaintersGuide.GuideTakenFlag);
            PaintersGuide.Touch(who, "rooftops");
            Grab("a rock, no guide");
            who.currentLocation = Game1.getLocationFromName("Farm");
            PaintersGuide.UseGuide(who);
            Grab("the guide outside the Vale");
            who.currentLocation = Game1.getLocationFromName(GildedGrove.Location);
            PaintersGuide.UseGuide(who);
            Grab("the guide in the Grove");
        }
        finally
        {
            PaintSplash.Stop();
            settle();
            who.currentLocation = placeBefore;
            foreach (string f in ours) who.mailReceived.Remove(f);
        }
        foreach (string id in GildedGrove.EchoIds)
            shown.AddRange(GildedGrove.EchoPages(id).Select(p => ($"echo {id}", p)));
        foreach (string id in ValeGraves.Ids)
            shown.AddRange(ValeGraves.Pages(id).Select(p => ($"grave {id}", p)));
        // The Grove's own lines are shown whole: each must be one clean page.
        foreach (string key in GildedGrove.StringKeys.Where(k => !k.StartsWith("vale.echo.")))
            shown.Add((key, PaintersGuide.Text(key)));

        var coded = shown.Where(x => Codes.IsMatch(x.Page) || x.Page == "(no box)").ToList();
        Check(monitor, "guide: no codes on screen", coded.Count == 0,
            coded.Count == 0 ? $"{shown.Count} pages from {shown.Select(x => x.Where).Distinct().Count()} places"
                : string.Join("; ", coded.Select(x => $"{x.Where}: '{(x.Page.Length > 60 ? x.Page[..60] + "..." : x.Page)}'")));
    }

    // ---- The way in and the way back, as real warps over the next few seconds ----

    private sealed record Step(string What, Func<bool> Ready, Action Then);

    private static void GroveWarps(IMonitor monitor)
    {
        var who = Game1.player;
        string placeBefore = who.currentLocation.NameOrUniqueName;
        Point tileBefore = who.TilePoint;
        int facingBefore = who.FacingDirection;
        bool hadGrove = who.mailReceived.Contains(PaintersGuide.GroveOpenFlag);
        var vale = Game1.getLocationFromName(ValeBus.Vale);
        var gate = new Vector2(ValeGraves.GraveyardGateTile.X, ValeGraves.GraveyardGateTile.Y);
        SObject? blocker = null;
        NPC? stander = null;
        int timeBefore = Game1.timeOfDay;
        Color lightBefore = Game1.outdoorLight;
        string dayLight = "", nightLight = "";
        int nightTicks = 0;
        string Light() => $"lighting {Game1.drawLighting}, outdoor {Game1.outdoorLight}";

        bool Settled() => Context.IsPlayerFree && !Game1.isWarping && Game1.locationRequest == null && !PaintSplash.Playing && !GildedGrove.WarpPending;
        bool In(string name) => Game1.currentLocation?.Name == name && Settled();
        // Says yes to the question just asked, the way its box would, and closes the box.
        void SayYes(GameLocation where)
        {
            var answer = where.afterQuestion;
            where.afterQuestion = null;
            Game1.activeClickableMenu = null;
            Game1.dialogueUp = false;
            answer?.Invoke(who, "Yes");
        }

        // A splash holds the player for its time (1.5 s, or 3 s grand), then lets go and calls back.
        int splashTicks = 0;
        bool splashDone = false, heldThrough = true;
        void StartSplash(bool grand, float? hold = null)
        {
            PaintSplash.HoldAt = hold;
            who.CanMove = true;
            splashTicks = 0;
            splashDone = false;
            heldThrough = true;
            PaintSplash.Play(PaintersGuide.Spots[0].Palette, grand, () => splashDone = true);
        }
        bool SplashOver()
        {
            splashTicks++;
            if (PaintSplash.Playing && who.CanMove)
                heldThrough = false;
            return !PaintSplash.Playing;
        }
        // A splash whatever its hold says, or one a box took over halfway, ends at last (or after
        // 200 ticks the check gives up and stops it).
        bool menuMidway = false, splashEnded = false;
        bool SplashOverOrStuck()
        {
            SplashOver();
            if (splashTicks == 30 && menuMidway)
            {
                Game1.activeClickableMenu = new DialogueBox("busy");
                who.CanMove = false;
            }
            return !PaintSplash.Playing || splashTicks > 200;
        }
        void EndStuck()
        {
            splashEnded = !PaintSplash.Playing;
            if (!splashEnded)
                PaintSplash.Stop();
        }
        int guideSlot = -1;
        bool guideAdded = false;
        int toolBefore = who.CurrentToolIndex;
        // The guide in hand, as a player holds it: its toolbar slot selected (one is put in a free
        // toolbar slot, and taken out again after, if it isn't in the toolbar).
        void HoldGuide()
        {
            int at = who.Items.ToList().FindIndex(i => i?.QualifiedItemId == "(O)" + PaintersGuide.GuideItemId);
            if (at < 0 || at >= 12)
            {
                at = Enumerable.Range(0, 12).Where(i => i >= who.Items.Count || who.Items[i] == null).DefaultIfEmpty(-1).First();
                if (at >= 0)
                {
                    while (who.Items.Count <= at)
                        who.Items.Add(null);
                    who.Items[at] = ItemRegistry.Create("(O)" + PaintersGuide.GuideItemId);
                    guideAdded = true;
                }
            }
            guideSlot = at;
            if (at >= 0)
                who.CurrentToolIndex = at;
        }
        void DropAddedGuide()
        {
            if (guideAdded && guideSlot >= 0 && guideSlot < who.Items.Count && who.Items[guideSlot]?.QualifiedItemId == "(O)" + PaintersGuide.GuideItemId)
                who.Items[guideSlot] = null;
            guideAdded = false;
            who.CurrentToolIndex = toolBefore;
        }
        Point clickAt = Point.Zero;
        Vector2 walkFrom = Vector2.Zero;
        int walkTicks = 0, walkDir = 0, waited = 0, walked = 0;
        string arrival = "";
        void SplashCheck(string what, int ticks) =>
            Check(monitor, what, splashDone && heldThrough && who.CanMove && Math.Abs(splashTicks - ticks) <= 2,
                $"{splashTicks} ticks (expected {ticks}), held {heldThrough}, called back {splashDone}, can move {who.CanMove}");

        who.mailReceived.Add(PaintersGuide.GroveOpenFlag);
        var steps = new List<Step>
        {
            new("guide: a splash ends and lets go", () => Settled(), () => StartSplash(false)),
            new("guide: a splash ends and lets go", SplashOver, () =>
            {
                SplashCheck("guide: a splash ends and lets go", 90);
                StartSplash(true);
            }),
            new("guide: the grand splash ends and lets go", SplashOver, () =>
            {
                SplashCheck("guide: the grand splash ends and lets go", 180);
                // museart_splashhold left set: the splash still ends, a second past its time, and the hold goes with it.
                StartSplash(false, 0.5f);
            }),
            new("guide: a held splash still ends", SplashOverOrStuck, () =>
            {
                EndStuck();
                bool holdGone = PaintSplash.HoldAt == null;
                PaintSplash.HoldAt = null;
                Check(monitor, "guide: a held splash still ends", splashEnded && splashTicks <= 90 + 60 + 2 && holdGone && who.CanMove,
                    $"ended {splashEnded} after {splashTicks} ticks, hold cleared {holdGone}, can move {who.CanMove}");
                // A box opens halfway through: the splash leaves the box's hold alone, and lets go once it closes.
                menuMidway = true;
                StartSplash(false);
            }),
            new("guide: a splash leaves a box's hold alone", SplashOverOrStuck, () =>
            {
                menuMidway = false;
                EndStuck();
                bool kept = !who.CanMove && Game1.activeClickableMenu is DialogueBox;
                Game1.activeClickableMenu = null;
                Game1.dialogueUp = false;
                walkTicks = 0;
                arrival = $"ended {splashEnded} after {splashTicks} ticks, box's hold kept {kept}";
                Check(monitor, "guide: a splash leaves a box's hold alone", splashEnded && kept, arrival);
                // Nor does it hold or let go of a player an event, or passing out, has.
                bool before = PaintSplash.OwnedElsewhere();
                Game1.eventUp = true;
                bool inEvent = PaintSplash.OwnedElsewhere();
                Game1.eventUp = false;
                who.passedOut = true;
                bool passedOut = PaintSplash.OwnedElsewhere();
                who.passedOut = false;
                Check(monitor, "guide: a splash minds events and passing out", !before && inEvent && passedOut,
                    $"free {before}, event {inEvent}, passed out {passedOut}");
            }),
            new("guide: a splash lets go after the box", () => ++walkTicks > 3, () =>
                Check(monitor, "guide: a splash lets go after the box", who.CanMove, $"{arrival}; can move {who.CanMove} once it closed")),
            // Right-click with the guide held, through the game's own Game1.pressActionButton, standing in
            // the Vale: whatever is in front takes the click first, and the guide opens only when nothing does.
            new("guide: right-click", () => Settled(), () =>
            {
                clickAt = ClickSpot(vale);
                Game1.warpFarmer(ValeBus.Vale, clickAt.X, clickAt.Y, 2);
            }),
            // (once the screen has faded in: the game takes no action press while it fades)
            new("guide: right-click", () => In(ValeBus.Vale) && !Game1.fadeToBlack && !Game1.globalFade, () =>
            {
                HoldGuide();
                RightClickChecks(monitor, who, vale, clickAt);
            }),
            // From one of the Vale's interiors (the Vale House, in front of the guide's frame): used, yes, the Grove.
            new("grove: warp in from a Vale interior", () => Settled(),
                () => Game1.warpFarmer(ValeResidents.ValeHouse, PaintersGuide.BoardTile.X, PaintersGuide.BoardTile.Y + 1, 0)),
            new("grove: warp in from a Vale interior", () => In(ValeResidents.ValeHouse), () =>
            {
                HoldGuide();
                PaintersGuide.UseGuide(who);
                SayYes(who.currentLocation);
            }),
            new("grove: warp in from a Vale interior", () => In(GildedGrove.Location), () =>
            {
                Check(monitor, "grove: warp in from a Vale interior", who.TilePoint == GildedGrove.ArrivalTile, $"at {who.TilePoint}, arrival {GildedGrove.ArrivalTile}");
                GildedGrove.WarpHome(who, false);
            }),
            new("grove: warp in", () => Settled(), () => Game1.warpFarmer(ValeBus.Vale, 20, 7, 2)),
            new("grove: warp in", () => In(ValeBus.Vale), () =>
            {
                // As a player does it: the guide in hand, used, and yes.
                HoldGuide();
                PaintersGuide.UseGuide(who);
                SayYes(vale);
            }),
            new("grove: warp in", () => In(GildedGrove.Location), () =>
            {
                Check(monitor, "grove: warp in", who.TilePoint == GildedGrove.ArrivalTile, $"at {who.TilePoint}, arrival {GildedGrove.ArrivalTile}");
                Check(monitor, "grove: paint motes in the Grove", GildedGrove.MoteTotal > 0, $"{GildedGrove.MoteTotal} motes");
                dayLight = Light();
                // Then walk, holding a key as a player would, towards an open tile beside the arrival.
                arrival = PlayerState();
                var here = who.currentLocation;
                (int dx, int dy, int dir)[] ways = { (-1, 0, 3), (1, 0, 1), (0, 1, 2), (0, -1, 0) };
                walkDir = ways.Where(w => GildedGrove.Open(here, new Point(who.TilePoint.X + w.dx, who.TilePoint.Y + w.dy)))
                    .Select(w => w.dir).DefaultIfEmpty(3).First();
                walkFrom = who.Position;
                waited = walked = 0;
            }),
            // The screen fades in first (the game holds the farmer for that); then they walk.
            // (waited and walked are counted from the arrival step.)
            new("grove: arrive free", () =>
            {
                if (walked == 0 && !who.CanMove && waited < 180)
                {
                    waited++;
                    walkFrom = who.Position;
                    return false;
                }
                if (walked == 0)
                    HoldKey(walkDir);
                return ++walked > 20;
            }, () =>
            {
                HoldKey(null);
                float moved = Vector2.Distance(walkFrom, who.Position);
                who.Halt();
                var sprite = who.FarmerSprite;
                bool posed = sprite.PauseForSingleAnimation || who.UsingTool || who.freezePause > 0;
                Check(monitor, "grove: arrive free", moved >= 32 && !posed && Context.IsPlayerFree && who.CanMove,
                    $"free to move after {waited} ticks, then walked {moved:0} px in 20 holding {who.CurrentItem?.Name ?? "nothing"}"
                    + $" (carried overhead {who.IsCarrying()}, posed {posed}, window focused {Game1.game1.IsActiveNoOverlay}); on arrival: {arrival}");
                DropAddedGuide();
                Game1.timeOfDay = 2200;
            }),
            new("grove: golden at night", () => ++nightTicks > 30, () =>
            {
                nightLight = Light();
                Game1.timeOfDay = timeBefore;
                Check(monitor, "grove: golden at night", nightLight == dayLight && Game1.drawLighting && Game1.outdoorLight == GildedGrove.GroveLight,
                    $"day: {dayLight}; 10pm: {nightLight}");
                GildedGrove.OnFrame(who);
                SayYes(who.currentLocation);
            }),
            new("grove: the frame home", () => In(ValeBus.Vale), () =>
            {
                Check(monitor, "grove: the frame home", who.TilePoint == ValeGraves.GraveyardGateTile, $"at {who.TilePoint}, gate {ValeGraves.GraveyardGateTile}");
                Check(monitor, "grove: the motes stop outside it", GildedGrove.MoteTotal == 0, $"{GildedGrove.MoteTotal} motes");
                if (!vale.Objects.ContainsKey(gate))
                {
                    blocker = ItemRegistry.Create<SObject>("(BC)130");      // a chest on the gate tile
                    vale.Objects.Add(gate, blocker);
                }
                GildedGrove.WarpIn(who, false);
            }),
            new("grove: the frame home, blocked", () => In(GildedGrove.Location), () =>
            {
                GildedGrove.OnFrame(who);
                SayYes(who.currentLocation);
            }),
            new("grove: the frame home, blocked", () => In(ValeBus.Vale), () =>
            {
                Point at = who.TilePoint, gateTile = ValeGraves.GraveyardGateTile;
                int far = Math.Max(Math.Abs(at.X - gateTile.X), Math.Abs(at.Y - gateTile.Y));
                Check(monitor, "grove: the frame home, blocked", blocker != null && at != gateTile && far == 1 && GildedGrove.Open(vale, at),
                    $"at {at}, gate {gateTile}, {far} away, blocker {blocker != null}");
                // Now someone standing on the gate instead of a chest.
                if (blocker != null && vale.Objects.TryGetValue(gate, out var placed) && placed == blocker)
                    vale.Objects.Remove(gate);
                blocker = null;
                stander = new NPC(new AnimatedSprite("Characters/Abigail", 0, 16, 32), gate * 64f, 2, ModEntry.Id + "_GateTester");
                vale.characters.Add(stander);
                GildedGrove.WarpIn(who, false);
            }),
            new("grove: the frame home, someone on the gate", () => In(GildedGrove.Location), () =>
            {
                GildedGrove.OnFrame(who);
                SayYes(who.currentLocation);
            }),
            new("grove: the frame home, someone on the gate", () => In(ValeBus.Vale), () =>
            {
                Point at = who.TilePoint, gateTile = ValeGraves.GraveyardGateTile;
                int far = Math.Max(Math.Abs(at.X - gateTile.X), Math.Abs(at.Y - gateTile.Y));
                Check(monitor, "grove: the frame home, someone on the gate", stander != null && at != gateTile && far == 1 && GildedGrove.Landable(vale, at),
                    $"at {at}, gate {gateTile}, {far} away, {stander?.Name} at {stander?.TilePoint}");
            }),
        };
        RunSteps(monitor, steps, () =>
        {
            HoldKey(null);
            DropAddedGuide();
            if (blocker != null && vale.Objects.TryGetValue(gate, out var o) && o == blocker)
                vale.Objects.Remove(gate);
            if (stander != null)
                vale.characters.Remove(stander);
            Game1.timeOfDay = timeBefore;
            Game1.outdoorLight = lightBefore;
            if (!hadGrove)
                who.mailReceived.Remove(PaintersGuide.GroveOpenFlag);
            Game1.activeClickableMenu = null;
            Game1.dialogueUp = false;
            who.CanMove = true;
            Game1.warpFarmer(placeBefore, tileBefore.X, tileBefore.Y, facingBefore);
            monitor.Log("Grove warp checks done; back where you were.", LogLevel.Info);
        });
    }

    // ---- Right-click with the guide held ----

    // Where to stand for the right-click checks: an open tile you can walk to near the graveyard's gate,
    // with open ground, nothing growing and no tile action on the tile below it (the one you face, facing down).
    private static Point ClickSpot(GameLocation vale)
    {
        var gate = ValeGraves.GraveyardGateTile;
        for (int r = 0; r <= 10; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                        continue;
                    var p = new Point(gate.X + dx, gate.Y + dy);
                    var front = new Point(p.X, p.Y + 1);
                    var box = new Rectangle(front.X * 64, front.Y * 64, 64, 64);
                    if (GildedGrove.Landable(vale, p) && GildedGrove.Open(vale, front)
                        && vale.doesTileHaveProperty(front.X, front.Y, "Action", "Buildings") == null
                        && !vale.terrainFeatures.ContainsKey(front.ToVector2())
                        && !vale.largeTerrainFeatures.Any(f => f.getBoundingBox().Intersects(box)))
                        return p;
                }
        return gate;
    }

    // The player stands at `at`, holding the guide, facing down, and right-clicks the tile in front
    // (Game1.pressActionButton, with the cursor on that tile). On bare
    // ground the guide opens (its page: the Grove's flag is put aside for this) and isn't used up. With a
    // person, a pet, a placed object (a keg with its wine ready), a tree, a bush, a ready crop, or a tile
    // action (a grave, faced from below) in front, the game's own action happens and the guide doesn't open.
    // Everything put down is taken away again, and the bag, the skills' XP, the graves' flags and the
    // ground are put back.
    private static void RightClickChecks(IMonitor monitor, Farmer who, GameLocation vale, Point at)
    {
        string guideId = "(O)" + PaintersGuide.GuideItemId;
        var front = new Vector2(at.X, at.Y + 1);
        var bag = who.Items.Select(i => (Item: i, Stack: i?.Stack ?? 0)).ToList();
        var xp = who.experiencePoints.ToArray();
        int debrisBefore = vale.debris.Count;
        var graveFlags = ValeGraves.Ids.Select(ValeGraves.FlagFor).Append(ValeGraves.ReadFlag).ToList();
        var readBefore = graveFlags.Where(who.mailReceived.Contains).ToHashSet();
        bool hadGrove = who.mailReceived.Contains(PaintersGuide.GroveOpenFlag);
        bool mouseBefore = Game1.wasMouseVisibleThisFrame;
        var mouseStateBefore = Game1.oldMouseState;
        float cursorBefore = Game1.mouseCursorTransparency;
        var results = new List<(string What, bool Ok, string Detail)>();
        int Guides() => who.Items.Where(i => i?.QualifiedItemId == guideId).Sum(i => i.Stack);
        void Add(string what, bool ok, string detail) => results.Add((what, ok, detail));

        // One right-click with the cursor on `tile`, as a player clicks: whether the game's own check took it
        // (it notes the tile it acted on), whether the guide opened, and what's open after. (The cursor, not
        // the tile the farmer faces: standing square on a tile, the game's grab tile is the farmer's own.)
        (bool Took, bool Guide, string Menu) Press(Point stand, int facing, Vector2 tile)
        {
            Game1.activeClickableMenu = null;
            Game1.dialogueUp = false;
            who.Halt();
            who.CanMove = true;
            who.freezePause = 0;
            who.setTileLocation(stand.ToVector2());
            who.faceDirection(facing);
            who.lastGrabTile = new Vector2(-99, -99);
            float zoom = Game1.options.zoomLevel;
            Game1.oldMouseState = new MouseState((int)((tile.X * 64 + 32 - Game1.viewport.X) * zoom), (int)((tile.Y * 64 + 32 - Game1.viewport.Y) * zoom),
                0, ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released);
            Game1.wasMouseVisibleThisFrame = true;
            Game1.mouseCursorTransparency = 1f;
            Game1.pressActionButton(default, Game1.oldMouseState, default);
            Game1.wasMouseVisibleThisFrame = mouseBefore;
            Game1.oldMouseState = mouseStateBefore;
            Game1.mouseCursorTransparency = cursorBefore;
            var menu = Game1.activeClickableMenu;
            // (the game notes the cursor's spot in tiles, a fraction into the tile)
            bool took = (int)who.lastGrabTile.X == (int)tile.X && (int)who.lastGrabTile.Y == (int)tile.Y;
            return (took, menu is LetterViewerMenu, menu?.GetType().Name ?? "none");
        }

        try
        {
            who.mailReceived.Remove(PaintersGuide.GroveOpenFlag);
            int guides = Guides();
            bool holding = who.CurrentItem?.QualifiedItemId == guideId;

            var bare = Press(at, 2, front);
            Add("bare ground opens the guide", holding && bare.Guide && !bare.Took && Guides() == guides,
                $"at {at}, clicking {front}: holding {holding}, opened {bare.Guide}, menu {bare.Menu}, guides {guides} -> {Guides()}");

            // A person, with something to say.
            var npc = new NPC(new AnimatedSprite("Characters/Abigail", 0, 16, 32), front * 64f, 0, ModEntry.Id + "_ClickTester");
            npc.CurrentDialogue.Push(new Dialogue(npc, null, "Hello."));
            vale.characters.Add(npc);
            try
            {
                var r = Press(at, 2, front);
                bool talked = Game1.activeClickableMenu is DialogueBox { characterDialogue: { } d } && d.speaker == npc;
                Add("a person talks", r.Took && !r.Guide && talked, $"took {r.Took}, guide {r.Guide}, menu {r.Menu}, talked {talked}");
            }
            finally { vale.characters.Remove(npc); }

            // A pet.
            var pet = new StardewValley.Characters.Pet((int)front.X, (int)front.Y, "0", "Cat");
            vale.characters.Add(pet);
            try
            {
                var r = Press(at, 2, front);
                bool petted = pet.lastPetDay.TryGetValue(who.UniqueMultiplayerID, out int day) && day == Game1.Date.TotalDays;
                Add("a pet is petted", r.Took && !r.Guide && petted, $"took {r.Took}, guide {r.Guide}, menu {r.Menu}, petted {petted}");
            }
            finally { vale.characters.Remove(pet); }

            // A placed object: a keg, its wine ready.
            var keg = ItemRegistry.Create<SObject>("(BC)12");
            keg.heldObject.Value = ItemRegistry.Create<SObject>("(O)348");
            keg.readyForHarvest.Value = true;
            keg.MinutesUntilReady = 0;
            vale.Objects.Add(front, keg);
            try
            {
                var r = Press(at, 2, front);
                bool emptied = keg.heldObject.Value == null;
                Add("a placed object gives its wine", r.Took && !r.Guide && emptied, $"took {r.Took}, guide {r.Guide}, menu {r.Menu}, wine taken {emptied}");
            }
            finally { vale.Objects.Remove(front); }

            // A tree, to shake.
            var tree = new StardewValley.TerrainFeatures.Tree("1", 5);
            vale.terrainFeatures.Add(front, tree);
            try
            {
                var r = Press(at, 2, front);
                Add("a tree shakes", r.Took && !r.Guide && tree.maxShake > 0, $"took {r.Took}, guide {r.Guide}, menu {r.Menu}, shaking {tree.maxShake > 0}");
            }
            finally { vale.terrainFeatures.Remove(front); }

            // A bush, to shake.
            var bush = new StardewValley.TerrainFeatures.Bush(front, 0, vale);
            vale.largeTerrainFeatures.Add(bush);
            try
            {
                var r = Press(at, 2, front);
                float shake = _helper!.Reflection.GetField<float>(bush, "maxShake").GetValue();
                Add("a bush shakes", r.Took && !r.Guide && shake > 0, $"took {r.Took}, guide {r.Guide}, menu {r.Menu}, shaking {shake > 0}");
            }
            finally { vale.largeTerrainFeatures.Remove(bush); }

            // A crop ready to harvest (a parsnip).
            var dirt = new StardewValley.TerrainFeatures.HoeDirt(1, vale);
            vale.terrainFeatures.Add(front, dirt);
            try
            {
                dirt.crop = new Crop("472", (int)front.X, (int)front.Y, vale);
                dirt.crop.growCompletely();
                var r = Press(at, 2, front);
                bool harvested = dirt.crop == null;
                Add("a ready crop is harvested", r.Took && !r.Guide && harvested, $"took {r.Took}, guide {r.Guide}, menu {r.Menu}, harvested {harvested}");
            }
            finally { vale.terrainFeatures.Remove(front); }

            // A tile action: a grave's stone, faced from a walkable tile beside it (its urn stands in front,
            // so behind it or to one side).
            (Point Stone, Point Stand, int Facing)? grave = null;
            var layer = vale.Map.GetLayer("Buildings");
            var sides = new[] { (dx: 0, dy: -1, face: 2), (dx: -1, dy: 0, face: 1), (dx: 1, dy: 0, face: 3), (dx: 0, dy: 1, face: 0) };
            for (int y = 1; y < layer.LayerHeight - 1 && grave == null; y++)
                for (int x = 1; x < layer.LayerWidth - 1 && grave == null; x++)
                    if (vale.doesTileHaveProperty(x, y, "Action", "Buildings")?.StartsWith(ValeGraves.GraveAction + " ") == true)
                        foreach (var (dx, dy, face) in sides)
                        {
                            var stand = new Point(x + dx, y + dy);
                            if (grave == null && vale.isTilePassable(stand.ToVector2()) && !vale.isWaterTile(stand.X, stand.Y)
                                && vale.getObjectAtTile(stand.X, stand.Y) == null && vale.isCharacterAtTile(stand.ToVector2()) == null)
                                grave = (new Point(x, y), stand, face);
                        }
            if (grave is var (stone, standAt, facing))
            {
                var r = Press(standAt, facing, stone.ToVector2());
                bool read = Game1.activeClickableMenu is DialogueBox;
                Add("a tile action (a grave) is read", r.Took && !r.Guide && read, $"grave at {stone} from {standAt}: took {r.Took}, guide {r.Guide}, menu {r.Menu}");
            }
            else
                Add("a tile action (a grave) is read", false, "no grave with walkable ground beside it");

            Add("the guide is never used up", Guides() == guides, $"guides {guides} -> {Guides()}");
        }
        catch (Exception ex)
        {
            Add("no error", false, ex.ToString());
        }
        finally
        {
            Game1.wasMouseVisibleThisFrame = mouseBefore;
            Game1.oldMouseState = mouseStateBefore;
            Game1.mouseCursorTransparency = cursorBefore;
            Game1.activeClickableMenu = null;
            Game1.dialogueUp = false;
            who.Halt();
            who.CanMove = true;
            who.freezePause = 0;
            who.FarmerSprite.StopAnimation();
            if (hadGrove)
                who.mailReceived.Add(PaintersGuide.GroveOpenFlag);
            for (int i = 0; i < who.Items.Count; i++)
            {
                var (item, stack) = i < bag.Count ? bag[i] : (null, 0);
                if (item != null)
                    item.Stack = stack;
                if (who.Items[i] != item)
                    who.Items[i] = item;
            }
            for (int i = 0; i < xp.Length; i++)
                who.experiencePoints[i] = xp[i];
            while (vale.debris.Count > debrisBefore)
                vale.debris.RemoveAt(vale.debris.Count - 1);
            foreach (string flag in graveFlags)
                if (!readBefore.Contains(flag))
                    who.mailReceived.Remove(flag);
        }
        foreach (var (what, ok, detail) in results)
            Check(monitor, "guide: right-click, " + what, ok, detail);
    }

    // Holds one movement key down, as a player does, through the game's own input hook (Game1.inputSimulator,
    // read in UpdateControlInput with or without the window's focus). Setting the farmer moving directly
    // isn't enough: while the window has focus, the game's input pass sees no key held and halts the
    // farmer every tick, so "grove: arrive free" walked 0 px whenever the dev window was in front (a fresh
    // launch) and passed when it wasn't. dir is the game's facing (0 up, 1 right, 2 down, 3 left); null
    // lets go. (Escape also clears the hook, as the game does for any simulator.)
    private static void HoldKey(int? dir)
    {
        var field = typeof(Game1).GetField("inputSimulator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        if (dir is int d)
            field.SetValue(null, new HeldKey(d));
        else if (field.GetValue(null) is HeldKey)
            field.SetValue(null, null);
    }

    private sealed class HeldKey : StardewValley.Util.IInputSimulator
    {
        private readonly int dir;

        public HeldKey(int dir) => this.dir = dir;

        public void SimulateInput(ref bool actionButtonPressed, ref bool switchToolButtonPressed, ref bool useToolButtonPressed,
            ref bool useToolButtonReleased, ref bool addItemToInventoryButtonPressed, ref bool cancelButtonPressed,
            ref bool moveUpPressed, ref bool moveRightPressed, ref bool moveLeftPressed, ref bool moveDownPressed,
            ref bool moveUpReleased, ref bool moveRightReleased, ref bool moveLeftReleased, ref bool moveDownReleased,
            ref bool moveUpHeld, ref bool moveRightHeld, ref bool moveLeftHeld, ref bool moveDownHeld)
        {
            moveUpHeld |= dir == 0;
            moveRightHeld |= dir == 1;
            moveDownHeld |= dir == 2;
            moveLeftHeld |= dir == 3;
        }
    }

    // Each step waits (up to 15 seconds) until it's ready, then runs; a step that times out or throws
    // fails and ends the run. The cleanup always runs.
    private static void RunSteps(IMonitor monitor, List<Step> steps, Action cleanup)
    {
        int i = 0, waited = 0;
        void Done()
        {
            _helper!.Events.GameLoop.UpdateTicked -= Tick;
            cleanup();
        }
        void Tick(object? sender, UpdateTickedEventArgs e)
        {
            Step? current = i < steps.Count ? steps[i] : null;
            try
            {
                if (current == null)
                {
                    Done();
                    return;
                }
                if (!current.Ready())
                {
                    if (++waited > 900)
                    {
                        Check(monitor, current.What, false, "timed out");
                        Done();
                    }
                    return;
                }
                waited = 0;
                i++;
                current.Then();
            }
            catch (Exception ex)
            {
                Check(monitor, current?.What ?? "grove: warps", false, ex.ToString());
                Done();
            }
        }
        _helper!.Events.GameLoop.UpdateTicked += Tick;
    }
}
#endif
