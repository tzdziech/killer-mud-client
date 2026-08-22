namespace MudClient.App.Models;

/// <summary>One entry in auto-farm's "keep memorized" list (see discussion #32's "obczarka"
/// request). <c>Required</c> entries block the farm — mem + rest — until satisfied, exactly like
/// the heal spell; non-required ("opportunistic") entries are only mem'd while the farm is already
/// stopped for another reason (HP recovery or a required spell), never triggering a stop on their
/// own.</summary>
public sealed record AutoFarmMemSpell(string Name, bool Required);
