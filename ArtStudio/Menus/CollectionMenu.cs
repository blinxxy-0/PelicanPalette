using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

// The painter's record (asked for at Sable's counter): every subject
// painted, as a wall of little tiles with how often and how well, and the
// milestones for painting many different things, with what each brings.
internal sealed class CollectionMenu : IClickableMenu
{
    private const int MaxWidth = 960, MaxHeight = 660, Pad = 36, Tile = 72, Gap = 10;

    private readonly Farmer _who;
    private readonly List<(string Subject, CatalogueEntry Entry)> _entries;
    private readonly List<(Rectangle Rect, string Subject, CatalogueEntry Entry)> _tiles = new();
    private Rectangle _grid, _prev, _next, _milestones;
    private int _page, _perPage = 1;
    private string? _hoverText;

    public CollectionMenu(Farmer who)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        _entries = Catalogue.All(who)
            .OrderBy(kv => kv.Value.FirstDay)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
        Layout();
    }

    private int Pages => Math.Max(1, (_entries.Count + _perPage - 1) / _perPage);

    private void Layout()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();

        _milestones = new Rectangle(xPositionOnScreen + Pad, yPositionOnScreen + height - Pad - 128, width - 2 * Pad, 128);
        _grid = new Rectangle(xPositionOnScreen + Pad + 8, yPositionOnScreen + Pad + 92, width - 2 * Pad - 16, _milestones.Y - 56 - (yPositionOnScreen + Pad + 92));
        int cols = Math.Max(1, (_grid.Width + Gap) / (Tile + Gap));
        int rows = Math.Max(1, (_grid.Height + Gap) / (Tile + Gap));
        _perPage = cols * rows;
        _page = Math.Clamp(_page, 0, Pages - 1);
        _prev = new Rectangle(_grid.X, _milestones.Y - 50, 48, 44);
        _next = new Rectangle(_grid.Right - 48, _milestones.Y - 50, 48, 44);

        _tiles.Clear();
        int i = 0;
        foreach (var (subject, entry) in _entries.Skip(_page * _perPage).Take(_perPage))
        {
            var rect = new Rectangle(_grid.X + i % cols * (Tile + Gap), _grid.Y + i / cols * (Tile + Gap), Tile, Tile);
            _tiles.Add((rect, subject, entry));
            i++;
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (Pages <= 1) return;
        if (_prev.Contains(x, y) && _page > 0) { _page--; Game1.playSound("shwip"); Layout(); }
        else if (_next.Contains(x, y) && _page < Pages - 1) { _page++; Game1.playSound("shwip"); Layout(); }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int page = Math.Clamp(_page + (direction > 0 ? -1 : 1), 0, Pages - 1);
        if (page == _page) return;
        _page = page;
        Game1.playSound("shwip");
        Layout();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        Ui.AuditMenu(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        _hoverText = null;
        var mouse = new Point(Game1.getMouseX(true), Game1.getMouseY(true));

        int left = xPositionOnScreen + Pad + 8;
        Ui.Text(b, I18n.Get("record.title"), new Vector2(left, yPositionOnScreen + Pad + 6));
        int painted = _entries.Sum(e => e.Entry.Count);
        string tally = I18n.Get("record.tally", new { subjects = _entries.Count, paintings = painted });
        Ui.Text(b, tally, new Vector2(xPositionOnScreen + width - Pad - 8 - Ui.TextWidth(tally), yPositionOnScreen + Pad + 6), Game1.textColor * 0.65f);
        Ui.Text(b, I18n.Get("record.note"), new Vector2(left, yPositionOnScreen + Pad + 44), Game1.textColor * 0.7f, 0.85f);

        if (_entries.Count == 0)
            Ui.Text(b, I18n.Get("record.empty"), new Vector2(left, _grid.Y + 8), Game1.textColor * 0.6f);
        foreach (var (rect, subject, entry) in _tiles)
            DrawTile(b, rect, subject, entry, mouse);

        if (Pages > 1)
        {
            Ui.Button(b, _prev, "<", _page > 0, false, _page > 0 && _prev.Contains(mouse));
            Ui.Button(b, _next, ">", _page < Pages - 1, false, _page < Pages - 1 && _next.Contains(mouse));
            Ui.TextCentered(b, I18n.Get("record.page", new { page = _page + 1, pages = Pages }), _grid.Center.X, _prev.Y + 10, Game1.textColor * 0.7f);
        }

        DrawMilestones(b, mouse);

        base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private void DrawTile(SpriteBatch b, Rectangle rect, string subject, CatalogueEntry entry, Point mouse)
    {
        b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
        var inner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
        string[] ids = subject.Split('|');
        if (Sitters.IsSitter(subject))
        {
            var texture = Sitters.PortraitTexture(Sitters.NpcOf(subject));
            if (texture != null)
                b.Draw(texture, inner, new Rectangle(0, 0, 64, 64), Color.White);
        }
        else if (Views.IsView(subject))
            Ui.Landscape(b, inner);
        else
        {
            // The first item large, and a second one tucked in the corner.
            ItemRegistry.Create(ids[0]).drawInMenu(b, new Vector2(rect.X + 4, rect.Y + 2), 0.9f, 1f, 0.9f, StackDrawType.Hide, Color.White, false);
            if (ids.Length > 1)
                ItemRegistry.Create(ids[1]).drawInMenu(b, new Vector2(rect.X + 30, rect.Y + 30), 0.5f, 1f, 0.9f, StackDrawType.Hide, Color.White, false);
        }

        var star = Ui.QualityStar(entry.BestQuality);
        if (star.HasValue)
            b.Draw(Game1.mouseCursors, new Rectangle(rect.X + 6, rect.Bottom - 26, 20, 20), star.Value, Color.White);
        if (entry.Count > 1)
            Utility.drawTinyDigits(entry.Count, b, new Vector2(rect.Right - 8 - Utility.getWidthOfTinyDigitString(entry.Count, 3f), rect.Bottom - 26), 3f, 1f, Color.White);

        if (rect.Contains(mouse))
            _hoverText = EaselMenu.SubjectTitle(ids) + "\n"
                + I18n.Get("record.hover", new { count = entry.Count, quality = Ui.QualityName(entry.BestQuality), date = WorldDate.ForDaysPlayed(entry.FirstDay).Localize() });
    }

    // The four milestones, reached ones in gold, the next with its progress.
    private void DrawMilestones(SpriteBatch b, Point mouse)
    {
        Ui.Paper(b, _milestones);
        int count = _entries.Count;
        var next = Catalogue.Milestones.FirstOrDefault(m => count < m.Subjects);
        string head = next.Perk == null ? I18n.Get("record.milestones.all") : I18n.Get("record.milestones.next", new { left = next.Subjects - count });
        Ui.Text(b, head, new Vector2(_milestones.X + 24, _milestones.Y + 22));

        int n = Catalogue.Milestones.Length;
        int slotW = (_milestones.Width - 40) / n;
        for (int i = 0; i < n; i++)
        {
            var (subjects, perk) = Catalogue.Milestones[i];
            bool reached = Catalogue.HasPerk(_who, perk);
            var slot = new Rectangle(_milestones.X + 20 + i * slotW, _milestones.Y + 62, slotW - 12, 44);
            Ui.Fill(b, slot, reached ? new Color(214, 160, 30) * 0.35f : Color.Black * 0.06f);
            Ui.Outline(b, slot, reached ? new Color(150, 100, 0) : Game1.textColor * 0.25f, 2);
            Ui.Text(b, I18n.Get("record.milestone", new { count = subjects, reward = I18n.Get("record.reward." + perk) }),
                new Vector2(slot.X + 10, slot.Y + 10), reached ? new Color(120, 75, 0) : Game1.textColor * 0.75f, 0.8f);
            if (slot.Contains(mouse))
                _hoverText = I18n.Get("record.reward-hover." + perk) + (reached ? "\n" + I18n.Get("record.reached") : "");
        }
    }
}
