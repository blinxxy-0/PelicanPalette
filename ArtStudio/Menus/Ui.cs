using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ArtStudio.Menus;

// Small drawing helpers in Stardew's own UI style, so the easel looks like
// it belongs next to the vanilla menus.
internal static class Ui
{
    private static readonly Rectangle PaperBox = new(384, 373, 18, 18);
    private static readonly Rectangle ButtonBox = new(432, 439, 9, 9);
    private static readonly Rectangle GoldStar = new(346, 400, 8, 8);

    public static void Fill(SpriteBatch b, Rectangle r, Color c) => b.Draw(Game1.staminaRect, r, c);

    // ---- Layout audit (dev snapshots) ----
    // While Audit is set, every piece of text, button and panel drawn is
    // recorded, so the snapshot command can report text that overlaps other
    // text, spills out of its button or panel, or leaves the menu.

    internal enum AuditKind { Menu, Panel, Button, Label, Text }

    internal readonly record struct AuditEntry(Rectangle Rect, string What, AuditKind Kind, int Owner);

    internal static List<AuditEntry>? Audit;

    private static int Record(Rectangle r, string what, AuditKind kind, int owner = -1)
    {
        if (Audit == null) return -1;
        Audit.Add(new AuditEntry(r, what, kind, owner));
        return Audit.Count - 1;
    }

    public static void AuditMenu(Rectangle r) => Record(r, "menu", AuditKind.Menu);

    // A modal overlay hides everything drawn before it.
    public static void BeginOverlay(Rectangle box)
    {
        Audit?.Clear();
        Record(box, "overlay", AuditKind.Menu);
    }

    public static List<string> AuditProblems()
    {
        var problems = new List<string>();
        if (Audit == null) return problems;
        var all = Audit;
        var texts = all.Where(e => e.Kind is AuditKind.Text or AuditKind.Label).ToList();
        var menu = all.LastOrDefault(e => e.Kind == AuditKind.Menu);

        // Glyph boxes from MeasureString include line spacing; trim before testing.
        static Rectangle Ink(Rectangle r) => new(r.X + 2, r.Y + 4, Math.Max(1, r.Width - 4), Math.Max(1, r.Height - 8));

        foreach (var t in texts)
        {
            var ink = Ink(t.Rect);
            if (menu.Kind == AuditKind.Menu && menu.Rect.Width > 0 && !Inset(menu.Rect, 8).Contains(ink))
                problems.Add($"'{t.What}' leaves the menu frame");
            // Labels only need to fit across: the font's line height is taller
            // than its letters, so a vertical check flags every button.
            if (t.Kind == AuditKind.Label && t.Owner >= 0 && (ink.Left < all[t.Owner].Rect.Left + 8 || ink.Right > all[t.Owner].Rect.Right - 8))
                problems.Add($"'{t.What}' doesn't fit its button");
            // A panel's frame is about 12px thick: text inside must clear it.
            foreach (var p in all.Where(e => e.Kind == AuditKind.Panel))
                if (ink.Intersects(p.Rect) && !Inset(p.Rect, 10).Contains(ink))
                    problems.Add($"'{t.What}' crosses the edge of a panel");
            foreach (var btn in all.Select((e, i) => (e, i)).Where(x => x.e.Kind == AuditKind.Button && x.i != t.Owner))
                if (ink.Intersects(btn.e.Rect))
                    problems.Add($"'{t.What}' overlaps the '{btn.e.What}' button");
        }
        for (int i = 0; i < texts.Count; i++)
        for (int j = i + 1; j < texts.Count; j++)
            if (Ink(texts[i].Rect).Intersects(Ink(texts[j].Rect)))
                problems.Add($"'{texts[i].What}' overlaps '{texts[j].What}'");
        return problems.Distinct().ToList();
    }

    private static Rectangle Inset(Rectangle r, int by) => new(r.X + by, r.Y + by, r.Width - 2 * by, r.Height - 2 * by);

    private static Rectangle Measure(string text, Vector2 pos, float scale)
    {
        var size = Game1.smallFont.MeasureString(text) * scale;
        return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
    }

