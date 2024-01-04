using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DreamScene2
{
    internal static class Program
    {
        [DllImport("Kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            IntPtr hwnd = NativeMethods.FindWindow(null, Constant.MainWindowTitle);
            if (hwnd != IntPtr.Zero)
            {
                const int SW_RESTORE = 9;
                NativeMethods.ShowWindow(hwnd, SW_RESTORE);
                NativeMethods.SetForegroundWindow(hwnd);
                return;
            }

            string dllPath = Helper.GetPathForStartupFolder("DS2Native.dll");
            LoadLibrary(dllPath);

#if NETCOREAPP3_0_OR_GREATER
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                Application.Run(new MainForm());
                return;
            }

            if (args[0] == "-b" || args[0] == "--background")
            {
                MainForm mainForm = new MainForm();
                if (args.Length > 1)
                    mainForm.PlayPath = args[1];
                mainForm.Opacity = 0;
                mainForm.Show();

                mainForm.Hide();
                mainForm.Opacity = 1;
                Application.Run();
            }
        }
    }
}
