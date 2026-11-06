namespace Quill.AzureDevOps;

internal static class RefNames
{
    private const string RefsHeadsPrefix = "refs/heads/";

    public static string FromBranch(string branch)
    {
        return branch.StartsWith("refs/", StringComparison.Ordinal)
            ? branch
            : RefsHeadsPrefix + branch;
    }
}
