using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network.NetEvents;
using StardewValley.Objects;

namespace ArtStudio.Social;

// The museum's painting wing (1.3, docs/design/museum_wing.md): a gallery
// off the exhibit hall's east wall (its map is tools/build_museum_wing.py).
// Every one of the 21 old paintings has its place: the Old Valley's five
// framed along the back wall, and the rest each on a white marble display
// in three rows across the floor (the Wreck's, the Miners', and Calico's
// with the Dwarves'). A painting you've donated is there; one still missing
// is a card (on the wall, or on its display's board). The paint-tube
// Junimo joins once its letter has come (all 21).
//
// The doorway is roped off until the morning after your first donation.
// Everything here is the museum's: none of it can be picked up.
internal static class PaintingWing
{
    public const string LocationName = ModEntry.Id + "_PaintingWing";
    public const string OpenFlag = ModEntry.Id + "_PaintingWingOpen";
    public const string ToldFlag = ModEntry.Id + "_PaintingWingTold";      // Gunther's told you it's open
    public const string Museum = "ArchaeologyHouse";
    private const string WingKey = ModEntry.Id + "/PaintingWing";       // what a piece is: a painting's id, "card:<id>", ...
    public const string CardId = "(F)" + ModEntry.Id + "_WingCard";
    public const string DisplayId = "(F)" + ModEntry.Id + "_WingDisplay";
    public const string RopeId = "(F)" + ModEntry.Id + "_WingRope";
    private const string BenchId = "(F)" + ModEntry.Id + "_GalleryBench";
    public const string JunimoId = "(F)" + ModEntry.Id + "_PaintTubeJunimo";
    public static readonly Point Doorway = new(49, 15);
    private static readonly Point JunimoTile = new(12, 25);
    private static readonly Point[] Benches = { new(6, 5), new(19, 5), new(6, 24), new(19, 24) };
    private const int Width = 27;
    // The display's board, in its sprite's pixels: where the painting goes.
    private static readonly Rectangle Board = new(6, 7, 36, 36);
    private static readonly Rectangle CardSprite = new(64, 16, 16, 16);

    // The back wall's paper row, and each row of displays' base row, with
    // the sets on it, centred: 3 tiles a place with a tile between.
    public const int WallRow = 1;
    private static readonly (int Row, bool OnWall, string[] Sets)[] Rows =
    {
        (WallRow, true, new[] { "valley" }),
        (10, false, new[] { "wreck" }), (16, false, new[] { "miners" }), (22, false, new[] { "calico", "dwarvish" }),
    };

    // Every painting's place: its frame's top-left tile on the wall, or its
    // display's base (the display stands three tiles above it).
    public static IEnumerable<(OldPainting Painting, Point Tile, bool OnWall)> Places()
    {
        foreach (var (row, onWall, sets) in Rows)
        {
            var paintings = OldPaintings.All.Where(p => sets.Contains(p.Set)).OrderBy(p => Array.IndexOf(sets, p.Set)).ToList();
            int x = (Width - (paintings.Count * 4 - 1)) / 2;
            foreach (var painting in paintings)
            {
                yield return (painting, new Point(x, row), onWall);
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
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            postfix: new HarmonyMethod(typeof(PaintingWing), nameof(DrawPostfix)));
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
        foreach (var (painting, tile, onWall) in Places())
        {
            bool donated = MuseumCollection.IsDonated(painting.Id);
            if (!onWall)
                wing.furniture.Add(Piece(DisplayId, tile, donated ? painting.Id : "card:" + painting.Id));
            else if (donated)
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

    // What a piece in the wing is.
    public static string? What(Furniture piece) => piece.modData.TryGetValue(WingKey, out string what) ? what : null;

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

    // The Old Valley's frame on the wall: gold leaf.
    internal static FrameStyle FrameFor(string set) => set switch
    {
        "wreck" => ArtItems.Driftwood,
        "miners" => ArtItems.Iron,
        "calico" => ArtItems.Sandstone,
        "dwarvish" => ArtItems.Dwarvish,
        _ => ArtItems.GoldLeaf,
    };

    // A display's painting (or card) on its board.
    private static void DrawPostfix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha, Netcode.NetVector2 ___drawPosition)
    {
        try
        {
            if (__instance.QualifiedItemId != DisplayId || __instance.isTemporarilyInvisible || What(__instance) is not { } what) return;
            var source = __instance.sourceRect.Value;
            Vector2 position = Furniture.isDrawingLocationFurniture
                ? Game1.GlobalToLocal(Game1.viewport, ___drawPosition.Value)
                : Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (source.Height * 4 - __instance.boundingBox.Height)));
            float depth = (__instance.boundingBox.Value.Bottom - 8) / 10000f + 0.00002f;
            var board = new Rectangle((int)position.X + Board.X * 4, (int)position.Y + Board.Y * 4, Board.Width * 4, Board.Height * 4);
            if (what.StartsWith("card:"))
            {
                var texture = Game1.content.Load<Texture2D>("Mods/" + ModEntry.Id + "/Gallery");
                var card = new Rectangle(board.Center.X - 32, board.Center.Y - 32, 64, 64);
                spriteBatch.Draw(texture, card, CardSprite, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, depth);
            }
            else if (OldPaintings.Get(what) is { } painting)
            {
                var art = OldPaintings.OriginalTexture(painting);
                int cell = Math.Max(1, board.Width / art.Width), side = cell * art.Width;
                var dest = new Rectangle(board.Center.X - side / 2, board.Center.Y - side / 2, side, side);
                spriteBatch.Draw(art, dest, null, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, depth);
            }
        }
        catch (Exception) { }
    }

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
            if (What(__instance) is not { } what || what is "bench" or "junimo") return true;
            __result = true;
            if (justCheckingForActivity) return false;
            if (what == "rope")
                Game1.drawObjectDialogue(I18n.Get("wing.closed"));
            else if (what.StartsWith("card:") && OldPaintings.Get(what.Substring(5)) is { } missing)
                Game1.drawObjectDialogue(I18n.Get("wing.card.missing", new { title = OldPaintings.Title(missing), set = OldPaintings.SetName(missing.Set), about = OldPaintings.About(missing), where = OldPaintings.WhereToFind(missing.Set) }));
            else if (OldPaintings.Get(what) is { } hung)
                Game1.drawObjectDialogue(I18n.Get("wing.card.hung", new { title = OldPaintings.Title(hung), set = OldPaintings.SetName(hung.Set), about = OldPaintings.About(hung) }));
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}
