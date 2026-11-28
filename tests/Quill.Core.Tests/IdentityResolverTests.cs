using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests;

public class IdentityResolverTests
{
    [Theory]
    [InlineData("@me")]
    [InlineData("@Me")]
    [InlineData(" @me ")]
    public async Task ResolveAsync_Me_UsesConnectionDataAndNoSearch(string value)
    {
        var identity = new ScriptedIdentityClient();

        var person = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", value, false, TestContext.Current.CancellationToken);

        person.ShouldBe(new Identity("user-1", "Jane Doe", null, false));
        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task ResolveAsync_MeTwice_CallsConnectionDataOnce()
    {
        var identity = new ScriptedIdentityClient();
        var resolver = new IdentityResolver(identity, TestPersonCache.Create());

        await resolver.ResolveAsync("--reviewer", "@me", false, TestContext.Current.CancellationToken);
        await resolver.MeAsync();

        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task ResolveAsync_Name_SearchesGeneral()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", @"CONTOSO\jroe")],
        };

        var person = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        person.ShouldBe(new Identity("id-2", "John Roe", @"CONTOSO\jroe", false));
        identity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_Email_SearchesMailAddress()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", @"CONTOSO\jroe")],
        };

        await new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "jroe@contoso.com", false, TestContext.Current.CancellationToken);

        identity.Calls.ShouldBe(["Search(MailAddress,jroe@contoso.com)"]);
    }

    [Fact]
    public async Task ResolveAsync_PersonAndGroup_ReturnsThePerson()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) =>
            [
                Match("id-3", "Contoso Team", @"CONTOSO\team", isContainer: true),
                Match("id-2", "John Roe", @"CONTOSO\jroe"),
            ],
        };

        var person = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        person.Id.ShouldBe("id-2");
    }

    [Fact]
    public async Task ResolveAsync_NoHits_Throws()
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<RejectedException>(
            () => new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "Jane", false, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--assignee 'Jane' matches nobody.");
    }

    [Fact]
    public async Task ResolveAsync_OnlyGroups_ThrowsAndSaysGroupsAreNotAccepted()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-3", "Contoso Team", @"CONTOSO\team", isContainer: true)],
        };

        var ex = await Should.ThrowAsync<RejectedException>(
            () => new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--reviewer", "Contoso Team", false, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--reviewer 'Contoso Team' matches only groups. Groups are not accepted; name a person.");
    }

    [Fact]
    public async Task ResolveAsync_TwoPeople_ThrowsAndListsBoth()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) =>
            [
                Match("id-1", "Jane Doe", @"CONTOSO\jdoe"),
                Match("id-2", "Jane Doe", @"CONTOSO\jdoe2"),
            ],
        };

        var ex = await Should.ThrowAsync<RejectedException>(
            () => new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--reviewer", "Jane Doe", false, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe(@"--reviewer 'Jane Doe' matches 2 people: Jane Doe (CONTOSO\jdoe), Jane Doe (CONTOSO\jdoe2). Pass the unique name.");
    }

    [Fact]
    public async Task ResolveAsync_SamePersonTwice_ReturnsThatPerson()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) =>
            [
                Match("id-2", "John Roe", @"CONTOSO\jroe"),
                Match("id-2", "John Roe", @"CONTOSO\jroe"),
            ],
        };

        var person = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        person.Id.ShouldBe("id-2");
    }

    [Fact]
    public async Task ResolveAsync_NoUniqueName_PersonHasNone()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", null)],
        };

        var person = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        person.ShouldBe(new Identity("id-2", "John Roe", null, false));
    }

    [Fact]
    public async Task ResolveAsync_TwoPeopleWithoutUniqueName_ListsDisplayNamesOnly()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) =>
            [
                Match("id-1", "John Roe", null),
                Match("id-2", "John Roe", @"CONTOSO\jroe2"),
            ],
        };

        var ex = await Should.ThrowAsync<RejectedException>(
            () => new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe(@"--assignee 'John Roe' matches 2 people: John Roe, John Roe (CONTOSO\jroe2). Pass the unique name.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ResolveAsync_Blank_ThrowsBeforeAnyRequest(string value)
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<UsageException>(
            () => new IdentityResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", value, false, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--assignee must not be empty.");
        identity.Calls.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("@me")]
    [InlineData("@ME")]
    [InlineData(" @me ")]
    public async Task ResolveForQueryAsync_Me_AnswersMeAndSendsNoRequest(string value)
    {
        var identity = new ScriptedIdentityClient();

        var filter = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveForQueryAsync("--assignee", value, TestContext.Current.CancellationToken);

        filter.ShouldBeSameAs(PersonFilter.Me);
        filter.Person.ShouldBeNull();
        identity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveForQueryAsync_Name_AnswersTheResolvedPerson()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", @"CONTOSO\jroe")],
        };

        var filter = await new IdentityResolver(identity, TestPersonCache.Create()).ResolveForQueryAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        filter.Person.ShouldBe(new Identity("id-2", "John Roe", @"CONTOSO\jroe", false));
        identity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveForQueryAsync_Blank_ThrowsUsageAndSendsNoRequest()
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<UsageException>(() =>
            new IdentityResolver(identity, TestPersonCache.Create()).ResolveForQueryAsync("--assignee", " ", TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--assignee must not be empty.");
        identity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_NameResolvedByAnEarlierRun_SendsNoSearch()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, JohnRoe);
        await first.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);
        var (second, secondIdentity) = Run(store);

        var person = await second.ResolveAsync("--assignee", "john roe", false, TestContext.Current.CancellationToken);

        person.ShouldBe(new Identity("id-2", "John Roe", @"CONTOSO\jroe", false));
        secondIdentity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveForQueryAsync_NameResolvedByAnEarlierRun_SendsNoSearch()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, JohnRoe);
        await first.ResolveForQueryAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);
        var (second, secondIdentity) = Run(store);

        var filter = await second.ResolveForQueryAsync("--assignee", "john roe", TestContext.Current.CancellationToken);

        filter.ShouldBe(PersonFilter.Of(new Identity("id-2", "John Roe", @"CONTOSO\jroe", false)));
        secondIdentity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task MeAsync_ResolvedByAnEarlierRun_SendsNoRequest()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store);
        await first.MeAsync();
        var (second, secondIdentity) = Run(store);

        var me = await second.MeAsync();

        me.ShouldBe(new CurrentUser("user-1", "Jane Doe"));
        secondIdentity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_MeResolvedByAnEarlierRun_ReturnsThePersonWithoutAUniqueName()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store);
        await first.MeAsync();
        var (second, secondIdentity) = Run(store);

        var person = await second.ResolveAsync("--assignee", "@me", false, TestContext.Current.CancellationToken);

        person.ShouldBe(new Identity("user-1", "Jane Doe", null, false));
        secondIdentity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task MeAsync_OtherPat_AsksTheServer()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store);
        await first.MeAsync();
        var (second, secondIdentity) = Run(store, pat: "other-pat");

        await second.MeAsync();

        secondIdentity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task ResolveAsync_NobodyMatched_AsksAgainOnTheNextRun()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store);
        await Should.ThrowAsync<RejectedException>(() => first.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken));
        var (second, secondIdentity) = Run(store, JohnRoe);

        await second.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_SeveralMatched_AsksAgainOnTheNextRun()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, (_, _) => [Match("id-1", "John Roe", @"CONTOSO\jroe"), Match("id-2", "John Roe", @"CONTOSO\jroe2")]);
        await Should.ThrowAsync<RejectedException>(() => first.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken));
        var (second, secondIdentity) = Run(store, JohnRoe);

        await second.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_OnlyGroupsMatched_AsksAgainOnTheNextRun()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, (_, _) => [Match("id-3", "John Roe", @"CONTOSO\team", isContainer: true)]);
        await Should.ThrowAsync<RejectedException>(() => first.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken));
        var (second, secondIdentity) = Run(store, JohnRoe);

        await second.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_After24Hours_AsksAgain()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        var (first, _) = Run(store, JohnRoe, clock: clock);
        await first.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);
        clock.Now += PersonCache.MaxAge;
        var (second, secondIdentity) = Run(store, JohnRoe, clock: clock);

        await second.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_OtherCollection_AsksTheServer()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, JohnRoe);
        await first.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);
        var (second, secondIdentity) = Run(store, JohnRoe, collection: "OTHER");

        await second.ResolveAsync("--assignee", "John Roe", false, TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_OneGroup_ReturnsTheGroup()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), (_, _) => [Platform()]);

        var group = await Reviewer(resolver, "Contoso Platform");

        group.ShouldBe(new Identity("id-3", @"[Fabrikam]\Contoso Platform", null, true));
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_ScopedName_IsSentAsItIs()
    {
        var (resolver, identity) = Run(new InMemoryPersonStore(), (_, _) => [Platform()]);

        await Reviewer(resolver, @"[Fabrikam]\Contoso Platform");

        identity.Calls.ShouldBe([@"Search(General,[Fabrikam]\Contoso Platform)"]);
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_OnePerson_ReturnsThePerson()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), JohnRoe);

        var person = await Reviewer(resolver, "John Roe");

        person.ShouldBe(new Identity("id-2", "John Roe", @"CONTOSO\jroe", false));
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_NoHits_Throws()
    {
        var (resolver, _) = Run(new InMemoryPersonStore());

        var ex = await Should.ThrowAsync<RejectedException>(() => Reviewer(resolver, "Contoso Platfrom"));

        ex.Message.ShouldBe("--reviewer 'Contoso Platfrom' matches no person and no group.");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_SeveralGroups_ListsEachScopedName()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), (_, _) =>
        [
            Group("id-5", @"[Fabrikam]\Contributors"),
            Group("id-6", @"[Northwind]\Contributors"),
            Group("id-7", @"[Tailspin]\Contributors"),
        ]);

        var ex = await Should.ThrowAsync<RejectedException>(() => Reviewer(resolver, "Contributors"));

        ex.Message.ShouldBe(@"--reviewer 'Contributors' matches 3 groups: [Fabrikam]\Contributors, [Northwind]\Contributors, [Tailspin]\Contributors. Pass the scoped name.");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_PersonAndGroup_ListsBoth()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), PlatformPersonAndGroup);

        var ex = await Should.ThrowAsync<RejectedException>(() => Reviewer(resolver, "Platform"));

        ex.Message.ShouldBe(@"--reviewer 'Platform' matches 1 person and 1 group: Platform (CONTOSO\svc-platform), [Fabrikam]\Platform. Pass the unique name or the scoped name.");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_TwoPeopleAndTwoGroups_UsesThePlurals()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), (_, _) =>
        [
            Group("id-3", @"[Fabrikam]\Platform"),
            Match("id-4", "Platform", @"CONTOSO\svc-platform"),
            Group("id-5", @"[Northwind]\Platform"),
            Match("id-6", "Platform", @"CONTOSO\svc-platform2"),
        ]);

        var ex = await Should.ThrowAsync<RejectedException>(() => Reviewer(resolver, "Platform"));

        ex.Message.ShouldBe(@"--reviewer 'Platform' matches 2 people and 2 groups: Platform (CONTOSO\svc-platform), Platform (CONTOSO\svc-platform2), [Fabrikam]\Platform, [Northwind]\Platform. Pass the unique name or the scoped name.");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_TwoPeople_KeepsTheMessageOfToday()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), (_, _) =>
        [
            Match("id-1", "Jane Doe", @"CONTOSO\jdoe"),
            Match("id-2", "Jane Doe", @"CONTOSO\jdoe2"),
        ]);

        var ex = await Should.ThrowAsync<RejectedException>(() => Reviewer(resolver, "Jane Doe"));

        ex.Message.ShouldBe(@"--reviewer 'Jane Doe' matches 2 people: Jane Doe (CONTOSO\jdoe), Jane Doe (CONTOSO\jdoe2). Pass the unique name.");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_SameGroupTwice_ReturnsThatGroup()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), (_, _) => [Platform(), Platform()]);

        var group = await Reviewer(resolver, "Contoso Platform");

        group.Id.ShouldBe("id-3");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_GroupWithoutAScopedName_IsListedAsItIs()
    {
        var (resolver, _) = Run(new InMemoryPersonStore(), (_, _) =>
        [
            Group("id-5", "Administrators"),
            Group("id-6", @"[Fabrikam]\Administrators"),
        ]);

        var ex = await Should.ThrowAsync<RejectedException>(() => Reviewer(resolver, "Administrators"));

        ex.Message.ShouldBe(@"--reviewer 'Administrators' matches 2 groups: Administrators, [Fabrikam]\Administrators. Pass the scoped name.");
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_Me_SendsNoSearch()
    {
        var (resolver, identity) = Run(new InMemoryPersonStore(), (_, _) => [Platform()]);

        var me = await Reviewer(resolver, "@me");

        me.ShouldBe(new Identity("user-1", "Jane Doe", null, false));
        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_GroupResolvedByAnEarlierRun_SendsNoSearch()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, (_, _) => [Platform()]);
        await Reviewer(first, "Contoso Platform");
        var (second, secondIdentity) = Run(store);

        var group = await Reviewer(second, "contoso platform");

        group.ShouldBe(new Identity("id-3", @"[Fabrikam]\Contoso Platform", null, true));
        secondIdentity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_GroupsOn_SeveralMatched_AsksAgainOnTheNextRun()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, PlatformPersonAndGroup);
        await Should.ThrowAsync<RejectedException>(() => Reviewer(first, "Platform"));
        var (second, secondIdentity) = Run(store, PlatformPersonAndGroup);

        await Should.ThrowAsync<RejectedException>(() => Reviewer(second, "Platform"));

        secondIdentity.Calls.ShouldBe(["Search(General,Platform)"]);
    }

    [Fact]
    public async Task ResolveAsync_PeopleOnlyThenGroupsOn_DoesNotShareTheEntry()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, PlatformPersonAndGroup);
        var person = await first.ResolveAsync("--author", "Platform", false, TestContext.Current.CancellationToken);
        var (second, secondIdentity) = Run(store, PlatformPersonAndGroup);

        await Should.ThrowAsync<RejectedException>(() => Reviewer(second, "Platform"));

        person.Id.ShouldBe("id-4");
        secondIdentity.Calls.ShouldBe(["Search(General,Platform)"]);
    }

    [Fact]
    public async Task ResolveAsync_GroupsOnThenPeopleOnly_DoesNotShareTheEntry()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, (_, _) => [Platform()]);
        await Reviewer(first, "Contoso Platform");
        var (second, secondIdentity) = Run(store, (_, _) => [Platform()]);

        var ex = await Should.ThrowAsync<RejectedException>(
            () => second.ResolveAsync("--author", "Contoso Platform", false, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--author 'Contoso Platform' matches only groups. Groups are not accepted; name a person.");
        secondIdentity.Calls.ShouldBe(["Search(General,Contoso Platform)"]);
    }

    private static IdentityMatch Platform() => Group("id-3", @"[Fabrikam]\Contoso Platform");

    private static IReadOnlyList<IdentityMatch> PlatformPersonAndGroup(IdentitySearchFilter filter, string value)
        => [Group("id-3", @"[Fabrikam]\Platform"), Match("id-4", "Platform", @"CONTOSO\svc-platform")];

    private static Task<Identity> Reviewer(IdentityResolver resolver, string value)
        => resolver.ResolveAsync("--reviewer", value, true, TestContext.Current.CancellationToken);

    private static IdentityMatch Group(string id, string name) => Match(id, name, null, isContainer: true);

    private static IReadOnlyList<IdentityMatch> JohnRoe(IdentitySearchFilter filter, string value)
        => [Match("id-2", "John Roe", @"CONTOSO\jroe")];

    private static (IdentityResolver Resolver, ScriptedIdentityClient Identity) Run(
        InMemoryPersonStore store,
        Func<IdentitySearchFilter, string, IReadOnlyList<IdentityMatch>>? search = null,
        string pat = "test-pat",
        string collection = "COL",
        TimeProvider? clock = null)
    {
        var identity = new ScriptedIdentityClient();
        if (search is not null)
        {
            identity.Search = search;
        }

        var cache = TestPersonCache.Create(store, clock, collection: collection, pat: pat);
        return (new IdentityResolver(identity, cache), identity);
    }

    private static IdentityMatch Match(string id, string name, string? unique, bool isContainer = false)
        => new(id, name, unique, isContainer);
}
