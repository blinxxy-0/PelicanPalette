using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace ArtStudio.Social;

// Margot's test restoration (1.3, restoration_mechanics.md §1, the approved
// scene in docs/margot_events.md): with Painting at 10 and the mastery bar
// full, talk to her at the museum and she sets you one of her old paintings.
// Restore it to gold or better and she certifies you a conservator: Painting
// Mastery (restoring your own finds), the vanilla mastery sound and sparkle,
// and the Master's Easel. Below gold, try again tomorrow. The painting is
// hers (the city's), so she keeps it either way.
internal static class MargotMastery
{
    public const string Location = "ArchaeologyHouse";
    private const string TriedKey = ModEntry.Id + "/MargotTestDay";
    private const string TestKey = ModEntry.Id + "/MargotTest";
    private const string DeclinedKey = ModEntry.Id + "/MargotTestDeclined";
    private static OldPainting? _testing;

    public static void Register(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(MargotMastery), nameof(CheckActionPrefix)));
        EaselMenu.PaintingFinished += OnPaintingFinished;
        // (And if the game closed mid-test, she has it back by morning.)
        helper.Events.GameLoop.DayStarted += (_, _) => TakeBackTestPainting(Game1.player);
        helper.Events.Display.MenuChanged += (_, e) =>
        {
            if (e.OldMenu is EaselMenu && e.NewMenu is not EaselMenu)
                TakeBackTestPainting(Game1.player);
        };
    }

    // (Only once she's written with her study board: introduction, board, test.)
    public static bool CanOffer(Farmer who) =>
        ArtProgress.MasteryReady(who) && !ArtProgress.HasMastery(who) && who.mailReceived.Contains(StudyBoard.LetterId)
        && (!who.modData.TryGetValue(TriedKey, out string day) || day != Game1.Date.TotalDays.ToString());

    // "Not today" holds for the day, so she has her usual line after.
    private static bool Declined(Farmer who) =>
        who.modData.TryGetValue(DeclinedKey, out string day) && day == Game1.Date.TotalDays.ToString();

    private static bool CheckActionPrefix(NPC __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (__instance.Name != Margot.Name || !who.IsLocalPlayer || Game1.eventUp) return true;
            bool atMuseum = __instance.currentLocation?.Name == Location;
            // Holding out a damaged painting is always about restoring it
            // (her fee service), before her line for the day, which stays.
            if (who.ActiveObject is { } held && OldPaintings.IsDamaged(held))
            {
                if (atMuseum ? !MargotService.TryHandle(__instance, who) : ArtProgress.HasMastery(who))
                    return true;
                if (!atMuseum)
                    Say(I18n.Get("margot.service.not-here"));
                __result = true;
                return false;
            }
            // The test, as soon as the bar's full: before her line for the day
            // (StudyBoard: her study board's full).
            if (atMuseum && CanOffer(who) && !Declined(who) && who.ActiveObject == null)
            {
                __result = true;
                StudyBoard.SetRemarked(who, 4);
                Offer(__instance, who);
                return false;
            }
            // A quarter more of the bar she hasn't mentioned: the game shows her line.
            if (who.ActiveObject == null && StudyBoard.TryRemark(__instance, who, atMuseum))
                return true;
            // At home the first time, the window box you chose.
            if (who.ActiveObject == null && Margot.TryWindowBoxLine(__instance, who))
                return true;
            if (!atMuseum || __instance.CurrentDialogue.Count > 0)
                return true;
            // A restoration of hers in progress, or posted.
            if (MargotService.TryHandle(__instance, who))
            {
                __result = true;
                return false;
            }
            return true;
        }
        catch (Exception)
        {
            return true;
        }
    }

    // She says it (with her portraits), then the question box asks; after
    // the box she was speaking in has closed, so her line for the day stays
    // under it.
    private static void Offer(NPC margot, Farmer who)
    {
        var offer = new Dialogue(margot, null, I18n.Get("margot.test.offer"));
        offer.onFinish = () => DelayedAction.functionAfterDelay(() =>
            who.currentLocation.createQuestionDialogue(I18n.Get("margot.test.ask"), new[]
            {
                new Response("yes", I18n.Get("margot.test.yes")),
                new Response("no", I18n.Get("margot.test.no")),
            }, (_, answer) =>
            {
                if (answer == "yes") DelayedAction.functionAfterDelay(() => AskMode(who), 50);
                else who.modData[DeclinedKey] = Game1.Date.TotalDays.ToString();
            }), 50);
        margot.CurrentDialogue.Push(offer);
        Game1.drawDialogue(margot);
    }

    // Which mode to paint it in (as the easel's setup screen asks, which the
    // test skips): the one you have, or another, saved as the easel's would be.
    private static void AskMode(Farmer who)
    {
        var current = ModEntry.Config.Difficulty;
        var answers = Difficulties.Order.Select(d => new Response(d.ToString(),
            I18n.Get("setup.mode." + d.ToString().ToLowerInvariant()) + (d == current ? " " + I18n.Get("margot.test.mode-current") : ""))).ToArray();
        who.currentLocation.createQuestionDialogue(I18n.Get("margot.test.mode"), answers, (_, answer) =>
        {
            if (Enum.TryParse<Difficulty>(answer, out var mode) && mode != ModEntry.Config.Difficulty)
            {
                ModEntry.Config.Difficulty = mode;
                ModEntry.SaveConfig();
            }
            DelayedAction.functionAfterDelay(() => StartTest(who), 50);
        });
    }

    // Always the Zuzu City Museum of Art's own painting, lent to her for
    // testing conservators (OldPaintings.Zuzu): never one of the valley's.
    internal static OldPainting TestPainting(Farmer who) => OldPaintings.Zuzu;

    internal static EaselMenu StartTest(Farmer who)
    {
        var painting = TestPainting(who);
        who.modData[TriedKey] = Game1.Date.TotalDays.ToString();
        var damaged = OldPaintings.CreateDamaged(painting);
        damaged.modData[TestKey] = "1";
        who.addItemToInventory(damaged);
        _testing = painting;
        var easel = EaselMenu.StartStillLife(who, painting.Size > 28 ? CanvasKind.Large : CanvasKind.Medium,
            new[] { OldPaintings.SubjectFor(painting.Id) });
        Game1.activeClickableMenu = easel;
        return easel;
    }

    private static void OnPaintingFinished(Farmer who, Item painting, OldPainting? restored)
    {
        if (_testing == null || restored?.Id != _testing.Id) return;
        _testing = null;
        painting.modData[TestKey] = "1";
        bool passed = painting.Quality >= PaintingRules.Gold;
        DelayedAction.functionAfterDelay(() => Verdict(who, passed), 400);
    }

    private static void Verdict(Farmer who, bool passed)
    {
        if (!passed)
        {
            Say(I18n.Get("margot.test.fail"));
            return;
        }
        Pass(who);
        Say(I18n.Get("margot.test.pass"));
    }

    internal static void Pass(Farmer who)
    {
        if (!who.mailReceived.Contains(OldPaintings.MasteryFlag))
            who.mailReceived.Add(OldPaintings.MasteryFlag);
        // As the Mastery Cave's pedestals: the sound and the sparkle.
        Game1.playSound("cowboy_monsterhit");
        DelayedAction.playSoundAfterDelay("cowboy_monsterhit", 200);
        Utility.addRainbowStarExplosion(who.currentLocation, who.Position + new Vector2(0, -64), 12);
        var easel = ItemRegistry.Create("(BC)" + ArtItems.MastersEaselId);
        if (!who.addItemToInventoryBool(easel))
            Game1.createItemDebris(easel, who.getStandingPosition(), who.FacingDirection, who.currentLocation);
    }

    // She keeps the painting (the city's), restored or not.
    internal static void TakeBackTestPainting(Farmer who)
    {
        foreach (var item in who.Items.Where(i => i != null && (i.modData.ContainsKey(TestKey) || OldPaintings.IsZuzu(OldPaintings.Of(i))
            || PaintingData.Read(i) is { } d && OldPaintings.IsZuzu(OldPaintings.OfSubject(d.Subject)))).ToList())
            who.removeItemFromInventory(item);
        _testing = null;
    }

    private static void Say(string text)
    {
        if (Margot.Npc is not { } margot) return;
        margot.CurrentDialogue.Push(new Dialogue(margot, null, text));
        Game1.drawDialogue(margot);
    }

    // For the dev test.
    internal static void DevReset(Farmer who)
    {
        who.modData.Remove(TriedKey);
        who.modData.Remove(DeclinedKey);
        _testing = null;
    }
}
