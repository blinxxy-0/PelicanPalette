using ArtStudio.Core;

namespace ArtStudio;

internal sealed class ModConfig
{
    // How much the painting minigame asks at once (see DifficultySettings).
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    // Scales the light meter on top of the difficulty, for anyone who wants
    // a little more (or less) time without changing anything else.
    public float LightTimeMultiplier { get; set; } = 1f;

    // Whether Simplified mode leaves glazes and highlights out of the
    // picture (1.3). Off, Simplified is free paint that never dries and a
    // wider mixing band, with the whole picture to copy.
    public bool SimplifiedPlainPictures { get; set; } = true;

    // Whether the villagers on the art-lover list (Leah, Elliott, Emily...)
    // like even a normal-quality painting. Off, they're as picky as anyone.
    public bool ArtLoversLikeAnyPainting { get; set; } = true;
}
