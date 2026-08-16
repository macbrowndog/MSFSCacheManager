using MSFSCacheManager.Models;
using System.Collections.Generic;

namespace MSFSCacheManager.Services
{
    public class CacheCleanupDefinitionFactory
    {
        private readonly CacheManagerService _cacheManager;

        public CacheCleanupDefinitionFactory(
            CacheManagerService cacheManager)
        {
            _cacheManager = cacheManager;
        }

        public CacheCleanupDefinition CreateGpuCleanup()
        {
            return new CacheCleanupDefinition
            {
                OperationName = "GPU Shader Cache",
                ReportTitle = "GPU SHADER CACHE CLEANUP",
                ProcessingStatus = "Processing GPU shader caches...",
                EmptyStatus = "No GPU shader cache folders found.",
                EmptyMessage =
                    "No NVIDIA or AMD shader cache folders were found on this computer.",
                EmptyTitle = "GPU Shader Cache",
                FailureStatus = "GPU shader cache cleanup failed.",
                FailureTitle = "GPU Shader Cache Error",
                RiskLevel = "Standard",
                Groups = new List<CacheCleanupGroup>
                {
                    CreateGroup(
                        "NVIDIA SHADER CACHES",
                        "NVIDIA",
                        CacheItemType.DirectoryContents,
                        _cacheManager.GetNvidiaShaderCacheLocations()),
                    CreateGroup(
                        "AMD SHADER CACHES",
                        "AMD",
                        CacheItemType.DirectoryContents,
                        _cacheManager.GetAmdShaderCacheLocations())
                }
            };
        }

        public CacheCleanupDefinition CreateRollingCacheCleanup()
        {
            CacheCleanupDefinition definition = CreateDefinition(
                "Rolling Cache",
                "MSFS ROLLING CACHE CLEANUP",
                "RollingCache",
                CacheItemType.File,
                _cacheManager.GetRollingCacheLocations(),
                "Searching for MSFS rolling cache files...",
                "No rolling cache files found.",
                "No ROLLINGCACHE.CCC files were found in the known MSFS locations.",
                "MSFS Rolling Cache",
                "Rolling cache cleanup failed.",
                "Rolling Cache Error");

            definition.Groups[0].Heading = "ROLLING CACHE";

            return definition;
        }

        public CacheCleanupDefinition CreateMsfsCacheCleanup() =>
            CreateDirectoryDefinition(
                "MSFS Cache", "MSFS GENERAL CACHE CLEANUP", "MSFSCache",
                _cacheManager.GetMSFSCacheLocations(),
                "Processing MSFS cache folders...", "No MSFS cache folders found.",
                "No general MSFS cache folders were found in the known locations.",
                "MSFS Cache", "MSFS cache cleanup failed.", "MSFS Cache Error");

        public CacheCleanupDefinition CreateSceneryCacheCleanup() =>
            CreateDirectoryDefinition(
                "Scenery Cache", "MSFS SCENERY CACHE CLEANUP", "SceneryCache",
                _cacheManager.GetSceneryCacheLocations(),
                "Processing Scenery Cache...", "No Scenery Cache folders found.",
                "No Scenery Cache folders were found in the known MSFS locations.",
                "Scenery Cache", "Scenery Cache cleanup failed.",
                "Scenery Cache Error");

        public CacheCleanupDefinition CreateSceneryIndexesCleanup() =>
            CreateDirectoryDefinition(
                "Scenery Indexes", "MSFS SCENERY INDEXES CLEANUP",
                "SceneryIndexes", _cacheManager.GetSceneryIndexesLocations(),
                "Processing Scenery Indexes...", "No Scenery Indexes folders found.",
                "No Scenery Indexes folders were found in the known MSFS locations.",
                "Scenery Indexes", "Scenery Indexes cleanup failed.",
                "Scenery Indexes Error");

        public CacheCleanupDefinition CreateDceCleanup() =>
            CreateDirectoryDefinition(
                "DCE Cache", "MSFS 2020 DCE CACHE CLEANUP", "DCECache",
                _cacheManager.GetDCECacheLocations(), "Processing DCE Cache...",
                "No DCE Cache folders found.",
                "No DCE Cache folders were found in the known MSFS 2020 locations.",
                "DCE Cache", "DCE Cache cleanup failed.", "DCE Cache Error");

