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
    private enum Stage { Centring, Throwing, Banding, Firing, Results }

    private const int MaxWidth = 1240, MaxHeight = 780;
    private const int Pad = 20, TopBar = 64, BottomBar = 56, SideWidth = 250;

    private readonly PotRenderer _art = new();
    private readonly PotteryRules _rules;
    private Stage _stage = Stage.Centring;
    private ClayPot _pot;
    private PotShape _shape;
    private ThrowMove _move = ThrowMove.PullUp;
    private PotResult? _result;
    private int _xp;                     // the Pottery skill's XP from this pot so far (PotterySkill), shown on the results

    // Opened from a wheel that takes Potter's Clay (Vale/ValeClay.cs): "Throw again" and "Next shape"
    // take a fresh lump for the shape first, and do nothing if there isn't enough (it says why).
    // Null in the dev prototype, where throws are free.
    internal Func<string, bool>? TakeClay;
    // ... how many lumps a shape takes (for the "Throw again" button's label),
    internal Func<string, int>? LumpsFor;
    // ... and the glazes cost their minerals: how many of a glaze's you have, and taking one the
    // first time a glaze touches this pot (a dip or a band). Null in the prototype: glazes free.
    internal Func<int, int>? GlazeStock;
    internal Func<int, bool>? TakeGlaze;
    private readonly HashSet<int> _glazesUsed = new();
    private bool _strokeBlocked;
    // At a real wheel the pot is one piece of work: thrown, then glazed, then fired. Its results
    // offer only the next step (and, after the throw, starting again with fresh clay). The user,
    // 2026-11-11: "why am i not just immediately forced into glazing and firing? feels like
    // minigame pieces". The prototype keeps its free buttons.
    private bool AtWheel => TakeClay != null;
    private float _spin;

    // centring
    private readonly Random _random = new();
    private Centring _centringWheel = null!;   // set by Restart
    private float _centring;

    // banding (Banding.cs): the glaze on the pot, the design to match, the glaze in the
    // brush, the banding wheel's turn, and how long the brush has been held (the hand
    // drifts more the longer you hold)
    private const float BandingTurn = MathF.Tau / 1.1f;     // radians a second: a turn in 1.1 s
    private PotGlaze? _glaze;
    private GlazeDesign? _design;
    private GlazeResult? _glazeResult;
    private int _glazeIdx;
    private float _bandAngle, _holdTime, _phase1, _phase2, _lastBucket;
    private bool _wasHeld;

    // firing (Firing.cs): the kiln, and how it came out
    private Kiln? _kiln;
    private FiringResult? _fireResult;

    private readonly Dictionary<string, Rectangle> _buttons = new();
    // (snapshots only: where the mouse is pretended to be, to show the hands)
    internal Point? FakeMouse;
    private int MouseX => FakeMouse?.X ?? Game1.getMouseX();
    private int MouseY => FakeMouse?.Y ?? Game1.getMouseY();
    internal Point SceneCentre(float up = 0.35f) => new(Scene.Center.X, Scene.Bottom - (int)(Scene.Height * up));

    public PotteryMenu(string shape, PotteryRules rules, bool skipCentring = false, bool bot = false, bool wreck = false, bool band = false, bool fire = false)
    {
        band |= fire;
        bot |= band;
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
            if (band)
                StartBanding();
            if (fire)
            {
                GlazeBot(0f);
                StartFiring();
            }
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

    // The simulation's greedy potter (PotBot.cs), its hand perfect: to check the rules.
    private static void Bot(ClayPot pot, PotShape shape) => PotBot.Throw(pot, shape, PotHand.Perfect);

    private void Restart()
    {
        _xp = 0;
        _glazesUsed.Clear();
        _pot = new ClayPot(_rules);
        _stage = Stage.Centring;
        _centringWheel = new Centring(_random);
        _result = null;
        _move = ThrowMove.PullUp;
        _glaze = null;
        _design = null;
        _glazeResult = null;
        _kiln = null;
        _fireResult = null;
    }

    // ---- Banding ----

    private void StartBanding()
    {
        _glaze = new PotGlaze(_pot.Rows);
        _design = GlazeDesign.For(_shape.Name);
        _glazeResult = null;
        _kiln = null;
        _fireResult = null;
        _glazeIdx = _design.Base;
        _wasHeld = false;
        _stage = Stage.Banding;
    }

    // The brush's bucket on the glaze grid: it touches the pot's left edge.
    private static float BrushBucket(float angle) => (-MathF.PI / 2f - angle) / MathF.Tau * PotGlaze.Buckets;

    // How far the brush hand has wandered after holding so long: a slow sway that grows
    // from nothing, to most of its reach in a couple of seconds.
    private static float Drift(float reach, float hold, float p1, float p2) =>
        reach * MathF.Min(1f, 0.35f + hold / 2f)
        * (0.6f * (MathF.Sin(1.7f * hold + p1) - MathF.Sin(p1)) + 0.4f * (MathF.Sin(3.1f * hold + p2) - MathF.Sin(p2))) * 0.5f;

    private float BrushHeight(int mouseY, bool held)
    {
        float h = RowAt(mouseY) + 0.5f + (held ? Drift(_rules.Drift, _holdTime, _phase1, _phase2) : 0f);
        return Math.Clamp(h, 0f, _pot.Rows);
    }

    private void UpdateBanding(float dt, bool held)
    {
        _bandAngle += BandingTurn * dt;
        if (held && !_wasHeld)
        {
            _strokeBlocked = !UseGlaze(_glazeIdx);
            _holdTime = 0f;
            _phase1 = (float)(_random.NextDouble() * MathF.Tau);
            _phase2 = (float)(_random.NextDouble() * MathF.Tau);
            _lastBucket = BrushBucket(_bandAngle);
        }
        _wasHeld = held;
        if (!held || _strokeBlocked)
            return;
        _holdTime += dt;
        float now = BrushBucket(_bandAngle);
        _glaze!.Paint(_glazeIdx, BrushHeight(Game1.getMouseY(), true), _lastBucket, now);
        _lastBucket = now;
    }

    // A glaze's first touch on this pot takes its mineral (at a wheel); false if there's none.
    private bool UseGlaze(int glaze)
    {
        if (TakeGlaze == null || _glazesUsed.Contains(glaze))
            return true;
        if (!TakeGlaze(glaze))
            return false;
        _glazesUsed.Add(glaze);
        return true;
    }

    internal float GlazeScore => _glazeResult?.Overall ?? 0f;

    private void FinishBanding()
    {
        _glazeResult = GlazeJudge.Judge(_glaze!, _design!);
        _xp += PotterySkill.OnGlazed(this, _glazeResult, _rules);
        _stage = Stage.Results;
    }

    // The simulation's bander, for checking the judge: dips, then paints each band with
    // whole turns of the wheel from its bottom edge to its top. `reach` is how far its
    // hand wanders (0: a perfect hand); it never corrects for it.
    internal void GlazeBot(float reach, bool finish = true, int bands = int.MaxValue)
    {
        if (_glaze == null || _design == null)
            return;
        _glaze.Dip(_design.Base);
        const float dt = 1f / 60f;
        foreach (var band in _design.Bands.Take(bands))
        {
            float lo = band.From * _pot.Rows + PotGlaze.BrushRows / 2f, hi = band.To * _pot.Rows - PotGlaze.BrushRows / 2f;
            if (hi < lo)
                lo = hi = (lo + hi) / 2f;
            float p1 = (float)(_random.NextDouble() * MathF.Tau), p2 = (float)(_random.NextDouble() * MathF.Tau);
            float angle = 0f, hold = 0f;
            for (float h = lo; ; h = MathF.Min(hi, h + 1f / PotGlaze.SlicesPerRow))
            {
                // a whole turn at this height
                for (float turned = 0f; turned < MathF.Tau; turned += BandingTurn * dt)
                {
                    float from = BrushBucket(angle);
                    angle += BandingTurn * dt;
                    hold += dt;
                    _glaze.Paint(band.Glaze, h + Drift(reach, hold, p1, p2), from, BrushBucket(angle));
                }
                if (h >= hi)
                    break;
            }
        }
        if (finish)
            FinishBanding();
    }

    // ---- Firing ----

    private void StartFiring()
    {
        _kiln = new Kiln(_glaze?.Base ?? -1, _rules, _random);
        _fireResult = null;
        _stage = Stage.Firing;
    }

    private void FinishFiring()
    {
        _fireResult = FiringJudge.Judge(_kiln!, _random);
        _xp += PotterySkill.OnFired(this, _fireResult, _rules);
        _stage = Stage.Results;
        PotteryPieces.OnFired(this, _shape.Name, _pot, _glaze, _fireResult, Stars);
    }

    internal float FireScore => _fireResult?.Score ?? 0f;

    // The simulation's stokers, for checking the firing: "steady" looks at the forecast
    // every two seconds and adds hardwood if it's heading low (as a player watching the
    // marker would); "eager" feeds wood or hardwood four times a second whenever it's
    // heading low (it overfeeds); "coal" throws coal in on a beat; "none" never feeds it.
    internal void FireBot(string mode, float until = Kiln.Duration, bool finish = true)
    {
        if (_kiln == null)
            return;
        const float dt = 1f / 60f;
        float next = 0f;
        while (!_kiln.Done && _kiln.Time < until)
        {
            if (_kiln.Time >= next)
            {
                float want = _kiln.Target(_kiln.Time + 3f);
                switch (mode)
                {
                    case "eager":
                        // feeds for where it'll be when this fuel has burnt
                        float gap = _kiln.Target(_kiln.Time + 4f) - _kiln.Forecast(4f);
                        if (gap > 2f)
                            _kiln.Add(gap > 8f ? Fuel.Hardwood : Fuel.Wood);
                        next += 0.25f;
                        break;
                    case "steady":
                        if (_kiln.Forecast(3f) < want)
                            _kiln.Add(Fuel.Hardwood);
                        next += 2f;
                        break;
                    case "coal":
                        _kiln.Add(Fuel.Coal);
                        next += 1.2f;
                        break;
                    default:
                        next = float.MaxValue;
                        break;
                }
            }
            _kiln.Step(dt);
        }
        if (finish && _kiln.Done)
            FinishFiring();
    }

    // The pot's stars, glaze and all: the throw counts three parts in four, the glaze one,
    // and the firing sets how much of the glaze's comes through (a kiln gift adds half a star).
    private float Stars
    {
        get
        {
            if (_fireResult is { Verdict: "Cracked" })
                return 0f;
            if (_glazeResult is not { } g || _result is not { } t)
                return _result?.Stars ?? 0f;
            float glaze = g.Overall * (_fireResult is { } f ? 0.7f + 0.3f * f.Score : 1f);
            float stars = MathF.Round((0.75f * t.Overall + 0.25f * glaze) * 10f) / 2f;
            return MathF.Min(5f, stars + (_fireResult?.Gift != null ? 0.5f : 0f));
        }
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
            case Stage.Banding:
                UpdateBanding(dt, held);
                break;
            case Stage.Firing:
                _kiln!.Step(dt);
                if (_kiln.Done)
                    FinishFiring();
                break;
        }
    }

    // Keep your hands on the lump (the mouse, held) and it settles; let go and it wanders.
    private void UpdateCentring(float dt, bool held)
    {
        float hand = (ArtX(Game1.getMouseX()) - _art.CentreX) / _art.UnitPx;
        _centringWheel.Step(dt, held ? hand : null, _rules.CentringBand);
        if (_centringWheel.Done)
        {
            _centring = _centringWheel.Score;
            _stage = Stage.Throwing;
        }
    }

    private void Finish()
    {
        _result = PotJudge.Judge(_pot, _shape, _centring);
        _xp += PotterySkill.OnThrown(this, _result, _rules);
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
                case "done": if (_stage == Stage.Banding) FinishBanding(); else Finish(); break;
                case "again":
                    if (TakeClay == null || TakeClay(_shape.Name))
                        Restart();
                    break;
                case "glaze": StartBanding(); break;
                case "fire": StartFiring(); break;
                case "wood": _kiln?.Add(Fuel.Wood); break;
                case "hard": _kiln?.Add(Fuel.Hardwood); break;
                case "coal": _kiln?.Add(Fuel.Coal); break;
                case "dip": if (_glaze != null && UseGlaze(_glazeIdx)) _glaze.Dip(_glazeIdx); break;
                case "leave": exitThisMenu(); break;
                case "sponge": _glaze?.Sponge(); break;
                case var k when k.StartsWith("g") && int.TryParse(k[1..], out int gi): _glazeIdx = gi; break;
                case "next":
                    string nextShape = PotShape.Names[(Array.IndexOf(PotShape.Names, _shape.Name) + 1) % PotShape.Names.Length];
                    if (TakeClay != null && !TakeClay(nextShape))
                        break;
                    _shape = PotShape.Make(nextShape, _pot.Volume);
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
        if (_stage == Stage.Firing)
        {
            if (key >= Keys.D1 && key < Keys.D1 + Kiln.Fuels.Length)
            {
                _kiln!.Add((Fuel)(key - Keys.D1));
                Game1.playSound("woodyStep");
            }
            return;
        }
        if (_stage == Stage.Banding)
        {
            if (key >= Keys.D1 && key < Keys.D1 + Glazes.All.Length)
                _glazeIdx = key - Keys.D1;
            return;
        }
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
            case Stage.Banding: DrawDesign(b); DrawGlazes(b, mx, my); break;
            case Stage.Firing: DrawFiringChart(b); DrawFuel(b, mx, my); break;
            case Stage.Results: DrawAdvice(b); DrawResults(b); break;
        }
        DrawBottomBar(b, mx, my);
        base.draw(b);
        drawMouse(b);
    }

    private void DrawTopBar(SpriteBatch b)
    {
        var top = TopArea;
        bool firing = _stage == Stage.Firing || _fireResult != null;
        bool glazing = _stage == Stage.Banding || _glazeResult != null;
        Ui.Text(b, $"{(firing ? "Firing" : glazing ? "Glazing" : "Throwing")}: the {_shape.Name}", new Vector2(top.X, top.Y + 4));
        Ui.Text(b, _rules.Name, new Vector2(top.X, top.Y + 34), Game1.textColor * 0.7f);
        if (_stage == Stage.Firing)
        {
            int left = (int)MathF.Ceiling(Kiln.Duration - _kiln!.Time);
            string time = $"{left / 60}:{left % 60:00} to go";
            Ui.Text(b, time, new Vector2(top.Right - Ui.TextWidth(time), top.Y + 14));
            return;
        }
        if (_stage == Stage.Banding)
        {
            // how many of the design's bands are on, near enough
            var live = GlazeJudge.Judge(_glaze!, _design!);
            int done = live.BandScoresPct.Count(p => p >= 60);
            string bands = $"Bands {done}/{_design!.Bands.Length}";
            Ui.Text(b, bands, new Vector2(top.Right - Ui.TextWidth(bands), top.Y + 14),
                done == _design.Bands.Length ? new Color(60, 120, 40) : Game1.textColor);
            return;
        }
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
        // what resting can't bring back: hatched dark from MaxPatience to the end
        float max = _pot.MaxPatience;
        if (max < 1f)
        {
            int lostX = bar.X + 3 + (int)((bar.Width - 6) * max);
            Ui.Fill(b, new Rectangle(lostX, bar.Y + 3, bar.Right - 3 - lostX, bar.Height - 6), new Color(30, 18, 12));
        }
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
        string[] glazeTips = _glazeResult != null ? GlazeJudge.Tips(_glaze!, _design!, _glazeResult).Take(2).ToArray() : Array.Empty<string>();
        string said = _pot.Collapsed ? (_pot.CollapseReason ?? "")
            : _fireResult != null && (_fireResult.Verdict != "Perfect" || _fireResult.Gift != null || glazeTips.Length == 0)
                ? FiringJudge.Tip(_kiln!, _fireResult) + (glazeTips.Length > 0 ? " " + glazeTips[0] : "")
            : glazeTips.Length > 0 ? string.Join(" ", glazeTips)
            : string.Join(" ", PotJudge.Tips(_pot, _shape, _result!));
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
        if (_kiln != null && _glaze != null)
        {
            // cooled when it's done: the glaze's own colours, the kiln opened
            bool hot = _stage == Stage.Firing;
            _art.DrawKiln(b, scene, _spin / 9f, _pot, _glaze,
                hot ? Math.Clamp((_kiln.Heat - Kiln.StartHeat) / 75f, 0f, 1f) : 0f,
                hot ? Math.Clamp(_kiln.Power / 6f, 0f, 1f) : 0f,
                dull: _fireResult is { Verdict: "Underfired" or "Overfired" } ? 0.4f : 0f,
                cracked: _fireResult is { Verdict: "Cracked" });
            return;
        }
        if (_glaze != null && (_stage == Stage.Banding || _glazeResult != null))
        {
            bool held = FakeMouse == null && Mouse.GetState().LeftButton == ButtonState.Pressed;
            _art.DrawBanding(b, scene, _spin, _bandAngle, _pot, _glaze, _design!,
                _stage == Stage.Banding && overPot ? BrushHeight(my, held && overPot) : float.NaN, _glazeIdx);
            return;
        }
        Color handColor = _move switch { ThrowMove.PullUp => new Color(160, 230, 140), ThrowMove.PressIn => new Color(250, 180, 90), _ => new Color(150, 200, 250) };
        _art.DrawScene(b, scene, _spin, _stage == Stage.Centring ? null : _pot, _shape, showReference: _stage != Stage.Centring,
            lumpOffset: _centringWheel.Wobble, handRow: RowAt(my), handColor: handColor,
            showHand: overPot && _stage != Stage.Results,
            handX: _stage == Stage.Centring ? ArtX(mx) : float.NaN);
    }

    private void DrawCentringPanel(SpriteBatch b)
    {
        var right = RightArea;
        var panel = new Rectangle(right.X, right.Y, right.Width, 330);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "Centring", panel.Center.X, panel.Y + 18);
        // the off-true gauge: the sway's width, swinging as the lump does, and the band it runs true in
        var track = new Rectangle(panel.X + 24, panel.Y + 70, panel.Width - 48, 30);
        Ui.Fill(b, track, new Color(70, 48, 34));
        float half = track.Width / 2f - 6;
        float bandW = half * (Centring.TrueWithin / Centring.MaxSway);
        Ui.Fill(b, new Rectangle((int)(track.Center.X - bandW), track.Y + 3, (int)(bandW * 2), track.Height - 6), new Color(120, 180, 90));
        float sway = half * (_centringWheel.Sway / Centring.MaxSway);
        Ui.Fill(b, new Rectangle((int)(track.Center.X - sway), track.Center.Y - 2, (int)(sway * 2), 4), new Color(232, 182, 140) * 0.5f);
        int markX = track.Center.X + (int)(_centringWheel.Wobble / Centring.MaxSway * half);
        Ui.Fill(b, new Rectangle(markX - 4, track.Y - 4, 8, track.Height + 8), new Color(232, 182, 140));
        Ui.Outline(b, track, new Color(40, 25, 15), 2);
        bool trueNow = _centringWheel.RunningTrue;
        string state = trueNow ? "Running true..." : _centringWheel.Sway < 1.5f ? "Nearly true" : "Off true";
        Ui.TextCentered(b, state, panel.Center.X, track.Bottom + 18, trueNow ? new Color(60, 120, 40) : Game1.textColor);
        Ui.Text(b, Game1.parseText("Hold the mouse on it and ride its sway: it settles. Off it, it works loose.", Game1.smallFont, panel.Width - 40),
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

    private static void Swatch(SpriteBatch b, Rectangle r, int glaze)
    {
        Color c = glaze >= 0 ? Glazes.All[glaze].Colour : new Color(206, 156, 120);
        Ui.Fill(b, r, PotRenderer.GlazeShade(c, 3));
        Ui.Fill(b, new Rectangle(r.X + 3, r.Y + 3, r.Width / 3, r.Height / 3), PotRenderer.GlazeShade(c, 5));
        Ui.Outline(b, r, PotRenderer.GlazeShade(c, 0), 2);
    }

    // Left, while banding: the design to match, and how it's going.
    private void DrawDesign(SpriteBatch b)
    {
        var left = LeftArea;
        var design = _design!;
        var panel = new Rectangle(left.X, left.Y, left.Width, 316);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "The design", panel.Center.X, panel.Y + 18);
        int x = panel.X + 24, y = panel.Y + 60;
        Swatch(b, new Rectangle(x, y + 2, 24, 24), design.Base);
        Ui.Text(b, Glazes.All[design.Base].Name, new Vector2(x + 36, y));
        Ui.Text(b, "all over", new Vector2(x + 36, y + 28), Game1.textColor * 0.7f, 0.85f);
        y += 64;
        foreach (var band in design.Bands)
        {
            Swatch(b, new Rectangle(x, y + 2, 24, 24), band.Glaze);
            Ui.Text(b, Glazes.All[band.Glaze].Name, new Vector2(x + 36, y));
            Ui.Text(b, GlazeDesign.Where(band), new Vector2(x + 36, y + 28), Game1.textColor * 0.7f, 0.85f);
            y += 64;
        }
        var live = GlazeJudge.Judge(_glaze!, design);
        Ui.Text(b, "Glaze", new Vector2(x, y + 4), Game1.textColor * 0.7f);
        Ui.Text(b, I18n.Rating(live.Overall), new Vector2(x + 92, y + 4));

        int hy = panel.Bottom + 16;
        Ui.Text(b, "Banding", new Vector2(left.X + 8, hy));
        Ui.Text(b, Game1.parseText("Dip first. A ring takes a whole turn. Your hand drifts: watch the brush.", Game1.smallFont, left.Width - 16),
            new Vector2(left.X + 8, hy + 32), Game1.textColor * 0.75f);
    }

    // Right, while banding: the glazes (keys 1 to 6), the dip and the sponge.
    private void DrawGlazes(SpriteBatch b, int mx, int my)
    {
        var right = RightArea;
        var panel = new Rectangle(right.X, right.Y, right.Width, 358);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "Glazes", panel.Center.X, panel.Y + 18);
        const int cell = 56, gap = 20;
        int gx = panel.Center.X - (3 * cell + 2 * gap) / 2;
        for (int i = 0; i < Glazes.All.Length; i++)
        {
            var r = new Rectangle(gx + i % 3 * (cell + gap), panel.Y + 56 + i / 3 * (cell + 10), cell, cell);
            Ui.Button(b, r, "", selected: _glazeIdx == i, hovered: r.Contains(mx, my));
            Swatch(b, new Rectangle(r.X + 12, r.Y + 12, cell - 24, cell - 24), i);
            _buttons[$"g{i}"] = r;
            // none of its mineral (and not on this pot yet): dimmed
            if (GlazeStock != null && !_glazesUsed.Contains(i) && GlazeStock(i) == 0)
                Ui.Fill(b, new Rectangle(r.X + 6, r.Y + 6, cell - 12, cell - 12), new Color(40, 25, 15) * 0.6f);
        }
        var g = Glazes.All[_glazeIdx];
        string label = GlazeStock == null ? $"{_glazeIdx + 1}  {g.Name}"
            : _glazesUsed.Contains(_glazeIdx) ? $"{g.Name}: on the pot"
            : $"{g.Name}: {g.Mineral} ({GlazeStock(_glazeIdx)})";
        Ui.TextCentered(b, label, panel.Center.X, panel.Y + 192, GlazeStock != null && !_glazesUsed.Contains(_glazeIdx) && GlazeStock(_glazeIdx) == 0 ? new Color(170, 50, 30) : Game1.textColor);
        var dip = new Rectangle(panel.X + 20, panel.Y + 236, panel.Width - 40, 48);
        var sponge = new Rectangle(dip.X, dip.Bottom + 10, dip.Width, 48);
        Ui.Button(b, dip, $"Dip in {g.Name.ToLowerInvariant()}", hovered: dip.Contains(mx, my));
        Ui.Button(b, sponge, "Sponge off bands", enabled: _glaze!.AnyBands, hovered: sponge.Contains(mx, my));
        _buttons["dip"] = dip;
        if (_glaze.AnyBands)
            _buttons["sponge"] = sponge;
    }

    // Left, while firing: the firing's chart, as a pyrometer draws it. The band climbs
    // to the peak and holds; the heat so far is the line; "now" moves across.
    private void DrawFiringChart(SpriteBatch b)
    {
        var left = LeftArea;
        var kiln = _kiln!;
        float ahead = kiln.Forecast(3f);
        var panel = new Rectangle(left.X, left.Y, left.Width, 336);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "The firing", panel.Center.X, panel.Y + 18);
        var chart = new Rectangle(panel.X + 22, panel.Y + 58, panel.Width - 44, 190);
        Ui.Fill(b, chart, new Color(70, 46, 34));
        int Y(float heat) => chart.Bottom - (int)(Math.Clamp(heat / 100f, 0f, 1f) * chart.Height);
        for (int x = 0; x < chart.Width; x += 2)
        {
            float t = x / (float)chart.Width * Kiln.Duration;
            float mid = kiln.Target(t);
            int top = Y(mid + kiln.Band), bottom = Y(mid - kiln.Band);
            Ui.Fill(b, new Rectangle(chart.X + x, top, 2, Math.Max(2, bottom - top)), new Color(110, 170, 80) * 0.8f);
        }
        foreach (var p in kiln.Trace)
        {
            int x = chart.X + (int)(p.X / Kiln.Duration * chart.Width);
            float d = p.Y - kiln.Target(p.X);
            Color c = MathF.Abs(d) <= kiln.Band ? new Color(255, 236, 170) : d < 0 ? new Color(130, 170, 240) : new Color(240, 90, 60);
            Ui.Fill(b, new Rectangle(x - 2, Y(p.Y) - 2, 4, 4), c);
        }
        int now = chart.X + (int)(kiln.Time / Kiln.Duration * chart.Width);
        // where the fire is taking it, three seconds on: a hollow marker ahead of the line
        if (kiln.Time + 3f < Kiln.Duration)
        {
            int fx = chart.X + (int)((kiln.Time + 3f) / Kiln.Duration * chart.Width), fy = Y(ahead);
            Ui.Outline(b, new Rectangle(fx - 4, fy - 4, 9, 9), Color.White * 0.85f, 2);
        }
        Ui.Fill(b, new Rectangle(now - 1, chart.Y, 2, chart.Height), Color.White * 0.5f);
        Ui.Outline(b, chart, new Color(40, 25, 15), 2);

        float off = kiln.Heat - kiln.Target(kiln.Time);
        bool inBand = MathF.Abs(off) <= kiln.Band;
        string state = inBand ? "In the band" : off < 0 ? "Too cool" : "Too hot";
        Ui.TextCentered(b, state, panel.Center.X, chart.Bottom + 14,
            inBand ? new Color(60, 120, 40) : off < 0 ? new Color(50, 80, 170) : new Color(170, 50, 30));

        string heading = ahead > kiln.Heat + 1.5f ? "The fire is lifting it" : ahead < kiln.Heat - 1.5f ? "It's cooling" : "Holding steady";
        Ui.TextCentered(b, heading, panel.Center.X, chart.Bottom + 44, Game1.textColor * 0.7f, 0.85f);
    }

    // Right, while firing: the fuels (keys 1 to 3).
    private void DrawFuel(SpriteBatch b, int mx, int my)
    {
        var right = RightArea;
        var kiln = _kiln!;
        var panel = new Rectangle(right.X, right.Y, right.Width, 250);
        Ui.Paper(b, panel);
        Ui.TextCentered(b, "Fuel", panel.Center.X, panel.Y + 18);
        string[] keys = { "wood", "hard", "coal" };
        int y = panel.Y + 56;
        for (int i = 0; i < Kiln.Fuels.Length; i++)
        {
            var r = new Rectangle(panel.X + 20, y, panel.Width - 40, 48);
            Ui.Button(b, r, $"{i + 1}  {Kiln.Fuels[i].Name}", hovered: r.Contains(mx, my));
            _buttons[keys[i]] = r;
            y += 58;
        }
        int hy = panel.Bottom + 16;
        foreach (var fuel in Kiln.Fuels)
        {
            string line = Game1.parseText($"{fuel.Name}: {fuel.Hint.ToLowerInvariant()}", Game1.smallFont, right.Width - 16);
            Ui.Text(b, line, new Vector2(right.X + 8, hy), Game1.textColor * 0.75f, 1f);
            hy += (int)Game1.smallFont.MeasureString(line).Y + 6;
        }
        Ui.Text(b, $"In the fire: {kiln.Burning}", new Vector2(right.X + 8, hy + 6));
    }

    private void DrawResults(SpriteBatch b)
    {
        var right = RightArea;
        var r = _result!;
        int y = right.Y + 4;
        if (_pot.Collapsed || _fireResult is { Verdict: "Cracked" })
        {
            Ui.Text(b, _pot.Collapsed ? "It collapsed." : "It cracked.", new Vector2(right.X + 8, y), new Color(170, 50, 30));
            y += 34;
        }
        Ui.Stars(b, Stars, right.X + 8, y, 40, 6);
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
        if (_glazeResult is { } g)
            Row("Glaze", I18n.Rating(g.Overall));
        if (_fireResult is { } f)
            Row("Firing", f.Verdict);
        if (_xp > 0)
            Row("Pottery", $"+{_xp} XP");
        PotteryPieces.DrawKept(b, _pot, right.X + 8, y + 12, right.Width - 16);
    }

    private void DrawBottomBar(SpriteBatch b, int mx, int my)
    {
        var bottom = BottomArea;
        string line = _stage switch
        {
            Stage.Centring => "Hold the mouse button on the clay and keep your hand over it.",
            Stage.Throwing => "Hold the mouse on the pot to work it at that height. Match the chalk line.",
            Stage.Banding => "Hold the brush on the turning pot to paint a ring. Match the coloured marks.",
            Stage.Firing => "Feed the fire (1, 2, 3) to keep the heat in the band as it climbs.",
            _ => "",
        };
        if (line.Length > 0)
            Ui.Text(b, line, new Vector2(bottom.X, bottom.Y + 12));
        if (_stage is Stage.Throwing or Stage.Banding)
        {
            var r = new Rectangle(bottom.Right - 180, bottom.Y, 180, bottom.Height);
            Ui.Button(b, r, "Finished", hovered: r.Contains(mx, my));
            _buttons["done"] = r;
        }
        else if (_stage == Stage.Results && AtWheel)
        {
            // one piece of work: the next step only (and a fresh start after the throw)
            var main = new Rectangle(bottom.Right - 220, bottom.Y, 220, bottom.Height);
            if (_glazeResult == null && !_pot.Collapsed)
            {
                Ui.Button(b, main, "Glaze it", hovered: main.Contains(mx, my));
                _buttons["glaze"] = main;
            }
            else if (_fireResult == null && !_pot.Collapsed)
            {
                Ui.Button(b, main, "Fire it", hovered: main.Contains(mx, my));
                _buttons["fire"] = main;
            }
            else
            {
                Ui.Button(b, main, _pot.Collapsed ? "Leave" : "Done", hovered: main.Contains(mx, my));
                _buttons["leave"] = main;
            }
            if (_glazeResult == null)
            {
                int lumps = LumpsFor?.Invoke(_shape.Name) ?? 1;
                var again = new Rectangle(main.X - 300, bottom.Y, 280, bottom.Height);
                Ui.Button(b, again, $"Throw again ({lumps} {(lumps == 1 ? "lump" : "lumps")})", hovered: again.Contains(mx, my));
                _buttons["again"] = again;
            }
        }
        else if (_stage == Stage.Results)
        {
            var next = new Rectangle(bottom.Right - 200, bottom.Y, 200, bottom.Height);
            var again = new Rectangle(next.X - 220, bottom.Y, 200, bottom.Height);
            Ui.Button(b, again, "Throw again", hovered: again.Contains(mx, my));
            Ui.Button(b, next, "Next shape", hovered: next.Contains(mx, my));
            _buttons["again"] = again;
            _buttons["next"] = next;
            if (!_pot.Collapsed)
            {
                var glaze = new Rectangle(again.X - 220, bottom.Y, 200, bottom.Height);
                Ui.Button(b, glaze, _glazeResult != null ? "Glaze again" : "Glaze it", hovered: glaze.Contains(mx, my));
                _buttons["glaze"] = glaze;
                if (_glazeResult != null)
                {
                    var fire = new Rectangle(glaze.X - 220, bottom.Y, 200, bottom.Height);
                    Ui.Button(b, fire, _fireResult != null ? "Fire again" : "Fire it", hovered: fire.Contains(mx, my));
                    _buttons["fire"] = fire;
                }
            }
        }
    }
}

