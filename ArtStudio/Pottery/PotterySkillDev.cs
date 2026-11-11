#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Pottery;

// Dev commands for the Pottery skill (PotterySkill.cs):
//   museart_potteryskill [level <n> | xp <n> | prof <key> [off] | reset]
//   museart_potteryskilltest   throws, glazes and fires with the bots: XP rises,
//                              and the professions change the rules
//   museart_snappotteryskill   the level-up screens and the skills page at both
//                              UI sizes, to dialogue_snapshots/potteryskill_*.png
internal static class PotterySkillDev
{
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_potteryskill", "The Pottery skill (1.5): prints level, XP and professions. Usage: museart_potteryskill [level <0-10> | xp <n> | prof <Thrower|Glazer|ProductionPotter|MasterOfSets|KilnKeeper|Alchemist> [off] | reset]", (_, args) =>
            Command(monitor, args));
        DevTools.AddCommand(helper, "museart_potteryskilltest", "Checks the Pottery skill: registration, XP from a bot's throw, glaze and firing, and each profession's effect on the rules.", (_, _) =>
            Test(helper, monitor));
        DevTools.AddCommand(helper, "museart_snappotteryskill", "Renders the Pottery level-up screens and the skills page at 1280x720 and 1920x1080 to dialogue_snapshots/potteryskill_*.png.", (_, _) =>
            Snap(helper, monitor));
    }

    private static void Command(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady || PotterySkill.Instance == null)
        {
            monitor.Log("Load a save first (and SpaceCore must be installed).", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        switch (args.FirstOrDefault())
        {
            case "level" when args.Length > 1 && int.TryParse(args[1], out int level):
                SetLevel(who, level);
                break;
            case "xp" when args.Length > 1 && int.TryParse(args[1], out int xp):
                Skills.AddExperience(who, PotterySkill.SkillId, xp);
                break;
            case "prof" when args.Length > 1:
                if (!SetProfession(who, args[1], args.ElementAtOrDefault(2) != "off"))
                    monitor.Log($"No profession '{args[1]}'. Keys: {string.Join(", ", PotterySkill.ProfessionKeys)}", LogLevel.Warn);
                break;
            case "reset":
                Reset(who);
                break;
        }
        monitor.Log(Describe(who), LogLevel.Info);
    }

    internal static string Describe(Farmer who)
    {
        var has = PotterySkill.ProfessionKeys.Where(k => PotterySkill.Has(who, k)).ToList();
        var normal = PotterySkill.RulesFor(who, PotteryRules.For("normal"));
        return $"Pottery: level {PotterySkill.Level(who)}, {PotterySkill.Xp(who)} XP; professions: {(has.Count > 0 ? string.Join(", ", has) : "none")}; "
            + $"price x{PotterySkill.PriceMultiplier(who):0.00}; Normal's rules for you: drain {normal.DrainMultiplier:0.00}, drift {normal.Drift:0.00}, kiln band {normal.KilnBand:0.0}, gift odds x{normal.GiftOdds:0.0}";
    }

    internal static void SetLevel(Farmer who, int level)
    {
        int target = level <= 0 ? 0 : PotterySkill.XpCurve[Math.Min(level, PotterySkill.MaxLevel) - 1];
        Skills.AddExperience(who, PotterySkill.SkillId, target - PotterySkill.Xp(who));
    }

    internal static bool SetProfession(Farmer who, string key, bool on)
    {
        var p = PotterySkill.Profession(key);
        if (p == null)
            return false;
        int id = p.GetVanillaId();
        if (on && !who.professions.Contains(id))
            who.professions.Add(id);
        if (!on)
            who.professions.Remove(id);
        return true;
    }

    internal static void Reset(Farmer who)
    {
        foreach (string key in PotterySkill.ProfessionKeys)
            SetProfession(who, key, false);
        SetLevel(who, 0);
    }

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static T Field<T>(object o, string name) => Traverse.Create(o).Field(name).GetValue<T>();

    private static void Test(IModHelper helper, IMonitor monitor)
    {
        if (!Context.IsWorldReady)
            return;
        var who = Game1.player;
        var skill = PotterySkill.Instance;
        Check(monitor, "the Pottery skill is registered with SpaceCore", skill != null && Skills.GetSkill(PotterySkill.SkillId) == skill);
        if (skill == null)
            return;
        Check(monitor, "it's called Pottery, with its own name and hover text", skill.GetName() == "Pottery" && !skill.GetSkillPageHoverText(1).StartsWith("pottery."), skill.GetName());
        Check(monitor, "six professions, all named and described",
            skill.Professions.Count == 6 && skill.Professions.All(p => p.GetName().Length > 0 && !p.GetName().StartsWith("pottery.") && !p.GetDescription().Contains("{{") && !p.GetDescription().StartsWith("pottery.")),
            string.Join("; ", skill.Professions.Select(p => $"{p.GetName()}: {p.GetDescription()}")));
        var pairs = skill.ProfessionsForLevels;
        Check(monitor, "Thrower and Glazer at 5; Production Potter / Master of Sets under Thrower, Kiln Keeper / Alchemist under Glazer",
            pairs.Count == 3 && pairs[0].Level == 5 && pairs[0].First.Id == "Thrower" && pairs[0].Second.Id == "Glazer"
            && pairs[1].Level == 10 && pairs[1].Requires.Id == "Thrower" && pairs[1].First.Id == "ProductionPotter" && pairs[1].Second.Id == "MasterOfSets"
            && pairs[2].Level == 10 && pairs[2].Requires.Id == "Glazer" && pairs[2].First.Id == "KilnKeeper" && pairs[2].Second.Id == "Alchemist");
        Check(monitor, "its curve is Painting's, 15000 XP to level 10", skill.ExperienceCurve.Length == 10 && skill.ExperienceCurve[9] == 15000);

        Reset(who);
        Check(monitor, "a reset farmer is level 0 with no professions", PotterySkill.Level(who) == 0 && PotterySkill.Xp(who) == 0 && PotterySkill.ProfessionKeys.All(k => !PotterySkill.Has(who, k)), Describe(who));

        // ---- XP, by how well it went ----
        var normal = PotteryRules.For("normal");
        Check(monitor, "a better throw gives more XP",
            PotterySkill.ThrowXp(5f, normal) > PotterySkill.ThrowXp(4f, normal) && PotterySkill.ThrowXp(4f, normal) > PotterySkill.ThrowXp(2f, normal) && PotterySkill.ThrowXp(0f, normal) > 0,
            $"0: {PotterySkill.ThrowXp(0f, normal)}, 2: {PotterySkill.ThrowXp(2f, normal)}, 4: {PotterySkill.ThrowXp(4f, normal)}, 5: {PotterySkill.ThrowXp(5f, normal)}");
        Check(monitor, "a better glaze gives more XP",
            PotterySkill.GlazeXp(1f, normal) > PotterySkill.GlazeXp(0.6f, normal) && PotterySkill.GlazeXp(0.6f, normal) > PotterySkill.GlazeXp(0.1f, normal),
            $"0.1: {PotterySkill.GlazeXp(0.1f, normal)}, 0.6: {PotterySkill.GlazeXp(0.6f, normal)}, 1: {PotterySkill.GlazeXp(1f, normal)}");
        Check(monitor, "a better firing gives more XP, a kiln gift a little more",
            PotterySkill.FireXp(new FiringResult(1f, "Perfect", "gift"), normal) > PotterySkill.FireXp(new FiringResult(1f, "Perfect", null), normal)
            && PotterySkill.FireXp(new FiringResult(1f, "Perfect", null), normal) > PotterySkill.FireXp(new FiringResult(0.3f, "Underfired", null), normal)
            && PotterySkill.FireXp(new FiringResult(0f, "Cracked", null), normal) > 0);
        Check(monitor, "Hard is worth more", PotterySkill.ThrowXp(4f, PotteryRules.For("hard")) > PotterySkill.ThrowXp(4f, normal));

        PotteryArt.Load(helper);
        var old = Game1.activeClickableMenu;
        try
        {
            // a menu the bots finished before it opened (as museart_pottery bot does) gives nothing
            int before = PotterySkill.Xp(who);
            var botMenu = new PotteryMenu("vase", normal, bot: true);
            Check(monitor, "the bot's throw, finished before the wheel opened, gives no XP", PotterySkill.Xp(who) == before, $"{PotterySkill.Xp(who) - before}");

            // the throw: the perfect bot, then Finished, with the wheel open
            var menu = new PotteryMenu("cup", normal, skipCentring: true);
            Game1.activeClickableMenu = menu;
            PotBot.Throw(Field<ClayPot>(menu, "_pot"), Field<PotShape>(menu, "_shape"), PotHand.Perfect);
            before = PotterySkill.Xp(who);
            AccessTools.Method(typeof(PotteryMenu), "Finish").Invoke(menu, null);
            var thrown = Field<PotResult>(menu, "_result");
            int gained = PotterySkill.Xp(who) - before;
            Check(monitor, "finishing a throw gives XP for its stars", gained > 0 && gained == PotterySkill.ThrowXp(thrown.Stars, normal), $"{thrown.Stars} stars: +{gained}");

            // the glaze: a perfect hand
            menu = new PotteryMenu("vase", normal, band: true);
            Game1.activeClickableMenu = menu;
            before = PotterySkill.Xp(who);
            menu.GlazeBot(0f);
            gained = PotterySkill.Xp(who) - before;
            Check(monitor, "finishing a glaze gives XP for its score", gained > 0 && gained == PotterySkill.GlazeXp(menu.GlazeScore, normal), $"{menu.GlazeScore:0.00}: +{gained}");

            // the firing: the steady stoker
            menu = new PotteryMenu("jug", normal, fire: true);
            Game1.activeClickableMenu = menu;
            before = PotterySkill.Xp(who);
            menu.FireBot("steady");
            gained = PotterySkill.Xp(who) - before;
            var fired = Field<FiringResult>(menu, "_fireResult");
            Check(monitor, "finishing a firing gives XP for its score", gained > 0 && gained == PotterySkill.FireXp(fired, normal), $"{fired.Verdict} {fired.Score:0.00}{(fired.Gift != null ? " + gift" : "")}: +{gained}");
            Check(monitor, "a whole good pot is about a painting's XP (40 to 60)", PotterySkill.Xp(who) is >= 40 and <= 60, $"{PotterySkill.Xp(who)}");

            // ---- the professions, on the rules ----
            var hard = PotteryRules.For("hard");
            foreach (string key in new[] { "Glazer", "ProductionPotter", "KilnKeeper" })
                SetProfession(who, key, false);
            Check(monitor, "without professions, the rules are the difficulty's own", PotterySkill.RulesFor(who, normal) == normal && PotterySkill.RulesFor(who, hard) == hard);

            SetProfession(who, "Glazer", true);
            var glazer = PotterySkill.RulesFor(who, normal);
            Check(monitor, "Glazer: the brush hand drifts 25% less", Has(who, "Glazer") && Near(glazer.Drift, normal.Drift * 0.75f) && glazer with { Drift = normal.Drift } == normal, $"{normal.Drift} -> {glazer.Drift}");
            float plain = 0f, steadier = 0f;
            for (int k = 0; k < 20; k++)
            {
                var a = new PotteryMenu(PotShape.Names[k % 4], normal, band: true);
                a.GlazeBot(normal.Drift);
                plain += a.GlazeScore / 20f;
                var g = new PotteryMenu(PotShape.Names[k % 4], glazer, band: true);
                g.GlazeBot(glazer.Drift);
                steadier += g.GlazeScore / 20f;
            }
            Check(monitor, "and a hand that never corrects glazes better with it", steadier > plain, $"{plain:0.00} -> {steadier:0.00}");
            Traverse.Create(typeof(DevTools)).Field("Commands").GetValue<Dictionary<string, Action<string, string[]>>>()["museart_pottery"]("museart_pottery", new[] { "vase", "normal" });
            var opened = Game1.activeClickableMenu is PotteryMenu pm ? Field<PotteryRules>(pm, "_rules") : null;
            Check(monitor, "museart_pottery opens the wheel with the farmer's rules", opened != null && Near(opened.Drift, glazer.Drift), $"drift {opened?.Drift}");
            SetProfession(who, "Glazer", false);

            SetProfession(who, "ProductionPotter", true);
            var production = PotterySkill.RulesFor(who, normal);
            Check(monitor, "Production Potter: patience drains 20% slower (lasts 25% longer)", Near(production.DrainMultiplier, 0.8f) && Near(PotterySkill.RulesFor(who, hard).DrainMultiplier, 2.4f),
                $"normal {production.DrainMultiplier:0.00}, hard {PotterySkill.RulesFor(who, hard).DrainMultiplier:0.00}");
            var p1 = new ClayPot(normal);
            var p2 = new ClayPot(production);
            for (int k = 0; k < 40; k++)
            {
                p1.Work(ThrowMove.PullUp, 1f, 0.1f);
                p2.Work(ThrowMove.PullUp, 1f, 0.1f);
            }
            Check(monitor, "and 4 s of pulling leaves the clay more patience", p2.Patience > p1.Patience, $"{p1.Patience:0.00} -> {p2.Patience:0.00}");
            Check(monitor, "and a small lump makes two cups (for clay items)", PotterySkill.CupsPerSmallLump(who) == 2);
            SetProfession(who, "ProductionPotter", false);

            SetProfession(who, "KilnKeeper", true);
            var keeper = PotterySkill.RulesFor(who, normal);
            Check(monitor, "Kiln Keeper: stoking's band 25% wider, gifts twice as often", Near(keeper.KilnBand, normal.KilnBand * 1.25f) && Near(keeper.GiftOdds, 2f),
                $"band {normal.KilnBand} -> {keeper.KilnBand}, odds {keeper.GiftOdds}");
            var rng = new Random(7);
            Check(monitor, "and the kiln takes the band from the rules", Near(new Kiln(-1, keeper, rng).Band, 7.5f));
            int Gifts(PotteryRules rules)
            {
                int gifts = 0;
                for (int k = 0; k < 20; k++)
                {
                    var m = new PotteryMenu("cup", rules, fire: true);
                    m.FireBot("steady", finish: false);
                    var kiln = Field<Kiln>(m, "_kiln");
                    for (int j = 0; j < 20; j++)
                        if (FiringJudge.Judge(kiln, rng) is { Gift: not null })
                            gifts++;
                }
                return gifts;
            }
            int usual = Gifts(PotteryRules.For("simplified")), twice = Gifts(PotteryRules.For("simplified") with { GiftOdds = keeper.GiftOdds });
            Check(monitor, "and many more kiln gifts come from the same firings", twice > usual * 1.5f, $"{usual} -> {twice} in 400");
            SetProfession(who, "KilnKeeper", false);

            SetProfession(who, "Thrower", true);
            Check(monitor, "Thrower: pots would sell for 25% more (for pot items)", Near(PotterySkill.PriceMultiplier(who), 1.25f));
            Check(monitor, "Thrower doesn't change how the wheel plays", PotterySkill.RulesFor(who, normal) == normal);
            SetProfession(who, "Thrower", false);
            Check(monitor, "without Thrower, the price is the usual", Near(PotterySkill.PriceMultiplier(who), 1f));

            // ---- levels ----
            SetLevel(who, 5);
            Check(monitor, "2150 XP is level 5", PotterySkill.Level(who) == 5, $"{PotterySkill.Xp(who)} XP, level {PotterySkill.Level(who)}");
            SetLevel(who, 10);
            Check(monitor, "15000 XP is level 10", PotterySkill.Level(who) == 10, $"{PotterySkill.Xp(who)} XP, level {PotterySkill.Level(who)}");
        }
        finally
        {
            Game1.activeClickableMenu = old;
            Reset(who);
            // the levels this test crossed shouldn't queue level-up screens for tonight
            Traverse.Create(typeof(Skills)).Property("NewLevels").GetValue<System.Collections.IList>()?.Clear();
        }
        monitor.Log("Pottery skill test done; " + Describe(who), LogLevel.Info);
    }

    private static bool Has(Farmer who, string key) => PotterySkill.Has(who, key);

    private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.001f;

    // The level-up screens (a plain level, the level-5 choice, both level-10 choices)
    // and SpaceCore's skills page, scrolled to the custom skills, at both UI sizes.
    private static void Snap(IModHelper helper, IMonitor monitor)
    {
        if (!Context.IsWorldReady || PotterySkill.Instance == null)
            return;
        var who = Game1.player;
        string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
        Directory.CreateDirectory(dir);
        var professions = who.professions.ToList();
        int xp = PotterySkill.Xp(who);
        var screens = new (string Name, string? Has, int Level)[]
        {
            ("levelup_3", null, 3),
            ("levelup_5", null, 5),
            ("levelup_10_thrower", "Thrower", 10),
            ("levelup_10_glazer", "Glazer", 10),
        };
        try
        {
            SetLevel(who, 7);
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
            {
                foreach (var (name, has, level) in screens)
                    DevTools.AtViewport(w, h, () =>
                    {
                        if (has != null)
                            SetProfession(who, has, true);
                        var menu = new SpaceCore.Interface.SkillLevelUpMenu(PotterySkill.SkillId, level);
                        // (the screen waits a moment before it draws its contents)
                        for (int k = 0; k < 60; k++)
                            menu.update(new GameTime(TimeSpan.FromSeconds(k / 60.0), TimeSpan.FromSeconds(1 / 60.0)));
                        DevTools.Render(menu, Path.Combine(dir, $"potteryskill_{name}_{w}.png"));
                        if (has != null)
                            SetProfession(who, has, false);
                    });
                DevTools.AtViewport(w, h, () =>
                {
                    var game = new GameMenu(GameMenu.skillsTab);
                    var page = game.pages[GameMenu.skillsTab];
                    // SpaceCore swaps its scrolling page in once the menu opens
                    var type = AccessTools.TypeByName("SpaceCore.Interface.NewSkillsPage");
                    if (type != null)
                    {
                        var ctor = AccessTools.Constructor(type, new[] { typeof(int), typeof(int), typeof(int), typeof(int) });
                        page = (IClickableMenu)ctor.Invoke(new object[] { page.xPositionOnScreen, page.yPositionOnScreen, page.width, page.height });
                        game.pages[GameMenu.skillsTab] = page;
                        for (int k = 0; k < 4; k++)
                            Traverse.Create(page).Method("scrollSkillsDown").GetValue();
                    }
                    DevTools.Render(game, Path.Combine(dir, $"potteryskill_page_{w}.png"));
                });
            }
        }
        finally
        {
            foreach (string key in PotterySkill.ProfessionKeys)
                SetProfession(who, key, false);
            foreach (int id in professions.Where(id => !who.professions.Contains(id)))
                who.professions.Add(id);
            Skills.AddExperience(who, PotterySkill.SkillId, xp - PotterySkill.Xp(who));
            Traverse.Create(typeof(Skills)).Property("NewLevels").GetValue<System.Collections.IList>()?.Clear();
        }
        monitor.Log($"Pottery skill screens rendered to {dir} (potteryskill_*.png). Hover text: {PotterySkill.Instance.GetSkillPageHoverText(7)}", LogLevel.Info);
    }
}
#endif
