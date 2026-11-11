#if DEBUG
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ArtStudio.Vale;

// What Lupini's heart events (tools/build_lupini_events.py) need from code.
//   His 2-heart event plays "on a fine day after a rainy one". The game doesn't keep yesterday's
//   weather in the save (Game1.wasRainingYesterday is gone once a save is reloaded), so each night
//   the valley's weather is written on the player, and the event's precondition asks for it:
//   G blinxxy.MuseArt_RAINED_YESTERDAY.
internal static class LupiniEvents
{
    public const string RainedYesterdayQuery = ModEntry.Id + "_RAINED_YESTERDAY";
    public const string RainedYesterdayKey = ModEntry.Id + "/RainedYesterday";

    public static void Register(IModHelper helper)
    {
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        GameStateQuery.Register(RainedYesterdayQuery, (_, context) => RainedYesterday(context.Player ?? Game1.player));
        DevTools.AddCommand(helper, "museart_lupinitest",
            "Lupini's heart events: 'hearts <n>' sets your hearts with him; 'rain T|F' sets yesterday's rain; 'sun' or 'rainy' sets today's "
            + "weather; 'check' says which of his events would play if you walked into the Vale (or the Forest) now, and why not.",
            (_, args) => Test(args));
    }

    private static void Test(string[] args)
    {
        if (!Context.IsWorldReady || args.Length == 0)
            return;
        var who = Game1.player;
        switch (args[0])
        {
            case "hearts":
                if (!who.friendshipData.ContainsKey("Lupini"))
                    who.friendshipData["Lupini"] = new Friendship();
                who.friendshipData["Lupini"].Points = int.Parse(args[1]) * 250;
                break;
            case "rain":
                who.modData[RainedYesterdayKey] = args.ElementAtOrDefault(1) == "T" ? "T" : "F";
                break;
            case "sun":
            case "rainy":
                var weather = Game1.netWorldState.Value.GetWeatherForLocation("Default");
                bool rain = args[0] == "rainy";
                weather.Weather = rain ? Game1.weather_rain : Game1.weather_sunny;
                weather.IsRaining = rain;
                Game1.isRaining = rain;
                break;
            case "check":
                foreach (string name in new[] { ModEntry.Id + "_GildedVale", "Forest" })
                {
                    var location = Game1.getLocationFromName(name);
                    var events = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/Events/" + name);
                    foreach (string key in events.Keys.Where(k => k.StartsWith(ModEntry.Id + "_Lupini") && k.Contains('/')))
                    {
                        string[] parts = Event.SplitPreconditions(key);
                        var failed = parts.Skip(1).Where(p => !Event.CheckPrecondition(location, parts[0], p)).ToList();
                        bool seen = who.eventsSeen.Contains(parts[0]);
                        Monitor?.Log($"{name} {parts[0]}: {(seen ? "seen" : failed.Count == 0 ? "WOULD PLAY" : "not now: " + string.Join("; ", failed))} [{key}]", LogLevel.Info);
                    }
                }
                Monitor?.Log($"Time {Game1.timeOfDay}, {Game1.currentSeason} {Game1.dayOfMonth} ({Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)}), "
                    + $"rained yesterday {RainedYesterday(who)}, hearts {who.getFriendshipHeartLevelForNPC("Lupini")}", LogLevel.Info);
                break;
        }
    }

    public static IMonitor? Monitor;

    public static bool RainedYesterday(Farmer who) =>
        who.modData.TryGetValue(RainedYesterdayKey, out string? value) && value == "T";

    // Before the save: today's rain (the valley's, wherever the player is standing) becomes
    // tomorrow's "yesterday".
    private static void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        bool raining = Game1.netWorldState.Value.GetWeatherForLocation("Default").IsRaining;
        Game1.player.modData[RainedYesterdayKey] = raining ? "T" : "F";
    }
}
#endif
