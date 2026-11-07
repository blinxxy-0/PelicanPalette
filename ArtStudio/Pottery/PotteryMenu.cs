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
    private readonly PotRenderer _art = new();
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

    public PotteryMenu(string shape, PotteryRules rules, bool skipCentring = false, bool bot = false, bool wreck = false)
        : base(Game1.uiViewport.Width / 2 - W / 2, Game1.uiViewport.Height / 2 - H / 2, W, H, showUpperRightCloseButton: true)
    {
        _rules = rules;
        _shapeName = Array.IndexOf(PotShape.Names, shape) >= 0 ? shape : PotShape.Names[new Random().Next(PotShape.Names.Length)];
        _pot = new ClayPot(rules);
        _shape = PotShape.Make(_shapeName, _pot.Volume);
        _art.FitTo(_shape);
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
        else if (wreck)
        {
            // pulling without a pause, to see a collapse
            _centring = 1f;
            for (int k = 0; k < 20000 && !_pot.Collapsed; k++)
                _pot.Work(ThrowMove.PullUp, (k * 0.25f) % Math.Max(1, _pot.Rows), 1f / 10f);
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

    private int PanelX => xPositionOnScreen + 650;
    // the wheel scene, at the game's own pixel scale
    private Rectangle Scene => new(xPositionOnScreen + 24, yPositionOnScreen + (H - PotRenderer.Height * PotRenderer.Scale) / 2,
        PotRenderer.Width * PotRenderer.Scale, PotRenderer.Height * PotRenderer.Scale);
    private float ArtX(int x) => (x - Scene.X) / (float)PotRenderer.Scale;
    private float ArtY(int y) => (y - Scene.Y) / (float)PotRenderer.Scale;

    protected override void cleanupBeforeExit()
    {
        _art.Dispose();
        base.cleanupBeforeExit();
    }

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
        float hand = (ArtX(Game1.getMouseX()) - _art.CentreX) / _art.UnitPx;
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

    private bool InPotArea(int x, int y) => Scene.Contains(x, y);

    private float RowAt(int y) => _art.RowAt(ArtY(y));

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
                    _art.FitTo(_shape);
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

        // the scene, framed as the easel frames its canvas
        var scene = Scene;
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), scene.X - 12, scene.Y - 12, scene.Width + 24, scene.Height + 24, Color.White, 4f, false);
        bool overPot = InPotArea(Game1.getMouseX(), Game1.getMouseY());
        Color handColor = _move switch { ThrowMove.PullUp => new Color(160, 230, 140), ThrowMove.PressIn => new Color(250, 180, 90), _ => new Color(150, 200, 250) };
        _art.DrawScene(b, scene, _spin, _stage == Stage.Centring ? null : _pot, _shape, showReference: _stage != Stage.Centring,
            lumpOffset: _wobble, handRow: RowAt(Game1.getMouseY()), handColor: handColor,
            showHand: overPot && _stage != Stage.Results,
            handX: _stage == Stage.Centring ? ArtX(Game1.getMouseX()) : float.NaN);
        DrawPanel(b);
        base.draw(b);
        drawMouse(b);
    }

    private void Fill(SpriteBatch b, int x, int y, int w, int h, Color c) =>
        b.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), c);

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
                Text("Hold the mouse on the pot to work it at that height. Match the chalk line.");
                Button("pull", "1  Pull up", _move == ThrowMove.PullUp);
                Button("press", "2  Press in", _move == ThrowMove.PressIn);
                Button("out", "3  Let out", _move == ThrowMove.LetOut);
                Text("Patience");
                Fill(b, x, y, w, 18, new Color(90, 60, 40));
                Fill(b, x + 2, y + 2, (int)((w - 4) * _pot.Patience), 14, _pot.Patience < 0.25f ? Color.OrangeRed : Color.SeaGreen);
                y += 28;
                Text(_rules.Regen > 0.5f ? "It never tires." : "Pause to let it firm up.");
                var live = PotJudge.Judge(_pot, _shape, _centring);
                Text($"Shape {live.Shape:P0}  Walls {live.Walls:P0}");
                Text("Cut-away wall: red is thin, blue is heavy.", Ink * 0.75f);
                Button("done", "Finished");
                break;
            case Stage.Results:
                var r = _result!;
                if (_pot.Collapsed)
                    Text("It collapsed. " + _pot.CollapseReason);
                Text($"{r.Stars:0.#} stars", Color.DarkGoldenrod, Game1.dialogueFont);
                Text($"Shape {r.Shape:P0}\nWalls {r.Walls:P0}\nCentring {r.Centring:P0}");
                if (!_pot.Collapsed)
                    foreach (string tip in PotJudge.Tips(_pot, _shape, r))
                        Text(tip, Ink * 0.8f);
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
        DevTools.AddCommand(helper, "museart_pottery", "The pottery wheel prototype (1.5). Usage: museart_pottery [cup|bowl|vase|jug] [simplified|normal|hard] [throw|bot|wreck] (throw: skip centring; bot: a greedy potter throws it, to check the rules; wreck: pull till it collapses)", (_, args) =>
        {
            if (!Context.IsWorldReady)
                return;
            string shape = args.Length > 0 ? args[0] : "";
            string rules = args.Length > 1 ? args[1] : "normal";
            Game1.activeClickableMenu = new PotteryMenu(shape, PotteryRules.For(rules), args.Contains("throw"), args.Contains("bot"), args.Contains("wreck"));
        });
    }
}
#endif
