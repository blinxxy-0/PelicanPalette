#if DEBUG
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace ArtStudio.Vale;

// The graveyard in the old Painter's clearing (the Vale painters' guide design, section 6;
// tools/vale_graveyard.py lays it): seven graves inside a low stone wall, each stone or marker
// carrying `blinxxy.MuseArt_Grave <id>` on its foot. Touch one to read it: a line about the
// marker, then what's on it (the name, what they made, their summers in the Vale, the epitaph).
// Reading a stone sets a flag the residents' lines wait for (data/tobin_dialogue.json: the urns,
// from 4 hearts; data/lupini_dialogue.json: Bram's stone).
internal static class ValeGraves
{
    public const string GraveAction = ModEntry.Id + "_Grave";

    // Any stone read; and each one's own (ReadFlag + "_" + id).
    public const string ReadFlag = ModEntry.Id + "_ValeGraveRead";

    // The walkable tile just inside the gate (tools/vale_graveyard.py GATE_INSIDE): where the Grove's
    // frame sets you down (GildedGrove.HomeTile), among the people you just met as echoes.
    public static readonly Point GraveyardGateTile = new(10, 14);

    // The back row (Bram's carved headstones, then his own plain stone) and the front row (clay).
    public static readonly string[] Ids = { "agnes", "hollis", "mabel", "bram", "ada", "hester", "amos" };

    // A stone's page break: its description, then what's written on it. "^" is a line on the stone,
    // and the dialogue box's font draws "^" as a line break, so it's left in.
    private const string PageBreak = "^^";

    public static IMonitor? Monitor;

    public static void Register(IMonitor monitor)
    {
        Monitor = monitor;
        GameLocation.RegisterTileAction(GraveAction, (_, args, who, _) => OnGrave(who, args.Skip(1).FirstOrDefault()));
    }

    public static string Key(string id) => "vale.grave." + id;

    public static string FlagFor(string id) => ReadFlag + "_" + id;

    // A stone's text as the pages it's shown in: the line about the marker, then the inscription,
    // a line of the stone to a line on screen. Empty for an id that isn't one of the seven.
    public static List<string> Pages(string? id)
    {
        if (id == null || !Ids.Contains(id))
            return new List<string>();
        return PaintersGuide.Text(Key(id)).Split(PageBreak)
            .Select(p => p.Trim().Trim('^'))
            .Where(p => p.Length > 0)
            .ToList();
    }

    // Touch a stone: read it, quietly. The flags are only the reader's own.
    public static bool OnGrave(Farmer who, string? id)
    {
        if (!who.IsLocalPlayer || Game1.activeClickableMenu != null)
            return false;
        var pages = Pages(id);
        if (pages.Count == 0)
        {
            Monitor?.Log($"The grave '{id}' isn't one of the seven.", LogLevel.Warn);
            return false;
        }
        who.mailReceived.Add(ReadFlag);
        who.mailReceived.Add(FlagFor(id!));
        Game1.drawObjectDialogue(pages);
        return true;
    }

    public static IEnumerable<string> StringKeys => Ids.Select(Key);
}
#endif
