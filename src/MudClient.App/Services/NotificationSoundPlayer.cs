using System.Runtime.InteropServices;

namespace MudClient.App.Services;

/// <summary>Plays the Windows system notification sound (the classic UI "ding") for chat-message
/// and trigger-match sound alerts. A direct P/Invoke to user32's MessageBeep rather than
/// System.Media.SystemSounds avoids adding the System.Windows.Extensions package for one
/// function — this fork only builds/ships for Windows anyway (see release.yml). Respects the
/// user's system volume/mute and sound scheme, and needs no bundled audio asset.</summary>
public static class NotificationSoundPlayer
{
    private const uint MB_ICONASTERISK = 0x00000040;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool MessageBeep(uint uType);

    public static void PlayNotification() => MessageBeep(MB_ICONASTERISK);
}
