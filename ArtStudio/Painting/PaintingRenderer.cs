using System.Collections.Generic;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArtStudio.Painting;

// Draws paintings: builds (and caches) a small texture from each painting's
// cells, and draws it inside the frame sprite wherever the item appears.
internal static class PaintingRenderer
{
    // The frame sprite is 16×16 with a 2px border; the art fills the rest.
    private const int FrameBorder = 2;
    private const int FrameSize = 16;

    private static readonly Color MudColor = new(96, 84, 70);
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Color CellColor(byte pigment, Purity purity, Mark mark = Mark.None)
    {
        var p = Palette.All[pigment];
        var (r, g, b) = Technique.Apply(p.R, p.G, p.B, mark);
        var c = new Color(r, g, b);
        return purity switch
        {
            Purity.Pure => c,
            Purity.Off => Color.Lerp(c, new Color(150, 145, 140), 0.22f),
            _ => Color.Lerp(c, MudColor, 0.5f),
        };
    }

    public static Texture2D? TextureFor(Item item)
    {
        string? raw = PaintingData.RawCells(item);
        if (raw == null) return null;
        if (Cache.TryGetValue(raw, out var cached) && !cached.IsDisposed)
            return cached;
        if (!PaintingData.TryDecodeCells(raw, out int size, out var cells, out var purity, out var marks))
            return null;
        var texture = Build(size, cells, purity, new Color(243, 236, 220), marks);
        Cache[raw] = texture;
        return texture;
    }

    // A small painting as it hangs at 1x1 (1.3): its painted cells with the
    // bare canvas round them cut away, and shrunk to fit SmallFrameInside if
    // it's bigger (a cell to a pixel, so it stays crisp), centred on the mat.
    public const int SmallFrameInside = 14;
    private static readonly Dictionary<string, Texture2D> SmallCache = new();

    public static Texture2D? SmallFrameTextureFor(Item item)
    {
        string? raw = PaintingData.RawCells(item);
        if (raw == null) return null;
        if (SmallCache.TryGetValue(raw, out var cached) && !cached.IsDisposed)
            return cached;
        if (!PaintingData.TryDecodeCells(raw, out int size, out var cells, out var purity, out var marks))
            return null;
        int x0 = size, y0 = size, x1 = -1, y1 = -1;
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
            if (cells[x, y] != PixelCanvas.Blank)
                (x0, y0, x1, y1) = (System.Math.Min(x0, x), System.Math.Min(y0, y), System.Math.Max(x1, x), System.Math.Max(y1, y));
        if (x1 < 0) (x0, y0, x1, y1) = (0, 0, size - 1, size - 1);
        int w = x1 - x0 + 1, h = y1 - y0 + 1, side = SmallFrameInside;
        float k = System.Math.Min(1f, side / (float)System.Math.Max(w, h));
        int dw = System.Math.Max(1, (int)System.MathF.Round(w * k)), dh = System.Math.Max(1, (int)System.MathF.Round(h * k));
        int ox = (side - dw) / 2, oy = (side - dh) / 2;
        var data = new Color[side * side];
        for (int i = 0; i < data.Length; i++) data[i] = MatColor;
        for (int dx = 0; dx < dw; dx++)
        for (int dy = 0; dy < dh; dy++)
        {
            int sx = x0 + System.Math.Min(w - 1, (int)((dx + 0.5f) / k)), sy = y0 + System.Math.Min(h - 1, (int)((dy + 0.5f) / k));
            if (cells[sx, sy] != PixelCanvas.Blank)
                data[(oy + dy) * side + ox + dx] = CellColor(cells[sx, sy], purity[sx, sy], marks?[sx, sy] ?? Mark.None);
        }
        var texture = new Texture2D(Game1.graphics.GraphicsDevice, side, side);
        texture.SetData(data);
        SmallCache[raw] = texture;
        return texture;
    }

