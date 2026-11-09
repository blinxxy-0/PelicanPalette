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

    // Bea in her own portrait if she's in the world; otherwise the same words without the codes.
    private static void BeaSays(string text)
    {
        if (Game1.getCharacterFromName(ValeResidents.Bea) is { } bea)
            Game1.DrawDialogue(new Dialogue(bea, null, text));
        else
            Game1.drawObjectDialogue(Regex.Replace(text, @"\$[a-z0-9]", "").Split("#$b#").ToList());
    }

    // Using the guide (right-click with it held). Before the last find, anywhere: its page, the eight
    // spots found so far. After it, in the Vale: the question about stepping in (yes: GildedGrove,
    // once the box has closed). After it, elsewhere:
    // the page says it only comes alive in the Vale, and that's false (nothing was done). False and
    // nothing shown, too, when the player isn't free or a splash is playing.
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
        if (here?.Name != ValeBus.Vale)
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
    public static void Patch(HarmonyLib.Harmony harmony)
    {
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

    // Right-click with the guide held. Facing a spot's stool, a person, an object, furniture or
    // anything with a tile action is the game's own click, left alone; anywhere else it opens the guide.
    public static void Register(IModHelper helper) => helper.Events.Input.ButtonPressed += (_, e) =>
    {
        if (!Context.IsPlayerFree || !e.Button.IsActionButton() || Game1.player.CurrentItem?.QualifiedItemId != "(O)" + GuideItemId)
            return;
        if (!ClickOpensGuide(Game1.currentLocation, e.Cursor.GrabTile))
            return;
        helper.Input.Suppress(e.Button);
        UseGuide(Game1.player);
    };

    // False where the game has something of its own to do with a right-click on that tile.
    public static bool ClickOpensGuide(GameLocation location, Vector2 tile)
    {
        int x = (int)tile.X, y = (int)tile.Y;
        if (location.doesTileHaveProperty(x, y, "Action", "Buildings") != null)
            return false;
        // An NPC stands two tiles tall: a click on their head is on the tile above their feet.
        if (location.isCharacterAtTile(tile) != null || location.isCharacterAtTile(tile + new Vector2(0, 1)) != null)
            return false;
        if (location.getObjectAtTile(x, y) != null || location.GetFurnitureAt(tile) != null)
            return false;
        return true;
    }

    // The tile action: blinxxy.MuseArt_PaintSpot <spotId>. An old tile with no id does nothing.
    public static bool OnTouch(Farmer who, string[] args)
    {
        if (args.Length < 2 || !Spots.Any(s => s.Id == args[1]))
            return false;
        if (PaintSplash.Playing)
            return true;
        Touch(who, args[1]);
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
                    () => Game1.drawObjectDialogue(Text("vale.guide.last_page").Split("#$b#").ToList()));
                break;
        }
    }

    // The strings this code shows (all in the dev pack's ValeStrings); the checks resolve each one.
    public static IEnumerable<string> StringKeys =>
        Spots.SelectMany(s => new[] { $"vale.guide.spot.{s.Id}.name", $"vale.guide.spot.{s.Id}.line" })
            .Concat(new[] { "vale.guide.plain_stool", "vale.guide.last_page", "vale.guide.board", "vale.guide.take", "vale.guide.again",
                "vale.guide.page.title", "vale.guide.page.unfound", "vale.guide.step_in", "vale.guide.not_here",
                "quest.painters_guide.title", "quest.painters_guide.description", "quest.painters_guide.objective", "vale.guide.item.description" })
            .Concat(GildedGrove.StringKeys);

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
