---
name: quill
description: Azure DevOps Server CLI for coding agents. Use when the user asks about work items / backlog items / PBIs / bugs / tasks, about pull requests, or about pipeline runs — reading, listing, creating, updating, pushing, walking the hierarchy, or reading comments, review threads and revisions.
---

# Quill — Azure DevOps Server CLI

Quill reads Azure DevOps Server work items, pull requests and pipeline runs as JSON, writes work items back, and opens pull requests. To edit a work item, pull it into a markdown file, change the file, and push the file back. Work item commands come in two groups: `backlog` keeps the backlog as markdown files that you pull, edit and push, and `wi` reads and creates work items on demand while you implement.

Quill needs the `quill` CLI on PATH, a Personal Access Token in `QUILL_PAT`, and a `.quill.json` config file. Do not check for any of them before you run a command, and do not ask the user for config values. Quill resolves them itself and reports a clear error when one is missing. Read [references/prerequisites.md](references/prerequisites.md) only when a command fails with such an error: it covers the install, the PAT scope each command needs, and the config file.

## Finding the right command

Run `quill --help`, `quill backlog --help`, `quill wi --help`, `quill pr --help`, `quill pipeline --help`, or `quill <group> <command> --help` for syntax, arguments, flags, and defaults. The help output is complete. Do not guess a flag, and do not expect this file to list one.

Pick the group first. Use `backlog` when you maintain the backlog as files: pull an item, edit it, push it back, or create one from a file. Use `wi` when you implement an item and want to read it, find related items, or create one without a file. Use `pr` for pull requests. Use `pipeline` for pipeline runs. Then pick the command from the group's table, and run its help for the rest.

### backlog

| Command | Use it when |
|---|---|
| `backlog pull` | You want to read one work item *and* edit it and push it back. Writes a file and overwrites without asking. |
| `backlog push` | You want to send an edited file back to the server. |
| `backlog create` | You want a new Feature, Product Backlog Item, Bug or Task, described by a markdown file. |

`backlog pull` writes a markdown file with YAML frontmatter, and `backlog push` and `backlog create` read it. Read [references/backlog-file-format.md](references/backlog-file-format.md) before you edit or write such a file. It lists the frontmatter fields, how `tags` and `predecessors` behave on push and create, and how to link to another work item.

### wi

| Command | Use it when |
|---|---|
| `wi view` | You want to read one work item. Nothing is written to disk. |
| `wi list` | You want to find work items by title text, assignee, state or type. For "what is assigned to me", pass `--assignee @me`. |
| `wi tree` | You want to orient yourself under an Epic or Feature. Use `--depth 1` for direct children only. |
| `wi comments` | You want the discussion on a work item. |
| `wi create` | You want a new Feature, Product Backlog Item, Bug or Task under a parent, without a file. |

### pr

| Command | Use it when |
|---|---|
| `pr list` | You want to find pull requests — by branch, by author, or the ones waiting on you (`--reviewer @me`). |
| `pr view` | You want one pull request with its description and its linked work items. |
| `pr threads` | You want the review comments on a pull request, with the file and the line the code sits on now. |
| `pr revisions` | You want to know what changed since you last reviewed. Lists one row per push, with the commits git needs to diff them. |
| `pr create` | You finished a branch and want a pull request open on it. Opens a draft. |

### pipeline

| Command | Use it when |
|---|---|
| `pipeline list` | You want a pipeline's ID. Filter by `--name` or `--folder`. |
| `pipeline runs` | You want a run's ID: the newest runs of a pipeline, a branch, or the failed ones. |
| `pipeline view` | You want one run. Add `--with-stages` for its stages, jobs, tasks and the issues each task reported. |
| `pipeline log` | You want the log of a job or task. Take `logId` from `pipeline view --with-stages`. Add `--start-line` and `--end-line` for a range, or `--tail` for the last lines. |

## Gotchas

These are the things the help output and a successful run do not tell you.

