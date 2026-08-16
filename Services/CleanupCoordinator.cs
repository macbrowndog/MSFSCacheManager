using MSFSCacheManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MSFSCacheManager.Services
{
    public class CleanupCoordinator
    {
        private readonly BackupService _backupService;

        public CleanupCoordinator(BackupService backupService)
        {
            _backupService = backupService;
        }

        public async Task<CacheCleanupResult> ExecuteAsync(
            CacheCleanupDefinition definition,
            IProgress<BackupProgress>? progress,
            CancellationToken cancellationToken)
        {
            CacheCleanupResult outcome = new();

            outcome.FoundAnyCache = HasAnyExistingLocation(definition);

            if (!outcome.FoundAnyCache)
            {
                return outcome;
            }

            cancellationToken.ThrowIfCancellationRequested();

            outcome.BackupSession =
                _backupService.CreateBackupSession();

            List<string> report = new()
            {
                definition.ReportTitle,
                ""
            };

            foreach (CacheCleanupGroup group in definition.Groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(group.Heading))
                {
                    report.Add($"=== {group.Heading} ===");
                }

                foreach (string path in group.Locations)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    BackupResult result;

                    if (group.ItemType == CacheItemType.File)
                    {
                        result = await
                            _backupService.MoveFileToBackupAsync(
                                path,
                                outcome.BackupSession,
                                group.BackupCategory,
                                report,
                                progress,
                                cancellationToken);
                    }
                    else
                    {
                        result = await
                            _backupService.MoveDirectoryContentsToBackupAsync(
                                path,
                                outcome.BackupSession,
                                group.BackupCategory,
                                report,
                                progress,
                                cancellationToken);
                    }

                    outcome.BackupResult.Add(result);
                }

                report.Add("");
            }

            AddSummary(report, outcome.BackupResult);

            _backupService.SaveReport(
                outcome.BackupSession,
                report);

            return outcome;
        }

        private bool HasAnyExistingLocation(
            CacheCleanupDefinition definition)
        {
            foreach (CacheCleanupGroup group in definition.Groups)
            {
                foreach (string path in group.Locations)
                {
                    bool exists = group.ItemType == CacheItemType.File
                        ? File.Exists(path)
                        : Directory.Exists(path);

                    if (exists)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AddSummary(
            List<string> report,
            BackupResult result)
        {
            report.Add("=== SUMMARY ===");
            report.Add($"Files moved: {result.FilesMoved}");
            report.Add($"Files skipped: {result.FilesSkipped}");
            report.Add($"Folders moved: {result.FoldersMoved}");
            report.Add($"Folders skipped: {result.FoldersSkipped}");
            report.Add($"Locations not found: {result.NotFoundCount}");
            report.Add($"Errors: {result.ErrorCount}");
        }
    }
}
