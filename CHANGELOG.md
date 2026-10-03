# Changelog

## v2.0.1 — 3 October 2026

### Added
- Separate **Open WASM 2020** and **Open WASM 2024** buttons to open the respective folders in File Explorer.
- Separate **Backup + clear WASM 2020** and **Backup + clear WASM 2024** buttons.
- WASM backup-and-clear actions move only files directly inside the selected version folder into a restorable backup. All subfolders, including empty folders, and their contents remain intact.
- Regression tests covering both versions, hidden loose files, backup contents, and preservation of nested aircraft files.

### Changed
- The Microsoft Store WASM scan now reports `LocalState\WASM\MSFS2020` and `LocalState\WASM\MSFS2024` separately instead of treating the entire `WASM` directory as one cache.
- WASM scan entries remain informational and unselected by default. Their sizes include subfolder contents and do not represent the amount removed by loose-file cleanup.
- Replaced whole-folder WASM cleanup with version-specific loose-file actions to protect aircraft and add-on data.
- WASM cleanup requires the simulator to be closed, skips file links, and refuses linked folder paths.

### Fixed
- **Select All** now selects every scan result, including WASM entries.
- The `MSFS2020` WASM subfolder is labelled **MSFS 2020**, even inside the MSFS 2024 Microsoft Store package.

### Validation
- All 25 automated tests pass.
- Release build published successfully.

