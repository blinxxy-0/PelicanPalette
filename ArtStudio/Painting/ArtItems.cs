using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Everything the painter uses besides the paintings themselves: the easel,
// canvases, brushes, picture frames and pigment jars, their crafting
// recipes, and the framed-painting furniture. All added to the game's data
// assets so they behave like vanilla items.
internal static class ArtItems
{
    public const string EaselId = ModEntry.Id + "_Easel";
    public const string QualifiedEaselId = "(BC)" + EaselId;
    public const string FrameItemId = ModEntry.Id + "_PictureFrame";
    public const string FramedSmallId = ModEntry.Id + "_FramedPainting";
    public const string FramedLargeId = ModEntry.Id + "_FramedPaintingLarge";

    public const string BigCraftablesTexture = "Mods/" + ModEntry.Id + "/BigCraftables";
    public const string FurnitureTexture = "Mods/" + ModEntry.Id + "/Furniture";

    // Sprite indices in the items sheet (16 columns of 16×16).
    private const int CanvasSprite = 1, BrushSprite = 4, FrameSprite = 8, PigmentSprite = 16;

    // Crafting recipes unlock with the Painting skill (SpaceCore reads the
    // "<skill id> <level>" condition and teaches them at the start of a day).
    private static string AtLevel(int level) => $"{PaintingSkill.SkillId} {level}";

    public static string CanvasId(CanvasKind kind) => $"{ModEntry.Id}_Canvas_{kind}";
    public static string QualifiedCanvasId(CanvasKind kind) => "(O)" + CanvasId(kind);
    public static string BrushId(BrushSpec brush) => $"{ModEntry.Id}_Brush_{brush.Id}";
    public static string PigmentId(int pigment) => $"{ModEntry.Id}_Pigment_{Palette.All[pigment].Id}";

    public static bool IsOurs(Item? item) => item?.ItemId.StartsWith(ModEntry.Id + "_") == true;
    public static bool IsEasel(Item? item) => item?.QualifiedItemId == QualifiedEaselId;
    public static bool IsFramed(Item? item) => item?.QualifiedItemId is "(F)" + FramedSmallId or "(F)" + FramedLargeId;

    public static BrushSpec? BrushFor(Item? item) =>
        item == null ? null : Brushes.All.FirstOrDefault(b => item.QualifiedItemId == "(O)" + BrushId(b));

    public static int? PigmentFor(Item? item)
    {
        if (item == null) return null;
        for (int i = 0; i < Palette.All.Length; i++)
            if (item.QualifiedItemId == "(O)" + PigmentId(i))
                return i;
        return null;
    }

    public static void AddObjects(IDictionary<string, ObjectData> objects)
    {
        AddObject(objects, CanvasId(CanvasKind.Small), "Small Canvas", "A stretched canvas, 20 cells square. Paint on it at an easel.", 15, CanvasSprite);
        AddObject(objects, CanvasId(CanvasKind.Medium), "Medium Canvas", "A stretched canvas, 28 cells square: room for two things side by side.", 50, CanvasSprite + 1);
        AddObject(objects, CanvasId(CanvasKind.Large), "Large Canvas", "A stretched canvas, 36 cells square: room for a whole arrangement.", 120, CanvasSprite + 2);

        var brushDescriptions = new Dictionary<string, string>
        {
            ["flat"] = "A wide, flat brush. Each mix covers far more of the canvas.",
            ["sable"] = "A soft round brush that makes mixing more forgiving.",
            ["rigger"] = "A long, fine brush. Hues hold steadier, and inspiration lingers.",
            ["iridium"] = "A brush that seems to know where the paint should go.",
        };
        var brushPrices = new Dictionary<string, int> { ["flat"] = 150, ["sable"] = 350, ["rigger"] = 450, ["iridium"] = 1500 };
        int sprite = BrushSprite;
        foreach (var brush in Brushes.All.Where(b => b != Brushes.HogBristle))
            AddObject(objects, BrushId(brush), brush.Name, brushDescriptions[brush.Id], brushPrices[brush.Id], sprite++, "muse_brush");

        AddObject(objects, FrameItemId, "Picture Frame", "Frame one of your paintings at an easel to hang it on a wall.", 60, FrameSprite);

        for (int i = 0; i < Palette.All.Length; i++)
        {
            var p = Palette.All[i];
            if (p.Starter) continue;
            AddObject(objects, PigmentId(i), $"{p.Name} Pigment",
                $"A jar of ground {p.Name.ToLowerInvariant()} pigment. Your easel adds it to your palette for good.",
                p.Rare ? 250 : 40, PigmentSprite + i, "muse_pigment");
        }
    }

    private static void AddObject(IDictionary<string, ObjectData> objects, string id, string name, string description, int price, int sprite, string? tag = null)
    {
        objects[id] = new ObjectData
        {
            Name = id,
            DisplayName = name,
            Description = description,
            Type = "Crafting",
            Category = SObject.CraftingCategory,
            Price = price,
            Texture = PaintingItems.ItemsTexture,
            SpriteIndex = sprite,
            ExcludeFromRandomSale = true,
            ContextTags = tag == null ? new List<string>() : new List<string> { tag },
        };
    }

