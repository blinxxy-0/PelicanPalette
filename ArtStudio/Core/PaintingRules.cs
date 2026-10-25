using System;

namespace ArtStudio.Core;

public enum CanvasKind { Small, Medium, Large }

// The numbers that turn skill level, brush and subject into how a painting
// session plays and what the finished piece is worth. Kept free of game
// types so ArtStudio.Sim can run them; tune here and re-run the sim.
public static class PaintingRules
{
    public const int MaxLevel = 10;

    // Stardew's own quality values (Object.Quality).
    public const int Normal = 0, Silver = 1, Gold = 2, Iridium = 4;

    public static int CanvasSize(CanvasKind kind) => kind switch
    {
        CanvasKind.Small => 20,
        CanvasKind.Medium => 28,
        _ => 36,
    };

    // A painting takes part of your day, like fishing or chopping wood: the
    // clock moves on by this much when you finish one. It's what keeps a
    // day's painting income in proportion.
    public static int Minutes(CanvasKind kind) => kind switch
    {
        CanvasKind.Small => 60,
        CanvasKind.Medium => 120,
        _ => 180,
    };

    // The latest a painting may end (2am: past it, the farmer passes out).
    public const int LatestFinish = 2600;

    public static int CanvasUnlockLevel(CanvasKind kind) => kind switch
    {
        CanvasKind.Small => 0,
        CanvasKind.Medium => 4,
        _ => 7,
    };

    // Tools widen with level, in Muse's order.
    public const int BroadBrushLevel = 2, RulerLevel = 3, MirrorLevel = 4, EyedropperLevel = 6;

    // ---- Mixing ----

    // A little taller than a fishing bar at the same level (96 + 8/level):
    // a painting takes many mixes, a catch only one.
    public static float BandHeight(int level, float brushBonus) => 116 + 8 * level + brushBonus;

    public static float MixTimeLimit(bool toppingUp) => toppingUp ? 3f : 5f;

    // Topping up the color already on the brush starts the meter well along.
    public static float MixStartMeter(bool toppingUp) => toppingUp ? 0.6f : 0.35f;

    // Cells one brush load covers. Badly mixed paint goes less far.
    public static int LoadSize(int level, int brushLoad, Purity purity)
    {
        float full = brushLoad + level;
        return (int)MathF.Round(full * purity switch
        {
            Purity.Pure => 1f,
            Purity.Off => 0.85f,
            _ => 0.6f,
        });
    }

    // ---- Light ----

    // Seconds of daylight for a session. Bigger and harder subjects get more
    // time, and each level adds a little.
    public static float LightSeconds(int canvasSize, int difficulty, int level, float multiplier)
    {
        float baseSeconds = canvasSize switch
        {
            <= 20 => 90f,
            <= 28 => 140f,
            _ => 200f,
        };
        return baseSeconds * (0.85f + difficulty / 200f) * (1f + 0.03f * level) * multiplier;
    }

    public const float SparkLightBonus = 5f;
    public const int SparksForInspired = 3;

    // ---- Quality ----

    // Accuracy first, then skill: the judge's stars plus a small bonus per
    // level, plus rewards for the two fishing-style extras (catching sparks,
    // never mixing badly).
    public const float InspiredBonus = 0.25f;

    public static float Score(float stars, int level, bool inspired, bool allPure, float inspiredBonus = InspiredBonus) =>
        stars + 0.03f * level + (inspired ? inspiredBonus : 0f) + (allPure ? 0.25f : 0f);

    // A new painter can't make an iridium piece however lucky the hand; the
    // ceiling rises with level.
    public static int MaxQuality(int level) => level switch
    {
        <= 2 => Silver,
        <= 5 => Gold,
        _ => Iridium,
    };

    public static int Quality(float score, int level)
    {
        int tier = score switch
        {
            >= 4.5f => Iridium,
            >= 3.5f => Gold,
            >= 2.5f => Silver,
            _ => Normal,
        };
        return Math.Min(tier, MaxQuality(level));
    }

    // ---- Value and XP ----

    // Base sell price before Stardew's own quality multiplier (silver 1.25x,
    // gold 1.5x, iridium 2x) and profession bonuses.
    public static int Value(int canvasSize, int difficulty, int subjectPrice, int level)
    {
        float canvasBase = canvasSize switch
        {
            <= 20 => 120f,
            <= 28 => 260f,
            _ => 500f,
        };
        float subject = 1f + Math.Min(subjectPrice, 1000) / 2000f;
        return (int)MathF.Round(canvasBase * (0.7f + difficulty / 100f) * subject * (1f + 0.02f * level));
    }

    // Originals have no reference to judge against, so no quality: they sell
    // on the canvas alone.
    public static int OriginalValue(int canvasSize) => (int)MathF.Round(Value(canvasSize, 30, 0, 0) * 0.6f);

    public static int Xp(float stars, int difficulty, int canvasSize)
    {
        float canvas = canvasSize switch
        {
            <= 20 => 1f,
            <= 28 => 1.5f,
            _ => 2.2f,
        };
        return (int)MathF.Round((15f + 9f * stars) * (0.6f + difficulty / 100f) * canvas);
    }

    public const int OriginalXp = 8;
    public const int SaleXp = 4;

    // Vanilla skill curve (total XP to reach each level 1-10).
    public static readonly int[] XpCurve = { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

    public static int LevelForXp(int xp)
    {
        int level = 0;
        while (level < MaxLevel && xp >= XpCurve[level]) level++;
        return level;
    }
}
