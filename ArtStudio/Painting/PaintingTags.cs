using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Context tags for paintings (1.3), so content (special orders above all)
// can ask for a kind of painting: every painting is the same item, and a
// special order matches what you bring only by its tags. They're worked out
// from the painting's own data:
//   muse_painting                 any painting
//   muse_subject_portrait         a villager's portrait, and muse_sitter_<name>
//   muse_subject_view             a view, and muse_view_<place> (muse_night, muse_rain)
//   muse_subject_still_life       a still life; muse_subject_food if it has
//                                 a dish or a drink in it, muse_legendary_fish
//                                 if a legendary fish
//   muse_subject_creature / muse_subject_animal
//   muse_masterpiece
// (The game's own quality_silver/gold/iridium tags already follow quality.)
internal static class PaintingTags
{
    // Drinks that aren't cooking: wine, beer, juice, pale ale, mead, coffee, green tea.
    private static readonly HashSet<string> Drinks = new() { "(O)348", "(O)346", "(O)350", "(O)303", "(O)459", "(O)395", "(O)614" };

    // The five legends, as the portfolio's Legends page has them.
    private static readonly HashSet<string> LegendaryFish = new() { "(O)159", "(O)160", "(O)163", "(O)775", "(O)682" };

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(SObject), "_PopulateContextTags"),
            postfix: new HarmonyMethod(typeof(PaintingTags), nameof(PopulatePostfix)));
    }

    private static void PopulatePostfix(SObject __instance, HashSet<string> tags)
    {
        try
        {
            if (PaintingItems.IsPainting(__instance) && PaintingData.Read(__instance) is { } data)
                tags.UnionWith(For(data));
        }
        catch (Exception)
        {
            // (Without our tags, then.)
        }
    }

    public static IEnumerable<string> For(PaintingData data)
    {
        yield return "muse_painting";
        if (data.Masterpiece) yield return "muse_masterpiece";
        if (data.IsSitter)
        {
            yield return "muse_subject_portrait";
            yield return "muse_sitter_" + Sitters.NpcOf(data.Subject).ToLowerInvariant();
        }
        else if (data.IsView)
        {
            yield return "muse_subject_view";
            yield return "muse_view_" + Views.PlaceOf(data.Subject).ToLowerInvariant();
            if (data.Conditions != null) yield return "muse_" + data.Conditions;
        }
        else if (data.IsCreature)
            yield return "muse_subject_creature";
        else if (data.IsAnimal)
            yield return "muse_subject_animal";
        else if (data.SubjectItems.Length > 0)
        {
            yield return "muse_subject_still_life";
            if (data.SubjectItems.Any(IsFoodOrDrink)) yield return "muse_subject_food";
            if (data.SubjectItems.Any(LegendaryFish.Contains)) yield return "muse_legendary_fish";
        }
    }

    private static bool IsFoodOrDrink(string qualifiedId) =>
        Drinks.Contains(qualifiedId)
        || qualifiedId.StartsWith("(O)") && Game1.objectData.TryGetValue(qualifiedId.Substring(3), out var item) && item.Category == SObject.CookingCategory;
}
