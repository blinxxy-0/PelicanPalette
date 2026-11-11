#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Pottery;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace ArtStudio.Vale;

// Potter's Clay, Tobin's clay pit and his teaching wheel (1.5's pottery, in development;
// docs/design/pottery_minigame.md, section 8). The item, the pit's and the wheel's tiles
// and their texts are the dev pack's (data/vale_clay.json).
//
// The pit: hoe its clay (tiles marked "Action blinxxy.MuseArt_ClayPit" on the Buildings
// layer) for a lump or two each dig, at a cost in energy, sometimes with a pebble, an old
// shard or a buried artifact. So many digs a day, more after rain, for everyone together
// (kept in the location's modData, which every player shares); frozen in winter.
// Right-click it for Tobin's note and how much is left today.
//
// The wheel: right-click the teaching wheel to throw a cup or a bowl (a lump) or a vase or
// a jug (two lumps; the section's small and medium). The clay is taken before the wheel
// opens (PotteryMenu), and not enough clay is a message, not a menu.
//
// museart_claytest checks it all.
internal static class ValeClay
{
    public const string ItemId = ModEntry.Id + "_PottersClay";
    public const string QualifiedItemId = "(O)" + ItemId;
    public const string PitAction = ModEntry.Id + "_ClayPit";
    public const string WheelAction = ModEntry.Id + "_PotteryWheel";
    private const string DugKey = ModEntry.Id + "_ClayPitDug";       // "<Game1.Date.TotalDays>:<digs>"

    // ---- The numbers ----
    public const int Digs = 6, RainDigs = 9;   // a day's digs; after rain (or in it), more
    public const float DigStamina = 4f;         // a hoe's swing on a field costs 2
    public const double TwoLumps = 0.4;         // else one
    public const double PebbleChance = 0.12, ShardChance = 0.05, ArtifactChance = 0.02;
    public const string Pebble = "(O)390";      // Stone
    public const string Shard = "(O)100";       // Chipped Amphora: an old shard of someone's pot
    // the buried artifacts a riverbank might give up: Arrowhead, Ancient Doll, Rusty Spoon, Bone Flute
    public static readonly string[] Artifacts = { "(O)101", "(O)103", "(O)110", "(O)119" };

    // the shapes the wheel throws, and the lumps each takes (section 8: small 1, medium 2)
    public static readonly (string Shape, int Lumps)[] Shapes = { ("cup", 1), ("bowl", 1), ("vase", 2), ("jug", 2) };

