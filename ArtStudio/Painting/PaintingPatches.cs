using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// Harmony patches that make each painting item look and behave like its
// own artwork rather than a stack of identical objects.
internal static class PaintingPatches
{
    private static IMonitor _monitor = null!;

    public static void Apply(Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        harmony.Patch(
            AccessTools.Method(typeof(SObject), nameof(SObject.drawInMenu),
                new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(PaintingPatches), nameof(DrawInMenuPrefix)));
        harmony.Patch(
            AccessTools.Method(typeof(SObject), nameof(SObject.drawWhenHeld)),
            prefix: new HarmonyMethod(typeof(PaintingPatches), nameof(DrawWhenHeldPrefix)));
        harmony.Patch(
            AccessTools.Method(typeof(SObject), nameof(SObject.maximumStackSize)),
            postfix: new HarmonyMethod(typeof(PaintingPatches), nameof(MaximumStackSizePostfix)));
        harmony.Patch(
            AccessTools.Method(typeof(SObject), nameof(SObject.getDescription)),
            postfix: new HarmonyMethod(typeof(PaintingPatches), nameof(GetDescriptionPostfix)));
    }

    // Draws the frame with the painting's own pixels inside, then the usual
    // quality star and stack number on top.
    private static bool DrawInMenuPrefix(SObject __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        if (!PaintingItems.IsPainting(__instance)) return true;
        try
        {
            if (drawShadow)
                __instance.DrawShadow(spriteBatch, location, color, layerDepth);
            PaintingRenderer.DrawFramed(spriteBatch, __instance, location + new Vector2(32f, 32f), 4f * scaleSize, color * transparency, layerDepth);
            __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);
            return false;
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't draw a painting in a menu: {ex}", LogLevel.Error);
            return true;
        }
    }

    private static bool DrawWhenHeldPrefix(SObject __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
    {
        if (!PaintingItems.IsPainting(__instance)) return true;
        float layerDepth = Math.Max(0f, (f.StandingPixel.Y + 3) / 10000f);
        PaintingRenderer.DrawFramed(spriteBatch, __instance, objectPosition + new Vector2(32f, 32f), 4f, Color.White, layerDepth);
        return false;
    }

    // Every painting is one of a kind.
    private static void MaximumStackSizePostfix(SObject __instance, ref int __result)
    {
        if (PaintingItems.IsPainting(__instance))
            __result = 1;
    }

    private static void GetDescriptionPostfix(SObject __instance, ref string __result)
    {
        if (!PaintingItems.IsPainting(__instance)) return;
        var data = PaintingData.Read(__instance);
        if (data != null)
            __result = Game1.parseText(PaintingItems.Provenance(data), Game1.smallFont,
                Traverse.Create(__instance).Method("getDescriptionWidth").GetValue<int>());
    }
}
