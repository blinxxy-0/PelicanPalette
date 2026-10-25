using System;
using ArtStudio.Core;
using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace ArtStudio.Menus;

// Mixing: picking a pigment that isn't already on the brush opens the mix
// bar over the palette. It's the fishing bar (see Core/MixBar): hold to lift
// your band, keep the pigment's hue marker inside it until the meter fills.
// The light keeps draining while you mix.
internal sealed partial class EaselMenu
{
    private MixBar? _mix;
    private int _mixPigment;
    private bool _mixTopUp;

    // The mix button has to be let go once after the click that opened the
    // bar, so that click doesn't count as the first lift.
    private bool _mixArmed;
    private bool _mixWasHeld;

    private Rectangle _mixTrack, _mixMeter;

    private void LayoutMix()
    {
        int trackH = _rightCol.Height - 140;
        _mixTrack = new Rectangle(_rightCol.X + 60, _rightCol.Y + 56, 64, trackH);
        _mixMeter = new Rectangle(_mixTrack.Right + 24, _mixTrack.Y, 20, trackH);
    }

    private void PickPigment(int pigment)
    {
        // When paint doesn't run out, a color is mixed once and then just
        // picked; picking the current color again remixes it if it isn't pure.
        if (!_rules.PaintLoads)
        {
            if (_mixed.TryGetValue(pigment, out var purity) && !(pigment == _selected && purity != Purity.Pure))
            {
                _selected = pigment;
                _loadPurity = purity;
                _load = _loadMax = int.MaxValue / 2;
                _dryWarned = false;
                Game1.playSound("smallSelect");
            }
            else
                StartMix(pigment);
            return;
        }

        // Already loaded with plenty of pure paint: just keep painting with it.
        // Otherwise picking it again tops the brush up (a shorter mix).
        if (pigment == _selected && _loadPurity == Purity.Pure && _load * 2 > _loadMax)
        {
            Game1.playSound("smallSelect");
            return;
        }
        StartMix(pigment);
    }

    private void StartMix(int pigment)
    {
        if (PaintLeft(pigment) <= 0)
        {
            Game1.playSound("cancel");
            Toast(I18n.Get("paint.out", new { pigment = I18n.Pigment(pigment) }), Color.OrangeRed, 2.5f);
            return;
        }
        var p = Palette.All[pigment];
        _mixTopUp = _rules.PaintLoads && pigment == _selected && _load > 0;
        _mixPigment = pigment;
        _mixArmed = false;
        _mixWasHeld = false;
        _stroking = false;
        _rulerStart = null;

        float difficulty = p.Difficulty * (1f - _brush.Steadiness);
        var settings = new MixSettings(
            difficulty,
            p.Motion,
            PaintingRules.BandHeight(_level, _brush.BandBonus + _rules.BandBonus),
            PaintingRules.MixStartMeter(_mixTopUp),
            PaintingRules.MixTimeLimit(_mixTopUp) + _rules.MixTimeBonus);
        _mix = new MixBar(settings, _random);
        Game1.playSound("slosh");
    }

    private bool MixHeld() =>
        LeftDown
        || _keys.IsKeyDown(Keys.Space)
        || Game1.isOneOfTheseKeysDown(_keys, Game1.options.useToolButton)
        || _pad.IsButtonDown(Buttons.A)
        || _pad.IsButtonDown(Buttons.X);

    private void UpdateMix()
    {
        bool held = MixHeld();
        if (!_mixArmed)
        {
            if (!held) _mixArmed = true;
            held = false;
        }
        if (held && !_mixWasHeld)
            Game1.playSound("fishingRodBend");
        _mixWasHeld = held;

        if (RightPressed)
        {
            CancelMix();
            return;
        }

        _mix!.Tick(held);
        if (_mix.Outcome != MixOutcome.Mixing)
            CompleteMix(_mix.Outcome);
    }

