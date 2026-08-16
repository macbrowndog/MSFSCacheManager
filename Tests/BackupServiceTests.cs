using MSFSCacheManager.Models;
using MSFSCacheManager.Services;
using System.Text.Json;

namespace MSFSCacheManager.Tests;

public class BackupServiceTests
{
    [Fact]
    public void BackupRootInsideSource_IsBlockedWithoutMovingData()
    {
        using TempDirectory temp = new();

        string source = temp.GetPath("Cache");
        string backupRoot = Path.Combine(source, "Backups");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "cache.bin"), "data");

        BackupService service = new(backupRoot);
        string session = service.CreateBackupSession();
        List<string> report = new();

        BackupResult result = service.MoveDirectoryContentsToBackup(
            source,
            session,
            "Test",
            report);

        Assert.Equal(1, result.ErrorCount);
        Assert.True(File.Exists(Path.Combine(source, "cache.bin")));
        Assert.Contains(report, line => line.Contains("overlap"));
    }

    [Fact]
    public void SourceInsideBackupRoot_IsBlockedWithoutMovingData()
    {
        using TempDirectory temp = new();

        string backupRoot = temp.GetPath("Backups");
        string source = Path.Combine(backupRoot, "ActiveCache");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "cache.bin"), "data");

        BackupService service = new(backupRoot);
        string session = service.CreateBackupSession();

        BackupResult result = service.MoveDirectoryContentsToBackup(
            source,
            session,
            "Test",
            new List<string>());

        Assert.Equal(1, result.ErrorCount);
        Assert.True(File.Exists(Path.Combine(source, "cache.bin")));
    }

    [Fact]
    public void SameNamedSources_GetDistinctManifestDestinations()
    {
        using TempDirectory temp = new();

        string first = temp.GetPath("One", "Cache");
        string second = temp.GetPath("Two", "Cache");
        Directory.CreateDirectory(first);
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(first, "same.bin"), "first");
        File.WriteAllText(Path.Combine(second, "same.bin"), "second");

        BackupService service = new(temp.GetPath("Backups"));
        string session = service.CreateBackupSession();
        List<string> report = new();

        service.MoveDirectoryContentsToBackup(first, session, "Test", report);
        service.MoveDirectoryContentsToBackup(second, session, "Test", report);

        BackupManifest manifest = Assert.IsType<BackupManifest>(
            service.LoadManifest(session));

        Assert.Equal(2, manifest.Entries.Count);
        Assert.NotEqual(
            manifest.Entries[0].BackupPath,
            manifest.Entries[1].BackupPath,
            StringComparer.OrdinalIgnoreCase);
        Assert.All(manifest.Entries, entry =>
            Assert.True(Directory.Exists(entry.BackupPath)));
    }

    [Fact]
    public void LockedFile_ProducesPartialBackupWithoutDataLoss()
    {
        using TempDirectory temp = new();

        string source = temp.GetPath("Cache");
        Directory.CreateDirectory(source);
        string lockedPath = Path.Combine(source, "locked.bin");
        string movablePath = Path.Combine(source, "movable.bin");
        File.WriteAllText(lockedPath, "locked");
        File.WriteAllText(movablePath, "movable");

        BackupService service = new(temp.GetPath("Backups"));
        string session = service.CreateBackupSession();

        using (FileStream locked = new(
                   lockedPath,
                   FileMode.Open,
                   FileAccess.ReadWrite,
                   FileShare.None))
        {
            BackupResult result = service.MoveDirectoryContentsToBackup(
                source,
                session,
                "Test",
                new List<string>());

            Assert.Equal(1, result.FilesMoved);
            Assert.Equal(1, result.FilesSkipped);
            Assert.Equal(1, result.ErrorCount);
            Assert.True(File.Exists(lockedPath));
            Assert.False(File.Exists(movablePath));
        }
    }

    [Fact]
    public void DirectoryMoveFailure_UsesRecursiveFallback()
    {
        using TempDirectory temp = new();

        string source = temp.GetPath("Cache");
        string nested = Path.Combine(source, "Nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "cache.bin"), "data");

        BackupService service = new(
            temp.GetPath("Backups"),
            (_, _) => throw new IOException("Simulated cross-volume move."));

        string session = service.CreateBackupSession();

        BackupResult result = service.MoveDirectoryContentsToBackup(
            source,
            session,
            "Test",
            new List<string>());

        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(1, result.FilesMoved);
        Assert.False(File.Exists(Path.Combine(nested, "cache.bin")));
    }

    [Fact]
    public void RestoreConflict_IsPreservedAndCanBeRetried()
    {
        using TempDirectory temp = new();

        string source = temp.GetPath("ROLLINGCACHE.CCC");
        File.WriteAllText(source, "original");

        BackupService service = new(temp.GetPath("Backups"));
        string session = service.CreateBackupSession();

        service.MoveFileToBackup(
            source,
            session,
            "RollingCache",
            new List<string>());

        File.WriteAllText(source, "new-active");

        RestoreResult firstRestore = service.RestoreBackupSession(session);

        Assert.Equal(1, firstRestore.ConflictsSkipped);
        Assert.Equal("new-active", File.ReadAllText(source));
        Assert.True(service.HasRestoreManifest(session));

        File.Delete(source);

        RestoreResult secondRestore = service.RestoreBackupSession(session);

        Assert.Equal("original", File.ReadAllText(source));
        Assert.False(service.HasRestoreManifest(session));
    }

    [Fact]
    public async Task PreCancelledBackup_DoesNotMoveData()
    {
        using TempDirectory temp = new();

        string source = temp.GetPath("Cache");
        Directory.CreateDirectory(source);
        string file = Path.Combine(source, "cache.bin");
        File.WriteAllText(file, "data");

        BackupService service = new(temp.GetPath("Backups"));
        string session = service.CreateBackupSession();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.MoveDirectoryContentsToBackupAsync(
                source,
                session,
                "Test",
                new List<string>(),
                null,
                cancellation.Token));

        Assert.True(File.Exists(file));
    }

    [Fact]
    public void ManifestBackupPathOutsideSession_IsRejected()
    {
        using TempDirectory temp = new();

        string source = temp.GetPath("ROLLINGCACHE.CCC");
        string external = temp.GetPath("external.bin");
        File.WriteAllText(source, "cache");
        File.WriteAllText(external, "external");

        BackupService service = new(temp.GetPath("Backups"));
        string session = service.CreateBackupSession();
        service.MoveFileToBackup(
            source,
            session,
            "RollingCache",
            new List<string>());

        BackupManifest manifest = Assert.IsType<BackupManifest>(
            service.LoadManifest(session));
        manifest.Entries[0].BackupPath = external;

        File.WriteAllText(
            Path.Combine(session, "backup_manifest.json"),
            JsonSerializer.Serialize(manifest));

        RestoreResult result = service.RestoreBackupSession(session);

        Assert.Equal(1, result.ErrorCount);
        Assert.Equal("external", File.ReadAllText(external));
    }
}
