# Quill

## Development Workflow

- Sourcecode lives on [Forgejo][forgejo] - the `main` branch is protected
- Sourcecode is mirrored to [GitHub][github]
- PR validation runs on Forgejo
- Releases run on GitHub

## Issue Tracker

- Issues live on [Redmine][redmine]
- Public issues live on GitHub

## Test Data

- Never commit real data from the Azure DevOps server: names, IDs, GUIDs, URLs, titles or messages.
- Replace each real value with a made-up one, such as `Contoso` or `Jane Doe`, and keep the shape of the data.

[forgejo]: https://code.raeffs.dev/raeffs-dot-dev/quill
[github]: https://github.com/raeffs/quill
[redmine]: https://projects.raeffs.dev/projects/quill
