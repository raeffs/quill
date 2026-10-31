# A dependency lives only on the successor's file

Azure DevOps stores a dependency on both work items: the successor shows a
Predecessor link, the predecessor a Successor link. The backlog file carries
only `predecessors`, written on the item that waits. There is no `successors`
key.

`predecessors` is a round-trip key, so push sets the exact set of links the file
lists, removals included (ADR 0008). If both directions were keys, one link would
live in two files. Dropping 123 from A's `predecessors` while B's stale file
still lists A under `successors` would leave the two files disagreeing, and the
last push would win. With one key, each link has exactly one file that owns it.
Predecessors are also the direction an agent acts on: what blocks the item it is
about to work on.

The price is that pulling a predecessor does not show what it blocks. A
read-only view of successors can come later without breaking this rule.
