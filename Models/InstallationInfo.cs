namespace MSFSCacheManager.Models
{
    public class InstallationInfo
    {
        public bool IsDetected { get; set; }

        public string Simulator { get; set; } = "";

        public string Platform { get; set; } = "";

        public string UserCfgPath { get; set; } = "";

        public string InstalledPackagesPath { get; set; } = "";
    }
}