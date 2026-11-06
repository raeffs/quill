namespace Quill.Core;

public static class PipelineParameters
{
    public static bool TrySplit(string token, out string name, out string value)
    {
        var index = token.IndexOf('=');
        if (index <= 0)
        {
            name = string.Empty;
            value = string.Empty;
            return false;
        }

        name = token[..index];
        value = token[(index + 1)..];
        return true;
    }
}
