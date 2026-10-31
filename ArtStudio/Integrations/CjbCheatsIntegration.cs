using System;
using System.Collections;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Integrations;

// CJB Cheats Menu (1.1.4): its Skills tab gets a Painting button beside the
// vanilla skills, raising the level a step at a time with the level-up
// screen (and so the profession choice at 5 and 10), and its Professions
// list gets the six Painting professions as checkboxes. CJB lists only the
// vanilla skills and has no API for others, so this adds the rows to its
// menu after it builds the Skills tab. Everything of CJB's is reached by
// reflection, so nothing here loads without it.
internal static class CjbCheatsIntegration
{
    public const string ModId = "CJBok.CheatsMenu";

    private static IMonitor _monitor = null!;
    private static Type _buttonType = null!, _checkboxType = null!;
    private static Func<object?, object?, string> _increaseLabel = null!;
    private static Func<object?, object?, object?, string> _professionLabel = null!;
    private static Func<string> _professionsTitle = null!;

    public static void Register(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        try
        {
            var menu = AccessTools.TypeByName("CJBCheatsMenu.Framework.CheatsMenu") ?? throw new InvalidOperationException("no CheatsMenu");
            _buttonType = AccessTools.TypeByName("CJBCheatsMenu.Framework.Components.CheatButton") ?? throw new InvalidOperationException("no CheatButton");
            _checkboxType = AccessTools.TypeByName("CJBCheatsMenu.Framework.Components.CheatCheckbox") ?? throw new InvalidOperationException("no CheatCheckbox");
            var i18n = AccessTools.TypeByName("CJBCheatsMenu.I18n") ?? throw new InvalidOperationException("no I18n");
            var increase = AccessTools.Method(i18n, "Skills_IncreaseSkill");
            var profession = AccessTools.Method(i18n, "Professions_Profession");
            var title = AccessTools.Method(i18n, "Professions_Title");
            _increaseLabel = (skill, level) => (string)increase.Invoke(null, new[] { skill, level })!;
            _professionLabel = (name, skill, level) => (string)profession.Invoke(null, ProfessionArgs(profession, name, skill, level))!;
            _professionsTitle = () => (string)title.Invoke(null, null)!;
            harmony.Patch(AccessTools.Method(menu, "SetOptions"), postfix: new HarmonyMethod(typeof(CjbCheatsIntegration), nameof(SetOptionsPostfix)));
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't add the Painting skill to CJB Cheats Menu: {ex.Message}", LogLevel.Warn);
        }
    }

    // The label's parameters by name (professionName, skillName, level), in
    // whatever order CJB declares them.
    private static object?[] ProfessionArgs(System.Reflection.MethodInfo method, object? name, object? skill, object? level) =>
        method.GetParameters().Select(p => p.Name switch
        {
            "professionName" => name,
            "skillName" => skill,
            _ => level,
        }).ToArray();

    private static void SetOptionsPostfix(IClickableMenu __instance)
    {
        try
        {
            if (Traverse.Create(__instance).Property("CurrentTab").GetValue()?.ToString() != "Skills")
                return;
            var options = Traverse.Create(__instance).Field("Options").GetValue<IList>();
            int slotWidth = Traverse.Create(__instance).Field("Cheats").Property("Context").Property("SlotWidth").GetValue<int>();
            string skill = PaintingSkill.Instance.GetName();

            // After the five vanilla skills (the tab opens with the Skills
            // title), before the Professions list.
            int professions = options.Cast<OptionsElement>().ToList().FindIndex(o => o.label == _professionsTitle() + ":");
            if (professions < 0) professions = options.Count;
            var button = Activator.CreateInstance(_buttonType,
                (Func<string>)(() => _increaseLabel(skill, ArtProgress.Level(Game1.player))),
                slotWidth,
                (Action)LevelUp,
                (Func<bool>)(() => ArtProgress.Level(Game1.player) >= PaintingRules.MaxLevel))!;
            options.Insert(Math.Min(6, professions), button);

            // The professions, at the end of the Professions list.
            foreach (var pair in PaintingSkill.Instance.ProfessionsForLevels)
                foreach (var profession in new[] { pair.First, pair.Second })
                {
                    int id = profession.GetVanillaId();
                    options.Add(Activator.CreateInstance(_checkboxType,
                        _professionLabel(profession.GetName(), skill, pair.Level),
                        Game1.player.professions.Contains(id),
                        (Action<bool>)(on => SetProfession(profession, on)))!);
                }
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't add the Painting skill to CJB Cheats Menu's Skills tab: {ex.Message}", LogLevel.Warn);
        }
    }

    // One level up, shown with SpaceCore's level-up screen right away (as
    // CJB does for the vanilla skills), not again at the end of the day.
    private static void LevelUp()
    {
        var who = Game1.player;
        int level = ArtProgress.Level(who);
        if (level >= PaintingRules.MaxLevel)
            return;
        var queued = Traverse.Create(typeof(Skills)).Property("NewLevels").GetValue<IList>();
        int count = queued.Count;
        ArtProgress.SetLevel(who, level + 1);
        while (queued.Count > count)
            queued.RemoveAt(queued.Count - 1);
        if (Game1.activeClickableMenu != null)
            Game1.nextClickableMenu.Add(Game1.activeClickableMenu);
        Game1.activeClickableMenu = new SpaceCore.Interface.SkillLevelUpMenu(PaintingSkill.SkillId, level + 1);
    }

    private static void SetProfession(Skills.Skill.Profession profession, bool on)
    {
        var professions = Game1.player.professions;
        int id = profession.GetVanillaId();
        if (on == professions.Contains(id))
            return;
        if (on)
        {
            professions.Add(id);
            profession.DoImmediateProfessionPerk();
        }
        else
        {
            professions.Remove(id);
            profession.UndoImmediateProfessionPerk();
        }
    }
}
