#if DEBUG
using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using SObject = StardewValley.Object;

namespace ArtStudio;

// Two-player tests (2026-10-25): one dev copy hosts a throwaway farm with a
// cabin ("museart_mp host"), another joins it over the local network as a
// farmhand ("museart_mp join"), and "museart_mptest <step>" runs each side's
// half of the checks: the farmhand paints, consigns and varnishes; the host
// sees it all arrive; the farmhand collects. The host's lobby is invite-only,
// so nobody else can see or join it.
internal static class DevMultiplayer
{
    private static IMonitor _monitor = null!;
    private static bool _joining;

    // Where the test puts the shared varnishing cabinet on the farm.
    private static readonly Vector2 CabinetTile = new(68, 16);

    // The game keeps its multiplayer manager to itself.
    private static Traverse Multiplayer => Traverse.Create(Traverse.Create(typeof(Game1)).Field("multiplayer").GetValue());

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        DevTools.AddCommand(helper, "museart_mp", "Two-player dev game. Usage: museart_mp host (from the title screen: a throwaway farm with a cabin, hosting) | museart_mp join (from the title screen: joins the host on this machine as its farmhand)", (_, args) =>
        {
            if (args.FirstOrDefault() == "host") Host();
            else if (args.FirstOrDefault() == "join") Join();
        });
        DevTools.AddCommand(helper, "museart_mptest", "Two-player checks, run in order: 'host-setup' (host), 'client' (farmhand), 'host-check' (host), 'client-collect' (farmhand).", (_, args) => Test(args.FirstOrDefault() ?? ""));
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private static void Host()
    {
        if (Context.IsWorldReady) return;
        Game1.startingCabins = 1;
        Game1.cabinsSeparate = false;
        DevGame.NewGame(_monitor);
        Game1.options.serverPrivacy = ServerPrivacy.InviteOnly;
        Game1.options.enableServer = true;
        Game1.multiplayerMode = 2;           // the game registers the host on its next update
        Multiplayer.Method("StartServer").GetValue();
        _monitor.Log($"Hosting (invite-only), {Game1.getFarm().buildings.Count(b => b.isCabin)} cabin(s).", LogLevel.Info);
    }

    private static void Join()
    {
        if (Context.IsWorldReady) return;
        Game1.options.pauseWhenOutOfFocus = false;
        Game1.activeClickableMenu = new FarmhandMenu(Multiplayer.Method("InitClient", new[] { typeof(Client) }, new object[] { new LidgrenClient("localhost") }).GetValue<Client>());
        _joining = true;
        _monitor.Log("Joining the host on this machine...", LogLevel.Info);
    }

