#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ArtStudio.Pottery;

// Firing: stoking the kiln (docs/design/pottery_minigame.md, section 3.5). The
// glazed pot goes in, and the firing climbs to its glaze's peak over half a
// minute. You feed the fire to keep the heat inside the band as it climbs:
// the kiln answers slowly (it's a lot of brick), so you feed it ahead of the
// climb, not when it's already cold. Each fuel burns its own way:
//   Wood      a quick, gentle lift
//   Hardwood  long and steady (why Tobin loves it)
//   Coal      hot and fast: easy to overdo
// Time in the band sets the glaze's result. Overfiring cracks a pot only in Hard.
internal enum Fuel { Wood, Hardwood, Coal }

internal sealed record FuelKind(string Name, float Heat, float BurnTime, string Hint);

internal sealed class Kiln
{
    public const float Duration = 30f, Climb = 24f;          // seconds: the whole firing, and the climb to the peak
    public const float Ambient = 10f, StartHeat = 20f, MaxHeat = 110f;
    public const float Loss = 0.05f;                          // the kiln's heat leaking away, a fraction a second
    private const float Grace = 2f;                            // seconds before the band counts: you're lighting it

    public static readonly FuelKind[] Fuels =
    {
        new("Wood", 8f, 3f, "A quick, gentle lift."),
        new("Hardwood", 15f, 9f, "Long and steady."),
        new("Coal", 13f, 1.5f, "Hot and fast: easy to overdo."),
    };

    // How hot each glaze wants to go (on the gauge's 0 to 100): the reds and the
    // celadon want the hottest firings.
    public static float PeakFor(int glaze) => glaze < 0 ? 72f : Glazes.All[glaze].Name switch
    {
        "Clear" => 70f,
        "Ash" => 76f,
        "Tenmoku" => 80f,
        "Cobalt" => 82f,
        "Celadon" => 86f,
        "Copper red" => 88f,
        _ => 78f,
    };

    public float Time { get; private set; }
    public float Heat { get; private set; } = StartHeat;
    public float Peak { get; }
    public float Band { get; }
    public float GiftOdds { get; }                            // against the usual chance (the Pottery skill's Kiln Keeper)
    public bool Cracked { get; private set; }
    public bool Done => Time >= Duration || Cracked;
    public readonly int[] Used = new int[Fuels.Length];
    public readonly List<Vector2> Trace = new();              // (time, heat), four a second
    public float Below { get; private set; }
    public float Above { get; private set; }

    private readonly bool _canCrack;
    private readonly List<(float Rate, float Left)> _burning = new();
    private float _scoreSum, _over, _nextSample;
    private int _samples;

    private readonly Random _random;
    private readonly float _gusts;                             // draughts: how hard they push the heat about
    private float _draught;

    public Kiln(int glaze, PotteryRules rules, Random random)
    {
        Peak = PeakFor(glaze);
        Band = rules.KilnBand;
        GiftOdds = rules.GiftOdds;
        _canCrack = rules.KilnCrack;
        _random = random;
        _gusts = rules.Name switch { "Simplified" => 0f, "Hard" => 7f, _ => 3.5f };
        Trace.Add(new Vector2(0, Heat));
    }

    // The middle of the band at a time, as a potter's firing schedule: climb, hold a
    // while to soak (so the clay heats through), climb again to the peak, hold there.
    // The holds are what stop one steady rhythm of fuel from following it.
    public const float SoakFrom = 9f, SoakTo = 14f;
    public float Target(float t)
    {
        static float Ease(float u) { u = Math.Clamp(u, 0f, 1f); return u * u * (3f - 2f * u); }
        float soak = StartHeat + (Peak - StartHeat) * 0.5f;
        return t < SoakFrom ? StartHeat + (soak - StartHeat) * Ease(t / SoakFrom)
             : t < SoakTo ? soak
             : soak + (Peak - soak) * Ease((t - SoakTo) / (Climb - SoakTo));
    }

