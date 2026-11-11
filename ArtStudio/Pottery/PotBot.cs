#if DEBUG
using System;
using System.Collections.Generic;

namespace ArtStudio.Pottery;

// Centring (docs/design/pottery_minigame.md, section 3): the lump wobbles on the wheel,
// your hands on it pull it true, and let go it wanders. Game-free, so the simulation
// (ArtStudio.Sim, `dotnet run -- pottery`) runs the same rules the menu does.
internal sealed class Centring
{
    public const float TrueWithin = 0.8f;   // units: running true
    public const float HoldTrue = 1.5f;     // seconds true and it's done
    public const float MaxTime = 14f;       // or done anyway, as true as it got
    public const float Range = 12f;

    private readonly Random _random;
    private float _v, _trueFor, _time;

    public float Wobble { get; private set; }
    public bool Done { get; private set; }
    public bool RunningTrue => MathF.Abs(Wobble) < TrueWithin;
    public float Score => Math.Clamp(1f - MathF.Abs(Wobble) / 8f, 0f, 1f);

    public Centring(Random random)
    {
        _random = random;
        Wobble = (random.Next(2) == 0 ? -1 : 1) * 6f;
    }

    // `hand`: where your hands are across the lump (units from the wheel's centre), or
    // null when you aren't holding it.
    public void Step(float dt, float? hand, float band)
    {
        if (Done)
            return;
        _time += dt;
        bool onIt = hand is float h && MathF.Abs(h - Wobble) < band;
        if (onIt)
        {
            _v += -Wobble * 4f * dt;               // your hands pull it true
            _v *= MathF.Pow(0.15f, dt);
        }
        else
            _v += (MathF.Sign(Wobble == 0 ? 1 : Wobble) * 3f + (float)(_random.NextDouble() - 0.5) * 18f) * dt;
        _v += (float)(_random.NextDouble() - 0.5) * 10f * dt;   // it never quite stops trying
        Wobble = Math.Clamp(Wobble + _v * dt, -Range, Range);
        _trueFor = RunningTrue ? _trueFor + dt : 0f;
        if (_trueFor > HoldTrue || _time > MaxTime)
            Done = true;
    }
}

// A potter's hand, for the simulation: how far off they work from where they mean to,
// how close they call "matching", how long they keep at it, and when they let the clay
// rest. `Perfect` is the old greedy bot exactly (the menu's "bot" mode).
internal sealed record PotHand(
    string Name,
    float Aim,          // rows: the spread of where a stroke lands around where it's meant
    float Height,       // rows: the spread of where they stop pulling up
    float Tolerance,    // units: a row this close to the outline is left alone
    int Sweeps,         // shaping passes up the pot before they call it done
    float RestAt,       // patience under which they stop and let it firm up
    float RestTo,       // ... and how firm they wait for
    float Overwork,     // the spread of how long each shaping stroke is held (1 = right)
    float Lag,          // centring: seconds behind the wobble their hands are
    float Wander,       // centring: units their hands wander off it
    int Rethins = 0,    // times they pull up through a wall left heavy, and shape it again
    int Reserve = 0)    // rows they leave unpulled at first, for the rethinning to raise
{
    public static readonly PotHand Perfect = new("perfect", 0f, 0f, 0.3f, 40, 0.35f, 0.9f, 0f, 0.1f, 0.3f);
    public static readonly PotHand Careful = new("careful", 0.35f, 0.6f, 0.5f, 20, 0.35f, 0.9f, 0.25f, 0.2f, 0.8f);
    public static readonly PotHand Average = new("average", 0.7f, 1.1f, 0.9f, 10, 0.25f, 0.8f, 0.5f, 0.3f, 1.5f);
    public static readonly PotHand Rushed = new("rushed", 1.1f, 1.8f, 1.4f, 5, 0.12f, 0.6f, 0.8f, 0.4f, 2.5f);
    public static readonly PotHand Restless = Rushed with { Name = "restless", RestAt = -1f };   // never lets it rest
    public static readonly PotHand[] All = { Perfect, Careful, Average, Rushed, Restless };
}

internal static class PotBot
{
    private const float Dt = 1f / 60f;

