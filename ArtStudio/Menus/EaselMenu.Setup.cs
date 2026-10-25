using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Menus;

// Setup: choose a canvas size and brush, then what to paint — one to three
// items from your bag as a still life, or a free canvas. The preview shows
// the reference exactly as you'll be asked to copy it, with its difficulty.
// The same screen frames finished paintings for hanging.
internal sealed partial class EaselMenu
{
    private const int SlotSize = 64, SlotGap = 8;
    private static readonly string[] CanvasKeys = { "small", "medium", "large" };

    private static string CanvasName(int i) => I18n.Get("canvas." + CanvasKeys[i]);

    private CanvasKind _setupCanvas = CanvasKind.Small;
    private bool _setupOriginal, _setupFraming;
    private readonly List<string> _setupItems = new();
    private Reference? _setupPreview;
    private SObject? _setupPainting;

    private Rectangle[] _canvasButtons = Array.Empty<Rectangle>();
    private readonly List<(Rectangle Rect, Item Item)> _itemSlots = new();
    private readonly List<(Rectangle Rect, string Npc)> _sitterSlots = new();
    private int _sitterLabelY;

    private static bool IsSitterSubject(IReadOnlyList<string> ids) => ids.Count == 1 && Sitters.IsSitter(ids[0]);
    private bool SetupPortrait => IsSitterSubject(_setupItems);
    private bool SetupView => IsViewSubject(_setupItems);
    // The view's slot sits first in the sitters' row.
    private const string ViewSlot = Views.Prefix;
    private Rectangle _originalButton, _brushButton, _frameButton, _beginButton, _previewArea, _linenButton;
    private bool _setupLinen;

    // Debug commands paint without supplies; the easel itself needs them.
    private bool _needsSupplies;

    private void EnterSetup()
    {
        _stage = EaselStage.Setup;
        _needsSupplies = StardewModdingAPI.Context.IsWorldReady;
        LayoutSetup();
        RefreshPreview();
    }

    private void LayoutSetup()
    {
        int x = xPositionOnScreen + Pad + 24, y = yPositionOnScreen + Pad + 72;
        _canvasButtons = new Rectangle[3];
        for (int i = 0; i < 3; i++)
            _canvasButtons[i] = new Rectangle(x + i * 182, y, 170, 56);
        _linenButton = new Rectangle(x + 3 * 182, y, 170, 56);

        int previewSize = Math.Min(320, height - 340);
        _previewArea = new Rectangle(xPositionOnScreen + width - Pad - 40 - previewSize, y + 40, previewSize, previewSize);
        _beginButton = new Rectangle(_previewArea.Center.X - 90, _previewArea.Bottom + 86, 180, 56);

        int rowY = yPositionOnScreen + height - Pad - 96;
        int rowW = (_previewArea.X - 60 - x - 2 * 12) / 3;
        _originalButton = new Rectangle(x, rowY, rowW, 56);
        _brushButton = new Rectangle(x + rowW + 12, rowY, rowW, 56);
        _frameButton = new Rectangle(x + 2 * (rowW + 12), rowY, rowW, 56);

        _itemSlots.Clear();
        _sitterSlots.Clear();
        int gridX = x, gridY = y + 110;
        int cols = Math.Max(1, (_previewArea.X - 40 - gridX) / (SlotSize + SlotGap));
        // Below the items, the villagers who'll sit for a portrait: keep a
        // label and one row for them.
        var sitters = _setupFraming || !StardewModdingAPI.Context.IsWorldReady ? new List<string>() : Sitters.Available(_who);
        if (_viewLocation != null && !_setupFraming)
            sitters.Insert(0, ViewSlot);
        int itemsBottom = sitters.Count > 0 ? rowY - 12 - 40 - SlotSize - 8 : rowY - 12;
        int n = 0;
        foreach (var item in Game1.player.Items)
        {
            if (item == null) continue;
            bool wanted = _setupFraming
                ? PaintingItems.IsPainting(item)
                : ReferenceBuilder.CanPaint(item) && !_itemSlots.Any(s => s.Item.QualifiedItemId == item.QualifiedItemId);
            if (!wanted) continue;
            var rect = new Rectangle(gridX + n % cols * (SlotSize + SlotGap), gridY + n / cols * (SlotSize + SlotGap), SlotSize, SlotSize);
            if (rect.Bottom > itemsBottom) break;
            _itemSlots.Add((rect, item));
            n++;
        }
        int rows = n == 0 ? 1 : (n + cols - 1) / cols;
        _sitterLabelY = gridY + rows * (SlotSize + SlotGap) + 4;
        for (int i = 0; i < sitters.Count && i < cols; i++)
            _sitterSlots.Add((new Rectangle(gridX + i * (SlotSize + SlotGap), _sitterLabelY + 36, SlotSize, SlotSize), sitters[i]));
    }

