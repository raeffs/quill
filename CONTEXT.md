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

**Action**:
What a command does between reading its flags and printing its rows: the
requests it sends to the server and the rules it applies to the answers. The
command parses flags into a request, hands it to the action, and prints what
comes back. Every command has exactly one action.
_Avoid_: operation, handler, use case, logic

**Rich text**:
The HTML Azure DevOps stores in a description or a comment. Quill hands it to
the agent as markdown and turns markdown back into it, so a linked work item
names its type and title in both.
_Avoid_: HTML field, body, content

## People

**Person**:
A human the server knows, never a group. An agent names a person by
`@me`, a display name, a unique name or an email address, and the name must
match exactly one person.
_Avoid_: user, identity, account

**Unique name**:
The name that tells one person from every other, such as `CONTOSO\jdoe`. Two
people can share a display name, but never a unique name.
_Avoid_: account name, alias, login

## Backlog

**Backlog**:
The work items an agent maintains as markdown files, pulled from the server and
pushed back. Not the Azure DevOps board view of the same name.
_Avoid_: sync, workspace, local items

**Predecessor**:
A work item that must be done before this one can start. Quill records the link
only on the item that waits.
_Avoid_: blocker, dependency

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
A reviewer's judgement on a pull request. It is one of five: approved, approved
with suggestions, waiting for the author, rejected, or no vote. Quill names
these; Azure DevOps numbers them.
_Avoid_: score, rating, verdict

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

**Check**:
A policy evaluation that can gate the merge of a pull request. Quill says
*check*; the server and the adapter say *policy evaluation*.
_Avoid_: policy, status, CI run

**Verdict**:
Quill's judgement on whether a pull request can merge. It is one of blocked
(someone must act), waiting (nothing to do but wait), ready, or none when the
pull request is not active.
_Avoid_: state, result

## Pipelines

**Pipeline**:
A YAML pipeline in the configured project. Quill names it by its integer ID.
_Avoid_: definition, build definition

**Run**:
One execution of a pipeline. Quill names it by its integer ID, which is unique
across the project.
_Avoid_: build, build number

**Stage**:
A named part of a run that groups jobs. A run has at least one stage, and a
stage fails when one of its jobs fails. A stage has no log of its own.
_Avoid_: phase, environment

**Job**:
A part of a stage that runs on one agent. Under a matrix each entry is its own
job, and the job's name carries the entry. The server keeps a grouping record
above the jobs of one YAML job; quill does not show it, unless it failed before
any job under it started. Then quill shows it as the job.
_Avoid_: phase, agent job, matrix entry

**Task**:
One step of a job. It is the smallest part of a run that has a log, and the
only part that reports issues.
_Avoid_: step, timeline record

**Issue**:
An error or warning a task reported while it ran. It carries the message and
the log line where the message stands; the cause sits above that line. A task
lists the first ten of each kind and counts them all.
_Avoid_: annotation, problem, finding, error

**Parameter**:
A value the pipeline's YAML declares and a run receives when an agent triggers
it. Quill passes it as a string and the pipeline types it.
_Avoid_: template parameter, variable, input

**Log**:
The text a job or task wrote while it ran. A job's log holds the whole job;
a task's log holds that task's section of it. Each log belongs to one run, and
quill names it by its integer ID within that run. A stage has no log.
_Avoid_: output, console, build log

**Attempt**:
One pass through the jobs of a run. A run starts with one attempt, and a retry
adds another to the same run for the jobs that failed. Quill names the run,
never the attempt.
_Avoid_: rerun, re-run, retry count

**Test run**:
The batch of test results one task published to a run. A run has none, one
or several, and quill reads them through the run. It is never a bare *run*.
_Avoid_: run, suite, test batch

**Test result**:
What one test did within a test run. It is never a bare *result*, which is
what a run concluded.
_Avoid_: result, test case, test

**Outcome**:
What a test result concluded. Azure DevOps names it: passed, failed, not
executed, and others. A run has a result; a test result has an outcome.
_Avoid_: result, status, verdict
