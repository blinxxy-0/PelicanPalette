#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArtStudio.Pottery;

internal sealed record PotSprite(Texture2D Texture, Rectangle Bounds);

// A kept pot's sprite (PotteryPieces), built from its profile and glaze at the
// game's own pixel scale. The wheel draws the pot side on; furniture is seen a
// little from above, as the game's furniture is, so here the pot is turned in
// 3D: each row a circle, seen as an ellipse, the rim's opening showing the
// glazed inside. The shading is the wheel's (PotRenderer): lit from the upper
// left, in a glaze's six shades, with the glaze's shine down the left, and the
// edges a darker shade of the colour beside them. The pot faces you as it came
// out of the kiln (turned as the kiln scene shows it), so its bands match.
internal static class PotPieceRenderer
{
    public const int Width = 16;
    private const float Tilt = 0.4f;          // a rim's ellipse, as tall over as wide
    private const float Aspect = 0.62f;       // a clay unit across to a row up, as on the wheel (PotRenderer's UnitPx to RowPx)
    private const float Turn = 0.6f;          // the pot's turn as the kiln shows it (PotRenderer.DrawKiln)
    private static readonly Color Bisque = new(206, 156, 120);

    public static int HeightFor(string shape) => PotteryPieces.IsTall(shape) ? 32 : 16;

    private static readonly Dictionary<string, PotSprite> Cache = new();

    public static PotSprite? TextureFor(Item item)
    {
        string? key = PotPiece.LookKey(item);
        if (key == null)
            return null;
        if (Cache.TryGetValue(key, out var cached) && !cached.Texture.IsDisposed)
            return cached;
        if (PotPiece.Read(item) is not { } piece)
            return null;
        var px = Build(piece, out int h, out var bounds);
        var texture = new Texture2D(Game1.graphics.GraphicsDevice, Width, h);
        texture.SetData(px);
        return Cache[key] = new PotSprite(texture, bounds);
    }

    public static void ClearCache()
    {
        foreach (var sprite in Cache.Values)
            sprite.Texture.Dispose();
        Cache.Clear();
    }

    // How big each shape may be drawn, in art pixels (width, height), and the most art
    // pixels a row of clay may take, so a pot keeps its size against the others: a vase
    // takes more clay than a cup, and stands bigger.
    private static (float W, float H, float RowPx) Box(string shape) => shape switch
    {
        "bowl" => (14f, 13f, 0.8f),
        "cup" => (12f, 14f, 0.8f),
        _ => (15f, 30f, 1.05f),
    };

    private enum Kind : byte { None, Outer, Inner, Floor, Rim }