    private bool CanvasUnlocked(CanvasKind kind) => _level >= PaintingRules.CanvasUnlockLevel(kind);

    private int CountInBag(string qualifiedId) =>
        Game1.player.Items.Where(i => i?.QualifiedItemId == qualifiedId).Sum(i => i.Stack);

    private int CanvasCount(CanvasKind kind) => CountInBag(ArtItems.QualifiedCanvasId(kind));

    private int FrameCount => CountInBag("(O)" + ArtItems.FrameItemId);

    private bool HasCanvas => !_needsSupplies || (_setupLinen ? LinenCount(_setupCanvas) : CanvasCount(_setupCanvas)) > 0;

    private int LinenCount(CanvasKind kind) => kind == CanvasKind.Small ? 0 : CountInBag(ArtItems.QualifiedLinenId(kind));

    // Linen is offered for medium and large canvases, once you can make it or have some.
    private bool LinenOffered => !_setupFraming && _setupCanvas != CanvasKind.Small
        && (_level >= PaintingRules.LinenLevel || LinenCount(_setupCanvas) > 0);

    // The default hog bristle brush, then every other brush in the bag.
    private List<(BrushSpec Spec, Item? Item)> BrushOptions()
    {
        var options = new List<(BrushSpec, Item?)> { (Brushes.HogBristle, null) };
        foreach (var item in Game1.player.Items)
            if (ArtItems.BrushFor(item) is BrushSpec spec)
                options.Add((spec, item));
        return options;
    }

    private void RefreshPreview()
    {
        int size = PaintingRules.CanvasSize(_setupCanvas);
        int max = ReferenceBuilder.MaxItems(size);
        while (_setupItems.Count > max)
            _setupItems.RemoveAt(_setupItems.Count - 1);
        _setupPreview = _setupOriginal || _setupFraming || _setupItems.Count == 0 ? null
            : SetupPortrait && !PaintingRules.PortraitFits(_setupCanvas) ? null
            : BuildReference(_setupItems, size);
    }

    private void UpdateSetup()
    {
    }

