using MudClient.App.Docking;

namespace MudClient.App.Tests;

public sealed class UserBuildFeaturesTests
{
    [Fact]
    public void UserBuild_HidesFarmPanelAndDoesNotStartSessionLoggingAutomatically()
    {
        Assert.False(BuildFeatures.FarmPanelAvailable);
        Assert.False(BuildFeatures.StartSessionLoggingAutomatically);

        var factory = new MudDockFactory(new object(), new object());
        factory.CreateLayout();

        Assert.DoesNotContain(factory.AllTools, tool => tool.Id == "AutomationFarm");
    }
}
