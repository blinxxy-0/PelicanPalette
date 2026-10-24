using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;

namespace ArtStudio.Painting;

// The player's painting progress. XP and level live in SpaceCore's Painting
// skill; the pigments they own live in their modData so they save with the
// farmer and sync in multiplayer.
internal static class ArtProgress
{
    private const string PigmentsKey = ModEntry.Id + "/Pigments";

    // Where XP was kept before the SpaceCore skill existed; moved over on load.
    private const string LegacyXpKey = ModEntry.Id + "/Xp";

    // Dev snapshots run from the title screen, where there's no save for
    // SpaceCore to track XP in.
    public static int? DevLevel { get; set; }

    public static IReadOnlyList<int> OwnedPigments(Farmer who)
    {
        if (!who.modData.TryGetValue(PigmentsKey, out string raw) || string.IsNullOrWhiteSpace(raw))
            return StarterPigments();
        var owned = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Palette.IndexOf)
            .Where(i => i >= 0)
            .Distinct()
            .OrderBy(i => i)
            .ToList();
        return owned.Count > 0 ? owned : StarterPigments();
    }

    public static bool Owns(Farmer who, int pigment) => OwnedPigments(who).Contains(pigment);

    public static bool Unlock(Farmer who, int pigment)
    {
        var owned = OwnedPigments(who).ToList();
        if (owned.Contains(pigment)) return false;
        owned.Add(pigment);
        SetOwned(who, owned);
        return true;
    }

    public static void SetOwned(Farmer who, IEnumerable<int> pigments) =>
        who.modData[PigmentsKey] = string.Join(",", pigments.OrderBy(i => i).Select(i => Palette.All[i].Id));

    public static int Xp(Farmer who) =>
        Context.IsWorldReady ? Skills.GetExperienceFor(who, PaintingSkill.SkillId) : 0;

    public static int Level(Farmer who) =>
        !Context.IsWorldReady && DevLevel.HasValue ? DevLevel.Value : Skills.GetSkillLevel(who, PaintingSkill.SkillId);

    // Returns the new level if this XP crossed one, else 0. SpaceCore queues
    // the level-up screen for the end of the day, like vanilla skills.
    public static int AddXp(Farmer who, int amount)
    {
        if (!Context.IsWorldReady) return 0;
        int before = Level(who);
        Skills.AddExperience(who, PaintingSkill.SkillId, amount);
        int after = Level(who);
        return after > before ? after : 0;
    }

    public static void SetLevel(Farmer who, int level)
    {
        if (!Context.IsWorldReady)
        {
            DevLevel = level;
            return;
        }
        int target = level <= 0 ? 0 : PaintingRules.XpCurve[Math.Min(level, PaintingRules.MaxLevel) - 1];
        Skills.AddExperience(who, PaintingSkill.SkillId, target - Xp(who));
    }

    public static void MigrateLegacyXp(Farmer who)
    {
        if (!who.modData.TryGetValue(LegacyXpKey, out string raw)) return;
        who.modData.Remove(LegacyXpKey);
        if (int.TryParse(raw, out int xp) && xp > 0)
            Skills.AddExperience(who, PaintingSkill.SkillId, xp);
    }

    private static List<int> StarterPigments() =>
        Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter).ToList();
}