    public static void AddBigCraftables(IDictionary<string, BigCraftableData> bigCraftables)
    {
        bigCraftables[EaselId] = new BigCraftableData
        {
            Name = EaselId,
            DisplayName = "Easel",
            Description = "Paint still lifes of anything you carry. Bring a canvas.",
            Price = 100,
            Texture = BigCraftablesTexture,
            SpriteIndex = 0,
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
        };
    }

    // Name/type/tilesheet size/bounding box/rotations/price/placement/display name/sprite/texture/off-limits/tags
    public static void AddFurniture(IDictionary<string, string> furniture)
    {
        // Furniture data is '/'-delimited, so the texture path uses backslashes.
        string texture = FurnitureTexture.Replace('/', '\\');
        furniture[FramedSmallId] = $"{FramedSmallId}/painting/2 2/2 2/1/250/-1/Framed Painting/0/{texture}/true";
        furniture[FramedLargeId] = $"{FramedLargeId}/painting/3 3/3 3/1/500/-1/Framed Painting/2/{texture}/true";
    }

    // ingredients/unused/yield/big craftable/unlock condition/display name
    public static void AddRecipes(IDictionary<string, string> recipes)
    {
        recipes[EaselId] = $"388 30 771 10/Home/{EaselId}/true/null/Easel";
        recipes[CanvasId(CanvasKind.Small)] = $"771 5 388 2/Home/{CanvasId(CanvasKind.Small)}/false/null/Small Canvas";
        recipes[CanvasId(CanvasKind.Medium)] = $"428 1 388 5/Home/{CanvasId(CanvasKind.Medium)}/false/{AtLevel(PaintingRules.CanvasUnlockLevel(CanvasKind.Medium))}/Medium Canvas";
        recipes[CanvasId(CanvasKind.Large)] = $"428 2 709 3/Home/{CanvasId(CanvasKind.Large)}/false/{AtLevel(PaintingRules.CanvasUnlockLevel(CanvasKind.Large))}/Large Canvas";
        recipes[FrameItemId] = $"388 10 334 1/Home/{FrameItemId}/false/{AtLevel(2)}/Picture Frame";

        recipes[BrushId(Brushes.All[2])] = $"771 30 388 5/Home/{BrushId(Brushes.All[2])}/false/{AtLevel(2)}/Flat Wash Brush";
        recipes[BrushId(Brushes.All[1])] = $"446 1 709 1/Home/{BrushId(Brushes.All[1])}/false/{AtLevel(3)}/Sable Round Brush";
        recipes[BrushId(Brushes.All[3])] = $"444 1 709 1/Home/{BrushId(Brushes.All[3])}/false/{AtLevel(6)}/Rigger Brush";
        recipes[BrushId(Brushes.All[4])] = $"337 1 446 1/Home/{BrushId(Brushes.All[4])}/false/{AtLevel(9)}/Iridium Brush";

        // Pigments ground from things found around the valley.
        void Pigment(string id, string ingredients, int level)
        {
            int i = Palette.IndexOf(id);
            recipes[PigmentId(i)] = $"{ingredients}/Home/{PigmentId(i)}/false/{AtLevel(level)}/{Palette.All[i].Name} Pigment";
        }
        Pigment("sienna", "330 3", 1);      // clay
        Pigment("rose", "402 2", 1);        // sweet pea
        Pigment("marigold", "18 2", 1);     // daffodil
        Pigment("leaf", "20 2", 1);         // leek
        Pigment("snow", "80 1", 3);         // quartz
        Pigment("plum", "410 3", 3);        // blackberry
        Pigment("cerulean", "258 3", 3);    // blueberry
        Pigment("violet", "66 1", 5);       // amethyst
        Pigment("aquamarine", "62 1", 7);   // aquamarine
        Pigment("ruby", "64 1", 9);         // ruby
    }

    // Recipes every painter knows from the start.
    public static readonly string[] StarterRecipes = { EaselId, CanvasId(CanvasKind.Small) };

    // Pierre buys paintings.
    public static void AddToShops(IDictionary<string, ShopData> shops)
    {
        if (!shops.TryGetValue("SeedShop", out var pierre)) return;
        pierre.SalableItemTags ??= new List<string>();
        if (!pierre.SalableItemTags.Contains(PaintingItems.ContextTag))
            pierre.SalableItemTags.Add(PaintingItems.ContextTag);
    }

    // Villagers who like even a normal-quality painting. Content packs can
    // add to it (EditData on Mods/blinxxy.MuseArt/ArtLovers).
    public const string ArtLoversAsset = "Mods/" + ModEntry.Id + "/ArtLovers";

    public static Dictionary<string, string> DefaultArtLovers() => new()
    {
        ["Leah"] = "a sculptor and painter",
        ["Elliott"] = "a writer with an eye for beauty",
        ["Emily"] = "makes her own clothes and loves color",
        ["Haley"] = "a photographer",
        ["Abigail"] = "plays music and paints her own world",
        ["Sebastian"] = "draws in his spare time",
        ["Sam"] = "a musician",
        ["Robin"] = "a builder who appreciates good craft",
        ["Sable"] = "runs the art supply shop",
    };
}
