using System.IO;
using System.Xml.Serialization;

namespace DreamScene2
{
    public class Settings
    {
        static string s_filePath = Helper.GetPathForUserAppDataFolder("settings.xml");
        static Settings s_settings;

        private Settings() { }

        public bool FirstRun { get; set; } = true;
        public bool AutoPlay { get; set; } = true;
        public int PlayMode { get; set; }

        public bool AutoPause1 { get; set; }
        public bool AutoPause2 { get; set; }
        public bool AutoPause3 { get; set; }

        public bool IsMuted { get; set; }
        public bool DisableWebSecurity { get; set; } = true;
        public bool UseDesktopInteraction { get; set; } = true;

        public bool CanPause()
        {
            return AutoPause1 || AutoPause2 || AutoPause3;
        }

        public static Settings Load()
        {
            if (s_settings != null)
                return s_settings;

            if (!File.Exists(s_filePath))
            {
                s_settings = new Settings();
                return s_settings;
            }

            using (FileStream fileStream = File.OpenRead(s_filePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                s_settings = (Settings)xmlSerializer.Deserialize(fileStream);
            }
            return s_settings;
        }

        public static void Save()
        {
            using (FileStream fileStream = File.Create(s_filePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                xmlSerializer.Serialize(fileStream, s_settings);
            }
        }
    }
}