- `backlog push` succeeds only when the work item's state is in `allowedStates` and the item is assigned to the authenticated user.
- `backlog create` needs frontmatter with `id: 0`, a `title`, a `type` of `Feature`, `Product Backlog Item`, `Bug` or `Task`, and a `parentId`. The parent must exist and be assigned to the authenticated user, and its type must fit the hierarchy: a Feature needs an Epic parent, a Product Backlog Item or Bug needs a Feature parent, and a Task needs a Product Backlog Item or Bug parent. The parent's state does not matter. On success quill rewrites the file with the server-assigned ID.
- `wi create` checks only the parent's type against the same hierarchy. The parent may be assigned to anyone and be in any state.
- `wi create` leaves the new item unassigned unless you pass `--assignee`. `backlog create` assigns it to the authenticated user.
- `wi create` takes a repeatable `--tag`, one value per flag. A tag must not contain `;` or `,` or be empty, and a bad value exits 3 before any request.
- `wi create` takes a repeatable `--predecessor`, one ID per flag. A value that is not a positive integer, or a repeated ID, exits 3 before any request. The server rejects missing targets and cycles, which exits 1 and creates nothing.
- `backlog create` and `wi create` give the new item its parent's area and iteration.
- A bare `wi list` exits with code 3. Pass at least one of `--title`, `--assignee`, `--state` or `--type`. There is no implicit `@me`.
- `wi list` has no `--skip`, unlike `pr list`. When you get exactly `--limit` rows, widen `--limit` or narrow the filter.
- `pipeline list` and `pipeline runs` have no `--skip`, unlike `pr list`. When you get exactly `--limit` rows, widen `--limit` or narrow the filter.
- Quill never reads the working copy, so no `--branch` means "the pipeline's default branch" on `pipeline trigger` and "every branch" on `pipeline runs`. Run git yourself: `--branch "$(git rev-parse --abbrev-ref HEAD)"` in bash, `--branch (git rev-parse --abbrev-ref HEAD)` in PowerShell.
- To diagnose a failure, run `pipeline view <runId> --with-stages`, select the tasks with `.stages[].jobs[].tasks[] | select(.result == "failed")`, and read their `issues` first. Each issue's `line` is the `##[error]` line in that task's log; the cause sits above it. Then open `pipeline log <runId> <logId> --start-line <line minus some lines> --end-line <line>`.
- `pipeline log` counts lines from 1, and both ends of a range are inclusive. `startLine` and `endLine` are the numbers of the first and last line printed, so `lines[i]` is line `startLine + i`. A range past the end prints `lines: []` with both keys `null`. `--tail` sends one extra request.
- `issues` lists the first ten errors and the first ten warnings of a task. `errorCount` and `warningCount` count them all.
- A pipeline without a `stages:` block has one stage named `__default`.
- A job with `tasks: []` did not run. Its `result` says why: `skipped` for a condition, `failed` for an error before it started, for example a template expansion error. A failed one can have a `logId` that holds the error. On a run in progress, `result: null` means the job has not started yet.
- A job has its own `issues`. A canceled, timed-out or lost job reports there, not on a task. When a job failed and none of its tasks did, read the job's `issues`.
- The last stage can have `name: null`. It holds the job the server runs after all stages, for example `Finalize build`. You cannot skip it with `pipeline trigger --skip-stage`.
- `wi list` does not validate `--state` and `--type` values against `.quill.json`. A typo returns `[]` rather than an error.
- `wi list` silently omits items that fail to load. It is a discovery list, not a structural view.
- `wi view` and `wi create` always print `tags` as an array, `[]` when the item has none. `wi list` rows do not carry `tags`, so call `wi view` for them.
- `wi view` and `wi create` always print `predecessors` as an array of IDs, `[]` when the item has none. `wi list` rows do not carry `predecessors`, so call `wi view` for them.
- `wi tree` and the `workItems` array of `pr view` emit error stubs instead of failing the whole call: `{"id": 99999, "error": "unreadable"}` for a 403 or 404 on one item, `{"id": 99999, "error": "batch-failed"}` when a sub-batch fails.
- In `wi tree`, a bare `{"id": 99999}` node means the child was clipped by `--depth`, not that it has no children. A fetched node with no children has `"children": []`.
- `wi tree` walks parent-child relations only. It does not follow `Related` links.
- `pr list` and `pr view` name votes: `approved`, `approvedWithSuggestions`, `waitingForAuthor`, `rejected`, `noVote`. `myVote` and every `reviewers[].vote` of `pr view` carry one of those names. `myVote` is `null` when the authenticated user is not a reviewer — a different fact from `noVote`. `myIsRequired` is `null` in the same case.
- The `votes` counts fold `approvedWithSuggestions` into `approved`, because the suggestions live in the threads. They skip container reviewers — a group attached as a reviewer casts no vote of its own, and counting it would inflate `noVote`.
- `lastMergeSourceCommit` and `lastMergeTargetCommit` on `pr view` are the two commits of the last merge attempt. They are not merge bases. To diff the pull request in a clone, use three dots: `git diff <lastMergeTargetCommit>...<lastMergeSourceCommit>`. A two-dot diff includes commits the pull request does not contain.
- Both commits go stale. The server repeats the merge attempt each time either branch moves, so a push after your last `pr view` leaves you holding an old pair. Call `pr view` again rather than diff the wrong pair. Both keys are `null` before the first merge attempt.
- `pr list` returns draft pull requests, marked `isDraft`. No flag excludes them. Filter on the key.
- `pr list` has no default `--reviewer`. A bare call returns every active pull request in the project.
- `pr list` returns a bare JSON array with no `hasMore`. Fewer rows than `--limit` means you reached the end. Exactly `--limit` rows means ask again with a larger `--skip`.
- A `--skip` walk is not a snapshot. The list is live and newest-first, so a pull request created part-way through shifts the rest down a place: you can be served one row twice, or miss one. Widen `--limit` instead when you need a stable set.
- Quill never reads the working copy, so no flag means "the branch I am on". Run git yourself: `--source-branch "$(git rev-parse --abbrev-ref HEAD)"` in bash, `--source-branch (git rev-parse --abbrev-ref HEAD)` in PowerShell.
- The list response truncates every description to 400 characters, so `pr list` omits the key entirely. Call `pr view` for the description.
- `labels` holds active label names only, and is `[]` when a pull request carries none. `mergeStatus` is the Azure DevOps status verbatim, `null` when the server sends none.
- `author` is `null` on a comment whose identity no longer resolves, for example a deleted user. `modifiedDate` is `null` until someone edits the comment.
- `startLine` and `endLine` of `pr threads` name the line at the head of the source branch, not the line the reviewer pointed at. `origStartLine`, `origEndLine`, `origStartColumn` and `origEndColumn` name the reviewer's own anchor. Compare the two to see whether the code moved.
- `positionState` says how far to trust `startLine`. Under `current` the reviewer commented on the latest revision. Under `tracked` Azure DevOps followed the code and found it. Under `deleted` the code is gone, and `startLine` marks where it was. Under `unverified` Azure DevOps tracked nothing, so open the file before you act. All four keys are `null` on a thread with no file.
- `origFilePath` appears only when the file was renamed after the reviewer commented. It is absent otherwise, not `null`.
- The columns belong to the original position alone. Azure DevOps drops the character range when it re-tracks an anchor, so a tracked thread reports `origEndColumn: null`.
- Quill does not read the file to check a line it prints. On `unverified` it reports the stale line rather than no line. The Azure DevOps web UI shows the same stale line, so comparing the two proves nothing.
- `pr revisions` lists one row per push, newest first. It takes no flags. Every row carries the three commits git needs, because no quill command follows this one.
- `commonCommit` is the merge base and the only correct diff base for a revision. `targetCommit` is the target branch head at that moment, and the two differ on most revisions. Diffing against `targetCommit` shows you commits the author never wrote.
- To compare two revisions, always use `range-diff`, never a plain diff between the two `sourceCommit` values. Compare each revision against its own base:

  ```bash
  git range-diff <commonCommit of older>..<sourceCommit of older> <commonCommit of newer>..<sourceCommit of newer>
  ```

