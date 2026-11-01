using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

// The portfolio book (1.2): a page at a time, its slots showing the
// paintings kept in them, and your bag below. Click a painting in your bag
// and it goes in the slot on this page it's for (or improves on one already
// there, and the old one comes back to you). Paintings that fit somewhere on
// the page are lit; the rest are dimmed.
internal sealed class PortfolioMenu : IClickableMenu
{
    private const int MaxWidth = 1000, MaxHeight = 700, Pad = 36, TabH = 52, SlotSize = 112, SlotGap = 24;

    private readonly Farmer _who;
    private readonly bool _readOnly;
    private int _page;
    private InventoryMenu? _inventory;
    private readonly List<Rectangle> _tabs = new();
    private readonly List<Rectangle> _slots = new();
    private string? _hoverText;

    // Opens the book for whoever asked, once nobody else has it open.
    public static void Open(Farmer who, bool readOnly = false)
    {
        if (readOnly)
        {
            Game1.activeClickableMenu = new PortfolioMenu(who, readOnly: true);
            return;
        }
        Portfolio.Mutex.RequestLock(
            () =>
            {
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new PortfolioMenu(who, readOnly: false);
            },
            () => Game1.drawObjectDialogue(I18n.Get("portfolio.busy")));
    }

    // The menu without taking the book (screenshots and layout checks).
    internal static PortfolioMenu DevCreate(Farmer who, int page = 0) => new(who, readOnly: false) { _page = page };

    // Let the book go when its menu closes.
    public static void ReleaseIfClosed()
    {
        if (Portfolio.Mutex.IsLockHeld() && Game1.activeClickableMenu is not PortfolioMenu { _readOnly: false })
            Portfolio.Mutex.ReleaseLock();
    }

    private PortfolioMenu(Farmer who, bool readOnly)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        _readOnly = readOnly;
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(_readOnly ? 470 : MaxHeight, Game1.uiViewport.Height - 32);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();

        _tabs.Clear();
        int tabW = (width - 2 * Pad - 16 - (Portfolio.Pages.Length - 1) * 8) / Portfolio.Pages.Length;
        for (int i = 0; i < Portfolio.Pages.Length; i++)
            _tabs.Add(new Rectangle(xPositionOnScreen + Pad + 8 + i * (tabW + 8), yPositionOnScreen + Pad, tabW, TabH));

        _slots.Clear();
        int count = Portfolio.Pages[_page].Slots.Length;
        int rowW = count * SlotSize + (count - 1) * SlotGap;
        int slotY = yPositionOnScreen + Pad + TabH + 104;
        for (int i = 0; i < count; i++)
            _slots.Add(new Rectangle(xPositionOnScreen + (width - rowW) / 2 + i * (SlotSize + SlotGap), slotY, SlotSize, SlotSize));

