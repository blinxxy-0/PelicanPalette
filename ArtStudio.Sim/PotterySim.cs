using ArtStudio.Pottery;

// Calibration for the pottery wheel (docs/design/pottery_minigame.md, section 10):
// potters of four hands throw every shape on every difficulty, centring included, and
// the throw judge's stars are tallied. The same rules the menu runs (ClayPot.cs, PotBot.cs).
//
//   dotnet run -- pottery [throws per cell, default 40]
internal static class PotterySim
{
    private static PotteryRules? Tried;
    private static PotteryRules Rules(string level) => Tried != null && Tried.Name == PotteryRules.For(level).Name ? Tried : PotteryRules.For(level);

    public static void Run(string[] args)
    {
        if (args.FirstOrDefault() == "rows") { Diag(); return; }
        int n = args.Length > 0 && int.TryParse(args[0], out int k) ? k : 40;
        // trying a level's patience: pottery 40 hard <drain multiplier> <regen per second>
        if (args.Length > 3)
            Tried = PotteryRules.For(args[1]) with { DrainMultiplier = float.Parse(args[2]), Regen = float.Parse(args[3]) };
        var levels = new[] { "simplified", "normal", "hard" };

        Console.WriteLine($"== Throws: stars by hand and difficulty ({n} per shape, all four shapes) ==");
        Console.WriteLine($"  {"level",-10} {"hand",-8} {"stars",5} {"5*",4} {"<3*",4} {"fell",4}  {"shape",5} {"walls",5} {"centr",5}  {"min",4}  histogram 0..5");
        foreach (string level in levels)
        {
            var rules = Rules(level);
            foreach (var hand in PotHand.All)
            {
                var rs = new List<(PotResult R, float Secs, int Rests)>();
                var rng = new Random(7);
                foreach (string name in PotShape.Names)
                    for (int i = 0; i < n; i++)
                        rs.Add(One(name, rules, hand, rng));
                var stars = rs.Select(x => x.R.Stars).ToList();
                var standing = rs.Where(x => x.R.Stars > 0 || x.R.Shape > 0).ToList();
                Console.WriteLine($"  {level,-10} {hand.Name,-8} {stars.Average(),5:0.00} {Pct(stars, s => s >= 5f),4} {Pct(stars, s => s < 3f),4} {Pct(rs, x => x.R.Shape == 0 && x.R.Walls == 0),4}  "
                    + $"{Avg(standing, x => x.R.Shape),5:0.00} {Avg(standing, x => x.R.Walls),5:0.00} {rs.Average(x => x.R.Centring),5:0.00}  {stars.Min(),4:0.0}  {Histogram(stars)}  {rs.Average(x => x.Secs),4:0}s  rests {rs.Average(x => x.Rests):0.0}");
            }
        }

        Console.WriteLine("\n== By shape, normal ==");
        foreach (var hand in PotHand.All)
        {
            var rng = new Random(11);
            var cols = PotShape.Names.Select(name =>
            {
                var s = Enumerable.Range(0, n).Select(_ => One(name, PotteryRules.For("normal"), hand, rng).R.Stars).ToList();
                return $"{name} {s.Average():0.00}";
            });
            Console.WriteLine($"  {hand.Name,-8} " + string.Join("   ", cols));
        }

        Console.WriteLine("\n== Plans: a perfect hand that holds rows back and pulls heavy walls up through (normal) ==");
        Plans();

        Console.WriteLine("\n== Handling: seconds of pulling, sweeping foot to rim without a pause, until it falls ==");
        foreach (string level in levels)
        {
            var cols = PotShape.Names.Select(name =>
            {
                var pot = new ClayPot(Rules(level));
                float t = 0f;
                for (int k = 0; t < 300f && !pot.Collapsed; k++, t += 0.1f)
                    pot.Work(ThrowMove.PullUp, (k * 0.25f) % pot.Rows, 0.1f);
                return $"{name} {(pot.Collapsed ? $"{t:0}s at {pot.Rows} rows" : "never")}";
            });
            Console.WriteLine($"  {level,-10} " + string.Join("   ", cols));
        }

        Console.WriteLine("\n== Not really throwing (normal) ==");
        Lazy("never touched", (pot, shape) => { });
        Lazy("pulled, not shaped", (pot, shape) =>
        {
            for (int k = 0; k < 20000 && pot.Rows < shape.Rows && !pot.Collapsed; k++)
            {
                if (pot.Patience < 0.35f) while (pot.Patience < 0.9f) pot.Rest(1f / 60f);
                pot.Work(ThrowMove.PullUp, (k * 0.25f) % pot.Rows, 0.1f);
            }
        });
        Lazy("shaped, not pulled", (pot, shape) =>
        {
            for (int sweep = 0; sweep < 20; sweep++)
                for (int i = 0; i < pot.Rows; i++)
                {
                    if (pot.Patience < 0.35f) while (pot.Patience < 0.9f) pot.Rest(1f / 60f);
                    float diff = shape.R[i] - pot.R[i];
                    if (MathF.Abs(diff) > 0.3f) pot.Work(diff > 0 ? ThrowMove.LetOut : ThrowMove.PressIn, i, 0.05f);
                }
        });
        Lazy("pulled 6 rows short", (pot, shape) => PotBot.Throw(pot, Short(shape, pot.Volume, 6), PotHand.Perfect));
        Lazy("pulled 6 rows tall", (pot, shape) => PotBot.Throw(pot, Short(shape, pot.Volume, -6), PotHand.Perfect));
        Lazy("another shape's", (pot, shape) => PotBot.Throw(pot, PotShape.Make(shape.Name == "bowl" ? "vase" : "bowl", pot.Volume), PotHand.Perfect));
    }

