using MoonSharp.Interpreter;

namespace MudClient.Core.Automation;

public sealed class TriggerEngine
{
    private readonly List<TriggerRule> _rules = [];

    /// <summary>
    /// When set, a trigger command matching <c>alias(regexAliasa)</c> is not
    /// sent verbatim. Instead the text inside the parentheses is run through
    /// this <see cref="AliasEngine"/>, and whatever the matching alias
    /// expands to is emitted in its place. Lets a trigger invoke an existing
    /// alias instead of duplicating its command template.
    /// </summary>
    public AliasEngine? Aliases { get; set; }

    /// <summary>Runs a rule's <see cref="TriggerRule.CommandTemplate"/> as Lua source when
    /// <see cref="TriggerRule.IsScript"/> is true. Null means script rules simply produce no
    /// commands (defensive default — the host always sets this).</summary>
    public LuaScriptEngine? Lua { get; set; }

    /// <summary>Raised when a script rule's Lua throws (syntax or runtime error), with
    /// (ruleName, message) — that rule contributes no commands for this evaluation, but the rest
    /// of the trigger list still runs.</summary>
    public event Action<string, string>? ScriptError;

    public IReadOnlyList<TriggerRule> Rules => _rules;

    public void Add(TriggerRule rule) => _rules.Add(rule);

    public void Clear() => _rules.Clear();

    /// <summary>
    /// Evaluates all enabled trigger rules against <paramref name="line"/>.
    /// Equivalent to <c>Evaluate(line, null)</c>.
    /// </summary>
    public IReadOnlyList<string> Evaluate(string line) =>
        Evaluate(line, separator: null);

    /// <summary>
    /// Evaluates all enabled trigger rules against <paramref name="line"/>,
    /// also splitting the command template on <paramref name="separator"/>
    /// when it is non-empty (in addition to newlines).
    /// </summary>
    public IReadOnlyList<string> Evaluate(string line, string? separator)
    {
        var commands = new List<string>();

        foreach (var rule in _rules)
        {
            if (!rule.Enabled)
            {
                continue;
            }

            var match = rule.Regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            IReadOnlyList<string> matched;
            if (rule.IsScript)
            {
                try
                {
                    matched = Lua?.Run(rule.CommandTemplate, line, match) ?? [];
                }
                catch (InterpreterException exception)
                {
                    ScriptError?.Invoke(rule.Name, exception.DecoratedMessage ?? exception.Message);
                    continue;
                }
            }
            else
            {
                var text = match.Result(rule.CommandTemplate);
                matched = CommandStacker.Split(text, separator);
            }

            foreach (var command in matched)
            {
                commands.AddRange(ExpandAliasCall(command, separator));
            }
        }

        return commands;
    }

    private IReadOnlyList<string> ExpandAliasCall(string command, string? separator)
    {
        if (Aliases is null)
        {
            return [command];
        }

        return Aliases.ProcessAliasCall(command, separator);
    }
}
