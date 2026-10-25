using System;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Menus;

// Puts the menu into a representative mid-session state for the dev
// snapshot command, so layout can be checked without playing a session.
internal sealed partial class EaselMenu
{
    internal void DevPaintPartly(float fraction, int seed)
    {
        if (_reference == null) return;
        var rng = new Random(seed);
        int n = _canvas.Size, budget = int.MaxValue, painted = 0, target = (int)(_reference.SubjectCells * fraction);
        for (int y = 0; y < n && painted < target; y++)
        for (int x = 0; x < n && painted < target; x++)
        {
            byte want = _reference.Cells[x, y];
            if (want == PixelCanvas.Blank) continue;
            double roll = rng.NextDouble();
            var purity = roll < 0.8 ? Purity.Pure : roll < 0.93 ? Purity.Off : Purity.Muddy;
            _canvas.Set(x, y, want, purity, ref budget);
            painted++;
        }
        _selected = _reference.PigmentsUsed[0];
        _loadPurity = Purity.Pure;
        _loadMax = PaintingRules.LoadSize(_level, _brush.Load, Purity.Pure);
        _load = _loadMax / 2;
        _lightLeft = _lightTotal * 0.3f;
        _sparksCaught = 2;
        _sparkCell = FirstUnpainted();
        _sparkLife = 4f;
        _sparkLeft = 3f;
    }

    private Microsoft.Xna.Framework.Point? FirstUnpainted()
    {
        int n = _canvas.Size;
        for (int y = n - 1; y >= 0; y--)
        for (int x = 0; x < n; x++)
            if (_reference!.Cells[x, y] != PixelCanvas.Blank && _canvas[x, y] == PixelCanvas.Blank)
                return new Microsoft.Xna.Framework.Point(x, y);
        return null;
    }

    // pigment -1 = top up the color already on the brush.
    internal void DevOpenMix(int pigment, int ticks, bool armed)
    {
        if (pigment < 0) pigment = _selected;
        if (!_rules.PaintLoads)
            DevMixedPalette();
        StartMix(pigment);
        _mixArmed = armed;
        for (int i = 0; i < ticks && _mix!.Outcome == MixOutcome.Mixing; i++)
            _mix.Tick(i % 40 < 22);
    }

    // A few colors already mixed, at each purity.
    internal void DevMixedPalette()
    {
        _mixed[_selected] = Purity.Pure;
        _mixed[Palette.IndexOf("ochre")] = Purity.Off;
        _mixed[Palette.IndexOf("cream")] = Purity.Muddy;
        if (!float.IsPositiveInfinity(_rules.DryTime))
        {
            _wet[_selected] = _rules.DryTime * 0.7f;
            _wet[Palette.IndexOf("ochre")] = _rules.DryTime * 0.3f;
            _wet[Palette.IndexOf("cream")] = _rules.DryTime;
        }
        _loadPurity = Purity.Pure;
        _load = _loadMax = int.MaxValue / 2;
    }

    internal void DevToast(string text) => Toast(text, new Color(214, 160, 30), 2f);

    // Fills from the top-left cell with a pigment through the real paint
    // path (load and Palette both spent); returns the cells painted.
    internal int DevFillWith(int pigment)
    {
        _canvas.Clear();
        _selected = pigment;
        _loadPurity = Purity.Pure;
        _load = _loadMax = 100000;
        _dryWarned = false;
        FillAt(new Point(0, 0));
        int painted = 0;
        foreach (byte c in _canvas.Snapshot())
            if (c != PixelCanvas.Blank) painted++;
        return painted;
    }

    internal int DevPaintLeft(int pigment) => PaintLeft(pigment);

    internal void DevLeavePrompt() => _confirmLeave = true;

    internal void DevLevelUp(int level) => _newLevel = level;

    internal void DevScribble()
    {
        int budget = int.MaxValue;
        for (int i = 0; i < _canvas.Size; i++)
            _canvas.Set(i, i, (byte)_owned[i % _owned.Count], Purity.Pure, ref budget);
    }

    internal void DevFraming(SObject painting)
    {
        _setupFraming = true;
        LayoutSetup();
        _setupPainting = painting;
    }

    internal void DevSetupFree()
    {
        _setupOriginal = true;
        RefreshPreview();
    }

    internal void DevFinish(bool lightGone)
    {
        Finish(lightGone);
        _revealTime = 10f;
    }

    internal SObject? DevPainting => _painting;

    // Drives the real setup path: pick the subject, choose the first brush
    // in the bag, then Begin (which uses up a canvas).
    internal void DevBeginFromSetup(string itemId)
    {
        DevSetup(itemId);
        var brush = BrushOptions().FirstOrDefault(o => o.Item != null);
        if (brush.Item != null)
        {
            _brush = brush.Spec;
            _brushItem = brush.Item;
        }
        TryBegin();
    }

    internal bool DevIsPainting => _stage == EaselStage.Painting;

    internal void DevKeep() => KeepAndClose();

    internal void DevSetCanvas(CanvasKind kind)
    {
        _setupCanvas = kind;
        RefreshPreview();
    }

    internal bool DevSitterOffered(string npc) => _sitterSlots.Any(s => s.Npc == npc);

    // Plein air: look at the view (as clicking its slot does).
    internal void DevPickView() => ClickView();

    internal Reference? DevPreview => _setupPreview;

    internal string? DevViewSubject => _view?.Subject;

    internal void DevSetup(params string[] itemIds)
    {
        _setupItems.Clear();
        _setupItems.AddRange(itemIds);
        RefreshPreview();
    }
}
