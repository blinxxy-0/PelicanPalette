using ArtStudio.Core;
using StardewModdingAPI;

namespace ArtStudio;

// Player-facing text, from i18n/<language>.json (default.json is English).
// Keys are grouped by where the text appears: easel screens, items, the
// skill, the shop. Tokens are {{name}} placeholders.
internal static class I18n
{
    private static ITranslationHelper? _translations;

    public static void Init(ITranslationHelper translations) => _translations = translations;

    public static string Get(string key, object? tokens = null) =>
        _translations?.Get(key, tokens).ToString() ?? key;

    // Pigments and their mixing motions are named in Core (shared with the
    // simulator, which stays English); the game shows them translated.
    public static string Pigment(int index) => Get("pigment." + Palette.All[index].Id);

    public static string Motion(MixMotion motion) => Get("motion." + motion.ToString().ToLowerInvariant());

    // The judge's word for a score ("Excellent", "Good"...), also from Core.
    public static string Rating(float value) => Get("rating." + PaintingJudge.Rating(value).ToLowerInvariant());
}
