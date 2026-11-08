#if DEBUG
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArtStudio.Pottery;

// The wheel scene as pixel art (docs/design/pottery_minigame.md, section 11):
// the pot changes shape every frame, so it can't be a sprite. It's drawn pixel
// by pixel onto a small canvas at the game's own scale (one art pixel shows as
// four screen pixels), shaded as a cylinder lit from the upper left, with a
// wet sheen, turning rings, outlines a darker shade of the clay beside them,
// and the right half cut away to show the wall. Behind it, Tobin's workshop
// wall and his kick wheel (PixelLab, styled on his pottery's kiln:
// art/pottery/, assets/pottery/).
internal sealed record Sprite(int W, int H, Color[] Px);

internal static class PotteryArt
{
    public static Sprite? Workshop, Wheel;
    // the wheel head's middle, in the wheel sprite (where a pot's foot stands)
    public const int WheelHeadX = 38, WheelHeadY = 6, WheelHeadLeft = 17, WheelHeadRight = 59, WheelHeadBottom = 8;
    private static bool _loaded;

    public static void Load(StardewModdingAPI.IModHelper helper)
    {
        if (_loaded)
            return;
        _loaded = true;
        Sprite? Get(string name)
        {
            try
            {
                var tex = helper.ModContent.Load<Texture2D>($"assets/pottery/{name}.png");
                var px = new Color[tex.Width * tex.Height];
                tex.GetData(px);
                return new Sprite(tex.Width, tex.Height, px);
            }
            catch
            {
                return null;        // (drawn plainly without it)
            }
        }
        Workshop = Get("workshop");
        Wheel = Get("wheel");
    }
}

internal sealed class PotRenderer : IDisposable
{
    public const int Scale = 4;
    public int Width { get; }
    public int Height { get; }
    private int Ground => Height - 33;               // the wheel head's top row (the wheel's legs reach the bottom)

    // terracotta, darkest to lightest
    private static readonly Color[] Ramp =
    {
        new(92, 44, 30), new(122, 62, 40), new(150, 82, 52), new(176, 104, 66), new(200, 128, 86), new(222, 158, 114),
    };
    private static readonly Color Sheen = new(244, 210, 176);
    private static readonly Color Inside = new(64, 34, 26), InsideDeep = new(46, 24, 18);
    private static readonly Color Chalk = new(255, 246, 222);
    private static readonly Color Thin = new(232, 70, 50), Heavy = new(70, 110, 220);

    private readonly Texture2D _texture;
    private readonly Color[] _px;
    private readonly byte[] _kind;     // 0 none, 1 clay, 2 inside (for outlines)

    public int RowPx { get; private set; } = 4;     // art pixels per row of the pot
    public float UnitPx { get; private set; } = 2.4f;
    public int CentreX => Width / 2;
    // the bottom row of clay: on the wheel head's near edge, inside the splash pan
    public int FootY => PotteryArt.Wheel != null ? Ground - 1 - PotteryArt.WheelHeadY + PotteryArt.WheelHeadBottom : Ground - 2;

    public PotRenderer(int width = 150, int height = 118)
    {
        Width = width;
        Height = height;
        _px = new Color[width * height];
        _kind = new byte[width * height];
        _texture = new Texture2D(Game1.graphics.GraphicsDevice, width, height);
    }

    public void Dispose() => _texture.Dispose();

    // Big enough to fill the scene, with room to pull the pot taller than the shape.
    public void FitTo(PotShape shape)
    {
        RowPx = Math.Clamp((int)((Ground - 8) / MathF.Max(shape.Rows * 1.35f, 16f)), 3, 7);
        UnitPx = MathF.Min(RowPx * 0.62f, 2.6f);
    }

    // The pot's row under an art-pixel height (for the hand), and back.
    public float RowAt(float artY) => (FootY - artY) / RowPx - 0.5f;

    public void DrawScene(SpriteBatch b, Rectangle dest, float spin, ClayPot? pot, PotShape shape, bool showReference,
        float lumpOffset, float handRow, Color handColor, bool showHand, float handX = float.NaN)
    {
        Array.Clear(_kind);
        Backdrop();
        Wheel(spin);
        if (pot == null)
            Lump(lumpOffset, spin);
        else
            Pot(pot, spin);
        Outline();
        if (pot != null && !pot.Collapsed)
            Rim(pot);
        PanFront();
        if (showReference)
            Reference(shape);
        if (showHand)
            Hand(handRow, handColor, pot, handX);
        _texture.SetData(_px);
        b.Draw(_texture, dest, Color.White);
    }

