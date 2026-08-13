using System.Text.Json;
using MudClient.App.Models;
using MudClient.App.Services;

namespace MudClient.App.Tests;

public sealed class AbilityMappingCoordinatorTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "KillerMudClient_Mapuj_" + Guid.NewGuid().ToString("N"));

    public AbilityMappingCoordinatorTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task RunAsync_CapturesEachNameInOrder()
    {
        var coordinator = new AbilityMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));
        var sent = new List<string>();

        Task Send(string command, CancellationToken cancellationToken)
        {
            sent.Add(command);
            if (command == "help kick")
            {
                coordinator.TryCaptureLine("Kick to szybki atak noga.");
                coordinator.TryCaptureLine("Wymagany poziom: 4.");
                coordinator.ObserveText("<418/488hp 90/100mv> ");
            }
            else if (command == "help bless")
            {
                coordinator.TryCaptureLine("Bless blogoslawi cel.");
                coordinator.ObserveText("<418/488hp 90/100mv> ");
            }

            return Task.CompletedTask;
        }

        var captured = await coordinator.RunAsync(
            "Paladyn",
            ["kick", "bless"],
            Send,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, captured.Count);
        var kick = Assert.Single(captured, entry => entry.Name == "kick");
        Assert.Equal("Paladyn", kick.Class);
        Assert.Equal("Kick to szybki atak noga.\nWymagany poziom: 4.", kick.RawHelpText);
        var bless = Assert.Single(captured, entry => entry.Name == "bless");
        Assert.Equal("Bless blogoslawi cel.", bless.RawHelpText);

        Assert.Equal(["help kick", "help bless"], sent);
        Assert.False(coordinator.IsCapturing);
    }

    [Fact]
    public async Task RunAsync_OnEntryCaptured_FiresAfterEachName()
    {
        var coordinator = new AbilityMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));

        Task Send(string command, CancellationToken cancellationToken)
        {
            coordinator.TryCaptureLine($"Opis dla: {command}");
            coordinator.ObserveText("<418/488hp 90/100mv> ");
            return Task.CompletedTask;
        }

        var snapshots = new List<int>();
        await coordinator.RunAsync(
            "Paladyn",
            ["kick", "bless"],
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
    public async Task RunAsync_PagesThroughPagerPromptsBeforeMovingToNextName()
    {
        var coordinator = new AbilityMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));
        var sent = new List<string>();

        Task Send(string command, CancellationToken cancellationToken)
        {
            sent.Add(command);
            if (command == "help holy prayer")
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
            else if (command == "help bless")
            {
                coordinator.TryCaptureLine("Krotki opis.");
                coordinator.ObserveText("<418/488hp 90/100mv> ");
            }

            return Task.CompletedTask;
        }

        var captured = await coordinator.RunAsync(
            "Paladyn",
            ["holy prayer", "bless"],
            Send,
            cancellationToken: TestContext.Current.CancellationToken);

        var prayer = Assert.Single(captured, entry => entry.Name == "holy prayer");
        Assert.Equal("Strona 1 z opisu.\nStrona 2 z opisu.", prayer.RawHelpText);
        Assert.Equal(["help holy prayer", string.Empty, "help bless"], sent);
    }

    [Fact]
    public async Task RunAsync_MudPromptCompletesResponseWithoutWaitingForQuietPeriod()
    {
        var coordinator = new AbilityMappingCoordinator(
            TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(500));

        Task Send(string command, CancellationToken cancellationToken)
        {
            coordinator.ObserveText(
                $"{command}\r\nOpis natychmiastowy.\r\n<418/488hp 90/100mv> ");
            return Task.CompletedTask;
        }

        var captured = await coordinator.RunAsync(
            "Paladyn", ["bless"], Send, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(captured);
    }

    [Fact]
    public async Task RunAsync_CancellationMidLoop_PreservesEntriesCapturedSoFar()
    {
        var coordinator = new AbilityMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(2));
        using var cancellation = new CancellationTokenSource();
        var snapshots = new List<IReadOnlyList<AbilityCaptureEntry>>();

        Task Send(string command, CancellationToken token)
        {
            // Cancelling here (before "help bless" gets its own response) leaves "kick" — whose
            // response already fully landed in the previous loop iteration — captured, while
            // "bless" and "stun" never are.
            if (command == "help bless")
            {
                cancellation.Cancel();
                return Task.CompletedTask;
            }

            coordinator.TryCaptureLine($"Opis: {command}");
            coordinator.ObserveText("<418/488hp 90/100mv> ");
            return Task.CompletedTask;
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.RunAsync(
            "Paladyn",
            ["kick", "bless", "stun"],
            Send,
            cancellationToken: cancellation.Token,
            onEntryCaptured: (mappedSoFar, _) =>
            {
                snapshots.Add(mappedSoFar.ToArray());
                return Task.CompletedTask;
            }));

        var snapshot = Assert.Single(snapshots);
        Assert.Single(snapshot);
        Assert.Equal("kick", snapshot[0].Name);
        Assert.False(coordinator.IsCapturing);
    }

    [Fact]
    public async Task RunAsync_TimesOutWithoutAnyResponseAndReleasesCapture()
    {
        var coordinator = new AbilityMappingCoordinator(
            TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAsync<TimeoutException>(() => coordinator.RunAsync(
            "Paladyn",
            ["kick"],
            (_, _) => Task.CompletedTask,
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.False(coordinator.IsCapturing);
    }

    [Fact]
    public async Task Store_SavesAtomicallyAndLoadsGeneratedJson()
    {
        var path = Path.Combine(_directory, "ability-help.json");
        var store = new AbilityCaptureStore(path);
        var document = new AbilityCaptureDocument
        {
            Entries =
            [
                new AbilityCaptureEntry
                {
                    Name = "kick",
                    Class = "Paladyn",
                    RawHelpText = "Kick to szybki atak.",
                    CapturedAt = DateTimeOffset.Parse("2026-08-13T12:00:00Z"),
                },
            ],
        };

        await store.SaveAsync(document, TestContext.Current.CancellationToken);
        var loaded = store.Load();

        Assert.Equal("kick", Assert.Single(loaded.Entries).Name);
        Assert.False(File.Exists(path + ".tmp"));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal("kick", json.RootElement.GetProperty("entries")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Store_CancelledSave_PreservesPreviousDocument()
    {
        var path = Path.Combine(_directory, "ability-help.json");
        var store = new AbilityCaptureStore(path);
        await store.SaveAsync(new AbilityCaptureDocument
        {
            Entries = [new AbilityCaptureEntry { Name = "kick", Class = "Paladyn" }],
        }, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(
            new AbilityCaptureDocument
            {
                Entries = [new AbilityCaptureEntry { Name = "bless", Class = "Paladyn" }],
            },
            cancellation.Token));

        Assert.Equal("kick", Assert.Single(store.Load().Entries).Name);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Store_WithoutUserFile_ReturnsEmptyDocument()
    {
        var store = new AbilityCaptureStore(Path.Combine(_directory, "nie-istnieje.json"));

        var document = store.Load();

        Assert.Empty(document.Entries);
    }
}
