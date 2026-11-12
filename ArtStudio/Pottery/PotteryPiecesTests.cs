#if DEBUG
using System;
using System.IO;
using System.Linq;
using ArtStudio.Core;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.SaveSerialization;

namespace ArtStudio.Pottery;

// The dev checks for kept pots (PotteryPieces), and sample pieces to look at.
internal static class PotteryPiecesTests
{
    public static void Register(IModHelper helper, IMonitor monitor)
    {
        DevTools.AddCommand(helper, "museart_potitemtest", "Kept pots (1.5): the bots throw, glaze and fire each shape; the piece in your bag, its data through modData and a save, its price, quality, name, gifts, shops, a table, and its sprite (a sheet of them to dialogue_snapshots/potpieces.png).", (_, _) => DevTools.WithCleanBag(Game1.player, () => Run(helper, monitor)));
        DevTools.AddCommand(helper, "museart_pots", "Sample pots of every shape and glaze. Usage: museart_pots [bag] (one of each shape) | place <x> <y> (every shape in every glaze, here, from that tile) | table <x> <y> <shape> <glaze 0-5> [gift] (on the table there) | gifts <x> <y> (a vase with each kiln gift, and a dull one) | scene <x> <y> (two tables with pieces on them, and some on the floor beside)", (_, args) => Spawn(monitor, args));
    }

    private static void Check(IMonitor monitor, string what, bool ok, string detail = "") =>
        monitor.Log($"[{(ok ? "PASS" : "FAIL")}] {what}{(detail.Length > 0 ? ": " + detail : "")}", ok ? LogLevel.Info : LogLevel.Error);

    // A pot as a perfect hand throws it, dipped in `glaze` and banded to the shape's design
    // (a band in the base's own glaze takes the design's base instead).
    public static PotPiece Sample(string shape, int glaze, string? gift = null, bool dull = false, float stars = 4.5f, int seed = 1)
    {
        var pot = new ClayPot(PotteryRules.For("normal"));
        PotBot.Throw(pot, PotShape.Make(shape, pot.Volume), PotHand.Perfect, new Random(seed));
        var coat = new PotGlaze(pot.Rows);
        if (glaze >= 0)
            coat.Dip(glaze);
        var design = GlazeDesign.For(shape);
        foreach (var band in design.Bands)
        {
            int g = band.Glaze != glaze ? band.Glaze : design.Base != glaze ? design.Base : (glaze + 2) % Glazes.All.Length;
            float lo = band.From * pot.Rows + PotGlaze.BrushRows / 2f, hi = band.To * pot.Rows - PotGlaze.BrushRows / 2f;
            if (hi < lo)
                lo = hi = (lo + hi) / 2f;
            for (float h = lo; ; h = MathF.Min(hi, h + 1f / PotGlaze.SlicesPerRow))
            {
                coat.Paint(g, h, 0f, PotGlaze.Buckets - 0.001f);
                if (h >= hi)
                    break;
            }
        }
        var firing = new FiringResult(dull ? 0.4f : 0.95f, dull ? "Underfired" : "Perfect", gift);
        return PotPiece.From(shape, pot, coat, firing, stars, Game1.player);
    }

