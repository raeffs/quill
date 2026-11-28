using Quill.Core.Models;
using Quill.Tests.Shared.Fakes;
using Shouldly;

namespace Quill.Core.Tests;

public class PersonCacheTests
{
    private static readonly Identity John = new("id-2", "John Roe", @"CONTOSO\jroe", false);

    private static readonly Identity Jane = new("id-1", "Jane Doe", null, false);

    [Fact]
    public void Find_NothingSaved_ReturnsNull() =>
        TestPersonCache.Create().Find("John Roe").ShouldBeNull();

    [Fact]
    public void Find_AfterRemember_ReturnsThePerson()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBe(John);
    }

    [Theory]
    [InlineData("john roe")]
    [InlineData(" John Roe ")]
    [InlineData("JOHN ROE")]
    public void Find_IgnoresCaseAndOuterSpace(string name)
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        TestPersonCache.Create(store, clock).Find(name).ShouldBe(John);
    }

    [Fact]
    public void Find_After24Hours_ReturnsNull()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        clock.Now += PersonCache.MaxAge - TimeSpan.FromTicks(1);
        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBe(John);

        clock.Now += TimeSpan.FromTicks(1);
        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBeNull();
    }

    [Fact]
    public void Find_DoesNotExtendTheAge()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        clock.Now += TimeSpan.FromHours(23);
        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBe(John);
        clock.Now += TimeSpan.FromHours(1);
        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBeNull();

        store.Saves.ShouldBe(1);
    }

    [Fact]
    public void Find_SavedAtInTheFuture_ReturnsNull()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        clock.Now -= TimeSpan.FromMinutes(1);

        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBeNull();
    }

    [Fact]
    public void Find_OtherServerUrl_ReturnsNull()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        TestPersonCache.Create(store, clock, serverUrl: "https://other.example.com").Find("John Roe").ShouldBeNull();
    }

    [Fact]
    public void Find_OtherCollection_ReturnsNull()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        TestPersonCache.Create(store, clock, collection: "OTHER").Find("John Roe").ShouldBeNull();
    }

    [Fact]
    public void Find_LoadsTheStoreOnceForARun()
    {
        var store = new InMemoryPersonStore();
        var cache = TestPersonCache.Create(store);

        cache.Find("John Roe");
        cache.Find("Jane Doe");

        store.Loads.ShouldBe(1);
    }

    [Fact]
    public void FindMe_AfterRememberMe_ReturnsTheCurrentUser()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).RememberMe(new CurrentUser("user-1", "Jane Doe"));

        TestPersonCache.Create(store, clock).FindMe().ShouldBe(new CurrentUser("user-1", "Jane Doe"));
    }

    [Fact]
    public void FindMe_OtherPat_ReturnsNull()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).RememberMe(new CurrentUser("user-1", "Jane Doe"));

        TestPersonCache.Create(store, clock, pat: "other-pat").FindMe().ShouldBeNull();
    }

    [Fact]
    public void FindMe_DoesNotReadAPersonNamedLikeTheHash()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        var scope = PersonCacheScope.For("https://devops.example.com", "COL", "test-pat");
        new PersonCache(store, scope, clock).Remember(scope.PatHash, John);

        new PersonCache(store, scope, clock).FindMe().ShouldBeNull();
    }

    [Fact]
    public void Remember_KeepsWhatAnotherRunSaved()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        var first = TestPersonCache.Create(store, clock);
        var second = TestPersonCache.Create(store, clock);
        first.Find("John Roe");

        second.Remember("Jane Doe", Jane);
        first.Remember("John Roe", John);

        store.Entries.Count.ShouldBe(2);
        TestPersonCache.Create(store, clock).Find("Jane Doe").ShouldBe(Jane);
        TestPersonCache.Create(store, clock).Find("John Roe").ShouldBe(John);
    }

    [Fact]
    public void Remember_DropsExpiredEntries()
    {
        var store = new InMemoryPersonStore();
        var clock = new FixedClock();
        TestPersonCache.Create(store, clock).Remember("John Roe", John);

        clock.Now += TimeSpan.FromHours(25);
        TestPersonCache.Create(store, clock).Remember("Jane Doe", Jane);

        store.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public void Remember_WritesTheKeyInOneForm()
    {
        var store = new InMemoryPersonStore();

        TestPersonCache.Create(store).Remember(" john roe ", John);

        store.Entries.Keys.ShouldBe(["HTTPS://DEVOPS.EXAMPLE.COM|COL|person|JOHN ROE"]);
    }
}
