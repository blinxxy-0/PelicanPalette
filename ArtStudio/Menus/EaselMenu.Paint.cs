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

internal enum PaintTool { Brush, Broad, Fill, Ruler, Knife, Dropper, Glaze, Highlight }

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
    private int _smearsSeen;
    private bool _bareWarned;

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
        _reference = BuildReference(itemIds, size);

        _timed = !IsOriginal && _rules.LightTimer;
        _lightTotal = _lightLeft = _timed
            ? PaintingRules.LightSeconds(size, _reference!.Difficulty, _level, LightMultiplier)
            : float.PositiveInfinity;
        _nextSpark = 5f + (float)_random.NextDouble() * 4f;
        _stage = EaselStage.Painting;
        LayoutPainting();
        ExplainTechniques();
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
        var tools = ((PaintTool[])Enum.GetValues(typeof(PaintTool))).Where(ToolAvailable).ToArray();
        int dabCols = 3, toolRows = (tools.Length + 3) / 2;
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

    // The technique tools come with the canvases that use them.
    private bool ToolAvailable(PaintTool tool) => tool switch
    {
        PaintTool.Glaze => TechniquesFor(_canvas.Size).HasFlag(Techniques.Glaze),
        PaintTool.Highlight => TechniquesFor(_canvas.Size).HasFlag(Techniques.Highlight),
        _ => true,
    };

    private static Mark MarkOf(PaintTool tool) => tool switch
    {
        PaintTool.Glaze => Mark.Glaze,
        PaintTool.Highlight => Mark.Highlight,
        _ => Mark.None,
    };

    // The first time a canvas asks for a technique, a word on how it works.
    private void ExplainTechniques()
    {
        if (_reference == null || !_reference.UsesTechniques || !StardewModdingAPI.Context.IsWorldReady) return;
        foreach (var mark in new[] { Mark.Highlight, Mark.Glaze })
        {
            string key = ModEntry.Id + "/Explained" + mark;
            if (Technique.Count(_reference.Marks, mark) == 0 || _who.modData.ContainsKey(key)) continue;
            _who.modData[key] = "1";
            Toast(I18n.Get("technique.explain." + mark.ToString().ToLowerInvariant()), new Color(214, 160, 30), 7f);
            return;
        }
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
        PaintTool.Brush => I18n.Get("tool.brush"),
        PaintTool.Broad => I18n.Get("tool.broad"),
        PaintTool.Fill => I18n.Get("tool.fill"),
        PaintTool.Ruler => I18n.Get("tool.ruler"),
        PaintTool.Knife => I18n.Get("tool.knife"),
        PaintTool.Glaze => I18n.Get("tool.glaze"),
        PaintTool.Highlight => I18n.Get("tool.highlight"),
        _ => I18n.Get("tool.dropper"),
    };

    private static string ToolHelp(PaintTool tool) => tool switch
    {
        PaintTool.Brush => I18n.Get("tool.brush.help"),
        PaintTool.Broad => I18n.Get("tool.broad.help"),
        PaintTool.Fill => I18n.Get("tool.fill.help"),
        PaintTool.Ruler => I18n.Get("tool.ruler.help"),
        PaintTool.Knife => I18n.Get("tool.knife.help"),
        PaintTool.Glaze => I18n.Get("tool.glaze.help"),
        PaintTool.Highlight => I18n.Get("tool.highlight.help"),
        _ => I18n.Get("tool.dropper.help"),
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
                case PaintTool.Glaze:
                case PaintTool.Highlight:
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
        if (MarkOf(_tool) is var mark and not Mark.None)
        {
            MarkStroke(from, to, mark);
            return;
        }
        if (!HasPaint()) return;
        int budget = Math.Min(_load, PaintLeft(_selected)), before = budget;
        foreach (var (a, b) in Mirrored(from, to))
            _canvas.Line(a.X, a.Y, b.X, b.Y, (byte)_selected, _loadPurity, BrushSize, ref budget);
        Spend(before - budget);
        AfterPaint();
    }

    // Glazes and highlights go over paint already down; they cost no paint.
    private void MarkStroke(Point from, Point to, Mark mark)
    {
        foreach (var (a, b) in Mirrored(from, to))
            _canvas.MarkLine(a.X, a.Y, b.X, b.Y, mark);
        if (!_bareWarned && _canvas[to.X, to.Y] == PixelCanvas.Blank)
        {
            _bareWarned = true;
            Toast(I18n.Get("technique.needs-paint", new { tool = ToolName(_tool) }), Color.OrangeRed, 2f);
        }
    }

    private void FillAt(Point cell)
    {
        if (!HasPaint()) return;
        int budget = Math.Min(_load, PaintLeft(_selected)), before = budget;
        _canvas.Fill(cell.X, cell.Y, (byte)_selected, _loadPurity, ref budget);
        Spend(before - budget);
        AfterPaint();
    }

    // Cells of a colour left on the Palette (unlimited when paint is free).
    private int PaintLeft(int pigment) => !UsesPaint ? int.MaxValue / 2 : pigment >= 0 ? _paint[pigment] : 0;

    // Painted cells come off the brush's load and out of the Palette.
    private void Spend(int cells)
    {
        if (cells <= 0) return;
        _load -= cells;
        if (!UsesPaint || _palette == null) return;
        _paint[_selected] = Math.Max(0, _paint[_selected] - cells);
        PaletteItem.Write(_palette, _paint);
        if (_paint[_selected] == 0)
            Toast(I18n.Get("paint.out", new { pigment = I18n.Pigment(_selected) }), Color.OrangeRed, 2.5f);
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
        if (_selected >= 0 && _load > 0 && PaintLeft(_selected) <= 0)
        {
            if (!_dryWarned)
            {
                _dryWarned = true;
                Game1.playSound("cancel");
                Toast(I18n.Get("paint.out", new { pigment = I18n.Pigment(_selected) }), Color.OrangeRed, 2.5f);
            }
            return false;
        }
        if (_selected >= 0 && _load > 0) return true;
        if (!_dryWarned)
        {
            _dryWarned = true;
            Game1.playSound("cancel");
            Toast(_selected < 0 ? I18n.Get("paint.pick-first")
                : _rules.PaintLoads ? I18n.Get("paint.brush-dry")
                : I18n.Get("paint.pigment-dried-remix", new { pigment = I18n.Pigment(_selected) }), Color.OrangeRed);
        }
        return false;
    }

    private void AfterPaint()
    {
        if (_canvas.Smeared > _smearsSeen)
        {
            _smearsSeen = _canvas.Smeared;
            Game1.playSound("slimeHit");
            Toast(I18n.Get("technique.smeared"), Color.OrangeRed, 2.5f);
        }
        if (_load <= 0 && !_dryWarned)
        {
            _dryWarned = true;
            Toast(I18n.Get("paint.brush-empty"), Color.OrangeRed, 1f);
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
                Toast(I18n.Get("paint.pigment-dried", new { pigment = I18n.Pigment(pigment) }), new Color(190, 130, 30));
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

        string text = _sparksCaught == PaintingRules.SparksForInspired ? I18n.Get("spark.inspired")
            : _timed ? I18n.Get("spark.light", new { seconds = PaintingRules.SparkLightBonus.ToString("0") })
            : I18n.Get("spark.count", new { caught = _sparksCaught, needed = PaintingRules.SparksForInspired });
        if (_random.NextDouble() < 0.04 && TryFindRarePigment() is int rare)
        {
            ArtProgress.Unlock(_who, rare);
            text = I18n.Get("spark.rare", new { pigment = I18n.Pigment(rare) });
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
                Toast(I18n.Get("paint.not-owned", new { pigment = I18n.Pigment(i) }), Color.OrangeRed);
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
            case Keys.T: SelectTool(PaintTool.Glaze); return;
            case Keys.H: SelectTool(PaintTool.Highlight); return;
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
        if (!ToolAvailable(tool)) return;
        if (!ToolUnlocked(tool))
        {
            Game1.playSound("cancel");
            Toast(I18n.Get("tool.locked", new { tool = ToolName(tool), level = ToolLevel(tool) }), Color.OrangeRed);
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
            Toast(I18n.Get("tool.mirror.locked", new { level = PaintingRules.MirrorLevel }), Color.OrangeRed);
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
            Toast(I18n.Get("paint.paint-first"), Color.OrangeRed);
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
        string title = SubjectTitle(_subjectItems);
        Ui.Text(b, title, new Vector2(_topBar.X, _topBar.Y + 4));
        if (_reference != null)
            Ui.Text(b, I18n.Get("paint.difficulty", new { difficulty = _reference.Difficulty }), new Vector2(_topBar.X, _topBar.Y + 34), Game1.textColor * 0.7f);

        if (IsOriginal) return;

        string sparks = I18n.Get("paint.sparks", new { caught = _sparksCaught, needed = PaintingRules.SparksForInspired });
        Ui.Text(b, sparks, new Vector2(_topBar.Right - Ui.TextWidth(sparks), _topBar.Y + 14),
            _sparksCaught >= PaintingRules.SparksForInspired ? new Color(150, 100, 0) : Game1.textColor);
        if (!_timed) return;

        // The light meter: the session's clock, draining warm to dusk.
        int barW = Math.Min(520, _topBar.Width - 560);
        var bar = new Rectangle(_topBar.Center.X - barW / 2 + 60, _topBar.Y + 14, barW, 28);
        Ui.Text(b, I18n.Get("paint.light"), new Vector2(bar.X - 64, bar.Y));
        Ui.Fill(b, bar, new Color(60, 40, 30));
        float frac = Math.Clamp(_lightLeft / _lightTotal, 0f, 1f);
        Ui.Fill(b, new Rectangle(bar.X + 3, bar.Y + 3, (int)((bar.Width - 6) * frac), bar.Height - 6), LightColor(frac));
        Ui.Outline(b, bar, new Color(40, 25, 15), 2);
        // Seconds left, inside the bar's right end; "paused" while the light waits for a mix.
        bool waiting = _mix != null && _rules.PauseLightWhileMixing;
        string left = I18n.Get(waiting ? "paint.light-left.paused" : "paint.light-left", new { seconds = Math.Ceiling(_lightLeft).ToString("0") });
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
            Ui.TextCentered(b, I18n.Get("paint.reference"), _noteArea.Center.X, _noteArea.Y + 12);
            DrawCells(b, _reference.Cells, null, _refArea, CanvasColor, false, _reference.Marks);
            DrawMarkHints(b, _refArea);
            Ui.Fill(b, _refArea, DuskTint());
            if (_sparkCell.HasValue)
                DrawReferenceSpark(b, CellRect(_refArea, _canvas.Size, _sparkCell.Value.X, _sparkCell.Value.Y));
            if (_mix == null && LinkedCell(MousePos) is Point linked)
                DrawLinkedCell(b, CellRect(_refArea, _canvas.Size, linked.X, linked.Y));
        }
        else
        {
            var lines = Game1.parseText(I18n.Get("paint.no-reference"), Game1.smallFont, SideWidth - 40);
            Ui.Text(b, lines, new Vector2(_noteArea.X + 20, _noteArea.Y + 24));
        }

        // What's on the brush.
        int y = _noteArea.Bottom + 16;
        Ui.Text(b, I18n.Get("paint.on-brush"), new Vector2(_leftCol.X, y));
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
                Ui.Text(b, I18n.Get("paint.cells-left", new { cells = _load }), new Vector2(gauge.X, gauge.Bottom + 6), Game1.textColor * 0.7f);
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
                    Ui.Text(b, I18n.Get("paint.wet-for", new { seconds = Math.Ceiling(wet).ToString("0") }), new Vector2(gauge.X, gauge.Bottom + 4), Game1.textColor * 0.7f);
                    hintY = gauge.Bottom + 34;
                }
                else if (_load <= 0)
                {
                    Ui.Text(b, I18n.Get("paint.dried"), new Vector2(_leftCol.X, hintY), new Color(150, 70, 50));
                    hintY += 30;
                }
                if (_loadPurity != Purity.Pure && _load > 0)
                {
                    var hint = Game1.parseText(I18n.Get("paint.remix-hint"), Game1.smallFont, SideWidth - 10);
                    Ui.Text(b, hint, new Vector2(_leftCol.X, hintY), Game1.textColor * 0.7f);
                }
            }
        }
        else
        {
            Ui.Fill(b, swatch, Color.Black * 0.1f);
            Ui.Text(b, I18n.Get("paint.brush-nothing"), new Vector2(swatch.Right + 12, swatch.Y + 12), Game1.textColor * 0.6f);
        }
    }

    private static string PurityLabel(Purity p) => p switch
    {
        Purity.Pure => I18n.Get("purity.pure"),
        Purity.Off => I18n.Get("purity.off"),
        _ => I18n.Get("purity.muddy"),
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
        DrawCells(b, _canvas.Snapshot(), _canvas.PuritySnapshot(), _canvasArea, CanvasColor, true, _canvas.MarksSnapshot());
        DrawMarkHints(b, _canvasArea);

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
            DrawCanvasSpark(b, CellRect(_canvasArea, n, _sparkCell.Value.X, _sparkCell.Value.Y), _sparkCell.Value);

        Ui.Fill(b, _canvasArea, DuskTint());

        if (_mix != null) return;

        // Pointing at the reference shows where that cell is on the canvas.
        if (RefCellAt(mouse) is Point fromReference)
            DrawLinkedCell(b, CellRect(_canvasArea, n, fromReference.X, fromReference.Y));

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

    // A spark never covers the colour it asks for (1.1.1, from player
    // feedback: the star hid the reference cell). On the canvas the cell
    // glows in that colour; on the reference only a ring round the cell
    // pulses, leaving the colour itself in plain sight.
    private float SparkFade => float.IsPositiveInfinity(_sparkLife) ? 1f : _sparkLife > 0f ? Math.Clamp(_sparkLeft / _sparkLife * 2f, 0.3f, 1f) : 0.3f;

    private static float SparkPulse => 0.5f + 0.5f * MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 10f);

    private static readonly Color SparkGold = new(255, 222, 90);

    private void DrawCanvasSpark(SpriteBatch b, Rectangle cell, Point at)
    {
        if (_reference != null)
        {
            var wanted = PaintingRenderer.CellColor(_reference.Cells[at.X, at.Y], Purity.Pure, _reference.Marks[at.X, at.Y]);
            Ui.Fill(b, cell, wanted * ((0.6f + 0.4f * SparkPulse) * SparkFade));
        }
        DrawSparkRing(b, cell);
    }

    private void DrawReferenceSpark(SpriteBatch b, Rectangle cell) => DrawSparkRing(b, cell);

    // A pulsing gold ring just outside the cell, dark-edged so it shows on
    // light and dark colours alike, with a glint at each corner.
    private void DrawSparkRing(SpriteBatch b, Rectangle cell)
    {
        int gap = 1 + (int)Math.Round(2 * SparkPulse);
        var ring = new Rectangle(cell.X - gap - 3, cell.Y - gap - 3, cell.Width + 2 * (gap + 3), cell.Height + 2 * (gap + 3));
        Ui.Outline(b, new Rectangle(ring.X - 1, ring.Y - 1, ring.Width + 2, ring.Height + 2), new Color(90, 55, 10) * SparkFade, 1);
        Ui.Outline(b, ring, SparkGold * SparkFade, 3);
        int g = Math.Max(3, cell.Width / 3);
        foreach (var (x, y) in new[] { (ring.Left - g, ring.Top - g), (ring.Right, ring.Top - g), (ring.Left - g, ring.Bottom), (ring.Right, ring.Bottom) })
            Ui.Fill(b, new Rectangle(x, y, g, g), SparkGold * SparkFade);
    }

    // ---- The canvas and the reference, linked (1.1.1) ----

    private Point? RefCellAt(Point p)
    {
        if (_reference == null || !_refArea.Contains(p)) return null;
        int n = _canvas.Size;
        return new Point((p.X - _refArea.X) * n / _refArea.Width, (p.Y - _refArea.Y) * n / _refArea.Height);
    }

    // The cell the mouse is on, on whichever grid it's over.
    private Point? LinkedCell(Point p) => CellAt(p) ?? RefCellAt(p);

    // A frame just outside the cell (so its colour stays in view, however
    // small the cell), white inside and dark outside to show on any colour.
    private static void DrawLinkedCell(SpriteBatch b, Rectangle cell)
    {
        Ui.Outline(b, new Rectangle(cell.X - 4, cell.Y - 4, cell.Width + 8, cell.Height + 8), Color.Black * 0.8f, 2);
        Ui.Outline(b, new Rectangle(cell.X - 2, cell.Y - 2, cell.Width + 4, cell.Height + 4), Color.White, 2);
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
                // How much paint the Palette has left of it: a gauge up the right edge.
                if (UsesPaint)
                {
                    int h = (int)((r.Height - 8) * Math.Clamp(_paint[i] / (float)(_palette != null ? PaletteItem.Capacity(_palette, _who) : 1), 0f, 1f));
                    Ui.Fill(b, new Rectangle(r.Right - 7, r.Y + 4, 4, r.Height - 8), Color.Black * 0.35f);
                    Ui.Fill(b, new Rectangle(r.Right - 7, r.Bottom - 4 - h, 4, h), _paint[i] > 0 ? Color.White * 0.9f : Color.OrangeRed);
                }
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
                _hoverText = I18n.Pigment(i) + "\n" + (owned ? I18n.Get("pigment.hover", new { difficulty = p.Difficulty, motion = I18n.Motion(p.Motion) })
                    + (UsesPaint ? "\n" + I18n.Get("pigment.paint-left", new { cells = _paint[i] }) : "") : I18n.Get("pigment.not-owned"));
        }

        foreach (var (tool, rect) in _toolButtons)
        {
            bool unlocked = ToolUnlocked(tool);
            Ui.Button(b, rect, unlocked ? ToolName(tool) : I18n.Get("tool.level-short", new { level = ToolLevel(tool) }), unlocked, _tool == tool, rect.Contains(mouse));
        }
        Ui.Button(b, _mirrorButton, MirrorUnlocked ? I18n.Get("tool.mirror") : I18n.Get("tool.level-short", new { level = PaintingRules.MirrorLevel }), MirrorUnlocked, _mirror, _mirrorButton.Contains(mouse));
        Ui.Button(b, _undoButton, I18n.Get("tool.undo"), _canvas.CanUndo, false, _undoButton.Contains(mouse));
        if (_mirrorButton.Contains(mouse))
            _hoverText = MirrorUnlocked ? I18n.Get("tool.mirror.help") : I18n.Get("tool.mirror.locked", new { level = PaintingRules.MirrorLevel });
        foreach (var (tool, rect) in _toolButtons)
            if (rect.Contains(mouse))
                _hoverText = ToolUnlocked(tool) ? ToolHelp(tool) : I18n.Get("tool.locked", new { tool = ToolName(tool), level = ToolLevel(tool) });
    }

    // With the glaze or highlight tool in hand, a dot on every cell of the
    // reference (and the matching canvas cell) that wants one.
    private void DrawMarkHints(SpriteBatch b, Rectangle area)
    {
        var mark = MarkOf(_tool);
        if (mark == Mark.None || _reference == null) return;
        int n = _reference.Size;
        var dot = mark == Mark.Glaze ? new Color(60, 30, 90) : Color.White;
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        {
            if (_reference.Marks[x, y] != mark) continue;
            var r = CellRect(area, n, x, y);
            int d = Math.Max(2, r.Width / 3);
            Ui.Fill(b, new Rectangle(r.Center.X - d / 2, r.Center.Y - d / 2, d, d), dot * 0.8f);
        }
    }

    private static float Luma(Color c) => 0.299f * c.R + 0.587f * c.G + 0.114f * c.B;

    private void DrawBottomBar(SpriteBatch b, Point mouse)
    {
        string help = _mix != null
            ? I18n.Get("paint.mix-help")
            : I18n.Get("paint.controls");
        Ui.Text(b, help, new Vector2(_leftCol.X, _finishButton.Y + 14), Game1.textColor * 0.75f);
        Ui.Button(b, _finishButton, _finishArmed > 0f ? I18n.Get("paint.finish.confirm") : I18n.Get("paint.finish"), _mix == null, _finishArmed > 0f, _finishButton.Contains(mouse));
    }

    private void DrawLeavePrompt(SpriteBatch b, Point mouse)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.4f);
        Ui.Frame(b, _leaveBox);
        Ui.BeginOverlay(_leaveBox);
        Ui.TextCentered(b, I18n.Get("paint.leave.prompt"), _leaveBox.Center.X, _leaveBox.Y + 44);
        Ui.Button(b, _leaveYes, I18n.Get("paint.leave.yes"), true, false, _leaveYes.Contains(mouse));
        Ui.Button(b, _leaveNo, I18n.Get("paint.leave.no"), true, false, _leaveNo.Contains(mouse));
    }
}
