using System.Text.Json;
using ArtStudio.Core;

// Calibration for the painting minigame (Muse's lesson: check the score
// distribution by simulation, never by eyeballing one case).
//
//   1. Judge: stars for each kind of attempt, all paint pure.
//   2. Mixing: how often a bot of a given hand gets pure/off/muddy paint.
//   3. Sessions: hand + level → quality tier distribution.
//
// Usage: dotnet run -- [refs.json from museart_export_refs] (Muse's sketches
// in muse_refs.json are always used).

var refs = LoadReferences(args);
Console.WriteLine($"{refs.Count} references\n");

Section("1. Judge: stars by attempt (all pure paint)");
var attempts = new (string Name, Func<Reference, Random, (byte[,], Purity[,])> Make)[]
{
    ("perfect", (r, _) => Copy(r)),
    ("5% wrong", (r, rng) => Wrong(r, rng, 0.05)),
    ("15% wrong", (r, rng) => Wrong(r, rng, 0.15)),
    ("35% wrong", (r, rng) => Wrong(r, rng, 0.35)),
    ("shifted 2", (r, _) => Shift(r, 2, 1)),
    ("75% done", (r, _) => Partial(r, 0.75)),
    ("50% done", (r, _) => Partial(r, 0.5)),
    ("flat color", (r, _) => Flat(r)),
    ("scribble", (r, rng) => Scribble(r, rng)),
};
foreach (var (name, make) in attempts)
{
    var stars = new List<float>();
    var rng = new Random(1);
    foreach (var r in refs)
        for (int k = 0; k < 4; k++)
        {
            var (cells, purity) = make(r, rng);
            stars.Add(PaintingJudge.Judge(cells, purity, r).Stars);
        }
    Console.WriteLine($"  {name,-11} mean {stars.Average():0.00}  min {stars.Min():0.0}  max {stars.Max():0.0}  {Histogram(stars)}");
}

Section("2. Mixing: pure / off / muddy %, mean seconds");
var hands = new Hand[] { new("sloppy", 18, 45f, 0), new("average", 10, 22f, 6), new("sharp", 6, 10f, 10) };
var pigments = new[] { "charcoal", "crimson", "slate", "cerulean", "ruby" }.Select(Palette.IndexOf).ToArray();
Console.WriteLine($"  {"hand",-8} {"lvl",3}  " + string.Join("  ", pigments.Select(p => $"{Palette.All[p].Name,-16}")));
var mixRates = new Dictionary<(string, int, int), (double Pure, double Off, double Muddy)>();
foreach (var hand in hands)
foreach (int level in new[] { 0, 3, 6, 10 })
{
    var cols = new List<string>();
    foreach (int p in pigments)
    {
        var (pure, off, muddy, secs) = MixStats(hand, level, p, 400);
        mixRates[(hand.Name, level, p)] = (pure, off, muddy);
        cols.Add($"{pure * 100,3:0}/{off * 100,2:0}/{muddy * 100,2:0} {secs,3:0.0}s");
    }
    Console.WriteLine($"  {hand.Name,-8} {level,3}  " + string.Join("  ", cols));
}

Section("3. Sessions: quality tier % (normal/silver/gold/iridium) by hand and level");
// How accurately each hand places cells, and how often it catches 3 sparks.
var accuracy = new Dictionary<string, (double Wrong, double Inspired)>
{
    ["sloppy"] = (0.25, 0.1),
    ["average"] = (0.10, 0.45),
    ["sharp"] = (0.03, 0.8),
};
foreach (var hand in hands)
foreach (int level in new[] { 0, 2, 4, 6, 8, 10 })
{
    var tiers = new int[5];
    var rng = new Random(7);
    var starsSeen = new List<float>();
    foreach (var r in refs)
        for (int k = 0; k < 6; k++)
        {
            var (cells, purity) = Wrong(r, rng, accuracy[hand.Name].Wrong);
            // Each pigment's cells get the purity of a mix of that pigment.
            var mixFor = new Dictionary<byte, Purity>();
            int n = r.Size;
            bool allPure = true;
            for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                byte c = cells[x, y];
                if (c == PixelCanvas.Blank) continue;
                if (!mixFor.TryGetValue(c, out var mp) || rng.NextDouble() < 0.08)
                    mixFor[c] = mp = RollMix(mixRates, hand.Name, level, c, rng);
                purity[x, y] = mp;
                if (mp != Purity.Pure) allPure = false;
            }
            var result = PaintingJudge.Judge(cells, purity, r);
            bool inspired = rng.NextDouble() < accuracy[hand.Name].Inspired;
            float score = PaintingRules.Score(result.Stars, level, inspired, allPure);
            tiers[PaintingRules.Quality(score, level)]++;
            starsSeen.Add(result.Stars);
        }
    double total = tiers.Sum();
    Console.WriteLine($"  {hand.Name,-8} lvl {level,2}  stars {starsSeen.Average():0.00}   " +
        $"{tiers[0] / total * 100,3:0} / {tiers[1] / total * 100,3:0} / {tiers[2] / total * 100,3:0} / {tiers[4] / total * 100,3:0}");
}

