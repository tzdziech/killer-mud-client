using MudClient.App.Services;
using MudClient.App.ViewModels;

namespace MudClient.App.Tests;

/// <summary>
/// Two running instances of the client sharing the same profile/global files (multiboxing)
/// used to have no way to merge their changes — whichever instance saved last silently
/// overwrote what the other one wrote. SaveActiveProfile now 3-way merges against disk
/// whenever it detects the file changed since this instance last loaded/saved it, so an
/// addition, edit or deletion made by the *other* instance survives instead of being clobbered.
/// </summary>
public sealed class ProfileSaveConflictTests
{
    private static async Task<string> CreateTempDirectoryAsync()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_SaveConflictTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        await Task.CompletedTask;
        return directory;
    }

    [Fact]
    public async Task SavingProfile_AfterAnotherInstanceSavedIt_MergesInsteadOfOverwriting()
    {
        var directory = await CreateTempDirectoryAsync();
        var profileService = new ProfileService(directory);
        var settingsService = new AppSettingsService(directory);

        try
        {
            await using var vm1 = new MainWindowViewModel(profileService, settingsService);
            vm1.NewProfileName = "TestHero";
            vm1.NewProfileHost = "killer-mud.pl";
            vm1.NewProfilePort = 4004;
            vm1.CreateProfileCommand.Execute(null);

            await Task.Delay(20, TestContext.Current.CancellationToken);

            // "Instance 2": loads and re-saves the same profile, moving the on-disk
            // timestamp past what vm1 last knew about.
            await using var vm2 = new MainWindowViewModel(profileService, settingsService);
            vm2.SelectedProfileName = "TestHero";
            vm2.SelectProfileCommand.Execute(null);

            await Task.Delay(20, TestContext.Current.CancellationToken);

            vm1.NewRuleName = "MojTrigger";
            vm1.NewRuleType = "trigger";
            vm1.NewRulePattern = "Jestes ranny";
            vm1.NewRuleAction = "heal";
            vm1.NewRuleIsGlobal = false;
            vm1.AddRuleCommand.Execute(null);

            Assert.Contains(vm1.Toasts, t => t.Type == "info" && t.Text.Contains("TestHero"));

            var onDisk = profileService.Load("TestHero");
            Assert.NotNull(onDisk);
            Assert.Contains(onDisk!.Rules, r => r.Name == "MojTrigger");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavingProfile_RepeatedlyFromSameInstance_NeverShowsMergeToast()
    {
        var directory = await CreateTempDirectoryAsync();
        var profileService = new ProfileService(directory);
        var settingsService = new AppSettingsService(directory);

        try
        {
            await using var vm = new MainWindowViewModel(profileService, settingsService);
            vm.NewProfileName = "TestHero";
            vm.NewProfileHost = "killer-mud.pl";
            vm.NewProfilePort = 4004;
            vm.CreateProfileCommand.Execute(null);

            for (var i = 0; i < 5; i++)
            {
                vm.NewRuleName = $"Rule{i}";
                vm.NewRuleType = "trigger";
                vm.NewRulePattern = "x";
                vm.NewRuleAction = "y";
                vm.NewRuleIsGlobal = false;
                vm.AddRuleCommand.Execute(null);
            }

            Assert.DoesNotContain(vm.Toasts, t => t.Type is "warning" or "info" && t.Text.Contains("inną instancję"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavingGlobalData_AfterAnotherInstanceAddedARule_MergesInsteadOfOverwriting()
    {
        var directory = await CreateTempDirectoryAsync();
        var profileService = new ProfileService(directory);
        var settingsService = new AppSettingsService(directory);

        try
        {
            await using var vm1 = new MainWindowViewModel(profileService, settingsService);
            vm1.NewRuleName = "GlobalOne";
            vm1.NewRuleType = "trigger";
            vm1.NewRulePattern = "x";
            vm1.NewRuleAction = "y";
            vm1.NewRuleIsGlobal = true;
            vm1.AddRuleCommand.Execute(null);

            await Task.Delay(20, TestContext.Current.CancellationToken);

            // "Instance 2": loads the global file at construction (sees GlobalOne), then adds
            // its own global trigger and saves — moving the on-disk timestamp past what vm1
            // last knew about.
            await using var vm2 = new MainWindowViewModel(profileService, settingsService);
            vm2.NewRuleName = "GlobalTwo";
            vm2.NewRuleType = "trigger";
            vm2.NewRulePattern = "z";
            vm2.NewRuleAction = "w";
            vm2.NewRuleIsGlobal = true;
            vm2.AddRuleCommand.Execute(null);

            await Task.Delay(20, TestContext.Current.CancellationToken);

            // vm1 still only knows about GlobalOne — adding GlobalThree and saving must not
            // drop vm2's GlobalTwo, which is exactly the bug multiboxing users hit.
            vm1.NewRuleName = "GlobalThree";
            vm1.NewRuleType = "trigger";
            vm1.NewRulePattern = "q";
            vm1.NewRuleAction = "r";
            vm1.NewRuleIsGlobal = true;
            vm1.AddRuleCommand.Execute(null);

            Assert.Contains(vm1.Toasts, t => t.Type == "info" && t.Text.Contains("globalne"));

            var onDisk = profileService.LoadGlobal();
            Assert.Contains(onDisk.Rules, r => r.Name == "GlobalOne");
            Assert.Contains(onDisk.Rules, r => r.Name == "GlobalTwo");
            Assert.Contains(onDisk.Rules, r => r.Name == "GlobalThree");

            // vm1's own in-memory view picks up the merged-in GlobalTwo too, so its *next*
            // save won't drop it again.
            Assert.Contains(vm1.AutomationRules, r => r.Name == "GlobalTwo");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavingGlobalData_WhenOtherInstanceDeletedAnEntryWeDidNotTouch_RespectsThatDeletion()
    {
        var directory = await CreateTempDirectoryAsync();
        var profileService = new ProfileService(directory);
        var settingsService = new AppSettingsService(directory);

        try
        {
            await using var vm1 = new MainWindowViewModel(profileService, settingsService);
            vm1.NewRuleName = "GlobalOne";
            vm1.NewRuleType = "trigger";
            vm1.NewRulePattern = "x";
            vm1.NewRuleAction = "y";
            vm1.NewRuleIsGlobal = true;
            vm1.AddRuleCommand.Execute(null);

            vm1.NewRuleName = "GlobalTwo";
            vm1.NewRuleType = "trigger";
            vm1.NewRulePattern = "z";
            vm1.NewRuleAction = "w";
            vm1.NewRuleIsGlobal = true;
            vm1.AddRuleCommand.Execute(null);

            await Task.Delay(20, TestContext.Current.CancellationToken);

            // "Instance 2": loads (sees both), deletes GlobalOne, saves.
            await using var vm2 = new MainWindowViewModel(profileService, settingsService);
            var toDelete = vm2.AutomationRules.First(r => r.Name == "GlobalOne");
            vm2.DeleteRuleCommand.Execute(toDelete);

            await Task.Delay(20, TestContext.Current.CancellationToken);

            // vm1 never touched GlobalOne itself — its save must not resurrect it just
            // because vm1's own in-memory copy still has it.
            vm1.NewRuleName = "GlobalThree";
            vm1.NewRuleType = "trigger";
            vm1.NewRulePattern = "q";
            vm1.NewRuleAction = "r";
            vm1.NewRuleIsGlobal = true;
            vm1.AddRuleCommand.Execute(null);

            var onDisk = profileService.LoadGlobal();
            Assert.DoesNotContain(onDisk.Rules, r => r.Name == "GlobalOne");
            Assert.Contains(onDisk.Rules, r => r.Name == "GlobalTwo");
            Assert.Contains(onDisk.Rules, r => r.Name == "GlobalThree");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
