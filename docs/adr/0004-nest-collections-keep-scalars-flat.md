# Nest collections, keep scalars flat

Quill nests a key only when it holds a collection or a set of counts:
`reviewers`, `threads`, `workItems`, `votes`, `diffStats`. Related scalars stay
flat, side by side. `myVote` and `myIsRequired` are siblings rather than a `me`
object. The merge fields sit next to `mergeStatus` rather than inside a `merge`
object.

A grouping object has to answer for its own absence, and both answers are bad.
If it disappears when it holds nothing, a read of `.merge.failureType` breaks
instead of returning `null`. If it is always there, it is usually an object of
nulls and the nesting buys nothing. A flat key with a `null` value never breaks
a path.
