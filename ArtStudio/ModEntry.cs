using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;

namespace ArtStudio;

public class ModEntry : Mod
{
    public const string Id = "blinxxy.MuseArt";

    internal static ModConfig Config { get; private set; } = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        Config = helper.ReadConfig<ModConfig>();
        var harmony = new Harmony(ModManifest.UniqueID);
        PaintingPatches.Apply(harmony, Monitor);
        ArtHooks.Apply(harmony, helper, Monitor);
        Shop.ArtShop.Apply(harmony, helper, Monitor);
        Shop.Consignment.Apply(harmony, helper, Monitor);
        Varnish.Apply(harmony, helper, Monitor);
        SableEvents.Register();
        NeutralLabels.Apply(harmony, Monitor);
        SableMarriage.Apply(harmony, Monitor);

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => PaintingRenderer.ClearCache();
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            PaintingSkill.Register(helper);
            RegisterConfigMenu();
        };

        DevTools.AddCommand(helper, "museart_paint", "Opens the easel. Usage: museart_paint [small|medium|large] [item IDs...]\nWith no items, opens the setup screen; 'held' paints the held item.", PaintCommand);
        DevTools.AddCommand(helper, "museart_free", "Starts a free-canvas painting. Usage: museart_free [small|medium|large]", FreeCommand);
        DevTools.AddCommand(helper, "museart_level", "Sets your Painting level. Usage: museart_level <0-10>", LevelCommand);
        DevTools.AddCommand(helper, "museart_pigments", "Sets owned pigments. Usage: museart_pigments all|starter|<id> ...", PigmentsCommand);
        DevTools.AddCommand(helper, "museart_supplies", "Gives you an easel, canvases, a brush of each kind, picture frames, a palette and two tubes of every colour.", SuppliesCommand);
        DevTools.AddCommand(helper, "museart_tastes", "Prints how every villager would take a painting of each quality. Usage: museart_tastes [item id of the subject]", TastesCommand);
        DevTools.AddCommand(helper, "museart_difficulty", "Prints the reference difficulty for some items. Usage: museart_difficulty [item IDs...]", DifficultyCommand);
        DevTools.Register(helper, Monitor);

#if DEBUG
        // F8 opens the easel anywhere, but only in debug builds and only once
        // switched on with museart_devkeys: play-testing a debug build should
        // still mean buying an easel.
        bool devKeys = false;
        DevTools.AddCommand(helper, "museart_devkeys", "Turns the F8 open-the-easel-anywhere key on or off for this session (off by default).", (_, _) =>
        {
            devKeys = !devKeys;
            Monitor.Log($"F8 opens the easel: {(devKeys ? "on" : "off")}.", LogLevel.Info);
        });
        helper.Events.Input.ButtonPressed += (_, e) =>
        {
            if (devKeys && e.Button == SButton.F8 && Context.IsPlayerFree)
                ArtHooks.OpenEasel(Game1.player);
        };
