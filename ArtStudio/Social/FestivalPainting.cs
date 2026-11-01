using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Social;

// Festivals, round two (1.3).
//
// Spirit's Eve and the Dance of the Moonlight Jellies: festivals don't allow
// easels, so Sable sets one up for the night, and its view is one you can
// only paint then: the haunted maze by lantern light, and the jellies out on
// the water. Each view is a set patch of the festival's map (the maze's
// middle, the sea off the beach), not the ground around the easel; the
// jellies, which the game draws as the dance's sprites, are added to it.
// The festival's clock stands still, so painting there takes no time.
//
// The Feast of the Winter Star: if Sable is your secret friend, their gift
// is one of their paintings, of the thing you've shipped most.
internal static class FestivalPainting
{
    // Unlit: the view is taken without the night's darkness and given a
    // gentler one (the maze, whose hedges are otherwise near black); the
    // Jellies keep the game's own night, deep blue for the jellies to glow in.
    internal sealed record Spot(string FestivalId, string Place, Point Easel, Point ViewCorner, bool Jellies, bool Unlit);

    public static readonly Spot[] Spots =
    {
        new("fall27", "SpiritsEve", new Point(20, 26), new Point(15, 24), Jellies: false, Unlit: true),
        new("summer28", "MoonlightJellies", new Point(64, 22), new Point(56, 24), Jellies: true, Unlit: false),
    };

    // The festival on now with an easel, if any.
    public static Spot? Current =>
        Game1.CurrentEvent is { isFestival: true } festival ? Spots.FirstOrDefault(s => festival.isSpecificFestival(s.FestivalId)) : null;

    public static bool IsFestivalPlace(string place) => Spots.Any(s => s.Place == place);

    // During a festival the game puts you in a temporary copy of the map.
    public static bool AtFestivalEasel(GameLocation location) => Current != null && location.Name == "Temp";

    public static void Apply(Harmony harmony, IModHelper helper)
    {
        harmony.Patch(AccessTools.Method(typeof(Event), nameof(Event.checkAction)),
            prefix: new HarmonyMethod(typeof(FestivalPainting), nameof(CheckActionPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Utility), nameof(Utility.getGiftFromNPC)),
            postfix: new HarmonyMethod(typeof(FestivalPainting), nameof(GiftPostfix)));
        helper.Events.GameLoop.UpdateTicked += (_, _) => SetUpEasel();
    }

    // ---- The easel ----

    private static Event? _easelFor;

    private static void SetUpEasel()
    {
        if (Current is not { } spot || Game1.CurrentEvent is not { } festival || _easelFor == festival) return;
        _easelFor = festival;
        var easel = Game1.content.Load<Texture2D>(ArtItems.BigCraftablesTexture);
        festival.festivalProps.Add(new Prop(easel, 0, 1, 1, 2, spot.Easel.X, spot.Easel.Y));
    }

    private static bool CheckActionPrefix(Event __instance, xTile.Dimensions.Location tileLocation, Farmer who, ref bool __result)
    {
        if (!__instance.isFestival || Current is not { } spot || tileLocation.X != spot.Easel.X
            || (tileLocation.Y != spot.Easel.Y && tileLocation.Y != spot.Easel.Y - 1))
            return true;
        ArtHooks.OpenEasel(who, new Vector2(spot.Easel.X, spot.Easel.Y));
        __result = true;
        return false;
    }

    // A festival view's night, gentler than the game's (taken off before the
    // capture): the darks lifted a little and the colours a touch richer, so
    // a dark green hedge reads as green rather than charcoal, then everything
    // dimmed a little and cooled towards blue. The lights (the lanterns'
    // flames, near white) are left as they are. Picked by eye from the maze
    // reduced at each canvas size, with the starting pigments and with all.
    private const float NightGamma = 0.75f, NightSaturation = 1.1f, NightDim = 0.85f;
    private static readonly Vector3 NightTint = new(0, 6, 26);

    public static void NightGrade(Color[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            if (p.A < 128 || p.R + p.G + p.B > 600) continue;
            var (h, l, s) = ToHls(p.R / 255f, p.G / 255f, p.B / 255f);
            var (r, g, b) = FromHls(h, MathF.Pow(l, NightGamma), Math.Min(1f, s * NightSaturation));
            pixels[i] = new Color(
                (int)Math.Min(255f, r * 255f * NightDim + NightTint.X),
                (int)Math.Min(255f, g * 255f * NightDim + NightTint.Y),
                (int)Math.Min(255f, b * 255f * NightDim + NightTint.Z),
                p.A);
        }
    }

