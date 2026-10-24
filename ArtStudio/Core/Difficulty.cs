namespace ArtStudio.Core;

public enum Difficulty { Relaxed, Normal, Hard }

// What a painting session asks of the player at once. Hard is the full
// fishing-style juggle (paint runs out, the light never stops, sparks are
// fleeting); playtesting showed that's too much for a default. Normal keeps
// one thing on your plate at a time (the light waits while you mix) but
// still asks for decisions: mixed colors dry after a while, so you choose
// what to mix and finish it before it goes. Relaxed has no clock at all.
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
//   RewardMultiplier      on sell value and XP
public sealed record DifficultySettings(
    bool PaintLoads,
    float DryTime,
    bool LightTimer,
    float LightMultiplier,
    bool PauseLightWhileMixing,
    float SparkLifetime,
    float BandBonus,
    float MixTimeBonus,
    float RewardMultiplier)
{
    public static DifficultySettings For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Relaxed => new(false, float.PositiveInfinity, false, 1f, true, float.PositiveInfinity, 40f, 2f, 1f),
        Difficulty.Hard => new(true, float.PositiveInfinity, true, 1f, false, 4f, 0f, 0f, 1.2f),
        _ => new(false, 25f, true, 0.9f, true, 8f, 16f, 1f, 1f),
    };
}
