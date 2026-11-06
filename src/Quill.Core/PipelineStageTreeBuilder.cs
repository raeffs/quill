using Quill.Core.Models;

namespace Quill.Core;

public static class PipelineStageTreeBuilder
{
    public static IReadOnlyList<PipelineStageResult> Build(IReadOnlyList<TimelineRecord> records)
    {
        var ids = records.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var children = records
            .Where(r => r.ParentId is not null && ids.Contains(r.ParentId))
            .ToLookup(r => r.ParentId!, StringComparer.Ordinal);
        var roots = records.Where(r => r.ParentId is null).ToList();

        IEnumerable<TimelineRecord> ChildrenOf(TimelineRecord parent, string type) =>
            Sorted(children[parent.Id].Where(c => IsType(c, type)));

        var stages = Sorted(roots.Where(r => IsType(r, "Stage")))
            .Select(stage => new PipelineStageResult
            {
                Name = stage.Identifier,
                DisplayName = stage.Name,
                State = stage.State,
                Result = stage.Result,
                Jobs = ChildrenOf(stage, "Phase")
                    .SelectMany(phase => BuildJobs(phase, ChildrenOf(phase, "Job").ToList(), ChildrenOf))
                    .ToList(),
            });

        var rootJobStages = Sorted(roots.Where(r => IsType(r, "Job") && children[r.Id].Any(c => IsType(c, "Task"))))
            .Select(job => new PipelineStageResult
            {
                Name = null,
                DisplayName = job.Name,
                State = job.State,
                Result = job.Result,
                Jobs = [BuildJob(job, logId: null, ChildrenOf)],
            });

        return stages.Concat(rootJobStages).ToList();
    }

    private static IEnumerable<PipelineJobResult> BuildJobs(
        TimelineRecord phase,
        List<TimelineRecord> jobs,
        Func<TimelineRecord, string, IEnumerable<TimelineRecord>> childrenOf)
    {
        if (jobs.Count == 0)
        {
            return
            [
                new PipelineJobResult
                {
                    Name = phase.Name,
                    State = phase.State,
                    Result = phase.Result,
                    LogId = phase.LogId,
                    Tasks = [],
                },
            ];
        }

        return jobs.Select(job => BuildJob(job, job.LogId, childrenOf));
    }

    private static PipelineJobResult BuildJob(
        TimelineRecord job,
        int? logId,
        Func<TimelineRecord, string, IEnumerable<TimelineRecord>> childrenOf) => new()
        {
            Name = job.Name,
            State = job.State,
            Result = job.Result,
            LogId = logId,
            Tasks = childrenOf(job, "Task").Select(BuildTask).ToList(),
        };

    private static PipelineTaskResult BuildTask(TimelineRecord task) => new()
    {
        Name = task.Name,
        State = task.State,
        Result = task.Result,
        LogId = task.LogId,
        ErrorCount = task.ErrorCount,
        WarningCount = task.WarningCount,
        Issues = task.Issues
            .Select(i => new PipelineIssueResult { Type = i.Type, Message = i.Message, Line = i.Line })
            .ToList(),
    };

    private static IEnumerable<TimelineRecord> Sorted(IEnumerable<TimelineRecord> records) =>
        records.OrderBy(r => r.Order is null).ThenBy(r => r.Order);

    private static bool IsType(TimelineRecord record, string type) =>
        string.Equals(record.Type, type, StringComparison.Ordinal);
}
