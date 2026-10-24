using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace ArtStudio.Menus;

internal enum PaintTool { Brush, Broad, Fill, Ruler, Knife, Dropper }

// Painting: the canvas against the light. Paint only goes on as far as the
// brush's load; reloading means mixing (EaselMenu.Mix). Inspiration sparks
// appear on the canvas now and then — paint them true for bonus light.
internal sealed partial class EaselMenu
{
    private static readonly Color CanvasColor = new(243, 236, 220);

    private PixelCanvas _canvas = new(PaintingRules.CanvasSize(CanvasKind.Small));
    private CanvasKind _canvasKind;
    private Reference? _reference;
    private string[] _subjectItems = Array.Empty<string>();
    private bool IsOriginal => _reference == null;

    // What's on the brush.
    private int _selected = -1;
    private Purity _loadPurity;
    private int _load, _loadMax;

    private PaintTool _tool = PaintTool.Brush;
    private bool _mirror;
    private bool _stroking, _erasing, _dryWarned;
    private Point _lastCell;
    private Point? _rulerStart;

    private float _lightLeft, _lightTotal;
    private bool _timed;

    // Colors already mixed this session, when paint doesn't run out: a mixed
    // color stays on the palette at the purity you managed until it dries
    // (_wet counts down the seconds of painting it has left).
    private readonly Dictionary<int, Purity> _mixed = new();
    private readonly Dictionary<int, float> _wet = new();

    // Inspiration sparks.
    private Point? _sparkCell;
    private float _sparkLeft, _sparkLife, _nextSpark;
    private int _sparksCaught;

    private int _mixesTotal, _mixesPure;
    private float _finishArmed;
    private bool _confirmLeave;

    // Layout.
    private Rectangle _topBar, _leftCol, _rightCol, _canvasArea, _noteArea, _refArea, _finishButton, _undoButton;
    private Rectangle _leaveBox, _leaveYes, _leaveNo;
    private Rectangle[] _dabs = Array.Empty<Rectangle>();
    private readonly Dictionary<PaintTool, Rectangle> _toolButtons = new();
    private Rectangle _mirrorButton;

    private void BeginPainting(CanvasKind kind, IReadOnlyList<string> itemIds)
    {
        _canvasKind = kind;
        int size = PaintingRules.CanvasSize(kind);
        _canvas = new PixelCanvas(size);
        _subjectItems = itemIds.ToArray();
        _reference = itemIds.Count > 0 ? ReferenceBuilder.StillLife(itemIds, size, _owned) : null;

        _timed = !IsOriginal && _rules.LightTimer;
        _lightTotal = _lightLeft = _timed
            ? PaintingRules.LightSeconds(size, _reference!.Difficulty, _level, LightMultiplier)
            : float.PositiveInfinity;
        _nextSpark = 5f + (float)_random.NextDouble() * 4f;
        _stage = EaselStage.Painting;
        LayoutPainting();
    }

    private void LayoutPainting()
    {
        int x = xPositionOnScreen + Pad + 12, y = yPositionOnScreen + Pad + 12;
        int innerW = width - 2 * (Pad + 12), innerH = height - 2 * (Pad + 12);

        _topBar = new Rectangle(x, y, innerW, TopBarHeight);
        int bodyY = _topBar.Bottom + 8, bodyH = innerH - TopBarHeight - BottomBarHeight - 16;
        _leftCol = new Rectangle(x, bodyY, SideWidth, bodyH);
        _rightCol = new Rectangle(x + innerW - SideWidth, bodyY, SideWidth, bodyH);

        int centerW = _rightCol.X - _leftCol.Right - 2 * Pad;
        int size = _canvas.Size;
        int side = Math.Min(bodyH, centerW) / size * size;
        _canvasArea = new Rectangle(_leftCol.Right + Pad + (centerW - side) / 2, bodyY + (bodyH - side) / 2, side, side);

        _noteArea = new Rectangle(_leftCol.X, _leftCol.Y, SideWidth, SideWidth + 40);
        int refSide = (SideWidth - 56) / size * size;
        _refArea = new Rectangle(_noteArea.X + (SideWidth - refSide) / 2, _noteArea.Y + 44, refSide, refSide);

        int bottomY = _leftCol.Bottom + 8;
        _finishButton = new Rectangle(x + innerW - 180, bottomY, 180, BottomBarHeight);

        // Palette: three columns of dabs, every pigment in the game (unowned
        // ones shown as faint stains, so the shop's wares are no surprise).
        // Then the tools, two to a row, with mirror and undo at the end.
        const int dabCols = 3, toolRows = 4;
        int dabRows = (Palette.All.Length + dabCols - 1) / dabCols;
        int toolH = 40;
        int dabH = Math.Clamp((bodyH - 30 - toolRows * (toolH + 6)) / dabRows - 6, 24, 40);
        int dabW = (SideWidth - 16 - (dabCols - 1) * 6) / dabCols;
        _dabs = new Rectangle[Palette.All.Length];
        for (int i = 0; i < _dabs.Length; i++)
            _dabs[i] = new Rectangle(_rightCol.X + 8 + i % dabCols * (dabW + 6), _rightCol.Y + 8 + i / dabCols * (dabH + 6), dabW, dabH);

        int toolsY = _dabs[^1].Bottom + 14;
        int toolW = (SideWidth - 22) / 2;
        Rectangle ToolSlot(int i) => new(_rightCol.X + 8 + i % 2 * (toolW + 6), toolsY + i / 2 * (toolH + 6), toolW, toolH);
        _toolButtons.Clear();
        var tools = (PaintTool[])Enum.GetValues(typeof(PaintTool));
        for (int i = 0; i < tools.Length; i++)
            _toolButtons[tools[i]] = ToolSlot(i);
        _mirrorButton = ToolSlot(tools.Length);
        _undoButton = ToolSlot(tools.Length + 1);

        int boxW = 600, boxH = 220;
        _leaveBox = new Rectangle(xPositionOnScreen + (width - boxW) / 2, yPositionOnScreen + (height - boxH) / 2, boxW, boxH);
        _leaveYes = new Rectangle(_leaveBox.X + 60, _leaveBox.Bottom - 92, 200, 56);
        _leaveNo = new Rectangle(_leaveBox.Right - 260, _leaveBox.Bottom - 92, 200, 56);

        LayoutMix();
    }