    public static void Outline(SpriteBatch b, Rectangle r, Color c, int t)
    {
        Fill(b, new Rectangle(r.X, r.Y, r.Width, t), c);
        Fill(b, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
        Fill(b, new Rectangle(r.X, r.Y, t, r.Height), c);
        Fill(b, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
    }

    public static void Paper(SpriteBatch b, Rectangle r)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, PaperBox, r.X, r.Y, r.Width, r.Height, Color.White, 4f, false);
        Record(r, "paper", AuditKind.Panel);
    }

    public static void Frame(SpriteBatch b, Rectangle r) =>
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);

    public static void Button(SpriteBatch b, Rectangle r, string label, bool enabled = true, bool selected = false, bool hovered = false)
    {
        var tint = !enabled ? Color.Gray : selected ? new Color(255, 230, 160) : hovered ? Color.Wheat : Color.White;
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, ButtonBox, r.X, r.Y, r.Width, r.Height, tint, 4f, false);
        var size = Game1.smallFont.MeasureString(label);
        var pos = new Vector2(r.Center.X - size.X / 2, r.Center.Y - size.Y / 2 + 2);
        Utility.drawTextWithShadow(b, label, Game1.smallFont, pos, enabled ? Game1.textColor : Game1.textColor * 0.5f);
        int owner = Record(r, label, AuditKind.Button);
        Record(Measure(label, pos, 1f), label, AuditKind.Label, owner);
    }

    public static void Text(SpriteBatch b, string text, Vector2 pos, Color? color = null, float scale = 1f)
    {
        Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, color ?? Game1.textColor, scale);
        Record(Measure(text, pos, scale), text.Replace('\n', ' '), AuditKind.Text);
    }

    public static void TextCentered(SpriteBatch b, string text, int centerX, int y, Color? color = null, float scale = 1f)
    {
        float w = Game1.smallFont.MeasureString(text).X * scale;
        Text(b, text, new Vector2(centerX - w / 2, y), color, scale);
    }

    // A little landscape (sky, hill, sun): the icon for a view.
    public static void Landscape(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(150, 196, 230));
        Fill(b, new Rectangle(r.X, r.Y + r.Height * 3 / 5, r.Width, r.Height - r.Height * 3 / 5), new Color(96, 150, 70));
        Fill(b, new Rectangle(r.X + r.Width / 5, r.Y + r.Height / 2, r.Width * 2 / 5, r.Height / 10), new Color(70, 120, 60));
        Fill(b, new Rectangle(r.Right - r.Width / 4, r.Y + r.Height / 8, r.Width / 8, r.Width / 8), new Color(250, 220, 110));
    }

        public static float TextWidth(string text) => Game1.smallFont.MeasureString(text).X;

    // A row of five stars filled to `stars` (half steps), each `size` px.
    public static void Stars(SpriteBatch b, float stars, int x, int y, int size, int gap)
    {
        for (int i = 0; i < 5; i++)
        {
            var dest = new Rectangle(x + i * (size + gap), y, size, size);
            b.Draw(Game1.mouseCursors, dest, GoldStar, Color.Black * 0.25f);
            float fill = Math.Clamp(stars - i, 0f, 1f);
            if (fill <= 0f) continue;
            var src = new Rectangle(GoldStar.X, GoldStar.Y, fill >= 1f ? GoldStar.Width : GoldStar.Width / 2, GoldStar.Height);
            b.Draw(Game1.mouseCursors, new Rectangle(dest.X, dest.Y, fill >= 1f ? size : size / 2, size), src, Color.White);
        }
    }

    // The vanilla quality star for a tier (none for normal quality).
    public static Rectangle? QualityStar(int quality) => quality switch
    {
        1 => new Rectangle(338, 400, 8, 8),
        2 => new Rectangle(346, 400, 8, 8),
        4 => new Rectangle(346, 392, 8, 8),
        _ => null,
    };

    public static string QualityName(int quality) => quality switch
    {
        1 => I18n.Get("quality.silver"),
        2 => I18n.Get("quality.gold"),
        4 => I18n.Get("quality.iridium"),
        _ => I18n.Get("quality.normal"),
    };

    public static Color QualityColor(int quality) => quality switch
    {
        1 => new Color(90, 100, 120),
        2 => new Color(150, 100, 0),
        4 => new Color(120, 60, 180),
        _ => Game1.textColor,
    };
}
