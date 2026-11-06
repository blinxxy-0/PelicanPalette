using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ArtStudio.Social;

// Margot's study board (1.3): Painting Mastery, made visible. The morning
// after you reach Painting 10 (and she's in the valley), Margot writes:
// your hand's ready, your eye isn't yet; keep painting, and come to her when
// her old study board is full. The board hangs on a wall and fills with
// pinned studies as the mastery bar does (a quarter at a time), is full when
// she's ready to test you, and gets her certificate pinned over it when you
// pass. Right-click it, or hover it in the bag, for the numbers.
//
// Talking to her, she notices each quarter once (her line comes before her
// one for the day), and with the bar full she offers the test straight away
// at the museum (elsewhere, she tells you to come and find her there).
internal static class StudyBoard
{
    public const string Id = ModEntry.Id + "_StudyBoard";
    public const string QualifiedId = "(F)" + Id;
    public const string LetterId = ModEntry.Id + "_MargotMastery";
    // If yours goes missing (sold, trashed, lost), she posts another.
    public const string AgainLetterId = ModEntry.Id + "_MargotBoardAgain";
    private const string AgainKey = ModEntry.Id + "/StudyBoardAgainDay";
    private const int AgainEveryDays = 7;
    // Furniture sheet row 4 (tools/art/study_board.py): six 2x2 stages side by side.
    public const int Sprite = 320;
    public const int Stages = 6;
    private const string RemarkKey = ModEntry.Id + "/MasteryRemark";

