using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Objects;

namespace ArtStudio.Painting;

internal enum FrameRarity { Common = 1, Uncommon = 2, Rare = 3, Epic = 4, Legendary = 6 }

internal enum FrameTrait { Light, Sparks, Thrift, Learning, Value, Night }

// What a frame does when it hangs (1.3, docs/design/1.3_plan.md section 5):
// a framed painting on a wall inspires paintings made at an easel in the
// same room, so you build a studio. Each style has one trait, and its
// rarity sets how strong it is (its rarity's number is how many steps of
// the trait it gives). Only the three strongest framed paintings in a room
// count.
internal static class Frames
{
    public const int Counted = 3;

    // Each step of a trait.
    public const float LightStep = 0.02f;       // more light
    public const float NightStep = 0.04f;       // more light, from 7pm
    public const float SparksStep = 0.05f;      // sparks come this much sooner
    public const float ThriftStep = 0.015f;     // the chance a cell's paint isn't used
    public const float LearningStep = 0.03f;    // more Painting XP
    public const float ValueStep = 0.02f;       // more value
    public const int NightFrom = 1900;

    public static readonly Dictionary<string, (FrameRarity Rarity, FrameTrait Trait)> Styles = new()
    {
        ["plain"] = (FrameRarity.Common, FrameTrait.Value),
        ["pine"] = (FrameRarity.Common, FrameTrait.Thrift),
        ["oak"] = (FrameRarity.Common, FrameTrait.Learning),
        ["birch"] = (FrameRarity.Common, FrameTrait.Sparks),
        ["white"] = (FrameRarity.Common, FrameTrait.Light),
        ["walnut"] = (FrameRarity.Uncommon, FrameTrait.Learning),
        ["red-lacquer"] = (FrameRarity.Uncommon, FrameTrait.Value),
        ["rope"] = (FrameRarity.Uncommon, FrameTrait.Thrift),
        ["moonlit"] = (FrameRarity.Rare, FrameTrait.Night),
        ["driftwood"] = (FrameRarity.Rare, FrameTrait.Sparks),
        ["iron"] = (FrameRarity.Rare, FrameTrait.Learning),
        ["sandstone"] = (FrameRarity.Rare, FrameTrait.Light),
        ["gold-leaf"] = (FrameRarity.Epic, FrameTrait.Value),
        ["dwarvish"] = (FrameRarity.Epic, FrameTrait.Thrift),
        ["prismatic"] = (FrameRarity.Legendary, FrameTrait.Sparks),
        ["qi"] = (FrameRarity.Legendary, FrameTrait.Value),
    };

    public static (FrameRarity Rarity, FrameTrait Trait) Of(FrameStyle style) => Styles[style.Id];

    // The frame style a framed painting hangs in.
    public static FrameStyle? StyleOf(Item? item) =>
        item == null ? null : ArtItems.FrameStyles.FirstOrDefault(s => item.QualifiedItemId == "(F)" + s.SmallId || item.QualifiedItemId == "(F)" + s.LargeId);

    // A trait's strength at an easel: the steps it gets from the room.
    public sealed class Studio
    {
        public readonly Dictionary<FrameTrait, int> Steps = new();
        public int Get(FrameTrait trait) => Steps.TryGetValue(trait, out int n) ? n : 0;
        public bool Any => Steps.Values.Any(n => n > 0);

        public float Light(int timeOfDay) =>
            1f + Get(FrameTrait.Light) * LightStep + (timeOfDay >= NightFrom ? Get(FrameTrait.Night) * NightStep : 0f);
        public float SparkInterval => 1f / (1f + Get(FrameTrait.Sparks) * SparksStep);
        public float ThriftChance => Get(FrameTrait.Thrift) * ThriftStep;
        public float Learning => 1f + Get(FrameTrait.Learning) * LearningStep;
        public float Value => 1f + Get(FrameTrait.Value) * ValueStep;
    }

    public static readonly Studio None = new();

    // The room's framed paintings: the three strongest, by rarity.
    public static Studio For(GameLocation? location)
    {
        var studio = new Studio();
        if (location == null) return studio;
        foreach (var (rarity, trait) in location.furniture.Select(StyleOf).Where(s => s != null).Select(s => Of(s!))
                     .OrderByDescending(f => (int)f.Rarity).Take(Counted))
            studio.Steps[trait] = studio.Get(trait) + (int)rarity;
        return studio;
    }
}