    private static void Spawn(IMonitor monitor, string[] args)
    {
        if (!Context.IsWorldReady)
            return;
        var who = Game1.player;
        var location = Game1.currentLocation;
        string mode = args.FirstOrDefault() ?? "bag";
        int Arg(int i) => int.Parse(args[i]);
        void Place(Furniture f, int x, int y)
        {
            f.TileLocation = new Vector2(x, y);
            location.furniture.Add(f);
        }
        switch (mode)
        {
            case "place":
                // a row a shape, a column a glaze; tall pieces get two rows of room
                int py = Arg(2);
                foreach (string shape in PotShape.Names)
                {
                    if (PotteryPieces.IsTall(shape))
                        py++;
                    for (int g = 0; g < Glazes.All.Length; g++)
                        Place(PotteryPieces.Create(Sample(shape, g, seed: g + 1), who), Arg(1) + g, py);
                    py += 2;
                }
                break;
            case "scene":
                // for looking at in a room: a big table and an end table with pieces on them,
                // and pieces on the floor beside them (anything lying about cleared first)
                location.debris.Clear();
                int sx = Arg(1), sy = Arg(2);
                var tables = DataLoader.Furniture(Game1.content).Where(p => p.Value.Split('/')[1] == "table").ToList();
                string big = tables.First(p => p.Value.Split('/')[3] == "2 2").Key, small = tables.FirstOrDefault(p => p.Value.Split('/')[3] == "1 1").Key ?? big;
                var bigTable = ItemRegistry.Create<Furniture>("(F)" + big);
                Place(bigTable, sx, sy);
                bigTable.performObjectDropInAction(PotteryPieces.Create(Sample("vase", 1), who), false, null);
                var endTable = ItemRegistry.Create<Furniture>("(F)" + small);
                Place(endTable, sx + 3, sy + 1);
                endTable.performObjectDropInAction(PotteryPieces.Create(Sample("cup", 3, "flash"), who), false, null);
                Place(PotteryPieces.Create(Sample("jug", 5), who), sx + 5, sy + 1);
                Place(PotteryPieces.Create(Sample("bowl", 4, "crackle"), who), sx + 6, sy + 1);
                Place(PotteryPieces.Create(Sample("vase", 2, "drip"), who), sx + 7, sy + 1);
                Place(PotteryPieces.Create(Sample("cup", 0), who), sx + 5, sy + 3);
                Place(PotteryPieces.Create(Sample("bowl", 1, dull: true), who), sx + 6, sy + 3);
                break;
            case "gifts":
                int gx = Arg(1);
                foreach (string? gift in PotPiece.GiftKinds.Append(null))
                    Place(PotteryPieces.Create(Sample("vase", 1, gift, dull: gift == null), who), gx++, Arg(2));
                break;
            case "table":
                var table = location.furniture.FirstOrDefault(f => f.TileLocation == new Vector2(Arg(1), Arg(2)) || f.GetBoundingBox().Contains(Arg(1) * 64 + 32, Arg(2) * 64 + 32));
                var pot = PotteryPieces.Create(Sample(args[3], int.Parse(args[4]), args.Length > 5 ? args[5] : null), who);
                if (table == null || !table.performObjectDropInAction(pot, false, null))
                    monitor.Log($"No table with room at {Arg(1)},{Arg(2)}.", LogLevel.Warn);
                break;
            default:
                for (int i = 0; i < PotShape.Names.Length; i++)
                    who.addItemToInventoryBool(PotteryPieces.Create(Sample(PotShape.Names[i], new[] { 1, 3, 2, 5 }[i], PotPiece.GiftKinds[i]), who));
                break;
        }
    }

