#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ArtStudio.Vale;

// A splash of paint across the screen, drawn as a HUD overlay (no cutscene, nothing loaded): a few
// bold splats with spiky edges and flung droplets, in the view's colours, pop in, hold while a few
// drips run down from them, then fade. Pixel art: every splat is drawn at the game's 4x pixel
// scale, in opaque paint, mostly round the middle of the screen so the toolbar and clock stay
// readable. It holds the player still while it plays.
internal static class PaintSplash
{
    private const int Px = 4;                   // screen (UI) pixels per art pixel

    // A splat's shape, made once: its painted pixels and where drips can leave it.
    // Mask: 0 empty, 1 paint, 2 shaded edge (bottom right), 3 gloss.
    private sealed class Shape
    {
        public int Size;
        public int Class;                       // 0 bold, 1 large, 2 medium, 3 small
        public byte[] Mask = Array.Empty<byte>();
        public Rectangle Bounds;                // painted pixels, in art pixels within the frame
        public float Core;                      // the body's radius, in art pixels
        public List<Point> DripRoots = new();   // (column, lowest body row) along the body's bottom
        public Texture2D? Paint, Gloss;
    }

    private sealed class Splat
    {
        public Shape Shape = null!;
        public Vector2 Centre;
        public bool Flip;
        public Color Colour;
        public float Land;                      // tick it lands on
        public List<Drip> Drips = new();
    }

    private sealed record Drip(int Column, int Root, int Width, float Start, float Run, int Length);
    private sealed record Drop(Shape Shape, Vector2 From, Vector2 To, Color Colour, float Land);

    private static readonly Random Rng = new();
    private static List<Shape>? _splatShapes, _dropShapes;
    private static readonly List<Splat> Splats = new();
    private static readonly List<Drop> Drops = new();
    private static int _ticks, _total, _elapsed;
    private static Action? _done;
    private static bool _heldStill, _releaseLater;

    // However it's held (museart_splashhold), no splash lasts more than a second past its time.
    private const int Margin = 60;

    public static bool Playing => _total > 0;

    // For screenshots (museart_splashhold): the next splash stops at this fraction of its time, until
    // released or until Margin runs out. It's cleared when that splash ends, and on return to title.
    internal static float? HoldAt;

    public static void Register(IModHelper helper)
    {
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) =>
        {
            Finish(false);
            HoldAt = null;
            _releaseLater = false;
            DropTextures();
        };
        // A new day starts clean: a splash still going (passing out at 2am) ends without its box.
        helper.Events.GameLoop.DayEnding += (_, _) =>
        {
            if (Playing)
                Finish(false);
            _releaseLater = false;
        };
        DevTools.AddCommand(helper, "museart_splashhold",
            "Holds the next paint splash at a point in its time, for screenshots (0 to 1; 'off' lets it finish). It ends a second past its time whatever the hold, and the hold goes with it. Usage: museart_splashhold 0.2|off",
            (_, args) => HoldAt = args.Length > 0 && float.TryParse(args[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float f) ? MathHelper.Clamp(f, 0f, 1f) : null);
        DevTools.AddCommand(helper, "museart_splashframe",
            "Saves the next frame, world and HUD together, to media/<name>.png (for the splash's screenshots). Usage: museart_splashframe <name>",
            (_, args) => _frameName = args.FirstOrDefault() ?? "splash");
        helper.Events.Display.Rendered += (_, _) => SaveFrame(helper);
        DevTools.AddCommand(helper, "museart_splash",
            "Plays the paint splash without finding anything, to look at it. Usage: museart_splash [<spot id>|grand]",
            (_, args) =>
            {
                if (!Context.IsWorldReady)
                    return;
                string which = args.FirstOrDefault() ?? PaintersGuide.Spots[0].Id;
                if (which == "grand")
                    Play(PaintersGuide.Spots.SelectMany(s => s.Palette).ToArray(), true);
                else
                    Play((PaintersGuide.Spots.FirstOrDefault(s => s.Id == which) ?? PaintersGuide.Spots[0]).Palette, false);
            });
    }

    private static string? _frameName;

