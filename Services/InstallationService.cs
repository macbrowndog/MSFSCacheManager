using System;
using System.IO;

namespace MSFSCacheManager.Services
{
    public class InstallationService
    {
        private readonly string _localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        private readonly string _roamingAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

        private readonly SettingsService _settingsService;

        public InstallationService()
        {
            _settingsService = new SettingsService();
        }

        public string LocalAppData => _localAppData;

        public string RoamingAppData => _roamingAppData;

        // ---------------------------------------------------------
        // MSFS 2024 USERCFG LOCATIONS
        // ---------------------------------------------------------

        public string? GetUserCfgPath()
        {
            string storePath =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe",
                    "LocalCache",
                    "UserCfg.opt");

            if (File.Exists(storePath))
            {
                return storePath;
            }

            string steamPath =
                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "UserCfg.opt");

            if (File.Exists(steamPath))
            {
                return steamPath;
            }

            return null;
        }

        public bool HasUserCfg()
        {
            return GetUserCfgPath() != null;
        }

        // ---------------------------------------------------------
        // AUTOMATICALLY DETECTED PACKAGES FOLDER
        // ---------------------------------------------------------

        public string? GetAutomaticallyDetectedPackagesPath()
        {
            string? userCfgPath = GetUserCfgPath();

            if (string.IsNullOrWhiteSpace(userCfgPath))
            {
                return null;
            }

            foreach (string line in File.ReadAllLines(userCfgPath))
            {
                if (!line.TrimStart()
                    .StartsWith("InstalledPackagesPath"))
                {
                    continue;
                }

                int firstQuote = line.IndexOf('"');
                int lastQuote = line.LastIndexOf('"');

                if (firstQuote >= 0 &&
                    lastQuote > firstQuote)
                {
                    return line.Substring(
                        firstQuote + 1,
                        lastQuote - firstQuote - 1);
                }
            }

            return null;
        }

        // ---------------------------------------------------------
        // ACTIVE PACKAGES FOLDER
        // ---------------------------------------------------------

        public string? GetInstalledPackagesPath()
        {
            string manualOverride =
                _settingsService.Load()
                    .PackagesFolderOverride;

            if (!string.IsNullOrWhiteSpace(manualOverride) &&
                Directory.Exists(manualOverride))
            {
                return manualOverride;
            }

            return GetAutomaticallyDetectedPackagesPath();
        }
    }
}