namespace Quill.Core.Models;

public sealed record PersonFilter
{
    private PersonFilter(Identity? person) => Person = person;

    public static PersonFilter Me { get; } = new((Identity?)null);

    public Identity? Person { get; }

    public static PersonFilter Of(Identity person)
    {
        ArgumentNullException.ThrowIfNull(person);
        return new PersonFilter(person);
    }
}
