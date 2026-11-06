using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PipelineRunResultBuilder
{
    private const string RefsHeadsPrefix = "refs/heads/";

    public static PipelineRunResult Build(PipelineRun run)
    {
        return new PipelineRunResult
        {
            Id = run.Id,
            PipelineId = run.PipelineId,
            PipelineName = run.PipelineName,
            BuildNumber = run.BuildNumber,
            Status = run.Status,
            Result = run.Result,
            Reason = run.Reason,
            SourceBranch = StripRefsHeads(run.SourceBranch),
            SourceVersion = run.SourceVersion,
            RequestedFor = run.RequestedFor,
            QueueTime = FormatIsoUtc(run.QueueTime),
            StartTime = run.StartTime is null ? null : FormatIsoUtc(run.StartTime.Value),
            FinishTime = run.FinishTime is null ? null : FormatIsoUtc(run.FinishTime.Value),
            Url = run.WebUrl,
        };
    }

    private static string StripRefsHeads(string refName)
    {
        return refName.StartsWith(RefsHeadsPrefix, StringComparison.Ordinal)
            ? refName[RefsHeadsPrefix.Length..]
            : refName;
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
