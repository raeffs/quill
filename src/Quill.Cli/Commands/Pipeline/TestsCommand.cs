using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quill.AzureDevOps;
using Quill.Core;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class TestsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID" };

        runIdArg.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var runId) && runId < 1)
            {
                result.AddError("runId must be a positive integer.");
            }
        });

        var withResultsOption = new Option<bool>("--with-results")
        {
            Description = "Add a results array to each test run, with message and stack trace.",
        };

        var outcomeOption = new Option<string[]>("--outcome")
        {
            Description = "Result outcome, for example Failed or Passed. Repeat for more. Replaces the default Failed. Passed to the server as is. Needs --with-results.",
            DefaultValueFactory = _ => ["Failed"],
        };

        var limitOption = new Option<int?>("--limit")
        {
            Description = "Maximum number of results per test run. Needs --with-results.",
        };

        limitOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var limit) && limit < 1)
            {
                result.AddError("--limit must be a positive integer.");
            }
        });

        var skipOption = new Option<int?>("--skip")
        {
            Description = "Number of results to skip per test run. Needs --with-results.",
        };

        skipOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0 && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var skip) && skip < 0)
            {
                result.AddError("--skip must not be negative.");
            }
        });

        var command = new Command("tests", "List the test runs of a pipeline run and print them as a JSON array")
        {
            runIdArg,
            withResultsOption,
            outcomeOption,
            limitOption,
            skipOption,
        };

        command.Validators.Add(commandResult =>
        {
            if (commandResult.GetValue(withResultsOption))
            {
                return;
            }

            if (IsTyped(commandResult.GetResult(outcomeOption))
                || IsTyped(commandResult.GetResult(limitOption))
                || IsTyped(commandResult.GetResult(skipOption)))
            {
                commandResult.AddError("--outcome, --limit and --skip need --with-results.");
            }
        });

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var runId = parseResult.GetValue(runIdArg);
            var withResults = parseResult.GetValue(withResultsOption);
            var query = new TestResultQuery
            {
                Outcomes = parseResult.GetValue(outcomeOption) ?? ["Failed"],
                Top = parseResult.GetValue(limitOption),
                Skip = parseResult.GetValue(skipOption),
            };
            await ExecuteAsync(serviceProvider, runId, withResults, query, cancellationToken);
        });

        return command;
    }

    private static bool IsTyped(OptionResult? result)
    {
        return result is not null && !result.Implicit;
    }

    private static async Task ExecuteAsync(
        IServiceProvider serviceProvider,
        int runId,
        bool withResults,
        TestResultQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var buildClient = serviceProvider.GetRequiredService<AzureDevOpsBuildClient>();

            var testRuns = await buildClient.ListTestRunsAsync(runId, cancellationToken);
            var rows = new List<PipelineTestRunResult>();
            foreach (var testRun in testRuns)
            {
                IReadOnlyList<TestCaseResult>? results = null;
                if (withResults)
                {
                    results = await buildClient.ListTestResultsAsync(testRun.Id, query, cancellationToken);
                }

                rows.Add(PipelineTestRunResultBuilder.Build(testRun, results));
            }

            Console.WriteLine(JsonSerializer.Serialize(rows, CommandHelpers.Context.ListPipelineTestRunResult));
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or HttpRequestException)
        {
            CommandHelpers.HandleError(ex);
        }
    }
}
