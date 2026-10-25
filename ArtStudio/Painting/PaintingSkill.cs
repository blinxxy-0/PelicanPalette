using System.Collections.Generic;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;

namespace ArtStudio.Painting;

// The Painting skill, registered with SpaceCore so it gets a skills-page
// bar, level-up screens and profession choices like the vanilla skills.
// Levels widen the toolkit and the light; professions at 5 and 10 pick a
// direction: painting outdoors (Plein Air) or selling well (Gallerist).
internal sealed class PaintingSkill : Skills.Skill
{
    public const string SkillId = ModEntry.Id + ".Painting";

    public static PaintingSkill Instance { get; private set; } = null!;

    public readonly ArtProfession PleinAir, Gallerist, SteadyHand, Visionary, PatronsFavorite, Prolific;

    // How much each profession changes, kept here so the descriptions and
    // the effects can't drift apart.
    public const float PleinAirLight = 1.2f, PleinAirSparkInterval = 0.7f;
    public const float GalleristPrice = Core.Market.GalleristPrice;
    public const float VisionaryInspiredBonus = 0.5f;
    public const float PatronFriendship = 1.25f;
    public const double ProlificSaveChance = 0.25;

    private PaintingSkill(IModHelper helper)
        : base(SkillId)
    {
        Icon = helper.ModContent.Load<Texture2D>("assets/skill_icon.png");
        SkillsPageIcon = helper.ModContent.Load<Texture2D>("assets/skill_page_icon.png");
        ExperienceCurve = PaintingRules.XpCurve;
        ExperienceBarColor = new Color(205, 110, 160);

        // SpaceCore expects this order: the two level-5 professions, then the four at level 10.
        PleinAir = Add(helper, "PleinAir", I18n.Get("profession.plein-air"), I18n.Get("profession.plein-air.description", new { percent = Percent(PleinAirLight) }), 0);
        Gallerist = Add(helper, "Gallerist", I18n.Get("profession.gallerist"), I18n.Get("profession.gallerist.description", new { percent = Percent(GalleristPrice) }), 1);
        SteadyHand = Add(helper, "SteadyHand", I18n.Get("profession.steady-hand"), I18n.Get("profession.steady-hand.description"), 2);
        Visionary = Add(helper, "Visionary", I18n.Get("profession.visionary"), I18n.Get("profession.visionary.description"), 3);
        PatronsFavorite = Add(helper, "PatronsFavorite", I18n.Get("profession.patrons-favorite"), I18n.Get("profession.patrons-favorite.description", new { percent = Percent(PatronFriendship) }), 4);
        Prolific = Add(helper, "Prolific", I18n.Get("profession.prolific"), I18n.Get("profession.prolific.description", new { chance = $"{ProlificSaveChance * 100:0}%" }), 5);

        ProfessionsForLevels.Add(new ProfessionPair(5, PleinAir, Gallerist));
        ProfessionsForLevels.Add(new ProfessionPair(10, SteadyHand, Visionary, PleinAir));
        ProfessionsForLevels.Add(new ProfessionPair(10, PatronsFavorite, Prolific, Gallerist));
    }

    public static void Register(IModHelper helper)
    {
        Instance = new PaintingSkill(helper);
        Skills.RegisterSkill(Instance);
    }

    private ArtProfession Add(IModHelper helper, string id, string name, string description, int iconIndex)
    {
        var profession = new ArtProfession(this, id, name, description)
        {
            Icon = ProfessionIcon(helper, iconIndex),
        };
        Professions.Add(profession);
        return profession;
    }

    private static Texture2D ProfessionIcon(IModHelper helper, int index) =>
        helper.ModContent.Load<Texture2D>($"assets/professions/{index}.png");

    private static string Percent(float multiplier) => $"{(multiplier - 1f) * 100f:0}%";

    public override string GetName() => I18n.Get("skill.name");

    public override List<string> GetExtraLevelUpInfo(int level)
    {
        var lines = new List<string> { I18n.Get("skill.level-up.light") };
        if (level == PaintingRules.BroadBrushLevel) lines.Add(I18n.Get("skill.level-up.broad"));
        if (level == PaintingRules.RulerLevel) lines.Add(I18n.Get("skill.level-up.ruler"));
        if (level == PaintingRules.MirrorLevel) lines.Add(I18n.Get("skill.level-up.mirror"));
        if (level == PaintingRules.EyedropperLevel) lines.Add(I18n.Get("skill.level-up.dropper"));
        if (level == PaintingRules.CanvasUnlockLevel(CanvasKind.Medium)) lines.Add(I18n.Get("skill.level-up.medium"));
        if (level == PaintingRules.CanvasUnlockLevel(CanvasKind.Large)) lines.Add(I18n.Get("skill.level-up.large"));
        if (level == Varnish.UnlockLevel) lines.Add(I18n.Get("skill.level-up.varnish"));
        if (level == PaintingRules.LinenLevel) lines.Add(I18n.Get("skill.level-up.linen"));
        if (PaintingRules.MaxQuality(level) > PaintingRules.MaxQuality(level - 1))
            lines.Add(I18n.Get("skill.level-up.quality", new { quality = QualityName(PaintingRules.MaxQuality(level)) }));
        return lines;
    }

    public override string GetSkillPageHoverText(int level) =>
        I18n.Get("skill.hover", new { percent = level * 3, quality = QualityName(PaintingRules.MaxQuality(level)) });

    private static string QualityName(int quality) => quality switch
    {
        PaintingRules.Silver => I18n.Get("quality.silver.lower"),
        PaintingRules.Gold => I18n.Get("quality.gold.lower"),
        PaintingRules.Iridium => I18n.Get("quality.iridium.lower"),
        _ => I18n.Get("quality.normal.lower"),
    };

    public static bool Has(Farmer who, Profession profession) => who.HasCustomProfession(profession);
}

internal sealed class ArtProfession : Skills.Skill.Profession
{
    private readonly string _name, _description;

    public ArtProfession(Skills.Skill skill, string id, string name, string description)
        : base(skill, id)
    {
        _name = name;
        _description = description;
    }

    public override string GetName() => _name;

    public override string GetDescription() => _description;
}
