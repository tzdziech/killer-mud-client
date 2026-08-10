namespace MudClient.App.ViewModels;

/// <summary>
/// One teacher or spellbook mob in the closed, autocompleting list behind the map's "Szukaj..."
/// dialog (see <see cref="MapViewModel.SearchEntries"/>). <see cref="SearchText"/> is what the
/// dialog's AutoCompleteBox actually filters against — for a teacher, their name plus every
/// skill/trick they teach and its range; for a spellbook mob, its name plus every spell its book
/// teaches — so typing a spell/skill name (not just the teacher's or mob's own name) still
/// surfaces the right entry.
/// </summary>
public sealed record MapSearchEntry(string Name, string SearchText);
