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
    public static void DrawArt(SpriteBatch b, Item item, Rectangle area, Color tint, float layerDepth)
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
        if (Varnish.FinishOf(item) is string finish)
            DrawSheen(b, dest, finish, tint, layerDepth + 0.000002f);
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
