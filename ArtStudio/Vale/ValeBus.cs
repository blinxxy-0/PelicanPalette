#if DEBUG
using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using StardewValley.Pathfinding;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace ArtStudio.Vale;

// Pam's bus gets a second destination (docs/design/1.4_plan.md, "The bus"): at
// the Bus Stop's ticket machine you choose the Desert or the Gilded Vale, and
// the bus drives off as it does for the Desert. In the Vale it waits on the
// road's straight by the shelter, facing west, as it waits in the Desert: you
// arrive as the screen comes up on it and its door opens, and ride back from
// there, the bus driving off west along the road. (A drive-in from the west,
// turning in the circle, looked wrong: the user, 2026-11-07.) Everything is the game's own bus, drawn and timed the way
// Desert.cs and BusStop.cs draw and time it.
//
// With Pathoschild's Central Station installed, it runs the ticket machines
// and the Vale is one of its bus stops (the dev pack's content.json); the
// shelter here then opens its menu, and none of the patches below step in.
internal static class ValeBus
{
    public const string Vale = ModEntry.Id + "_GildedVale";

    // Anywhere in the Gilded Vale: its outdoor map, or one of its interiors (the Vale House, the
    // studios, the pottery...: every location named blinxxy.MuseArt_Vale<something>). Not the Grove.
    public static bool IsInVale(GameLocation? location) =>
        location != null && (location.Name == Vale || location.Name.StartsWith(ModEntry.Id + "_Vale", StringComparison.Ordinal));
    public const string OpenMail = ModEntry.Id + "_ValeOpen";       // the letter that opens the route (to come)
    public const int Fare = 300;
    private const string CentralStationId = "Pathoschild.CentralStation";

    // Where the bus waits (its top-left, in tiles), and where you step off.
    private static readonly Vector2 Park = new(2f, 4.75f);
    private static readonly Point StepOff = new(3, 8);
    // What you click to ride back: the bus, or the shelter beside it.
    private static readonly Rectangle BusTiles = new(2, 5, 8, 4);
    private static readonly Rectangle ShelterTiles = new(3, 1, 5, 4);

    private static readonly Rectangle BusSource = new(288, 1247, 128, 64);
    private static readonly Rectangle PamSource = new(384, 1311, 15, 19);
    private static readonly Rectangle TransparentWindow = new(0, 0, 21, 41);
    private static readonly Vector2 PamOffset = new(0f, 29f);

    private static IReflectionHelper _reflection = null!;
    private static IInputHelper _input = null!;
    private static IMonitor _monitor = null!;
    private static bool _centralStation;
    private static bool _boardedForVale, _returningFromVale, _arrivingInVale;

    // the Vale's bus
    private static Vector2 _pos, _motion;
    private static bool _arriving, _drivingOff, _leaving;
    private static TemporaryAnimatedSprite? _door;

    public static void Register(IModHelper helper, Harmony harmony, IMonitor monitor)
    {
        _monitor = monitor;
        _reflection = helper.Reflection;
        _input = helper.Input;
        _centralStation = helper.ModRegistry.IsLoaded(CentralStationId);

        if (!_centralStation)
        {
            harmony.Patch(AccessTools.Method(typeof(BusStop), nameof(BusStop.checkAction)),
                prefix: new HarmonyMethod(typeof(ValeBus), nameof(TicketMachinePrefix)));
            harmony.Patch(AccessTools.Method(typeof(BusStop), "busLeftToDesert"),
                prefix: new HarmonyMethod(typeof(ValeBus), nameof(BusLeftPrefix)));
        }
        harmony.Patch(AccessTools.Method(typeof(BusStop), "resetLocalState"),
            postfix: new HarmonyMethod(typeof(ValeBus), nameof(BusStopResetPostfix)));
        harmony.Patch(AccessTools.Method(typeof(GameLocation), nameof(GameLocation.draw), new[] { typeof(SpriteBatch) }),
            postfix: new HarmonyMethod(typeof(ValeBus), nameof(DrawPostfix)));

        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.DayStarted += (_, _) => Parked();
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        DevTools.AddCommand(helper, "museart_vale_open", "Opens the bus route to the Gilded Vale (the letter's flag), or closes it. Usage: museart_vale_open [off]", (_, args) =>
        {
            if (args.Length > 0 && args[0] == "off")
                Game1.player.mailReceived.Remove(OpenMail);
            else
                Game1.player.mailReceived.Add(OpenMail);
        });
        DevTools.AddCommand(helper, "museart_valebustest", "Throwaway farm only: the Vale bus. Usage: museart_valebustest setup|ask|go|back", (_, args) => Test(args));
    }

