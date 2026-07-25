using MSFSCacheManager.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MSFSCacheManager.Windows
{
    public partial class BackupManagerWindow : Window
    {
        private readonly BackupService _backupService;

        private readonly Dictionary<string, string> _sessionPaths =
            new Dictionary<string, string>();

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

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}