    public float Power => _burning.Sum(b => b.Rate);
    public int Burning => _burning.Count;
    // Where the heat will be in so many seconds if nothing more goes in.
    public float Forecast(float seconds)
    {
        float heat = Heat;
        var burning = _burning.ToList();
        for (float t = 0; t < seconds; t += 0.1f)
        {
            float power = 0f;
            for (int i = 0; i < burning.Count; i++)
                if (burning[i].Left > t)
                    power += burning[i].Rate;
            heat += (power - Loss * (heat - Ambient)) * 0.1f;
        }
        return heat;
    }

    public void Add(Fuel fuel)
    {
        if (Done)
            return;
        var kind = Fuels[(int)fuel];
        _burning.Add((kind.Heat / kind.BurnTime, kind.BurnTime));
        Used[(int)fuel]++;
    }

    public void Step(float dt)
    {
        if (Done)
            return;
        Time = MathF.Min(Duration, Time + dt);
        float power = Power;
        for (int i = _burning.Count - 1; i >= 0; i--)
        {
            var (rate, left) = _burning[i];
            if (left - dt <= 0f)
                _burning.RemoveAt(i);
            else
                _burning[i] = (rate, left - dt);
        }
        // draughts through the kiln: a wandering push, now cooling, now fanning it
        _draught += (-_draught * 0.6f + (float)(_random.NextDouble() * 2 - 1) * _gusts * 2.5f) * dt;
        Heat = Math.Clamp(Heat + (power - Loss * (Heat - Ambient) + _draught) * dt, Ambient, MaxHeat);

        if (Time > Grace)
        {
            float d = Heat - Target(Time);
            _scoreSum += Math.Clamp(1f - MathF.Max(0f, MathF.Abs(d) - Band) / Band, 0f, 1f);
            _samples++;
            if (d < -Band) Below += dt;
            if (d > Band) Above += dt;
            // Hard: far over the band for long, and the pot cracks
            _over = d > Band + 6f ? _over + dt : MathF.Max(0f, _over - dt);
            if (_canCrack && _over > 1.5f)
                Cracked = true;
        }
        if (Time >= _nextSample)
        {
            Trace.Add(new Vector2(Time, Heat));
            _nextSample += 0.25f;
        }
    }

    public float Score => Cracked ? 0f : _samples > 0 ? _scoreSum / _samples : 0f;
}

internal sealed record FiringResult(float Score, string Verdict, string? Gift);

internal static class FiringJudge
{
    private static readonly string[] Gifts =
    {
        "The glaze crackled like river ice.",
        "The glaze ran into a fine drip at the foot.",
        "A flash of colour where the flame licked it.",
        "The glaze pooled deep and glassy in the rings.",
    };

    public static FiringResult Judge(Kiln kiln, Random random)
    {
        float s = kiln.Score;
        string verdict = kiln.Cracked ? "Cracked" : s >= 0.9f ? "Perfect" : s >= 0.65f ? "Well fired"
            : kiln.Below >= kiln.Above ? "Underfired" : "Overfired";
        // a kiln gift: likely from a perfect firing, rare from a good one
        double chance = Math.Min(1.0, (verdict == "Perfect" ? 0.5 : verdict == "Well fired" ? 0.1 : 0) * kiln.GiftOdds);
        string? gift = random.NextDouble() < chance ? Gifts[random.Next(Gifts.Length)] : null;
        return new FiringResult(s, verdict, gift);
    }

    public static string Tip(Kiln kiln, FiringResult r) => r.Verdict switch
    {
        "Cracked" => "It cracked: the kiln ran far too hot for too long.",
        "Underfired" => "It's underfired, so the glaze is dull. Feed the fire before the heat drops: the kiln is slow to answer.",
        "Overfired" => "It's overfired, so the glaze went dull. Less at once; coal burns hot.",
        "Perfect" => r.Gift ?? "A true firing.",
        _ => r.Gift ?? (kiln.Below > kiln.Above
            ? "It ran cool at times. Hardwood holds the heat steady."
            : "It ran hot at times. Let it settle before you feed it again."),
    };
}
#endif
