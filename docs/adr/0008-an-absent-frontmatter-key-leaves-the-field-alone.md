# An absent frontmatter key leaves the field alone

`backlog push` sends a round-trip key only when the file carries it. A file with no
`tags` key leaves the work item's tags as they are. A file with `tags: []`
removes them all. Pull always writes the key, so a freshly pulled file states
the value outright.

Files outlive quill versions. A file pulled before a key existed has no line for
it, and reading that silence as "empty" would make the next push wipe a field
the agent never touched. ADR 0003 draws the same line for output: absent means
quill was told nothing, an empty value is a value.

The price is two ways to write "no tags" on create, where absent and `[]` both
mean none. Every future round-trip key follows this rule, not only `tags`.
