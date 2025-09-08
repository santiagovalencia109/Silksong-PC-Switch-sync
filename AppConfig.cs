using System;
using System.IO;
using System.Text.Json;

namespace SwitchFileSync
{
    public class AppConfig
    {
        public string PcPath { get; set; } = "";
        public string SwitchPath { get; set; } = "";

        private static string ConfigFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig Load()
        {
            if (!File.Exists(ConfigFile))
                return new AppConfig();

            string json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }
}