        if (!_readOnly)
        {
            int invW = 12 * 64;
            _inventory = new InventoryMenu(xPositionOnScreen + (width - invW) / 2, yPositionOnScreen + height - Pad - 3 * 64 - 8, playerInventory: true,
                highlightMethod: item => Portfolio.SlotFor(_page, item) >= 0);
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        for (int i = 0; i < _tabs.Count; i++)
            if (_tabs[i].Contains(x, y) && i != _page)
            {
                _page = i;
                Game1.playSound("shwip");
                Layout();
                return;
            }
        if (_inventory == null) return;
        var item = _inventory.getItemAt(x, y);
        if (item == null) return;
        if (!Portfolio.IsKeepable(item) || Portfolio.SlotFor(_page, item) < 0)
        {
            Game1.playSound("cancel");
            return;
        }
        var (_, replaced) = Portfolio.Place(_who, _page, item);
        Game1.playSound(replaced != null ? "coin" : "newArtifact");
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        Ui.AuditMenu(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        _hoverText = null;
        var mouse = new Point(Game1.getMouseX(true), Game1.getMouseY(true));

        for (int i = 0; i < _tabs.Count; i++)
        {
            var page = Portfolio.Pages[i];
            string label = I18n.Get("portfolio.tab." + page.Id) + (Portfolio.IsFinished(page) ? " *" : "");
            Ui.Button(b, _tabs[i], label, true, i == _page, _tabs[i].Contains(mouse));
            if (i == _page)
                Ui.Outline(b, _tabs[i], new Color(150, 100, 0), 3);
        }

        var current = Portfolio.Pages[_page];
        int left = xPositionOnScreen + Pad + 8, y = yPositionOnScreen + Pad + TabH + 18;
        Ui.Text(b, Portfolio.PageName(current), new Vector2(left, y));
        string state = Portfolio.IsFinished(current) ? I18n.Get("portfolio.finished")
            : I18n.Get("portfolio.filled", new { filled = Filled(_page), total = current.Slots.Length });
        Ui.Text(b, state, new Vector2(xPositionOnScreen + width - Pad - 8 - Ui.TextWidth(state), y), Game1.textColor * 0.65f);
        Ui.Text(b, I18n.Get("portfolio.page." + current.Id + ".note"), new Vector2(left, y + 38), Game1.textColor * 0.7f, 0.85f);

        int first = Portfolio.FirstIndex(_page);
        for (int i = 0; i < _slots.Count; i++)
            DrawSlot(b, _slots[i], current.Slots[i], Portfolio.In(first + i), mouse);

        int rewardY = _slots[0].Bottom + 70;
        Ui.TextCentered(b, I18n.Get("portfolio.reward", new { reward = I18n.Get("portfolio.page." + current.Id + ".reward") }),
            xPositionOnScreen + width / 2, rewardY, Portfolio.IsFinished(current) ? new Color(120, 75, 0) : Game1.textColor * 0.75f, 0.85f);

        if (_inventory != null)
        {
            _inventory.draw(b);
            var hovered = _inventory.getItemAt(mouse.X, mouse.Y);
            if (hovered != null)
                _hoverText = Portfolio.IsKeepable(hovered) && Portfolio.SlotFor(_page, hovered) >= 0
                    ? I18n.Get("portfolio.click-to-keep", new { painting = hovered.DisplayName })
                    : hovered.DisplayName;
        }

        base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private int Filled(int page) =>
        Enumerable.Range(Portfolio.FirstIndex(page), Portfolio.Pages[page].Slots.Length).Count(i => Portfolio.In(i) != null);

    private void DrawSlot(SpriteBatch b, Rectangle rect, PortfolioSlot slot, Item? painting, Point mouse)
    {
        // A mount like a photo corner: dark paper, the painting inset.
        Ui.Fill(b, rect, new Color(60, 44, 36));
        var inner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
        var texture = painting != null ? PaintingRenderer.TextureFor(painting) : null;
        if (texture != null)
        {
            b.Draw(texture, inner, Color.White);
            var star = Ui.QualityStar(painting!.Quality);
            if (star.HasValue)
                b.Draw(Game1.mouseCursors, new Rectangle(rect.X + 4, rect.Bottom - 28, 24, 24), star.Value, Color.White);
        }
        else
        {
            Ui.Fill(b, inner, new Color(243, 236, 220) * 0.25f);
            bool lit = _inventory != null && _who.Items.Any(item => item != null && Portfolio.Fits(slot, item));
            Ui.Outline(b, inner, lit ? new Color(255, 222, 90) : new Color(243, 236, 220) * 0.35f, 2);
        }

        // Its label under the mount, in two lines: smaller if it needs more
        // (a translation can run long), never cut off.
        string name = I18n.Get("portfolio.slot." + slot.Id);
        float scale = 0.8f;
        string label = Game1.parseText(name, Game1.smallFont, (int)((SlotSize + SlotGap - 8) / scale));
        while (label.Split('\n').Length > 2 && scale > 0.55f)
        {
            scale -= 0.05f;
            label = Game1.parseText(name, Game1.smallFont, (int)((SlotSize + SlotGap - 8) / scale));
        }
        int line = 0;
        foreach (string text in label.Split('\n').Take(2))
            Ui.TextCentered(b, text, rect.Center.X, rect.Bottom + 10 + line++ * (int)(32 * scale), Game1.textColor * 0.8f, scale);

        if (!rect.Contains(mouse)) return;
        if (painting != null && PaintingData.Read(painting) is { } data)
        {
            string title = EaselMenu.SubjectTitle(data.Subject.Split('|'));
            _hoverText = title + "\n" + I18n.Get("portfolio.kept", new { quality = Ui.QualityName(painting.Quality), painter = data.Painter });
        }
        else
            _hoverText = I18n.Get("portfolio.slot." + slot.Id) + "\n" + I18n.Get("portfolio.slot." + slot.Id + ".hint");
    }
}
