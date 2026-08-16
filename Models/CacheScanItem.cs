using System;

namespace MSFSCacheManager.Models
{
    public class CacheScanItem
    {
        public bool IsSelected { get; set; } = true;

        public string Category { get; set; } = "";

        public string Simulator { get; set; } = "";

        public string Platform { get; set; } = "";

        public string RiskLevel { get; set; } = "";

        public string Path { get; set; } = "";

        public long SizeBytes { get; set; }

        public int FileCount { get; set; }

        public DateTime? LastModified { get; set; }

        public string FormattedSize => FormatSize(SizeBytes);

        public string FormattedLastModified =>
            LastModified?.ToString("g") ?? "Unknown";

        public static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unit = 0;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return $"{value:0.##} {units[unit]}";
        }
    }
}
