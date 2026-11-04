using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArtStudio.Painting;

// Creatures (1.2): a study of a monster you've slain, or of a Junimo once
// the Community Center (or Joja's) is done, painted from memory. The
// reference is the creature's own sprite, its first frame, in your colours,
// the way a still life is its item's sprite. A creature's subject is
// "creature:<monster name>".
//
// The game doesn't record a monster sprite's frame size anywhere, so each
// first frame is found from the sheet itself: frames are a quarter of the
// sheet wide (a few sheets aren't, below), and the figure ends at the
// first clear row under it (a few touch the next frame, so their height is
// given). Slimes and Junimos are drawn grey and tinted by the game, so
// they're tinted here as the game does by default.
internal sealed record CreatureSpec(string Name, int FrameWidth = 0, int FrameHeight = 0, Color? Tint = null);

internal static class Creatures
{
    public const string Prefix = "creature:";
    public const string Junimo = "Junimo";

    private static readonly Color SlimeGreen = new(110, 200, 60), JunimoGreen = new(120, 210, 90);

    // Monsters by the name the game counts kills under (also their sprite
    // sheet's name), in the order you meet them. Left out: those whose first
    // frame is only a hole or a splash (Duggies, Lava Lurks), and the Spider,
    // whose first frame is its legs.
    public static readonly CreatureSpec[] All =
    {
        new("Green Slime", Tint: SlimeGreen), new("Bug"), new("Grub"), new("Fly"), new("Bat"), new("Rock Crab"),
        new("Dust Spirit"), new("Frost Bat"), new("Ghost"), new("Skeleton"), new("Lava Bat"), new("Lava Crab"),
        new("Metal Head"), new("Shadow Brute"), new("Shadow Shaman", FrameHeight: 24), new("Squid Kid"),
        new("Armored Bug"), new("Carbon Ghost"), new("Putrid Ghost"), new("Skeleton Mage"), new("Stone Golem"),
        new("Wilderness Golem", FrameHeight: 24), new("Big Slime", 32, Tint: SlimeGreen), new("Iridium Crab"),
        new("Mummy"), new("Serpent", 32, 32), new("Pepper Rex", 32), new("Iridium Bat"), new("Haunted Skull", 16, 16),
        new("Royal Serpent", 32, 32), new("Truffle Crab"), new("Blue Squid", 24), new("Dwarvish Sentry", FrameHeight: 32),
        new("Shadow Sniper", 32), new("Assassin Bug"), new("Hot Head"), new("False Magma Cap"),
        new("Magma Sprite", 16, 16), new("Magma Sparker", 16), new("Tiger Slime", Tint: new Color(240, 150, 60)),
        new("Iridium Golem"), new("Stick Bug"),
        new(Junimo, 16, 16, JunimoGreen),
    };

    private static readonly Dictionary<string, CreatureSpec> ByName = All.ToDictionary(c => c.Name);

    public static bool IsCreature(string? subject) => subject?.StartsWith(Prefix) == true;

    public static string SubjectFor(string name) => Prefix + name;

    public static string NameOf(string subject) => subject.Substring(Prefix.Length);

    // ---- Who's been met ----

    public static bool Unlocked(Farmer who, string name) => name == Junimo
        ? Game1.MasterPlayer.hasCompletedCommunityCenter() || Utility.hasFinishedJojaRoute()
        : who.stats.getMonstersKilled(name) > 0;

    public static List<string> Available(Farmer who) => All.Where(c => Unlocked(who, c.Name) && Frame(c.Name) != null).Select(c => c.Name).ToList();

    // ---- Names and worth ----

    public static string DisplayName(string name)
    {
        if (name == Junimo) return I18n.Get("creature.junimo");
        // Data/Monsters: "health/damage/.../displayName", the name last.
        return DataLoader.Monsters(Game1.content).TryGetValue(name, out string? raw) && raw.Split('/') is { Length: > 14 } fields && fields[14].Length > 0
            ? fields[14] : name;
    }

    // Like an item's price for a still life: tougher monsters are worth more.
    public static int SubjectPrice(string subject)
    {
        string name = NameOf(subject);
        if (name == Junimo) return 500;
        int health = DataLoader.Monsters(Game1.content).TryGetValue(name, out string? raw) && int.TryParse(raw.Split('/')[0], out int h) ? h : 50;
        return Math.Clamp(health * 2, 100, 900);
    }

    // ---- The sprite ----

    private static readonly Dictionary<string, (Texture2D Texture, Rectangle Source)?> Frames = new();

