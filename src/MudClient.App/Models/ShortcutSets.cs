using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MudClient.App.Models;

public sealed partial class GroupSpellSetEntry : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string name = string.Empty;
    public ObservableCollection<GroupSpellShortcut> Spells { get; } = [];
}

public sealed partial class ActionSetEntry : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string name = string.Empty;
    public ObservableCollection<OffensiveActionShortcut> OffensiveActions { get; } = [];
    public ObservableCollection<CustomCommandShortcut> CustomCommands { get; } = [];
}