internal static class PotteryProto
{
    // A menu as Tobin's wheel opens it (Vale/ValeClay.cs), for the snapshots: clay always there,
    // two lumps for the tall shapes, and a few of some glazes' minerals (none of others).
    private static PotteryMenu AtAWheel(PotteryMenu m)
    {
        m.TakeClay = _ => true;
        m.LumpsFor = shape => shape is "vase" or "jug" ? 2 : 1;
        m.GlazeStock = glaze => glaze % 3 == 2 ? 0 : glaze * 4 + 1;
        m.TakeGlaze = _ => true;
        return m;
    }

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_pottery", "The pottery wheel prototype (1.5). Usage: museart_pottery [cup|bowl|vase|jug] [simplified|normal|hard] [throw|bot|wreck|band|fire] (throw: skip centring; bot: a greedy potter throws it, to check the rules; wreck: pull till it collapses; band: the bot throws it, then you glaze it; fire: the bots throw and glaze it, then you fire it)", (_, args) =>
        {
            if (!Context.IsWorldReady)
                return;
            string shape = args.Length > 0 ? args[0] : "";
            string rules = args.Length > 1 ? args[1] : "normal";
            PotteryArt.Load(helper);
            Game1.activeClickableMenu = new PotteryMenu(shape, PotterySkill.RulesFor(Game1.player, PotteryRules.For(rules)), args.Contains("throw"), args.Contains("bot"), args.Contains("wreck"), args.Contains("band"), args.Contains("fire"));
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
                ("collapsed_tired", () => new PotteryMenu("vase", PotteryRules.For("hard"), wreck: true)),
                ("banding", () => new PotteryMenu("vase", rules, band: true)),
                ("banding_brush", () => { var m = new PotteryMenu("vase", rules, band: true); m.GlazeBot(1.2f, finish: false, bands: 1); m.FakeMouse = m.SceneCentre(0.45f); return m; }),
                ("results_glazed", () => { var m = new PotteryMenu("jug", rules, band: true); m.GlazeBot(1.2f); return m; }),
                ("firing_early", () => { var m = new PotteryMenu("vase", rules, fire: true); m.FireBot("steady", until: 6f, finish: false); return m; }),
                ("firing", () => { var m = new PotteryMenu("vase", rules, fire: true); m.FireBot("steady", until: 22f, finish: false); return m; }),
                ("results_fired", () => { var m = new PotteryMenu("jug", rules, fire: true); m.FireBot("steady"); return m; }),
                ("results_cracked", () => { var m = new PotteryMenu("cup", PotteryRules.For("hard"), fire: true); m.FireBot("coal"); return m; }),
                // at a wheel: one piece of work, the next step only; the swatches show minerals in hand
                ("wheel_results", () => AtAWheel(new PotteryMenu("vase", rules, bot: true))),
                ("wheel_collapsed", () => AtAWheel(new PotteryMenu("jug", rules, wreck: true))),
                ("wheel_banding", () => AtAWheel(new PotteryMenu("vase", rules, band: true))),
                ("wheel_glazed", () => { var m = AtAWheel(new PotteryMenu("jug", rules, band: true)); m.GlazeBot(1.2f); return m; }),
                ("wheel_fired", () => { var m = AtAWheel(new PotteryMenu("cup", rules, fire: true)); m.FireBot("steady"); return m; }),
            };
            // the firing against the bots' stoking, on each difficulty (the averages of 10)
            foreach (string level in new[] { "simplified", "normal", "hard" })
                foreach (string mode in new[] { "steady", "eager", "coal", "none" })
                {
                    float sum = 0f;
                    for (int k = 0; k < 10; k++)
                    {
                        var m = new PotteryMenu(PotShape.Names[k % 4], PotteryRules.For(level), fire: true);
                        m.FireBot(mode);
                        sum += m.FireScore;
                        m.exitThisMenuNoSound();
                    }
                    monitor.Log($"firing: {level} {mode}: {sum / 10f:0.00}", LogLevel.Info);
                }
            // the glaze judge against the bot's hands: perfect, and wandering as Normal's and
            // Hard's do, never corrected (the averages of 20)
            foreach (string shape in PotShape.Names)
                foreach (var (label, reach) in new[] { ("perfect", 0f), ("simplified", PotteryRules.For("simplified").Drift), ("normal", PotteryRules.NormalDrift), ("hard", PotteryRules.HardDrift) })
                {
                    float sum = 0f;
                    for (int k = 0; k < 20; k++)
                    {
                        var m = new PotteryMenu(shape, rules, band: true);
                        m.GlazeBot(reach);
                        sum += m.GlazeScore;
                        m.exitThisMenuNoSound();
                    }
                    monitor.Log($"glaze judge: {shape} {label}: {sum / 20f:0.00}", LogLevel.Info);
                }
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
