using System.Collections.Generic;

namespace MSFSCacheManager.Models
{
    public enum CacheItemType
    {
        DirectoryContents,
        File
    }

    public class CacheCleanupGroup
    {
        public string Heading { get; set; } = "";

        public string BackupCategory { get; set; } = "";

        public CacheItemType ItemType { get; set; }

        public List<string> Locations { get; set; } = new();
    }

    public class CacheCleanupDefinition
    {
        public string OperationName { get; set; } = "";

        public string ReportTitle { get; set; } = "";

        public string ProcessingStatus { get; set; } = "";

        public string EmptyStatus { get; set; } = "";

        public string EmptyMessage { get; set; } = "";

        public string EmptyTitle { get; set; } = "";

        public string FailureStatus { get; set; } = "";

        public string FailureTitle { get; set; } = "";

        public string RiskLevel { get; set; } = "Standard";

        public List<CacheCleanupGroup> Groups { get; set; } = new();
    }

    public class CacheCleanupResult
    {
        public bool FoundAnyCache { get; set; }

        public string BackupSession { get; set; } = "";

        public BackupResult BackupResult { get; set; } = new();
    }
}
