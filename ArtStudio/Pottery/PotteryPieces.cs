#if DEBUG
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ArtStudio.Core;
using ArtStudio.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.Objects;

namespace ArtStudio.Pottery;

// A fired pot you keep (docs/design/pottery_minigame.md, section 7): what the
// kiln gives back goes into your bag as furniture, decor for a table or the
// floor like the game's vases. Like a painting, every piece is one of a kind:
// its profile (the wall's rows of radius and thickness), its glaze (the base
// coat and the banding grid), how the firing came out and its stars live in the
// item's modData, and its sprite is drawn from them at runtime (PotPieceRenderer).
// One furniture item per shape: cups and bowls are one-tile decor, vases and
// jugs a 1x2 sprite on a one-tile footprint, so they still fit on a table.
internal sealed class PotPiece
{
    private const string Key = ModEntry.Id + "/Pot";
    private const string ShapeKey = Key + "Shape", ProfileKey = Key + "Profile", GlazeKey = Key + "Glaze",
        FiringKey = Key + "Firing", VerdictKey = Key + "Verdict", GiftKey = Key + "Gift", StarsKey = Key + "Stars",
        PotterKey = Key + "Potter", DateKey = Key + "Date", PriceKey = Key + "Price";

    // The profile is kept to an eighth of a clay unit, a byte a number.
    private const float ProfileStep = 8f;

    public string Shape { get; init; } = "cup";
    public float[] R { get; init; } = Array.Empty<float>();
    public float[] T { get; init; } = Array.Empty<float>();
    public int Base { get; init; } = -1;                         // -1: bisque, never dipped
    public int Slices { get; init; }
    public int[] Bands { get; init; } = Array.Empty<int>();      // Slices x PotGlaze.Buckets; -1 where only the base shows
    public bool Dull { get; init; }                              // under- or overfired: the glaze came out dull
    public string Verdict { get; init; } = "";
    public string? Gift { get; init; }                           // the kiln's gift: crackle, drip, flash or pool
    public float Stars { get; init; }
    public string Potter { get; init; } = "";
    public string Date { get; init; } = "";

    public int Rows => R.Length;
    public bool Tall => PotteryPieces.IsTall(Shape);
    public int GlazeAt(int slice, int bucket) =>
        Slices == 0 ? Base : Bands[Math.Clamp(slice, 0, Slices - 1) * PotGlaze.Buckets + bucket] is var g && g >= 0 ? g : Base;
    public IEnumerable<int> BandGlazes => Bands.Where(g => g >= 0 && g != Base).Distinct();

    public static readonly string[] GiftKinds = { "crackle", "drip", "flash", "pool" };

    // The kiln's gift, from FiringJudge's words for it.
    public static string? GiftKind(string? gift) => gift == null ? null
        : gift.Contains("crackle") ? "crackle"
        : gift.Contains("drip") ? "drip"
        : gift.Contains("flash") ? "flash"
        : gift.Contains("pool") ? "pool"
        : "crackle";

    public static PotPiece From(string shape, ClayPot pot, PotGlaze glaze, FiringResult firing, float stars, Farmer? who)
    {
        int slices = glaze.Slices;
        var bands = new int[slices * PotGlaze.Buckets];
        for (int s = 0; s < slices; s++)
            for (int k = 0; k < PotGlaze.Buckets; k++)
                bands[s * PotGlaze.Buckets + k] = glaze.Band(s, k);
        return new PotPiece
        {
            Shape = shape,
            R = pot.R.Take(pot.Rows).Select(Quantize).ToArray(),
            T = pot.T.Take(pot.Rows).Select(Quantize).ToArray(),
            Base = glaze.Base,
            Slices = slices,
            Bands = bands,
            Dull = firing.Verdict is "Underfired" or "Overfired",
            Verdict = firing.Verdict,
            Gift = GiftKind(firing.Gift),
            Stars = stars,
            Potter = who?.Name ?? "",
            Date = Game1.Date?.Localize() ?? "",
        };
    }

    private static float Quantize(float v) => Math.Clamp(MathF.Round(v * ProfileStep), 0f, 255f) / ProfileStep;

    // ---- modData ----

