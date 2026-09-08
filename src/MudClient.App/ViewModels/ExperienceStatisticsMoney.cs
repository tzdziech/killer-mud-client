using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using MudClient.App.Models;

namespace MudClient.App.ViewModels;

public sealed partial class ExperienceStatisticsViewModel
{
    private const long CopperPerSilver = 60;
    private const long CopperPerGold = 15 * CopperPerSilver;
    private const long CopperPerMithril = 12 * CopperPerGold;

    public ObservableCollection<MoneyBreakdownRow> SessionMoneyIncomeBreakdown { get; } = [];
    public ObservableCollection<MoneyBreakdownRow> SessionMoneyExpenseBreakdown { get; } = [];

    public long SessionMoneyIncome => _session.MoneyEvents.Where(IsIncome).Sum(item => item.CopperValue);
    public long SessionMoneyExpense => _session.MoneyEvents.Where(item => !IsIncome(item)).Sum(item => item.CopperValue);
    public long SessionMoneyBalance => SessionMoneyIncome - SessionMoneyExpense;
    public string SessionMoneyIncomeText => FormatMoney(SessionMoneyIncome);
    public string SessionMoneyExpenseText => FormatMoney(SessionMoneyExpense);
    public string SessionMoneyBalanceText => FormatSignedMoney(SessionMoneyBalance);

    public bool ObserveMoneyLine(string line, DateTimeOffset? when = null)
    {
        var text = Regex.Replace(line, "\\x1B\\[[0-9;?]*[ -/]*[@-~]", string.Empty).Trim();
        if (!TryClassifyMoneyEvent(text, out var kind) || !TryReadMoney(text, out var copperValue))
        {
            return false;
        }

        _session.MoneyEvents.Add(new MoneyEventData
        {
            Kind = kind,
            CopperValue = copperValue,
            Description = ReadTransactionDescription(text, kind),
            When = when ?? DateTimeOffset.Now,
        });
        _session.LastUpdatedAt = when ?? DateTimeOffset.Now;
        RefreshMoney();
        return true;
    }

    private void ResetMoneyRuntime() => RefreshMoney();

    private void RefreshMoney()
    {
        RebuildMoneyBreakdown(SessionMoneyIncomeBreakdown, _session.MoneyEvents.Where(IsIncome));
        RebuildMoneyBreakdown(SessionMoneyExpenseBreakdown, _session.MoneyEvents.Where(item => !IsIncome(item)));
        OnPropertyChanged(nameof(SessionMoneyIncome));
        OnPropertyChanged(nameof(SessionMoneyExpense));
        OnPropertyChanged(nameof(SessionMoneyBalance));
        OnPropertyChanged(nameof(SessionMoneyIncomeText));
        OnPropertyChanged(nameof(SessionMoneyExpenseText));
        OnPropertyChanged(nameof(SessionMoneyBalanceText));
    }

    private static void RebuildMoneyBreakdown(ObservableCollection<MoneyBreakdownRow> target,
        IEnumerable<MoneyEventData> events)
    {
        target.Clear();
        foreach (var group in events.GroupBy(item => item.Kind)
                     .OrderByDescending(group => group.Sum(item => item.CopperValue)))
        {
            target.Add(new MoneyBreakdownRow(MoneyKindLabel(group.Key),
                FormatMoney(group.Sum(item => item.CopperValue)), group.Count()));
        }
    }

    private static bool TryClassifyMoneyEvent(string text, out MoneyEventKind kind)
    {
        if (Regex.IsMatch(text, "^Naliczyl(?:es|as) ", RegexOptions.IgnoreCase)) kind = MoneyEventKind.Loot;
        else if (text.StartsWith("Sprzedajesz ", StringComparison.OrdinalIgnoreCase)) kind = MoneyEventKind.Sale;
        else if (text.StartsWith("Kupujesz ", StringComparison.OrdinalIgnoreCase)) kind = MoneyEventKind.Purchase;
        else if (text.StartsWith("Naprawa kosztowala ", StringComparison.OrdinalIgnoreCase)) kind = MoneyEventKind.Repair;
        else if (text.Contains("daje ci ", StringComparison.OrdinalIgnoreCase) &&
                 text.Contains(" w zamian za ", StringComparison.OrdinalIgnoreCase)) kind = MoneyEventKind.Training;
        else { kind = default; return false; }
        return true;
    }

    private static bool TryReadMoney(string text, out long copperValue)
    {
        copperValue = 0;
        var matches = Regex.Matches(text,
            "(?<![0-9])([0-9]+)\\s+(miedzian\\w*|srebrn\\w*|zlot\\w*|mithril\\w*)",
            RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            var count = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var denomination = match.Groups[2].Value;
            var multiplier = denomination.StartsWith("mithril", StringComparison.OrdinalIgnoreCase)
                ? CopperPerMithril
                : denomination.StartsWith("zlot", StringComparison.OrdinalIgnoreCase)
                    ? CopperPerGold
                    : denomination.StartsWith("srebr", StringComparison.OrdinalIgnoreCase)
                        ? CopperPerSilver
                        : 1;
            checked { copperValue += count * multiplier; }
        }
        return matches.Count > 0;
    }

    private static string? ReadTransactionDescription(string text, MoneyEventKind kind)
    {
        if (kind is MoneyEventKind.Loot or MoneyEventKind.Repair) return null;
        var marker = kind == MoneyEventKind.Training ? " w zamian za " : " za ";
        var end = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (end < 0) return null;
        var start = kind is MoneyEventKind.Sale or MoneyEventKind.Purchase
            ? text.IndexOf(' ') + 1
            : 0;
        return text[start..end].Trim();
    }

    public static string FormatMoney(long copperValue)
    {
        var value = Math.Abs(copperValue);
        var mithril = value / CopperPerMithril; value %= CopperPerMithril;
        var gold = value / CopperPerGold; value %= CopperPerGold;
        var silver = value / CopperPerSilver;
        var copper = value % CopperPerSilver;
        var parts = new List<string>();
        if (mithril > 0) parts.Add($"{mithril:N0}m");
        if (gold > 0) parts.Add($"{gold:N0}g");
        if (silver > 0) parts.Add($"{silver:N0}s");
        if (copper > 0 || parts.Count == 0) parts.Add($"{copper:N0}c");
        return string.Join(" ", parts);
    }

    private static string FormatSignedMoney(long copperValue) =>
        copperValue switch
        {
            > 0 => $"+{FormatMoney(copperValue)}",
            < 0 => $"-{FormatMoney(copperValue)}",
            _ => "0c",
        };

    private static bool IsIncome(MoneyEventData item) => item.Kind is MoneyEventKind.Loot or MoneyEventKind.Sale;
    private static string MoneyKindLabel(MoneyEventKind kind) => kind switch
    {
        MoneyEventKind.Loot => "Łupy i monety z ciał",
        MoneyEventKind.Sale => "Sprzedaż",
        MoneyEventKind.Purchase => "Zakupy",
        MoneyEventKind.Repair => "Naprawy",
        MoneyEventKind.Training => "Nauka umiejętności",
        _ => "Inne",
    };
}

public sealed record MoneyBreakdownRow(string Category, string Amount, int Count)
{
    public string CountText => $"Transakcje: {Count:N0}";
}
