using System.Text.Json;

namespace Quill.Core.Models;

public class PolicyEvaluation
{
    public required string Name { get; init; }

    public bool IsBlocking { get; init; }

    public required string Status { get; init; }

    public DateTimeOffset? StartedDate { get; init; }

    public DateTimeOffset? CompletedDate { get; init; }

    public JsonElement? Context { get; init; }
}
