namespace Quill.Core.Models;

public sealed record PersonFilter
{
    private PersonFilter(Person? person) => Person = person;

    public static PersonFilter Me { get; } = new((Person?)null);

    public Person? Person { get; }

    public static PersonFilter Of(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);
        return new PersonFilter(person);
    }
}
