using System;

namespace ArtStudio.Core;

public enum MixOutcome { Mixing, Pure, Off, Muddy }

public readonly record struct MixSettings(float Difficulty, MixMotion Motion, float BandHeight, float StartMeter, float TimeLimit);

// Loading the brush with a pigment: the fishing bar, reskinned. The pigment's
// "true hue" marker wanders up and down the track; holding the button lifts
// the player's mix band and letting go lets it fall. Keeping the marker
// inside the band fills the mix meter.
//
// The marker and band physics are the fishing minigame's own (BobberBar
// update, per 60fps tick, same 568-unit track), so the feel carries over
// exactly: a level-N painter's band is as tall as a level-N angler's bar.
// What's different is the stakes: a mix lasts a few seconds, and running
// out of time or letting the meter empty doesn't lose the fish — it gives
// you worse paint.
public sealed class MixBar
{
    public const float Track = 568f;
    private const float MarkerHalf = 14f;

    // Meter rates per tick. A clean mix from the starting meter takes about
    // 1.5 seconds; drifting off empties it in under two.
    private const float FillRate = 0.0072f;
    private const float DrainRate = 0.0035f;
    private const float OffThreshold = 0.5f;

    private readonly MixSettings _settings;
    private readonly Random _random;

    private float _markerTarget = -1f;
    private float _markerSpeed;
    private float _floatSink;

    public float Marker { get; private set; }
    public float BandPos { get; private set; }
    public float BandSpeed { get; private set; }
    public float BandHeight => _settings.BandHeight;
    public float Meter { get; private set; }
    public float Elapsed { get; private set; }
    public float TimeLimit => _settings.TimeLimit;
    public bool InBand { get; private set; }
    public MixOutcome Outcome { get; private set; } = MixOutcome.Mixing;

    public MixBar(MixSettings settings, Random random)
    {
        _settings = settings;
        _random = random;
        Meter = settings.StartMeter;
        BandPos = Track - settings.BandHeight;
        Marker = Track * (0.35f + 0.3f * (float)random.NextDouble());
    }

    // One 60fps tick. held = the mix button is down.
    public void Tick(bool held)
    {
        if (Outcome != MixOutcome.Mixing) return;

        MoveMarker();
        MoveBand(held);

        // Marker is the droplet's center; it counts once its middle is inside the band.
        InBand = Marker >= BandPos && Marker <= BandPos + BandHeight;
        Meter = Math.Clamp(Meter + (InBand ? FillRate : -DrainRate), 0f, 1f);
        Elapsed += 1f / 60f;

        if (Meter >= 1f)
            Outcome = MixOutcome.Pure;
        else if (Meter <= 0f)
            Outcome = MixOutcome.Muddy;
        else if (Elapsed >= TimeLimit)
            Outcome = Meter >= OffThreshold ? MixOutcome.Off : MixOutcome.Muddy;
    }

    private void MoveMarker()
    {
        float difficulty = _settings.Difficulty;
        var motion = _settings.Motion;
        bool smooth = motion == MixMotion.Smooth;

        if (_random.NextDouble() < difficulty * (smooth ? 20 : 1) / 4000f && (!smooth || _markerTarget == -1f))
        {
            float down = Track - Marker, up = Marker;
            float reach = Math.Min(99f, difficulty + _random.Next(10, 45)) / 100f;
            _markerTarget = Marker + _random.Next((int)Math.Min(-up, down), (int)down) * reach;
        }

        if (motion == MixMotion.Floater)
            _floatSink = Math.Max(_floatSink - 0.01f, -1.5f);
        else if (motion == MixMotion.Sinker)
            _floatSink = Math.Min(_floatSink + 0.01f, 1.5f);

        if (Math.Abs(Marker - _markerTarget) > 3f && _markerTarget != -1f)
        {
            float accel = (_markerTarget - Marker) / (_random.Next(10, 30) + (100f - Math.Min(100f, difficulty)));
            _markerSpeed += (accel - _markerSpeed) / 5f;
        }
        else if (!smooth && _random.NextDouble() < difficulty / 2000f)
            _markerTarget = Marker + (_random.Next(2) == 0 ? _random.Next(-100, -51) : _random.Next(50, 101));
        else
            _markerTarget = -1f;

        if (motion == MixMotion.Dart && _random.NextDouble() < difficulty / 1000f)
            _markerTarget = Marker + (_random.Next(2) == 0
                ? _random.Next(-100 - (int)difficulty * 2, -51)
                : _random.Next(50, 101 + (int)difficulty * 2));

        _markerTarget = Math.Clamp(_markerTarget, -1f, Track);
        Marker = Math.Clamp(Marker + _markerSpeed + _floatSink, MarkerHalf, Track - MarkerHalf);
    }

    private void MoveBand(bool held)
    {
        float max = Track - BandHeight;
        float gravity = held ? -0.25f : 0.25f;
        if (held && (BandPos == 0f || BandPos == max))
            BandSpeed = 0f;
        if (InBand)
            gravity *= 0.6f;

        BandSpeed += gravity;
        BandPos += BandSpeed;
        if (BandPos > max)
        {
            BandPos = max;
            BandSpeed = -BandSpeed * 2f / 3f;
        }
        else if (BandPos < 0f)
        {
            BandPos = 0f;
            BandSpeed = -BandSpeed * 2f / 3f;
        }
    }

    public static Purity ToPurity(MixOutcome outcome) => outcome switch
    {
        MixOutcome.Pure => Purity.Pure,
        MixOutcome.Off => Purity.Off,
        _ => Purity.Muddy,
    };
}
