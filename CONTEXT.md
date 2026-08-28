# Quill

Quill reads Azure DevOps Server work items and pull requests as JSON, and writes
work items back. A coding agent reads that JSON. The agent first chooses what to
look at, then acts on it, and quill's language follows that split.

## Language

**Triage row**:
What a command emits while the agent is still choosing. It holds only what an
agent needs to pick which item to open next.
_Avoid_: list row, summary, preview

**Detail row**:
What a command emits once the agent has chosen. It repeats every key of the
triage row unchanged. It adds a key only when that key changes what the agent
does next. It is not a copy of the API response. Row count decides nothing: a
command that returns many rows emits detail rows when no command follows it.
_Avoid_: full object, view shape

## Merging

**Merge attempt**:
Azure DevOps merges the source branch into the target branch to find out whether
it can. It repeats the attempt each time either branch moves. It reports the
outcome and the two commits it used.
_Avoid_: trial merge, merge preview

## Pull request review

**Revision**:
One recorded state of a pull request's source branch. Opening the pull request
makes one, and so does every push, rebase or retarget after it.
_Avoid_: iteration, update, push

**Review thread**:
A conversation anchored to code. It has a file, usually a line range, and
comments people wrote.
_Avoid_: comment thread, discussion

**System thread**:
What Azure DevOps records when something happens to a pull request — a push, a
vote, a reviewer change. It has the shape of a review thread and is not one.
No author wrote it, the server localises its text, and its content sits in
properties. Quill reads the properties and never the text.
_Avoid_: auto thread, generated comment

**Anchor**:
Where a review thread points in the code. It names a file, and on most threads a
line range. A thread with no line range points at the whole file.
_Avoid_: location, pin, marker

**Original position**:
The anchor as the reviewer left it. It belongs to the revision the reviewer
commented on, and the file has often changed since.
_Avoid_: as posted, old line

**Current position**:
Where the anchored code sits at the head of the source branch. Azure DevOps
tracks the code from the original position, and cannot always do so.
_Avoid_: new line, tracked position, latest position

**Position state**:
How far an agent can trust a current position. It is one of four: current,
tracked, deleted, or unverified. Under *current* the reviewer commented on the
latest revision, so nothing has moved. Under *tracked* Azure DevOps followed
the code and found it. Under *deleted* Azure DevOps followed the code and found
it gone, and the current position marks where it was. Under *unverified* Azure
DevOps tracked nothing, and the current position repeats the original one.
_Avoid_: confidence, accuracy, tracking status

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

## Draft and publish

**Draft**:
A pull request Azure DevOps holds back from review. It runs no build validation,
accepts no votes, and gains no reviewer a branch policy would otherwise require.
Only a reviewer someone names on it hears about it.
_Avoid_: WIP, work in progress, unpublished

**Publish**:
The act that ends a draft. It assigns the reviewers policy requires, evaluates
the policies, and opens voting. A pull request cannot return to draft without
discarding every vote it has collected.
_Avoid_: undraft, open, ready for review