    private static void Run(IModHelper helper, IMonitor monitor)
    {
        if (!Context.IsWorldReady)
            return;
        var who = Game1.player;
        var rules = PotteryRules.For("normal");
        int Pots() => who.Items.Count(PotteryPieces.IsPot);

        foreach (string shape in PotShape.Names)
        {
            // ---- from the wheel: throw, glaze and fire it, as the bots do ----
            int before = Pots();
            PotteryPieces.LastFired = null;
            var menu = new PotteryMenu(shape, rules, fire: true);
            Game1.activeClickableMenu = menu;
            menu.FireBot("steady");
            var last = PotteryPieces.LastFired;
            Check(monitor, $"{shape}: the fired pot goes into the bag", last != null && Pots() == before + 1 && who.Items.Contains(last.Value.Item), $"{Pots() - before} new; open: {Game1.activeClickableMenu?.GetType().Name ?? "none"}, level {PotterySkill.Level(who)}");
            if (last is not { } fired)
            {
                Game1.activeClickableMenu = null;
                continue;
            }
            var item = fired.Item;
            var piece = PotPiece.Read(item);
            Check(monitor, $"{shape}: it's furniture of its shape", item.QualifiedItemId == PotteryPieces.QualifiedIdFor(shape) && piece?.Shape == shape, item.QualifiedItemId);
            if (piece == null)
                continue;
            Check(monitor, $"{shape}: its profile is kept", piece.Rows >= 4 && piece.R.All(r => r > 0f) && piece.T.All(t => t > 0f) && piece.T.Length == piece.Rows,
                $"{piece.Rows} rows, widest {piece.R.Max():0.0}");
            var design = GlazeDesign.For(shape);
            Check(monitor, $"{shape}: its glaze is kept (base and bands)", piece.Base == design.Base && piece.Slices == piece.Rows * PotGlaze.SlicesPerRow
                && design.Bands.All(b => piece.BandGlazes.Contains(b.Glaze)), $"{piece.GlazeName}, bands {string.Join("/", piece.BandGlazes.Select(g => Glazes.All[g].Name))}");
            Check(monitor, $"{shape}: its firing and stars are kept", piece.Verdict is "Perfect" or "Well fired" or "Underfired" or "Overfired"
                && piece.Dull == (piece.Verdict is "Underfired" or "Overfired") && piece.Stars == fired.Stars, $"{piece.Verdict}, gift {piece.Gift ?? "none"}, {piece.Stars} stars");
            Check(monitor, $"{shape}: quality from its stars", item.Quality == PotteryPieces.Quality(piece.Stars), $"quality {item.Quality}");
            int sell = item.sellToStorePrice();
            Check(monitor, $"{shape}: it has a price", item.Price == PotteryPieces.Value(piece, who) && item.Price > 0
                && Math.Abs(sell - item.Price * Market.QualityPrice(item.Quality)) <= 1f, $"{item.Price}g base, sells for {sell}g");
            Check(monitor, $"{shape}: named for its glaze", item.DisplayName == piece.Name && item.DisplayName.EndsWith(PotPiece.Title(shape)), item.DisplayName);
            string description = item.getDescription();
            Check(monitor, $"{shape}: its description gives its stars", description.Contains("star"), description.Replace('\n', ' '));

            // ---- through modData: a copy, a fresh item, and a save ----
            var copy = (Furniture)item.getOne();
            Check(monitor, $"{shape}: a copy keeps it all", Same(item, copy) && copy.Price == item.Price && copy.Quality == item.Quality);
            var fresh = ItemRegistry.Create<Furniture>(item.QualifiedItemId);
            foreach (var pair in item.modData.Pairs)
                fresh.modData[pair.Key] = pair.Value;
            var reread = PotPiece.Read(fresh);
            Check(monitor, $"{shape}: its modData reads back the same", reread != null && reread.R.SequenceEqual(piece.R) && reread.T.SequenceEqual(piece.T)
                && reread.Bands.SequenceEqual(piece.Bands) && reread.Base == piece.Base && reread.Gift == piece.Gift && reread.Stars == piece.Stars && reread.Dull == piece.Dull);
            Furniture? saved = null;
            try
            {
                using var stream = new MemoryStream();
                SaveSerializer.GetSerializer(typeof(Farmer)).Serialize(stream, who);
                stream.Position = 0;
                var back = (Farmer)SaveSerializer.GetSerializer(typeof(Farmer)).Deserialize(stream)!;
                saved = back.Items.OfType<Furniture>().FirstOrDefault(f => PotPiece.LookKey(f) == PotPiece.LookKey(item) && f.modData.GetValueOrDefault(ModEntry.Id + "/PotStars") == item.modData[ModEntry.Id + "/PotStars"]);
            }
            catch (Exception ex)
            {
                monitor.Log($"Saving the farmer failed: {ex.Message}", LogLevel.Warn);
            }
            Check(monitor, $"{shape}: saved and loaded, it's the same piece", saved != null && Same(item, saved) && saved.Price == item.Price && saved.Quality == item.Quality
                && saved.DisplayName == item.DisplayName && PotPieceRenderer.TextureFor(saved) != null, saved == null ? "not found after loading" : $"{saved.DisplayName}, {saved.Price}g");

            // ---- its sprite ----
            var sprite = PotPieceRenderer.TextureFor(item);
            int h = PotPieceRenderer.HeightFor(shape);
            var px = new Color[PotPieceRenderer.Width * h];
            sprite?.Texture.GetData(px);
            int opaque = px.Count(c => c.A > 0);
            Check(monitor, $"{shape}: it renders a sprite", sprite != null && sprite.Texture.Height == h && opaque >= 30, $"{opaque} pixels, {sprite?.Bounds}");
            Check(monitor, $"{shape}: it stands on its tile, inside the sprite", sprite != null && sprite.Bounds.Bottom == h - 1 && sprite.Bounds.X >= 0 && sprite.Bounds.Right <= PotPieceRenderer.Width,
                $"bottom row {sprite?.Bounds.Bottom - 1} of {h}");
            Check(monitor, $"{shape}: no black in it (outlines are a darker shade)", px.Where(c => c.A > 0).All(c => c.R + c.G + c.B > 24));

            // ---- firing the same pot again replaces it; a menu nobody opened gives nothing ----
            int count = Pots();
            AccessTools.Method(typeof(PotteryMenu), "StartFiring").Invoke(menu, null);
            menu.FireBot("steady");
            var again = PotteryPieces.LastFired?.Item;
            Check(monitor, $"{shape}: firing it again replaces it", again != null && again != item && Pots() == count && who.Items.Contains(again) && !who.Items.Contains(item));
            Game1.activeClickableMenu = null;
            var unseen = new PotteryMenu(shape, rules, fire: true);
            unseen.FireBot("steady");
            Check(monitor, $"{shape}: a wheel that isn't open gives nothing", Pots() == count);
            unseen.exitThisMenuNoSound();
            menu.exitThisMenuNoSound();
            Game1.activeClickableMenu = null;
        }

        // ---- a pot that cracks gives nothing ----
        {
            int count = Pots();
            var menu = new PotteryMenu("cup", PotteryRules.For("hard"), fire: true);
            Game1.activeClickableMenu = menu;
            PotteryPieces.LastFired = null;
            menu.FireBot("coal");
            Check(monitor, "a cracked pot gives nothing", Pots() == count && PotteryPieces.LastFired == null, $"{menu.FireScore:0.00}");
            menu.exitThisMenuNoSound();
            Game1.activeClickableMenu = null;
        }

        // ---- gifts: nobody dislikes it; Tobin, Fern and Bea are fond of good pottery ----
        var plain = PotteryPieces.Create(Sample("cup", 1, stars: 1f), who);
        var gold = PotteryPieces.Create(Sample("bowl", 0, stars: 4f), who);
        var iridium = PotteryPieces.Create(Sample("vase", 3, stars: 5f), who);
        Check(monitor, "a pot can be given", plain.canBeGivenAsGift() && iridium.canBeGivenAsGift());
        var villagers = Utility.getAllCharacters().Where(n => n.IsVillager && Game1.NPCGiftTastes.ContainsKey(n.Name)).ToList();
        var dislikers = villagers.Where(n => n.getGiftTasteForThisItem(plain) is NPC.gift_taste_dislike or NPC.gift_taste_hate).Select(n => n.Name).ToList();
        Check(monitor, "nobody dislikes a plain pot", villagers.Count > 0 && dislikers.Count == 0, $"{villagers.Count} villagers{(dislikers.Count > 0 ? ": " + string.Join(", ", dislikers) : "")}");
        var unloved = villagers.Where(n => n.getGiftTasteForThisItem(iridium) != NPC.gift_taste_love).Select(n => n.Name).ToList();
        Check(monitor, "everybody loves an iridium pot", unloved.Count == 0, string.Join(", ", unloved));
        int Taste(string name, Item pot) => (Game1.getCharacterFromName(name) is NPC npc ? npc.getGiftTasteForThisItem(pot) : PotteryPieces.PotTaste(new NPC { Name = name }, pot));
        Check(monitor, "Pierre shrugs at a plain pot, Leah (an art lover) likes it", Taste("Pierre", plain) == NPC.gift_taste_neutral && Taste("Leah", plain) == NPC.gift_taste_like);
        foreach (string name in PotteryPieces.PotteryLovers)
            Check(monitor, $"{name} likes any pot and loves a gold one", Taste(name, plain) == NPC.gift_taste_like && Taste(name, gold) == NPC.gift_taste_love,
                Game1.getCharacterFromName(name) == null ? "(not in this world: by the rule alone)" : "");
        if (Game1.getCharacterFromName("Pierre") is NPC pierre)
        {
            who.addItemToInventoryBool(iridium);
            // (a full bag: IndexOf is -1, and holding slot -1 crashes the game's update)
            who.CurrentToolIndex = Math.Max(0, who.Items.IndexOf(iridium));
            Check(monitor, "Pierre will take one from your hand", who.ActiveObject == iridium && pierre.tryToReceiveActiveObject(who, probe: true));
            who.friendshipData.TryAdd("Pierre", new Friendship());
            int points = who.getFriendshipLevelForNPC("Pierre");
            pierre.receiveGift(iridium, who, updateGiftLimitInfo: false, showResponse: false);
            Check(monitor, "giving it is a loved gift", who.getFriendshipLevelForNPC("Pierre") >= points + 80, $"{points} -> {who.getFriendshipLevelForNPC("Pierre")}");
            who.removeItemFromInventory(iridium);
        }

        // ---- sold where paintings are, and at Tobin's ----
        var shops = DataLoader.Shops(Game1.content);
        Check(monitor, "a pot carries the pottery tag", plain.HasContextTag(PotteryPieces.ContextTag));
        bool sells = false;
        try { sells = new ShopMenu("SeedShop", shops["SeedShop"], null, playOpenSound: false).highlightItemToSell(plain); }
        catch (Exception ex) { monitor.Log(ex.Message, LogLevel.Warn); }
        Check(monitor, "Pierre buys pots", sells);
        string tobins = ModEntry.Id + "_ValePottery";
        Check(monitor, "Tobin's counter buys pots", !shops.ContainsKey(tobins) || shops[tobins].SalableItemTags?.Contains(PotteryPieces.ContextTag) == true,
            shops.ContainsKey(tobins) ? "" : "(his shop isn't loaded)");

        // ---- on a table: one tile, so it fits; its price stays its own ----
        var tableId = DataLoader.Furniture(Game1.content).First(p => p.Value.Split('/')[1] == "table" && p.Value.Split('/')[3] == "2 2").Key;
        var table = ItemRegistry.Create<Furniture>("(F)" + tableId);
        var spot = Enumerable.Range(1, 30).SelectMany(y => Enumerable.Range(1, 30).Select(x => new Vector2(x, y)))
            .FirstOrDefault(t => table.canBePlacedHere(Game1.currentLocation, t));
        if (spot != Vector2.Zero)
        {
            table.TileLocation = spot;
            Game1.currentLocation.furniture.Add(table);
            var vase = PotteryPieces.Create(Sample("jug", 2), who);
            bool on = table.performObjectDropInAction(vase, false, null);
            var held = table.heldObject.Value as Furniture;
            Check(monitor, "a jug sits on a table, with its own price", on && held != null && PotteryPieces.IsPot(held) && held.Price == vase.Price && held.Price != PotteryPieces.BaseValue("jug"),
                $"{held?.Price}g on the table, {vase.Price}g in the bag");
            Game1.currentLocation.furniture.Remove(table);
        }
        else
            Check(monitor, "a jug sits on a table", false, "no room here for a table");

        // ---- every draw path, to a sheet to look at; the results screen with the kept piece ----
        Sheet(helper, monitor, who);
        ResultsScreens(helper, monitor, who);
    }