    // Banding: the whole pot, no cut-away, in its glazes, turning; the design's bands as
    // coloured marks hugging its right side; the brush touching its left side.
    // `angle` is the banding wheel's turn (radians); `brushHeight` in rows from the foot
    // (NaN: no brush).
    public void DrawBanding(SpriteBatch b, Rectangle dest, float spin, float angle, ClayPot pot, PotGlaze glaze,
        GlazeDesign design, float brushHeight, int brushGlaze)
    {
        Array.Clear(_kind);
        Backdrop();
        Wheel(spin);
        GlazedPot(pot, glaze, angle);
        Outline();
        GlazedRim(pot, glaze, angle);
        PanFront();
        DesignMarks(pot, glaze, design);
        if (!float.IsNaN(brushHeight))
            Brush(pot, brushHeight, brushGlaze);
        _texture.SetData(_px);
        b.Draw(_texture, dest, Color.White);
    }

    // A glaze's six shades, darkest to lightest: shadows cooler and darker, lights warmer.
    public static Color GlazeShade(Color c, int k)
    {
        var dark = new Color((int)(c.R * 0.42f), (int)(c.G * 0.44f), (int)(c.B * 0.52f + 10));
        var light = Color.Lerp(c, new Color(255, 242, 222), 0.38f);
        float t = k / 5f;
        return t < 0.6f ? Color.Lerp(dark, c, t / 0.6f) : Color.Lerp(c, light, (t - 0.6f) / 0.4f);
    }

    private static readonly Color Bisque = new(206, 156, 120);       // dried, unglazed clay: pale and matte

    // The bucket of the glaze grid a pixel shows: the wheel turns the pot left to right
    // across its front, so a ring painted at the left edge comes round to face you.
    private static int BucketAt(float u, float angle)
    {
        float theta = MathF.Asin(Math.Clamp(u, -1f, 1f)) - angle;
        int k = (int)MathF.Floor(theta / MathF.Tau * PotGlaze.Buckets);
        return ((k % PotGlaze.Buckets) + PotGlaze.Buckets) % PotGlaze.Buckets;
    }

    private void GlazedPot(ClayPot pot, PotGlaze glaze, float angle)
    {
        int rows = pot.Rows, cx = CentreX;
        for (int yy = 0; yy < rows * RowPx; yy++)
        {
            int i = Math.Min(rows - 1, yy / RowPx);
            float f = (yy % RowPx) / (float)RowPx;
            float r = (i + 1 < rows ? pot.R[i] * (1 - f) + pot.R[i + 1] * f : pot.R[i]) * UnitPx;
            int slice = Math.Min(glaze.Slices - 1, yy * PotGlaze.SlicesPerRow / RowPx);
            int y = FootY - yy;
            if (y < 0 || y >= Height)
                continue;
            for (int x = (int)MathF.Floor(cx - r); x <= (int)MathF.Ceiling(cx + r); x++)
            {
                float u = (x + 0.5f - cx) / MathF.Max(r, 1f);
                if (MathF.Abs(u) > 1f || x < 0 || x >= Width)
                    continue;
                float n = MathF.Sqrt(MathF.Max(0f, 1f - u * u));
                float light = 0.62f * n - 0.32f * u + 0.06f;
                int k = Math.Clamp((int)(light * 6), 0, 5);
                int g = glaze.At(slice, BucketAt(u, angle));
                Color c;
                if (g < 0)
                {
                    c = GlazeShade(Bisque, k);
                    if (yy % 5 == 0 && MathF.Abs(u) < 0.9f)
                        c = GlazeShade(Bisque, Math.Max(0, k - 1));       // the throwing rings, dry
                }
                else
                {
                    var gl = Glazes.All[g];
                    c = GlazeShade(gl.Colour, k);
                    if (gl.Speckled)
                    {
                        // iron flecks, turning with the pot
                        int fleck = (int)MathF.Floor((MathF.Asin(Math.Clamp(u, -1f, 1f)) - angle) * 18f);
                        if (((fleck * 73856093) ^ (yy * 19349663)) % 9 == 0)
                            c = GlazeShade(gl.Colour, Math.Max(0, k - 3));
                    }
                    if (u > -0.62f && u < -0.46f)
                        c = Color.Lerp(GlazeShade(gl.Colour, 5), Color.White, 0.55f);     // glaze shines
                }
                _px[y * Width + x] = c;
                _kind[y * Width + x] = 1;
            }
        }
    }

