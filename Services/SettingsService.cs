using MSFSCacheManager.Models;
using System;
using System.IO;
using System.Text.Json;

namespace MSFSCacheManager.Services
{
    public class SettingsService
    {
        private readonly string _settingsFolder;
        private readonly string _settingsFile;

        public SettingsService()
        {
            _settingsFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MSFSCacheManager");

            _settingsFile = Path.Combine(
                _settingsFolder,
                "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    string json = File.ReadAllText(_settingsFile);

                    AppSettings? settings =
                        JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        return EnsureDefaults(settings);
                    }
                }
            }
            catch
            {
                // Use safe defaults if settings cannot be read.
            }

            return CreateDefaultSettings();
        }

        public void Save(AppSettings settings)
        {
            AppSettings safeSettings = EnsureDefaults(settings);

            Directory.CreateDirectory(_settingsFolder);

            string json = JsonSerializer.Serialize(
                safeSettings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_settingsFile, json);
        }

        public AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                BackupFolder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments),
                    "MSFS Cache Manager",
                    "Backups")
            };
        }

        private AppSettings EnsureDefaults(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.BackupFolder))
            {
                settings.BackupFolder =
                    CreateDefaultSettings().BackupFolder;
            }

            return settings;
        }
    }
}