    private bool ToolUnlocked(PaintTool tool) => tool switch
    {
        PaintTool.Broad => _level >= PaintingRules.BroadBrushLevel,
        PaintTool.Ruler => _level >= PaintingRules.RulerLevel,
        PaintTool.Dropper => _level >= PaintingRules.EyedropperLevel,
        _ => true,
    };

    private bool MirrorUnlocked => _level >= PaintingRules.MirrorLevel;

    private static string ToolName(PaintTool tool) => tool switch
    {
        PaintTool.Brush => "Brush",
        PaintTool.Broad => "Broad",
        PaintTool.Fill => "Fill",
        PaintTool.Ruler => "Ruler",
        PaintTool.Knife => "Knife",
        _ => "Dropper",
    };

    private static string ToolHelp(PaintTool tool) => tool switch
    {
        PaintTool.Brush => "Brush (B): one cell at a time.",
        PaintTool.Broad => "Broad brush (G): 2x2 cells at a time.",
        PaintTool.Fill => "Fill (F): floods an area, as far as your paint goes.",
        PaintTool.Ruler => "Ruler (L): drag for a perfectly straight line.",
        PaintTool.Knife => "Palette knife (K): scrapes paint off. Right-click does too.",
        _ => "Dropper (D): click a cell to mix its color.",
    };

    private static int ToolLevel(PaintTool tool) => tool switch
    {
        PaintTool.Broad => PaintingRules.BroadBrushLevel,
        PaintTool.Ruler => PaintingRules.RulerLevel,
        PaintTool.Dropper => PaintingRules.EyedropperLevel,
        _ => 0,
    };

    // ---- Update ----

    private void UpdatePainting(float dt)
    {
        if (_confirmLeave) return;

        // On the gentler difficulties the world holds still while you mix.
        bool paused = _mix != null && _rules.PauseLightWhileMixing;
        if (_timed && !paused)
        {
            _lightLeft -= dt;
            if (_lightLeft <= 0f)
            {
                _lightLeft = 0f;
                Finish(lightGone: true);
                return;
            }
        }
        if (!IsOriginal && !paused)
            UpdateSpark(dt);
        if (!paused)
            DryColors(dt);
        if (_finishArmed > 0f) _finishArmed -= dt;

        if (_mix != null)
        {
            UpdateMix();
            return;
        }

        UpdateCanvasInput();
        CheckSparkCaught();
    }

    private Point? CellAt(Point p)
    {
        if (!_canvasArea.Contains(p)) return null;
        int n = _canvas.Size;
        return new Point((p.X - _canvasArea.X) * n / _canvasArea.Width, (p.Y - _canvasArea.Y) * n / _canvasArea.Height);
    }

    private Point CellAtClamped(Point p)
    {
        int n = _canvas.Size;
        int x = Math.Clamp((p.X - _canvasArea.X) * n / Math.Max(1, _canvasArea.Width), 0, n - 1);
        int y = Math.Clamp((p.Y - _canvasArea.Y) * n / Math.Max(1, _canvasArea.Height), 0, n - 1);
        return new Point(x, y);
    }

