using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Menus;
using ArtStudio.Painting;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network.NetEvents;
using SObject = StardewValley.Object;

namespace ArtStudio.Social;

// The museum's restored paintings (1.3, restoration_mechanics.md §5): hand
// Gunther a restored old painting and it joins the collection (the whole
// farm's, like the museum's own). Talking to him again shows it. Each set
// finished brings its frame style by letter; more come at 6, 12 and all 21
// donated.
//
// A reward is granted whenever its condition holds and its letter exists in
// the game's mail, so one added later reaches players who already qualify.
internal static class MuseumCollection
{
    public const string Curator = "Gunther";
    private const string DonatedPrefix = ModEntry.Id + "_Donated_";
    private const string RewardPrefix = ModEntry.Id + "_Collection_";

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
            prefix: new HarmonyMethod(typeof(MuseumCollection), nameof(CheckActionPrefix)));
        // His counter (1.3, from playtesting): donating a painting sits with
        // donating artifacts, and the collection with it.
        harmony.Patch(AccessTools.Method(typeof(StardewValley.Locations.LibraryMuseum), "OpenGuntherDialogueMenu"),
            prefix: new HarmonyMethod(typeof(MuseumCollection), nameof(CounterPrefix)));
        harmony.Patch(AccessTools.Method(typeof(StardewValley.Locations.LibraryMuseum), nameof(GameLocation.answerDialogueAction)),
            prefix: new HarmonyMethod(typeof(MuseumCollection), nameof(CounterAnswerPrefix)));
    }

    // A restored old painting the museum hasn't got, framed or not.
    public static OldPainting? Donatable(Item? item) =>
        item != null && (PaintingItems.IsPainting(item) || ArtItems.IsFramed(item))
        && PaintingData.Read(item) is { IsOld: true } data && OldPaintings.OfSubject(data.Subject) is { } old && !IsDonated(old.Id) ? old : null;

    public static bool HasDonatable(Farmer who) => who.Items.Any(i => Donatable(i) != null);

    // Gunther's counter: the game's Donate and Collect, with a painting to
    // donate, and the collection to look at once there's one in it.
    private static bool CounterPrefix(StardewValley.Locations.LibraryMuseum __instance)
    {
        try
        {
            var who = Game1.player;
            bool paintings = HasDonatable(who), collection = DonatedCount > 0;
            if (!paintings && !collection) return true;
            var answers = new List<Response>();
            bool locked = AccessTools.Field(typeof(StardewValley.Locations.LibraryMuseum), "mutex").GetValue(__instance) is StardewValley.Network.NetMutex mutex && mutex.IsLocked();
            if (__instance.doesFarmerHaveAnythingToDonate(who) && !locked)
                answers.Add(new Response("Donate", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Donate")));
            if (paintings)
                answers.Add(new Response("DonatePainting", I18n.Get("museum.counter.donate-painting")));
            if (__instance.getRewardsForPlayer(who).Count > 0)
                answers.Add(new Response("Collect", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Collect")));
            if (collection)
                answers.Add(new Response("PaintingCollection", I18n.Get("museum.counter.collection")));
            answers.Add(new Response("Leave", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Gunther_Leave")));
            __instance.createQuestionDialogue("", answers.ToArray(), "Museum");
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static bool CounterAnswerPrefix(string questionAndAnswer, ref bool __result)
    {
        if (questionAndAnswer == "Museum_DonatePainting")
        {
            OpenDonation(Game1.player);
            __result = true;
            return false;
        }
        if (questionAndAnswer == "Museum_PaintingCollection")
        {
            Game1.activeClickableMenu = new CollectionView();
            __result = true;
            return false;
        }
        return true;
    }

    // Picking which painting to give him, from the bag.
    internal static void OpenDonation(Farmer who)
    {
        Game1.activeClickableMenu = new PaintingPickerMenu(I18n.Get("museum.pick"), item => Donatable(item) != null, item =>
        {
            if (Donatable(item) is { } old && Game1.getCharacterFromName(Curator) is { } curator)
                Donate(who, curator, item, old);
        }, item => Donatable(item) is { } old ? OldPaintings.Title(old) + "\n" + OldPaintings.SetName(old.Set) : "");
    }

    public static bool IsDonated(string id) => Game1.player.mailReceived.Contains(DonatedPrefix + id);

    public static int DonatedCount => OldPaintings.All.Count(p => IsDonated(p.Id));

    public static bool SetDone(string set) => OldPaintings.All.Where(p => p.Set == set).All(p => IsDonated(p.Id));

    // The rewards: a key (its letter is RewardPrefix + key) and when it's earned.
    public static readonly (string Key, Func<bool> Earned)[] Rewards =
        OldPaintings.Sets.Select(set => ("set-" + set, (Func<bool>)(() => SetDone(set))))
            .Concat(new (string, Func<bool>)[]
            {
                // (The Old Valley's gallery furniture comes in three letters,
                // a letter carrying one item.)
                ("set-valley-rope", () => SetDone("valley")),
                ("set-valley-plinth", () => SetDone("valley")),
                ("count-6", () => DonatedCount >= 6),
                ("count-12", () => DonatedCount >= 12),
                ("count-21", () => DonatedCount >= OldPaintings.All.Length),
            }).ToArray();

    public static string RewardLetter(string key) => RewardPrefix + key;

    private static bool CheckActionPrefix(NPC __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (__instance.Name != Curator) return true;
            // Holding a restored old painting comes first, before his line for
            // the day (which stays for the next time you talk to him).
            if (who.ActiveObject is SObject held && (PaintingItems.IsPainting(held) || ArtItems.IsFramed(held))
                && PaintingData.Read(held) is { IsOld: true } data && OldPaintings.OfSubject(data.Subject) is { } old)
            {
                __result = true;
                if (IsDonated(old.Id))
                    Say(__instance, I18n.Get("museum.already", new { title = OldPaintings.Title(old) }));
                else
                    who.currentLocation.createQuestionDialogue(I18n.Get("museum.donate-ask", new { title = OldPaintings.Title(old) }), new[]
                    {
                        new Response("yes", I18n.Get("museum.donate-yes")),
                        new Response("no", I18n.Get("museum.donate-no")),
                    }, (_, answer) => { if (answer == "yes") Donate(who, __instance, held, old); });
                return false;
            }
            if (__instance.CurrentDialogue.Count > 0) return true;
            // The first time you talk to him once the painting wing's open.
            if (who.ActiveObject == null && PaintingWing.IsOpen && !who.mailReceived.Contains(PaintingWing.ToldFlag))
            {
                __result = true;
                who.mailReceived.Add(PaintingWing.ToldFlag);
                Say(__instance, I18n.Get("museum.wing-open"));
                return false;
            }
            if (who.ActiveObject == null && DonatedCount > 0
                && who.friendshipData.TryGetValue(Curator, out var friendship) && friendship.TalkedToToday)
            {
                __result = true;
                Game1.activeClickableMenu = new CollectionView();
                return false;
            }
            return true;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public static void Donate(Farmer who, NPC curator, Item painting, OldPainting old)
    {
        who.removeItemFromInventory(painting);
        // Framed, he keeps the painting and hands you back its frame.
        if (ArtItems.IsFramed(painting) && Frames.StyleOf(painting) is { } style)
            who.addItemByMenuIfNecessary(ItemRegistry.Create("(O)" + style.ItemId));
        Game1.player.team.RequestSetMail(PlayerActionTarget.All, DonatedPrefix + old.Id, MailType.Received, add: true);
        who.mailReceived.Add(DonatedPrefix + old.Id);
        Game1.playSound("newArtifact");
        var earned = GrantRewards(who);
        // The first: the wing opens tomorrow morning (PaintingWing.cs), and he says so.
        Say(curator, ThanksFor(old, earned));
    }

    // His thanks, and for a set just finished or a count just reached
    // (1.3, from playtesting), a page saying there's something in tomorrow
    // morning's post.
    public static string ThanksFor(OldPainting old, IReadOnlyList<string> earned)
    {
        var pages = new List<string>
        {
            I18n.Get(DonatedCount == 1 && !PaintingWing.IsOpen ? "museum.thanks-first" : "museum.thanks",
                new { title = OldPaintings.Title(old), count = DonatedCount, total = OldPaintings.All.Length }),
        };
        foreach (string key in earned)
        {
            if (key.StartsWith("set-") && OldPaintings.Sets.Contains(key.Substring(4)))
                pages.Add(I18n.Get("museum.reward.set", new { set = OldPaintings.SetName(key.Substring(4)) }));
            else if (key.StartsWith("count-"))
                pages.Add(I18n.Get("museum.reward." + key));
        }
        return string.Join("#$b#", pages);
    }

    // Letters for rewards earned and not yet sent (for whoever donated
    // last), for tomorrow's post. Returns the ones just sent.
    public static List<string> GrantRewards(Farmer who)
    {
        var mail = DataLoader.Mail(Game1.content);
        var sent = new List<string>();
        foreach (var (key, earned) in Rewards)
        {
            string letter = RewardLetter(key);
            if (!mail.ContainsKey(letter) || who.mailReceived.Contains(letter) || who.mailForTomorrow.Contains(letter) || !earned()) continue;
            Game1.addMailForTomorrow(letter);
            sent.Add(key);
        }
        return sent;
    }

    private static void Say(NPC npc, string text)
    {
        npc.CurrentDialogue.Clear();
        npc.CurrentDialogue.Push(new Dialogue(npc, null, text));
        Game1.drawDialogue(npc);
    }

    // ---- The collection, set by set ----

    internal static IClickableMenu DevCollection(int set = -1, int column = 0)
    {
        var view = new CollectionView();
        if (set >= 0)
            view.DevMouse = new Point(view.xPositionOnScreen + CollectionView.Pad + CollectionView.LabelW + column * (CollectionView.Cell + CollectionView.Gap) + CollectionView.Cell / 2,
                view.yPositionOnScreen + CollectionView.Pad + 60 + set * (CollectionView.Cell + CollectionView.Gap) + CollectionView.Cell / 2);
        return view;
    }

    private sealed class CollectionView : IClickableMenu
    {
        public const int Cell = 80, Gap = 12, Pad = 40, LabelW = 200;
        public Point? DevMouse;
        private string? _hover, _hoverTitle;

        public CollectionView()
            : base(0, 0, 0, 0, showUpperRightCloseButton: true)
        {
            width = Math.Min(Game1.uiViewport.Width - 32, 2 * Pad + LabelW + 5 * (Cell + Gap));
            height = Math.Min(Game1.uiViewport.Height - 32, 2 * Pad + 60 + OldPaintings.Sets.Length * (Cell + Gap));
            xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
            yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
            initializeUpperRightCloseButton();
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
            Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
            Ui.AuditMenu(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
            _hover = _hoverTitle = null;
            var mouse = DevMouse ?? new Point(Game1.getMouseX(true), Game1.getMouseY(true));
            int x0 = xPositionOnScreen + Pad, y = yPositionOnScreen + Pad;
            Ui.Text(b, I18n.Get("museum.collection", new { count = DonatedCount, total = OldPaintings.All.Length }), new Vector2(x0, y));
            y += 60;
            foreach (string set in OldPaintings.Sets)
            {
                Ui.Text(b, OldPaintings.SetName(set), new Vector2(x0, y + Cell / 2 - 14), SetDone(set) ? new Color(120, 75, 0) : Game1.textColor, 0.85f);
                int x = x0 + LabelW;
                foreach (var painting in OldPaintings.All.Where(p => p.Set == set))
                {
                    var rect = new Rectangle(x, y, Cell, Cell);
                    Ui.Fill(b, rect, new Color(60, 44, 36));
                    var inner = new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12);
                    if (IsDonated(painting.Id) && Original(painting) is { } picture)
                        b.Draw(picture, inner, Color.White);
                    else
                        Ui.Fill(b, inner, new Color(243, 236, 220) * 0.2f);
                    if (rect.Contains(mouse))
                    {
                        // Its card, donated or not; missing, where to look.
                        _hoverTitle = OldPaintings.Title(painting);
                        string text = OldPaintings.About(painting);
                        if (!IsDonated(painting.Id))
                            text += "\n\n" + I18n.Get("museum.missing", new { where = OldPaintings.WhereToFind(set), cart = I18n.Get("old.where.cart") });
                        _hover = Game1.parseText(text, Game1.smallFont, 420);
                    }
                    x += Cell + Gap;
                }
                y += Cell + Gap;
            }
            base.draw(b);
            if (_hover != null) drawHoverText(b, _hover, Game1.smallFont, overrideX: DevMouse?.X ?? -1, overrideY: DevMouse?.Y ?? -1, boldTitleText: _hoverTitle);
            drawMouse(b);
        }

        private static readonly Dictionary<string, Texture2D> Pictures = new();

        private static Texture2D? Original(OldPainting painting)
        {
            if (Pictures.TryGetValue(painting.Id, out var cached) && !cached.IsDisposed) return cached;
            return Pictures[painting.Id] = PaintingRenderer.Build(painting.Size, OldPaintings.Original(painting),
                new Core.Purity[painting.Size, painting.Size], new Color(243, 236, 220));
        }
    }
}