    // For a throwaway farm (museart_devgame): the route opened and the bus mended, the
    // ticket machine's question, the ride out, the ride back.
    private static void Test(string[] args)
    {
        switch (args.FirstOrDefault())
        {
            case "setup":
                Game1.player.mailReceived.Add(OpenMail);
                Game1.player.mailReceived.Add("ccVault");
                Game1.player.Money = Math.Max(Game1.player.Money, 5000);
                // Pam at the bus, ready to drive (her schedule has her there from mid-morning)
                if (Game1.getCharacterFromName("Pam") is { } pam)
                {
                    Game1.warpCharacter(pam, "BusStop", new Point(21, 10));
                    pam.controller = null;
                    pam.ClearSchedule();
                }
                Game1.warpFarmer("BusStop", 22, 11, 0);
                break;
            case "ask":
                var stop = (BusStop)Game1.getLocationFromName("BusStop");
                var layer = stop.Map.RequireLayer("Buildings");
                for (int x = 0; x < layer.LayerWidth; x++)
                    for (int y = 0; y < layer.LayerHeight; y++)
                        if (stop.getTileIndexAt(x, y, "Buildings", "outdoors") == 1057)
                        {
                            stop.checkAction(new Location(x, y), Game1.viewport, Game1.player);
                            return;
                        }
                break;
            case "go":
                Game1.activeClickableMenu = null;
                Board((BusStop)Game1.getLocationFromName("BusStop"), Fare, toVale: true);
                break;
            case "back":
                RideBack(Game1.currentLocation);
                break;
        }
    }

    private static bool RouteOpen =>
        Game1.MasterPlayer.mailReceived.Contains(OpenMail) && Game1.MasterPlayer.mailReceived.Contains("ccVault");

    // ---- At the Bus Stop ----

    private static bool TicketMachinePrefix(BusStop __instance, Location tileLocation, Farmer who, ref bool __result)
    {
        if (!RouteOpen || who.isRidingHorse() || __instance.getTileIndexAt(tileLocation, "Buildings", "outdoors") != 1057)
            return true;
        var stop = __instance;
        stop.createQuestionDialogue("Where to?", new[]
        {
            new Response("Desert", $"Calico Desert ({Utility.getNumberWithCommas(stop.TicketPrice)}g)"),
            new Response("Vale", $"The Gilded Vale ({Utility.getNumberWithCommas(Fare)}g)"),
            new Response("Cancel", Game1.content.LoadString("Strings\\Locations:Desert_Return_No")),
        }, (_, answer) =>
        {
            if (answer == "Desert")
                Board(stop, stop.TicketPrice, toVale: false);
            else if (answer == "Vale")
                Board(stop, Fare, toVale: true);
        });
        __result = true;
        return false;
    }

    // The game's "Bus_Yes" (BusStop.answerDialogue), with the fare and destination ours.
    private static void Board(BusStop stop, int fare, bool toVale)
    {
        NPC pam = Game1.getCharacterFromName("Pam");
        if (!Game1.netWorldState.Value.canDriveYourselfToday.Value
            && (!stop.characters.Contains(pam) || pam.TilePoint.X != 21 || pam.TilePoint.Y != 10))
        {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BusStop_NoDriver"));
            return;
        }
        if (Game1.player.Money < fare)
        {
            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BusStop_NotEnoughMoneyForTicket"));
            return;
        }
        Game1.player.Money -= fare;
        _boardedForVale = toVale;
        _monitor.Log($"Vale bus: boarded at the Bus Stop (for the Vale: {toVale}) " + Environment.StackTrace, LogLevel.Trace);
        Game1.freezeControls = true;
        Game1.viewportFreeze = true;
        _reflection.GetField<int>(stop, "forceWarpTimer").SetValue(8000);
        var reachedDoor = _reflection.GetMethod(stop, "playerReachedBusDoor");
        Game1.player.controller = new PathFindController(Game1.player, stop, new Point(22, 9), 0,
            (c, l) => reachedDoor.Invoke(c, l));
        Game1.player.setRunning(isRunning: true);
        if (Game1.player.mount != null)
            Game1.player.mount.farmerPassesThrough = true;
        Desert.warpedToDesert = false;
    }

