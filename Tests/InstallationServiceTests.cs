using MSFSCacheManager.Services;

namespace MSFSCacheManager.Tests;

public class InstallationServiceTests
{
    [Fact]
    public void MalformedUserCfg_ReturnsNullInsteadOfInventingPath()
    {
        using TempDirectory temp = new();

        string roaming = temp.GetPath("Roaming");
        string cfgDirectory = Path.Combine(
            roaming,
            "Microsoft Flight Simulator 2024");
        Directory.CreateDirectory(cfgDirectory);
        File.WriteAllLines(
            Path.Combine(cfgDirectory, "UserCfg.opt"),
            new[]
            {
                "Version 1",
                "InstalledPackagesPath missing-quotes",
                "InstalledPackagesPath \"unterminated"
            });

        InstallationService service = new(
            temp.GetPath("Local"),
            roaming);

        Assert.Null(service.GetAutomaticallyDetectedPackagesPath());
    }

    [Fact]
    public void ValidUserCfg_ExtractsQuotedPackagesPath()
    {
        using TempDirectory temp = new();

        string roaming = temp.GetPath("Roaming");
        string cfgDirectory = Path.Combine(
            roaming,
            "Microsoft Flight Simulator 2024");
        string packages = temp.GetPath("Packages");
        Directory.CreateDirectory(cfgDirectory);
        File.WriteAllText(
            Path.Combine(cfgDirectory, "UserCfg.opt"),
            $"InstalledPackagesPath \"{packages}\"");

        InstallationService service = new(
            temp.GetPath("Local"),
            roaming);

        Assert.Equal(
            packages,
            service.GetAutomaticallyDetectedPackagesPath());
    }
}
