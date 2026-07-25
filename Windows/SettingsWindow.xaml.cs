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

        // ---------------------------------------------------------
        // LOAD SETTINGS
        // ---------------------------------------------------------

        private void LoadSettings()
        {
            AppSettings settings = _settingsService.Load();

            BackupFolderTextBox.Text =
                settings.BackupFolder;

            PackagesFolderOverrideTextBox.Text =
                settings.PackagesFolderOverride;
        }

        // ---------------------------------------------------------
        // LOAD MSFS INSTALLATION INFORMATION
        // ---------------------------------------------------------

        private void LoadInstallationInformation()
        {
            string? userCfgPath =
                _installationService.GetUserCfgPath();

            string? activePackagesPath =
                _installationService.GetInstalledPackagesPath();

            if (!string.IsNullOrWhiteSpace(
                PackagesFolderOverrideTextBox.Text))
            {
                DetectionStatusText.Text =
                    "Manual folder override";

                UserCfgPathText.Text =
                    string.IsNullOrWhiteSpace(userCfgPath)
                        ? "Automatic UserCfg.opt not found"
                        : userCfgPath;
            }
            else if (string.IsNullOrWhiteSpace(userCfgPath))
            {
                DetectionStatusText.Text =
                    "MSFS 2024 not detected";

                UserCfgPathText.Text =
                    "Not found";
            }
            else
            {
                DetectionStatusText.Text =
                    "MSFS 2024 detected";

                UserCfgPathText.Text =
                    userCfgPath;
            }

            PackagesPathText.Text =
                string.IsNullOrWhiteSpace(activePackagesPath)
                    ? "Not found"
                    : activePackagesPath;
        }

        // ---------------------------------------------------------
        // BROWSE PACKAGES FOLDER
        // ---------------------------------------------------------

        private void BrowsePackagesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = "Choose MSFS Packages Folder",
                InitialDirectory =
                    PackagesFolderOverrideTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                PackagesFolderOverrideTextBox.Text =
                    dialog.FolderName;
            }
        }

        // ---------------------------------------------------------
        // BROWSE BACKUP FOLDER
        // ---------------------------------------------------------

        private void BrowseBackupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Title = "Choose Backup Folder",
                InitialDirectory =
                    BackupFolderTextBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                BackupFolderTextBox.Text =
                    dialog.FolderName;
            }
        }

        // ---------------------------------------------------------
        // RESTORE DEFAULTS
        // ---------------------------------------------------------

        private void RestoreDefaultsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AppSettings defaults =
                _settingsService.CreateDefaultSettings();

            BackupFolderTextBox.Text =
                defaults.BackupFolder;

            PackagesFolderOverrideTextBox.Text = "";

            LoadInstallationInformation();
        }

        // ---------------------------------------------------------
        // SAVE SETTINGS
        // ---------------------------------------------------------

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string backupFolder =
                BackupFolderTextBox.Text.Trim();

            string packagesOverride =
                PackagesFolderOverrideTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(backupFolder))
            {
                MessageBox.Show(
                    "Please choose a backup folder.",
                    "Backup Folder Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (!string.IsNullOrWhiteSpace(packagesOverride) &&
                !Directory.Exists(packagesOverride))
            {
                MessageBox.Show(
                    "The selected MSFS Packages folder does not exist.",
                    "Packages Folder Not Found",
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
                        BackupFolder = backupFolder,
                        PackagesFolderOverride =
                            packagesOverride
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

        // ---------------------------------------------------------
        // ABOUT WINDOW
        // ---------------------------------------------------------

        private void AboutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AboutWindow aboutWindow =
                new AboutWindow();

            aboutWindow.Owner = this;

            aboutWindow.ShowDialog();
        }

        // ---------------------------------------------------------
        // CANCEL
        // ---------------------------------------------------------

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}