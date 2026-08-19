using MudClient.Core.Gmcp;

namespace MudClient.Core.Automation;

/// <summary>
/// Detects the transition into a state where another group member is fighting
/// in the player's current room. Members fighting an explicitly excluded enemy
/// do not qualify. Repeated GMCP updates do not retrigger it.
/// </summary>
public sealed class AutoAssistPolicy
{
    private readonly object _sync = new();
    private bool _assistRequested;

    public bool ShouldAssist(
        bool enabled,
        string? currentRoom,
        string? selfName,
        bool selfIsFighting,
        CharacterGroupUpdate? group,
        IReadOnlyList<RoomPerson> people,
        IReadOnlyCollection<string> excludedEnemyNames)
    {
        lock (_sync)
        {
            var shouldAssist = enabled
                && !selfIsFighting
                && !string.IsNullOrWhiteSpace(currentRoom)
                && group is not null
                && HasFightingMemberInRoom(
                    currentRoom,
                    selfName,
                    group,
                    people,
                    excludedEnemyNames);

            if (!shouldAssist)
            {
                _assistRequested = false;
                return false;
            }

            if (_assistRequested)
            {
                return false;
            }

            _assistRequested = true;
            return true;
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _assistRequested = false;
        }
    }

    /// <summary>Finds the enemy name of whichever group member <see cref="ShouldAssist"/> just
    /// qualified as fight-worthy — for substituting a "{cel}" placeholder in a user-configured
    /// auto-combat command (e.g. "charge {cel}") that, unlike bare "as", needs an explicit target.
    /// Pure and stateless (no exclusion check): by the time this is worth calling, ShouldAssist has
    /// already vetted that a valid, non-excluded target exists.
    ///
    /// Returns <c>IsFighting</c> separately from <c>EnemyName</c> because Char.Group can report
    /// "fighting" before Room.People delivers the enemy — the same race documented on
    /// <see cref="ShouldAssist"/>'s exclusion handling. A caller that needs the enemy name (i.e.
    /// the template uses "{cel}") must be able to tell "still fighting, name not in yet — keep
    /// waiting" (<c>IsFighting: true, EnemyName: null</c>) apart from "no longer anyone to assist —
    /// give up" (<c>IsFighting: false</c>), since <see cref="ShouldAssist"/>'s own one-shot latch
    /// only fires once per fight and won't ask again once the name does arrive.</summary>
    public static (bool IsFighting, string? EnemyName) FindFightingEnemyName(
        string? currentRoom,
        string? selfName,
        CharacterGroupUpdate? group,
        IReadOnlyList<RoomPerson> people)
    {
        if (string.IsNullOrWhiteSpace(currentRoom) || group is null)
        {
            return (false, null);
        }

        var anyFighting = false;

        foreach (var member in group.Members)
        {
            if (string.Equals(member.Name, selfName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(member.Room?.Trim(), currentRoom.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            var roomPerson = people.FirstOrDefault(person =>
                string.Equals(person.Name, member.Name, StringComparison.OrdinalIgnoreCase));
            var isFighting = string.Equals(
                                 member.Position,
                                 "fighting",
                                 StringComparison.OrdinalIgnoreCase)
                             || roomPerson?.IsFighting == true;

            if (!isFighting)
            {
                continue;
            }

            anyFighting = true;

            if (roomPerson?.Enemy is { } enemy && !string.IsNullOrWhiteSpace(enemy))
            {
                return (true, enemy);
            }
        }

        return (anyFighting, null);
    }

    private static bool HasFightingMemberInRoom(
        string currentRoom,
        string? selfName,
        CharacterGroupUpdate group,
        IReadOnlyList<RoomPerson> people,
        IReadOnlyCollection<string> excludedEnemyNames)
    {
        foreach (var member in group.Members)
        {
            if (string.Equals(member.Name, selfName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(member.Room?.Trim(), currentRoom.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            var roomPerson = people.FirstOrDefault(person =>
                string.Equals(person.Name, member.Name, StringComparison.OrdinalIgnoreCase));
            var isFighting = string.Equals(
                                 member.Position,
                                 "fighting",
                                 StringComparison.OrdinalIgnoreCase)
                             || roomPerson?.IsFighting == true;

            if (!isFighting)
            {
                continue;
            }

            // Char.Group can report "fighting" before Room.People delivers the enemy.
            // With exclusions configured, wait for that precise association instead of
            // sending "as" early and learning only afterwards that the mob was excluded.
            if (excludedEnemyNames.Count > 0
                && (roomPerson?.IsFighting != true
                    || string.IsNullOrWhiteSpace(roomPerson.Enemy)))
            {
                continue;
            }

            if (roomPerson?.Enemy is { } enemy
                && excludedEnemyNames.Any(excluded =>
                    string.Equals(
                        excluded.Trim(),
                        enemy.Trim(),
                        StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
