using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using Quill.Core;

namespace Quill.Cli;

internal static class Validators
{
    public static void PositiveInteger(Argument<int> argument, string name) =>
        argument.Validators.Add(result => RejectBelow(result, 1, $"{name} must be a positive integer."));

    public static void PositiveInteger(Option<int> option, string name) =>
        option.Validators.Add(result => RejectBelow(result, 1, $"{name} must be a positive integer."));

    public static void PositiveInteger(Option<int?> option, string name) =>
        option.Validators.Add(result => RejectBelow(result, 1, $"{name} must be a positive integer."));

    public static void NotNegative(Option<int?> option, string name) =>
        option.Validators.Add(result => RejectBelow(result, 0, $"{name} must not be negative."));

    public static void NotEmpty(Option<string> option, string name) =>
        option.Validators.Add(result =>
        {
            if (result.Tokens.Any(token => string.IsNullOrWhiteSpace(token.Value)))
            {
                result.AddError($"{name} must not be empty.");
            }
        });

    public static void KnownThreadStatus(Option option, string name) =>
        option.Validators.Add(result =>
        {
            if (result.Tokens.Any(token => !ThreadStatus.IsKnown(token.Value)))
            {
                result.AddError($"{name} must be one of: {string.Join(", ", ThreadStatus.All)}.");
            }
        });

    private static void RejectBelow(SymbolResult result, int minimum, string message)
    {
        if (result.Tokens.Count > 0
            && int.TryParse(result.Tokens[0].Value, CultureInfo.InvariantCulture, out var value)
            && value < minimum)
        {
            result.AddError(message);
        }
    }
}
