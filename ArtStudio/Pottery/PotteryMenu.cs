#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Pottery;

// The pottery prototype's wheel (docs/design/pottery_minigame.md, sections 10
// and 11): centring, then throwing against a reference profile, then the
// stars. Laid out as the easel is, so the two read as one family: the title
// and the patience bar across the top (like the easel's Light), the wheel
// framed in the middle, the clay's grades on the left and the hands on the
// right, the instructions and Finish along the bottom. (No Reference panel:
// the chalk line on the wheel is the reference. The user, 2026-11-07.)
// museart_pottery [cup|bowl|vase|jug] [simplified|normal|hard] [throw|bot|wreck]
internal sealed class PotteryMenu : IClickableMenu
{
    private enum Stage { Centring, Throwing, Results }

    private const int MaxWidth = 1240, MaxHeight = 780;
    private const int Pad = 20, TopBar = 64, BottomBar = 56, SideWidth = 250;

    private readonly PotRenderer _art = new();
    private readonly PotteryRules _rules;
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
    // (snapshots only: where the mouse is pretended to be, to show the hands)
    internal Point? FakeMouse;
    private int MouseX => FakeMouse?.X ?? Game1.getMouseX();
    private int MouseY => FakeMouse?.Y ?? Game1.getMouseY();
    internal Point SceneCentre(float up = 0.35f) => new(Scene.Center.X, Scene.Bottom - (int)(Scene.Height * up));

