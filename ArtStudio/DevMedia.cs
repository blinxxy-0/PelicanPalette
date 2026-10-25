#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace ArtStudio;

// Material for the mod page and the wiki (2026-10-25), all written to the
// mod's media/ folder:
//   museart_frame <name> [hud]  this dev copy's next rendered frame (its own
//                               window's back buffer, nothing else on screen;
//                               at zoom 1, where the world fills it unscaled)
//   museart_signtext <text>     a heading in the game's own bold pixel font,
//                               on transparency, for the page's wooden signs
//   museart_wikidata            the mod's items, recipes, stock, pigments,
//                               brushes, skill and numbers as JSON, with an
//                               icon for every item they mention
internal static class DevMedia
{
    private static IModHelper _helper = null!;
    private static IMonitor _monitor = null!;
    private static string? _pendingFrame;
    private static bool _hudWas;

    private static string Dir
    {
        get
        {
            string dir = Path.Combine(_helper.DirectoryPath, "media");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        _monitor = monitor;
        DevTools.AddCommand(helper, "museart_frame", "Saves this game's next frame to media/<name>.png. Usage: museart_frame <name> [hud]", (_, args) =>
        {
            _pendingFrame = args.FirstOrDefault() ?? "frame";
            _hudWas = Game1.displayHUD;
            if (args.ElementAtOrDefault(1) != "hud")
                Game1.displayHUD = false;
        });
        DevTools.AddCommand(helper, "museart_signtext", "Renders a heading in the game's bold font to media/sign_<slug>.png. Usage: museart_signtext Features", (_, args) => SignText(string.Join(" ", args)));
        DevTools.AddCommand(helper, "museart_wikidata", "Writes media/wikidata.json and media/icons/ for the wiki.", (_, _) => WikiData());
        // Staging a shot.
        DevTools.AddCommand(helper, "museart_zoom", "Sets the game's zoom. Usage: museart_zoom 1.5", (_, args) =>
            Game1.options.desiredBaseZoomLevel = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture));
        DevTools.AddCommand(helper, "museart_time", "Sets the clock. Usage: museart_time 1730", (_, args) => Game1.timeOfDay = int.Parse(args[0]));
        DevTools.AddCommand(helper, "museart_farmer", "Shows or hides the farmer. Usage: museart_farmer on|off", (_, args) => Game1.displayFarmer = args.FirstOrDefault() != "off");
        DevTools.AddCommand(helper, "museart_place", "Puts a villager somewhere, facing a way (0 up, 1 right, 2 down, 3 left). Usage: museart_place Sable Forest 84 35 2", (_, args) =>
        {
            var npc = Game1.getCharacterFromName(args[0]);
            if (npc == null) return;
            Game1.warpCharacter(npc, args[1], new Vector2(int.Parse(args[2]), int.Parse(args[3])));
            npc.faceDirection(args.Length > 4 ? int.Parse(args[4]) : 2);
            npc.controller = null;
            npc.Halt();
        });
        DevTools.AddCommand(helper, "museart_give", "Gives the farmer an item, for staging shots. Usage: museart_give <qualified id> [count], or museart_give tube <pigment index> [count]", (_, args) =>
        {
            string id = args[0] == "tube" ? "(O)" + ArtItems.TubeId(int.Parse(args[1])) : args[0];
            int count = int.Parse(args.Skip(args[0] == "tube" ? 2 : 1).FirstOrDefault() ?? "1");
            // A full bag gives up its last slot.
            var item = ItemRegistry.Create(id, count);
            if (!Game1.player.addItemToInventoryBool(item))
                Game1.player.Items[Game1.player.Items.Count - 1] = item;
        });
        DevTools.AddCommand(helper, "museart_stand", "Puts the farmer (and so the camera) on a tile, wherever it is, without the game nudging them aside. Usage: museart_stand 82 32", (_, args) =>
            Game1.player.Position = new Vector2(int.Parse(args[0]), int.Parse(args[1])) * 64f);
        DevTools.AddCommand(helper, "museart_seen", "Marks events as already seen. Usage: museart_seen blinxxy.MuseArt_SableIntro", (_, args) =>
        {
            foreach (string id in args)
                Game1.player.eventsSeen.Add(id);
        });
        DevTools.AddCommand(helper, "museart_scenario", "Opens one of the easel snapshot scenarios in the game. Usage: museart_scenario <name> [level]", (_, args) =>
        {
            var match = DevTools.Scenarios(args.Length > 1 ? int.Parse(args[1]) : 8).FirstOrDefault(s => s.Name == args[0]);
            if (match.Make != null)
                Game1.activeClickableMenu = match.Make();
        });
        helper.Events.Display.Rendered += OnRendered;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private static void OnRendered(object? sender, RenderedEventArgs e)
    {
        if (_pendingFrame == null) return;
        var device = Game1.graphics.GraphicsDevice;
        int w = device.PresentationParameters.BackBufferWidth, h = device.PresentationParameters.BackBufferHeight;
        var data = new Color[w * h];
        device.GetBackBufferData(data);
        // OpenGL hands the rows back bottom first.
        var rows = new Color[w * h];
        for (int y = 0; y < h; y++)
            Array.Copy(data, (h - 1 - y) * w, rows, y * w, w);
        // The open menu is drawn after this point in the frame, so it's laid
        // over the world on the next update, outside the draw.
        _capturedWorld = (rows, w, h, _pendingFrame);
        _pendingFrame = null;
        Game1.displayHUD = _hudWas;
    }

    private static (Color[] Pixels, int W, int H, string Name)? _capturedWorld;

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (_capturedWorld is not { } shot) return;
        _capturedWorld = null;
        var device = Game1.graphics.GraphicsDevice;
        var pixels = shot.Pixels;
        if (Game1.activeClickableMenu is { } menu)
        {
            using var target = new RenderTarget2D(device, shot.W, shot.H);
            var previous = device.GetRenderTargets();
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            float cursor = Game1.mouseCursorTransparency;
            Game1.mouseCursorTransparency = 0f;          // no pointer in the shot
            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            menu.draw(Game1.spriteBatch);
            Game1.spriteBatch.End();
            Game1.mouseCursorTransparency = cursor;
            device.SetRenderTargets(previous);
            var ui = new Color[shot.W * shot.H];
            target.GetData(ui);
            for (int i = 0; i < pixels.Length; i++)
            {
                float a = ui[i].A / 255f;
                if (a <= 0f) continue;
                // The menu was drawn premultiplied: colour + (1 - alpha) * world.
                pixels[i] = new Color(
                    (byte)Math.Min(255, ui[i].R + pixels[i].R * (1 - a)),
                    (byte)Math.Min(255, ui[i].G + pixels[i].G * (1 - a)),
                    (byte)Math.Min(255, ui[i].B + pixels[i].B * (1 - a)));
            }
        }
        using var texture = new Texture2D(device, shot.W, shot.H);
        texture.SetData(pixels);
        using var stream = File.Create(Path.Combine(Dir, shot.Name + ".png"));
        texture.SaveAsPng(stream, shot.W, shot.H);
        _monitor.Log($"Frame saved: media/{shot.Name}.png ({shot.W}x{shot.H}).", LogLevel.Info);
    }

