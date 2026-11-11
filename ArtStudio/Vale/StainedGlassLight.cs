#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace ArtStudio.Vale;

// Light through the Corries' stained glass takes the glass's colours (the user, 2026-11-11).
// A window's daylight is two things in the game, both white for every window:
//  - the beam, a sprite in the cursors sheet drawn below the window (GameLocation.drawLightGlows);
//  - a window light in the lighting pass, which lifts the room's darkness round it.
// For the Corries' windows the white beam is replaced. The game's beam starts over the window,
// so recoloured it smeared a copy of the panes over the glass (tried 2026-11-11). Instead a
// faint shaft falls from the bottom of the window to the floor, and on the floor lies a pool:
// the window's panes, as their light lands, under the furniture and anyone standing in it.
// Whatever stands in front of the glass (a wardrobe, a picture hung over it) blocks its light:
// the pool and shaft are built for each placed window from the glass its neighbours' drawn
// pixels leave uncovered (the user, 2026-11-11). The window light is tinted with the glass's
// average colour, less as more of the glass is covered. The game subtracts the lightmap
// from the scene, so a light's colour is what it takes away: the glass's colour inverted.
internal static class StainedGlassLight
{
    private static readonly HashSet<string> Windows = new()
    {
        "(F)blinxxy.MuseArt_CorrieRoseWindow",
        "(F)blinxxy.MuseArt_CorrieValeWindow",
        "(F)blinxxy.MuseArt_CorrieArchWindow",
        "(F)blinxxy.MuseArt_CorrieSpringWindow",
        "(F)blinxxy.MuseArt_CorrieFarmWindow",
    };

    // The game's window beam (GameLocation.drawLightGlows), and where it hangs from the glow point.
    private static readonly Rectangle BeamSource = new(21, 1695, 41, 67);
    private static readonly Vector2 BeamOrigin = new(19f, 22f);

    private const float PoolAlpha = 0.42f;    // the pool on the floor, at its middle
    private const float ShaftAlpha = 0.16f;   // the shaft, where it leaves the glass
    private const float PoolStretch = 1.15f;  // the pool, a little longer than the window is tall
    private const float PoolDepth = 0.0006f;  // over rugs (at most 2e-9 + y / 100000), under all else
    private const float RoomTint = 0.35f;     // how strongly the room light takes the glass's colour

