#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace ArtStudio.Vale;

// The old painters' guide's eight spots (the Vale painters' guide design, section 4):
// touch a stool with the guide and the view is found, once. Progress is per player,
// kept as mailReceived flags. The rules live in Touch; what the player sees and hears
// (the splash, the boxes) is OnTouch, so the checks can run the rules alone.
internal static class PaintersGuide
{
    public sealed record Spot(string Id, Color[] Palette);

    // The spec's table, in the guide's order: three to five colours each.
    public static readonly Spot[] Spots =
    {
        new("rooftops", new[] { new Color(176, 64, 52), new Color(204, 104, 72), new Color(140, 52, 48), new Color(150, 148, 146) }),
        new("northlight", new[] { new Color(240, 196, 44), new Color(252, 222, 96), new Color(120, 176, 224), new Color(184, 216, 240) }),
        new("wildflowers", new[] { new Color(88, 156, 68), new Color(140, 196, 90), new Color(216, 60, 48), new Color(92, 128, 220) }),
        new("lakewindow", new[] { new Color(52, 108, 176), new Color(104, 164, 214), new Color(180, 214, 232), new Color(240, 200, 120) }),
        new("parterre", new[] { new Color(64, 116, 72), new Color(104, 152, 92), new Color(164, 160, 148), new Color(208, 112, 128) }),
        new("falls", new[] { new Color(246, 250, 252), new Color(200, 224, 232), new Color(92, 148, 84), new Color(56, 104, 76) }),
        new("tree", new[] { new Color(236, 180, 40), new Color(252, 216, 96), new Color(200, 140, 32), new Color(255, 238, 168) }),
        new("stage", new[] { new Color(150, 146, 136), new Color(190, 184, 170), new Color(84, 128, 72), new Color(116, 156, 88) }),
    };

    public const string GuideItemId = ModEntry.Id + "_PaintersGuide";
    public const string QuestId = ModEntry.Id + "_PaintersGuide";
    public const string GuideTakenFlag = ModEntry.Id + "_GuideTaken";
    public const string GroveOpenFlag = ModEntry.Id + "_GroveOpen";

    public static string FoundFlag(string spotId) => $"{ModEntry.Id}_SpotFound_{spotId}";

    public enum TouchResult { NoGuide, FirstFind, Again, Completed }

    public static int FoundCount(Farmer who) => Spots.Count(s => who.mailReceived.Contains(FoundFlag(s.Id)));

    // The rules only. effects:false is for the checks: no splash, no boxes.
    public static TouchResult Touch(Farmer who, string spotId, bool effects = true)
    {
        var spot = Spots.FirstOrDefault(s => s.Id == spotId)
            ?? throw new ArgumentException($"No painting spot called '{spotId}'.", nameof(spotId));
        TouchResult result = Rules(who, spot);
        if (effects)
            Show(result, spot);
        return result;
    }

    private static TouchResult Rules(Farmer who, Spot spot)
    {
        if (!who.mailReceived.Contains(GuideTakenFlag))
            return TouchResult.NoGuide;
        if (who.mailReceived.Contains(FoundFlag(spot.Id)))
            return TouchResult.Again;

        who.mailReceived.Add(FoundFlag(spot.Id));
        if (FoundCount(who) < Spots.Length)
            return TouchResult.FirstFind;

        who.mailReceived.Add(GroveOpenFlag);
        if (who.hasQuest(QuestId))
            who.completeQuest(QuestId);
        return TouchResult.Completed;
    }

    // The quest log's line: "3 of 8 spots found". Quest.currentObjective is read back from
    // Data/Quests every time (setting it does nothing for a data quest), so the game's own getter
    // is patched to answer for this quest: the string carries [found], from the pack's i18n arg.
    private static void ObjectivePostfix(Quest __instance, ref string __result)
    {
        if (__instance.id.Value == QuestId && Context.IsWorldReady)
            __result = Text("quest.painters_guide.objective").Replace("[found]", FoundCount(Game1.player).ToString());
    }

    private static bool Holds(Farmer who) => who.Items.Any(i => i?.QualifiedItemId == "(O)" + GuideItemId);

    // The guide's frame in the Vale House's tea room, right of the guest-book case: the wall tile in
    // front of it, which you face from the floor below (tools/build_vale_house.py, guide_frame).
    public static readonly Point BoardTile = new(13, 19);

    // The tile action: blinxxy.MuseArt_TakeGuide, on the frame by the Vale House's guest books.
    // The first time: the frame's words, Bea's, the guide in your bag and its quest. After that,
    // if the guide's gone from your bag, another copy; if you still have it, only the frame's words.
    // effects:false is for the checks: no boxes.
    public static void TakeGuide(Farmer who, bool effects = true)
    {
        bool first = !who.mailReceived.Contains(GuideTakenFlag);
        bool replace = !first && !Holds(who);
        if (first)
            who.mailReceived.Add(GuideTakenFlag);
        if (first || replace)
        {
            Item guide = ItemRegistry.Create("(O)" + GuideItemId);
            if (!who.addItemToInventoryBool(guide))
                Game1.createItemDebris(guide, who.getStandingPosition(), -1, who.currentLocation);
        }
        if (!who.hasQuest(QuestId) && !who.mailReceived.Contains(GroveOpenFlag))
        {
            who.addQuest(QuestId);
        }
        if (!effects)
            return;
        if (first)
        {
            Game1.drawObjectDialogue(Text("vale.guide.board"));
            Game1.afterDialogues = () => BeaSays(Text("vale.guide.take"));
        }
        else
            Game1.drawObjectDialogue(Text(replace ? "vale.guide.again" : "vale.guide.board"));
    }