    private int BrushSize => _tool == PaintTool.Broad ? 2 : 1;

    private void UpdateCanvasInput()
    {
        var mouse = MousePos;
        var cell = CellAt(mouse);

        // Right button: palette knife, scrapes paint back to bare canvas.
        if (RightPressed && cell.HasValue)
        {
            _canvas.PushUndo();
            _erasing = true;
            _lastCell = cell.Value;
            Scrape(cell.Value, cell.Value);
        }
        else if (_erasing && RightDown && cell.HasValue)
        {
            Scrape(_lastCell, cell.Value);
            _lastCell = cell.Value;
        }
        if (!RightDown) _erasing = false;

        if (LeftPressed && cell.HasValue)
        {
            _dryWarned = false;
            switch (_tool)
            {
                case PaintTool.Brush:
                case PaintTool.Broad:
                case PaintTool.Knife:
                    _canvas.PushUndo();
                    _stroking = true;
                    _lastCell = cell.Value;
                    StrokeTo(cell.Value, cell.Value);
                    break;
                case PaintTool.Fill:
                    _canvas.PushUndo();
                    FillAt(cell.Value);
                    break;
                case PaintTool.Ruler:
                    _rulerStart = cell.Value;
                    break;
                case PaintTool.Dropper:
                    byte picked = _canvas[cell.Value.X, cell.Value.Y];
                    if (picked == PixelCanvas.Blank && _reference != null)
                        picked = _reference.Cells[cell.Value.X, cell.Value.Y];
                    if (picked != PixelCanvas.Blank && _owned.Contains(picked))
                        PickPigment(picked);
                    break;
            }
        }
        else if (_stroking && LeftDown && cell.HasValue)
        {
            StrokeTo(_lastCell, cell.Value);
            _lastCell = cell.Value;
        }

        if (LeftReleased)
        {
            if (_rulerStart.HasValue)
            {
                _canvas.PushUndo();
                StrokeTo(_rulerStart.Value, CellAtClamped(mouse));
                _rulerStart = null;
            }
            _stroking = false;
        }
    }

    private void Scrape(Point from, Point to)
    {
        int budget = int.MaxValue;
        foreach (var (a, b) in Mirrored(from, to))
            _canvas.Line(a.X, a.Y, b.X, b.Y, PixelCanvas.Blank, Purity.Pure, BrushSize, ref budget);
    }

    private void StrokeTo(Point from, Point to)
    {
        if (_tool == PaintTool.Knife)
        {
            Scrape(from, to);
            return;
        }
        if (!HasPaint()) return;
        foreach (var (a, b) in Mirrored(from, to))
            _canvas.Line(a.X, a.Y, b.X, b.Y, (byte)_selected, _loadPurity, BrushSize, ref _load);
        AfterPaint();
    }

    private void FillAt(Point cell)
    {
        if (!HasPaint()) return;
        _canvas.Fill(cell.X, cell.Y, (byte)_selected, _loadPurity, ref _load);
        AfterPaint();
    }

    // With the mirror on, every stroke is repeated reflected across the
    // canvas's vertical center line.
    private IEnumerable<(Point, Point)> Mirrored(Point from, Point to)
    {
        yield return (from, to);
        if (!_mirror) yield break;
        int n = _canvas.Size, s = BrushSize;
        yield return (new Point(n - from.X - s, from.Y), new Point(n - to.X - s, to.Y));
    }

    private bool HasPaint()
    {
        if (_selected >= 0 && _load > 0) return true;
        if (!_dryWarned)
        {
            _dryWarned = true;
            Game1.playSound("cancel");
            Toast(_selected < 0 ? "Pick a pigment and mix it first."
                : _rules.PaintLoads ? "Your brush is dry. Mix more paint."
                : $"Your {Palette.All[_selected].Name} has dried. Mix it again.", Color.OrangeRed);
        }
        return false;
    }

    private void AfterPaint()
    {
        if (_load <= 0 && !_dryWarned)
        {
            _dryWarned = true;
            Toast("Brush is empty.", Color.OrangeRed, 1f);
        }
    }

    // Mixed colors dry as you paint. A dried color leaves the palette (and
    // the brush, if it was on it) and has to be mixed again.
    private void DryColors(float dt)
    {
        if (_rules.PaintLoads || float.IsPositiveInfinity(_rules.DryTime) || _wet.Count == 0) return;
        foreach (int pigment in _wet.Keys.ToList())
        {
            _wet[pigment] -= dt;
            if (_wet[pigment] > 0f) continue;
            _wet.Remove(pigment);
            _mixed.Remove(pigment);
            if (pigment == _selected)
            {
                _load = 0;
                _dryWarned = true;
                Game1.playSound("dwop");
                Toast($"Your {Palette.All[pigment].Name} has dried.", new Color(190, 130, 30));
            }
        }
    }

