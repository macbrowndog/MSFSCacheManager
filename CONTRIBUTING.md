# Contributing

Contributions and bug reports are welcome.

## Development setup

1. Install the .NET 8 SDK on Windows.
2. Fork or clone the repository.
3. Create a focused branch.
4. Build and run the safety suite:

```powershell
dotnet build MSFSCacheManager.csproj --configuration Release
dotnet test Tests\MSFSCacheManager.Tests.csproj --configuration Release
```

## Pull requests

- Keep cleanup-location changes separate from unrelated visual changes.
- Add or update tests for file movement, path validation, manifests, or restoration behavior.
- Never use real simulator data as a test fixture.
- Use unique temporary directories and verify deletion targets before cleanup.
- Explain any new cache location and whether it is Standard or Advanced risk.
- Confirm the Release build has no warnings and all tests pass.

Changes affecting cache paths or restore behavior should include a concise data-loss risk assessment.
