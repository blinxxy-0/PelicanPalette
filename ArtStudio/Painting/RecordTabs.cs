using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// The painting record's tabs (1.2): what can be painted, sorted the way the
// game's Collections tab sorts it, so the record can show what's still to
// paint as silhouettes and reward painting a whole category. The record's
// saved data doesn't change (Catalogue keeps one entry per subject); the
// tabs are worked out from it.
//
// A still life of several items counts for each of them. A view counts for
// its place in any season.
internal enum RecordTab { Overview, Crops, Forage, Fish, Minerals, Cooking, Portraits, Views, Other }

// One subject in a tab, painted or not: an item's qualified ID, a sitter
// ("sitter:Leah") or a place ("view:Beach").
internal sealed record TabMember(string Key, CatalogueEntry? Painted, IReadOnlyList<string> Seasons);

internal static class RecordTabs
{
    // The tabs that can be completed (not the overview, nor "everything else").
    public static readonly RecordTab[] Completable =
        { RecordTab.Crops, RecordTab.Forage, RecordTab.Fish, RecordTab.Minerals, RecordTab.Cooking, RecordTab.Portraits, RecordTab.Views };

    // Rewards for painting a share of a tab.
    public static readonly int[] RewardPercents = { 25, 50, 100 };
    public const int RewardTubes = 3, RewardFrames = 2;
    public const float SpecialistValue = 1.1f;

    // The colour each tab's first reward brings.
    public static string TabPigment(RecordTab tab) => tab switch
    {
        RecordTab.Crops => "leaf",
        RecordTab.Forage => "moss",
        RecordTab.Fish => "cerulean",
        RecordTab.Minerals => "violet",
        RecordTab.Cooking => "crimson",
        RecordTab.Portraits => "peach",
        _ => "slate",
    };

    private const string ViewPrefix = "view:";

    // Places that are outdoors but aren't really places to paint.
    private static readonly HashSet<string> NotViews = new() { "Temp", "BeachNightMarket", "Backwoods_Temp" };

    // ---- Which tab ----

    public static RecordTab TabOfItem(string qualifiedId)
    {
        if (!qualifiedId.StartsWith("(O)") || !Game1.objectData.TryGetValue(qualifiedId.Substring(3), out var data))
            return RecordTab.Other;
        return TabOf(qualifiedId, data);
    }

    private static RecordTab TabOf(string qualifiedId, ObjectData data)
    {
        return data.Category switch
        {
            SObject.GreensCategory => RecordTab.Forage,
            // Produce you grow (from seeds or a fruit tree) is a crop; the
            // rest, like salmonberries, is forage.
            SObject.VegetableCategory or SObject.FruitsCategory or SObject.flowersCategory =>
                Grown().Contains(qualifiedId) && data.ContextTags?.Contains("forage_item") != true ? RecordTab.Crops : RecordTab.Forage,
            SObject.FishCategory when !data.ExcludeFromFishingCollection => RecordTab.Fish,
            SObject.GemCategory or SObject.mineralsCategory => RecordTab.Minerals,
            SObject.CookingCategory => RecordTab.Cooking,
            _ => RecordTab.Other,
        };
    }

    private static HashSet<string>? _grown;

    // Everything a crop or fruit tree gives (read once; the game's data
    // doesn't change during play).
    private static HashSet<string> Grown()
    {
        if (_grown != null) return _grown;
        var grown = new HashSet<string>();
        foreach (var crop in Game1.cropData.Values)
            if (!string.IsNullOrEmpty(crop.HarvestItemId))
                grown.Add(ItemRegistry.QualifyItemId(crop.HarvestItemId) ?? crop.HarvestItemId);
        foreach (var tree in Game1.fruitTreeData.Values)
            foreach (var fruit in tree.Fruit ?? new())
                if (!string.IsNullOrEmpty(fruit.ItemId))
                    grown.Add(ItemRegistry.QualifyItemId(fruit.ItemId) ?? fruit.ItemId);
        return _grown = grown;
    }

