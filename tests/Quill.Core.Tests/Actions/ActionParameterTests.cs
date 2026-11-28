using System.Reflection;
using Shouldly;

namespace Quill.Core.Tests.Actions;

public class ActionParameterTests
{
    [Fact]
    public void NoAction_TakesTheIdentityClient()
    {
        var offenders = typeof(PersonResolver).Assembly.GetTypes()
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith("Quill.Core.Actions", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(IIdentityClient)))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
