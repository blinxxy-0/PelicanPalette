using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Menus;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Shop;

// Leaving paintings with Sable: the paintings in your bag on the left (click
// one to put it on an easel), the three easels on the right, each with its
// painting, what it should fetch and when, and a button to take it back.
internal sealed class ConsignMenu : IClickableMenu
{
    private const int MaxWidth = 960, MaxHeight = 660, Pad = 36, Slot = 64, SlotGap = 8;
    private const float NoteScale = 0.85f;

    private readonly Farmer _who;
    private readonly List<(Rectangle Rect, Item Item)> _bag = new();
    private readonly List<(Rectangle Card, Rectangle TakeBack, Item? Item)> _easels = new();
    private Rectangle _bagArea;
    private string _note = "";
    private string? _hoverText;

    public ConsignMenu(Farmer who)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();

        // Under the title and the note (as many lines as it wraps to).
        _note = Game1.parseText(I18n.Get("consign.note", new { max = Consignment.MaxConsigned }), Game1.smallFont, (int)((width - 2 * Pad - 16) / NoteScale));
        int top = yPositionOnScreen + Pad + 44 + (int)(Game1.smallFont.MeasureString(_note).Y * NoteScale) + 20;
        int cardW = Math.Min(440, width / 2 - Pad);
        int cardH = Math.Min(150, (yPositionOnScreen + height - Pad - top - 2 * SlotGap) / Consignment.MaxConsigned);
        int cardX = xPositionOnScreen + width - Pad - cardW;
        _bagArea = new Rectangle(xPositionOnScreen + Pad + 8, top + 40, cardX - 32 - (xPositionOnScreen + Pad + 8), yPositionOnScreen + height - Pad - top - 40);

        _bag.Clear();
        int cols = Math.Max(1, (_bagArea.Width + SlotGap) / (Slot + SlotGap));
        int n = 0;
        foreach (var item in _who.Items)
        {
            if (item == null || !PaintingItems.IsPainting(item)) continue;
            var rect = new Rectangle(_bagArea.X + n % cols * (Slot + SlotGap), _bagArea.Y + n / cols * (Slot + SlotGap), Slot, Slot);
            if (rect.Bottom > _bagArea.Bottom) break;
            _bag.Add((rect, item));
            n++;
        }

        _easels.Clear();
        var consigned = Consignment.All(_who).ToList();
        for (int i = 0; i < Consignment.MaxConsigned; i++)
        {
            var card = new Rectangle(cardX, top + i * (cardH + SlotGap), cardW, cardH);
            var takeBack = new Rectangle(card.Right - 156, card.Bottom - 60, 140, 48);
            _easels.Add((card, takeBack, i < consigned.Count ? consigned[i] : null));
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        foreach (var (rect, item) in _bag)
        {
            if (!rect.Contains(x, y)) continue;
            if (Consignment.Consign(_who, item))
            {
                Game1.playSound("Ship");
                Layout();
            }
            else
                Game1.playSound("cancel");
            return;
        }
        foreach (var (_, takeBack, item) in _easels)
        {
            if (item == null || !takeBack.Contains(x, y)) continue;
            if (Consignment.TakeBack(_who, item))
            {
                Game1.playSound("pickUpItem");
                Layout();
            }
            else
            {
                Game1.playSound("cancel");
                Game1.showRedMessage(I18n.Get("consign.bag-full"));
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
        Ui.Text(b, I18n.Get("consign.title"), new Vector2(left, yPositionOnScreen + Pad + 6));
        Ui.Text(b, _note, new Vector2(left, yPositionOnScreen + Pad + 44), Game1.textColor * 0.7f, NoteScale);

        bool room = Consignment.HasRoom(_who);
        Ui.Text(b, I18n.Get("consign.from-bag"), new Vector2(left, _bagArea.Y - 40));
        if (_bag.Count == 0)
            Ui.Text(b, I18n.Get("consign.no-paintings"), new Vector2(left, _bagArea.Y + 8), Game1.textColor * 0.6f);
        foreach (var (rect, item) in _bag)
        {
            b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), room ? Color.White : Color.Gray);
            item.drawInMenu(b, new Vector2(rect.X, rect.Y), 1f, room ? 1f : 0.5f, 0.9f, StackDrawType.Hide, Color.White, false);
            if (rect.Contains(mouse))
                _hoverText = item.DisplayName + "\n" + (room
                    ? I18n.Get("consign.hover", new { price = Consignment.Price(item) })
                    : I18n.Get("consign.full", new { max = Consignment.MaxConsigned }));
        }

        foreach (var (card, takeBack, item) in _easels)
            DrawEasel(b, card, takeBack, item, mouse);

        base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private void DrawEasel(SpriteBatch b, Rectangle card, Rectangle takeBack, Item? item, Point mouse)
    {
        Ui.Paper(b, card);
        int art = Math.Min(card.Height - 32, 96);
        var artArea = new Rectangle(card.X + 16, card.Y + (card.Height - art) / 2, art, art);
        if (item == null)
        {
            Ui.Fill(b, artArea, Color.Black * 0.06f);
            Ui.Text(b, I18n.Get("consign.empty"), new Vector2(artArea.Right + 18, card.Y + 20), Game1.textColor * 0.55f);
            return;
        }
        PaintingRenderer.DrawArt(b, item, artArea, Color.White, 0.9f);
        int x = artArea.Right + 18;
        Ui.Text(b, item.DisplayName, new Vector2(x, card.Y + 16), null, 0.9f);
        int days = Consignment.DaysLeft(item);
        Ui.Text(b, I18n.Get(days <= 1 ? "consign.sells.tomorrow" : "consign.sells.days", new { price = Consignment.Price(item), days }),
            new Vector2(x, card.Y + 46), new Color(120, 75, 0), 0.85f);
        Ui.Button(b, takeBack, I18n.Get("consign.take-back"), true, false, takeBack.Contains(mouse));
    }
}
