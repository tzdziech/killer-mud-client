namespace MudClient.App.Models;

/// <summary>
/// Persisted per-character configuration: notes, aliases, triggers and timers.
/// </summary>
public sealed class ProfileData
{
    /// <summary>Local account label used in the picker and as the profile file name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>MUD login sent to the server; empty in legacy files means <see cref="Name"/>.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Server address used by this account.</summary>
    public string Host { get; set; } = "killer-mud.pl";

    /// <summary>Server port used by this account.</summary>
    public int Port { get; set; } = 4004;

    /// <summary>
    /// Text encoding used to talk to this account's server (see <see cref="Core.Networking.MudTextEncodings"/>).
    /// Defaults to auto-detection; empty/legacy files also fall back to auto-detect.
    /// </summary>
    public string Encoding { get; set; } = Core.Networking.MudTextEncodings.Auto;

    /// <summary>Account password encrypted with DPAPI (base64); empty = no password stored.</summary>
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>
    /// True for a freshly created account that has never been registered on the
    /// MUD. On the first connection the client sends the character-creation
    /// sequence (login, "t", password, password, space) instead of a plain login;
    /// the flag is cleared afterwards so later logins send only name + password.
    /// </summary>
    public bool NeedsRegistration { get; set; }

    public List<ProfileNote> Notes { get; set; } = [];

    public List<ProfileRule> Rules { get; set; } = [];

    public List<ProfileTimer> Timers { get; set; } = [];

    public List<ProfileLocation> Locations { get; set; } = [];

    /// <summary>Grouping folders (per kind) owned by this profile.</summary>
    public List<ProfileFolder> Folders { get; set; } = [];

    /// <summary>Last 10 death locations, newest first.</summary>
    public List<ProfileDeath> Deaths { get; set; } = [];

    /// <summary>
    /// Legacy flat buff list. New clients migrate it to a default set and keep
    /// the selected set mirrored here for compatibility with older clients.
    /// </summary>
    public List<string> RequiredBuffs { get; set; } = [];

    /// <summary>Named buff sets available to this character.</summary>
    public List<ProfileBuffSet> BuffSets { get; set; } = [];

    /// <summary>Id of the set last selected in the buffs widget.</summary>
    public string ActiveBuffSetId { get; set; } = string.Empty;

    /// <summary>Every spell name this character has ever reported via the "spell"/"spell all"
    /// command, with whether it's currently known (a memorization count present) or still
    /// missing (blank "(  )"). A spell simply absent from this list has never been seen in that
    /// output at all — see <see cref="MudClient.App.Services.SpellKnowledgeParser"/> for how it's
    /// captured and <see cref="MudClient.App.Services.SpellKnowledgeClassifier"/> for how the map
    /// uses the three-way distinction to color spellbook-mob tooltips.</summary>
    public List<ProfileSpellEntry> KnownSpells { get; set; } = [];

    /// <summary>Every skill name this character has ever reported via the "skill" command, with
    /// its last-seen current level. A skill simply absent from this list has never been seen in
    /// that output at all — see <see cref="MudClient.App.Services.SkillKnowledgeParser"/> for how
    /// it's captured and <see cref="MudClient.App.Services.SkillKnowledgeClassifier"/> for how the
    /// map uses it to color teacher tooltips.</summary>
    public List<ProfileSkillEntry> KnownSkills { get; set; } = [];

    /// <summary>Rectangular map region auto-farm is allowed to roam within, drawn via right-click
    /// drag on the map. Null when never defined — see
    /// <see cref="MudClient.Core.Map.FarmTraversalPlanner"/> for how it's used.</summary>
    public ProfileFarmRegion? AutoFarmRegion { get; set; }

    /// <summary>Default/limits for <see cref="AutoFarmHpThresholdPercent"/>.</summary>
    public const int DefaultAutoFarmHpThresholdPercent = 30;
    public const int MinAutoFarmHpThresholdPercent = 5;
    public const int MaxAutoFarmHpThresholdPercent = 90;

    /// <summary>HP percent at/below which auto-farm pauses hopping between rooms and runs
    /// <see cref="MudClient.Core.Automation.HealthRecoveryPolicy"/> instead.</summary>
    public int AutoFarmHpThresholdPercent { get; set; } = DefaultAutoFarmHpThresholdPercent;

    /// <summary>Spell auto-farm casts on itself (memorizing it first if needed) once HP drops to
    /// or below <see cref="AutoFarmHpThresholdPercent"/>. Blank means "just rest, no self-heal".</summary>
    public string AutoFarmHealSpellName { get; set; } = string.Empty;

    /// <summary>Spells auto-farm always keeps memorized — checked alongside the HP threshold
    /// before every room hop; any missing one gets "mem"med (and the character rests) the same
    /// way the heal spell does. Independent of whether they're currently active as a buff.</summary>
    public List<string> AutoFarmRequiredMemorizedSpells { get; set; } = [];

    /// <summary>Per-character automation/preference toggles (autostand, autoscan, ...). Null means
    /// this profile predates per-profile automation settings — see
    /// <see cref="ProfileAutomationSettings"/> for the one-time migration fallback.</summary>
    public ProfileAutomationSettings? Automation { get; set; }