    // ---- Sparks ----

    private void UpdateSpark(float dt)
    {
        if (_sparkCell.HasValue)
        {
            _sparkLeft -= dt;
            if (_sparkLeft <= 0f) _sparkCell = null;
            return;
        }

        _nextSpark -= dt;
        if (_nextSpark > 0f) return;
        _nextSpark = (7f + (float)_random.NextDouble() * 5f) * (Has(PaintingSkill.Instance.PleinAir) ? PaintingSkill.PleinAirSparkInterval : 1f);

        var candidates = new List<Point>();
        int n = _canvas.Size;
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        {
            byte want = _reference!.Cells[x, y];
            if (want == PixelCanvas.Blank) continue;
            if (_canvas[x, y] == want && _canvas.PurityAt(x, y) == Purity.Pure) continue;
            candidates.Add(new Point(x, y));
        }
        if (candidates.Count == 0) return;

        _sparkCell = candidates[_random.Next(candidates.Count)];
        _sparkLife = _sparkLeft = _rules.SparkLifetime + _brush.SparkTime;
        Game1.playSound("dwop");
    }

    // A spark is caught by painting its cell the reference's color, mixed pure.
    private void CheckSparkCaught()
    {
        if (!_sparkCell.HasValue || _reference == null) return;
        var c = _sparkCell.Value;
        if (_canvas[c.X, c.Y] != _reference.Cells[c.X, c.Y] || _canvas.PurityAt(c.X, c.Y) != Purity.Pure) return;

        _sparkCell = null;
        _sparksCaught++;
        if (_timed) _lightLeft += PaintingRules.SparkLightBonus;
        Game1.playSound("newArtifact");

        string text = _sparksCaught == PaintingRules.SparksForInspired ? "Inspired!"
            : _timed ? $"Spark! +{PaintingRules.SparkLightBonus:0}s light"
            : $"Spark! {_sparksCaught}/{PaintingRules.SparksForInspired}";
        if (_random.NextDouble() < 0.04 && TryFindRarePigment() is int rare)
        {
            ArtProgress.Unlock(_who, rare);
            text = $"Spark! You found {Palette.All[rare].Name} pigment!";
        }
        Toast(text, new Color(214, 160, 30), 2f);
    }

    private int? TryFindRarePigment()
    {
        var rare = Enumerable.Range(0, Palette.All.Length)
            .Where(i => Palette.All[i].Rare && !ArtProgress.Owns(_who, i))
            .ToList();
        return rare.Count > 0 ? rare[_random.Next(rare.Count)] : null;
    }

    // ---- Clicks and keys ----

    private void ClickPainting(int x, int y)
    {
        if (_confirmLeave)
        {
            if (_leaveYes.Contains(x, y))
            {
                _confirmLeave = false;
                _stage = EaselStage.Results;
                exitThisMenu();
            }
            else if (_leaveNo.Contains(x, y))
            {
                _confirmLeave = false;
                Game1.playSound("smallSelect");
            }
            return;
        }
        if (_mix != null) return;

        for (int i = 0; i < _dabs.Length; i++)
        {
            if (!_dabs[i].Contains(x, y)) continue;
            if (_owned.Contains(i))
                PickPigment(i);
            else
            {
                Game1.playSound("cancel");
                Toast($"You don't have {Palette.All[i].Name} yet.", Color.OrangeRed);
            }
            return;
        }

        foreach (var (tool, rect) in _toolButtons)
        {
            if (!rect.Contains(x, y)) continue;
            SelectTool(tool);
            return;
        }

        if (_mirrorButton.Contains(x, y)) ToggleMirror();
        else if (_undoButton.Contains(x, y)) Undo();
        else if (_finishButton.Contains(x, y)) TryFinish();
    }

    private void KeyPainting(Keys key)
    {
        if (_confirmLeave)
        {
            if (key == Keys.Escape || key == Keys.N) _confirmLeave = false;
            else if (key == Keys.Y || key == Keys.Enter) ClickPainting(_leaveYes.Center.X, _leaveYes.Center.Y);
            return;
        }
        if (_mix != null)
        {
            if (key == Keys.Escape) CancelMix();
            return;
        }

        switch (key)
        {
            case Keys.Escape:
                _confirmLeave = true;
                Game1.playSound("bigDeSelect");
                return;
            case Keys.Enter: TryFinish(); return;
            case Keys.B: SelectTool(PaintTool.Brush); return;
            case Keys.G: SelectTool(PaintTool.Broad); return;
            case Keys.F: SelectTool(PaintTool.Fill); return;
            case Keys.L: SelectTool(PaintTool.Ruler); return;
            case Keys.K: SelectTool(PaintTool.Knife); return;
            case Keys.D: SelectTool(PaintTool.Dropper); return;
            case Keys.M: ToggleMirror(); return;
            case Keys.Z: Undo(); return;
        }

        // 1-9, 0 pick the first ten owned pigments, in palette order.
        int slot = key switch
        {
            >= Keys.D1 and <= Keys.D9 => key - Keys.D1,
            Keys.D0 => 9,
            _ => -1,
        };
        if (slot >= 0 && slot < _owned.Count)
            PickPigment(_owned[slot]);
    }

