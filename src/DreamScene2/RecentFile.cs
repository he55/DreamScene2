using System.Collections.Generic;
using System.IO;

namespace DreamScene2
{
    public static class RecentFile
    {
        static readonly string s_recentPath = Helper.GetPathForAppFolder("recent.txt");
        static readonly List<string> s_recentFiles = new List<string>();

        public static List<string> Load()
        {
            if (File.Exists(s_recentPath))
            {
                string[] paths = File.ReadAllLines(s_recentPath);
                s_recentFiles.AddRange(paths);
            }
            return s_recentFiles;
        }

        public static void Save()
        {
            File.WriteAllLines(s_recentPath, s_recentFiles);
        }

        public static void Clean()
        {
            s_recentFiles.Clear();
            File.WriteAllText(s_recentPath, "");
        }

        public static void Update(string path)
        {
            if (s_recentFiles.Count == 0 || s_recentFiles[0] != path)
            {
                if (s_recentFiles.Contains(path))
                    s_recentFiles.Remove(path);

                s_recentFiles.Insert(0, path);
            }
        }
    }
}
