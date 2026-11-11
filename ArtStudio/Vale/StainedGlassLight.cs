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
// The window light is tinted with the glass's average colour. The game subtracts the lightmap
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

    private sealed record Glass(Texture2D Pool, Texture2D Shaft, int Inset, Color Average);
    private static readonly Dictionary<string, Glass> Cache = new();
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
    }

    private static void Clear()
    {
        foreach (var glass in Cache.Values)
        {
            glass.Pool.Dispose();
            glass.Shaft.Dispose();
        }
        Cache.Clear();
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
                if (lit.TryGetValue(glow, out var window) && Get(window) is { } glass)
                    DrawLight(b, __instance, window, glass, depth);
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
    private static void DrawLight(SpriteBatch b, GameLocation location, Furniture window, Glass glass, float depth)
    {
        Rectangle box = window.boundingBox.Value;
        int inset = glass.Inset * 4;
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
                if (light.lightContext.Value == LightSource.LightContext.WindowLight && lit.TryGetValue(light.position.Value, out var window) && Get(window) is { } glass)
                {
                    tinted++;
                    Color c = glass.Average;
                    light.color.Value = new Color((int)((255 - c.R) * RoomTint), (int)((255 - c.G) * RoomTint), (int)((255 - c.B) * RoomTint));
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

        // the pool: the panes, softening to nothing at the edges (premultiplied alpha)
        var pool = new Color[iw * ih];
        for (int y = 0; y < ih; y++)
            for (int x = 0; x < iw; x++)
            {
                if (pane[x, y] is not Vector3 v)
                    continue;
                float ex = MathF.Min(x + 0.5f, iw - x - 0.5f) / (iw / 2f), ey = MathF.Min(y + 0.5f, ih - y - 0.5f) / (ih / 2f);
                float a = PoolAlpha * MathF.Min(1f, 2.2f * MathF.Min(ex, ey));
                pool[y * iw + x] = Premultiplied(v, a);
            }

        // the shaft: each column the mean of the glass above it, fading as it falls
        const int shaftRows = 16;
        var shaft = new Color[iw * shaftRows];
        for (int x = 0; x < iw; x++)
        {
            Vector3 col = Vector3.Zero;
            int m = 0;
            for (int y = 0; y < ih; y++)
                if (pane[x, y] is Vector3 v) { col += v; m++; }
            col = m > 0 ? col / m : mean;
            col /= Math.Max(col.X, Math.Max(col.Y, col.Z));
            float side = MathF.Min(x + 0.5f, iw - x - 0.5f) / (iw / 2f);
            for (int y = 0; y < shaftRows; y++)
                shaft[y * iw + x] = Premultiplied(col, ShaftAlpha * (1f - y / (float)shaftRows) * MathF.Min(1f, 3f * side));
        }

        var poolTexture = new Texture2D(Game1.graphics.GraphicsDevice, iw, ih);
        poolTexture.SetData(pool);
        var shaftTexture = new Texture2D(Game1.graphics.GraphicsDevice, iw, shaftRows);
        shaftTexture.SetData(shaft);
        return new Glass(poolTexture, shaftTexture, inset, new Color(mean));
    }

    private static Color Premultiplied(Vector3 rgb, float a) =>
        new((int)(rgb.X * a * 255), (int)(rgb.Y * a * 255), (int)(rgb.Z * a * 255), (int)(a * 255));
}
#endif