    // The tab a painting's subject belongs to (a still life: its first item's).
    public static RecordTab TabOfSubject(string subject)
    {
        if (subject == PaintingData.OriginalSubject || FarmerPortrait.IsPortrait(subject)) return RecordTab.Other;
        if (Sitters.IsSitter(subject)) return RecordTab.Portraits;
        if (Views.IsView(subject)) return RecordTab.Views;
        return TabOfItem(subject.Split('|')[0]);
    }

    // ---- What a tab holds ----

    // Everything a tab can hold, painted or not, in the game's own order;
    // "everything else" holds only what's been painted.
    public static List<TabMember> Members(Farmer who, RecordTab tab)
    {
        var painted = PaintedKeys(who);
        if (tab == RecordTab.Overview)
            return Catalogue.All(who).OrderBy(kv => kv.Value.FirstDay).Select(kv => new TabMember(kv.Key, kv.Value, Array.Empty<string>())).ToList();
        if (tab == RecordTab.Other)
            return painted.Where(kv => !kv.Key.StartsWith(ViewPrefix) && !Sitters.IsSitter(kv.Key) && TabOfItem(kv.Key) == RecordTab.Other)
                .OrderBy(kv => kv.Value.Entry.FirstDay)
                .Select(kv => new TabMember(kv.Key, kv.Value.Entry, kv.Value.Seasons)).ToList();
        return AllKeys(tab)
            .Select(key => painted.TryGetValue(key, out var p) ? new TabMember(key, p.Entry, p.Seasons) : new TabMember(key, null, Array.Empty<string>()))
            .ToList();
    }

    public static (int Done, int Total) Progress(Farmer who, RecordTab tab)
    {
        var members = Members(who, tab);
        return (members.Count(m => m.Painted != null), members.Count);
    }

    // Every key a completable tab can hold.
    private static IEnumerable<string> AllKeys(RecordTab tab)
    {
        switch (tab)
        {
            case RecordTab.Portraits:
                return Utility.getAllVillagers()
                    .Where(npc => npc.CanSocialize && Sitters.PortraitTexture(npc.Name) != null)
                    .Select(npc => npc.Name).Distinct()
                    .Select(Sitters.SubjectFor);
            case RecordTab.Views:
                return Game1.locations
                    .Where(l => l.IsOutdoors && l is not MineShaft && l is not VolcanoDungeon && !NotViews.Contains(l.Name))
                    .Select(l => ViewPrefix + l.Name).Distinct();
            default:
                return Game1.objectData
                    .Where(kv => !kv.Value.ExcludeFromShippingCollection || kv.Value.Category == SObject.FishCategory)
                    .Select(kv => "(O)" + kv.Key)
                    .Where(id => !id.StartsWith("(O)" + ModEntry.Id + "_") && IsSixteen(id) && TabOfItem(id) == tab);
        }
    }

    private static bool IsSixteen(string qualifiedId)
    {
        var rect = ItemRegistry.GetDataOrErrorItem(qualifiedId).GetSourceRect();
        return rect.Width == ReferenceBuilder.SpriteSize && rect.Height == ReferenceBuilder.SpriteSize;
    }

    // What's been painted, by tab key: each item of a still life, each sitter,
    // each place (with the seasons it's been painted in).
    private static Dictionary<string, (CatalogueEntry Entry, List<string> Seasons)> PaintedKeys(Farmer who)
    {
        var keys = new Dictionary<string, (CatalogueEntry Entry, List<string> Seasons)>();
        void Add(string key, CatalogueEntry entry, string? season = null)
        {
            if (!keys.TryGetValue(key, out var have))
                have = (new CatalogueEntry { FirstDay = entry.FirstDay }, new List<string>());
            have.Entry.Count += entry.Count;
            have.Entry.BestQuality = Math.Max(have.Entry.BestQuality, entry.BestQuality);
            have.Entry.FirstDay = Math.Min(have.Entry.FirstDay, entry.FirstDay);
            if (season != null && !have.Seasons.Contains(season)) have.Seasons.Add(season);
            keys[key] = have;
        }
        foreach (var (subject, entry) in Catalogue.All(who))
        {
            if (Views.IsView(subject))
            {
                string[] parts = subject.Substring(ViewPrefix.Length).Split(':');
                Add(ViewPrefix + parts[0], entry, parts.Length > 1 ? parts[1] : null);
            }
            else if (Sitters.IsSitter(subject))
                Add(subject, entry);
            else if (subject != PaintingData.OriginalSubject && !FarmerPortrait.IsPortrait(subject))
                foreach (string id in subject.Split('|'))
                    Add(id, entry);
        }
        return keys;
    }

