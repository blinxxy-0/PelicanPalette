using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TokenizableStrings;

namespace ArtStudio.Painting;

// Animals (1.3): your pet, your horse and the farm animals you own, painted
// from any easel the way creatures are, from the animal's own sprite (side
// on, its first walking frame facing right) in your colours. An animal's
// subject is "animal:<type>": a farm animal's type ("White Chicken"), a pet
// type ("Cat"), or "Horse". A pet is painted as your own pet's breed.
internal static class Animals
{
    public const string Prefix = "animal:";
    public const string Horse = "Horse";

    public static bool IsAnimal(string? subject) => subject?.StartsWith(Prefix) == true;

    public static string SubjectFor(string type) => Prefix + type;

    public static string TypeOf(string subject) => subject.Substring(Prefix.Length);

    private static bool IsPet(string type) => Game1.petData?.ContainsKey(type) == true;

    // Every animal the record lists: the farm animals, then the pets, then
    // the horse, in the game's own order.
    public static IEnumerable<string> AllTypes()
    {
        foreach (string type in Game1.farmAnimalData.Keys)
            yield return type;
        foreach (string type in Game1.petData.Keys)
            yield return type;
        yield return Horse;
    }

    // ---- Who you own ----

    // The animals on the farm now (anyone's: the farm is shared), by type,
    // and each pet type's breed.
    private static (HashSet<string> Types, Dictionary<string, string> Breeds) Owned()
    {
        var types = new HashSet<string>();
        var breeds = new Dictionary<string, string>();
        Utility.ForEachLocation(location =>
        {
            foreach (var animal in location.animals.Values)
                types.Add(animal.type.Value);
            foreach (var character in location.characters)
            {
                if (character is Pet pet && pet.petType.Value is { } petType)
                {
                    types.Add(petType);
                    breeds.TryAdd(petType, pet.whichBreed.Value);
                }
                else if (character is StardewValley.Characters.Horse)
                    types.Add(Horse);
            }
            return true;
        });
        return (types, breeds);
    }

    public static bool Unlocked(string type) => Owned().Types.Contains(type);

    public static List<string> Available() =>
        Owned().Types is var owned ? AllTypes().Where(t => owned.Contains(t) && Frame(t) != null).ToList() : new();

    // ---- Names and worth ----

    public static string DisplayName(string type)
    {
        if (type == Horse) return I18n.Get("animal.horse");
        string? raw = Game1.farmAnimalData.TryGetValue(type, out var farm) ? farm.DisplayName
            : Game1.petData.TryGetValue(type, out var pet) ? pet.DisplayName : null;
        return string.IsNullOrEmpty(raw) ? type : TokenParser.ParseText(raw);
    }

    // Like a creature's: an animal that costs more to buy is worth more to paint.
    public static int SubjectPrice(string subject)
    {
        string type = TypeOf(subject);
        if (Game1.farmAnimalData.TryGetValue(type, out var farm))
            return farm.PurchasePrice > 0 ? Math.Clamp(farm.PurchasePrice / 3, 200, 600) : 700;   // (the ones you can't buy are rarer)
        return 400;
    }

    // ---- The sprite ----

