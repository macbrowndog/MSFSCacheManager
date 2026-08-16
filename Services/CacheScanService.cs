using MSFSCacheManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MSFSCacheManager.Services
{
    public class CacheScanService
    {
        private readonly CacheCleanupDefinitionFactory _definitions;

        public CacheScanService(
            CacheCleanupDefinitionFactory definitions)
        {
            _definitions = definitions;
        }

        public List<CacheScanItem> Scan(
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            return Scan(
                _definitions.CreateAll(),
                progress,
                cancellationToken);
        }

        public List<CacheScanItem> Scan(
            IEnumerable<CacheCleanupDefinition> definitions,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<CacheScanItem> results = new();
            HashSet<string> scannedPaths =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (CacheCleanupDefinition definition in definitions)
            {
                foreach (CacheCleanupGroup group in definition.Groups)
                {
                    foreach (string path in group.Locations)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!scannedPaths.Add(path) ||
                            !LocationExists(path, group.ItemType))
                        {
                            continue;
                        }

                        progress?.Report(
                            new BackupProgress
                            {
                                CurrentPath = path
                            });

                        CacheScanItem item = CreateScanItem(
                            path,
                            definition,
                            group,
                            cancellationToken);

                        results.Add(item);

                        progress?.Report(
                            new BackupProgress
                            {
                                CurrentPath = path,
                                ItemsProcessed = 1
                            });
                    }
                }
            }

            results.Sort(
                (left, right) => right.SizeBytes.CompareTo(left.SizeBytes));

            return results;
        }

        private bool LocationExists(
            string path,
            CacheItemType itemType)
        {
            return itemType == CacheItemType.File
                ? File.Exists(path)
                : Directory.Exists(path);
        }

        private CacheScanItem CreateScanItem(
            string path,
            CacheCleanupDefinition definition,
            CacheCleanupGroup group,
            CancellationToken cancellationToken)
        {
            long size = 0;
            int fileCount = 0;
            DateTime? lastModified = null;

            if (group.ItemType == CacheItemType.File)
            {
                FileInfo file = new(path);

                size = file.Length;
                fileCount = 1;
                lastModified = file.LastWriteTime;
            }
            else
            {
                DirectoryInfo directory = new(path);
                lastModified = directory.LastWriteTime;

                EnumerationOptions options = new()
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint
                };

                try
                {
                    foreach (string filePath in
                             Directory.EnumerateFiles(path, "*", options))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            FileInfo file = new(filePath);

                            size += file.Length;
                            fileCount++;

                            if (!lastModified.HasValue ||
                                file.LastWriteTime > lastModified.Value)
                            {
                                lastModified = file.LastWriteTime;
                            }
                        }
                        catch (IOException)
                        {
                            // A changing or locked cache file is skipped.
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Inaccessible files do not block the scan.
                        }
                    }
                }
                catch (IOException)
                {
                    // Return the information collected before the error.
                }
                catch (UnauthorizedAccessException)
                {
                    // Return the information collected before the error.
                }
            }

            return new CacheScanItem
            {
                Category = definition.OperationName,
                Simulator = InferSimulator(path, definition.OperationName),
                Platform = InferPlatform(path, group.BackupCategory),
                RiskLevel = definition.RiskLevel,
                Path = path,
                SizeBytes = size,
                FileCount = fileCount,
                LastModified = lastModified
            };
        }

        private string InferSimulator(
            string path,
            string category)
        {
            if (category == "GPU Shader Cache")
            {
                return "All simulators";
            }

            if (category == "DCE Cache")
            {
                return "MSFS 2020";
            }

            if (category == "Streamed Packages" ||
                path.Contains("2024", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("Microsoft.Limitless_", StringComparison.OrdinalIgnoreCase))
            {
                return "MSFS 2024";
            }

            return "MSFS 2020";
        }

        private string InferPlatform(
            string path,
            string backupCategory)
        {
            if (backupCategory == "NVIDIA" ||
                backupCategory == "AMD")
            {
                return backupCategory;
            }

            if (path.Contains(
                    "Microsoft.FlightSimulator_",
                    StringComparison.OrdinalIgnoreCase) ||
                path.Contains(
                    "Microsoft.Limitless_",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Microsoft Store";
            }

            if (path.Contains(
                    "AppData\\Roaming\\Microsoft Flight Simulator",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Steam / standalone";
            }

            if (backupCategory == "StreamedPackages")
            {
                return "Custom packages";
            }

            return "Local system";
        }
    }
}
