# Changelog

## [Unreleased]

### Added

- `quill pr checks`: Prints a `verdict`: `blocked`, `waiting`, `ready`, or `null`.
- `pr list --reviewer` and `--author`, `wi list --assignee`, `wi create --assignee`, `backlog create --assignee`: Accept `@me`, a display name, a unique name or an email address. A value that matches nobody or several people exits 1.

### Changed

- Installing needs the .NET 11 SDK. The portable build needs the .NET 11 runtime.
- `backlog create --assigned-to` is now `--assignee`.
- `wi list --assignee`, `wi create --assignee`: Resolve the person first. A value other than `@me` needs the Identity: Read PAT scope.
- `pr list --reviewer`, `--author`, `wi list --assignee`, `wi create --assignee`, `backlog create --assignee`: A blank value exits 3. `backlog create --assignee ""` used to use the current user.
- `pr list` (`--skip`, `--state`), `pr threads --limit`, `wi comments --limit`, `wi tree --depth`: Report a bad value as a usage error on stderr with exit code 3, like the `pipeline` commands, instead of a JSON error row on stdout.
- Every command: A failure exits with the code of its failure mode: 1 Rejected, 2 Unavailable, 3 Usage, 4 Defect, 5 Denied. Exit codes 4 and 5 are new.
- Every command: A 401 or 403 from the server exits 5. It exited 2. A command that resolves a person names the Identity: Read PAT scope on a 403 too, not only on a 401.
- `wi view`, `pr view`, `pipeline view` and every other read: A 400, 404, 409, 412 or 422 from the server exits 1. It exited 2. This includes an ID that does not exist.
- `pr create`: A 400, 404, 409, 412 or 422 from the server exits 1. It exited 2. `TF401179`, for a pull request already open on the branch pair, is one of them.
- `pr create`: A repository with no default branch exits 1. It exited 3.
- `pipeline trigger`, `pipeline cancel`, `pipeline retry`: A 404 exits 1. It exited 2.
- `wi create`, `backlog push`, `backlog create`, `pipeline trigger`, `pipeline cancel`, `pipeline retry`: A 408 exits 2. It exited 1.
- Every command: A status that quill does not handle, such as a 3xx or a 418, exits 4. It exited 1 for a 4xx on `wi create`, `backlog push`, `backlog create`, `pipeline trigger`, `pipeline cancel` and `pipeline retry`, and 2 everywhere else.
- `wi list`, `wi view --with-children`, `wi tree`, `pr view`: Quill retries a 408 on the work item batch read once, like a 429 or a 5xx. When the retry fails too, the command exits 0. `wi list` and `wi view --with-children` leave out the items. `wi tree` and `pr view` mark them `batch-failed`. The command exited 2.
- `pipeline log`: A log ID that the run does not have exits 1. It exited 2.
- `pipeline trigger`: When the run is queued and the read after it fails, the message still starts `Queued run N, but reading it failed`. The exit code now follows the read: 1 for a 400, 404, 409, 412 or 422, 2 for no usable answer, and 5 for a 401 or 403. It was 2 for every failed status. A read that hits a Defect exits 4, and its message does not start `Queued run N`.
- `wi create`, `backlog create`: When quill creates the work item and a later step fails, the message starts `Created work item N, but` and names the step: `linking it failed`, `reading it failed` or `writing the file failed`. The exit code still follows the failed step. The message named only the failure of the step.
- `backlog create`: Exits 3 when `id` is not 0, `title` is empty, `type` is missing or unknown, or `parentId` is missing or 0. It exited 1.
- Every command: An answer that quill cannot read exits 4. It exited 3, or escaped as an unhandled exception when the body was not JSON.
- Every command: A failure that quill does not handle prints an error row with exit 4 and writes the quill version and the stack trace to stderr. It escaped as an unhandled exception, with no error row. Every error row with exit 4 ends with `This is a bug in quill. Report it at https://github.com/raeffs/quill/issues and attach the text quill wrote to stderr.`
- Every command: When a request gets no answer within 30 seconds and the command fails, quill prints an error row and exits 2. The timeout escaped as an unhandled exception, with no error row.
- Every command: The error row of a failed request no longer starts with `API error:`. It carries the server's message and the status, or `The server answered 503 (Service Unavailable).` when the server sends no message. A request with no response reads `No answer from the server: …`, and a timeout reads `The server did not answer within 30 seconds.`

### Fixed

- `backlog pull` and the commands that print a description or comment now convert a work item link on any host to the `(#id)` form. A link written before a server move used to stay a full URL.
- `backlog pull` and the commands that print a description or comment: A link lookup that hits a Defect fails the command with exit 4. Quill used to hide the Defect and print the fallback link text.
- `wi view`, `wi comments`, `wi tree`, `backlog pull`, `pr view`, `pr threads`, `pr revisions`: Report an ID below 1 as a usage error on stderr with exit code 3. `wi list` and `pr list` do the same for a `--limit` below 1.
- Every command: A `.quill.json` that is not valid JSON, or that holds a value of the wrong type, exits 3 with `Failed to parse .quill.json: {reason}`. A `.quill.json` that quill cannot read exits 3 with `Cannot read {path}: {reason}`. Both escaped as an unhandled exception, with no error row.
- `backlog pull`, `backlog push`, `backlog create`, `wi create --description-file`, `pr create --description-file`: A file or directory that quill cannot read or write exits 3 with `Cannot read {path}: {reason}` or `Cannot write {path}: {reason}`. It escaped as an unhandled exception, with no error row. A file that does not exist still exits 3 with `File not found: {path}`.
- `wi create --parent`, `pr create` (`--repo`, `--source-branch`, `--title`): Report a `--parent` below 1, or an empty or blank value, as a usage error on stderr with exit code 3. Each escaped as an unhandled exception, with no error row.
- `backlog push`, `backlog create`: An `id` below 1 on a push exits 3 with `Work item id must be a positive integer for a push. To create the work item, set id to 0 and run 'backlog create'.` A negative `parentId` on a create exits 3 with `Frontmatter must contain a positive 'parentId' field.` Each escaped as an unhandled exception, with no error row.
- Every command: A `serverUrl` in `.quill.json` that is not a valid URL exits 3 with `serverUrl is not a valid URL: {value}`. It escaped as an unhandled exception, with no error row.
- `wi create`, `backlog create`, `backlog push`: A description link whose ID is too large for a work item ID, such as `[x](#99999999999)`, stays an ordinary link. It escaped as an unhandled exception, with no error row.

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
