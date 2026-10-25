using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArtStudio.Painting;

// Portraits of villagers (2026-10-25). Anyone you're close to (at least
// PaintingRules.PortraitHearts hearts) will sit for you, on a medium or
// large canvas. The reference is their own portrait (the neutral face,
// 64×64), halved to 32×32 and matched to your colours: the whole portrait
// on a large canvas, head and shoulders on a medium one.
//
// A portrait's subject is "sitter:<internal name>", in the same place a
// still life keeps its items, so commissions and the painting's data carry
// it the same way.
internal static class Sitters
{
    public const string Prefix = "sitter:";
    private const int PortraitSize = 64, Scale = 2;

    public static string SubjectFor(string npc) => Prefix + npc;

    public static bool IsSitter(string? subject) => subject?.StartsWith(Prefix) == true;

    public static string NpcOf(string subject) => subject.Substring(Prefix.Length);

    public static string DisplayName(string npc) =>
        Game1.getCharacterFromName(npc)?.displayName ?? (Game1.characterData.TryGetValue(npc, out var data) ? StardewValley.TokenizableStrings.TokenParser.ParseText(data.DisplayName) : npc);

    public static Texture2D? PortraitTexture(string npc)
    {
        try { return Game1.content.Load<Texture2D>("Portraits/" + npc); }
        catch { return null; }
    }

    public static bool CanSitFor(Farmer who, string npc) =>
        who.getFriendshipHeartLevelForNPC(npc) >= PaintingRules.PortraitHearts
        && Game1.getCharacterFromName(npc) is NPC { CanSocialize: true }
        && PortraitTexture(npc) != null;

    // Everyone who'll sit for the player, closest friends first.
    public static List<string> Available(Farmer who) =>
        who.friendshipData.Keys
            .Where(npc => CanSitFor(who, npc))
            .OrderByDescending(npc => who.friendshipData[npc].Points)
            .ThenBy(npc => npc)
            .ToList();

    public static Reference Portrait(string npc, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None)
    {
        var texture = PortraitTexture(npc) ?? throw new InvalidOperationException($"No portrait for {npc}.");
        var pixels = new Color[PortraitSize * PortraitSize];
        texture.GetData(0, new Rectangle(0, 0, PortraitSize, PortraitSize), pixels, 0, pixels.Length);

        // Each 2×2 block becomes the colour most of its opaque pixels match
        // (after matching to the owned pigments), or stays blank if it's
        // mostly transparent: the outline and features stay crisp, which an
        // average would blur.
        int half = PortraitSize / Scale;
        var small = new byte[half, half];
        var smallMarks = new Mark[half, half];
        var counts = new Dictionary<(byte, Mark), int>();
        for (int x = 0; x < half; x++)
        for (int y = 0; y < half; y++)
        {
            counts.Clear();
            int opaque = 0;
            for (int dx = 0; dx < Scale; dx++)
            for (int dy = 0; dy < Scale; dy++)
            {
                var p = pixels[(y * Scale + dy) * PortraitSize + x * Scale + dx];
                if (p.A < 128) continue;
                opaque++;
                var c = Technique.Nearest(p.R, p.G, p.B, owned, allowed);
                counts[c] = counts.GetValueOrDefault(c) + 1;
            }
            if (opaque < 2)
                small[x, y] = PixelCanvas.Blank;
            else
                (small[x, y], smallMarks[x, y]) = counts.OrderByDescending(kv => kv.Value).First().Key;
        }
        Technique.Limit(small, smallMarks);

        // Centred across; resting on the bottom edge on a large canvas, or
        // cropped at the shoulders on a medium one (the head stays whole).
        var cells = PixelCanvas.NewBlank(canvasSize);
        var marks = new Mark[canvasSize, canvasSize];
        int ox = (canvasSize - half) / 2, oy = Math.Max(0, canvasSize - half);
        for (int x = 0; x < half; x++)
        for (int y = 0; y < half; y++)
        {
            int cx = ox + x, cy = oy + y;
            if (small[x, y] != PixelCanvas.Blank && cx >= 0 && cy >= 0 && cx < canvasSize && cy < canvasSize)
            {
                cells[cx, cy] = small[x, y];
                marks[cx, cy] = smallMarks[x, y];
            }
        }
        return new Reference(cells, marks);
    }
}
