# Changelog

## [Unreleased]

### Added

- `quill pr checks`: Prints a `verdict`: `blocked`, `waiting`, `ready`, or `null`.
- `pr list --reviewer` and `--author`, `wi list --assignee`, `wi create --assignee`, `backlog create --assignee`: Accept `@me`, a display name, a unique name or an email address. A value that matches nobody or several people exits 1.

### Changed

- `backlog create --assigned-to` is now `--assignee`.
- `wi list --assignee`, `wi create --assignee`: Resolve the person first. A value other than `@me` needs the Identity: Read PAT scope.
- `pr list --reviewer`, `--author`, `wi list --assignee`, `wi create --assignee`, `backlog create --assignee`: A blank value exits 1. `backlog create --assignee ""` used to use the current user.
- `pr list` (`--skip`, `--state`), `pr threads --limit`, `wi comments --limit`, `wi tree --depth`: Report a bad value as a usage error on stderr with exit code 3, like the `pipeline` commands, instead of a JSON error row on stdout.

### Fixed

- `backlog pull` and the commands that print a description or comment now convert a work item link on any host to the `(#id)` form. A link written before a server move used to stay a full URL.

## [2.2.0]

### Added

- `quill pipeline list`: Lists pipelines.
- `quill pipeline runs`: Lists pipeline runs.
- `quill pipeline view`: Prints one pipeline run.
- `quill pipeline log`: Prints a pipeline log.
- `quill pipeline trigger`: Queues a pipeline run.
- `quill pipeline wait`: Waits until a pipeline run completes.
- `quill pipeline cancel`: Cancels a pipeline run.
- `quill pipeline retry`: Reruns the failed jobs of a pipeline run.
- `quill pipeline tests`: Lists the test runs of a pipeline run.
- `quill pr checks`: Prints the policy evaluations that gate a pull request.

## [2.1.0]

### Added

- `quill backlog create`, `quill wi create`: Create a Feature under an Epic.
- `quill backlog pull`, `quill backlog push`, `quill backlog create`: Support for `tags` and `predecessors` in the frontmatter.
- `quill wi create`: Takes a repeatable `--tag` and `--predecessor`.
- `quill wi view`, `quill wi create`: Print `tags` and `predecessors`.

### Changed

- `quill backlog create`, `quill wi create`: A new work item takes its parent's area path.

## [2.0.0]

### Added

- `quill wi create`: Creates a Product Backlog Item, Bug or Task under a parent.

### Changed

- `quill backlog pull`, `quill backlog push`, `quill backlog create`: Moved from the `wi` group.
- `quill wi view`: `--with-children` combines with any other flag.
- `quill wi list`: Takes `--title` to match title words. `--assignee` no longer defaults to `@me`.
- `quill backlog create`: The parent must fit a fixed type hierarchy.
- `quill backlog create`: Accepts `type: Task`.

### Removed

- `quill wi view`: No longer offers `--markdown`. Use `quill backlog pull` instead.
- `quill wi search`: Use `quill wi list --title` instead.
- `quill wi create-task`: Use `quill wi create --type Task` instead.
- `.quill.json`: `allowedParentStates` is no longer read.

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