    private static (float H, float L, float S) ToHls(float r, float g, float b)
    {
        float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        float l = (max + min) / 2f;
        if (max == min) return (0f, l, 0f);
        float d = max - min;
        float s = l <= 0.5f ? d / (max + min) : d / (2f - max - min);
        float h = max == r ? (g - b) / d + (g < b ? 6f : 0f) : max == g ? (b - r) / d + 2f : (r - g) / d + 4f;
        return (h / 6f, l, s);
    }

    private static (float R, float G, float B) FromHls(float h, float l, float s)
    {
        if (s == 0f) return (l, l, l);
        float q = l < 0.5f ? l * (1f + s) : l + s - l * s, p = 2f * l - q;
        float Channel(float t)
        {
            t = t < 0f ? t + 1f : t > 1f ? t - 1f : t;
            return t < 1f / 6f ? p + (q - p) * 6f * t : t < 0.5f ? q : t < 2f / 3f ? p + (q - p) * (2f / 3f - t) * 6f : p;
        }
        return (Channel(h + 1f / 3f), Channel(h), Channel(h - 1f / 3f));
    }

    // The jellies over the water, in tiles from the view's corner, each drawn
    // two tiles across (a tile's worth would be a speck on the canvas).
    private static readonly Point[] JellyTiles =
    {
        new(2, 8), new(5, 11), new(8, 7), new(10, 11), new(12, 8), new(3, 14), new(7, 14), new(13, 13),
    };
    private const int JellyScale = 2;
    private static readonly Rectangle JellySource = new(256, 16, 16, 16);

    // Adds the jellies to a capture of the sea (16 pixels a tile).
    public static void AddJellies(Color[] pixels, int size)
    {
        Texture2D sheet;
        try { sheet = Game1.content.Load<Texture2D>("Maps\\Festivals"); }
        catch (Exception) { return; }
        var jelly = new Color[16 * 16];
        sheet.GetData(0, JellySource, jelly, 0, jelly.Length);
        foreach (var tile in JellyTiles)
            for (int y = 0; y < 16 * JellyScale; y++)
            for (int x = 0; x < 16 * JellyScale; x++)
            {
                int px = tile.X * 16 + x, py = tile.Y * 16 + y;
                var c = jelly[y / JellyScale * 16 + x / JellyScale];
                if (px >= size || py >= size || c.A < 128) continue;
                pixels[py * size + px] = c;
            }
    }

    // ---- The Winter Star ----

    private static void GiftPostfix(NPC who, ref Item __result)
    {
        try
        {
            if (who?.Name == "Sable" && Game1.CurrentEvent?.isSpecificFestival("winter25") == true)
                __result = SablesPainting(Game1.player);
        }
        catch (Exception)
        {
            // (Their usual gift, then.)
        }
    }

    // The thing you've shipped most (that can be painted), or a parsnip.
    public static string Favourite(Farmer who) =>
        who.basicShipped.Pairs.OrderByDescending(kv => kv.Value)
            .Select(kv => "(O)" + kv.Key)
            .FirstOrDefault(id => ItemRegistry.Exists(id) && ReferenceBuilder.CanPaint(ItemRegistry.Create(id)))
        ?? "(O)24";

    // A still life of it, by Sable: medium canvas, every colour, their best.
    public static SObject SablesPainting(Farmer who)
    {
        string subject = Favourite(who);
        int size = PaintingRules.CanvasSize(CanvasKind.Medium);
        var reference = ReferenceBuilder.StillLife(new[] { subject }, size, Enumerable.Range(0, Palette.All.Length).ToList(), Techniques.Glaze);
        var data = new PaintingData
        {
            Size = size,
            Cells = reference.Cells,
            Purity = new Purity[size, size],
            Marks = reference.Marks,
            Subject = subject,
            Stars = 5f,
            Difficulty = reference.Difficulty,
            Painter = "Sable",
            Date = I18n.Get("painting.date", new { season = Utility.getSeasonNameFromNumber(Game1.seasonIndex), day = Game1.dayOfMonth, year = Game1.year }),
        };
        int price = PaintingRules.Value(size, reference.Difficulty, ItemRegistry.Create(subject).salePrice(), 10);
        return PaintingItems.Create(data, PaintingRules.Iridium, price);
    }
}
