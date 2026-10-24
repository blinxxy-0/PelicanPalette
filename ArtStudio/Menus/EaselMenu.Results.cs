using System;
using System.Linq;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Menus;

// Results: the finished piece beside its reference, the judge's stars with
// the reasons behind them, the session's extras (purity, sparks, light) and
// what all of that made the painting worth.
internal sealed partial class EaselMenu
{
    private PaintingResult? _result;
    private float _purityPct;
    private bool _allPure, _inspired, _lightGone;
    private float _score;
    private int _quality, _xp, _newLevel;
    private string _critique = "";
    private int _critiquePortrait = -1;      // Sable's portrait frame when it's their critique
    private SObject? _painting;
    private bool _given;
    private Texture2D? _resultTexture;
    private float _revealTime;
    private Rectangle _keepButton;

    private void Finish(bool lightGone)
    {
        _mix = null;
        _lightGone = lightGone;
        _stage = EaselStage.Results;
        _revealTime = 0f;

        var cells = _canvas.Snapshot();
        var purity = _canvas.PuritySnapshot();
        int painted = 0;
        float purityCredit = 0f;
        foreach (var (c, p) in cells.Cast<byte>().Zip(purity.Cast<Purity>()))
        {
            if (c == PixelCanvas.Blank) continue;
            painted++;
            purityCredit += PaintingJudge.PurityCredit(p);
        }
        _purityPct = painted > 0 ? purityCredit / painted : 0f;
        _allPure = painted > 0 && Math.Abs(_purityPct - 1f) < 0.0001f;
        _inspired = _sparksCaught >= PaintingRules.SparksForInspired;

        int size = _canvas.Size;
        if (painted == 0)
        {
            Game1.playSound("fishEscape");
            _critique = "The light went before you'd painted a thing.";
            if (SableCritiques)
                (_critique, _critiquePortrait) = ("You ran out of light before you started. Everyone does once. Start smaller.", 2);
            LayoutResults();
            return;
        }

        if (_reference != null)
        {
            _result = PaintingJudge.Judge(cells, purity, _reference);
            float inspiredBonus = Has(PaintingSkill.Instance.Visionary) ? PaintingSkill.VisionaryInspiredBonus : PaintingRules.InspiredBonus;
            _score = PaintingRules.Score(_result.Stars, _level, _inspired, _allPure, inspiredBonus);
            _quality = PaintingRules.Quality(_score, _level);
            _xp = (int)Math.Round(PaintingRules.Xp(_result.Stars, _reference.Difficulty, size) * _rules.RewardMultiplier);
            if (SableCritiques)
                (_critique, _critiquePortrait) = SableCritique(_result, painted);
            else
                _critique = Critique(_result, painted);
        }
        else
        {
            _quality = PaintingRules.Normal;
            _xp = PaintingRules.OriginalXp;
            if (SableCritiques)
                (_critique, _critiquePortrait) = ("An original. Don't let anyone price it for you. Including me.", 1);
            else
                _critique = "An original. It's yours, whatever anyone says.";
        }

        int subjectPrice = _subjectItems.Length == 0 ? 0 : _subjectItems.Max(id => ItemRegistry.Create(id).salePrice());
        int price = _reference != null
            ? (int)Math.Round(PaintingRules.Value(size, _reference.Difficulty, subjectPrice, _level) * _rules.RewardMultiplier)
            : PaintingRules.OriginalValue(size);
        if (Has(PaintingSkill.Instance.Gallerist))
            price = (int)Math.Round(price * PaintingSkill.GalleristPrice);

        var data = new PaintingData
        {
            Size = size,
            Cells = cells,
            Purity = purity,
            Subject = _subjectItems.Length == 0 ? PaintingData.OriginalSubject : string.Join("|", _subjectItems),
            Stars = _result?.Stars ?? 0f,
            Difficulty = _reference?.Difficulty ?? 0,
            Painter = _who.Name,
            Date = $"{Utility.getSeasonNameFromNumber(Game1.seasonIndex)} {Game1.dayOfMonth}, Year {Game1.year}",
        };
        _painting = PaintingItems.Create(data, _quality, price);
        _resultTexture = PaintingRenderer.Build(size, cells, purity, CanvasColor);

        _newLevel = ArtProgress.AddXp(_who, _xp);
        if (_brushItem != null)
            ArtHooks.WearBrush(_who, _brushItem, _brush);
        Game1.playSound(lightGone ? "dwop" : "achievement");
        LayoutResults();
    }

