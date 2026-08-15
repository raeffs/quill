# Changelog

## [Unreleased]

### Added

- `quill pr list --source-branch` and `--target-branch`.
- `quill pr list --skip N`, for paging alongside `--limit`.
- `mergeStatus`, `labels` and `votes` on every `quill pr list` and `quill pr view` row.

### Changed

- `quill pr list` returns draft pull requests.
- `quill pr list --reviewer` has no default.

### Fixed

- A pull request payload without a `reviewers` key no longer crashes `quill pr list` and `quill pr view`.

## [1.0.0]

### Added

- First public release on nuget.org as `Raeffs.Quill`, with Native AOT packages for `linux-x64` and `win-x64` and a framework-dependent fallback.
