namespace Quill.Core.Models;

public enum IdentitySearchFilter
{
    General,
    MailAddress,
}

public sealed record IdentityMatch(string Id, string DisplayName, string? UniqueName, bool IsContainer);
