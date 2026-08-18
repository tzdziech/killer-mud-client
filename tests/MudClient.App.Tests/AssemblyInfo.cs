using Xunit;

// Avalonia's headless test platform keeps per-thread UI state (Dispatcher, Compositor).
// xUnit runs test collections (i.e. test classes) in parallel by default, so several
// headless sessions initialize the Avalonia platform on different threads at once — which
// intermittently crashes init with "The calling thread cannot access this object because a
// different thread owns it". Serialize the assembly so UI tests never run concurrently.
//
// Tried scoping this down to just the "Avalonia UI" collection (see AvaloniaUiCollection
// below) so the ~80% of this assembly that's plain unit tests could run in parallel — that
// made a full run livelock for 80+ minutes, pegging every core, instead of the ~2 minutes
// this blanket setting takes. Whatever global state Avalonia's headless platform touches
// isn't safe to run alongside *any* concurrently-executing collection, not just another
// Avalonia one. Left as assembly-wide until that's understood well enough to relax safely.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MudClient.App.Tests;

// Keep every Avalonia test in one non-parallel collection as an explicit guard against accidental
// concurrent platform access. Per-test Avalonia isolation still requires each test to close every
// window it opens before its application and compositor are torn down. Apply
// [Collection(AvaloniaUiCollection.Name)] to every class using [AvaloniaFact]/[AvaloniaTheory].
[CollectionDefinition(Name)]
public sealed class AvaloniaUiCollection
{
    public const string Name = "Avalonia UI";
}
