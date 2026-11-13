# Quill prerequisites

Read this file when a quill command fails before it reaches the server: the command is not found, the PAT is missing or lacks a scope, or the config file is missing or invalid.

## Install

Quill needs the `quill` CLI on PATH. Install it once per machine:

```bash
dotnet tool install -g Raeffs.Quill
```

The install needs the .NET 11 SDK. On `linux-x64` and `win-x64` quill then runs as a native binary. Other platforms need the .NET 11 runtime.

## Personal Access Token

`QUILL_PAT` holds a Personal Access Token. Each command needs its own scope:

| Command | PAT scope |
|---|---|
| every `quill backlog` and `quill wi` command | Work Items: Read & Write |
| `pr list`, `pr view`, `pr threads`, `pr revisions` | Code: Read |
| `pr create` | Code: Read & Write |
| `pipeline list`, `pipeline runs`, `pipeline view`, `pipeline log`, `pipeline wait` | Build: Read |
| `pipeline trigger`, `pipeline cancel`, `pipeline retry` | Build: Read & execute |
| `pipeline tests` | Test Management: Read |
| `pr checks` | Code: Read |
| `--reviewer`, `--author` or `--assignee` with a value other than `@me` | Identity: Read |

`pr view` also reads the linked work items, so it needs Work Items: Read. `pr create` links work items through the pull request and needs no work item scope, whether or not you pass `--work-item`.

Without Identity: Read, a command that resolves a person exits 5, and the message names Identity: Read.

A pipeline command fails with `401 (Unauthorized)` and exit 5 when the PAT lacks its scope. The server sends 401 for a missing scope and for a bad PAT alike. If the same PAT serves `pr` and `wi` commands, the PAT is fine and the scope is missing.

## Config file

`.quill.json` sits in the current working directory, or in the user profile directory (`$USERPROFILE` on Windows, `$HOME` elsewhere) as a fallback.

```json
{
  "serverUrl": "https://your-server.com/tfs",
  "collection": "DefaultCollection",
  "project": "YourProject",
  "allowedStates": ["New", "Active"]
}
```

All fields are required. `allowedStates` controls which work items you can push. Quill validates the assignee against the PAT user.
