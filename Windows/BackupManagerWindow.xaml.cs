using MSFSCacheManager.Services;
using System;
using System.Collections.Generic;
using MSFSCacheManager.Models;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MSFSCacheManager.Windows
{
    public partial class BackupManagerWindow : Window
    {
        private readonly BackupService _backupService;

        private readonly Dictionary<string, string> _sessionPaths =
            new Dictionary<string, string>();

        private CancellationTokenSource? _restoreCancellation;
        private int _restoreItemsProcessed;

        public BackupManagerWindow(
            BackupService backupService)
        {
            InitializeComponent();

            _backupService = backupService;

            LoadBackupSessions();
        }

        private void LoadBackupSessions()
        {
            BackupSessionsList.ItemsSource = null;

            _sessionPaths.Clear();

            ReportTextBox.Text =
                "Select a backup session to view its report.";

            RestoreButton.IsEnabled = false;

            string backupRoot =
                _backupService.GetBackupRoot();

            if (!Directory.Exists(backupRoot))
            {
                ReportTextBox.Text =
                    "No backup folder has been created yet.";

                return;
            }

            DirectoryInfo[] sessions =
                new DirectoryInfo(backupRoot)
                    .GetDirectories()
                    .OrderByDescending(
                        directory => directory.CreationTime)
                    .ToArray();

            List<string> sessionNames =
                new List<string>();

            foreach (DirectoryInfo session in sessions)
            {
                sessionNames.Add(session.Name);

                _sessionPaths.Add(
                    session.Name,
                    session.FullName);
            }

            BackupSessionsList.ItemsSource = sessionNames;

            if (sessionNames.Count == 0)
            {
                ReportTextBox.Text =
                    "No backup sessions were found.";
            }
        }

        private void BackupSessionsList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            string? selectedSession =
                BackupSessionsList.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedSession) ||
                !_sessionPaths.ContainsKey(selectedSession))
            {
                return;
            }

            string reportPath =
                Path.Combine(
                    _sessionPaths[selectedSession],
                    "backup_report.txt");

            RestoreButton.IsEnabled =
                _backupService.HasRestoreManifest(
                    _sessionPaths[selectedSession]);

            if (!File.Exists(reportPath))
            {
                ReportTextBox.Text =
                    "No backup report was found for this session.";

                return;
            }

            try
            {
                ReportTextBox.Text =
                    File.ReadAllText(reportPath);
            }
            catch (Exception ex)
            {
                ReportTextBox.Text =
                    $"Unable to read the report.\n\n{ex.Message}";
            }
        }

        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadBackupSessions();
        }

        private void OpenSessionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? selectedSession =
                BackupSessionsList.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedSession) ||
                !_sessionPaths.ContainsKey(selectedSession))
            {
                MessageBox.Show(
                    "Please select a backup session first.",
                    "No Backup Session Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            OpenPath(_sessionPaths[selectedSession]);
        }

        private void OpenReportButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? selectedSession =
                BackupSessionsList.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedSession) ||
                !_sessionPaths.ContainsKey(selectedSession))
            {
                MessageBox.Show(
                    "Please select a backup session first.",
                    "No Backup Session Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            string reportPath =
                Path.Combine(
                    _sessionPaths[selectedSession],
                    "backup_report.txt");

            if (!File.Exists(reportPath))
            {
                MessageBox.Show(
                    "This backup session does not contain a report.",
                    "Report Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            OpenPath(reportPath);
        }

        private void OpenPath(string path)
        {
            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Open Backup",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RestoreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? selectedSession =
                BackupSessionsList.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedSession) ||
                !_sessionPaths.TryGetValue(
                    selectedSession,
                    out string? sessionPath))
            {
                MessageBox.Show(
                    "Please select a restorable backup session first.",
                    "No Backup Session Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (!_backupService.HasRestoreManifest(sessionPath))
            {
                MessageBox.Show(
                    "This session does not contain a restore manifest. " +
                    "Older backups can still be restored manually using " +
                    "their backup report.",
                    "Restore Manifest Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (IsMSFSRunning())
            {
                MessageBox.Show(
                    "Microsoft Flight Simulator appears to be running.\n\n" +
                    "Close MSFS 2020 or MSFS 2024 completely before " +
                    "restoring cache data.",
                    "Microsoft Flight Simulator Is Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MessageBoxResult confirmation = MessageBox.Show(
                $"Restore backup session {selectedSession}?\n\n" +
                "Files will be moved back to their original locations. " +
                "Existing files and folders will not be overwritten; " +
                "conflicts will remain in the backup session.",
                "Restore Backup Session",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _restoreCancellation = new CancellationTokenSource();
                _restoreItemsProcessed = 0;

                BackupSessionsList.IsEnabled = false;
                SessionActionsPanel.IsEnabled = false;
                RestoreProgressPanel.Visibility = Visibility.Visible;
                CancelRestoreButton.IsEnabled = true;
                RestoreProgressText.Text = "Preparing restore...";

                Progress<BackupProgress> progress = new(update =>
                {
                    _restoreItemsProcessed += update.ItemsProcessed;
                    RestoreProgressText.Text =
                        $"{_restoreItemsProcessed} item(s) processed — " +
                        update.CurrentPath;
                });

                RestoreButton.IsEnabled = false;

                RestoreResult result = await
                    _backupService.RestoreBackupSessionAsync(
                        sessionPath,
                        progress,
                        _restoreCancellation.Token);

                if (File.Exists(result.ReportPath))
                {
                    ReportTextBox.Text =
                        File.ReadAllText(result.ReportPath);
                }

                MessageBoxImage icon =
                    result.ErrorCount > 0 ||
                    result.ConflictsSkipped > 0
                        ? MessageBoxImage.Warning
                        : MessageBoxImage.Information;

                MessageBox.Show(
                    "Restore processing completed.\n\n" +
                    $"Files restored: {result.FilesRestored}\n" +
                    $"Folders restored: {result.FoldersRestored}\n" +
                    $"Conflicts skipped: {result.ConflictsSkipped}\n" +
                    $"Items not found: {result.NotFoundCount}\n" +
                    $"Errors: {result.ErrorCount}\n\n" +
                    "A detailed restore report was saved in the session folder.",
                    "Restore Complete",
                    MessageBoxButton.OK,
                    icon);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(
                    "Restore was cancelled. Items already restored remain " +
                    "in their active locations, and unrestored items remain " +
                    "available in the backup session.",
                    "Restore Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Restore Backup",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _restoreCancellation?.Dispose();
                _restoreCancellation = null;

                BackupSessionsList.IsEnabled = true;
                SessionActionsPanel.IsEnabled = true;
                RestoreProgressPanel.Visibility = Visibility.Collapsed;

                RestoreButton.IsEnabled =
                    _backupService.HasRestoreManifest(sessionPath);
            }
        }

        private void CancelRestoreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CancelRestoreButton.IsEnabled = false;
            RestoreProgressText.Text =
                "Cancelling after the current file operation...";

            _restoreCancellation?.Cancel();
        }

        private bool IsMSFSRunning()
        {
            foreach (string processName in new[]
            {
                "FlightSimulator",
                "FlightSimulator2024"
            })
            {
                if (Process.GetProcessesByName(processName).Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
