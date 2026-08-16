using System;
using System.Collections.Generic;
using System.IO;

namespace MSFSCacheManager.Services
{
    public class CacheManagerService
    {
        // Windows user folders
        private readonly string _localAppData;
        private readonly string _roamingAppData;
        private readonly string _userProfile;
        private readonly InstallationService _installationService;

        public CacheManagerService()
        {
            _localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            _roamingAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);

            _userProfile =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);

            _installationService =
                new InstallationService();
        }

        // ---------------------------------------------------------
        // NVIDIA SHADER CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetNvidiaShaderCacheLocations()
        {
            return new List<string>
            {
                Path.Combine(
                    _localAppData,
                    "D3DSCache"),

                Path.Combine(
                    _localAppData,
                    "NVIDIA",
                    "GLCache"),

                Path.Combine(
                    _localAppData,
                    "NVIDIA",
                    "PerDriverVersion",
                    "GLCache"),

                Path.Combine(
                    _roamingAppData,
                    "NVIDIA",
                    "ComputeCache"),

                Path.Combine(
                    _userProfile,
                    "AppData",
                    "LocalLow",
                    "NVIDIA",
                    "PerDriverVersion",
                    "DXCache"),

                Path.Combine(
                    _userProfile,
                    "AppData",
                    "LocalLow",
                    "NVIDIA",
                    "PerDriverVersion",
                    "GLCache")
            };
        }

        // ---------------------------------------------------------
        // AMD SHADER CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetAmdShaderCacheLocations()
        {
            return new List<string>
            {
                Path.Combine(
                    _localAppData,
                    "AMD",
                    "DxCache"),

                Path.Combine(
                    _localAppData,
                    "AMD",
                    "DX9Cache"),

                Path.Combine(
                    _localAppData,
                    "AMD",
                    "DxcCache"),

                Path.Combine(
                    _localAppData,
                    "AMD",
                    "OglCache")
            };
        }

        // ---------------------------------------------------------
        // MSFS STEAM CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetSteamMSFSCacheLocations()
        {
            return new List<string>
            {
                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "cache"),

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "SceneryCache"),

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "SceneryIndexes"),

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "DCE"),

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "cache"),

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "SceneryIndexes"),

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "Packages",
                    "StreamedPackages")
            };
        }

        // ---------------------------------------------------------
        // MICROSOFT STORE CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetStoreMSFSCacheLocations()
        {
            string msfs2020Package =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            string msfs2024Package =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            return new List<string>
            {
                Path.Combine(
                    msfs2020Package,
                    "LocalCache",
                    "SceneryCache"),

                Path.Combine(
                    msfs2020Package,
                    "LocalCache",
                    "SceneryIndexes"),

                Path.Combine(
                    msfs2020Package,
                    "LocalState",
                    "cache"),

                Path.Combine(
                    msfs2020Package,
                    "LocalState",
                    "DCE"),

                Path.Combine(
                    msfs2024Package,
                    "LocalState",
                    "Cache"),

                Path.Combine(
                    msfs2024Package,
                    "LocalCache",
                    "SceneryIndexes")
            };
        }

        // ---------------------------------------------------------
        // MSFS GENERAL CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetMSFSCacheLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            string msfs2024StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "cache"),

                // Steam / Standard MSFS 2024

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "cache"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalState",
                    "cache"),

                // Microsoft Store MSFS 2024

                Path.Combine(
                    msfs2024StorePackage,
                    "LocalState",
                    "Cache")
            };
        }

        // ---------------------------------------------------------
        // MSFS SCENERY CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetSceneryCacheLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "SceneryCache"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalCache",
                    "SceneryCache")
            };
        }

        // ---------------------------------------------------------
        // MSFS SCENERY INDEX LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetSceneryIndexesLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            string msfs2024StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "SceneryIndexes"),

                // Steam / Standard MSFS 2024

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "SceneryIndexes"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalCache",
                    "SceneryIndexes"),

                // Microsoft Store MSFS 2024

                Path.Combine(
                    msfs2024StorePackage,
                    "LocalCache",
                    "SceneryIndexes")
            };
        }

        // ---------------------------------------------------------
        // MSFS 2020 DCE CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetDCECacheLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "DCE"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalState",
                    "DCE")
            };
        }

        // ---------------------------------------------------------
        // MSFS STREAMED PACKAGES LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetStreamedPackagesLocations()
        {
            List<string> locations =
                new List<string>();

            // Steam / Standard MSFS 2024

            AddUniqueLocation(
                locations,
                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "Packages",
                    "StreamedPackages"));

            // Microsoft Store MSFS 2024 fallback location

            string msfs2024StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            AddUniqueLocation(
                locations,
                Path.Combine(
                    msfs2024StorePackage,
                    "LocalCache",
                    "Packages",
                    "StreamedPackages"));

            // Custom Packages folder detected from UserCfg.opt.
            // Example:
            // D:\MSFS2024\Packages\StreamedPackages

            string? installedPackagesPath =
                _installationService.GetInstalledPackagesPath();

            if (!string.IsNullOrWhiteSpace(installedPackagesPath))
            {
                AddUniqueLocation(
                    locations,
                    Path.Combine(
                        installedPackagesPath,
                        "StreamedPackages"));
            }

            return locations;
        }

        // ---------------------------------------------------------
        // MSFS SIMOBJECTS CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetSimObjectsCacheLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            string msfs2024StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "SimObjects"),

                // Steam / Standard MSFS 2024

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "SimObjects"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalCache",
                    "SimObjects"),

                // Microsoft Store MSFS 2024

                Path.Combine(
                    msfs2024StorePackage,
                    "LocalCache",
                    "SimObjects")
            };
        }

        // ---------------------------------------------------------
        // MSFS WASM CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetWASMCacheLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            string msfs2024StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "Packages",
                    "wasm"),

                // Steam / Standard MSFS 2024

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "Packages",
                    "wasm"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalState",
                    "Packages",
                    "wasm"),

                // Microsoft Store MSFS 2024
                // Verified:
                // ...\Microsoft.Limitless_8wekyb3d8bbwe\LocalState\WASM

                Path.Combine(
                    msfs2024StorePackage,
                    "LocalState",
                    "WASM")
            };
        }

        // ---------------------------------------------------------
        // MSFS ROLLING CACHE FILE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetRollingCacheLocations()
        {
            string msfs2020StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.FlightSimulator_8wekyb3d8bbwe");

            string msfs2024StorePackage =
                Path.Combine(
                    _localAppData,
                    "Packages",
                    "Microsoft.Limitless_8wekyb3d8bbwe");

            return new List<string>
            {
                // Steam / Standard MSFS 2020

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator",
                    "ROLLINGCACHE.CCC"),

                // Steam / Standard MSFS 2024

                Path.Combine(
                    _roamingAppData,
                    "Microsoft Flight Simulator 2024",
                    "ROLLINGCACHE.CCC"),

                // Microsoft Store MSFS 2020

                Path.Combine(
                    msfs2020StorePackage,
                    "LocalCache",
                    "ROLLINGCACHE.CCC"),

                // Microsoft Store MSFS 2024

                Path.Combine(
                    msfs2024StorePackage,
                    "LocalCache",
                    "ROLLINGCACHE.CCC")
            };
        }

        // ---------------------------------------------------------
        // CHECK IF A CACHE LOCATION EXISTS
        // ---------------------------------------------------------

        public bool CacheLocationExists(string path)
        {
            return Directory.Exists(path) ||
                   File.Exists(path);
        }

        // ---------------------------------------------------------
        // GET ALL KNOWN CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetAllCacheLocations()
        {
            List<string> locations = new();

            foreach (List<string> cacheGroup in new[]
            {
                GetNvidiaShaderCacheLocations(),
                GetAmdShaderCacheLocations(),
                GetSteamMSFSCacheLocations(),
                GetStoreMSFSCacheLocations(),
                GetMSFSCacheLocations(),
                GetSceneryCacheLocations(),
                GetSceneryIndexesLocations(),
                GetDCECacheLocations(),
                GetStreamedPackagesLocations(),
                GetSimObjectsCacheLocations(),
                GetWASMCacheLocations(),
                GetRollingCacheLocations()
            })
            {
                foreach (string path in cacheGroup)
                {
                    AddUniqueLocation(locations, path);
                }
            }

            return locations;
        }

        // ---------------------------------------------------------
        // GET ALL EXISTING CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetExistingCacheLocations()
        {
            List<string> existingLocations =
                new List<string>();

            foreach (string path in GetAllCacheLocations())
            {
                if (CacheLocationExists(path))
                {
                    AddUniqueLocation(
                        existingLocations,
                        path);
                }
            }

            return existingLocations;
        }

        // ---------------------------------------------------------
        // ADD UNIQUE LOCATION
        // ---------------------------------------------------------

        private void AddUniqueLocation(
            List<string> locations,
            string location)
        {
            foreach (string existingLocation in locations)
            {
                if (string.Equals(
                    existingLocation,
                    location,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            locations.Add(location);
        }
    }
}
