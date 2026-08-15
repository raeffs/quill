# Flags gate requests, not fields

`--with-threads` and `--with-diff-stats` each cost another call to Azure DevOps.
The flag gives the agent control over that cost. That is the only job a flag
has. Quill always emits a field that already sits in a response it reads,
however long the row grows.

Gating a free field buys nothing and charges twice. The agent has to know the
flag exists, and a field it forgot to ask for looks exactly like a field quill
does not have.