    // The client-style comment from Muse, plus the minigame's new failure
    // modes: running out of light, and muddy mixing.
    private string Critique(PaintingResult r, int painted)
    {
        string subject = SubjectPhrase(_subjectItems);
        if (_lightGone && painted < _reference!.SubjectCells * 0.6f)
            return "The light went before you could finish it.";
        if (r.Stars >= 4.5f)
            return _purityPct < 0.9f ? "Lovely work. A cleaner mix and it would sing." : "It looks just like the real thing.";
        if (r.Stars <= 1f)
            return $"Hmm... is this meant to be {subject}?";
        if (_purityPct < 0.7f)
            return "The colors have gone muddy. Take more care at the palette.";
        if (r.Neatness < 0.7f && r.Neatness < r.Shape && r.Neatness < r.Color)
            return "It's nice, but there are stray marks all around it.";
        if (r.Shape < r.Color)
            return $"The colors are right, but the shape isn't quite {subject} yet.";
        if (r.Stars >= 3.5f)
            return "Very nice! A few colors aren't quite right, but it's charming.";
        return "That's the right shape, but the colors aren't there yet.";
    }

    // From 2 hearts, Sable (the art shop's owner) writes the critique.
    private bool SableCritiques => _who.getFriendshipHeartLevelForNPC("Sable") >= 2;

    // The same reading as Critique, in Sable's voice, with the portrait to show.
    private (string, int) SableCritique(PaintingResult r, int painted)
    {
        string subject = SubjectPhrase(_subjectItems);
        if (_lightGone && painted < _reference!.SubjectCells * 0.6f)
            return ("You ran out of light. Everyone does. Paint the big shapes first.", 2);
        if (r.Stars >= 4.5f)
            return _purityPct < 0.9f
                ? ("Good bones. The mixing's muddy at the edges. Fix that and I'd hang it.", 0)
                : ("...I'd hang that. I don't say that.", 1);
        if (r.Stars <= 1f)
            return ($"Is that {subject}? Don't answer. Try again.", 6);
        if (_purityPct < 0.7f)
            return ("Your colors went muddy. Mud is just a color that got distracted.", 3);
        if (r.Neatness < 0.7f && r.Neatness < r.Shape && r.Neatness < r.Color)
            return ("Stray marks. Every one you leave, the eye trips over.", 5);
        if (r.Shape < r.Color)
            return ($"Right colors, wrong shape. Look at {subject} longer than you paint it.", 0);
        if (r.Stars >= 3.5f)
            return ("Close. A few of those colors are guesses. Stop guessing.", 3);
        return ("The shape's there. Now earn the colors.", 0);
    }

    private void LayoutResults()
    {
        _keepButton = new Rectangle(xPositionOnScreen + width - Pad - 220, yPositionOnScreen + height - Pad - 88, 200, 64);
    }

    private void UpdateResults(float dt) => _revealTime += dt;

    private void ClickResults(int x, int y)
    {
        if (_keepButton.Contains(x, y))
            KeepAndClose();
    }

    private void KeepAndClose()
    {
        exitThisMenu();
        GiveResultIfPending();
    }

    private void GiveResultIfPending()
    {
        if (_given || _painting == null) return;
        _given = true;
        if (Game1.activeClickableMenu == null)
            Game1.player.addItemByMenuIfNecessary(_painting);
        else if (!Game1.player.addItemToInventoryBool(_painting))
            Game1.createItemDebris(_painting, Game1.player.getStandingPosition(), Game1.player.FacingDirection);
        if (_newLevel > 0)
            Game1.addHUDMessage(new HUDMessage($"Painting level {_newLevel}!") { noIcon = true });
        _resultTexture?.Dispose();
    }