    private static void SignText(string text)
    {
        var device = Game1.graphics.GraphicsDevice;
        int w = SpriteText.getWidthOfString(text) + 16, h = SpriteText.getHeightOfString(text) + 16;
        using var target = new RenderTarget2D(device, w, h);
        var previous = device.GetRenderTargets();
        device.SetRenderTarget(target);
        device.Clear(Color.Transparent);
        var batch = Game1.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        SpriteText.drawString(batch, text, 8, 8, color: Color.White);
        batch.End();
        device.SetRenderTargets(previous);
        string slug = new string(text.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        using var stream = File.Create(Path.Combine(Dir, "sign_" + slug + ".png"));
        target.SaveAsPng(stream, w, h);
        _monitor.Log($"Sign text saved: media/sign_{slug}.png.", LogLevel.Info);
    }

    // ---- The wiki's data ----

    private static readonly HashSet<string> Icons = new();

    private static string Name(string qualifiedId)
    {
        Icons.Add(qualifiedId);
        return ItemRegistry.GetDataOrErrorItem(qualifiedId).DisplayName;
    }

    private static object Item(string qualifiedId)
    {
        var data = ItemRegistry.GetDataOrErrorItem(qualifiedId);
        Icons.Add(qualifiedId);
        var item = ItemRegistry.Create(qualifiedId);
        return new { id = qualifiedId, name = data.DisplayName, description = data.Description, sellPrice = item.sellToStorePrice() };
    }

    private static List<object> Ingredients(string raw)
    {
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var list = new List<object>();
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            string id = parts[i].StartsWith("(") ? parts[i] : "(O)" + parts[i];
            list.Add(new { id, name = Name(id), count = int.Parse(parts[i + 1]) });
        }
        return list;
    }

