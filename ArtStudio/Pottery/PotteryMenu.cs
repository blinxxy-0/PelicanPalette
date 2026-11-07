#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Pottery;

// The pottery prototype's wheel (docs/design/pottery_minigame.md, section 10):
// plain shapes and no art, for feeling out the rules before anything else.
// Centring, then throwing against a reference profile, then the stars.
// museart_pottery [cup|bowl|vase|jug] [simplified|normal|hard]
internal sealed class PotteryMenu : IClickableMenu
{
    private enum Stage { Centring, Throwing, Results }

    private const int W = 1000, H = 660;
    // drawn as big as the shape allows, with room to pull it taller than the reference
    private int RowPx => (int)Math.Clamp(540f / MathF.Max(_shape.Rows * 1.4f, 18f), 8f, 26f);
    private int UnitPx => (int)MathF.Min(RowPx * 0.85f, 11f);
    private static readonly Color Clay = new(156, 98, 70), ClayDark = new(110, 64, 44), Hollow = new(58, 34, 26);
    private static readonly Color Panel = new(255, 236, 200), Ink = new(86, 22, 12);

    private readonly PotteryRules _rules;
    private readonly string _shapeName;
    private Stage _stage = Stage.Centring;
    private ClayPot _pot;
    private PotShape _shape;
    private ThrowMove _move = ThrowMove.PullUp;
    private PotResult? _result;
    private float _spin;

    // centring
    private float _wobble, _wobbleV, _centredFor, _centringTime, _centring;
    private readonly Random _random = new();

    private readonly Dictionary<string, Rectangle> _buttons = new();

    public PotteryMenu(string shape, PotteryRules rules, bool skipCentring = false, bool bot = false)
        : base(Game1.uiViewport.Width / 2 - W / 2, Game1.uiViewport.Height / 2 - H / 2, W, H, showUpperRightCloseButton: true)
    {
        _rules = rules;
        _shapeName = Array.IndexOf(PotShape.Names, shape) >= 0 ? shape : PotShape.Names[new Random().Next(PotShape.Names.Length)];
        _pot = new ClayPot(rules);
        _shape = PotShape.Make(_shapeName, _pot.Volume);
        Restart();
        if (skipCentring || bot)
        {
            _centring = 1f;
            _stage = Stage.Throwing;
        }
        if (bot)
        {
            Bot(_pot, _shape);
            Finish();
        }
    }

    // The simulation's greedy potter (for checking the rules): sweep pulls from the foot
    // until it's tall enough, then press in or let out row by row towards the reference,
    // resting whenever the clay tires.
    private static void Bot(ClayPot pot, PotShape shape)
    {
        const float dt = 1f / 60f;
        void RestIfTired()
        {
            if (pot.Patience < 0.35f)
                for (int k = 0; k < 6000 && pot.Patience < 0.9f; k++)
                    pot.Rest(dt);
        }
        for (int pass = 0; pass < 200 && pot.Rows < shape.Rows && !pot.Collapsed; pass++)
            for (float at = 0; at < pot.Rows && pot.Rows < shape.Rows && !pot.Collapsed; at += 0.25f)
            {
                RestIfTired();
                pot.Work(ThrowMove.PullUp, at, dt * 6);
            }
        for (int sweep = 0; sweep < 40 && !pot.Collapsed; sweep++)
        {
            float worst = 0;
            for (int i = 0; i < Math.Min(pot.Rows, shape.Rows) && !pot.Collapsed; i++)
            {
                float diff = shape.R[i] - pot.R[i];
                worst = MathF.Max(worst, MathF.Abs(diff));
                if (MathF.Abs(diff) > 0.3f)
                {
                    RestIfTired();
                    pot.Work(diff > 0 ? ThrowMove.LetOut : ThrowMove.PressIn, i, dt * 3);
                }
            }
            if (worst < 0.4f)
                break;
        }
    }