    // The sheet and its frame size; a pet's is your own pet's breed (or the first).
    private static (Texture2D Texture, int Width, int Height)? Sheet(string type)
    {
        string? path;
        int w = 32, h = 32;
        if (type == Horse)
            path = "Animals\\horse";
        else if (Game1.farmAnimalData.TryGetValue(type, out var farm))
            (path, w, h) = (farm.Texture, farm.SpriteWidth, farm.SpriteHeight);
        else if (Game1.petData.TryGetValue(type, out var pet) && pet.Breeds?.Count > 0)
        {
            string? breed = Owned().Breeds.TryGetValue(type, out string? b) ? b : null;
            path = (pet.Breeds.FirstOrDefault(x => x.Id == breed) ?? pet.Breeds[0]).Texture;
        }
        else
            return null;
        try
        {
            return string.IsNullOrEmpty(path) ? null : (Game1.content.Load<Texture2D>(path), w, h);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Facing right: the first frame of each sheet's second row, without the
    // see-through shadow some sheets draw under the animal.
    private const int Solid = 255;

    public static (Texture2D Texture, Rectangle Source)? Frame(string type)
    {
        if (Sheet(type) is not { } sheet) return null;
        var frame = new Rectangle(0, sheet.Height, sheet.Width, sheet.Height);
        return Creatures.Trim(sheet.Texture, frame, Solid) is { } source ? (sheet.Texture, source) : null;
    }

    // Whole, or, for the few that still read well smaller (picked by eye),
    // shrunk to fit from a medium canvas up. The rest of the big ones (the
    // cows, the pig, the horse) lose their outline shrunk: large only.
    private static readonly HashSet<string> Shrinks = new() { "Goat", "Sheep", "Ostrich" };

    public static bool Fits(string type, int canvasSize) =>
        Frame(type) is { } frame && (frame.Source.Width <= canvasSize - 2 && frame.Source.Height <= canvasSize - 2
            || Shrinks.Contains(type) && canvasSize >= PaintingRules.CanvasSize(CanvasKind.Medium));

    public static void Draw(SpriteBatch b, string type, Rectangle area, Color? multiply = null)
    {
        if (Frame(type) is { } frame)
            Creatures.DrawSprite(b, frame.Texture, frame.Source, Color.White, area, multiply);
    }

    public static Reference Reference(string type, int canvasSize, IReadOnlyList<int> owned, Techniques allowed = Techniques.None)
    {
        var (texture, source) = Frame(type) ?? throw new InvalidOperationException($"No sprite for {type}.");
        return Creatures.ReferenceFrom(texture, source, Color.White, canvasSize, owned, allowed, shrink: true, minAlpha: Solid);
    }
}

// Studies: creatures and animals, the subjects painted from a sprite and
// picked from a panel in place of the bag. Each kind's subjects begin with
// its prefix, which is also its slot in the setup screen's row.
internal static class Studies
{
    // (And restorations, 1.3: an old painting's slot opens the damaged ones in your bag.)
    public static bool IsStudy(string? subject) => Creatures.IsCreature(subject) || Animals.IsAnimal(subject) || OldPaintings.IsOld(subject);

    public static List<string> Available(Farmer who, string prefix) => prefix == Animals.Prefix
        ? Animals.Available().Select(Animals.SubjectFor).ToList()
        : prefix == OldPaintings.Prefix ? (OldPaintings.CanRestore(who) ? OldPaintings.InBag(who) : new List<string>())
        : Creatures.Available(who).Select(Creatures.SubjectFor).ToList();

    public static string DisplayName(string subject) => OldPaintings.OfSubject(subject) is { } old ? OldPaintings.Title(old)
        : Animals.IsAnimal(subject) ? Animals.DisplayName(Animals.TypeOf(subject)) : Creatures.DisplayName(Creatures.NameOf(subject));

    // An old painting is restored on its own canvas, so only that size.
    public static bool Fits(string subject, int canvasSize) => OldPaintings.OfSubject(subject) is { } old ? old.Size == canvasSize
        : Animals.IsAnimal(subject) ? Animals.Fits(Animals.TypeOf(subject), canvasSize) : Creatures.Fits(Creatures.NameOf(subject), canvasSize);

    public static void Draw(SpriteBatch b, string subject, Rectangle area, Color? multiply = null)
    {
        if (OldPaintings.OfSubject(subject) is { } old) OldPaintings.Draw(b, old, area, multiply);
        else if (Animals.IsAnimal(subject)) Animals.Draw(b, Animals.TypeOf(subject), area, multiply);
        else Creatures.Draw(b, Creatures.NameOf(subject), area, multiply);
    }

    public static Reference Reference(string subject, int canvasSize, IReadOnlyList<int> owned, Techniques allowed) =>
        OldPaintings.OfSubject(subject) is { } old ? OldPaintings.DamagedReference(old)
        : Animals.IsAnimal(subject) ? Animals.Reference(Animals.TypeOf(subject), canvasSize, owned, allowed)
        : Creatures.Reference(Creatures.NameOf(subject), canvasSize, owned, allowed);

    public static int SubjectPrice(string subject) => OldPaintings.OfSubject(subject) is { } old ? OldPaintings.SetValue(old.Set)
        : Animals.IsAnimal(subject) ? Animals.SubjectPrice(subject) : Creatures.SubjectPrice(subject);

    public static string Title(string subject) => OldPaintings.OfSubject(subject) is { } old ? OldPaintings.Title(old)
        : I18n.Get(Animals.IsAnimal(subject) ? "painting.title.animal" : "painting.title.creature", new { name = DisplayName(subject) });

    public static string Provenance(string subject) => I18n.Get(Animals.IsAnimal(subject) ? "painting.provenance.animal" : "painting.provenance.creature", new { name = DisplayName(subject) });
}
