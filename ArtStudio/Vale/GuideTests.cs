#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
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
            "Checks the painters' guide's rules (no guide no find, a first find, a repeat, the eighth completes, an unknown id is refused), then the Gilded Grove: its season, the warp in and the frame home.",
            (_, _) => Run(monitor));
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

            // The right-click hook leaves the game's own clicks alone: people, objects, furniture, tile actions.
            Check(monitor, "guide: right-click leaves NPCs, objects, furniture and tile actions alone", HookLeavesClicks(vale, out string hook), hook);

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

    // The game's own click, for each thing the hook must leave alone; true only if it leaves them all
    // and still opens the guide on bare ground.
    private static bool HookLeavesClicks(GameLocation vale, out string detail)
    {
        var tile = new Vector2(GildedGrove.ReturnTile.X, GildedGrove.ReturnTile.Y);
        var results = new List<string>();
        bool bare = PaintersGuide.ClickOpensGuide(vale, tile);
        results.Add($"bare {bare}");

        bool obj = false, furn = false;
        if (!vale.Objects.ContainsKey(tile))
        {
            vale.Objects.Add(tile, ItemRegistry.Create<SObject>("(O)390"));
            try { obj = !PaintersGuide.ClickOpensGuide(vale, tile); }
            finally { vale.Objects.Remove(tile); }
        }
        results.Add($"object {obj}");
        var chair = Furniture.GetFurnitureInstance("0", tile);
        vale.furniture.Add(chair);
        try { furn = !PaintersGuide.ClickOpensGuide(vale, tile); }
        finally { vale.furniture.Remove(chair); }
        results.Add($"furniture {furn}");

        // Someone already in the world, wherever they are: their feet and their head.
        var npc = Utility.getAllCharacters().FirstOrDefault(c => c.currentLocation != null && c.IsVillager);
        bool feet = npc != null && !PaintersGuide.ClickOpensGuide(npc.currentLocation, npc.Tile);
        bool head = npc != null && !PaintersGuide.ClickOpensGuide(npc.currentLocation, npc.Tile - new Vector2(0, 1));
        results.Add($"{npc?.Name ?? "no NPC"} feet {feet} head {head}");

        Vector2? action = null;
        var layer = vale.Map.GetLayer("Buildings");
        for (int y = 0; y < layer.LayerHeight && action == null; y++)
            for (int x = 0; x < layer.LayerWidth && action == null; x++)
                if (vale.doesTileHaveProperty(x, y, "Action", "Buildings") != null)
                    action = new Vector2(x, y);
        bool act = action != null && !PaintersGuide.ClickOpensGuide(vale, action.Value);
        results.Add($"tile action {act}");
        detail = string.Join(", ", results);
        return bare && obj && furn && feet && head && act;
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
        var gate = new Vector2(GildedGrove.ReturnTile.X, GildedGrove.ReturnTile.Y);
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

        who.mailReceived.Add(PaintersGuide.GroveOpenFlag);
        var steps = new List<Step>
        {
            new("grove: warp in", () => true, () => Game1.warpFarmer(ValeBus.Vale, 20, 7, 2)),
            new("grove: warp in", () => In(ValeBus.Vale), () =>
            {
                PaintersGuide.UseGuide(who);
                SayYes(vale);
            }),
            new("grove: warp in", () => In(GildedGrove.Location), () =>
            {
                Check(monitor, "grove: warp in", who.TilePoint == GildedGrove.ArrivalTile, $"at {who.TilePoint}, arrival {GildedGrove.ArrivalTile}");
                Check(monitor, "grove: paint motes in the Grove", GildedGrove.MoteTotal > 0, $"{GildedGrove.MoteTotal} motes");
                dayLight = Light();
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
                Check(monitor, "grove: the frame home", who.TilePoint == GildedGrove.ReturnTile, $"at {who.TilePoint}, gate {GildedGrove.ReturnTile}");
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
                Point at = who.TilePoint, gateTile = GildedGrove.ReturnTile;
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
                Point at = who.TilePoint, gateTile = GildedGrove.ReturnTile;
                int far = Math.Max(Math.Abs(at.X - gateTile.X), Math.Abs(at.Y - gateTile.Y));
                Check(monitor, "grove: the frame home, someone on the gate", stander != null && at != gateTile && far == 1 && GildedGrove.Landable(vale, at),
                    $"at {at}, gate {gateTile}, {far} away, {stander?.Name} at {stander?.TilePoint}");
            }),
        };
        RunSteps(monitor, steps, () =>
        {
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
