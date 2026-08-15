# Quill does not diff

Azure DevOps returns no unified diff. Quill would have to build any diff it
emitted, in text or in counts: read the changed paths, ask a second endpoint for
the line blocks, then guess from the extension which files are binary. Git
builds the same file list and the same counts from the two commits `pr view`
already emits, and it builds them better. So quill emits the commits of the merge
attempt and stops there.

This is the other side of ADR 0002. Quill never assumes a checkout, and it never
supplies what a checkout supplies. Its surface is the server API. Git is the
agent's business.

An agent with no clone loses the file and line counts, and quill offers nothing
in their place. That is the price. The clone takes less time than the three
requests it replaces.