    private static void WikiData()
    {
        if (!Context.IsWorldReady)
        {
            _monitor.Log("Load a game first (museart_devgame).", LogLevel.Warn);
            return;
        }
        Icons.Clear();
        string prefix = ModEntry.Id + "_";

        var objects = Game1.objectData.Where(kv => kv.Key.StartsWith(prefix)).Select(kv => Item("(O)" + kv.Key)).ToList();
        var bigCraftables = Game1.bigCraftableData.Where(kv => kv.Key.StartsWith(prefix)).Select(kv => Item("(BC)" + kv.Key)).ToList();

        var recipes = CraftingRecipe.craftingRecipes.Where(kv => kv.Key.StartsWith(prefix)).Select(kv =>
        {
            var f = kv.Value.Split('/');
            var yield = f[2].Split(' ');
            bool big = f[3] == "true";
            string id = (big ? "(BC)" : "(O)") + yield[0];
            string condition = f[4];
            int? level = condition.StartsWith(PaintingSkill.SkillId) ? int.Parse(condition.Split(' ')[1]) : null;
            return new { item = Name(id), id, makes = yield.Length > 1 ? int.Parse(yield[1]) : 1, ingredients = Ingredients(f[0]), level, known = ArtItems.StarterRecipes.Contains(kv.Key) };
        }).ToList();

        var shop = DataLoader.Shops(Game1.content)[Shop.ArtShop.ShopId].Items.Select(i => new
        {
            item = Name(i.ItemId),
            id = i.ItemId,
            price = i.Price,
            condition = i.Condition,
            oncePerPlayer = i.AvailableStock == 1,
        }).ToList();

        var pigments = Palette.All.Select((p, i) => new
        {
            id = p.Id,
            name = I18n.Pigment(i),
            hex = $"#{p.Rgb:X6}",
            difficulty = p.Difficulty,
            motion = p.Motion.ToString(),
            starter = p.Starter,
            rare = p.Rare,
            tubePrice = ArtItems.TubePrice(i),
            mill = ArtItems.MillRecipes.Where(r => r.Pigment == p.Id).Select(r => new { input = Name(r.Input), id = r.Input, count = r.Count, tubes = r.Tubes }).ToList(),
            tube = "(O)" + ArtItems.TubeId(i),
        }).ToList();
        foreach (var p in pigments) Icons.Add(p.tube);

        var brushes = Brushes.All.Select(b => new
        {
            id = b.Id,
            name = I18n.Get("brush." + b.Id),
            description = I18n.Get("brush." + b.Id + ".description"),
            load = b.Load, band = b.BandBonus, steadiness = b.Steadiness, sparkTime = b.SparkTime, uses = b.Durability,
            item = b == Brushes.HogBristle ? null : "(O)" + ArtItems.BrushId(b),
        }).ToList();

        var skill = PaintingSkill.Instance;
        var levels = Enumerable.Range(1, 10).Select(level => new
        {
            level,
            xp = PaintingRules.XpCurve[level - 1],
            unlocks = skill.GetExtraLevelUpInfo(level),
            maxQuality = Menus.Ui.QualityName(PaintingRules.MaxQuality(level)),
        }).ToList();
        var professions = new[] { skill.PleinAir, skill.Gallerist, skill.SteadyHand, skill.Visionary, skill.PatronsFavorite, skill.Prolific }
            .Select(p => new { name = p.GetName(), description = p.GetDescription() }).ToList();

        var tastes = DataLoader.NpcGiftTastes(Game1.content).TryGetValue("Sable", out string raw) ? raw.Split('/') : Array.Empty<string>();
        object Taste(int field) => field < tastes.Length
            ? tastes[field].Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !t.StartsWith("-"))
                .Select(t => t.StartsWith("(") ? t : "(O)" + t).Select(id => (object)new { id, name = Name(id) }).ToList()
            : new List<object>();
        var sable = Game1.characterData.TryGetValue("Sable", out var sd) ? sd : null;

