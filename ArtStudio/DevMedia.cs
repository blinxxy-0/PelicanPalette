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
        DevTools.AddCommand(helper, "museart_monstersheets", "Saves the top of every monster sprite sheet (and the Junimo's) to media/monsters/<name>.png, to measure their frames.", (_, _) =>
        {
            string dir = Path.Combine(_helper.DirectoryPath, "media", "monsters");
            Directory.CreateDirectory(dir);
            var names = Directory.GetFiles(Path.Combine(Constants.GamePath, "Content", "Characters", "Monsters"), "*.xnb").Select(Path.GetFileNameWithoutExtension).ToList();
            foreach (string name in names.Append("../Junimo"))
            {
                var texture = Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("Characters/Monsters/" + name);
                using var stream = File.Create(Path.Combine(dir, Path.GetFileName(name) + $"_{texture.Width}x{texture.Height}.png"));
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
            _monitor.Log($"Saved {names.Count + 1} sheets.", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_export", "Saves any game texture to media/export_<name>.png. Usage: museart_export TileSheets/BuffsIcons", (_, args) =>
        {
            var texture = Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(args[0]);
            string path = Path.Combine(_helper.DirectoryPath, "media", "export_" + args[0].Replace('/', '_') + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = File.Create(path);
            texture.SaveAsPng(stream, texture.Width, texture.Height);
            _monitor.Log($"Saved {path} ({texture.Width}x{texture.Height}).", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_signtext", "Renders a heading in the game's bold font to media/sign_<slug>.png. Usage: museart_signtext Features", (_, args) => SignText(string.Join(" ", args)));
        DevTools.AddCommand(helper, "museart_updatecard", "Renders the update card's parchment, lines and in-game icons to media/update_card.png (tools/art/update_card.py adds the sky, title and portraits).", (_, _) => UpdateCard());
        DevTools.AddCommand(helper, "museart_spriteaudit", "Every paintable item, creature and animal (1.3): which have see-through pixels and how much their true colours change the picture, and which small paintings are bigger than 16x16.", (_, _) => SpriteAudit());
        DevTools.AddCommand(helper, "museart_feasible", "How much work a large painting is against its light in each mode: cells, colours, regions, glazes, mixes and the clicks a second it asks (1.3). Run in a game, outdoors for the view.", (_, _) => Feasible());
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
            npc.IsInvisible = false;              // (Julian hides between visits)
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
        DevTools.AddCommand(helper, "museart_viewrefs", "Saves the view from the farmer's tile as references at each canvas size, to media/viewref_<size>.png (all pigments).", (_, _) =>
        {
            var capture = Views.Take(Game1.currentLocation, Game1.player.Tile);
            if (capture == null) { monitor.Log("No view here (stand outdoors).", LogLevel.Error); return; }
            var owned = Enumerable.Range(0, Palette.All.Length).ToList();
            foreach (var kind in new[] { CanvasKind.Small, CanvasKind.Medium, CanvasKind.Large })
            {
                int size = PaintingRules.CanvasSize(kind);
                var reference = Views.Reference(capture, size, owned);
                var purity = new Purity[size, size];
                var texture = PaintingRenderer.Build(size, reference.Cells, purity, Color.White, reference.Marks);
                string dir = Path.Combine(helper.DirectoryPath, "media");
                Directory.CreateDirectory(dir);
                using var stream = File.Create(Path.Combine(dir, $"viewref_{size}.png"));
                texture.SaveAsPng(stream, size, size);
                monitor.Log($"Saved the {size}x{size} view.", LogLevel.Info);
            }
            using var raw = File.Create(Path.Combine(helper.DirectoryPath, "media", "viewref_raw.png"));
            capture.Thumbnail?.SaveAsPng(raw, capture.Size, capture.Size);
        });
        DevTools.AddCommand(helper, "museart_act", "Uses a map tile as if the farmer clicked it (tile actions, doors). Usage: museart_act 80 35", (_, args) =>
        {
            var tile = new xTile.Dimensions.Location(int.Parse(args[0]), int.Parse(args[1]));
            bool done = Game1.currentLocation.checkAction(tile, Game1.viewport, Game1.player);
            monitor.Log($"Action at {tile}: {(done ? "handled" : "nothing")}; menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}.", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_nextpage", "Turns an open dialogue box to its next page, as a click would (for screenshots of later pages).", (_, _) =>
        {
            if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox box)
                box.receiveLeftClick(box.xPositionOnScreen + 8, box.yPositionOnScreen + 8);
        });
        DevTools.AddCommand(helper, "museart_stand", "Puts the farmer (and so the camera) on a tile, wherever it is, without the game nudging them aside. Usage: museart_stand 82 32", (_, args) =>
            Game1.player.Position = new Vector2(int.Parse(args[0]), int.Parse(args[1])) * 64f);
        DevTools.AddCommand(helper, "museart_wallpaper", "Papers this room's walls with a wallpaper, to compare them. Usage: museart_wallpaper blinxxy.MuseArt_Wallpapers:2", (_, args) =>
            (Game1.currentLocation as StardewValley.Locations.DecoratableLocation)?.SetWallpaper(args[0], null));
        DevTools.AddCommand(helper, "museart_passable", "Logs which tiles of this location the farmer can walk on ('.') or not ('#'), row by row. Usage: museart_passable 28 0 35 9", (_, args) =>
        {
            int x0 = int.Parse(args[0]), y0 = int.Parse(args[1]), x1 = int.Parse(args[2]), y1 = int.Parse(args[3]);
            var location = Game1.currentLocation;
            for (int y = y0; y <= y1; y++)
                monitor.Log($"y{y,3}: " + string.Concat(Enumerable.Range(x0, x1 - x0 + 1).Select(x =>
                    location.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport) && !location.IsTileOccupiedBy(new Vector2(x, y)) ? '.' : '#')), LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_tile", "Logs what's on each layer of a tile in this location, and what's standing on it. Usage: museart_tile 78 31", (_, args) =>
        {
            int x = int.Parse(args[0]), y = int.Parse(args[1]);
            var location = Game1.currentLocation;
            foreach (var layer in location.map.Layers)
            {
                var tile = layer.Tiles[x, y];
                if (tile != null)
                    monitor.Log($"({x},{y}) {layer.Id}: {tile.TileSheet.Id} {tile.TileIndex} {string.Join(" ", tile.Properties.Select(p => p.Key + "=" + p.Value))} {string.Join(" ", tile.TileIndexProperties.Select(p => p.Key + "=" + p.Value))}", LogLevel.Info);
            }
            var at = new Vector2(x, y);
            if (location.objects.TryGetValue(at, out var obj)) monitor.Log($"({x},{y}) object: {obj.QualifiedItemId}", LogLevel.Info);
            if (location.terrainFeatures.TryGetValue(at, out var feature)) monitor.Log($"({x},{y}) terrain: {feature.GetType().Name}", LogLevel.Info);
            foreach (var clump in location.resourceClumps.Where(c => c.occupiesTile(x, y))) monitor.Log($"({x},{y}) clump: {clump.parentSheetIndex.Value}", LogLevel.Info);
            foreach (var bush in location.largeTerrainFeatures.Where(f => f.getBoundingBox().Intersects(new Rectangle(x * 64, y * 64, 64, 64)))) monitor.Log($"({x},{y}) large: {bush.GetType().Name}", LogLevel.Info);
        });
        DevTools.AddCommand(helper, "museart_placef", "Places furniture in this location at a tile, for staging shots ('clear' takes it all out first). Usage: museart_placef (F)blinxxy.MuseArt_GalleryBench 5 5", (_, args) =>
        {
            if (args[0] == "clear")
            {
                Game1.currentLocation.furniture.Clear();
                return;
            }
            var furniture = ItemRegistry.Create<StardewValley.Objects.Furniture>(args[0]);
            furniture.TileLocation = new Vector2(int.Parse(args[1]), int.Parse(args[2]));
            Game1.currentLocation.furniture.Add(furniture);
        });
        DevTools.AddCommand(helper, "museart_actors", "Lists the playing event's actors and where they stand.", (_, _) =>
            monitor.Log("Actors: " + string.Join(", ", Game1.CurrentEvent?.actors.Select(a => $"{a.Name} {a.TilePoint}") ?? Array.Empty<string>()), LogLevel.Info));
        DevTools.AddCommand(helper, "museart_hangmargot", "Clears this room's furniture and hangs what Margot's 10-heart event gives for an answer, at the tiles given. Usage: museart_hangmargot EachOther 1 1 4 1 (or Window 3 1)", (_, args) =>
        {
            var who = Game1.player;
            string flag = ModEntry.Id + "_Margot" + args[0];
            who.mailReceived.Add(flag);
            var items = Social.MargotEvents.Paintings(who);
            who.mailReceived.Remove(flag);
            Game1.currentLocation.furniture.Clear();
            int n = 1;
            foreach (var item in items.OfType<StardewValley.Objects.Furniture>())
            {
                item.TileLocation = new Vector2(int.Parse(args[n]), int.Parse(args[n + 1]));
                Game1.currentLocation.furniture.Add(item);
                n += 2;
            }
        });
        DevTools.AddCommand(helper, "museart_seen", "Marks events as already seen. Usage: museart_seen blinxxy.MuseArt_SableIntro", (_, args) =>
        {
            foreach (string id in args)
                Game1.player.eventsSeen.Add(id);
        });
        DevTools.AddCommand(helper, "museart_flag", "Sets mail flags, then sleeps to the next morning so Content Patcher's conditions see them. Usage: museart_flag blinxxy.MuseArt_MargotCottageBuilt", (_, args) =>
        {
            foreach (string id in args)
                Game1.player.mailReceived.Add(id);
            Game1.saveOnNewDay = false;
            Game1.player.currentLocation = Utility.getHomeOfFarmer(Game1.player);
            Game1.player.Position = new Microsoft.Xna.Framework.Vector2(9f, 9f) * 64f;
            Game1.player.isInBed.Value = true;
            Game1.NewDay(0f);
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

    // ---- The 1.2 update card ----

    // What's new in 1.2, one line each, with the icons beside it: items drawn
    // as the game draws them, creatures and villagers from their sprites.
    // The Python side (tools/art/update_card.py) adds what the game can't
    // draw here (the lectern, plan chest and skiff from our map art), and
    // the sky, the gold title and the portraits.
    // The 1.3 update card (1.2's is in git history, and docs/media/page/update_1_2.png).
    private const string UpdateTitle = "1.3 Update";
    private static readonly (string Line, string[] Icons)[] UpdateLines =
    {
        ("-Margot Pell, the conservator", new[] { "npc:MargotPell" }),
        ("-Six heart events and a wedding", new[] { "(O)460", "(O)458" }),
        ("-21 old paintings to find", new[] { "old:valley_mill", "old:wreck_ship", "old:calico_queen" }),
        ("-Painting Mastery: restore them", new[] { "(BC)" + ArtItems.MastersEaselId }),
        ("-A museum collection and wing", new[] { "(F)" + LivingPainting.Id, "(O)blinxxy.MuseArt_MargotsLoupe" }),
        ("-Sixteen frames that do something", new[] { "(O)" + ArtItems.MoreFrame("birch").ItemId, "(O)" + ArtItems.GoldLeafFrameItemId, "(O)" + ArtItems.MoreFrame("prismatic").ItemId }),
        ("-The Framer's Bench", new[] { "(BC)" + FramersBench.BenchId }),
        ("-Paint your pets and farm animals", new[] { "animal:White Chicken", "animal:Brown Cow" }),
        ("-Easels at three more festivals", new[] { "(O)746" }),
        ("-Five special orders", new[] { "(O)858" }),
        ("-Julian buys a painting a season", new[] { "npc:JulianVane" }),
        ("-Wheel zoom, and a Simplified mode", Array.Empty<string>()),
    };

    public const int CardWidth = 1200, CardTop = 250, CardLine = 72;

    private static void UpdateCard()
    {
        if (!Context.IsWorldReady) return;
        var device = Game1.graphics.GraphicsDevice;
        int textW = UpdateLines.Max(l => SpriteText.getWidthOfString(l.Line));
        int iconX = 64 + textW + 40;
        int h = CardTop + UpdateLines.Length * CardLine + 260;
        using var target = new RenderTarget2D(device, CardWidth, h);
        var previous = device.GetRenderTargets();
        device.SetRenderTarget(target);
        device.Clear(Color.Transparent);
        var b = Game1.spriteBatch;
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        StardewValley.Menus.IClickableMenu.drawTextureBox(b, 0, 0, CardWidth, h, Color.White);
        SpriteText.drawStringHorizontallyCenteredAt(b, UpdateTitle, CardWidth / 2, CardTop - 84);
        var slots = new List<string>();
        for (int i = 0; i < UpdateLines.Length; i++)
        {
            var (line, icons) = UpdateLines[i];
            int y = CardTop + i * CardLine;
            SpriteText.drawString(b, line, 64, y);
            for (int j = 0; j < icons.Length; j++)
            {
                var at = new Vector2(iconX + j * 72, y - 12);
                string icon = icons[j];
                if (icon.StartsWith("(") )
                    ItemRegistry.Create(icon).drawInMenu(b, at, 1f, 1f, 0.9f, StackDrawType.Hide);
                else if (icon == "moonlit-painting")
                    ArtHooks.Frame(DevTools.TestPainting("view:Town:winter", PaintingRules.Gold), moonlit: true).drawInMenu(b, at, 1f, 1f, 0.9f, StackDrawType.Hide);
                else if (icon == "painting")
                    ArtHooks.Frame(DevTools.TestPainting("(O)24", PaintingRules.Iridium)).drawInMenu(b, at, 1f, 1f, 0.9f, StackDrawType.Hide);
                else if (icon == "tabs")
                    DevTools.TestPainting("(O)24", PaintingRules.Gold).drawInMenu(b, at, 1f, 1f, 0.9f, StackDrawType.Hide);
                else if (icon.StartsWith("old:") && OldPaintings.Get(icon.Substring(4)) is { } old)
                    OldPaintings.Draw(b, old, new Rectangle((int)at.X + 4, (int)at.Y + 4, 56, 56));
                else if (icon.StartsWith("animal:"))
                    Animals.Draw(b, icon.Substring("animal:".Length), new Rectangle((int)at.X + 4, (int)at.Y + 4, 56, 56));
                else if (icon.StartsWith("creature:"))
                    Creatures.Draw(b, icon.Substring("creature:".Length), new Rectangle((int)at.X + 4, (int)at.Y + 4, 56, 56));
                else if (icon.StartsWith("ribbon:"))
                    b.Draw(Game1.content.Load<Texture2D>($"Mods/{ModEntry.Id}/JulianItems"), new Rectangle((int)at.X + 8, (int)at.Y + 4, 48, 48),
                        new Rectangle(48 + 16 * int.Parse(icon.Substring("ribbon:".Length)), 0, 16, 16), Color.White);
                else if (icon.StartsWith("npc:"))
                {
                    // (Each villager's own frame size: Krobus's frames are shorter
                    // than most. One who hasn't arrived yet, as Margot may not
                    // have, is drawn from her sheet.)
                    string who = icon.Substring(4);
                    var npc = Game1.getCharacterFromName(who);
                    var sheet = npc?.Sprite.Texture ?? Game1.content.Load<Texture2D>("Characters/" + who);
                    int fw = npc?.Sprite.SpriteWidth ?? 16, fh = npc?.Sprite.SpriteHeight ?? 32;
                    b.Draw(sheet, new Rectangle((int)at.X + 32 - fw, (int)at.Y + 58 - fh * 2, fw * 2, fh * 2), new Rectangle(0, 0, fw, fh), Color.White);
                }
                else
                    slots.Add($"{icon} {(int)at.X} {(int)at.Y}");       // drawn by the Python side
            }
        }
        b.End();
        device.SetRenderTargets(previous);
        using (var stream = File.Create(Path.Combine(Dir, "update_card.png")))
            target.SaveAsPng(stream, CardWidth, h);
        File.WriteAllLines(Path.Combine(Dir, "update_card_slots.txt"), slots);
        _monitor.Log($"Update card saved: media/update_card.png ({CardWidth}x{h}), {slots.Count} icons left for the Python side.", LogLevel.Info);
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

    // ---- How hard the hardest paintings are (1.3) ----

    // For large portraits and the view at Painting level 10 with every pigment:
    // the work a careful player does (each region's edge painted cell by
    // cell, its inside filled once; on Hard, a remix every load; every glaze
    // and highlight its own click) against the light each mode gives, at the
    // Master's Easel. Mixing takes about 4 seconds a colour.
    private static void SpriteAudit()
    {
        if (!Context.IsWorldReady) return;
        var owned = Enumerable.Range(0, Palette.All.Length).ToList();
        // Each sprite's pixels, from the item, creature or animal.
        var sprites = new List<(string Kind, string Name, Color[] Pixels, int W, int H)>();
        void Add(string kind, string name, Texture2D texture, Rectangle rect)
        {
            rect = Rectangle.Intersect(rect, texture.Bounds);
            if (rect.Width <= 0 || rect.Height <= 0) return;
            var px = new Color[rect.Width * rect.Height];
            texture.GetData(0, rect, px, 0, px.Length);
            sprites.Add((kind, name, px, rect.Width, rect.Height));
        }
        foreach (string id in Game1.objectData.Keys.Select(k => "(O)" + k))
        {
            if (!ReferenceBuilder.CanPaint(ItemRegistry.Create(id))) continue;
            var data = ItemRegistry.GetDataOrErrorItem(id);
            Add("item", $"{data.DisplayName} [{id}]", data.GetTexture(), data.GetSourceRect());
        }
        foreach (var c in Creatures.All)
            if (Creatures.Frame(c.Name) is { } f) Add("creature", c.Name, f.Texture, f.Source);
        foreach (string type in Animals.AllTypes())
            if (Animals.Frame(type) is { } f) Add("animal", type, f.Texture, f.Source);

        // 1. See-through pixels: how many sprites, and how much they change.
        int affected = 0;
        var changes = new List<(string Kind, string Name, int Pixels, int Changed, double Lighter)>();
        foreach (var (kind, name, px, w, h) in sprites)
        {
            int see = 0, changed = 0; double lighter = 0;
            foreach (var p in px)
            {
                if (p.A < 128 || p.A == 255) continue;
                see++;
                int before = Palette.Nearest(p.R, p.G, p.B, owned);
                var t = ReferenceBuilder.OverCanvas(p);
                int after = Palette.Nearest(t.R, t.G, t.B, owned);
                if (before != after) changed++;
                lighter += (0.299 * (t.R - p.R) + 0.587 * (t.G - p.G) + 0.114 * (t.B - p.B));
            }
            if (see == 0) continue;
            affected++;
            changes.Add((kind, name, see, changed, lighter / see));
        }
        _monitor.Log($"See-through pixels: {affected} of {sprites.Count} sprites ({string.Join(", ", sprites.GroupBy(s => s.Kind).Select(g => $"{g.Count()} {g.Key}s"))})", LogLevel.Info);
        foreach (var g in changes.GroupBy(c => c.Kind))
            _monitor.Log($"  {g.Key}s: {g.Count()} with see-through pixels; {g.Count(c => c.Changed > 0)} now painted differently", LogLevel.Info);
        foreach (var c in changes.OrderByDescending(c => c.Changed).Take(25))
            _monitor.Log($"  {c.Kind} {c.Name}: {c.Pixels} see-through pixels, {c.Changed} change pigment, {c.Lighter:0} lighter on average", LogLevel.Info);

        // 1b. Sprites that look like the game's red error sign.
        var errorData = ItemRegistry.GetDataOrErrorItem("(O)" + ModEntry.Id + "_NoSuchItem");
        var er = errorData.GetSourceRect();
        var errorPx = new Color[er.Width * er.Height];
        errorData.GetTexture().GetData(0, er, errorPx, 0, errorPx.Length);
        var lookAlikes = sprites.Where(s => s.Pixels.Length == errorPx.Length && s.Pixels.Zip(errorPx).Count(t => t.First == t.Second) > errorPx.Length * 0.9)
            .Select(s => s.Name).ToList();
        _monitor.Log($"Error-sign look-alikes: {lookAlikes.Count}{(lookAlikes.Count > 0 ? ": " + string.Join(", ", lookAlikes.Take(40)) : "")}", LogLevel.Info);

        // 2. Small paintings bigger than 16x16 (painted cells only).
        int small = PaintingRules.CanvasSize(CanvasKind.Small), over = 0, total = 0;
        var big = new List<string>();
        foreach (var (kind, name, px, w, h) in sprites)
        {
            if (kind != "item" && (w > small - 2 || h > small - 2)) continue;     // (only those a small canvas takes)
            int x0 = w, x1 = -1, y0 = h, y1 = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].A >= 128)
                    (x0, x1, y0, y1) = (Math.Min(x0, x), Math.Max(x1, x), Math.Min(y0, y), Math.Max(y1, y));
            if (x1 < 0) continue;
            total++;
            int bw = x1 - x0 + 1, bh = y1 - y0 + 1;
            if (bw > 16 || bh > 16) { over++; big.Add($"{kind} {name} {bw}x{bh}"); }
        }
        _monitor.Log($"Small paintings: {total} subjects a small canvas takes; {over} bigger than 16x16 (views, which fill the canvas, aside){(big.Count > 0 ? ": " + string.Join(", ", big) : "")}", LogLevel.Info);
    }

    private static void Feasible()
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        ArtProgress.SetLevel(who, 10);
        ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length));
        var owned = ArtProgress.OwnedPigments(who);
        var rng = new Random(7);
        var items = Game1.objectData.Keys.Select(id => "(O)" + id).Where(id => ReferenceBuilder.CanPaint(ItemRegistry.Create(id))).ToList();
        const float MixSeconds = 4f;
        foreach (var kind in new[] { CanvasKind.Small, CanvasKind.Medium, CanvasKind.Large })
        {
            int size = PaintingRules.CanvasSize(kind);
            var allowed = PaintingRules.TechniquesFor(size, false);
            var refs = new List<Reference>();
            for (int i = 0; i < 25; i++)
                refs.Add(ReferenceBuilder.StillLife(items.OrderBy(_ => rng.Next()).Take(ReferenceBuilder.MaxItems(size)).ToArray(), size, owned, allowed));
            if (kind != CanvasKind.Small)
                foreach (string npc in new[] { "Abigail", "Emily", "Lewis", "Wizard", "Sable", "Haley", "Clint" })
                    if (Sitters.PortraitTexture(npc) != null) refs.Add(Sitters.Portrait(npc, size, owned, allowed));
            int load = PaintingRules.LoadSize(10, Brushes.All.First(b => b.Id == "iridium").Load, Purity.Pure);
            foreach (var mode in new[] { Difficulty.Normal, Difficulty.Hard })
            {
                var rules = DifficultySettings.For(mode);
                var paces = refs.Select(r =>
                {
                    float light = PaintingRules.LightSeconds(r, 10, rules.LightMultiplier, !rules.PauseLightWhileMixing);
                    int mixes = r.PigmentsUsed.Count + (rules.PaintLoads ? r.SubjectCells / load : 0);
                    float left = light - (rules.PauseLightWhileMixing ? 0f : MixSeconds * mixes);
                    return (Pace: left > 0 ? r.Work / left : 99f, r.Difficulty, Light: light);
                }).OrderBy(t => t.Pace).ToList();
                var easy = paces.Where(t => t.Difficulty < 40).Select(t => t.Pace).ToList();
                var hard = paces.Where(t => t.Difficulty >= 70).Select(t => t.Pace).ToList();
                string Avg(List<float> v) => v.Count == 0 ? "-" : $"{v.Average():0.00}";
                _monitor.Log($"{kind}, {mode}: {paces.Count} paintings, light {paces.Min(t => t.Light):0}-{paces.Max(t => t.Light):0}s; clicks a second {paces[0].Pace:0.00} to {paces[^1].Pace:0.00} (median {paces[paces.Count / 2].Pace:0.00}); easy (under 40) {Avg(easy)}, hard (70+) {Avg(hard)}", LogLevel.Info);
            }
        }
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

        // A villager's birthday and gifts (Sable, and Margot from 1.3).
        object? Villager(string npc)
        {
            if (!Game1.characterData.TryGetValue(npc, out var cd)) return null;
            var tastes = DataLoader.NpcGiftTastes(Game1.content).TryGetValue(npc, out string raw) ? raw.Split('/') : Array.Empty<string>();
            object Taste(int field) => field < tastes.Length
                ? tastes[field].Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => !t.StartsWith("-"))
                    .Select(t => t.StartsWith("(") ? t : "(O)" + t).Select(id => { Icons.Add(id); return (object)new { id, name = Name(id) }; }).ToList()
                : new List<object>();
            return new
            {
                birthday = $"{cd.BirthSeason} {cd.BirthDay}",
                gender = cd.Gender.ToString(),
                romanceable = cd.CanBeRomanced,
                loves = Taste(1), likes = Taste(3), dislikes = Taste(5), hates = Taste(7), neutral = Taste(9),
            };
        }

        // The frame styles (1.3): rarity, trait and the item.
        var frames = ArtItems.FrameStyles.Select(f =>
        {
            var (rarity, trait) = Painting.Frames.Of(f);
            string id = "(O)" + f.ItemId;
            Icons.Add(id);
            return new { style = f.Id, item = Name(id), id, rarity = rarity.ToString(), steps = (int)rarity, trait = trait.ToString(),
                percent = (int)rarity * Painting.Frames.Step(trait) * 100f };
        }).ToList();

        // The old paintings (1.3), their sets, values and Margot's fees.
        var oldPaintings = OldPaintings.All.Select(o => new
        {
            id = o.Id, title = OldPaintings.Title(o), set = o.Set, setName = OldPaintings.SetName(o.Set), size = o.Size,
            value = OldPaintings.SetValue(o.Set), fee = Social.MargotService.Fee(o),
        }).ToList();

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
            sable = Villager("Sable"),
            margot = Villager("MargotPell"),
            frames,
            oldPaintings,
            frameSteps = new { light = Painting.Frames.LightStep, night = Painting.Frames.NightStep, sparks = Painting.Frames.SparksStep, thrift = Painting.Frames.ThriftStep, learning = Painting.Frames.LearningStep, value = Painting.Frames.ValueStep, counted = Painting.Frames.Counted },
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
