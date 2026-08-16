using MSFSCacheManager.Models;
using MSFSCacheManager.Services;

namespace MSFSCacheManager.Tests;

public class CacheScanServiceTests
{
    [Fact]
    public void Scan_CalculatesMetadataDeduplicatesAndSorts()
    {
        using TempDirectory temp = new();

        string directory = temp.GetPath(
            "Microsoft.Limitless_8wekyb3d8bbwe",
            "StreamedPackages");
        string nested = Path.Combine(directory, "Nested");
        string rollingCache = temp.GetPath("ROLLINGCACHE.CCC");
        Directory.CreateDirectory(nested);
        File.WriteAllBytes(
            Path.Combine(directory, "one.bin"),
            new byte[100]);
        File.WriteAllBytes(
            Path.Combine(nested, "two.bin"),
            new byte[300]);
        File.WriteAllBytes(rollingCache, new byte[50]);

        CacheCleanupDefinition directoryDefinition = new()
        {
            OperationName = "Streamed Packages",
            RiskLevel = "Advanced",
            Groups = new List<CacheCleanupGroup>
            {
                new()
                {
                    BackupCategory = "StreamedPackages",
                    ItemType = CacheItemType.DirectoryContents,
                    Locations = new List<string> { directory, directory }
                }
            }
        };

        CacheCleanupDefinition fileDefinition = new()
        {
            OperationName = "Rolling Cache",
            RiskLevel = "Standard",
            Groups = new List<CacheCleanupGroup>
            {
                new()
                {
                    BackupCategory = "RollingCache",
                    ItemType = CacheItemType.File,
                    Locations = new List<string> { rollingCache }
                }
            }
        };

        CacheManagerService cacheManager = new();
        CacheScanService scanner = new(
            new CacheCleanupDefinitionFactory(cacheManager));

        List<CacheScanItem> results = scanner.Scan(
            new[] { fileDefinition, directoryDefinition },
            null,
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(400, results[0].SizeBytes);
        Assert.Equal(2, results[0].FileCount);
        Assert.Equal("Streamed Packages", results[0].Category);
        Assert.Equal("MSFS 2024", results[0].Simulator);
        Assert.Equal("Microsoft Store", results[0].Platform);
        Assert.Equal("Advanced", results[0].RiskLevel);
        Assert.True(results[0].IsSelected);
        Assert.Equal("400 B", results[0].FormattedSize);
        Assert.NotNull(results[0].LastModified);
        Assert.Equal(50, results[1].SizeBytes);
    }

    [Fact]
    public void Scan_HonorsCancellation()
    {
        CacheManagerService cacheManager = new();
        CacheScanService scanner = new(
            new CacheCleanupDefinitionFactory(cacheManager));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(
            () => scanner.Scan(
                Array.Empty<CacheCleanupDefinition>(),
                null,
                cancellation.Token));
    }
}