    private void ClickSetup(int x, int y)
    {
        if (!_setupFraming)
            for (int i = 0; i < _canvasButtons.Length; i++)
            {
                if (!_canvasButtons[i].Contains(x, y)) continue;
                var kind = (CanvasKind)i;
                if (!CanvasUnlocked(kind))
                {
                    Game1.playSound("cancel");
                    return;
                }
                _setupCanvas = kind;
                if (!LinenOffered) _setupLinen = false;
                Game1.playSound("smallSelect");
                RefreshPreview();
                return;
            }

        foreach (var (rect, item) in _itemSlots)
        {
            if (!rect.Contains(x, y)) continue;
            Game1.playSound("smallSelect");
            if (_setupFraming)
            {
                _setupPainting = item as SObject;
                return;
            }
            _setupOriginal = false;
            if (SetupPortrait || SetupView) _setupItems.Clear();
            string id = item.QualifiedItemId;
            if (_setupItems.Contains(id))
                _setupItems.Remove(id);
            else
            {
                int max = ReferenceBuilder.MaxItems(PaintingRules.CanvasSize(_setupCanvas));
                if (_setupItems.Count >= max)
                    _setupItems.RemoveAt(0);
                _setupItems.Add(id);
            }
            RefreshPreview();
            return;
        }

        foreach (var (rect, npc) in _sitterSlots)
        {
            if (!rect.Contains(x, y)) continue;
            if (npc == ViewSlot)
            {
                ClickView();
                return;
            }
            Game1.playSound("smallSelect");
            _setupOriginal = false;
            string subject = Sitters.SubjectFor(npc);
            bool again = _setupItems.Contains(subject);
            _setupItems.Clear();
            if (!again)
            {
                _setupItems.Add(subject);
                if (!PaintingRules.PortraitFits(_setupCanvas) && CanvasUnlocked(CanvasKind.Medium))
                    _setupCanvas = CanvasKind.Medium;
            }
            RefreshPreview();
            return;
        }

        if (_linenButton.Contains(x, y) && LinenOffered)
        {
            _setupLinen = !_setupLinen;
            Game1.playSound("smallSelect");
            return;
        }

        if (_originalButton.Contains(x, y) && !_setupFraming)
        {
            _setupOriginal = !_setupOriginal;
            if (_setupOriginal) _setupItems.Clear();
            Game1.playSound("smallSelect");
            RefreshPreview();
        }
        else if (_brushButton.Contains(x, y) && !_setupFraming)
        {
            var options = BrushOptions();
            int current = options.FindIndex(o => o.Item == _brushItem && o.Spec == _brush);
            var next = options[(current + 1) % options.Count];
            _brush = next.Spec;
            _brushItem = next.Item;
            Game1.playSound("toolSwap");
        }
        else if (_frameButton.Contains(x, y))
        {
            _setupFraming = !_setupFraming;
            _setupPainting = null;
            Game1.playSound("smallSelect");
            LayoutSetup();
            RefreshPreview();
        }
        else if (_beginButton.Contains(x, y))
        {
            if (_setupFraming)
                TryFrame();
            else
                TryBegin();
        }
    }

    // The view from the easel: looked at (rendered) the first time it's picked.
    private void ClickView()
    {
        _view ??= Views.Take(_viewLocation!, _easelTile);
        if (_view == null)
        {
            Game1.playSound("cancel");
            return;
        }
        Game1.playSound("smallSelect");
        _setupOriginal = false;
        bool again = SetupView;
        _setupItems.Clear();
        if (!again) _setupItems.Add(_view.Subject);
        RefreshPreview();
    }

    // A painting may not run past 2am (in multiplayer the clock is shared
    // and keeps running anyway, so there's no extra time to add).
    private bool TooLate => !Context.IsMultiplayer && Context.IsWorldReady
        && ArtHooks.FinishTime(PaintingRules.Minutes(_setupCanvas, IsLongSubject(_setupItems))) > PaintingRules.LatestFinish;

    private bool NeedsPalette => UsesPaint && _palette == null;

    // Colours the subject needs more of than the Palette holds (cells of each
    // in the reference, against what's left).
    private List<int> ShortColours()
    {
        if (!UsesPaint || _palette == null || _setupPreview == null) return new List<int>();
        var need = new int[Palette.All.Length];
        foreach (byte c in _setupPreview.Cells)
            if (c != PixelCanvas.Blank && c < need.Length)
                need[c]++;
        return Enumerable.Range(0, need.Length).Where(i => need[i] > _paint[i]).ToList();
    }

