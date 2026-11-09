#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;

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
        // The quest itself comes with the guide item (a later task); until then there's
        // nothing to complete.
        if (who.hasQuest(QuestId))
            who.completeQuest(QuestId);
        return TouchResult.Completed;
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
                Game1.drawObjectDialogue(Text("vale.guide.plain_stool", "The stool's seat is worn smooth. Someone sat here for years, looking at something."));
                break;
            case TouchResult.FirstFind:
                PaintSplash.Play(spot.Palette, false, () => Game1.drawObjectDialogue(new List<string>
                {
                    Text($"vale.guide.spot.{id}.name", id),
                    Text($"vale.guide.spot.{id}.line", ""),
                }));
                break;
            case TouchResult.Again:
                Game1.drawObjectDialogue(Text($"vale.guide.spot.{id}.line", ""));
                break;
            case TouchResult.Completed:
                PaintSplash.Play(Spots.SelectMany(s => s.Palette).ToArray(), true,
                    () => Game1.drawObjectDialogue(Text("vale.guide.last_page", "").Split("#$b#").ToList()));
                break;
        }
    }

    private static string Text(string key, string fallback)
    {
        try
        {
            var strings = Game1.content.Load<Dictionary<string, string>>("Mods/" + ModEntry.Id + "/ValeStrings");
            return strings.TryGetValue(key, out string? text) && !string.IsNullOrWhiteSpace(text) ? text : fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
#endif
