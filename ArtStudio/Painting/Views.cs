using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArtStudio.Painting;

// Plein air (2026-10-25): an easel set up outdoors offers the view around it
// as a subject. The game renders the whole location (its own map
// screenshot, trees, buildings and all, at 16 pixels a tile); the square of
// it around the easel, mostly what lies ahead of it, is reduced to the
// canvas, each cell the most common of its block's colours in the
// painter's pigments. The whole canvas is the picture, so a view takes
// longer and is worth more than a still life; somewhere far from the farm,
// or in rain, snow or at night, more again.
//
// A view's subject is "view:<location>:<season>", so the same place in
// another season is a new subject.
internal static class Views
{
    public const string Prefix = "view:";
    private const int TilePixels = 16, Tiles = 16;

    public static bool IsView(string? subject) => subject?.StartsWith(Prefix) == true;

    public static string SubjectFor(GameLocation location) => $"{Prefix}{location.NameOrUniqueName}:{location.GetSeasonKey()}";

    public static string PlaceOf(string subject) => subject.Substring(Prefix.Length).Split(':')[0];

    public static string SeasonName(string subject)
    {
        string[] parts = subject.Substring(Prefix.Length).Split(':');
        return parts.Length > 1 && Enum.TryParse(parts[1], true, out StardewValley.Season season) ? Utility.getSeasonNameFromNumber((int)season) : "";
    }

    public static string PlaceName(string subject)
    {
        string name = PlaceOf(subject);
        var location = Game1.getLocationFromName(name);
        return location?.DisplayName ?? name;
    }

    // The square of the location around the easel, in game colours: what the
    // painter copies from (reduced per canvas size by Reference()).
    public sealed class Capture
    {
        public string Subject { get; init; } = "";
        public Color[] Pixels { get; init; } = Array.Empty<Color>();
        public int Size { get; init; }
        public Texture2D? Thumbnail { get; set; }
        public Dictionary<int, Reference> BySize { get; } = new();
    }

    public static Capture? Take(GameLocation location, Vector2 easelTile)
    {
        if (!location.IsOutdoors || location != Game1.currentLocation) return null;
        string name = "museart_view_" + Game1.ticks;
        string? file = Game1.game1.takeMapScreenshot(TilePixels / 64f, name, null);
        if (file == null) return null;
        string path = Path.Combine(Game1.game1.GetScreenshotFolder(), file);
        // The game never closes the file it saved (it leaves the stream to
        // the garbage collector), so collect it: that flushes and closes it.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var shot = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
            int size = Tiles * TilePixels;
            int x0 = Math.Clamp(((int)easelTile.X - Tiles / 2) * TilePixels, 0, Math.Max(0, shot.Width - size));
            int y0 = Math.Clamp(((int)easelTile.Y - Tiles + 3) * TilePixels, 0, Math.Max(0, shot.Height - size));
            int side = Math.Min(size, Math.Min(shot.Width - x0, shot.Height - y0));
            var pixels = new Color[side * side];
            shot.GetData(0, new Rectangle(x0, y0, side, side), pixels, 0, pixels.Length);
            var thumb = new Texture2D(Game1.graphics.GraphicsDevice, side, side);
            thumb.SetData(pixels);
            return new Capture { Subject = SubjectFor(location), Pixels = pixels, Size = side, Thumbnail = thumb };
        }
        finally
        {
            try { File.Delete(path); } catch (IOException) { }
        }
    }

    // The capture reduced to a canvas: each cell the most common colour of
    // its block once matched to the owned pigments, so edges stay crisp.
    public static Reference Reference(Capture capture, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None)
    {
        if (capture.BySize.TryGetValue(canvasSize, out var cached)) return cached;
        var cells = new byte[canvasSize, canvasSize];
        var marks = new Mark[canvasSize, canvasSize];
        var counts = new Dictionary<(byte, Mark), int>();
        float block = capture.Size / (float)canvasSize;
        for (int cx = 0; cx < canvasSize; cx++)
        for (int cy = 0; cy < canvasSize; cy++)
        {
            counts.Clear();
            int x0 = (int)(cx * block), x1 = Math.Max(x0 + 1, (int)((cx + 1) * block));
            int y0 = (int)(cy * block), y1 = Math.Max(y0 + 1, (int)((cy + 1) * block));
            for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
            {
                var p = capture.Pixels[y * capture.Size + x];
                var c = Technique.Nearest(p.R, p.G, p.B, owned, allowed);
                counts[c] = counts.GetValueOrDefault(c) + 1;
            }
            (cells[cx, cy], marks[cx, cy]) = counts.OrderByDescending(kv => kv.Value).First().Key;
        }
        Technique.Limit(cells, marks);
        return capture.BySize[canvasSize] = new Reference(cells, marks);
    }

    // What a view's subject is worth, like an item's price for a still life:
    // the farm and town are everyday, the far corners of the valley are not.
    public static int SubjectPrice(string subject)
    {
        string place = PlaceOf(subject);
        int price = place switch
        {
            "Farm" or "Town" or "BusStop" => 200,
            "Forest" or "Beach" or "Mountain" => 300,
            "Woods" or "Railroad" or "Backwoods" => 450,
            "Summit" or "Desert" => 800,
            _ when place.StartsWith("Island") => 700,
            _ => 350,
        };
        float conditions = 1f;
        if (Game1.IsRainingHere(Game1.getLocationFromName(place))) conditions *= 1.15f;
        if (Game1.season == StardewValley.Season.Winter) conditions *= 1.1f;
        if (Game1.timeOfDay >= 1900) conditions *= 1.15f;
        return (int)(price * conditions);
    }
}
