using System.Collections.Generic;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Turns things in the game into references to paint from. A still life is
// the item's own 16×16 sprite, quantized to the pigments the player owns,
// so a perfect copy is always possible — and owning more pigments makes the
// reference richer (and harder, and worth more).
internal static class ReferenceBuilder
{
    public const int SpriteSize = 16;

    public static bool CanPaint(Item item)
    {
        if (item is not SObject obj || obj.bigCraftable.Value || obj is StardewValley.Objects.Furniture)
            return false;
        if (ArtItems.IsOurs(item))
            return false;
        var rect = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).GetSourceRect();
        return rect.Width == SpriteSize && rect.Height == SpriteSize;
    }

    public static byte[,] Subject(string qualifiedItemId, IReadOnlyList<int> owned) =>
        Subject(qualifiedItemId, owned, Techniques.None, out _);

    // With techniques allowed, a shaded pixel may become a glazed colour and
    // a shine a highlight (Core/Technique.cs).
    public static byte[,] Subject(string qualifiedItemId, IReadOnlyList<int> owned, Techniques allowed, out Mark[,] marks)
    {
        ParsedItemData data = ItemRegistry.GetDataOrErrorItem(qualifiedItemId);
        var texture = data.GetTexture();
        var rect = data.GetSourceRect();
        var pixels = new Color[rect.Width * rect.Height];
        texture.GetData(0, rect, pixels, 0, pixels.Length);

        var cells = new byte[rect.Width, rect.Height];
        marks = new Mark[rect.Width, rect.Height];
        for (int x = 0; x < rect.Width; x++)
        for (int y = 0; y < rect.Height; y++)
        {
            var p = pixels[y * rect.Width + x];
            if (p.A < 128)
            {
                cells[x, y] = PixelCanvas.Blank;
                continue;
            }
            (cells[x, y], marks[x, y]) = Technique.Nearest(p.R, p.G, p.B, owned, allowed);
        }
        Technique.Limit(cells, marks);
        return cells;
    }

    // One to three items arranged on the canvas. Later items sit in front.
    // Arrangements on bigger canvases rest on a little table edge so they
    // read as a still life rather than floating stickers.
    public static Reference StillLife(IReadOnlyList<string> itemIds, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None)
    {
        if (itemIds.Count == 1)
            return Reference.Centered(Subject(itemIds[0], owned, allowed, out var subjectMarks), canvasSize, subjectMarks);

        var cells = PixelCanvas.NewBlank(canvasSize);
        var marks = new Mark[canvasSize, canvasSize];
        int tableTop = canvasSize - 5;
        byte tableTopColor = (byte)Palette.Nearest(0xA8, 0x5D, 0x33, owned);
        byte tableColor = (byte)Palette.Nearest(0x6E, 0x4A, 0x2F, owned);
        for (int x = 0; x < canvasSize; x++)
        {
            cells[x, tableTop] = tableTopColor;
            for (int y = tableTop + 1; y < canvasSize - 1; y++)
                cells[x, y] = tableColor;
        }

        var slots = Arrangement(itemIds.Count, canvasSize, tableTop);
        for (int i = 0; i < itemIds.Count; i++)
        {
            var subject = Subject(itemIds[i], owned, allowed, out var subjectMarks);
            var (ox, oy) = slots[i];
            for (int x = 0; x < SpriteSize; x++)
            for (int y = 0; y < SpriteSize; y++)
            {
                byte c = subject[x, y];
                int cx = ox + x, cy = oy + y;
                if (c != PixelCanvas.Blank && cx >= 0 && cy >= 0 && cx < canvasSize && cy < canvasSize)
                {
                    cells[cx, cy] = c;
                    marks[cx, cy] = subjectMarks[x, y];
                }
            }
        }
        return new Reference(cells, marks);
    }

    // Top-left corners for each item, back to front, resting on the table.
    private static (int X, int Y)[] Arrangement(int count, int canvasSize, int tableTop)
    {
        int floor = tableTop - SpriteSize + 2;
        if (count == 2)
            return new[] { (canvasSize / 2 - 13, floor - 5), (canvasSize / 2 - 3, floor) };
        return new[] { (2, floor - 7), (canvasSize - 18, floor - 7), (canvasSize / 2 - 8, floor) };
    }

    public static int MaxItems(int canvasSize) => canvasSize switch
    {
        <= 20 => 1,
        <= 28 => 2,
        _ => 3,
    };
}
