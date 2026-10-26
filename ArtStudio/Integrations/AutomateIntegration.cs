using System;
using System.IO;
using System.Reflection;
using ArtStudio.Painting;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace ArtStudio.Integrations;

// Automate (1.1): connected to chests, a Varnishing Cabinet takes an
// unvarnished painting and a tin of Varnish from them, and puts the painting
// back once it's fully cured (the gallery finish), as Automate does with a
// cask. The Pigment Mill is a plain Data/Machines machine, which Automate
// already handles.
//
// The code that uses Automate's types is in ArtStudio.Automate.dll, beside
// this mod's DLL (1.1.2): SMAPI couldn't load ArtStudio.dll at all without
// Automate while it referred to Automate. That DLL is loaded only when
// Automate is installed, and given the cabinet's behaviour as functions.
internal static class AutomateIntegration
{
    public const string ModId = "Pathoschild.Automate";
    private const string BridgeFile = "ArtStudio.Automate.dll";

    public static void Register(IModHelper helper, IMonitor monitor)
    {
        try
        {
            var assembly = Assembly.LoadFrom(Path.Combine(helper.DirectoryPath, BridgeFile));
            var bridge = assembly.GetType("ArtStudio.Automate.AutomateBridge", throwOnError: true)!;
            var machineType = bridge.GetNestedType("Machine")!;
            var machine = Activator.CreateInstance(machineType)!;
            void Set(string name, object value) => machineType.GetProperty(name)!.SetValue(machine, value);
            Set("TypeId", Varnish.QualifiedCabinetId);
            Set("Is", (Func<SObject, bool>)Varnish.IsCabinet);
            Set("IsEmpty", (Func<SObject, bool>)(cabinet => cabinet.heldObject.Value == null));
            Set("IsDone", (Func<SObject, bool>)Varnish.IsCured);
            Set("Output", (Func<SObject, Item?>)Varnish.Peek);
            Set("Clear", (Action<SObject>)Varnish.Clear);
            Set("IsInput", (Func<Item, bool>)(item => Varnish.CanVarnish(item)));
            Set("IsFuel", (Func<Item, bool>)(item => Varnish.IsVarnish(item)));
            Set("Start", (Action<SObject, Item>)Varnish.Start);
            bool added = (bool)bridge.GetMethod("Register")!.Invoke(null, new[] { helper, monitor, machine })!;
            if (added)
                monitor.Log("Varnishing Cabinets work with Automate.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't set up Varnishing Cabinets for Automate (everything else works): {ex}", LogLevel.Warn);
        }
    }
}
