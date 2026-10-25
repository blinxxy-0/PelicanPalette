using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ArtStudio.Core;
using ArtStudio.Painting;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Shop;

// A villager's request on the art shop's board: a painting of something
// they love or like, at least a given quality, for a reward. Accepting it
// starts the clock; handing them a matching painting completes it.
internal sealed class Commission
{
    public string Npc { get; set; } = "";
    public string ItemId { get; set; } = "";
    public int MinQuality { get; set; }
    public int Reward { get; set; }
    public int ExpiresOn { get; set; }
    public bool Accepted { get; set; }

    // A villager can ask for their own portrait ("sitter:<name>") instead of an item.
    public bool IsPortrait => Sitters.IsSitter(ItemId);

    public string ItemName => IsPortrait ? I18n.Get("commission.portrait") : ItemRegistry.GetDataOrErrorItem(ItemId).DisplayName;

    public string NpcName => Game1.getCharacterFromName(Npc)?.displayName ?? Npc;

    public int DaysLeft => ExpiresOn - Game1.Date.TotalDays;

    public bool Matches(Item item) =>
        PaintingItems.IsPainting(item)
        && PaintingData.Read(item)?.Subject.Split('|').Contains(ItemId) == true;
}

internal static class Commissions
{
    private const string Key = ModEntry.Id + "/Commissions";

    public const int MaxPosted = 3, MaxAccepted = 2;
    public const int DaysToAccept = 3, DaysToDeliver = 7;
    public const int FriendshipReward = 100, XpReward = 40;
    public const float PatronRewardMultiplier = 1.5f;
    // How often a friend who'd sit for a portrait asks for one instead of an item.
    public const double PortraitChance = 0.35;

    public static List<Commission> All(Farmer who)
    {
        if (!who.modData.TryGetValue(Key, out string raw) || string.IsNullOrEmpty(raw))
            return new List<Commission>();
        try { return JsonSerializer.Deserialize<List<Commission>>(raw) ?? new List<Commission>(); }
        catch (JsonException) { return new List<Commission>(); }
    }

    public static void Save(Farmer who, List<Commission> list) =>
        who.modData[Key] = JsonSerializer.Serialize(list);

    public static IEnumerable<Commission> Posted(Farmer who) => All(who).Where(c => !c.Accepted);

    public static IEnumerable<Commission> Accepted(Farmer who) => All(who).Where(c => c.Accepted);

    // Each morning: let lapsed ones go and top the board back up.
    public static void NewDay(Farmer who, IMonitor monitor, Random random)
    {
        var list = All(who);
        foreach (var lapsed in list.Where(c => c.DaysLeft < 0).ToList())
        {
            list.Remove(lapsed);
            if (lapsed.Accepted)
                Game1.addHUDMessage(new HUDMessage(lapsed.IsPortrait ? I18n.Get("commission.lapsed.portrait", new { npc = lapsed.NpcName })
                    : I18n.Get("commission.lapsed", new { npc = lapsed.NpcName, item = lapsed.ItemName })) { noIcon = true });
        }

        var taken = new HashSet<string>(list.Select(c => c.Npc));
        int level = ArtProgress.Level(who);
        for (int tries = 0; list.Count(c => !c.Accepted) < MaxPosted && tries < 30; tries++)
        {
            var commission = Generate(who, level, taken, random);
            if (commission == null) break;
            list.Add(commission);
            taken.Add(commission.Npc);
        }
        Save(who, list);
    }

