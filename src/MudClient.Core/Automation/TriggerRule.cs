using System.Text.RegularExpressions;

namespace MudClient.Core.Automation;

public sealed class TriggerRule
{
    public TriggerRule(string name, string pattern, string commandTemplate, bool enabled = true, bool isScript = false)
    {
        Name = name;
        Pattern = pattern;
        CommandTemplate = commandTemplate;
        Enabled = enabled;
        IsScript = isScript;
        Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
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

    internal Regex Regex { get; }
}
