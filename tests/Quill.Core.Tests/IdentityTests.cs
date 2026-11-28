using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests;

public class IdentityTests
{
    [Fact]
    public void AssignedTo_WithUniqueName_IsTheCombinedForm()
    {
        new Identity("id", "Jane Doe", @"CONTOSO\jdoe", false).AssignedTo.ShouldBe(@"Jane Doe <CONTOSO\jdoe>");
    }

    [Fact]
    public void AssignedTo_WithoutUniqueName_IsTheDisplayName()
    {
        new Identity("id", "Jane Doe", null, false).AssignedTo.ShouldBe("Jane Doe");
    }

    [Fact]
    public void AssignedTo_Group_Throws()
    {
        var group = new Identity("id-3", @"[Fabrikam]\Contoso Platform", null, true);

        Should.Throw<InvalidOperationException>(() => group.AssignedTo);
    }
}
