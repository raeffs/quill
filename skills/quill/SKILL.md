---
name: quill
description: Sync Azure DevOps backlog items with local markdown files. Use when the user asks to pull, push, create, update, or sync work items / backlog items / PBIs / tasks with Azure DevOps. Supports creating new PBIs/Bugs and tasks, and managing parent-child relationships. Validation of allowed states is enforced via .quill.json. Assignee validation uses the authenticated PAT user.
---

# Quill — Azure DevOps Backlog Sync

Quill is a CLI tool that syncs Azure DevOps Server backlog items with local markdown files. Use it to pull work items from the server into markdown files, edit them locally, and push changes back.

Requires the `quill` CLI on PATH. Install once per machine with:

```bash
dotnet tool install -g Raeffs.Quill
```

Requires the .NET 10 SDK.

## Executable

```
quill
```

## Prerequisites

- `QUILL_PAT` environment variable must be set with a Personal Access Token. Needs **Work Items: Read & Write** for `quill wi …` commands and additionally **Code: Read** for `quill pr …` commands.
- `.quill.json` config file must exist in either the current working directory (project-specific) or the user profile directory (`$USERPROFILE` on Windows, `$HOME` elsewhere, used as a fallback)

Don't preflight-check for the config file or prompt the user for its values — just run the command. Quill resolves the config itself and will report a clear error if it's genuinely missing from both locations.

### Configuration file (`.quill.json`)

```json
{
  "serverUrl": "https://your-server.com/tfs",
  "collection": "DefaultCollection",
  "project": "YourProject",
  "allowedStates": ["New", "Active"],
  "allowedParentStates": ["New", "Approved"]
}
```

All fields are required. `allowedStates` controls which work items can be pushed. `allowedParentStates` controls which parent states are allowed when creating new PBIs and tasks. Assignee is validated automatically against the PAT user.

## Work item commands

### Pull a work item

```bash
quill wi pull <work-item-id> <file-path>
```

Fetches the work item from Azure DevOps and writes it as a markdown file. Overwrites the file if it exists. If the work item has a parent, the `parentId` field is included in the frontmatter.

Example:
```bash
quill wi pull 12345 ./backlog/12345.md
```

### View a work item

```bash
quill wi view <id> [--markdown | --with-children]
```

Prints a work item to stdout without writing to disk. Use this when you just need to read a PBI — no target file, no overwrite, no cleanup. Reach for `pull` only when you intend to edit and push back.

Default output is a JSON object with eight keys:

- `id`, `type`, `title`, `state` — from the work item
- `assignedTo` — display name, or `null` if unassigned
- `parentId` — int, or `null` if there is no parent
- `description` — markdown-converted body (same pipeline as `pull`), `""` when empty
- `relatedIds` — ints from `Related` link relations, `[]` when none

Flags:

- `--markdown` — emit the exact document `pull` would have written (frontmatter + body), byte-for-byte. Consume it the same way as a pulled file.
- `--with-children` — append a top-level `children: [{id, title, state}, ...]` key. Fetched in one batch request. Always `[]` when there are none.

`--markdown` and `--with-children` are mutually exclusive (exit code 3).

Example:
```bash
quill wi view 12345
quill wi view 12345 --markdown
quill wi view 12345 --with-children
```

### Push a work item

```bash
quill wi push <file-path>
```

Reads the local markdown file and updates the work item on Azure DevOps. The work item ID comes from the file's frontmatter. Only updates if the work item's state is allowed and the work item is assigned to the authenticated user.

Example:
```bash
quill wi push ./backlog/12345.md
```

### Create a work item

```bash
quill wi create <file-path> [--assigned-to <name>]
```

Creates a new Product Backlog Item or Bug on Azure DevOps from a markdown file. The file must have frontmatter with `id: 0`, a `title`, a `type` (either "Product Backlog Item" or "Bug"), and a `parentId`. The parent work item must exist, be assigned to the current user, and be in an allowed state. The markdown body is pushed as the work item description, and `[text](#id)` references become `Related` links — same as `push`. On success, the file is rewritten with the server-assigned ID.

