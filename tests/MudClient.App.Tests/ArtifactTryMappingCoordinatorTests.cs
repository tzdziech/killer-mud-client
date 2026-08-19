using System.Text.Json;
using MudClient.App.Models;
using MudClient.App.Services;

namespace MudClient.App.Tests;

public sealed class ArtifactTryMappingCoordinatorTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "KillerMudClient_MapujTry_" + Guid.NewGuid().ToString("N"));

    public ArtifactTryMappingCoordinatorTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task RunAsync_SendsTryOneThroughCountInOrder()
    {
        var coordinator = new ArtifactTryMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));
        var sent = new List<string>();

        Task Send(string command, CancellationToken cancellationToken)
        {
            sent.Add(command);
            if (command.Length > 0)
            {
                coordinator.TryCaptureLine($"Wynik dla: {command}");
                coordinator.ObserveText("<418/488hp 90/100mv> ");
            }

            return Task.CompletedTask;
        }

        var captured = await coordinator.RunAsync(
            3, Send, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["try 1", "try 2", "try 3"], sent);
        Assert.Equal([1, 2, 3], captured.Select(entry => entry.Number));
        Assert.Equal("Wynik dla: try 2", captured[1].RawText);
        Assert.False(coordinator.IsCapturing);
    }

    [Fact]
    public async Task RunAsync_OnEntryCaptured_FiresAfterEachTry()
    {
        var coordinator = new ArtifactTryMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));

        Task Send(string command, CancellationToken cancellationToken)
        {
            coordinator.TryCaptureLine($"Opis: {command}");
            coordinator.ObserveText("<418/488hp 90/100mv> ");
            return Task.CompletedTask;
        }

        var snapshots = new List<int>();
        await coordinator.RunAsync(
            2,
            Send,
            cancellationToken: TestContext.Current.CancellationToken,
            onEntryCaptured: (mappedSoFar, _) =>
            {
                snapshots.Add(mappedSoFar.Count);
                return Task.CompletedTask;
            });

        Assert.Equal([1, 2], snapshots);
    }

    [Fact]
    public async Task RunAsync_PagesThroughPagerPromptsBeforeMovingToNextNumber()
    {
        var coordinator = new ArtifactTryMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));
        var sent = new List<string>();

        Task Send(string command, CancellationToken cancellationToken)
        {
            sent.Add(command);
            if (command == "try 1")
            {
                coordinator.TryCaptureLine("Strona 1 z opisu.");
                coordinator.TryCaptureLine("[Nacisnij Enter aby kontynuowac]");
                coordinator.ObserveText("> ");
            }
            else if (command.Length == 0)
            {
                coordinator.TryCaptureLine("Strona 2 z opisu.");
                coordinator.ObserveText("<418/488hp 90/100mv> ");
            }
            else if (command == "try 2")
            {
                coordinator.TryCaptureLine("Krotki opis.");
                coordinator.ObserveText("<418/488hp 90/100mv> ");
            }

            return Task.CompletedTask;
        }

        var captured = await coordinator.RunAsync(
            2, Send, cancellationToken: TestContext.Current.CancellationToken);

        var first = Assert.Single(captured, entry => entry.Number == 1);
        Assert.Equal("Strona 1 z opisu.\nStrona 2 z opisu.", first.RawText);
        Assert.Equal(["try 1", string.Empty, "try 2"], sent);
    }

    [Fact]
    public async Task RunAsync_CancellationMidLoop_PreservesEntriesCapturedSoFar()
    {
        var coordinator = new ArtifactTryMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));
        using var cancellation = new CancellationTokenSource();
        var snapshots = new List<IReadOnlyList<ArtifactTryEntry>>();

        Task Send(string command, CancellationToken token)
        {
            if (command == "try 2")
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            }

            coordinator.TryCaptureLine($"Opis: {command}");
            coordinator.ObserveText("<418/488hp 90/100mv> ");
            return Task.CompletedTask;
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.RunAsync(
            3,
            Send,
            cancellationToken: cancellation.Token,
            onEntryCaptured: (mappedSoFar, _) =>
            {
                snapshots.Add(mappedSoFar.ToArray());
                return Task.CompletedTask;
            }));

        var snapshot = Assert.Single(snapshots);
        Assert.Single(snapshot);
        Assert.Equal(1, snapshot[0].Number);
        Assert.False(coordinator.IsCapturing);
    }

    [Fact]
    public async Task RunAsync_TimesOutWithoutAnyResponseAndReleasesCapture()
    {
        var coordinator = new ArtifactTryMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAsync<TimeoutException>(() => coordinator.RunAsync(
            1,
            (_, _) => Task.CompletedTask,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.False(coordinator.IsCapturing);
    }

    [Fact]
    public async Task Store_SavesAtomicallyAndLoadsGeneratedJson()
    {
        var path = Path.Combine(_directory, "artifact-try.json");
        var store = new ArtifactTryStore(path);
        var document = new ArtifactTryDocument
        {
            Entries =
            [
                new ArtifactTryEntry
                {
                    Number = 1,
                    RawText = "Probujesz artefakt numer 1.",
                    CapturedAt = DateTimeOffset.Parse("2026-08-13T12:00:00Z"),
                },
            ],
        };

        await store.SaveAsync(document, TestContext.Current.CancellationToken);
        var loaded = store.Load();

        Assert.Equal(1, Assert.Single(loaded.Entries).Number);
        Assert.False(File.Exists(path + ".tmp"));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(1, json.RootElement.GetProperty("entries")[0].GetProperty("number").GetInt32());
    }

    [Fact]
    public void Store_WithoutUserFile_LoadsBundledArtifactSnapshot()
    {
        var store = new ArtifactTryStore(Path.Combine(_directory, "nie-istnieje.json"));

        var document = store.Load();

        Assert.NotEmpty(document.Entries);
    }
}
