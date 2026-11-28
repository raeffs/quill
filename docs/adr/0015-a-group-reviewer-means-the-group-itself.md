# A group reviewer means the group itself

`pr list --reviewer` accepts a group as well as a person. It then returns the
pull requests where that group is attached as a reviewer. It does not return a
pull request where only a member of the group is attached.

The server works this way: `searchCriteria.reviewerId` matches the identity
attached, and it does not expand a group. To add the members, quill would send
one query for each member and merge the rows. Each list is capped, so quill
could not build a correct page from them, and `--limit` and `--skip` would lie.
ADR 0001 refuses that for the same reason.

That has a price. An empty result for a team does not mean that nothing waits
on its members. The skill doc says so, and the agent asks for each person when
it needs that answer.

The rule of ADR 0012 holds for a group. The name must match exactly one
identity. When it matches several groups, or a person and a group, the command
exits 1 and lists the candidates. Only `--reviewer` accepts a group, because a
group writes no pull request and no work item is assigned to one.
