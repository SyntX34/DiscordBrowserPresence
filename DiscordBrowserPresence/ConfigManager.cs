using Newtonsoft.Json;
using System;
using System.IO;

namespace DiscordBrowserPresence
{
    public class AppConfig
    {
        public string DiscordApplicationId { get; set; } = "";
        public int UpdateIntervalSeconds { get; set; } = 3;
        public bool StartMinimized { get; set; } = false;
        public bool AutoStart { get; set; } = true;
        public bool FirstTimeSetup { get; set; } = true;
    }

    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DiscordBrowserPresence",
            "config.json"
        );

        public AppConfig Config { get; private set; }

        public ConfigManager()
        {
            Config = new AppConfig();
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    SaveConfig();
                }
            }
            catch
            {
                Config = new AppConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                string? directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public static string GetConfigPath()
        {
            return ConfigPath;
        }
    }
}