#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using xTile.Dimensions;

namespace ArtStudio.Vale;

// The Old Studios' sale (docs/design/vale_newcomers.md, section 4; Debug only like the dev
// pack). The notices, scaffolding and renovated fronts are the dev pack's conditional map
// edits (data/vale_studios.json, from tools/vale_studio_sale.py); this is Robin's side.
//
// On weekdays in the fortnight before each move-in her schedule takes her to the Vale.
// The rule (the user): her job there must never stop the player building on the farm.
//  - A farm build or house upgrade already wins, in the game itself: each morning
//    NPC.updateConstructionAnimation sends her to the site and sets ignoreScheduleToday,
//    so the Vale simply has no Robin that day (its scaffolding stays).
//  - Her counter: the game's only answers when she's standing behind it (or, on her island
//    days, opens the stock on trust). So while she's out on the Vale job, in shop hours, the
//    counter takes orders on trust: a note, then the whole carpenter menu, shop, upgrades
//    and building, exactly as she'd offer it.
internal static class ValeStudios
{
    private const string Vale = ModEntry.Id + "_GildedVale";
    private const string StringsAsset = "Mods/" + ModEntry.Id + "/ValeStrings";

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.carpenters)),
            prefix: new HarmonyMethod(typeof(ValeStudios), nameof(CarpentersPrefix)));

        DevTools.AddCommand(helper, "museart_studiodate", "Jumps the calendar for checking the Old Studios' sale: sets the date and updates the content packs. Usage: museart_studiodate <year> <season> <day>",
            (_, args) => SetDate(helper, monitor, args));
        DevTools.AddCommand(helper, "museart_studiotest", "Checks the Old Studios' sale for today's date: each studio's state on the map, the plaques' words, and Robin's day. "
            + "'counter': uses Robin's shop counter now. 'upgrade': a house upgrade this morning, which must take Robin off the Vale job.",
            (_, args) => Test(monitor, args.FirstOrDefault()));
    }

    /// <summary>Whether Robin's day is a Vale job day (her schedule today goes to the Vale) and a farm build hasn't taken it over.</summary>
    internal static bool OnValeJob(out NPC? robin)
    {
        robin = Game1.getCharacterFromName("Robin");
        // (a farm build sets ignoreScheduleToday only once her sprite next updates, so ask the farm too)
        if (robin == null || robin.ignoreScheduleToday || robin.ScheduleKey == null
            || Game1.player.daysUntilHouseUpgrade.Value > 0 || Game1.IsThereABuildingUnderConstruction())
            return false;
        var raw = robin.getMasterScheduleRawData();
        return raw != null && raw.TryGetValue(robin.ScheduleKey, out string? day) && day.Contains(Vale);
    }

    private static bool CarpentersPrefix(GameLocation __instance, Location tileLocation, ref bool __result)
    {
        if (!OnValeJob(out NPC? robin) || robin!.currentLocation == __instance
            || Game1.timeOfDay < 900 || Game1.timeOfDay >= 1700)
            return true;
        Game1.drawObjectDialogue(S("robin.away", "Robin's not here. A note on the counter: 'Out in the Gilded Vale on a job, back this evening. Orders in the box, I'll see to them. -R'"));
        Game1.afterDialogues = () => OrderBook(__instance);
        __result = true;
        return false;
    }

    /// <summary>The carpenter menu as GameLocation.carpenters builds it with Robin there, answered by the game's own handlers.</summary>
    private static void OrderBook(GameLocation location)
    {
        var who = Game1.player;
        if (who.daysUntilHouseUpgrade.Value >= 0 || Game1.IsThereABuildingUnderConstruction())
        {
            Utility.TryOpenShopMenu("Carpenter", null, playOpenSound: true);
            return;
        }
        string L(string key) => Game1.content.LoadString("Strings\\Locations:" + key);
        var list = new List<Response> { new("Shop", L("ScienceHouse_CarpenterMenu_Shop")) };
        if (Game1.IsMasterGame)
        {
            if (who.houseUpgradeLevel.Value < 3)
                list.Add(new Response("Upgrade", L("ScienceHouse_CarpenterMenu_UpgradeHouse")));
            else if ((Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.mailReceived.Contains("JojaMember") || Game1.MasterPlayer.hasCompletedCommunityCenter())
                     && Game1.RequireLocation<Town>("Town").daysUntilCommunityUpgrade.Value <= 0
                     && (!Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade") || !Game1.MasterPlayer.mailReceived.Contains("communityUpgradeShortcuts")))
                list.Add(new Response("CommunityUpgrade", L("ScienceHouse_CarpenterMenu_CommunityUpgrade")));
        }
        else if (who.houseUpgradeLevel.Value < 3)
            list.Add(new Response("Upgrade", L("ScienceHouse_CarpenterMenu_UpgradeCabin")));
        if (who.houseUpgradeLevel.Value >= 2)
            list.Add(new Response("Renovate", L(Game1.IsMasterGame ? "ScienceHouse_CarpenterMenu_RenovateHouse" : "ScienceHouse_CarpenterMenu_RenovateCabin")));
        list.Add(new Response("Construct", L("ScienceHouse_CarpenterMenu_Construct")));
        list.Add(new Response("Leave", L("ScienceHouse_CarpenterMenu_Leave")));
        location.createQuestionDialogue(S("robin.book", "Her order book lies open beside the box."), list.ToArray(), (_, answer) =>
        {
            if (answer == "Shop")
                Utility.TryOpenShopMenu("Carpenter", null, playOpenSound: true);     // (as on her island days: nobody behind the counter)
            else if (answer != "Leave")
                location.answerDialogueAction("carpenter_" + answer, Array.Empty<string>());
        });
    }

    private static string S(string key, string fallback)
    {
        try
        {
            var strings = Game1.content.Load<Dictionary<string, string>>(StringsAsset);
            return strings.TryGetValue(key, out string? text) && !string.IsNullOrWhiteSpace(text) ? text : fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    // ---- checking it in a dev game ----

    private static void SetDate(IModHelper helper, IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || args.Length < 3 || !int.TryParse(args[0], out int year) || !int.TryParse(args[2], out int day)
            || !Enum.TryParse(args[1], ignoreCase: true, out Season season))
        {
            monitor.Log("Usage: museart_studiodate <year> <season> <day>", LogLevel.Warn);
            return;
        }
        Game1.year = year;
        Game1.season = season;
        Game1.dayOfMonth = day;
        Game1.setGraphicsForSeason();
        RunConsoleCommand(monitor, "patch", "update");      // Content Patcher re-reads the date now, not tomorrow
        Game1.getLocationFromName(Vale)?.reloadMap();
        Game1.currentLocation?.updateSeasonalTileSheets();
        monitor.Log($"Now {season} {day}, year {year} (ValeDate {(year - 1) * 112 + (int)season * 28 + day}).", LogLevel.Info);
    }

    /// <summary>Runs another mod's console command (SMAPI has no public way to; dev builds only).</summary>
    private static void RunConsoleCommand(IMonitor monitor, string name, params string[] args)
    {
        try
        {
            var core = AccessTools.Property(AccessTools.TypeByName("StardewModdingAPI.Framework.SCore"), "Instance").GetValue(null);
            var manager = AccessTools.Field(core!.GetType(), "CommandManager").GetValue(core);
            var command = AccessTools.Method(manager!.GetType(), "Get").Invoke(manager, new object[] { name });
            var callback = (Action<string, string[]>)AccessTools.Property(command!.GetType(), "Callback").GetValue(command)!;
            callback(name, args);
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't run '{name} {string.Join(" ", args)}': {ex.Message}", LogLevel.Warn);
        }
    }

    private static readonly (string Name, int X, int Y, int WordsX)[] Plaques = { ("II", 86, 35, 85), ("III", 91, 35, 90) };

    private static void Test(IMonitor monitor, string? mode)
    {
        if (!Context.IsWorldReady) return;
        if (mode == "counter")
        {
            // the Carpenter tile action, as clicking Robin's counter runs it (her spot is 8 18)
            var shop = Game1.getLocationFromName("ScienceHouse");
            bool handled = shop.carpenters(new Location(8, 19));
            string Box() => Game1.activeClickableMenu is StardewValley.Menus.DialogueBox box
                ? $"dialogue '{box.getCurrentString()}'" + (box.responses?.Length > 0 ? $" answers [{string.Join(", ", box.responses.Select(r => r.responseKey))}]" : "")
                : Game1.activeClickableMenu?.GetType().Name ?? "nothing open";
            monitor.Log($"counter at {Game1.timeOfDay}, Robin on the Vale job {OnValeJob(out NPC? r)}, in {r?.currentLocation?.Name}: handled {handled}; {Box()}", LogLevel.Info);
            if (Game1.afterDialogues != null)
            {
                Game1.activeClickableMenu = null;
                var after = Game1.afterDialogues;
                Game1.afterDialogues = null;
                after();
                monitor.Log($"  then: {Box()}", LogLevel.Info);
            }
            return;
        }
        if (mode == "upgrade")
        {
            // as the morning goes: her schedule, then the game's construction check
            var builder = Game1.getCharacterFromName("Robin");
            int was = Game1.player.daysUntilHouseUpgrade.Value;
            Game1.player.daysUntilHouseUpgrade.Value = 2;
            Game1.warpCharacter(builder, builder.DefaultMap, builder.DefaultPosition / 64f);
            builder.TryLoadSchedule();
            AccessTools.Method(typeof(NPC), "updateConstructionAnimation").Invoke(builder, null);
            monitor.Log($"house upgrade under way: Robin's schedule '{builder.ScheduleKey}', ignoring it {builder.ignoreScheduleToday}, on the Vale job {OnValeJob(out _)}, in {builder.currentLocation?.Name} {builder.TilePoint}", LogLevel.Info);
            Game1.player.daysUntilHouseUpgrade.Value = was;
            return;
        }
        var vale = Game1.getLocationFromName(Vale);
        if (vale?.Map == null)
        {
            monitor.Log("no Vale", LogLevel.Warn);
            return;
        }
        monitor.Log($"{Game1.season} {Game1.dayOfMonth}, year {Game1.year}: ValeDate {(Game1.year - 1) * 112 + (int)Game1.season * 28 + Game1.dayOfMonth}", LogLevel.Info);
        foreach (var (name, x, y, wx) in Plaques)
        {
            var plaque = vale.Map.GetLayer("AlwaysFront")?.Tiles[x, y];
            var words = vale.Map.GetLayer("Buildings")?.Tiles[wx, 38];
            string message = words?.Properties.TryGetValue("Action", out var a) == true ? a.ToString() : "none";
            string key = message.StartsWith("Message ") ? message["Message ".Length..] : "";
            string text = key == "" ? "" : Game1.content.LoadString("Strings\\StringsFromMaps:" + key);
            monitor.Log($"Studio {name}: plaque tile {plaque?.TileSheet?.Id}:{plaque?.TileIndex}; words: {message} = \"{text}\"", LogLevel.Info);
        }
        var mail = DataLoader.Mail(Game1.content);
        foreach (var trigger in DataLoader.TriggerActions(Game1.content).Where(t => t.Id.StartsWith(ModEntry.Id + "_RobinValeJob")))
            monitor.Log($"{trigger.Id}: letter {(mail.ContainsKey(trigger.Id) ? "written" : "MISSING")}, comes this morning {GameStateQuery.CheckConditions(trigger.Condition)}", LogLevel.Info);
        foreach (var (x, y) in new[] { (82, 44), (83, 44), (94, 38) })
        {
            var t = vale.Map.GetLayer("Buildings")?.Tiles[x, y];
            monitor.Log($"timber at {x},{y}: {(t == null ? "none" : t.TileSheet.Id + ":" + t.TileIndex)}; passable {vale.isTilePassable(new Location(x, y), Game1.viewport)}", LogLevel.Info);
        }
        var robin = Game1.getCharacterFromName("Robin");
        string[]? route = WarpPathfindingCache.GetLocationRoute("ScienceHouse", Vale, Gender.Female);
        monitor.Log($"Robin: schedule '{robin?.ScheduleKey}', ignoring it today {robin?.ignoreScheduleToday}, on the Vale job {OnValeJob(out _)}, "
            + $"in {robin?.currentLocation?.Name} {robin?.TilePoint}; route home > Vale: {(route == null ? "NONE" : string.Join(">", route))}; "
            + $"building on the farm {Game1.IsThereABuildingUnderConstruction()}, house upgrade in {Game1.player.daysUntilHouseUpgrade.Value} days", LogLevel.Info);
        if (robin?.Schedule != null)
            foreach (var (time, leg) in robin.Schedule)
                monitor.Log($"  {time}: {leg.targetLocationName} {leg.targetTile} ({leg.route?.Count ?? 0} steps from the leg before)", LogLevel.Info);
    }
}
#endif
