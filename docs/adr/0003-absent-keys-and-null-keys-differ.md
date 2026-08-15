# Absent keys and null keys mean different things

Quill's JSON uses both, and the difference tells the agent something. A key is
absent when quill declined to look: an unset flag such as `--with-threads`, or a
case that never arose, such as `oldPath` on a file nobody renamed. A key is
`null` when quill looked and found no value: `myVote` on a pull request you do
not review, `mergeFailureType` on a merge that did not fail.