    private static bool BusLeftPrefix()
    {
        _monitor.Log($"Vale bus: the bus left the Bus Stop (for the Vale: {_boardedForVale}) " + Environment.StackTrace, LogLevel.Trace);
        if (!_boardedForVale)
            return true;
        _boardedForVale = false;
        _arrivingInVale = true;
        Game1.viewportFreeze = true;
        Game1.warpFarmer(Vale, StepOff.X, StepOff.Y, flip: false);
        Game1.globalFade = false;
        return false;
    }

    // Back from the Vale, the bus drives in and opens its door, as it does back from the Desert.
    private static void BusStopResetPostfix(BusStop __instance)
    {
        _monitor.Log($"Vale bus: Bus Stop reset (back from the Vale: {_returningFromVale}, for the Vale: {_boardedForVale})", LogLevel.Trace);
        if (!_returningFromVale)
            return;
        _returningFromVale = false;
        _boardedForVale = false;
        Game1.player.controller = null;
        _reflection.GetField<int>(__instance, "forceWarpTimer").SetValue(0);
        Game1.changeMusicTrack("silence");
        _reflection.GetField<Vector2>(__instance, "busPosition").SetValue(new Vector2(21f, 6f) * 64f);
        _reflection.GetField<TemporaryAnimatedSprite>(__instance, "busDoor").SetValue(NewDoor(new Vector2(21f, 6f) * 64f, closed: true));
        Game1.displayFarmer = false;
        __instance.busDriveBack();
    }

    // ---- In the Vale ----

