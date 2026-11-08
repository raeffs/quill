# Quill judges only what it already holds

Quill may compute a judgement when every input sits in a response the command
already reads, and the judgement changes what the agent does next. It may also
interpret the values the server sends, such as what a check status means for the
merge. It must not send a request only to feed a judgement, and it must not
invent an input the server does not send.

The `pr checks` verdict passes. It reads the pull request and the policy
evaluations, and `pr checks` already requests both. A diff fails, as ADR 0006
says: it needs extra requests, and it guesses from the file extension which
files are binary.

A judgement quill may not compute stays the agent's work. That is the price.
