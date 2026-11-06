using System.Globalization;
using Quill.Core.Models;

namespace Quill.Core;

public static class PipelineTestRunResultBuilder
{
    public static PipelineTestRunResult Build(TestRun run, IReadOnlyList<TestCaseResult>? results)
    {
        return new PipelineTestRunResult
        {
            Id = run.Id,
            Name = run.Name,
            State = run.State,
            Total = run.TotalTests,
            Passed = run.PassedTests,
            Failed = run.UnanalyzedTests,
            Skipped = run.NotApplicableTests,
            Results = results?.Select(BuildResult).ToList(),
        };
    }

    private static PipelineTestCaseResult BuildResult(TestCaseResult result)
    {
        return new PipelineTestCaseResult
        {
            Title = result.Title,
            Name = result.Name,
            Storage = result.Storage,
            Outcome = result.Outcome,
            Message = result.Message,
            StackTrace = result.StackTrace,
            DurationMs = result.DurationInMs is null
                ? null
                : (int)Math.Round(result.DurationInMs.Value, MidpointRounding.AwayFromZero),
            FailingSince = result.FailingSince is null ? null : FormatIsoUtc(result.FailingSince.Value),
            FailingSinceRunId = result.FailingSinceRunId,
        };
    }

    private static string FormatIsoUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
