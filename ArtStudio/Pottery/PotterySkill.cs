#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Pottery;

// The Pottery skill (docs/design/pottery_minigame.md, section 8b; 1.5, Debug only
// like the rest of pottery until it ships). A SpaceCore skill from 1 to 10, as
// Painting is: XP from throwing, glazing and firing, and professions at 5 and 10.
//
//   Level 5          Level 10
//   Thrower      ->  Production Potter, Master of Sets
//   Glazer       ->  Kiln Keeper, Alchemist
//
// The professions that change how the wheel, the banding and the kiln play are
// applied to the rules (RulesFor), so the minigame code never asks about them.
// Three wait on things not built yet; see the WAITING notes below.
internal sealed class PotterySkill : Skills.Skill
{
    public const string SkillId = ModEntry.Id + ".Pottery";
    public const int MaxLevel = 10;

    public static PotterySkill? Instance { get; private set; }

    // The keys Has() takes, in SpaceCore's order: the two at level 5, then the four at 10.
    public static readonly string[] ProfessionKeys = { "Thrower", "Glazer", "ProductionPotter", "MasterOfSets", "KilnKeeper", "Alchemist" };

    // How much each profession changes, kept here so the descriptions and the
    // effects can't drift apart.
    public const float ThrowerPrice = 1.25f;              // pots sell for 25% more (WAITING: pot items and selling)
    public const float GlazerDrift = 0.75f;               // the brush hand wanders 25% less while banding
    public const float ProductionPatience = 1.25f;        // patience lasts 25% longer (drain / 1.25)
    public const int ProductionCupsPerLump = 2;           // a small lump makes two cups (WAITING: clay items)
    public const float MasterOfSetsValue = 2f;            // matching sets worth double (WAITING: matching sets)
    public const float MasterOfSetsTolerance = 1.5f;      // and a 50% wider tolerance (WAITING: matching sets)
    public const float KilnKeeperBand = 1.25f;            // stoking's band 25% wider
    public const float KilnKeeperGifts = 2f;              // kiln gifts twice as often
    // WAITING (mineral glazes as items): a rare glaze from a common mineral.
    public static readonly IReadOnlyDictionary<string, string> AlchemistMinerals = new Dictionary<string, string>
    {
        ["Copper red"] = "Copper Ore",
    };

    // The curve: Painting's (the vanilla skills'), so a level takes the same XP.
    public static readonly int[] XpCurve = PaintingRules.XpCurve;

    private readonly Dictionary<string, Skills.Skill.Profession> _byKey = new();

    private PotterySkill(IModHelper helper)
        : base(SkillId)
    {
        Icon = helper.ModContent.Load<Texture2D>("assets/pottery/skill/skill_icon.png");
        SkillsPageIcon = helper.ModContent.Load<Texture2D>("assets/pottery/skill/skill_page_icon.png");
        ExperienceCurve = XpCurve;
        ExperienceBarColor = new Color(196, 102, 62);     // terracotta

        var thrower = Add(helper, 0, new { percent = Percent(ThrowerPrice) });
        var glazer = Add(helper, 1, new { percent = $"{(1f - GlazerDrift) * 100f:0}%" });
        var production = Add(helper, 2, new { percent = Percent(ProductionPatience) });
        var sets = Add(helper, 3, new { percent = Percent(MasterOfSetsTolerance) });
        var kiln = Add(helper, 4, new { percent = Percent(KilnKeeperBand) });
        var alchemist = Add(helper, 5, null);

        ProfessionsForLevels.Add(new ProfessionPair(5, thrower, glazer));
        ProfessionsForLevels.Add(new ProfessionPair(10, production, sets, thrower));
        ProfessionsForLevels.Add(new ProfessionPair(10, kiln, alchemist, glazer));
    }