    private void SelectTool(PaintTool tool)
    {
        if (!ToolUnlocked(tool))
        {
            Game1.playSound("cancel");
            Toast($"{ToolName(tool)} unlocks at Painting level {ToolLevel(tool)}.", Color.OrangeRed);
            return;
        }
        _tool = tool;
        Game1.playSound("smallSelect");
    }

    private void ToggleMirror()
    {
        if (!MirrorUnlocked)
        {
            Game1.playSound("cancel");
            Toast($"Mirror unlocks at Painting level {PaintingRules.MirrorLevel}.", Color.OrangeRed);
            return;
        }
        _mirror = !_mirror;
        Game1.playSound("smallSelect");
    }

    private void Undo()
    {
        if (!_canvas.CanUndo) return;
        _canvas.Undo();
        Game1.playSound("shwip");
    }

    // Finish needs a second click within two seconds, so a stray click
    // can't end a painting early.
    private void TryFinish()
    {
        if (_canvas.IsEmpty)
        {
            Game1.playSound("cancel");
            Toast("Paint something first.", Color.OrangeRed);
            return;
        }
        if (_finishArmed <= 0f)
        {
            _finishArmed = 2f;
            Game1.playSound("smallSelect");
            return;
        }
        Finish(lightGone: false);
    }

    // ---- Draw ----

    private void DrawPainting(SpriteBatch b)
    {
        var mouse = MousePos;
        DrawTopBar(b);
        DrawNote(b);
        DrawCanvas(b, mouse);
        if (_mix != null)
            DrawMix(b);
        else
            DrawPalette(b, mouse);
        DrawBottomBar(b, mouse);

        if (_toastLeft > 0f && _toast != null)
            DrawToast(b);

        if (_confirmLeave)
            DrawLeavePrompt(b, mouse);
    }

    // A short message on a dark plate across the top of the canvas, so it
    // never collides with the bars and labels around it.
    private void DrawToast(SpriteBatch b)
    {
        float alpha = Math.Min(1f, _toastLeft * 2f);
        var lines = Game1.parseText(_toast, Game1.smallFont, _canvasArea.Width - 40);
        var size = Game1.smallFont.MeasureString(lines);
        var plate = new Rectangle(_canvasArea.Center.X - (int)size.X / 2 - 14, _canvasArea.Y + 10, (int)size.X + 28, (int)size.Y + 12);
        Ui.Fill(b, plate, new Color(40, 25, 15) * (0.85f * alpha));
        Ui.Text(b, lines, new Vector2(plate.X + 14, plate.Y + 6), Color.Lerp(_toastColor, Color.White, 0.35f) * alpha);
    }

    private void DrawTopBar(SpriteBatch b)
    {
        string title = IsOriginal ? "Free canvas" : _subjectItems.Length == 1
            ? "Still Life: " + ItemRegistry.GetDataOrErrorItem(_subjectItems[0]).DisplayName
            : "Still Life";
        Ui.Text(b, title, new Vector2(_topBar.X, _topBar.Y + 4));
        if (_reference != null)
            Ui.Text(b, $"Difficulty {_reference.Difficulty}", new Vector2(_topBar.X, _topBar.Y + 34), Game1.textColor * 0.7f);

        if (IsOriginal) return;

        string sparks = $"Sparks {_sparksCaught}/{PaintingRules.SparksForInspired}";
        Ui.Text(b, sparks, new Vector2(_topBar.Right - Ui.TextWidth(sparks), _topBar.Y + 14),
            _sparksCaught >= PaintingRules.SparksForInspired ? new Color(150, 100, 0) : Game1.textColor);
        if (!_timed) return;

        // The light meter: the session's clock, draining warm to dusk.
        int barW = Math.Min(520, _topBar.Width - 560);
        var bar = new Rectangle(_topBar.Center.X - barW / 2 + 60, _topBar.Y + 14, barW, 28);
        Ui.Text(b, "Light", new Vector2(bar.X - 64, bar.Y));
        Ui.Fill(b, bar, new Color(60, 40, 30));
        float frac = Math.Clamp(_lightLeft / _lightTotal, 0f, 1f);
        Ui.Fill(b, new Rectangle(bar.X + 3, bar.Y + 3, (int)((bar.Width - 6) * frac), bar.Height - 6), LightColor(frac));
        Ui.Outline(b, bar, new Color(40, 25, 15), 2);
        // Seconds left, inside the bar's right end; "paused" while the light waits for a mix.
        bool waiting = _mix != null && _rules.PauseLightWhileMixing;
        string left = waiting ? $"{Math.Ceiling(_lightLeft):0}s, paused" : $"{Math.Ceiling(_lightLeft):0}s";
        Ui.Text(b, left, new Vector2(bar.Right - 10 - Ui.TextWidth(left) * 0.8f, bar.Y + 2), Color.White * 0.9f, 0.8f);
    }