    // A request from a villager the player has met, for one of the things
    // they love or like that can be painted.
    private static Commission? Generate(Farmer who, int level, HashSet<string> taken, Random random)
    {
        var clients = Utility.getAllVillagers()
            .Where(n => n.CanSocialize && !taken.Contains(n.Name) && who.friendshipData.ContainsKey(n.Name)
                && Game1.NPCGiftTastes.ContainsKey(n.Name))
            .ToList();
        if (clients.Count == 0) return null;
        var npc = clients[random.Next(clients.Count)];

        int minQuality = level switch
        {
            < 3 => PaintingRules.Normal,
            < 6 => PaintingRules.Silver,
            < 9 => PaintingRules.Gold,
            _ => random.NextDouble() < 0.3 ? PaintingRules.Iridium : PaintingRules.Gold,
        };
        float qualityFactor = 1f + minQuality * 0.25f;
        float multiplier = PaintingSkill.Has(who, PaintingSkill.Instance.PatronsFavorite) ? PatronRewardMultiplier : 1f;

        // Their own portrait: only from someone close enough to sit for it,
        // and once the player has a canvas big enough.
        if (level >= PaintingRules.CanvasUnlockLevel(CanvasKind.Medium) && Sitters.CanSitFor(who, npc.Name) && random.NextDouble() < PortraitChance)
        {
            int portraitValue = (int)(PaintingRules.Value(PaintingRules.CanvasSize(CanvasKind.Medium), 70, 1000, level) * PaintingRules.PortraitValue);
            return new Commission
            {
                Npc = npc.Name,
                ItemId = Sitters.SubjectFor(npc.Name),
                MinQuality = minQuality,
                Reward = (int)Math.Round(portraitValue * qualityFactor * 1.5f * multiplier / 10f) * 10,
                ExpiresOn = Game1.Date.TotalDays + DaysToAccept,
            };
        }

        var tastes = Game1.NPCGiftTastes[npc.Name].Split('/');
        var wanted = tastes.ElementAtOrDefault(1)?.Split(' ').Concat(tastes.ElementAtOrDefault(3)?.Split(' ') ?? Array.Empty<string>())
            .Where(id => id.Length > 0 && !id.StartsWith("-"))
            .Select(id => ItemRegistry.Create(ItemRegistry.QualifyItemId(id) ?? id, allowNull: true))
            .Where(i => i != null && ReferenceBuilder.CanPaint(i))
            .ToList();
        if (wanted == null || wanted.Count == 0) return null;
        var subject = wanted[random.Next(wanted.Count)]!;
        int value = PaintingRules.Value(PaintingRules.CanvasSize(CanvasKind.Small), 50, subject.salePrice(), level);
        int reward = (int)Math.Round(value * qualityFactor * 1.5f * multiplier / 10f) * 10;

        return new Commission
        {
            Npc = npc.Name,
            ItemId = subject.QualifiedItemId,
            MinQuality = minQuality,
            Reward = reward,
            ExpiresOn = Game1.Date.TotalDays + DaysToAccept,
        };
    }

    public static bool Accept(Farmer who, Commission commission)
    {
        var list = All(who);
        var match = list.FirstOrDefault(c => !c.Accepted && c.Npc == commission.Npc && c.ItemId == commission.ItemId);
        if (match == null || list.Count(c => c.Accepted) >= MaxAccepted) return false;
        match.Accepted = true;
        match.ExpiresOn = Game1.Date.TotalDays + DaysToDeliver;
        Save(who, list);
        return true;
    }

    public static Commission? ActiveFor(Farmer who, string npc) => Accepted(who).FirstOrDefault(c => c.Npc == npc);

    // Completes the commission: the painting goes, the reward comes.
    public static void Deliver(Farmer who, NPC npc, Commission commission, SObject painting)
    {
        who.reduceActiveItemByOne();
        who.Money += commission.Reward;
        who.changeFriendship(FriendshipReward, npc);
        int newLevel = ArtProgress.AddXp(who, XpReward);
        var list = All(who);
        list.RemoveAll(c => c.Accepted && c.Npc == commission.Npc && c.ItemId == commission.ItemId);
        Save(who, list);

        Game1.playSound("purchaseClick");
        npc.doEmote(Character.happyEmote);
        bool better = painting.Quality > commission.MinQuality;
        string line = commission.IsPortrait
            ? I18n.Get(better ? "commission.delivered.portrait.better" : "commission.delivered.portrait", new { reward = commission.Reward })
            : I18n.Get(better ? "commission.delivered.better" : "commission.delivered", new { item = commission.ItemName.ToLowerInvariant(), reward = commission.Reward });
        npc.setNewDialogue(new Dialogue(npc, null, line), add: true);
        Game1.drawDialogue(npc);
        if (newLevel > 0)
            Game1.addHUDMessage(new HUDMessage(I18n.Get("results.level-up", new { level = newLevel })) { noIcon = true });
    }

    public static string QualityText(int quality) => quality switch
    {
        PaintingRules.Silver => I18n.Get("commission.quality.silver"),
        PaintingRules.Gold => I18n.Get("commission.quality.gold"),
        PaintingRules.Iridium => I18n.Get("commission.quality.iridium"),
        _ => I18n.Get("commission.quality.any"),
    };
}
