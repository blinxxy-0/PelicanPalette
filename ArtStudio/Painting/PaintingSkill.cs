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
    public const float GalleristPrice = 1.25f;
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
        PleinAir = Add(helper, "PleinAir", "Plein Air", $"The light lasts {Percent(PleinAirLight)} longer and inspiration sparks come more often.", 0);
        Gallerist = Add(helper, "Gallerist", "Gallerist", $"Your paintings are worth {Percent(GalleristPrice)} more.", 1);
        SteadyHand = Add(helper, "SteadyHand", "Steady Hand", "Your mixes never turn muddy: at worst, they come out a bit off.", 2);
        Visionary = Add(helper, "Visionary", "Visionary", "An Inspired painting counts for twice as much toward its quality.", 3);
        PatronsFavorite = Add(helper, "PatronsFavorite", "Patron's Favorite", $"Paintings you give as gifts earn {Percent(PatronFriendship)} more friendship.", 4);
        Prolific = Add(helper, "Prolific", "Prolific", $"A {ProlificSaveChance:P0} chance that starting a painting doesn't use up the canvas.", 5);

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

    public override string GetName() => "Painting";

    public override List<string> GetExtraLevelUpInfo(int level)
    {
        var lines = new List<string> { "+3% more light per painting" };
        if (level == PaintingRules.BroadBrushLevel) lines.Add("Broad brush: paint 2x2 cells at once");
        if (level == PaintingRules.RulerLevel) lines.Add("Ruler: drag out perfectly straight lines");
        if (level == PaintingRules.MirrorLevel) lines.Add("Mirror: strokes repeat across the middle");
        if (level == PaintingRules.EyedropperLevel) lines.Add("Dropper: click the canvas to pick its color");
        if (level == PaintingRules.CanvasUnlockLevel(CanvasKind.Medium)) lines.Add("Medium canvases: still lifes of two items");
        if (level == PaintingRules.CanvasUnlockLevel(CanvasKind.Large)) lines.Add("Large canvases: still lifes of three items");
        if (PaintingRules.MaxQuality(level) > PaintingRules.MaxQuality(level - 1))
            lines.Add($"Your paintings can now reach {QualityName(PaintingRules.MaxQuality(level))} quality");
        return lines;
    }

    public override string GetSkillPageHoverText(int level) =>
        $"+{level * 3}% light per painting\nBest quality: {QualityName(PaintingRules.MaxQuality(level))}";

    private static string QualityName(int quality) => quality switch
    {
        PaintingRules.Silver => "silver",
        PaintingRules.Gold => "gold",
        PaintingRules.Iridium => "iridium",
        _ => "normal",
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