    private static Color LightColor(float frac) => frac switch
    {
        > 0.5f => Color.Lerp(new Color(240, 160, 60), new Color(255, 214, 90), (frac - 0.5f) * 2f),
        > 0.2f => Color.Lerp(new Color(200, 90, 70), new Color(240, 160, 60), (frac - 0.2f) / 0.3f),
        _ => Color.Lerp(new Color(70, 80, 150), new Color(200, 90, 70), frac / 0.2f),
    };

    // How much the scene has dimmed (painted over canvas and reference).
    private Color DuskTint()
    {
        if (!_timed) return Color.Transparent;
        float frac = Math.Clamp(_lightLeft / _lightTotal, 0f, 1f);
        if (frac < 0.15f) return new Color(30, 30, 90) * ((0.15f - frac) / 0.15f * 0.35f);
        if (frac < 0.4f) return new Color(255, 140, 40) * ((0.4f - frac) / 0.25f * 0.12f);
        return Color.Transparent;
    }

    private void DrawNote(SpriteBatch b)
    {
        Ui.Paper(b, _noteArea);
        if (_reference != null)
        {
            Ui.TextCentered(b, "Reference", _noteArea.Center.X, _noteArea.Y + 12);
            DrawCells(b, _reference.Cells, null, _refArea, CanvasColor, false);
            Ui.Fill(b, _refArea, DuskTint());
            if (_sparkCell.HasValue)
                DrawSpark(b, CellRect(_refArea, _canvas.Size, _sparkCell.Value.X, _sparkCell.Value.Y));
        }
        else
        {
            var lines = Game1.parseText("No reference: paint whatever you like. Originals aren't judged.", Game1.smallFont, SideWidth - 40);
            Ui.Text(b, lines, new Vector2(_noteArea.X + 20, _noteArea.Y + 24));
        }

        // What's on the brush.
        int y = _noteArea.Bottom + 16;
        Ui.Text(b, "On the brush", new Vector2(_leftCol.X, y));
        var swatch = new Rectangle(_leftCol.X, y + 34, 48, 48);
        if (_selected >= 0)
        {
            Ui.Fill(b, swatch, PaintingRenderer.CellColor((byte)_selected, _loadPurity));
            Ui.Outline(b, swatch, Game1.textColor, 2);
            Ui.Text(b, Palette.All[_selected].Name, new Vector2(swatch.Right + 12, swatch.Y));
            Ui.Text(b, PurityLabel(_loadPurity), new Vector2(swatch.Right + 12, swatch.Y + 26), PurityColor(_loadPurity));
            if (_rules.PaintLoads)
            {
                var gauge = new Rectangle(_leftCol.X, swatch.Bottom + 12, SideWidth - 20, 16);
                Ui.Fill(b, gauge, new Color(60, 40, 30));
                float frac = _loadMax > 0 ? Math.Clamp(_load / (float)_loadMax, 0f, 1f) : 0f;
                Ui.Fill(b, new Rectangle(gauge.X + 2, gauge.Y + 2, (int)((gauge.Width - 4) * frac), gauge.Height - 4),
                    PaintingRenderer.CellColor((byte)_selected, _loadPurity));
                Ui.Text(b, $"{_load} cells left", new Vector2(gauge.X, gauge.Bottom + 6), Game1.textColor * 0.7f);
            }
            else
            {
                int hintY = swatch.Bottom + 10;
                if (_wet.TryGetValue(_selected, out float wet))
                {
                    var gauge = new Rectangle(_leftCol.X, hintY + 2, SideWidth - 20, 16);
                    Ui.Fill(b, gauge, new Color(60, 40, 30));
                    float frac = Math.Clamp(wet / _rules.DryTime, 0f, 1f);
                    Ui.Fill(b, new Rectangle(gauge.X + 2, gauge.Y + 2, (int)((gauge.Width - 4) * frac), gauge.Height - 4),
                        PaintingRenderer.CellColor((byte)_selected, _loadPurity));
                    Ui.Text(b, $"Wet for {Math.Ceiling(wet):0}s", new Vector2(gauge.X, gauge.Bottom + 4), Game1.textColor * 0.7f);
                    hintY = gauge.Bottom + 34;
                }
                else if (_load <= 0)
                {
                    Ui.Text(b, "Dried. Mix it again.", new Vector2(_leftCol.X, hintY), new Color(150, 70, 50));
                    hintY += 30;
                }
                if (_loadPurity != Purity.Pure && _load > 0)
                {
                    var hint = Game1.parseText("Click the color again to remix it.", Game1.smallFont, SideWidth - 10);
                    Ui.Text(b, hint, new Vector2(_leftCol.X, hintY), Game1.textColor * 0.7f);
                }
            }
        }
        else
        {
            Ui.Fill(b, swatch, Color.Black * 0.1f);
            Ui.Text(b, "Nothing yet", new Vector2(swatch.Right + 12, swatch.Y + 12), Game1.textColor * 0.6f);
        }
    }

