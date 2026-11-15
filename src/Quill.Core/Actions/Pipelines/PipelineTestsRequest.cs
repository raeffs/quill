using Quill.Core.Models;

namespace Quill.Core.Actions.Pipelines;

public sealed record PipelineTestsRequest(int RunId, bool WithResults, TestResultQuery Query);