        var data = new
        {
            version = _helper.ModRegistry.Get(ModEntry.Id)!.Manifest.Version.ToString(),
            objects,
            bigCraftables,
            recipes,
            shop,
            pigments,
            brushes,
            levels,
            professions,
            canvases = Enum.GetValues<CanvasKind>().Select(k => new { kind = k.ToString(), size = PaintingRules.CanvasSize(k), minutes = PaintingRules.Minutes(k), level = PaintingRules.CanvasUnlockLevel(k), techniques = PaintingRules.TechniquesFor(PaintingRules.CanvasSize(k), false).ToString() }),
            quality = new { silver = 2.5, gold = 3.5, iridium = 4.5, inspiredBonus = PaintingRules.InspiredBonus, sparksForInspired = PaintingRules.SparksForInspired },
            market = new
            {
                artShop = Market.ArtShopPremium, consign = Market.ConsignPremium, gallerist = Market.GalleristPrice, renown = Market.RenownValue,
                novelty = new[] { -1, 0, 1, 2, 3, 4 }.Select(n => new { thisWeek = n, value = n < 0 ? Market.Novelty(false, 0) : Market.Novelty(true, n) }),
                finishes = Market.Finishes.Select(f => new { f.Days, f.Key, f.Value }),
                portrait = PaintingRules.PortraitValue, masterpiece = PaintingRules.MasterpieceValue,
            },
            catalogue = Catalogue.Milestones.Select(m => new { subjects = m.Subjects, perk = m.Perk }),
            commissions = new { Shop.Commissions.MaxPosted, Shop.Commissions.MaxAccepted, Shop.Commissions.DaysToDeliver, Shop.Commissions.FriendshipReward, portraitChance = Shop.Commissions.PortraitChance },
            sable = sable == null ? null : new
            {
                birthday = $"{sable.BirthSeason} {sable.BirthDay}",
                gender = sable.Gender.ToString(),
                romanceable = sable.CanBeRomanced,
                loves = Taste(1), likes = Taste(3), dislikes = Taste(5), hates = Taste(7), neutral = Taste(9),
            },
            artLovers = ArtItems.DefaultArtLovers().Keys,
        };
        File.WriteAllText(Path.Combine(Dir, "wikidata.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));

        // An icon for every item, at the game's 16px (the wiki scales them).
        string iconDir = Path.Combine(Dir, "icons");
        Directory.CreateDirectory(iconDir);
        foreach (string id in Icons)
        {
            var parsed = ItemRegistry.GetDataOrErrorItem(id);
            var texture = parsed.GetTexture();
            var rect = parsed.GetSourceRect();
            var pixels = new Color[rect.Width * rect.Height];
            texture.GetData(0, rect, pixels, 0, pixels.Length);
            using var icon = new Texture2D(Game1.graphics.GraphicsDevice, rect.Width, rect.Height);
            icon.SetData(pixels);
            using var stream = File.Create(Path.Combine(iconDir, IconFile(id)));
            icon.SaveAsPng(stream, rect.Width, rect.Height);
        }
        _monitor.Log($"Wiki data written: media/wikidata.json, {Icons.Count} icons.", LogLevel.Info);
    }

    public static string IconFile(string qualifiedId) =>
        new string(qualifiedId.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '.' ? c : '-').ToArray()).Trim('-') + ".png";
}
#endif
