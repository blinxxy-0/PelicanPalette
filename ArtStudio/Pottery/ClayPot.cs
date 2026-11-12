#if DEBUG
using System;
using System.Linq;

namespace ArtStudio.Pottery;

// The pottery prototype (docs/design/pottery_minigame.md, 1.5): a pot on the
// wheel is round, so its whole shape is one profile, the wall from foot to
// rim. Here that's a stack of rows, each with an outer radius and a wall
// thickness, and the clay in them is conserved: you can only move it.
//
// Units: a row is one step up the wall; radius and thickness are in "clay
// units" (the screen draws a unit as a few pixels). A row's clay is its
// ring's area over pi: r^2 - (r - t)^2 = 2rt - t^2.
internal enum ThrowMove { PullUp, PressIn, LetOut }

internal sealed record PotteryRules(
    string Name,
    float DrainMultiplier,      // how fast handling uses up the clay's patience
    float Regen,                // patience per second back while you pause
    bool Collapse,              // walls too thin for the clay above them fall in
    float CentringBand,         // how close your hands must stay to the lump (units)
    float Drift,                // banding: how far your brush hand wanders (rows)
    float KilnBand,             // firing: the band's half-width (on the kiln gauge's 0 to 100)
    bool KilnCrack,             // firing: far too hot for too long cracks the pot
    float GiveWay = 0f,         // seconds you can keep working clay out of patience before it falls (0: never)
    float GiftOdds = 1f,        // firing: how often kiln gifts come, against the usual (Kiln Keeper: 2)
    float Recover = 1f)         // the share of the patience spent that resting can bring back
{
    // (2026-11-11, the user after playing: "patience raises too fast, it should be more like
    // almost permanent loss with slight raises", and the whole thing too forgiving. Resting gives
    // back only a little of what's been spent, so a throw is one budget of handling: work surely.
    // Clay worked on past its patience gives way on Normal too. Calibrated: pottery_minigame.md
    // section 10.)
    public const float HardDrain = 1.6f, NormalDrift = 1.6f, HardDrift = 2.4f, NormalKilnBand = 5f, HardKilnBand = 4f;

    public static PotteryRules For(string name) => name switch
    {
        "simplified" => new("Simplified", 0f, 1f, false, 3f, 0.35f, 10f, false),
        // Hard: patience drains faster and comes back least; the brush wanders more; the kiln's
        // band is narrower and overfiring cracks. (It once drained 3x with full rests, so a throw
        // needed a rest or two; with rests giving little back, that's 1.6x.)
        "hard" => new("Hard", HardDrain, 0.03f, true, 1.5f, HardDrift, HardKilnBand, true, GiveWay: 2f, Recover: 0.08f),
        _ => new("Normal", 1f, 0.04f, true, 2.1f, NormalDrift, NormalKilnBand, false, GiveWay: 3f, Recover: 0.15f),
    };
}

internal sealed class ClayPot
{
    public const int MaxRows = 44;
    public const float MaxRadius = 26f, MinRadius = 2f;
    public const float StartRows = 8, StartRadius = 9f, StartThickness = 4.5f;

    // Tunable feel (the prototype's knobs)
    private const float Spread = 1.6f;          // rows a hand's pressure reaches, either side
    private const float PressRate = 5f;         // units of radius per second, pressing in or letting out
    private const float PullRate = 1.6f;        // units of thickness per second, pulling up
    private const float PullNarrowing = 0.6f;   // pulling up draws the wall in a little, as it does
    private const float DrainPull = 0.055f, DrainPress = 0.04f, DrainLetOut = 0.05f;   // patience per second
    private const float SagBelow = 0.25f;       // patience under which the walls start to slump
    private const float PullFloor = 1.5f;       // you can't pull a wall thinner than your fingers can feel
    private const float StretchFloor = 1.3f;    // letting out stops when the wall won't stretch further
    private const float MaxStep = 1.5f;         // neighbouring rows can't differ by more: the wall drags along

    public readonly float[] R = new float[MaxRows];
    public readonly float[] T = new float[MaxRows];
    public int Rows { get; private set; }
    public float Patience { get; private set; } = 1f;
    public float Spent { get; private set; }                    // all the patience used up so far
    // as high as resting can bring patience back: only the rules' share of what's been spent
    public float MaxPatience => MathF.Max(0f, 1f - (1f - _rules.Recover) * Spent);
    public bool Collapsed { get; private set; }
    public string? CollapseReason { get; private set; }
    public readonly float Volume;
    private float _risen;                        // clay pulled up, waiting to make the next row
    private float _spentFor;                     // seconds worked with no patience left

