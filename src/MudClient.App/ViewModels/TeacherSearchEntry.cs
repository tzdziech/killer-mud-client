using MudClient.App.Models;

namespace MudClient.App.ViewModels;

/// <summary>
/// One teacher in the closed, autocompleting list behind the map's "Szukaj..." dialog (see
/// <see cref="MapViewModel.TeacherSearchEntries"/>). <see cref="SearchText"/> is what the
/// dialog's AutoCompleteBox actually filters against — the teacher's name plus every skill/trick
/// they teach and its range/requirement, so typing a spell or skill name (not just a teacher's
/// name) surfaces the right teacher too.
/// </summary>
public sealed record TeacherSearchEntry(string Name, string SearchText);