    // The world and the HUD are drawn to their own buffers and only put together after this event,
    // so both are read and laid one over the other (the HUD's is premultiplied). museart_frame reads
    // the back buffer instead, which at this point has neither the HUD nor the splash.
    private static void SaveFrame(IModHelper helper)
    {
        if (_frameName == null)
            return;
        string name = _frameName;
        _frameName = null;
        var device = Game1.graphics.GraphicsDevice;
        int w = device.PresentationParameters.BackBufferWidth, h = device.PresentationParameters.BackBufferHeight;
        Color[] Read(RenderTarget2D? target)
        {
            var data = new Color[w * h];
            if (target == null)
                return data;
            var raw = new Color[target.Width * target.Height];
            target.GetData(raw);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    data[y * w + x] = raw[Math.Min(target.Height - 1, y * target.Height / h) * target.Width + Math.Min(target.Width - 1, x * target.Width / w)];
            return data;
        }
        var world = Read(Game1.game1.screen);
        var ui = Read(Game1.game1.uiScreen);
        for (int i = 0; i < world.Length; i++)
        {
            float a = ui[i].A / 255f;
            world[i] = new Color((byte)Math.Min(255, ui[i].R + world[i].R * (1 - a)), (byte)Math.Min(255, ui[i].G + world[i].G * (1 - a)),
                (byte)Math.Min(255, ui[i].B + world[i].B * (1 - a)), (byte)255);
        }
        string dir = System.IO.Path.Combine(helper.DirectoryPath, "media");
        System.IO.Directory.CreateDirectory(dir);
        using var texture = new Texture2D(device, w, h);
        texture.SetData(world);
        using var stream = System.IO.File.Create(System.IO.Path.Combine(dir, name + ".png"));
        texture.SaveAsPng(stream, w, h);
        PaintersGuide.Monitor?.Log($"Splash frame saved: media/{name}.png ({w}x{h}; world {Game1.game1.screen?.Width}x{Game1.game1.screen?.Height}, "
            + $"ui {Game1.game1.uiScreen?.Width}x{Game1.game1.uiScreen?.Height}; splash {(Playing ? $"tick {_ticks}/{_total}, {Splats.Count} splats, {Drops.Count} drops" : "not playing")}).", LogLevel.Info);
    }

    // palette: the view's colours; grand: the eighth find (twice as long, more splats, every spot's colours).
    public static void Play(Color[] palette, bool grand, Action? onDone = null)
    {
        if (Playing || palette.Length == 0)
            return;
        _total = grand ? 180 : 90;      // ticks: 1.5 s, or 3 s
        _ticks = _elapsed = 0;
        _done = onDone;
        Lay(palette, grand);
        _heldStill = Game1.player.CanMove;
        if (_heldStill)
            Game1.player.CanMove = false;
        Game1.playSound("wand");
    }

    // For the checks (museart_guidetest): ends a splash at once, without its closing box.
    internal static void Stop() => Finish(false);

    // For the checks: ends a splash at once, as if its time were up (its closing box shows).
    internal static void FinishNow() => Finish(true);