    public static void Register(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            prefix: new HarmonyMethod(typeof(StudyBoard), nameof(DrawPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.drawInMenu),
                new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(StudyBoard), nameof(DrawInMenuPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.getDescription)),
            postfix: new HarmonyMethod(typeof(StudyBoard), nameof(DescriptionPostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.checkForAction)),
            prefix: new HarmonyMethod(typeof(StudyBoard), nameof(CheckForActionPrefix)));
        helper.Events.GameLoop.DayStarted += (_, _) =>
        {
            SendLetterIfDue(Game1.player);
            SendAgainIfLost(Game1.player);
        };
    }

    public static bool IsBoard(Item? item) => item?.QualifiedItemId == QualifiedId;

    // 0 bare, 1-3 a quarter more each, 4 full (ready for her test), 5 with her certificate.
    public static int Stage(Farmer who) =>
        ArtProgress.HasMastery(who) ? 5
        : ArtProgress.MasteryReady(who) ? 4
        : ArtProgress.Level(who) < PaintingRules.MaxLevel ? 0
        : Math.Clamp(ArtProgress.MasteryXp(who) * 4 / ArtProgress.MasteryXpNeeded, 0, 3);

    // Which quarter of the bar is filled (1-4; 0 before the first, or once a conservator).
    public static int Milestone(Farmer who) =>
        ArtProgress.HasMastery(who) || ArtProgress.Level(who) < PaintingRules.MaxLevel ? 0
        : Math.Clamp(ArtProgress.MasteryXp(who) * 4 / ArtProgress.MasteryXpNeeded, 0, 4);

    public static Rectangle SourceRect(int stage)
    {
        int index = Sprite + 2 * Math.Clamp(stage, 0, Stages - 1);
        return new Rectangle(index % 80 * 16, index / 80 * 16, 32, 32);
    }

    // Name/type/tilesheet size/bounding box/rotations/price/placement/display name/sprite/texture/off-limits
    public static string FurnitureData(string texture) =>
        $"{Id}/painting/2 2/2 2/1/500/-1/{I18n.Get("item.study-board")}/{Sprite}/{texture}/true";

    // ---- Margot's letter ----

    // (Not before you've met her: reaching 10 before she came, the letter
    // waits for the morning after you first talk to her, so it never comes
    // from a stranger. Her test waits for it in turn: MargotMastery.CanOffer.)
    public static bool LetterDue(Farmer who) =>
        ArtProgress.Level(who) >= PaintingRules.MaxLevel && Margot.Arrived && who.friendshipData.ContainsKey(Margot.Name) && !ArtProgress.HasMastery(who)
        && !who.mailReceived.Contains(LetterId) && !who.mailbox.Contains(LetterId) && !who.mailForTomorrow.Contains(LetterId);

    internal static void SendLetterIfDue(Farmer who)
    {
        if (Context.IsWorldReady && LetterDue(who))
            who.mailbox.Add(LetterId);
    }

    // A board anywhere: in a bag, hung or set down anywhere, in any chest.
    public static bool BoardExists()
    {
        bool found = false;
        Utility.ForEachItem(item =>
        {
            found |= IsBoard(item);
            return !found;
        });
        return found;
    }

    // Lost it (it's nowhere, and it isn't waiting in the post): she posts
    // another, at most once a week, so one you got rid of on purpose doesn't
    // come back every morning.
    public static bool AgainDue(Farmer who) =>
        who.mailReceived.Contains(LetterId) && !who.mailbox.Contains(LetterId)
        && !who.mailbox.Contains(AgainLetterId) && !who.mailForTomorrow.Contains(AgainLetterId)
        && (!who.modData.TryGetValue(AgainKey, out string raw) || !int.TryParse(raw, out int last) || Game1.Date.TotalDays - last >= AgainEveryDays)
        && !BoardExists();

    internal static void SendAgainIfLost(Farmer who)
    {
        if (!Context.IsWorldReady || !AgainDue(who)) return;
        who.mailReceived.Remove(AgainLetterId);
        who.mailbox.Add(AgainLetterId);
        who.modData[AgainKey] = Game1.Date.TotalDays.ToString();
    }

    // ---- Her remarks ----

    public static int Remarked(Farmer who) =>
        who.modData.TryGetValue(RemarkKey, out string raw) && int.TryParse(raw, out int m) ? m : 0;

    public static void SetRemarked(Farmer who, int milestone) => who.modData[RemarkKey] = milestone.ToString();

    // A quarter she hasn't mentioned yet: her line goes on top of the day's,
    // and the game shows it as it would any (a full bar at the museum is
    // the test offer instead, MargotMastery).
    public static bool TryRemark(NPC margot, Farmer who, bool atMuseum)
    {
        if (!who.mailReceived.Contains(LetterId)) return false;
        int milestone = Milestone(who);
        if (milestone <= Remarked(who) || (milestone == 4 && atMuseum)) return false;
        SetRemarked(who, milestone);
        margot.CurrentDialogue.Push(new Dialogue(margot, null, I18n.Get("margot.mastery." + milestone * 25)));
        return true;
    }

    // ---- Drawing ----

    private static Texture2D Texture => Game1.content.Load<Texture2D>(ArtItems.FurnitureTexture);

    private static bool DrawPrefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha, Netcode.NetVector2 ___drawPosition)
    {
        if (!IsBoard(__instance)) return true;
        if (__instance.isTemporarilyInvisible) return false;
        try
        {
            var source = SourceRect(Stage(Game1.player));
            Vector2 position = Furniture.isDrawingLocationFurniture
                ? Game1.GlobalToLocal(Game1.viewport, ___drawPosition.Value)
                : Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (source.Height * 4 - __instance.boundingBox.Height)));
            spriteBatch.Draw(Texture, position, source, Color.White * alpha, 0f, Vector2.Zero, 4f,
                __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (__instance.boundingBox.Value.Bottom - 8) / 10000f);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static readonly Func<Furniture, float> GetScaleSize =
        AccessTools.MethodDelegate<Func<Furniture, float>>(AccessTools.Method(typeof(Furniture), "getScaleSize"));

    private static bool DrawInMenuPrefix(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color)
    {
        if (!IsBoard(__instance)) return true;
        try
        {
            var source = SourceRect(Stage(Game1.player));
            spriteBatch.Draw(Texture, location + new Vector2(32f, 32f), source, color * transparency, 0f,
                new Vector2(source.Width / 2, source.Height / 2), GetScaleSize(__instance) * scaleSize, SpriteEffects.None, layerDepth);
            __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    // ---- What it tells you ----

    public static string Status(Farmer who)
    {
        int stage = Stage(who);
        string text = I18n.Get("study-board.stage." + stage);
        if (stage < 4)
            text += " " + I18n.Get("skill.mastery.progress", new { xp = ArtProgress.MasteryXp(who), needed = ArtProgress.MasteryXpNeeded });
        return text;
    }

    private static void DescriptionPostfix(Furniture __instance, ref string __result)
    {
        if (IsBoard(__instance))
            __result = Game1.parseText(I18n.Get("item.study-board.description") + "\n" + Status(Game1.player), Game1.smallFont, 272);
    }

    private static bool CheckForActionPrefix(Furniture __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!IsBoard(__instance) || __instance.Location == null) return true;
        __result = true;
        if (!justCheckingForActivity && who.IsLocalPlayer)
            Game1.drawObjectDialogue(Status(who));
        return false;
    }
}