    // ---- What the player knows of it ----

    // Whether to name a subject not yet painted (like the Collections tab,
    // which shows "???" for things you've never had).
    public static bool Known(Farmer who, RecordTab tab, string key)
    {
        if (tab == RecordTab.Views) return true;
        if (tab == RecordTab.Portraits) return who.friendshipData.ContainsKey(Sitters.NpcOf(key));
        string id = key.StartsWith("(O)") ? key.Substring(3) : key;
        return tab switch
        {
            RecordTab.Fish => who.fishCaught.ContainsKey(key) || who.fishCaught.ContainsKey(id),
            RecordTab.Minerals => who.mineralsFound.ContainsKey(id) || who.mineralsFound.ContainsKey(key),
            RecordTab.Cooking => who.recipesCooked.ContainsKey(id) || who.recipesCooked.ContainsKey(key),
            _ => who.basicShipped.ContainsKey(id) || who.basicShipped.ContainsKey(key),
        };
    }

    // ---- Rewards ----

    private static string PerkKey(RecordTab tab, int percent) => $"tab-{tab.ToString().ToLowerInvariant()}-{percent}";

    public static bool HasReward(Farmer who, RecordTab tab, int percent) => Catalogue.HasPerk(who, PerkKey(tab, percent));

    // Paintings of a tab you've painted all of sell for more.
    public static float ValueFor(Farmer who, string subject)
    {
        var tab = TabOfSubject(subject);
        return Completable.Contains(tab) && HasReward(who, tab, 100) ? SpecialistValue : 1f;
    }

    // Grants any tab rewards newly reached; returns what each brought, for
    // the easel to announce. Checked after every painting, so a save from
    // before 1.2 catches up the first time it paints.
    public static List<string> GrantRewards(Farmer who)
    {
        var granted = new List<string>();
        foreach (var tab in Completable)
        {
            var (done, total) = Progress(who, tab);
            if (total == 0) continue;
            foreach (int percent in RewardPercents)
            {
                if (done * 100 < percent * total || HasReward(who, tab, percent)) continue;
                Catalogue.AddPerk(who, PerkKey(tab, percent));
                Grant(who, tab, percent);
                granted.Add(I18n.Get("record.tab-reward." + percent, new { tab = TabName(tab), pigment = I18n.Pigment(Palette.IndexOf(TabPigment(tab))) }));
            }
        }
        return granted;
    }

    private static void Grant(Farmer who, RecordTab tab, int percent)
    {
        Item? gift = null;
        switch (percent)
        {
            case 25:
                int pigment = Palette.IndexOf(TabPigment(tab));
                if (!ArtProgress.Owns(who, pigment))
                    ArtProgress.SetOwned(who, ArtProgress.OwnedPigments(who).Append(pigment));
                gift = ItemRegistry.Create("(O)" + ArtItems.TubeId(pigment), RewardTubes);
                break;
            case 50:
                gift = ItemRegistry.Create("(O)" + ArtItems.FrameItemId, RewardFrames);
                break;
        }
        if (gift != null && !who.addItemToInventoryBool(gift))
            Game1.createItemDebris(gift, who.getStandingPosition(), who.FacingDirection);
    }

    public static string TabName(RecordTab tab) => I18n.Get("record.tab." + tab.ToString().ToLowerInvariant());
}
