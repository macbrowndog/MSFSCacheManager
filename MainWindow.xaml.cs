using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using MSFSCacheManager.Services;
using MSFSCacheManager.Windows;


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
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
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
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
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
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
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
        // SCENERY CACHE BUTTON
        // ---------------------------------------------------------

        private void SceneryCacheButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
            MessageBoxResult result =
     MessageBox.Show(
         "SCENERY CACHE - ADVANCED TROUBLESHOOTING\n\n" +
         "This operation will clear the detected MSFS Scenery Cache folders.\n\n" +
         "Use this option when troubleshooting scenery loading, outdated scenery data, " +
         "or other scenery-related issues.\n\n" +
         "Cache contents will be moved to the Backups folder before being removed " +
         "from their active location.\n\n" +
         "Microsoft Flight Simulator must be completely closed.\n\n" +
         "Do you want to continue?",
         "Clear Scenery Cache",
         MessageBoxButton.YesNo,
         MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "Scenery Cache cleanup cancelled.";

                return;
            }

            ClearSceneryCache();
        }


        // ---------------------------------------------------------
        // CLEAR SCENERY CACHE
        // ---------------------------------------------------------

        private void ClearSceneryCache()
        {
            try
            {
                StatusText.Text =
                    "Processing Scenery Cache...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var cacheLocations =
                    _cacheManager.GetSceneryCacheLocations();

                bool anyCacheExists =
                    cacheLocations.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No Scenery Cache folders found.";

                    MessageBox.Show(
                        "No Scenery Cache folders were found " +
                        "in the known MSFS locations.",
                        "Scenery Cache",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS SCENERY CACHE CLEANUP");

                report.Add("");

                foreach (string path in cacheLocations)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "SceneryCache",
                                report);

                    totalResult.Add(
                        result);
                }

                AddCleanupSummary(
                    report,
                    totalResult);

                _backupService.SaveReport(
                    backupSession,
                    report);

                ShowCleanupResult(
                    "Scenery Cache",
                    totalResult);

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Scenery Cache cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "Scenery Cache Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // SCENERY INDEXES BUTTON
        // ---------------------------------------------------------

        private void SceneryIndexesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
            MessageBoxResult result =
    MessageBox.Show(
        "SCENERY INDEXES - ADVANCED TROUBLESHOOTING\n\n" +
        "This operation will clear the detected MSFS Scenery Indexes folders.\n\n" +
        "Use this option when troubleshooting scenery indexing or scenery loading issues.\n\n" +
        "Microsoft Flight Simulator may need to rebuild scenery index data the next " +
        "time the simulator starts. This may temporarily increase loading or processing time.\n\n" +
        "Existing data will be moved to the Backups folder before being removed " +
        "from its active location.\n\n" +
        "Microsoft Flight Simulator must be completely closed.\n\n" +
        "Do you want to continue?",
        "Clear Scenery Indexes",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "Scenery Indexes cleanup cancelled.";

                return;
            }

            ClearSceneryIndexes();
        }


        // ---------------------------------------------------------
        // CLEAR SCENERY INDEXES
        // ---------------------------------------------------------

        private void ClearSceneryIndexes()
        {
            try
            {
                StatusText.Text =
                    "Processing Scenery Indexes...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var cacheLocations =
                    _cacheManager.GetSceneryIndexesLocations();

                bool anyCacheExists =
                    cacheLocations.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No Scenery Indexes folders found.";

                    MessageBox.Show(
                        "No Scenery Indexes folders were found " +
                        "in the known MSFS locations.",
                        "Scenery Indexes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS SCENERY INDEXES CLEANUP");

                report.Add("");

                foreach (string path in cacheLocations)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "SceneryIndexes",
                                report);

                    totalResult.Add(
                        result);
                }

                AddCleanupSummary(
                    report,
                    totalResult);

                _backupService.SaveReport(
                    backupSession,
                    report);

                ShowCleanupResult(
                    "Scenery Indexes",
                    totalResult);

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Scenery Indexes cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "Scenery Indexes Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        // ---------------------------------------------------------
        // DCE CACHE BUTTON
        // ---------------------------------------------------------

        private void DCEButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
            MessageBoxResult result =
     MessageBox.Show(
         "DCE CACHE - ADVANCED TROUBLESHOOTING\n\n" +
         "This operation will clear the detected MSFS DCE Cache folders.\n\n" +
         "This option is intended for troubleshooting specific simulator cache issues " +
         "rather than routine maintenance.\n\n" +
         "Detected cache contents will be moved to the Backups folder before being " +
         "removed from their active location.\n\n" +
         "Microsoft Flight Simulator must be completely closed.\n\n" +
         "Do you want to continue?",
         "Clear DCE Cache",
         MessageBoxButton.YesNo,
         MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "DCE Cache cleanup cancelled.";

                return;
            }

            ClearDCECache();
        }


        // ---------------------------------------------------------
        // CLEAR DCE CACHE
        // ---------------------------------------------------------

        private void ClearDCECache()
        {
            try
            {
                StatusText.Text =
                    "Processing DCE Cache...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var cacheLocations =
                    _cacheManager.GetDCECacheLocations();

                bool anyCacheExists =
                    cacheLocations.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No DCE Cache folders found.";

                    MessageBox.Show(
     "No MSFS 2020 DCE cache folder was found.\n\n" +
     "DCE cache cleanup applies to Microsoft Flight Simulator 2020 only.",
     "DCE Cache",
     MessageBoxButton.OK,
     MessageBoxImage.Information);

                    StatusText.Text =
                        "No MSFS 2020 DCE cache folder found.";

                    return;
                }

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS DCE CACHE CLEANUP");

                report.Add("");

                foreach (string path in cacheLocations)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "DCECache",
                                report);

                    totalResult.Add(
                        result);
                }

                AddCleanupSummary(
                    report,
                    totalResult);

                _backupService.SaveReport(
                    backupSession,
                    report);

                ShowCleanupResult(
                    "DCE Cache",
                    totalResult);

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "DCE Cache cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "DCE Cache Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // STREAMED PACKAGES BUTTON
        // ---------------------------------------------------------

        private void StreamedPackagesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
            MessageBoxResult result =
     MessageBox.Show(
         "STREAMED PACKAGES - ADVANCED TROUBLESHOOTING\n\n" +
         "This operation will process the detected MSFS Streamed Packages cache.\n\n" +
         "WARNING:\n" +
         "Cached streamed content may need to be downloaded again the next time " +
         "Microsoft Flight Simulator requires it.\n\n" +
         "This option should normally only be used when troubleshooting problems " +
         "with streamed or downloaded simulator content.\n\n" +
         "Detected data will be moved to the Backups folder before being removed " +
         "from its active location.\n\n" +
         "Microsoft Flight Simulator must be completely closed.\n\n" +
         "Do you want to continue?",
         "Clear Streamed Packages",
         MessageBoxButton.YesNo,
         MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "Streamed Packages cleanup cancelled.";

                return;
            }

            ClearStreamedPackages();
        }


        // ---------------------------------------------------------
        // CLEAR STREAMED PACKAGES
        // ---------------------------------------------------------

        private void ClearStreamedPackages()
        {
            try
            {
                StatusText.Text =
                    "Processing Streamed Packages...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var cacheLocations =
                    _cacheManager
                        .GetStreamedPackagesLocations();

                bool anyCacheExists =
                    cacheLocations.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No Streamed Packages folders found.";

                    MessageBox.Show(
                        "No Streamed Packages cache folders were found " +
                        "in the known MSFS 2024 locations.",
                        "Streamed Packages",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS STREAMED PACKAGES CLEANUP");

                report.Add("");

                foreach (string path in cacheLocations)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "StreamedPackages",
                                report);

                    totalResult.Add(
                        result);
                }

                AddCleanupSummary(
                    report,
                    totalResult);

                _backupService.SaveReport(
                    backupSession,
                    report);

                ShowCleanupResult(
                    "Streamed Packages",
                    totalResult);

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Streamed Packages cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "Streamed Packages Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // SIMOBJECTS BUTTON
        // ---------------------------------------------------------

private void SimObjectsButton_Click(
    object sender,
    RoutedEventArgs e)
{
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
            MessageBoxResult result =
      MessageBox.Show(
          "SIMOBJECTS - ADVANCED TROUBLESHOOTING\n\n" +

          "This operation will clear the detected MSFS SimObjects " +
          "cache folders.\n\n" +

          "SimObjects may contain locally cached aircraft and AI aircraft " +
          "data. Clearing this data may cause Microsoft Flight Simulator " +
          "to rebuild or re-download aircraft-related content and may " +
          "result in longer loading times.\n\n" +

          "This operation is NOT recommended for routine cache maintenance. " +
          "Use it only when troubleshooting a specific SimObjects or " +
          "aircraft-related problem.\n\n" +

          "Detected data will be moved to the Backups folder before being " +
          "removed from its active location.\n\n" +

          "Microsoft Flight Simulator must be completely closed.\n\n" +

          "Do you want to continue?",
          "Clear SimObjects Cache",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "SimObjects cleanup cancelled.";

                return;
            }

            ClearSimObjectsCache();
        }


// ---------------------------------------------------------
// CLEAR SIMOBJECTS CACHE
// ---------------------------------------------------------

private void ClearSimObjectsCache()
{
    try
    {
        StatusText.Text =
            "Processing SimObjects Cache...";

        List<string> report =
            new List<string>();

        BackupResult totalResult =
            new BackupResult();

        var cacheLocations =
            _cacheManager
                .GetSimObjectsCacheLocations();

        bool anyCacheExists =
            cacheLocations.Exists(
                Directory.Exists);

        if (!anyCacheExists)
        {
            StatusText.Text =
                "No SimObjects cache folders found.";

            MessageBox.Show(
                "No SimObjects cache folders were found " +
                "in the known MSFS locations.",
                "SimObjects Cache",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        string backupSession =
            _backupService.CreateBackupSession();

        report.Add(
            "MSFS SIMOBJECTS CACHE CLEANUP");

        report.Add("");

        foreach (string path in cacheLocations)
        {
            BackupResult result =
                _backupService
                    .MoveDirectoryContentsToBackup(
                        path,
                        backupSession,
                        "SimObjects",
                        report);

            totalResult.Add(
                result);
        }

        AddCleanupSummary(
            report,
            totalResult);

        _backupService.SaveReport(
            backupSession,
            report);

        ShowCleanupResult(
            "SimObjects Cache",
            totalResult);

        DetectCaches();
    }
    catch (Exception ex)
    {
        StatusText.Text =
            "SimObjects cleanup failed.";

        MessageBox.Show(
            ex.Message,
            "SimObjects Cache Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

        // ---------------------------------------------------------
        // WASM CACHE BUTTON
        // ---------------------------------------------------------

        private void WASMButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsureMSFSIsClosed())
            {
                return;
            }
            MessageBoxResult result =
     MessageBox.Show(
         "WASM CACHE - ADVANCED TROUBLESHOOTING\n\n" +
         "This operation will clear the detected MSFS WASM cache folders.\n\n" +
         "Use this option when troubleshooting aircraft or add-ons that use WASM modules.\n\n" +
         "Affected WASM modules may need to be rebuilt by Microsoft Flight Simulator " +
         "the next time the associated aircraft or add-on is loaded. The first load " +
         "after cleanup may therefore take longer than usual.\n\n" +
         "Detected cache data will be moved to the Backups folder before being removed " +
         "from its active location.\n\n" +
         "Microsoft Flight Simulator must be completely closed.\n\n" +
         "Do you want to continue?",
         "Clear WASM Cache",
         MessageBoxButton.YesNo,
         MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    "WASM Cache cleanup cancelled.";

                return;
            }

            ClearWASMCache();
        }


        // ---------------------------------------------------------
        // CLEAR WASM CACHE
        // ---------------------------------------------------------

        private void ClearWASMCache()
        {
            try
            {
                StatusText.Text =
                    "Processing WASM Cache...";

                List<string> report =
                    new List<string>();

                BackupResult totalResult =
                    new BackupResult();

                var cacheLocations =
                    _cacheManager
                        .GetWASMCacheLocations();

                bool anyCacheExists =
                    cacheLocations.Exists(
                        Directory.Exists);

                if (!anyCacheExists)
                {
                    StatusText.Text =
                        "No WASM cache folders found.";

                    MessageBox.Show(
                        "No WASM cache folders were found " +
                        "in the known MSFS locations.",
                        "WASM Cache",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                string backupSession =
                    _backupService.CreateBackupSession();

                report.Add(
                    "MSFS WASM CACHE CLEANUP");

                report.Add("");

                foreach (string path in cacheLocations)
                {
                    BackupResult result =
                        _backupService
                            .MoveDirectoryContentsToBackup(
                                path,
                                backupSession,
                                "WASMCache",
                                report);

                    totalResult.Add(
                        result);
                }

                AddCleanupSummary(
                    report,
                    totalResult);

                _backupService.SaveReport(
                    backupSession,
                    report);

                ShowCleanupResult(
                    "WASM Cache",
                    totalResult);

                DetectCaches();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "WASM Cache cleanup failed.";

                MessageBox.Show(
                    ex.Message,
                    "WASM Cache Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

                     


        // ---------------------------------------------------------
        // ADD CLEANUP SUMMARY TO REPORT
        // ---------------------------------------------------------

        private void AddCleanupSummary(
            List<string> report,
            BackupResult result)
        {
            report.Add("");
            report.Add(
                "=== SUMMARY ===");

            report.Add(
                $"Files moved: {result.FilesMoved}");

            report.Add(
                $"Files skipped: {result.FilesSkipped}");

            report.Add(
                $"Folders moved: {result.FoldersMoved}");

            report.Add(
                $"Folders skipped: {result.FoldersSkipped}");

            report.Add(
                $"Locations not found: {result.NotFoundCount}");

            report.Add(
                $"Errors: {result.ErrorCount}");
        }


        // ---------------------------------------------------------
        // SHOW CLEANUP RESULT
        // ---------------------------------------------------------

        private void ShowCleanupResult(
            string operationName,
            BackupResult result)
        {
            if (result.ErrorCount == 0)
            {
                StatusText.Text =
                    $"{operationName} complete. " +
                    $"{result.FilesMoved} file(s) backed up.";
            }
            else
            {
                StatusText.Text =
                    $"{operationName} completed with " +
                    $"{result.ErrorCount} error(s).";
            }

            MessageBoxImage icon =
                result.ErrorCount > 0
                    ? MessageBoxImage.Warning
                    : MessageBoxImage.Information;

            MessageBox.Show(
                $"{operationName} processing completed.\n\n" +
                $"Files moved: {result.FilesMoved}\n" +
                $"Files skipped: {result.FilesSkipped}\n" +
                $"Folders moved: {result.FoldersMoved}\n" +
                $"Folders skipped: {result.FoldersSkipped}\n" +
                $"Errors: {result.ErrorCount}\n\n" +
                $"A backup and detailed report were created.",
                $"{operationName} Complete",
                MessageBoxButton.OK,
                icon);
        }

        // ---------------------------------------------------------
        // CHECK IF MICROSOFT FLIGHT SIMULATOR IS RUNNING
        // ---------------------------------------------------------

        private bool IsMSFSRunning()
        {
            string[] processNames =
            {
        "FlightSimulator",
        "FlightSimulator2024"
    };

            foreach (string processName in processNames)
            {
                Process[] processes =
                    Process.GetProcessesByName(
                        processName);

                if (processes.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }


        // ---------------------------------------------------------
        // VERIFY MSFS IS CLOSED
        // ---------------------------------------------------------

        private bool EnsureMSFSIsClosed()
        {
            if (!IsMSFSRunning())
            {
                return true;
            }

            StatusText.Text =
                "Cleanup blocked - Microsoft Flight Simulator is running.";

            MessageBox.Show(
                "Microsoft Flight Simulator appears to be running.\n\n" +
                "Please close MSFS 2020 or MSFS 2024 completely " +
                "before clearing cache files.\n\n" +
                "This helps prevent locked files, incomplete backups, " +
                "or cache files being recreated while cleanup is running.",
                "Microsoft Flight Simulator Is Running",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
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

        // ---------------------------------------------------------
        // SETTINGS
        // ---------------------------------------------------------

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SettingsWindow window =
                new SettingsWindow();

            window.Owner = this;

            window.ShowDialog();
        }
    }
}
