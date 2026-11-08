using Quill.Core.Models;

namespace Quill.Core;

public sealed class PersonResolver(IIdentityClient identity)
{
    private Task<CurrentUser>? connectionData;

    public static bool IsMe(string value) => string.Equals(value.Trim(), "@me", StringComparison.OrdinalIgnoreCase);

    public Task<CurrentUser> MeAsync() => connectionData ??= identity.GetCurrentUserAsync();

    public async Task<Person> ResolveAsync(string flag, string value, CancellationToken cancellationToken)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidRequestException($"{flag} must not be empty.");
        }

        if (IsMe(trimmed))
        {
            var me = await MeAsync();
            return new Person(me.Id, me.DisplayName, null);
        }

        var filter = trimmed.Contains('@', StringComparison.Ordinal) ? IdentitySearchFilter.MailAddress : IdentitySearchFilter.General;
        var hits = await identity.SearchAsync(filter, trimmed, cancellationToken);
        var people = hits.Where(hit => !hit.IsContainer).DistinctBy(hit => hit.Id, StringComparer.Ordinal).ToList();

        if (people.Count == 0)
        {
            throw new InvalidRequestException(hits.Count == 0
                ? $"{flag} '{trimmed}' matches nobody."
                : $"{flag} '{trimmed}' matches only groups. Groups are not accepted; name a person.");
        }

        if (people.Count > 1)
        {
            var listed = string.Join(", ", people.Select(p => p.UniqueName is null ? p.DisplayName : $"{p.DisplayName} ({p.UniqueName})"));
            throw new InvalidRequestException($"{flag} '{trimmed}' matches {people.Count} people: {listed}. Pass the unique name.");
        }

        var match = people[0];
        return new Person(match.Id, match.DisplayName, match.UniqueName);
    }
}
