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

        public SettingsWindow()
        {
            InitializeComponent();

            _settingsService = new SettingsService();

            LoadSettings();
        }

        private void LoadSettings()
        {
            AppSettings settings = _settingsService.Load();

            BackupFolderTextBox.Text = settings.BackupFolder;
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