using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;

namespace Quill.Cli;

internal static class Validators
{
    public static void PositiveInteger(Argument<int> argument, string name) =>
        argument.Validators.Add(result => RejectBelow(result, 1, $"{name} must be a positive integer."));

    public static void PositiveInteger(Option<int?> option, string name) =>
        option.Validators.Add(result => RejectBelow(result, 1, $"{name} must be a positive integer."));

    public static void NotNegative(Option<int?> option, string name) =>
        option.Validators.Add(result => RejectBelow(result, 0, $"{name} must not be negative."));

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
