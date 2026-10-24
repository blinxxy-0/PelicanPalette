using System;
using System.Collections.Generic;
using System.Linq;
using ArtStudio.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Shop;

// The art shop's commission board: today's postings, each with the client's
// portrait, what they want painted, how good it has to be and the reward;
// and the commissions you've taken on, with the days left to deliver.
internal sealed class CommissionBoardMenu : IClickableMenu
{
    private const int MaxWidth = 960, MaxHeight = 660, Pad = 36, CardHeight = 136, CardGap = 8;

    private readonly Farmer _who;
    private readonly List<(Rectangle Card, Rectangle Accept, Commission Commission)> _cards = new();
    private Rectangle _activeArea;
    private string? _hoverText;

    public CommissionBoardMenu(Farmer who)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _who = who;
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(MaxWidth, Game1.uiViewport.Width - 64);
        height = Math.Min(MaxHeight, Game1.uiViewport.Height - 64);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
        initializeUpperRightCloseButton();

        _cards.Clear();
        int y = yPositionOnScreen + Pad + 56;
        foreach (var commission in Commissions.Posted(_who).Take(Commissions.MaxPosted))
        {
            var card = new Rectangle(xPositionOnScreen + Pad, y, width - 2 * Pad, CardHeight);
            var accept = new Rectangle(card.Right - 172, card.Center.Y - 28, 156, 56);
            _cards.Add((card, accept, commission));
            y += CardHeight + CardGap;
        }
        int activeTop = yPositionOnScreen + Pad + 56 + Commissions.MaxPosted * (CardHeight + CardGap) + 2;
        _activeArea = new Rectangle(xPositionOnScreen + Pad, activeTop, width - 2 * Pad, yPositionOnScreen + height - Pad - activeTop);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => Layout();

    private bool CanAcceptMore => Commissions.Accepted(_who).Count() < Commissions.MaxAccepted;

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        foreach (var (_, accept, commission) in _cards)
        {
            if (!accept.Contains(x, y)) continue;
            if (CanAcceptMore && Commissions.Accept(_who, commission))
            {
                Game1.playSound("newArtifact");
                Game1.addHUDMessage(new HUDMessage($"Commission taken: a {commission.ItemName} for {commission.NpcName}.") { noIcon = true });
                Layout();
            }
            else
                Game1.playSound("cancel");
            return;
        }
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);
        Ui.Frame(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        Ui.AuditMenu(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        _hoverText = null;
        var mouse = new Point(Game1.getMouseX(true), Game1.getMouseY(true));

        Ui.Text(b, "Commission Board", new Vector2(xPositionOnScreen + Pad + 8, yPositionOnScreen + Pad + 6));
        string note = "Paint it, then hand it to them yourself.";
        Ui.Text(b, note, new Vector2(xPositionOnScreen + width - Pad - 8 - Ui.TextWidth(note), yPositionOnScreen + Pad + 6), Game1.textColor * 0.65f);

        if (_cards.Count == 0)
            Ui.Text(b, "No new requests today. Check back tomorrow.", new Vector2(xPositionOnScreen + Pad + 8, yPositionOnScreen + Pad + 70), Game1.textColor * 0.6f);
        foreach (var (card, accept, commission) in _cards)
            DrawCard(b, card, accept, commission, mouse);

        DrawAccepted(b);

        base.draw(b);
        if (_hoverText != null)
            drawHoverText(b, _hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private void DrawCard(SpriteBatch b, Rectangle card, Rectangle accept, Commission commission, Point mouse)
    {
        Ui.Paper(b, card);
        var portrait = new Rectangle(card.X + 16, card.Y + 20, 96, 96);
        var texture = Game1.getCharacterFromName(commission.Npc)?.Portrait ?? TryLoadPortrait(commission.Npc);
        if (texture != null)
            b.Draw(texture, portrait, new Rectangle(0, 0, 64, 64), Color.White);
        else
            Ui.Fill(b, portrait, Color.Black * 0.1f);

        int x = portrait.Right + 18;
        // Three lines: who and what, the subject, then quality and time.
        Ui.Text(b, $"{commission.NpcName} would like a painting of", new Vector2(x, card.Y + 16));
        var item = ItemRegistry.Create(commission.ItemId);
        item.drawInMenu(b, new Vector2(x - 8, card.Y + 38), 0.75f, 1f, 0.9f, StackDrawType.Hide, Color.White, false);
        Ui.Text(b, commission.ItemName, new Vector2(x + 48, card.Y + 52));
        string days = commission.DaysLeft <= 0 ? "last day to take it" : $"{commission.DaysLeft + 1} days to take it";
        Ui.Text(b, $"Quality: {Commissions.QualityText(commission.MinQuality)},  {days}", new Vector2(x, card.Y + 88), Game1.textColor * 0.7f);
        Ui.Text(b, $"{commission.Reward}g", new Vector2(accept.X - 16 - Ui.TextWidth($"{commission.Reward}g"), card.Y + 52), new Color(120, 75, 0));

        bool can = CanAcceptMore;
        Ui.Button(b, accept, "Accept", can, false, can && accept.Contains(mouse));
        if (accept.Contains(mouse))
            _hoverText = can
                ? $"Deliver within {Commissions.DaysToDeliver} days for {commission.Reward}g and {commission.NpcName}'s gratitude."
                : $"You can only take on {Commissions.MaxAccepted} commissions at a time.";
    }

    private static Texture2D? TryLoadPortrait(string npc)
    {
        try { return Game1.content.Load<Texture2D>("Portraits/" + npc); }
        catch (Microsoft.Xna.Framework.Content.ContentLoadException) { return null; }
    }

    private void DrawAccepted(SpriteBatch b)
    {
        var accepted = Commissions.Accepted(_who).ToList();
        int y = _activeArea.Y;
        Ui.Text(b, $"Your commissions ({accepted.Count}/{Commissions.MaxAccepted})", new Vector2(_activeArea.X + 8, y));
        y += 34;
        if (accepted.Count == 0)
            Ui.Text(b, "None yet.", new Vector2(_activeArea.X + 24, y), Game1.textColor * 0.6f);
        foreach (var c in accepted)
        {
            string days = c.DaysLeft <= 0 ? "due today" : $"{c.DaysLeft + 1} days left";
            Ui.Text(b, $"{c.NpcName}: {c.ItemName} ({Commissions.QualityText(c.MinQuality)}), {c.Reward}g, {days}",
                new Vector2(_activeArea.X + 24, y), Game1.textColor * 0.85f);
            y += 30;
        }
    }
}
