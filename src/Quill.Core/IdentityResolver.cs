using Quill.Core.Models;

namespace Quill.Core;

public sealed class IdentityResolver(IIdentityClient identity, PersonCache cache)
{
    private Task<CurrentUser>? _connectionData;

    public Task<CurrentUser> MeAsync() => _connectionData ??= LoadMeAsync();

    public async Task<PersonFilter> ResolveForQueryAsync(string flag, string value, CancellationToken cancellationToken)
    {
        return IsMe(value)
            ? PersonFilter.Me
            : PersonFilter.Of(await ResolveAsync(flag, value, cancellationToken));
    }

    public async Task<Identity> ResolveAsync(string flag, string value, CancellationToken cancellationToken)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new UsageException($"{flag} must not be empty.");
        }

        if (IsMe(trimmed))
        {
            var me = await MeAsync();
            return new Identity(me.Id, me.DisplayName, null, false);
        }

        var known = cache.Find(trimmed, false);
        if (known is not null)
        {
            return known;
        }

        var filter = trimmed.Contains('@', StringComparison.Ordinal) ? IdentitySearchFilter.MailAddress : IdentitySearchFilter.General;
        var hits = await identity.SearchAsync(filter, trimmed, cancellationToken);
        var people = hits.Where(hit => !hit.IsContainer).DistinctBy(hit => hit.Id, StringComparer.Ordinal).ToList();

        if (people.Count == 0)
        {
            throw new RejectedException(hits.Count == 0
                ? $"{flag} '{trimmed}' matches nobody."
                : $"{flag} '{trimmed}' matches only groups. Groups are not accepted; name a person.");
        }

        if (people.Count > 1)
        {
            var listed = string.Join(", ", people.Select(p => p.UniqueName is null ? p.DisplayName : $"{p.DisplayName} ({p.UniqueName})"));
            throw new RejectedException($"{flag} '{trimmed}' matches {people.Count} people: {listed}. Pass the unique name.");
        }

        var match = people[0];
        var person = new Identity(match.Id, match.DisplayName, match.UniqueName, false);
        cache.Remember(trimmed, false, person);
        return person;
    }

    private async Task<CurrentUser> LoadMeAsync()
    {
        var known = cache.FindMe();
        if (known is not null)
        {
            return known;
        }

        var me = await identity.GetCurrentUserAsync();
        cache.RememberMe(me);
        return me;
    }

    private static bool IsMe(string value) => string.Equals(value.Trim(), "@me", StringComparison.OrdinalIgnoreCase);
}
