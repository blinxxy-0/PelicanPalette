using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

// Picking one painting from your bag for someone (1.2: Julian's contest at
// the Fair, Sable's stall at the Night Market): the bag, with the paintings
// that can be picked lit, and a word about each under the mouse.
internal class PaintingPickerMenu : IClickableMenu
{
    private readonly string _title;
    private readonly Func<Item, bool> _canPick;
    private readonly Action<Item> _onPick;
    private readonly Func<Item, string> _hover;
    private readonly InventoryMenu _inventory;

    public PaintingPickerMenu(string title, Func<Item, bool> canPick, Action<Item> onPick, Func<Item, string> hover)
        : base(Game1.uiViewport.Width / 2 - 432, Game1.uiViewport.Height / 2 - 186, 864, 372, showUpperRightCloseButton: true)
    {
        _title = title;
        _canPick = canPick;
        _onPick = onPick;
        _hover = hover;
        _inventory = new InventoryMenu(xPositionOnScreen + 48, yPositionOnScreen + 120, playerInventory: true, highlightMethod: item => _canPick(item));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (_inventory.getItemAt(x, y) is not { } item || !_canPick(item)) return;
        exitThisMenu();
        _onPick(item);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        Ui.Text(b, _title, new Vector2(xPositionOnScreen + 48, yPositionOnScreen + 48));
        _inventory.draw(b);
        base.draw(b);
        if (_inventory.getItemAt(Game1.getMouseX(true), Game1.getMouseY(true)) is { } hovered)
            drawHoverText(b, _canPick(hovered) ? _hover(hovered) : hovered.DisplayName, Game1.smallFont);
        drawMouse(b);
    }
}
