namespace MudClient.App;

/// <summary>
/// Defines the capabilities of the currently compiled client variant.
/// Build with <c>-c User</c> for the regular client or <c>-c Admin</c> for
/// the administrative variant.
/// </summary>
public static class BuildFeatures
{
#if ADMIN_BUILD
    public static bool FarmPanelAvailable => true;
    public static bool StartSessionLoggingAutomatically => true;
#else
    public static bool FarmPanelAvailable => false;
    public static bool StartSessionLoggingAutomatically => false;
#endif
}