Use `--assigned-to` to assign the new work item to a specific user (any identifier Azure DevOps resolves, e.g. display name or unique name). If omitted, the work item is assigned to the authenticated PAT user.

Example:
```bash
quill wi create ./backlog/new-item.md
quill wi create ./backlog/new-item.md --assigned-to "Jane Doe"
```

### Create a task

```bash
quill wi create-task <parent-id> <title>
```

Creates a new Task as a child of the specified parent work item. The parent must exist, be assigned to the current user, and be in an allowed state. No markdown file is needed.

Example:
```bash
quill wi create-task 12345 "Implement login validation"
```

### Walk the subtree

```bash
quill wi tree <id> [--depth <N> | --all]
```

Fetches the parent-child hierarchy rooted at `<id>` in one go and prints it as nested JSON. Use it to orient yourself under a Feature or Epic. For just the direct children of a single node, use `quill wi tree <id> --depth 1`.

- `--depth N` (default `3`) — how many levels below the root to fetch. `1` = direct children only. The default matches the canonical Azure DevOps shape (Epic → Feature → PBI/Bug → Task).
- `--all` — unbounded traversal. Overrides `--depth`.

Each node shape depends on what was fetched:

| Shape | Meaning |
|-------|---------|
| `{ id, title, type, state, children: [...] }` | Fetched node. `children: []` means fetched but no children. |
| `{ id }` | Depth-clipped stub — child exists but was not fetched because of `--depth`. |
| `{ id, error: "unreadable" }` | Server returned 403/404 for this id individually. |
| `{ id, error: "batch-failed" }` | A sub-batch failed twice (5xx / timeout / network); all its ids are emitted with this error. |

Traversal follows parent-child relations only — `Related` links are not walked.

Example:
```bash
quill wi tree 35699              # Epic, default depth 3
quill wi tree 35699 --depth 1    # Epic + direct children only
quill wi tree 35699 --all        # whole subtree
```

### List my work items

```bash
quill wi list [--assignee <name>|@me] [--state <state>] [--type <type>] [--limit N]
```

Convenience wrapper over `search` for the canonical query "what work is assigned to me right now." `--assignee` defaults to `@me` when omitted; all other flags behave identically to `search`.

- `--assignee` — defaults to `@me`. Pass an explicit name/identifier to override. Users wanting a cross-assignee view should use `search` directly.
- `--state` and `--type` are multi-value — repeat the flag to OR values. No default; state/type values are not validated against `.quill.json`, so typos return an empty array.
- `--limit` caps results server-side via WIQL `$top=N` (default `50`).
- No positional query. Text search stays on `search`.

Results have the same JSON array shape as `search` and are ordered by `System.ChangedDate DESC`.

Example:
```bash
quill wi list
quill wi list --state Active
quill wi list --state New --state Active --type "Product Backlog Item"
quill wi list --assignee "Jane Doe" --type Bug
quill wi list --limit 10
```

### Search work items

```bash
quill wi search [<query>] [--assignee <name>|@me] [--state <state>] [--type <type>] [--limit N]
```

Runs a WIQL query across the entire project and prints matches as a flat JSON array. Use this when you need to find items that would otherwise require walking the hierarchy with `tree` (or `tree --depth 1`).

- Positional `<query>` is optional free text, matched against the **title only** (`[System.Title] CONTAINS WORDS '...'`).
- `--assignee` is single-value. Accepts `@me` (expands to the WIQL `@Me` macro) or any display name / identifier that Azure DevOps resolves. Same philosophy as `quill wi create --assigned-to`.
- `--state` and `--type` are multi-value — repeat the flag to OR values (e.g. `--state Active --state New`).
- `--limit` caps results server-side via WIQL `$top=N` (default `50`).
- Invoking with no positional and no filters exits with code `3` and an error — supply a query or at least one filter.

