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
    private static readonly string[] CanvasNames = { "Small", "Medium", "Large" };

    private CanvasKind _setupCanvas = CanvasKind.Small;
    private bool _setupOriginal, _setupFraming;
    private readonly List<string> _setupItems = new();
    private Reference? _setupPreview;
    private SObject? _setupPainting;

    private Rectangle[] _canvasButtons = Array.Empty<Rectangle>();
    private readonly List<(Rectangle Rect, Item Item)> _itemSlots = new();
    private Rectangle _originalButton, _brushButton, _frameButton, _beginButton, _previewArea;

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

        int previewSize = Math.Min(320, height - 340);
        _previewArea = new Rectangle(xPositionOnScreen + width - Pad - 40 - previewSize, y + 40, previewSize, previewSize);
        _beginButton = new Rectangle(_previewArea.Center.X - 90, _previewArea.Bottom + 86, 180, 56);

        int rowY = yPositionOnScreen + height - Pad - 96;
        int rowW = (_previewArea.X - 60 - x - 2 * 12) / 3;
        _originalButton = new Rectangle(x, rowY, rowW, 56);
        _brushButton = new Rectangle(x + rowW + 12, rowY, rowW, 56);
        _frameButton = new Rectangle(x + 2 * (rowW + 12), rowY, rowW, 56);

        _itemSlots.Clear();
        int gridX = x, gridY = y + 110;
        int cols = Math.Max(1, (_previewArea.X - 40 - gridX) / (SlotSize + SlotGap));
        int n = 0;
        foreach (var item in Game1.player.Items)
        {
            if (item == null) continue;
            bool wanted = _setupFraming
                ? PaintingItems.IsPainting(item)
                : ReferenceBuilder.CanPaint(item) && !_itemSlots.Any(s => s.Item.QualifiedItemId == item.QualifiedItemId);
            if (!wanted) continue;
            var rect = new Rectangle(gridX + n % cols * (SlotSize + SlotGap), gridY + n / cols * (SlotSize + SlotGap), SlotSize, SlotSize);
            if (rect.Bottom > rowY - 12) break;
            _itemSlots.Add((rect, item));
            n++;
        }
    }

    private bool CanvasUnlocked(CanvasKind kind) => _level >= PaintingRules.CanvasUnlockLevel(kind);

    private int CountInBag(string qualifiedId) =>
        Game1.player.Items.Where(i => i?.QualifiedItemId == qualifiedId).Sum(i => i.Stack);

    private int CanvasCount(CanvasKind kind) => CountInBag(ArtItems.QualifiedCanvasId(kind));

    private int FrameCount => CountInBag("(O)" + ArtItems.FrameItemId);

    private bool HasCanvas => !_needsSupplies || CanvasCount(_setupCanvas) > 0;

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
        _setupPreview = !_setupOriginal && !_setupFraming && _setupItems.Count > 0
            ? ReferenceBuilder.StillLife(_setupItems, size, _owned)
            : null;
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

    private void TryBegin()
    {
        if (!_setupOriginal && _setupItems.Count == 0) return;
        if (!HasCanvas)
        {
            Game1.playSound("cancel");
            return;
        }
        Game1.playSound("bigSelect");
        if (_needsSupplies)
            UseCanvas(_setupCanvas);
        BeginPainting(_setupCanvas, _setupOriginal ? Array.Empty<string>() : _setupItems.ToArray());
    }

    private void UseCanvas(CanvasKind kind)
    {
        if (PaintingSkill.Has(_who, PaintingSkill.Instance.Prolific) && _random.NextDouble() < PaintingSkill.ProlificSaveChance)
        {
            Toast("Prolific: this one didn't use up a canvas!", new Color(60, 140, 60), 2f);
            return;
        }
        var canvas = _who.Items.First(i => i?.QualifiedItemId == ArtItems.QualifiedCanvasId(kind));
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
        Toast("Framed! Hang it on a wall.", new Color(60, 140, 60), 2f);
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
        Ui.Button(b, _originalButton, "Free canvas", !_setupFraming, _setupOriginal, _originalButton.Contains(mouse));
        string brushLabel = _brushItem == null ? "Hog bristle" : $"{ShortBrushName(_brush)} ({ArtHooks.UsesLeft(_brushItem, _brush)})";
        Ui.Button(b, _brushButton, brushLabel, !_setupFraming, false, _brushButton.Contains(mouse));
        Ui.Button(b, _frameButton, _setupFraming ? "Back to painting" : "Frame a painting", true, _setupFraming, _frameButton.Contains(mouse));
        if (_brushButton.Contains(mouse) && !_setupFraming)
            _hoverText = $"{_brush.Name}{(_brushItem == null ? "" : $", {ArtHooks.UsesLeft(_brushItem, _brush)} paintings left in it")}.\nClick to switch brushes.";
        if (_frameButton.Contains(mouse) && !_setupFraming)
            _hoverText = $"Turn a painting into wall decor. Uses a picture frame (you have {FrameCount}).";

        if (_toastLeft > 0f && _toast != null)
            Ui.TextCentered(b, _toast, _previewArea.Center.X, _previewArea.Y - 52, _toastColor * Math.Min(1f, _toastLeft * 2f));
    }

    private static string ShortBrushName(BrushSpec brush) => brush.Id switch
    {
        "sable" => "Sable",
        "flat" => "Flat wash",
        "rigger" => "Rigger",
        "iridium" => "Iridium",
        _ => "Hog bristle",
    };

    private void DrawStillLifeChoice(SpriteBatch b, int left, Point mouse)
    {
        Ui.Text(b, "Choose a canvas", new Vector2(left, yPositionOnScreen + Pad + 30));
        for (int i = 0; i < _canvasButtons.Length; i++)
        {
            var kind = (CanvasKind)i;
            bool unlocked = CanvasUnlocked(kind);
            int size = PaintingRules.CanvasSize(kind);
            string label = !unlocked ? $"Lv {PaintingRules.CanvasUnlockLevel(kind)}"
                : _needsSupplies ? $"{CanvasNames[i]} ({CanvasCount(kind)})"
                : CanvasNames[i];
            Ui.Button(b, _canvasButtons[i], label, unlocked, _setupCanvas == kind, _canvasButtons[i].Contains(mouse));
            if (_canvasButtons[i].Contains(mouse))
                _hoverText = unlocked
                    ? $"{CanvasNames[i]} canvas: {size}x{size}, room for {ReferenceBuilder.MaxItems(size)} item(s)."
                    : $"{CanvasNames[i]} canvases unlock at Painting level {PaintingRules.CanvasUnlockLevel(kind)}.";
        }

        int max = ReferenceBuilder.MaxItems(PaintingRules.CanvasSize(_setupCanvas));
        Ui.Text(b, max == 1 ? "Paint a still life: pick an item" : $"Paint a still life: pick up to {max} items",
            new Vector2(left, _canvasButtons[0].Bottom + 18));
        if (_itemSlots.Count == 0)
            Ui.Text(b, "Nothing in your bag to paint from.", new Vector2(left, _canvasButtons[0].Bottom + 70), Game1.textColor * 0.6f);
        DrawSlots(b, mouse, slot => _setupItems.IndexOf(slot.QualifiedItemId), max > 1);

        // Preview of what you'll be copying.
        Ui.Paper(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40));
        int canvasSize = PaintingRules.CanvasSize(_setupCanvas);
        var info = new Vector2(_previewArea.X - 20, _previewArea.Bottom + 28);
        if (_setupPreview != null)
        {
            DrawCells(b, _setupPreview.Cells, null, _previewArea, new Color(243, 236, 220), false);
            float light = PaintingRules.LightSeconds(canvasSize, _setupPreview.Difficulty, _level, LightMultiplier);
            Ui.Text(b, $"Difficulty {_setupPreview.Difficulty},  {_setupPreview.PigmentsUsed.Count} pigments", info);
            Ui.Text(b, _rules.LightTimer ? $"{light:0} seconds of light" : "No time limit", info + new Vector2(0, 28), Game1.textColor * 0.75f);
        }
        else
        {
            Ui.Fill(b, _previewArea, new Color(243, 236, 220));
            string msg = _setupOriginal ? "A blank canvas. Paint whatever you like." : "Pick something to paint.";
            var lines = Game1.parseText(msg, Game1.smallFont, _previewArea.Width - 40);
            Ui.Text(b, lines, new Vector2(_previewArea.X + 20, _previewArea.Center.Y - 20), Game1.textColor * 0.7f);
        }

        bool ready = (_setupOriginal || _setupItems.Count > 0) && HasCanvas;
        Ui.Button(b, _beginButton, "Begin", ready, false, ready && _beginButton.Contains(mouse));
        string footer = !HasCanvas ? $"You need a {CanvasNames[(int)_setupCanvas].ToLowerInvariant()} canvas." : $"{ModEntry.Config.Difficulty} mode";
        Ui.Text(b, footer, new Vector2(_previewArea.X - 20, _beginButton.Bottom + 10), !HasCanvas ? new Color(150, 70, 50) : Game1.textColor * 0.8f);
        if (!HasCanvas && _beginButton.Contains(mouse))
            _hoverText = _setupCanvas == CanvasKind.Small ? "Craft small canvases from fiber and wood." : "Craft bigger canvases from cloth and wood.";
    }

    private void DrawFraming(SpriteBatch b, int left, Point mouse)
    {
        Ui.Text(b, "Frame a painting to hang it on a wall", new Vector2(left, yPositionOnScreen + Pad + 30));
        Ui.Text(b, $"Picture frames: {FrameCount}", new Vector2(left, _canvasButtons[0].Y + 14), FrameCount > 0 ? Game1.textColor : new Color(150, 70, 50));
        if (_itemSlots.Count == 0)
            Ui.Text(b, "No paintings in your bag.", new Vector2(left, _canvasButtons[0].Bottom + 70), Game1.textColor * 0.6f);
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
            var lines = Game1.parseText("Pick a painting to frame.", Game1.smallFont, _previewArea.Width - 40);
            Ui.Text(b, lines, new Vector2(_previewArea.X + 20, _previewArea.Center.Y - 20), Game1.textColor * 0.7f);
        }

        bool ready = _setupPainting != null && FrameCount > 0;
        Ui.Button(b, _beginButton, "Frame it", ready, false, ready && _beginButton.Contains(mouse));
        if (FrameCount == 0)
            Ui.Text(b, "You need a picture frame.", new Vector2(_previewArea.X - 20, _beginButton.Bottom + 10), new Color(150, 70, 50));
    }

    // Inventory slots; `order` gives a selected slot its number (or -1).
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
                    : $"{item.DisplayName}\n{wantedBy.NpcName} commissioned a painting of this ({Shop.Commissions.QualityText(wantedBy.MinQuality)}).";
        }
    }
}
