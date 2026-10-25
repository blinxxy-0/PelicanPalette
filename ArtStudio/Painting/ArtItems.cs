using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Everything the painter uses besides the paintings themselves: the easel,
// canvases, brushes, picture frames, pigment jars (which teach a colour),
// the Palette and paint tubes, the Pigment Mill, their crafting recipes,
// and the framed-painting furniture. All added to the game's data
// assets so they behave like vanilla items.
internal static class ArtItems
{
    public const string EaselId = ModEntry.Id + "_Easel";
    public const string QualifiedEaselId = "(BC)" + EaselId;
    public const string FrameItemId = ModEntry.Id + "_PictureFrame";
    public const string PaletteId = ModEntry.Id + "_Palette";
    public const string MillId = ModEntry.Id + "_PigmentMill";
    public const string QualifiedMillId = "(BC)" + MillId;
    public const string FramedSmallId = ModEntry.Id + "_FramedPainting";
    public const string FramedLargeId = ModEntry.Id + "_FramedPaintingLarge";

    public const string BigCraftablesTexture = "Mods/" + ModEntry.Id + "/BigCraftables";
    public const string FurnitureTexture = "Mods/" + ModEntry.Id + "/Furniture";

    // Sprite indices in the items sheet (16 columns of 16×16).
    private const int CanvasSprite = 1, BrushSprite = 4, FrameSprite = 8, PaletteSprite = 9, PigmentSprite = 16, TubeSprite = 48;

    // Crafting recipes unlock with the Painting skill (SpaceCore reads the
    // "<skill id> <level>" condition and teaches them at the start of a day).
    private static string AtLevel(int level) => $"{PaintingSkill.SkillId} {level}";

    public static string CanvasId(CanvasKind kind) => $"{ModEntry.Id}_Canvas_{kind}";
    public static string QualifiedCanvasId(CanvasKind kind) => "(O)" + CanvasId(kind);
    public static string BrushId(BrushSpec brush) => $"{ModEntry.Id}_Brush_{brush.Id}";
    public static string TubeId(int pigment) => $"{ModEntry.Id}_Tube_{Palette.All[pigment].Id}";

    public static bool IsPalette(Item? item) => item?.QualifiedItemId == "(O)" + PaletteId;

    public static int? TubeFor(Item? item)
    {
        if (item == null) return null;
        for (int i = 0; i < Palette.All.Length; i++)
            if (item.QualifiedItemId == "(O)" + TubeId(i))
                return i;
        return null;
    }

    // What a tube costs at Sable's (docs/painting_depth.md: a painting's paint
    // should cost at most about 15% of its normal-quality price). The game's
    // sell price is a quarter of that, so milling isn't a way to print money.
    public static int TubePrice(int pigment)
    {
        var p = Palette.All[pigment];
        return p.Rare ? 60 : p.Starter ? 8 : p.Id is "snow" or "plum" or "cerulean" ? 20 : 12;
    }

