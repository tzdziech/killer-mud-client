namespace MudClient.App.Models;

/// <summary>One row of the Wędrowiec skill tree's class-filter checklist — a class name plus
/// whether it's currently one of the browsed/combined specializations. A plain snapshot record
/// (not <c>ObservableObject</c>): the whole list is rebuilt and re-bound whenever the underlying
/// selection changes, so individual rows never need to raise their own change notifications.</summary>
public sealed record AbilityClassOption(string Name, bool IsSelected);
