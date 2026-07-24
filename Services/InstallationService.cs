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

        public string LocalAppData => _localAppData;

        public string RoamingAppData => _roamingAppData;

        // ---------------------------------------------------------
        // MSFS 2024 USERCFG LOCATIONS
        // ---------------------------------------------------------

        public string? GetUserCfgPath()
        {
            // Microsoft Store

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

            // Steam

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

        // ---------------------------------------------------------
        // HAS USERCFG?
        // ---------------------------------------------------------

        public bool HasUserCfg()
        {
            return GetUserCfgPath() != null;
        }

        // ---------------------------------------------------------
        // INSTALLED PACKAGES PATH
        // ---------------------------------------------------------

        public string? GetInstalledPackagesPath()
        {
            string? userCfgPath = GetUserCfgPath();

            if (string.IsNullOrWhiteSpace(userCfgPath))
            {
                return null;
            }

            foreach (string line in File.ReadAllLines(userCfgPath))
            {
                if (line.TrimStart().StartsWith("InstalledPackagesPath"))
                {
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
            }

            return null;
        }
    }
}