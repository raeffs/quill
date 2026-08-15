# Listing commands do not drop rows client-side

Azure DevOps caps a list response with `$top`. If quill then removes rows, a
`--limit N` call returns fewer than N rows while more matches wait on the server,
and the output says nothing about it. So quill emits every row the server returns
and lets the agent filter on the fields in the row. A filter quill cannot push
into the query is documentation, not a flag.

One exception stands: noise that carries no domain meaning. `pr threads` still
drops deleted comments and the threads Azure DevOps generates for itself. Quill
hides artefacts of the API. It does not hide domain state.
