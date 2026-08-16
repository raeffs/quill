# Changelog

## [1.1.0]

### Added

- `quill pr list`: Filter pull requests with `--source-branch` and `--target-branch`.
- `quill pr list`: Allow paging using `--skip N` alongside `--limit`.
- `quill pr list / view`: Include `mergeStatus`, `labels` and `votes`.
- `quill pr view`: Includes `lastMergeSourceCommit` and `lastMergeTargetCommit`.
- `quill pr revisions`: Lists revisions of a pull request.

### Changed

- `quill pr list`: Returns draft pull requests.
- `quill pr list`: `--reviewer` no longer has a default.

### Removed

- `quill pr view`: No longer offers `--with-diff-stats`.

### Fixed

- A pull request payload without a `reviewers` key no longer crashes `quill pr list` and `quill pr view`.

## [1.0.0]

### Added

- First public release on nuget.org as `Raeffs.Quill`, with Native AOT packages for `linux-x64` and `win-x64` and a framework-dependent fallback.