    private static Texture2D? Sheet(string name)
    {
        try
        {
            return Game1.content.Load<Texture2D>(name == Junimo ? "Characters/Junimo" : "Characters/Monsters/" + name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // The first frame's figure on its sheet, trimmed to its pixels.
    public static (Texture2D Texture, Rectangle Source)? Frame(string name)
    {
        if (Frames.TryGetValue(name, out var cached)) return cached;
        (Texture2D, Rectangle)? found = null;
        if (ByName.TryGetValue(name, out var spec) && Sheet(name) is { } texture)
        {
            int fw = spec.FrameWidth > 0 ? spec.FrameWidth : texture.Width / 4;
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            bool Row(int y) => Enumerable.Range(0, fw).Any(x => pixels[y * texture.Width + x].A > 0);
            int top = Enumerable.Range(0, texture.Height).FirstOrDefault(Row, -1);
            if (top >= 0)
            {
                int bottom = spec.FrameHeight > 0 ? Math.Min(texture.Height, spec.FrameHeight) : top;
                if (spec.FrameHeight == 0)
                    while (bottom < texture.Height && Row(bottom)) bottom++;
                int left = fw, right = 0, y0 = texture.Height, y1 = 0;
                for (int y = top; y < bottom; y++)
                for (int x = 0; x < fw; x++)
                    if (pixels[y * texture.Width + x].A > 0)
                        (left, right, y0, y1) = (Math.Min(left, x), Math.Max(right, x), Math.Min(y0, y), Math.Max(y1, y));
                if (right >= left)
                    found = (texture, new Rectangle(left, y0, right - left + 1, y1 - y0 + 1));
            }
        }
        Frames[name] = found;
        return found;
    }

    public static Color TintOf(string name) => ByName.TryGetValue(name, out var spec) && spec.Tint is { } tint ? tint : Color.White;

    // Whether the creature fits the canvas whole.
    public static bool Fits(string name, int canvasSize) =>
        Frame(name) is { } frame && frame.Source.Width <= canvasSize - 2 && frame.Source.Height <= canvasSize - 2;

    public static void Draw(SpriteBatch b, string name, Rectangle area, Color? multiply = null)
    {
        if (Frame(name) is { } frame)
            DrawSprite(b, frame.Texture, frame.Source, TintOf(name), area, multiply);
    }

    // A sprite in a slot, as big as fits (shared with animals).
    public static void DrawSprite(SpriteBatch b, Texture2D texture, Rectangle source, Color tint, Rectangle area, Color? multiply = null)
    {
        float scale = Math.Min(area.Width / (float)source.Width, area.Height / (float)source.Height);
        scale = scale >= 2f ? MathF.Floor(scale) : scale;     // (whole steps when there's room; a tall one fills its slot)
        var size = new Vector2(source.Width, source.Height) * scale;
        var at = new Vector2(area.Center.X - size.X / 2, area.Center.Y - size.Y / 2);
        if (multiply is { } m) tint = new Color(tint.ToVector4() * m.ToVector4());
        b.Draw(texture, at, source, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    // A frame trimmed to its figure's pixels (those at least minAlpha), or null if it's empty.
    public static Rectangle? Trim(Texture2D texture, Rectangle frame, int minAlpha = 1)
    {
        frame = Rectangle.Intersect(frame, texture.Bounds);
        if (frame.Width <= 0 || frame.Height <= 0) return null;
        var pixels = new Color[frame.Width * frame.Height];
        texture.GetData(0, frame, pixels, 0, pixels.Length);
        int left = frame.Width, right = -1, top = frame.Height, bottom = -1;
        for (int y = 0; y < frame.Height; y++)
        for (int x = 0; x < frame.Width; x++)
            if (pixels[y * frame.Width + x].A >= minAlpha)
                (left, right, top, bottom) = (Math.Min(left, x), Math.Max(right, x), Math.Min(top, y), Math.Max(bottom, y));
        return right < 0 ? null : new Rectangle(frame.X + left, frame.Y + top, right - left + 1, bottom - top + 1);
    }

    // The reference: the frame's pixels, tinted, in the painter's colours, centred.
    public static Reference Reference(string name, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None)
    {
        var (texture, source) = Frame(name) ?? throw new InvalidOperationException($"No sprite for {name}.");
        return ReferenceFrom(texture, source, TintOf(name), canvasSize, owned, allowed);
    }

    // Any sprite as a reference (shared with animals). With shrink, a sprite
    // too big for the canvas is made smaller to fit, a block of pixels to a
    // cell, each cell taking its block's commonest colour; on the figure's
    // edge, its outline's colour, so the outline stays whole.
    public static Reference ReferenceFrom(Texture2D texture, Rectangle source, Color tint, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None, bool shrink = false, int minAlpha = 128)
    {
        var pixels = new Color[source.Width * source.Height];
        texture.GetData(0, source, pixels, 0, pixels.Length);
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (p.A < minAlpha) pixels[i] = Color.Transparent;
            // The game keeps a see-through pixel's colour already multiplied
            // by its alpha, so a ghost's pale mint read as dull grey-green and
            // its reference came out in Stone and Slate (1.3, from players).
            // What's painted is how it looks over the canvas.
            else if (p.A < 255)
                pixels[i] = ReferenceBuilder.OverCanvas(p);
        }
        int w = source.Width, h = source.Height;
        if (shrink && Math.Max(w, h) > canvasSize - 2)
            (pixels, w, h) = Shrunk(pixels, w, h, canvasSize - 2);
        // On a square of blank cells (the technique limit expects one), centred.
        int side = Math.Max(w, h);
        int ox = (side - w) / 2, oy = side - h;
        var cells = PixelCanvas.NewBlank(side);
        var marks = new Mark[side, side];
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var p = pixels[y * w + x];
            if (p.A < 128) continue;
            (cells[ox + x, oy + y], marks[ox + x, oy + y]) = Technique.Nearest((byte)(p.R * tint.R / 255), (byte)(p.G * tint.G / 255), (byte)(p.B * tint.B / 255), owned, allowed);
        }
        Technique.Limit(cells, marks, owned);
        return Core.Reference.Centered(cells, canvasSize, marks);
    }

    private static (Color[] Pixels, int Width, int Height) Shrunk(Color[] pixels, int w, int h, int fit)
    {
        float scale = fit / (float)Math.Max(w, h);
        int nw = Math.Max(1, (int)MathF.Round(w * scale)), nh = Math.Max(1, (int)MathF.Round(h * scale));
        bool Solid(Color[] px, int pw, int ph, int x, int y) => x >= 0 && y >= 0 && x < pw && y < ph && px[y * pw + x].A >= 128;
        // The sprite's outline: solid pixels next to a clear one.
        bool Edge(int x, int y) => Solid(pixels, w, h, x, y)
            && !(Solid(pixels, w, h, x - 1, y) && Solid(pixels, w, h, x + 1, y) && Solid(pixels, w, h, x, y - 1) && Solid(pixels, w, h, x, y + 1));
        var shrunk = new Color[nw * nh];
        var outline = new Color?[nw * nh];
        for (int y = 0; y < nh; y++)
        for (int x = 0; x < nw; x++)
        {
            int x0 = x * w / nw, x1 = Math.Max(x0 + 1, (x + 1) * w / nw);
            int y0 = y * h / nh, y1 = Math.Max(y0 + 1, (y + 1) * h / nh);
            var counts = new Dictionary<Color, int>();
            var edges = new Dictionary<Color, int>();
            int clear = 0;
            for (int sy = y0; sy < y1; sy++)
            for (int sx = x0; sx < x1; sx++)
            {
                var p = pixels[sy * w + sx];
                if (p.A < 128) clear++;
                else counts[p] = counts.GetValueOrDefault(p) + 1;
                if (Edge(sx, sy)) edges[p] = edges.GetValueOrDefault(p) + 1;
            }
            // Mostly clear stays clear; otherwise the commonest colour.
            if (counts.Count > 0 && counts.Values.Sum() >= clear)
                shrunk[y * nw + x] = counts.OrderByDescending(kv => kv.Value).First().Key;
            if (edges.Count > 0)
                outline[y * nw + x] = edges.OrderByDescending(kv => kv.Value).First().Key;
        }
        // Cells on the shrunk figure's edge take their block's outline colour.
        var result = (Color[])shrunk.Clone();
        for (int y = 0; y < nh; y++)
        for (int x = 0; x < nw; x++)
        {
            bool edge = Solid(shrunk, nw, nh, x, y)
                && !(Solid(shrunk, nw, nh, x - 1, y) && Solid(shrunk, nw, nh, x + 1, y) && Solid(shrunk, nw, nh, x, y - 1) && Solid(shrunk, nw, nh, x, y + 1));
            if (edge && outline[y * nw + x] is { } line)
                result[y * nw + x] = line;
        }
        return (result, nw, nh);
    }
}
