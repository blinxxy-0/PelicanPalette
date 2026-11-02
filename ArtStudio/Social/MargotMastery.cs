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
    private static OldPainting? _testing;

    public static void Register(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(MargotMastery), nameof(CheckActionPrefix)));
        EaselMenu.PaintingFinished += OnPaintingFinished;
        helper.Events.Display.MenuChanged += (_, e) =>
        {
            if (e.OldMenu is EaselMenu && e.NewMenu is not EaselMenu)
                TakeBackTestPainting(Game1.player);
        };
    }

    public static bool CanOffer(Farmer who) =>
        ArtProgress.MasteryReady(who) && !ArtProgress.HasMastery(who)
        && (!who.modData.TryGetValue(TriedKey, out string day) || day != Game1.Date.TotalDays.ToString());

    private static bool CheckActionPrefix(NPC __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (__instance.Name != Margot.Name || __instance.currentLocation?.Name != Location || __instance.CurrentDialogue.Count > 0)
                return true;
            // Her restoring for a fee comes first (a painting to hand her, or to collect).
            if (MargotService.TryHandle(__instance, who))
            {
                __result = true;
                return false;
            }
            if (!CanOffer(who) || who.ActiveObject != null)
                return true;
            __result = true;
            who.currentLocation.createQuestionDialogue(I18n.Get("margot.test.offer"), new[]
            {
                new Response("yes", I18n.Get("margot.test.yes")),
                new Response("no", I18n.Get("margot.test.no")),
            }, (_, answer) => { if (answer == "yes") StartTest(who); });
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    // One of her old paintings: one you haven't found, so it's new to you
    // (any, if you've found them all), damaged as it was found.
    internal static OldPainting TestPainting(Farmer who)
    {
        var found = OldPaintings.Found(who);
        bool Held(OldPainting p) => who.Items.Any(i => i != null && OldPaintings.Of(i)?.Id == p.Id);
        return OldPaintings.All.FirstOrDefault(p => !found.Contains(p.Id) && !Held(p))
            ?? OldPaintings.All.First(p => !Held(p));
    }

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
        foreach (var item in who.Items.Where(i => i != null && i.modData.ContainsKey(TestKey)).ToList())
            who.removeItemFromInventory(item);
        _testing = null;
    }

    private static void Say(string text)
    {
        if (Margot.Npc is not { } margot) return;
        margot.CurrentDialogue.Clear();
        margot.CurrentDialogue.Push(new Dialogue(margot, null, text));
        Game1.drawDialogue(margot);
    }

    // For the dev test.
    internal static void DevReset(Farmer who)
    {
        who.modData.Remove(TriedKey);
        _testing = null;
    }
}