    // Takes the first free cabin, then fills in the new farmhand's details.
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!_joining) return;
        if (Context.IsWorldReady && Game1.activeClickableMenu == null)
        {
            _joining = false;
            _monitor.Log($"Joined as a farmhand: {Game1.player.Name}, {Game1.otherFarmers.Count} other player(s).", LogLevel.Info);
            return;
        }
        if (Game1.activeClickableMenu is FarmhandMenu menu && !menu.approvingFarmhand)
        {
            var slots = Traverse.Create(menu).Field("menuSlots").GetValue<System.Collections.IList>();
            var slot = slots?.OfType<FarmhandMenu.FarmhandSlot>().FirstOrDefault(s => !s.BelongsToAnotherPlayer());
            slot?.Activate();
        }
        else if (Game1.activeClickableMenu is CharacterCustomization custom && e.IsMultipleOf(30))
        {
            Game1.player.Name = "Helper";
            Game1.player.displayName = "Helper";
            Game1.player.favoriteThing.Value = "Painting";
            Traverse.Create(custom).Method("optionButtonClick", "OK").GetValue();
        }
    }

    private static void Check(string what, bool ok, string detail = "") =>
        _monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    private static SObject Paint(Farmer who, string item)
    {
        var menu = EaselMenu.StartStillLife(who, CanvasKind.Small, new[] { item });
        menu.DevPaintPartly(1f, 3);
        menu.DevFinish(false);
        return menu.DevPainting!;
    }

    private static void Test(string step)
    {
        if (!Context.IsWorldReady) return;
        var who = Game1.player;
        var farm = Game1.getFarm();
        switch (step)
        {
            case "host-setup":
            {
                Check("this copy is the host", Context.IsMainPlayer && Context.IsMultiplayer);
                Check("a farmhand has joined", Game1.otherFarmers.Count == 1, $"{Game1.otherFarmers.Count} other player(s)");
                farm.objects.Remove(CabinetTile);
                var cabinet = ItemRegistry.Create<SObject>(Varnish.QualifiedCabinetId);
                Check("the host places a varnishing cabinet on the farm", cabinet.placementAction(farm, (int)CabinetTile.X * 64, (int)CabinetTile.Y * 64, who));
                break;
            }
            case "client":
            {
                Check("this copy is a farmhand", !Context.IsMainPlayer && Context.IsMultiplayer, who.Name);
                Check("the farmhand has the art shop and Sable", Game1.getLocationFromName(Shop.ArtShop.LocationName) != null && Game1.getCharacterFromName("Sable") != null);
                ArtProgress.SetLevel(who, 8);
                ArtProgress.SetOwned(who, Enumerable.Range(0, Palette.All.Length));
                int time = Game1.timeOfDay, xp = ArtProgress.Xp(who);

                // Painting: no clock change in multiplayer, but the painting, XP and record all arrive.
                var painting = Paint(who, "(O)254");
                who.addItemToInventory(painting);
                Check("a farmhand's painting is made", PaintingItems.IsPainting(painting) && PaintingData.Read(painting) != null, painting.DisplayName);
                Check("painting earns the farmhand XP", ArtProgress.Xp(who) > xp, $"{xp} -> {ArtProgress.Xp(who)}");
                Check("and goes in their record", Catalogue.TimesPainted(who, "(O)254") >= 1);
                Check("the clock doesn't jump in multiplayer", Game1.timeOfDay == time && ArtHooks.PendingMinutes >= 0, $"{time} -> {Game1.timeOfDay}");

                // Consignment: into the shared world.
                foreach (var old in Shop.Consignment.All(who).ToList())
                    Shop.Consignment.Consigned(who).Remove(old);
                Check("the farmhand consigns a painting", Shop.Consignment.Consign(who, painting) && Shop.Consignment.All(who).Count() == 1);

                // Varnishing in the host's cabinet, aged as if it went in a month ago.
                var cabinet = farm.objects.TryGetValue(CabinetTile, out var obj) ? obj : null;
                Check("the farmhand sees the host's cabinet", cabinet != null && Varnish.IsCabinet(cabinet));
                if (cabinet != null)
                {
                    who.addItemToInventory(ItemRegistry.Create("(O)" + Varnish.VarnishId, 1));
                    var toVarnish = Paint(who, "(O)276");
                    Check("the farmhand loads it", Varnish.Load(cabinet, toVarnish, who));
                    cabinet.modData[ModEntry.Id + "/VarnishStart"] = (Game1.Date.TotalDays - 28).ToString();
                }

                // Plein air works on a farmhand's screen too.
                if (who.currentLocation.IsOutdoors)
                {
                    var view = Views.Take(who.currentLocation, who.Tile);
                    Check("a farmhand can paint the view", view != null && view.Size > 0, view?.Subject ?? "no capture");
                }
                else
                    _monitor.Log("(Farmhand indoors: view capture skipped.)", LogLevel.Info);
                break;
            }
            case "host-check":
            {
                var farmhand = Game1.otherFarmers.Values.FirstOrDefault();
                Check("the host sees the farmhand", farmhand != null);
                if (farmhand == null) break;
                var consigned = Shop.Consignment.All(farmhand).ToList();
                Check("the host sees the farmhand's consigned painting", consigned.Count == 1 && PaintingData.Read(consigned[0]) != null, $"{consigned.Count}");
                Check("and the host's own easels are empty", !Shop.Consignment.All(who).Any());
                Check("the farmhand's record reached the host", Catalogue.TimesPainted(farmhand, "(O)254") >= 1);
                var cabinet = farm.objects.TryGetValue(CabinetTile, out var obj) ? obj : null;
                Check("the farmhand's painting is in the host's cabinet", cabinet?.heldObject.Value is SObject held && PaintingItems.IsPainting(held), cabinet?.heldObject.Value?.DisplayName ?? "empty");
                if (cabinet != null)
                {
                    Check("with the day it went in", Varnish.DaysIn(cabinet) >= 28, $"{Varnish.DaysIn(cabinet)} days");
                    Varnish.MarkCured();
                    Check("the host's morning marks it cured", cabinet.readyForHarvest.Value);
                }
                break;
            }
            case "client-collect":
            {
                var cabinet = farm.objects.TryGetValue(CabinetTile, out var obj) ? obj : null;
                Check("the farmhand sees it cured", cabinet?.readyForHarvest.Value == true);
                if (cabinet != null)
                {
                    foreach (var item in who.Items.Where(i => i != null && Varnish.FinishOf(i) == "gallery").ToList())
                        who.removeItemFromInventory(item);
                    cabinet.checkForAction(who);
                    Check("and takes it out with a gallery finish", who.Items.Any(i => i != null && Varnish.FinishOf(i) == "gallery") && cabinet.heldObject.Value == null);
                }
                int money = who.Money;
                foreach (var item in Shop.Consignment.All(who))
                    item.modData[ModEntry.Id + "/SellsOn"] = Game1.Date.TotalDays.ToString();
                int expected = Shop.Consignment.All(who).Sum(Shop.Consignment.Price);
                Shop.Consignment.SellDue(who);
                Check("the farmhand's consigned painting sells and pays", who.Money - money == expected && expected > 0 && !Shop.Consignment.All(who).Any(), $"+{who.Money - money}g of {expected}g");
                Shop.Commissions.NewDay(who, _monitor, new Random(1));
                Check("the farmhand gets their own commission board", Shop.Commissions.Posted(who).Any());
                break;
            }
        }
    }
}
#endif
