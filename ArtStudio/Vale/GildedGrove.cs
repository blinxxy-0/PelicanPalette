#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ArtStudio.Vale;

// The Gilded Grove (the Vale painters' guide design, section 5): a painting of the first painters'
// favourite place, a small map of its own, reached only through the guide's last page and left only
// by the painted frame at its edge (section 3). It's always fall there (its location context's
// SeasonOverride, so the game's seasonal sheets show their fall faces all year) and always sunny;
// its light is held at a warm late-afternoon gold day and night, and paint motes drift while you're in it.
internal static class GildedGrove
{
    public const string Location = ModEntry.Id + "_GildedGrove";
    public const string FrameAction = ModEntry.Id + "_GroveFrame";
    public const string HiddenEaselAction = ModEntry.Id + "_HiddenEasel";
    public const string FoundersEaselAction = ModEntry.Id + "_FoundersEasel";
    public const string EchoAction = ModEntry.Id + "_Echo";

    // Where the last page sets you down: the middle of the Grove, facing the view
    // (tools/build_vale_grove.py ARRIVAL; data/vale_guide.json DefaultArrivalTile).
    public static readonly Point ArrivalTile = new(12, 11);

    // Where the frame sets you down in the Vale: the old trail's foot, below the Painter's clearing.
    // TODO(Task 6): the graveyard's gate. Replace with ValeGraves.GraveyardGateTile once it exists.
    public static readonly Point ReturnTile = new(8, 23);

    // The paint wash's gold.
    private static readonly Color[] Gold =
    {
        new(236, 180, 40), new(252, 216, 96), new(200, 140, 32), new(255, 238, 168), new(222, 150, 72),
    };

    // The Grove's light, held all day and night: the game's lighting takes this colour away from the
    // scene, so a little green and more blue taken out leaves a warm late-afternoon gold.
    public static readonly Color GroveLight = new(0, 24, 64);