    private void GlazedRim(ClayPot pot, PotGlaze glaze, float angle)
    {
        int top = FootY - pot.Rows * RowPx + 1;
        float r = pot.R[pot.Rows - 1] * UnitPx, inner = (pot.R[pot.Rows - 1] - pot.T[pot.Rows - 1]) * UnitPx;
        int g = glaze.At(glaze.Slices - 1, BucketAt(0f, angle));
        Color lip = g < 0 ? GlazeShade(Bisque, 5) : GlazeShade(Glazes.All[g].Colour, 5);
        int cx = CentreX;
        for (int x = (int)(cx - r) + 1; x < cx + r - 1; x++)
            Set(x, top, MathF.Abs(x + 0.5f - cx) < inner ? InsideDeep : lip);
    }

    // The design: each band as a short coloured line just right of the pot, following its
    // edge, with a chalk line outside it so dark glazes show against the wall.
    private void DesignMarks(ClayPot pot, PotGlaze glaze, GlazeDesign design)
    {
        int rows = pot.Rows, cx = CentreX;
        foreach (var band in design.Bands)
        {
            var (from, to) = GlazeDesign.Slices(band, glaze.Slices);
            int y0 = from * RowPx / PotGlaze.SlicesPerRow, y1 = ((to + 1) * RowPx + PotGlaze.SlicesPerRow - 1) / PotGlaze.SlicesPerRow - 1;
            Color c = GlazeShade(Glazes.All[band.Glaze].Colour, 4);
            for (int yy = y0; yy <= y1; yy++)
            {
                int i = Math.Min(rows - 1, yy / RowPx);
                float f = (yy % RowPx) / (float)RowPx;
                float r = (i + 1 < rows ? pot.R[i] * (1 - f) + pot.R[i + 1] * f : pot.R[i]) * UnitPx;
                int x = (int)MathF.Ceiling(cx + r) + 3;
                Set(x, FootY - yy, c);
                Set(x + 1, FootY - yy, c);
                Set(x + 2, FootY - yy, c);
                Set(x + 3, FootY - yy, Chalk);
            }
        }
    }

    // The brush, its tip on the pot's left side at its height: a dab of the glaze it carries
    // as tall as it paints, and a wooden handle reaching out to the left.
    private void Brush(ClayPot pot, float height, int glazeIndex)
    {
        int rows = pot.Rows;
        float row = Math.Clamp(height - 0.5f, 0f, rows - 1);
        float r = pot.R[(int)row] * UnitPx;
        int edge = (int)MathF.Floor(CentreX - r) - 1;
        int yMid = (int)MathF.Round(FootY - height * RowPx);
        int half = Math.Max(1, (int)(PotGlaze.BrushRows * RowPx / 2f));
        Color baseC = glazeIndex >= 0 ? Glazes.All[glazeIndex].Colour : Chalk;
        // the bristles, loaded with glaze, outlined a darker shade of it
        for (int y = yMid - half - 1; y <= yMid + half + 1; y++)
            for (int x = edge - 4; x <= edge; x++)
            {
                bool rim = y == yMid - half - 1 || y == yMid + half + 1 || x == edge - 4;
                Set(x, y, rim ? GlazeShade(baseC, 0) : GlazeShade(baseC, x == edge - 3 ? 4 : 3));
            }
        // the ferrule, then the handle
        for (int y = yMid - 2; y <= yMid + 2; y++)
        {
            Set(edge - 5, y, y == yMid - 2 ? new Color(220, 220, 228) : new Color(150, 150, 160));
            Set(edge - 6, y, new Color(96, 96, 108));
        }
        for (int x = edge - 22; x <= edge - 7; x++)
        {
            Set(x, yMid - 2, new Color(84, 52, 34));
            Set(x, yMid - 1, new Color(190, 140, 92));
            Set(x, yMid, new Color(150, 104, 66));
            Set(x, yMid + 1, new Color(84, 52, 34));
        }
    }