    // Whether something else has the player now: an event, passing out, or a box or menu. The splash
    // then neither holds them nor lets them go; that's for whatever has them.
    internal static bool OwnedElsewhere() =>
        Game1.eventUp || Game1.player.passedOut || Game1.activeClickableMenu != null || Game1.dialogueUp;

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (_releaseLater && !Playing && Context.IsWorldReady)
            ReleaseLater();
        if (!Playing)
            return;
        // Stay held for the whole splash, even if something else gives movement back, unless an
        // event, passing out or a box has taken the player over.
        if (_heldStill && !OwnedElsewhere())
            Game1.player.CanMove = false;
        if (++_elapsed >= _total + Margin)
        {
            Finish(true);
            return;
        }
        if (HoldAt is float hold && _ticks >= (int)(hold * _total) && _ticks < _total - 1)
            return;
        if (++_ticks >= _total)
            Finish(true);
    }

    // A box or menu came up during the splash: the player is let go once it's closed, unless an
    // event or passing out has them by then (those give movement back themselves).
    private static void ReleaseLater()
    {
        if (Game1.eventUp || Game1.player.passedOut)
            _releaseLater = false;
        else if (!OwnedElsewhere())
        {
            Game1.player.CanMove = true;
            _releaseLater = false;
        }
    }

    // Lets go only of what the splash took: the player it held still, and only if nothing else has
    // them now (a box: once it closes; an event or passing out: never, they let go themselves).
    private static void Finish(bool callDone)
    {
        var done = _done;
        if (_heldStill && Context.IsWorldReady)
        {
            if (!OwnedElsewhere())
                Game1.player.CanMove = true;
            else if (!Game1.eventUp && !Game1.player.passedOut)
                _releaseLater = true;
        }
        _heldStill = false;
        _total = _ticks = _elapsed = 0;
        HoldAt = null;
        Splats.Clear();
        Drops.Clear();
        _done = null;
        if (callDone)
            done?.Invoke();
    }

    // ---- Laying the splash out ----

    // The screen's corners and bands that hold the HUD: the clock and money box (top right), the
    // energy bar (bottom right), the game's messages such as "Journal Updated" (bottom left, and the
    // eighth find always raises one), and the toolbar's band (bottom, or top when the farmer stands low).
    private static List<Rectangle> HudZones(int w, int h) => new()
    {
        new Rectangle(w - 380, 0, 380, 320),
        new Rectangle(w - 140, h - 360, 140, 360),
        new Rectangle(0, h - 260, 380, 260),
        new Rectangle(0, h - 136, w, 136),
        new Rectangle(0, 0, w, 124),
    };

    private static void Lay(Color[] palette, bool grand)
    {
        _splatShapes ??= MakeSplatShapes();
        _dropShapes ??= MakeDropShapes();
        Splats.Clear();
        Drops.Clear();
        var view = Game1.uiViewport;
        int w = view.Width, h = view.Height;
        var zones = HudZones(w, h);
        var centre = new Vector2(w / 2f, h / 2f);

        // Colours: the palette shuffled, dealt round so each splat differs from the last. The grand
        // splash has more colours than splats: its splats take the most varied, vivid ones (so not
        // a run of greys or of golds), and its droplets every colour.
        var all = palette.Distinct().ToList();
        var colours = all.OrderBy(_ => Rng.Next()).ToList();

        // Sizes, big first: one or two bold splats, then smaller ones round them.
        var sizes = grand
            ? new[] { 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3 }
            : new[] { 0, 1, 1, 2, 3, 3 };
        float spreadX = grand ? 0.36f : 0.3f, spreadY = grand ? 0.26f : 0.22f;
        if (colours.Count > sizes.Length)
            colours = Varied(colours, sizes.Length);

        for (int i = 0; i < sizes.Length; i++)
        {
            // Pick a shape of that size, then the best of forty places near the middle: clear
            // of the HUD, and as far from the splats already laid as needed to keep them apart.
            // (If no place fits, a smaller shape is tried.)
            Shape shape = null!;
            bool flip = false;
            int maxDrip = 0;
            Vector2? best = null;
            float bestScore = float.MinValue;
            for (int tries = 0; tries < 160; tries++)
            {
                if (tries % 40 == 0)
                {
                    if (best != null)
                        break;
                    int size = Math.Min(3, sizes[i] + tries / 40);
                    var choices = _splatShapes.Where(s => s.Class == size).ToList();
                    shape = choices[Rng.Next(choices.Count)];
                    flip = Rng.Next(2) == 0;
                    maxDrip = (int)(shape.Core * 2.6f);
                }
                var at = Snap(centre + new Vector2(Gauss() * spreadX * w, Gauss() * spreadY * h));
                var foot = Footprint(shape, at, flip, 1.15f);
                int dripEnd = (int)at.Y + (int)((shape.DripRoots.Max(r => r.Y) - shape.Size / 2f + maxDrip + 3) * Px);
                var withDrips = new Rectangle(foot.X, foot.Y, foot.Width, Math.Max(foot.Bottom, dripEnd) - foot.Y);
                if (foot.Left < 8 || foot.Right > w - 8 || foot.Top < 8 || withDrips.Bottom > h - 8 || zones.Any(z => z.Intersects(withDrips)))
                    continue;
                float apart = Splats.Count == 0 ? 1f
                    : Splats.Min(o => Vector2.Distance(o.Centre, at) / ((o.Shape.Core + shape.Core) * Px * 1.6f));
                float score = Math.Min(apart, 1f) - 0.15f * Vector2.Distance(at, centre) / w;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = at;
                }
            }
            if (best == null)
                continue;

            float land = grand
                ? (i < 4 ? 0 : i < 8 ? 26 : 52) + (i % 4) * 3 + Rng.Next(3)
                : i * 3 + Rng.Next(3);
            var splat = new Splat
            {
                Shape = shape,
                Centre = best.Value,
                Flip = flip,
                Colour = colours[i % colours.Count],
                Land = land,
            };
            // Drips from most splats: one or two, from the body's lower edge, running slowly.
            int drips = sizes[i] < 3 || Rng.NextDouble() < 0.5 ? 1 + (Rng.NextDouble() < 0.4 ? 1 : 0) : 0;
            var roots = shape.DripRoots.OrderBy(_ => Rng.Next()).Take(drips).ToList();
            foreach (var root in roots)
            {
                int col = flip ? shape.Size - 1 - root.X : root.X;
                float start = land + (grand ? 20 : 14) + Rng.Next(grand ? 30 : 12);
                float run = _total * 0.82f - start;
                splat.Drips.Add(new Drip(col, root.Y, shape.Core >= 14 ? 4 : 3, start, run,
                    (int)(maxDrip * (0.5f + 0.5f * (float)Rng.NextDouble()))));
            }
            Splats.Add(splat);

            // Droplets flung from it, out past its own spatter, some in the other colours.
            int flung = grand ? 3 : 2 + Rng.Next(2);
            for (int d = 0; d < flung; d++)
            {
                double angle = Rng.NextDouble() * Math.PI * 2;
                float dist = shape.Core * Px * (2.3f + 1.4f * (float)Rng.NextDouble());
                var to = Snap(best.Value + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dist);
                var drop = _dropShapes[Rng.Next(_dropShapes.Count)];
                var r = new Rectangle((int)to.X - drop.Size * Px / 2, (int)to.Y - drop.Size * Px / 2, drop.Size * Px, drop.Size * Px);
                if (r.Left < 0 || r.Right > w || r.Top < 0 || r.Bottom > h || zones.Any(z => z.Intersects(r)))
                    continue;
                var colour = Rng.NextDouble() < 0.6 ? splat.Colour : all[Rng.Next(all.Count)];
                Drops.Add(new Drop(drop, best.Value, to, colour, land));
            }
        }
    }

    // Picks n colours, each in turn the one furthest from those already picked, weighted to the vivid.
    private static List<Color> Varied(List<Color> from, int n)
    {
        static float Chroma(Color c) => (Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B))) / 255f;
        static float Apart(Color a, Color b) => Vector3.Distance(a.ToVector3(), b.ToVector3());
        var picked = new List<Color> { from.OrderByDescending(Chroma).First() };
        while (picked.Count < n)
            picked.Add(from.Where(c => !picked.Contains(c)).OrderByDescending(c => picked.Min(p => Apart(c, p)) * (0.35f + Chroma(c))).First());
        return picked.OrderBy(_ => Rng.Next()).ToList();
    }

    private static float Gauss() => ((float)Rng.NextDouble() + (float)Rng.NextDouble() + (float)Rng.NextDouble()) / 1.5f - 1f;

    private static Vector2 Snap(Vector2 v) => new((float)Math.Round(v.X / Px) * Px, (float)Math.Round(v.Y / Px) * Px);

    // Where a splat's paint lies on screen at a given scale.
    private static Rectangle Footprint(Shape shape, Vector2 centre, bool flip, float scale)
    {
        float half = shape.Size / 2f;
        int left = flip ? shape.Size - shape.Bounds.Right : shape.Bounds.Left;
        float x0 = centre.X + (left - half) * Px * scale, y0 = centre.Y + (shape.Bounds.Top - half) * Px * scale;
        return new Rectangle((int)x0, (int)y0, (int)(shape.Bounds.Width * Px * scale), (int)(shape.Bounds.Height * Px * scale));
    }

    // ---- Drawing ----

    private static void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Playing || _splatShapes == null || _dropShapes == null)
            return;
        var b = e.SpriteBatch;
        var device = b.GraphicsDevice;
        foreach (var s in _splatShapes.Concat(_dropShapes))
            EnsureTextures(s, device);

        float t = _ticks;
        float fadeFrom = _total * 0.82f;
        float alpha = t < fadeFrom ? 1f : 1f - (t - fadeFrom) / (_total - fadeFrom);
        alpha = MathHelper.Clamp(alpha, 0f, 1f);
        if (alpha <= 0f)
            return;

        // Droplets first (flying out as their splat lands), then the splats with their drips.
        foreach (var d in Drops)
        {
            float p = (t - d.Land) / 6f;
            if (p <= 0f)
                continue;
            float eased = 1f - (1f - Math.Min(p, 1f)) * (1f - Math.Min(p, 1f));
            var at = p >= 1f ? d.To : Vector2.Lerp(d.From, d.To, eased);
            DrawShape(b, d.Shape, at, false, 1f, d.Colour, alpha);
        }

        foreach (var s in Splats)
        {
            float p = (t - s.Land) / 7f;
            if (p <= 0f)
                continue;
            // The pop: in fast, a touch too big, then settling to its size (whole art pixels from then on).
            float scale = p >= 1f ? 1f
                : p < 0.55f ? MathHelper.Lerp(0.35f, 1.2f, 1f - (1f - p / 0.55f) * (1f - p / 0.55f))
                : MathHelper.Lerp(1.2f, 1f, (p - 0.55f) / 0.45f);
            foreach (var drip in s.Drips)
                DrawDrip(b, s, drip, t, alpha);
            DrawShape(b, s.Shape, s.Centre, s.Flip, scale, s.Colour, alpha);
        }
    }

    private static void DrawShape(SpriteBatch b, Shape shape, Vector2 centre, bool flip, float scale, Color colour, float alpha)
    {
        if (shape.Paint == null || shape.Gloss == null)
            return;
        float half = shape.Size / 2f;
        var origin = new Vector2(half, half);
        var effects = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        var pos = scale == 1f ? Snap(centre) : centre;
        b.Draw(shape.Paint, pos, null, colour * alpha, 0f, origin, Px * scale, effects, 1f);
        b.Draw(shape.Gloss, pos, null, Gloss(colour) * alpha, 0f, origin, Px * scale, effects, 1f);
    }

    // A drip: a narrow run of paint from the body's lower edge, slowing as it goes, with a bead at
    // its end and its right side in shade, like the splats' own edges.
    private static void DrawDrip(SpriteBatch b, Splat s, Drip d, float t, float alpha)
    {
        float u = (t - d.Start) / Math.Max(1f, d.Run);
        if (u <= 0f)
            return;
        u = Math.Min(u, 1f);
        int length = (int)(d.Length * (1f - (1f - u) * (1f - u) * (1f - u)));
        float half = s.Shape.Size / 2f;
        int x = (int)(s.Centre.X + (d.Column - half) * Px) - (d.Width / 2) * Px;
        int y = (int)(s.Centre.Y + (d.Root - half) * Px) - Px;       // tucked one pixel into the body
        var paint = s.Colour * alpha;
        var shade = Shade(s.Colour) * alpha;
        int total = length + 1;
        if (total <= 1)
            return;
        b.Draw(Game1.staminaRect, new Rectangle(x, y, d.Width * Px, total * Px), paint);
        b.Draw(Game1.staminaRect, new Rectangle(x + (d.Width - 1) * Px, y + 2 * Px, Px, Math.Max(0, total - 2) * Px), shade);
        // The bead: a pixel wider each side for two rows, its lower right in shade.
        int by = y + total * Px - Px;
        b.Draw(Game1.staminaRect, new Rectangle(x - Px, by - Px, (d.Width + 2) * Px, 2 * Px), paint);
        b.Draw(Game1.staminaRect, new Rectangle(x, by + Px, d.Width * Px, Px), paint);
        b.Draw(Game1.staminaRect, new Rectangle(x + d.Width * Px, by, Px, Px), shade);
        b.Draw(Game1.staminaRect, new Rectangle(x, by + Px, d.Width * Px, Px), shade);
        b.Draw(Game1.staminaRect, new Rectangle(x - Px, by - Px, Px, Px), Gloss(s.Colour) * alpha);
    }

    private static Color Shade(Color c) => new((int)(c.R * 0.7f), (int)(c.G * 0.7f), (int)(c.B * 0.72f), 255);

    private static Color Gloss(Color c) => Color.Lerp(c, Color.White, 0.55f);

    // ---- The shapes ----

    // Eight splats in four sizes, the same every time (a fixed seed), each a round body with
    // spikes thrown out of it, beads on some spike tips and spatter round it.
    private static List<Shape> MakeSplatShapes()
    {
        var rng = new Random(1607);
        var shapes = new List<Shape>();
        float[] cores = { 20f, 18f, 15f, 14f, 11.5f, 11f, 8.5f, 8f };
        for (int i = 0; i < cores.Length; i++)
        {
            var shape = MakeSplat(rng, cores[i]);
            shape.Class = i / 2;
            shapes.Add(shape);
        }
        return shapes;
    }

    private static List<Shape> MakeDropShapes()
    {
        return new List<Shape> { MakeDrop(1f), MakeDrop(2f), MakeDrop(2.5f), MakeDrop(3f) };
    }

    private static Shape MakeSplat(Random rng, float core)
    {
        int size = (int)Math.Ceiling(core * 5.2f) | 1;
        float c = size / 2f;
        var paint = new bool[size * size];
        var body = new bool[size * size];
        float p1 = (float)(rng.NextDouble() * 6.28), p2 = (float)(rng.NextDouble() * 6.28), p3 = (float)(rng.NextDouble() * 6.28);
        int spikes = 9 + rng.Next(6);
        var spike = new (float Angle, float Length, float Width)[spikes];
        for (int k = 0; k < spikes; k++)
            spike[k] = ((float)(k * Math.PI * 2 / spikes + (rng.NextDouble() - 0.5) * 0.5),
                core * (0.3f + 0.85f * (float)rng.NextDouble()),
                0.09f + 0.12f * (float)rng.NextDouble());

        float Edge(float a) => core * (1f + 0.1f * (float)Math.Sin(3 * a + p1) + 0.07f * (float)Math.Sin(5 * a + p2) + 0.04f * (float)Math.Sin(8 * a + p3));
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = (float)Math.Sqrt(dx * dx + dy * dy), a = (float)Math.Atan2(dy, dx);
                float edge = Edge(a), reach = edge;
                foreach (var s in spike)
                {
                    float off = Math.Abs(MathHelper.WrapAngle(a - s.Angle));
                    if (off < s.Width)
                        reach = Math.Max(reach, edge + s.Length * (float)Math.Pow(1f - off / s.Width, 1.5));
                }
                if (d <= reach)
                    paint[y * size + x] = true;
                if (d <= edge)
                    body[y * size + x] = true;
            }

        void Dot(float cx, float cy, float r)
        {
            for (int y = (int)(cy - r - 1); y <= (int)(cy + r + 1); y++)
                for (int x = (int)(cx - r - 1); x <= (int)(cx + r + 1); x++)
                {
                    if (x < 1 || y < 1 || x >= size - 1 || y >= size - 1)
                        continue;
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r + 0.25f)
                        paint[y * size + x] = true;
                }
        }
        // Beads past some spike tips, and spatter round the splat.
        foreach (var s in spike)
            if (rng.NextDouble() < 0.5)
            {
                float dist = Edge(s.Angle) + s.Length + 1.5f + 2.5f * (float)rng.NextDouble();
                Dot(c + (float)Math.Cos(s.Angle) * dist, c + (float)Math.Sin(s.Angle) * dist, (0.7f + 0.9f * (float)rng.NextDouble()) * core / 10f);
            }
        int spatter = 5 + rng.Next(6);
        for (int k = 0; k < spatter; k++)
        {
            double a = rng.NextDouble() * Math.PI * 2;
            float dist = core * (1.4f + 1.0f * (float)rng.NextDouble());
            Dot(c + (float)Math.Cos(a) * dist, c + (float)Math.Sin(a) * dist, (0.5f + 0.8f * (float)rng.NextDouble()) * core / 10f);
        }

        var shape = new Shape { Size = size, Core = core };
        shape.Mask = Shade(paint, size);
        // Gloss: a bright crescent high on the body's upper left and a fleck beside it, like light
        // on wet paint.
        float gx = c - core * 0.3f, gy = c - core * 0.32f, gr = core * 0.42f, cut = Math.Max(1.5f, core * 0.14f);
        bool InGloss(float x, float y, float ox, float oy)
        {
            float dx = (x - ox) / 1.2f, dy = y - oy;
            return dx * dx + dy * dy <= gr * gr;
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                bool arc = InGloss(px, py, gx, gy) && !InGloss(px, py, gx + cut, gy + cut);
                float fx = px - (c + core * 0.12f), fy = py - (c - core * 0.62f);
                bool fleck = fx * fx + fy * fy <= Math.Max(0.6f, core * 0.07f) * Math.Max(0.6f, core * 0.07f) + 0.5f;
                if ((arc || fleck) && body[y * size + x] && shape.Mask[y * size + x] == 1)
                    shape.Mask[y * size + x] = 3;
            }
        shape.Bounds = BoundsOf(shape.Mask, size);
        // Drip roots: columns across the body's lower half, at the lowest body pixel of each.
        for (int x = (int)(c - core * 0.6f); x <= (int)(c + core * 0.6f); x += 2)
        {
            int y = (int)c;
            while (y + 1 < size && body[(y + 1) * size + x])
                y++;
            shape.DripRoots.Add(new Point(x, y));
        }
        return shape;
    }

    // A round drop: an even width centres on a pixel corner and an odd one on a pixel, so each
    // reads round at its size (2: a square, 4: corners cut, 5 and 6: round).
    private static Shape MakeDrop(float r)
    {
        int size = (int)Math.Round(r * 2) + 2;
        float c = size / 2f;
        var paint = new bool[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                paint[y * size + x] = dx * dx + dy * dy <= r * r;
            }
        var shape = new Shape { Size = size, Core = r, Mask = Shade(paint, size) };
        shape.Bounds = BoundsOf(shape.Mask, size);
        return shape;
    }

    // Paint pixels with open space below or to the right (and paint above or to the left, so thin
    // spikes and specks keep their colour) take the shade.
    private static byte[] Shade(bool[] paint, int size)
    {
        var mask = new byte[size * size];
        bool At(int x, int y) => x >= 0 && y >= 0 && x < size && y < size && paint[y * size + x];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (!At(x, y))
                    continue;
                bool open = !At(x + 1, y) || !At(x, y + 1) || !At(x + 1, y + 1);
                bool backed = At(x - 1, y) && At(x, y - 1);
                mask[y * size + x] = (byte)(open && backed ? 2 : 1);
            }
        return mask;
    }

    private static Rectangle BoundsOf(byte[] mask, int size)
    {
        int l = size, t = size, r = -1, btm = -1;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (mask[y * size + x] != 0)
                {
                    l = Math.Min(l, x); r = Math.Max(r, x);
                    t = Math.Min(t, y); btm = Math.Max(btm, y);
                }
        return r < 0 ? Rectangle.Empty : new Rectangle(l, t, r - l + 1, btm - t + 1);
    }

    // The textures are made from the masks on the device in use, and made again if it was reset
    // or replaced, or after a return to the title.
    private static void EnsureTextures(Shape s, GraphicsDevice device)
    {
        if (s.Paint != null && !s.Paint.IsDisposed && s.Paint.GraphicsDevice == device
            && s.Gloss != null && !s.Gloss.IsDisposed)
            return;
        s.Paint?.Dispose();
        s.Gloss?.Dispose();
        var paint = new Color[s.Mask.Length];
        var gloss = new Color[s.Mask.Length];
        var shadeGrey = new Color(178, 178, 178, 255);
        for (int i = 0; i < s.Mask.Length; i++)
        {
            switch (s.Mask[i])
            {
                case 1: paint[i] = Color.White; break;
                case 2: paint[i] = shadeGrey; break;
                case 3: paint[i] = Color.White; gloss[i] = Color.White; break;
            }
        }
        s.Paint = new Texture2D(device, s.Size, s.Size);
        s.Paint.SetData(paint);
        s.Gloss = new Texture2D(device, s.Size, s.Size);
        s.Gloss.SetData(gloss);
    }

    private static void DropTextures()
    {
        foreach (var s in (_splatShapes ?? new()).Concat(_dropShapes ?? new()))
        {
            s.Paint?.Dispose();
            s.Gloss?.Dispose();
            s.Paint = s.Gloss = null;
        }
    }
}
#endif