    private const string StringsAsset = "Mods/" + ModEntry.Id + "/ValeStrings";
    private static IModHelper _helper = null!;

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        _helper = helper;
        harmony.Patch(AccessTools.Method(typeof(Hoe), nameof(Hoe.DoFunction)),
            prefix: new HarmonyMethod(typeof(ValeClay), nameof(HoePrefix)));
        GameLocation.RegisterTileAction(PitAction, (location, _, _, _) =>
        {
            Game1.drawObjectDialogue(S("clay.pit.note", "Tobin's clay pit. 'Take what you need. Fill the holes. -T'") + " " + PitState(location));
            return true;
        });
        GameLocation.RegisterTileAction(WheelAction, (location, _, who, _) =>
        {
            Wheel(location, who);
            return true;
        });
        DevTools.AddCommand(helper, "museart_claytest",
            "Checks Potter's Clay: the item, Tobin's shop, the clay pit (a dig, the day's limit, the refill, rain, winter) and the teaching wheel. "
            + "'museart_claytest dig' digs the pit tile in front of you (for screenshots); 'museart_claytest wheel' gives 3 lumps and uses the teaching wheel.",
            (_, args) =>
            {
                if (!Context.IsWorldReady)
                    return;
                var who = Game1.player;
                if (args.FirstOrDefault() == "dig")
                    Dig(who.currentLocation, who, who.GetGrabTile().ToPoint(), null);
                else if (args.FirstOrDefault() == "tub")
                {
                    // three Slaking Tubs in a row in front of you, for screenshots: idle, slaking, ready
                    var start = who.GetGrabTile();
                    for (int k = 0; k < 3; k++)
                    {
                        var tile = start + new Vector2(k * 2 - 2, 0);
                        who.currentLocation.objects.Remove(tile);
                        var tub = ItemRegistry.Create<StardewValley.Object>(ClayTests.QualifiedTubId);
                        tub.placementAction(who.currentLocation, (int)tile.X * 64, (int)tile.Y * 64, who);
                        if (k > 0 && who.currentLocation.objects.TryGetValue(tile, out var placed))
                        {
                            placed.performObjectDropInAction(ItemRegistry.Create("(O)330", 3), false, who);
                            if (k == 2)
                                placed.minutesElapsed(placed.MinutesUntilReady);
                        }
                    }
                }
                else if (args.FirstOrDefault() == "wheel")
                {
                    who.addItemToInventory(ItemRegistry.Create(QualifiedItemId, 3));
                    Wheel(who.currentLocation, who);
                }
                else
                    ClayTests.Run(monitor);
            });
    }

    // ---- The pit ----

    public static bool IsPit(GameLocation location, Point tile) =>
        location.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings") is { } action
        && (action == PitAction || action.StartsWith(PitAction + " "));

    public static bool Frozen(GameLocation location) => location.GetSeason() == Season.Winter;

    public static int DigsToday(GameLocation location) =>
        Game1.wasRainingYesterday || location.IsRainingHere() ? RainDigs : Digs;

    public static int Dug(GameLocation location)
    {
        if (!location.modData.TryGetValue(DugKey, out string? value))
            return 0;
        string[] parts = value.Split(':');
        return parts.Length == 2 && int.TryParse(parts[0], out int day) && day == Game1.Date.TotalDays && int.TryParse(parts[1], out int dug)
            ? dug
            : 0;   // dug on another day: it's refilled overnight
    }

    public static void SetDug(GameLocation location, int dug) => location.modData[DugKey] = $"{Game1.Date.TotalDays}:{dug}";

    public static int DigsLeft(GameLocation location) => Math.Max(0, DigsToday(location) - Dug(location));

    private static string PitState(GameLocation location) =>
        Frozen(location) ? S("clay.pit.frozen", "The pit's frozen hard.")
        : DigsLeft(location) > 0 ? string.Format(S("clay.pit.left", "Digs left today: {0}."), DigsLeft(location))
        : S("clay.pit.dug_out", "The pit's dug out for today.");

    // A hoe on the pit digs it (one dig a swing, however far it's charged), instead of tilling.
    private static bool HoePrefix(Hoe __instance, GameLocation location, int x, int y, Farmer who)
    {
        var tile = new Point(x / 64, y / 64);
        if (location is null || !IsPit(location, tile))
            return true;
        who.stopJittering();
        Dig(location, who, tile, __instance);
        return false;
    }

    public enum Outcome { Dug, DugOut, Frozen }

    public static Outcome Dig(GameLocation location, Farmer who, Point tile, Tool? tool, Random? random = null)
    {
        if (Frozen(location))
        {
            location.playSound("hammer", tile.ToVector2());
            if (who.IsLocalPlayer)
                Game1.drawObjectDialogue(S("clay.pit.frozen", "The pit's frozen hard. Nothing will dig it till spring."));
            return Outcome.Frozen;
        }
        int left = DigsLeft(location);
        if (left <= 0)
        {
            if (who.IsLocalPlayer)
                Game1.drawObjectDialogue(S("clay.pit.dug_out", "The pit's dug out for today. It'll seep back overnight."));
            return Outcome.DugOut;
        }
        SetDug(location, Dug(location) + 1);
        if (tool is not { isEfficient.Value: true })
            who.Stamina -= DigStamina;
        location.playSound("hoeHit", tile.ToVector2());
        location.temporarySprites.Add(new TemporaryAnimatedSprite(12, tile.ToVector2() * 64f, Color.White, 8, Game1.random.Next(2) == 0, 50f));
        var from = tile.ToVector2() * 64f + new Vector2(32f, 32f);
        foreach (var item in Finds(random ?? Game1.random))
            Game1.createItemDebris(item, from, (who.FacingDirection + 2) % 4, location);
        if (left == 1 && who.IsLocalPlayer)
            Game1.addHUDMessage(HUDMessage.ForCornerTextbox(S("clay.pit.last", "That's the pit dug out for today.")));
        return Outcome.Dug;
    }

    // What one dig turns up: a lump or two, and now and then something else.
    public static List<Item> Finds(Random random)
    {
        var found = new List<Item> { ItemRegistry.Create(QualifiedItemId, random.NextDouble() < TwoLumps ? 2 : 1) };
        double roll = random.NextDouble();
        if (roll < PebbleChance)
            found.Add(ItemRegistry.Create(Pebble));
        else if (roll < PebbleChance + ShardChance)
            found.Add(ItemRegistry.Create(Shard));
        else if (roll < PebbleChance + ShardChance + ArtifactChance)
            found.Add(ItemRegistry.Create(Artifacts[random.Next(Artifacts.Length)]));
        return found;
    }

    // ---- The teaching wheel ----

    // ---- THE GATE (Tobin's 2-heart event, docs/design/pottery_minigame.md section 8) ----
    // He shows you the wheel at 2 hearts. When that event is written, return here whether
    // `who` has seen it (e.g. who.eventsSeen.Contains(ModEntry.Id + "_TobinHeart2")) and give
    // the locked wheel a line in Wheel below. Until then it's open to anyone.
    public static bool WheelOpenTo(Farmer who) => true;

    public static int LumpsFor(string shape) => Shapes.FirstOrDefault(s => s.Shape == shape).Lumps is var n and > 0 ? n : 1;

    private static void Wheel(GameLocation location, Farmer who)
    {
        if (!WheelOpenTo(who))
            return;   // (the gate's line goes here)
        int have = who.Items.CountId(QualifiedItemId);
        if (have == 0)
        {
            Game1.drawObjectDialogue(who.Items.ContainsId("(O)330")
                ? S("clay.wheel.field_clay", "Not field clay: it's grit and roots. Potter's clay. -T")
                : S("clay.wheel.none", "The teaching wheel. 'Potter's clay. Buy it at the counter, or dig it at the pit. -T'"));
            return;
        }
        var answers = Shapes
            .Select(s => new Response(s.Shape, string.Format(S("clay.wheel.shape", "{0} ({1})"), S("clay.wheel." + s.Shape, s.Shape), Lumps(s.Lumps))))
            .Append(new Response("Leave", S("clay.wheel.leave", "Not now")))
            .ToArray();
        location.createQuestionDialogue(string.Format(S("clay.wheel.ask", "You have {0} of Potter's Clay. What will you throw?"), Lumps(have)), answers, (farmer, answer) =>
        {
            if (answer != "Leave")
                Throw(farmer, answer);
        });
    }

    // Takes the shape's clay and opens the wheel; not enough clay, a message and no wheel.
    public static bool Throw(Farmer who, string shape)
    {
        int need = LumpsFor(shape), have = who.Items.CountId(QualifiedItemId);
        if (have < need)
        {
            Game1.drawObjectDialogue(string.Format(S("clay.wheel.short", "{0} takes {1} of Potter's Clay. You have {2}."), S("clay.wheel." + shape, shape), Lumps(need), Lumps(have)));
            return false;
        }
        who.Items.ReduceId(QualifiedItemId, need);
        PotteryArt.Load(_helper);
        Game1.activeClickableMenu = new PotteryMenu(shape, PotteryRules.For(Rules()));
        return true;
    }

    // the wheel's difficulty follows the easel's setting
    private static string Rules() => ModEntry.Config.Difficulty switch
    {
        Difficulty.Simplified => "simplified",
        Difficulty.Hard => "hard",
        _ => "normal",
    };

    private static string Lumps(int n) => string.Format(n == 1 ? S("clay.wheel.lump", "{0} lump") : S("clay.wheel.lumps", "{0} lumps"), n);

    internal static string S(string key, string fallback)
    {
        try
        {
            var strings = Game1.content.Load<Dictionary<string, string>>(StringsAsset);
            return strings.TryGetValue(key, out string? text) && !string.IsNullOrWhiteSpace(text) ? text : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
#endif