    // The tile action's handler.
    public static bool OnTake(Farmer who)
    {
        TakeGuide(who);
        return true;
    }

    // Bea in her own portrait if she's in the world; otherwise the same words, a page at a time,
    // without the codes (asObject: the plain box even if she's about, for the checks).
    internal static void BeaSays(string text, bool asObject = false)
    {
        if (!asObject && Game1.getCharacterFromName(ValeResidents.Bea) is { } bea)
            Game1.DrawDialogue(new Dialogue(bea, null, text));
        else
            Game1.drawObjectDialogue(Pages(text));
    }

    // Text written as a villager's dialogue, as the pages of a plain box: split at its page breaks
    // first, then its portrait and other codes ($0, $u, $h...) taken out, since a plain box shows them
    // as they're written.
    internal static List<string> Pages(string text) =>
        text.Split("#$b#")
            .Select(p => Regex.Replace(p, @"\$[A-Za-z0-9]", "").Replace("#", "").Trim())
            .Where(p => p.Length > 0)
            .ToList();

    // Using the guide (right-click with it held, when the game has nothing of its own to do there:
    // PerformUseActionPrefix). Before the last find, anywhere: its page, the eight spots found so far.
    // After it, anywhere in the Vale (the outdoor map or any of its interiors, ValeBus.IsInVale): the
    // question about stepping in (yes: GildedGrove, once the box has closed). After it, in the Grove
    // itself: a line saying you're already there. After it, elsewhere: the page says it only comes
    // alive in the Vale. Those last two are false (nothing was done). False and nothing shown, too,
    // when the player isn't free or a splash is playing.
    public static bool UseGuide(Farmer who)
    {
        if (!Context.IsPlayerFree || PaintSplash.Playing)
            return false;
        GameLocation here = who.currentLocation;
        if (!who.mailReceived.Contains(GroveOpenFlag))
        {
            Game1.activeClickableMenu = new LetterViewerMenu(PageText(who));
            return true;
        }
        if (GildedGrove.InGrove(who))
        {
            Game1.drawObjectDialogue(Text("vale.guide.in_grove"));
            return false;
        }
        if (!ValeBus.IsInVale(here))
        {
            Game1.drawObjectDialogue(Text("vale.guide.not_here"));
            return false;
        }
        here.createQuestionDialogue(Text("vale.guide.step_in"), new[]
        {
            new Response("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")),
            new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")),
        }, (_, answer) =>
        {
            if (answer == "Yes")
                GildedGrove.RequestWarpIn(who);
        });
        return true;
    }

    private static string PageText(Farmer who) =>
        Text("vale.guide.page.title") + "^^" + string.Join("^", Spots.Select(s => who.mailReceived.Contains(FoundFlag(s.Id))
            ? "[x] " + Text($"vale.guide.spot.{s.Id}.name")
            : "[ ] " + Text("vale.guide.page.unfound")));

    // The game's own data can't say "never dropped" (CanBeTrashed and CanBeGivenAsGift are all it has),
    // so the guide's canBeDropped is false here: toss it from the bag and it stays in your hands.
    // Right-click is the game's own Object.performUseAction (PerformUseActionPrefix), and the guide
    // is never used up by it (ReduceActiveItemPrefix).
    public static void Patch(HarmonyLib.Harmony harmony)
    {
        harmony.Patch(HarmonyLib.AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction), new[] { typeof(GameLocation) }),
            prefix: new HarmonyLib.HarmonyMethod(typeof(PaintersGuide), nameof(PerformUseActionPrefix)));
        harmony.Patch(HarmonyLib.AccessTools.Method(typeof(Farmer), nameof(Farmer.reduceActiveItemByOne)),
            prefix: new HarmonyLib.HarmonyMethod(typeof(PaintersGuide), nameof(ReduceActiveItemPrefix)));
        harmony.Patch(HarmonyLib.AccessTools.Method(typeof(Item), nameof(Item.canBeDropped)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(PaintersGuide), nameof(CanBeDroppedPostfix)));
        harmony.Patch(HarmonyLib.AccessTools.PropertyGetter(typeof(Quest), nameof(Quest.currentObjective)),
            postfix: new HarmonyLib.HarmonyMethod(typeof(PaintersGuide), nameof(ObjectivePostfix)));
    }

    private static void CanBeDroppedPostfix(Item __instance, ref bool __result)
    {
        if (__instance.QualifiedItemId == "(O)" + GuideItemId)
            __result = false;
    }

    // Right-click with the guide held, the game's way. Game1.pressActionButton first tries the tile
    // you're facing or pointing at (tryToCheckAt: a person, a pet, a farm animal, another farmer, a
    // building's door, a placed object, furniture, a crop, a tree, a bush, a tile action...), and only
    // when nothing there takes the click, and the player can move, does it call the held object's
    // performUseAction: that's where the guide opens. The game's own performUseAction does nothing for
    // it, so it's skipped. True whenever the guide showed something (its page, the question, or one of
    // the lines that say it does nothing here): the press was the guide's, and the game goes no further
    // (with false it would try the tiles around for something else to do, under the open box).
    private static bool PerformUseActionPrefix(StardewValley.Object __instance, ref bool __result)
    {
        if (__instance.QualifiedItemId != "(O)" + GuideItemId)
            return true;
        bool used = UseGuide(Game1.player);
        __result = used || Game1.activeClickableMenu != null;
        return false;
    }

    // pressActionButton takes one away from any object whose use says true (a totem, a book): the
    // guide is never used up.
    private static bool ReduceActiveItemPrefix(Farmer __instance) =>
        __instance.CurrentItem?.QualifiedItemId != "(O)" + GuideItemId;

    // Lupini's own spot, the one you stopped at on his 4-heart walk (tools/build_lupini_events.py): a
    // paint rock like the guide's, by Bea's washing line, the bus's turning circle or Tobin's clay
    // pit. It isn't one of the guide's eight and never counts towards them: touching it says what he
    // saw there (vale.lupini_spot.<id>), guide or no guide.
    public static readonly string[] ChosenSpots = { "washing", "bus", "claypit" };

    // The tile action: blinxxy.MuseArt_PaintSpot <spotId>. An old tile with no id does nothing.
    public static bool OnTouch(Farmer who, string[] args)
    {
        if (args.Length < 2 || !Spots.Any(s => s.Id == args[1]))
            return args.Length >= 2 && TouchChosen(args[1]);
        if (PaintSplash.Playing)
            return true;
        Touch(who, args[1]);
        return true;
    }

    private static bool TouchChosen(string id)
    {
        if (!ChosenSpots.Contains(id))
            return false;
        Game1.drawObjectDialogue(Text($"vale.lupini_spot.{id}"));
        return true;
    }

    private static void Show(TouchResult result, Spot spot)
    {
        string id = spot.Id;
        switch (result)
        {
            case TouchResult.NoGuide:
                Game1.drawObjectDialogue(Text("vale.guide.plain_stool"));
                break;
            case TouchResult.FirstFind:
                PaintSplash.Play(spot.Palette, false, () => Game1.drawObjectDialogue(new List<string>
                {
                    Text($"vale.guide.spot.{id}.name"),
                    Text($"vale.guide.spot.{id}.line"),
                }));
                break;
            case TouchResult.Again:
                Game1.drawObjectDialogue(Text($"vale.guide.spot.{id}.line"));
                break;
            case TouchResult.Completed:
                PaintSplash.Play(Spots.SelectMany(s => s.Palette).ToArray(), true,
                    () => Game1.drawObjectDialogue(Pages(Text("vale.guide.last_page"))));
                break;
        }
    }

    // The strings this code shows (all in the dev pack's ValeStrings); the checks resolve each one.
    public static IEnumerable<string> StringKeys =>
        Spots.SelectMany(s => new[] { $"vale.guide.spot.{s.Id}.name", $"vale.guide.spot.{s.Id}.line" })
            .Concat(new[] { "vale.guide.plain_stool", "vale.guide.last_page", "vale.guide.board", "vale.guide.take", "vale.guide.again",
                "vale.guide.page.title", "vale.guide.page.unfound", "vale.guide.step_in", "vale.guide.not_here", "vale.guide.in_grove",
                "quest.painters_guide.title", "quest.painters_guide.description", "quest.painters_guide.objective", "vale.guide.item.description" })
            .Concat(GildedGrove.StringKeys)
            .Concat(ValeGraves.StringKeys);

    public static IMonitor? Monitor;
    private static readonly HashSet<string> Warned = new();

    // A missing asset or key shows the key itself, and says so once in the log, so broken
    // wiring is visible on screen and not an empty box.
    internal static string Text(string key)
    {
        try
        {
            var strings = Game1.content.Load<Dictionary<string, string>>("Mods/" + ModEntry.Id + "/ValeStrings");
            if (strings.TryGetValue(key, out string? text) && !string.IsNullOrWhiteSpace(text))
                return text;
            Warn(key, "has no text in Mods/" + ModEntry.Id + "/ValeStrings");
        }
        catch (Exception e)
        {
            Warn(key, "couldn't be read: " + e.Message);
        }
        return key;
    }

    private static void Warn(string key, string why)
    {
        if (Warned.Add(key))
            Monitor?.Log($"Painters' guide string '{key}' {why}.", LogLevel.Warn);
    }
}
#endif
