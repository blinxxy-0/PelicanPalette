namespace ArtStudio.Core;

// What a painting sells for beyond its own value (PaintingRules.Value):
// every price multiplier the game applies, kept here, free of game types,
// so the simulator's economy check (ArtStudio.Sim economy) runs on the
// shipping numbers. The game's classes refer to these.
//
// Tuned 2026-10-25 so the top of the craft (a new sitter's Masterpiece,
// varnished and consigned) earns under twice late Skull Cavern runs per
// hour, and an ordinary late painting about what they do.
public static class Market
{
    // Stardew's own quality multiplier on sale price (silver 1.25x, gold 1.5x, iridium 2x).
    public static float QualityPrice(int quality) => 1f + 0.25f * quality;

    // Sable pays more for paintings than Pierre does.
    public const float ArtShopPremium = 1.2f;

    // Consigned at Sable's gallery: sells in a few days for more than a shop pays.
    public const float ConsignPremium = 1.35f;

    // The Gallerist profession (Painting level 5).
    public const float GalleristPrice = 1.25f;

    // The 30-subject milestone.
    public const float RenownValue = 1.1f;

    // A subject's first painting ever pays more. After that, only the same
    // week counts: the first two of it this week sell at full price, then
    // less, and every Monday it's full price again (so a favourite subject
    // can't be farmed all week, but never runs out for good).
    public static float Novelty(bool paintedBefore, int timesThisWeek) =>
        !paintedBefore ? 1.25f
        : timesThisWeek switch
        {
            <= 1 => 1f,
            <= 3 => 0.85f,
            _ => 0.7f,
        };

    // Varnishing, like a cask: days in the cabinet, finish, value.
    public static readonly (int Days, string Key, float Value)[] Finishes =
    {
        (7, "satin", 1.1f),
        (14, "gloss", 1.2f),
        (28, "gallery", 1.3f),
    };
}
