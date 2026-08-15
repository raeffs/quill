---
name: quill
description: Azure DevOps Server CLI for coding agents. Use when the user asks about work items / backlog items / PBIs / bugs / tasks, or about pull requests — reading, searching, listing, creating, updating, pushing, walking the hierarchy, or reading comments and review threads.
---

# Quill — Azure DevOps Server CLI

Quill reads Azure DevOps Server work items and pull requests as JSON, and writes work items back. To edit a work item, pull it into a markdown file, change the file, and push the file back.

Quill needs the `quill` CLI on PATH. Install it once per machine:

```bash
dotnet tool install -g Raeffs.Quill
```

The install needs the .NET 10 SDK. On `linux-x64` and `win-x64` quill then runs as a native binary. Other platforms need the .NET 10 runtime.

## Finding the right command

Run `quill --help`, `quill wi --help`, or `quill wi <command> --help` for syntax, arguments, flags, and defaults. The help output is complete. Do not guess a flag, and do not expect this file to list one.

Use this table to pick the command. Run its help for the rest.

| Command | Use it when |
|---|---|
| `wi view` | You want to read one work item. Nothing is written to disk. |
| `wi pull` | You want to read one work item *and* edit it and push it back. Writes a file and overwrites without asking. |
| `wi push` | You want to send an edited file back to the server. |
| `wi create` | You want a new Product Backlog Item or Bug, described by a markdown file. |
| `wi create-task` | You want a new Task under a PBI or Bug. Title only, no file. |
| `wi list` | You want the answer to "what is assigned to me right now". |
| `wi search` | You want to find items anywhere in the project by title text or filter. |
| `wi tree` | You want to orient yourself under an Epic or Feature. Use `--depth 1` for direct children only. |
| `wi comments` | You want the discussion on a work item. |
| `pr list` | You want the answer to "what reviews are waiting on me". |
| `pr view` | You want one pull request with its description and its linked work items. |
| `pr threads` | You want the review comments on a pull request, with file and line. |

## Prerequisites

- `QUILL_PAT` holds a Personal Access Token. `quill wi` commands need **Work Items: Read & Write**. `quill pr` commands also need **Code: Read**.
- `.quill.json` sits in the current working directory, or in the user profile directory (`$USERPROFILE` on Windows, `$HOME` elsewhere) as a fallback.

```json
{
  "serverUrl": "https://your-server.com/tfs",
  "collection": "DefaultCollection",
  "project": "YourProject",
  "allowedStates": ["New", "Active"],
  "allowedParentStates": ["New", "Approved"]
}
```

All fields are required. `allowedStates` controls which work items you can push. `allowedParentStates` controls which parent states accept a new PBI or task. Quill validates the assignee against the PAT user.

Do not check for the config file before you run a command, and do not ask the user for its values. Quill resolves the config itself and reports a clear error when the file is missing from both locations.

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

| Field | Meaning |
|-------|---------|
| `id` | Work item ID. Required for push. Set it to `0` for create. Written on pull. |
| `type` | Work item type. Required for create. Read-only on pull. |
| `title` | Work item title. Pushed to the server. Required for create. |
| `state` | Work item state. Read-only. Written on pull. |
| `parentId` | Parent work item ID. Required for create. Written on pull when a parent exists. |

Write `[link text](#<work-item-id>)` to link to another backlog item. On push, quill replaces the link text with `{Type} {Id}: {Title}` from the server, creates a `Related` relation, and turns the link into a URL. On pull, quill converts links back to `[{Type}: {Title}](#<id>)`.

## Gotchas

These are the things the help output and a successful run do not tell you.

- `wi view --markdown` and `wi view --with-children` are mutually exclusive. Passing both exits with code 3.
- `wi search` with no query and no filter exits with code 3. Supply a query or at least one filter.
- `wi push` succeeds only when the work item's state is in `allowedStates` and the item is assigned to the authenticated user.
- `wi create` needs frontmatter with `id: 0`, a `title`, a `type` of `Product Backlog Item` or `Bug`, and a `parentId`. The parent must exist, be assigned to the authenticated user, and be in an allowed state. On success quill rewrites the file with the server-assigned ID.
- `wi create-task` applies the same parent rules.
- Quill does not validate state and type filter values against `.quill.json`. A typo returns `[]` rather than an error.
- `wi tree` and the `workItems` array of `pr view` emit error stubs instead of failing the whole call: `{"id": 99999, "error": "unreadable"}` for a 403 or 404 on one item, `{"id": 99999, "error": "batch-failed"}` when a sub-batch fails.
- In `wi tree`, a bare `{"id": 99999}` node means the child was clipped by `--depth`, not that it has no children. A fetched node with no children has `"children": []`.
- `wi search` silently omits items that fail to load. Search is a discovery list, not a structural view.
- `wi tree` walks parent-child relations only. It does not follow `Related` links.
- In `pr view --with-diff-stats`, `oldPath` appears only on `rename` entries and `binary: true` appears only on binary files. Both keys are absent otherwise, not `null`. Binary files always report `added: 0, removed: 0`.
- Reviewer vote values follow the Azure DevOps convention: `10` approved, `5` approved with suggestions, `0` no vote, `-5` waiting for author, `-10` rejected. In `pr list` and `pr view`, `myVote` and `myIsRequired` are `null` when the authenticated user is not a reviewer.
- `author` is `null` on a comment whose identity no longer resolves, for example a deleted user. `modifiedDate` is `null` until someone edits the comment.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Validation failure (wrong state or assignee) |
| 2 | API error (network, auth, server) |
| 3 | Configuration or argument error |