    private void Restart()
    {
        _pot = new ClayPot(_rules);
        _stage = Stage.Centring;
        _wobble = (_random.Next(2) == 0 ? -1 : 1) * 6f;
        _wobbleV = 0;
        _centredFor = _centringTime = 0;
        _result = null;
        _move = ThrowMove.PullUp;
    }

    private int Cx => xPositionOnScreen + 320;
    private int Base => yPositionOnScreen + H - 70;
    private int PanelX => xPositionOnScreen + 650;

    public override void update(GameTime time)
    {
        base.update(time);
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        _spin += dt * 9f;
        var mouse = Mouse.GetState();
        bool held = mouse.LeftButton == ButtonState.Pressed && InPotArea(Game1.getMouseX(), Game1.getMouseY());

        switch (_stage)
        {
            case Stage.Centring:
                UpdateCentring(dt, held);
                break;
            case Stage.Throwing:
                if (held)
                    _pot.Work(_move, RowAt(Game1.getMouseY()), dt);
                else
                    _pot.Rest(dt);
                if (_pot.Collapsed)
                    Finish();
                break;
        }
    }

    // Keep your hands on the lump (the mouse, held) and it settles; let go and it wanders.
    private void UpdateCentring(float dt, bool held)
    {
        _centringTime += dt;
        float hand = (Game1.getMouseX() - Cx) / (float)UnitPx;
        bool onIt = held && MathF.Abs(hand - _wobble) < _rules.CentringBand;
        if (onIt)
        {
            _wobbleV += -_wobble * 4f * dt;               // your hands pull it true
            _wobbleV *= MathF.Pow(0.15f, dt);
        }
        else
            _wobbleV += (MathF.Sign(_wobble == 0 ? 1 : _wobble) * 3f + (float)(_random.NextDouble() - 0.5) * 18f) * dt;
        _wobbleV += (float)(_random.NextDouble() - 0.5) * 10f * dt;   // it never quite stops trying
        _wobble = Math.Clamp(_wobble + _wobbleV * dt, -12f, 12f);
        _centredFor = MathF.Abs(_wobble) < 0.8f ? _centredFor + dt : 0f;
        if (_centredFor > 1.5f || _centringTime > 14f)
        {
            _centring = Math.Clamp(1f - MathF.Abs(_wobble) / 8f, 0f, 1f);
            _stage = Stage.Throwing;
        }
    }

    private void Finish()
    {
        _result = PotJudge.Judge(_pot, _shape, _centring);
        _stage = Stage.Results;
    }

    private bool InPotArea(int x, int y) =>
        x >= xPositionOnScreen + 20 && x < PanelX - 10 && y >= yPositionOnScreen + 20 && y < Base + 30;

