using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Network.NetEvents;

namespace ArtStudio.Painting;

// The portfolio book in the art shop's back room (1.2): pages of slots, like
// the Community Center's bundles, each slot asking for a particular painting.
// Unlike a bundle, the painting is kept: it sits in its slot, shown as itself,
// and a better one can replace it later (the old one comes back). A filled
// slot never empties, so a finished page stays finished.
//
// One book for the whole farm: the paintings live in a global inventory (one
// entry per slot, in page order), and a finished page is a mail flag every
// player gets.
internal sealed record PortfolioSlot(string Id, Func<Item, PaintingData, bool> Fits);

internal sealed record PortfolioPage(string Id, PortfolioSlot[] Slots);

internal static class Portfolio
{
    public const string InventoryId = ModEntry.Id + "_Portfolio";

    // Pages are append-only, and so are the slots within a page: a slot's
    // painting is stored at its position in the book.
    public static readonly PortfolioPage[] Pages =
    {
        new("seasons", new PortfolioSlot[]
        {
            new("town-spring", (_, d) => d.Subject == "view:Town:spring"),
            new("town-summer", (_, d) => d.Subject == "view:Town:summer"),
            new("town-fall", (_, d) => d.Subject == "view:Town:fall"),
            new("town-winter", (_, d) => d.Subject == "view:Town:winter"),
            new("farm", (_, d) => d.IsView && Views.PlaceOf(d.Subject) == "Farm"),
        }),
        new("harvest", new PortfolioSlot[]
        {
            new("crop-spring", (_, d) => d.SubjectItems.Any(id => GrowsIn(id, Season.Spring))),
            new("crop-summer", (_, d) => d.SubjectItems.Any(id => GrowsIn(id, Season.Summer))),
            new("crop-fall", (_, d) => d.SubjectItems.Any(id => GrowsIn(id, Season.Fall))),
            new("orchard", (_, d) => d.SubjectItems.Any(IsTreeFruit)),
            new("three-crops", (_, d) => d.SubjectItems.Length == 3 && d.SubjectItems.All(id => RecordTabs.TabOfItem(id) == RecordTab.Crops)),
        }),
        new("fine-work", new PortfolioSlot[]
        {
            new("gold", (item, _) => item.Quality >= PaintingRules.Gold),
            new("iridium", (item, _) => item.Quality >= PaintingRules.Iridium),
            new("glazed", (item, d) => PaintingData.GlazedOf(item) ?? (d.Marks != null && Technique.Count(d.Marks, Mark.Glaze) > 0)),
            new("varnished", (item, _) => Varnish.FinishOf(item) != null),
            new("masterpiece", (item, _) => PaintingData.IsMasterpiece(item)),
        }),
    };

    public static int SlotCount => Pages.Sum(p => p.Slots.Length);

    // Where a page's first slot sits in the book.
    public static int FirstIndex(int page) => Pages.Take(page).Sum(p => p.Slots.Length);

    public static Inventory Items
    {
        get
        {
            var items = Game1.player.team.GetOrCreateGlobalInventory(InventoryId);
            while (items.Count < SlotCount)
                items.Add(null);
            return items;
        }
    }

    public static StardewValley.Network.NetMutex Mutex => Game1.player.team.GetOrCreateGlobalInventoryMutex(InventoryId);

    public static Item? In(int index) => index < Items.Count ? Items[index] : null;

    // ---- What fits ----

    // A painting, framed or not, with its picture (a blank one from an item
    // spawner can't go in).
    public static bool IsKeepable(Item? item) =>
        item != null && (PaintingItems.IsPainting(item) || ArtItems.IsFramed(item)) && PaintingData.Read(item) != null;

    public static bool Fits(PortfolioSlot slot, Item item) =>
        IsKeepable(item) && PaintingData.Read(item) is { } data && slot.Fits(item, data);

    // A better painting for the same slot: higher quality, or as good with more stars.
    public static bool IsBetter(Item candidate, Item current)
    {
        if (candidate.Quality != current.Quality) return candidate.Quality > current.Quality;
        return (PaintingData.Read(candidate)?.Stars ?? 0f) > (PaintingData.Read(current)?.Stars ?? 0f);
    }

