#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Menus;
using StardewValley.Pathfinding;

namespace ArtStudio.Vale;

// museart_valetest: checks the Vale residents in a running game (1.4, Debug only), the
// way museart_npctest checks Sable. Each line is [PASS], [FAIL], or [INFO] for something
// that depends on a switch or the save (a resident without art, the route's letter not
// yet come). Run it in a dev save, ideally one with blinxxy.MuseArt_ValeOpen
// (museart_flag blinxxy.MuseArt_ValeOpen) and the pack's ValeResidents switch on.
// Nothing it changes is kept: money, the room booking and open menus are put back.
internal static class ValeTests
{
    private sealed record Resident(string Name, string Slug, string Home, Point HomeTile, string Love, string Hate);

    private static readonly Resident[] Residents =
    {
        new("BeaThistle", "bea", ValeResidents.ValeHouse, new Point(26, 6), "(O)614", "(O)766"),
        new("TobinClay", "tobin", ModEntry.Id + "_ValePottery", new Point(22, 6), "(O)330", "(O)168"),
        new("Lupini", "lupini", ModEntry.Id + "_ValeAtelier", new Point(18, 6), "(O)348", "(O)766"),
        new("FernAster", "fern", ModEntry.Id + "_ValeFern", new Point(6, 6), "(O)347", "(O)769"),
        new("EmmettWren", "emmett", ModEntry.Id + "_ValeEmmett", new Point(6, 6), "(O)281", "(O)766"),
        // the Corrie twins, from Spring 1, Year 3 (Studio II, their beds at either end)
        new("OpalCorrie", "opal", ModEntry.Id + "_ValeStudio2", new Point(4, 6), "(O)595", "(O)168"),
        new("JetCorrie", "jet", ModEntry.Id + "_ValeStudio2", new Point(17, 6), "(O)70", "(O)168"),
    };

    private static readonly string[] ArtLovers = { "Lupini", "TobinClay", "FernAster", "EmmettWren", "OpalCorrie", "JetCorrie" };

    // The counters and the tile actions the dev pack puts on them (data/vale_shops.json).
    private static readonly (string Location, Point Tile, string ActionStart, string Shop)[] Counters =
    {
        (ValeResidents.ValeHouse, new Point(20, 22), ModEntry.Id + "_ValeTeaCounter", ValeResidents.TeaRoomShop),
        (ModEntry.Id + "_ValeFern", new Point(23, 18), ModEntry.Id + "_ValeShop " + ModEntry.Id + "_ValeSeeds FernAster", ModEntry.Id + "_ValeSeeds"),
        (ModEntry.Id + "_ValePottery", new Point(4, 18), ModEntry.Id + "_ValeShop " + ModEntry.Id + "_ValePottery TobinClay", ModEntry.Id + "_ValePottery"),
    };

    // The bedroom doors (data/vale_doors.json), locked to each resident once they're on.
    private static readonly (string Name, string Location, Point Tile)[] Doors =
    {
        ("BeaThistle", ValeResidents.ValeHouse, new Point(25, 11)),
        ("TobinClay", ModEntry.Id + "_ValePottery", new Point(20, 13)),
        ("Lupini", ModEntry.Id + "_ValeAtelier", new Point(11, 15)),
        ("FernAster", ModEntry.Id + "_ValeFern", new Point(8, 12)),
        ("EmmettWren", ModEntry.Id + "_ValeEmmett", new Point(5, 11)),
    };

