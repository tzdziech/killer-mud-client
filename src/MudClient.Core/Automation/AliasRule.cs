using System.Text.RegularExpressions;

namespace MudClient.Core.Automation;

public sealed class AliasRule
{
    public AliasRule(string name, string pattern, string replacement, bool enabled = true, bool isScript = false)
    {
        Name = name;
        Pattern = pattern;
        Replacement = replacement;
        Enabled = enabled;
        IsScript = isScript;
        Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    public string Name { get; }

    public string Pattern { get; }

    /// <summary>A "$1"-style replacement template when <see cref="IsScript"/> is false; Lua
    /// source (run via <see cref="LuaScriptEngine"/>) when it's true.</summary>
    public string Replacement { get; }

    public bool Enabled { get; set; }

    /// <summary>True when <see cref="Replacement"/> is Lua source instead of a replacement
    /// template — see <see cref="AliasEngine.Lua"/>.</summary>
    public bool IsScript { get; }

    internal Regex Regex { get; }
}
