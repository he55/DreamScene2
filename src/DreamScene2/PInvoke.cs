using System;
using System.Runtime.InteropServices;

namespace DreamScene2
{
    public static class PInvoke
    {
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("DS2Native.dll")]
        public static extern ulong DS2_GetLastInputTickCount();

        [DllImport("DS2Native.dll")]
        public static extern IntPtr DS2_GetDesktopWindowHandle();

        [DllImport("DS2Native.dll")]
        public static extern int DS2_TestScreen(RECT rect);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_SetWindowPosition(IntPtr hWnd, RECT rect);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_RestoreLastWindowPosition();

        [DllImport("DS2Native.dll")]
        public static extern void DS2_RefreshDesktop();
    }

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}