    private void TryBegin()
    {
        if (!_setupOriginal && _setupItems.Count == 0) return;
        if (SetupPortrait && !PaintingRules.PortraitFits(_setupCanvas))
        {
            Game1.playSound("cancel");
            Toast(I18n.Get("setup.portrait-canvas"), new Color(150, 70, 50), 2.5f);
            return;
        }
        if (!HasCanvas)
        {
            Game1.playSound("cancel");
            return;
        }
        if (TooLate)
        {
            Game1.playSound("cancel");
            Toast(I18n.Get("setup.too-late"), new Color(150, 70, 50), 2.5f);
            return;
        }
        if (NeedsPalette)
        {
            Game1.playSound("cancel");
            Toast(I18n.Get("setup.need-palette"), new Color(150, 70, 50), 3f);
            return;
        }
        Game1.playSound("bigSelect");
        if (_needsSupplies)
            UseCanvas(_setupCanvas);
        _linen = _setupLinen;
        BeginPainting(_setupCanvas, _setupOriginal ? Array.Empty<string>() : _setupItems.ToArray());
    }

    private void UseCanvas(CanvasKind kind)
    {
        if (PaintingSkill.Has(_who, PaintingSkill.Instance.Prolific) && _random.NextDouble() < PaintingSkill.ProlificSaveChance)
        {
            Toast(I18n.Get("setup.prolific"), new Color(60, 140, 60), 2f);
            return;
        }
        string id = _setupLinen ? ArtItems.QualifiedLinenId(kind) : ArtItems.QualifiedCanvasId(kind);
        var canvas = _who.Items.First(i => i?.QualifiedItemId == id);
        if (canvas.Stack > 1) canvas.Stack--;
        else _who.removeItemFromInventory(canvas);
    }

    private void TryFrame()
    {
        if (_setupPainting == null || FrameCount <= 0)
        {
            Game1.playSound("cancel");
            return;
        }
        var frameItem = _who.Items.First(i => i?.QualifiedItemId == "(O)" + ArtItems.FrameItemId);
        if (frameItem.Stack > 1) frameItem.Stack--;
        else _who.removeItemFromInventory(frameItem);

        var framed = ArtHooks.Frame(_setupPainting);
        int slot = _who.Items.IndexOf(_setupPainting);
        _who.Items[slot] = framed;
        Game1.playSound("hammer");
        Toast(I18n.Get("setup.framed"), new Color(60, 140, 60), 2f);
        _setupPainting = null;
        LayoutSetup();
    }

    private void DrawSetup(SpriteBatch b)
    {
        int left = xPositionOnScreen + Pad + 24;
        var mouse = MousePos;

        if (_setupFraming)
            DrawFraming(b, left, mouse);
        else
            DrawStillLifeChoice(b, left, mouse);

        // The bottom row: free canvas, brush, framing.
        Ui.Button(b, _originalButton, I18n.Get("paint.title.free"), !_setupFraming, _setupOriginal, _originalButton.Contains(mouse));
        string brushLabel = _brushItem == null ? ShortBrushName(_brush) : I18n.Get("setup.brush-label", new { brush = ShortBrushName(_brush), uses = ArtHooks.UsesLeft(_brushItem, _brush) });
        Ui.Button(b, _brushButton, brushLabel, !_setupFraming, false, _brushButton.Contains(mouse));
        Ui.Button(b, _frameButton, _setupFraming ? I18n.Get("setup.back-to-painting") : I18n.Get("setup.frame-a-painting"), true, _setupFraming, _frameButton.Contains(mouse));
        if (_brushButton.Contains(mouse) && !_setupFraming)
            _hoverText = (_brushItem == null ? I18n.Get("setup.brush-hover", new { brush = I18n.Get("brush." + _brush.Id) }) : I18n.Get("setup.brush-hover.uses", new { brush = I18n.Get("brush." + _brush.Id), uses = ArtHooks.UsesLeft(_brushItem, _brush) }))
                + "\n" + I18n.Get("setup.brush-switch");
        if (_frameButton.Contains(mouse) && !_setupFraming)
            _hoverText = I18n.Get("setup.frame-hover", new { frames = FrameCount });

        if (_toastLeft > 0f && _toast != null)
            Ui.TextCentered(b, _toast, _previewArea.Center.X, _previewArea.Y - 52, _toastColor * Math.Min(1f, _toastLeft * 2f));
    }

