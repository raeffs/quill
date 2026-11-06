namespace Quill.Core.Models;

public class PipelineRun
{
    public required int Id { get; init; }

    public required int PipelineId { get; init; }

    public required string PipelineName { get; init; }

    public required string BuildNumber { get; init; }

    public required string Status { get; init; }

    public string? Result { get; init; }

    public required string Reason { get; init; }

    public required string SourceBranch { get; init; }

    public required string SourceVersion { get; init; }

    public required string RequestedFor { get; init; }

    public required DateTimeOffset QueueTime { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? FinishTime { get; init; }

    public required string WebUrl { get; init; }
}
