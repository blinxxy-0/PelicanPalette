using System;
using System.Linq;
using System.Collections.Generic;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

internal enum EaselStage { Setup, Painting, Results }

// The easel: pick a canvas and subject (Setup), paint it against the light
// with the mixing bar and inspiration sparks (Painting), then see how it
// turned out (Results). Split across partial files by stage.
internal sealed partial class EaselMenu : IClickableMenu
{
    private const int MaxWidth = 1240, MaxHeight = 780;
    private const int Pad = 20, TopBarHeight = 64, BottomBarHeight = 56, SideWidth = 260;

    private readonly Random _random = new();
    private readonly Farmer _who;
    private readonly int _level;
    private readonly IReadOnlyList<int> _owned;
    private BrushSpec _brush;
    private Item? _brushItem;
    private readonly DifficultySettings _rules;

    // Paint (docs/painting_depth.md): the Palette in the bag, and how many
    // cells of each colour it holds. Relaxed paints for free.
    private readonly Item? _palette;
    private readonly int[] _paint = new int[Palette.All.Length];
    // (Only in a loaded game: the title-screen snapshots have no Palette.)
    private static bool UsesPaint => PaletteItem.Required && StardewModdingAPI.Context.IsWorldReady;
    private EaselStage _stage = EaselStage.Setup;

    private MouseState _mouse, _prevMouse;
    private KeyboardState _keys, _prevKeys;
    private GamePadState _pad, _prevPad;

    // A short message that floats above the canvas ("Pure mix!", "Brush is dry").
    private string? _toast;
    private Color _toastColor;
    private float _toastLeft;

    private string? _hoverText;

    // Plein air: where the easel stands, when it stands outdoors, and the
    // view from it once the painter has looked (taken on first pick).
    private readonly GameLocation? _viewLocation;
    private readonly Vector2 _easelTile;
    private Views.Capture? _view;

