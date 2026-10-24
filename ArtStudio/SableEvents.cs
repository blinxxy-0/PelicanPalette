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
            string line = mail.Contains(ModEntry.Id + "_LetterPaintFirst") ? "I did what you said. I painted something first."
                : mail.Contains(ModEntry.Id + "_LetterStay") ? "I stayed. And then I painted something."
                : "I made my call.";
            ev.ReplaceCurrentCommand($"speak Sable \"{line}\"");
        });
    }
}
