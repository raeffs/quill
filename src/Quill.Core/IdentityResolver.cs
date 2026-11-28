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
            : PersonFilter.Of(await ResolveAsync(flag, value, false, cancellationToken));
    }

    public async Task<Identity> ResolveAsync(string flag, string value, bool acceptGroups, CancellationToken cancellationToken)
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

        var known = cache.Find(trimmed, acceptGroups);
        if (known is not null)
        {
            return known;
        }

        var filter = trimmed.Contains('@', StringComparison.Ordinal) ? IdentitySearchFilter.MailAddress : IdentitySearchFilter.General;
        var hits = (await identity.SearchAsync(filter, trimmed, cancellationToken))
            .DistinctBy(hit => hit.Id, StringComparer.Ordinal)
            .ToList();
        var people = hits.Where(hit => !hit.IsContainer).ToList();
        var groups = acceptGroups ? hits.Where(hit => hit.IsContainer).ToList() : [];

        if (people.Count + groups.Count != 1)
        {
            throw new RejectedException($"{flag} '{trimmed}' {Mismatch(people, groups, hits.Count, acceptGroups)}");
        }

        var match = people.Count == 1 ? people[0] : groups[0];
        var resolved = new Identity(match.Id, match.DisplayName, match.UniqueName, match.IsContainer);
        cache.Remember(trimmed, acceptGroups, resolved);
        return resolved;
    }

    private static string Mismatch(List<IdentityMatch> people, List<IdentityMatch> groups, int hits, bool acceptGroups)
    {
        var listedPeople = people.Select(p => p.UniqueName is null ? p.DisplayName : $"{p.DisplayName} ({p.UniqueName})");
        var listedGroups = groups.Select(g => g.DisplayName);

        if (people.Count > 0 && groups.Count > 0)
        {
            var counts = $"{people.Count} {(people.Count == 1 ? "person" : "people")} and {groups.Count} {(groups.Count == 1 ? "group" : "groups")}";
            return $"matches {counts}: {string.Join(", ", listedPeople.Concat(listedGroups))}. Pass the unique name or the scoped name.";
        }

        if (people.Count > 0)
        {
            return $"matches {people.Count} people: {string.Join(", ", listedPeople)}. Pass the unique name.";
        }

        if (groups.Count > 0)
        {
            return $"matches {groups.Count} groups: {string.Join(", ", listedGroups)}. Pass the scoped name.";
        }

        if (acceptGroups)
        {
            return "matches no person and no group.";
        }

        return hits == 0 ? "matches nobody." : "matches only groups. Groups are not accepted; name a person.";
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
