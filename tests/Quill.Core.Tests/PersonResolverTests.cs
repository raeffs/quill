using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests;

public class PersonResolverTests
{
    [Theory]
    [InlineData("@me")]
    [InlineData("@Me")]
    [InlineData(" @me ")]
    public async Task ResolveAsync_Me_UsesConnectionDataAndNoSearch(string value)
    {
        var identity = new ScriptedIdentityClient();

        var person = await new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", value, TestContext.Current.CancellationToken);

        person.ShouldBe(new Person("user-1", "Jane Doe", null));
        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task ResolveAsync_MeTwice_CallsConnectionDataOnce()
    {
        var identity = new ScriptedIdentityClient();
        var resolver = new PersonResolver(identity, TestPersonCache.Create());

        await resolver.ResolveAsync("--reviewer", "@me", TestContext.Current.CancellationToken);
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

        var person = await new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        person.ShouldBe(new Person("id-2", "John Roe", @"CONTOSO\jroe"));
        identity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_Email_SearchesMailAddress()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", @"CONTOSO\jroe")],
        };

        await new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "jroe@contoso.com", TestContext.Current.CancellationToken);

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

        var person = await new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        person.Id.ShouldBe("id-2");
    }

    [Fact]
    public async Task ResolveAsync_NoHits_Throws()
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<RejectedException>(
            () => new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "Jane", TestContext.Current.CancellationToken));

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
            () => new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--reviewer", "Contoso Team", TestContext.Current.CancellationToken));

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
            () => new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--reviewer", "Jane Doe", TestContext.Current.CancellationToken));

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

        var person = await new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        person.Id.ShouldBe("id-2");
    }

    [Fact]
    public async Task ResolveAsync_NoUniqueName_PersonHasNone()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", null)],
        };

        var person = await new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        person.ShouldBe(new Person("id-2", "John Roe", null));
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
            () => new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken));

        ex.Message.ShouldBe(@"--assignee 'John Roe' matches 2 people: John Roe, John Roe (CONTOSO\jroe2). Pass the unique name.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ResolveAsync_Blank_ThrowsBeforeAnyRequest(string value)
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<UsageException>(
            () => new PersonResolver(identity, TestPersonCache.Create()).ResolveAsync("--assignee", value, TestContext.Current.CancellationToken));

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

        var filter = await new PersonResolver(identity, TestPersonCache.Create()).ResolveForQueryAsync("--assignee", value, TestContext.Current.CancellationToken);

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

        var filter = await new PersonResolver(identity, TestPersonCache.Create()).ResolveForQueryAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        filter.Person.ShouldBe(new Person("id-2", "John Roe", @"CONTOSO\jroe"));
        identity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveForQueryAsync_Blank_ThrowsUsageAndSendsNoRequest()
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<UsageException>(() =>
            new PersonResolver(identity, TestPersonCache.Create()).ResolveForQueryAsync("--assignee", " ", TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--assignee must not be empty.");
        identity.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_NameResolvedByAnEarlierRun_SendsNoSearch()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, JohnRoe);
        await first.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);
        var (second, secondIdentity) = Run(store);

        var person = await second.ResolveAsync("--assignee", "john roe", TestContext.Current.CancellationToken);

        person.ShouldBe(new Person("id-2", "John Roe", @"CONTOSO\jroe"));
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

        filter.ShouldBe(PersonFilter.Of(new Person("id-2", "John Roe", @"CONTOSO\jroe")));
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

        var person = await second.ResolveAsync("--assignee", "@me", TestContext.Current.CancellationToken);

        person.ShouldBe(new Person("user-1", "Jane Doe", null));
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
        await Should.ThrowAsync<RejectedException>(() => first.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken));
        var (second, secondIdentity) = Run(store, JohnRoe);

        await second.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_SeveralMatched_AsksAgainOnTheNextRun()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, (_, _) => [Match("id-1", "John Roe", @"CONTOSO\jroe"), Match("id-2", "John Roe", @"CONTOSO\jroe2")]);
        await Should.ThrowAsync<RejectedException>(() => first.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken));
        var (second, secondIdentity) = Run(store, JohnRoe);

        await second.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_OnlyGroupsMatched_AsksAgainOnTheNextRun()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, (_, _) => [Match("id-3", "John Roe", @"CONTOSO\team", isContainer: true)]);
        await Should.ThrowAsync<RejectedException>(() => first.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken));
        var (second, secondIdentity) = Run(store, JohnRoe);

        await second.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_After24Hours_AsksAgain()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        var (first, _) = Run(store, JohnRoe, clock: clock);
        await first.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);
        clock.Now += PersonCache.MaxAge;
        var (second, secondIdentity) = Run(store, JohnRoe, clock: clock);

        await second.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public async Task ResolveAsync_OtherCollection_AsksTheServer()
    {
        var store = new InMemoryPersonStore();
        var (first, _) = Run(store, JohnRoe);
        await first.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);
        var (second, secondIdentity) = Run(store, JohnRoe, collection: "OTHER");

        await second.ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        secondIdentity.Calls.ShouldBe(["Search(General,John Roe)"]);
    }

    [Fact]
    public void AssignedTo_WithUniqueName_IsTheCombinedForm()
    {
        new Person("id", "Jane Doe", @"CONTOSO\jdoe").AssignedTo.ShouldBe(@"Jane Doe <CONTOSO\jdoe>");
    }

    [Fact]
    public void AssignedTo_WithoutUniqueName_IsTheDisplayName()
    {
        new Person("id", "Jane Doe", null).AssignedTo.ShouldBe("Jane Doe");
    }

    private static IReadOnlyList<IdentityMatch> JohnRoe(IdentitySearchFilter filter, string value)
        => [Match("id-2", "John Roe", @"CONTOSO\jroe")];

    private static (PersonResolver Resolver, ScriptedIdentityClient Identity) Run(
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
        return (new PersonResolver(identity, cache), identity);
    }

    private static IdentityMatch Match(string id, string name, string? unique, bool isContainer = false)
        => new(id, name, unique, isContainer);
}