    public void WriteTo(Item item)
    {
        item.modData[ShapeKey] = Shape;
        item.modData[ProfileKey] = EncodeProfile();
        item.modData[GlazeKey] = EncodeGlaze();
        item.modData[FiringKey] = Dull ? "dull" : "true";
        item.modData[VerdictKey] = Verdict;
        if (Gift != null)
            item.modData[GiftKey] = Gift;
        else
            item.modData.Remove(GiftKey);
        item.modData[StarsKey] = Stars.ToString(CultureInfo.InvariantCulture);
        item.modData[PotterKey] = Potter;
        item.modData[DateKey] = Date;
    }

    // What the renderer draws from: a key that changes whenever the look does.
    public static string? LookKey(Item item) =>
        item.modData.TryGetValue(ProfileKey, out string profile) && item.modData.TryGetValue(GlazeKey, out string glaze)
            ? $"{item.ItemId}|{profile}|{glaze}|{item.modData.GetValueOrDefault(FiringKey)}|{item.modData.GetValueOrDefault(GiftKey)}"
            : null;

    public static PotPiece? Read(Item item)
    {
        if (!item.modData.TryGetValue(ProfileKey, out string profile) || !item.modData.TryGetValue(GlazeKey, out string glaze))
            return null;
        if (!TryDecodeProfile(profile, out var r, out var t) || !TryDecodeGlaze(glaze, out int baseGlaze, out int slices, out var bands))
            return null;
        item.modData.TryGetValue(ShapeKey, out string shape);
        item.modData.TryGetValue(FiringKey, out string firing);
        item.modData.TryGetValue(VerdictKey, out string verdict);
        item.modData.TryGetValue(GiftKey, out string gift);
        item.modData.TryGetValue(StarsKey, out string stars);
        item.modData.TryGetValue(PotterKey, out string potter);
        item.modData.TryGetValue(DateKey, out string date);
        return new PotPiece
        {
            Shape = PotShape.Names.Contains(shape) ? shape : PotteryPieces.ShapeOf(item) ?? "cup",
            R = r,
            T = t,
            Base = baseGlaze,
            Slices = slices,
            Bands = bands,
            Dull = firing == "dull",
            Verdict = verdict ?? "",
            Gift = string.IsNullOrEmpty(gift) ? null : gift,
            Stars = float.TryParse(stars, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) ? s : 0f,
            Potter = potter ?? "",
            Date = date ?? "",
        };
    }

    // Kept beside the item's own price, which the game resets from the furniture data
    // whenever the piece is set down on a table (Furniture.InitializeAtTile).
    public static void WritePrice(Item item, int price) => item.modData[PriceKey] = price.ToString(CultureInfo.InvariantCulture);
    public static int? PriceOf(Item item) => item.modData.TryGetValue(PriceKey, out string raw) && int.TryParse(raw, out int p) ? p : null;

    // Rows, then each row's radius, then each row's thickness, a byte each.
    private string EncodeProfile()
    {
        int n = Math.Min(Rows, 255);
        var bytes = new byte[1 + 2 * n];
        bytes[0] = (byte)n;
        for (int i = 0; i < n; i++)
        {
            bytes[1 + i] = (byte)Math.Clamp((int)MathF.Round(R[i] * ProfileStep), 0, 255);
            bytes[1 + n + i] = (byte)Math.Clamp((int)MathF.Round(T[i] * ProfileStep), 0, 255);
        }
        return Convert.ToBase64String(bytes);
    }