        public CacheCleanupDefinition CreateStreamedPackagesCleanup() =>
            CreateDirectoryDefinition(
                "Streamed Packages", "MSFS 2024 STREAMED PACKAGES CLEANUP",
                "StreamedPackages", _cacheManager.GetStreamedPackagesLocations(),
                "Processing Streamed Packages...",
                "No Streamed Packages folders found.",
                "No Streamed Packages folders were found in the known MSFS 2024 locations.",
                "Streamed Packages", "Streamed Packages cleanup failed.",
                "Streamed Packages Error");

        public CacheCleanupDefinition CreateSimObjectsCleanup() =>
            CreateDirectoryDefinition(
                "SimObjects Cache", "MSFS SIMOBJECTS CACHE CLEANUP", "SimObjects",
                _cacheManager.GetSimObjectsCacheLocations(),
                "Processing SimObjects Cache...", "No SimObjects cache folders found.",
                "No SimObjects cache folders were found in the known MSFS locations.",
                "SimObjects Cache", "SimObjects cleanup failed.",
                "SimObjects Cache Error");

        public CacheCleanupDefinition CreateWasmCleanup() =>
            CreateDirectoryDefinition(
                "WASM Cache", "MSFS WASM CACHE CLEANUP", "WASMCache",
                _cacheManager.GetWASMCacheLocations(), "Processing WASM Cache...",
                "No WASM Cache folders found.",
                "No WASM Cache folders were found in the known MSFS locations.",
                "WASM Cache", "WASM Cache cleanup failed.", "WASM Cache Error");

        public List<CacheCleanupDefinition> CreateAll()
        {
            return new List<CacheCleanupDefinition>
            {
                CreateGpuCleanup(),
                CreateRollingCacheCleanup(),
                CreateMsfsCacheCleanup(),
                MarkAdvanced(CreateSceneryCacheCleanup()),
                MarkAdvanced(CreateSceneryIndexesCleanup()),
                MarkAdvanced(CreateDceCleanup()),
                MarkAdvanced(CreateStreamedPackagesCleanup()),
                MarkAdvanced(CreateSimObjectsCleanup()),
                MarkAdvanced(CreateWasmCleanup())
            };
        }

        private CacheCleanupDefinition MarkAdvanced(
            CacheCleanupDefinition definition)
        {
            definition.RiskLevel = "Advanced";
            return definition;
        }

        private CacheCleanupDefinition CreateDirectoryDefinition(
            string operationName,
            string reportTitle,
            string category,
            List<string> locations,
            string processingStatus,
            string emptyStatus,
            string emptyMessage,
            string emptyTitle,
            string failureStatus,
            string failureTitle)
        {
            return CreateDefinition(
                operationName,
                reportTitle,
                category,
                CacheItemType.DirectoryContents,
                locations,
                processingStatus,
                emptyStatus,
                emptyMessage,
                emptyTitle,
                failureStatus,
                failureTitle);
        }

        private CacheCleanupDefinition CreateDefinition(
            string operationName,
            string reportTitle,
            string category,
            CacheItemType itemType,
            List<string> locations,
            string processingStatus,
            string emptyStatus,
            string emptyMessage,
            string emptyTitle,
            string failureStatus,
            string failureTitle)
        {
            return new CacheCleanupDefinition
            {
                OperationName = operationName,
                ReportTitle = reportTitle,
                ProcessingStatus = processingStatus,
                EmptyStatus = emptyStatus,
                EmptyMessage = emptyMessage,
                EmptyTitle = emptyTitle,
                FailureStatus = failureStatus,
                FailureTitle = failureTitle,
                Groups = new List<CacheCleanupGroup>
                {
                    CreateGroup(
                        operationName.ToUpperInvariant(),
                        category,
                        itemType,
                        locations)
                }
            };
        }

        private CacheCleanupGroup CreateGroup(
            string heading,
            string category,
            CacheItemType itemType,
            List<string> locations)
        {
            return new CacheCleanupGroup
            {
                Heading = heading,
                BackupCategory = category,
                ItemType = itemType,
                Locations = locations
            };
        }
    }
}