    private static string ShortBrushName(BrushSpec brush) => brush.Id switch
    {
        "sable" => I18n.Get("brush.short.sable"),
        "flat" => I18n.Get("brush.short.flat"),
        "rigger" => I18n.Get("brush.short.rigger"),
        "iridium" => I18n.Get("brush.short.iridium"),
        _ => I18n.Get("brush.short.hog"),
    };

    private void DrawStillLifeChoice(SpriteBatch b, int left, Point mouse)
    {
        Ui.Text(b, I18n.Get("setup.choose-canvas"), new Vector2(left, yPositionOnScreen + Pad + 30));
        for (int i = 0; i < _canvasButtons.Length; i++)
        {
            var kind = (CanvasKind)i;
            bool unlocked = CanvasUnlocked(kind);
            int size = PaintingRules.CanvasSize(kind);
            string label = !unlocked ? I18n.Get("tool.level-short", new { level = PaintingRules.CanvasUnlockLevel(kind) })
                : _needsSupplies ? I18n.Get("setup.canvas-count", new { canvas = CanvasName(i), count = CanvasCount(kind) })
                : CanvasName(i);
            Ui.Button(b, _canvasButtons[i], label, unlocked, _setupCanvas == kind, _canvasButtons[i].Contains(mouse));
            if (_canvasButtons[i].Contains(mouse))
                _hoverText = unlocked
                    ? I18n.Get("setup.canvas-hover." + CanvasKeys[i], new { size, items = ReferenceBuilder.MaxItems(size) })
                    : I18n.Get("setup.canvas-locked." + CanvasKeys[i], new { level = PaintingRules.CanvasUnlockLevel(kind) });
        }

        if (LinenOffered)
        {
            string linen = _needsSupplies ? I18n.Get("setup.linen-count", new { count = LinenCount(_setupCanvas) }) : I18n.Get("setup.linen");
            Ui.Button(b, _linenButton, linen, true, _setupLinen, _linenButton.Contains(mouse));
            if (_linenButton.Contains(mouse))
                _hoverText = I18n.Get("setup.linen-hover");
        }

        int max = ReferenceBuilder.MaxItems(PaintingRules.CanvasSize(_setupCanvas));
        Ui.Text(b, max == 1 ? I18n.Get("setup.pick-one") : I18n.Get("setup.pick-many", new { max }),
            new Vector2(left, _canvasButtons[0].Bottom + 18));
        if (_itemSlots.Count == 0)
            Ui.Text(b, I18n.Get("setup.nothing-to-paint"), new Vector2(left, _canvasButtons[0].Bottom + 70), Game1.textColor * 0.6f);
        DrawSlots(b, mouse, slot => _setupItems.IndexOf(slot.QualifiedItemId), max > 1);
        DrawSitters(b, left, mouse);

        // Preview of what you'll be copying.
        Ui.Paper(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40));
        int canvasSize = PaintingRules.CanvasSize(_setupCanvas);
        var info = new Vector2(_previewArea.X - 20, _previewArea.Bottom + 28);
        if (_setupPreview != null)
        {
            DrawCells(b, _setupPreview.Cells, null, _previewArea, new Color(243, 236, 220), false, _setupPreview.Marks);
            float light = PaintingRules.LightSeconds(canvasSize, _setupPreview.Difficulty, _level, LightMultiplier);
            Ui.Text(b, I18n.Get("setup.preview", new { difficulty = _setupPreview.Difficulty, pigments = _setupPreview.PigmentsUsed.Count }), info);
            Ui.Text(b, _rules.LightTimer ? I18n.Get("setup.light", new { seconds = light.ToString("0") }) : I18n.Get("setup.no-time-limit"), info + new Vector2(0, 28), Game1.textColor * 0.75f);
        }
        else
        {
            Ui.Fill(b, _previewArea, new Color(243, 236, 220));
            string msg = _setupOriginal ? I18n.Get("setup.blank-canvas") : I18n.Get("setup.pick-something");
            var lines = Game1.parseText(msg, Game1.smallFont, _previewArea.Width - 40);
            Ui.Text(b, lines, new Vector2(_previewArea.X + 20, _previewArea.Center.Y - 20), Game1.textColor * 0.7f);
        }