    private static bool TryDecodeProfile(string raw, out float[] r, out float[] t)
    {
        r = t = Array.Empty<float>();
        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); }
        catch (FormatException) { return false; }
        if (bytes.Length == 0 || bytes[0] == 0 || bytes.Length != 1 + 2 * bytes[0])
            return false;
        int n = bytes[0];
        r = Enumerable.Range(0, n).Select(i => bytes[1 + i] / ProfileStep).ToArray();
        t = Enumerable.Range(0, n).Select(i => bytes[1 + n + i] / ProfileStep).ToArray();
        return true;
    }

    // The base coat and the slice count, then the banding grid as runs: (glaze + 1, run
    // length) pairs. Bands are long runs of one glaze, so a pot's grid is a few hundred bytes.
    private string EncodeGlaze()
    {
        var bytes = new List<byte> { (byte)(Base + 1), (byte)(Slices & 0xFF), (byte)(Slices >> 8) };
        for (int i = 0; i < Bands.Length;)
        {
            int v = Bands[i], run = 1;
            while (i + run < Bands.Length && Bands[i + run] == v && run < 255)
                run++;
            bytes.Add((byte)(v + 1));
            bytes.Add((byte)run);
            i += run;
        }
        return Convert.ToBase64String(bytes.ToArray());
    }

    private static bool TryDecodeGlaze(string raw, out int baseGlaze, out int slices, out int[] bands)
    {
        baseGlaze = -1;
        slices = 0;
        bands = Array.Empty<int>();
        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); }
        catch (FormatException) { return false; }
        if (bytes.Length < 3 || (bytes.Length - 3) % 2 != 0)
            return false;
        baseGlaze = bytes[0] - 1;
        slices = bytes[1] | bytes[2] << 8;
        if (baseGlaze >= Glazes.All.Length)
            baseGlaze = -1;
        bands = new int[slices * PotGlaze.Buckets];
        int at = 0;
        for (int i = 3; i < bytes.Length; i += 2)
        {
            int v = bytes[i] - 1;
            if (v >= Glazes.All.Length)
                v = -1;
            for (int k = 0; k < bytes[i + 1] && at < bands.Length; k++)
                bands[at++] = v;
        }
        return at == bands.Length;
    }

    // ---- Words ----

    public static string Title(string text) =>
        string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));

    public string GlazeName => Base < 0 ? "Bisque" : Title(Glazes.All[Base].Name);

    // "Celadon Vase", "Copper Red Jug", "Bisque Cup".
    public string Name => $"{GlazeName} {Title(Shape)}";

    public string Description
    {
        get
        {
            var lines = new List<string>();
            var bands = BandGlazes.Select(g => Glazes.All[g].Name.ToLowerInvariant()).ToList();
            string what = Base < 0 ? $"An unglazed {Shape}" : $"A {Shape} in {Glazes.All[Base].Name.ToLowerInvariant()}";
            lines.Add(bands.Count == 0 ? what + "." : $"{what}, banded in {string.Join(" and ", bands)}.");
            string stars = Stars % 1f == 0f ? $"{Stars:0}" : $"{Stars:0.0}";
            lines.Add($"{stars} {(Stars == 1f ? "star" : "stars")}, {Verdict.ToLowerInvariant()}{(Dull ? ": the glaze came out dull" : "")}.");
            if (Gift != null)
                lines.Add("Kiln gift: " + Gift switch
                {
                    "drip" => "the glaze ran into a fine drip at the foot.",
                    "flash" => "a flash of colour where the flame licked it.",
                    "pool" => "the glaze pooled deep and glassy in the rings.",
                    _ => "the glaze crackled like river ice.",
                });
            var signature = new[] { string.IsNullOrEmpty(Potter) ? null : $"Thrown by {Potter}", string.IsNullOrEmpty(Date) ? null : Date }
                .Where(s => s != null).ToList();
            if (signature.Count > 0)
                lines.Add(string.Join(", ", signature) + ".");
            return string.Join("\n", lines);
        }
    }
}

internal static class PotteryPieces
{
    public const string ContextTag = "muse_pottery";
    public const string TextureAsset = "Mods/" + ModEntry.Id + "/PotteryPieces";
    private const int SmallSprite = 0, TallSprite = 1;

    public static string IdFor(string shape) => $"{ModEntry.Id}_Pottery{PotPiece.Title(shape)}";
    public static string QualifiedIdFor(string shape) => "(F)" + IdFor(shape);
    public static bool IsTall(string shape) => shape is "vase" or "jug";
    public static bool IsPot(Item? item) => item is Furniture && ShapeOf(item) != null;
    public static string? ShapeOf(Item? item) =>
        item == null ? null : PotShape.Names.FirstOrDefault(s => item.QualifiedItemId == QualifiedIdFor(s));

    // The Pottery skill's level caps the quality, as Painting's does, once the skill lands;
    // until then the stars alone set it, at painting's thresholds.
    public static int Quality(float stars) => PaintingRules.Quality(stars, PaintingRules.MaxLevel);

