using System.Linq;
using ArtStudio.Painting;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network.NetEvents;

namespace ArtStudio.Social;

// What Margot's heart events need from the mod (scripts in the content pack,
// tools/build_margot_events.py; the approved draft is docs/margot_events.md).
//
// Her 4-heart event, at Robin's, commissions her cottage: three days later
// it stands by the river and she lives there. The flag that shows it is set
// the night before, so Content Patcher sees it when the day starts.
internal static class MargotEvents
{
    public const string CottageHome = ModEntry.Id + "_MargotCottage";
    public const string RobinLetter = ModEntry.Id + "_MargotCottageLetter";
    private const string DueKey = ModEntry.Id + "/MargotCottageDue";
    // Her Saloon room, which becomes Gus's guest room once she's moved out.
    public const string SaloonRoom = ModEntry.Id + "_MargotRoom";
    public const string GuestWallpaper = ModEntry.Id + "_Wallpapers:3";
    private const string PaperedKey = ModEntry.Id + "/GuestRoomPapered";
    public const int BuildDays = 3;
    // Where she stands at home in the cottage.
    public static readonly Microsoft.Xna.Framework.Point CottageTile = new(7, 5);

    public static bool CottageBuilt => Game1.MasterPlayer.mailReceived.Contains(Margot.CottageFlag);

    public static void Register(IModHelper helper)
    {
        // 4 hearts: Robin starts building.
        Event.RegisterCommand(ModEntry.Id + "_MargotCommission", (ev, _, _) =>
        {
            Commission();
            ev.CurrentCommand++;
        });
        // 8 hearts: what she did about the museum's errand, by your 6-heart answer.
        Event.RegisterCommand(ModEntry.Id + "_MargotErrand", (ev, _, _) =>
            ev.ReplaceCurrentCommand($"speak {Margot.Name} \"{ErrandLine(Game1.player)}\""));
        // 10 hearts: what you painted together, by your answer.
        Event.RegisterCommand(ModEntry.Id + "_MargotPaintings", (ev, _, _) =>
        {
            foreach (var item in Paintings(Game1.player))
                Game1.player.addItemByMenuIfNecessary(item);
            ev.CurrentCommand++;
        });
        // 14 hearts: the card under the painting you restored together.
        Event.RegisterCommand(ModEntry.Id + "_MargotCard", (ev, _, _) =>
            ev.ReplaceCurrentCommand($"message \"{CardLine(Game1.player)}\""));

        helper.Events.GameLoop.DayEnding += (_, _) => FinishBuilding();
        helper.Events.GameLoop.DayStarted += (_, _) => MoveIn();
        helper.Events.Player.Warped += (_, e) =>
        {
            if (e.NewLocation.Name == SaloonRoom)
                PaperGuestRoom();
        };
    }

    internal static void Commission()
    {
        if (!Context.IsMainPlayer || CottageBuilt || Game1.MasterPlayer.modData.ContainsKey(DueKey)) return;
        Game1.MasterPlayer.modData[DueKey] = (Game1.Date.TotalDays + BuildDays).ToString();
    }

    // The night before it's done: the flag for everyone, and Robin's letter.
    internal static void FinishBuilding()
    {
        if (!Context.IsMainPlayer || CottageBuilt) return;
        if (!Game1.MasterPlayer.modData.TryGetValue(DueKey, out string raw) || !int.TryParse(raw, out int due)) return;
        if (Game1.Date.TotalDays + 1 < due) return;
        SetBuilt();
    }

    internal static void SetBuilt()
    {
        foreach (var who in Game1.getAllFarmers())
            if (!who.mailReceived.Contains(Margot.CottageFlag))
                who.mailReceived.Add(Margot.CottageFlag);
        Game1.player.team.RequestSetMail(PlayerActionTarget.All, Margot.CottageFlag, MailType.Received, add: true);
        Game1.player.team.RequestSetMail(PlayerActionTarget.All, RobinLetter, MailType.Tomorrow, add: true);
    }