    // A window's glass (per kind of window), and one placed window's light (rebuilt when what
    // stands in front of it changes).
    private sealed record Glass(Vector3?[,] Pane, int Inset, Vector3 Mean, Color Average);
    private sealed record Light(int Key, Texture2D Pool, Texture2D Shaft, float Open);
    private static readonly Dictionary<string, Glass> Cache = new();
    private static readonly Dictionary<Furniture, Light> Lights = new();
    private static readonly Dictionary<(Texture2D, Rectangle), Color[]> Sprites = new();
    private static IMonitor _monitor = null!;

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.drawLightGlows)),
            prefix: new HarmonyMethod(typeof(StainedGlassLight), nameof(DrawLightGlowsPrefix)));
        harmony.Patch(AccessTools.Method(typeof(GameLocation), "resetLocalState"),
            postfix: new HarmonyMethod(typeof(StainedGlassLight), nameof(TintRoomLight)));
        helper.Events.Content.AssetsInvalidated += (_, e) =>
        {
            if (e.NamesWithoutLocale.Any(n => n.IsEquivalentTo("Mods/blinxxy.MuseArt/CorrieFurniture") || n.IsEquivalentTo("LooseSprites/Cursors")))
                Clear();
        };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
        helper.Events.Player.Warped += (_, e) =>
        {
            if (e.IsLocalPlayer)
                DropLights(l => true);
        };
    }

    private static void Clear()
    {
        foreach (var light in Lights.Values)
        {
            light.Pool.Dispose();
            light.Shaft.Dispose();
        }
        Lights.Clear();
        Cache.Clear();
        Sprites.Clear();
    }

    private static void DropLights(Func<Light, bool> which)
    {
        foreach (var (window, light) in Lights.Where(p => which(p.Value)).ToList())
        {
            light.Pool.Dispose();
            light.Shaft.Dispose();
            Lights.Remove(window);
        }
    }

    public static bool IsStainedGlass(Furniture f) => Windows.Contains(f.QualifiedItemId);

    // The Corries' windows in a location whose daylight is on, by their glow point.
    private static Dictionary<Vector2, Furniture> LitWindows(GameLocation location)
    {
        var lit = new Dictionary<Vector2, Furniture>();
        foreach (var f in location.furniture)
            if (f.lightGlowPosition is Vector2 glow && IsStainedGlass(f))
                lit[glow] = f;
        return lit;
    }

    // The game's own loop (GameLocation.drawLightGlows), with the shaft and pool in place of the beam under the
    // Corries' windows. Any other location draws as it always has.
    private static bool DrawLightGlowsPrefix(GameLocation __instance, SpriteBatch b)
    {
        try
        {
            var lit = LitWindows(__instance);
            if (lit.Count == 0)
                return true;
            foreach (Vector2 glow in __instance.lightGlows)
            {
                if (!__instance.lightGlowLayerCache.ContainsKey(glow))
                    __instance.lightGlowLayerCache.Add(glow, GlowLayer(__instance, glow));
                float depth = (glow.Y + 64f * __instance.lightGlowLayerCache[glow]) / 10000f;
                Vector2 at = Game1.GlobalToLocal(Game1.viewport, glow);
                if (lit.TryGetValue(glow, out var window) && LightOf(__instance, window) is { } light)
                    DrawLight(b, __instance, window, light, depth);
                else
                    b.Draw(Game1.mouseCursors, at, BeamSource, Color.White, 0f, BeamOrigin, 4f, SpriteEffects.None, depth);
            }
            return false;
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Stained glass light: {ex}", LogLevel.Error);
            return true;
        }
    }

    // The shaft from the window's sill to the floor, and the pool of its panes on the floor.
    private static void DrawLight(SpriteBatch b, GameLocation location, Furniture window, Light glass, float depth)
    {
        if (glass.Open <= 0f)
            return;
        Rectangle box = window.boundingBox.Value;
        int inset = (box.Width / 4 - glass.Pool.Width) / 2 * 4;
        int left = box.X + inset, width = glass.Pool.Width * 4;
        int sill = box.Bottom - inset;
        int floor = FloorBelow(location, box) * 64 + 8;
        int poolHeight = (int)(glass.Pool.Height * 4 * PoolStretch);
        b.Draw(glass.Pool, Local(new Rectangle(left, floor, width, poolHeight)), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, PoolDepth);
        if (floor > sill)
            b.Draw(glass.Shaft, Local(new Rectangle(left, sill, width, floor - sill + poolHeight / 3)), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    private static Rectangle Local(Rectangle world) =>
        new(world.X - Game1.viewport.X, world.Y - Game1.viewport.Y, world.Width, world.Height);

    // The first row under the window that isn't wall. A room with wallpaper knows its walls;
    // elsewhere a wall row is one blocked most of the way across the map. (Beds and benches are
    // often in the Buildings layer too, so "a Buildings tile under the window" put a pool on
    // the Corries' bed, 2026-11-11.)
    private static int FloorBelow(GameLocation location, Rectangle box)
    {
        var buildings = location.map?.GetLayer("Buildings");
        int centre = box.Center.X / 64;
        int row = box.Bottom / 64;
        for (int k = 0; k < 6 && buildings != null && row < buildings.LayerHeight; k++, row++)
        {
            bool wall = location is DecoratableLocation room && room.isTileOnWall(centre, row);
            if (!wall)
            {
                int blocked = 0;
                for (int x = 0; x < buildings.LayerWidth; x++)
                    if (buildings.Tiles[x, row] != null)
                        blocked++;
                wall = blocked >= buildings.LayerWidth * 0.6f;
            }
            if (!wall)
                return row;
        }
        return box.Bottom / 64 + 1;
    }

    // As the game picks a beam's layer (GameLocation.drawLightGlows).
    private static float GlowLayer(GameLocation location, Vector2 glow)
    {
        var below = location.GetFurnitureAt(new Vector2((int)(glow.X / 64f), (int)(glow.Y / 64f) + 2));
        if (below != null && below.sourceRect.Height / 16 - below.getTilesHigh() > 1)
            return 2.5f;
        if (location is FarmHouse { upgradeLevel: > 0 } house)
        {
            var tile = new Vector2((int)(glow.X / 64f), (int)(glow.Y / 64f));
            Vector2 d = Utility.PointToVector2(house.getKitchenStandingSpot()) - tile;
            if (d.Y == 3f && (d.X == 2f || d.X == 3f || d.X == -1f || d.X == -2f))
                return 1.5f;
        }
        return 10f;
    }

    // The window lights the game made for the Corries' windows, tinted with their glass.
    private static void TintRoomLight(GameLocation __instance)
    {
        try
        {
            if (__instance != Game1.currentLocation)
                return;
            var lit = LitWindows(__instance);
            if (lit.Count == 0)
                return;
            int tinted = 0;
            foreach (var light in Game1.currentLightSources.Values)
                if (light.lightContext.Value == LightSource.LightContext.WindowLight && lit.TryGetValue(light.position.Value, out var window)
                    && Get(window) is { } glass && LightOf(__instance, window) is { } own)
                {
                    tinted++;
                    Color c = glass.Average;
                    float tint = RoomTint * own.Open;
                    light.color.Value = new Color((int)((255 - c.R) * tint), (int)((255 - c.G) * tint), (int)((255 - c.B) * tint));
                }
            _monitor.Log($"Stained glass: {tinted} of {lit.Count} window lights tinted in {__instance.NameOrUniqueName}.");
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Stained glass room light: {ex}", LogLevel.Error);
        }
    }

    private static Glass? Get(Furniture window)
    {
        if (Cache.TryGetValue(window.QualifiedItemId, out var made))
            return made;
        var data = ItemRegistry.GetDataOrErrorItem(window.QualifiedItemId);
        Texture2D sheet = data.GetTexture();
        Rectangle day = window.defaultSourceRect.Value;
        var pixels = new Color[day.Width * day.Height];
        sheet.GetData(0, day, pixels, 0, pixels.Length);
        made = Make(pixels, day.Width, day.Height);
        Cache[window.QualifiedItemId] = made;
        return made;
    }

    // From the window's day frame: the glass inside the frame, each pane's colour brought up to
    // full brightness (light through it, not the glass itself), the leading letting none through.
    private static Glass Make(Color[] glass, int gw, int gh)
    {
        int inset = Math.Max(2, gw / 10);
        int iw = gw - 2 * inset, ih = gh - 2 * inset;
        var pane = new Vector3?[iw, ih];
        Vector3 sum = Vector3.Zero;
        int n = 0;
        for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                Color c = glass[(y + inset) * gw + x + inset];
                float max = Math.Max(c.R, Math.Max(c.G, c.B));
                if (c.A < 200 || max < 60)
                    continue;   // the leading, or a gap
                var v = new Vector3(c.R, c.G, c.B) / max;
                pane[x, y] = v;
                sum += v;
                n++;
            }
        Vector3 mean = n > 0 ? sum / n : Vector3.One;
        mean /= Math.Max(mean.X, Math.Max(mean.Y, mean.Z));

        return new Glass(pane, inset, mean, new Color(mean));
    }

    // One placed window's light, kept until what stands in front of its glass changes.
    private static Light? LightOf(GameLocation location, Furniture window)
    {
        if (Get(window) is not { } glass)
            return null;
        var (blocked, key) = Blocked(location, window, glass);
        if (Lights.TryGetValue(window, out var light) && light.Key == key)
            return light;
        if (light != null)
        {
            light.Pool.Dispose();
            light.Shaft.Dispose();
        }
        light = Build(glass, blocked, OnFloorObjects(location, window, glass), key);
        Lights[window] = light;
        return light;
    }

    // The glass pixels covered by furniture drawn in front of the window, read from their
    // sprites' own pixels (a rack's gaps let light through), and a key for what covers it.
    private static (bool[,] Blocked, int Key) Blocked(GameLocation location, Furniture window, Glass glass)
    {
        int iw = glass.Pane.GetLength(0), ih = glass.Pane.GetLength(1);
        var blocked = new bool[iw, ih];
        Rectangle box = window.boundingBox.Value;
        var inner = new Rectangle(box.X + glass.Inset * 4, box.Y + glass.Inset * 4, iw * 4, ih * 4);
        float windowDepth = Depth(window);
        int windowIndex = location.furniture.IndexOf(window);
        int key = 17;
        int index = -1;
        foreach (var f in location.furniture)
        {
            index++;
            if (f == window || f.furniture_type.Value == Furniture.rug)
                continue;
            Rectangle sprite = SpriteBounds(f);
            if (!sprite.Intersects(inner))
                continue;
            float depth = Depth(f);
            if (depth < windowDepth || (depth == windowDepth && index < windowIndex))
                continue;   // drawn behind the window
            Rectangle source = f.sourceRect.Value;
            Color[] pixels = Pixels(f, source);
            key = HashCode.Combine(key, f.QualifiedItemId, f.TileLocation, source, f.flipped.Value);
            for (int y = 0; y < ih; y++)
                for (int x = 0; x < iw; x++)
                {
                    int wx = inner.X + x * 4 + 2, wy = inner.Y + y * 4 + 2;
                    if (!sprite.Contains(wx, wy))
                        continue;
                    int sx = (wx - sprite.X) / 4, sy = (wy - sprite.Y) / 4;
                    if (f.flipped.Value)
                        sx = source.Width - 1 - sx;
                    if (pixels[sy * source.Width + sx].A > 40)
                        blocked[x, y] = true;
                }
        }
        // the map's own art drawn over furniture (a wardrobe or a tool rack painted into the room)
        foreach (string layerName in new[] { "Front", "AlwaysFront" })
        {
            var layer = location.map?.GetLayer(layerName);
            if (layer == null)
                continue;
            for (int y = 0; y < ih; y++)
                for (int x = 0; x < iw; x++)
                {
                    int wx = inner.X + x * 4 + 2, wy = inner.Y + y * 4 + 2;
                    int tx = wx / 64, ty = wy / 64;
                    if (blocked[x, y] || tx < 0 || ty < 0 || tx >= layer.LayerWidth || ty >= layer.LayerHeight)
                        continue;
                    if (layer.Tiles[tx, ty] is { } tile && TileOpaqueAt(tile, (wx % 64) / 4, (wy % 64) / 4))
                        blocked[x, y] = true;
                }
        }
        return (blocked, key);
    }

    // Whether a map tile's pixel is drawn (its tilesheet as the game loaded it; if that can't be
    // read, the tile is taken to cover the glass).
    private static bool TileOpaqueAt(xTile.Tiles.Tile tile, int px, int py)
    {
        Texture2D? texture = TileSheetTexture(tile.TileSheet);
        if (texture == null)
            return true;
        var bounds = tile.TileSheet.GetTileImageBounds(tile.TileIndex);
        var source = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        if (!Sprites.TryGetValue((texture, source), out var pixels))
        {
            pixels = new Color[source.Width * source.Height];
            if (Rectangle.Intersect(source, texture.Bounds) == source)
                texture.GetData(0, source, pixels, 0, pixels.Length);
            else
                Array.Fill(pixels, Color.White);
            Sprites[(texture, source)] = pixels;
        }
        px = px * source.Width / 16;
        py = py * source.Height / 16;
        return pixels[Math.Clamp(py, 0, source.Height - 1) * source.Width + Math.Clamp(px, 0, source.Width - 1)].A > 40;
    }

    private static Texture2D? TileSheetTexture(xTile.Tiles.TileSheet sheet)
    {
        try
        {
            var field = AccessTools.Field(Game1.mapDisplayDevice.GetType(), "m_tileSheetTextures");
            if (field?.GetValue(Game1.mapDisplayDevice) is System.Collections.IDictionary textures && textures.Contains(sheet))
                return textures[sheet] as Texture2D;
            return Game1.content.Load<Texture2D>(sheet.ImageSource);
        }
        catch
        {
            return null;
        }
    }

    // Where a piece of furniture is drawn (Furniture.draw: its sprite stands on its bounding box).
    private static Rectangle SpriteBounds(Furniture f)
    {
        Rectangle source = f.sourceRect.Value, box = f.boundingBox.Value;
        return new Rectangle((int)f.TileLocation.X * 64, (int)f.TileLocation.Y * 64 - (source.Height * 4 - box.Height), source.Width * 4, source.Height * 4);
    }

    // Its layer, as Furniture.draw sets it (wall pieces sit 48 px further back).
    private static float Depth(Furniture f)
    {
        int type = f.furniture_type.Value;
        bool onWall = type == Furniture.painting || type == Furniture.window || type == 17;
        return (f.boundingBox.Value.Bottom - (onWall ? 48 : 8)) / 10000f;
    }

    private static Color[] Pixels(Furniture f, Rectangle source)
    {
        Texture2D texture = ItemRegistry.GetDataOrErrorItem(f.QualifiedItemId).GetTexture();
        if (!Sprites.TryGetValue((texture, source), out var pixels))
        {
            pixels = new Color[source.Width * source.Height];
            Rectangle clamped = Rectangle.Intersect(source, texture.Bounds);
            if (clamped == source)
                texture.GetData(0, source, pixels, 0, pixels.Length);
            Sprites[(texture, source)] = pixels;
        }
        return pixels;
    }

    // The pool's pixels that would land on something the map draws on its Buildings layer (a
    // toolbox, a bed). That layer is drawn before anything layered, so the pool would lie on
    // top of it; the light is cut there instead, as if those things stood over it (the user,
    // 2026-11-11). Furniture needs nothing: it's drawn over the pool already.
    private static bool[,] OnFloorObjects(GameLocation location, Furniture window, Glass glass)
    {
        int iw = glass.Pane.GetLength(0), ih = glass.Pane.GetLength(1);
        var covered = new bool[iw, ih];
        var layer = location.map?.GetLayer("Buildings");
        if (layer == null)
            return covered;
        Rectangle box = window.boundingBox.Value;
        int left = box.X + glass.Inset * 4;
        int floor = FloorBelow(location, box) * 64 + 8;
        for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                int wx = left + x * 4 + 2, wy = floor + (int)((y + 0.5f) * 4 * PoolStretch);
                int tx = wx / 64, ty = wy / 64;
                if (tx >= 0 && ty >= 0 && tx < layer.LayerWidth && ty < layer.LayerHeight
                    && layer.Tiles[tx, ty] is { } tile && TileOpaqueAt(tile, (wx % 64) / 4, (wy % 64) / 4))
                    covered[x, y] = true;
            }
        return covered;
    }

    private static Light Build(Glass glass, bool[,] blocked, bool[,] onObjects, int key)
    {
        var pane = glass.Pane;
        int iw = pane.GetLength(0), ih = pane.GetLength(1);
        Vector3? Through(int x, int y) => blocked[x, y] ? null : pane[x, y];

        // the pool: the uncovered panes, softening to nothing at the edges (premultiplied alpha)
        var pool = new Color[iw * ih];
        int glassPixels = 0, open = 0;
        for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                if (pane[x, y] != null)
                {
                    glassPixels++;
                    if (!blocked[x, y])
                        open++;
                }
                if (onObjects[x, y] || Through(x, y) is not Vector3 v)
                    continue;
                float ex = MathF.Min(x + 0.5f, iw - x - 0.5f) / (iw / 2f), ey = MathF.Min(y + 0.5f, ih - y - 0.5f) / (ih / 2f);
                float a = PoolAlpha * MathF.Min(1f, 2.2f * MathF.Min(ex, ey));
                pool[y * iw + x] = Premultiplied(v, a);
            }

        // the shaft: each column the mean of the glass above it that light gets through, as
        // strong as the share of that column's glass left uncovered, fading as it falls
        const int shaftRows = 16;
        var shaft = new Color[iw * shaftRows];
        for (int x = 0; x < iw; x++)
        {
            Vector3 col = Vector3.Zero;
            int through = 0, panes = 0;
            for (int y = 0; y < ih; y++)
            {
                if (pane[x, y] != null)
                    panes++;
                if (Through(x, y) is Vector3 v) { col += v; through++; }
            }
            if (through == 0)
                continue;
            col /= through;
            col /= Math.Max(col.X, Math.Max(col.Y, col.Z));
            float share = through / (float)Math.Max(1, panes);
            float side = MathF.Min(x + 0.5f, iw - x - 0.5f) / (iw / 2f);
            for (int y = 0; y < shaftRows; y++)
                shaft[y * iw + x] = Premultiplied(col, share * ShaftAlpha * (1f - y / (float)shaftRows) * MathF.Min(1f, 3f * side));
        }

        var poolTexture = new Texture2D(Game1.graphics.GraphicsDevice, iw, ih);
        poolTexture.SetData(pool);
        var shaftTexture = new Texture2D(Game1.graphics.GraphicsDevice, iw, shaftRows);
        shaftTexture.SetData(shaft);
        return new Light(key, poolTexture, shaftTexture, open / (float)Math.Max(1, glassPixels));
    }

    private static Color Premultiplied(Vector3 rgb, float a) =>
        new((int)(rgb.X * a * 255), (int)(rgb.Y * a * 255), (int)(rgb.Z * a * 255), (int)(a * 255));
}
#endif