- An older `sourceCommit` may be missing from your clone. A force push leaves it unreachable from every remote ref, so a plain `git fetch` skips it and git reports `fatal: bad object`. Fetch it by SHA: `git fetch origin <sourceCommit>`. The server serves it.
- To answer "what changed since I last reviewed", read `pr threads` for the date of your last comment. Take the newest `pr revisions` row created before that date, and `range-diff` it against the newest row of all. Comparing only the rows created after your comment skips the revision you actually read.
- `pr create` always opens a draft. There is no `--draft` and no `--publish`. A draft runs no build validation, accepts no votes, and gains no reviewer a branch policy would require. Nothing starts and nobody hears about it until a human publishes it in the web UI.
- `pr create` takes flags, not a file — except `--description-file`, which takes a path or `-` for stdin. Unlike `backlog create`, there is no frontmatter file.
- The server caps a description at 4000 characters and rejects a longer one with `code: 2`. It does not truncate. Quill passes the server's message through, so read the `error` string before you retry.
- `--target-branch` is optional. Without it quill reads the repository and uses its default branch, which costs one extra request. Pass the flag to skip it.
- The output is the `pr view` row without `workItems` and `threads`. Both keys are absent, not `null` — quill did not look, because each costs another request. Call `pr view` for them.
- `mergeStatus` is `queued` on every `pr create`. The server has not finished the merge attempt yet, and `lastMergeSourceCommit` and `lastMergeTargetCommit` belong to that unfinished attempt. Call `pr view` for a settled merge status.
- `myVote` and `myIsRequired` are `null` on `pr create`. You are the author, so you are not a reviewer, and a draft gains no policy-required reviewer.
- A pull request already open on the same branch pair fails with `TF401179` and a non-zero exit. Quill does not check first and does not return the existing one. Recover with `pr list --source-branch <branch>`.
- `pr create` has no confirmation flag and no allow-list. The PAT scope is the only boundary. Opening a draft is cheap to undo.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Validation failure (wrong state or assignee), or the server rejected a work item write |
| 2 | API error (network, auth, server) |
| 3 | Configuration or argument error |
