using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.GameData.Objects;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// The painting item: one vanilla object type whose instances each carry
// their own pixels (PaintingData), quality, price and name.
internal static class PaintingItems
{
    public const string PaintingId = ModEntry.Id + "_Painting";
    public const string QualifiedPaintingId = "(O)" + PaintingId;
    public const string ContextTag = "muse_painting";
    public const string ItemsTexture = "Mods/" + ModEntry.Id + "/Items";

    public static bool IsPainting(Item? item) => item?.QualifiedItemId == QualifiedPaintingId;

    public static void AddObjectData(IDictionary<string, ObjectData> objects)
    {
        objects[PaintingId] = new ObjectData
        {
            Name = PaintingId,
            DisplayName = "Painting",
            Description = "A painting, made by hand at an easel.",
            Type = "Basic",
            Category = 0,
            Price = 100,
            Texture = ItemsTexture,
            SpriteIndex = 0,
            ExcludeFromRandomSale = true,
            ExcludeFromShippingCollection = true,
            ContextTags = new List<string> { ContextTag },
        };
    }

    public static SObject Create(PaintingData data, int quality, int price)
    {
        var item = ItemRegistry.Create<SObject>(QualifiedPaintingId);
        item.Quality = quality;
        item.Price = price;
        data.WriteTo(item);

        // The name is a vanilla tokenizable format: "Still Life: Parsnip"
        // follows the subject's own (translated) name, the way "Blueberry
        // Wine" follows the blueberry's.
        var subjects = data.SubjectItems;
        if (data.IsPortrait)
            item.displayNameFormat = Title(data);
        else if (subjects.Length == 1)
        {
            item.preservedParentSheetIndex.Value = subjects[0];
            item.displayNameFormat = "Still Life: %PRESERVED_DISPLAY_NAME";
        }
        else if (subjects.Length > 1)
        {
            item.preservedParentSheetIndex.Value = subjects[0];
            item.displayNameFormat = "Still Life with %PRESERVED_DISPLAY_NAME";
        }
        else
            item.displayNameFormat = "Original Painting";
        return item;
    }

    // "Still Life: Parsnip", "Still Life with Melon", "Original Painting",
    // "Portrait of Tester".
    public static string Title(PaintingData data)
    {
        if (data.IsPortrait) return "Portrait of " + FarmerPortrait.SitterName(data.Subject);
        var subjects = data.SubjectItems;
        if (subjects.Length == 0) return "Original Painting";
        string first = ItemRegistry.GetDataOrErrorItem(subjects[0]).DisplayName;
        return subjects.Length == 1 ? $"Still Life: {first}" : $"Still Life with {first}";
    }

    // Shown under the item's name: who painted it, when, and of what.
    public static string Provenance(PaintingData data)
    {
        string subject = data.IsPortrait
            ? $"A portrait of {FarmerPortrait.SitterName(data.Subject)}."
            : data.IsOriginal
            ? "An original work."
            : "Painted from life: " + string.Join(", ", data.SubjectItems.Select(id => ItemRegistry.GetDataOrErrorItem(id).DisplayName)) + ".";
        var signature = new[] { string.IsNullOrEmpty(data.Painter) ? null : "By " + data.Painter, data.Date }
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        return signature.Count == 0 ? subject : $"{subject}\n{string.Join(", ", signature)}.";
    }
}
