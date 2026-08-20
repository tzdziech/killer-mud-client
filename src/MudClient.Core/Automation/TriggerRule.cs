using System.Text.RegularExpressions;

namespace MudClient.Core.Automation;

public sealed class TriggerRule
{
    public TriggerRule(
        string name, string pattern, string commandTemplate, bool enabled = true, bool isScript = false,
        bool playSoundOnMatch = false)
    {
        Name = name;
        Pattern = pattern;
        CommandTemplate = commandTemplate;
        Enabled = enabled;
        IsScript = isScript;
        PlaySoundOnMatch = playSoundOnMatch;
        // Patterns can arrive from an imported trigger pack, not just the local user — a
        // pathological pattern (catastrophic backtracking) must not be able to hang trigger
        // evaluation indefinitely for every incoming server line. TriggerEngine treats a
        // timeout as "no match".
        Regex = new Regex(
            pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500));
    }

    public string Name { get; }

    public string Pattern { get; }

    /// <summary>A "$1"-style command template when <see cref="IsScript"/> is false; Lua source
    /// (run via <see cref="LuaScriptEngine"/>) when it's true.</summary>
    public string CommandTemplate { get; }

    public bool Enabled { get; set; }

    /// <summary>True when <see cref="CommandTemplate"/> is Lua source instead of a command
    /// template — see <see cref="TriggerEngine.Lua"/>.</summary>
    public bool IsScript { get; }

    /// <summary>True to have <see cref="TriggerEngine.RuleMatched"/> fire for this rule whenever
    /// it matches — the host app plays a notification sound in response.</summary>
    public bool PlaySoundOnMatch { get; }

    internal Regex Regex { get; }
}