    public static IMonitor? Monitor;

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        Monitor = monitor;
        GameLocation.RegisterTileAction(FrameAction, (_, _, who, _) => OnFrame(who));
        GameLocation.RegisterTileAction(HiddenEaselAction, (_, _, who, tile) => OnHiddenEasel(who, tile));
        GameLocation.RegisterTileAction(FoundersEaselAction, (_, _, who, _) => OnFoundersEasel(who));
        // Task 9 fills this in; until then it does nothing.
        GameLocation.RegisterTileAction(EchoAction, (_, _, _, _) => false);
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer) Motes.Clear(); };
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { _pending = null; Motes.Clear(); };
    }

    public static bool InGrove(Farmer who) => who.currentLocation?.Name == Location;

    // Where the screen is (the light and the motes follow what's drawn, not where a farmer is filed).
    private static bool ShowingGrove => Context.IsWorldReady && Game1.currentLocation?.Name == Location;

    // ---- In and out ----

    // Answering a question happens inside its box, so the player isn't free yet: the warp waits for the
    // box to close (up to a few seconds), then WarpIn or WarpHome checks everything again.
    private static (Farmer Who, bool In, int Ticks)? _pending;

    public static void RequestWarpIn(Farmer who) => _pending = (who, true, 300);
    public static void RequestWarpHome(Farmer who) => _pending = (who, false, 300);
    public static bool WarpPending => _pending != null;

    // The last page: a flash, sparkles and a wash of gold paint, then the Grove. Only from the Vale,
    // only with the Grove open, only when the player is free. effects:false warps at once (the checks).
    public static bool WarpIn(Farmer who, bool effects = true)
    {
        if (!Context.IsPlayerFree || PaintSplash.Playing || who.currentLocation?.Name != ValeBus.Vale
            || !who.mailReceived.Contains(PaintersGuide.GroveOpenFlag))
            return false;
        return Go(who, effects, true);
    }

    // The frame: back to the Vale, on the graveyard's gate or the nearest open tile to it.
    public static bool WarpHome(Farmer who, bool effects = true)
    {
        if (!Context.IsPlayerFree || PaintSplash.Playing || !InGrove(who))
            return false;
        return Go(who, effects, false);
    }

    private static bool Go(Farmer who, bool effects, bool into)
    {
        if (!effects)
            return FinishWarp(who, into);
        Game1.flashAlpha = 1f;
        who.currentLocation.playSound("wand");
        var rng = Game1.random;
        for (int i = 0; i < 14; i++)
            who.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(354, rng.Next(25, 75), 6, 1,
                new Vector2(rng.Next((int)who.Position.X - 192, (int)who.Position.X + 128), rng.Next((int)who.Position.Y - 192, (int)who.Position.Y + 64)),
                false, rng.NextDouble() < 0.5));
        PaintSplash.Play(Gold, false, () => FinishWarp(who, into));
        return true;
    }

    // The warp itself, at the end of the paint wash. Everything is checked again, since a second and a
    // half has passed: a box may have opened, or it may be 2am and time to pass out. If anything's
    // wrong, nothing happens and the player can move again.
    public static bool FinishWarp(Farmer who, bool into)
    {
        bool ok = Context.IsWorldReady && Context.IsPlayerFree && !Game1.eventUp && !Game1.isWarping
            && Game1.timeOfDay < 2600 && !who.passedOut
            && who.currentLocation?.Name == (into ? ValeBus.Vale : Location)
            && (!into || who.mailReceived.Contains(PaintersGuide.GroveOpenFlag));
        if (!ok)
        {
            if (!Game1.eventUp && Game1.activeClickableMenu == null)
                who.CanMove = true;
            return false;
        }
        if (into)
            Game1.warpFarmer(Location, ArrivalTile.X, ArrivalTile.Y, 0);
        else
        {
            var vale = Game1.getLocationFromName(ValeBus.Vale);
            Point tile = vale == null ? ReturnTile : HomeTile(vale);
            Game1.warpFarmer(ValeBus.Vale, tile.X, tile.Y, 2);
        }
        return true;
    }

    // ReturnTile if it's open and reachable on foot from the Vale's paths; otherwise the nearest such
    // tile, searching outward ring by ring.
    public static Point HomeTile(GameLocation vale)
    {
        var reach = Reachable(vale);
        for (int r = 0; r <= 8; r++)
        {
            Point? best = null;
            double bestDist = double.MaxValue;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                        continue;
                    var p = new Point(ReturnTile.X + dx, ReturnTile.Y + dy);
                    double d = dx * dx + dy * dy;
                    if (d < bestDist && Open(vale, p) && (reach.Count == 0 || reach.Contains(p)))
                        (best, bestDist) = (p, d);
                }
            if (best != null)
                return best.Value;
        }
        Monitor?.Log($"No open tile near {ReturnTile} in the Vale; using it anyway.", LogLevel.Warn);
        return ReturnTile;
    }

    // Dry, walkable ground nothing stands on (a placed object, furniture, an NPC, a tree). Other farmers
    // don't count (farmers walk through each other), nor do things you walk over (grass, flooring).
    public static bool Open(GameLocation location, Point p)
    {
        var tile = new Vector2(p.X, p.Y);
        return location.isTileOnMap(tile)
            && location.isTilePassable(tile)
            && !location.isWaterTile(p.X, p.Y)
            && !location.IsTileOccupiedBy(tile, CollisionMask.All & ~CollisionMask.Farmers, CollisionMask.All)
            && location.getObjectAtTile(p.X, p.Y) == null
            && location.isCharacterAtTile(tile) == null;
    }

    // Whether the frame may set you down here: open, and reachable on foot from the Vale's paths.
    public static bool Landable(GameLocation vale, Point p)
    {
        var reach = Reachable(vale);
        return Open(vale, p) && (reach.Count == 0 || reach.Contains(p));
    }

    // Where the Vale's paths start: the bus stop (tools/vale_routes.py PLACES, "bus stop").
    public static readonly Point PathStart = new(12, 8);

    // Every tile you can walk to from the bus stop over the map's own ground: not water, nothing solid
    // on it, no tree or bush in the way. Empty if the bus stop itself isn't walkable (then Open alone
    // decides).
    public static HashSet<Point> Reachable(GameLocation vale)
    {
        var seen = new HashSet<Point>();
        bool Walk(Point p)
        {
            var v = new Vector2(p.X, p.Y);
            return vale.isTileOnMap(v) && vale.isTilePassable(v) && !vale.isWaterTile(p.X, p.Y)
                && !vale.IsTileOccupiedBy(v, CollisionMask.TerrainFeatures, CollisionMask.All);
        }
        if (!Walk(PathStart))
        {
            Monitor?.Log($"The Vale's bus stop {PathStart} isn't walkable; the frame lands on any open tile.", LogLevel.Warn);
            return seen;
        }
        var queue = new Queue<Point>();
        seen.Add(PathStart);
        queue.Enqueue(PathStart);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var n in new[] { new Point(p.X + 1, p.Y), new Point(p.X - 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X, p.Y - 1) })
                if (!seen.Contains(n) && Walk(n))
                {
                    seen.Add(n);
                    queue.Enqueue(n);
                }
        }
        return seen;
    }

    // The frame's tile action: the question, then home.
    public static bool OnFrame(Farmer who)
    {
        if (PaintSplash.Playing)
            return true;
        who.currentLocation.createQuestionDialogue(PaintersGuide.Text("vale.grove.frame"), new[]
        {
            new Response("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")),
            new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")),
        }, (_, answer) =>
        {
            if (answer == "Yes")
                RequestWarpHome(who);
        });
        return true;
    }

    // ---- The hidden easel ----

    // Where the two easels stand: their feet, which carry the actions (tools/build_vale_grove.py PIECES).
    public static readonly Point HiddenEaselTile = new(16, 8);
    public static readonly Point FoundersEaselTile = new(9, 21);

    // The gold light's bonus: you start one spark of the three (PaintingRules.SparksForInspired) that
    // make a painting Inspired, so a masterpiece is a little more likely here.
    public const int HiddenEaselSparks = 1;

    // The hidden easel opens the easel itself, the one found object in the Vale that still does. It
    // looks out over the panorama, so plein air paints the Vale from above.
    public static bool OnHiddenEasel(Farmer who, Point tile)
    {
        if (!who.IsLocalPlayer || Game1.activeClickableMenu != null)
            return false;
        ArtHooks.OpenEasel(who, tile.ToVector2(), startingSparks: HiddenEaselSparks);
        return true;
    }

    // ---- The Founders' easel ----

    // Set for a player once they've offered the Founders' easel a masterpiece (each player once).
    public const string FoundersFlag = ModEntry.Id + "_FoundersOffered";
    private const string AskLine = "vale.grove.founders.ask";

    // What the easel says to this player holding this item (the line's key): thanks once they've
    // offered; the bare canvas wanted, for a framed masterpiece; the question, for a masterpiece;
    // otherwise, that it waits for one.
    public static string LineFor(Farmer who, Item? item) =>
        who.mailReceived.Contains(FoundersFlag) ? "vale.grove.founders.thanks"
        : item != null && ArtItems.IsFramed(item) && PaintingData.IsMasterpiece(item) ? "vale.grove.founders.framed"
        : item != null && PaintingItems.IsPainting(item) && PaintingData.IsMasterpiece(item) ? AskLine
        : "vale.grove.founders.not_masterpiece";

    // The offering: a masterpiece from your bag, once. The easel takes it (it's gone, as Old Master
    // Cannoli's berry is), and you get the Founders' Frame. False, and nothing changes, otherwise.
    public static bool Offer(Farmer who, Item? item)
    {
        if (item == null || LineFor(who, item) != AskLine || !who.Items.Contains(item))
            return false;
        if (item.Stack > 1)
            item.Stack--;
        else
            who.removeItemFromInventory(item);
        who.mailReceived.Add(FoundersFlag);
        var frame = ItemRegistry.Create("(O)" + ArtItems.MoreFrame("founders").ItemId);
        if (!who.addItemToInventoryBool(frame))
            Game1.createItemDebris(frame, who.getStandingPosition(), who.FacingDirection, who.currentLocation);
        return true;
    }

    public static bool OnFoundersEasel(Farmer who)
    {
        if (!who.IsLocalPlayer)
            return false;
        var item = who.CurrentItem;
        string line = LineFor(who, item);
        if (line != AskLine)
        {
            Game1.drawObjectDialogue(PaintersGuide.Text(line));
            return true;
        }
        who.currentLocation.createQuestionDialogue(PaintersGuide.Text(line), new[]
        {
            new Response("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")),
            new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")),
        }, (_, answer) =>
        {
            if (answer == "Yes" && Offer(who, item))
            {
                who.currentLocation.playSound("newArtifact");
                Game1.drawObjectDialogue(PaintersGuide.Text("vale.grove.founders.taken"));
            }
        });
        return true;
    }

    // Afterwards, for that player, the easel holds a finished canvas (any canvas, not theirs). It's
    // drawn over the empty easel from the Grove's own sheet, so each player (and each split screen)
    // sees their own: the art sits in the column after the map's pieces, at the pieces' top row
    // (tools/build_vale_grove.py EXTRAS). Only the canvas is drawn (the top tile and the canvas's
    // last two rows), so a farmer standing below the easel isn't covered.
    public static bool ShowsFinishedCanvas(Farmer who) => who.mailReceived.Contains(FoundersFlag);

    private const int FinishedCanvasColumn = 7, PiecesRow = 7;
    private static readonly Rectangle FinishedCanvasSource = new(FinishedCanvasColumn * 16, PiecesRow * 16, 16, 18);

    private static Texture2D? _sheet;
    private static string? _sheetSource;

    // The Grove's own sheet, as the map loaded it (null, and said in the log, if it can't be had).
    public static Texture2D? FinishedCanvasTexture(GameLocation? grove)
    {
        var tiles = grove?.Map?.TileSheets.FirstOrDefault(t => t.ImageSource?.Contains("GildedGrove") == true);
        if (tiles == null)
            return null;
        if (_sheetSource != tiles.ImageSource || _sheet?.IsDisposed == true)
        {
            _sheetSource = tiles.ImageSource;
            try { _sheet = Game1.content.Load<Texture2D>(tiles.ImageSource); }
            catch (Exception ex)
            {
                _sheet = null;
                Monitor?.Log($"The Grove's sheet '{tiles.ImageSource}' couldn't be loaded for the Founders' easel: {ex.Message}", LogLevel.Warn);
            }
        }
        return _sheet;
    }

    private static void DrawFinishedCanvas(SpriteBatch b)
    {
        if (!ShowsFinishedCanvas(Game1.player) || FinishedCanvasTexture(Game1.currentLocation) is not { } sheet)
            return;
        var at = Game1.GlobalToLocal(Game1.viewport, new Vector2(FoundersEaselTile.X * 64, (FoundersEaselTile.Y - 1) * 64));
        b.Draw(sheet, at, FinishedCanvasSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
    }

    // ---- The light and the motes ----

    private sealed class Mote
    {
        public Vector2 Pos;      // world pixels
        public Vector2 Drift;    // pixels a tick
        public Color Colour;
        public int Size, Age, Life;
        public float Phase;
    }

    private static readonly List<Mote> Motes = new();
    private const int MoteCount = 28;

    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (_pending is var (who, into, ticks))
        {
            if (Context.IsPlayerFree)
            {
                _pending = null;
                if (into)
                    WarpIn(who);
                else
                    WarpHome(who);
            }
            else
                _pending = ticks > 1 ? (who, into, ticks - 1) : null;
        }

        HoldLight();
        if (!ShowingGrove)
        {
            Motes.Clear();
            return;
        }
        var rng = Game1.random;
        var view = Game1.viewport;
        for (int i = Motes.Count - 1; i >= 0; i--)
        {
            var m = Motes[i];
            m.Pos += m.Drift;
            m.Pos.X += (float)Math.Sin((m.Age + m.Phase) / 40f) * 0.25f;
            if (++m.Age >= m.Life)
                Motes.RemoveAt(i);
        }
        while (Motes.Count < MoteCount)
            Motes.Add(new Mote
            {
                Pos = new Vector2(view.X + rng.Next(view.Width), view.Y + rng.Next(view.Height)),
                Drift = new Vector2((float)(rng.NextDouble() - 0.5) * 0.3f, -0.15f - (float)rng.NextDouble() * 0.35f),
                Colour = rng.NextDouble() < 0.6 ? Gold[rng.Next(Gold.Length)] : PaintersGuide.Spots[rng.Next(PaintersGuide.Spots.Length)].Palette[0],
                Size = rng.Next(2) == 0 ? 4 : 8,
                Life = rng.Next(180, 420),
                Phase = (float)rng.NextDouble() * 100f,
            });
    }

    // The game works its light out again every tick (Game1.UpdateGameClock and UpdateOther), before
    // this runs and before the frame is drawn. In the Grove it's set back to the gold, so night never
    // falls there. The game's own outdoor light is kept and put back on the way out, because by day
    // the game doesn't work it out again.
    private static Color? _gameLight;

    private static void HoldLight()
    {
        if (ShowingGrove)
        {
            if (Game1.outdoorLight != GroveLight)
                _gameLight = Game1.outdoorLight;
            Game1.outdoorLight = GroveLight;
            Game1.drawLighting = true;
        }
        else if (_gameLight is Color light)
        {
            if (Game1.outdoorLight == GroveLight)
                Game1.outdoorLight = light;
            _gameLight = null;
        }
    }

    private static void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!ShowingGrove)
            return;
        DrawFinishedCanvas(e.SpriteBatch);
        var view = Game1.viewport;
        foreach (var m in Motes)
        {
            // Fade in and out over each mote's life, and twinkle a little.
            float t = m.Age / (float)m.Life;
            float alpha = Math.Min(1f, Math.Min(t, 1f - t) * 5f) * (0.65f + 0.35f * (float)Math.Sin((m.Age + m.Phase) / 9f));
            Vector2 at = Game1.GlobalToLocal(view, m.Pos);
            e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle((int)at.X, (int)at.Y, m.Size, m.Size), m.Colour * alpha);
        }
    }

    public static int MoteTotal => Motes.Count;

    public static IEnumerable<string> StringKeys => new[]
    {
        "vale.grove.frame", "vale.grove.founders.ask", "vale.grove.founders.taken", "vale.grove.founders.thanks",
        "vale.grove.founders.not_masterpiece", "vale.grove.founders.framed",
    };
}
#endif
