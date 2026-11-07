# An action in Core carries what a command does

Every command has one action in `Quill.Core`. The action takes the client
interfaces it needs and a request, sends the requests, applies the rules to the
answers, and returns the rows. The command parses flags into the request, hands
it to the action, and prints what comes back. One shared frame in the CLI
resolves the clients, serialises the rows, and maps a failure to the error row
and the exit code. A handler holds no call sequence of its own.

The call sequence is the only thing a command owns, and it was the one part no
test ran. The flag that gates a second request (ADR 0005), the loop over test
runs, the two-request tail of a log, the stubs for a linked work item the batch
could not read: each sat in a handler that resolved a concrete client, so a test
could not put a fake under it. The parsing tests stopped at the parse, and the
builder tests started after the sequence. Moving the sequence behind the client
interfaces makes the interface the test surface: one action test with scripted
fakes asserts both the rows and the requests sent.

The alternative was to test the handlers end to end over a scripted HTTP server,
as the backlog tests do. Those tests cross every seam at once and stay, but one
per command would pin HTTP shapes to assert a loop. The other alternative was to
leave the sequences in the CLI and accept the gap.

The price is that Core names files after CLI commands, and every command pays
one more file and one more test. An action takes only what it uses, so the
widest command has the widest signature. Disk stays a CLI concern: an action
takes and returns content as strings, never a path.