    private void DrawResults(SpriteBatch b)
    {
        int x = xPositionOnScreen + Pad + 40, y = yPositionOnScreen + Pad + 20;
        int artSide = Math.Min(height - 2 * Pad - 140, (width - 2 * Pad) / 2 - 60);
        var art = new Rectangle(x, y + 64, artSide, artSide);

        Ui.Text(b, _painting?.DisplayName ?? "No painting", new Vector2(x - 16, y + 8));

        var frame = new Rectangle(art.X - 16, art.Y - 16, art.Width + 32, art.Height + 32);
        Ui.Fill(b, frame, new Color(110, 70, 40));
        Ui.Outline(b, frame, new Color(60, 35, 20), 4);
        if (_resultTexture != null)
            b.Draw(_resultTexture, art, Color.White);
        else
            Ui.Fill(b, art, CanvasColor);

        int cx = art.Right + 72;
        int cy = y + 64;

        // The reference, small, for comparison.
        if (_reference != null)
        {
            int refSide = 96;
            var refArea = new Rectangle(xPositionOnScreen + width - Pad - 40 - refSide, cy - 8, refSide, refSide);
            Ui.Paper(b, new Rectangle(refArea.X - 12, refArea.Y - 12, refArea.Width + 24, refArea.Height + 24));
            DrawCells(b, _reference.Cells, null, refArea, CanvasColor, false);
        }
        float reveal = Math.Clamp(_revealTime / 0.8f, 0f, 1f);

        if (_result != null)
        {
            Ui.Stars(b, _result.Stars * reveal, cx, cy, 40, 8);
            cy += 64;
            Criterion(b, "Shape", PaintingJudge.Rating(_result.Shape), cx, ref cy);
            Criterion(b, "Color", PaintingJudge.Rating(_result.Color), cx, ref cy);
            Criterion(b, "Neatness", PaintingJudge.Rating(_result.Neatness), cx, ref cy);
            Criterion(b, "Mixing", $"{_purityPct * 100f:0}% pure" + (_allPure ? " (flawless)" : ""), cx, ref cy);
            Criterion(b, "Sparks", _inspired ? $"{_sparksCaught} (Inspired!)" : _sparksCaught.ToString(), cx, ref cy);
            if (_timed)
                Criterion(b, "Light", _lightGone ? "ran out" : $"{_lightLeft:0}s to spare", cx, ref cy);
            cy += 8;
        }

        if (_painting != null)
        {
            var star = Ui.QualityStar(_quality);
            if (star.HasValue)
                b.Draw(Game1.mouseCursors, new Rectangle(cx, cy, 40, 40), star.Value, Color.White);
            string qualityText = _reference == null ? "Original" : $"{Ui.QualityName(_quality)} quality";
            Ui.Text(b, qualityText, new Vector2(cx + (star.HasValue ? 52 : 0), cy + 6), Ui.QualityColor(_quality));
            if (_reference != null && PaintingRules.Quality(_score, PaintingRules.MaxLevel) > _quality)
                Ui.Text(b, "(your skill caps it here for now)", new Vector2(cx, cy + 40), Game1.textColor * 0.6f);
            cy += 76;
            Ui.Text(b, $"Sells for {_painting.sellToStorePrice()}g", new Vector2(cx, cy));
            cy += 34;
            Ui.Text(b, $"+{_xp} Painting XP" + (_newLevel > 0 ? $"  (Level {_newLevel}!)" : ""), new Vector2(cx, cy), new Color(60, 140, 60));
            cy += 50;
        }

        int textX = cx;
        if (_critiquePortrait >= 0)
        {
            // Sable's face and name beside their quote.
            var portrait = Game1.content.Load<Texture2D>("Portraits/Sable");
            var face = new Rectangle(cx, cy, 56, 56);
            b.Draw(portrait, face, Game1.getSourceRectForStandardTileSheet(portrait, _critiquePortrait, 64, 64), Color.White);
            Ui.Outline(b, face, new Color(60, 35, 20), 2);
            Ui.Text(b, "Sable", new Vector2(face.X + 2, face.Bottom + 4), Game1.textColor * 0.7f, 0.8f);
            textX = face.Right + 14;
        }
        var lines = Game1.parseText($"\"{_critique}\"", Game1.smallFont, _keepButton.X - 24 - textX);
        Ui.Text(b, lines, new Vector2(textX, cy), Game1.textColor * 0.85f);

        Ui.Button(b, _keepButton, _painting != null ? "Keep it" : "Close", true, false, _keepButton.Contains(MousePos));
    }

    private static void Criterion(SpriteBatch b, string label, string value, int x, ref int y)
    {
        Ui.Text(b, label, new Vector2(x, y), Game1.textColor * 0.7f);
        Ui.Text(b, value, new Vector2(x + 140, y));
        y += 34;
    }
}
