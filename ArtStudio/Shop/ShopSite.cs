using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace ArtStudio.Shop;

// Where the art shop stands in the Forest. Normally at 76, 27 (the yard and house, 13 x 12), north
// of the riverside path. Aymeric de Borel's alternate house (the version that makes him compatible
// with Eugene, Nexus 23851) is built on that same ground, in Stardew Valley Expanded's Forest, so
// with it installed the shop moves 19 tiles west and 3 south, onto the open woods below SVE's crop
// field (57, 30), clear of his house, Eugene's garden and Alec Revisited's patch. (A player's report,
// 2026-11-12; the user's choice: move the shop only when both are installed.)
//
// Content Patcher reads the choice through the token {{blinxxy.MuseArt/ShopMoved}} ("true" or
// "false"): data/shop_moved.json in each content pack lays the shop, its warps, schedules and
// events at the moved site, and the usual patches step aside. The code reads Offset.
internal static class ShopSite
{
    public const string AymericId = "Adaire.Aymeric";
    public static readonly Point MovedBy = new(-19, 3);

    public static Point Offset { get; private set; } = Point.Zero;
    public static bool Moved => Offset != Point.Zero;

    public static Point At(int x, int y) => new(x + Offset.X, y + Offset.Y);

    public static void Init(IModHelper helper, IMonitor monitor)
    {
        if (AymericAlternateHouse(helper, monitor))
        {
            Offset = MovedBy;
            monitor.Log("Aymeric's alternate (Eugene-compatible) house is installed and stands where the art shop does, "
                + $"so the shop moves to Forest {76 + MovedBy.X}, {27 + MovedBy.Y}, on the open ground below the crop field.", LogLevel.Info);
        }
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            var cp = helper.ModRegistry.GetApi<IContentPatcherApi>("Pathoschild.ContentPatcher");
            cp?.RegisterToken(helper.ModRegistry.Get(ModEntry.Id)!.Manifest, "ShopMoved", () => new[] { Moved ? "true" : "false" });
        };
    }

    // Aymeric's alternate house is the one whose Forest patch starts at x 71 (his usual house is
    // at 34, 35). Both share his mod id, so his patch file says which is installed. If it can't be
    // read, Aymeric together with any Eugene mod means the alternate (that's what it's for).
    private static bool AymericAlternateHouse(IModHelper helper, IMonitor monitor)
    {
        var aymeric = helper.ModRegistry.Get(AymericId);
        if (aymeric == null)
            return false;
        try
        {
            if (aymeric.GetType().GetProperty("DirectoryPath")?.GetValue(aymeric) is string dir)
            {
                string content = File.ReadAllText(Path.Combine(dir, "content.json"));
                int at = content.IndexOf("\"Maps/Forest\"", StringComparison.Ordinal);
                if (at >= 0)
                {
                    string patch = content.Substring(at, Math.Min(600, content.Length - at));
                    return System.Text.RegularExpressions.Regex.IsMatch(patch, "\"ToArea\"\\s*:\\s*\\{\\s*\"X\"\\s*:\\s*7[0-9]\\b");
                }
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't read Aymeric's Forest patch ({ex.Message}); deciding by whether Eugene is installed.", LogLevel.Trace);
        }
        return helper.ModRegistry.GetAll().Any(m => m.Manifest.UniqueID.Contains("Eugene", StringComparison.OrdinalIgnoreCase));
    }
}

// The part of Content Patcher's API this uses (registering a simple token).
public interface IContentPatcherApi
{
    void RegisterToken(IManifest mod, string name, Func<System.Collections.Generic.IEnumerable<string>?> getValue);
}
