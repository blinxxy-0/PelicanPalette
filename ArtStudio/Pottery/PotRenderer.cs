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
// and the right half cut away to show the wall. The wheel and the workshop
// behind are placeholders until their PixelLab art (section 11's art list).
internal sealed class PotRenderer : IDisposable
{
    public const int Width = 150, Height = 136, Scale = 4;
    private const int Ground = Height - 26;          // the wheel head's top row

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
    private readonly Color[] _px = new Color[Width * Height];
    private readonly byte[] _kind = new byte[Width * Height];     // 0 none, 1 clay, 2 inside (for outlines)

    public int RowPx { get; private set; } = 4;     // art pixels per row of the pot
    public float UnitPx { get; private set; } = 2.4f;
    public int CentreX => Width / 2;
    public int FootY => Ground - 2;                 // the bottom row of clay, standing on the wheel head

    public PotRenderer() => _texture = new Texture2D(Game1.graphics.GraphicsDevice, Width, Height);

    public void Dispose() => _texture.Dispose();

    // Big enough to fill the scene, with room to pull the pot taller than the shape.
    public void FitTo(PotShape shape)
    {
        RowPx = Math.Clamp((int)(100f / MathF.Max(shape.Rows * 1.35f, 16f)), 3, 7);
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
        if (showReference)
            Reference(shape);
        if (showHand)
            Hand(handRow, handColor, pot, handX);
        _texture.SetData(_px);
        b.Draw(_texture, dest, Color.White);
    }

    private void Set(int x, int y, Color c)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _px[y * Width + x] = c;
    }

    private void Backdrop()
    {
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

    // Your hands (placeholders until their art): a short bar either side at the working height.
    private void Hand(float row, Color c, ClayPot? pot, float handX)
    {
        int y = (int)MathF.Round(FootY - (row + 0.5f) * RowPx);
        if (!float.IsNaN(handX))
        {
            int hx = (int)handX;
            for (int yy = FootY - 34; yy < FootY - 2; yy++)
                Set(hx, yy, new Color(255, 236, 214));
            return;
        }
        float r = pot != null && pot.Rows > 0 ? pot.R[Math.Clamp((int)row, 0, pot.Rows - 1)] * UnitPx : 20f;
        for (int k = 0; k < 7; k++)
        {
            Set((int)(CentreX - r) - 3 - k, y, c);
            Set((int)(CentreX + r) + 3 + k, y, c);
            Set((int)(CentreX - r) - 3 - k, y + 1, Color.Lerp(c, Color.Black, 0.3f));
            Set((int)(CentreX + r) + 3 + k, y + 1, Color.Lerp(c, Color.Black, 0.3f));
        }
    }
}
#endif
