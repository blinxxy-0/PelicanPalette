using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;

namespace ArtStudio.Social;

// The museum's painting wing (1.3, docs/design/museum_wing.md): a gallery
// off the exhibit hall's east wall (its map is tools/build_museum_wing.py).
// Every one of the 21 old paintings has its place on its walls: the Old
// Valley's along the back wall, then the Wreck's, the Miners', and Calico's
// with the Dwarves' on the three freestanding walls. A painting you've
// donated hangs there in its set's frame; one still missing is a card.
// The paint-tube Junimo joins once its letter has come (all 21).
//
// The doorway is roped off until the morning after your first donation.
// Everything here is the museum's: none of it can be picked up.
internal static class PaintingWing
{
    public const string LocationName = ModEntry.Id + "_PaintingWing";
    public const string OpenFlag = ModEntry.Id + "_PaintingWingOpen";
    public const string Museum = "ArchaeologyHouse";
    private const string WingKey = ModEntry.Id + "/PaintingWing";       // the place a piece belongs to
    private const string CardId = "(F)" + ModEntry.Id + "_WingCard";
    private const string RopeId = "(F)" + ModEntry.Id + "_WingRope";
    private const string BenchId = "(F)" + ModEntry.Id + "_GalleryBench";
    private const string JunimoId = "(F)" + ModEntry.Id + "_PaintTubeJunimo";
    public static readonly Point Doorway = new(49, 15);
    private static readonly Point JunimoTile = new(13, 27);
    private static readonly Point[] Benches = { new(7, 5), new(20, 5), new(7, 12), new(20, 12), new(7, 19), new(20, 19) };

    // Each wall's paper row, and the sets hung on it, centred, 3 tiles a
    // painting with a tile between.
    private static readonly (int Row, string[] Sets)[] Walls =
    {
        (1, new[] { "valley" }), (8, new[] { "wreck" }), (15, new[] { "miners" }), (22, new[] { "calico", "dwarvish" }),
    };

    private const int Width = 29;

    // Every painting's place: the top-left tile of its frame.
    public static IEnumerable<(OldPainting Painting, Point Tile)> Places()
    {
        foreach (var (row, sets) in Walls)
        {
            var paintings = OldPaintings.All.Where(p => sets.Contains(p.Set)).OrderBy(p => Array.IndexOf(sets, p.Set)).ToList();
            int x = (Width - (paintings.Count * 4 - 1)) / 2;
            foreach (var painting in paintings)
            {
                yield return (painting, new Point(x, row));
                x += 4;
            }
        }
    }

    public static bool IsOpen => Game1.MasterPlayer.mailReceived.Contains(OpenFlag);

    public static void Register(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.canBeRemoved)),
            prefix: new HarmonyMethod(typeof(PaintingWing), nameof(CanBeRemovedPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.checkForAction)),
            prefix: new HarmonyMethod(typeof(PaintingWing), nameof(CheckForActionPrefix)));
        // The night after the first donation the flag is set, so the rope is
        // gone in the morning.
        helper.Events.GameLoop.DayEnding += (_, _) =>
        {
            if (Context.IsMainPlayer && !IsOpen && MuseumCollection.DonatedCount > 0)
                Game1.player.team.RequestSetMail(PlayerActionTarget.All, OpenFlag, MailType.Received, add: true);
        };
        helper.Events.GameLoop.DayStarted += (_, _) =>
        {
            if (!Context.IsMainPlayer) return;
            if (Game1.getLocationFromName(Museum) is { } museum) RopeDoorway(museum);
            if (Game1.getLocationFromName(LocationName) is { } wing) Hang(wing);
        };
        helper.Events.Player.Warped += (_, e) =>
        {
            if (!Context.IsMainPlayer) return;
            if (e.NewLocation.Name == LocationName) Hang(e.NewLocation);
            else if (e.NewLocation.Name == Museum) RopeDoorway(e.NewLocation);
        };
    }

    // The rope across the doorway while the wing's closed.
    internal static void RopeDoorway(GameLocation museum)
    {
        var rope = museum.furniture.FirstOrDefault(f => f.modData.ContainsKey(WingKey));
        if (IsOpen)
        {
            if (rope != null) museum.furniture.Remove(rope);
        }
        else if (rope == null)
            museum.furniture.Add(Piece(RopeId, Doorway, "rope"));
    }

    // Puts up what belongs where: each painting (or its card), the benches,
    // and the Junimo once it's been sent.
    internal static void Hang(GameLocation wing)
    {
        foreach (var piece in wing.furniture.Where(f => f.modData.ContainsKey(WingKey)).ToList())
            wing.furniture.Remove(piece);
        foreach (var (painting, tile) in Places())
        {
            if (MuseumCollection.IsDonated(painting.Id))
            {
                var framed = ArtHooks.Frame(Restoration(painting), FrameFor(painting.Set));
                framed.TileLocation = tile.ToVector2();
                framed.modData[WingKey] = painting.Id;
                wing.furniture.Add(framed);
            }
            else
                wing.furniture.Add(Piece(CardId, new Point(tile.X + 1, tile.Y + 1), "card:" + painting.Id));
        }
        foreach (var bench in Benches)
            wing.furniture.Add(Piece(BenchId, bench, "bench"));
        if (Game1.player.mailReceived.Contains(MuseumCollection.RewardLetter("count-21")))
            wing.furniture.Add(Piece(JunimoId, JunimoTile, "junimo"));
    }

    private static Furniture Piece(string id, Point tile, string what)
    {
        var piece = ItemRegistry.Create<Furniture>(id);
        piece.TileLocation = tile.ToVector2();
        piece.modData[WingKey] = what;
        return piece;
    }

    // The painting as it was, whole.
    internal static StardewValley.Object Restoration(OldPainting painting)
    {
        var data = new PaintingData
        {
            Size = painting.Size,
            Cells = OldPaintings.Original(painting),
            Purity = new Purity[painting.Size, painting.Size],
            Subject = OldPaintings.SubjectFor(painting.Id),
            Stars = 5,
        };
        return PaintingItems.Create(data, PaintingRules.Iridium, OldPaintings.RestoredValue(painting, PaintingRules.Iridium));
    }

    // Each set in its own frame; the Old Valley in gold leaf.
    internal static FrameStyle FrameFor(string set) => set switch
    {
        "wreck" => ArtItems.Driftwood,
        "miners" => ArtItems.Iron,
        "calico" => ArtItems.Sandstone,
        "dwarvish" => ArtItems.Dwarvish,
        _ => ArtItems.GoldLeaf,
    };

    private static bool CanBeRemovedPrefix(Furniture __instance, ref bool __result)
    {
        if (!__instance.modData.ContainsKey(WingKey)) return true;
        __result = false;
        return false;
    }

    // Clicking a painting or a card reads its card; the rope, its sign.
    private static bool CheckForActionPrefix(Furniture __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        try
        {
            if (!__instance.modData.TryGetValue(WingKey, out string what) || what is "bench" or "junimo") return true;
            __result = true;
            if (justCheckingForActivity) return false;
            if (what == "rope")
                Game1.drawObjectDialogue(I18n.Get("wing.closed"));
            else if (what.StartsWith("card:") && OldPaintings.Get(what.Substring(5)) is { } missing)
                Game1.drawObjectDialogue(I18n.Get("wing.card.missing", new { set = OldPaintings.SetName(missing.Set) }));
            else if (OldPaintings.Get(what) is { } hung)
                Game1.drawObjectDialogue(I18n.Get("wing.card.hung", new { title = OldPaintings.Title(hung), set = OldPaintings.SetName(hung.Set) }));
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}
