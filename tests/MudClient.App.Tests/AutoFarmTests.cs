using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.Core.Map;
using Xunit;

namespace MudClient.App.Tests;

/// <summary>Covers the auto-farm engine's observable behavior on <see cref="MainWindowViewModel"/>
/// — pure decision logic (thresholds, next-room picking) is covered separately by
/// HealthRecoveryPolicyTests/FarmTraversalPlannerTests in MudClient.Core.Tests. Reaches private
/// state via reflection, same pattern as AutowalkArrivalTests.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class AutoFarmTests
{
    private static MainWindowViewModel CreateViewModel(out string directory)
    {
        directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_AutoFarmTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return new MainWindowViewModel(settingsService: new AppSettingsService(directory));
    }

    private static void InvokePrivate(MainWindowViewModel viewModel, string methodName, params object?[] args) =>
        typeof(MainWindowViewModel).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(viewModel, args);

    private static void SetPrivateField(MainWindowViewModel viewModel, string fieldName, object? value) =>
        typeof(MainWindowViewModel).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(viewModel, value);

    [AvaloniaFact]
    public async Task StartAutoFarmCommand_NoRegionDefined_CannotExecute()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            Assert.False(viewModel.StartAutoFarmCommand.CanExecute(null));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task StartAutoFarm_NoRegionDefined_ShowsErrorToastAndStaysInactive()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            InvokePrivate(viewModel, "StartAutoFarm");

            Assert.False(viewModel.IsAutoFarmActive);
            Assert.Contains(viewModel.Toasts, t => t.Text.Contains("zaznacz obszar farmy"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AutoFarmHpThresholdPercent_OutOfRange_IsClamped()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_AutoFarmTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(settingsService: new AppSettingsService(directory));

        try
        {
            viewModel.AutoFarmHpThresholdPercent = 999;
            Assert.Equal(ProfileData.MaxAutoFarmHpThresholdPercent, viewModel.AutoFarmHpThresholdPercent);

            viewModel.AutoFarmHpThresholdPercent = -50;
            Assert.Equal(ProfileData.MinAutoFarmHpThresholdPercent, viewModel.AutoFarmHpThresholdPercent);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static MapRoom CreateRoom(int id, string vnum) => new()
    {
        Id = id,
        AreaId = 1,
        Coordinates = new MapCoordinates(0, 0, 0),
        UserData = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["vnum"] = System.Text.Json.JsonSerializer.SerializeToElement(vnum),
        },
    };

    /// <summary>Arranges a one-step autowalk about to arrive at "999" (mirroring
    /// AutowalkArrivalTests.ArriveAtDestination), then reports that room change.</summary>
    private static void ArriveAtDestination(MainWindowViewModel viewModel)
    {
        var from = CreateRoom(998, "998");
        var to = CreateRoom(999, "999");
        SetPrivateField(viewModel, "_autowalkPath", new MapPath
        {
            From = from,
            To = to,
            Steps = [new MapPathStep("north", to)],
            TotalCost = 1,
        });
        SetPrivateField(viewModel, "_autowalkStep", 0);
        SetPrivateField(viewModel, "_autowalkTargetName", "Cel");

        InvokePrivate(viewModel, "OnAutowalkLocationChanged", "999");

        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public async Task Arrival_WhileAutoFarmActive_SkipsRestOnArrivalEvenWhenEnabled()
    {
        var viewModel = CreateViewModel(out var directory);
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);

        try
        {
            Assert.True(viewModel.AutowalkRestOnArrivalEnabled);
            SetPrivateField(viewModel, "_autoFarmActive", true);

            ArriveAtDestination(viewModel);

            Assert.DoesNotContain(output, line => line.Contains("> rest"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task Arrival_WhileAutoFarmActiveButRegionCleared_ContinuesFarmAndStopsWithToast()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            SetPrivateField(viewModel, "_autoFarmActive", true);
            SetPrivateField(viewModel, "_autoFarmRegion", null);

            ArriveAtDestination(viewModel);

            // CompleteAutowalkArrival must have called ContinueAutoFarm, which (no region) stops
            // the farm with its own toast — proving the arrival hook actually fired.
            Assert.False(viewModel.IsAutoFarmActive);
            Assert.Contains(viewModel.Toasts, t => t.Text.Contains("obszar nie jest już zdefiniowany"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task Arrival_WhileAutoFarmActiveWithMissingRequiredSpell_TriggersMaintenanceNotTraversal()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            viewModel.AutoFarmRequiredMemorizedSpellsText = "armor";
            SetPrivateField(viewModel, "_autoFarmActive", true);
            // No region defined and no HP data — if this fell through to the traversal branch
            // it would stop with "obszar nie jest już zdefiniowany"; a missing required spell
            // must be caught first instead.
            SetPrivateField(viewModel, "_autoFarmRegion", null);

            ArriveAtDestination(viewModel);

            Assert.True(viewModel.IsAutoFarmActive);
            Assert.Equal("Uzupełniam brakujące zaklęcia — odpoczywam.", viewModel.AutoFarmStatusText);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task StopAutoFarm_WhenNotActive_DoesNothing()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            var toastCountBefore = viewModel.Toasts.Count;

            InvokePrivate(viewModel, "StopAutoFarm", "test");

            Assert.False(viewModel.IsAutoFarmActive);
            Assert.Equal(toastCountBefore, viewModel.Toasts.Count);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }
}
