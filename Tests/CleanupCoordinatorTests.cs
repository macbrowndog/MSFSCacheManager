using MSFSCacheManager.Models;
using MSFSCacheManager.Services;

namespace MSFSCacheManager.Tests;

public class CleanupCoordinatorTests
{
    [Theory]
    [InlineData("MSFS2020")]
    [InlineData("MSFS2024")]
    public async Task WasmLooseFiles_BacksUpOnlyRootFilesAndPreservesAllDirectories(string version)
    {
        using TempDirectory temp = new();
        string root = temp.GetPath(version);
        Directory.CreateDirectory(Path.Combine(root, "aircraft", "work"));
        Directory.CreateDirectory(Path.Combine(root, "empty"));
        File.WriteAllText(Path.Combine(root, "cache.bin"), "loose cache");
        File.WriteAllText(Path.Combine(root, "hidden.dat"), "hidden cache");
        File.SetAttributes(Path.Combine(root, "hidden.dat"), FileAttributes.Hidden);
        string nested = Path.Combine(root, "aircraft", "work", "settings.dat");
        File.WriteAllText(nested, "aircraft data");
        var factory = new CacheCleanupDefinitionFactory(new CacheManagerService());
        var definition = factory.CreateWasmLooseFilesCleanup(version, root);
        Assert.Equal(CacheItemType.File, definition.Groups[0].ItemType);
        Assert.Equal(2, definition.Groups[0].Locations.Count);
        var backup = new BackupService(temp.GetPath("Backups"));
        var result = await new CleanupCoordinator(backup).ExecuteAsync(definition, null, CancellationToken.None);
        Assert.Equal(2, result.BackupResult.FilesMoved);
        Assert.Equal(0, result.BackupResult.ErrorCount);
        Assert.Empty(Directory.GetFiles(root));
        Assert.True(Directory.Exists(Path.Combine(root, "empty")));
        Assert.Equal("aircraft data", File.ReadAllText(nested));
        var manifest = backup.LoadManifest(result.BackupSession)!;
        Assert.Equal(2, manifest.Entries.Count);
        Assert.All(manifest.Entries, e => Assert.True(File.Exists(e.BackupPath)));
        Assert.Contains(manifest.Entries, e => File.ReadAllText(e.BackupPath) == "loose cache");
        Assert.Contains(manifest.Entries, e => File.ReadAllText(e.BackupPath) == "hidden cache");
        var empty = factory.CreateWasmLooseFilesCleanup(version, root);
        Assert.Empty(empty.Groups[0].Locations);
    }
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

