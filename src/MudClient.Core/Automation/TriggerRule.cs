using System.Text.RegularExpressions;

namespace MudClient.Core.Automation;

public sealed class TriggerRule
{
    public TriggerRule(string name, string pattern, string commandTemplate, bool enabled = true)
    {
        Name = name;
        Pattern = pattern;
        CommandTemplate = commandTemplate;
        Enabled = enabled;
        // Patterns can arrive from an imported trigger pack, not just the local user — a
        // pathological pattern (catastrophic backtracking) must not be able to hang trigger
        // evaluation indefinitely for every incoming server line. TriggerEngine treats a
        // timeout as "no match".
        Regex = new Regex(
            pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500));
    }

    public string Name { get; }

    public string Pattern { get; }

    public string CommandTemplate { get; }

    public bool Enabled { get; set; }

    internal Regex Regex { get; }
}