    private static string PurityLabel(Purity p) => p switch
    {
        Purity.Pure => "Pure",
        Purity.Off => "A bit off",
        _ => "Muddy",
    };

    private static Color PurityColor(Purity p) => p switch
    {
        Purity.Pure => new Color(60, 140, 60),
        Purity.Off => new Color(190, 130, 30),
        _ => new Color(150, 70, 50),
    };

    private void DrawCanvas(SpriteBatch b, Point mouse)
    {
        var border = new Rectangle(_canvasArea.X - 12, _canvasArea.Y - 12, _canvasArea.Width + 24, _canvasArea.Height + 24);
        Ui.Fill(b, border, new Color(110, 70, 40));
        Ui.Outline(b, border, new Color(60, 35, 20), 4);
        DrawCells(b, _canvas.Snapshot(), _canvas.PuritySnapshot(), _canvasArea, CanvasColor, true);

        // Faint grid so cells can be counted against the reference.
        int n = _canvas.Size;
        if (_canvasArea.Width / n >= 10)
        {
            var line = Color.Black * 0.06f;
            for (int i = 1; i < n; i++)
            {
                int gx = _canvasArea.X + _canvasArea.Width * i / n, gy = _canvasArea.Y + _canvasArea.Height * i / n;
                Ui.Fill(b, new Rectangle(gx, _canvasArea.Y, 1, _canvasArea.Height), line);
                Ui.Fill(b, new Rectangle(_canvasArea.X, gy, _canvasArea.Width, 1), line);
            }
        }

        if (_mirror)
            Ui.Fill(b, new Rectangle(_canvasArea.Center.X - 1, _canvasArea.Y, 2, _canvasArea.Height), new Color(80, 120, 200) * 0.5f);

        if (_sparkCell.HasValue)
            DrawSpark(b, CellRect(_canvasArea, n, _sparkCell.Value.X, _sparkCell.Value.Y));

        Ui.Fill(b, _canvasArea, DuskTint());

        if (_mix != null) return;

        // Ruler preview and cursor footprint.
        if (_rulerStart.HasValue && _selected >= 0)
        {
            var end = CellAtClamped(mouse);
            foreach (var (x, y) in PixelCanvas.LinePoints(_rulerStart.Value.X, _rulerStart.Value.Y, end.X, end.Y))
                Ui.Fill(b, CellRect(_canvasArea, n, x, y), PaintingRenderer.CellColor((byte)_selected, _loadPurity) * 0.5f);
        }
        var cell = CellAt(mouse);
        if (cell.HasValue)
        {
            int s = BrushSize;
            var tl = CellRect(_canvasArea, n, cell.Value.X, cell.Value.Y);
            var br = CellRect(_canvasArea, n, Math.Min(n - 1, cell.Value.X + s - 1), Math.Min(n - 1, cell.Value.Y + s - 1));
            Ui.Outline(b, new Rectangle(tl.X, tl.Y, br.Right - tl.X, br.Bottom - tl.Y), Color.Black * 0.5f, 2);
        }
    }

