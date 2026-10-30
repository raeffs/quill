# Changelog

## [Unreleased]

### Changed

- `quill backlog pull`, `quill backlog push`, `quill backlog create`: Moved from the `wi` group. `quill wi pull`, `quill wi push` and `quill wi create` no longer parse.
- `quill wi view`: `--with-children` combines with any other flag.
- `quill wi list`: Takes `--title` to match title words. `--assignee` no longer defaults to `@me`, and a call without a filter exits with code 3.
- `quill backlog create`, `quill wi create-task`: The parent must fit a fixed type hierarchy. A Product Backlog Item or Bug needs a Feature parent, and a Task needs a Product Backlog Item or Bug parent. The parent's state no longer matters.
- `quill backlog create`: Accepts `type: Task`.

### Removed

- `quill wi view`: No longer offers `--markdown`. Use `quill backlog pull` to get the backlog file format.
- `quill wi search`: Use `quill wi list --title` instead.
- `.quill.json`: `allowedParentStates` is no longer read. A config that still has it loads, and the key is ignored.

## [1.2.0]

### Added

- `quill pr create`: Opens a pull request as a draft. Takes `--repo`, `--source-branch` and `--title`, an optional `--target-branch` that defaults to the repository's default branch, a `--description-file` that reads a path or stdin, and a repeatable `--work-item`.

### Fixed

- A command that fails now exits with the code it reports, rather than 0.

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