    private static void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer || e.NewLocation.Name != Vale)
            return;
        _monitor.Log($"Vale bus: arrived in the Vale from {e.OldLocation.Name} (by bus: {_arrivingInVale}) " + Environment.StackTrace, LogLevel.Trace);
        _drivingOff = _leaving = false;
        if (_arrivingInVale)
        {
            _arrivingInVale = false;
            // the screen comes up on the bus waiting; its door opens and you step off
            Parked();
            _arriving = true;
            _door = NewDoor(_pos, closed: true);
            Game1.displayFarmer = false;
            Game1.freezeControls = true;
            Game1.changeMusicTrack("silence");
            GameLocation here = e.NewLocation;
            DelayedAction.functionAfterDelay(() =>
            {
                _door = NewDoor(_pos, closed: false);
                _door.pingPong = true;
                _door.interval = 70f;
                _door.currentParentTileIndex = 5;
                _door.endFunction = _ => SteppedOff(here);
                here.localSound("trashcanlid");
            }, 900);
        }
        else
            Parked();
    }

    private static void Parked()
    {
        _arriving = false;
        _motion = Vector2.Zero;
        _pos = Park * 64f;
        _door = NewDoor(_pos, closed: false);
        // its body blocks you, as the Desert's does (a Passable property on Back is a wall
        // to the farmer); the row in front of its door stays free
        if (Game1.getLocationFromName(Vale) is { } vale)
            for (int x = BusTiles.Left; x < BusTiles.Right; x++)
                for (int y = 6; y <= 7; y++)
                    vale.setTileProperty(x, y, "Back", "Passable", "F");
    }

    private static TemporaryAnimatedSprite NewDoor(Vector2 busPosition, bool closed) =>
        new("LooseSprites\\Cursors", new Rectangle(closed ? 368 : 288, 1311, 16, 38), busPosition + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
        {
            interval = 999999f,
            animationLength = closed ? 1 : 6,
            holdLastFrame = true,
            layerDepth = (busPosition.Y + 192f) / 10000f + 1E-05f,
            scale = 4f
        };

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        GameLocation here = Game1.currentLocation;
        if (here?.Name != Vale || !Context.IsPlayerFree || !e.Button.IsActionButton() || _arriving || _drivingOff)
            return;
        Vector2 tile = e.Cursor.GrabTile;
        Point at = new((int)tile.X, (int)tile.Y);
        if (!BusTiles.Contains(at) && !ShelterTiles.Contains(at))
            return;
        if (Vector2.Distance(Game1.player.Tile, tile) > 2.5f)
            return;
        _input.Suppress(e.Button);
        Game1.player.Halt();
        if (_centralStation)
        {
            here.performAction("CentralStation Bus", Game1.player, new Location(at.X, at.Y));
            return;
        }
        here.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:Desert_Return_Question"), new[]
        {
            new Response("Yes", Game1.content.LoadString("Strings\\Locations:Desert_Return_Yes")),
            new Response("Not", Game1.content.LoadString("Strings\\Locations:Desert_Return_No")),
        }, (_, answer) =>
        {
            if (answer == "Yes")
                RideBack(here);
        });
    }

    // Desert.playerReachedBusDoor and busDriveOff
    private static void RideBack(GameLocation here)
    {
        Game1.viewportFreeze = true;
        Game1.player.position.X = -10000f;
        Game1.freezeControls = true;
        Game1.player.CanMove = false;
        here.playSound("stoneStep");
        _door = NewDoor(_pos, closed: false);
        _door.timer = 0f;
        _door.interval = 70f;
        _door.paused = false;
        _door.endFunction = _ => Game1.globalFadeToBlack(() =>
        {
            Game1.globalFadeToClear();
            here.localSound("batFlap");
            _drivingOff = true;
            here.localSound("busDriveOff");
            Game1.changeMusicTrack("silence");
        });
        here.localSound("trashcanlid");
    }

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        GameLocation here = Game1.currentLocation;
        if (!Context.IsWorldReady || here?.Name != Vale)
            return;
        if (_arriving || _drivingOff)
            Game1.player.CanMove = false;

        if (_drivingOff && !_leaving)
        {
            _motion.X -= 0.075f;
            if (_pos.X + 512f < 0f)
            {
                _leaving = true;
                Game1.globalFadeToBlack(() =>
                {
                    _returningFromVale = true;
                    Game1.viewport.Y = -100000;
                    Game1.viewportFreeze = true;
                    Game1.warpFarmer("BusStop", 22, 10, flip: true);
                }, 0.01f);
            }
        }
        _pos += _motion;
        if (_door != null)
        {
            _door.Position += _motion;
            _door.update(Game1.currentGameTime);
        }
    }

    // Desert.doorOpenAfterReturn
    private static void SteppedOff(GameLocation here)
    {
        here.localSound("batFlap");
        _arriving = false;
        _door = NewDoor(_pos, closed: false);
        Game1.player.Position = StepOff.ToVector2() * 64f;
        here.lastTouchActionLocation = Game1.player.Tile;
        Game1.displayFarmer = true;
        Game1.player.forceCanMove();
        Game1.player.faceDirection(2);
        Game1.freezeControls = false;
        Game1.viewportFreeze = false;
        Game1.changeMusicTrack("none", track_interruptable: true);
        GameLocation.HandleMusicChange(null, here);
    }

    // Desert.draw: the bus, its door, and Pam at the wheel while it moves.
    private static void DrawPostfix(GameLocation __instance, SpriteBatch b)
    {
        if (__instance.Name != Vale || __instance != Game1.currentLocation)
            return;
        float depth = (_pos.Y + 192f) / 10000f;
        b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((int)_pos.X, (int)_pos.Y)), BusSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
        _door?.draw(b);
        if (_drivingOff)
        {
            if (Game1.netWorldState.Value.canDriveYourselfToday.Value)
            {
                Game1.player.faceDirection(3);
                Game1.player.blinkTimer = -1000;
                Game1.player.FarmerRenderer.draw(b, new FarmerSprite.AnimationFrame(117, 99999, 0, secondaryArm: false, flip: true), 117, new Rectangle(48, 608, 16, 32),
                    Game1.GlobalToLocal(new Vector2((int)(_pos.X + 4f), (int)(_pos.Y - 8f)) + PamOffset * 4f), Vector2.Zero, depth + 4E-04f, Color.White, 0f, 1f, Game1.player);
                b.Draw(Game1.mouseCursors2, Game1.GlobalToLocal(Game1.viewport, new Vector2((int)_pos.X, (int)_pos.Y - 40) + PamOffset * 4f), TransparentWindow, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth + 8E-04f);
            }
            else
                b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((int)_pos.X, (int)_pos.Y) + PamOffset * 4f), PamSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth + 4E-04f);
        }
    }
}
#endif
