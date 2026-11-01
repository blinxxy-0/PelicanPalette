using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

// The painter's record (asked for at Sable's counter). The overview is every
// subject painted, as a wall of little tiles with how often and how well,
// and the milestones for painting many different things. The other tabs
// (1.2) sort it the way the game's Collections tab does: crops, forage,
// fish, minerals, cooking, portraits and views, with what's still to paint
// as silhouettes and rewards for painting a share of the tab.
internal sealed class CollectionMenu : IClickableMenu
{
    private const int MaxWidth = 960, MaxHeight = 660, Pad = 36, Tile = 72, Gap = 10, TabSize = 64, TabGap = 8, PanelHeight = 128;

    private static readonly RecordTab[] Tabs = Enum.GetValues<RecordTab>();

    private readonly Farmer _who;
    private RecordTab _tab;
    private List<TabMember> _members;
    private readonly List<(Rectangle Rect, TabMember Member)> _tiles = new();
    private readonly Dictionary<RecordTab, Rectangle> _tabRects = new();
    private Rectangle _grid, _prev, _next, _panel;
    private int _page, _perPage = 1;
    private string? _hoverText;

    // Your own paintings of subjects, where one's kept (the portfolio or the
    // plan chest), shown in place of the subject's icon.
    private readonly Dictionary<string, Item> _kept;