    private void DrawSpark(SpriteBatch b, Rectangle cell)
    {
        float t = float.IsPositiveInfinity(_sparkLife) ? 1f : _sparkLife > 0f ? _sparkLeft / _sparkLife : 0f;
        float pulse = 1f + 0.25f * MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 10f);
        int size = (int)(Math.Max(cell.Width, 12) * 1.6f * pulse);
        var dest = new Rectangle(cell.Center.X - size / 2, cell.Center.Y - size / 2, size, size);
        b.Draw(Game1.mouseCursors, dest, new Rectangle(346, 400, 8, 8), Color.White * Math.Clamp(t * 2f, 0.3f, 1f));
        Ui.Outline(b, cell, new Color(255, 230, 120), 2);
    }

    private void DrawPalette(SpriteBatch b, Point mouse)
    {
        Ui.Paper(b, new Rectangle(_rightCol.X - 8, _rightCol.Y - 8, _rightCol.Width + 16, _undoButton.Bottom - _rightCol.Y + 22));
        var used = _reference?.PigmentsUsed;
        for (int i = 0; i < _dabs.Length; i++)
        {
            var r = _dabs[i];
            var p = Palette.All[i];
            var color = new Color(p.R, p.G, p.B);
            bool owned = _owned.Contains(i);
            if (owned)
            {
                Ui.Fill(b, r, color);
                Ui.Outline(b, r, i == _selected ? Color.White : Color.Black * 0.5f, i == _selected ? 4 : 2);
                int slot = _owned.ToList().IndexOf(i);
                if (slot < 10)
                    Ui.Text(b, ((slot + 1) % 10).ToString(), new Vector2(r.X + 4, r.Y + 2), Luma(color) > 140 ? Game1.textColor : Color.White, 0.8f);
                if (used != null && used.Contains(i))
                    Ui.Fill(b, new Rectangle(r.Right - 12, r.Y + 6, 6, 6), Luma(color) > 140 ? Color.Black * 0.6f : Color.White * 0.9f);
                // Colors already mixed (when paint doesn't run out): a strip in the purity's color.
                if (_mixed.TryGetValue(i, out var mixed))
                {
                    float wetFrac = _wet.TryGetValue(i, out float wet) ? Math.Clamp(wet / _rules.DryTime, 0f, 1f) : 1f;
                    Ui.Fill(b, new Rectangle(r.X + 3, r.Bottom - 7, (int)((r.Width - 6) * wetFrac), 4), PurityColor(mixed));
                }
            }
            else
            {
                Ui.Fill(b, r, color * 0.15f);
                Ui.Outline(b, r, Color.Black * 0.15f, 1);
            }
            if (r.Contains(mouse))
                _hoverText = owned ? $"{p.Name}\nMix difficulty {p.Difficulty} ({p.Motion})" : $"{p.Name}\n(not owned)";
        }

        foreach (var (tool, rect) in _toolButtons)
        {
            bool unlocked = ToolUnlocked(tool);
            Ui.Button(b, rect, unlocked ? ToolName(tool) : $"Lv {ToolLevel(tool)}", unlocked, _tool == tool, rect.Contains(mouse));
        }
        Ui.Button(b, _mirrorButton, MirrorUnlocked ? "Mirror" : $"Lv {PaintingRules.MirrorLevel}", MirrorUnlocked, _mirror, _mirrorButton.Contains(mouse));
        Ui.Button(b, _undoButton, "Undo", _canvas.CanUndo, false, _undoButton.Contains(mouse));
        if (_mirrorButton.Contains(mouse))
            _hoverText = MirrorUnlocked ? "Mirror (M): strokes repeat across the middle." : $"Mirror unlocks at Painting level {PaintingRules.MirrorLevel}.";
        foreach (var (tool, rect) in _toolButtons)
            if (rect.Contains(mouse))
                _hoverText = ToolUnlocked(tool) ? ToolHelp(tool) : $"{ToolName(tool)} unlocks at Painting level {ToolLevel(tool)}.";
    }

    private static float Luma(Color c) => 0.299f * c.R + 0.587f * c.G + 0.114f * c.B;

    private void DrawBottomBar(SpriteBatch b, Point mouse)
    {
        string help = _mix != null
            ? "Hold the mouse button (or Space) to lift your mix. Keep the hue inside it!"
            : "Left: paint   Right: scrape   1-0: pigments   Z: undo   Esc: leave";
        Ui.Text(b, help, new Vector2(_leftCol.X, _finishButton.Y + 14), Game1.textColor * 0.75f);
        Ui.Button(b, _finishButton, _finishArmed > 0f ? "Sure?" : "Finish", _mix == null, _finishArmed > 0f, _finishButton.Contains(mouse));
    }

    private void DrawLeavePrompt(SpriteBatch b, Point mouse)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.4f);
        Ui.Frame(b, _leaveBox);
        Ui.BeginOverlay(_leaveBox);
        Ui.TextCentered(b, "Stop painting? The canvas will be lost.", _leaveBox.Center.X, _leaveBox.Y + 44);
        Ui.Button(b, _leaveYes, "Stop", true, false, _leaveYes.Contains(mouse));
        Ui.Button(b, _leaveNo, "Keep going", true, false, _leaveNo.Contains(mouse));
    }
}