    public EaselMenu(Farmer who, BrushSpec? brush = null, Vector2? easelTile = null)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        if (easelTile.HasValue && who.currentLocation is { IsOutdoors: true } outdoors)
            (_viewLocation, _easelTile) = (outdoors, easelTile.Value);
        _level = ArtProgress.Level(who);
        _owned = ArtProgress.OwnedPigments(who);
        _brush = brush ?? Brushes.HogBristle;
        _rules = DifficultySettings.For(ModEntry.Config.Difficulty);
        if (UsesPaint && (_palette = PaletteItem.Find(who)) != null)
        {
            int tubes = PaletteItem.Refill(who, _palette).Values.Sum();
            _paint = PaletteItem.Read(_palette);
            if (tubes > 0)
                Toast(I18n.Get("palette.refilled", new { tubes }), new Color(60, 140, 60), 2.5f);
        }
        Reposition();
        EnterSetup();
    }

    // Skips Setup and starts painting straight away (debug command).
    public static EaselMenu StartStillLife(Farmer who, CanvasKind kind, IReadOnlyList<string> itemIds)
    {
        var menu = new EaselMenu(who);
        menu.BeginPainting(kind, itemIds);
        return menu;
    }

    public static EaselMenu StartOriginal(Farmer who, CanvasKind kind)
    {
        var menu = new EaselMenu(who);
        menu.BeginPainting(kind, Array.Empty<string>());
        return menu;
    }

    private bool Has(ArtProfession profession) => PaintingSkill.Instance != null && PaintingSkill.Has(_who, profession);

    // Light on top of the difficulty: the player's own setting and Plein Air.
    private float LightMultiplier =>
        _rules.LightMultiplier * ModEntry.Config.LightTimeMultiplier
        * (Has(PaintingSkill.Instance.PleinAir) ? PaintingSkill.PleinAirLight : 1f);

    private Point MousePos => new(Game1.getMouseX(true), Game1.getMouseY(true));
    private bool LeftDown => _mouse.LeftButton == ButtonState.Pressed;
    private bool LeftPressed => LeftDown && _prevMouse.LeftButton == ButtonState.Released;
    private bool LeftReleased => !LeftDown && _prevMouse.LeftButton == ButtonState.Pressed;
    private bool RightDown => _mouse.RightButton == ButtonState.Pressed;
    private bool RightPressed => RightDown && _prevMouse.RightButton == ButtonState.Released;
    private bool KeyPressed(Keys k) => _keys.IsKeyDown(k) && !_prevKeys.IsKeyDown(k);

    private void Reposition()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();
        LayoutSetup();
        LayoutPainting();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Reposition();

    // Leaving mid-painting goes through the "abandon?" prompt instead.
    public override bool readyToClose() => _stage != EaselStage.Painting;

    public override bool shouldDrawCloseButton() => _stage != EaselStage.Painting;

    public override void update(GameTime time)
    {
        _prevMouse = _mouse;
        _mouse = Game1.input.GetMouseState();
        _prevKeys = _keys;
        _keys = Game1.input.GetKeyboardState();
        _prevPad = _pad;
        _pad = Game1.input.GetGamePadState();

        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        if (_toastLeft > 0f) _toastLeft -= dt;

        switch (_stage)
        {
            case EaselStage.Setup: UpdateSetup(); break;
            case EaselStage.Painting: UpdatePainting(dt); break;
            case EaselStage.Results: UpdateResults(dt); break;
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        switch (_stage)
        {
            case EaselStage.Setup:
                base.receiveLeftClick(x, y, playSound);
                ClickSetup(x, y);
                break;
            case EaselStage.Painting:
                ClickPainting(x, y);
                break;
            case EaselStage.Results:
                base.receiveLeftClick(x, y, playSound);
                ClickResults(x, y);
                break;
        }
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        // Right-click is the palette knife while painting (handled in update).
    }

    public override void receiveKeyPress(Keys key)
    {
        switch (_stage)
        {
            case EaselStage.Setup:
                base.receiveKeyPress(key);
                break;
            case EaselStage.Painting:
                KeyPainting(key);
                break;
            case EaselStage.Results:
                if (Game1.options.doesInputListContain(Game1.options.menuButton, key) || key == Keys.Enter)
                    KeepAndClose();
                break;
        }
    }

    protected override void cleanupBeforeExit()
    {
        GiveResultIfPending();
        base.cleanupBeforeExit();
    }

    private void Toast(string text, Color color, float seconds = 1.6f)
    {
        _toast = text;
        _toastColor = color;
        _toastLeft = seconds;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        Ui.AuditMenu(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        _hoverText = null;

        switch (_stage)
        {
            case EaselStage.Setup: DrawSetup(b); break;
            case EaselStage.Painting: DrawPainting(b); break;
            case EaselStage.Results: DrawResults(b); break;
        }

        if (shouldDrawCloseButton())
            base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    // Draws a pigment grid into `area` (cells may be non-square-pixel-exact;
    // each cell gets floor-sized rects so there are no seams).
    private static void DrawCells(SpriteBatch b, byte[,] cells, Purity[,]? purity, Rectangle area, Color? blank, bool grime)
    {
        int n = cells.GetLength(0);
        for (int x = 0; x < n; x++)
        for (int y = 0; y < n; y++)
        {
            var r = CellRect(area, n, x, y);
            byte c = cells[x, y];
            if (c == PixelCanvas.Blank)
            {
                if (blank.HasValue) Ui.Fill(b, r, blank.Value);
                continue;
            }
            var p = purity?[x, y] ?? Purity.Pure;
            Ui.Fill(b, r, PaintingRenderer.CellColor(c, p));
            if (grime && p == Purity.Muddy && r.Width >= 6)
            {
                int d = Math.Max(2, r.Width / 4);
                Ui.Fill(b, new Rectangle(r.X + d, r.Y + d, d, d), new Color(70, 58, 46) * 0.7f);
                Ui.Fill(b, new Rectangle(r.Right - 2 * d, r.Bottom - 2 * d, d, d), new Color(70, 58, 46) * 0.5f);
            }
        }
    }

    private static Rectangle CellRect(Rectangle area, int n, int x, int y)
    {
        int x0 = area.X + area.Width * x / n, x1 = area.X + area.Width * (x + 1) / n;
        int y0 = area.Y + area.Height * y / n, y1 = area.Y + area.Height * (y + 1) / n;
        return new Rectangle(x0, y0, x1 - x0, y1 - y0);
    }

    private static bool IsViewSubject(IReadOnlyList<string> ids) => ids.Count == 1 && Views.IsView(ids[0]);

    // Portraits and views fill the canvas: they take an hour longer.
    private static bool IsLongSubject(IReadOnlyList<string> ids) => IsSitterSubject(ids) || IsViewSubject(ids);

    private Reference? BuildReference(IReadOnlyList<string> ids, int size) =>
        ids.Count == 0 ? null
        : IsSitterSubject(ids) ? Sitters.Portrait(Sitters.NpcOf(ids[0]), size, _owned)
        : IsViewSubject(ids) ? (_view != null ? Views.Reference(_view, size, _owned) : null)
        : ReferenceBuilder.StillLife(ids, size, _owned);

    // The subject as a title: "Still Life: Parsnip", "Portrait of Leah".
    internal static string SubjectTitle(IReadOnlyList<string> ids) =>
        ids.Count == 0 ? I18n.Get("paint.title.free")
        : IsSitterSubject(ids) ? I18n.Get("painting.title.portrait", new { name = Sitters.DisplayName(Sitters.NpcOf(ids[0])) })
        : IsViewSubject(ids) ? I18n.Get("painting.title.view", new { place = Views.PlaceName(ids[0]), season = Views.SeasonName(ids[0]) })
        : ids.Count == 1 ? I18n.Get("paint.title.still-life-of", new { item = ItemRegistry.GetDataOrErrorItem(ids[0]).DisplayName })
        : I18n.Get("paint.title.still-life");

    private static string SubjectPhrase(IReadOnlyList<string> itemIds)
    {
        if (itemIds.Count == 0) return I18n.Get("subject.anything");
        if (IsSitterSubject(itemIds)) return Sitters.DisplayName(Sitters.NpcOf(itemIds[0]));
        if (IsViewSubject(itemIds)) return I18n.Get("subject.view");
        if (itemIds.Count > 1) return I18n.Get("subject.still-life");
        string name = ItemRegistry.GetDataOrErrorItem(itemIds[0]).DisplayName.ToLowerInvariant();
        // (English needs "a" or "an"; a translation can leave {{article}} out.)
        return I18n.Get("subject.item", new { item = name, article = "aeiou".IndexOf(name[0]) >= 0 ? "an" : "a" });
    }
}
