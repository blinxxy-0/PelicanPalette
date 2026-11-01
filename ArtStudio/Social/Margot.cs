using System.Linq;
using ArtStudio.Painting;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network.NetEvents;

namespace ArtStudio.Social;

// Margot Pell (1.3), the conservator (data/margot.json). She isn't in the
// valley until the first damaged old painting turns up: the morning after
// anyone finds one, Gunther's letter says the city museum is sending
// someone, and she arrives a week after the letter. Her data's
// UnlockConditions reads the arrival flag set here.
internal static class Margot
{
    public const string Name = "MargotPell";
    public const string LetterFlag = ModEntry.Id + "_MargotLetter";
    public const string ArrivedFlag = ModEntry.Id + "_MargotArrived";
    public const string CottageFlag = ModEntry.Id + "_MargotCottageBuilt";
    // Her cottage on the town map, with its bench (tools/build_margot_cottage.py).
    public static readonly Microsoft.Xna.Framework.Rectangle CottageArea = new(18, 91, 6, 8);
    private const string DueKey = ModEntry.Id + "/MargotDue";
    public const int DaysAfterLetter = 7;

    public static NPC? Npc => Game1.getCharacterFromName(Name);

    public static bool Arrived => Game1.MasterPlayer.mailReceived.Contains(ArrivedFlag);

    public static void Register(IModHelper helper)
    {
        helper.Events.GameLoop.DayStarted += (_, _) =>
        {
            Update();
            ClearCottagePlot();
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

    internal static void Update()
    {
        if (!Context.IsMainPlayer || Arrived) return;
        var host = Game1.MasterPlayer;
        int today = Game1.Date.TotalDays;
        if (!host.modData.ContainsKey(DueKey))
        {
            if (!Game1.getAllFarmers().Any(OldPaintings.FoundAny)) return;
            // The letter comes tomorrow; she comes a week after it.
            Game1.player.team.RequestSetMail(PlayerActionTarget.All, LetterFlag, MailType.Tomorrow, add: true);
            host.modData[DueKey] = (today + 1 + DaysAfterLetter).ToString();
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
