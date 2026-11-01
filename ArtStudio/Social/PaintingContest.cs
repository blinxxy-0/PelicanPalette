using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace ArtStudio.Social;

// The painting contest at the Stardew Valley Fair (1.2), judged by Julian
// Vane at a table beside the grange display. Talk to him before Lewis judges
// the grange and enter one painting; it stands on his easel for the day.
// Lewis's judging judges it too, against three villagers; talk to Julian
// afterwards for your placing, star tokens and a ribbon, and your painting
// back. (Proposed in docs/design/1.2_plan.md and approved 2026-10-31.)
//
// The entry waits in a global inventory of its own, so a painting is never
// lost if the game closes during the Fair: it's handed back the next time.
internal static class PaintingContest
{
    public const string FestivalId = "fall16";
    // Julian's table: on the grass by the grange row. Stardew Valley Expanded
    // fills that corner with stalls and planters, so with it he's on the quiet
    // grass by the lamp, left of the north road (the user's pick).
    // (The content pack places him to match: data/julian.json.)
    public static Point JulianTile => _sve ? new(18, 51) : new(48, 59);
    public static Point EaselTile => new(JulianTile.X + 1, JulianTile.Y);

    // The villagers' entries lean on the tree by the grange row; with Stardew
    // Valley Expanded, on the birch behind his table.
    public static Point EntriesTile => _sve ? new(16, 50) : new(52, 61);
    private static bool _sve;
    public static bool WithSve => _sve;
    private const string EntryPrefix = ModEntry.Id + "_ContestEntry_";
    private const string DoneKey = ModEntry.Id + "/ContestYear";

    public static readonly int[] Prizes = { 1000, 500, 200, 50 };

    // Each rival's range of scores (out of 100). Evelyn takes Elliott's place
    // once she's painting again (after Sable's 8-heart event).
    private static readonly (string Npc, int Low, int High)[] Rivals =
    {
        ("Leah", 60, 78), ("Elliott", 52, 70), ("Emily", 45, 65),
    };

    public static void Apply(Harmony harmony, IModHelper helper)
    {
        _sve = helper.ModRegistry.IsLoaded("FlashShifter.StardewValleyExpandedCP");
        harmony.Patch(AccessTools.Method(typeof(Event), nameof(Event.checkAction)),
            prefix: new HarmonyMethod(typeof(PaintingContest), nameof(CheckActionPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Event), nameof(Event.draw), new[] { typeof(SpriteBatch) }),
            postfix: new HarmonyMethod(typeof(PaintingContest), nameof(DrawPostfix)));
        helper.Events.GameLoop.UpdateTicked += (_, _) => SetUpEasel();
        helper.Events.GameLoop.DayStarted += (_, _) => ReturnStrayEntry(Game1.player);
    }

    public static Inventory Entry(Farmer who) => Game1.player.team.GetOrCreateGlobalInventory(EntryPrefix + who.UniqueMultiplayerID);

    public static Item? EntryOf(Farmer who) => Entry(who).FirstOrDefault(i => i != null);

    private static bool DoneThisYear(Farmer who) => who.modData.TryGetValue(DoneKey, out string y) && y == Game1.year.ToString();

    // ---- Scoring ----

    // Out of 100: quality, stars, the subject's difficulty, a varnish, a Masterpiece.
    public static int Score(Item painting)
    {
        var data = PaintingData.Read(painting);
        int score = painting.Quality switch
        {
            >= PaintingRules.Iridium => 75,
            >= PaintingRules.Gold => 60,
            >= PaintingRules.Silver => 45,
            _ => 30,
        };
        score += (int)Math.Round((data?.Stars ?? 0f) * 3f);
        score += Math.Min(5, (data?.Difficulty ?? 0) / 20);
        score += Varnish.FinishOf(painting) switch { "gallery" => 8, "gloss" => 5, "satin" => 3, _ => 0 };
        if (PaintingData.IsMasterpiece(painting)) score += 10;
        return Math.Clamp(score, 0, 100);
    }