    // The kiln's results with the piece kept (in the bag, and with a full bag at your feet),
    // at both common sizes with the layout audit, to dialogue_snapshots/pottery_kept_*.png.
    private static void ResultsScreens(IModHelper helper, IMonitor monitor, Farmer who)
    {
        string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
        var problems = new System.Collections.Generic.List<string>();
        foreach (bool full in new[] { false, true })
            foreach (var (w, h) in new[] { (1280, 720), (1920, 1080) })
                DevTools.AtViewport(w, h, () =>
                {
                    var filler = new System.Collections.Generic.List<Item>();
                    if (full)
                        for (int i = 0; i < who.Items.Count; i++)
                            if (who.Items[i] == null)
                                filler.Add(who.Items[i] = ItemRegistry.Create("(O)390"));
                    var menu = new PotteryMenu("vase", PotteryRules.For("normal"), fire: true);
                    Game1.activeClickableMenu = menu;
                    menu.FireBot("steady");
                    Menus.Ui.Audit = new System.Collections.Generic.List<Menus.Ui.AuditEntry>();
                    DevTools.Render(menu, Path.Combine(dir, $"pottery_kept{(full ? "_floor" : "")}_{w}.png"));
                    problems.AddRange(Menus.Ui.AuditProblems().Select(p => $"{(full ? "full bag " : "")}@{w}x{h}: {p}"));
                    Menus.Ui.Audit = null;
                    menu.exitThisMenuNoSound();
                    Game1.activeClickableMenu = null;
                    foreach (var item in filler)
                        who.removeItemFromInventory(item);
                    Game1.currentLocation.debris.Clear();
                });
        Check(monitor, "the results show the kept piece, with no layout problems", problems.Count == 0, problems.Count == 0 ? Path.Combine(dir, "pottery_kept_*.png") : string.Join("; ", problems));
    }