#endif
    }

    private void RegisterConfigMenu()
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm == null) return;

        var difficulties = Enum.GetNames(typeof(Difficulty));
        gmcm.Register(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
        gmcm.AddSectionTitle(ModManifest, () => I18n.Get("config.section.easel"));
        gmcm.AddParagraph(ModManifest, () => I18n.Get("config.intro"));
        gmcm.AddTextOption(ModManifest,
            () => Config.Difficulty.ToString(),
            value => Config.Difficulty = Enum.Parse<Difficulty>(value),
            () => I18n.Get("config.difficulty"),
            () => string.Join("\n\n", difficulties.Select(d => I18n.Get("config.difficulty." + d.ToLowerInvariant()) + ": " + I18n.Get("config.difficulty." + d.ToLowerInvariant() + ".description"))),
            allowedValues: difficulties,
            formatAllowedValue: d => I18n.Get("config.difficulty." + d.ToLowerInvariant()));
        gmcm.AddNumberOption(ModManifest,
            () => Config.LightTimeMultiplier,
            value => Config.LightTimeMultiplier = value,
            () => I18n.Get("config.light"),
            () => I18n.Get("config.light.description"),
            min: 0.5f, max: 3f, interval: 0.1f,
            formatValue: v => $"{v:0.0}x");
        gmcm.AddSectionTitle(ModManifest, () => I18n.Get("config.section.gifts"));
        gmcm.AddBoolOption(ModManifest,
            () => Config.ArtLoversLikeAnyPainting,
            value => Config.ArtLoversLikeAnyPainting = value,
            () => I18n.Get("config.art-lovers"),
            () => I18n.Get("config.art-lovers.description"));
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        var name = e.NameWithoutLocale;
        if (name.IsEquivalentTo("Data/Objects"))
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, ObjectData>().Data;
                PaintingItems.AddObjectData(data);
                ArtItems.AddObjects(data);
            });
        else if (name.IsEquivalentTo("Data/BigCraftables"))
            e.Edit(asset => ArtItems.AddBigCraftables(asset.AsDictionary<string, BigCraftableData>().Data));
        else if (name.IsEquivalentTo("Data/Furniture"))
            e.Edit(asset => ArtItems.AddFurniture(asset.AsDictionary<string, string>().Data));
        else if (name.IsEquivalentTo("Data/Machines"))
            e.Edit(asset => ArtItems.AddMachines(asset.AsDictionary<string, StardewValley.GameData.Machines.MachineData>().Data));
        else if (name.IsEquivalentTo("Data/CraftingRecipes"))
            e.Edit(asset => ArtItems.AddRecipes(asset.AsDictionary<string, string>().Data));
        else if (name.IsEquivalentTo("Data/Shops"))
            e.Edit(asset => ArtItems.AddToShops(asset.AsDictionary<string, ShopData>().Data));
        else if (name.IsEquivalentTo(ArtItems.ArtLoversAsset))
            e.LoadFrom(ArtItems.DefaultArtLovers, AssetLoadPriority.Exclusive);
        else if (name.IsEquivalentTo(PaintingItems.ItemsTexture))
            e.LoadFromModFile<Texture2D>("assets/items.png", AssetLoadPriority.Exclusive);
        else if (name.IsEquivalentTo(ArtItems.BigCraftablesTexture))
            e.LoadFromModFile<Texture2D>("assets/bigcraftables.png", AssetLoadPriority.Exclusive);
        else if (name.IsEquivalentTo(ArtItems.FurnitureTexture))
            e.LoadFromModFile<Texture2D>("assets/furniture.png", AssetLoadPriority.Exclusive);
    }

    private static CanvasKind? ParseCanvas(string arg) => arg.ToLowerInvariant() switch
    {
        "small" => CanvasKind.Small,
        "medium" => CanvasKind.Medium,
        "large" => CanvasKind.Large,
        _ => null,
    };

    private bool ReadyForMenu()
    {
        if (Context.IsWorldReady && Game1.activeClickableMenu == null) return true;
        Monitor.Log("Load a save and close any open menu first.", LogLevel.Warn);
        return false;
    }

    private void PaintCommand(string name, string[] args)
    {
        if (!ReadyForMenu()) return;
        var kind = args.Length > 0 ? ParseCanvas(args[0]) : null;
        var items = args.Skip(kind.HasValue ? 1 : 0).ToList();
        if (items.Count == 0)
        {
            Game1.activeClickableMenu = new EaselMenu(Game1.player);
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == "held")
            {
                if (Game1.player.ActiveObject == null)
                {
                    Monitor.Log("You're not holding anything.", LogLevel.Warn);
                    return;
                }
                items[i] = Game1.player.ActiveObject.QualifiedItemId;
            }
            items[i] = ItemRegistry.QualifyItemId(items[i]) ?? items[i];
            var item = ItemRegistry.Create(items[i], allowNull: true);
            if (item == null || !ReferenceBuilder.CanPaint(item))
            {
                Monitor.Log($"Can't paint '{items[i]}': it needs to be an item with a 16x16 sprite.", LogLevel.Warn);
                return;
            }
        }
        var canvas = kind ?? (items.Count switch { 1 => CanvasKind.Small, 2 => CanvasKind.Medium, _ => CanvasKind.Large });
        Game1.activeClickableMenu = EaselMenu.StartStillLife(Game1.player, canvas, items.Take(ReferenceBuilder.MaxItems(PaintingRules.CanvasSize(canvas))).ToList());
    }

    private void FreeCommand(string name, string[] args)
    {
        if (!ReadyForMenu()) return;
        var kind = args.Length > 0 ? ParseCanvas(args[0]) ?? CanvasKind.Small : CanvasKind.Small;
        Game1.activeClickableMenu = EaselMenu.StartOriginal(Game1.player, kind);
    }

    private void LevelCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || args.Length == 0 || !int.TryParse(args[0], out int level))
        {
            Monitor.Log("Usage (with a save loaded): museart_level <0-10>", LogLevel.Warn);
            return;
        }
        ArtProgress.SetLevel(Game1.player, Math.Clamp(level, 0, PaintingRules.MaxLevel));
        Monitor.Log($"Painting level is now {ArtProgress.Level(Game1.player)} ({ArtProgress.Xp(Game1.player)} XP).", LogLevel.Info);
    }

    private void PigmentsCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady || args.Length == 0)
        {
            Monitor.Log("Usage (with a save loaded): museart_pigments all|starter|<id> ...", LogLevel.Warn);
            return;
        }
        var all = Enumerable.Range(0, Palette.All.Length);
        if (args[0] == "all")
            ArtProgress.SetOwned(Game1.player, all);
        else if (args[0] == "starter")
            ArtProgress.SetOwned(Game1.player, all.Where(i => Palette.All[i].Starter));
        else
            foreach (string id in args)
            {
                int i = Palette.IndexOf(id);
                if (i < 0) Monitor.Log($"No pigment '{id}'. Pigments: {string.Join(", ", Palette.All.Select(p => p.Id))}", LogLevel.Warn);
                else ArtProgress.Unlock(Game1.player, i);
            }
        Monitor.Log("You own: " + string.Join(", ", ArtProgress.OwnedPigments(Game1.player).Select(i => Palette.All[i].Name)), LogLevel.Info);
    }

    private void SuppliesCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady) return;
        var items = new List<Item>
        {
            ItemRegistry.Create(ArtItems.QualifiedEaselId),
            ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Small), 10),
            ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Medium), 5),
            ItemRegistry.Create(ArtItems.QualifiedCanvasId(CanvasKind.Large), 3),
            ItemRegistry.Create("(O)" + ArtItems.FrameItemId, 5),
        };
        items.AddRange(Brushes.All.Where(b => b != Brushes.HogBristle).Select(b => ItemRegistry.Create("(O)" + ArtItems.BrushId(b))));
        items.Add(ItemRegistry.Create("(O)" + ArtItems.PaletteId));
        items.AddRange(Enumerable.Range(0, Palette.All.Length).Select(i => ItemRegistry.Create("(O)" + ArtItems.TubeId(i), 2)));
        foreach (var item in items)
            Game1.player.addItemByMenuIfNecessary(item);
        Monitor.Log($"Gave {items.Count} painting supplies.", LogLevel.Info);
    }

    private void TastesCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady) return;
        string subject = args.Length > 0 ? ItemRegistry.QualifyItemId(args[0]) ?? args[0] : "(O)24";
        var data = new PaintingData
        {
            Size = 20,
            Cells = Core.PixelCanvas.NewBlank(20),
            Purity = new Purity[20, 20],
            Subject = subject,
        };
        string Taste(int t) => t switch { 0 => "LOVE", 2 => "like", 4 => "DISLIKE", 6 => "HATE", _ => "-" };
        Monitor.Log($"Painting of {ItemRegistry.GetDataOrErrorItem(subject).DisplayName}: normal / silver / gold / iridium", LogLevel.Info);
        foreach (var npc in Utility.getAllVillagers().OrderBy(n => n.Name))
        {
            if (!npc.CanReceiveGifts()) continue;
            var tastes = new[] { 0, 1, 2, 4 }.Select(q => Taste(npc.getGiftTasteForThisItem(PaintingItems.Create(data, q, 100))));
            Monitor.Log($"  {npc.Name,-12} {string.Join(" / ", tastes)}", LogLevel.Info);
        }
    }

    private void DifficultyCommand(string name, string[] args)
    {
        string[] ids = args.Length > 0 ? args : new[] { "24", "190", "254", "276", "128", "72", "424", "395", "766", "60" };
        var owned = Context.IsWorldReady ? ArtProgress.OwnedPigments(Game1.player) : Enumerable.Range(0, Palette.All.Length).Where(i => Palette.All[i].Starter).ToList();
        foreach (string raw in ids)
        {
            string id = ItemRegistry.QualifyItemId(raw) ?? raw;
            var item = ItemRegistry.Create(id, allowNull: true);
            if (item == null || !ReferenceBuilder.CanPaint(item))
            {
                Monitor.Log($"{raw}: can't paint", LogLevel.Info);
                continue;
            }
            var reference = ReferenceBuilder.StillLife(new[] { id }, 20, owned);
            Monitor.Log($"{item.DisplayName,-20} difficulty {reference.Difficulty,3}  pigments {reference.PigmentsUsed.Count}  cells {reference.SubjectCells}", LogLevel.Info);
        }
    }
}
