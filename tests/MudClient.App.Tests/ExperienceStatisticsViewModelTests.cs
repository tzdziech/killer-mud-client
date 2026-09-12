using MudClient.App.Models;
using MudClient.App.ViewModels;
using MudClient.Core.Statistics;

namespace MudClient.App.Tests;

public sealed class ExperienceStatisticsViewModelTests
{
    [Fact]
    public void FormatsSessionDurationWithoutFractionalSeconds()
    {
        Assert.Equal("27h 04m 05s", ExperienceStatisticsViewModel.FormatDuration(
            TimeSpan.FromHours(27) + TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(5.9)));
    }

    [Fact]
    public void PresentsKillAndDamageExperienceInSingleReliableBindingValue()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());
        var when = DateTimeOffset.Now;

        viewModel.ApplyCombatDamage(44, "Ghul", "Agron", isOwnDamage: true, when: when.AddSeconds(-2));
        viewModel.ApplyCombatDamage(22, "Ghul", "Aragorn", isOwnDamage: false, when: when.AddSeconds(-1));

        viewModel.Apply([
            new ExperienceChange(ExperienceChangeKind.Damage, 21, "Ghul", 31, 900, when),
            new ExperienceChange(ExperienceChangeKind.KillReward, 133, "Ghul", 31, 900, when),
        ]);

        Assert.Equal("21 / 133", viewModel.DamageAndKillExperienceText);
        Assert.Equal("44 / 66", viewModel.OwnAndGroupDamageText);
        var mob = Assert.Single(viewModel.Mobs);
        Assert.Equal(154, mob.AverageTotalExperience);
        Assert.Equal(154, mob.LastTotalExperience);
        Assert.Equal(66, mob.LastApproximateHp);
        Assert.Single(mob.TrendPoints);
        Assert.Contains("Ghul", viewModel.StrongestHitDetails);
    }

    [Fact]
    public void CountsDeathsSeparatelyFromLostExperience()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());

        viewModel.Apply([
            new ExperienceChange(ExperienceChangeKind.Death, 0, null, 31, 900, DateTimeOffset.Now),
            new ExperienceChange(ExperienceChangeKind.DeathLoss, 50, null, 31, 950, DateTimeOffset.Now),
        ]);

        Assert.Equal(1, viewModel.DeathCount);
        Assert.Equal(50, viewModel.DeathLoss);
    }

    [Fact]
    public void StrongestHitUsesOnlyOwnDamage()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());
        var when = DateTimeOffset.Now;
        viewModel.ApplyCombatDamage(75, "Ghul", "Agron", true, when);
        viewModel.ApplyCombatDamage(200, "Ghul", "Kultyści", false, when.AddMilliseconds(1));

        Assert.Contains("75", viewModel.StrongestHitDetails);
        Assert.DoesNotContain("200", viewModel.StrongestHitDetails);
    }

    [Fact]
    public void SeparatesExactReceivedHealthFromEstimatedHealingGiven()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());
        var when = DateTimeOffset.Now;
        viewModel.ObserveHealthVitals(300, 700, false, false, 31, when);
        viewModel.ObserveHealthVitals(400, 700, false, false, 31, when.AddMilliseconds(10));
        viewModel.ObserveHealthLine("Norga wymawia slowa, 'cure serious'.", "Agron", 31, when.AddMilliseconds(20));
        viewModel.ObserveHealthLine("Twoje cialo wypelnia lecznicze cieplo, kilka twoich ran goi sie.", "Agron", 31, when.AddMilliseconds(30));
        viewModel.ObserveHealthLine("Wymawiasz slowa, 'cure light'.", "Agron", 31, when.AddSeconds(1));
        viewModel.ObserveHealthLine("Kilka ran Norgi goi sie.", "Agron", 31, when.AddSeconds(1.1));

        Assert.Equal(100, viewModel.SessionHealthRestored);
        Assert.Equal(31, viewModel.SessionHealingGiven);
    }

    [Fact]
    public void RemovesColorMarkersFromHealthBreakdownNames()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());
        var when = DateTimeOffset.Now;
        viewModel.ObserveHealthVitals(100, 100, true, false, 31, when);
        viewModel.ObserveHealthVitals(78, 100, true, false, 31, when.AddMilliseconds(10));
        viewModel.ObserveHealthLine("\u001b[31m{ySz{x{Yak{x{yal{x\u001b[0m lekko rani cie!",
            "Agron", 31, when.AddMilliseconds(20));
        viewModel.ObserveHealthLine("Wymawiasz slowa, 'cure light'.", "Agron", 31, when.AddSeconds(1));
        viewModel.ObserveHealthLine("Kilka ran \u001b[32m{yNor{xgi\u001b[0m goi sie.",
            "Agron", 31, when.AddSeconds(1.1));

        var damage = Assert.Single(viewModel.SessionHealthBreakdown,
            row => row.Category == "Obrażenia — ataki");
        var healing = Assert.Single(viewModel.SessionHealthBreakdown,
            row => row.Category == "Leczenie — udzielone");
        Assert.Equal("Szakal", damage.Source);
        Assert.Equal("Norgi", healing.Target);
        Assert.DoesNotContain('{', damage.Details + healing.Details);
        Assert.DoesNotContain('\u001b', damage.Details + healing.Details);
    }

    [Fact]
    public void SeparatesSessionAndLastCombatDamageByMemberAndType()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());
        var when = DateTimeOffset.Now;

        viewModel.ObserveHealthCombatState(true, when);
        viewModel.ApplyCombatDamage(75, "Gwardzista", "Agron", true,
            when.AddMilliseconds(10), "Ciecie");
        viewModel.ApplyCombatDamage(84, "Gwardzista", "Agron", true,
            when.AddMilliseconds(20), "Ciecie");
        viewModel.ApplyCombatDamage(22, "Gwardzista", "{yNor{xga", false,
            when.AddMilliseconds(30), "Walniecie");
        viewModel.ObserveHealthCombatState(false, when.AddSeconds(1));

        viewModel.ObserveHealthCombatState(true, when.AddSeconds(2));
        viewModel.ApplyCombatDamage(50, "Ghul", "Duży wilk", false,
            when.AddSeconds(2.1), "Ugryzienie");
        viewModel.ApplyCombatDamage(34, "Ghul", "Agron", true,
            when.AddSeconds(2.2), "Ciecie");
        viewModel.ObserveHealthCombatState(false, when.AddSeconds(3));

        Assert.Collection(viewModel.LastCombatParticipantDamage,
            wilk =>
            {
                Assert.Equal("Duży wilk", wilk.DisplayName);
                Assert.Equal(50, wilk.Amount);
                Assert.Equal("Ugryzienie: ~50 HP", wilk.TypeBreakdownText);
            },
            agron =>
            {
                Assert.Equal("Agron (Ty)", agron.DisplayName);
                Assert.Equal(34, agron.Amount);
                Assert.Equal("Ciecie: ~34 HP", agron.TypeBreakdownText);
            });
        Assert.DoesNotContain(viewModel.LastCombatParticipantDamage, row => row.Name == "Norga");

        Assert.Collection(viewModel.SessionParticipantDamage,
            agron =>
            {
                Assert.Equal("Agron (Ty)", agron.DisplayName);
                Assert.Equal(193, agron.Amount);
                Assert.Equal("Ciecie: ~193 HP", agron.TypeBreakdownText);
            },
            wilk => Assert.Equal(50, wilk.Amount),
            norga => Assert.Equal(22, norga.Amount));
    }

    [Fact]
    public void TracksIncomeExpensesAndCurrencyConversion()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());

        Assert.True(viewModel.ObserveMoneyLine("Naliczyles 30 miedzianych monet."));
        Assert.True(viewModel.ObserveMoneyLine("Sprzedajesz kamien za 1 srebrna monete."));
        Assert.True(viewModel.ObserveMoneyLine("Kupujesz racje za 15 miedzianych monet."));
        Assert.True(viewModel.ObserveMoneyLine("Naprawa kosztowala 1 zlota monete."));
        Assert.False(viewModel.ObserveMoneyLine("Wplacasz na swoje konto 100 zlotych monet."));

        Assert.Equal(90, viewModel.SessionMoneyIncome);
        Assert.Equal(915, viewModel.SessionMoneyExpense);
        Assert.Equal("1g 1s 30c", ExperienceStatisticsViewModel.FormatMoney(990));
    }

    [Fact]
    public void BuildsHistoryTotalsRecordsAndTenMostRecentOpponentEntries()
    {
        var startedAt = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.FromHours(2));
        var historical = new ExperienceSessionData
        {
            StartedAt = startedAt,
            LastUpdatedAt = startedAt.AddHours(2),
        };
        for (var index = 0; index < 12; index++)
        {
            historical.Changes.Add(new ExperienceChangeData
            {
                Kind = ExperienceChangeKind.KillReward,
                Amount = 100 + index,
                EnemyName = $"Mob {index}",
                Level = 31,
                When = startedAt.AddMinutes(index),
            });
        }
        historical.Changes.Add(new ExperienceChangeData
        {
            Kind = ExperienceChangeKind.Damage,
            Amount = 500,
            EnemyName = "Mob 11",
            Level = 31,
            When = startedAt.AddMinutes(11),
        });

        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData { Sessions = [historical] });

        Assert.Equal(12, viewModel.Mobs.Count);
        Assert.Contains("02h 00m 00s", viewModel.LongestSessionDetails);
        Assert.True(viewModel.TotalRecordedExperience >= 1766);
    }

    [Fact]
    public void ResetRemovesAllRecordedStatisticsAndStartsEmptySession()
    {
        var viewModel = new ExperienceStatisticsViewModel();
        viewModel.Start(new ExperienceStatisticsData());
        viewModel.Apply([new ExperienceChange(
            ExperienceChangeKind.KillReward, 100, "Ghul", 31, 900, DateTimeOffset.Now)]);

        viewModel.Reset();

        Assert.Equal(0, viewModel.TotalKills);
        Assert.Empty(viewModel.Mobs);
        Assert.Single(viewModel.Data.Sessions);
        Assert.Empty(viewModel.Data.Sessions[0].Changes);
        Assert.Empty(viewModel.Data.Sessions[0].CombatDamage);
    }
}
