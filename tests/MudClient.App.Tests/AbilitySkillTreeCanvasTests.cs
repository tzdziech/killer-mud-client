using Avalonia;
using Avalonia.Controls;
using MudClient.App.Controls;
using MudClient.App.Models;
using MudClient.Core.Killeropedia;
using Xunit;

namespace MudClient.App.Tests;

public sealed class AbilitySkillTreeCanvasTests
{
    private static AbilitySkillTreeEntry MakeAbility(
        string name,
        string type,
        string? school = null,
        string wandererSpecialization = "kazda specjalizacja",
        int wandererLevel = 1,
        string browsedClass = "Wedrowiec")
    {
        var source = new AbilityCaptureEntry
        {
            Name = name,
            Type = type,
            School = school,
            WandererSpecialization = wandererSpecialization,
            AvailableForClasses =
            [
                new ClassLevelRequirement(browsedClass, wandererLevel),
                new ClassLevelRequirement("Wedrowiec", wandererLevel),
            ],
        };

        return AbilitySkillTreeEntry.Create(source, browsedClass)
            ?? throw new InvalidOperationException("Test setup produced an excluded ability.");
    }

    // ====================================================================
    // GetBranch — groups spells by school, skills by bierny/aktywny
    // ====================================================================

    [Theory]
    [InlineData("skill bierny", "Umiejętności bierne")]
    [InlineData("skill aktywny", "Umiejętności aktywne")]
    public void GetBranch_Skill_GroupsByPassiveOrActive(string type, string expectedBranch)
    {
        var ability = MakeAbility("axe", type);

        Assert.Equal(expectedBranch, AbilitySkillTreeCanvas.GetBranch(ability));
    }

    [Fact]
    public void GetBranch_SpellWithSchool_UsesSchoolAsBranch()
    {
        var ability = MakeAbility("light", "czar", school: "Przemiany");

        Assert.Equal("Przemiany", AbilitySkillTreeCanvas.GetBranch(ability));
    }

    [Fact]
    public void GetBranch_SpellWithoutSchool_FallsBackToCzary()
    {
        var ability = MakeAbility("aid", "czar wspomagajacy", school: null);

        Assert.Equal("Czary", AbilitySkillTreeCanvas.GetBranch(ability));
    }

    // ====================================================================
    // ComputeLayout — pure radial geometry
    // ====================================================================

    [Fact]
    public void ComputeLayout_NoAbilities_ProducesNoNodes()
    {
        var layout = AbilitySkillTreeCanvas.ComputeLayout([], new Size(400, 400));

        Assert.Empty(layout.Nodes);
    }

    [Fact]
    public void ComputeLayout_ZeroSizeViewport_ReturnsEmptyLayout()
    {
        var abilities = new[] { MakeAbility("axe", "skill bierny") };

        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, new Size(0, 0));

        Assert.Empty(layout.Nodes);
    }

    [Fact]
    public void ComputeLayout_OneNodePerAbility()
    {
        var abilities = new[]
        {
            MakeAbility("axe", "skill bierny"),
            MakeAbility("sword", "skill bierny"),
            MakeAbility("light", "czar", school: "Przemiany"),
        };

        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, new Size(500, 500));

        Assert.Equal(3, layout.Nodes.Count);
        Assert.Equal(["axe", "light", "sword"], layout.Nodes.Select(n => n.Ability.Name).OrderBy(n => n));
    }

    [Fact]
    public void ComputeLayout_AllNodesStayInsideTheViewportRadius()
    {
        var abilities = Enumerable.Range(1, 20)
            .Select(i => MakeAbility($"skill{i}", i % 2 == 0 ? "skill bierny" : "skill aktywny", wandererLevel: i))
            .ToArray();

        var viewport = new Size(400, 400);
        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, viewport);

        var maxAllowedRadius = Math.Min(viewport.Width, viewport.Height) / 2;
        foreach (var node in layout.Nodes)
        {
            var distance = Math.Sqrt(
                Math.Pow(node.Center.X - layout.Center.X, 2) + Math.Pow(node.Center.Y - layout.Center.Y, 2));
            Assert.True(distance <= maxAllowedRadius, $"Node {node.Ability.Name} escaped the viewport radius.");
        }
    }

    [Fact]
    public void ComputeLayout_CrowdedSameLevelRing_SpreadsNodesInsteadOfStackingThem()
    {
        // Regression: a real Paladyn capture has a dozen+ passive skills all learnable at level 1,
        // which used to render as one solid overlapping blob instead of a readable fan.
        var abilities = Enumerable.Range(1, 20)
            .Select(i => MakeAbility($"skill{i:00}", "skill bierny", wandererLevel: 1))
            .ToArray();

        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, new Size(600, 600));
        var nodeRadius = layout.Nodes[0].Radius;

        for (var i = 0; i < layout.Nodes.Count; i++)
        {
            for (var j = i + 1; j < layout.Nodes.Count; j++)
            {
                var a = layout.Nodes[i];
                var b = layout.Nodes[j];
                var distance = Math.Sqrt(Math.Pow(a.Center.X - b.Center.X, 2) + Math.Pow(a.Center.Y - b.Center.Y, 2));
                Assert.True(
                    distance >= nodeRadius * 1.3,
                    $"{a.Ability.Name} and {b.Ability.Name} landed almost on top of each other ({distance:F1}px apart).");
            }
        }
    }

    [Fact]
    public void ComputeLayout_EveryNodeHasAConnectorLeadingBackTowardTheHub()
    {
        var abilities = new[]
        {
            MakeAbility("axe", "skill bierny", wandererLevel: 1),
            MakeAbility("axe mastery", "skill bierny", wandererLevel: 20),
        };

        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, new Size(400, 400));

        Assert.Equal(2, layout.Connectors.Count);
    }

    // ====================================================================
    // HitTestNode
    // ====================================================================

    [Fact]
    public void HitTestNode_PointAtNodeCenter_ReturnsThatNode()
    {
        var abilities = new[] { MakeAbility("axe", "skill bierny") };
        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, new Size(400, 400));
        var target = layout.Nodes[0];

        var hit = AbilitySkillTreeCanvas.HitTestNode(layout, target.Center);

        Assert.NotNull(hit);
        Assert.Equal("axe", hit!.Value.Ability.Name);
    }

    [Fact]
    public void HitTestNode_PointFarFromAnyNode_ReturnsNull()
    {
        var abilities = new[] { MakeAbility("axe", "skill bierny") };
        var layout = AbilitySkillTreeCanvas.ComputeLayout(abilities, new Size(400, 400));

        var hit = AbilitySkillTreeCanvas.HitTestNode(layout, new Point(-1000, -1000));

        Assert.Null(hit);
    }

    // ====================================================================
    // BuildTooltip
    // ====================================================================

    [Fact]
    public void BuildTooltip_ContainsAbilityName()
    {
        var source = new AbilityCaptureEntry
        {
            Name = "smite evil",
            Type = "skill aktywny",
            WandererSpecialization = "kazda specjalizacja",
            Description = "Zadaje dodatkowe obrazenia zlym istotom.",
            AvailableForClasses = [new ClassLevelRequirement("Wedrowiec", 4)],
        };
        var ability = AbilitySkillTreeEntry.Create(source, "Wedrowiec")!;

        var tooltip = AbilitySkillTreeCanvas.BuildTooltip(ability);

        var content = Assert.IsType<StackPanel>(Assert.IsType<Border>(tooltip).Child);
        var texts = content.Children
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .ToList();
        Assert.Contains("smite evil", texts);
        Assert.Contains(ability.Description, texts);
    }
}