    // Sizes, like canvases (section 7): a cup or bowl is small, a vase or jug medium.
    public static int BaseValue(string shape) => IsTall(shape) ? 240 : 110;

    // The piece's own value, before the game's quality multiplier (Market.QualityPrice,
    // applied when it's sold). Stars lift it as a painting's difficulty does; a kiln gift
    // adds a little, a dull glaze takes a little off.
    public static int Value(PotPiece piece, Farmer? who)
    {
        float value = BaseValue(piece.Shape) * (0.7f + 0.06f * Math.Clamp(piece.Stars, 0f, 5f))
            * (piece.Gift != null ? 1.15f : 1f) * (piece.Dull ? 0.85f : 1f);
        // Thrower: multiply by PotterySkill.PriceMultiplier(who) when the skill lands
        return Math.Max(1, (int)MathF.Round(value));
    }

    public static Furniture Create(PotPiece piece, Farmer? who)
    {
        var item = ItemRegistry.Create<Furniture>(QualifiedIdFor(piece.Shape));
        piece.WriteTo(item);
        item.Quality = Quality(piece.Stars);
        int price = Value(piece, who);
        item.Price = price;
        PotPiece.WritePrice(item, price);
        return item;
    }

    // ---- From the wheel ----

    // The piece each pot on the wheel became, so firing the same pot again replaces it
    // (a pot can't come out of the kiln twice) instead of giving a second one.
    private static readonly ConditionalWeakTable<ClayPot, Furniture> Kept = new();
    private static readonly ConditionalWeakTable<ClayPot, string> KeptWhere = new();

    // For the dev checks: the last piece the kiln gave, and the stars it was given at.
    internal static (Furniture Item, float Stars)? LastFired;