    public CollectionMenu(Farmer who, RecordTab tab = RecordTab.Overview)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        _kept = RecordTabs.KeptPaintings();
        _tab = tab;
        _members = RecordTabs.Members(who, tab);
        Layout();
    }

    private int Pages => Math.Max(1, (_members.Count + _perPage - 1) / _perPage);

    // The overview and the completable tabs have a panel of rewards at the
    // bottom; "everything else" has none.
    private bool HasPanel => _tab != RecordTab.Other;

    private void Layout()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();

        _tabRects.Clear();
        for (int i = 0; i < Tabs.Length; i++)
            _tabRects[Tabs[i]] = new Rectangle(xPositionOnScreen + Pad + 8 + i * (TabSize + TabGap), yPositionOnScreen + Pad, TabSize, TabSize);

        int bottom = yPositionOnScreen + height - Pad;
        _panel = HasPanel ? new Rectangle(xPositionOnScreen + Pad, bottom - PanelHeight, width - 2 * Pad, PanelHeight) : new Rectangle(xPositionOnScreen + Pad, bottom, width - 2 * Pad, 0);
        int gridTop = yPositionOnScreen + Pad + TabSize + 94;
        _grid = new Rectangle(xPositionOnScreen + Pad + 8, gridTop, width - 2 * Pad - 16, _panel.Y - 56 - gridTop);
        int cols = Math.Max(1, (_grid.Width + Gap) / (Tile + Gap));
        int rows = Math.Max(1, (_grid.Height + Gap) / (Tile + Gap));
        _perPage = cols * rows;
        _page = Math.Clamp(_page, 0, Pages - 1);
        _prev = new Rectangle(_grid.X, _panel.Y - 50, 48, 44);
        _next = new Rectangle(_grid.Right - 48, _panel.Y - 50, 48, 44);

        _tiles.Clear();
        int n = 0;
        foreach (var member in _members.Skip(_page * _perPage).Take(_perPage))
        {
            var rect = new Rectangle(_grid.X + n % cols * (Tile + Gap), _grid.Y + n / cols * (Tile + Gap), Tile, Tile);
            _tiles.Add((rect, member));
            n++;
        }
    }

    private void ShowTab(RecordTab tab)
    {
        if (tab == _tab) return;
        _tab = tab;
        _members = RecordTabs.Members(_who, tab);
        _page = 0;
        Game1.playSound("smallSelect");
        Layout();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        foreach (var (tab, rect) in _tabRects)
            if (rect.Contains(x, y))
            {
                ShowTab(tab);
                return;
            }
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

        foreach (var (tab, rect) in _tabRects)
            DrawTab(b, tab, rect, mouse);

        int left = xPositionOnScreen + Pad + 8, titleY = yPositionOnScreen + Pad + TabSize + 16;
        if (_tab == RecordTab.Overview)
        {
            Ui.Text(b, I18n.Get("record.title"), new Vector2(left, titleY));
            int painted = _members.Sum(m => m.Painted!.Count);
            DrawRight(b, I18n.Get("record.tally", new { subjects = _members.Count, paintings = painted }), titleY);
            Ui.Text(b, I18n.Get("record.note"), new Vector2(left, titleY + 38), Game1.textColor * 0.7f, 0.85f);
        }
        else
        {
            Ui.Text(b, RecordTabs.TabName(_tab), new Vector2(left, titleY));
            int done = _members.Count(m => m.Painted != null);
            DrawRight(b, _tab == RecordTab.Other ? I18n.Get("record.tab.other-count", new { count = done })
                : I18n.Get("record.tab.progress", new { done, total = _members.Count }), titleY);
            Ui.Text(b, I18n.Get(_tab == RecordTab.Other ? "record.tab.other-note" : "record.tab.note"), new Vector2(left, titleY + 38), Game1.textColor * 0.7f, 0.85f);
        }

        if (_members.Count == 0)
            Ui.Text(b, I18n.Get(_tab == RecordTab.Overview ? "record.empty" : "record.tab.empty"), new Vector2(left, _grid.Y + 8), Game1.textColor * 0.6f);
        foreach (var (rect, member) in _tiles)
            DrawTile(b, rect, member, mouse);

        if (Pages > 1)
        {
            Ui.Button(b, _prev, "<", _page > 0, false, _page > 0 && _prev.Contains(mouse));
            Ui.Button(b, _next, ">", _page < Pages - 1, false, _page < Pages - 1 && _next.Contains(mouse));
            Ui.TextCentered(b, I18n.Get("record.page", new { page = _page + 1, pages = Pages }), _grid.Center.X, _prev.Y + 10, Game1.textColor * 0.7f);
        }

        if (_tab == RecordTab.Overview)
            DrawMilestones(b, mouse);
        else if (HasPanel)
            DrawTabRewards(b, mouse);

        base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private void DrawRight(SpriteBatch b, string text, int y) =>
        Ui.Text(b, text, new Vector2(xPositionOnScreen + width - Pad - 8 - Ui.TextWidth(text), y), Game1.textColor * 0.65f);

    // ---- Tabs ----

    private void DrawTab(SpriteBatch b, RecordTab tab, Rectangle rect, Point mouse)
    {
        b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
        if (tab == _tab)
            Ui.Outline(b, rect, new Color(150, 100, 0), 3);
        else if (rect.Contains(mouse))
            Ui.Fill(b, rect, Color.White * 0.25f);
        var inner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
        switch (tab)
        {
            case RecordTab.Portraits:
                var portrait = Sitters.PortraitTexture("Sable");
                if (portrait != null)
                    b.Draw(portrait, inner, new Rectangle(0, 0, 64, 64), Color.White);
                break;
            case RecordTab.Views:
                Ui.Landscape(b, inner);
                break;
            case RecordTab.Creatures:
                Creatures.Draw(b, "Green Slime", inner);
                break;
            case RecordTab.Animals:
                Animals.Draw(b, "White Chicken", inner);
                break;
            default:
                ItemRegistry.Create(TabIcon(tab)).drawInMenu(b, new Vector2(rect.X, rect.Y), 0.85f, 1f, 0.9f, StackDrawType.Hide, Color.White, false);
                break;
        }
        if (rect.Contains(mouse))
        {
            string name = tab == RecordTab.Overview ? I18n.Get("record.title") : RecordTabs.TabName(tab);
            if (RecordTabs.Completable.Contains(tab))
            {
                var (done, total) = RecordTabs.Progress(_who, tab);
                name += "\n" + I18n.Get("record.tab.progress", new { done, total });
            }
            _hoverText = name;
        }
    }

    private static string TabIcon(RecordTab tab) => tab switch
    {
        RecordTab.Overview => "(O)" + ArtItems.PaletteId,
        RecordTab.Crops => "(O)24",       // parsnip
        RecordTab.Forage => "(O)16",      // wild horseradish
        RecordTab.Fish => "(O)145",       // sunfish
        RecordTab.Minerals => "(O)66",    // amethyst
        RecordTab.Cooking => "(O)194",    // fried egg
        _ => "(O)428",                    // cloth
    };

    // ---- Tiles ----

    private void DrawTile(SpriteBatch b, Rectangle rect, TabMember member, Point mouse)
    {
        b.Draw(Game1.menuTexture, rect, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White);
        var inner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
        bool painted = member.Painted != null;
        var tint = painted ? Color.White : Color.Black * 0.35f;
        string key = member.Key;
        string[] ids = key.Split('|');

        if (painted && _kept.TryGetValue(key, out var kept) && PaintingRenderer.TextureFor(kept) is { } picture)
            b.Draw(picture, inner, Color.White);
        else if (Studies.IsStudy(key))
            Studies.Draw(b, key, inner, painted ? null : Color.Black * 0.35f);
        else if (Sitters.IsSitter(key))
        {
            var texture = Sitters.PortraitTexture(Sitters.NpcOf(key));
            if (texture != null)
                b.Draw(texture, inner, new Rectangle(0, 0, 64, 64), tint);
        }
        else if (Views.IsView(key))
        {
            Ui.Landscape(b, inner);
            if (!painted)
                Ui.Fill(b, inner, Color.Black * 0.6f);
            else if (_tab == RecordTab.Views)
                DrawSeasons(b, inner, member.Seasons);
        }
        else
        {
            // The first item large, and a second one tucked in the corner.
            ItemRegistry.Create(ids[0]).drawInMenu(b, new Vector2(rect.X + 4, rect.Y + 2), 0.9f, 1f, 0.9f, StackDrawType.Hide, tint, false);
            if (ids.Length > 1)
                ItemRegistry.Create(ids[1]).drawInMenu(b, new Vector2(rect.X + 30, rect.Y + 30), 0.5f, 1f, 0.9f, StackDrawType.Hide, tint, false);
        }

        if (member.Painted is { } entry)
        {
            var star = Ui.QualityStar(entry.BestQuality);
            if (star.HasValue)
                b.Draw(Game1.mouseCursors, new Rectangle(rect.X + 6, rect.Bottom - 26, 20, 20), star.Value, Color.White);
            if (entry.Count > 1)
                Utility.drawTinyDigits(entry.Count, b, new Vector2(rect.Right - 8 - Utility.getWidthOfTinyDigitString(entry.Count, 3f), rect.Bottom - 26), 3f, 1f, Color.White);
        }

        if (rect.Contains(mouse))
            _hoverText = HoverFor(member);
    }

    // A view's seasons: four small squares along the top, filled for each
    // season it's been painted in.
    private static void DrawSeasons(SpriteBatch b, Rectangle inner, IReadOnlyList<string> seasons)
    {
        string[] all = { "spring", "summer", "fall", "winter" };
        Color[] colors = { new(110, 190, 80), new(240, 200, 60), new(220, 120, 50), new(170, 210, 240) };
        for (int i = 0; i < all.Length; i++)
        {
            var pip = new Rectangle(inner.X + 2 + i * 12, inner.Y + 2, 10, 10);
            Ui.Fill(b, pip, Color.Black * 0.5f);
            if (seasons.Contains(all[i]))
                Ui.Fill(b, new Rectangle(pip.X + 2, pip.Y + 2, 6, 6), colors[i]);
        }
    }

    private string HoverFor(TabMember member)
    {
        string key = member.Key;
        bool view = Views.IsView(key), sitter = Sitters.IsSitter(key), creature = Studies.IsStudy(key);
        string name = _tab == RecordTab.Overview ? EaselMenu.SubjectTitle(key.Split('|'))
            : view ? Views.PlaceName(key)
            : sitter ? Sitters.DisplayName(Sitters.NpcOf(key))
            : creature ? Studies.DisplayName(key)
            : ItemRegistry.GetDataOrErrorItem(key).DisplayName;

        if (member.Painted is not { } entry)
        {
            if (!RecordTabs.Known(_who, _tab, key))
                name = I18n.Get("record.unknown");
            string hint = view && Social.FestivalPainting.IsFestivalPlace(Views.PlaceOf(key)) ? "record.unpainted.festival-view"
                : view ? "record.unpainted.view"
                : key == Sitters.SubjectFor("Krobus") ? "record.unpainted.krobus"
                : key == Sitters.SubjectFor("Wizard") ? "record.unpainted.wizard"
                : sitter ? "record.unpainted.sitter" : Animals.IsAnimal(key) ? "record.unpainted.animal"
                : creature ? (key == Creatures.SubjectFor(Creatures.Junimo) ? "record.unpainted.junimo" : "record.unpainted.creature") : "record.unpainted.item";
            return name + "\n" + I18n.Get(hint, new { hearts = Core.PaintingRules.PortraitHearts });
        }
        string text = name + "\n" + I18n.Get("record.hover", new { count = entry.Count, quality = Ui.QualityName(entry.BestQuality), date = WorldDate.ForDaysPlayed(entry.FirstDay).Localize() });
        if (view && _tab == RecordTab.Views && member.Seasons.Count > 0)
            text += "\n" + I18n.Get("record.view.seasons", new { seasons = string.Join(", ", member.Seasons.Select(SeasonName)) });
        return text;
    }

    private static string SeasonName(string key) =>
        Enum.TryParse(key, true, out Season season) ? Utility.getSeasonNameFromNumber((int)season) : key;

    // ---- Panels ----

    // The four milestones, reached ones in gold, the next with its progress.
    private void DrawMilestones(SpriteBatch b, Point mouse)
    {
        Ui.Paper(b, _panel);
        int count = _members.Count;
        var next = Catalogue.Milestones.FirstOrDefault(m => count < m.Subjects);
        string head = next.Perk == null ? I18n.Get("record.milestones.all") : I18n.Get("record.milestones.next", new { left = next.Subjects - count });
        Ui.Text(b, head, new Vector2(_panel.X + 24, _panel.Y + 22));

        int n = Catalogue.Milestones.Length;
        for (int i = 0; i < n; i++)
        {
            var (subjects, perk) = Catalogue.Milestones[i];
            DrawRewardSlot(b, i, n, Catalogue.HasPerk(_who, perk),
                I18n.Get("record.milestone", new { count = subjects, reward = I18n.Get("record.reward." + perk) }),
                I18n.Get("record.reward-hover." + perk), mouse);
        }
    }

    // A tab's rewards at a quarter, a half and all of it painted.
    private void DrawTabRewards(SpriteBatch b, Point mouse)
    {
        Ui.Paper(b, _panel);
        int done = _members.Count(m => m.Painted != null), total = Math.Max(1, _members.Count);
        Ui.Text(b, I18n.Get("record.tab.rewards", new { percent = done * 100 / total }), new Vector2(_panel.X + 24, _panel.Y + 22));

        string tab = RecordTabs.TabName(_tab);
        string pigment = I18n.Pigment(Core.Palette.IndexOf(RecordTabs.TabPigment(_tab)));
        int n = RecordTabs.RewardPercents.Length;
        for (int i = 0; i < n; i++)
        {
            int percent = RecordTabs.RewardPercents[i];
            DrawRewardSlot(b, i, n, RecordTabs.HasReward(_who, _tab, percent),
                I18n.Get("record.tab.reward." + percent, new { percent, pigment }),
                I18n.Get("record.tab.reward-hover." + percent, new { tab, pigment, tubes = RecordTabs.RewardTubes, frames = RecordTabs.RewardFrames }), mouse);
        }
    }

    private void DrawRewardSlot(SpriteBatch b, int i, int n, bool reached, string label, string hover, Point mouse)
    {
        int slotW = (_panel.Width - 40) / n;
        var slot = new Rectangle(_panel.X + 20 + i * slotW, _panel.Y + 62, slotW - 12, 44);
        Ui.Fill(b, slot, reached ? new Color(214, 160, 30) * 0.35f : Color.Black * 0.06f);
        Ui.Outline(b, slot, reached ? new Color(150, 100, 0) : Game1.textColor * 0.25f, 2);
        Ui.Text(b, label, new Vector2(slot.X + 10, slot.Y + 10), reached ? new Color(120, 75, 0) : Game1.textColor * 0.75f, 0.8f);
        if (slot.Contains(mouse))
            _hoverText = hover + (reached ? "\n" + I18n.Get("record.reached") : "");
    }
}
