using Microsoft.Win32;
using MSFSCacheManager.Models;
using MSFSCacheManager.Services;
using System;
using System.IO;
using System.Windows;

namespace MSFSCacheManager.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly InstallationService _installationService;

        public SettingsWindow()
        {
            InitializeComponent();

            _settingsService = new SettingsService();
            _installationService = new InstallationService();

            LoadSettings();
            LoadInstallationInformation();
        }

        private void LoadSettings()
        {
            AppSettings settings = _settingsService.Load();

            BackupFolderTextBox.Text = settings.BackupFolder;
        }

        private void LoadInstallationInformation()
        {
            string? userCfgPath =
                _installationService.GetUserCfgPath();

            string? packagesPath =
                _installationService.GetInstalledPackagesPath();

            if (string.IsNullOrWhiteSpace(userCfgPath))
            {
                DetectionStatusText.Text = "MSFS 2024 not detected";
                UserCfgPathText.Text = "Not found";
                PackagesPathText.Text = "Not found";

                return;
            }

            DetectionStatusText.Text = "MSFS 2024 detected";
            UserCfgPathText.Text = userCfgPath;

            PackagesPathText.Text =
                string.IsNullOrWhiteSpace(packagesPath)
                    ? "InstalledPackagesPath not found"
                    : packagesPath;
        }

        private void BrowseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = "Choose Backup Folder",
                InitialDirectory = BackupFolderTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                BackupFolderTextBox.Text = dialog.FolderName;
            }
        }

        private void RestoreDefaultsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AppSettings defaults =
                _settingsService.CreateDefaultSettings();

            BackupFolderTextBox.Text = defaults.BackupFolder;
        }

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string backupFolder =
                BackupFolderTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(backupFolder))
            {
                MessageBox.Show(
                    "Please choose a backup folder.",
                    "Backup Folder Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Directory.CreateDirectory(backupFolder);

                _settingsService.Save(
                    new AppSettings
                    {
                        BackupFolder = backupFolder
                    });

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Unable to Save Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}