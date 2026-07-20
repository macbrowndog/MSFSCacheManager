using MSFSCacheManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace MSFSCacheManager
{
    public partial class MainWindow : Window
    {
        private readonly CacheManagerService _cacheManager;
        private readonly BackupService _backupService;

        public MainWindow()
        {
            InitializeComponent();

            _cacheManager =
                new CacheManagerService();

            _backupService =
                new BackupService();

            DetectCaches();
        }

        // ---------------------------------------------------------
        // DETECT CACHES
        // ---------------------------------------------------------

        private void DetectCaches()
        {
            var caches =
                _cacheManager.GetExistingCacheLocations();

            if (caches.Count == 0)
            {
                StatusText.Text =
                    "No cache locations detected.";
            }
            else
            {
                StatusText.Text =
                    $"{caches.Count} cache locations detected.";
            }
        }

        // ---------------------------------------------------------
        // SCAN CACHE LOCATIONS
        // ---------------------------------------------------------

        private void ScanButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var caches =
                _cacheManager.GetExistingCacheLocations();

            CacheScanWindow scanWindow =
                new CacheScanWindow(caches);

            scanWindow.Owner = this;

            scanWindow.ShowDialog();
        }

        // ---------------------------------------------------------
        // GPU SHADER CACHE
        // ---------------------------------------------------------

        private void ShaderCacheButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    "This will move existing NVIDIA and AMD shader cache folders " +
                    "to the Backups folder.\n\n" +
                    "No files will be permanently deleted.\n\n" +
                    "It is recommended that Microsoft Flight Simulator and other " +
                    "GPU-intensive applications are closed before continuing.\n\n" +
                    "Do you want to continue?",
                    "Clear GPU Shader Cache",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "GPU shader cache cleanup cancelled.";

                return;
            }

            ClearGpuShaderCaches();
        }

        // ---------------------------------------------------------
        // CLEAR GPU SHADER CACHES
        // ---------------------------------------------------------

        private void ClearGpuShaderCaches()
        {
            try
            {
                StatusText.Text =
                    "Processing GPU shader caches...";

                List<string> report = new();

                BackupResult totalResult =
                    new BackupResult();

                // -------------------------------------------------
                // GET CACHE LOCATIONS
                // -------------------------------------------------

                var nvidiaCaches =
                    _cacheManager.GetNvidiaShaderCacheLocations();

                var amdCaches =
                    _cacheManager.GetAmdShaderCacheLocations();

                // -------------------------------------------------
                // CHECK IF ANY CACHE EXISTS
                // -------------------------------------------------

                bool anyCacheExists =
                    nvidiaCaches.Exists(
                        Directory.Exists) ||
                    amdCaches.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No GPU shader cache folders found.";

                    MessageBox.Show(
                        "No NVIDIA or AMD shader cache folders " +
                        "were found on this computer.",
                        "GPU Shader Cache",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                // -------------------------------------------------
                // CREATE BACKUP SESSION
                // -------------------------------------------------

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "GPU SHADER CACHE CLEANUP");

                report.Add("");

                // -------------------------------------------------
                // NVIDIA
                // -------------------------------------------------

                report.Add(
                    "=== NVIDIA SHADER CACHES ===");

                foreach (string path in nvidiaCaches)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "NVIDIA",
                                report);

                    totalResult.Add(
                        result);
                }

                // -------------------------------------------------
                // AMD
                // -------------------------------------------------

                report.Add("");
                report.Add(
                    "=== AMD SHADER CACHES ===");

                foreach (string path in amdCaches)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "AMD",
                                report);

                    totalResult.Add(
                        result);
                }

                // -------------------------------------------------
                // SUMMARY
                // -------------------------------------------------

                report.Add("");
                report.Add(
                    "=== SUMMARY ===");

                report.Add(
                    $"Files moved: {totalResult.FilesMoved}");

                report.Add(
                    $"Files skipped: {totalResult.FilesSkipped}");

                report.Add(
                    $"Folders moved: {totalResult.FoldersMoved}");

                report.Add(
                    $"Folders skipped: {totalResult.FoldersSkipped}");

                report.Add(
                    $"Locations not found: {totalResult.NotFoundCount}");

                report.Add(
                    $"Errors: {totalResult.ErrorCount}");

                // -------------------------------------------------
                // SAVE REPORT
                // -------------------------------------------------

                _backupService.SaveReport(
                    backupSession,
                    report);

                // -------------------------------------------------
                // RESULT
                // -------------------------------------------------

                StatusText.Text =
                    $"GPU shader cache complete. " +
                    $"{totalResult.FilesMoved} files backed up.";

                MessageBoxImage messageIcon =
                    totalResult.ErrorCount > 0
                        ? MessageBoxImage.Warning
                        : MessageBoxImage.Information;

                MessageBox.Show(
                    $"GPU shader cache processing completed.\n\n" +
                    $"Files moved: {totalResult.FilesMoved}\n" +
                    $"Files skipped: {totalResult.FilesSkipped}\n" +
                    $"Folders moved: {totalResult.FoldersMoved}\n" +
                    $"Folders skipped: {totalResult.FoldersSkipped}\n" +
                    $"Errors: {totalResult.ErrorCount}\n\n" +
                    $"A detailed backup report was created.",
                    "GPU Shader Cache Complete",
                    MessageBoxButton.OK,
                    messageIcon);

                // -------------------------------------------------
                // RESCAN
                // -------------------------------------------------

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "GPU shader cache cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "GPU Shader Cache Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        // ---------------------------------------------------------
        // ROLLING CACHE BUTTON
        // ---------------------------------------------------------

        private void RollingCacheButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    "This will move detected MSFS rolling cache files " +
                    "to the Backups folder.\n\n" +
                    "No files will be permanently deleted.\n\n" +
                    "Microsoft Flight Simulator should be closed " +
                    "before continuing.\n\n" +
                    "Do you want to continue?",
                    "Clear MSFS Rolling Cache",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "Rolling cache cleanup cancelled.";

                return;
            }

            ClearRollingCaches();
        }


        // ---------------------------------------------------------
        // CLEAR ROLLING CACHE
        // ---------------------------------------------------------

        private void ClearRollingCaches()
        {
            try
            {
                StatusText.Text =
                    "Searching for MSFS rolling cache files...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var rollingCaches =
                    _cacheManager.GetRollingCacheLocations();

                // -------------------------------------------------
                // CHECK FOR EXISTING FILES
                // -------------------------------------------------

                bool anyCacheExists =
                    rollingCaches.Exists(
                        File.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No rolling cache files found.";

                    MessageBox.Show(
                        "No ROLLINGCACHE.CCC files were found " +
                        "in the known MSFS locations.",
                        "MSFS Rolling Cache",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                // -------------------------------------------------
                // CREATE BACKUP SESSION
                // -------------------------------------------------

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS ROLLING CACHE CLEANUP");

                report.Add("");

                // -------------------------------------------------
                // PROCESS FILES
                // -------------------------------------------------

                foreach (string path in rollingCaches)
                {
                    BackupResult result =
                        _backupService.MoveFileToBackup(
                            path,
                            backupSession,
                            "RollingCache",
                            report);

                    totalResult.Add(
                        result);
                }

                // -------------------------------------------------
                // SUMMARY
                // -------------------------------------------------

                report.Add("");
                report.Add(
                    "=== SUMMARY ===");

                report.Add(
                    $"Rolling cache files moved: " +
                    $"{totalResult.FilesMoved}");

                report.Add(
                    $"Files skipped: " +
                    $"{totalResult.FilesSkipped}");

                report.Add(
                    $"Locations not found: " +
                    $"{totalResult.NotFoundCount}");

                report.Add(
                    $"Errors: " +
                    $"{totalResult.ErrorCount}");

                // -------------------------------------------------
                // SAVE REPORT
                // -------------------------------------------------

                _backupService.SaveReport(
                    backupSession,
                    report);

                // -------------------------------------------------
                // UPDATE STATUS
                // -------------------------------------------------

                if (totalResult.ErrorCount == 0)
                {
                    StatusText.Text =
                        $"Rolling cache complete. " +
                        $"{totalResult.FilesMoved} file(s) backed up.";
                }
                else
                {
                    StatusText.Text =
                        $"Rolling cache completed with " +
                        $"{totalResult.ErrorCount} error(s).";
                }

                // -------------------------------------------------
                // SHOW RESULTS
                // -------------------------------------------------

                MessageBoxImage icon =
                    totalResult.ErrorCount > 0
                        ? MessageBoxImage.Warning
                        : MessageBoxImage.Information;

                MessageBox.Show(
                    $"Rolling cache processing completed.\n\n" +
                    $"Files moved: {totalResult.FilesMoved}\n" +
                    $"Files skipped: {totalResult.FilesSkipped}\n" +
                    $"Errors: {totalResult.ErrorCount}\n\n" +
                    $"A backup and detailed report were created.",
                    "MSFS Rolling Cache Complete",
                    MessageBoxButton.OK,
                    icon);

                // -------------------------------------------------
                // RESCAN
                // -------------------------------------------------

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Rolling cache cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "Rolling Cache Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        // ---------------------------------------------------------
        // MSFS CACHE BUTTON
        // ---------------------------------------------------------

        private void MSFSCacheButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    "This will clear the detected MSFS cache folders.\n\n" +
                    "Cache contents will be moved to the Backups folder " +
                    "and will not be permanently deleted.\n\n" +
                    "Microsoft Flight Simulator should be closed " +
                    "before continuing.\n\n" +
                    "Do you want to continue?",
                    "Clear MSFS Cache",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "MSFS cache cleanup cancelled.";

                return;
            }

            ClearMSFSCaches();
        }


        // ---------------------------------------------------------
        // CLEAR MSFS CACHE
        // ---------------------------------------------------------

        private void ClearMSFSCaches()
        {
            try
            {
                StatusText.Text =
                    "Processing MSFS cache folders...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var cacheLocations =
                    _cacheManager.GetMSFSCacheLocations();

                // -------------------------------------------------
                // CHECK FOR EXISTING CACHE FOLDERS
                // -------------------------------------------------

                bool anyCacheExists =
                    cacheLocations.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No MSFS cache folders found.";

                    MessageBox.Show(
                        "No MSFS cache folders were found " +
                        "in the known locations.",
                        "MSFS Cache",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                // -------------------------------------------------
                // CREATE BACKUP SESSION
                // -------------------------------------------------

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS CACHE CLEANUP");

                report.Add("");

                // -------------------------------------------------
                // PROCESS CACHE LOCATIONS
                // -------------------------------------------------

                foreach (string path in cacheLocations)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "MSFSCache",
                                report);

                    totalResult.Add(
                        result);
                }

                // -------------------------------------------------
                // SUMMARY
                // -------------------------------------------------

                report.Add("");
                report.Add(
                    "=== SUMMARY ===");

                report.Add(
                    $"Files moved: {totalResult.FilesMoved}");

                report.Add(
                    $"Files skipped: {totalResult.FilesSkipped}");

                report.Add(
                    $"Folders moved: {totalResult.FoldersMoved}");

                report.Add(
                    $"Folders skipped: {totalResult.FoldersSkipped}");

                report.Add(
                    $"Locations not found: {totalResult.NotFoundCount}");

                report.Add(
                    $"Errors: {totalResult.ErrorCount}");

                // -------------------------------------------------
                // SAVE REPORT
                // -------------------------------------------------

                _backupService.SaveReport(
                    backupSession,
                    report);

                // -------------------------------------------------
                // STATUS
                // -------------------------------------------------

                if (totalResult.ErrorCount == 0)
                {
                    StatusText.Text =
                        $"MSFS cache complete. " +
                        $"{totalResult.FilesMoved} file(s) backed up.";
                }
                else
                {
                    StatusText.Text =
                        $"MSFS cache completed with " +
                        $"{totalResult.ErrorCount} error(s).";
                }

                // -------------------------------------------------
                // RESULT MESSAGE
                // -------------------------------------------------

                MessageBoxImage icon =
                    totalResult.ErrorCount > 0
                        ? MessageBoxImage.Warning
                        : MessageBoxImage.Information;

                MessageBox.Show(
                    $"MSFS cache processing completed.\n\n" +
                    $"Files moved: {totalResult.FilesMoved}\n" +
                    $"Files skipped: {totalResult.FilesSkipped}\n" +
                    $"Folders moved: {totalResult.FoldersMoved}\n" +
                    $"Folders skipped: {totalResult.FoldersSkipped}\n" +
                    $"Errors: {totalResult.ErrorCount}\n\n" +
                    $"A backup and detailed report were created.",
                    "MSFS Cache Complete",
                    MessageBoxButton.OK,
                    icon);

                // -------------------------------------------------
                // RESCAN
                // -------------------------------------------------

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "MSFS cache cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "MSFS Cache Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }



        // ---------------------------------------------------------
        // OPEN BACKUPS FOLDER
        // ---------------------------------------------------------

        private void BackupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                _backupService.OpenBackupFolder();

                StatusText.Text =
                    "Backups folder opened.";
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Unable to open backups folder.";

                MessageBox.Show(
                    ex.Message,
                    "Backup Folder Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}