    // A standard normal draw (Box-Muller).
    public static float Gauss(Random rng) =>
        MathF.Sqrt(-2f * MathF.Log(1f - (float)rng.NextDouble())) * MathF.Cos(MathF.Tau * (float)rng.NextDouble());

    // Hands following the wobble, a little behind it and wandering. Returns seconds taken.
    public static float Centre(Centring c, PotHand hand, float band, Random rng)
    {
        var seen = new Queue<float>();
        int lagSteps = Math.Max(1, (int)(hand.Lag / Dt));
        float err = 0f, time = 0f;
        while (!c.Done)
        {
            seen.Enqueue(c.Wobble);
            float aimAt = seen.Count > lagSteps ? seen.Dequeue() : seen.Peek();
            err = err * 0.97f + Gauss(rng) * hand.Wander * 0.25f;
            c.Step(Dt, aimAt + err, band);
            time += Dt;
        }
        return time;
    }

    // The greedy potter: sweep pulls from the foot until it's tall enough, then press in or
    // let out row by row towards the outline, resting whenever the clay tires. Returns the
    // seconds taken, working and resting, and how many times it rested.
    public static (float Secs, int Rests) Throw(ClayPot pot, PotShape shape, PotHand hand, Random? rng = null)
    {
        rng ??= new Random(0);
        float time = 0f;
        int rests = 0;
        float Noise(float spread) => spread > 0f ? Gauss(rng) * spread : 0f;
        void RestIfTired()
        {
            if (pot.Patience < hand.RestAt)
                rests++;
            if (pot.Patience < hand.RestAt)
                for (int k = 0; k < 6000 && pot.Patience < hand.RestTo; k++)
                {
                    pot.Rest(Dt);
                    time += Dt;
                }
        }
        void Work(ThrowMove move, float at, float dt)
        {
            pot.Work(move, Math.Clamp(at, 0f, pot.Rows), dt);
            time += dt;
        }
        int tall = Math.Clamp(shape.Rows + (int)MathF.Round(Noise(hand.Height)), 4, ClayPot.MaxRows);
        void PullTo(int height)
        {
            for (int pass = 0; pass < 200 && pot.Rows < height && !pot.Collapsed; pass++)
                for (float at = 0; at < pot.Rows && pot.Rows < height && !pot.Collapsed; at += 0.25f)
                {
                    RestIfTired();
                    Work(ThrowMove.PullUp, at + Noise(hand.Aim), Dt * 6);
                }
        }
        PullTo(tall - hand.Reserve);
        void Shape()
        {
            for (int sweep = 0; sweep < hand.Sweeps && !pot.Collapsed; sweep++)
            {
                float worst = 0;
                for (int i = 0; i < Math.Min(pot.Rows, shape.Rows) && !pot.Collapsed; i++)
                {
                    float diff = shape.R[i] - pot.R[i];
                    worst = MathF.Max(worst, MathF.Abs(diff));
                    if (MathF.Abs(diff) > hand.Tolerance)
                    {
                        RestIfTired();
                        float hold = MathF.Max(0.2f, 1f + Noise(hand.Overwork));
                        Work(diff > 0 ? ThrowMove.LetOut : ThrowMove.PressIn, i + Noise(hand.Aim), Dt * 3 * hold);
                    }
                }
                if (worst < hand.Tolerance + 0.1f)
                    break;
            }
        }
        Shape();
        // The game's tip, taken: a wall left heavy by pressing in is pulled up through to
        // thin it, which raises the pot, so it's shaped again. Only a hand that plans does it.
        for (int fix = 0; fix < hand.Rethins && !pot.Collapsed; fix++)
        {
            int rows = pot.Rows;
            int heavy = -1;
            for (int i = rows - 1; i >= 0; i--)
                if (pot.T[i] > PotJudge.HeavyWall(i, rows) - 0.2f) { heavy = i; break; }
            if (heavy < 0 || rows > tall)
                break;
            for (float at = heavy; at < pot.Rows && pot.Rows <= tall && !pot.Collapsed; at += 0.25f)
            {
                RestIfTired();
                Work(ThrowMove.PullUp, at + Noise(hand.Aim), Dt * 2);
            }
            Shape();
        }
        if (pot.Rows < tall && !pot.Collapsed)
        {
            PullTo(tall);
            Shape();
        }
        return (time, rests);
    }
}
#endif
