using System;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace ArtStudio.Automate;

// Automate support for Pelican Palette's Varnishing Cabinet, in a DLL of
// its own (1.1.2). SMAPI rewrites a mod's DLL when it loads it, and that
// needs every assembly the DLL refers to, so code using Automate's types
// inside ArtStudio.dll stopped the whole mod loading without Automate. This
// DLL is never referenced by ArtStudio.dll: Pelican Palette loads it itself,
// only when Automate is installed, and hands it the cabinet's behaviour as
// plain functions, so it doesn't refer to Pelican Palette either.
//
// The cabinet works like a cask: connected to chests, it takes an input (an
// unvarnished painting) and fuel (a tin of Varnish), and gives the result
// back once it's done (fully cured).
public static class AutomateBridge
{
    public const string AutomateId = "Pathoschild.Automate";

    public sealed class Machine
    {
        public string TypeId { get; init; } = "";
        public Func<SObject, bool> Is { get; init; } = _ => false;
        public Func<SObject, bool> IsEmpty { get; init; } = _ => true;
        public Func<SObject, bool> IsDone { get; init; } = _ => false;
        public Func<SObject, Item?> Output { get; init; } = _ => null;
        public Action<SObject> Clear { get; init; } = _ => { };
        public Func<Item, bool> IsInput { get; init; } = _ => false;
        public Func<Item, bool> IsFuel { get; init; } = _ => false;
        public Action<SObject, Item> Start { get; init; } = (_, _) => { };
    }

    // Called by Pelican Palette (by reflection) once the game has launched.
    public static bool Register(IModHelper helper, IMonitor monitor, Machine machine)
    {
        var api = helper.ModRegistry.GetApi<IAutomateAPI>(AutomateId);
        if (api == null)
        {
            monitor.Log("Automate is installed but its API isn't available; Varnishing Cabinets won't be automated.", LogLevel.Warn);
            return false;
        }
        api.AddFactory(new Factory(machine));
        return true;
    }

    private sealed class Factory : IAutomationFactory
    {
        private readonly Machine _machine;

        public Factory(Machine machine) => _machine = machine;

        public IAutomatable? GetFor(SObject obj, GameLocation location, in Vector2 tile) =>
            _machine.Is(obj) ? new CaskLike(_machine, obj, location, tile) : null;

        public IAutomatable? GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile) => null;

        public IAutomatable? GetFor(Building building, GameLocation location, in Vector2 tile) => null;

        public IAutomatable? GetForTile(GameLocation location, in Vector2 tile) => null;
    }

    private sealed class CaskLike : IMachine
    {
        private readonly Machine _machine;
        private readonly SObject _obj;

        public CaskLike(Machine machine, SObject obj, GameLocation location, Vector2 tile)
        {
            _machine = machine;
            _obj = obj;
            Location = location;
            TileArea = new Rectangle((int)tile.X, (int)tile.Y, 1, 1);
        }

        public string MachineTypeID => _machine.TypeId;

        public GameLocation Location { get; }

        public Rectangle TileArea { get; }

        public MachineState GetState() =>
            _machine.IsEmpty(_obj) ? MachineState.Empty
            : _machine.IsDone(_obj) ? MachineState.Done
            : MachineState.Processing;

        public ITrackedStack? GetOutput() =>
            _machine.IsDone(_obj) && _machine.Output(_obj) is Item done
                ? new TrackedItem(done).OnEmpty((_, _) => _machine.Clear(_obj))
                : null;

        public bool SetInput(IStorage input)
        {
            if (!_machine.IsEmpty(_obj)
                || !input.TryGetIngredient(stack => _machine.IsInput(stack.Sample), 1, out var item)
                || !input.TryGetIngredient(stack => _machine.IsFuel(stack.Sample), 1, out var fuel))
                return false;
            var taken = item.Take();
            if (taken == null) return false;
            fuel.Reduce();
            _machine.Start(_obj, taken);
            return true;
        }
    }
}
