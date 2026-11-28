# Quill remembers a person for a day

Quill saves each person it resolves in a file in the user's cache directory and
uses the file for 24 hours. It saves a name only when the name matched exactly
one person. It saves the current user under a hash of the PAT. This removes the
identity request from most runs, and four commands sent that request on every
run.

That has a price, and it bends ADR 0012. When a second person with the same
display name joins, the server answers "matches 2 people", but for up to 24
hours quill answers with the first person. Quill has then picked one person for
the agent. We accept this because agents type the display names that quill's
rows show them, and a cache that saved only unique names would stay almost
empty.

The age is fixed from the write, so the window always closes. A read does not
extend it, and no config key changes it.

We refused four things:

- To save a name that matched nobody. A person who joins later would stay
  unknown, and the agent could not tell.
- To delete an entry after a rejected command. Quill would have to know which
  refusals mean that a person is gone. The 24 hours cover that case.
- A flag, an environment variable or a command to go around the cache. A stale
  entry looks correct, so an agent has no signal to use one. A human deletes
  the file.
- To fail or warn when the file is broken. The cache only saves requests, so a
  broken cache saves none and the command works as before.
