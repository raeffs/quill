using Quill.Core.Models;

namespace Quill.Core.Actions.Backlog;

public sealed record BacklogCreateOutcome(CreateResult Row, string FileContent);
