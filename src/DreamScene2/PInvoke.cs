using System;
using System.Runtime.InteropServices;

namespace DreamScene2
{
    public static class PInvoke
    {
        [DllImport("User32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(
            IntPtr parentHandle,
            IntPtr childAfter,
            string className,
            string windowTitle);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


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

    /// <summary>Enumeration of the different ways of showing a window using
    /// ShowWindow.</summary>
    public enum WindowShowStyle : uint
    {
        /// <summary>Hides the window and activates another window.</summary>
        SW_HIDE = 0,

        /// <summary>Activates and displays a window. If the window is minimized
        /// or maximized, the system restores it to its original size and
        /// position. An application should specify this flag when displaying
        /// the window for the first time.</summary>
        SW_SHOWNORMAL = 1,

        /// <summary>Activates the window and displays it as a minimized window.</summary>
        SW_SHOWMINIMIZED = 2,

        /// <summary>Activates the window and displays it as a maximized window.</summary>
        SW_SHOWMAXIMIZED = 3,

        /// <summary>Maximizes the specified window.</summary>
        SW_MAXIMIZE = 3,

        /// <summary>Displays a window in its most recent size and position.
        /// This value is similar to "ShowNormal", except the window is not
        /// actived.</summary>
        SW_SHOWNOACTIVATE = 4,

        /// <summary>Activates the window and displays it in its current size
        /// and position.</summary>
        SW_SHOW = 5,

        /// <summary>Minimizes the specified window and activates the next
        /// top-level window in the Z order.</summary>
        SW_MINIMIZE = 6,

        /// <summary>Displays the window as a minimized window. This value is
        /// similar to "ShowMinimized", except the window is not activated.</summary>
        SW_SHOWMINNOACTIVE = 7,

        /// <summary>Displays the window in its current size and position. This
        /// value is similar to "Show", except the window is not activated.</summary>
        SW_SHOWNA = 8,

        /// <summary>Activates and displays the window. If the window is
        /// minimized or maximized, the system restores it to its original size
        /// and position. An application should specify this flag when restoring
        /// a minimized window.</summary>
        SW_RESTORE = 9,

        /// <summary>Sets the show state based on the SW_ value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.</summary>
        SW_SHOWDEFAULT = 10,

        /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
        /// that owns the window is hung. This flag should only be used when
        /// minimizing windows from a different thread.</summary>
        SW_FORCEMINIMIZE = 11,
    }

    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}
