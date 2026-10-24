using System;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Sable's portrait of the farmer (the 10-heart event): the farmer as they
// look that day, from their own sprite (hair, skin, clothes, hat), painted in
// every pigment, head and shoulders on the large canvas against a backdrop.
// It's an ordinary painting underneath, so it frames, hangs and saves like
// the player's own.
internal static class FarmerPortrait
{
    public const string SubjectPrefix = "portrait:";
    private const int Price = 2500;

    public static bool IsPortrait(string subject) => subject.StartsWith(SubjectPrefix);

    public static string SitterName(string subject) => subject.Substring(SubjectPrefix.Length);

    public static SObject Create(Farmer who)
    {
        int size = PaintingRules.CanvasSize(CanvasKind.Large);
        var sprite = RenderFront(who);
        var cells = PixelCanvas.NewBlank(size);
        var all = Enumerable.Range(0, Palette.All.Length).ToArray();

        // A plain slate backdrop with a band of umber below (a chair back).
        byte slate = (byte)Palette.IndexOf("slate"), umber = (byte)Palette.IndexOf("umber");
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
            cells[x, y] = y < size - 6 ? slate : umber;

        // Head and shoulders at twice the sprite's size, centred.
        int top = Enumerable.Range(0, sprite.GetLength(1)).First(y => Enumerable.Range(0, 16).Any(x => sprite[x, y].A >= 128));
        int ox = (size - 32) / 2, oy = 2;
        for (int sx = 0; sx < 16; sx++)
        for (int sy = top; sy < top + (size - oy) / 2; sy++)
        {
            if (sy >= sprite.GetLength(1)) break;
            var p = sprite[sx, sy];
            if (p.A < 128) continue;
            byte c = (byte)Palette.Nearest(p.R, p.G, p.B, all);
            for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < 2; dy++)
            {
                int cx = ox + sx * 2 + dx, cy = oy + (sy - top) * 2 + dy;
                if (cx < size && cy < size)
                    cells[cx, cy] = c;
            }
        }

        var data = new PaintingData
        {
            Size = size,
            Cells = cells,
            Purity = new Purity[size, size],
            Subject = SubjectPrefix + who.Name,
            Stars = 5,
            Painter = Game1.getCharacterFromName("Sable")?.displayName ?? "Sable",
            Date = I18n.Get("painting.date", new { season = Utility.getSeasonNameFromNumber(Game1.seasonIndex), day = Game1.dayOfMonth, year = Game1.year }),
        };
        return PaintingItems.Create(data, SObject.bestQuality, Price);
    }

    // The farmer's standing front view as 16 pixels wide (by 40 tall, with
    // room above for a hat), drawn the way the game draws them, at 4x, then
    // sampled back down to one pixel per sprite pixel.
    private static Color[,] RenderFront(Farmer who)
    {
        const int scale = 4, width = 16, height = 40, headroom = 8;
        var device = Game1.graphics.GraphicsDevice;
        var previous = device.GetRenderTargets();
        using var target = new RenderTarget2D(device, width * scale, height * scale);
        using var batch = new SpriteBatch(device);
        try
        {
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
            who.FarmerRenderer.draw(batch, new FarmerSprite.AnimationFrame(0, 0, false, false), 0, new Rectangle(0, 0, 16, 32),
                new Vector2(0, headroom * scale), Vector2.Zero, 0.5f, 2, Color.White, 0f, 1f, who);
            batch.End();
        }
        finally
        {
            device.SetRenderTargets(previous);
        }

        var pixels = new Color[width * scale * height * scale];
        target.GetData(pixels);
        var sprite = new Color[width, height];
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            sprite[x, y] = pixels[(y * scale + scale / 2) * width * scale + x * scale + scale / 2];
        return sprite;
    }
}
