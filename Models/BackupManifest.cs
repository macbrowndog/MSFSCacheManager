using System;
using System.Collections.Generic;

namespace MSFSCacheManager.Models
{
    public class BackupManifest
    {
        public int FormatVersion { get; set; } = 1;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public List<BackupManifestEntry> Entries { get; set; } = new();
    }

    public class BackupManifestEntry
    {
        public string SourcePath { get; set; } = "";

        public string BackupPath { get; set; } = "";

        public string Category { get; set; } = "";

        public string ItemType { get; set; } = "";

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