    // Once it's built, the cottage is her home (and where she goes at night).
    internal static void MoveIn()
    {
        if (!Context.IsMainPlayer || !CottageBuilt || Margot.Npc is not { } margot) return;
        if (margot.DefaultMap == CottageHome) return;
        if (margot.getSpouse() != null) return;           // (married, the farm is home)
        margot.DefaultMap = CottageHome;
        margot.DefaultPosition = new Microsoft.Xna.Framework.Vector2(CottageTile.X, CottageTile.Y) * 64f;
        Game1.warpCharacter(margot, CottageHome, CottageTile);
        margot.TryLoadSchedule();
    }

    // The first time you walk into the guest room (data/margot.json lays it
    // over hers once she's moved out), Gus has papered it; after that it
    // keeps whatever's on its walls. (Not at day start: the map is re-laid
    // after that, and its new walls would lose the paper.)
    internal static void PaperGuestRoom()
    {
        if (!Context.IsMainPlayer || !CottageBuilt || Game1.getLocationFromName(SaloonRoom) is not StardewValley.Locations.DecoratableLocation room) return;
        if (room.modData.ContainsKey(PaperedKey)) return;
        room.SetWallpaper(GuestWallpaper, null);
        room.modData[PaperedKey] = "1";
    }

    // "Let's paint each other": your portrait of her, and hers of you,
    // painted from you as you look that night (her first painting of her
    // own: careful, gold). "Let's paint the window": the one you both painted.
    internal static System.Collections.Generic.List<Item> Paintings(Farmer who)
    {
        var items = new System.Collections.Generic.List<Item>();
        if (who.mailReceived.Contains(ModEntry.Id + "_MargotEachOther"))
        {
            items.Add(ItemRegistry.Create("(F)" + ModEntry.Id + "_MargotByYou"));
            // (Signed as on her restorations.)
            items.Add(ArtHooks.Frame(FarmerPortrait.Create(who, I18n.Get("margot.name"), "moss", 4, 1500)));
        }
        else if (who.mailReceived.Contains(ModEntry.Id + "_MargotWindow"))
            items.Add(ItemRegistry.Create("(F)" + ModEntry.Id + "_NorthWindow"));
        return items;
    }

    internal static string ErrandLine(Farmer who)
    {
        string key = who.mailReceived.Contains(ModEntry.Id + "_MargotTalk") ? "margot.errand.talk"
            : who.mailReceived.Contains(ModEntry.Id + "_MargotNotMine") ? "margot.errand.not-mine"
            : "margot.errand.report";
        string line = I18n.Get(key);
        // (Once Sable's 8-heart event has told the "E." story: it's Evelyn's.)
        if (key == "margot.errand.talk" && who.eventsSeen.Contains(ModEntry.Id + "_Sable8h"))
            line = line.Replace("#$b#", "#$b#" + I18n.Get("margot.errand.evelyn") + "#$b#");
        return line;
    }

    internal static string CardLine(Farmer who)
    {
        var painting = OldPaintings.All.FirstOrDefault(p => MuseumCollection.IsDonated(p.Id)) ?? OldPaintings.Get("valley_mill");
        string title = painting != null ? OldPaintings.Title(painting) : "";
        return I18n.Get("margot.card", new { title, farmer = who.Name, year = Game1.year });
    }

    // For the dev test: forget the cottage.
    internal static void DevReset()
    {
        foreach (var who in Game1.getAllFarmers())
        {
            who.mailReceived.Remove(Margot.CottageFlag);
            who.mailReceived.Remove(RobinLetter);
            who.mailForTomorrow.Remove(RobinLetter);
        }
        Game1.MasterPlayer.modData.Remove(DueKey);
        Game1.getLocationFromName(SaloonRoom)?.modData.Remove(PaperedKey);
    }
}