    private static void Plans()
    {
        foreach (int reserve in new[] { 0, 2, 4, 6 })
            foreach (int rethins in new[] { 0, 4, 12 })
            {
                var cols = PotShape.Names.Select(name =>
                {
                    var pot = new ClayPot(PotteryRules.For("normal"));
                    var shape = PotShape.Make(name, pot.Volume);
                    PotBot.Throw(pot, shape, PotHand.Perfect with { Rethins = rethins, Reserve = reserve });
                    var r = PotJudge.Judge(pot, shape, 1f);
                    return $"{name} {r.Stars:0.0} (s{r.Shape:0.00} w{r.Walls:0.00} {pot.Rows}/{shape.Rows})";
                });
                Console.WriteLine($"  reserve {reserve} rethins {rethins,2}: " + string.Join("  ", cols));
            }
    }

    // Row by row, a perfect hand's pot on Normal against its outline: what the walls score.
    private static void Diag()
    {

        foreach (string name in PotShape.Names)
        {
            var pot = new ClayPot(PotteryRules.For("normal"));
            var shape = PotShape.Make(name, pot.Volume);
            PotBot.Throw(pot, shape, PotHand.Perfect);
            var r = PotJudge.Judge(pot, shape, 1f);
            Console.WriteLine($"{name}: rows {pot.Rows}/{shape.Rows} shape {r.Shape:0.00} walls {r.Walls:0.00} stars {r.Stars}");
            for (int i = 0; i < pot.Rows; i++)
                Console.WriteLine($"  {i,2} R {pot.R[i],5:0.0} want {(i < shape.Rows ? shape.R[i] : 0),5:0.0}  T {pot.T[i],4:0.00}  fit {PotJudge.WallFit(pot.T[i], i, pot.Rows):0.00}");
        }
    }

    private static (PotResult R, float Secs, int Rests) One(string name, PotteryRules rules, PotHand hand, Random rng)
    {
        var pot = new ClayPot(rules);
        var shape = PotShape.Make(name, pot.Volume);
        var centring = new Centring(rng);
        float secs = PotBot.Centre(centring, hand, rules.CentringBand, rng);
        var (thrown, rests) = PotBot.Throw(pot, shape, hand, rng);
        return (PotJudge.Judge(pot, shape, centring.Score), secs + thrown, rests);
    }

    private static void Lazy(string label, Action<ClayPot, PotShape> act)
    {
        var cols = PotShape.Names.Select(name =>
        {
            var pot = new ClayPot(PotteryRules.For("normal"));
            var shape = PotShape.Make(name, pot.Volume);
            act(pot, shape);
            return $"{name} {PotJudge.Judge(pot, shape, 1f).Stars:0.0}";
        });
        Console.WriteLine($"  {label,-20} " + string.Join("   ", cols));
    }

    // The outline cut short (or, negative, the same rows stood on a taller pot: the bot pulls to its height).
    private static PotShape Short(PotShape shape, float volume, int by)
    {
        var rows = Math.Clamp(shape.Rows - by, 4, ClayPot.MaxRows);
        return PotShape.Of(shape.Name, Enumerable.Range(0, rows).Select(i => shape.R[Math.Min(i * (shape.Rows - 1) / Math.Max(1, rows - 1), shape.Rows - 1)]).ToArray());
    }

    private static string Pct<T>(List<T> xs, Func<T, bool> f) => $"{100 * xs.Count(f) / Math.Max(1, xs.Count)}%";
    private static float Avg<T>(List<T> xs, Func<T, float> f) => xs.Count == 0 ? 0f : xs.Average(f);

    private static string Histogram(List<float> stars)
    {
        const string bars = " ▁▂▃▄▅▆▇█";
        var counts = Enumerable.Range(0, 11).Select(i => stars.Count(s => MathF.Abs(s - i / 2f) < 0.01f)).ToArray();
        int max = Math.Max(1, counts.Max());
        return string.Concat(counts.Select(c => bars[(int)MathF.Ceiling(c * 8f / max)]));
    }
}