// ---------------------------------------------------------------------------

static void Section(string title) => Console.WriteLine($"\n== {title} ==");

static string Histogram(List<float> stars)
{
    var buckets = new int[11];
    foreach (var s in stars) buckets[(int)Math.Round(s * 2)]++;
    return string.Join(" ", Enumerable.Range(1, 10).Select(i => $"{i / 2.0:0.#}:{buckets[i]}"));
}

static (byte[,], Purity[,]) Copy(Reference r) => ((byte[,])r.Cells.Clone(), new Purity[r.Size, r.Size]);

static (byte[,], Purity[,]) Wrong(Reference r, Random rng, double rate)
{
    var (cells, purity) = Copy(r);
    var used = r.PigmentsUsed;
    for (int x = 0; x < r.Size; x++)
    for (int y = 0; y < r.Size; y++)
    {
        if (cells[x, y] == PixelCanvas.Blank || rng.NextDouble() >= rate) continue;
        // Half the slips are a neighbouring color from the same subject, half spill outside.
        if (rng.NextDouble() < 0.5)
        {
            var others = used.Where(p => p != cells[x, y]).ToList();
            cells[x, y] = (byte)(others.Count > 0 ? others[rng.Next(others.Count)] : (cells[x, y] + 1) % Palette.All.Length);
        }
        else
        {
            int nx = Math.Clamp(x + rng.Next(-2, 3), 0, r.Size - 1), ny = Math.Clamp(y + rng.Next(-2, 3), 0, r.Size - 1);
            if (cells[nx, ny] == PixelCanvas.Blank) cells[nx, ny] = cells[x, y];
        }
    }
    return (cells, purity);
}

static (byte[,], Purity[,]) Shift(Reference r, int dx, int dy)
{
    var cells = PixelCanvas.NewBlank(r.Size);
    for (int x = 0; x < r.Size; x++)
    for (int y = 0; y < r.Size; y++)
    {
        int nx = x + dx, ny = y + dy;
        if (nx < r.Size && ny < r.Size) cells[nx, ny] = r.Cells[x, y];
    }
    return (cells, new Purity[r.Size, r.Size]);
}

// The light ran out: painted top-down until a fraction of the subject was done.
static (byte[,], Purity[,]) Partial(Reference r, double fraction)
{
    var cells = PixelCanvas.NewBlank(r.Size);
    int target = (int)(r.SubjectCells * fraction), done = 0;
    for (int y = 0; y < r.Size && done < target; y++)
    for (int x = 0; x < r.Size && done < target; x++)
        if (r.Cells[x, y] != PixelCanvas.Blank) { cells[x, y] = r.Cells[x, y]; done++; }
    return (cells, new Purity[r.Size, r.Size]);
}

static (byte[,], Purity[,]) Flat(Reference r)
{
    var (cells, purity) = Copy(r);
    byte one = (byte)r.PigmentsUsed[0];
    for (int x = 0; x < r.Size; x++)
    for (int y = 0; y < r.Size; y++)
        if (cells[x, y] != PixelCanvas.Blank) cells[x, y] = one;
    return (cells, purity);
}

static (byte[,], Purity[,]) Scribble(Reference r, Random rng)
{
    var cells = PixelCanvas.NewBlank(r.Size);
    for (int i = 0; i < r.SubjectCells; i++)
        cells[rng.Next(r.Size), rng.Next(r.Size)] = (byte)r.PigmentsUsed[rng.Next(r.PigmentsUsed.Count)];
    return (cells, new Purity[r.Size, r.Size]);
}

