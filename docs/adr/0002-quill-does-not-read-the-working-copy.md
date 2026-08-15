# Quill does not read the working copy

Quill needs `.quill.json`, a PAT and a network, and nothing more. It never assumes
the current directory holds a checkout, or that a checkout matches the configured
project.

An agent that wants the pull request for its current branch runs git itself and
passes the name. The git failure then lands where the agent can read it.

`@me` sets no precedent for this. It resolves against the Azure DevOps identity
API, not the filesystem.