    private readonly PotteryRules _rules;

    public ClayPot(PotteryRules rules)
    {
        _rules = rules;
        Rows = (int)StartRows;
        for (int i = 0; i < Rows; i++)
        {
            R[i] = StartRadius;
            T[i] = StartThickness;
        }
        Volume = TotalClay();
    }

    public static float Area(float r, float t) => 2f * r * t - t * t;

    // The thickness that holds a row's clay at a new radius (the smaller root of
    // t^2 - 2rt + A = 0); a wall can't be thicker than the pot is wide.
    private static float ThicknessFor(float r, float area) =>
        r * r <= area ? r : r - MathF.Sqrt(r * r - area);

    public float TotalClay() => Enumerable.Range(0, Rows).Sum(i => Area(R[i], T[i])) + _risen;

    // A hand at a height (in rows, from the foot) for dt seconds.
    public void Work(ThrowMove move, float atRow, float dt)
    {
        if (Collapsed)
            return;
        for (int i = 0; i < Rows; i++)
        {
            float w = MathF.Exp(-((i - atRow) * (i - atRow)) / (2f * Spread * Spread));
            if (w < 0.02f)
                continue;
            float area = Area(R[i], T[i]);
            switch (move)
            {
                case ThrowMove.PressIn:
                case ThrowMove.LetOut:
                    float dr = (move == ThrowMove.PressIn ? -1f : 1f) * PressRate * dt * w;
                    float r = Math.Clamp(R[i] + dr, MathF.Max(MinRadius, T[i] * 0.6f), MaxRadius);
                    float t = ThicknessFor(r, area);
                    if (move == ThrowMove.LetOut && t < StretchFloor)
                        break;
                    R[i] = r;
                    T[i] = t;
                    break;
                case ThrowMove.PullUp:
                    // the hand rides up the wall: everything below it thins, the clay rises
                    if (i > atRow + Spread)
                        break;
                    float take = MathF.Min(PullRate * dt * w, T[i] - PullFloor);
                    if (take <= 0f)
                        break;
                    float newT = T[i] - take;
                    float newR = MathF.Max(MinRadius, R[i] - PullNarrowing * dt * w);
                    _risen += area - Area(newR, newT);
                    R[i] = newR;
                    T[i] = newT;
                    break;
            }
        }
        // risen clay makes new rows on top, like the one below it
        while (Rows < MaxRows && Rows > 0)
        {
            float need = Area(R[Rows - 1], T[Rows - 1]);
            if (_risen < need)
                break;
            R[Rows] = R[Rows - 1];
            T[Rows] = T[Rows - 1];
            Rows++;
            _risen -= need;
        }
        // clay is one piece: a row dragged far from its neighbour pulls them together
        for (int i = 1; i < Rows; i++)
        {
            float gap = R[i] - R[i - 1];
            if (MathF.Abs(gap) <= MaxStep)
                continue;
            float fix = (MathF.Abs(gap) - MaxStep) / 2f * MathF.Sign(gap);
            foreach (var (j, sign) in new[] { (i, -1f), (i - 1, 1f) })
            {
                float a = Area(R[j], T[j]);
                R[j] += sign * fix;
                T[j] = ThicknessFor(R[j], a);
            }
        }
        float drain = move switch { ThrowMove.PullUp => DrainPull, ThrowMove.PressIn => DrainPress, _ => DrainLetOut };
        float used = MathF.Min(Patience, drain * _rules.DrainMultiplier * dt);
        Patience -= used;
        Spent += used;
        _spentFor = Patience <= 0f ? _spentFor + dt : 0f;
        if (_rules.GiveWay > 0f && _spentFor > _rules.GiveWay)
        {
            Collapsed = true;
            CollapseReason = "You kept working it after its patience ran out. Tired clay gives way: work it less, and surer.";
            return;
        }
        Settle(dt, working: true);
    }

    // Time passing: the clay firms up while you pause, and slumps when it's tired.
    public void Rest(float dt) => Settle(dt, working: false);