static (double Pure, double Off, double Muddy, double Seconds) MixStats(Hand hand, int level, int pigment, int trials)
{
    var p = Palette.All[pigment];
    int pure = 0, off = 0, muddy = 0;
    double seconds = 0;
    var rng = new Random(pigment * 31 + level);
    for (int t = 0; t < trials; t++)
    {
        var bar = new MixBar(new MixSettings(p.Difficulty, p.Motion, PaintingRules.BandHeight(level, 0),
            PaintingRules.MixStartMeter(false), PaintingRules.MixTimeLimit(false)), rng);
        var seen = new Queue<float>();
        while (bar.Outcome == MixOutcome.Mixing)
        {
            seen.Enqueue(bar.Marker);
            float observed = seen.Count > hand.Delay ? seen.Dequeue() : seen.Peek();
            observed += (float)(rng.NextDouble() * 2 - 1) * hand.Noise;
            float center = bar.BandPos + bar.BandHeight / 2 + bar.BandSpeed * hand.Lookahead;
            bar.Tick(center > observed);
        }
        seconds += bar.Elapsed;
        if (bar.Outcome == MixOutcome.Pure) pure++;
        else if (bar.Outcome == MixOutcome.Off) off++;
        else muddy++;
    }
    return (pure / (double)trials, off / (double)trials, muddy / (double)trials, seconds / trials);
}

static Purity RollMix(Dictionary<(string, int, int), (double Pure, double Off, double Muddy)> rates, string hand, int level, int pigment, Random rng)
{
    // Interpolate from the nearest simulated pigment by difficulty.
    var known = rates.Keys.Where(k => k.Item1 == hand && k.Item2 == NearestLevel(level)).ToList();
    var best = known.OrderBy(k => Math.Abs(Palette.All[k.Item3].Difficulty - Palette.All[pigment].Difficulty)).First();
    var (pure, off, _) = rates[best];
    double roll = rng.NextDouble();
    return roll < pure ? Purity.Pure : roll < pure + off ? Purity.Off : Purity.Muddy;
}

static int NearestLevel(int level) => new[] { 0, 3, 6, 10 }.OrderBy(l => Math.Abs(l - level)).First();

static List<Reference> LoadReferences(string[] args)
{
    var all = Enumerable.Range(0, Palette.All.Length).ToList();
    var list = new List<Reference>();

    string musePath = Path.Combine(AppContext.BaseDirectory, "muse_refs.json");
    if (!File.Exists(musePath)) musePath = Path.Combine(Directory.GetCurrentDirectory(), "muse_refs.json");
    foreach (var img in JsonDocument.Parse(File.ReadAllText(musePath)).RootElement.EnumerateArray())
    {
        int w = img.GetProperty("w").GetInt32(), h = img.GetProperty("h").GetInt32();
        var cells = new byte[w, h];
        var rows = img.GetProperty("rgba").EnumerateArray().Select(e => e.GetString()!).ToArray();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var hex = rows[y].Substring(x * 8, 8);
            byte r = Convert.ToByte(hex[..2], 16), g = Convert.ToByte(hex[2..4], 16), b = Convert.ToByte(hex[4..6], 16), a = Convert.ToByte(hex[6..8], 16);
            cells[x, y] = a < 128 ? PixelCanvas.Blank : (byte)Palette.Nearest(r, g, b, all);
        }
        list.Add(new Reference(cells));
    }

    // Item references exported from the game: palette-index rows.
    if (args.Length > 0 && File.Exists(args[0]))
    {
        var items = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement.EnumerateArray().ToList();
        foreach (var item in items.Where((_, i) => i % 8 == 0))
        {
            var rows = item.GetProperty("starter").EnumerateArray().Select(e => e.GetString()!).ToArray();
            var cells = new byte[16, 16];
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                cells[x, y] = rows[y][x] == '.' ? PixelCanvas.Blank : (byte)(rows[y][x] - 'a');
            var r = Reference.Centered(cells, 20);
            if (r.SubjectCells > 20) list.Add(r);
        }
    }
    return list;
}

internal record Hand(string Name, int Delay, float Noise, float Lookahead);
