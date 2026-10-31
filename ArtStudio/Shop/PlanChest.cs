using ArtStudio.Painting;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ArtStudio.Shop;

// The plan chest in the art shop's back room (1.2): wide, flat drawers to
// keep paintings in, framed or not, for the whole farm. It's the game's own
// chest menu (shift-click and all) over a global inventory, holding a big
// chest's worth and taking nothing but paintings.
internal sealed class PlanChest : Chest
{
    public const string InventoryId = ModEntry.Id + "_PlanChest";

    private PlanChest()
        : base(true)
    {
        GlobalInventoryId = InventoryId;
        SpecialChestType = SpecialChestTypes.BigChest;
    }

    // Opens it, once nobody else is looking through it.
    public static void Open(Farmer who)
    {
        var chest = new PlanChest();
        chest.GetMutex().RequestLock(
            () =>
            {
                Game1.playSound("openChest");
                chest.ShowMenu();
            },
            () => Game1.drawObjectDialogue(I18n.Get("plan-chest.busy")));
    }

    // Kept while the menu is open; let go when it closes (as a placed chest
    // does in its own update, which this one, never placed, doesn't get).
    public static void ReleaseIfClosed()
    {
        var mutex = Game1.player.team.GetOrCreateGlobalInventoryMutex(InventoryId);
        if (mutex.IsLockHeld() && Game1.activeClickableMenu is not ItemGrabMenu { context: PlanChest })
            mutex.ReleaseLock();
    }

    public override void ShowMenu()
    {
        var menu = new ItemGrabMenu(GetItemsForPlayer(), reverseGrab: false, showReceivingMenu: true, Portfolio.IsKeepable,
            grabItemFromInventory, null, grabItemFromChest, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true,
            allowRightClick: true, showOrganizeButton: true, ItemGrabMenu.source_chest, this, -1, this);
        // A drawer, not a painted chest: no colour to choose.
        menu.chestColorPicker = null;
        menu.colorPickerToggleButton = null;
        Game1.activeClickableMenu = menu;
    }
}
