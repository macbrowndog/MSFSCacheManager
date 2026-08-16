using MSFSCacheManager.Models;
using MSFSCacheManager.Services;

namespace MSFSCacheManager.Tests;

public class CleanupCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_ProcessesMixedGroupsAndCreatesManifest()
    {
        using TempDirectory temp = new();

        string directory = temp.GetPath("Active", "DirectoryCache");
        string file = temp.GetPath("Active", "ROLLINGCACHE.CCC");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "directory.bin"), "directory");
        File.WriteAllText(file, "file");

        BackupService backup = new(temp.GetPath("Backups"));
        CleanupCoordinator coordinator = new(backup);
        CacheCleanupDefinition definition = new()
        {
            OperationName = "Mixed",
            ReportTitle = "MIXED TEST",
            Groups = new List<CacheCleanupGroup>
            {
                new()
                {
                    Heading = "DIRECTORY",
                    BackupCategory = "Directory",
                    ItemType = CacheItemType.DirectoryContents,
                    Locations = new List<string> { directory }
                },
                new()
                {
                    Heading = "FILE",
                    BackupCategory = "File",
                    ItemType = CacheItemType.File,
                    Locations = new List<string> { file }
                }
            }
        };

        CacheCleanupResult result = await coordinator.ExecuteAsync(
            definition,
            null,
            CancellationToken.None);

        Assert.True(result.FoundAnyCache);
        Assert.Equal(2, result.BackupResult.FilesMoved);
        BackupManifest manifest = Assert.IsType<BackupManifest>(
            backup.LoadManifest(result.BackupSession));
        Assert.Equal(2, manifest.Entries.Count);
    }

    [Fact]
    public async Task ExecuteAsync_NoCacheDoesNotCreateSession()
    {
        using TempDirectory temp = new();

        string backupRoot = temp.GetPath("Backups");
        BackupService backup = new(backupRoot);
        CleanupCoordinator coordinator = new(backup);
        CacheCleanupDefinition definition = new()
        {
            Groups = new List<CacheCleanupGroup>
            {
                new()
                {
                    ItemType = CacheItemType.DirectoryContents,
                    Locations = new List<string> { temp.GetPath("Missing") }
                }
            }
        };

        CacheCleanupResult result = await coordinator.ExecuteAsync(
            definition,
            null,
            CancellationToken.None);

        Assert.False(result.FoundAnyCache);
        Assert.False(Directory.Exists(backupRoot));
    }
}
