using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MSFSCacheManager.Models;
using MSFSCacheManager.Services;
using MSFSCacheManager.Windows;


namespace MSFSCacheManager
{
    public partial class MainWindow : Window
    {
        private readonly CacheManagerService _cacheManager;
        private readonly BackupService _backupService;
        private readonly CleanupCoordinator _cleanupCoordinator;
        private readonly CacheCleanupDefinitionFactory _cleanupDefinitions;
        private readonly CacheScanService _cacheScanService;
        private CancellationTokenSource? _operationCancellation;
        private int _processedItems;

        public MainWindow()
        {
            InitializeComponent();

            _cacheManager =
                new CacheManagerService();

            _backupService =
                new BackupService();

            _cleanupCoordinator =
                new CleanupCoordinator(_backupService);

            _cleanupDefinitions =
                new CacheCleanupDefinitionFactory(_cacheManager);

            _cacheScanService =
                new CacheScanService(_cleanupDefinitions);

            DetectCaches();
        }

        private async Task RunOperationAsync(
            Func<CancellationToken, IProgress<BackupProgress>, Task> operation)
        {
            if (_operationCancellation != null)
            {
                return;
            }

            _operationCancellation = new CancellationTokenSource();
            _processedItems = 0;

            OperationsPanel.IsEnabled = false;
            SettingsButton.IsEnabled = false;
            OperationProgressPanel.Visibility = Visibility.Visible;
            OperationProgressBar.IsIndeterminate = true;
            CancelOperationButton.IsEnabled = true;
            OperationDetailText.Text = "Preparing operation...";
            StatusIndicatorText.Text = "●  WORKING";
            StatusIndicatorText.Foreground =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(22, 131, 216));

            Progress<BackupProgress> progress = new(update =>
            {
                _processedItems += update.ItemsProcessed;

                StatusText.Text =
                    $"Processing cache data — {_processedItems} item(s) processed.";

                OperationDetailText.Text = update.CurrentPath;
            });

            try
            {
                await operation(
                    _operationCancellation.Token,
                    progress);
            }
            catch (OperationCanceledException)
            {
                StatusText.Text =
                    $"Operation cancelled after {_processedItems} item(s).";

                MessageBox.Show(
                    "The operation was cancelled. Items already moved remain " +
                    "safely recorded in the backup manifest and can be restored.",
                    "Operation Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            finally
            {
                _operationCancellation.Dispose();
                _operationCancellation = null;

                OperationsPanel.IsEnabled = true;
                SettingsButton.IsEnabled = true;
                OperationProgressPanel.Visibility = Visibility.Collapsed;
                StatusIndicatorText.Text = "●  READY";
                StatusIndicatorText.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(70, 199, 120));
            }
        }

        private void CancelOperationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CancelOperationButton.IsEnabled = false;
            OperationDetailText.Text =
                "Cancelling after the current file operation...";

            _operationCancellation?.Cancel();
        }

        private async Task ExecuteCleanupAsync(
            CacheCleanupDefinition definition,
            CancellationToken cancellationToken,
            IProgress<BackupProgress> progress)
        {
            try
            {
                StatusText.Text = definition.ProcessingStatus;

                CacheCleanupResult outcome = await
                    _cleanupCoordinator.ExecuteAsync(
                        definition,
                        progress,
                        cancellationToken);

                if (!outcome.FoundAnyCache)
                {
                    StatusText.Text = definition.EmptyStatus;

                    MessageBox.Show(
                        definition.EmptyMessage,
                        definition.EmptyTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                ShowCleanupResult(
                    definition.OperationName,
                    outcome.BackupResult);

                DetectCaches();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                StatusText.Text = definition.FailureStatus;

                MessageBox.Show(
                    ex.Message,
                    definition.FailureTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

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

        private async void ScanButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await RunOperationAsync(
                async (cancellationToken, progress) =>
                {
                    StatusText.Text = "Scanning cache locations...";
                    OperationDetailText.Text =
                        "Checking known MSFS and GPU cache paths...";

                    var caches = await Task.Run(
                        () => _cacheScanService.Scan(
                            progress,
                            cancellationToken),
                        cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    CacheScanWindow scanWindow =
                        new CacheScanWindow(caches);

                    scanWindow.Owner = this;
                    scanWindow.ShowDialog();
                });
        }

        // ---------------------------------------------------------
        // GPU SHADER CACHE
        // ---------------------------------------------------------

        private async void ShaderCacheButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateGpuCleanup(),
                    token,
                    progress));
        }

        // ---------------------------------------------------------
        // ROLLING CACHE
        // ---------------------------------------------------------

        private async void RollingCacheButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateRollingCacheCleanup(),
                    token,
                    progress));
        }


        // ---------------------------------------------------------
        // MSFS CACHE
        // ---------------------------------------------------------

        private async void MSFSCacheButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateMsfsCacheCleanup(),
                    token,
                    progress));
        }


        // ---------------------------------------------------------
        // SCENERY CACHE
        // ---------------------------------------------------------

        private async void SceneryCacheButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateSceneryCacheCleanup(),
                    token,
                    progress));
        }


        // ---------------------------------------------------------
        // SCENERY INDEXES
        // ---------------------------------------------------------

        private async void SceneryIndexesButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateSceneryIndexesCleanup(),
                    token,
                    progress));
        }


        // ---------------------------------------------------------
        // DCE CACHE
        // ---------------------------------------------------------

        private async void DCEButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateDceCleanup(),
                    token,
                    progress));
        }


        // ---------------------------------------------------------
        // STREAMED PACKAGES
        // ---------------------------------------------------------

        private async void StreamedPackagesButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateStreamedPackagesCleanup(),
                    token,
                    progress));
        }


        // ---------------------------------------------------------
        // SIMOBJECTS CACHE
        // ---------------------------------------------------------

private async void SimObjectsButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateSimObjectsCleanup(),
                    token,
                    progress));
        }


// ---------------------------------------------------------
        // WASM CACHE
// ---------------------------------------------------------

        private async void WASMButton_Click(
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

            await RunOperationAsync(
                (token, progress) => ExecuteCleanupAsync(
                    _cleanupDefinitions.CreateWasmCleanup(),
                    token,
                    progress));
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
        // BACKUP MANAGER
        // ---------------------------------------------------------

        private void BackupManagerButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            BackupManagerWindow backupManagerWindow =
                new BackupManagerWindow(_backupService);

            backupManagerWindow.Owner = this;

            backupManagerWindow.ShowDialog();
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
