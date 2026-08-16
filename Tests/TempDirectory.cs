namespace MSFSCacheManager.Tests;

internal sealed class TempDirectory : IDisposable
{
    private readonly string _testRoot;

    public TempDirectory()
    {
        _testRoot = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MSFSCacheManager.Tests");

        Path = System.IO.Path.Combine(
            _testRoot,
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetPath(params string[] parts)
    {
        string result = Path;

        foreach (string part in parts)
        {
            result = System.IO.Path.Combine(result, part);
        }

        return result;
    }

    public void Dispose()
    {
        string root =
            System.IO.Path.GetFullPath(_testRoot) +
            System.IO.Path.DirectorySeparatorChar;

        string target = System.IO.Path.GetFullPath(Path);

        if (!target.StartsWith(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Refusing to remove an unsafe test path.");
        }

        if (Directory.Exists(target))
        {
            Directory.Delete(target, true);
        }
    }
}
