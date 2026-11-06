using System.Linq;
using ArtStudio.Painting;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network.NetEvents;

namespace ArtStudio.Social;

// Margot Pell (1.3), the conservator (data/margot.json). She isn't in the
// valley until the first damaged old painting turns up and someone shows
// it to Gunther (his short event at the museum, data/margot_events.json):
// he writes to the city museum, his letter comes the next morning to say
// she's on her way, and she arrives a week after the letter. Her data's
// UnlockConditions reads the arrival flag set here.
internal static class Margot
{
    public const string Name = "MargotPell";
    public const string LetterFlag = ModEntry.Id + "_MargotLetter";
    public const string ArrivedFlag = ModEntry.Id + "_MargotArrived";
    public const string CottageFlag = ModEntry.Id + "_MargotCottageBuilt";
    // Her cottage on the town map, with its bench (tools/build_margot_cottage.py).
    public static readonly Microsoft.Xna.Framework.Rectangle CottageArea = new(18, 91, 6, 9);
    private const string DueKey = ModEntry.Id + "/MargotDue";
    public const int DaysAfterLetter = 7;

    public static NPC? Npc => Game1.getCharacterFromName(Name);

    public static bool Arrived => Game1.MasterPlayer.mailReceived.Contains(ArrivedFlag);

    public static void Register(IModHelper helper)
    {
        // Gunther's event, once you've shown him the painting: his letter,
        // to everyone, tomorrow morning.
        Event.RegisterCommand(ModEntry.Id + "_MargotSendFor", (ev, _, _) =>
        {
            SendFor();
            ev.CurrentCommand++;
        });
        // "blinxxy.MuseArt_HAS_DAMAGED_PAINTING <player>": they've a damaged
        // old painting in their bag (for Gunther's event).
        GameStateQuery.Register(ModEntry.Id + "_HAS_DAMAGED_PAINTING", (query, context) =>
            GameStateQuery.Helpers.WithPlayer(context.Player, query[1], who => who.Items.Any(OldPaintings.IsDamaged)));
        helper.Events.GameLoop.DayStarted += (_, _) =>
        {
            Update();
            ClearCottagePlot();
            LiftOldPath();
        };
    }

    // Once the cottage stands, nothing the town grew there (its bush, weeds,
    // stones) stays under it.
    internal static void ClearCottagePlot()
    {
        if (!Context.IsMainPlayer || !Game1.MasterPlayer.mailReceived.Contains(CottageFlag)) return;
        var town = Game1.getLocationFromName("Town");
        if (town == null) return;
        var area = new Microsoft.Xna.Framework.Rectangle(CottageArea.X * 64, CottageArea.Y * 64, CottageArea.Width * 64, CottageArea.Height * 64);
        town.largeTerrainFeatures.RemoveWhere(f => f.getBoundingBox().Intersects(area));
        foreach (var tile in town.terrainFeatures.Keys.Where(t => CottageArea.Contains((int)t.X, (int)t.Y)).ToList())
            town.terrainFeatures.Remove(tile);
        foreach (var tile in town.objects.Keys.Where(t => CottageArea.Contains((int)t.X, (int)t.Y)).ToList())
            town.objects.Remove(tile);
    }

    // The window box you chose in her 4-heart scene (roses, lavender or
    // marigolds): the first time you talk to her at home, she mentions it.
    private static readonly string[] Boxes = { "Roses", "Lavender", "Marigold" };
    private const string BoxToldFlag = ModEntry.Id + "_MargotBoxTold";

    internal static bool TryWindowBoxLine(NPC margot, Farmer who)
    {
        if (margot.currentLocation?.Name != MargotEvents.CottageHome || who.mailReceived.Contains(BoxToldFlag)) return false;
        string? box = Boxes.FirstOrDefault(b => who.mailReceived.Contains(ModEntry.Id + "_MargotBox" + b));
        if (box == null) return false;
        who.mailReceived.Add(BoxToldFlag);
        margot.CurrentDialogue.Push(new Dialogue(margot, null, I18n.Get("margot.window-box." + box.ToLowerInvariant())));
        return true;
    }

    // The path from the town road to her door is part of the map
    // (data/margot.json, tools/art/margot_path.py): down Town column 23
    // from the road, then along row 96 to the door step.
    public static readonly Microsoft.Xna.Framework.Vector2[] PathTiles =
    {
        new(23, 92), new(23, 93), new(23, 94), new(23, 95), new(23, 96), new(22, 96), new(21, 96),
    };

    // (A build before the map's path laid it as loose Stone Walkway Floor:
    // that goes, once, from any save that has it.)
    private const string OldPathFlag = ModEntry.Id + "_MargotPathLaid";

    internal static void LiftOldPath()
    {
        if (!Context.IsMainPlayer || !Game1.MasterPlayer.mailReceived.Remove(OldPathFlag)) return;
        var town = Game1.getLocationFromName("Town");
        if (town == null) return;
        foreach (var tile in PathTiles)
            if (town.terrainFeatures.TryGetValue(tile, out var feature) && feature is StardewValley.TerrainFeatures.Flooring)
                town.terrainFeatures.Remove(tile);
    }

    // Gunther writes to the city museum: his letter comes tomorrow.
    internal static void SendFor()
    {
        if (Arrived || LetterSent) return;
        Game1.player.team.RequestSetMail(PlayerActionTarget.All, LetterFlag, MailType.Tomorrow, add: true);
    }

    // Anyone's had his letter, or has it waiting (in the mailbox, or tomorrow's post).
    public static bool LetterSent => Game1.getAllFarmers().Any(who =>
        who.mailReceived.Contains(LetterFlag) || who.mailbox.Contains(LetterFlag) || who.mailForTomorrow.Contains(LetterFlag));

    // The morning his letter's in the mailbox, she's due a week later.
    internal static void Update()
    {
        if (!Context.IsMainPlayer || Arrived) return;
        var host = Game1.MasterPlayer;
        int today = Game1.Date.TotalDays;
        if (!host.modData.ContainsKey(DueKey))
        {
            if (!Game1.getAllFarmers().Any(who => who.mailbox.Contains(LetterFlag) || who.mailReceived.Contains(LetterFlag))) return;
            host.modData[DueKey] = (today + DaysAfterLetter).ToString();
            return;
        }
        if (int.TryParse(host.modData[DueKey], out int due) && today >= due)
            Arrive();
    }

    // Sets the flag and puts her in the world today (the game adds unlocked
    // villagers before the day starts, so she'd otherwise wait a day more).
    internal static void Arrive()
    {
        Game1.player.team.RequestSetMail(PlayerActionTarget.All, ArrivedFlag, MailType.Received, add: true);
        if (!Game1.MasterPlayer.mailReceived.Contains(ArrivedFlag))
            Game1.MasterPlayer.mailReceived.Add(ArrivedFlag);
        if (Game1.AddCharacterIfNecessary(Name) && Npc is { } margot)
            margot.TryLoadSchedule();
    }

    // For the dev test: forget she ever came.
    internal static void DevReset()
    {
        foreach (var who in Game1.getAllFarmers())
        {
            who.mailReceived.Remove(LetterFlag);
            who.mailReceived.Remove(ArrivedFlag);
            who.mailForTomorrow.Remove(LetterFlag);
            who.mailbox.Remove(LetterFlag);
        }
        Game1.MasterPlayer.modData.Remove(DueKey);
        if (Npc is { } margot)
            margot.currentLocation?.characters.Remove(margot);
    }
}
