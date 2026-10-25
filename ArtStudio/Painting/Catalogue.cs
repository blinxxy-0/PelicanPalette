using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ArtStudio.Core;
using StardewValley;

namespace ArtStudio.Painting;

// The painter's record (2026-10-25): every subject they've painted, how
// often, and at their best quality. It prices novelty (a subject you've
// never painted pays more; the fifth parsnip this week pays less), fills
// the collection page, and pays out milestones for the variety of subjects.
internal sealed class CatalogueEntry
{
    public int Count { get; set; }
    public int BestQuality { get; set; }
    public int FirstDay { get; set; }

    // Paintings of it this week (Week: the game's week number, from Monday).
    public int Week { get; set; }
    public int WeekCount { get; set; }
}

internal static class Catalogue
{
    private const string Key = ModEntry.Id + "/Catalogue";
    private const string PerksKey = ModEntry.Id + "/CollectionPerks";

    // Milestones for different subjects painted, and what each brings.
    public static readonly (int Subjects, string Perk)[] Milestones =
    {
        (5, "tubes"),          // three tubes of aquamarine (and the colour itself)
        (15, "palette"),       // the palette's wells hold half again as much
        (30, "renown"),        // paintings are worth 10% more
        (50, "iridium"),       // a letter from Sable, with an iridium brush
    };
    public const float RenownValue = Market.RenownValue;

    public static Dictionary<string, CatalogueEntry> All(Farmer who)
    {
        if (!who.modData.TryGetValue(Key, out string raw) || string.IsNullOrEmpty(raw))
            return new Dictionary<string, CatalogueEntry>();
        try { return JsonSerializer.Deserialize<Dictionary<string, CatalogueEntry>>(raw) ?? new(); }
        catch (JsonException) { return new Dictionary<string, CatalogueEntry>(); }
    }

    private static void Save(Farmer who, Dictionary<string, CatalogueEntry> all) =>
        who.modData[Key] = JsonSerializer.Serialize(all);

    // A still life of the same items in any order is the same subject.
    public static string SubjectKey(string subject) =>
        subject.Contains('|') ? string.Join("|", subject.Split('|').OrderBy(s => s, StringComparer.Ordinal)) : subject;

    public static int TimesPainted(Farmer who, string subject) =>
        All(who).TryGetValue(SubjectKey(subject), out var entry) ? entry.Count : 0;

    public static int CurrentWeek => Game1.Date.TotalDays / 7;

    // Paintings of this subject so far this week.
    public static int TimesThisWeek(Farmer who, string subject) =>
        All(who).TryGetValue(SubjectKey(subject), out var entry) && entry.Week == CurrentWeek ? entry.WeekCount : 0;

    // How much a painting's price moves for being new, or familiar this week.
    public static float Novelty(Farmer who, string subject) =>
        Market.Novelty(TimesPainted(who, subject) > 0, TimesThisWeek(who, subject));

    public static bool HasPerk(Farmer who, string perk) =>
        who.modData.TryGetValue(PerksKey, out string raw) && raw.Split(',').Contains(perk);


    // Records a finished painting; returns the milestone it reached, if any.
    public static (int Subjects, string Perk)? Record(Farmer who, string subject, int quality)
    {
        if (subject == PaintingData.OriginalSubject) return null;
        var all = All(who);
        string key = SubjectKey(subject);
        bool isNew = !all.TryGetValue(key, out var entry);
        entry ??= new CatalogueEntry { FirstDay = Game1.Date.TotalDays };
        entry.Count++;
        if (entry.Week != CurrentWeek)
            (entry.Week, entry.WeekCount) = (CurrentWeek, 0);
        entry.WeekCount++;
        entry.BestQuality = Math.Max(entry.BestQuality, quality);
        all[key] = entry;
        Save(who, all);
        if (!isNew) return null;

        foreach (var milestone in Milestones)
            if (all.Count == milestone.Subjects && !HasPerk(who, milestone.Perk))
            {
                Grant(who, milestone.Perk);
                return milestone;
            }
        return null;
    }

    private static void Grant(Farmer who, string perk)
    {
        who.modData[PerksKey] = who.modData.TryGetValue(PerksKey, out string raw) && raw.Length > 0 ? raw + "," + perk : perk;
        switch (perk)
        {
            case "tubes":
                // (Granted as the easel shows its results, so not by a menu.)
                var tubes = ItemRegistry.Create("(O)" + ArtItems.TubeId(Palette.IndexOf("aquamarine")), 3);
                if (!who.addItemToInventoryBool(tubes))
                    Game1.createItemDebris(tubes, who.getStandingPosition(), who.FacingDirection);
                break;
            case "iridium":
                Game1.addMailForTomorrow(ModEntry.Id + "_Collection50");
                break;
        }
    }
}
