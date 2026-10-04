# Changelog

## v2.0.2 — 4 October 2026

### Changed
- Replaced Open WASM and bulk cleanup buttons with Select WASM 2020 files and Select WASM 2024 files.
- Added an in-app folder browser: directories appear first, double-click opens a folder, and Up returns to its parent within the selected WASM root.
- Shows only the current directory instead of every file recursively.
- Users select files, including files within add-on folders, then confirm backup and removal. No cancels without changes.
- Removed the Windows shell file picker so right-click Delete cannot bypass backup confirmation.
- Only selected files move to backup. Directories and unselected files remain intact.
- New backups preserve source-relative directories beneath WASM-MSFS2020 or WASM-MSFS2024. Existing backups are unchanged.
- Links and paths outside the selected WASM root remain excluded.

### Validation
- 26 automated tests pass, covering selected-file backups, original directory paths, and rejection of outside paths.
- User verified selection and backup behavior.


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
