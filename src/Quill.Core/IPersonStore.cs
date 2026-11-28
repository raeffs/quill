namespace Quill.Core;

public interface IPersonStore
{
    IReadOnlyDictionary<string, PersonEntry> Load();

    void Save(IReadOnlyDictionary<string, PersonEntry> entries);
}
