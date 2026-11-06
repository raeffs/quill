# Backlog file format

Read this file before you edit a file that `backlog pull` wrote, or write a file for `backlog push` or `backlog create`. No other command reads or emits this format.

```markdown
---
id: 12345
type: Product Backlog Item
title: The work item title
state: Active
parentId: 67890
tags: [backend, tech-debt]
predecessors: [67891]
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
| `tags` | Tags, as a YAML list. Round-trip: written on pull, sent on push and create. On push a list replaces all tags, `[]` removes them all, and no key leaves them alone. On create no key and `[]` both mean no tags. A tag must not contain `;` or `,`; a bad value exits 3. |
| `predecessors` | IDs of the work items that must be done before this one, as a YAML list. Written only on the item that waits; there is no `successors` key. Round-trip: written on pull, sent on push and create. On push a list sets the exact set of Predecessor links, `[]` removes them all, and no key leaves them alone. On create no key and `[]` both mean none. A value that is not a positive integer, or a repeated ID, exits 3. The server rejects missing targets and cycles, which exits 1. |

Write `[link text](#<work-item-id>)` to link to another backlog item. On push, quill replaces the link text with `{Type} {Id}: {Title}` from the server, creates a `Related` relation, and turns the link into a URL. On pull, quill converts links back to `[{Type}: {Title}](#<id>)`.