    private const string Vale = ModEntry.Id + "_GildedVale";
    private const string OpenMail = ModEntry.Id + "_ValeOpen";

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_valetest",
            "Checks the Gilded Vale's residents: their data and art, schedules (every leg walkable), shops, Bea's room, the bedroom doors, Lupini's letter, the bus route for their town days, and the town's talk.",
            (_, _) => Run(monitor));
        // museart_valepassout [booked]: a throwaway dev game only (it ends the day). Stands you on
        // the Vale House floor at 1:50am; at 2am you pass out, and the morning after checks Bea
        // put you to bed: no clinic or Joja letter, woke in the guest bed, 200g or nothing taken.
        DevTools.AddCommand(helper, "museart_valepassout",
            "Dev game only: passes out on the Vale House floor at 2am, then checks the morning (no clinic letter, woke in the guest bed). 'booked' books the room first.",
            (_, args) => StartPassOut(monitor, args.Length > 0 && args[0] == "booked"));
        helper.Events.GameLoop.DayStarted += (_, _) => CheckPassOut(monitor);
    }

    private static (int Money, bool Booked)? _passOut;

    private static void StartPassOut(IMonitor monitor, bool booked)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        Game1.saveOnNewDay = false;
        if (booked)
            who.modData[ValeResidents.RoomNightKey] = Game1.Date.TotalDays.ToString();
        else
            who.modData.Remove(ValeResidents.RoomNightKey);
        who.mailReceived.Remove(ValeResidents.ArtistsRoomMail);
        who.mailForTomorrow.Clear();
        _passOut = (who.Money, booked);
        Game1.warpFarmer(ValeResidents.ValeHouse, 12, 8, 2);
        Game1.timeOfDay = 2550;
        Info(monitor, $"on the Vale House floor at 1:50am ({(booked ? "booked" : "not booked")}, {who.Money}g); passing out at 2am");
    }

    private static void CheckPassOut(IMonitor monitor)
    {
        if (_passOut is not var (money, booked))
            return;
        _passOut = null;
        var who = Game1.player;
        var letters = who.mailbox.Concat(who.mailForTomorrow).Where(m => m.StartsWith("passedOut")).ToList();
        Check(monitor, "passing out in the Vale House: no clinic or Joja letter", letters.Count == 0, string.Join(", ", letters));
        int expected = booked ? 0 : ValeResidents.RoomPrice;
        Check(monitor, $"passing out in the Vale House: {expected}g taken", money - who.Money == expected, $"{money}g -> {who.Money}g");
        Check(monitor, "passing out in the Vale House: you wake there", who.currentLocation?.Name == ValeResidents.ValeHouse, who.currentLocation?.Name ?? "nowhere");
        Check(monitor, "passing out in the Vale House: in the guest bed", who.TilePoint == new Point(8, 6), who.TilePoint.ToString());
        Check(monitor, "passing out in the Vale House: Bea's morning note", Game1.hudMessages.Any(h => h.message.Contains("Bea")), string.Join(" | ", Game1.hudMessages.Select(h => h.message)));
    }

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static void Info(IMonitor monitor, string what) => monitor.Log($"[INFO] {what}", LogLevel.Info);

    private static void Run(IMonitor monitor)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        Info(monitor, who.mailReceived.Contains(OpenMail)
            ? "the Vale is open in this save (blinxxy.MuseArt_ValeOpen)"
            : "the Vale isn't open in this save yet, so no resident is in the world: museart_flag blinxxy.MuseArt_ValeOpen");

        foreach (var r in Residents)
            CheckResident(monitor, r);
        CheckShops(monitor);
        CheckRoom(monitor, who);
        CheckDoors(monitor);
        CheckLetters(monitor, who);
        CheckRoute(monitor);
        CheckTalk(monitor);
    }

    // ---- Each resident: switched on, in the world, art, gifts, dialogue, schedule ----

    private static void CheckResident(IMonitor monitor, Resident r)
    {
        if (!Game1.characterData.TryGetValue(r.Name, out CharacterData? data))
        {
            Info(monitor, $"{r.Name} is switched off (needs ValeResidents: true and assets/{r.Slug}/sprites.png and portraits.png)");
            return;
        }
        Check(monitor, $"{r.Name}: home is {r.Home} {r.HomeTile}",
            data.Home?.FirstOrDefault() is { } home && home.Location == r.Home && home.Tile == r.HomeTile,
            data.Home?.FirstOrDefault() is { } h ? $"{h.Location} {h.Tile}" : "no home");
        Check(monitor, $"{r.Name}: the art lovers' list has them only if they should be", ArtLovers.Contains(r.Name) == Game1.content.Load<Dictionary<string, string>>(Painting.ArtItems.ArtLoversAsset).ContainsKey(r.Name));

        var npc = Game1.getCharacterFromName(r.Name);
        if (npc == null)
        {
            Info(monitor, $"{r.Name} is switched on but not in the world (the Vale's letter not yet come: UnlockConditions)");
            return;
        }
        Texture2D? sprite = npc.Sprite?.Texture, portrait = npc.Portrait;
        Check(monitor, $"{r.Name}: sprite sheet 64 wide, at least the four walking rows", sprite != null && sprite.Width == 64 && sprite.Height >= 128, sprite == null ? "none" : $"{sprite.Width}x{sprite.Height}");
        Check(monitor, $"{r.Name}: portraits 128 wide, at least six", portrait != null && portrait.Width == 128 && portrait.Height >= 192, portrait == null ? "none" : $"{portrait.Width}x{portrait.Height}");
        int Taste(string id) => npc.getGiftTasteForThisItem(ItemRegistry.Create(id));
        Check(monitor, $"{r.Name}: loves {r.Love}, hates {r.Hate}", Taste(r.Love) == NPC.gift_taste_love && Taste(r.Hate) == NPC.gift_taste_hate);
        var dialogue = Game1.content.Load<Dictionary<string, string>>("Characters/Dialogue/" + r.Name);
        Check(monitor, $"{r.Name}: has an introduction and a line for every day", dialogue.ContainsKey("Introduction") && new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }.All(dialogue.ContainsKey), $"{dialogue.Count} lines");
        CheckSchedule(monitor, npc);
    }

    // Every schedule entry, leg by leg: each stop reachable from the one before it, across
    // locations through their warps and doors (as museart_npctest does for Sable). Legs into
    // town also report how long the walk is, for setting their times.
    private static void CheckSchedule(IMonitor monitor, NPC npc)
    {
        if (npc.getMasterScheduleRawData() is not { } schedules)
        {
            Info(monitor, $"{npc.Name} has no schedule yet: stays home");
            return;
        }
        foreach (var (key, script) in schedules)
        {
            if (key.StartsWith("marriage"))
                continue;
            var problems = new List<string>();
            var notes = new List<string>();
            Dictionary<int, SchedulePathDescription> parsed;
            try
            {
                parsed = npc.parseMasterSchedule(key, script);
            }
            catch (Exception ex)
            {
                Check(monitor, $"{npc.Name}: schedule '{key}' parses", false, ex.Message);
                continue;
            }
            string from = npc.DefaultMap;
            Point at = (npc.DefaultPosition / 64).ToPoint();
            foreach (var stop in parsed.OrderBy(p => p.Key).Select(p => p.Value))
            {
                int tiles = 0;
                string[]? route = from == stop.targetLocationName ? new[] { from } : WarpPathfindingCache.GetLocationRoute(from, stop.targetLocationName, npc.Gender);
                if (route == null)
                {
                    problems.Add($"{stop.time}: no way from {from} to {stop.targetLocationName}");
                }
                else
                {
                    Point start = at;
                    for (int i = 0; i < route.Length; i++)
                    {
                        var location = Game1.RequireLocation(route[i]);
                        Point end = i < route.Length - 1 ? location.getWarpPointTo(route[i + 1]) : stop.targetTile;
                        var path = PathFindController.findPathForNPCSchedules(start, end, location, 30000, npc);
                        if (path == null)
                            problems.Add($"{stop.time}: no path in {location.Name} from {start} to {end}");
                        else
                            tiles += path.Count;
                        if (i < route.Length - 1)
                            start = location.getWarpPointTarget(end, npc);
                    }
                    if (route.Length > 2 || (route.Contains(Vale) && route.Any(l => l != Vale && !l.StartsWith(ModEntry.Id))))
                        notes.Add($"{stop.time} to {stop.targetLocationName}: {tiles} tiles, there by {Utility.ModifyTime(stop.time, WalkMinutes(tiles))}");
                }
                from = stop.targetLocationName;
                at = stop.targetTile;
            }
            Check(monitor, $"{npc.Name}: schedule '{key}' is walkable", problems.Count == 0,
                problems.Count == 0 ? $"{parsed.Count} stops" : string.Join("; ", problems));
            foreach (string note in notes)
                Info(monitor, $"{npc.Name} '{key}': {note}");
        }
    }

    private static int WalkMinutes(int tiles) => (int)Math.Ceiling(tiles * 32 / 60.0 / (Game1.realMilliSecondsPerGameTenMinutes / 10000.0));

    // ---- The shops: their stock exists, each opens, each counter carries its action ----

    private static void CheckShops(IMonitor monitor)
    {
        var shops = DataLoader.Shops(Game1.content);
        foreach (var (locationName, tile, actionStart, shopId) in Counters)
        {
            if (!shops.TryGetValue(shopId, out var shop))
            {
                Check(monitor, $"shop {shopId} exists", false);
                continue;
            }
            var missing = shop.Items.Where(i => !ItemRegistry.Exists(i.ItemId)).Select(i => i.ItemId).ToList();
            Check(monitor, $"shop {shopId}: every item exists", missing.Count == 0, missing.Count == 0 ? $"{shop.Items.Count} items" : string.Join(", ", missing));
            var location = Game1.getLocationFromName(locationName);
            string? action = location?.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings");
            Check(monitor, $"shop {shopId}: its counter ({locationName} {tile}) opens it", action != null && action.StartsWith(actionStart), action ?? "no action");
            var before = Game1.activeClickableMenu;
            bool opened = Utility.TryOpenShopMenu(shopId, "None", playOpenSound: false) && Game1.activeClickableMenu is ShopMenu;
            Check(monitor, $"shop {shopId} opens", opened);
            if (opened)
                Game1.activeClickableMenu = before;
        }
    }

    // ---- Bea's room for the night: 200g at her counter, and the bed refuses until then ----

    private static void CheckRoom(IMonitor monitor, Farmer who)
    {
        var house = Game1.getLocationFromName(ValeResidents.ValeHouse);
        if (house == null)
        {
            Check(monitor, "the Vale House exists", false);
            return;
        }
        int money = who.Money;
        // the bed's refusal and Bea's answer each open a dialogue box, which stops you moving
        // until it's closed; taking the box away doesn't give that back, so it's put back here
        // (the user, 2026-11-08: "i typed the valetest command and cannot move anymore")
        bool canMove = who.CanMove;
        bool hadNight = who.modData.TryGetValue(ValeResidents.RoomNightKey, out string night);
        bool artists = who.mailReceived.Contains(ValeResidents.ArtistsRoomMail);
        try
        {
            who.modData.Remove(ValeResidents.RoomNightKey);
            who.mailReceived.Remove(ValeResidents.ArtistsRoomMail);
            Check(monitor, "Bea's room: the guest bed refuses you before you've booked", !ValeResidents.TouchActionPrefix(house, new[] { "Sleep" }));
            who.Money = ValeResidents.RoomPrice - 1;
            ValeResidents.BookRoom(who, null);
            Check(monitor, "Bea's room: you can't book it without the gold", !ValeResidents.BookedTonight(who) && who.Money == ValeResidents.RoomPrice - 1);
            who.Money = ValeResidents.RoomPrice + 50;
            ValeResidents.BookRoom(who, null);
            Check(monitor, $"Bea's room: booking takes {ValeResidents.RoomPrice}g", ValeResidents.BookedTonight(who) && who.Money == 50, $"{who.Money}g left");
            Check(monitor, "Bea's room: the guest bed lets you sleep once you've booked", ValeResidents.TouchActionPrefix(house, new[] { "Sleep" }));
            Check(monitor, "Bea's room: other touch actions there are left alone", ValeResidents.TouchActionPrefix(house, new[] { "Door" }));
        }
        finally
        {
            who.Money = money;
            if (hadNight) who.modData[ValeResidents.RoomNightKey] = night; else who.modData.Remove(ValeResidents.RoomNightKey);
            if (artists) who.mailReceived.Add(ValeResidents.ArtistsRoomMail);
            Game1.activeClickableMenu = null;
            Game1.dialogueUp = false;
            who.CanMove = canMove;
        }
    }

    // ---- The bedroom doors: Door <name> once that resident is on, plain Door before ----

    private static void CheckDoors(IMonitor monitor)
    {
        foreach (var (name, locationName, tile) in Doors)
        {
            string? action = Game1.getLocationFromName(locationName)?.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings");
            string expected = Game1.characterData.ContainsKey(name) ? "Door " + name : "Door";
            Check(monitor, $"{name}'s bedroom door ({locationName} {tile}) is '{expected}'", action == expected, action ?? "no action");
        }

        // the artists' room: locked to everyone, even at 10 hearts with Bea, until her key
        GameLocation? house = Game1.getLocationFromName(ValeResidents.ValeHouse);
        if (house == null)
            return;
        Farmer who = Game1.player;
        bool keyed = who.mailReceived.Contains(ValeResidents.ArtistsRoomMail);
        string artists = house.doesTileHaveProperty(15, 11, "Action", "Buildings");
        string want = Game1.characterData.ContainsKey(ValeResidents.Bea) && !keyed ? "Door " + ValeResidents.ArtistsRoomDoor : "Door";
        Check(monitor, $"the artists' room door (15 11) is '{want}'", artists == want, artists ?? "no action");
        if (artists != "Door " + ValeResidents.ArtistsRoomDoor)
            return;
        bool hadBea = who.friendshipData.TryGetValue(ValeResidents.Bea, out Friendship? old);
        int points = old?.Points ?? 0;
        if (!hadBea)
            who.friendshipData[ValeResidents.Bea] = new Friendship();
        who.friendshipData[ValeResidents.Bea].Points = 2500;
        house.interiorDoors.TryGetValue(new Point(15, 11), out bool wasOpen);
        var menu = Game1.activeClickableMenu;
        house.performAction(artists.Split(' '), who, new xTile.Dimensions.Location(15, 11));
        house.interiorDoors.TryGetValue(new Point(15, 11), out bool open);
        string? said = (Game1.activeClickableMenu as StardewValley.Menus.DialogueBox)?.getCurrentString();
        Check(monitor, "at 10 hearts with Bea the artists' room stays shut", !open || wasOpen, open ? "it opened" : "");
        Check(monitor, "and its card says it's not let out", said?.Contains("Not let out") == true, said ?? "no dialogue");
        Game1.activeClickableMenu = menu;
        if (hadBea)
            who.friendshipData[ValeResidents.Bea].Points = points;
        else
            who.friendshipData.Remove(ValeResidents.Bea);
    }

    // ---- Lupini's letter, the residents' letters, and the Vale's letter paper ----

    private static void CheckLetters(IMonitor monitor, Farmer who)
    {
        var mail = DataLoader.Mail(Game1.content);
        foreach (string id in new[] { OpenMail, ModEntry.Id + "_BeaLetter", ModEntry.Id + "_TobinLetter", ModEntry.Id + "_FernLetter", ModEntry.Id + "_EmmettLetter", ModEntry.Id + "_LupiniPostcard" })
            Check(monitor, $"letter {id} exists, on the Vale's paper", mail.TryGetValue(id, out string? text) && text.Contains("[letterbg Mods/" + ModEntry.Id + "/ValeLetter 0]"));
        Texture2D? paper = null;
        try { paper = Game1.content.Load<Texture2D>("Mods/" + ModEntry.Id + "/ValeLetter"); } catch (Exception) { }
        Check(monitor, "the Vale's letter paper is 320x180", paper is { Width: 320, Height: 180 }, paper == null ? "missing" : $"{paper.Width}x{paper.Height}");

        var trigger = DataLoader.TriggerActions(Game1.content).FirstOrDefault(t => t.Id == OpenMail);
        Check(monitor, "Lupini's letter has its trigger", trigger != null);
        if (trigger != null && !who.mailReceived.Contains(OpenMail))
        {
            bool afterMarket = Game1.year > 1 || (Game1.season == Season.Winter && Game1.dayOfMonth >= 18);
            Info(monitor, $"Lupini's letter would come tomorrow morning: {GameStateQuery.CheckConditions(trigger.Condition)} "
                + $"(after the first Night Market: {afterMarket}; bus mended: {Game1.MasterPlayer.mailReceived.Contains("ccVault")}; "
                + $"Painting 3: {GameStateQuery.CheckConditions(ModEntry.Id + "_PAINTING_LEVEL 3")}; a Lupini painting bought: {who.mailReceived.Contains(ValeResidents.BoughtLupiniMail)})");
        }
    }

    // ---- The NPC route between the Vale's bus and the Bus Stop, for their town days ----

    private static void CheckRoute(IMonitor monitor)
    {
        static string? NpcWarps(string name) =>
            Game1.getLocationFromName(name)?.Map?.Properties.TryGetValue("NPCWarp", out var value) == true ? value.ToString() : null;
        string? valeSide = NpcWarps(Vale), busSide = NpcWarps("BusStop");
        bool on = valeSide != null && valeSide.Contains("BusStop");
        if (!on)
        {
            Info(monitor, "the residents' town days are switched off (ValeResidentsTownTrips), so there's no NPC route out of the Vale");
            return;
        }
        Check(monitor, "the Bus Stop's NPC warps include the Vale's, beside the Desert's", busSide != null && busSide.Contains("23 9 " + Vale), busSide ?? "none");
        string[]? there = WarpPathfindingCache.GetLocationRoute(Vale, "SeedShop", Gender.Female);
        string[]? back = WarpPathfindingCache.GetLocationRoute("Saloon", Vale, Gender.Male);
        Check(monitor, "an NPC can get from the Vale to Pierre's", there != null, there == null ? "no route" : string.Join(" > ", there));
        Check(monitor, "an NPC can get from the Saloon back to the Vale", back != null, back == null ? "no route" : string.Join(" > ", back));
    }

    // ---- The town's talk about the Vale (conversation topics) ----

    private static void CheckTalk(IMonitor monitor)
    {
        string topic = ModEntry.Id + "_ValeOpened";
        var missing = new[] { "Pam", "Lewis", "Gus", "Sable", "Elliott", "Leah", "Evelyn", "Caroline", "Clint", "Robin", "Sam", "Haley", "JulianVane", "MargotPell" }
            .Where(name =>
            {
                try { return !Game1.content.Load<Dictionary<string, string>>("Characters/Dialogue/" + name).ContainsKey(topic); }
                catch (Exception) { return true; }
            }).ToList();
        Check(monitor, "the valley has a line each about the Vale opening", missing.Count == 0, missing.Count == 0 ? "14 villagers" : "missing: " + string.Join(", ", missing));
        Info(monitor, Game1.player.activeDialogueEvents.ContainsKey(topic)
            ? $"the Vale's opening is being talked about now ({Game1.player.activeDialogueEvents[topic]} days left)"
            : "nobody's talking about the Vale yet (that starts the morning after Lupini's letter)");
    }
}
#endif
