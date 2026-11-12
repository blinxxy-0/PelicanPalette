#if DEBUG
using System;
using System.Collections.Generic;

namespace ArtStudio.Pottery;

// Centring (docs/design/pottery_minigame.md, section 3): the lump wobbles on the wheel,
// your hands on it pull it true, and let go it wanders. Game-free, so the simulation
// (ArtStudio.Sim, `dotnet run -- pottery`) runs the same rules the menu does.
internal sealed class Centring
{
    // An off-centre lump doesn't wander about the wheel: it's carried round by it, so seen from the
    // front it sways side to side about the axis, once a turn, as far as it's off true. Your hands
    // ride that sway (the mouse, held over the lump) and press it true; off it, or let go, and the
    // lump works further off. It can't leave the wheel: the sway never grows past MaxSway. (The user,
    // 2026-11-11: "the clay sometimes goes fully off the spinny thing with no real regard for physics".)
    public const float TrueWithin = 0.6f;   // units of sway: running true
    public const float HoldTrue = 1.5f;     // seconds true and it's done
    public const float MaxTime = 14f;       // or done anyway, as true as it got
    public const float MaxSway = 6f;        // units: the most it's ever off (inside the wheel head)
    public const float Range = 12f;         // units the screen's off-true meter spans either side
    public const float Turn = MathF.Tau * 0.8f;   // radians a second: the wheel slow, for centring
    private const float Settle = 2.2f, Push = 1.4f, Creep = 0.7f;   // sway per second: pressed true, pushed off, let go

    private readonly Random _random;
    private float _trueFor, _time;

    public float Sway { get; private set; }       // how far off true it is (units)
    public float Phase { get; private set; }      // where the wheel is in its turn (radians)
    public float Wobble => Sway * MathF.Sin(Phase);   // where the lump is across the wheel, this instant
    public bool Done { get; private set; }
    public bool RunningTrue => Sway < TrueWithin;
    public float Score => Math.Clamp(1f - Sway / 4f, 0f, 1f);

    public Centring(Random random)
    {
        _random = random;
        Sway = 5f + (float)random.NextDouble() * 1f;
        Phase = (float)(random.NextDouble() * MathF.Tau);
    }

    // `hand`: where your hands are across the lump (units from the wheel's centre), or
    // null when you aren't holding it. On it is within `band` of where the lump is now.
    public void Step(float dt, float? hand, float band)
    {
        if (Done)
            return;
        _time += dt;
        Phase = (Phase + Turn * dt) % MathF.Tau;
        float change;
        if (hand is float h)
        {
            float off = MathF.Abs(h - Wobble);
            // on it: settles, faster the closer you ride it; off it: you shove it further off
            change = off < band ? -Settle * (1f - 0.6f * off / band) : Push;
        }
        else
            change = Creep;
        change += (float)(_random.NextDouble() - 0.5) * 0.8f;          // it never quite stops trying
        Sway = Math.Clamp(Sway + change * dt, 0f, MaxSway);
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

    // Hands riding the lump's sway. A person reads its rhythm and moves with it rather than
    // chasing it, so the bot aims where the lump will be, a little early or late: its timing
    // error wanders about 0 with a spread of `Lag` seconds, and its hand wanders `Wander` units.
    // Returns seconds taken.
    public static float Centre(Centring c, PotHand hand, float band, Random rng)
    {
        float timing = 0f, err = 0f, time = 0f;
        while (!c.Done)
        {
            timing = timing * 0.97f + Gauss(rng) * hand.Lag * 0.25f;
            err = err * 0.97f + Gauss(rng) * hand.Wander * 0.25f;
            float aimAt = c.Sway * MathF.Sin(c.Phase + Centring.Turn * timing);
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
            if (pot.Patience < hand.RestAt && pot.Patience < MathF.Min(hand.RestTo, pot.MaxPatience) - 0.001f)
                rests++;
            // (rest only as far as the clay can come back: MaxPatience)
            if (pot.Patience < hand.RestAt)
                for (int k = 0; k < 6000 && pot.Patience < MathF.Min(hand.RestTo, pot.MaxPatience) - 0.001f; k++)
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
