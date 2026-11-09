#if DEBUG
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ArtStudio.Painting;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace ArtStudio.Vale;

// The Vale's own fish (1.4, Debug only; docs/superpowers/specs/2026-11-08-vale-fish-design.md).
// museart_fishdump writes the game's fish data, as loaded (so with every mod's edits), to
// fishdump/ in the mod folder: Data/Fish, Data/FishPondData, Data/AquariumFish, the Fish and
// FishAreas of the Forest, Mountain, Town and the Vale, and every fish sprite.
// museart_fishtest checks the five in a running game: their data, art, collection, painting
// record, pond and tank, and that each one bites in its own water, season, weather and time
// and nowhere else (many draws from the game's own fish picker at a tile in each water).
internal static class ValeFish
{
    private const string Vale = ModEntry.Id + "_GildedVale";
    private const string Prefix = "(O)blinxxy.MuseArt.Vale_";

    private enum Water { River, Pool, Basin, Lake }

    // A bobber tile in each of the Vale's waters (GildedVale.tmx, and data/vale_fish.json's areas).
    private static readonly Dictionary<Water, Vector2> Tiles = new()
    {
        [Water.River] = new Vector2(61, 18),
        [Water.Pool] = new Vector2(61, 30),
        [Water.Basin] = new Vector2(82, 4),
        [Water.Lake] = new Vector2(65, 63),
    };

    // Each fish: where it bites and one season, weather and time when it should; then a season
    // and a time when it shouldn't (each tried with the rest as when it should), and whether
    // the other weather should stop it.
    private sealed record Fish(string Id, Water Water, Season Season, bool Rain, int Time, Season OffSeason, int OffTime, bool WeatherMatters);

