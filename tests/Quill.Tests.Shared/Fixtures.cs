namespace Quill.Tests.Shared;

public static class Fixtures
{
    public static string Read(string fileName)
    {
        using var stream = typeof(Fixtures).Assembly.GetManifestResourceStream(fileName)
            ?? throw new InvalidOperationException($"Fixture '{fileName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
