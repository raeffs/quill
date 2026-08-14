using Quill.Core.Config;
using Quill.Core.Models;

namespace Quill.Cli;

internal interface IQuillConfigProvider
{
    QuillConfig Load();
}

internal sealed class QuillConfigProvider : IQuillConfigProvider
{
    public QuillConfig Load() => ConfigLoader.Load(Directory.GetCurrentDirectory());
}