    private void CompleteMix(MixOutcome outcome)
    {
        var purity = MixBar.ToPurity(outcome);
        if (purity == Purity.Muddy && Has(PaintingSkill.Instance.SteadyHand))
            purity = Purity.Off;
        if (_rules.PaintLoads)
        {
            // Topping up adds to what's left; the brush takes on the new mix's purity.
            int fresh = PaintingRules.LoadSize(_level, _brush.Load, purity);
            _load = _mixTopUp ? _load + fresh : fresh;
            _loadMax = Math.Max(_load, PaintingRules.LoadSize(_level, _brush.Load, Purity.Pure));
        }
        else
        {
            // Remixing a color that's still wet never makes it worse; either
            // way it's freshly wet again.
            if (_mixed.TryGetValue(_mixPigment, out var before) && before < purity)
                purity = before;
            _mixed[_mixPigment] = purity;
            if (!float.IsPositiveInfinity(_rules.DryTime))
                _wet[_mixPigment] = _rules.DryTime;
            _load = _loadMax = int.MaxValue / 2;
        }
        _selected = _mixPigment;
        _loadPurity = purity;
        _dryWarned = false;
        _mix = null;

        _mixesTotal++;
        if (purity == Purity.Pure) _mixesPure++;

        switch (purity)
        {
            case Purity.Pure:
                Game1.playSound("jingle1");
                Toast(I18n.Get("mix.pure", new { pigment = I18n.Pigment(_selected) }), new Color(60, 140, 60), 1.2f);
                break;
            case Purity.Off:
                Game1.playSound("dwop");
                Toast(I18n.Get("mix.off"), new Color(190, 130, 30), 1.4f);
                break;
            default:
                Game1.playSound("slimeHit");
                Toast(I18n.Get("mix.muddy"), new Color(150, 70, 50), 1.6f);
                break;
        }
    }

    private void CancelMix()
    {
        _mix = null;
        Game1.playSound("bigDeSelect");
    }

    private void DrawMix(SpriteBatch b)
    {
        var mix = _mix!;
        var p = Palette.All[_mixPigment];
        var color = new Color(p.R, p.G, p.B);

        Ui.Paper(b, new Rectangle(_rightCol.X - 8, _rightCol.Y - 8, _rightCol.Width + 16, _rightCol.Height + 16));
        Ui.TextCentered(b, I18n.Get(_mixTopUp ? "mix.topping-up" : "mix.mixing", new { pigment = I18n.Pigment(_mixPigment) }), _rightCol.Center.X, _rightCol.Y + 8);

        // Track.
        var track = _mixTrack;
        Ui.Fill(b, new Rectangle(track.X - 6, track.Y - 6, track.Width + 12, track.Height + 12), new Color(110, 70, 40));
        Ui.Fill(b, track, new Color(70, 55, 45));
        float scale = track.Height / MixBar.Track;

        // The player's band, tinted with the pigment and brighter while on target.
        int bandY = track.Y + (int)(mix.BandPos * scale), bandH = (int)(mix.BandHeight * scale);
        var bandRect = new Rectangle(track.X + 4, bandY, track.Width - 8, bandH);
        Ui.Fill(b, bandRect, Color.Lerp(color, Color.White, 0.45f) * (mix.InBand ? 0.95f : 0.6f));
        Ui.Outline(b, bandRect, mix.InBand ? Color.White : Color.Black * 0.4f, 3);

        // The hue marker: a dab of the pure pigment.
        int m = track.Y + (int)(mix.Marker * scale);
        var marker = new Rectangle(track.Center.X - 14, m - 14, 28, 28);
        var shake = mix.InBand ? Point.Zero : new Point(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2));
        marker.Offset(shake);
        Ui.Fill(b, marker, color);
        Ui.Outline(b, marker, Color.Black * 0.7f, 3);

        // Mix meter, red to green like the fishing catch bar.
        var meter = _mixMeter;
        Ui.Fill(b, new Rectangle(meter.X - 4, meter.Y - 4, meter.Width + 8, meter.Height + 8), new Color(110, 70, 40));
        Ui.Fill(b, meter, new Color(70, 55, 45));
        int fillH = (int)(meter.Height * mix.Meter);
        Ui.Fill(b, new Rectangle(meter.X, meter.Bottom - fillH, meter.Width, fillH), Utility.getRedToGreenLerpColor(mix.Meter));
        // The "a bit off" line: finish above it if time runs out.
        Ui.Fill(b, new Rectangle(meter.X - 4, meter.Y + meter.Height / 2, meter.Width + 8, 2), Color.White * 0.8f);

        float timeLeft = Math.Max(0f, mix.TimeLimit - mix.Elapsed);
        Ui.TextCentered(b, I18n.Get("mix.time-left", new { seconds = timeLeft.ToString("0.0") }), _rightCol.Center.X, track.Bottom + 16, timeLeft < 1.5f ? Color.OrangeRed : Game1.textColor);
        if (!_mixArmed)
            Ui.TextCentered(b, I18n.Get("mix.hold"), _rightCol.Center.X, track.Bottom + 44, Game1.textColor * 0.7f);
        else
            Ui.TextCentered(b, I18n.Get("mix.motion", new { motion = I18n.Motion(p.Motion) }), _rightCol.Center.X, track.Bottom + 44, Game1.textColor * 0.6f);
    }
}
