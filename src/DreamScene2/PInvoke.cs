using System;
using System.Runtime.InteropServices;

namespace DreamScene2
{
    public static class PInvoke
    {
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("User32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("User32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("User32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);


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
        public static extern void DS2_RefreshDesktop(int animated = 0);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_ToggleShowDesktopIcons();

        [DllImport("DS2Native.dll")]
        public static extern int DS2_IsVisibleDesktopIcons();

        [DllImport("DS2Native.dll")]
        public static extern int DS2_StartForwardMouseKeyboardMessage(IntPtr hWnd);

        [DllImport("DS2Native.dll")]
        public static extern void DS2_EndForwardMouseKeyboardMessage();

        [DllImport("DS2Native.dll")]
        public static extern void DS2_ToggleProcess(uint dwPID, int bResumeProcess);
    }

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}
