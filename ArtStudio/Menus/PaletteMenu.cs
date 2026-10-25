using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

// A placed Palette, opened like a chest that only takes paint: a well for
// every colour, showing how many tubes' worth it holds (click one to take a
// whole tube out), and the tubes in your bag underneath (click to put one
// in, shift-click for the whole stack).
internal sealed class PaletteMenu : IClickableMenu
{
    private const int MaxWidth = 980, MaxHeight = 680, Pad = 36, Slot = 64, SlotGap = 8, Cols = 3;

    private readonly Farmer _who;
    private readonly Item _palette;
    private readonly List<(Rectangle Rect, int Pigment)> _wells = new();
    private readonly List<(Rectangle Rect, Item Tube)> _bag = new();
    private Rectangle _bagArea;
    private string _note = "";
    private const float NoteScale = 0.85f;
    private string? _hoverText;

    public PaletteMenu(Farmer who, Item palette)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        _palette = palette;
        PaletteItem.Initialize(palette);
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();

        // The note wraps to the menu's width; the wells start under it.
        _note = Game1.parseText(I18n.Get("palette.menu.note", new { tubes = FormatTubes(PaletteItem.Capacity(_palette, _who)) }),
            Game1.smallFont, (int)((width - 2 * Pad - 16) / NoteScale));
        _wells.Clear();
        int top = yPositionOnScreen + Pad + 44 + (int)(Game1.smallFont.MeasureString(_note).Y * NoteScale) + 12;
        int colW = (width - 2 * Pad - (Cols - 1) * 12) / Cols;
        int rows = (Palette.All.Length + Cols - 1) / Cols;
        int rowH = 40;
        for (int i = 0; i < Palette.All.Length; i++)
            _wells.Add((new Rectangle(xPositionOnScreen + Pad + i % Cols * (colW + 12), top + i / Cols * (rowH + 4), colW, rowH), i));

        int bagTop = top + rows * (rowH + 4) + 44;
        _bagArea = new Rectangle(xPositionOnScreen + Pad, bagTop, width - 2 * Pad, yPositionOnScreen + height - Pad - bagTop);
        _bag.Clear();
        int perRow = Math.Max(1, (_bagArea.Width + SlotGap) / (Slot + SlotGap));
        int n = 0;
        foreach (var item in _who.Items)
        {
            if (item == null || ArtItems.TubeFor(item) == null) continue;
            var rect = new Rectangle(_bagArea.X + n % perRow * (Slot + SlotGap), _bagArea.Y + n / perRow * (Slot + SlotGap), Slot, Slot);
            if (rect.Bottom > _bagArea.Bottom) break;
            _bag.Add((rect, item));
            n++;
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        foreach (var (rect, pigment) in _wells)
        {
            if (!rect.Contains(x, y)) continue;
            var tube = PaletteItem.Read(_palette)[pigment] >= PaletteItem.TubeCells ? ItemRegistry.Create("(O)" + ArtItems.TubeId(pigment)) : null;
            if (tube != null && _who.couldInventoryAcceptThisItem(tube) && PaletteItem.Withdraw(_palette, pigment) is { } taken)
            {
                _who.addItemToInventoryBool(taken);
                Game1.playSound("pickUpItem");
                Layout();
            }
            else
                Game1.playSound("cancel");
            return;
        }
        foreach (var (rect, tube) in _bag)
        {
            if (!rect.Contains(x, y)) continue;
            bool all = Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
            if (PaletteItem.Deposit(_who, _palette, tube, all ? int.MaxValue : 1) > 0)
            {
                Game1.playSound("Ship");
                Layout();
            }
            else
            {
                Game1.playSound("cancel");
                Game1.showRedMessage(I18n.Get("palette.full", new { pigment = I18n.Pigment(ArtItems.TubeFor(tube)!.Value) }));
            }
            return;
        }
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        Ui.AuditMenu(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        _hoverText = null;
        var mouse = new Point(Game1.getMouseX(true), Game1.getMouseY(true));
        int left = xPositionOnScreen + Pad + 8;

        int capacity = PaletteItem.Capacity(_palette, _who);
        Ui.Text(b, _palette.DisplayName, new Vector2(left, yPositionOnScreen + Pad + 6));
        Ui.Text(b, _note, new Vector2(left, yPositionOnScreen + Pad + 44), Game1.textColor * 0.7f, NoteScale);

        var paint = PaletteItem.Read(_palette);
        foreach (var (rect, pigment) in _wells)
            DrawWell(b, rect, pigment, paint[pigment], capacity, mouse);

        Ui.Text(b, I18n.Get("palette.menu.bag"), new Vector2(left, _bagArea.Y - 36));
        if (_bag.Count == 0)
            Ui.Text(b, I18n.Get("palette.menu.no-tubes"), new Vector2(left, _bagArea.Y + 8), Game1.textColor * 0.6f);
        foreach (var (rect, tube) in _bag)
        {
            b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
            tube.drawInMenu(b, new Vector2(rect.X, rect.Y), 1f, 1f, 0.9f, StackDrawType.Draw, Color.White, false);
            if (rect.Contains(mouse))
                _hoverText = tube.DisplayName + "\n" + I18n.Get("palette.menu.put-in");
        }

        base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private void DrawWell(SpriteBatch b, Rectangle rect, int pigment, int cells, int capacity, Point mouse)
    {
        bool known = ArtProgress.Owns(_who, pigment);
        Ui.Fill(b, rect, Color.Black * (rect.Contains(mouse) ? 0.1f : 0.05f));
        var p = Palette.All[pigment];
        var dab = new Rectangle(rect.X + 6, rect.Y + 6, rect.Height - 12, rect.Height - 12);
        Ui.Fill(b, dab, new Color(p.R, p.G, p.B) * (known || cells > 0 ? 1f : 0.3f));
        Ui.Outline(b, dab, Color.Black * 0.4f, 2);
        int x = dab.Right + 10;
        // How full the well is, under the name.
        var gauge = new Rectangle(x, rect.Bottom - 10, rect.Right - 8 - x, 5);
        Ui.Fill(b, gauge, new Color(60, 40, 30) * 0.5f);
        Ui.Fill(b, new Rectangle(gauge.X, gauge.Y, (int)(gauge.Width * Math.Clamp(cells / (float)capacity, 0f, 1f)), gauge.Height), new Color(p.R, p.G, p.B));
        Ui.Text(b, I18n.Pigment(pigment), new Vector2(x, rect.Y + 2), known || cells > 0 ? Game1.textColor : Game1.textColor * 0.45f, 0.8f);
        string amount = FormatTubes(cells);
        Ui.Text(b, amount, new Vector2(rect.Right - 8 - Ui.TextWidth(amount) * 0.8f, rect.Y + 2), Game1.textColor * 0.75f, 0.8f);
        if (rect.Contains(mouse))
            _hoverText = I18n.Pigment(pigment) + "\n" + I18n.Get("palette.menu.well", new { cells, capacity })
                + "\n" + (cells >= PaletteItem.TubeCells ? I18n.Get("palette.menu.take-out") : I18n.Get("palette.menu.not-a-tube"));
    }

    // "2.5 tubes": a well's paint in tubes, to a tenth.
    private static string FormatTubes(int cells) =>
        cells == PaletteItem.TubeCells ? I18n.Get("palette.menu.tube")
        : I18n.Get("palette.menu.tubes", new { tubes = (cells / (float)PaletteItem.TubeCells).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) });
}
