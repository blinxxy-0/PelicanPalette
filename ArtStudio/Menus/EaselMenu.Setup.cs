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
    // Pages, when there's more to paint than fits (1.3: with many friends,
    // monsters, animals and a full bag at once, the rest had been left off).
    private int _gridPage, _sitterPage, _gridPages = 1, _sitterPages = 1;
    private Rectangle _gridPrev, _gridNext, _sitterPrev, _sitterNext;
    // The strip above the bottom buttons for the room's frame bonuses.
    private const int StudioStrip = 56;
    private int _studioY;
    private List<string> _allSitters = new();     // every page's, for the row's label

    private static bool IsSitterSubject(IReadOnlyList<string> ids) => ids.Count == 1 && Sitters.IsSitter(ids[0]);
    private bool SetupPortrait => IsSitterSubject(_setupItems);
    private bool SetupView => IsViewSubject(_setupItems);
    // Creatures (1.2) and animals (1.3): a slot each in the row below opens
    // them in place of the bag. _setupStudy is the open one's slot, or null.
    private bool SetupCreature => IsCreatureSubject(_setupItems);
    private bool CreatureTooSmall => SetupCreature && !Studies.Fits(_setupItems[0], PaintingRules.CanvasSize(_setupCanvas));
    private string? _setupStudy;
    private bool _setupCreatures => _setupStudy != null;
    private readonly List<(Rectangle Rect, string Subject)> _creatureSlots = new();
    private const string CreatureSlot = Creatures.Prefix, AnimalSlot = Animals.Prefix, RestoreSlot = OldPaintings.Prefix;
    private static bool IsStudySlot(string slot) => slot == CreatureSlot || slot == AnimalSlot || slot == RestoreSlot;
    // Restoring an old painting (1.3): it's its own canvas, so none is used.
    private bool SetupRestoring => _setupItems.Count == 1 && OldPaintings.IsOld(_setupItems[0]);
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
        int at = _viewLocation != null ? 1 : 0;
        foreach (string slot in new[] { RestoreSlot, CreatureSlot, AnimalSlot })
        {
            bool any = !_setupFraming && StardewModdingAPI.Context.IsWorldReady && Studies.Available(_who, slot).Count > 0;
            if (any)
                sitters.Insert(at++, slot);
            else if (_setupStudy == slot)
                _setupStudy = null;
        }
        var creatures = _setupStudy != null ? Studies.Available(_who, _setupStudy) : new List<string>();
        _creatureSlots.Clear();

        // From the bottom up: the buttons, the frame strip (at a placed easel),
        // the sitters' row, and the grid in what's left, a page at a time.
        int bottom = rowY - 12;
        bool strip = _atPlacedEasel && !_setupFraming;
        if (strip)
        {
            bottom -= StudioStrip;
            _studioY = bottom + 8;
        }
        int itemsBottom = sitters.Count > 0 ? bottom - 40 - SlotSize - 8 : bottom;
        int rowsFit = Math.Max(1, (itemsBottom - gridY + SlotGap) / (SlotSize + SlotGap));
        int perPage = rowsFit * cols;
        Rectangle Cell(int i) => new(gridX + i % cols * (SlotSize + SlotGap), gridY + i / cols * (SlotSize + SlotGap), SlotSize, SlotSize);

        bool showCreatures = _setupCreatures && !_setupFraming;
        var items = new List<Item>();
        if (!showCreatures)
            foreach (var item in Game1.player.Items)
            {
                if (item == null) continue;
                bool wanted = _setupFraming
                    ? PaintingItems.IsPainting(item) && PaintingData.Read(item) != null
                    : ReferenceBuilder.CanPaint(item) && !items.Any(i => i.QualifiedItemId == item.QualifiedItemId);
                if (wanted) items.Add(item);
            }
        int total = showCreatures ? creatures.Count : items.Count;
        _gridPages = Math.Max(1, (total + perPage - 1) / perPage);
        _gridPage = Math.Clamp(_gridPage, 0, _gridPages - 1);
        int first = _gridPage * perPage, n = 0;
        for (int i = first; i < Math.Min(total, first + perPage); i++, n++)
        {
            if (showCreatures) _creatureSlots.Add((Cell(n), creatures[i]));
            else _itemSlots.Add((Cell(n), items[i]));
        }
        int gridRight = gridX + cols * (SlotSize + SlotGap) - SlotGap;
        (_gridPrev, _gridNext) = PageArrows(gridRight, _canvasButtons[0].Bottom + 14, _gridPages);

        int rows = _gridPages > 1 ? rowsFit : n == 0 ? 1 : (n + cols - 1) / cols;
        _sitterLabelY = gridY + rows * (SlotSize + SlotGap) + 4;
        _allSitters = sitters;
        _sitterPages = Math.Max(1, (sitters.Count + cols - 1) / cols);
        _sitterPage = Math.Clamp(_sitterPage, 0, _sitterPages - 1);
        for (int i = _sitterPage * cols, k = 0; i < sitters.Count && k < cols; i++, k++)
            _sitterSlots.Add((new Rectangle(gridX + k * (SlotSize + SlotGap), _sitterLabelY + 36, SlotSize, SlotSize), sitters[i]));
        (_sitterPrev, _sitterNext) = PageArrows(gridRight, _sitterLabelY - 4, _sitterPages);
    }

    // The game's small arrows, at the right end of a label's line, when
    // there's more than one page.
    private static (Rectangle Prev, Rectangle Next) PageArrows(int right, int y, int pages) =>
        pages <= 1 ? (Rectangle.Empty, Rectangle.Empty)
            : (new Rectangle(right - 36 - 64 - 36, y, 36, 33), new Rectangle(right - 36, y, 36, 33));

    private void DrawPageArrows(SpriteBatch b, Rectangle prev, Rectangle next, int page, int pages, Point mouse)
    {
        if (pages <= 1) return;
        b.Draw(Game1.mouseCursors, prev, new Rectangle(352, 495, 12, 11), Color.White * (page > 0 ? (prev.Contains(mouse) ? 1f : 0.85f) : 0.3f));
        b.Draw(Game1.mouseCursors, next, new Rectangle(365, 495, 12, 11), Color.White * (page < pages - 1 ? (next.Contains(mouse) ? 1f : 0.85f) : 0.3f));
        string text = $"{page + 1}/{pages}";
        Ui.Text(b, text, new Vector2((prev.Right + next.X) / 2f - Ui.TextWidth(text) / 2, prev.Y), Game1.textColor * 0.8f);
    }

    // The wheel turns the page of whichever row the mouse is over.
    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        // Painting: over the canvas or the reference, the wheel zooms (1.3).
        if (_stage == EaselStage.Painting)
        {
            var mouse = MousePos;
            if (_mix == null && (_canvasArea.Contains(mouse) || _refArea.Contains(mouse)))
                ZoomAt(mouse, direction > 0 ? 1 : -1);
            return;
        }
        if (_stage != EaselStage.Setup) return;
        int d = direction > 0 ? -1 : 1;
        bool grid = Game1.getMouseY() < _sitterLabelY || _sitterPages <= 1;
        ref int page = ref grid ? ref _gridPage : ref _sitterPage;
        int pages = grid ? _gridPages : _sitterPages;
        if (pages <= 1 || page + d < 0 || page + d >= pages) return;
        page += d;
        Game1.playSound("shwip");
        LayoutSetup();
    }

    // Turns a page if an arrow was clicked.
    private bool ClickPage(int x, int y)
    {
        int Turn(ref int page, int pages, Rectangle prev, Rectangle next)
        {
            if (prev.Contains(x, y) && page > 0) return -1;
            if (next.Contains(x, y) && page < pages - 1) return 1;
            return 0;
        }
        int d = Turn(ref _gridPage, _gridPages, _gridPrev, _gridNext);
        if (d != 0) _gridPage += d;
        else if ((d = Turn(ref _sitterPage, _sitterPages, _sitterPrev, _sitterNext)) != 0) _sitterPage += d;
        else return false;
        Game1.playSound("shwip");
        LayoutSetup();
        return true;
    }

    private bool CanvasUnlocked(CanvasKind kind) => _level >= PaintingRules.CanvasUnlockLevel(kind);

    private int CountInBag(string qualifiedId) =>
        Game1.player.Items.Where(i => i?.QualifiedItemId == qualifiedId).Sum(i => i.Stack);

    private int CanvasCount(CanvasKind kind) => CountInBag(ArtItems.QualifiedCanvasId(kind));

    private int FrameCount => CountInBag("(O)" + ArtItems.FrameItemId);

    // Framing in a Moonlit Frame (1.2) or a gold-leaf one (1.3) instead of a
    // plain one: offered once you have one, as buttons where the canvas
    // sizes usually are (plain first).
    private FrameStyle? _frameStyle;
    private int StyleCount(FrameStyle style) => CountInBag("(O)" + style.ItemId);
    private List<FrameStyle> OfferedStyles => ArtItems.FrameStyles.Where(s => s == ArtItems.Plain || StyleCount(s) > 0).ToList();
    private bool FrameStylesOffered => OfferedStyles.Count > 1;

    // The frame picker (1.3): a row of the frames you have, in the canvas
    // sizes' row, each its icon and how many; hovering shows its rarity and
    // what it does when it hangs.
    private const int StyleCell = 46;
    private Rectangle StyleButton(int i, int count)
    {
        var first = _canvasButtons[0];
        return new Rectangle(first.X + i * (StyleCell + 2), first.Y + (first.Height - StyleCell) / 2, StyleCell, StyleCell);
    }
    private readonly Dictionary<string, Item> _frameIcons = new();
    private Item FrameIcon(FrameStyle style) =>
        _frameIcons.TryGetValue(style.Id, out var icon) ? icon : _frameIcons[style.Id] = ItemRegistry.Create("(O)" + style.ItemId);

    private string FrameHover(FrameStyle style)
    {
        var (rarity, trait) = Frames.Of(style);
        return FrameIcon(style).DisplayName + " (" + I18n.Get("frames.rarity." + rarity.ToString().ToLowerInvariant()) + ")\n"
            + I18n.Get("frames.trait." + trait.ToString().ToLowerInvariant()) + "\n" + I18n.Get("frames.have", new { frames = StyleCount(style) });
    }
    // The chosen style, if you have that frame; else plain, else the first you have.
    private FrameStyle UsingStyle => _frameStyle != null && StyleCount(_frameStyle) > 0 ? _frameStyle
        : FrameCount > 0 ? ArtItems.Plain
        : ArtItems.FrameStyles.FirstOrDefault(s => StyleCount(s) > 0) ?? ArtItems.Plain;
    private int FramesToUse => StyleCount(UsingStyle);

    private bool HasCanvas => !_needsSupplies || SetupRestoring || (_setupLinen ? LinenCount(_setupCanvas) : CanvasCount(_setupCanvas)) > 0;

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
            : CreatureTooSmall ? null
            : BuildReference(_setupItems, size);
        // Whether simple pictures would change this one: only a picture
        // with glazes or highlights to take out (none on a small canvas).
        _simpleChanges = _setupPreview != null && !SetupRestoring
            && (ModEntry.Config.SimplePictures ? BuildReference(_setupItems, size, simple: false) : _setupPreview)?.UsesTechniques == true;
    }

    private bool _simpleChanges;

    private void UpdateSetup()
    {
    }

    private void ClickSetup(int x, int y)
    {
        if (_setupFraming && FrameStylesOffered)
            for (int i = 0; i < OfferedStyles.Count; i++)
                if (StyleButton(i, OfferedStyles.Count).Contains(x, y))
                {
                    _frameStyle = OfferedStyles[i];
                    Game1.playSound("smallSelect");
                    return;
                }
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

        if (ClickPage(x, y)) return;

        foreach (var (rect, subject) in _creatureSlots)
        {
            if (!rect.Contains(x, y)) continue;
            Game1.playSound("smallSelect");
            _setupOriginal = false;
            bool again = _setupItems.Contains(subject);
            _setupItems.Clear();
            if (!again)
            {
                _setupItems.Add(subject);
                // A bigger creature needs a bigger canvas: the smallest it fits that's unlocked.
                foreach (var kind in new[] { CanvasKind.Small, CanvasKind.Medium, CanvasKind.Large })
                    if (CanvasUnlocked(kind) && Studies.Fits(subject, PaintingRules.CanvasSize(kind)) && kind >= _setupCanvas)
                    {
                        _setupCanvas = kind;
                        break;
                    }
            }
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
            if (SetupPortrait || SetupView || SetupCreature) _setupItems.Clear();
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
            if (IsStudySlot(npc))
            {
                _setupStudy = _setupStudy == npc ? null : npc;
                _gridPage = 0;
                Game1.playSound("shwip");
                LayoutSetup();
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
            _gridPage = _sitterPage = 0;
            Game1.playSound("smallSelect");
            LayoutSetup();
            RefreshPreview();
        }
        else if (_modeButton.Contains(x, y))
            CycleMode();
        else if (_simpleButton.Contains(x, y))
            ToggleSimple();
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
    private bool TooLate => !Context.IsMultiplayer && Context.IsWorldReady && !Game1.isFestival()
        && ArtHooks.FinishTime(PaintingRules.Minutes(_setupCanvas, IsLongSubject(_setupItems))) > PaintingRules.LatestFinish;

    private bool NeedsPalette => UsesPaint && _palette == null;

    private Rectangle _modeButton, _simpleButton;

    // Simple pictures on or off; saved, and the reference redrawn.
    private void ToggleSimple()
    {
        ModEntry.Config.SimplePictures = !ModEntry.Config.SimplePictures;
        ModEntry.SaveConfig();
        Game1.playSound("smallSelect");
        RefreshPreview();
    }

    // "Leah commissioned a painting of this (gold)", saying when it's to be glazed.
    private static string CommissionedText(Shop.Commission wantedBy) =>
        I18n.Get(wantedBy.Requirement == Shop.Commissions.Glazed ? "setup.commissioned.glazed" : "setup.commissioned",
            new { npc = wantedBy.NpcName, quality = Shop.Commissions.QualityText(wantedBy.MinQuality) });

    // Relaxed, Normal, Hard, and round again; saved, and used from this painting on.
    private void CycleMode()
    {
        ModEntry.Config.Difficulty = (Difficulty)(((int)ModEntry.Config.Difficulty + 1) % 3);
        ModEntry.SaveConfig();
        _rules = DifficultySettings.For(ModEntry.Config.Difficulty);
        LoadPalette();
        Game1.playSound("smallSelect");
        LayoutSetup();
        RefreshPreview();
    }

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
        if (CreatureTooSmall)
        {
            Game1.playSound("cancel");
            Toast(I18n.Get("setup.creature-canvas"), new Color(150, 70, 50), 2.5f);
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
        if (_needsSupplies && !SetupRestoring)
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
        if (_setupPainting == null || FramesToUse <= 0)
        {
            Game1.playSound("cancel");
            return;
        }
        var style = UsingStyle;
        string frameId = "(O)" + style.ItemId;
        var frameItem = _who.Items.First(i => i?.QualifiedItemId == frameId);
        if (frameItem.Stack > 1) frameItem.Stack--;
        else _who.removeItemFromInventory(frameItem);

        var framed = ArtHooks.Frame(_setupPainting, style);
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
        DrawPageArrows(b, _gridPrev, _gridNext, _gridPage, _gridPages, mouse);
        DrawPageArrows(b, _sitterPrev, _sitterNext, _sitterPage, _sitterPages, mouse);

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
        Ui.Text(b, _setupStudy == RestoreSlot ? I18n.Get("setup.pick-old") : _setupStudy == AnimalSlot ? I18n.Get("setup.pick-animal") : _setupCreatures ? I18n.Get("setup.pick-creature") : max == 1 ? I18n.Get("setup.pick-one") : I18n.Get("setup.pick-many", new { max }),
            new Vector2(left, _canvasButtons[0].Bottom + 18));
        if (_itemSlots.Count == 0 && !_setupCreatures)
            Ui.Text(b, I18n.Get("setup.nothing-to-paint"), new Vector2(left, _canvasButtons[0].Bottom + 70), Game1.textColor * 0.6f);
        DrawSlots(b, mouse, slot => _setupItems.IndexOf(slot.QualifiedItemId), max > 1);
        DrawCreatures(b, mouse);
        DrawSitters(b, left, mouse);
        DrawStudio(b, mouse);

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
        bool warn = portraitTooSmall || CreatureTooSmall || !HasCanvas || TooLate || NeedsPalette || shortOf.Count > 0;
        string footer = portraitTooSmall ? I18n.Get("setup.portrait-canvas-short")
            : CreatureTooSmall ? I18n.Get("setup.creature-canvas-short")
            : !HasCanvas ? I18n.Get(_setupLinen ? "setup.need-linen" : "setup.need-canvas." + CanvasKeys[(int)_setupCanvas])
            : TooLate ? I18n.Get("setup.too-late-short")
            : NeedsPalette ? I18n.Get("setup.need-palette-short")
            : shortOf.Count > 0 ? I18n.Get("setup.low-paint", new { pigments = string.Join(", ", shortOf.Take(3).Select(I18n.Pigment)) })
            : I18n.Get("setup.mode-takes", new { mode = I18n.Get("setup.mode." + ModEntry.Config.Difficulty.ToString().ToLowerInvariant()), hours });
        var footerAt = new Vector2(_previewArea.X - 20, _beginButton.Bottom + 10);
        // With nothing to warn about, the footer is the mode, and clicking it
        // changes it (1.1.1: players didn't know Relaxed, with no time limit,
        // was there, and it was only in the config).
        _modeButton = warn || _setupFraming ? Rectangle.Empty
            : new Rectangle((int)footerAt.X - 4, (int)footerAt.Y - 2, (int)Ui.TextWidth(footer) + 8, 32);
        bool modeHover = _modeButton.Contains(mouse);
        if (_modeButton != Rectangle.Empty)
        {
            if (modeHover)
                Ui.Fill(b, _modeButton, Color.White * 0.35f);
            Ui.Fill(b, new Rectangle((int)footerAt.X, _modeButton.Bottom - 4, (int)Ui.TextWidth(footer), 2), Game1.textColor * (modeHover ? 0.8f : 0.35f));
        }
        Ui.Text(b, footer, footerAt, warn ? new Color(150, 70, 50) : Game1.textColor * (modeHover ? 1f : 0.8f));
        if (modeHover)
            _hoverText = Game1.parseText(I18n.Get("setup.mode-hover." + ModEntry.Config.Difficulty.ToString().ToLowerInvariant()), Game1.smallFont, 520);

        // Below it, simple pictures (1.3: flat colour was Relaxed's until
        // players asked for the clock to go without the picture going too).
        string simple = I18n.Get(ModEntry.Config.SimplePictures ? "setup.simple.on" : "setup.simple.off");
        // (Above the reference it changes: under the mode there's no room.)
        var simpleAt = new Vector2(_previewArea.Center.X - Ui.TextWidth(simple) / 2, _previewArea.Y - 58);
        _simpleButton = _setupFraming ? Rectangle.Empty
            : new Rectangle((int)simpleAt.X - 4, (int)simpleAt.Y - 2, (int)Ui.TextWidth(simple) + 8, 32);
        if (_simpleButton != Rectangle.Empty)
        {
            bool simpleHover = _simpleButton.Contains(mouse);
            // Greyed when this picture looks the same either way.
            float shade = _simpleChanges ? 1f : 0.55f;
            if (simpleHover)
                Ui.Fill(b, _simpleButton, Color.White * 0.35f);
            Ui.Fill(b, new Rectangle((int)simpleAt.X, _simpleButton.Bottom - 4, (int)Ui.TextWidth(simple), 2), Game1.textColor * (simpleHover ? 0.8f : 0.35f) * shade);
            Ui.Text(b, simple, simpleAt, Game1.textColor * (simpleHover ? 1f : 0.8f) * shade);
            if (simpleHover)
                _hoverText = Game1.parseText(I18n.Get("setup.simple-hover") + (_setupPreview != null && !_simpleChanges
                    ? "\n\n" + I18n.Get(_setupCanvas == CanvasKind.Small ? "setup.simple-same-small" : "setup.simple-same") : ""), Game1.smallFont, 520);
        }
        if (!HasCanvas && _beginButton.Contains(mouse))
            _hoverText = _setupCanvas == CanvasKind.Small ? I18n.Get("setup.craft-small") : I18n.Get("setup.craft-big");
    }

    // What the framed paintings in this room give the painting (1.3: players
    // couldn't tell what frames did): an icon for each bonus, like the
    // game's buff icons, in the strip above the bottom buttons. Hovering one
    // says what it does; with none hung, a line says how to get them.
    private void DrawStudio(SpriteBatch b, Point mouse)
    {
        if (!_atPlacedEasel || _setupFraming) return;
        var bonuses = _studio.Bonuses().ToList();
        int x = _originalButton.X, y = _studioY;
        if (bonuses.Count == 0 || Frames.Icons == null)
        {
            Ui.Text(b, I18n.Get("setup.studio.none"), new Vector2(x, y + 10), Game1.textColor * 0.6f);
            return;
        }
        Ui.Text(b, I18n.Get("setup.studio"), new Vector2(x, y + 10), Game1.textColor * 0.75f);
        x += (int)Ui.TextWidth(I18n.Get("setup.studio")) + 14;
        const int Icon = 48;     // 3x, as the game draws its buff icons
        foreach (var (trait, percent) in bonuses)
        {
            var rect = new Rectangle(x, y, Icon, Icon);
            b.Draw(Frames.Icons, rect, Frames.IconSource(trait), Color.White);
            if (rect.Contains(mouse))
                _hoverText = Game1.parseText(I18n.Get("setup.studio." + trait.ToString().ToLowerInvariant(), new { percent = percent.ToString("0.#") })
                    + "\n" + I18n.Get("setup.studio-why"), Game1.smallFont, 520);
            x += Icon + 8;
        }
    }

    private void DrawFraming(SpriteBatch b, int left, Point mouse)
    {
        Ui.Text(b, I18n.Get("setup.frame-title"), new Vector2(left, yPositionOnScreen + Pad + 30));
        if (FrameStylesOffered)
        {
            var styles = OfferedStyles;
            for (int i = 0; i < styles.Count; i++)
            {
                var style = styles[i];
                var cell = StyleButton(i, styles.Count);
                // On an inventory slot, like the bag's; the chosen one outlined.
                b.Draw(Game1.menuTexture, cell, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White * (cell.Contains(mouse) ? 1f : 0.85f));
                if (style == UsingStyle)
                    foreach (var edge in new[] { new Rectangle(cell.X - 3, cell.Y - 3, cell.Width + 6, 3), new Rectangle(cell.X - 3, cell.Bottom, cell.Width + 6, 3),
                                 new Rectangle(cell.X - 3, cell.Y, 3, cell.Height), new Rectangle(cell.Right, cell.Y, 3, cell.Height) })
                        b.Draw(Game1.staminaRect, edge, new Color(214, 72, 32));
                FrameIcon(style).drawInMenu(b, new Vector2(cell.Center.X - 32, cell.Center.Y - 32), 0.72f, StyleCount(style) > 0 ? 1f : 0.4f, 0.9f,
                    StackDrawType.Hide, Color.White, false);
                int count = StyleCount(style);
                Utility.drawTinyDigits(count, b, new Vector2(cell.Right - Utility.getWidthOfTinyDigitString(count, 2f) - 3, cell.Bottom - 15), 2f, 1f, Color.White);
                if (cell.Contains(mouse))
                    _hoverText = FrameHover(style);
            }
        }
        else
            Ui.Text(b, I18n.Get("setup.frames", new { frames = FrameCount }), new Vector2(left, _canvasButtons[0].Y + 14), FrameCount > 0 ? Game1.textColor : new Color(150, 70, 50));
        if (_itemSlots.Count == 0)
            Ui.Text(b, I18n.Get("setup.no-paintings"), new Vector2(left, _canvasButtons[0].Bottom + 70), Game1.textColor * 0.6f);
        DrawSlots(b, mouse, slot => slot == _setupPainting ? 0 : -1, false);

        Ui.Paper(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40));
        if (UsingStyle == ArtItems.Moonlit)
            DrawMoonlitBorder(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40));
        else if (UsingStyle == ArtItems.GoldLeaf)
            DrawGoldLeafBorder(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40));
        else if (SetFrameColours.TryGetValue(UsingStyle.Id, out var colours))
            DrawSetFrameBorder(b, new Rectangle(_previewArea.X - 20, _previewArea.Y - 20, _previewArea.Width + 40, _previewArea.Height + 40), colours);
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

        bool ready = _setupPainting != null && FramesToUse > 0;
        Ui.Button(b, _beginButton, I18n.Get("setup.frame-it"), ready, false, ready && _beginButton.Contains(mouse));
        if (FramesToUse == 0)
            Ui.Text(b, I18n.Get("setup.need-frame"), new Vector2(_previewArea.X - 20, _beginButton.Bottom + 10), new Color(150, 70, 50));
    }

    // The preview in the Moonlit Frame's colours, so the choice shows: midnight
    // blue, a silver inner edge, and the moon at the top (as the furniture has).
    private static void DrawMoonlitBorder(SpriteBatch b, Rectangle r)
    {
        var midnight = new Color(36, 44, 92);
        var dark = new Color(22, 26, 58);
        var silver = new Color(168, 180, 210);
        var moon = new Color(232, 214, 150);
        Ui.Fill(b, r, dark);
        Ui.Fill(b, new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8), midnight);
        Ui.Fill(b, new Rectangle(r.X + 16, r.Y + 16, r.Width - 32, r.Height - 32), silver);
        int cx = r.Center.X;
        foreach (var (x, y, w, h) in new[] { (-8, 0, 12, 4), (-12, 4, 8, 4), (-12, 8, 4, 4), (-12, 12, 8, 4), (-8, 16, 12, 4) })
            Ui.Fill(b, new Rectangle(cx + x, r.Y + 2 + y, w, h), moon);
    }

    // The collection's frames in the preview (1.3): each one's outline, wood
    // or metal, highlight and gem, as tools/art/set_frames.py draws them.
    private static readonly Dictionary<string, (Color Dark, Color Mid, Color Light, Color Gem)> SetFrameColours = new()
    {
        ["driftwood"] = (new(58, 56, 60), new(162, 156, 146), new(224, 220, 208), new(200, 236, 250)),
        ["iron"] = (new(30, 30, 38), new(108, 108, 120), new(176, 176, 188), new(230, 170, 110)),
        // 1.3's other frames (tools/art/frame_styles.py's palettes).
        ["pine"] = (new(110, 72, 40), new(226, 184, 118), new(250, 228, 182), new(160, 110, 60)),
        ["oak"] = (new(64, 38, 22), new(172, 122, 66), new(220, 178, 116), new(104, 64, 34)),
        ["birch"] = (new(150, 144, 132), new(236, 232, 220), new(252, 250, 244), new(70, 64, 58)),
        ["white"] = (new(150, 150, 156), new(230, 230, 234), new(252, 252, 254), new(214, 214, 220)),
        ["walnut"] = (new(36, 20, 14), new(112, 72, 44), new(164, 118, 76), new(214, 178, 96)),
        ["red-lacquer"] = (new(70, 12, 16), new(186, 48, 44), new(232, 104, 84), new(236, 196, 84)),
        ["rope"] = (new(96, 70, 40), new(196, 164, 112), new(236, 214, 170), new(136, 104, 64)),
        ["prismatic"] = (new(90, 60, 140), new(90, 190, 220), new(250, 220, 120), new(240, 110, 160)),
        ["qi"] = (new(40, 14, 58), new(132, 64, 170), new(200, 140, 230), new(236, 196, 84)),
        ["sandstone"] = (new(110, 66, 40), new(222, 178, 112), new(250, 232, 190), new(190, 250, 240)),
        ["dwarvish"] = (new(52, 34, 26), new(176, 122, 56), new(232, 192, 108), new(190, 250, 240)),
    };

    private static void DrawSetFrameBorder(SpriteBatch b, Rectangle r, (Color Dark, Color Mid, Color Light, Color Gem) c)
    {
        Ui.Fill(b, r, c.Dark);
        Ui.Fill(b, new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8), c.Mid);
        Ui.Fill(b, new Rectangle(r.X + 8, r.Y + 8, r.Width - 16, 4), c.Light);
        Ui.Fill(b, new Rectangle(r.X + 16, r.Y + 16, r.Width - 32, r.Height - 32), c.Light);
        Ui.Fill(b, new Rectangle(r.Center.X - 6, r.Y + 2, 12, 8), c.Gem);
    }

    // The preview in gold leaf: a warm brown edge, gold, a pale gold inner
    // edge, and the crest at the top (as the furniture has).
    private static void DrawGoldLeafBorder(SpriteBatch b, Rectangle r)
    {
        Ui.Fill(b, r, new Color(82, 46, 18));
        Ui.Fill(b, new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8), new Color(220, 168, 52));
        Ui.Fill(b, new Rectangle(r.X + 8, r.Y + 8, r.Width - 16, 4), new Color(255, 238, 168));
        Ui.Fill(b, new Rectangle(r.X + 16, r.Y + 16, r.Width - 32, r.Height - 32), new Color(244, 206, 96));
        int cx = r.Center.X;
        foreach (var (x, y, w, h) in new[] { (-4, 0, 8, 4), (-8, 4, 16, 4), (-4, 8, 8, 4) })
            Ui.Fill(b, new Rectangle(cx + x, r.Y + 2 + y, w, h), new Color(255, 252, 226));
    }

    // Inventory slots; `order` gives a selected slot its number (or -1).
    // Friends who'll sit for a portrait: their faces, a gold ribbon if one
    // of them has commissioned it.
    private void DrawSitters(SpriteBatch b, int left, Point mouse)
    {
        if (_sitterSlots.Count == 0) return;
        bool creatures = _allSitters.Any(IsStudySlot);
        int friends = _allSitters.Count(s => s != ViewSlot && !IsStudySlot(s));
        bool viewOnly = friends == 0 && !creatures && _viewLocation != null;
        Ui.Text(b, I18n.Get(creatures ? "setup.pick-more" : viewOnly ? "setup.pick-view" : _viewLocation != null ? "setup.pick-view-or-portrait" : "setup.pick-portrait"), new Vector2(left, _sitterLabelY));
        foreach (var (rect, npc) in _sitterSlots)
        {
            b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
            if (npc == ViewSlot)
            {
                DrawViewSlot(b, rect, mouse);
                continue;
            }
            if (IsStudySlot(npc))
            {
                Studies.Draw(b, Studies.Available(_who, npc).FirstOrDefault() ?? Creatures.SubjectFor("Green Slime"), new Rectangle(rect.X + 10, rect.Y + 10, rect.Width - 20, rect.Height - 20));
                if (_setupStudy == npc)
                    Ui.Outline(b, rect, new Color(214, 160, 30), 4);
                if (rect.Contains(mouse))
                    _hoverText = I18n.Get(_setupStudy == npc ? "setup.creatures-back" : npc == AnimalSlot ? "setup.animals-hover" : npc == RestoreSlot ? "setup.restore-hover" : "setup.creatures-hover");
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
                    + (wantedBy == null ? "" : "\n" + CommissionedText(wantedBy));
        }
    }

    // Creatures you've met, or animals you own, in place of the bag.
    private void DrawCreatures(SpriteBatch b, Point mouse)
    {
        foreach (var (rect, subject) in _creatureSlots)
        {
            b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
            Studies.Draw(b, subject, new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16));
            if (_setupItems.Contains(subject))
                Ui.Outline(b, rect, new Color(214, 160, 30), 4);
            if (rect.Contains(mouse))
                _hoverText = I18n.Get(OldPaintings.IsOld(subject) ? "setup.old-hover" : Animals.IsAnimal(subject) ? "setup.animal-hover" : "setup.creature-hover", new { name = Studies.DisplayName(subject) });
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
                    : item.DisplayName + "\n" + CommissionedText(wantedBy);
        }
    }
}
