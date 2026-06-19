using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CodeSummarizer.Windows.Services;

public static class NativeWindowSecurity
{
    private const uint WindowDisplayAffinityNone = 0x00000000;
    private const uint WindowDisplayAffinityExcludeFromCapture = 0x00000011;

    public static bool SetCaptureProtection(Window window, bool enabled)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return false;

        return SetWindowDisplayAffinity(handle,
            enabled ? WindowDisplayAffinityExcludeFromCapture : WindowDisplayAffinityNone);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowDisplayAffinity(IntPtr windowHandle, uint affinity);
}
