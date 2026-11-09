#if DEBUG
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ArtStudio.Vale;

// A paint splash across the screen, drawn as a HUD overlay (no cutscene, nothing loaded):
// blots in the palette grow, hold and drip away. It holds the player still while it plays.
internal static class PaintSplash
{
    private sealed record Blot(Vector2 Centre, float Radius, Color Colour, float Delay, float DripSpeed);

    private static List<Blot> _blots = new();
    private static int _ticks, _total;
    private static Action? _done;
    private static bool _heldStill;
    private static Texture2D? _disc;

    public static bool Playing => _total > 0;

    public static void Register(IModHelper helper)
    {
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Finish(false);
    }

    // palette: the view's colours; grand: the eighth find (twice as long, every spot's colours).
    public static void Play(Color[] palette, bool grand, Action? onDone = null)
    {
        if (Playing || palette.Length == 0)
            return;
        var rng = new Random();
        int count = grand ? 20 : 12 + rng.Next(5);
        var view = Game1.uiViewport;
        _blots = new List<Blot>();
        for (int i = 0; i < count; i++)
        {
            float radius = Math.Min(view.Width, view.Height) * (0.12f + 0.16f * (float)rng.NextDouble());
            _blots.Add(new Blot(
                new Vector2(view.Width * (0.08f + 0.84f * (float)rng.NextDouble()), view.Height * (0.08f + 0.84f * (float)rng.NextDouble())),
                radius, palette[rng.Next(palette.Length)], 0.25f * (float)rng.NextDouble(), 0.3f + 0.7f * (float)rng.NextDouble()));
        }
        _total = grand ? 180 : 90;      // ticks: 1.5 s, or 3 s
        _ticks = 0;
        _done = onDone;
        _heldStill = Game1.player.CanMove;
        if (_heldStill)
            Game1.player.CanMove = false;
        Game1.playSound("wand");
    }

    // For the checks (museart_guidetest): ends a splash at once, without its closing box.
    internal static void Stop() => Finish(false);

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Playing)
            return;
        // Stay held for the whole splash, even if something else gives movement back.
        if (_heldStill)
            Game1.player.CanMove = false;
        if (++_ticks >= _total)
            Finish(true);
    }

    private static void Finish(bool callDone)
    {
        var done = _done;
        if (_heldStill && Context.IsWorldReady)
            Game1.player.CanMove = true;
        _heldStill = false;
        _total = 0;
        _blots.Clear();
        _done = null;
        if (callDone)
            done?.Invoke();
    }

    // Grow over the first fifth, hold to three fifths, then drip: slide down and fade.
    private static void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Playing)
            return;
        _disc ??= MakeDisc(e.SpriteBatch.GraphicsDevice);
        float t = _ticks / (float)_total;
        foreach (var blot in _blots)
        {
            float grow = MathHelper.Clamp((t - blot.Delay) / 0.2f, 0f, 1f);
            if (grow <= 0f)
                continue;
            float eased = 1f - (1f - grow) * (1f - grow);
            float drip = MathHelper.Clamp((t - 0.6f) / 0.4f, 0f, 1f);
            float alpha = 0.85f * (1f - drip * drip);
            float size = blot.Radius * 2f * eased;
            int fall = (int)(drip * blot.DripSpeed * Game1.uiViewport.Height * 0.4f);
            var dest = new Rectangle((int)(blot.Centre.X - size / 2), (int)(blot.Centre.Y - size / 2) + fall, (int)size, (int)size);
            e.SpriteBatch.Draw(_disc, dest, blot.Colour * alpha);
            if (fall > 0)       // the drip itself: a narrow tail from where the blot began
            {
                int w = Math.Max(4, (int)(size * 0.18f));
                e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle((int)blot.Centre.X - w / 2, (int)blot.Centre.Y, w, fall), blot.Colour * alpha);
            }
        }
    }

    private static Texture2D MakeDisc(GraphicsDevice device)
    {
        const int size = 64;
        var tex = new Texture2D(device, size, size);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // A slightly lumpy edge, so it reads as a splash and not a circle.
                float dx = x - size / 2f + 0.5f, dy = y - size / 2f + 0.5f;
                float angle = (float)Math.Atan2(dy, dx);
                float edge = size / 2f * (0.88f + 0.12f * (float)Math.Sin(angle * 5f));
                pixels[y * size + x] = dx * dx + dy * dy <= edge * edge ? Color.White : Color.Transparent;
            }
        tex.SetData(pixels);
        return tex;
    }
}
#endif
