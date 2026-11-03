using ArtStudio.Core;

namespace ArtStudio;

internal sealed class ModConfig
{
    // How much the painting minigame asks at once (see DifficultySettings).
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    // Scales the light meter on top of the difficulty, for anyone who wants
    // a little more (or less) time without changing anything else.
    public float LightTimeMultiplier { get; set; } = 1f;

    // Flat-colour pictures: no glazes or highlights in the reference (the
    // Glaze tool's still there for a glazed commission). Any difficulty; it
    // was part of Relaxed until 1.3, when players said it was a shame to
    // lose the picture just to lose the clock.
    public bool SimplePictures { get; set; }

    // Whether the villagers on the art-lover list (Leah, Elliott, Emily...)
    // like even a normal-quality painting. Off, they're as picky as anyone.
    public bool ArtLoversLikeAnyPainting { get; set; } = true;
}
