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
        if (Frame(name) is not { } frame) return;
        float scale = Math.Min(area.Width / (float)frame.Source.Width, area.Height / (float)frame.Source.Height);
        scale = scale >= 2f ? MathF.Floor(scale) : scale;     // (whole steps when there's room; a tall one fills its slot)
        var size = new Vector2(frame.Source.Width, frame.Source.Height) * scale;
        var at = new Vector2(area.Center.X - size.X / 2, area.Center.Y - size.Y / 2);
        var tint = TintOf(name);
        if (multiply is { } m) tint = new Color(tint.ToVector4() * m.ToVector4());
        b.Draw(frame.Texture, at, frame.Source, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    // The reference: the frame's pixels, tinted, in the painter's colours, centred.
    public static Reference Reference(string name, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None)
    {
        var (texture, source) = Frame(name) ?? throw new InvalidOperationException($"No sprite for {name}.");
        var pixels = new Color[source.Width * source.Height];
        texture.GetData(0, source, pixels, 0, pixels.Length);
        var tint = TintOf(name);
        // On a square of blank cells (the technique limit expects one), centred.
        int side = Math.Max(source.Width, source.Height);
        int ox = (side - source.Width) / 2, oy = side - source.Height;
        var cells = PixelCanvas.NewBlank(side);
        var marks = new Mark[side, side];
        for (int x = 0; x < source.Width; x++)
        for (int y = 0; y < source.Height; y++)
        {
            var p = pixels[y * source.Width + x];
            if (p.A < 128) continue;
            (cells[ox + x, oy + y], marks[ox + x, oy + y]) = Technique.Nearest((byte)(p.R * tint.R / 255), (byte)(p.G * tint.G / 255), (byte)(p.B * tint.B / 255), owned, allowed);
        }
        Technique.Limit(cells, marks);
        return Core.Reference.Centered(cells, canvasSize, marks);
    }
}
