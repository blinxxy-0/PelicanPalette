using System;
using System.Linq;
using ArtStudio.Painting;
using HarmonyLib;
using StardewValley;
using StardewValley.Internal;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;

namespace ArtStudio.Social;

// Special orders that ask for paintings (1.3, data/special_orders.json).
//
// A "Deliver" objective only takes everything still owed in one stack (ten
// hardwood at once), and paintings never stack, so "three portraits" could
// never be handed over. For a painting, a Deliver objective takes it one at
// a time instead; anything else is left to the game.
internal static class PaintingOrders
{
    private static readonly AccessTools.FieldRef<OrderObjective, SpecialOrder> OrderOf =
        AccessTools.FieldRefAccess<OrderObjective, SpecialOrder>("_order");

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(DeliverObjective), nameof(DeliverObjective.OnItemDelivered)),
            prefix: new HarmonyMethod(typeof(PaintingOrders), nameof(DeliverPrefix)));
    }

    private static bool DeliverPrefix(DeliverObjective __instance, Farmer farmer, NPC npc, Item item, bool probe, ref int __result)
    {
        try
        {
            if (!PaintingItems.IsPainting(item) || __instance.IsComplete() || npc.Name != __instance.targetName.Value || !Accepts(__instance, item))
                return true;
            __result = 1;
            if (probe) return false;
            var one = item.getOne();
            OrderOf(__instance)?.donatedItems.Add(one);
            item.Stack--;
            __instance.IncrementCount(1);
            if (!string.IsNullOrEmpty(__instance.message.Value))
            {
                npc.CurrentDialogue.Push(new Dialogue(npc, null, __instance.message.Value));
                Game1.drawDialogue(npc);
            }
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    // The objective's tags, as the game reads them: any one set (each a
    // comma-separated list of tags that must all match, each tag allowing
    // '/' alternatives).
    private static bool Accepts(DeliverObjective objective, Item item) =>
        objective.acceptableContextTagSets.Count == 0
        || objective.acceptableContextTagSets.Any(set => set.Split(',').All(tag => ItemContextTagManager.DoAnyTagsMatch(tag.Split('/'), item.GetContextTags())));
}
