using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MSFSCacheManager.Services
{
    public class BackupService
    {
        private readonly string _backupRoot;

        public BackupService()
        {
            _backupRoot = Path.Combine(
                AppContext.BaseDirectory,
                "Backups");
        }

        // ---------------------------------------------------------
        // BACKUP ROOT
        // ---------------------------------------------------------

        public string GetBackupRoot()
        {
            return _backupRoot;
        }

        // ---------------------------------------------------------
        // CREATE BACKUP ROOT
        // ---------------------------------------------------------

        public void EnsureBackupRootExists()
        {
            Directory.CreateDirectory(_backupRoot);
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
                    _backupRoot,
                    timestamp);

            // Prevent duplicate session folder names
            // if two operations start in the same second.

            if (Directory.Exists(sessionFolder))
            {
                sessionFolder += "_" +
                    DateTime.Now.ToString("fff");
            }

            Directory.CreateDirectory(
                sessionFolder);

            return sessionFolder;
        }

        // ---------------------------------------------------------
        // MOVE DIRECTORY CONTENTS TO BACKUP
        // ---------------------------------------------------------

        public BackupResult MoveDirectoryContentsToBackup(
            string sourcePath,
            string backupSession,
            string category,
            List<string> report)
        {
            BackupResult result =
                new BackupResult();

            if (!Directory.Exists(sourcePath))
            {
                report.Add(
                    $"NOT FOUND: {sourcePath}");

                result.NotFoundCount++;

                return result;
            }

            report.Add("");
            report.Add(
                $"SOURCE: {sourcePath}");

            string sourceFolderName =
                new DirectoryInfo(
                    sourcePath).Name;

            string destinationRoot =
                Path.Combine(
                    backupSession,
                    category,
                    sourceFolderName);

            try
            {
                Directory.CreateDirectory(
                    destinationRoot);
            }
            catch (Exception ex)
            {
                report.Add(
                    $"ERROR CREATING BACKUP FOLDER: {destinationRoot}");

                report.Add(
                    $"   {ex.Message}");

                result.ErrorCount++;

                return result;
            }

            // -----------------------------------------------------
            // MOVE FILES
            // -----------------------------------------------------

            string[] files;

            try
            {
                files =
                    Directory.GetFiles(
                        sourcePath);
            }
            catch (Exception ex)
            {
                report.Add(
                    $"ERROR READING FILES: {sourcePath}");

                report.Add(
                    $"   {ex.Message}");

                result.ErrorCount++;

                return result;
            }

            foreach (string file in files)
            {
                try
                {
                    string fileName =
                        Path.GetFileName(
                            file);

                    string destination =
                        GetUniqueDestinationPath(
                            destinationRoot,
                            fileName);

                    File.Move(
                        file,
                        destination);

                    report.Add(
                        $"MOVED FILE: {file}");

                    report.Add(
                        $"        TO: {destination}");

                    result.FilesMoved++;
                }
                catch (Exception ex)
                {
                    report.Add(
                        $"SKIPPED FILE: {file}");

                    report.Add(
                        $"   REASON: {ex.Message}");

                    result.FilesSkipped++;
                    result.ErrorCount++;
                }
            }

            // -----------------------------------------------------
            // MOVE SUBFOLDERS
            // -----------------------------------------------------

            string[] directories;

            try
            {
                directories =
                    Directory.GetDirectories(
                        sourcePath);
            }
            catch (Exception ex)
            {
                report.Add(
                    $"ERROR READING SUBFOLDERS: {sourcePath}");

                report.Add(
                    $"   {ex.Message}");

                result.ErrorCount++;

                return result;
            }

            foreach (string directory in directories)
            {
                MoveDirectoryRecursive(
                    directory,
                    destinationRoot,
                    report,
                    result);
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
            BackupResult result)
        {
            string folderName =
                new DirectoryInfo(
                    sourceDirectory).Name;

            string destinationDirectory =
                GetUniqueDestinationPath(
                    destinationParent,
                    folderName);

            // First try moving the whole directory.
            // This is much faster when no files are locked.

            try
            {
                Directory.Move(
                    sourceDirectory,
                    destinationDirectory);

                report.Add(
                    $"MOVED FOLDER: {sourceDirectory}");

                report.Add(
                    $"          TO: {destinationDirectory}");

                result.FoldersMoved++;

                return;
            }
            catch
            {
                // If the complete folder cannot be moved,
                // process its contents individually.
            }

            try
            {
                Directory.CreateDirectory(
                    destinationDirectory);
            }
            catch (Exception ex)
            {
                report.Add(
                    $"SKIPPED FOLDER: {sourceDirectory}");

                report.Add(
                    $"   REASON: {ex.Message}");

                result.FoldersSkipped++;
                result.ErrorCount++;

                return;
            }

            // Move individual files

            try
            {
                foreach (
                    string file
                    in Directory.GetFiles(
                        sourceDirectory))
                {
                    try
                    {
                        string destination =
                            GetUniqueDestinationPath(
                                destinationDirectory,
                                Path.GetFileName(file));

                        File.Move(
                            file,
                            destination);

                        report.Add(
                            $"MOVED FILE: {file}");

                        report.Add(
                            $"        TO: {destination}");

                        result.FilesMoved++;
                    }
                    catch (Exception ex)
                    {
                        report.Add(
                            $"SKIPPED FILE: {file}");

                        report.Add(
                            $"   REASON: {ex.Message}");

                        result.FilesSkipped++;
                        result.ErrorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                report.Add(
                    $"ERROR READING FOLDER: {sourceDirectory}");

                report.Add(
                    $"   {ex.Message}");

                result.ErrorCount++;
            }

            // Process nested folders

            try
            {
                foreach (
                    string subDirectory
                    in Directory.GetDirectories(
                        sourceDirectory))
                {
                    MoveDirectoryRecursive(
                        subDirectory,
                        destinationDirectory,
                        report,
                        result);
                }
            }
            catch (Exception ex)
            {
                report.Add(
                    $"ERROR READING SUBFOLDERS: {sourceDirectory}");

                report.Add(
                    $"   {ex.Message}");

                result.ErrorCount++;
            }

            // Remove empty original folder

            try
            {
                if (Directory.Exists(sourceDirectory) &&
                    Directory.GetFileSystemEntries(
                        sourceDirectory).Length == 0)
                {
                    Directory.Delete(
                        sourceDirectory);

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
            List<string> report)
        {
            BackupResult result =
                new BackupResult();

            if (!File.Exists(sourcePath))
            {
                report.Add(
                    $"NOT FOUND: {sourcePath}");

                result.NotFoundCount++;

                return result;
            }

            try
            {
                string destinationFolder =
                    Path.Combine(
                        backupSession,
                        category);

                Directory.CreateDirectory(
                    destinationFolder);

                string fileName =
                    Path.GetFileName(
                        sourcePath);

                string destinationPath =
                    GetUniqueDestinationPath(
                        destinationFolder,
                        fileName);

                File.Move(
                    sourcePath,
                    destinationPath);

                report.Add(
                    $"MOVED FILE: {sourcePath}");

                report.Add(
                    $"        TO: {destinationPath}");

                result.FilesMoved++;
            }
            catch (Exception ex)
            {
                report.Add(
                    $"SKIPPED FILE: {sourcePath}");

                report.Add(
                    $"   REASON: {ex.Message}");

                result.FilesSkipped++;
                result.ErrorCount++;
            }

            return result;
        }



        // ---------------------------------------------------------
        // UNIQUE DESTINATION
        // ---------------------------------------------------------

        private string GetUniqueDestinationPath(
            string destinationFolder,
            string name)
        {
            string destination =
                Path.Combine(
                    destinationFolder,
                    name);

            if (!Directory.Exists(destination) &&
                !File.Exists(destination))
            {
                return destination;
            }

            string fileName =
                Path.GetFileNameWithoutExtension(
                    name);

            string extension =
                Path.GetExtension(
                    name);

            string timestamp =
                DateTime.Now.ToString(
                    "HH-mm-ss-fff");

            return Path.Combine(
                destinationFolder,
                $"{fileName}_{timestamp}{extension}");
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

            output.AddRange(
                report);

            File.WriteAllLines(
                reportPath,
                output);
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
                    FileName = _backupRoot,
                    UseShellExecute = true
                });
        }
    }

    // -------------------------------------------------------------
    // BACKUP RESULT
    // -------------------------------------------------------------

    public class BackupResult
    {
        public int FilesMoved { get; set; }

        public int FilesSkipped { get; set; }

        public int FoldersMoved { get; set; }

        public int FoldersSkipped { get; set; }

        public int NotFoundCount { get; set; }

        public int ErrorCount { get; set; }

        public void Add(
            BackupResult other)
        {
            FilesMoved +=
                other.FilesMoved;

            FilesSkipped +=
                other.FilesSkipped;

            FoldersMoved +=
                other.FoldersMoved;

            FoldersSkipped +=
                other.FoldersSkipped;

            NotFoundCount +=
                other.NotFoundCount;

            ErrorCount +=
                other.ErrorCount;
        }
    }
}