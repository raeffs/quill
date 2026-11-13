# A failure mode names what the agent does next

A command fails in one of five failure modes. Each has its own exit code, and
each tells the agent to do one thing.

| Code | Failure mode | The agent |
|------|--------------|-----------|
| 1 | Rejected | changes what it asks for |
| 2 | Unavailable | tries the same command again |
| 3 | Usage | fixes the command, the config or the file |
| 4 | Defect | stops and suggests a bug report |
| 5 | Denied | stops and tells the user to check the PAT |

A failure mode earns a code only when it leads to a different action. Quill had
three codes. Defect got the fourth because no retry and no change to the command
fixes a case quill does not handle. Denied got the fifth because a retry never
fixes a refused PAT, and an agent that retries on exit 2 would retry it until it
gave up. A sixth mode needs a sixth action.

One test separates Usage from Rejected: can quill tell that the input is wrong
without sending a request? A tag that holds a `;` is Usage. A person flag that
matches nobody is Rejected, because only the server knows who exists.

The HTTP status decides the mode of a failed request, for a read and a write
alike:

| Status | Failure mode |
|--------|--------------|
| 400, 404, 409, 412, 422 | Rejected |
| 408, 429, 5xx, no response, timeout | Unavailable |
| 401, 403 | Denied |
| any other | Defect |

An answer quill cannot read is a Defect, and so is every exception that no
module gave a mode. The frame can therefore print an error row for every
failure after a successful parse.

The error row keeps two keys, `error` and `code`. We considered a marker key on
the row in place of a new code. The agent would then read two things to choose
one action, and a loop that watches only the exit code would never see the
marker.

The status cannot tell every case apart, and that is the price. The server
answers 404 for a work item that does not exist and for a project the config
names wrongly, so a wrong project reports as Rejected and not as Usage. It
answers 403 for a PAT that lacks a scope and for one item the PAT may not
touch, so both report as Denied. The server's message names what it refused,
and the error row carries that message. A request that needs another mode than
the table gives states so itself: Azure DevOps answers 500 when a run cannot
change state, and quill reports that as Rejected.

Unavailable does not tell whether a write took effect. After a timeout, a lost
connection or a 5xx, the server may already have created the work item, the
pull request or the run, and a second try makes a second one. The agent retries
a read as it is, but it retries a write only after it checks that the write did
not happen.

Unavailable does not promise that a second try succeeds either. A refused
connection, a wrong host name, a TLS failure and a 500 for a lasting fault all
report as Unavailable. When the same failure comes back, the agent stops and
tells the user.