    // What the Pigment Mill grinds into each colour: (item, how many, tubes out).
    public static readonly (string Pigment, string Input, int Count, int Tubes)[] MillRecipes =
    {
        ("charcoal", "(O)382", 1, 2),       // coal
        ("umber", "(O)709", 1, 2),          // hardwood
        ("sienna", "(O)330", 1, 2),         // clay
        ("ochre", "(O)380", 1, 2),          // iron ore
        ("cream", "(O)372", 1, 2),          // clam shell
        ("stone", "(O)390", 5, 2),          // stone
        ("crimson", "(O)420", 1, 2),        // red mushroom
        ("crimson", "(O)260", 1, 2),        // hot pepper
        ("rose", "(O)402", 1, 2),           // sweet pea
        ("marigold", "(O)18", 1, 2),        // daffodil
        ("moss", "(O)771", 5, 2),           // fiber
        ("leaf", "(O)20", 1, 2),            // leek
        ("slate", "(O)84", 1, 3),           // frozen tear
        ("cerulean", "(O)258", 1, 2),       // blueberry
        ("plum", "(O)410", 1, 2),           // blackberry
        ("snow", "(O)80", 1, 3),            // quartz
        ("violet", "(O)66", 1, 3),          // amethyst
        ("aquamarine", "(O)62", 1, 3),      // aquamarine
        ("ruby", "(O)64", 1, 3),            // ruby
        ("peach", "(O)634", 1, 2),          // apricot
    };
    public const int MillMinutes = 600;
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
        AddObject(objects, CanvasId(CanvasKind.Small), I18n.Get("item.canvas.small"), I18n.Get("item.canvas.small.description"), 15, CanvasSprite);
        AddObject(objects, CanvasId(CanvasKind.Medium), I18n.Get("item.canvas.medium"), I18n.Get("item.canvas.medium.description"), 50, CanvasSprite + 1);
        AddObject(objects, CanvasId(CanvasKind.Large), I18n.Get("item.canvas.large"), I18n.Get("item.canvas.large.description"), 120, CanvasSprite + 2);

        var brushPrices = new Dictionary<string, int> { ["flat"] = 150, ["sable"] = 350, ["rigger"] = 450, ["iridium"] = 1500 };
        int sprite = BrushSprite;
        foreach (var brush in Brushes.All.Where(b => b != Brushes.HogBristle))
            AddObject(objects, BrushId(brush), I18n.Get("brush." + brush.Id), I18n.Get("brush." + brush.Id + ".description"), brushPrices[brush.Id], sprite++, "muse_brush");

        AddObject(objects, FrameItemId, I18n.Get("item.frame"), I18n.Get("item.frame.description"), 60, FrameSprite);
        AddObject(objects, PaletteId, I18n.Get("item.palette"), I18n.Get("item.palette.description"), 40, PaletteSprite);
        for (int i = 0; i < Palette.All.Length; i++)
            AddObject(objects, TubeId(i), I18n.Get("item.tube", new { pigment = I18n.Pigment(i) }),
                I18n.Get("item.tube.description", new { pigment = I18n.Pigment(i).ToLowerInvariant(), cells = PaletteItem.TubeCells }),
                System.Math.Max(2, TubePrice(i) / 4), TubeSprite + i, "muse_paint_tube");

        for (int i = 0; i < Palette.All.Length; i++)
        {
            var p = Palette.All[i];
            if (p.Starter) continue;
            AddObject(objects, PigmentId(i), I18n.Get("item.pigment", new { pigment = I18n.Pigment(i) }),
                I18n.Get("item.pigment.description", new { pigment = I18n.Pigment(i).ToLowerInvariant() }),
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
            DisplayName = I18n.Get("item.easel"),
            Description = I18n.Get("item.easel.description"),
            Price = 100,
            Texture = BigCraftablesTexture,
            SpriteIndex = 0,
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
        };
        bigCraftables[MillId] = new BigCraftableData
        {
            Name = MillId,
            DisplayName = I18n.Get("item.mill"),
            Description = I18n.Get("item.mill.description"),
            Price = 500,
            Texture = BigCraftablesTexture,
            SpriteIndex = 1,
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
        };
    }

    // The Pigment Mill, an artisan machine like the Preserves Jar: put in a
    // material, and the next morning there are tubes of paint. Milling a
    // colour you don't know yet teaches it (the tube is learned on pickup).
    public static void AddMachines(IDictionary<string, MachineData> machines)
    {
        var rules = MillRecipes.Select((r, n) => new MachineOutputRule
        {
            Id = $"{r.Pigment}_{n}",
            Triggers = new List<MachineOutputTriggerRule>
            {
                new() { Id = "ItemPlaced", Trigger = MachineOutputTrigger.ItemPlacedInMachine, RequiredItemId = r.Input, RequiredCount = r.Count },
            },
            OutputItem = new List<MachineItemOutput>
            {
                new() { Id = "Tubes", ItemId = "(O)" + TubeId(Palette.IndexOf(r.Pigment)), MinStack = r.Tubes, MaxStack = r.Tubes },
            },
            MinutesUntilReady = MillMinutes,
        }).ToList();
        machines[QualifiedMillId] = new MachineData
        {
            OutputRules = rules,
            InvalidItemMessage = I18n.Get("mill.invalid"),
            LoadEffects = new List<MachineEffects> { new() { Id = "Load", Sounds = new List<MachineSoundData> { new() { Id = "Ship" } } } },
        };
    }