    public static Color[] Build(PotPiece p, out int height, out Rectangle bounds)
    {
        int w = Width, h = height = HeightFor(p.Shape);
        int n = p.Rows;
        float cos = MathF.Sqrt(1f - Tilt * Tilt);

        float Lerp(float[] a, float y)
        {
            int i = Math.Clamp((int)MathF.Floor(y), 0, n - 1);
            float f = Math.Clamp(y - i, 0f, 1f);
            return i + 1 < n ? a[i] * (1 - f) + a[i + 1] * f : a[i];
        }
        float R(float y) => MathF.Max(0.5f, Lerp(p.R, y));
        float Wall(float y) => Math.Clamp(Lerp(p.T, y), 0.3f, R(y) - 0.2f);

        // The pot's extent at a row a pixel: above the foot's middle (the rim's far edge)
        // and below it (the foot's near edge). Everything scales with the pixels a row takes.
        float above = 0f, below = 0f, widest = 0f;
        for (float y = 0f; y <= n; y += 0.1f)
        {
            float r = R(y) * Aspect;
            above = MathF.Max(above, y * cos + Tilt * r);
            below = MathF.Max(below, Tilt * r - y * cos);
            widest = MathF.Max(widest, r);
        }
        var box = Box(p.Shape);
        float s = MathF.Min(box.RowPx, MathF.Min(box.W / (2f * widest), box.H / (above + below)));
        float u = Aspect * s;                        // art pixels to a clay unit across
        float cx = w / 2f;
        float baseY = h - 1.02f - below * s;         // the foot's near edge on the sprite's second row up

        var colour = new Color[w * h];
        var depth = new float[w * h];
        var kind = new Kind[w * h];
        Array.Fill(depth, float.NegativeInfinity);

        // A point on the pot (x across, y up the pot in pixels, z towards you), kept where
        // it's nearest the eye.
        void Plot(float x, float y, float z, Color c, Kind k)
        {
            float sx = cx + x, sy = baseY - y * cos + z * Tilt;
            int ix = (int)MathF.Floor(sx), iy = (int)MathF.Floor(sy);
            if (ix < 0 || ix >= w || iy < 0 || iy >= h)
                return;
            float d = z * cos + y * Tilt;
            int i = iy * w + ix;
            if (d <= depth[i])
                return;
            depth[i] = d;
            colour[i] = c;
            kind[i] = k;
        }

        Color Glazed(int g, int shade) => PotRenderer.GlazeShade(g < 0 ? Bisque : Glazes.All[g].Colour, Math.Clamp(shade, 0, 5));
        int Bucket(float theta)
        {
            int k = (int)MathF.Floor((theta - Turn) / MathF.Tau * PotGlaze.Buckets);
            return ((k % PotGlaze.Buckets) + PotGlaze.Buckets) % PotGlaze.Buckets;
        }
        int SliceAt(float y) => Math.Clamp((int)(y * PotGlaze.SlicesPerRow), 0, Math.Max(0, p.Slices - 1));

        const int Around = 256;
        float step = 0.2f / s;                       // a fifth of a pixel up the pot
        float floorY = MathF.Min(1f, n * 0.15f);

        // the outside: the wheel's light (PotRenderer), with the slope's light from above
        for (float y = 0f; y <= n; y += step)
        {
            float slope = (R(MathF.Min(n, y + 0.5f)) - R(MathF.Max(0f, y - 0.5f))) * Aspect / cos;
            float nh = 1f / MathF.Sqrt(1f + slope * slope), ny = -slope * nh;
            float r = R(y) * u;
            int slice = SliceAt(y);
            for (int a = 0; a < Around; a++)
            {
                float theta = a * MathF.Tau / Around - MathF.PI;
                float sin = MathF.Sin(theta), c = MathF.Cos(theta);
                float light = nh * (0.62f * c - 0.32f * sin) + 0.06f + 0.35f * MathF.Max(0f, ny) - 0.2f * MathF.Max(0f, -ny);
                int shade = (int)(light * 6);
                int bucket = Bucket(theta);
                int g = p.GlazeAt(slice, bucket);
                Color col = Glazed(g, shade);
                if (g >= 0)
                {
                    var gl = Glazes.All[g];
                    if (gl.Speckled && (((bucket * 2 + (int)(y * 2.5f)) * 73856093) ^ (slice * 19349663)) % 7 == 0)
                        col = Glazed(g, shade - 3);                                         // iron flecks
                    if (!p.Dull && c > 0f && sin > -0.66f && sin < -0.4f && ny > -0.5f)
                        col = Color.Lerp(Glazed(g, 5), Color.White, 0.45f);                // the glaze's shine
                }
                Plot(r * sin, y * s, r * c, col, Kind.Outer);
            }
        }
        // the inside: glazed by the dip, darker deeper down, lit on the far right
        int inside = p.Base;
        for (float y = floorY; y <= n; y += step)
        {
            float r = (R(y) - Wall(y)) * u;
            float deep = (n - y) / MathF.Max(1f, n);
            for (int a = 0; a < Around; a++)
            {
                float theta = a * MathF.Tau / Around - MathF.PI;
                float sin = MathF.Sin(theta), c = MathF.Cos(theta);
                float light = 0.3f + 0.22f * sin - 0.12f * c - 0.9f * deep;
                Plot(r * sin, y * s, r * c, Glazed(inside, (int)MathF.Floor(light * 6)), Kind.Inner);
            }
        }
        // its floor
        float floorR = (R(floorY) - Wall(floorY)) * u;
        for (float rr = 0f; rr <= floorR; rr += 0.2f)
            for (int a = 0; a < Around; a++)
            {
                float theta = a * MathF.Tau / Around;
                Plot(rr * MathF.Sin(theta), floorY * s, rr * MathF.Cos(theta), Glazed(inside, n > 6 ? 0 : 1), Kind.Floor);
            }
        // the rim: the lip's top, lit, in the glaze at the top of the pot
        float rimOut = R(n) * u, rimIn = (R(n) - Wall(n)) * u;
        int top = SliceAt(n);
        for (float rr = rimIn; rr <= rimOut + 0.01f; rr += 0.2f)
            for (int a = 0; a < Around; a++)
            {
                float theta = a * MathF.Tau / Around - MathF.PI;
                float sin = MathF.Sin(theta);
                Plot(rr * sin, n * s + 0.01f, rr * MathF.Cos(theta), Glazed(p.GlazeAt(top, Bucket(theta)), sin < 0.3f ? 5 : 4), Kind.Rim);
            }

        // A badly fired glaze is greyed, as the kiln shows it.
        if (p.Dull)
            for (int i = 0; i < colour.Length; i++)
                if (kind[i] != Kind.None)
                {
                    Color c = colour[i];
                    int grey = (c.R * 3 + c.G * 6 + c.B) / 10;
                    colour[i] = Color.Lerp(c, new Color(grey, grey, (int)(grey * 0.95f)), 0.4f);
                }

        KilnGift(p, colour, kind, w, h);

        // Edges against nothing take a darker shade of themselves (never black).
        var edge = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (kind[i] == Kind.None)
                    continue;
                edge[i] = x == 0 || x == w - 1 || y == 0 || y == h - 1
                    || kind[i - 1] == Kind.None || kind[i + 1] == Kind.None || kind[i - w] == Kind.None || kind[i + w] == Kind.None;
            }
        for (int i = 0; i < colour.Length; i++)
            if (edge[i])
            {
                Color c = colour[i];
                colour[i] = new Color((int)(c.R * 0.6f), (int)(c.G * 0.55f), (int)(c.B * 0.55f));
            }

        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (kind[y * w + x] != Kind.None)
                    (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, y), Math.Max(x1, x), Math.Max(y1, y));
        bounds = x1 < 0 ? new Rectangle(0, 0, w, h) : new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        return colour;
    }

    private static int Hash(int x, int y) => (int)(((uint)(x * 73856093) ^ (uint)(y * 19349663)) % 1000u);

    // A kiln gift, small enough to read at a tile: a fine crackle over the glaze, a drip
    // at the foot, a warm flash on one side, or the glaze pooled darker in the rings.
    private static void KilnGift(PotPiece p, Color[] colour, Kind[] kind, int w, int h)
    {
        if (p.Gift == null)
            return;
        var outer = Enumerable.Range(0, w * h).Where(i => kind[i] == Kind.Outer).ToList();
        if (outer.Count == 0)
            return;
        int top = outer.Min(i => i / w), bottom = outer.Max(i => i / w);
        int left = outer.Min(i => i % w), right = outer.Max(i => i % w);
        switch (p.Gift)
        {
            case "crackle":
                // a net of fine lines, lighter than the glaze
                foreach (int i in outer)
                {
                    int x = i % w, y = i / w;
                    if ((x + 2 * y) % 5 == 0 && Hash(x, y) % 3 != 0 || (2 * x - y + 40) % 7 == 0 && Hash(y, x) % 2 == 0)
                        colour[i] = Color.Lerp(colour[i], new Color(250, 244, 226), 0.3f);
                }
                break;
            case "drip":
                // runs of the glaze down to the foot, darker and glassy
                foreach (int i in outer)
                {
                    int x = i % w, y = i / w;
                    if (Hash(x, 7) % 3 == 0 && y >= bottom - 2 - Hash(x, 3) % 2 && x > left && x < right)
                        colour[i] = Color.Lerp(colour[i], new Color(colour[i].R / 2, colour[i].G / 2, colour[i].B / 2 + 12), 0.55f);
                }
                break;
            case "flash":
                // a blush where the flame licked it, low on the right
                float fx = left + (right - left) * 0.75f, fy = top + (bottom - top) * 0.6f, reach = MathF.Max(3f, (right - left) * 0.45f);
                foreach (int i in outer)
                {
                    int x = i % w, y = i / w;
                    float d = MathF.Sqrt((x - fx) * (x - fx) + (y - fy) * (y - fy) * 0.6f) / reach;
                    if (d < 1f)
                        colour[i] = Color.Lerp(colour[i], new Color(206, 104, 56), 0.45f * (1f - d));
                }
                break;
            default:
                // pooled in the throwing rings: every third pixel row a shade deeper
                foreach (int i in outer)
                    if ((i / w - top) % 3 == 2)
                    {
                        Color c = colour[i];
                        colour[i] = new Color((int)(c.R * 0.82f), (int)(c.G * 0.84f), Math.Min(255, (int)(c.B * 0.9f) + 6));
                    }
                break;
        }
    }
}
#endif
