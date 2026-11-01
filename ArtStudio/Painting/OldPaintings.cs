using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Old paintings (1.3, docs/design/restoration_mechanics.md): 21 paintings in
// five sets, found damaged around the valley and restored, by Margot for a
// fee or, with Painting Mastery, at your own easel.
//
// Each painting's picture is a small image in the mod's pigments
// (assets/old_paintings/<id>.png, 28 or 36 square). Its damage is fixed:
// flaked patches, a tear and a water stain, about a third of it, worked out
// from its id so it's the same every time.
//
// A damaged painting is its own item, one per set (so content can drop one
// from a set: "(O)blinxxy.MuseArt_DamagedPainting_wreck"). Which painting it
// is, is chosen when it reaches someone's bag, preferring ones they haven't
// found. It carries its damaged picture as a painting does, so it draws as
// one; it's not a painting to anything else (the shop, the book, orders).
// A restored one is an ordinary painting whose subject is "old:<id>".
internal sealed record OldPainting(string Id, string Set, int Size);

internal static class OldPaintings
{
    public const string Prefix = "old:";
    public const string ItemPrefix = ModEntry.Id + "_DamagedPainting_";
    private const string IdKey = ModEntry.Id + "/OldPainting";
    private const string FoundKey = ModEntry.Id + "/OldFound";
    // Restoring at your own easel comes with Painting Mastery (Margot, step 6).
    public const string MasteryFlag = ModEntry.Id + "_PaintingMastery";

    public static readonly string[] Sets = { "valley", "wreck", "miners", "calico", "dwarvish" };

    public static readonly OldPainting[] All =
    {
        new("valley_square", "valley", 28), new("valley_farm", "valley", 28), new("valley_mill", "valley", 28),
        new("valley_lewis", "valley", 28), new("valley_harvest", "valley", 28),
        new("wreck_ship", "wreck", 28), new("wreck_storm", "wreck", 28), new("wreck_harbour", "wreck", 28),
        new("wreck_captain", "wreck", 28), new("wreck_lighthouse", "wreck", 28),
        new("miners_town", "miners", 28), new("miners_family", "miners", 28), new("miners_picnic", "miners", 28),
        new("miners_entrance", "miners", 28), new("miners_cavern", "miners", 28),
        new("calico_city", "calico", 36), new("calico_market", "calico", 36), new("calico_queen", "calico", 36),
        new("calico_caravan", "calico", 36),
        new("dwarvish_forge", "dwarvish", 36), new("dwarvish_king", "dwarvish", 36),
    };

    private static readonly Dictionary<string, OldPainting> ById = All.ToDictionary(p => p.Id);

    // What a restored painting of each set is worth before quality.
    public static int SetValue(string set) => set switch
    {
        "valley" => 1500,
        "wreck" or "miners" => 2000,
        "calico" => 4000,
        _ => 6000,
    };

    private static IModHelper? _helper;

    public static void Register(IModHelper helper)
    {
        _helper = helper;
        helper.Events.Player.InventoryChanged += (_, e) =>
        {
            if (e.IsLocalPlayer)
                foreach (var item in e.Added)
                    Assign(e.Player, item);
        };
    }

    // ---- Which painting ----

    public static OldPainting? Get(string id) => ById.GetValueOrDefault(id);

    public static bool IsOld(string? subject) => subject?.StartsWith(Prefix) == true;

    public static string SubjectFor(string id) => Prefix + id;

    public static OldPainting? OfSubject(string subject) => IsOld(subject) ? Get(subject.Substring(Prefix.Length)) : null;

    public static string Title(OldPainting painting) => I18n.Get("old." + painting.Id);

    public static string SetName(string set) => I18n.Get("old.set." + set);

    // ---- The pictures ----

    private static readonly Dictionary<string, byte[,]> Originals = new();
    private static readonly Dictionary<string, bool[,]> Damages = new();

