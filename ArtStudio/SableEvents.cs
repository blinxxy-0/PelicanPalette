using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using StardewValley;

namespace ArtStudio;

// Event commands for Sable's heart events (scripts in the content pack).
internal static class SableEvents
{
    public static void Register()
    {
        // Gives the player Sable's framed portrait of them (10 hearts).
        Event.RegisterCommand(ModEntry.Id + "_GiveFarmerPortrait", (ev, _, _) =>
        {
            Game1.player.addItemByMenuIfNecessary(ArtHooks.Frame(FarmerPortrait.Create(Game1.player)));
            ev.CurrentCommand++;
        });

        // Sable's first line at 10 hearts answers what you told them at 6
        // hearts about the museum's letter.
        Event.RegisterCommand(ModEntry.Id + "_LetterAnswer", (ev, _, _) =>
        {
            var mail = Game1.player.mailReceived;
            string line = I18n.Get(mail.Contains(ModEntry.Id + "_LetterPaintFirst") ? "sable.letter-answer.paint-first"
                : mail.Contains(ModEntry.Id + "_LetterStay") ? "sable.letter-answer.stay"
                : "sable.letter-answer.your-call");
            ev.ReplaceCurrentCommand($"speak Sable \"{line}\"");
        });

        // At the gallery evening (14 hearts, 1.2), a guest pages through your
        // portfolio book: someone who finds their own portrait in it, or Leah
        // and your best painting, or, with nothing in the book yet, your
        // sketches (docs/gallery_portfolio.md, approved 2026-11-01).
        Event.RegisterCommand(ModEntry.Id + "_GalleryPortfolio", (ev, _, _) =>
        {
            var (narration, lines) = GalleryPortfolio();
            ev.ReplaceCurrentCommand($"message \"{Quotable(narration)}\"");
            foreach (var (speaker, line) in lines.Reverse())
                ev.InsertNextCommand($"speak {speaker} \"{Quotable(line)}\"");
        });
    }

    // ---- The Summit (1.3) ----

    // Painting the view from the Summit once the portfolio book is finished
    // closes the book: Sable comes up the path with it, and you look through
    // it together. Tender if you're married to Sable, warm and proud if not;
    // Julian gets a mention once his story's done. The game walks it.
    public const string SummitEventId = ModEntry.Id + "_SummitBook";

    public static bool SummitSceneDue(Farmer who, string subject) =>
        Views.IsView(subject) && Views.PlaceOf(subject) == "Summit" && Portfolio.BookFinished
        && !who.eventsSeen.Contains(SummitEventId) && who.currentLocation?.Name == "Summit";

    public static void StartSummitScene(Farmer who)
    {
        bool married = who.spouse == "Sable";
        string Say(string key) => $"speak Sable \"{Quotable(I18n.Get(key))}\"";
        var commands = new System.Collections.Generic.List<string>
        {
            "continue", "11 15", "farmer 11 13 0 Sable 13 24 0", "skippable",
            "pause 1000",
            // Up the path, round the bench, to stand beside you at the top.
            "move Sable 0 -11 0", "move Sable -1 0 3", "faceDirection farmer 1", "pause 400",
            Say(married ? "summit.arrive.married" : "summit.arrive"),
            Say("summit.book"),
            $"message \"{Quotable(I18n.Get("summit.pages"))}\"",
            Say("summit.faces"),
        };
        if (who.eventsSeen.Contains(ModEntry.Id + "_Julian10h"))
            commands.Add(Say("summit.julian"));
        if (married)
            commands.Add("emote Sable 20");
        commands.Add(Say(married ? "summit.proud.married" : "summit.proud"));
        commands.Add("faceDirection farmer 0");
        commands.Add("faceDirection Sable 0");
        commands.Add("pause 800");
        commands.Add(Say("summit.last-page"));
        commands.Add("pause 600");
        commands.Add("end");
        who.currentLocation.startEvent(new Event(string.Join("/", commands), null, SummitEventId));
    }

    // (A painting's title could hold a double quote, which would end the event's quoted text.)
    private static string Quotable(string text) => text.Replace('"', '\'');

    // The guests at the gallery evening, in the order they'd be first to
    // find their portrait.
    internal static readonly string[] GalleryGuests = { "Lewis", "Leah", "Elliott", "Haley", "Robin", "Evelyn", "George", "Emily" };

    internal static (string Narration, (string Speaker, string Line)[] Lines) GalleryPortfolio()
    {
        var book = Portfolio.Items.Where(item => item != null).Select(item => (Item: item!, Data: PaintingData.Read(item!)))
            .Where(p => p.Data != null).ToList();
        if (book.Count == 0)
            return (I18n.Get("gallery.portfolio.sketches"), new[]
            {
                ("Leah", I18n.Get("gallery.portfolio.sketches.leah")),
                ("Sable", I18n.Get("gallery.portfolio.sketches.sable")),
            });

        string narration = I18n.Get("gallery.portfolio.book");
        foreach (string guest in GalleryGuests)
            if (book.Any(p => p.Data!.IsSitter && Sitters.NpcOf(p.Data.Subject) == guest))
                return (narration, new[] { (guest, I18n.Get("gallery.portfolio.portrait." + guest.ToLowerInvariant())) });

        var best = book.OrderByDescending(p => PaintingData.IsMasterpiece(p.Item) ? PaintingRules.Iridium + 1 : p.Item.Quality)
            .ThenByDescending(p => p.Data!.Stars).First();
        string tier = PaintingData.IsMasterpiece(best.Item) || best.Item.Quality >= PaintingRules.Iridium ? "iridium"
            : best.Item.Quality >= PaintingRules.Gold ? "gold" : "plain";
        return (narration, new[] { ("Leah", I18n.Get("gallery.portfolio.best." + tier, new { title = PaintingItems.Title(best.Data!) })) });
    }
}