    private static bool Same(Item a, Item b) =>
        a.modData.Pairs.Where(p => p.Key.StartsWith(ModEntry.Id + "/Pot")).All(p => b.modData.TryGetValue(p.Key, out string v) && v == p.Value);

    // Every shape in every glaze, drawn the ways the game draws furniture (in a menu, on a
    // tile, on a table), to dialogue_snapshots/potpieces.png at 1x and 3x.
    private static void Sheet(IModHelper helper, IMonitor monitor, Farmer who)
    {
        string dir = Path.Combine(helper.DirectoryPath, "dialogue_snapshots");
        Directory.CreateDirectory(dir);
        var device = Game1.graphics.GraphicsDevice;
        int cols = Glazes.All.Length + PotPiece.GiftKinds.Length + 1, cell = 72;
        int w = cols * cell, h = PotShape.Names.Length * cell * 2;
        bool ok = true;
        using (var target = new RenderTarget2D(device, w, h))
        {
            var batch = Game1.spriteBatch;
            var previous = device.GetRenderTargets();
            device.SetRenderTarget(target);
            device.Clear(new Color(196, 160, 112));
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
            try
            {
                for (int row = 0; row < PotShape.Names.Length; row++)
                    for (int col = 0; col < cols; col++)
                    {
                        string shape = PotShape.Names[row];
                        var piece = col < Glazes.All.Length ? Sample(shape, col, seed: col + 1)
                            : col < cols - 1 ? Sample(shape, 1, PotPiece.GiftKinds[col - Glazes.All.Length]) : Sample(shape, 1, dull: true);
                        var pot = PotteryPieces.Create(piece, who);
                        pot.drawInMenu(batch, new Vector2(col * cell + 4, row * cell * 2 + 4), 1f, 1f, 0.5f, StackDrawType.Draw, Color.White, false);
                        pot.drawAtNonTileSpot(batch, new Vector2(col * cell + 4, row * cell * 2 + cell + 4 - (PotPieceRenderer.HeightFor(shape) * 4 - 64)), 0.5f);
                    }
            }
            catch (Exception ex)
            {
                ok = false;
                monitor.Log($"Drawing pots failed: {ex}", LogLevel.Error);
            }
            batch.End();
            device.SetRenderTargets(previous);
            using var stream = File.Create(Path.Combine(dir, "potpieces.png"));
            target.SaveAsPng(stream, w, h);
        }
        Check(monitor, "every shape, glaze, kiln gift and dull glaze draws, in a menu and in the world", ok, Path.Combine(dir, "potpieces.png"));
    }
}
#endif
