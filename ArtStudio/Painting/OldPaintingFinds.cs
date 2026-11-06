using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;

namespace ArtStudio.Painting;

// Where damaged old paintings turn up (1.3, restoration_mechanics.md §2):
//   the Old Valley   artifact spots, 1.5% a dig
//   the Wreck        fishing treasure chests, 4% a chest
//   the Miners       mine crates and barrels, 0.4% to 0.8% deeper down
//   Calico           Skull Cavern crates 1%; its treasure rooms 10%
//   the Dwarves      Volcano chests, 8%
// and the Travelling Cart sometimes sells one (data/old_paintings.json).
//
// The first is guaranteed: from Painting level 3, the next artifact spot
// or fishing treasure brings one, and keeps doing so (whenever you've none
// in your bag) until it's been shown to Gunther and he's written for Margot. Wearing Margot's
// loupe (twelve donated to the museum) makes each chance half as big again.
internal static class OldPaintingFinds
{
    public const float ArtifactChance = 0.015f, TreasureChance = 0.04f, CalicoCrateChance = 0.01f, TreasureRoomChance = 0.1f, VolcanoChance = 0.08f;
    public const int FirstFromLevel = 3;
    private const string FirstGivenFlag = ModEntry.Id + "_OldPaintingFirst";
    public const string LoupeId = ModEntry.Id + "_MargotsLoupe";
    public const float LoupeBoost = 1.5f;

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.digUpArtifactSpot)),
            postfix: new HarmonyMethod(typeof(OldPaintingFinds), nameof(DigPostfix)));
        foreach (string end in new[] { nameof(FishingRod.openTreasureMenuEndFunction), nameof(FishingRod.openChestEndFunction) })
            harmony.Patch(AccessTools.Method(typeof(FishingRod), end),
                postfix: new HarmonyMethod(typeof(OldPaintingFinds), nameof(TreasurePostfix)));
        harmony.Patch(AccessTools.Method(typeof(BreakableContainer), nameof(BreakableContainer.releaseContents)),
            postfix: new HarmonyMethod(typeof(OldPaintingFinds), nameof(CratePostfix)));
        harmony.Patch(AccessTools.Method(typeof(MineShaft), nameof(MineShaft.getTreasureRoomItem)),
            postfix: new HarmonyMethod(typeof(OldPaintingFinds), nameof(TreasureRoomPostfix)));
        harmony.Patch(AccessTools.Method(typeof(VolcanoDungeon), nameof(VolcanoDungeon.PopulateChest)),
            postfix: new HarmonyMethod(typeof(OldPaintingFinds), nameof(VolcanoPostfix)));
    }

    // The guaranteed first one is due: from Painting level 3, until someone's
    // shown Gunther one and he's written for Margot, whenever there's none in
    // your bag. (So one lost, trashed or left in a chest before he's seen it
    // is just replaced by the next dig or fishing chest; Margot never waits
    // on luck.)
    public static bool FirstDue(Farmer who) =>
        ArtProgress.Level(who) >= FirstFromLevel && !Social.Margot.Arrived && !Social.Margot.LetterSent
        && !who.Items.Any(OldPaintings.IsDamaged);

    // A find's chance for this farmer: the loupe, worn, raises it.
    public static float Chance(Farmer? who, float chance) =>
        who != null && who.isWearingRing(LoupeId) ? chance * LoupeBoost : chance;

    private static bool Roll(Farmer who, float chance)
    {
        if (FirstDue(who))
        {
            who.mailReceived.Add(FirstGivenFlag);
            return true;
        }
        return Game1.random.NextDouble() < Chance(who, chance);
    }

    private static Item New(string set) => ItemRegistry.Create("(O)" + OldPaintings.ItemIdFor(set));

    private static void DigPostfix(GameLocation __instance, int xLocation, int yLocation, Farmer who)
    {
        try
        {
            if (who != null && Roll(who, ArtifactChance))
                Game1.createItemDebris(New("valley"), new Vector2(xLocation, yLocation) * 64f, -1, __instance);
        }
        catch (Exception) { }
    }

    // A fishing treasure chest's menu is open: in with the rest of it.
    private static void TreasurePostfix(FishingRod __instance)
    {
        try
        {
            if (Game1.activeClickableMenu is ItemGrabMenu menu && menu.context == __instance && Roll(Game1.player, TreasureChance))
                menu.ItemsToGrabMenu.actualInventory.Add(New("wreck"));
        }
        catch (Exception) { }
    }

    private static void CratePostfix(BreakableContainer __instance, Farmer who)
    {
        try
        {
            if (__instance.Location is not MineShaft mine) return;
            bool cavern = mine.mineLevel > MineShaft.bottomOfMineLevel;
            float chance = cavern ? CalicoCrateChance : 0.004f + 0.004f * Math.Min(1f, mine.mineLevel / (float)MineShaft.bottomOfMineLevel);
            if (Game1.random.NextDouble() < Chance(who, chance))
                Game1.createItemDebris(New(cavern ? "calico" : "miners"), __instance.TileLocation * 64f, -1, mine);
        }
        catch (Exception) { }
    }

    private static void TreasureRoomPostfix(ref Item __result)
    {
        if (Game1.random.NextDouble() < Chance(Game1.player, TreasureRoomChance))
            __result = New("calico");
    }

    private static void VolcanoPostfix(System.Collections.Generic.IList<Item> items)
    {
        if (Game1.random.NextDouble() < Chance(Game1.player, VolcanoChance))
            items.Add(New("dwarvish"));
    }
}
