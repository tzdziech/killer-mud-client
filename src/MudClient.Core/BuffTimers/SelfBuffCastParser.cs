namespace MudClient.Core.BuffTimers;

public static class SelfBuffCastParser
{
    public static bool TryParse(string command, string? characterName, out string buffName)
    {
        buffName = string.Empty;
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var text = command.Trim();
        var separator = text.IndexOfAny([' ', '\t']);
        if (separator < 0 || !IsCastVerb(text[..separator]))
        {
            return false;
        }

        text = text[(separator + 1)..].TrimStart();
        if (!TryReadSpell(ref text, out var spell) || !TryReadToken(ref text, out var target))
        {
            return false;
        }

        if (text.Length != 0 || !IsSelfTarget(target, characterName))
        {
            return false;
        }

        buffName = NormalizeName(spell);
        return buffName.Length > 0;
    }

    public static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        var open = normalized.IndexOf('(');
        if (open >= 0)
        {
            normalized = normalized[..open].TrimEnd();
        }

        return normalized.StartsWith("mass ", StringComparison.OrdinalIgnoreCase)
            ? normalized[5..].TrimStart().ToLowerInvariant()
            : normalized.ToLowerInvariant();
    }

    private static bool IsCastVerb(string verb) =>
        string.Equals(verb, "cast", StringComparison.OrdinalIgnoreCase)
        || string.Equals(verb, "c", StringComparison.OrdinalIgnoreCase);

    private static bool IsSelfTarget(string target, string? characterName) =>
        string.Equals(target, "self", StringComparison.OrdinalIgnoreCase)
        || string.Equals(target, "siebie", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrWhiteSpace(characterName)
        && string.Equals(target, characterName, StringComparison.OrdinalIgnoreCase);

    private static bool TryReadSpell(ref string text, out string spell)
    {
        spell = string.Empty;
        if (text.Length == 0)
        {
            return false;
        }

        if (text[0] is '\'' or '"')
        {
            var quote = text[0];
            var end = text.IndexOf(quote, 1);
            if (end <= 1)
            {
                return false;
            }

            spell = text[1..end];
            text = text[(end + 1)..].TrimStart();
            return true;
        }

        return TryReadToken(ref text, out spell);
    }

    private static bool TryReadToken(ref string text, out string token)
    {
        token = string.Empty;
        text = text.TrimStart();
        if (text.Length == 0)
        {
            return false;
        }

        var end = text.IndexOfAny([' ', '\t']);
        if (end < 0)
        {
            token = text;
            text = string.Empty;
        }
        else
        {
            token = text[..end];
            text = text[end..].TrimStart();
        }

        return token.Length > 0;
    }
}