    /// <summary>Lua source defining reusable helper functions/values shared by every "script"
    /// alias/trigger/timer on this character — loaded once when the profile activates (see
    /// <see cref="MudClient.Core.Automation.LuaScriptEngine.LoadLibrary"/>), before any of them
    /// can run. Empty means no library.</summary>
    public string LuaLibrary { get; set; } = string.Empty;
}

/// <summary>One spell name from this character's own class spell list, as last reported by the
/// "spell"/"spell all" command.</summary>
public sealed class ProfileSpellEntry
{
    public string Name { get; set; } = string.Empty;

    /// <summary>True when the last-seen memorization count was non-blank (e.g. "(29)"); false
    /// when it was blank ("(  )") — still learnable but not yet obtained.</summary>
    public bool Known { get; set; }
}

/// <summary>Persisted form of <see cref="MudClient.Core.Map.FarmRegion"/> (a plain record struct,
/// which System.Text.Json can round-trip directly, but is kept as its own class here so the
/// persisted shape doesn't change if the Core type's member order or representation ever does).</summary>
public sealed class ProfileFarmRegion
{
    public int AreaId { get; set; }

    public double Z { get; set; }

    public double MinX { get; set; }

    public double MinY { get; set; }

    public double MaxX { get; set; }

    public double MaxY { get; set; }
}

/// <summary>One skill name from this character's own class skill list, as last reported by the
/// "skill" command.</summary>
public sealed class ProfileSkillEntry
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Last-seen current skill level (the second of the three "skill" command columns).</summary>
    public int Current { get; set; }
}

public sealed class ProfileBuffSet
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public List<string> Buffs { get; set; } = [];
}

/// <summary>
/// Rules, timers and autowalk locations marked as global — shared by all
/// profiles and stored in a single file next to the per-profile ones.
/// </summary>
public sealed class GlobalData
{
    public List<ProfileNote> Notes { get; set; } = [];

    public List<ProfileRule> Rules { get; set; } = [];

    public List<ProfileTimer> Timers { get; set; } = [];

    public List<ProfileLocation> Locations { get; set; } = [];

    /// <summary>Grouping folders (per kind) shared by all profiles.</summary>
    public List<ProfileFolder> Folders { get; set; } = [];
}

/// <summary>
/// A grouping folder persisted per character or in the shared global file.
/// Folders form a tree via <see cref="ParentId"/> and group items of a single
/// <see cref="Kind"/>.
/// </summary>
public sealed class ProfileFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Parent folder id, or null for a root folder.</summary>
    public string? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Domain this folder belongs to (Timers/Aliases/Triggers/Notes/Autowalk).</summary>
    public FolderKind Kind { get; set; }

    /// <summary>True when stored in the shared global file, not a profile.</summary>
    public bool IsGlobal { get; set; }
}

/// <summary>A named autowalk target room stored per character.</summary>
public sealed class ProfileLocation
{
    public string Name { get; set; } = string.Empty;

    public string Vnum { get; set; } = string.Empty;

    /// <summary>True when stored in the shared global file, not a profile.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Id of the containing folder, or null when loose.</summary>
    public string? FolderId { get; set; }
}

/// <summary>A death location stored per character (newest first, max 10).</summary>
public sealed class ProfileDeath
{
    public string Vnum { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public string When { get; set; } = string.Empty;
}

/// <summary>
/// A repeating timer stored per character. Fires every
/// Minutes/Seconds/Milliseconds and sends Commands in order until disabled.
/// </summary>
public sealed class ProfileTimer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public int Minutes { get; set; }

    public int Seconds { get; set; }

    public int Milliseconds { get; set; }

    /// <summary>Commands sent in this order on every tick.</summary>
    public List<string> Commands { get; set; } = [];

    /// <summary>
    /// Raw command text preserving the original user input (e.g. "look;exa").
    /// When non-empty, <see cref="MakeTimerEntry"/> uses this instead of
    /// joining <see cref="Commands"/> with newlines, so the user's chosen
    /// separator characters are not lost across save/load cycles.
    /// </summary>
    public string CommandsText { get; set; } = string.Empty;

    /// <summary>True when <see cref="CommandsText"/> is Lua source instead of a plain command
    /// list.</summary>
    public bool IsScript { get; set; }

    public bool IsEnabled { get; set; }

    /// <summary>True when stored in the shared global file, not a profile.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Id of the containing folder, or null when loose.</summary>
    public string? FolderId { get; set; }
}

public sealed class ProfileNote
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>True when stored in the shared global file, not a profile.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Id of the containing folder, or null when loose.</summary>
    public string? FolderId { get; set; }
}

public sealed class ProfileRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    /// <summary>"alias", "trigger" or "timer".</summary>
    public string Type { get; set; } = "alias";

    public string Pattern { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    /// <summary>True when <see cref="Action"/> is Lua source instead of a replacement/command
    /// template.</summary>
    public bool IsScript { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>True when stored in the shared global file, not a profile.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Id of the containing folder, or null when loose.</summary>
    public string? FolderId { get; set; }

    /// <summary>Trigger-only: plays a notification sound on every match (see
    /// MudClient.Core.Automation.TriggerEngine.RuleMatched). Ignored for aliases/timers.</summary>
    public bool PlaySoundOnMatch { get; set; }
}