Results are ordered by `System.ChangedDate DESC` — items you've touched recently surface first. State/type values are not validated against `.quill.json`; typos return an empty array. Single quotes in user-supplied values are escaped (e.g. `O'Brien`). Items that fail to load in the follow-up batch are silently omitted — search is a discovery list, not a structural view.

Example:
```bash
quill wi search --assignee @me --state Active
quill wi search "login validation" --type Bug
quill wi search --state Active --state New --type "Product Backlog Item"
```

### Read comments on a work item

```bash
quill wi comments <id> [--limit N]
```

Prints the work item's comment thread as a JSON array, **newest first**. Deleted comments and system revision entries (state changes, field edits) are excluded. Each comment's body is converted to markdown via the same pipeline as `pull` and `view` — work-item URLs become `[Type: Title](#id)`.

- `--limit N` — return only the N most recent comments (must be `>= 1`, no upper cap). Omit to return all comments.
- Empty thread returns `[]`.

Example:
```bash
quill wi comments 12345
quill wi comments 12345 --limit 5
```

## Pull request commands

### List pull requests

```bash
quill pr list [--reviewer @me] [--author @me] [--state active|completed|abandoned|all] [--repo <name>] [--include-drafts] [--limit N]
```

Lists Azure DevOps pull requests and prints matches as a JSON array. The canonical "what reviews are waiting on me?" query.

- `--reviewer` — single value. Defaults to `@me`. In this release, only `@me` is accepted; passing anything else exits with code `3`. Named-identity resolution is tracked in issue #99.
- `--author` — single value. Same `@me`-only semantics as `--reviewer`. No default — omit it to list across all authors.
- `--state` — single value. One of `active | completed | abandoned | all`. Defaults to `active`. Diverges from `wi list`'s multi-value `--state` because Azure DevOps' `searchCriteria.status` only accepts one value; `all` is the escape hatch.
- `--repo` — single value. Filters by repository display name. Switches the request to the repo-scoped endpoint (`_apis/git/repositories/{name}/pullrequests`).
- `--include-drafts` — opt-in flag. Drafts are filtered out by default (a draft PR is not "waiting on review yet"). Filtering is applied client-side after the API call.
- `--limit N` — default `50`. Matches `wi list`.
- No positional query.

Sort order follows the Azure DevOps default (for `active`, that is creation date descending).

Example:
```bash
quill pr list
quill pr list --state completed --repo importer
quill pr list --author @me --include-drafts
quill pr list --limit 10
```

### View a pull request

```bash
quill pr view <id> [--with-threads] [--with-diff-stats]
```

Prints a single pull request as a JSON object. Use this when you need the description, linked work items, and (optionally) review threads or diff stats — all in one call.

The base output is a superset of the `pr list` per-row shape plus two extra always-present keys:

- `description` — markdown-converted PR description via the same pipeline as `wi view` / `wi comments`, so work-item URLs become `[Type: Title](#id)`. Empty PR description → `""`.
- `workItems` — array of work items linked to the PR. Shape matches `wi search` per-row (`{id, title, state, type, assignedTo, parentId}`). Always present; `[]` when none. Per-item failures emit error stubs mirroring `wi tree`: `{"id": 99999, "error": "unreadable"}` for individual 403/404 in the WI batch fetch, `{"id": 99999, "error": "batch-failed"}` for a sub-batch failure. A complete failure of the PR→work-items refs endpoint surfaces as exit code 2.

Flags:

- `--with-threads` — appends a top-level `threads` array with the same payload as `pr threads` (newest-first; system threads and deleted items filtered). Empty thread list → `[]`.
- `--with-diff-stats` — appends a top-level `diffStats` object with per-file added/removed counts and aggregate totals. **Not** the unified diff — for that, use `pr diff`. Object shape:
  - `totalFiles`, `totalAdded`, `totalRemoved` — always present (zero when the PR has no changes yet).
  - `files[]` — each entry has `path`, `changeType` (one of `add` / `edit` / `delete` / `rename`), `added`, `removed` always present.
  - `oldPath` — present **only** on `rename` entries (omitted otherwise, not `null`).
  - `binary: true` — present **only** on binary files (omitted otherwise). Binary files always report `added: 0, removed: 0`.

The flags are independent and can be combined. Flag-gated keys (`threads`, `diffStats`) are omitted from the output entirely when the flag isn't passed.

Example:
```bash
quill pr view 4711
quill pr view 4711 --with-threads
quill pr view 4711 --with-diff-stats
quill pr view 4711 --with-threads --with-diff-stats
```

### Read review threads on a pull request

```bash
quill pr threads <id> [--status <s>] [--limit N]
```

Prints a pull request's review threads as a JSON array, **newest first**. Deleted threads, deleted comments, and system-generated threads (vote updates, status updates, ref updates, policy events, etc. — anything ADO marks with `CodeReviewThreadType`) are excluded. Threads whose comments are all deleted drop out entirely. Comment text goes through the same markdown pipeline as `wi comments`, so work-item URLs become `[Type: Title](#id)`.

- `--status` — multi-value. Repeat to OR (`--status active --status pending`). Accepts ADO's status values (`active`, `fixed`, `wontFix`, `closed`, `pending`, `byDesign`). Omit to return all statuses. Status values are not validated; typos return an empty array.
- `--limit N` — cap the number of threads returned after sorting and filtering (must be `>= 1`). Omit to return all matching threads.

Each thread shape:

- `id`, `status` — pass-through of ADO's thread id and `CommentThreadStatus`.
- `filePath`, `side`, `startLine`, `endLine` — set together for file-scoped threads; all four `null` together for overall-PR threads. `side` is `"right"` for comments on new/modified code, `"left"` for comments on deleted code. `startLine == endLine` for single-line threads.
- `comments[]` — same shape as `wi comments` (`{id, author, createdDate, modifiedDate, text}`), in chronological ascending order (reading order).

Empty result returns `[]`. PR not found / 403 exits with code 2.

Example:
```bash
quill pr threads 4711
quill pr threads 4711 --status active
quill pr threads 4711 --status active --status pending --limit 20
```

## Markdown file format

```markdown
---
id: 12345
type: Product Backlog Item
title: The work item title
state: Active
parentId: 67890
---

Description content goes here in markdown.

Link to another work item: [Product Backlog Item: Other Item](#67890)
```

### Frontmatter fields

| Field | Description |
|-------|-------------|
| `id` | Work item ID (required for push, set to `0` for create, written on pull) |
| `type` | Work item type (required for create, read-only on pull) |
| `title` | Work item title (pushed to server, required for create) |
| `state` | Work item state (read-only, written on pull) |
| `parentId` | Parent work item ID (required for create, included on pull if parent exists) |

### Links to other work items

Use `[link text](#<work-item-id>)` syntax to link to other backlog items. On push:
- The link text is ignored and replaced with `{Type} {Id}: {Title}` from the server
- A "Related" relation is created between the work items
- The link becomes a clickable URL in Azure DevOps

On pull, links from Azure DevOps are converted back to `[{Type}: {Title}](#<id>)` format.

## Output

All output is JSON, designed for machine consumption.

### Success (push)
```json
{"id":12345,"title":"The title","updatedFields":["title","description"],"relationsAdded":[67890]}
```

### Success (pull)
```json
{"id":12345,"title":"The title","file":"./backlog/12345.md"}
```

### Success (view)
```json
{"id":12345,"type":"Product Backlog Item","title":"The title","state":"Active","assignedTo":"Jane Doe","parentId":67890,"description":"<markdown body>","relatedIds":[201,202]}
```

With `--with-children`, a `children` array is appended. With `--markdown`, the output is the same markdown document `pull` writes rather than JSON.

### Success (create / create-task)
```json
{"id":12345,"title":"The title"}
```

### Success (search)
```json
[{"id":201,"title":"Task one","state":"New","type":"Task","assignedTo":"Jane Doe","parentId":12345}]
```

### Success (comments)
```json
[{"id":9002,"author":"John Roe","createdDate":"2026-04-11T08:00:00Z","modifiedDate":null,"text":"Blocked on dependency."}]
```

`author` is `null` when the identity can't be resolved (e.g. deleted user). `modifiedDate` is `null` when the comment has never been edited; otherwise it's an ISO-8601 UTC timestamp.

### Success (pr list)
```json
[
  {
    "id": 4711,
    "title": "Fix retry policy in importer",
    "author": "Jane Doe",
    "state": "active",
    "isDraft": false,
    "repo": "importer",
    "url": "https://ado.example.com/DefaultCollection/MyProject/_git/importer/pullrequest/4711",
    "sourceBranch": "feat/retry",
    "targetBranch": "main",
    "createdDate": "2026-05-12T08:00:00Z",
    "closedDate": null,
    "reviewers": [
      {"displayName": "John Roe", "vote": 0, "isRequired": true},
      {"displayName": "Jane Doe", "vote": 10, "isRequired": false}
    ],
    "myVote": 0,
    "myIsRequired": true
  }
]
```

Vote values follow the Azure DevOps convention: `10` approved, `5` approved with suggestions, `0` no vote, `-5` waiting for author, `-10` rejected. `myVote` and `myIsRequired` are `null` when the authenticated user isn't on the reviewer list; `0` for `myVote` means the user is a reviewer who hasn't voted yet. `closedDate` is `null` for `active` PRs; ISO-8601 UTC otherwise.

### Success (pr view)
```json
{
  "id": 4711,
  "title": "Fix retry policy in importer",
  "author": "Jane Doe",
  "state": "active",
  "isDraft": false,
  "repo": "importer",
  "url": "https://ado.example.com/DefaultCollection/MyProject/_git/importer/pullrequest/4711",
  "sourceBranch": "feat/retry",
  "targetBranch": "main",
  "createdDate": "2026-05-12T08:00:00Z",
  "closedDate": null,
  "reviewers": [{"displayName": "John Roe", "vote": 0, "isRequired": true}],
  "myVote": 0,
  "myIsRequired": true,
  "description": "<markdown body>",
  "workItems": [
    {"id": 12345, "title": "Importer reliability", "state": "Active", "type": "Product Backlog Item", "assignedTo": "Jane Doe", "parentId": 999}
  ]
}
```

With `--with-threads`, a `threads` array (same shape as `pr threads`) is appended. With `--with-diff-stats`, a `diffStats` object is appended:

```json
{
  "diffStats": {
    "totalFiles": 4,
    "totalAdded": 142,
    "totalRemoved": 38,
    "files": [
      {"path": "src/Foo.cs", "changeType": "edit", "added": 12, "removed": 3},
      {"path": "src/Bar.cs", "changeType": "rename", "oldPath": "src/Baz.cs", "added": 0, "removed": 0},
      {"path": "src/Old.cs", "changeType": "delete", "added": 0, "removed": 48},
      {"path": "assets/logo.png", "changeType": "edit", "added": 0, "removed": 0, "binary": true}
    ]
  }
}
```

### Success (pr threads)
```json
[
  {
    "id": 88123,
    "status": "active",
    "filePath": "src/Importer/Retry.cs",
    "side": "right",
    "startLine": 42,
    "endLine": 42,
    "comments": [
      {"id": 1, "author": "John Roe", "createdDate": "2026-05-13T09:00:00Z", "modifiedDate": null, "text": "Consider exponential backoff here."}
    ]
  },
  {
    "id": 88200,
    "status": "active",
    "filePath": null,
    "side": null,
    "startLine": null,
    "endLine": null,
    "comments": [
      {"id": 9, "author": "Jane Doe", "createdDate": "2026-05-13T11:00:00Z", "modifiedDate": "2026-05-13T11:05:00Z", "text": "Overall, LGTM."}
    ]
  }
]
```

`status` is one of ADO's `CommentThreadStatus` values (`active`, `fixed`, `wontFix`, `closed`, `pending`, `byDesign`). `filePath`, `side`, `startLine`, `endLine` are either all set (file-scoped thread) or all `null` (overall-PR thread). Comments within a thread are sorted chronologically ascending; threads themselves are newest-first.

### Success (tree)
```json
{"id":1,"title":"Epic","type":"Epic","state":"Active","children":[{"id":10,"title":"Feature","type":"Feature","state":"Active","children":[{"id":100}]},{"id":11,"error":"unreadable"}]}
```

### Error
```json
{"error":"description of what went wrong","code":1}
```

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Validation failure (wrong state or assignee) |
| 2 | API error (network, auth, server) |
| 3 | Configuration or argument error |
