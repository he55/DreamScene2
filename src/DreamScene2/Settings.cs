using System.IO;
using System.Xml.Serialization;

namespace DreamScene2
{
    public class Settings
    {
        static string settingsFilePath = Helper.GetPathForAppFolder("settings.xml");
        static Settings s_settings;

        private Settings() { }

        public bool FirstRun { get; set; } = true;
        public bool AutoPlay { get; set; }

        public bool AutoPause1 { get; set; }
        public bool AutoPause2 { get; set; }
        public bool AutoPause3 { get; set; } = true;

        public bool IsMuted { get; set; }
        public int Volume { get; set; } = 3;
        public bool DisableWebSecurity { get; set; }
        public bool DesktopInteraction { get; set; } = true;

        public static Settings Load()
        {
            if (s_settings == null)
            {
                if (File.Exists(settingsFilePath))
                {
                    using (FileStream fileStream = File.OpenRead(settingsFilePath))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                        s_settings = (Settings)xmlSerializer.Deserialize(fileStream);
                    }
                }
                else
                {
                    s_settings = new Settings();
                }
            }
            return s_settings;
        }

        public static void Save()
        {
            using (FileStream fileStream = File.Create(settingsFilePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
                xmlSerializer.Serialize(fileStream, s_settings);
            }
        }
    }
}
