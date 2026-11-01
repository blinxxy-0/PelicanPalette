using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using StardewValley;

namespace ArtStudio.Social;

// What Julian's heart events need from the mod (scripts in the content pack,
// tools/build_julian_events.py; the approved draft is docs/julian_events.md).
internal static class JulianEvents
{
    public static void Register()
    {
        // "blinxxy.MuseArt_PORTFOLIO_KEPT <count>": the farm's portfolio book
        // holds at least that many paintings (his 2-heart event needs three).
        GameStateQuery.Register(ModEntry.Id + "_PORTFOLIO_KEPT", (query, _) =>
            int.TryParse(query.ElementAtOrDefault(1), out int count) && Portfolio.Items.Count(item => item != null) >= count);

        // His review of your best kept painting, at the museum (4 hearts):
        // named, and honest about it in a way that depends on how good it is.
        Event.RegisterCommand(ModEntry.Id + "_JulianReview", (ev, _, _) =>
            ev.ReplaceCurrentCommand($"speak {Julian.Name} \"{ReviewLine()}\""));

        // Sable's last word at his 10 hearts, only if you're married to them.
        Event.RegisterCommand(ModEntry.Id + "_SableDinner", (ev, _, _) =>
        {
            if (Game1.player.friendshipData.TryGetValue("Sable", out var friendship) && friendship.IsMarried())
                ev.ReplaceCurrentCommand($"speak Sable \"{I18n.Get("julian.event.sable-dinner")}\"");
            else
                ev.CurrentCommand++;
        });
    }

    internal static string ReviewLine()
    {
        var best = RecordTabs.KeptPaintings().Values.Distinct()
            .OrderByDescending(item => item.Quality)
            .ThenByDescending(item => PaintingData.Read(item)?.Stars ?? 0f)
            .FirstOrDefault();
        if (best == null || PaintingData.Read(best) is not { } data)
            return I18n.Get("julian.review.none");
        string title = PaintingItems.Title(data);
        string key = best.Quality >= PaintingRules.Iridium ? "julian.review.iridium"
            : best.Quality >= PaintingRules.Gold ? "julian.review.gold"
            : "julian.review.plain";
        return I18n.Get(key, new { title });
    }
}
