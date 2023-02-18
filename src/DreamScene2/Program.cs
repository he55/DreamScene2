using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DreamScene2
{
    internal static class Program
    {
        [DllImport("Kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetProcessDPIAware();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            IntPtr hwnd = PInvoke.FindWindow(null, Constant.MainWindowTitle);
            if (hwnd != IntPtr.Zero)
            {
                const int SW_RESTORE = 9;
                PInvoke.ShowWindow(hwnd, SW_RESTORE);
                PInvoke.SetForegroundWindow(hwnd);
                return;
            }

            string dllPath = Helper.GetPathForStartupFolder("DS2Native.dll");
            LoadLibrary(dllPath);

#if NETCOREAPP3_0_OR_GREATER
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
#else
            SetProcessDPIAware();
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                Application.Run(new MainForm());
                return;
            }

            if (args[0] == Constant.Cmd)
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
            else if (args[0] == "/s")
            {
            }
            else if (args[0] == "/p")
            {
                int p = int.Parse(args[1]);
            }
            else if (args[0].StartsWith("/c:"))
            {
                int p = int.Parse(args[0].Substring(3));
            }
        }

        static void ExtractResources()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.EndsWith(".dll"))
                {
                    string fileName = resourceName.Substring(nameof(DreamScene2).Length + 1);
                    string filePath = Path.Combine(Application.StartupPath, fileName);
                    if (!File.Exists(filePath))
                    {
                        using (FileStream fileStream = File.Create(filePath))
                        {
                            Stream stream = assembly.GetManifestResourceStream(resourceName);
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }
    }
}