    // The slot on a page a painting would go in: the first empty one it fits,
    // else the first filled one it would improve. -1 if none. A page lists
    // its slots easiest first, so they're tried from the end: a Masterpiece
    // goes in "a Masterpiece", not "a gold painting", and three crops
    // together in "three crops", not "a spring crop".
    public static int SlotFor(int page, Item item)
    {
        int first = FirstIndex(page);
        var slots = Pages[page].Slots;
        for (int i = slots.Length - 1; i >= 0; i--)
            if (In(first + i) == null && Fits(slots[i], item))
                return first + i;
        for (int i = slots.Length - 1; i >= 0; i--)
            if (In(first + i) is { } current && Fits(slots[i], item) && IsBetter(item, current))
                return first + i;
        return -1;
    }

    // ---- Putting paintings in ----

    // Puts a painting from the player's bag in its slot on the page. Returns
    // the slot it went in (or -1), and the painting it replaced, which goes
    // back to the player.
    public static (int Slot, Item? Replaced) Place(Farmer who, int page, Item item)
    {
        int slot = SlotFor(page, item);
        if (slot < 0) return (-1, null);
        var items = Items;
        var replaced = items[slot];
        // (Paintings don't stack, each having its own picture.)
        who.removeItemFromInventory(item);
        items[slot] = item;
        if (replaced != null && !who.addItemToInventoryBool(replaced))
            Game1.createItemDebris(replaced, who.getStandingPosition(), who.FacingDirection);
        CheckFinished(who);
        return (slot, replaced);
    }

    // ---- Finished pages ----

    public static string PageFlag(PortfolioPage page) => $"{ModEntry.Id}_Portfolio_{page.Id}";

    public static bool IsFilled(int page)
    {
        int first = FirstIndex(page);
        return Enumerable.Range(first, Pages[page].Slots.Length).All(i => In(i) != null);
    }

    public static bool IsFinished(PortfolioPage page) => Game1.player.mailReceived.Contains(PageFlag(page));

    // A page finished for the first time: the flag for everyone, and Sable's
    // letter (with the page's reward) for whoever filled its last slot.
    private static void CheckFinished(Farmer who)
    {
        for (int p = 0; p < Pages.Length; p++)
        {
            var page = Pages[p];
            if (!IsFilled(p) || IsFinished(page)) continue;
            Game1.player.team.RequestSetMail(PlayerActionTarget.All, PageFlag(page), MailType.Received, add: true);
            who.mailReceived.Add(PageFlag(page));
            Game1.addMailForTomorrow(PageFlag(page) + "_Letter");
            Game1.playSound("reward");
            Game1.addHUDMessage(new HUDMessage(I18n.Get("portfolio.page-done", new { page = PageName(page) })) { noIcon = true });
        }
    }

    public static string PageName(PortfolioPage page) => I18n.Get("portfolio.page." + page.Id);

    // ---- Crops ----

    private static Dictionary<string, List<Season>>? _cropSeasons;
    private static HashSet<string>? _treeFruit;

    private static bool GrowsIn(string qualifiedId, Season season)
    {
        if (_cropSeasons == null)
        {
            _cropSeasons = new Dictionary<string, List<Season>>();
            foreach (var crop in Game1.cropData.Values)
                if (!string.IsNullOrEmpty(crop.HarvestItemId) && crop.Seasons != null)
                {
                    string id = ItemRegistry.QualifyItemId(crop.HarvestItemId) ?? crop.HarvestItemId;
                    if (!_cropSeasons.TryGetValue(id, out var seasons))
                        _cropSeasons[id] = seasons = new List<Season>();
                    seasons.AddRange(crop.Seasons);
                }
        }
        return _cropSeasons.TryGetValue(qualifiedId, out var list) && list.Contains(season);
    }

    private static bool IsTreeFruit(string qualifiedId)
    {
        _treeFruit ??= Game1.fruitTreeData.Values
            .SelectMany(t => t.Fruit ?? new())
            .Where(f => !string.IsNullOrEmpty(f.ItemId))
            .Select(f => ItemRegistry.QualifyItemId(f.ItemId) ?? f.ItemId)
            .ToHashSet();
        return _treeFruit.Contains(qualifiedId);
    }
}