    private void Set(int x, int y, Color c)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _px[y * Width + x] = c;
    }

    // A sprite onto the canvas, over what's there.
    private void Blit(Sprite s, int x0, int y0)
    {
        for (int y = 0; y < s.H; y++)
        {
            int ty = y0 + y;
            if (ty < 0 || ty >= Height) continue;
            for (int x = 0; x < s.W; x++)
            {
                int tx = x0 + x;
                if (tx < 0 || tx >= Width) continue;
                Color c = s.Px[y * s.W + x];
                if (c.A > 127)
                    _px[ty * Width + tx] = new Color(c.R, c.G, c.B);
            }
        }
    }

    private void Backdrop()
    {
        if (PotteryArt.Workshop is { } wall)
        {
            Blit(wall, 0, 0);
            return;
        }
        // Tobin's workshop, for now: a plank wall, a shelf, a plank floor
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                Color c;
                if (y < Ground + 4)
                {
                    c = new Color(118, 80, 56);
                    if (x % 18 == 0) c = new Color(98, 66, 46);
                    else if (x % 18 == 1) c = new Color(132, 92, 64);
                    if (y is >= 22 and <= 24) c = y == 22 ? new Color(150, 108, 74) : new Color(84, 56, 40);   // the shelf
                }
                else
                {
                    c = new Color(150, 106, 70);
                    if ((y - Ground) % 6 == 0) c = new Color(126, 86, 56);
                }
                _px[y * Width + x] = c;
            }
        // pots on the shelf
        foreach (var (sx, w, h) in new[] { (14, 6, 9), (26, 9, 7), (110, 7, 11), (124, 5, 8) })
            for (int y = 21 - h; y < 22; y++)
                for (int x = sx; x < sx + w; x++)
                {
                    float u = (x - sx) / (float)(w - 1) * 2f - 1f;
                    Set(x, y, x == sx || x == sx + w - 1 || y == 21 - h ? new Color(96, 52, 38) : u < -0.2f ? new Color(186, 112, 76) : new Color(150, 84, 56));
                }
    }

    private void Wheel(float spin)
    {
        int cx = CentreX;
        if (PotteryArt.Wheel is { } wheel)
        {
            int wx = cx - PotteryArt.WheelHeadX, wy = Ground - 1 - PotteryArt.WheelHeadY;
            Blit(wheel, wx, wy);
            // its shine going round
            int span = PotteryArt.WheelHeadRight - PotteryArt.WheelHeadLeft;
            int sx = wx + PotteryArt.WheelHeadLeft + 2 + (int)((spin * 5f) % (span - 4));
            for (int k = 0; k < 3; k++)
                Set(sx + k, Ground - 1, new Color(232, 232, 238));
            return;
        }
        // the splash pan
        for (int y = Ground + 1; y < Ground + 11; y++)
            for (int x = cx - 40; x <= cx + 40; x++)
            {
                bool edge = Math.Abs(x - cx) >= 39 || y == Ground + 10;
                Set(x, y, edge ? new Color(84, 60, 42) : y == Ground + 1 ? new Color(160, 124, 90) : new Color(132, 98, 70));
            }
        // the wheel head, with its shine going round
        for (int x = cx - 30; x <= cx + 30; x++)
        {
            Set(x, Ground - 1, new Color(184, 184, 192));
            Set(x, Ground, new Color(146, 146, 156));
            Set(x, Ground + 1, new Color(104, 104, 114));
        }
        int shine = cx - 30 + (int)((spin * 6f) % 61f);
        for (int k = 0; k < 3; k++)
            Set(shine + k, Ground - 1, new Color(230, 230, 236));
    }

    // One row of clay at an art-pixel height: shaded outside on the left half, cut away
    // on the right to show the wall (tinted where it's thin or heavy).
    private void ClayRow(int y, float r, float inner, int ringPhase, float wet, Color? wallTint, bool cut)
    {
        int cx = CentreX;
        int x0 = (int)MathF.Floor(cx - r), x1 = (int)MathF.Ceiling(cx + r);
        for (int x = x0; x <= x1; x++)
        {
            float u = (x + 0.5f - cx) / MathF.Max(r, 1f);
            if (MathF.Abs(u) > 1f || y < 0 || y >= Height)
                continue;
            int i = y * Width + x;
            if (cut && x >= cx && x + 0.5f - cx < inner)
            {
                // inside the pot: shadow, deepest just inside the wall
                _px[i] = x + 0.5f - cx > inner - 1.5f ? InsideDeep : Inside;
                _kind[i] = 2;
                continue;
            }
            Color c;
            if (cut && x >= cx)
            {
                // the cut face of the wall: flat, lit clay
                c = Ramp[3];
                if (wallTint is { } tint) c = Color.Lerp(c, tint, 0.7f);
            }
            else
            {
                float n = MathF.Sqrt(MathF.Max(0f, 1f - u * u));
                float light = 0.62f * n - 0.32f * u + 0.06f;
                int k = Math.Clamp((int)(light * Ramp.Length), 0, Ramp.Length - 1);
                c = Ramp[k];
                if (ringPhase % 5 == 0 && MathF.Abs(u) < 0.9f)
                    c = Ramp[Math.Max(0, k - 1)];          // throwing rings
                if (u > -0.62f && u < -0.48f && ringPhase % 6 != 2)
                    c = Color.Lerp(Ramp[Math.Min(Ramp.Length - 1, k + 1)], Sheen, 0.45f + 0.5f * wet);   // the sheen
                if (wet > 0f)
                    c = Color.Lerp(c, Sheen, wet * 0.18f);                         // tired clay goes pale and slick
            }
            _px[i] = c;
            _kind[i] = 1;
        }
    }

    private void Pot(ClayPot pot, float spin)
    {
        int rows = pot.Rows;
        float wet = Math.Clamp(1f - pot.Patience / 0.6f, 0f, 1f);
        if (pot.Collapsed)
        {
            // fallen in: a sagging heap, widest at the foot, the wall folded over at the top
            float widest = 0f;
            for (int i = 0; i < rows; i++)
                widest = MathF.Max(widest, pot.R[i]);
            int h = Math.Max(8, (int)(rows * RowPx * 0.38f));
            for (int yy = 0; yy < h; yy++)
            {
                float u = yy / (float)h;
                float r = widest * UnitPx * (1.08f - 0.55f * u * u) + MathF.Sin(u * 11f) * 1.4f;
                ClayRow(FootY - yy, r, 0f, yy, 0.7f, null, cut: false);
            }
            return;
        }
        int ring = (int)(spin * 3f);
        for (int yy = 0; yy < rows * RowPx; yy++)
        {
            int i = Math.Min(rows - 1, yy / RowPx);
            float f = (yy % RowPx) / (float)RowPx;
            float r = i + 1 < rows ? pot.R[i] * (1 - f) + pot.R[i + 1] * f : pot.R[i];
            float t = i + 1 < rows ? pot.T[i] * (1 - f) + pot.T[i + 1] * f : pot.T[i];
            float fit = PotJudge.WallFit(pot.T[i], i, rows);
            Color? tint = fit >= 0.99f ? null : Color.Lerp(Ramp[3], pot.T[i] < PotJudge.ThinWall ? Thin : Heavy, 1f - fit * 0.5f);
            // the floor: the bottom two art rows stay solid
            ClayRow(FootY - yy, r * UnitPx, yy < 2 ? 0f : (r - t) * UnitPx, yy + ring, wet, tint, cut: true);
        }
    }

    private void Lump(float offset, float spin)
    {
        int cx0 = CentreX;
        float shake = MathF.Sin(spin * 2.3f) * MathF.Min(3f, MathF.Abs(offset)) * 0.5f;
        int dx = (int)MathF.Round((offset + shake) * UnitPx);
        int h = 30;
        for (int yy = 0; yy < h; yy++)
        {
            float u = yy / (float)h;
            float r = (11f - 4f * u * u) * UnitPx;
            // drawn shifted: temporarily move the centre
            int y = FootY - yy;
            for (int x = (int)(cx0 + dx - r); x <= cx0 + dx + r; x++)
            {
                float ux = (x + 0.5f - cx0 - dx) / r;
                if (MathF.Abs(ux) > 1f) continue;
                float n = MathF.Sqrt(1f - ux * ux);
                float top = MathF.Min(1f, (h - yy) / 4f);
                float light = 0.6f * n - 0.3f * ux + 0.1f + 0.15f * (1f - top);
                Color c = Ramp[Math.Clamp((int)(light * Ramp.Length), 0, Ramp.Length - 1)];
                if ((yy + (int)(spin * 3f)) % 5 == 0 && MathF.Abs(ux) < 0.85f) c = Ramp[Math.Max(0, Array.IndexOf(Ramp, c) - 1)];
                if (ux > -0.6f && ux < -0.45f) c = Color.Lerp(c, Sheen, 0.5f);
                Set(x, y, c);
                if (x >= 0 && x < Width && y >= 0 && y < Height) _kind[y * Width + x] = 1;
            }
        }
        // the true centre, a small gold mark on the wheel head
        Set(cx0, Ground - 1, Color.Gold);
        Set(cx0, Ground, Color.Gold);
    }

    // The splash pan's near rim, drawn again in front of the clay's foot so the pot stands in it.
    private void PanFront()
    {
        if (PotteryArt.Wheel is not { } wheel)
            return;
        int wx = CentreX - PotteryArt.WheelHeadX, wy = Ground - 1 - PotteryArt.WheelHeadY;
        for (int y = PotteryArt.WheelHeadBottom + 1; y < wheel.H; y++)
            for (int x = 0; x < wheel.W; x++)
            {
                Color c = wheel.Px[y * wheel.W + x];
                if (c.A > 127)
                    Set(wx + x, wy + y, new Color(c.R, c.G, c.B));
            }
    }

    // Any clay pixel against the background takes a darker shade of itself.
    private void Outline()
    {
        var kind = (byte[])_kind.Clone();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                if (kind[i] != 1)
                    continue;
                bool edge = x == 0 || x == Width - 1 || y == 0
                    || kind[i - 1] == 0 || kind[i + 1] == 0 || kind[i - Width] == 0;
                if (edge)
                {
                    Color c = _px[i];
                    _px[i] = new Color((int)(c.R * 0.6f), (int)(c.G * 0.55f), (int)(c.B * 0.55f));
                }
            }
    }

    private void Rim(ClayPot pot)
    {
        int top = FootY - pot.Rows * RowPx + 1;
        float r = pot.R[pot.Rows - 1] * UnitPx, inner = (pot.R[pot.Rows - 1] - pot.T[pot.Rows - 1]) * UnitPx;
        int cx = CentreX;
        for (int x = (int)(cx - r) + 1; x < cx + r - 1; x++)
        {
            bool hollow = x >= cx && x + 0.5f - cx < inner;
            Set(x, top, hollow ? InsideDeep : Ramp[5]);
        }
    }

    // The shape to match: a dotted chalk line.
    private void Reference(PotShape shape)
    {
        int cx = CentreX;
        for (int yy = 0; yy < shape.Rows * RowPx; yy++)
        {
            int i = Math.Min(shape.Rows - 1, yy / RowPx);
            float f = (yy % RowPx) / (float)RowPx;
            float r = (i + 1 < shape.Rows ? shape.R[i] * (1 - f) + shape.R[i + 1] * f : shape.R[i]) * UnitPx;
            if (yy % 2 != 0)
                continue;
            Set((int)MathF.Round(cx - r) - 1, FootY - yy, Chalk);
            Set((int)MathF.Round(cx + r), FootY - yy, Chalk);
        }
        int top = FootY - shape.Rows * RowPx;
        float rt = shape.R[^1] * UnitPx;
        for (int x = (int)(cx - rt); x <= cx + rt; x += 2)
            Set(x, top, Chalk);
    }

    // Where you're working: two small chalk ticks just outside the pot at that height. (No
    // hands: the user, 2026-11-07, "those hands look bad and block the object".) Centring
    // needs no mark: the mouse is the hand.
    private void Hand(float row, Color c, ClayPot? pot, float handX)
    {
        if (!float.IsNaN(handX) || pot == null || pot.Rows == 0)
            return;
        int y = (int)MathF.Round(FootY - (row + 0.5f) * RowPx);
        float r = pot.R[Math.Clamp((int)row, 0, pot.Rows - 1)] * UnitPx;
        for (int k = 0; k < 4; k++)
        {
            Set((int)(CentreX - r) - 3 - k, y, Chalk);
            Set((int)(CentreX + r) + 3 + k, y, Chalk);
        }
    }
}
#endif