    public PotteryMenu(string shape, PotteryRules rules, bool skipCentring = false, bool bot = false, bool wreck = false)
    {
        _rules = rules;
        string name = Array.IndexOf(PotShape.Names, shape) >= 0 ? shape : PotShape.Names[new Random().Next(PotShape.Names.Length)];
        _pot = new ClayPot(rules);
        _shape = PotShape.Make(name, _pot.Volume);
        _art.FitTo(_shape);
        Reposition();
        Restart();
        if (skipCentring || bot || wreck)
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
            for (int k = 0; k < 20000 && !_pot.Collapsed; k++)
                _pot.Work(ThrowMove.PullUp, (k * 0.25f) % Math.Max(1, _pot.Rows), 1f / 10f);
            Finish();
        }
    }

    private void Reposition()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Reposition();

    protected override void cleanupBeforeExit()
    {
        _art.Dispose();
        base.cleanupBeforeExit();
    }

    // ---- Layout ----

    private Rectangle TopArea => new(xPositionOnScreen + Pad + 24, yPositionOnScreen + Pad + 8, width - Pad * 2 - 48, TopBar);
    private Rectangle LeftArea => new(xPositionOnScreen + Pad + 16, TopArea.Bottom + 8, SideWidth, height - TopBar - BottomBar - Pad * 2 - 24);
    private Rectangle RightArea => new(xPositionOnScreen + width - Pad - 16 - SideWidth, LeftArea.Y, SideWidth, LeftArea.Height);
    private Rectangle BottomArea => new(TopArea.X, yPositionOnScreen + height - Pad - BottomBar, TopArea.Width, BottomBar);
    // the wheel, at the game's own pixel scale, centred between the side panels
    private Rectangle Scene
    {
        get
        {
            int w = _art.Width * PotRenderer.Scale, h = _art.Height * PotRenderer.Scale;
            int midX = (LeftArea.Right + RightArea.X) / 2;
            int midY = LeftArea.Y + LeftArea.Height / 2;
            return new Rectangle(midX - w / 2, midY - h / 2, w, h);
        }
    }

    private float ArtX(int x) => (x - Scene.X) / (float)PotRenderer.Scale;
    private float ArtY(int y) => (y - Scene.Y) / (float)PotRenderer.Scale;
    private bool InPotArea(int x, int y) => Scene.Contains(x, y);
    private float RowAt(int y) => _art.RowAt(ArtY(y));

    // ---- Rules ----

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

    public override void update(GameTime time)
    {
        base.update(time);
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        _spin += dt * 9f;
        bool held = Mouse.GetState().LeftButton == ButtonState.Pressed && InPotArea(Game1.getMouseX(), Game1.getMouseY());
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

    // ---- Draw ----

    private static readonly (ThrowMove Move, string Key, string Label, string Name, string Hint)[] Moves =
    {
        (ThrowMove.PullUp, "pull", "1  Pull up", "Pull up", "Draws the clay up: taller, with thinner walls."),
        (ThrowMove.PressIn, "press", "2  Press in", "Press in", "Narrows it at your hand. The wall thickens."),
        (ThrowMove.LetOut, "out", "3  Let out", "Let out", "Swells it at your hand. The wall thins."),
    };

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        _buttons.Clear();
        int mx = MouseX, my = MouseY;

        DrawTopBar(b);
        DrawScene(b, mx, my);
        switch (_stage)
        {
            case Stage.Centring: DrawCentringPanel(b); break;
            case Stage.Throwing: DrawClay(b); DrawTools(b, mx, my); break;
            case Stage.Results: DrawAdvice(b); DrawResults(b); break;
        }
        DrawBottomBar(b, mx, my);
        base.draw(b);
        drawMouse(b);
    }

    private void DrawTopBar(SpriteBatch b)
    {
        var top = TopArea;
        Ui.Text(b, $"Throwing: the {_shape.Name}", new Vector2(top.X, top.Y + 4));
        Ui.Text(b, _rules.Name, new Vector2(top.X, top.Y + 34), Game1.textColor * 0.7f);
        if (_stage != Stage.Throwing)
            return;

        // Patience: the clay's, as the easel has its light
        int barW = Math.Min(460, top.Width - 560);
        var bar = new Rectangle(top.Center.X - barW / 2 + 60, top.Y + 14, barW, 28);
        Ui.Text(b, "Patience", new Vector2(bar.X - 104, bar.Y));
        Ui.Fill(b, bar, new Color(60, 40, 30));
        float p = _pot.Patience;
        Color fill = p > 0.5f ? Color.Lerp(new Color(110, 170, 80), new Color(150, 200, 90), (p - 0.5f) * 2f)
                   : p > 0.25f ? Color.Lerp(new Color(220, 150, 60), new Color(110, 170, 80), (p - 0.25f) / 0.25f)
                   : Color.Lerp(new Color(200, 70, 50), new Color(220, 150, 60), p / 0.25f);
        Ui.Fill(b, new Rectangle(bar.X + 3, bar.Y + 3, (int)((bar.Width - 6) * p), bar.Height - 6), fill);
        Ui.Outline(b, bar, new Color(40, 25, 15), 2);
        string state = _rules.Regen > 0.5f ? "never tires" : p < 0.25f ? "slumping!" : "";
        if (state.Length > 0)
            Ui.Text(b, state, new Vector2(bar.Right - 10 - Ui.TextWidth(state) * 0.8f, bar.Y + 2), Color.White * 0.9f, 0.8f);

        // how tall it is against the shape
        string rows = $"Height {_pot.Rows}/{_shape.Rows}";
        Ui.Text(b, rows, new Vector2(top.Right - Ui.TextWidth(rows), top.Y + 14),
            _pot.Rows >= _shape.Rows ? new Color(60, 120, 40) : Game1.textColor);
    }

    // Left, while throwing: how it's going, and what the move in hand does.
    private void DrawClay(SpriteBatch b)
    {
        var left = LeftArea;
        var panel = new Rectangle(left.X, left.Y, left.Width, 286);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "The clay", panel.Center.X, panel.Y + 18);
        var live = PotJudge.Judge(_pot, _shape, _centring);
        int y = panel.Y + 60;
        foreach (var (label, value) in new[] { ("Shape", live.Shape), ("Walls", live.Walls) })
        {
            Ui.Text(b, label, new Vector2(panel.X + 24, y), Game1.textColor * 0.7f);
            Ui.Text(b, I18n.Rating(value), new Vector2(panel.X + 116, y));
            y += 34;
        }
        y += 6;
        Ui.Text(b, Game1.parseText("In the cut-away, red is too thin and blue too heavy.", Game1.smallFont, panel.Width - 48),
            new Vector2(panel.X + 24, y), Game1.textColor * 0.7f);

        var hint = Moves.First(m => m.Move == _move);
        int hy = panel.Bottom + 16;
        Ui.Text(b, hint.Name, new Vector2(left.X + 8, hy));
        Ui.Text(b, Game1.parseText(hint.Hint, Game1.smallFont, left.Width - 16), new Vector2(left.X + 8, hy + 32), Game1.textColor * 0.75f);
    }

    // Left, on the results: what cost the most, as Tobin would put it.
    private void DrawAdvice(SpriteBatch b)
    {
        var left = LeftArea;
        string said = _pot.Collapsed ? (_pot.CollapseReason ?? "") : string.Join(" ", PotJudge.Tips(_pot, _shape, _result!));
        string quote = Game1.parseText($"\"{said}\"", Game1.smallFont, left.Width - 48);
        int textH = (int)Game1.smallFont.MeasureString(quote).Y;
        var panel = new Rectangle(left.X, left.Y, left.Width, textH + 110);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "Tobin says", panel.Center.X, panel.Y + 18);
        Ui.Text(b, quote, new Vector2(panel.X + 24, panel.Y + 60), Game1.textColor * 0.85f);
    }

    private void DrawScene(SpriteBatch b, int mx, int my)
    {
        var scene = Scene;
        drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), scene.X - 12, scene.Y - 12, scene.Width + 24, scene.Height + 24, Color.White, 4f, false);
        bool overPot = InPotArea(mx, my);
        Color handColor = _move switch { ThrowMove.PullUp => new Color(160, 230, 140), ThrowMove.PressIn => new Color(250, 180, 90), _ => new Color(150, 200, 250) };
        _art.DrawScene(b, scene, _spin, _stage == Stage.Centring ? null : _pot, _shape, showReference: _stage != Stage.Centring,
            lumpOffset: _wobble, handRow: RowAt(my), handColor: handColor,
            showHand: overPot && _stage != Stage.Results,
            handX: _stage == Stage.Centring ? ArtX(mx) : float.NaN);
    }

    private void DrawCentringPanel(SpriteBatch b)
    {
        var right = RightArea;
        var panel = new Rectangle(right.X, right.Y, right.Width, 330);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "Centring", panel.Center.X, panel.Y + 18);
        // the off-true gauge: the lump's drift, and the band it runs true in
        var track = new Rectangle(panel.X + 24, panel.Y + 70, panel.Width - 48, 30);
        Ui.Fill(b, track, new Color(70, 48, 34));
        float bandW = track.Width * (0.8f / 12f);
        Ui.Fill(b, new Rectangle((int)(track.Center.X - bandW), track.Y + 3, (int)(bandW * 2), track.Height - 6), new Color(120, 180, 90));
        int markX = track.Center.X + (int)(_wobble / 12f * (track.Width / 2 - 6));
        Ui.Fill(b, new Rectangle(markX - 4, track.Y - 4, 8, track.Height + 8), new Color(232, 182, 140));
        Ui.Outline(b, track, new Color(40, 25, 15), 2);
        bool trueNow = MathF.Abs(_wobble) < 0.8f;
        string state = trueNow ? "Running true..." : MathF.Abs(_wobble) < 4f ? "Nearly true" : "Off true";
        Ui.TextCentered(b, state, panel.Center.X, track.Bottom + 18, trueNow ? new Color(60, 120, 40) : Game1.textColor);
        Ui.Text(b, Game1.parseText("Keep your hand over the lump as it wanders. Held, it settles.", Game1.smallFont, panel.Width - 40),
            new Vector2(panel.X + 20, track.Bottom + 60), Game1.textColor * 0.75f);
    }

    private void DrawTools(SpriteBatch b, int mx, int my)
    {
        var right = RightArea;
        var panel = new Rectangle(right.X, right.Y, right.Width, 250);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "Hands", panel.Center.X, panel.Y + 18);
        int y = panel.Y + 56;
        foreach (var m in Moves)
        {
            var r = new Rectangle(panel.X + 20, y, panel.Width - 40, 48);
            Ui.Button(b, r, m.Label, selected: _move == m.Move, hovered: r.Contains(mx, my));
            _buttons[m.Key] = r;
            y += 58;
        }

    }

    private void DrawResults(SpriteBatch b)
    {
        var right = RightArea;
        var r = _result!;
        int y = right.Y + 4;
        if (_pot.Collapsed)
        {
            Ui.Text(b, "It collapsed.", new Vector2(right.X + 8, y), new Color(170, 50, 30));
            y += 34;
        }
        Ui.Stars(b, r.Stars, right.X + 8, y, 40, 6);
        y += 58;
        void Row(string label, string value)
        {
            Ui.Text(b, label, new Vector2(right.X + 8, y), Game1.textColor * 0.7f);
            Ui.Text(b, value, new Vector2(right.X + 8 + Ui.TextWidth("Centring") + 18, y));
            y += 32;
        }
        Row("Shape", I18n.Rating(r.Shape));
        Row("Walls", I18n.Rating(r.Walls));
        Row("Centring", I18n.Rating(r.Centring));
    }

    private void DrawBottomBar(SpriteBatch b, int mx, int my)
    {
        var bottom = BottomArea;
        string line = _stage switch
        {
            Stage.Centring => "Hold the mouse button on the clay and keep your hand over it.",
            Stage.Throwing => "Hold the mouse on the pot to work it at that height. Match the chalk line.",
            _ => "",
        };
        if (line.Length > 0)
            Ui.Text(b, line, new Vector2(bottom.X, bottom.Y + 12));
        if (_stage == Stage.Throwing)
        {
            var r = new Rectangle(bottom.Right - 180, bottom.Y, 180, bottom.Height);
            Ui.Button(b, r, "Finished", hovered: r.Contains(mx, my));
            _buttons["done"] = r;
        }
        else if (_stage == Stage.Results)
        {
            var next = new Rectangle(bottom.Right - 200, bottom.Y, 200, bottom.Height);
            var again = new Rectangle(next.X - 220, bottom.Y, 200, bottom.Height);
            Ui.Button(b, again, "Throw again", hovered: again.Contains(mx, my));
            Ui.Button(b, next, "Next shape", hovered: next.Contains(mx, my));
            _buttons["again"] = again;
            _buttons["next"] = next;
        }
    }
}

