# Quill resolves a person before it sends one

Every flag that names a person goes through one resolver: `pr list --reviewer`
and `--author`, `wi list --assignee`, `wi create --assignee` and
`backlog create --assignee`. The resolver accepts `@me`, a display name, a
unique name or an email address. The name must match exactly one person.
When it matches nobody, or several people, the command exits 1 and lists the
candidates. Quill never picks one person for the agent.

The work item flags could pass the name to the server unchanged, because WIQL
and `System.AssignedTo` accept a display name. A write then fails on a name it
cannot match, but a query does not. It returns no rows, and the agent reads
that as "nothing assigned". Pull request search accepts only an id, so quill
must resolve the name there. Two commands that handle a person two ways would
fail in two ways, and the skill doc would have to teach both.

That has a price. A command that names a person other than `@me` sends one
more request, and the PAT needs Identity: Read.
