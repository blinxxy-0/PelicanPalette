using ArtStudio.Painting;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace ArtStudio.Integrations;

// Automate (1.1): connected to chests, a Varnishing Cabinet takes an
// unvarnished painting and a tin of Varnish from them, and puts the painting
// back once it's fully cured (the gallery finish), as Automate does with a
// cask. The Pigment Mill is a plain Data/Machines machine, which Automate
// already handles.
//
// Only touched when Automate is installed: this class is the one place that
// uses Automate's types.
internal static class AutomateIntegration
{
    public const string ModId = "Pathoschild.Automate";

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        var api = helper.ModRegistry.GetApi<IAutomateAPI>(ModId);
        if (api == null)
        {
            monitor.Log("Automate is installed but its API isn't available; Varnishing Cabinets won't be automated.", LogLevel.Warn);
            return;
        }
        api.AddFactory(new Factory());
        monitor.Log("Varnishing Cabinets work with Automate.", LogLevel.Trace);
    }

    private sealed class Factory : IAutomationFactory
    {
        public IAutomatable? GetFor(SObject obj, GameLocation location, in Vector2 tile) =>
            Varnish.IsCabinet(obj) ? new CabinetMachine(obj, location, tile) : null;

        public IAutomatable? GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile) => null;

        public IAutomatable? GetFor(Building building, GameLocation location, in Vector2 tile) => null;

        public IAutomatable? GetForTile(GameLocation location, in Vector2 tile) => null;
    }

    private sealed class CabinetMachine : IMachine
    {
        private readonly SObject _cabinet;

        public CabinetMachine(SObject cabinet, GameLocation location, Vector2 tile)
        {
            _cabinet = cabinet;
            Location = location;
            TileArea = new Rectangle((int)tile.X, (int)tile.Y, 1, 1);
        }

        public string MachineTypeID => Varnish.QualifiedCabinetId;

        public GameLocation Location { get; }

        public Rectangle TileArea { get; }

        public MachineState GetState() =>
            _cabinet.heldObject.Value == null ? MachineState.Empty
            : Varnish.IsCured(_cabinet) ? MachineState.Done
            : MachineState.Processing;

        public ITrackedStack? GetOutput() =>
            Varnish.IsCured(_cabinet) && Varnish.Peek(_cabinet) is SObject cured
                ? new TrackedItem(cured).OnEmpty((_, _) => Varnish.Clear(_cabinet))
                : null;

        public bool SetInput(IStorage input)
        {
            if (_cabinet.heldObject.Value != null
                || !input.TryGetIngredient(stack => Varnish.CanVarnish(stack.Sample), 1, out var painting)
                || !input.TryGetIngredient(stack => Varnish.IsVarnish(stack.Sample), 1, out var tin))
                return false;
            var item = painting.Take();
            if (item == null) return false;
            tin.Reduce();
            Varnish.Start(_cabinet, item);
            return true;
        }
    }
}
