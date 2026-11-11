#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ArtStudio.Vale;

// The Vale residents' own behaviour (1.4, Debug only like the dev pack): their shop
// counters, Bea's room for the night, and noticing a Lupini painting bought at the
// Night Market (for his letter opening the Vale). The shops' stock, the counters' tiles
// and every text are in the dev pack (data/vale_shops.json, data/vale_letter.json).
internal static class ValeResidents
{
    public const string ValeHouse = ModEntry.Id + "_ValeHouse";
    public const string TeaRoomShop = ModEntry.Id + "_ValeTeaRoom";
    public const string Bea = "BeaThistle";

    // A room for the night (docs/bea_brief.md): paid at Bea's counter, good until you
    // wake. The guest bed (TouchAction Sleep) says the room's booked until then.
    public const int RoomPrice = 200;
    internal const string RoomNightKey = ModEntry.Id + "/ValeRoomNight";
    // Bea's 10-heart event gives you the artists' room for free (tools/build_bea_events.py): this
    // mail flag, and the game's own doorUnlock flag for its door (ArtistsRoomDoorUnlock).
    public const string ArtistsRoomMail = ModEntry.Id + "_ValeArtistsRoom";

    // Where Bea serves: behind her tea counter (data/bea_schedule.json, 21 21), the row
    // between it and the wall, and the kitchen tile she comes in by.
    private static readonly Rectangle BeaArea = new(19, 20, 5, 2);

    // Lupini's nine canon paintings (Data/Furniture 1838-1854, every other id).
    private static readonly HashSet<string> LupiniPaintings =
        new(Enumerable.Range(0, 9).Select(i => "(F)" + (1838 + i * 2)));
    public const string BoughtLupiniMail = ModEntry.Id + "_BoughtLupini";

    private const string StringsAsset = "Mods/" + ModEntry.Id + "/ValeStrings";