    // The painting as it was, in pigment indices.
    public static byte[,] Original(OldPainting painting)
    {
        if (Originals.TryGetValue(painting.Id, out var cached)) return cached;
        int n = painting.Size;
        var cells = PixelCanvas.NewBlank(n);
        var texture = _helper?.ModContent.Load<Texture2D>($"assets/old_paintings/{painting.Id}.png");
        if (texture != null)
        {
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            var all = Enumerable.Range(0, Palette.All.Length).ToList();
            for (int x = 0; x < Math.Min(n, texture.Width); x++)
            for (int y = 0; y < Math.Min(n, texture.Height); y++)
            {
                var p = pixels[y * texture.Width + x];
                cells[x, y] = (byte)all.OrderBy(i => Sq(Palette.All[i].R - p.R) + Sq(Palette.All[i].G - p.G) + Sq(Palette.All[i].B - p.B)).First();
            }
        }
        return Originals[painting.Id] = cells;
    }

    private static int Sq(int v) => v * v;

    // Its damage: true where the paint is gone. About a third of the
    // picture, the same every time for the same painting.
    public static bool[,] Damage(OldPainting painting)
    {
        if (Damages.TryGetValue(painting.Id, out var cached)) return cached;
        int n = painting.Size;
        var damaged = new bool[n, n];
        var random = new Random(StableHash(painting.Id));
        int target = n * n / 3, count = 0;
        void Mark(int x, int y)
        {
            if (x < 0 || y < 0 || x >= n || y >= n || damaged[x, y]) return;
            damaged[x, y] = true;
            count++;
        }
        // A tear, corner to middle and on.
        int tx = random.Next(2) == 0 ? 0 : n - 1, ty = random.Next(n / 3);
        float dx = tx == 0 ? 1f : -1f, dy = 0.6f + (float)random.NextDouble() * 0.6f;
        for (float fx = tx, fy = ty; fx >= 0 && fx < n && fy < n && count < n * n / 10; fx += dx * 0.7f, fy += dy * 0.7f)
        {
            Mark((int)fx, (int)fy);
            if (random.NextDouble() < 0.4) Mark((int)fx + 1, (int)fy);
            fx += ((float)random.NextDouble() - 0.5f) * 0.6f;
        }
        // A water stain along one edge.
        int edge = random.Next(4), stainW = n / 2 + random.Next(n / 4), stainD = 2 + random.Next(n / 6);
        int start = random.Next(n - stainW);
        for (int i = 0; i < stainW; i++)
        {
            int depth = (int)(stainD * MathF.Sin(MathF.PI * i / stainW)) + random.Next(2);
            for (int d = 0; d < depth; d++)
                switch (edge)
                {
                    case 0: Mark(start + i, d); break;
                    case 1: Mark(n - 1 - d, start + i); break;
                    case 2: Mark(start + i, n - 1 - d); break;
                    default: Mark(d, start + i); break;
                }
        }
        // Flaked patches, ragged, until about a third is gone.
        while (count < target)
        {
            int cx = random.Next(n), cy = random.Next(n), r = 1 + random.Next(3);
            for (int x = cx - r; x <= cx + r; x++)
            for (int y = cy - r; y <= cy + r; y++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + random.Next(2))
                    Mark(x, y);
        }
        return Damages[painting.Id] = damaged;
    }

