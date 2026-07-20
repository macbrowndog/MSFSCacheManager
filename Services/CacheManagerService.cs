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
        }

        // ---------------------------------------------------------
        // NVIDIA SHADER CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetNvidiaShaderCacheLocations()
        {
            return new List<string>
            {
                Path.Combine(_localAppData, "D3DSCache"),

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
                    "SceneryIndexes"),

                Path.Combine(
                    msfs2024Package,
                    "LocalState",
                    "StreamedPackages")
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
        // -------------------------------------------------
        // STEAM / STANDARD MSFS 2020
        // -------------------------------------------------

        Path.Combine(
            _roamingAppData,
            "Microsoft Flight Simulator",
            "cache"),

        // -------------------------------------------------
        // STEAM / STANDARD MSFS 2024
        // -------------------------------------------------

        Path.Combine(
            _roamingAppData,
            "Microsoft Flight Simulator 2024",
            "cache"),

        // -------------------------------------------------
        // MICROSOFT STORE MSFS 2020
        // -------------------------------------------------

        Path.Combine(
            msfs2020StorePackage,
            "LocalState",
            "cache"),

        // -------------------------------------------------
        // MICROSOFT STORE MSFS 2024
        // -------------------------------------------------

        Path.Combine(
            msfs2024StorePackage,
            "LocalState",
            "Cache")
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
        // Steam / standard MSFS 2020

        Path.Combine(
            _roamingAppData,
            "Microsoft Flight Simulator",
            "ROLLINGCACHE.CCC"),

        // Steam / standard MSFS 2024

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
        // GET ALL EXISTING CACHE LOCATIONS
        // ---------------------------------------------------------

        public List<string> GetExistingCacheLocations()
        {
            List<string> allLocations = new();

            allLocations.AddRange(
                GetNvidiaShaderCacheLocations());

            allLocations.AddRange(
                GetAmdShaderCacheLocations());

            allLocations.AddRange(
                GetSteamMSFSCacheLocations());

            allLocations.AddRange(
                GetStoreMSFSCacheLocations());

            return allLocations.FindAll(
                CacheLocationExists);
        }
    }
}