    public static void Register(IModHelper helper, Harmony harmony)
    {
        // "blinxxy.MuseArt_ValeShop <shop id> <owner> <x> <y> <w> <h>": open the shop if its
        // owner is in that tile area. If they're away, the shop's own closed message. If they
        // aren't in the world at all (the dev switch is off, or the Vale isn't open yet), it
        // opens on trust, so the stock can be tried before the residents have art.
        GameLocation.RegisterTileAction(ModEntry.Id + "_ValeShop", (location, args, _, _) =>
        {
            if (!ArgUtility.TryGet(args, 1, out string shop, out string error)
                || !ArgUtility.TryGet(args, 2, out string owner, out error)
                || !ArgUtility.TryGetRectangle(args, 3, out Rectangle area, out error))
            {
                location.LogTileActionError(args, 0, 0, error);
                return false;
            }
            OpenShop(location, shop, owner, area);
            return true;
        });
        GameLocation.RegisterTileAction(ModEntry.Id + "_ValeTeaCounter", (location, _, who, _) =>
        {
            TeaCounter(location, who);
            return true;
        });

        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performTouchAction), new[] { typeof(string[]), typeof(Vector2) }),
            prefix: new HarmonyMethod(typeof(ValeResidents), nameof(TouchActionPrefix)));

        harmony.Patch(AccessTools.Method(typeof(Farmer), nameof(Farmer.passOutFromTired)),
            prefix: new HarmonyMethod(typeof(ValeResidents), nameof(PassOutPrefix)));
        helper.Events.GameLoop.DayStarted += (_, _) => TuckedInMessage();
        helper.Events.GameLoop.DayStarted += (_, _) => KeyOpensDoor(Game1.player);

        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.ShowLockedDoorMessage)),
            prefix: new HarmonyMethod(typeof(ValeResidents), nameof(LockedDoorPrefix)));

        helper.Events.Player.InventoryChanged += OnInventoryChanged;
    }

    private static bool OpenShop(GameLocation location, string shop, string owner, Rectangle area)
    {
        bool inWorld = Game1.getCharacterFromName(owner) != null;
        return Utility.TryOpenShopMenu(shop, location, area, forceOpen: !inWorld);
    }

    // ---- Bea's tea counter: food and tea, or a room for the night ----

    private static void TeaCounter(GameLocation location, Farmer who)
    {
        NPC? bea = Game1.getCharacterFromName(Bea);
        bool here = bea != null && bea.currentLocation == location && BeaArea.Contains(bea.TilePoint);
        if (bea != null && !here)
        {
            Game1.drawObjectDialogue(S("counter.away", "Nobody's at the counter. A card on the bell says 'Back in a minute. Probably. -B'"));
            return;
        }
        bool booked = BookedTonight(who);
        location.createQuestionDialogue(bea != null ? S("counter", "\"What can I get you?\"") : S("counter.bell", "There's a bell on the counter, and a slate of today's food."), new[]
        {
            new Response("Food", S("counter.food", "Tea and something to eat")),
            new Response("Room", booked ? S("counter.room.booked", "My room tonight") : string.Format(S("counter.room", "A room for the night ({0}g)"), RoomPrice)),
            new Response("Leave", S("counter.leave", "Never mind")),
        }, (farmer, answer) =>
        {
            if (answer == "Food")
                OpenShop(farmer.currentLocation, TeaRoomShop, Bea, BeaArea);
            else if (answer == "Room")
                BookRoom(farmer, bea);
        });
    }

    internal static void BookRoom(Farmer who, NPC? bea)
    {
        string line;
        if (BookedTonight(who) || who.mailReceived.Contains(ArtistsRoomMail))
            line = S("room.already", "Your room's upstairs, first on the left. The bed's made, and the roof's one of the good ones.");
        else if (who.Money < RoomPrice)
            line = S("room.money", "Two hundred for the night, love. Come back when the farm's been kind to you.");
        else
        {
            who.Money -= RoomPrice;
            who.modData[RoomNightKey] = Game1.Date.TotalDays.ToString();
            Game1.playSound("purchaseClick");
            line = S("room.booked", "There. Upstairs, first door on the left. Breakfast is whenever you come down, and the stairs creak, so I'll know.");
        }
        if (bea != null)
            Game1.DrawDialogue(new Dialogue(bea, null, line));
        else
            Game1.drawObjectDialogue(line);
    }

    // Booked today (you sleep the same night), or the artists' room is yours for good.
    public static bool BookedTonight(Farmer who) =>
        who.mailReceived.Contains(ArtistsRoomMail)
        || (who.modData.TryGetValue(RoomNightKey, out string day) && day == Game1.Date.TotalDays.ToString());

    internal static bool TouchActionPrefix(GameLocation __instance, string[] action)
    {
        if (__instance.Name != ValeHouse || action.Length == 0 || !string.Equals(action[0], "Sleep", StringComparison.OrdinalIgnoreCase))
            return true;
        if (BookedTonight(Game1.player))
            return true;
        Game1.drawObjectDialogue(S("room.locked", "A card on the pillow: 'Booked. Ask Bea at the counter downstairs.'"));
        return false;
    }

    // ---- The artists' room: not let out ----
    // Its door (data/vale_doors.json) is "Door blinxxy.MuseArt_ArtistsRoom" until Bea's
    // 10-heart key: no villager has that name, so the game's 2-heart rule never opens it,
    // and its locked message would be blank. This is its card on the door instead.
    public const string ArtistsRoomDoor = ModEntry.Id + "_ArtistsRoom";

    // The game's "Door <name>" action opens for anyone with the mail flag doorUnlock<name>. Bea's
    // 10-heart event gives it with her key, so the door opens the moment you have it (the map's
    // lock itself only comes off at the next location change); this keeps the two together for a
    // key given any other way.
    public const string ArtistsRoomDoorUnlock = "doorUnlock" + ArtistsRoomDoor;

    internal static void KeyOpensDoor(Farmer who)
    {
        if (who.mailReceived.Contains(ArtistsRoomMail))
            who.mailReceived.Add(ArtistsRoomDoorUnlock);
    }

    internal static bool LockedDoorPrefix(string[] action)
    {
        if (action.Length < 2 || action[1] != ArtistsRoomDoor)
            return true;
        Game1.drawObjectDialogue(S("artists.locked", "A card pinned to the door, in Bea's hand: 'The Artists' Room. Not let out. -B'"));
        return false;
    }

    // ---- Passing out in the Vale House: Bea puts you to bed ----
    // Out on your feet at 2am in the inn (the user, 2026-11-08: paid for the room, got in at
    // 1:50, and fell asleep on the floor) shouldn't bring Harvey's or Joja's bill. The map is
    // PassOutSafe, so the game sends no letter and takes no gold. This wakes you in the guest
    // bed, not back at the farm, and charges the room if you hadn't booked it.
    private static readonly Point GuestBed = new(8, 6);
    internal const string TuckedInKey = ModEntry.Id + "/ValeTuckedIn";

    internal static void PassOutPrefix(Farmer who)
    {
        try
        {
            if (!who.IsLocalPlayer || who.currentLocation?.Name != ValeHouse)
                return;
            who.lastSleepLocation.Value = ValeHouse;
            who.lastSleepPoint.Value = GuestBed;
            int paid = 0;
            if (!BookedTonight(who))
            {
                paid = Math.Min(RoomPrice, who.Money);
                who.Money -= paid;
            }
            who.modData[TuckedInKey] = paid.ToString();
        }
        catch (Exception)
        {
            // never stand between the player and the end of the day
        }
    }

    private static void TuckedInMessage()
    {
        if (!Game1.player.modData.TryGetValue(TuckedInKey, out string paid))
            return;
        Game1.player.modData.Remove(TuckedInKey);
        string text = paid == "0" ? S("room.tucked", "You woke up in the guest bed. Bea must have found you on the floor and put you to bed.")
            : string.Format(S("room.tucked.paid", "You woke up in the guest bed. Bea found you on the floor and put you to bed. She took {0}g for the room."), paid);
        Game1.addHUDMessage(new HUDMessage(text) { noIcon = true, timeLeft = 6000 });
    }

    // ---- A Lupini painting from the Night Market (his letter opening the Vale) ----

    private static void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!e.IsLocalPlayer || e.Player.mailReceived.Contains(BoughtLupiniMail))
            return;
        if (e.Added.Any(item => LupiniPaintings.Contains(item.QualifiedItemId)))
            e.Player.mailReceived.Add(BoughtLupiniMail);
    }

    // ---- Texts, from the dev pack (Mods/blinxxy.MuseArt/ValeStrings), with English fallbacks ----

    private static string S(string key, string fallback)
    {
        try
        {
            var strings = Game1.content.Load<Dictionary<string, string>>(StringsAsset);
            return strings.TryGetValue(key, out string? text) && !string.IsNullOrWhiteSpace(text) ? text : fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
#endif