internal static class PotteryProto
{
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_pottery", "The pottery wheel prototype (1.5). Usage: museart_pottery [cup|bowl|vase|jug] [simplified|normal|hard] [throw|bot|wreck] (throw: skip centring; bot: a greedy potter throws it, to check the rules; wreck: pull till it collapses)", (_, args) =>
        {
            if (!Context.IsWorldReady)
                return;
            string shape = args.Length > 0 ? args[0] : "";
            string rules = args.Length > 1 ? args[1] : "normal";
            PotteryArt.Load(helper);
            Game1.activeClickableMenu = new PotteryMenu(shape, PotteryRules.For(rules), args.Contains("throw"), args.Contains("bot"), args.Contains("wreck"));
        });
        // Every stage at both common sizes, with the layout audit (overlapping or spilling text),
        // to dialogue_snapshots/pottery_*.png.
        DevTools.AddCommand(helper, "museart_snappottery", "Renders the pottery wheel's stages at 1280x720 and 1920x1080, with the layout audit.", (_, _) =>
        {
            if (!Context.IsWorldReady)
                return;
            string dir = System.IO.Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
            System.IO.Directory.CreateDirectory(dir);
            PotteryArt.Load(helper);
            var rules = PotteryRules.For("normal");
            var stages = new (string Name, Func<PotteryMenu> Make)[]
            {
                ("centring", () => new PotteryMenu("vase", rules)),
                ("centring_hands", () => { var m = new PotteryMenu("vase", rules); m.FakeMouse = m.SceneCentre(0.2f); return m; }),
                ("throwing", () => new PotteryMenu("vase", rules, skipCentring: true)),
                ("throwing_hands", () => { var m = new PotteryMenu("vase", rules, skipCentring: true); m.FakeMouse = m.SceneCentre(0.3f); return m; }),
                ("results_hands", () => { var m = new PotteryMenu("jug", rules, bot: true); return m; }),
                ("results", () => new PotteryMenu("vase", rules, bot: true)),
                ("collapsed", () => new PotteryMenu("cup", rules, wreck: true)),
            };
            var problems = new List<string>();
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
                foreach (var (name, make) in stages)
                    DevTools.AtViewport(w, h, () =>
                    {
                        var menu = make();
                        Ui.Audit = new List<Ui.AuditEntry>();
                        DevTools.Render(menu, System.IO.Path.Combine(dir, $"pottery_{name}_{w}.png"));
                        problems.AddRange(Ui.AuditProblems().Select(p => $"{name} @{w}x{h}: {p}"));
                        Ui.Audit = null;
                        menu.exitThisMenuNoSound();
                    });
            foreach (string problem in problems)
                monitor.Log(problem, LogLevel.Warn);
            monitor.Log($"Pottery rendered; {problems.Count} layout problems.", LogLevel.Info);
        });
    }
}
#endif