    private void Settle(float dt, bool working)
    {
        if (Collapsed)
            return;
        if (!working && Patience < MaxPatience)
            Patience = MathF.Min(MaxPatience, Patience + _rules.Regen * dt);
        if (Patience < SagBelow)
        {
            // tired clay slumps outward, most at the top
            float slump = (SagBelow - Patience) * 1.2f * dt;
            for (int i = 1; i < Rows; i++)
            {
                float area = Area(R[i], T[i]);
                R[i] = MathF.Min(MaxRadius, R[i] + slump * i / Rows);
                T[i] = ThicknessFor(R[i], area);
            }
        }
        if (!_rules.Collapse)
            return;
        for (int i = 0; i < Rows; i++)
        {
            // a wall must carry the clay above it, and wet clay carries less
            float above = Rows - 1 - i;
            float needed = 0.7f + 0.022f * above * (1.6f - Patience);
            if (T[i] < needed)
            {
                Collapsed = true;
                CollapseReason = "The walls were too thin to hold the clay above them.";
                return;
            }
        }
    }
}

// A shape to throw: the radius at each row, sized so the lump has just enough
// clay for it with good walls.
internal sealed class PotShape
{
    public const float GoodWall = 2.2f;
    public string Name { get; }
    public float[] R { get; }
    public int Rows => R.Length;

    public static PotShape Of(string name, float[] r) => new(name, r);   // the simulation's odd outlines

    private PotShape(string name, float[] r)
    {
        Name = name;
        R = r;
    }

    public static readonly string[] Names = { "cup", "bowl", "vase", "jug" };

    public static PotShape Make(string name, float volume)
    {
        Func<float, float> radius = name switch
        {
            "bowl" => u => 4.5f + 10.5f * MathF.Pow(u, 0.55f),
            "vase" => u => u < 0.7f ? 6f + 5.5f * MathF.Sin(MathF.PI * u / 0.7f * 0.75f)
                         : u < 0.88f ? 6f + 5.5f * MathF.Sin(MathF.PI * 0.75f) - (u - 0.7f) / 0.18f * 5.5f
                         : 4.3f + (u - 0.88f) / 0.12f * 2.2f,
            "jug" => u => 8.5f + 1.8f * MathF.Sin(MathF.PI * u) - 2.2f * MathF.Max(0f, u - 0.75f) / 0.25f,
            _ => u => 7f + 1.4f * u,           // the cup: straight, a little flared
        };
        // as many rows as the clay makes at good walls
        for (int rows = 4; rows <= ClayPot.MaxRows; rows++)
        {
            float[] r = Enumerable.Range(0, rows).Select(i => radius(rows == 1 ? 0f : i / (float)(rows - 1))).ToArray();
            float clay = r.Sum(x => ClayPot.Area(x, GoodWall));
            if (clay >= volume * 0.92f || rows == ClayPot.MaxRows)
                return new PotShape(name, r);
        }
        throw new InvalidOperationException();
    }
}

internal sealed record PotResult(float Shape, float Walls, float Centring, float Overall, float Stars);

internal static class PotJudge
{
    public const float ThinWall = 1.5f;
    // How strict the throw's judge is. (2026-11-11, the user: "getting a bad star rating is nearly
    // impossible". It had: a silhouette scored from 55% overlap to full at 95%, walls falling off over
    // 0.5 thin and 1.5 heavy, steps of 0.9 for smoothness, centring worth 15%. Calibrated by
    // simulation: pottery_minigame.md section 10.)
    public const float ShapeFloor = 0.76f, ShapeSpan = 0.21f;           // overlap at nothing, and to full marks
    public const float ThinFalloff = 0.4f, HeavyFalloff = 1.0f;         // units past a good wall to none
    public const float SmoothSpan = 0.6f;                               // the mean step in thickness for none
    public const float CentringWeight = 0.25f;                          // how much an off-centre pot loses

    // The thickness a good wall has at a row: never under 1.5, and no heavier than 3.4,
    // except the foot (thick, trimmed later) and the rim (compressed, firm). (The user,
    // 2026-11-07, was stuck at 3.5 stars: the first judge wanted one even thickness from
    // foot to rim, which pulling never gives and no potter aims for.)
    public static float HeavyWall(int row, int rows) => row < 2 ? 4.6f : row >= rows - 2 ? 4.2f : 3.4f;

    // 1 for a good wall, falling off either side.
    public static float WallFit(float t, int row, int rows)
    {
        float hi = HeavyWall(row, rows);
        if (t < ThinWall) return Math.Clamp(1f - (ThinWall - t) / ThinFalloff, 0f, 1f);
        if (t > hi) return Math.Clamp(1f - (t - hi) / HeavyFalloff, 0f, 1f);
        return 1f;
    }

