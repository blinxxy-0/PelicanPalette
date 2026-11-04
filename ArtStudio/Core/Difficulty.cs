namespace ArtStudio.Core;

// (Simplified is last so older configs' numbers still mean the same; the
// easel offers them easiest first, as Order has them.)
public enum Difficulty { Relaxed, Normal, Hard, Simplified }

public static class Difficulties
{
    public static readonly Difficulty[] Order = { Difficulty.Simplified, Difficulty.Relaxed, Difficulty.Normal, Difficulty.Hard };

    // Simplified (1.3): the easiest, with references in plain paint only (no
    // glazes or highlights to copy) and free paint that never dries.
    // (Unless the player has turned that off: plainPictures is their setting.)
    public static bool PlainPictures(Difficulty d, bool plainPictures = true) => d == Difficulty.Simplified && plainPictures;

    // No paint spent and no Palette needed: Simplified only, from 1.3 (it
    // was Relaxed's until then).
    public static bool FreePaint(Difficulty d) => d == Difficulty.Simplified;
}

// What a painting session asks of the player at once. Hard is the full
// fishing-style juggle (paint runs out, the light never stops, sparks are
// fleeting); playtesting showed that's too much for a default. Normal keeps
// one thing on your plate at a time (the light waits while you mix) but
// still asks for decisions: mixed colors dry after a while, so you choose
// what to mix and finish it before it goes. Relaxed is Normal with no clock
// at all (1.3: from player feedback, Relaxed should only take the time
// limit away). Simplified is the easiest: no clock, free paint that never
// dries, a wider mixing band, and plain-paint pictures.
//
//   PaintLoads            the brush runs dry and must be remixed
//   DryTime               seconds of painting before a mixed color dries
//                         (when paint doesn't run out; infinity = never)
//   LightTimer            there's a light meter at all
//   LightMultiplier       how much longer the light lasts than Hard's
//   PauseLightWhileMixing the clock stops while the mix bar is up
//   SparkLifetime         seconds before a spark fades (infinity = never)
//   BandBonus             extra mix band height, in fishing-track units
//   MixTimeBonus          extra seconds before a mix times out
//   PriceMultiplier       on a painting's value, set when it's finished
//                         (1.3: Simplified spends only the canvas, so it's
//                         worth half; Relaxed has all the time in the world;
//                         Hard asks the most, so it pays three times)
//   XpMultiplier          on Painting XP
public sealed record DifficultySettings(
    bool PaintLoads,
    float DryTime,
    bool LightTimer,
    float LightMultiplier,
    bool PauseLightWhileMixing,
    float SparkLifetime,
    float BandBonus,
    float MixTimeBonus,
    float PriceMultiplier,
    float XpMultiplier)
{
    public static DifficultySettings For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Simplified => new(false, float.PositiveInfinity, false, 1f, true, float.PositiveInfinity, 40f, 2f, 0.5f, 1f),
        // (Sparks wait, as there's no clock to race.)
        Difficulty.Relaxed => new(false, 25f, false, 0.9f, true, float.PositiveInfinity, 16f, 1f, 0.85f, 1f),
        Difficulty.Hard => new(true, float.PositiveInfinity, true, 0.8f, false, 4f, 0f, 0f, 3f, 1.2f),
        _ => new(false, 25f, true, 0.9f, true, 8f, 16f, 1f, 1f, 1f),
    };
}
