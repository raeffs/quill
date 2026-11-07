using Quill.Core.Actions.Pipelines;
using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests.Actions.Pipelines;

public class ListActionTests
{
    [Fact]
    public async Task RunAsync_ListsPipelinesAndBuildsOneRowEach()
    {
        var client = new ScriptedBuildClient
        {
            ListPipelines = _ => [Pipeline(12), Pipeline(13)],
        };
        var query = new PipelineListQuery { Top = 50 };

        var rows = await ListAction.RunAsync(client, query, TestContext.Current.CancellationToken);

        rows.Select(row => row.Id).ShouldBe([12, 13]);
        client.Calls.ShouldBe(["ListPipelines"]);
    }

    private static Pipeline Pipeline(int id) => new() { Id = id, Name = "Contoso CI", Path = "Contoso" };
}