    private static readonly Fish[] All =
    {
        new(Prefix + "ClaybankLoach", Water.River, Season.Summer, false, 1000, Season.Winter, 2200, false),
        new(Prefix + "CascadeTrout", Water.Pool, Season.Spring, true, 1000, Season.Summer, 2200, true),
        new(Prefix + "FallsSculpin", Water.Basin, Season.Winter, false, 2200, Season.Summer, 1000, false),
        new(Prefix + "LakeblueShiner", Water.Lake, Season.Summer, false, 1200, Season.Fall, 2000, true),
        new(Prefix + "GiltCarp", Water.Lake, Season.Fall, false, 1200, Season.Summer, 2000, true),
    };

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_fishtest",
            "Checks the Vale's own fish: data, art, collection, painting record, pond, tank, and where and when each one bites.",
            (_, _) => Test(monitor));
        DevTools.AddCommand(helper, "museart_fishdump",
            "Writes Data/Fish, Data/FishPondData, Data/AquariumFish and some locations' fish to fishdump/ in the mod folder.",
            (_, _) => Dump(helper, monitor));
    }

    private static void Dump(IModHelper helper, IMonitor monitor)
    {
        void Write(string name, object value) => helper.Data.WriteJsonFile($"fishdump/{name}.json", value);

        Write("Fish", DataLoader.Fish(Game1.content));
        Write("FishPondData", DataLoader.FishPondData(Game1.content));
        Write("AquariumFish", DataLoader.AquariumFish(Game1.content));
        var locations = DataLoader.Locations(Game1.content);
        var wanted = new[] { "Default", "Forest", "Mountain", "Town", "Woods", "Desert", ModEntry.Id + "_GildedVale" };
        Write("Locations", wanted.Where(locations.ContainsKey)
            .ToDictionary(id => id, id => new { locations[id].FishAreas, locations[id].Fish }));
        var objects = DataLoader.Objects(Game1.content);
        Write("FishObjects", objects.Where(kv => kv.Value.Category == StardewValley.Object.FishCategory)
            .ToDictionary(kv => kv.Key, kv => kv.Value));
        // The sprites to draw from: every fish icon as itself, and the aquarium sheet.
        string sprites = Path.Combine(helper.DirectoryPath, "fishdump", "sprites");
        Directory.CreateDirectory(sprites);
        foreach (string id in objects.Where(kv => kv.Value.Category == StardewValley.Object.FishCategory).Select(kv => kv.Key))
        {
            var data = ItemRegistry.GetDataOrErrorItem("(O)" + id);
            SaveCrop(data.GetTexture(), data.GetSourceRect(), Path.Combine(sprites, id + ".png"));
        }
        var aquarium = Game1.content.Load<Texture2D>("LooseSprites\\AquariumFish");
        SaveCrop(aquarium, aquarium.Bounds, Path.Combine(sprites, "AquariumFish.png"));
        monitor.Log($"Fish data written to {Path.Combine(helper.DirectoryPath, "fishdump")}.", LogLevel.Info);
    }

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static void Test(IMonitor monitor)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first (museart_devgame).", LogLevel.Warn);
            return;
        }
        var who = Game1.player;
        var vale = Game1.getLocationFromName(Vale);
        Check(monitor, "the Vale is loaded", vale != null);
        if (vale == null) return;

        var fishData = DataLoader.Fish(Game1.content);
        var aquarium = DataLoader.AquariumFish(Game1.content);
        var tank = DataLoader.Furniture(Game1.content).Keys.Select(id => ItemRegistry.Create("(F)" + id)).OfType<FishTankFurniture>().FirstOrDefault();
        foreach (var fish in All)
        {
            string id = fish.Id.Substring(3);
            var item = ItemRegistry.Create(fish.Id);
            bool isObject = Game1.objectData.TryGetValue(id, out var obj);
            Check(monitor, $"{id} is a fish in the collection", isObject && obj!.Category == StardewValley.Object.FishCategory && !obj.ExcludeFromFishingCollection);
            var parsed = ItemRegistry.GetDataOrErrorItem(fish.Id);
            Check(monitor, $"{id} has its own 16x16 icon", !parsed.IsErrorItem && parsed.GetSourceRect().Width == 16 && parsed.GetTexture() != null,
                $"{parsed.TextureName} {parsed.GetSourceRect()}");
            Check(monitor, $"{id} has a name and description", item.DisplayName.Length > 0 && !item.DisplayName.Contains("blinxxy") && item.getDescription().Length > 10, item.DisplayName);
            Check(monitor, $"{id} has fishing data", fishData.ContainsKey(id));
            Check(monitor, $"{id} goes in the painting record's Fish tab",
                RecordTabs.TabOfItem(fish.Id) == RecordTab.Fish && RecordTabs.Members(who, RecordTab.Fish).Any(m => m.Key == fish.Id));
            var pond = FishPond.GetRawData(id);
            Check(monitor, $"{id} can live in a Fish Pond and gives roe", pond != null && pond.ProducedItems.Any(p => p.ItemId == "(O)812"), pond?.Id ?? "no pond data");
            Check(monitor, $"{id} can go in a fish tank", aquarium.ContainsKey(id) && tank != null && tank.CanBeDeposited(item), aquarium.GetValueOrDefault(id) ?? "no aquarium data");
        }
        // The extras the ponds give (data/vale_fish.json); the Trout and Shiner give roe only.
        Check(monitor, "a Loach pond sometimes gives Clay", FishPond.GetRawData("blinxxy.MuseArt.Vale_ClaybankLoach")?.ProducedItems.Any(p => p.ItemId == "(O)330") == true);
        Check(monitor, "a Gilt Carp pond sometimes gives Gold Ore", FishPond.GetRawData("blinxxy.MuseArt.Vale_GiltCarp")?.ProducedItems.Any(p => p.ItemId == "(O)384") == true);

        // Where and when they bite: the game's own picker, many times at each water's tile.
        var season = Game1.season;
        int time = Game1.timeOfDay, level = who.fishingLevel.Value;
        var weather = vale.GetWeather();
        bool rain = weather.IsRaining, worldRain = Game1.isRaining;
        try
        {
            who.fishingLevel.Value = 10;
            foreach (var fish in All)
            {
                string name = fish.Id.Substring(Prefix.Length);
                var on = Catches(vale, who, fish.Water, fish.Season, fish.Rain, fish.Time);
                Check(monitor, $"{name} bites in the {fish.Water} ({fish.Season}, {(fish.Rain ? "rain" : "sun")}, {fish.Time})",
                    on.GetValueOrDefault(fish.Id) > 0, Summary(on));
                var offSeason = Catches(vale, who, fish.Water, fish.OffSeason, fish.Rain, fish.Time);
                Check(monitor, $"{name} doesn't bite in {fish.OffSeason} (season {vale.GetSeason()})", offSeason.GetValueOrDefault(fish.Id) == 0, Summary(offSeason));
                var offTime = Catches(vale, who, fish.Water, fish.Season, fish.Rain, fish.OffTime);
                Check(monitor, $"{name} doesn't bite at {fish.OffTime}", offTime.GetValueOrDefault(fish.Id) == 0, Summary(offTime));
                var otherWeather = Catches(vale, who, fish.Water, fish.Season, !fish.Rain, fish.Time);
                Check(monitor, fish.WeatherMatters ? $"{name} doesn't bite in {(fish.Rain ? "sun" : "rain")}" : $"{name} bites in rain or sun",
                    (otherWeather.GetValueOrDefault(fish.Id) == 0) == fish.WeatherMatters, Summary(otherWeather));
                // The Loach lives in the whole river, so the pools and the basin are its water too.
                foreach (var water in Tiles.Keys.Where(w => w != fish.Water && !(fish.Water == Water.River && w is Water.Pool or Water.Basin)))
                {
                    var elsewhere = Catches(vale, who, water, fish.Season, fish.Rain, fish.Time);
                    Check(monitor, $"{name} doesn't bite in the {water}", elsewhere.GetValueOrDefault(fish.Id) == 0, Summary(elsewhere));
                }
            }
            // The game's own fish bite too: river fish in the river, lake fish in the lake.
            var river = Catches(vale, who, Water.River, Season.Spring, false, 1000);
            Check(monitor, "the game's river fish bite in the Vale's river", river.Keys.Count(k => k is "(O)132" or "(O)702" or "(O)145" or "(O)143" or "(O)706") >= 2, Summary(river));
            var lake = Catches(vale, who, Water.Lake, Season.Spring, false, 1000);
            Check(monitor, "the game's lake fish bite in the Vale's lake", lake.Keys.Count(k => k is "(O)136" or "(O)142" or "(O)700") >= 2, Summary(lake));
            Check(monitor, "no river-only fish in the lake (Bream, Shad)", !lake.ContainsKey("(O)132") && !lake.ContainsKey("(O)706"), Summary(lake));
        }
        finally
        {
            who.fishingLevel.Value = level;
            Game1.season = season;
            Game1.timeOfDay = time;
            weather.IsRaining = rain;
            Game1.isRaining = worldRain;
        }
    }

    // What the game's picker gives at one water's tile, in a given season, weather and time.
    private static Dictionary<string, int> Catches(GameLocation vale, Farmer who, Water water, Season season, bool rain, int time, int draws = 600)
    {
        Game1.season = season;
        Game1.timeOfDay = time;
        vale.GetWeather().IsRaining = rain;
        Game1.isRaining = rain;
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < draws; i++)
        {
            var item = GameLocation.GetFishFromLocationData(vale.Name, Tiles[water], 5, who, false, false, vale);
            if (item == null) continue;
            counts[item.QualifiedItemId] = counts.GetValueOrDefault(item.QualifiedItemId) + 1;
        }
        return counts;
    }

    private static string Summary(Dictionary<string, int> counts) =>
        string.Join(", ", counts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key.Replace(Prefix, "")} {kv.Value}"));

    private static void SaveCrop(Texture2D texture, Rectangle rect, string path)
    {
        var pixels = new Color[rect.Width * rect.Height];
        texture.GetData(0, rect, pixels, 0, pixels.Length);
        using var crop = new Texture2D(Game1.graphics.GraphicsDevice, rect.Width, rect.Height);
        crop.SetData(pixels);
        using var stream = File.Create(path);
        crop.SaveAsPng(stream, rect.Width, rect.Height);
    }
}
#endif