    // The villagers' entries this year: the same for the whole Fair.
    public static List<(string Npc, int Score)> RivalScores(Farmer who)
    {
        var random = Utility.CreateRandom(Game1.uniqueIDForThisGame, Game1.year, 1616);
        bool evelyn = who.eventsSeen.Contains(ModEntry.Id + "_Sable8h");
        return Rivals.Select(r => (evelyn && r.Npc == "Elliott" ? "Evelyn" : r.Npc, random.Next(r.Low, r.High + 1))).ToList();
    }

    // 1 for first, up to 4 for last. Ties go to the player.
    public static int Placing(int score, IEnumerable<int> rivals) => 1 + rivals.Count(r => r > score);

    // ---- At the Fair ----

    private static bool CheckActionPrefix(Event __instance, xTile.Dimensions.Location tileLocation, Farmer who, ref bool __result)
    {
        if (!__instance.isFestival || !__instance.isSpecificFestival(FestivalId) || __instance.getActorByName(Julian.Name) is not { } julian)
            return true;
        var at = julian.TilePoint;
        if (at.X != tileLocation.X || (at.Y != tileLocation.Y && at.Y != tileLocation.Y + 1))
            return true;
        Talk(__instance, julian, who);
        __result = true;
        return false;
    }

    private static void Talk(Event fair, NPC julian, Farmer who)
    {
        if (DoneThisYear(who))
            Say(julian, I18n.Get("contest.after"));
        else if (fair.grangeJudged)
            Result(julian, who);
        else if (EntryOf(who) is { } entry)
            Say(julian, I18n.Get("contest.entered", new { painting = entry.DisplayName }));
        else if (!who.Items.Any(PaintingItems.IsPainting))
            Say(julian, I18n.Get("contest.no-painting"));
        else
        {
            Say(julian, I18n.Get("contest.invite"));
            Game1.afterDialogues = () => Game1.activeClickableMenu = new ContestMenu(who, julian);
        }
    }

    private static void Say(NPC julian, string text)
    {
        julian.CurrentDialogue.Clear();
        julian.CurrentDialogue.Push(new Dialogue(julian, null, text));
        Game1.drawDialogue(julian);
    }

    // Enters a painting from the bag.
    public static void Enter(Farmer who, Item painting)
    {
        who.removeItemFromInventory(painting);
        Entry(who).Clear();
        Entry(who).Add(painting);
        Game1.playSound("newArtifact");
    }

    // After the judging: the placing, the tokens, a ribbon, the painting back.
    public static (int Placing, int Score, List<(string Npc, int Score)> Rivals)? Judge(Farmer who)
    {
        if (EntryOf(who) is not { } entry) return null;
        int score = Score(entry);
        var rivals = RivalScores(who);
        int placing = Placing(score, rivals.Select(r => r.Score));
        who.festivalScore += Prizes[placing - 1];
        if (placing <= 3)
            who.addItemByMenuIfNecessary(ItemRegistry.Create($"(F){ModEntry.Id}_Ribbon{placing}"));
        Entry(who).Clear();
        who.addItemByMenuIfNecessary(entry);
        who.modData[DoneKey] = Game1.year.ToString();
        return (placing, score, rivals);
    }

    private static void Result(NPC julian, Farmer who)
    {
        if (Judge(who) is not { } result)
        {
            Say(julian, I18n.Get("contest.no-entry"));
            who.modData[DoneKey] = Game1.year.ToString();
            return;
        }
        var winner = result.Rivals.OrderByDescending(r => r.Score).First();
        Game1.playSound(result.Placing == 1 ? "reward" : "newArtifact");
        Say(julian, I18n.Get("contest.result." + result.Placing, new
        {
            score = result.Score,
            tokens = Prizes[result.Placing - 1],
            rival = NPC.GetDisplayName(winner.Npc),
        }));
    }