    // Registered from ModEntry (Debug only), on GameLaunched as Painting is.
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            Instance = new PotterySkill(helper);
            Skills.RegisterSkill(Instance);
        };
        PotterySkillDev.Register(helper, monitor);
    }

    private Skills.Skill.Profession Add(IModHelper helper, int index, object? tokens)
    {
        string key = ProfessionKeys[index];
        string i18n = "pottery.profession." + key.ToLowerInvariant();
        var profession = new ArtProfession(this, key, I18n.Get(i18n), I18n.Get(i18n + ".description", tokens))
        {
            Icon = helper.ModContent.Load<Texture2D>($"assets/pottery/skill/professions/{index}.png"),
        };
        Professions.Add(profession);
        _byKey[key] = profession;
        return profession;
    }

    private static string Percent(float multiplier) => $"{(multiplier - 1f) * 100f:0}%";

    public override string GetName() => I18n.Get("pottery.skill.name");

    public override List<string> GetExtraLevelUpInfo(int level) => new() { I18n.Get("pottery.skill.level-up") };

    public override string GetSkillPageHoverText(int level) => I18n.Get("pottery.skill.hover");

    // ---- Who has what ----

    public static Skills.Skill.Profession? Profession(string key) =>
        Instance != null && Instance._byKey.TryGetValue(key, out var p) ? p : null;

    // "Thrower", "Glazer", "ProductionPotter", "MasterOfSets", "KilnKeeper" or "Alchemist".
    public static bool Has(Farmer? who, string professionKey) =>
        who != null && Profession(professionKey) is { } p && who.HasCustomProfession(p);

    public static int Level(Farmer who) => Instance == null ? 0 : Skills.GetSkillLevel(who, SkillId);

    public static int Xp(Farmer who) => Instance == null ? 0 : Skills.GetExperienceFor(who, SkillId);

    // Thrower: what a pot sells for. (WAITING: pot items and selling; nothing calls it yet.)
    public static float PriceMultiplier(Farmer who) => Has(who, "Thrower") ? ThrowerPrice : 1f;

    // Production Potter: cups from a small lump. (WAITING: clay items; nothing calls it yet.)
    public static int CupsPerSmallLump(Farmer who) => Has(who, "ProductionPotter") ? ProductionCupsPerLump : 1;

    // ---- The professions, on the rules ----

    // The rules this farmer plays a difficulty's rules with.
    public static PotteryRules RulesFor(Farmer? who, PotteryRules rules) =>
        Apply(rules, Has(who, "Glazer"), Has(who, "ProductionPotter"), Has(who, "KilnKeeper"));

    public static PotteryRules Apply(PotteryRules rules, bool glazer, bool productionPotter, bool kilnKeeper)
    {
        if (glazer)
            rules = rules with { Drift = rules.Drift * GlazerDrift };
        if (productionPotter)
            rules = rules with { DrainMultiplier = rules.DrainMultiplier / ProductionPatience };
        if (kilnKeeper)
            rules = rules with { KilnBand = rules.KilnBand * KilnKeeperBand, GiftOdds = rules.GiftOdds * KilnKeeperGifts };
        return rules;
    }

    // ---- XP ----
    //
    // Paced to Painting's (PaintingRules.Xp): a small still life at 4 stars and an
    // average difficulty is about 50 XP for two to three minutes at the easel, about
    // 20 a minute. A whole pot takes about as long (a throw with centring 60 to 90 s,
    // banding about 40 s, the firing 30 s), so a good pot is about 50 too, split by the
    // time each stage takes: throw 25 at 4.5 stars, glaze 11 at 0.85, firing 11 at 0.9.
    // A perfect pot is 54 (56 with a kiln gift). The throw and the glaze give almost
    // nothing for a lump never touched or a pot only dipped, so finishing early isn't
    // a shortcut; the firing always takes its 30 s. Hard is worth 1.2x, as Painting's.
    public static int ThrowXp(float stars, PotteryRules rules) =>
        Scaled(MathF.Max(2f, 7f * (stars - 1f)), rules);

    public static int GlazeXp(float overall, PotteryRules rules) =>
        Scaled(MathF.Max(1f, 14f * (overall - 0.2f) / 0.8f), rules);

    public static int FireXp(FiringResult result, PotteryRules rules) =>
        Scaled(result.Verdict == "Cracked" ? 2f : 4f + 8f * result.Score + (result.Gift != null ? 2f : 0f), rules);

    private static int Scaled(float xp, PotteryRules rules) =>
        (int)MathF.Round(xp * (rules.Name == "Hard" ? 1.2f : 1f));

    // Each stage's finish, from PotteryMenu: XP only for a stage the player finished at
    // the wheel (the menu is open), not for one the bots finished for a snapshot, a
    // calibration or a bot-thrown start. Returns the XP given.
    public static int OnThrown(IClickableMenu menu, PotResult result, PotteryRules rules) =>
        Give(menu, ThrowXp(result.Stars, rules));

    public static int OnGlazed(IClickableMenu menu, GlazeResult result, PotteryRules rules) =>
        Give(menu, GlazeXp(result.Overall, rules));

    public static int OnFired(IClickableMenu menu, FiringResult result, PotteryRules rules) =>
        Give(menu, FireXp(result, rules));

    public static int LastXp { get; private set; }

    private static int Give(IClickableMenu menu, int xp)
    {
        if (Instance == null || !Context.IsWorldReady || Game1.activeClickableMenu != menu)
            return 0;
        // SpaceCore queues the level-up screen for the end of the day, like vanilla skills.
        Skills.AddExperience(Game1.player, SkillId, xp);
        LastXp = xp;
        return xp;
    }
}
#endif