    private float RowAt(int y) => (Base - y) / (float)RowPx - 0.5f;

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        foreach (var (key, rect) in _buttons)
        {
            if (!rect.Contains(x, y))
                continue;
            Game1.playSound("smallSelect");
            switch (key)
            {
                case "pull": _move = ThrowMove.PullUp; break;
                case "press": _move = ThrowMove.PressIn; break;
                case "out": _move = ThrowMove.LetOut; break;
                case "done": Finish(); break;
                case "again": Restart(); break;
                case "next":
                    _shape = PotShape.Make(PotShape.Names[(Array.IndexOf(PotShape.Names, _shape.Name) + 1) % PotShape.Names.Length], _pot.Volume);
                    Restart();
                    break;
            }
            return;
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);
        if (key == Keys.D1) _move = ThrowMove.PullUp;
        if (key == Keys.D2) _move = ThrowMove.PressIn;
        if (key == Keys.D3) _move = ThrowMove.LetOut;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, W, H, Color.White);
        _buttons.Clear();

        DrawWheel(b);
        if (_stage == Stage.Centring)
            DrawLump(b);
        else
            DrawPot(b);
        if (_stage != Stage.Centring)
            DrawReference(b);
        DrawPanel(b);
        base.draw(b);
        drawMouse(b);
    }

    private void Fill(SpriteBatch b, int x, int y, int w, int h, Color c) =>
        b.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), c);

    private void DrawWheel(SpriteBatch b)
    {
        Fill(b, Cx - 230, Base, 460, 14, new Color(120, 120, 128));
        Fill(b, Cx - 230, Base + 14, 460, 6, new Color(80, 80, 88));
        // spokes moving, to show it turning
        for (int k = 0; k < 8; k++)
        {
            float u = ((_spin * 0.15f + k / 8f) % 1f);
            int x = Cx - 230 + (int)(u * 460);
            Fill(b, x, Base + 2, 3, 10, new Color(150, 150, 158));
        }
    }

    private void DrawLump(SpriteBatch b)
    {
        float shake = MathF.Sin(_spin * 2.3f) * MathF.Min(3f, MathF.Abs(_wobble)) * 0.6f;
        int cx = Cx + (int)((_wobble + shake) * UnitPx);
        for (int i = 0; i < 9; i++)
        {
            float r = 10f - i * 0.35f;
            int w = (int)(r * UnitPx * 2);
            Fill(b, cx - w / 2, Base - (i + 1) * RowPx, w, RowPx, i % 2 == 0 ? Clay : ClayDark * 0.9f);
        }
        // your hands
        int hx = Game1.getMouseX();
        Fill(b, hx - 2, Base - 9 * RowPx - 20, 4, 9 * RowPx + 20, Color.White * 0.7f);
        // the true centre
        Fill(b, Cx - 1, Base - 9 * RowPx - 30, 2, 20, Color.Gold);
    }

    private void DrawPot(SpriteBatch b)
    {
        var pot = _pot;
        Color clay = pot.Collapsed ? new Color(120, 90, 80) : Color.Lerp(Clay, new Color(186, 120, 90), 1f - pot.Patience);
        // the floor
        Fill(b, Cx - (int)(pot.R[0] * UnitPx), Base - 6, (int)(pot.R[0] * UnitPx * 2), 6, ClayDark);
        for (int i = 0; i < pot.Rows; i++)
        {
            int y = Base - (i + 1) * RowPx;
            int outer = (int)(pot.R[i] * UnitPx);
            int inner = (int)((pot.R[i] - pot.T[i]) * UnitPx);
            // left: cut through, showing the wall
            Fill(b, Cx - outer, y, outer - inner, RowPx, clay);
            Fill(b, Cx - inner, y, inner, RowPx, Hollow);
            // right: the outside, turning
            Fill(b, Cx, y, outer, RowPx, clay);
            float u = (_spin * 0.4f + i * 0.13f) % 1f;
            Fill(b, Cx + (int)(u * outer), y, 2, RowPx, ClayDark * 0.6f);
            Fill(b, Cx + outer - 3, y, 3, RowPx, ClayDark);
        }
        Fill(b, Cx, Base - pot.Rows * RowPx, 1, pot.Rows * RowPx, ClayDark * 0.8f);
        // your hand
        if (_stage == Stage.Throwing && InPotArea(Game1.getMouseX(), Game1.getMouseY()))
        {
            int y = Game1.getMouseY();
            Color c = _move switch { ThrowMove.PullUp => Color.LightGreen, ThrowMove.PressIn => Color.Orange, _ => Color.LightBlue };
            Fill(b, Cx - 250, y - 1, 500, 2, c * 0.8f);
        }
    }

    // The reference profile, as a line through the middle of each row's edge.
    private void DrawReference(SpriteBatch b)
    {
        var col = Color.White * 0.8f;
        Vector2 At(int i, int side) => new(Cx + side * _shape.R[i] * UnitPx, Base - (i + 0.5f) * RowPx);
        foreach (int side in new[] { -1, 1 })
        {
            Line(b, new Vector2(Cx + side * _shape.R[0] * UnitPx, Base), At(0, side), col);
            for (int i = 1; i < _shape.Rows; i++)
                Line(b, At(i - 1, side), At(i, side), col);
            Line(b, At(_shape.Rows - 1, side), new Vector2(Cx + side * _shape.R[^1] * UnitPx, Base - _shape.Rows * RowPx), col);
        }
        int top = Base - _shape.Rows * RowPx;
        int rt = (int)(_shape.R[^1] * UnitPx);
        Fill(b, Cx - rt, top - 1, rt * 2, 2, col);
    }

    private static void Line(SpriteBatch b, Vector2 from, Vector2 to, Color c)
    {
        Vector2 d = to - from;
        b.Draw(Game1.staminaRect, from, null, c, MathF.Atan2(d.Y, d.X), new Vector2(0f, 0.5f), new Vector2(d.Length(), 2f), SpriteEffects.None, 1f);
    }

    private void DrawPanel(SpriteBatch b)
    {
        int x = PanelX, y = yPositionOnScreen + 30, w = W - (PanelX - xPositionOnScreen) - 30;
        Fill(b, x - 10, y - 10, w + 20, H - 40, Panel);
        void Text(string s, Color? c = null, SpriteFont? font = null)
        {
            font ??= Game1.smallFont;
            foreach (string line in Game1.parseText(s, font, w).Split('\n'))
            {
                b.DrawString(font, line, new Vector2(x, y), c ?? Ink);
                y += (int)font.MeasureString("M").Y;
            }
            y += 6;
        }
        void Button(string key, string label, bool on = false)
        {
            var rect = new Rectangle(x, y, w, 44);
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), rect.X, rect.Y, rect.Width, rect.Height, on ? Color.LightGreen : Color.White, 4f, false);
            var size = Game1.smallFont.MeasureString(label);
            b.DrawString(Game1.smallFont, label, new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2), Ink);
            _buttons[key] = rect;
            y += 52;
        }

        Text($"The {_shape.Name} ({_rules.Name})", font: Game1.dialogueFont);
        switch (_stage)
        {
            case Stage.Centring:
                Text("Centring. Hold the mouse on the clay and keep your hands over it as it wanders. Held true, it settles.");
                Text($"Off true: {MathF.Abs(_wobble):0.0}");
                break;
            case Stage.Throwing:
                Text("Hold the mouse on the pot: your hand works at that height. Match the white outline.");
                Button("pull", "1  Pull up", _move == ThrowMove.PullUp);
                Button("press", "2  Press in", _move == ThrowMove.PressIn);
                Button("out", "3  Let out", _move == ThrowMove.LetOut);
                Text("Patience");
                Fill(b, x, y, w, 18, new Color(90, 60, 40));
                Fill(b, x + 2, y + 2, (int)((w - 4) * _pot.Patience), 14, _pot.Patience < 0.25f ? Color.OrangeRed : Color.SeaGreen);
                y += 28;
                Text(_rules.Regen > 0.5f ? "Simplified: it never tires." : "Pause and it firms up again.");
                Button("done", "Finished");
                break;
            case Stage.Results:
                var r = _result!;
                if (_pot.Collapsed)
                    Text("It collapsed. " + _pot.CollapseReason);
                Text($"{r.Stars:0.#} stars", Color.DarkGoldenrod, Game1.dialogueFont);
                Text($"Shape {r.Shape:P0}\nWalls {r.Walls:P0}\nCentring {r.Centring:P0}");
                Button("again", "Throw again");
                Button("next", "Next shape");
                break;
        }
    }
}

internal static class PotteryProto
{
    public static void Register(IModHelper helper)
    {
        DevTools.AddCommand(helper, "museart_pottery", "The pottery wheel prototype (1.5). Usage: museart_pottery [cup|bowl|vase|jug] [simplified|normal|hard] [throw|bot] (throw: skip centring; bot: a greedy potter throws it, to check the rules)", (_, args) =>
        {
            if (!Context.IsWorldReady)
                return;
            string shape = args.Length > 0 ? args[0] : "";
            string rules = args.Length > 1 ? args[1] : "normal";
            Game1.activeClickableMenu = new PotteryMenu(shape, PotteryRules.For(rules), args.Contains("throw"), args.Contains("bot"));
        });
    }
}
#endif
