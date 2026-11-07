using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Quill.Core;
using Quill.Core.Actions;
using Quill.Core.Models;

namespace Quill.Cli.Commands.Pipeline;

internal static class TestsCommand
{
    public static Command Create(IServiceProvider serviceProvider)
    {
        var runIdArg = new Argument<int>("runId") { Description = "The pipeline run ID" };

        Validators.PositiveInteger(runIdArg, "runId");

        var withResultsOption = new Option<bool>("--with-results")
        {
            Description = "Add a results array to each test run, with message and stack trace.",
        };

        var outcomeOption = new Option<string[]>("--outcome")
        {
            Description = "Test result outcome, for example Failed or Passed. Repeat for more. Replaces the default Failed. Passed to the server as is. Needs --with-results.",
            DefaultValueFactory = _ => ["Failed"],
        };

        var limitOption = new Option<int?>("--limit")
        {
            Description = "Maximum number of results per test run. Needs --with-results.",
        };

        Validators.PositiveInteger(limitOption, "--limit");

        var skipOption = new Option<int?>("--skip")
        {
            Description = "Number of results to skip per test run. Needs --with-results.",
        };

        Validators.NotNegative(skipOption, "--skip");

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

        command.SetAction((parseResult, cancellationToken) =>
        {
            var request = new PipelineTestsRequest(
                parseResult.GetValue(runIdArg),
                parseResult.GetValue(withResultsOption),
                new TestResultQuery
                {
                    Outcomes = parseResult.GetValue(outcomeOption) ?? ["Failed"],
                    Top = parseResult.GetValue(limitOption),
                    Skip = parseResult.GetValue(skipOption),
                });

            return CommandHelpers.RunAsync(
                ct => PipelineTestsAction.RunAsync(serviceProvider.GetRequiredService<IAzureDevOpsBuildClient>(), request, ct),
                CommandHelpers.Context.ListPipelineTestRunResult,
                cancellationToken);
        });

        return command;
    }

    private static bool IsTyped(OptionResult? result)
    {
        return result is not null && !result.Implicit;
    }
}
