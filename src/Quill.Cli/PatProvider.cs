namespace Quill.Cli;

internal interface IPatProvider
{
    string GetPat();
}

internal sealed class EnvironmentPatProvider : IPatProvider
{
    public string GetPat() =>
        Environment.GetEnvironmentVariable("QUILL_PAT")
        ?? throw new InvalidOperationException("QUILL_PAT environment variable is not set.");
}