    // Called when the kiln's results are shown. Only for the pot at the wheel you're
    // using: the snapshot and calibration runs build menus that are never opened, and
    // those give nothing.
    public static void OnFired(IClickableMenu menu, string shape, ClayPot pot, PotGlaze? glaze, FiringResult? firing, float stars)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != menu || glaze == null || firing == null)
            return;
        var who = Game1.player;
        Kept.TryGetValue(pot, out var previous);
        int slot = previous == null ? -1 : who.Items.IndexOf(previous);
        if (pot.Collapsed || firing.Verdict == "Cracked")
        {
            // fired again and cracked: the pot that was in your bag is the cracked one now
            if (slot >= 0)
                who.Items[slot] = null;
            Kept.Remove(pot);
            KeptWhere.Remove(pot);
            return;
        }
        var item = Create(PotPiece.From(shape, pot, glaze, firing, stars, who), who);
        Kept.AddOrUpdate(pot, item);
        LastFired = (item, stars);
        if (slot >= 0)
        {
            who.Items[slot] = item;
            KeptWhere.AddOrUpdate(pot, "bag");
        }
        else if (who.addItemToInventoryBool(item))
            KeptWhere.AddOrUpdate(pot, "bag");
        else
        {
            Game1.createItemDebris(item, who.getStandingPosition(), who.FacingDirection, who.currentLocation);
            KeptWhere.AddOrUpdate(pot, "floor");
        }
    }

    public static Furniture? KeptFor(ClayPot pot) => Kept.TryGetValue(pot, out var item) ? item : null;

    // On the results: the piece, and where it went.
    public static void DrawKept(SpriteBatch b, ClayPot pot, int x, int y, int width)
    {
        if (!Kept.TryGetValue(pot, out var item))
            return;
        KeptWhere.TryGetValue(pot, out string? where);
        // (wrapped to the panel: the full-bag line is long)
        string said = Game1.parseText(where == "floor" ? "Your bag's full: it's at your feet." : "Into your bag:", Game1.smallFont, width);
        Ui.Text(b, said, new Vector2(x, y), Game1.textColor * 0.7f);
        int icon = 72;
        var box = new Rectangle(x, y + (int)Game1.smallFont.MeasureString(said).Y + 4, icon, icon);
        if (PotPieceRenderer.TextureFor(item) is { } sprite)
        {
            float scale = Math.Min(4f, MathF.Floor(icon / (float)Math.Max(sprite.Bounds.Width, sprite.Bounds.Height)));
            var src = sprite.Bounds;
            b.Draw(sprite.Texture, new Vector2(box.Center.X - src.Width * scale / 2f, box.Bottom - src.Height * scale), src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }
        string name = item.DisplayName;
        var at = new Vector2(box.Right + 12, box.Center.Y - 14);
        float fit = Math.Min(1f, (x + width - at.X) / Math.Max(1f, Ui.TextWidth(name)));
        Ui.Text(b, name, at, null, Math.Max(0.75f, fit));
    }

    // ---- Into the game ----

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => PotPieceRenderer.ClearCache();

        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            prefix: new HarmonyMethod(typeof(PotteryPieces), nameof(DrawPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.drawAtNonTileSpot)),
            prefix: new HarmonyMethod(typeof(PotteryPieces), nameof(DrawAtNonTileSpotPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.drawInMenu),
                new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(PotteryPieces), nameof(DrawInMenuPrefix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), "loadDisplayName"),
            postfix: new HarmonyMethod(typeof(PotteryPieces), nameof(NamePostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.getDescription)),
            postfix: new HarmonyMethod(typeof(PotteryPieces), nameof(DescriptionPostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.canBeGivenAsGift)),
            postfix: new HarmonyMethod(typeof(PotteryPieces), nameof(GiftablePostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.InitializeAtTile)),
            postfix: new HarmonyMethod(typeof(PotteryPieces), nameof(InitializeAtTilePostfix)));
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.getGiftTasteForThisItem)),
            postfix: new HarmonyMethod(typeof(PotteryPieces), nameof(GiftTastePostfix)));

        PotteryPiecesTests.Register(helper, monitor);
    }

    private static IMonitor _monitor = null!;

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        var name = e.NameWithoutLocale;
        if (name.IsEquivalentTo("Data/Furniture"))
            e.Edit(asset => AddFurniture(asset.AsDictionary<string, string>().Data));
        else if (name.IsEquivalentTo("Data/Shops"))
            e.Edit(asset => AddToShops(asset.AsDictionary<string, ShopData>().Data), AssetEditPriority.Late);
        else if (name.IsEquivalentTo(TextureAsset))
            // Nothing drawn here: each piece draws its own sprite (PotPieceRenderer). The
            // sheet is only the furniture data's footprint: a 16x16 tile and a 16x32 one.
            e.LoadFrom(() =>
            {
                var texture = new Texture2D(Game1.graphics.GraphicsDevice, 32, 32);
                texture.SetData(new Color[32 * 32]);
                return texture;
            }, AssetLoadPriority.Exclusive);
    }

    // Name/type/tilesheet size/bounding box/rotations/price/placement/display name/sprite/texture/off-limits/tags
    private static void AddFurniture(IDictionary<string, string> furniture)
    {
        string texture = TextureAsset.Replace('/', '\\');
        foreach (string shape in PotShape.Names)
        {
            bool tall = IsTall(shape);
            furniture[IdFor(shape)] = $"{IdFor(shape)}/decor/{(tall ? "1 2" : "1 1")}/1 1/1/{BaseValue(shape)}/2/{PotPiece.Title(shape)}/{(tall ? TallSprite : SmallSprite)}/{texture}/true/{ContextTag}";
        }
    }

    // Sold where paintings are (Pierre's), and at Tobin's pottery counter in the Vale.
    private static void AddToShops(IDictionary<string, ShopData> shops)
    {
        foreach (string id in new[] { "SeedShop", ModEntry.Id + "_ValePottery" })
        {
            if (!shops.TryGetValue(id, out var shop))
                continue;
            shop.SalableItemTags ??= new List<string>();
            if (!shop.SalableItemTags.Contains(ContextTag))
                shop.SalableItemTags.Add(ContextTag);
        }
    }

    // ---- Drawing ----

    private static bool DrawPrefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha, Netcode.NetVector2 ___drawPosition)
    {
        if (!IsPot(__instance))
            return true;
        try
        {
            if (__instance.isTemporarilyInvisible)
                return false;
            if (PotPieceRenderer.TextureFor(__instance) is not { } sprite)
                return true;
            var source = __instance.sourceRect.Value;
            Vector2 shake = __instance.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero;
            Vector2 position = Furniture.isDrawingLocationFurniture
                ? Game1.GlobalToLocal(Game1.viewport, ___drawPosition.Value + shake)
                : Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (source.Height * 4 - __instance.boundingBox.Height)) + shake);
            float depth = (__instance.boundingBox.Value.Bottom - 8) / 10000f;
            spriteBatch.Draw(sprite.Texture, position, null, Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
            return false;
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't draw a pot: {ex}", LogLevel.Error);
            return true;
        }
    }

    // On a table (a table's held piece is drawn here).
    private static bool DrawAtNonTileSpotPrefix(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, float layerDepth, float alpha)
    {
        if (!IsPot(__instance) || PotPieceRenderer.TextureFor(__instance) is not { } sprite)
            return true;
        spriteBatch.Draw(sprite.Texture, location, null, Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, layerDepth);
        return false;
    }

    // In menus: the piece itself, as big as fits the slot at a whole pixel scale (the
    // furniture's sprite is mostly empty round a cup), then the quality star.
    private static bool DrawInMenuPrefix(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color)
    {
        if (!IsPot(__instance))
            return true;
        try
        {
            if (PotPieceRenderer.TextureFor(__instance) is not { } sprite)
                return true;
            var src = sprite.Bounds;
            float scale = Math.Clamp(MathF.Floor(52f / Math.Max(src.Width, src.Height)), 2f, 4f) * scaleSize;
            spriteBatch.Draw(sprite.Texture, location + new Vector2(32f, 32f), src, color * transparency, 0f,
                new Vector2(src.Width / 2f, src.Height / 2f), scale, SpriteEffects.None, layerDepth);
            __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
            return false;
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't draw a pot in a menu: {ex}", LogLevel.Error);
            return true;
        }
    }

    private static void NamePostfix(Furniture __instance, ref string __result)
    {
        if (IsPot(__instance) && PotPiece.Read(__instance) is { } piece)
            __result = piece.Name;
    }

    private static void DescriptionPostfix(Furniture __instance, ref string __result)
    {
        if (IsPot(__instance) && PotPiece.Read(__instance) is { } piece)
            __result = Game1.parseText(piece.Description, Game1.smallFont, 272);
    }

    // Set down on a table, the game resets a piece from its furniture data: its price comes back.
    private static void InitializeAtTilePostfix(Furniture __instance)
    {
        if (IsPot(__instance) && PotPiece.PriceOf(__instance) is int price)
            __instance.Price = price;
    }

    // ---- Gifts ----

    // Furniture can't be given, but a pot is a gift like a painting.
    private static void GiftablePostfix(Furniture __instance, ref bool __result)
    {
        if (IsPot(__instance))
            __result = true;
    }

    // Who's fond of good pottery: the Vale's potter, and the two who use his ware every day.
    public static readonly string[] PotteryLovers = { "TobinClay", "FernAster", "BeaThistle" };

    private static void GiftTastePostfix(NPC __instance, Item item, ref int __result)
    {
        if (!IsPot(item))
            return;
        try
        {
            __result = PotTaste(__instance, item);
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't work out a gift taste for a pot: {ex}", LogLevel.Error);
        }
    }

    // Painting's rule (ArtHooks.PaintingTaste): nobody dislikes art. Iridium is loved by
    // everyone, silver and gold liked, a plain piece liked by art lovers and shrugged at by
    // the rest. Tobin, Fern and Bea like any piece and love a gold one.
    public static int PotTaste(NPC npc, Item pot)
    {
        bool artLover = ModEntry.Config.ArtLoversLikeAnyPainting
            && Game1.content.Load<Dictionary<string, string>>(Painting.ArtItems.ArtLoversAsset).ContainsKey(npc.Name);
        if (PotteryLovers.Contains(npc.Name))
            return pot.Quality >= PaintingRules.Gold ? NPC.gift_taste_love : NPC.gift_taste_like;
        return pot.Quality switch
        {
            PaintingRules.Iridium => NPC.gift_taste_love,
            PaintingRules.Gold or PaintingRules.Silver => NPC.gift_taste_like,
            _ => artLover ? NPC.gift_taste_like : NPC.gift_taste_neutral,
        };
    }
}
#endif
