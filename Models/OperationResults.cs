namespace MSFSCacheManager.Models
{
    public class BackupResult
    {
        public int FilesMoved { get; set; }

        public int FilesSkipped { get; set; }

        public int FoldersMoved { get; set; }

        public int FoldersSkipped { get; set; }

        public int NotFoundCount { get; set; }

        public int ErrorCount { get; set; }

        public void Add(BackupResult other)
        {
            FilesMoved += other.FilesMoved;
            FilesSkipped += other.FilesSkipped;
            FoldersMoved += other.FoldersMoved;
            FoldersSkipped += other.FoldersSkipped;
            NotFoundCount += other.NotFoundCount;
            ErrorCount += other.ErrorCount;
        }
    }

    public class RestoreResult
    {
        public int FilesRestored { get; set; }

        public int FoldersRestored { get; set; }

        public int ConflictsSkipped { get; set; }

        public int NotFoundCount { get; set; }

        public int ErrorCount { get; set; }

        public string ReportPath { get; set; } = "";
    }
}
