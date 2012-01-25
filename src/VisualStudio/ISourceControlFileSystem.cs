using System.Collections.Generic;

namespace NuGet.VisualStudio
{
    public interface ISourceControlFileSystem : IFileSystem
    {
        bool IsSourceControlBound(string path);
        bool BindToSourceControl(IEnumerable<string> paths);
    }
}