        bool ready = (_setupOriginal || _setupItems.Count > 0) && HasCanvas;
        Ui.Button(b, _beginButton, I18n.Get("setup.begin"), ready, false, ready && _beginButton.Contains(mouse));
        // Under Begin: what's missing, or else the mode and how long it takes.
        int hours = PaintingRules.Minutes(_setupCanvas, IsLongSubject(_setupItems)) / 60;
        var shortOf = ShortColours();
        bool portraitTooSmall = SetupPortrait && !PaintingRules.PortraitFits(_setupCanvas);
        bool warn = portraitTooSmall || !HasCanvas || TooLate || NeedsPalette || shortOf.Count > 0;
        string footer = portraitTooSmall ? I18n.Get("setup.portrait-canvas-short")
            : !HasCanvas ? I18n.Get(_setupLinen ? "setup.need-linen" : "setup.need-canvas." + CanvasKeys[(int)_setupCanvas])
            : TooLate ? I18n.Get("setup.too-late-short")
            : NeedsPalette ? I18n.Get("setup.need-palette-short")
            : shortOf.Count > 0 ? I18n.Get("setup.low-paint", new { pigments = string.Join(", ", shortOf.Take(3).Select(I18n.Pigment)) })
            : I18n.Get("setup.mode-takes", new { mode = I18n.Get("setup.mode." + ModEntry.Config.Difficulty.ToString().ToLowerInvariant()), hours });
        Ui.Text(b, footer, new Vector2(_previewArea.X - 20, _beginButton.Bottom + 10), warn ? new Color(150, 70, 50) : Game1.textColor * 0.8f);
        if (!HasCanvas && _beginButton.Contains(mouse))
            _hoverText = _setupCanvas == CanvasKind.Small ? I18n.Get("setup.craft-small") : I18n.Get("setup.craft-big");
    }

    private void DrawFraming(SpriteBatch b, int left, Point mouse)
    {
        Ui.Text(b, I18n.Get("setup.frame-title"), new Vector2(left, yPositionOnScreen + Pad + 30));
        Ui.Text(b, I18n.Get("setup.frames", new { frames = FrameCount }), new Vector2(left, _canvasButtons[0].Y + 14), FrameCount > 0 ? Game1.textColor : new Color(150, 70, 50));
        if (_itemSlots.Count == 0)
            Ui.Text(b, I18n.Get("setup.no-paintings"), new Vector2(left, _canvasButtons[0].Bottom + 70), Game1.textColor * 0.6f);
        DrawSlots(b, mouse, slot => slot == _setupPainting ? 0 : -1, false);

        Ui.Paper(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40));
        Ui.Fill(b, _previewArea, new Color(243, 236, 220));
        if (_setupPainting != null)
        {
            PaintingRenderer.DrawArt(b, _setupPainting, _previewArea, Color.White, 0.9f);
            Ui.Text(b, _setupPainting.DisplayName, new Vector2(_previewArea.X - 20, _previewArea.Bottom + 28));
        }
        else
        {
            var lines = Game1.parseText(I18n.Get("setup.pick-painting"), Game1.smallFont, _previewArea.Width - 40);
            Ui.Text(b, lines, new Vector2(_previewArea.X + 20, _previewArea.Center.Y - 20), Game1.textColor * 0.7f);
        }

        bool ready = _setupPainting != null && FrameCount > 0;
        Ui.Button(b, _beginButton, I18n.Get("setup.frame-it"), ready, false, ready && _beginButton.Contains(mouse));
        if (FrameCount == 0)
            Ui.Text(b, I18n.Get("setup.need-frame"), new Vector2(_previewArea.X - 20, _beginButton.Bottom + 10), new Color(150, 70, 50));
    }

    // Inventory slots; `order` gives a selected slot its number (or -1).
    // Friends who'll sit for a portrait: their faces, a gold ribbon if one
    // of them has commissioned it.
    private void DrawSitters(SpriteBatch b, int left, Point mouse)
    {
        if (_sitterSlots.Count == 0) return;
        bool viewOnly = _sitterSlots.Count == 1 && _viewLocation != null;
        Ui.Text(b, I18n.Get(viewOnly ? "setup.pick-view" : _viewLocation != null ? "setup.pick-view-or-portrait" : "setup.pick-portrait"), new Vector2(left, _sitterLabelY));
        foreach (var (rect, npc) in _sitterSlots)
        {
            b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
            if (npc == ViewSlot)
            {
                DrawViewSlot(b, rect, mouse);
                continue;
            }
            var texture = Sitters.PortraitTexture(npc);
            if (texture != null)
                b.Draw(texture, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8), new Rectangle(0, 0, 64, 64), Color.White);
            if (_setupItems.Contains(Sitters.SubjectFor(npc)))
                Ui.Outline(b, rect, new Color(214, 160, 30), 4);
            var wantedBy = Shop.Commissions.Accepted(_who).FirstOrDefault(c => c.ItemId == Sitters.SubjectFor(npc));
            if (wantedBy != null)
            {
                Ui.Fill(b, new Rectangle(rect.Right - 14, rect.Y + 4, 10, 14), new Color(214, 160, 30));
                Ui.Fill(b, new Rectangle(rect.Right - 12, rect.Y + 18, 6, 4), new Color(150, 100, 0));
            }
            if (rect.Contains(mouse))
                _hoverText = I18n.Get("setup.portrait-hover", new { name = Sitters.DisplayName(npc) })
                    + (wantedBy == null ? "" : "\n" + I18n.Get("setup.commissioned", new { npc = wantedBy.NpcName, quality = Shop.Commissions.QualityText(wantedBy.MinQuality) }));
        }
    }

    // The view: what the painter saw, once they've looked; a little
    // landscape (sky, hills, a sun) before then.
    private void DrawViewSlot(SpriteBatch b, Rectangle rect, Point mouse)
    {
        var inner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
        if (_view?.Thumbnail != null)
            b.Draw(_view.Thumbnail, inner, Color.White);
        else
            Ui.Landscape(b, inner);
        if (SetupView)
            Ui.Outline(b, rect, new Color(214, 160, 30), 4);
        if (rect.Contains(mouse))
            _hoverText = I18n.Get("setup.view-hover", new { place = _viewLocation!.DisplayName });
    }

    private void DrawSlots(SpriteBatch b, Point mouse, Func<Item, int> order, bool numbered)
    {
        foreach (var (rect, item) in _itemSlots)
        {
            int n = order(item);
            b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
            item.drawInMenu(b, new Vector2(rect.X, rect.Y), 1f, 1f, 0.9f, StackDrawType.Hide, Color.White, false);
            if (n >= 0)
            {
                Ui.Outline(b, rect, new Color(214, 160, 30), 4);
                if (numbered) Ui.Text(b, (n + 1).ToString(), new Vector2(rect.X + 6, rect.Y + 2), Color.White);
            }
            var wantedBy = Shop.Commissions.Accepted(_who).FirstOrDefault(c => c.ItemId == item.QualifiedItemId);
            if (wantedBy != null && !_setupFraming)
            {
                // A little gold ribbon: someone's waiting on a painting of this.
                Ui.Fill(b, new Rectangle(rect.Right - 14, rect.Y + 4, 10, 14), new Color(214, 160, 30));
                Ui.Fill(b, new Rectangle(rect.Right - 12, rect.Y + 18, 6, 4), new Color(150, 100, 0));
            }
            if (rect.Contains(mouse))
                _hoverText = wantedBy == null ? item.DisplayName
                    : item.DisplayName + "\n" + I18n.Get("setup.commissioned", new { npc = wantedBy.NpcName, quality = Shop.Commissions.QualityText(wantedBy.MinQuality) });
        }
    }
}