    // Draws a 1x1-framed painting's picture into the frame's 14x14 inside.
    public static void DrawSmallFramed(SpriteBatch b, Item item, Rectangle inside, Color tint, float layerDepth)
    {
        b.Draw(Game1.staminaRect, inside, null, MatColor * (tint.A / 255f), 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        if (SmallFrameTextureFor(item) is not { } art) return;
        b.Draw(art, inside, null, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth + 0.000001f);
        if (Varnish.FinishOf(item) is string finish)
            DrawSheen(b, inside, finish, tint, layerDepth + 0.000002f);
    }

    public static Texture2D Build(int size, byte[,] cells, Purity[,] purity, Color blank, Mark[,]? marks = null)
    {
        var data = new Color[size * size];
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
            data[y * size + x] = cells[x, y] == PixelCanvas.Blank ? blank : CellColor(cells[x, y], purity[x, y], marks?[x, y] ?? Mark.None);
        var texture = new Texture2D(Game1.graphics.GraphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    // Draws the framed painting centered on `center`, at `scale` screen
    // pixels per frame-sprite pixel (4 = normal item size).
    public static void DrawFramed(SpriteBatch b, Item item, Vector2 center, float scale, Color tint, float layerDepth)
    {
        var frame = Game1.content.Load<Texture2D>(PaintingItems.ItemsTexture);
        b.Draw(frame, center, new Rectangle(0, 0, FrameSize, FrameSize), tint, 0f, new Vector2(FrameSize / 2f), scale, SpriteEffects.None, layerDepth);

        float inner = (FrameSize - 2 * FrameBorder) * scale;
        DrawArt(b, item, new Rectangle((int)(center.X - inner / 2), (int)(center.Y - inner / 2), (int)inner, (int)inner), tint, layerDepth + 0.00001f);
    }

    private static readonly Color MatColor = new(236, 226, 204);

    // Draws the painting's pixels inside `area` at the largest whole number
    // of screen pixels per cell, centered on a mat.
    // `alive`: hung on a wall, where a living masterwork moves (DrawLife).
    public static void DrawArt(SpriteBatch b, Item item, Rectangle area, Color tint, float layerDepth, bool alive = false)
    {
        var art = TextureFor(item);
        b.Draw(Game1.staminaRect, area, null, MatColor * (tint.A / 255f), 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        // A painting with no picture (spawned by an item spawner, say) shows
        // as the bare canvas.
        if (art == null) return;
        int cell = System.Math.Max(1, System.Math.Min(area.Width, area.Height) / art.Width);
        int side = cell * art.Width;
        if (side > System.Math.Min(area.Width, area.Height))
            side = System.Math.Min(area.Width, area.Height);
        var dest = new Rectangle(area.Center.X - side / 2, area.Center.Y - side / 2, side, side);
        b.Draw(art, dest, null, tint, 0f, Vector2.Zero, SpriteEffects.None, layerDepth + 0.000001f);
        if (alive && PaintingData.IsLiving(item))
            DrawLife(b, item, dest, tint, layerDepth + 0.0000015f);
        if (Varnish.FinishOf(item) is string finish)
            DrawSheen(b, dest, finish, tint, layerDepth + 0.000002f);
    }

    // A machine's "ready" bubble shows its held item's plain sprite: for a
    // painting, a bare canvas. This draws the painting itself over it (the
    // Varnishing Cabinet, the Framer's Bench), bobbing with the bubble.
    public static void DrawInReadyBubble(SpriteBatch b, StardewValley.Object machine, Item painting, int x, int y)
    {
        float bubbleDepth = (y + 1) * 64 / 10000f + machine.TileLocation.X / 50000f;
        float bob = 4f * (float)System.Math.Round(System.Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
        painting.drawInMenu(b, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 96 - 8 + bob)), 1f, 0.75f, bubbleDepth + 1.5E-05f,
            StackDrawType.Hide, Color.White, drawShadow: false);
    }

    // ---- Living masterworks (the Master's Easel, 1.3) ----

    // A gold-or-better painting made at the Master's Easel moves a little
    // when it hangs, by what it's of. A scene (a view painted outdoors, a
    // restored old painting, an original) comes alive in its colours: glints
    // drift across its water (slate, cerulean, aquamarine), firelight
    // breathes in its reds and golds (crimson, ruby, marigold), and a breeze
    // passes over its leaves (moss, leaf). Anything else (a still life, a
    // portrait, an animal, a creature), where a blue might be a shirt, gets a
    // soft band of window light drifting slowly across it now and then.
    private enum Life : byte { None, Water, Fire, Leaf }
    private static readonly Dictionary<string, (int Size, Life[,] Kinds, bool Scene)> LifeCache = new();

    public static bool IsScene(PaintingData data) => data.IsView || data.IsOld || data.IsOriginal;

    private static Life LifeOf(byte pigment) => pigment == PixelCanvas.Blank || pigment >= Palette.All.Length ? Life.None
        : Palette.All[pigment].Id switch
        {
            "slate" or "cerulean" or "aquamarine" => Life.Water,
            "crimson" or "ruby" or "marigold" => Life.Fire,
            "moss" or "leaf" => Life.Leaf,
            _ => Life.None,
        };

    // Every 9 seconds, a soft diagonal band of warm light crosses the
    // picture in about 4, as the sun through a window would.
    private const double LightPeriod = 9.0, LightSweep = 4.0;

    private static void DrawWindowLight(SpriteBatch b, Rectangle dest, int size, float cell, int side, double t, float alpha, float layerDepth)
    {
        double phase = t % LightPeriod;
        if (phase > LightSweep) return;
        // The band's centre, from before the top left to past the bottom right.
        float centre = (float)(phase / LightSweep) * 1.6f - 0.3f;
        const float width = 0.16f;
        var warm = new Color(255, 236, 190);
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            float at = (x + y * 0.6f) / (size * 1.6f);
            float off = System.Math.Abs(at - centre);
            if (off >= width) continue;
            float strength = (1f - off / width) * 0.2f;
            var r = new Rectangle(dest.X + (int)(x * cell), dest.Y + (int)(y * cell), side, side);
            b.Draw(Game1.staminaRect, r, null, warm * (strength * alpha), 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        }
    }

    public static void DrawLife(SpriteBatch b, Item item, Rectangle dest, Color tint, float layerDepth)
    {
        if (PaintingData.RawCells(item) is not { } raw) return;
        if (!LifeCache.TryGetValue(raw, out var life))
        {
            if (!PaintingData.TryDecodeCells(raw, out int size, out var cells, out _, out _)) return;
            var kinds = new Life[size, size];
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                kinds[x, y] = LifeOf(cells[x, y]);
            LifeCache[raw] = life = (size, kinds, PaintingData.Read(item) is { } data && IsScene(data));
        }
        double t = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        float alpha = tint.A / 255f, cell = dest.Width / (float)life.Size;
        int side = (int)System.Math.Ceiling(cell);
        if (!life.Scene)
        {
            DrawWindowLight(b, dest, life.Size, cell, side, t, alpha, layerDepth);
            return;
        }
        for (int x = 0; x < life.Size; x++)
        for (int y = 0; y < life.Size; y++)
        {
            var kind = life.Kinds[x, y];
            if (kind == Life.None) continue;
            float seed = ((x * 73 + y * 151) % 97) / 97f;
            Color glow;
            float strength;
            switch (kind)
            {
                case Life.Water:
                    // Bands of light sliding along the water, a glint at each crest.
                    float wave = (float)System.Math.Sin(t * 2.0 - x * 0.9 + y * 0.45 + seed * 0.8);
                    if (wave < 0.8f) continue;
                    glow = Color.White;
                    strength = (wave - 0.8f) / 0.2f * 0.4f;
                    break;
                case Life.Fire:
                    // A warm breathing glow, each cell a little out of step.
                    float flicker = (float)(0.5 + 0.5 * System.Math.Sin(t * 7.0 + seed * 6.28)) * (float)(0.6 + 0.4 * System.Math.Sin(t * 2.3 + seed * 3.1));
                    glow = new Color(255, 214, 120);
                    strength = 0.04f + 0.16f * flicker;
                    break;
                default:
                    // A breeze crossing the leaves, left to right.
                    float gust = (float)System.Math.Sin(t * 1.4 - x * 0.35 + System.Math.Sin(y * 0.7) * 0.6);
                    if (gust < 0.55f) continue;
                    glow = new Color(240, 255, 200);
                    strength = (gust - 0.55f) / 0.45f * 0.18f;
                    break;
            }
            var r = new Rectangle(dest.X + (int)(x * cell), dest.Y + (int)(y * cell), side, side);
            b.Draw(Game1.staminaRect, r, null, glow * (strength * alpha), 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        }
    }

    // Varnish catches the light: a soft diagonal glint across the top left,
    // brighter the glossier the finish.
    private static void DrawSheen(SpriteBatch b, Rectangle art, string finish, Color tint, float layerDepth)
    {
        float strength = finish switch { "satin" => 0.12f, "gloss" => 0.2f, _ => 0.28f } * (tint.A / 255f);
        int step = System.Math.Max(1, art.Width / 16);
        for (int i = 0; i < art.Width / 2; i += step)
        {
            var glint = new Rectangle(art.X + i, art.Y + art.Width / 2 - i - step, step, step * 2);
            if (glint.Y < art.Y) break;
            b.Draw(Game1.staminaRect, glint, null, Color.White * strength, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
        }
    }

    public static void ClearCache()
    {
        foreach (var texture in Cache.Values)
            texture.Dispose();
        Cache.Clear();
    }
}