    // Name/type/tilesheet size/bounding box/rotations/price/placement/display name/sprite/texture/off-limits/tags
    public static void AddFurniture(IDictionary<string, string> furniture)
    {
        // Furniture data is '/'-delimited, so the texture path uses backslashes.
        string texture = FurnitureTexture.Replace('/', '\\');
        furniture[FramedSmallId] = $"{FramedSmallId}/painting/2 2/2 2/1/250/-1/{I18n.Get("item.framed")}/0/{texture}/true";
        furniture[FramedLargeId] = $"{FramedLargeId}/painting/3 3/3 3/1/500/-1/{I18n.Get("item.framed")}/2/{texture}/true";
    }

    // ingredients/unused/yield/big craftable/unlock condition/display name
    public static void AddRecipes(IDictionary<string, string> recipes)
    {
        recipes[EaselId] = $"388 30 771 10/Home/{EaselId}/true/null/{I18n.Get("item.easel")}";
        recipes[CanvasId(CanvasKind.Small)] = $"771 5 388 2/Home/{CanvasId(CanvasKind.Small)}/false/null/{I18n.Get("item.canvas.small")}";
        recipes[CanvasId(CanvasKind.Medium)] = $"428 1 388 5/Home/{CanvasId(CanvasKind.Medium)}/false/{AtLevel(PaintingRules.CanvasUnlockLevel(CanvasKind.Medium))}/{I18n.Get("item.canvas.medium")}";
        recipes[CanvasId(CanvasKind.Large)] = $"428 2 709 3/Home/{CanvasId(CanvasKind.Large)}/false/{AtLevel(PaintingRules.CanvasUnlockLevel(CanvasKind.Large))}/{I18n.Get("item.canvas.large")}";
        recipes[FrameItemId] = $"388 10 334 1/Home/{FrameItemId}/false/{AtLevel(2)}/{I18n.Get("item.frame")}";

        recipes[BrushId(Brushes.All[2])] = $"771 30 388 5/Home/{BrushId(Brushes.All[2])}/false/{AtLevel(2)}/{I18n.Get("brush.flat")}";
        recipes[BrushId(Brushes.All[1])] = $"446 1 709 1/Home/{BrushId(Brushes.All[1])}/false/{AtLevel(3)}/{I18n.Get("brush.sable")}";
        recipes[BrushId(Brushes.All[3])] = $"444 1 709 1/Home/{BrushId(Brushes.All[3])}/false/{AtLevel(6)}/{I18n.Get("brush.rigger")}";
        recipes[BrushId(Brushes.All[4])] = $"337 1 446 1/Home/{BrushId(Brushes.All[4])}/false/{AtLevel(9)}/{I18n.Get("brush.iridium")}";

        // The Palette (known from the start) and the Pigment Mill. Pigments
        // themselves aren't crafted any more: the Mill grinds paint tubes,
        // and a tube of a new colour teaches it.
        recipes[PaletteId] = $"388 10/Home/{PaletteId}/false/null/{I18n.Get("item.palette")}";
        recipes[MillId] = $"390 30 334 2 388 10/Home/{MillId}/true/{AtLevel(1)}/{I18n.Get("item.mill")}";
    }

    // Recipes every painter knows from the start.
    public static readonly string[] StarterRecipes = { EaselId, CanvasId(CanvasKind.Small), PaletteId };

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
