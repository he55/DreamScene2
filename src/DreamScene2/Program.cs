using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DreamScene2
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string filePath = Helper.GetPathForAppFolder("lock");
                File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            }
            catch
            {
                MessageBox.Show("当前已经有一个实例在运行。", Constant.ProjectName);
                return;
            }

            string extPath = Helper.ExtPath();
            if (!Directory.Exists(extPath))
            {
                Directory.CreateDirectory(extPath);
            }

#if NET5_0_OR_GREATER
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length != 0 && args[0] == Constant.Cmd)
            {
                MainDialog mainDialog = new MainDialog();
                mainDialog.Opacity = 0;
                mainDialog.Show();

                mainDialog.Hide();
                mainDialog.Opacity = 1;

                Application.Run();
            }
            else
            {
                Application.Run(new MainDialog());
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
