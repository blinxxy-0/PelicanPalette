#if DEBUG
using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;

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

            var bad = PaintersGuide.StringKeys.Where(k => { string t = PaintersGuide.Text(k); return t.Length == 0 || t == k; }).ToList();
            Check(monitor, "guide: every guide string resolves", bad.Count == 0,
                bad.Count == 0 ? $"{PaintersGuide.StringKeys.Count()} strings" : "missing: " + string.Join(", ", bad));
        }
        finally
        {
            Clear();
            foreach (string f in had) who.mailReceived.Add(f);
        }
    }
}
#endif
