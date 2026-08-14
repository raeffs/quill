# Quill

Azure DevOps Server CLI for coding agents.

Reads work items and pull requests as JSON. Edits work items as markdown.

## Install

Installing needs the .NET 10 SDK. Earlier SDKs fail with an `unsupported runner` error.

```sh
dotnet tool install -g Raeffs.Quill
```

On `linux-x64` and `win-x64`, quill is a native binary and runs without a .NET runtime. Other platforms get a portable build that needs the .NET 10 runtime.

## Update

```sh
dotnet tool update -g Raeffs.Quill
```

## Usage

See [`skills/quill/SKILL.md`](https://github.com/raeffs/quill/blob/main/skills/quill/SKILL.md) for commands, configuration (`.quill.json`), and the markdown file format.