    // A string hash that's the same on every run (string.GetHashCode isn't).
    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in text) hash = hash * 31 + c;
            return hash;
        }
    }

    // What's left of it: the original with the damaged cells bare.
    public static byte[,] Damaged(OldPainting painting)
    {
        var cells = (byte[,])Original(painting).Clone();
        var damaged = Damage(painting);
        for (int x = 0; x < painting.Size; x++)
        for (int y = 0; y < painting.Size; y++)
            if (damaged[x, y]) cells[x, y] = PixelCanvas.Blank;
        return cells;
    }

    // ---- The damaged item ----

    public static string ItemIdFor(string set) => ItemPrefix + set;

    public static void AddObjectData(IDictionary<string, ObjectData> objects)
    {
        foreach (string set in Sets)
            objects[ItemIdFor(set)] = new ObjectData
            {
                Name = ItemIdFor(set),
                DisplayName = I18n.Get("item.damaged-painting"),
                Description = I18n.Get("item.damaged-painting.description." + set),
                Type = "Basic",
                Category = 0,
                Price = 100,
                Texture = PaintingItems.ItemsTexture,
                SpriteIndex = 0,
                CanBeGivenAsGift = false,
                ExcludeFromRandomSale = true,
                ExcludeFromShippingCollection = true,
                ContextTags = new List<string> { "muse_damaged_painting", "muse_old_" + set },
            };
    }

    public static bool IsDamaged(Item? item) => item?.ItemId.StartsWith(ItemPrefix) == true;

    public static OldPainting? Of(Item item) =>
        IsDamaged(item) && item.modData.TryGetValue(IdKey, out string id) ? Get(id) : null;

    // A damaged painting of a particular one.
    public static SObject CreateDamaged(OldPainting painting)
    {
        var item = ItemRegistry.Create<SObject>("(O)" + ItemIdFor(painting.Set));
        Write(item, painting);
        return item;
    }

    private static void Write(Item item, OldPainting painting)
    {
        item.modData[IdKey] = painting.Id;
        new PaintingData
        {
            Size = painting.Size,
            Cells = Damaged(painting),
            Purity = new Purity[painting.Size, painting.Size],
            Subject = SubjectFor(painting.Id),
        }.WriteTo(item);
    }

    // A damaged painting reaching a bag without being one in particular yet
    // (from a dig, a chest, the cart) becomes one of its set: one not found
    // before if there's any left.
    public static void Assign(Farmer who, Item item)
    {
        if (!IsDamaged(item) || Of(item) != null) return;
        string set = item.ItemId.Substring(ItemPrefix.Length);
        var inSet = All.Where(p => p.Set == set).ToList();
        if (inSet.Count == 0) return;
        var found = Found(who);
        var fresh = inSet.Where(p => !found.Contains(p.Id)).ToList();
        var pick = (fresh.Count > 0 ? fresh : inSet)[Game1.random.Next(fresh.Count > 0 ? fresh.Count : inSet.Count)];
        Write(item, pick);
        MarkFound(who, pick.Id);
    }

    public static HashSet<string> Found(Farmer who) =>
        who.modData.TryGetValue(FoundKey, out string raw) ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet() : new();

    public static bool FoundAny(Farmer who) => Found(who).Count > 0;

    private static void MarkFound(Farmer who, string id)
    {
        var found = Found(who);
        if (found.Add(id))
            who.modData[FoundKey] = string.Join(",", found);
    }

    // ---- Restoring ----

    public static bool CanRestore(Farmer who) => who.mailReceived.Contains(MasteryFlag);

    // The damaged paintings in a bag, one of each.
    public static List<string> InBag(Farmer who) =>
        who.Items.Where(IsDamaged).Select(Of).Where(p => p != null).Select(p => SubjectFor(p!.Id)).Distinct().ToList();

    public static Reference DamagedReference(OldPainting painting) => new(Damaged(painting));

    public static Texture2D? DamagedTexture(OldPainting painting)
    {
        if (Textures.TryGetValue(painting.Id, out var cached) && !cached.IsDisposed) return cached;
        var texture = PaintingRenderer.Build(painting.Size, Damaged(painting), new Purity[painting.Size, painting.Size], new Color(214, 200, 170));
        return Textures[painting.Id] = texture;
    }

    private static readonly Dictionary<string, Texture2D> Textures = new();

    public static void Draw(SpriteBatch b, OldPainting painting, Rectangle area, Color? multiply = null)
    {
        if (DamagedTexture(painting) is { } texture)
            b.Draw(texture, area, (multiply ?? Color.White));
    }

    // A restored painting's worth: its set's, by quality.
    public static int RestoredValue(OldPainting painting, int quality) =>
        (int)Math.Round(SetValue(painting.Set) * quality switch
        {
            >= PaintingRules.Iridium => 2f,
            >= PaintingRules.Gold => 1.5f,
            >= PaintingRules.Silver => 1.25f,
            _ => 1f,
        });

    // Takes one damaged painting of this one from the bag (once it's restored).
    public static void UseUp(Farmer who, OldPainting painting)
    {
        var item = who.Items.FirstOrDefault(i => i != null && Of(i)?.Id == painting.Id);
        if (item != null) who.removeItemFromInventory(item);
    }
}
