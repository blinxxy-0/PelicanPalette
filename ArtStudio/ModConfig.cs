using ArtStudio.Core;

namespace ArtStudio;

internal sealed class ModConfig
{
    // How much the painting minigame asks at once (see DifficultySettings).
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    // Scales the light meter on top of the difficulty, for anyone who wants
    // a little more (or less) time without changing anything else.
    public float LightTimeMultiplier { get; set; } = 1f;
}
