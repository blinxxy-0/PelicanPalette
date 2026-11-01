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
