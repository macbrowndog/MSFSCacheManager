using MSFSCacheManager.Services;

namespace MSFSCacheManager.Tests;

public class PathSafetyServiceTests
{
    [Theory]
    [InlineData(@"C:\Caches\MSFS", @"C:\Caches\MSFS", true)]
    [InlineData(@"C:\Caches\MSFS", @"C:\Caches\MSFS\Backups", true)]
    [InlineData(@"C:\Backups", @"C:\Backups\MSFSCache", true)]
    [InlineData(@"C:\Caches\MSFS", @"C:\Backups\MSFS", false)]
    [InlineData(@"C:\Caches\MSFS", @"C:\Caches\MSFS2", false)]
    [InlineData(@"C:\Caches\MSFS", @"c:\caches\msfs\", true)]
    public void PathsOverlap_DetectsOnlyRealOverlap(
        string first,
        string second,
        bool expected)
    {
        Assert.Equal(
            expected,
            PathSafetyService.PathsOverlap(first, second));
    }

    [Fact]
    public void IsSameOrWithin_HandlesDriveRoot()
    {
        Assert.True(
            PathSafetyService.IsSameOrWithin(
                @"C:\Caches\MSFS",
                @"C:\"));
    }

    [Fact]
    public void PathsOverlap_RejectsEmptyPath()
    {
        Assert.Throws<ArgumentException>(
            () => PathSafetyService.PathsOverlap("", @"C:\Backups"));
    }
}
