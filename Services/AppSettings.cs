using System;
using System.IO;
using System.Text.Json;

namespace TrustedMaster.Services
{
    public sealed class AppSettings
    {
        public string DefaultUserMode { get; set; } = "TrustedInstaller";

        public bool DefaultEnableAllPrivileges { get; set; }

        public static AppSettings Load()
        {
            try
            {
                string file = StoragePaths.SettingsFile;
                if (!File.Exists(file))
                {
                    return new AppSettings();
                }

                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file)) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(StoragePaths.SettingsFile, JsonSerializer.Serialize(this, options));
            }
            catch
            {
            }
        }
    }
}
