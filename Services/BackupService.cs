using System;
using System.Collections.Generic;
using MSFSCacheManager.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MSFSCacheManager.Services
{
    public class BackupService
    {
        private readonly SettingsService _settingsService;
        private readonly string? _backupRootOverride;
        private readonly Action<string, string> _directoryMove;

        public BackupService()
        {
            _settingsService = new SettingsService();
            _directoryMove = Directory.Move;
        }

        internal BackupService(
            string backupRootOverride,
            Action<string, string>? directoryMove = null)
            : this()
        {
            _backupRootOverride = backupRootOverride;

            if (directoryMove != null)
            {
                _directoryMove = directoryMove;
            }
        }

        // ---------------------------------------------------------
        // BACKUP ROOT
        // ---------------------------------------------------------

        public string GetBackupRoot()
        {
            return _backupRootOverride ??
                   _settingsService.Load().BackupFolder;
        }

        // ---------------------------------------------------------
        // CREATE BACKUP ROOT
        // ---------------------------------------------------------

        public void EnsureBackupRootExists()
        {
            Directory.CreateDirectory(GetBackupRoot());
        }

        // ---------------------------------------------------------
        // CREATE TIMESTAMPED BACKUP SESSION
        // ---------------------------------------------------------

        public string CreateBackupSession()
        {
            EnsureBackupRootExists();

            string timestamp =
                DateTime.Now.ToString(
                    "yyyy-MM-dd_HH-mm-ss");

            string sessionFolder =
                Path.Combine(
                    GetBackupRoot(),
                    timestamp);

            if (Directory.Exists(sessionFolder))
            {
                sessionFolder += $"_{Guid.NewGuid():N}"[..9];
            }

            Directory.CreateDirectory(sessionFolder);

            SaveManifest(
                sessionFolder,
                new BackupManifest());

            return sessionFolder;
        }

        // ---------------------------------------------------------
        // MOVE DIRECTORY CONTENTS TO BACKUP
        // ---------------------------------------------------------

        public BackupResult MoveDirectoryContentsToBackup(
            string sourcePath,
            string backupSession,
            string category,
            List<string> report,
            IProgress<BackupProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            BackupResult result =
                new BackupResult();

            cancellationToken.ThrowIfCancellationRequested();

            ReportProgress(progress, sourcePath, 0);

            if (!Directory.Exists(sourcePath))
            {
                report.Add($"NOT FOUND: {sourcePath}");

                result.NotFoundCount++;

                return result;
            }

            if (!IsBackupLocationSafe(
                    sourcePath,
                    report,
                    result))
            {
                return result;
            }

            report.Add("");
            report.Add($"SOURCE: {sourcePath}");

            string sourceFolderName =
                new DirectoryInfo(sourcePath).Name;

            string destinationRoot =
                GetUniqueDestinationPath(
                    Path.Combine(
                        backupSession,
                        category),
                    sourceFolderName);

            try
            {
                Directory.CreateDirectory(destinationRoot);

                AddManifestEntry(
                    backupSession,
                    new BackupManifestEntry
                    {
                        SourcePath = Path.GetFullPath(sourcePath),
                        BackupPath = Path.GetFullPath(destinationRoot),
                        Category = category,
                        ItemType = "DirectoryContents"
                    });
            }
            catch (Exception ex)
            {
                report.Add(
                    $"ERROR PREPARING BACKUP FOLDER: {destinationRoot}");

                report.Add($"   {ex.Message}");

                result.ErrorCount++;

                return result;
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(sourcePath);
            }
            catch (Exception ex)
            {
                report.Add($"ERROR READING FILES: {sourcePath}");
                report.Add($"   {ex.Message}");

                result.ErrorCount++;

                return result;
            }

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string destination =
                        GetUniqueDestinationPath(
                            destinationRoot,
                            Path.GetFileName(file));

                    File.Move(file, destination);

                    report.Add($"MOVED FILE: {file}");
                    report.Add($"        TO: {destination}");

                    result.FilesMoved++;
                    ReportProgress(progress, file, 1);
                }
                catch (Exception ex)
                {
                    report.Add($"SKIPPED FILE: {file}");
                    report.Add($"   REASON: {ex.Message}");

                    result.FilesSkipped++;
                    result.ErrorCount++;
                    ReportProgress(progress, file, 1);
                }
            }

            string[] directories;

            try
            {
                directories = Directory.GetDirectories(sourcePath);
            }
            catch (Exception ex)
            {
                report.Add($"ERROR READING SUBFOLDERS: {sourcePath}");
                report.Add($"   {ex.Message}");

                result.ErrorCount++;

                return result;
            }

            foreach (string directory in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                MoveDirectoryRecursive(
                    directory,
                    destinationRoot,
                    report,
                    result,
                    progress,
                    cancellationToken);
            }

            return result;
        }

        // ---------------------------------------------------------
        // RECURSIVE DIRECTORY BACKUP
        // ---------------------------------------------------------

        private void MoveDirectoryRecursive(
            string sourceDirectory,
            string destinationParent,
            List<string> report,
            BackupResult result,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string folderName =
                new DirectoryInfo(sourceDirectory).Name;

            string destinationDirectory =
                GetUniqueDestinationPath(
                    destinationParent,
                    folderName);

            try
            {
                _directoryMove(
                    sourceDirectory,
                    destinationDirectory);

                report.Add($"MOVED FOLDER: {sourceDirectory}");
                report.Add($"          TO: {destinationDirectory}");

                result.FoldersMoved++;
                ReportProgress(progress, sourceDirectory, 1);

                return;
            }
            catch
            {
                // Move individual contents if the folder is locked.
            }

            try
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            catch (Exception ex)
            {
                report.Add($"SKIPPED FOLDER: {sourceDirectory}");
                report.Add($"   REASON: {ex.Message}");

                result.FoldersSkipped++;
                result.ErrorCount++;

                return;
            }

            try
            {
                foreach (string file in Directory.GetFiles(sourceDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string destination =
                            GetUniqueDestinationPath(
                                destinationDirectory,
                                Path.GetFileName(file));

                        File.Move(file, destination);

                        report.Add($"MOVED FILE: {file}");
                        report.Add($"        TO: {destination}");

                        result.FilesMoved++;
                        ReportProgress(progress, file, 1);
                    }
                    catch (Exception ex)
                    {
                        report.Add($"SKIPPED FILE: {file}");
                        report.Add($"   REASON: {ex.Message}");

                        result.FilesSkipped++;
                        result.ErrorCount++;
                        ReportProgress(progress, file, 1);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                report.Add($"ERROR READING FOLDER: {sourceDirectory}");
                report.Add($"   {ex.Message}");

                result.ErrorCount++;
            }

            try
            {
                foreach (string subDirectory in Directory.GetDirectories(sourceDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    MoveDirectoryRecursive(
                        subDirectory,
                        destinationDirectory,
                        report,
                        result,
                        progress,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                report.Add($"ERROR READING SUBFOLDERS: {sourceDirectory}");
                report.Add($"   {ex.Message}");

                result.ErrorCount++;
            }

            try
            {
                if (Directory.Exists(sourceDirectory) &&
                    Directory.GetFileSystemEntries(sourceDirectory).Length == 0)
                {
                    Directory.Delete(sourceDirectory);

                    result.FoldersMoved++;
                }
                else
                {
                    result.FoldersSkipped++;
                }
            }
            catch
            {
                result.FoldersSkipped++;
            }
        }

        // ---------------------------------------------------------
        // MOVE SINGLE FILE TO BACKUP
        // ---------------------------------------------------------

        public BackupResult MoveFileToBackup(
            string sourcePath,
            string backupSession,
            string category,
            List<string> report,
            IProgress<BackupProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            BackupResult result =
                new BackupResult();

            cancellationToken.ThrowIfCancellationRequested();

            ReportProgress(progress, sourcePath, 0);

            if (!File.Exists(sourcePath))
            {
                report.Add($"NOT FOUND: {sourcePath}");

                result.NotFoundCount++;

                return result;
            }

            if (!IsBackupLocationSafe(
                    sourcePath,
                    report,
                    result))
            {
                return result;
            }

            try
            {
                string destinationFolder =
                    Path.Combine(
                        backupSession,
                        category);

                Directory.CreateDirectory(destinationFolder);

                string destinationPath =
                    GetUniqueDestinationPath(
                        destinationFolder,
                        Path.GetFileName(sourcePath));

                AddManifestEntry(
                    backupSession,
                    new BackupManifestEntry
                    {
                        SourcePath = Path.GetFullPath(sourcePath),
                        BackupPath = Path.GetFullPath(destinationPath),
                        Category = category,
                        ItemType = "File"
                    });

                File.Move(sourcePath, destinationPath);

                report.Add($"MOVED FILE: {sourcePath}");
                report.Add($"        TO: {destinationPath}");

                result.FilesMoved++;
                ReportProgress(progress, sourcePath, 1);
            }
            catch (Exception ex)
            {
                report.Add($"SKIPPED FILE: {sourcePath}");
                report.Add($"   REASON: {ex.Message}");

                result.FilesSkipped++;
                result.ErrorCount++;
                ReportProgress(progress, sourcePath, 1);
            }

            return result;
        }

        // ---------------------------------------------------------
        // VALIDATE BACKUP AND SOURCE PATH SEPARATION
        // ---------------------------------------------------------

        private bool IsBackupLocationSafe(
            string sourcePath,
            List<string> report,
            BackupResult result)
        {
            string backupRoot = GetBackupRoot();

            try
            {
                if (!PathSafetyService.PathsOverlap(
                        sourcePath,
                        backupRoot))
                {
                    return true;
                }

                report.Add("");
                report.Add("BLOCKED: Backup and cache paths overlap.");
                report.Add($"SOURCE: {sourcePath}");
                report.Add($"BACKUP ROOT: {backupRoot}");

                result.ErrorCount++;

                return false;
            }
            catch (Exception ex)
            {
                report.Add("");
                report.Add("BLOCKED: Unable to validate path safety.");
                report.Add($"SOURCE: {sourcePath}");
                report.Add($"BACKUP ROOT: {backupRoot}");
                report.Add($"REASON: {ex.Message}");

                result.ErrorCount++;

                return false;
            }
        }

        // ---------------------------------------------------------
        // BACKUP MANIFEST
        // ---------------------------------------------------------

        public BackupManifest? LoadManifest(string backupSession)
        {
            string manifestPath = Path.Combine(
                backupSession,
                "backup_manifest.json");

            if (!File.Exists(manifestPath))
            {
                return null;
            }

            string json = File.ReadAllText(manifestPath);

            return JsonSerializer.Deserialize<BackupManifest>(json);
        }

        public bool HasRestoreManifest(string backupSession)
        {
            try
            {
                BackupManifest? manifest = LoadManifest(backupSession);

                if (manifest == null)
                {
                    return false;
                }

                foreach (BackupManifestEntry entry in manifest.Entries)
                {
                    if (File.Exists(entry.BackupPath) ||
                        Directory.Exists(entry.BackupPath) &&
                        Directory.GetFileSystemEntries(
                            entry.BackupPath).Length > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void AddManifestEntry(
            string backupSession,
            BackupManifestEntry entry)
        {
            BackupManifest manifest =
                LoadManifest(backupSession) ?? new BackupManifest();

            manifest.Entries.Add(entry);

            SaveManifest(backupSession, manifest);
        }

        private void SaveManifest(
            string backupSession,
            BackupManifest manifest)
        {
            string manifestPath = Path.Combine(
                backupSession,
                "backup_manifest.json");

            string temporaryPath = manifestPath + ".tmp";

            string json = JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, manifestPath, true);
        }

        // ---------------------------------------------------------
        // RESTORE BACKUP SESSION
        // ---------------------------------------------------------

        public RestoreResult RestoreBackupSession(
            string backupSession,
            IProgress<BackupProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            RestoreResult result = new();

            cancellationToken.ThrowIfCancellationRequested();

            string backupRoot = GetBackupRoot();

            if (!PathSafetyService.IsSameOrWithin(
                    backupSession,
                    backupRoot))
            {
                throw new InvalidOperationException(
                    "The selected session is outside the configured backup folder.");
            }

            BackupManifest manifest =
                LoadManifest(backupSession) ??
                throw new InvalidOperationException(
                    "This backup session does not contain a restore manifest.");

            List<string> report = new()
            {
                "MSFS CACHE MANAGER",
                "RESTORE REPORT",
                "",
                $"Created: {DateTime.Now}",
                $"Session: {backupSession}",
                "",
                "Existing files are never overwritten.",
                "----------------------------------------",
                ""
            };

            foreach (BackupManifestEntry entry in manifest.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                RestoreManifestEntry(
                    entry,
                    backupSession,
                    backupRoot,
                    report,
                    result,
                    progress,
                    cancellationToken);
            }

            report.Add("");
            report.Add("=== SUMMARY ===");
            report.Add($"Files restored: {result.FilesRestored}");
            report.Add($"Folders restored: {result.FoldersRestored}");
            report.Add($"Conflicts skipped: {result.ConflictsSkipped}");
            report.Add($"Items not found: {result.NotFoundCount}");
            report.Add($"Errors: {result.ErrorCount}");

            string reportPath = Path.Combine(
                backupSession,
                $"restore_report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            File.WriteAllLines(reportPath, report);

            result.ReportPath = reportPath;

            return result;
        }

        public Task<RestoreResult> RestoreBackupSessionAsync(
            string backupSession,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () => RestoreBackupSession(
                    backupSession,
                    progress,
                    cancellationToken),
                cancellationToken);
        }

        public Task<BackupResult> MoveDirectoryContentsToBackupAsync(
            string sourcePath,
            string backupSession,
            string category,
            List<string> report,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () => MoveDirectoryContentsToBackup(
                    sourcePath,
                    backupSession,
                    category,
                    report,
                    progress,
                    cancellationToken),
                cancellationToken);
        }

        public Task<BackupResult> MoveFileToBackupAsync(
            string sourcePath,
            string backupSession,
            string category,
            List<string> report,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () => MoveFileToBackup(
                    sourcePath,
                    backupSession,
                    category,
                    report,
                    progress,
                    cancellationToken),
                cancellationToken);
        }

        private void ReportProgress(
            IProgress<BackupProgress>? progress,
            string currentPath,
            int itemsProcessed)
        {
            progress?.Report(
                new BackupProgress
                {
                    CurrentPath = currentPath,
                    ItemsProcessed = itemsProcessed
                });
        }

        private void RestoreManifestEntry(
            BackupManifestEntry entry,
            string backupSession,
            string backupRoot,
            List<string> report,
            RestoreResult result,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!PathSafetyService.IsSameOrWithin(
                        entry.BackupPath,
                        backupSession))
                {
                    throw new InvalidOperationException(
                        "The manifest backup path is outside its session folder.");
                }

                if (PathSafetyService.PathsOverlap(
                        entry.SourcePath,
                        backupRoot))
                {
                    throw new InvalidOperationException(
                        "The original location overlaps the backup folder.");
                }

                report.Add($"SOURCE: {entry.SourcePath}");
                report.Add($"BACKUP: {entry.BackupPath}");

                if (string.Equals(
                        entry.ItemType,
                        "File",
                        StringComparison.OrdinalIgnoreCase))
                {
                    RestoreFile(
                        entry.BackupPath,
                        entry.SourcePath,
                        report,
                        result,
                        progress,
                        cancellationToken);
                }
                else if (string.Equals(
                             entry.ItemType,
                             "DirectoryContents",
                             StringComparison.OrdinalIgnoreCase))
                {
                    RestoreDirectoryContents(
                        entry.BackupPath,
                        entry.SourcePath,
                        report,
                        result,
                        progress,
                        cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unsupported manifest item type: {entry.ItemType}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                report.Add($"ERROR: {ex.Message}");
                result.ErrorCount++;
            }

            report.Add("");
        }

        private void RestoreDirectoryContents(
            string backupDirectory,
            string destinationDirectory,
            List<string> report,
            RestoreResult result,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(backupDirectory))
            {
                report.Add("NOT FOUND: Backup directory is missing or already restored.");
                result.NotFoundCount++;
                return;
            }

            Directory.CreateDirectory(destinationDirectory);

            foreach (string file in Directory.GetFiles(backupDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                RestoreFile(
                    file,
                    Path.Combine(
                        destinationDirectory,
                        Path.GetFileName(file)),
                    report,
                    result,
                    progress,
                    cancellationToken);
            }

            foreach (string directory in Directory.GetDirectories(backupDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();

                RestoreDirectory(
                    directory,
                    Path.Combine(
                        destinationDirectory,
                        Path.GetFileName(directory)),
                    report,
                    result,
                    progress,
                    cancellationToken);
            }

            DeleteIfEmpty(backupDirectory);
        }

        private void RestoreDirectory(
            string backupDirectory,
            string destinationDirectory,
            List<string> report,
            RestoreResult result,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(destinationDirectory) ||
                File.Exists(destinationDirectory))
            {
                report.Add($"CONFLICT, MERGING SAFELY: {destinationDirectory}");

                if (File.Exists(destinationDirectory))
                {
                    result.ConflictsSkipped++;
                    ReportProgress(progress, destinationDirectory, 1);
                    return;
                }

                RestoreDirectoryContents(
                    backupDirectory,
                    destinationDirectory,
                    report,
                    result,
                    progress,
                    cancellationToken);

                return;
            }

            try
            {
                _directoryMove(
                    backupDirectory,
                    destinationDirectory);

                report.Add($"RESTORED FOLDER: {destinationDirectory}");
                result.FoldersRestored++;
                ReportProgress(progress, destinationDirectory, 1);
            }
            catch
            {
                Directory.CreateDirectory(destinationDirectory);

                RestoreDirectoryContents(
                    backupDirectory,
                    destinationDirectory,
                    report,
                    result,
                    progress,
                    cancellationToken);

                result.FoldersRestored++;
            }
        }

        private void RestoreFile(
            string backupFile,
            string destinationFile,
            List<string> report,
            RestoreResult result,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(backupFile))
            {
                report.Add($"NOT FOUND: {backupFile}");
                result.NotFoundCount++;
                return;
            }

            if (File.Exists(destinationFile) ||
                Directory.Exists(destinationFile))
            {
                report.Add($"CONFLICT SKIPPED: {destinationFile}");
                result.ConflictsSkipped++;
                ReportProgress(progress, destinationFile, 1);
                return;
            }

            string? destinationParent =
                Path.GetDirectoryName(destinationFile);

            if (!string.IsNullOrWhiteSpace(destinationParent))
            {
                Directory.CreateDirectory(destinationParent);
            }

            try
            {
                File.Move(backupFile, destinationFile);

                report.Add($"RESTORED FILE: {destinationFile}");
                result.FilesRestored++;
                ReportProgress(progress, destinationFile, 1);
            }
            catch (Exception ex)
            {
                report.Add($"ERROR RESTORING FILE: {destinationFile}");
                report.Add($"   {ex.Message}");
                result.ErrorCount++;
                ReportProgress(progress, destinationFile, 1);
            }
        }

        private void DeleteIfEmpty(string directory)
        {
            try
            {
                if (Directory.Exists(directory) &&
                    Directory.GetFileSystemEntries(directory).Length == 0)
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
                // Leaving an empty backup folder is harmless.
            }
        }

        // ---------------------------------------------------------
        // UNIQUE DESTINATION
        // ---------------------------------------------------------

        private string GetUniqueDestinationPath(
            string destinationFolder,
            string name)
        {
            string destination =
                Path.Combine(destinationFolder, name);

            if (!Directory.Exists(destination) &&
                !File.Exists(destination))
            {
                return destination;
            }

            string fileName =
                Path.GetFileNameWithoutExtension(name);

            string extension =
                Path.GetExtension(name);

            int suffix = 2;

            while (true)
            {
                string candidate = Path.Combine(
                    destinationFolder,
                    $"{fileName}_{suffix}{extension}");

                if (!Directory.Exists(candidate) &&
                    !File.Exists(candidate))
                {
                    return candidate;
                }

                suffix++;
            }
        }

        // ---------------------------------------------------------
        // SAVE REPORT
        // ---------------------------------------------------------

        public void SaveReport(
            string backupSession,
            List<string> report)
        {
            string reportPath =
                Path.Combine(
                    backupSession,
                    "backup_report.txt");

            List<string> output = new()
            {
                "MSFS CACHE MANAGER",
                "BACKUP REPORT",
                "",
                $"Created: {DateTime.Now}",
                "",
                "----------------------------------------",
                ""
            };

            output.AddRange(report);

            File.WriteAllLines(reportPath, output);
        }

        // ---------------------------------------------------------
        // OPEN BACKUPS FOLDER
        // ---------------------------------------------------------

        public void OpenBackupFolder()
        {
            EnsureBackupRootExists();

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = GetBackupRoot(),
                    UseShellExecute = true
                });
        }
    }

}
