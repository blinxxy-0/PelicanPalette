using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace ArtStudio.Painting;

// The living painting (1.3, the museum collection's reward for six donated):
// an old painting of a mill on a river at dusk whose picture moves a little,
// the water shimmering and the window's lamp flickering. Its sheet holds four
// frames side by side (tools/art/living_painting.py); any hung where you are
// steps through them.
internal static class LivingPainting
{
    public const string Id = ModEntry.Id + "_LivingPainting";
    public const string QualifiedId = "(F)" + Id;
    private const int Frames = 4, FrameMs = 350, Size = 32;

    public static void Register(IModHelper helper) =>
        helper.Events.GameLoop.UpdateTicked += (_, e) =>
        {
            if (!Context.IsWorldReady || !e.IsMultipleOf(6) || Game1.currentLocation is not { } location) return;
            int frame = Frame(Game1.currentGameTime.TotalGameTime.TotalMilliseconds);
            foreach (var painting in location.furniture.Where(f => f.QualifiedItemId == QualifiedId))
            {
                var rect = new Rectangle(frame * Size, 0, Size, Size);
                if (painting.sourceRect.Value != rect)
                    painting.sourceRect.Value = rect;
            }
        };

    public static int Frame(double ms) => (int)(ms / FrameMs) % Frames;
}
