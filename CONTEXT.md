# Quill

Quill reads Azure DevOps Server work items and pull requests as JSON, and writes
work items back. A coding agent reads that JSON. The agent first chooses what to
look at, then acts on it, and quill's language follows that split.

## Language

**Triage row**:
What a listing command emits per result. It holds only what an agent needs to
choose which item to open next.
_Avoid_: list row, summary, preview

**Detail row**:
What a single-item command emits. It repeats every key of the triage row
unchanged. It adds a key only when that key changes what the agent does next. It
is not a copy of the API response.
_Avoid_: full object, view shape

## Merging

**Merge attempt**:
Azure DevOps merges the source branch into the target branch to find out whether
it can. It repeats the attempt each time either branch moves. It reports the
outcome and the two commits it used.
_Avoid_: trial merge, merge preview

## Pull request review

**Vote**:
A reviewer's verdict on a pull request. It is one of five: approved, approved
with suggestions, waiting for the author, rejected, or no vote. Quill names
these; Azure DevOps numbers them.
_Avoid_: score, rating

**Waiting for the author**:
A reviewer has read the pull request and wants changes. Under *no vote* the
reviewer has not read it yet.
_Avoid_: waiting, pending, needs work

**Container reviewer**:
A group attached to a pull request as a reviewer. A group casts no vote of its
own — its members vote as people — so quill counts the members, not the group.
_Avoid_: team reviewer, group vote
