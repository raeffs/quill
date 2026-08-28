# Write commands take flags, not frontmatter files

`wi pull` writes a markdown file with frontmatter, `wi push` reads it back, and
`id` binds the two. The frontmatter is there because the file round-trips.
`pr create` has no round trip: there is no `pr pull` and no `pr push`, so a
frontmatter file would be written once and never read again. The pull request
write commands therefore take a flag for every scalar — repository, branches,
title, work item IDs — and a file for the body alone, through
`--description-file`.

The agent already holds every scalar. It reads the branch from git, as ADR 0002
requires, and it holds the work item IDs from the task it was given. Writing
those values into a file and passing the path duplicates them with no reader on
the other end. The description is the one input that is genuinely file-shaped:
markdown with newlines, quotes and backticks, which a command line mangles.

The price is two input shapes in one CLI. An agent that learned `wi create`
takes a file has to learn that `pr create` does not, and the skill doc has to
say so.
