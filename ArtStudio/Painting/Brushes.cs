namespace ArtStudio.Painting;

// Brushes are the painting minigame's tackle: an equip slot that changes
// how mixing and painting play. Load = cells per full load before level;
// BandBonus widens the mix band (in fishing-track units); Steadiness calms
// the hue marker (0.2 = 20% lower effective difficulty); SparkTime adds
// seconds before an inspiration spark fades.
internal sealed record BrushSpec(string Id, string Name, int Load, float BandBonus, float Steadiness, float SparkTime, int Durability);

internal static class Brushes
{
    public static readonly BrushSpec HogBristle = new("hog", "Hog Bristle Brush", 12, 0f, 0f, 0f, 0);

    public static readonly BrushSpec[] All =
    {
        HogBristle,
        new("sable", "Sable Round Brush", 12, 32f, 0f, 0f, 20),
        new("flat", "Flat Wash Brush", 22, 0f, 0f, 0f, 20),
        new("rigger", "Rigger Brush", 10, 0f, 0.25f, 1.5f, 20),
        new("iridium", "Iridium Brush", 20, 24f, 0.2f, 1f, 40),
    };
}
