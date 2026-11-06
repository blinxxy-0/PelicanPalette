using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Social;

// Margot restores a damaged old painting for you, for a fee, before you
// have Painting Mastery (1.3, restoration_mechanics.md §3): hand it to her
// at the museum and it's ready three days later, at silver quality
// ("careful, but cautious"), as Clint breaks geodes. One at a time. The
// morning it's ready she posts it back: her letter comes with the painting
// attached, so there's no walk back to the museum (from playtesting).
internal static class MargotService
{
    private const string JobKey = ModEntry.Id + "/MargotJob";       // "<painting id>|<day it's ready>"
    public const string ReturnMail = ModEntry.Id + "_MargotRestored";
    public const int Days = 3;

    public static void Register(IModHelper helper)
    {
        helper.Events.GameLoop.DayStarted += (_, _) => PostIfReady(Game1.player);
        helper.Events.Display.MenuChanged += (_, e) =>
        {
            if (e.NewMenu is LetterViewerMenu letter && letter.mailTitle == ReturnMail && !letter.isFromCollection)
                Enclose(letter, Game1.player);
        };
    }

    // The morning it's ready: her letter, in your mailbox today.
    internal static void PostIfReady(Farmer who)
    {
        if (Job(who) is not { } job || Game1.Date.TotalDays < job.Ready || who.mailbox.Contains(ReturnMail)) return;
        who.mailbox.Add(ReturnMail);
    }

    // Opening her letter: the restored painting comes with it (the letter
    // hands over whatever's left in it when it's closed, so it can't be lost).
    internal static Item? Enclose(LetterViewerMenu letter, Farmer who)
    {
        if (Job(who) is not { } job || Game1.Date.TotalDays < job.Ready) return null;
        var restored = Restored(job.Painting);
        letter.itemsToGrab.Add(new ClickableComponent(new Rectangle(letter.xPositionOnScreen + letter.width / 2 - 48, letter.yPositionOnScreen + letter.height - 32 - 96, 96, 96), restored)
        {
            myID = 104, leftNeighborID = 101, rightNeighborID = 102,
        });
        if (letter.backButton != null) letter.backButton.rightNeighborID = 104;
        if (letter.forwardButton != null) letter.forwardButton.leftNeighborID = 104;
        who.modData.Remove(JobKey);
        return restored;
    }

    public static int Fee(OldPainting painting) => painting.Set switch
    {
        "valley" => 600,
        "wreck" or "miners" => 800,
        "calico" => 1500,
        _ => 2500,                                                  // Dwarvish
    };

    public static (OldPainting Painting, int Ready)? Job(Farmer who)
    {
        if (!who.modData.TryGetValue(JobKey, out string raw)) return null;
        string[] parts = raw.Split('|');
        return parts.Length == 2 && OldPaintings.Get(parts[0]) is { } p && int.TryParse(parts[1], out int ready) ? (p, ready) : null;
    }

    // Talking to her at the museum: a painting to restore, or one to collect.
    // Returns true if she had something to say about it.
    public static bool TryHandle(NPC margot, Farmer who)
    {
        var job = Job(who);
        if (who.ActiveObject is { } held && OldPaintings.IsDamaged(held) && OldPaintings.Of(held) is { } old)
        {
            if (ArtProgress.HasMastery(who))
                Say(margot, I18n.Get("margot.service.yourself"));
            else if (job is { } busy)
                Say(margot, I18n.Get("margot.service.busy", new { title = OldPaintings.Title(busy.Painting) }));
            else
            {
                int fee = Fee(old);
                who.currentLocation.createQuestionDialogue(
                    I18n.Get("margot.service.offer", new { title = OldPaintings.Title(old), fee }), new[]
                    {
                        new Response("yes", I18n.Get("margot.service.yes")),
                        new Response("no", I18n.Get("margot.service.no")),
                    }, (_, answer) => { if (answer == "yes") TakeIn(margot, who, held, old); });
            }
            return true;
        }
        if (who.ActiveObject == null && job is { } waiting)
        {
            Say(margot, I18n.Get(Game1.Date.TotalDays < waiting.Ready ? "margot.service.waiting" : "margot.service.posted",
                new { title = OldPaintings.Title(waiting.Painting) }));
            return true;
        }
        return false;
    }

    internal static bool TakeIn(NPC margot, Farmer who, Item damaged, OldPainting old)
    {
        int fee = Fee(old);
        if (who.Money < fee)
        {
            Say(margot, I18n.Get("margot.service.poor", new { fee }));
            return false;
        }
        who.Money -= fee;
        who.removeItemFromInventory(damaged);
        who.modData[JobKey] = $"{old.Id}|{Game1.Date.TotalDays + Days}";
        Game1.playSound("purchase");
        Say(margot, I18n.Get("margot.service.taken"));
        return true;
    }

    // Her restoration: the picture whole again, at silver.
    internal static Item Restored(OldPainting old)
    {
        int size = old.Size;
        var data = new PaintingData
        {
            Size = size,
            Cells = OldPaintings.Original(old),
            Purity = new Purity[size, size],
            Subject = OldPaintings.SubjectFor(old.Id),
            Stars = 3.5f,
            Painter = I18n.Get("margot.name"),
            Date = I18n.Get("painting.date", new { season = Utility.getSeasonNameFromNumber(Game1.seasonIndex), day = Game1.dayOfMonth, year = Game1.year }),
        };
        return PaintingItems.Create(data, PaintingRules.Silver, OldPaintings.RestoredValue(old, PaintingRules.Silver));
    }

    // On top of her line for the day, which stays for next time.
    private static void Say(NPC npc, string text)
    {
        npc.CurrentDialogue.Push(new Dialogue(npc, null, text));
        Game1.drawDialogue(npc);
    }

    // For the dev test.
    internal static void DevReset(Farmer who) => who.modData.Remove(JobKey);
}
