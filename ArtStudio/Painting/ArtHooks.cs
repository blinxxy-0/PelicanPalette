using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace ArtStudio.Painting;

// How paintings and painting supplies plug into the rest of the game:
// using the easel, gift tastes, framed paintings on the wall, brush wear
// shown in menus, and Painting XP from selling and shipping.
internal static class ArtHooks
{
    private static IMonitor _monitor = null!;

    public static void Apply(Harmony harmony, IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;

        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
            prefix: new HarmonyMethod(typeof(ArtHooks), nameof(EaselActionPrefix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
            prefix: new HarmonyMethod(typeof(ArtHooks), nameof(PlacedPaletteActionPrefix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.performToolAction)),
            prefix: new HarmonyMethod(typeof(ArtHooks), nameof(PlacedPaletteToolPrefix)));
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.getGiftTasteForThisItem)),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(GiftTastePostfix)));
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.receiveGift)),
            prefix: new HarmonyMethod(typeof(ArtHooks), nameof(ReceiveGiftPrefix)),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(ReceiveGiftPostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(FurnitureDrawPostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.drawInMenu),
                new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(FurnitureDrawInMenuPostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), "loadDisplayName"),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(FurnitureNamePostfix)));
        harmony.Patch(AccessTools.Method(typeof(Furniture), nameof(Furniture.getDescription)),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(FurnitureDescriptionPostfix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.drawInMenu),
                new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(BrushWearPostfix)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.maximumStackSize)),
            postfix: new HarmonyMethod(typeof(ArtHooks), nameof(BrushStackPostfix)));

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Player.InventoryChanged += OnInventoryChanged;
        helper.Events.Display.MenuChanged += OnMenuChanged;

        // Shop stock and other conditions can ask whether you know a colour:
        // "blinxxy.MuseArt_KNOWS_PIGMENT <player> <pigment id>".
        GameStateQuery.Register(ModEntry.Id + "_KNOWS_PIGMENT", (query, context) =>
            GameStateQuery.Helpers.WithPlayer(context.Player, query[1], who => ArtProgress.Owns(who, Palette.IndexOf(query[2]))));
    }

    // ---- The easel ----

    private static bool EaselActionPrefix(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!ArtItems.IsEasel(__instance)) return true;
        __result = true;
        if (!justCheckingForActivity && who.IsLocalPlayer && Game1.activeClickableMenu == null)
            OpenEasel(who, __instance.TileLocation);
        return false;
    }

    // ---- A Palette on the floor: storage for paint ----

    private static bool PlacedPaletteActionPrefix(SObject __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
    {
        if (!ArtItems.IsPalette(__instance)) return true;
        __result = true;
        if (!justCheckingForActivity && who.IsLocalPlayer && Game1.activeClickableMenu == null)
            Game1.activeClickableMenu = new PaletteMenu(who, __instance);
        return false;
    }

    // Picked up with any tool, paint and all (the game's own pickup would
    // make a fresh, empty Palette).
    private static bool PlacedPaletteToolPrefix(SObject __instance, Tool t, ref bool __result)
    {
        if (!ArtItems.IsPalette(__instance) || __instance.Location is not GameLocation location) return true;
        __result = false;
        var tile = __instance.TileLocation;
        location.objects.Remove(tile);
        Game1.createItemDebris(__instance.getOne(), tile * 64f + new Vector2(32f), -1, location);
        location.playSound("hammer");
        return false;
    }

    public static void OpenEasel(Farmer who, Vector2? easelTile = null)
    {
        LearnPigments(who);
        Game1.activeClickableMenu = new EaselMenu(who, easelTile: easelTile);
    }

    // A pigment is knowledge, like a recipe: the jar you buy, are given or
    // grind is learned the moment it reaches your bag, and never stays
    // there. (Checked a few times a second rather than inside the inventory
    // event, which fires mid-change.)
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (e.IsMultipleOf(15) && Context.IsWorldReady)
            LearnPigments(Game1.player);
    }

    private static void LearnPigments(Farmer who)
    {
        foreach (var item in who.Items.Where(i => ArtItems.PigmentFor(i) != null).ToList())
        {
            int pigment = ArtItems.PigmentFor(item)!.Value;
            who.removeItemFromInventory(item);
            Learn(who, pigment);
        }
        // A tube of a colour you don't know yet (milled, or a gift) teaches
        // it too, and stays in the bag as paint.
        foreach (var tube in who.Items.Where(i => ArtItems.TubeFor(i) != null))
            Learn(who, ArtItems.TubeFor(tube)!.Value);
        // A new Palette comes with the starter colours.
        foreach (var palette in who.Items.Where(ArtItems.IsPalette))
            PaletteItem.Initialize(palette);
        StoreTubes(who);
    }

    // Tubes go straight into your Palette (the biggest one in your bag), as
    // long as there's room for the whole tube. Not while the easel is open:
    // it holds the Palette's paint itself until you step away.
    private static void StoreTubes(Farmer who)
    {
        if (!PaletteItem.Required || Game1.activeClickableMenu is EaselMenu || PaletteItem.Find(who) is not Item palette) return;
        var used = PaletteItem.Refill(who, palette);
        int tubes = used.Values.Sum();
        if (tubes == 0) return;
        Game1.playSound("coin");
        Game1.addHUDMessage(new HUDMessage(used.Count == 1
            ? I18n.Get("palette.stored", new { tubes, pigment = I18n.Pigment(used.Keys.First()), palette = palette.DisplayName })
            : I18n.Get("palette.stored.many", new { tubes, palette = palette.DisplayName })) { noIcon = true });
    }

    private static void Learn(Farmer who, int pigment)
    {
        if (!ArtProgress.Unlock(who, pigment)) return;
        Game1.playSound("newRecipe");
        Game1.addHUDMessage(new HUDMessage(I18n.Get("pigment.learned", new { pigment = I18n.Pigment(pigment) })) { noIcon = true });
    }

    // ---- Painting takes time ----

    // Minutes of painting done since the easel opened. The clock moves on
    // when you step away from it, behind a fade, so a run of paintings costs
    // its time all at once and the clock never jumps under an open menu.
    private static int _pendingMinutes;

    public static int PendingMinutes => _pendingMinutes;

    // Tests start from a fresh morning, whatever ran before them.
    internal static void DevFreshMorning()
    {
        _pendingMinutes = 0;
        Game1.timeOfDay = 900;
    }

    public static void AddPaintingTime(int minutes) => _pendingMinutes += minutes;

    // When a painting started now (after the ones already done) would end.
    public static int FinishTime(int minutes) => Utility.ModifyTime(Game1.timeOfDay, _pendingMinutes + minutes);

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is not EaselMenu || e.NewMenu != null || _pendingMinutes <= 0) return;
        int minutes = _pendingMinutes;
        _pendingMinutes = 0;
        PassTime(minutes);
    }

    // The clock runs on by `minutes`, ten minutes at a time as the game's own
    // does (so schedules, machines and lighting all follow). In multiplayer
    // the shared clock kept running while you painted, so nothing is added.
    public static void PassTime(int minutes)
    {
        if (Context.IsMultiplayer) return;
        Game1.globalFadeToBlack(() =>
        {
            int target = Math.Min(Utility.ModifyTime(Game1.timeOfDay, minutes), PaintingRules.LatestFinish);
            while (Game1.timeOfDay < target)
                Game1.performTenMinuteClockUpdate();
            Game1.globalFadeToClear();
            Game1.addHUDMessage(new HUDMessage(I18n.Get("painting.time-passed", new { time = Game1.getTimeOfDayString(Game1.timeOfDay) })) { noIcon = true });
        });
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        ArtProgress.MigrateLegacyXp(Game1.player);
        foreach (string recipe in ArtItems.StarterRecipes)
            Game1.player.craftingRecipes.TryAdd(recipe, 0);
    }

    // ---- Gifts ----

    // Iridium paintings are loved by everyone; gold and silver liked; normal
    // ones liked by art lovers and shrugged at by everyone else. Nobody
    // dislikes a painting. A painting of something the villager loves goes
    // up a step. (Only loves: most crops are a universal like, and counting
    // those would make nearly every painting a hit.)
    private static void GiftTastePostfix(NPC __instance, Item item, ref int __result)
    {
        if (!PaintingItems.IsPainting(item)) return;
        try
        {
            __result = PaintingTaste(__instance, item);
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Couldn't work out a gift taste for a painting: {ex}", LogLevel.Error);
        }
    }

    public static int PaintingTaste(NPC npc, Item painting)
    {
        bool artLover = ModEntry.Config.ArtLoversLikeAnyPainting
            && Game1.content.Load<Dictionary<string, string>>(ArtItems.ArtLoversAsset).ContainsKey(npc.Name);
        var data = PaintingData.Read(painting);
        // Your portrait of someone, given to them: always loved.
        if (data != null && data.IsSitter && Sitters.NpcOf(data.Subject) == npc.Name)
            return NPC.gift_taste_love;

        int taste = painting.Quality switch
        {
            PaintingRules.Iridium => NPC.gift_taste_love,
            PaintingRules.Gold or PaintingRules.Silver => NPC.gift_taste_like,
            _ => artLover ? NPC.gift_taste_like : NPC.gift_taste_neutral,
        };

        if (taste != NPC.gift_taste_love && data != null && data.SubjectItems.Any(id => LovesSubject(npc, id)))
            taste = taste == NPC.gift_taste_like ? NPC.gift_taste_love : NPC.gift_taste_like;
        return taste;
    }

    private static bool LovesSubject(NPC npc, string itemId)
    {
        var subject = ItemRegistry.Create(itemId, allowNull: true);
        return subject != null && npc.getGiftTasteForThisItem(subject) == NPC.gift_taste_love;
    }

    private static void ReceiveGiftPrefix(SObject o, Farmer giver, ref float friendshipChangeMultiplier)
    {
        if (PaintingItems.IsPainting(o) && PaintingSkill.Has(giver, PaintingSkill.Instance.PatronsFavorite))
            friendshipChangeMultiplier *= PaintingSkill.PatronFriendship;
        if (PaintingItems.IsPainting(o) && PaintingData.IsMasterpiece(o))
            friendshipChangeMultiplier *= MasterpieceFriendship;
    }

    public const float MasterpieceFriendship = 1.5f;

    // Sable knows what they're looking at.
    private static void ReceiveGiftPostfix(NPC __instance, SObject o)
    {
        if (__instance.Name != "Sable" || !PaintingItems.IsPainting(o) || !PaintingData.IsMasterpiece(o)) return;
        __instance.CurrentDialogue.Clear();
        __instance.CurrentDialogue.Push(new Dialogue(__instance, null, I18n.Get("sable.masterpiece-gift")));
        Game1.drawDialogue(__instance);
    }

    // ---- Framed paintings ----

    // The frame sprite's border, in sprite pixels.
    private const int FrameBorder = 3;

    private static void FurnitureDrawPostfix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha, Netcode.NetVector2 ___drawPosition)
    {
        if (!ArtItems.IsFramed(__instance) || __instance.isTemporarilyInvisible) return;
        var source = __instance.sourceRect.Value;
        Vector2 position = Furniture.isDrawingLocationFurniture
            ? Game1.GlobalToLocal(Game1.viewport, ___drawPosition.Value)
            : Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - (source.Height * 4 - __instance.boundingBox.Height)));
        float depth = (__instance.boundingBox.Value.Bottom - 48) / 10000f;
        var inner = new Rectangle((int)position.X + FrameBorder * 4, (int)position.Y + FrameBorder * 4,
            (source.Width - 2 * FrameBorder) * 4, (source.Height - 2 * FrameBorder) * 4);
        PaintingRenderer.DrawArt(spriteBatch, __instance, inner, Color.White * alpha, depth + 0.00001f);
    }

    private static void FurnitureDrawInMenuPostfix(Furniture __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, Color color)
    {
        if (!ArtItems.IsFramed(__instance)) return;
        var source = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).GetSourceRect();
        float scale = GetScaleSize(__instance) * scaleSize;
        float w = (source.Width - 2 * FrameBorder) * scale, h = (source.Height - 2 * FrameBorder) * scale;
        var inner = new Rectangle((int)(location.X + 32 - w / 2), (int)(location.Y + 32 - h / 2), (int)w, (int)h);
        PaintingRenderer.DrawArt(spriteBatch, __instance, inner, color * transparency, layerDepth + 0.00001f);
    }

    private static readonly Func<Furniture, float> GetScaleSize =
        AccessTools.MethodDelegate<Func<Furniture, float>>(AccessTools.Method(typeof(Furniture), "getScaleSize"));

    private static void FurnitureNamePostfix(Furniture __instance, ref string __result)
    {
        if (ArtItems.IsFramed(__instance) && PaintingData.Read(__instance) is PaintingData data)
            __result = I18n.Get("painting.framed-name", new { title = PaintingItems.Title(data) });
    }

    private static void FurnitureDescriptionPostfix(Furniture __instance, ref string __result)
    {
        if (ArtItems.IsFramed(__instance) && PaintingData.Read(__instance) is PaintingData data)
            __result = Game1.parseText(PaintingItems.Provenance(data) + "\n" + I18n.Get("painting.framed-hint"), Game1.smallFont, 272);
    }

    // Turns a painting into wall furniture, using up one picture frame.
    public static Furniture Frame(SObject painting)
    {
        var data = PaintingData.Read(painting)!;
        string id = data.Size <= PaintingRules.CanvasSize(CanvasKind.Small) ? ArtItems.FramedSmallId : ArtItems.FramedLargeId;
        var framed = ItemRegistry.Create<Furniture>("(F)" + id);
        data.WriteTo(framed);
        return framed;
    }

    // ---- Brushes ----

    private const string UsesKey = ModEntry.Id + "/BrushUses";

    public static int UsesLeft(Item brushItem, BrushSpec brush) =>
        brush.Durability - (brushItem.modData.TryGetValue(UsesKey, out string raw) && int.TryParse(raw, out int used) ? used : 0);

    // Returns true if the brush wore out (and was removed).
    public static bool WearBrush(Farmer who, Item brushItem, BrushSpec brush)
    {
        int used = brush.Durability - UsesLeft(brushItem, brush) + 1;
        brushItem.modData[UsesKey] = used.ToString();
        if (used < brush.Durability) return false;
        who.removeItemFromInventory(brushItem);
        Game1.addHUDMessage(new HUDMessage(I18n.Get("brush.worn-out", new { brush = I18n.Get("brush." + brush.Id) })) { noIcon = true });
        return true;
    }

    private static void BrushWearPostfix(SObject __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize)
    {
        var brush = ArtItems.BrushFor(__instance);
        if (brush == null || brush.Durability <= 0) return;
        int left = UsesLeft(__instance, brush);
        if (left >= brush.Durability) return;
        float frac = left / (float)brush.Durability;
        spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)location.X, (int)(location.Y + 56f * scaleSize), (int)(64f * scaleSize * frac), (int)(8f * scaleSize)),
            Utility.getRedToGreenLerpColor(frac));
    }

    private static void BrushStackPostfix(SObject __instance, ref int __result)
    {
        if (ArtItems.BrushFor(__instance) != null)
            __result = 1;
    }

    // ---- Selling ----

    private static int SaleXp(Item painting) => PaintingRules.SaleXp + 2 * painting.Quality;

    // A painting leaving the inventory while a shop is open was sold.
    private static void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!e.IsLocalPlayer || Game1.activeClickableMenu is not ShopMenu) return;
        int xp = e.Removed.Where(PaintingItems.IsPainting).Sum(SaleXp);
        if (xp > 0)
            ArtProgress.AddXp(e.Player, xp);
    }

    private static void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        var bin = Game1.getFarm().getShippingBin(Game1.player);
        int xp = bin.Where(PaintingItems.IsPainting).Sum(SaleXp);
        if (xp > 0)
            ArtProgress.AddXp(Game1.player, xp);
    }
}
