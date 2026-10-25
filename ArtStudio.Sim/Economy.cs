using ArtStudio.Core;

// The economy check (dotnet run -- economy): what a painting earns for the
// hours it takes, from a first parsnip to a varnished Masterpiece sold on
// consignment, against the game's own ways to earn. Every number comes
// from PaintingRules and Market, so this runs on the shipping rules.
internal static class Economy
{
    // Paint costs about this per cell (tubes are 8-60g for 100 cells, most 8-20g).
    private const float PaintPerCell = 0.15f;

    private record Case(
        string Name, int Level, CanvasKind Kind, int Difficulty, int SubjectPrice, int Cells,
        int Quality, bool Long = false, bool Gallerist = false, bool Renown = false, int TimesBefore = 1,
        bool Masterpiece = false, string? Finish = null, string Sale = "shop", int CanvasCost = 0, int Extras = 0);

    // Rough active income in the base game, per in-game hour (from the wiki's
    // prices and typical catch and floor rates; a sense of scale, not a rule).
    private static readonly (string What, int PerHour)[] Benchmarks =
    {
        ("Early fishing (spring, bamboo pole)", 150),
        ("Mid-game fishing or the Mines", 500),
        ("Late Skull Cavern runs", 2000),
    };

    public static void Run()
    {
        int Hours(Case c) => PaintingRules.Minutes(c.Kind, c.Long) / 60;
        var cases = new[]
        {
            new Case("First parsnip (L1, small, normal, to Pierre)", 1, CanvasKind.Small, 45, 35, 120, PaintingRules.Normal, Sale: "pierre", CanvasCost: 25, TimesBefore: 0),
            new Case("Early still life (L3, small, silver, Sable's)", 3, CanvasKind.Small, 50, 80, 120, PaintingRules.Silver, CanvasCost: 25),
            new Case("Mid still life (L5, medium pair, gold, Gallerist)", 5, CanvasKind.Medium, 55, 120, 260, PaintingRules.Gold, Gallerist: true, CanvasCost: 80),
            new Case("Mid portrait (L6, medium, gold, Gallerist)", 6, CanvasKind.Medium, 100, PaintingRules.PortraitSubjectPrice, 500, PaintingRules.Gold, Long: true, Gallerist: true, CanvasCost: 80),
            new Case("Late still life (L9, large trio, iridium, renown)", 9, CanvasKind.Large, 60, 150, 420, PaintingRules.Iridium, Gallerist: true, Renown: true, CanvasCost: 200),
            new Case("Late view (L9, large, iridium, renown)", 9, CanvasKind.Large, 100, 450, 1296, PaintingRules.Iridium, Long: true, Gallerist: true, Renown: true, CanvasCost: 200),
            new Case("Late portrait (L10, large, iridium, renown)", 10, CanvasKind.Large, 100, PaintingRules.PortraitSubjectPrice, 900, PaintingRules.Iridium, Long: true, Gallerist: true, Renown: true, CanvasCost: 200),
            new Case("  ...consigned", 10, CanvasKind.Large, 100, PaintingRules.PortraitSubjectPrice, 900, PaintingRules.Iridium, Long: true, Gallerist: true, Renown: true, CanvasCost: 200, Sale: "consign"),
            new Case("  ...a new sitter, Masterpiece on linen", 10, CanvasKind.Large, 100, PaintingRules.PortraitSubjectPrice, 900, PaintingRules.Iridium, Long: true, Gallerist: true, Renown: true, TimesBefore: 0, Masterpiece: true, CanvasCost: 1000, Sale: "consign"),
            new Case("  ...and varnished to a gallery finish", 10, CanvasKind.Large, 100, PaintingRules.PortraitSubjectPrice, 900, PaintingRules.Iridium, Long: true, Gallerist: true, Renown: true, TimesBefore: 0, Masterpiece: true, Finish: "gallery", CanvasCost: 1000, Sale: "consign", Extras: 60),
        };

        Console.WriteLine($"{"Painting",-52} {"hours",5} {"sells",7} {"costs",6} {"per hour",9}");
        foreach (var c in cases)
        {
            int size = PaintingRules.CanvasSize(c.Kind);
            float price = PaintingRules.Value(size, c.Difficulty, c.SubjectPrice, c.Level);
            if (c.Long) price *= PaintingRules.PortraitValue;
            if (c.Gallerist) price *= Market.GalleristPrice;
            price *= Market.Novelty(c.TimesBefore > 0, 0);
            if (c.Renown) price *= Market.RenownValue;
            if (c.Masterpiece) price *= PaintingRules.MasterpieceValue;
            if (c.Finish != null) price *= Market.Finishes.First(f => f.Key == c.Finish).Value;
            price *= Market.QualityPrice(c.Quality);
            price *= c.Sale switch { "pierre" => 1f, "consign" => Market.ConsignPremium, _ => Market.ArtShopPremium };
            float costs = c.CanvasCost + c.Cells * PaintPerCell + c.Extras;
            int hours = Hours(c);
            Console.WriteLine($"{c.Name,-52} {hours,5} {price,7:0} {costs,6:0} {(price - costs) / hours,9:0}");
        }

        Console.WriteLine();
        foreach (var (what, perHour) in Benchmarks)
            Console.WriteLine($"{what,-52} {"",5} {"",7} {"",6} {perHour,9}");
        Console.WriteLine($"{"Iridium ancient fruit wine (cask, passive), each",-52} {"",5} {3300,7}");
    }
}
