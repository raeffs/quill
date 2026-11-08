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

        var person = await new PersonResolver(identity).ResolveAsync("--assignee", value, TestContext.Current.CancellationToken);

        person.ShouldBe(new Person("user-1", "Jane Doe", null));
        identity.Calls.ShouldBe(["GetCurrentUser"]);
    }

    [Fact]
    public async Task ResolveAsync_MeTwice_CallsConnectionDataOnce()
    {
        var identity = new ScriptedIdentityClient();
        var resolver = new PersonResolver(identity);

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

        var person = await new PersonResolver(identity).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

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

        await new PersonResolver(identity).ResolveAsync("--assignee", "jroe@contoso.com", TestContext.Current.CancellationToken);

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

        var person = await new PersonResolver(identity).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        person.Id.ShouldBe("id-2");
    }

    [Fact]
    public async Task ResolveAsync_NoHits_Throws()
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<InvalidRequestException>(
            () => new PersonResolver(identity).ResolveAsync("--assignee", "Jane", TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--assignee 'Jane' matches nobody.");
    }

    [Fact]
    public async Task ResolveAsync_OnlyGroups_ThrowsAndSaysGroupsAreNotAccepted()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-3", "Contoso Team", @"CONTOSO\team", isContainer: true)],
        };

        var ex = await Should.ThrowAsync<InvalidRequestException>(
            () => new PersonResolver(identity).ResolveAsync("--reviewer", "Contoso Team", TestContext.Current.CancellationToken));

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

        var ex = await Should.ThrowAsync<InvalidRequestException>(
            () => new PersonResolver(identity).ResolveAsync("--reviewer", "Jane Doe", TestContext.Current.CancellationToken));

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

        var person = await new PersonResolver(identity).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

        person.Id.ShouldBe("id-2");
    }

    [Fact]
    public async Task ResolveAsync_NoUniqueName_PersonHasNone()
    {
        var identity = new ScriptedIdentityClient
        {
            Search = (_, _) => [Match("id-2", "John Roe", null)],
        };

        var person = await new PersonResolver(identity).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken);

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

        var ex = await Should.ThrowAsync<InvalidRequestException>(
            () => new PersonResolver(identity).ResolveAsync("--assignee", "John Roe", TestContext.Current.CancellationToken));

        ex.Message.ShouldBe(@"--assignee 'John Roe' matches 2 people: John Roe, John Roe (CONTOSO\jroe2). Pass the unique name.");
    }
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ResolveAsync_Blank_ThrowsBeforeAnyRequest(string value)
    {
        var identity = new ScriptedIdentityClient();

        var ex = await Should.ThrowAsync<InvalidRequestException>(
            () => new PersonResolver(identity).ResolveAsync("--assignee", value, TestContext.Current.CancellationToken));

        ex.Message.ShouldBe("--assignee must not be empty.");
        identity.Calls.ShouldBeEmpty();
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

    private static IdentityMatch Match(string id, string name, string? unique, bool isContainer = false)
        => new(id, name, unique, isContainer);
}
