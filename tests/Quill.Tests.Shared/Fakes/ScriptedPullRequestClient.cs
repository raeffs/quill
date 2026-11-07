using System.Globalization;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Tests.Shared.Fakes;

public sealed class ScriptedPullRequestClient : IAzureDevOpsPullRequestClient
{
#pragma warning disable CA1002, MA0016 // Tests assert on the recorded calls as a list
    public List<string> Calls { get; } = [];
#pragma warning restore CA1002, MA0016

    public Func<PullRequestListQuery, IReadOnlyList<PullRequest>> List { get; set; } = Unscripted<PullRequestListQuery, IReadOnlyList<PullRequest>>();

    public Func<int, PullRequest> GetById { get; set; } = Unscripted<int, PullRequest>();

    public Func<string, int, IReadOnlyList<PolicyEvaluation>> GetPolicyEvaluations { get; set; } = Unscripted<string, int, IReadOnlyList<PolicyEvaluation>>();

    public Func<int, string, IReadOnlyList<PullRequestThread>> GetThreads { get; set; } = Unscripted<int, string, IReadOnlyList<PullRequestThread>>();

    public Func<int, string, IReadOnlyList<PullRequestRevision>> GetRevisions { get; set; } = Unscripted<int, string, IReadOnlyList<PullRequestRevision>>();

    public Func<int, string, IReadOnlyList<int>> GetWorkItemRefs { get; set; } = Unscripted<int, string, IReadOnlyList<int>>();

    public Func<PullRequestCreateRequest, PullRequest> Create { get; set; } = Unscripted<PullRequestCreateRequest, PullRequest>();

    public Task<IReadOnlyList<PullRequest>> ListAsync(PullRequestListQuery query, CancellationToken cancellationToken = default)
        => Record("List", () => List(query));

    public Task<PullRequest> GetByIdAsync(int prId, CancellationToken cancellationToken = default)
        => Record($"GetById({Format(prId)})", () => GetById(prId));

    public Task<IReadOnlyList<PolicyEvaluation>> GetPolicyEvaluationsAsync(string projectId, int codeReviewId, CancellationToken cancellationToken = default)
        => Record($"GetPolicyEvaluations({projectId},{Format(codeReviewId)})", () => GetPolicyEvaluations(projectId, codeReviewId));

    public Task<IReadOnlyList<PullRequestThread>> GetThreadsAsync(int prId, string repo, CancellationToken cancellationToken = default)
        => Record($"GetThreads({Format(prId)},{repo})", () => GetThreads(prId, repo));

    public Task<IReadOnlyList<PullRequestRevision>> GetRevisionsAsync(int prId, string repo, CancellationToken cancellationToken = default)
        => Record($"GetRevisions({Format(prId)},{repo})", () => GetRevisions(prId, repo));

    public Task<IReadOnlyList<int>> GetWorkItemRefsAsync(int prId, string repo, CancellationToken cancellationToken = default)
        => Record($"GetWorkItemRefs({Format(prId)},{repo})", () => GetWorkItemRefs(prId, repo));

    public Task<PullRequest> CreateAsync(PullRequestCreateRequest request, CancellationToken cancellationToken = default)
        => Record("Create", () => Create(request));

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static Func<T, TResult> Unscripted<T, TResult>() => _ => throw new NotSupportedException();

    private static Func<T1, T2, TResult> Unscripted<T1, T2, TResult>() => (_, _) => throw new NotSupportedException();

    private Task<T> Record<T>(string call, Func<T> script)
    {
        Calls.Add(call);
        return Task.FromResult(script());
    }
}
