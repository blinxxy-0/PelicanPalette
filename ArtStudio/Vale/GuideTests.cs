#if DEBUG
using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using SObject = StardewValley.Object;

namespace ArtStudio.Vale;

// museart_guidetest: checks the painters' guide's rules in a running game (Debug only):
// the spots are found only with the guide, once each, and the eighth opens the Grove.
// The player's flags are saved and put back, so a run changes nothing.
internal static class GuideTests
{
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_guidetest",
            "Checks the painters' guide's rules: no guide no find, a first find, a repeat, the eighth completes, an unknown id is refused.",
            (_, _) => Run(monitor));
    }

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static void Run(IMonitor monitor)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        var ours = PaintersGuide.Spots.Select(s => PaintersGuide.FoundFlag(s.Id))
            .Append(PaintersGuide.GuideTakenFlag).Append(PaintersGuide.GroveOpenFlag).ToList();
        var had = ours.Where(who.mailReceived.Contains).ToHashSet();
        void Clear() { foreach (string f in ours) who.mailReceived.Remove(f); }

        // The guide item and its quest, put back as they were (the bag's slots too).
        string itemId = "(O)" + PaintersGuide.GuideItemId;
        int Held() => who.Items.Where(i => i?.QualifiedItemId == itemId).Sum(i => i.Stack);
        void DropGuides() { for (int i = 0; i < who.Items.Count; i++) if (who.Items[i]?.QualifiedItemId == itemId) who.Items[i] = null; }
        Quest? Ours() => who.questLog.FirstOrDefault(q => q.id.Value == PaintersGuide.QuestId);
        int Quests() => who.questLog.Count(q => q.id.Value == PaintersGuide.QuestId);
        var heldBefore = who.Items.Select((it, slot) => (it, slot)).Where(x => x.it?.QualifiedItemId == itemId).ToList();
        var questBefore = who.questLog.Where(q => q.id.Value == PaintersGuide.QuestId).ToList();
        var place = who.currentLocation;
        var menuBefore = Game1.activeClickableMenu;
        // Whatever box a use opened is closed again, so the player counts as free for the next one.
        void Settle() { Game1.activeClickableMenu = menuBefore; Game1.dialogueUp = false; who.CanMove = true; }
        try
        {
            Clear();
            var r = PaintersGuide.Touch(who, "rooftops", false);
            Check(monitor, "guide: no guide, no find", r == PaintersGuide.TouchResult.NoGuide && PaintersGuide.FoundCount(who) == 0,
                $"{r}, found {PaintersGuide.FoundCount(who)}");

            who.mailReceived.Add(PaintersGuide.GuideTakenFlag);
            r = PaintersGuide.Touch(who, "rooftops", false);
            Check(monitor, "guide: first find", r == PaintersGuide.TouchResult.FirstFind && PaintersGuide.FoundCount(who) == 1,
                $"{r}, found {PaintersGuide.FoundCount(who)}");

            r = PaintersGuide.Touch(who, "rooftops", false);
            Check(monitor, "guide: a second touch doesn't count", r == PaintersGuide.TouchResult.Again && PaintersGuide.FoundCount(who) == 1,
                $"{r}, found {PaintersGuide.FoundCount(who)}");

            var results = PaintersGuide.Spots.Skip(1).Select(s => PaintersGuide.Touch(who, s.Id, false)).ToList();
            bool shape = results.Take(6).All(x => x == PaintersGuide.TouchResult.FirstFind) && results[6] == PaintersGuide.TouchResult.Completed;
            Check(monitor, "guide: the eighth completes",
                shape && who.mailReceived.Contains(PaintersGuide.GroveOpenFlag) && PaintersGuide.FoundCount(who) == 8,
                $"{string.Join(",", results)}, grove {who.mailReceived.Contains(PaintersGuide.GroveOpenFlag)}, found {PaintersGuide.FoundCount(who)}");

            bool threw = false;
            try { PaintersGuide.Touch(who, "nowhere", false); }
            catch (ArgumentException) { threw = true; }
            Check(monitor, "guide: an unknown id is refused", threw);

            // The tile action: an old tile with no id, or an unknown id, does nothing and says false.
            bool noId = PaintersGuide.OnTouch(who, new[] { GildedVale.PaintSpotAction });
            bool unknownId = PaintersGuide.OnTouch(who, new[] { GildedVale.PaintSpotAction, "nowhere" });
            Check(monitor, "guide: the action with no id does nothing", !noId && PaintersGuide.FoundCount(who) == 8, $"returned {noId}");
            Check(monitor, "guide: the action with an unknown id does nothing", !unknownId, $"returned {unknownId}");

            // The guide item, its quest, and using it.
            Clear();
            DropGuides();
            foreach (var q in questBefore) who.questLog.Remove(q);

            PaintersGuide.TakeGuide(who, false);
            Check(monitor, "guide: taking it",
                who.mailReceived.Contains(PaintersGuide.GuideTakenFlag) && Held() == 1 && Quests() == 1,
                $"flag {who.mailReceived.Contains(PaintersGuide.GuideTakenFlag)}, held {Held()}, quests {Quests()}");

            DropGuides();
            int foundBefore = PaintersGuide.FoundCount(who);
            PaintersGuide.TakeGuide(who, false);
            Check(monitor, "guide: a lost guide comes back", Held() == 1 && Quests() == 1 && PaintersGuide.FoundCount(who) == foundBefore,
                $"held {Held()}, quests {Quests()}, found {PaintersGuide.FoundCount(who)}");
            PaintersGuide.TakeGuide(who, false);
            Check(monitor, "guide: a second one isn't given while you hold one", Held() == 1 && Quests() == 1, $"held {Held()}, quests {Quests()}");

            PaintersGuide.Touch(who, "rooftops", false);
            string objective = Ours()?.currentObjective ?? "(no quest)";
            Check(monitor, "guide: the objective counts", objective.Contains('1'), objective);

            var item = ItemRegistry.Create(itemId);
            Check(monitor, "guide: it can't be given, trashed or sold",
                !item.canBeTrashed() && !item.canBeDropped() && !item.canBeShipped() && !(item is SObject o && o.canBeGivenAsGift()),
                $"trash {item.canBeTrashed()}, drop {item.canBeDropped()}, ship {item.canBeShipped()}, gift {(item as SObject)?.canBeGivenAsGift()}");

            who.mailReceived.Add(PaintersGuide.GroveOpenFlag);
            var farm = Game1.getLocationFromName("Farm");
            who.currentLocation = farm;
            bool usedOutside = PaintersGuide.UseGuide(who);
            bool stayed = Game1.currentLocation == place && who.currentLocation == farm;
            Settle();
            who.currentLocation = place;
            Check(monitor, "guide: outside the Vale", !usedOutside && stayed, $"returned {usedOutside}");

            who.currentLocation = Game1.getLocationFromName(ValeBus.Vale);
            Game1.activeClickableMenu = new DialogueBox("busy");
            bool usedBusy = PaintersGuide.UseGuide(who);
            bool menuKept = Game1.activeClickableMenu is DialogueBox { isQuestion: false };
            Settle();
            who.currentLocation = place;
            Check(monitor, "guide: not while busy", !usedBusy && menuKept, $"returned {usedBusy}");

            var vale = Game1.getLocationFromName(ValeBus.Vale);
            who.currentLocation = vale;
            bool usedInVale = PaintersGuide.UseGuide(who);
            bool asked = Game1.activeClickableMenu is DialogueBox { isQuestion: true };
            Settle();
            vale.afterQuestion = null;
            who.currentLocation = place;
            Check(monitor, "guide: in the Vale, open", usedInVale && asked, $"returned {usedInVale}, asked {asked}");

            var bad = PaintersGuide.StringKeys.Where(k => { string t = PaintersGuide.Text(k); return t.Length == 0 || t == k; }).ToList();
            Check(monitor, "guide: every guide string resolves", bad.Count == 0,
                bad.Count == 0 ? $"{PaintersGuide.StringKeys.Count()} strings" : "missing: " + string.Join(", ", bad));
        }
        finally
        {
            who.currentLocation = place;
            Settle();
            Clear();
            foreach (string f in had) who.mailReceived.Add(f);
            DropGuides();
            foreach (var (it, slot) in heldBefore) who.Items[slot] = it;
            foreach (var q in who.questLog.Where(q => q.id.Value == PaintersGuide.QuestId).ToList()) who.questLog.Remove(q);
            foreach (var q in questBefore) who.questLog.Add(q);
        }
    }
}
#endif
