using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;

namespace ArtStudio.Social;

// Julian Vane (1.2), the critic who visits from Zuzu City the 22nd to the
// 24th of every season (data/julian.json). On those days he starts by the
// bus, out of sight, appears at 10 as if off the morning bus, and goes out
// of sight again once he's back at the bus stop for the evening one.
//
// He's only in the valley twelve days a year, so the friendship that drains
// on days you don't talk to someone would undo every visit: points lost
// overnight after a day he wasn't here are put back. (Not talking to him
// while he's in town still costs, as with anyone.)
internal static class Julian
{
    public const string Name = "JulianVane";
    public const int FirstVisitDay = 22, LastVisitDay = 24;
    public static readonly Point BusTile = new(22, 11);     // by the bus door
    public const int Arrives = 1000, LeavesBy = 2000;
    private const string PointsKey = ModEntry.Id + "/JulianPoints";

    public static bool IsVisitDay(int dayOfMonth) => dayOfMonth >= FirstVisitDay && dayOfMonth <= LastVisitDay;

    public static NPC? Npc => Game1.getCharacterFromName(Name);

    public static void Register(IModHelper helper)
    {
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.TimeChanged += (_, e) => UpdateVisibility(e.NewTime);
        helper.Events.GameLoop.DayEnding += (_, _) => RememberFriendship(Game1.player);
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        KeepFriendship(Game1.player, Game1.dayOfMonth);
        UpdateVisibility(Game1.timeOfDay);
    }

    // Puts back friendship lost overnight after a day he wasn't in the valley.
    internal static void KeepFriendship(Farmer who, int today)
    {
        // Yesterday: the day before today, across the end of a season (28 days).
        int yesterday = today == 1 ? 28 : today - 1;
        if (!IsVisitDay(yesterday)
            && who.friendshipData.TryGetValue(Name, out var friendship)
            && who.modData.TryGetValue(PointsKey, out string raw) && int.TryParse(raw, out int before)
            && friendship.Points < before)
            friendship.Points = before;
    }

    internal static void RememberFriendship(Farmer who)
    {
        if (who.friendshipData.TryGetValue(Name, out var friendship))
            who.modData[PointsKey] = friendship.Points.ToString();
    }

    // Out of sight before the morning bus and after the evening one.
    public static void UpdateVisibility(int time)
    {
        if (!Context.IsMainPlayer || Npc is not { } julian) return;
        bool visiting = IsVisitDay(Game1.dayOfMonth);
        bool gone = julian.currentLocation?.Name == "BusStop" && julian.TilePoint == BusTile && time > Arrives + 100;
        julian.IsInvisible = !visiting || time < Arrives || time >= LeavesBy || gone;
    }
}
