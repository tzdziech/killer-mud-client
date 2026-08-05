using System.Text.RegularExpressions;

namespace MudClient.Core.Automation;

public sealed class AliasRule
{
    public AliasRule(string name, string pattern, string replacement, bool enabled = true)
    {
        Name = name;
        Pattern = pattern;
        Replacement = replacement;
        Enabled = enabled;
        // Patterns can arrive from an imported alias pack, not just the local user — a
        // pathological pattern (catastrophic backtracking) must not be able to hang command
        // sending indefinitely. AliasEngine treats a timeout as "no match".
        Regex = new Regex(
            pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500));
    }

    public string Name { get; }

    public string Pattern { get; }

    public string Replacement { get; }

    public bool Enabled { get; set; }

    internal Regex Regex { get; }
}
