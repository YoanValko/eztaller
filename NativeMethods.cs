using System.Runtime.InteropServices;

namespace AppInstaller;

public static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool InitiateSystemShutdownExW(
        [MarshalAs(UnmanagedType.LPWStr)] string? lpMachineName,
        [MarshalAs(UnmanagedType.LPWStr)] string? lpMessage,
        uint dwTimeout,
        bool bForceAppsClosed,
        bool bRebootAfterShutdown,
        uint dwReason);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AbortSystemShutdownW([MarshalAs(UnmanagedType.LPWStr)] string? lpMachineName);

    public const uint EWX_REBOOT = 0x00000002;
    public const uint EWX_FORCE = 0x00000004;
    public const uint EWX_FORCEIFHUNG = 0x00000010;
    public const uint SHTDN_REASON_MAJOR_OTHER = 0x00000000;
    public const uint SHTDN_REASON_MINOR_OTHER = 0x00000000;
    public const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;
}