    // A painting entered in a Fair the game closed during goes back to its painter.
    private static void ReturnStrayEntry(Farmer who)
    {
        if (EntryOf(who) is { } entry)
        {
            Entry(who).Clear();
            who.addItemByMenuIfNecessary(entry);
        }
    }

    // ---- Julian's easel: your entry on it, for the day ----

    // The easel (and the villagers' entries) are the Fair's props, so they're
    // solid and sort with everyone walking past.
    private static Event? _easelFor;

    private static void SetUpEasel()
    {
        if (Game1.CurrentEvent is not { isFestival: true } fair || !fair.isSpecificFestival(FestivalId) || _easelFor == fair)
            return;
        _easelFor = fair;
        var easel = Game1.content.Load<Texture2D>(ArtItems.BigCraftablesTexture);
        fair.festivalProps.Add(new Prop(easel, 0, 1, 1, 2, EaselTile.X, EaselTile.Y));
        // Leaning against the tree beside it, the villagers' entries: drawn
        // below (a prop would sort level with the trunk and go behind it), so
        // their prop is a blank one, only there to make them solid.
        if (_blank == null)
        {
            // (Cleared explicitly: a new texture's pixels aren't always clear,
            // and some players saw a black block under the entries.)
            _blank = new Texture2D(Game1.graphics.GraphicsDevice, 32, 32);
            _blank.SetData(new Color[32 * 32]);
        }
        fair.festivalProps.Add(new Prop(_blank, 0, 2, 1, 2, EntriesTile.X, EntriesTile.Y));
    }

    private static Texture2D? _blank;

    // The entries' sprite in JulianItems, and how far from their tiles
    // they're drawn (in sprite pixels): up against the trunk, the tallest
    // leaning on it.
    private static readonly Rectangle EntriesSource = new(48, 16, 32, 32);
    private static Point EntriesNudge => _sve ? new(4, -6) : new(-6, -9);

    // The villagers' entries, just in front of the tree; and your entry on
    // the easel, just in front of the easel.
    private static void DrawPostfix(Event __instance, SpriteBatch b)
    {
        if (__instance != _easelFor) return;
        var items = Game1.content.Load<Texture2D>($"Mods/{ModEntry.Id}/JulianItems");
        var stack = Game1.GlobalToLocal(Game1.viewport, new Vector2(EntriesTile.X * 64 + EntriesNudge.X * 4, (EntriesTile.Y - 1) * 64 + EntriesNudge.Y * 4));
        b.Draw(items, stack, EntriesSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (EntriesTile.Y * 64 + 68) / 10000f);     // (just in front of the trunk, which sorts at its tile's bottom + 1 or 2)

        if (EntryOf(Game1.player) is not { } entry || PaintingRenderer.TextureFor(entry) is not { } picture)
            return;
        var at = Game1.GlobalToLocal(Game1.viewport, new Vector2(EaselTile.X * 64, (EaselTile.Y - 1) * 64));
        b.Draw(picture, new Rectangle((int)at.X + 8, (int)at.Y + 12, 48, 48), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, EaselTile.Y * 64 / 10000f + 0.0001f);
    }
}

// Picking a painting to enter: your bag, with the paintings lit.
internal sealed class ContestMenu : PaintingPickerMenu
{
    public ContestMenu(Farmer who, NPC julian)
        : base(I18n.Get("contest.pick"), PaintingItems.IsPainting, item =>
        {
            PaintingContest.Enter(who, item);
            julian.CurrentDialogue.Clear();
            julian.CurrentDialogue.Push(new Dialogue(julian, null, I18n.Get("contest.thanks", new { painting = item.DisplayName })));
            Game1.drawDialogue(julian);
        }, item => I18n.Get("contest.pick-hover", new { painting = item.DisplayName }))
    {
    }
}
