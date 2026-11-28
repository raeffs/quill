using System.Security.Cryptography;
using Shouldly;

namespace Quill.Core.Tests;

public class PersonCacheScopeTests
{
    [Fact]
    public void For_NormalisesTheServerUrlAndTheCollection() =>
        PersonCacheScope.For("https://DevOps.example.com/", " Col ", "p")
            .ShouldBe(PersonCacheScope.For("https://devops.example.com", "COL", "p"));

    [Fact]
    public void For_HashesThePat()
    {
        var scope = PersonCacheScope.For("https://devops.example.com", "COL", "test-pat");

        scope.PatHash.ShouldBe(Convert.ToHexString(SHA256.HashData("test-pat"u8)));
        scope.PatHash.ShouldNotContain("test-pat");
    }
}