    public static PotResult Judge(ClayPot pot, PotShape shape, float centring)
    {
        if (pot.Collapsed)
            return new PotResult(0, 0, centring, 0, 0);
        // Shape: the silhouettes' overlap over their union, row by row
        int n = Math.Max(pot.Rows, shape.Rows);
        float inter = 0, union = 0;
        for (int i = 0; i < n; i++)
        {
            float a = i < pot.Rows ? pot.R[i] : 0f, b = i < shape.Rows ? shape.R[i] : 0f;
            inter += MathF.Min(a, b);
            union += MathF.Max(a, b);
        }
        float iou = union > 0 ? inter / union : 0f;
        // (a silhouette 70% right is a poor pot: stretch the top of the range)
        float shapeScore = Math.Clamp((iou - ShapeFloor) / ShapeSpan, 0f, 1f);
        // Walls: every row a good thickness, changing smoothly up the pot
        int rows = pot.Rows;
        float fine = Enumerable.Range(0, rows).Average(i => WallFit(pot.T[i], i, rows));
        float smooth = rows > 3 ? Math.Clamp(1f - (float)Enumerable.Range(2, rows - 2).Average(i => MathF.Abs(pot.T[i] - pot.T[i - 1])) / SmoothSpan, 0f, 1f) : 1f;
        float walls = 0.6f * fine + 0.4f * smooth;
        float overall = (0.7f * shapeScore + 0.3f * walls) * (1f - CentringWeight + CentringWeight * centring);
        return new PotResult(shapeScore, walls, centring, overall, MathF.Round(overall * 10f) / 2f);
    }

    // What cost the most, in a potter's words: up to two tips.
    public static string[] Tips(ClayPot pot, PotShape shape, PotResult r)
    {
        var tips = new System.Collections.Generic.List<string>();
        string Part(int i, int rows) => i < rows / 3 ? "low down" : i < 2 * rows / 3 ? "in the middle" : "near the top";
        if (r.Shape < 0.95f)
        {
            if (pot.Rows < shape.Rows - 1)
                tips.Add("It's shorter than the outline: pull up more before you shape it.");
            else if (pot.Rows > shape.Rows + 1)
                tips.Add("It's taller than the outline: stop pulling sooner.");
            else
            {
                int worst = Enumerable.Range(0, Math.Min(pot.Rows, shape.Rows)).OrderByDescending(i => MathF.Abs(pot.R[i] - shape.R[i])).First();
                tips.Add($"It's too {(pot.R[worst] > shape.R[worst] ? "wide" : "narrow")} {Part(worst, shape.Rows)}.");
            }
        }
        int thin = Enumerable.Range(0, pot.Rows).FirstOrDefault(i => pot.T[i] < ThinWall, -1);
        int heavy = Enumerable.Range(0, pot.Rows).FirstOrDefault(i => pot.T[i] > HeavyWall(i, pot.Rows) + 0.3f, -1);
        if (thin >= 0)
            tips.Add($"The wall's thin {Part(thin, pot.Rows)}: pull less there.");
        // (the simulation, 2026-11-09: a neck pressed in at full height can't be thinned
        // without overshooting; the 5-star vase holds a few rows back, then pulls the neck up)
        bool Heavy(int i) => pot.T[i] > HeavyWall(i, pot.Rows) + 0.3f;
        bool heavyNeck = Enumerable.Range(2 * pot.Rows / 3, Math.Max(0, pot.Rows - 2 - 2 * pot.Rows / 3)).Any(Heavy);
        if (heavyNeck)
            tips.Add("The neck's heavy: pressing in thickens it. Stop pulling a few rows short, press the neck in, then pull up through it to full height.");
        if (heavy >= 0 && !(heavyNeck && heavy >= 2 * pot.Rows / 3))
            tips.Add($"The wall's heavy {Part(heavy, pot.Rows)}: pull up through it to thin it before you shape it. Pressing in thickens a wall.");
        if (r.Centring < 0.9f)
            tips.Add("It ran a little off-centre: hold the lump longer at the start.");
        if (tips.Count == 0)
            tips.Add(r.Stars >= 5f ? "A potter's pot." : "Smoother walls: long, slow pulls from the foot to the rim.");
        return tips.Take(2).ToArray();
    }
}
#endif
