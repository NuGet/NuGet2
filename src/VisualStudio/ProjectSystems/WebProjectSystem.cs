using System;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class WebProjectSystem : VsProjectSystem
    {
        public WebProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
        }

        public override bool IsSupportedFile(string path)
        {
            string fileName = Path.GetFileName(path);
            return !(fileName.StartsWith("app.", StringComparison.OrdinalIgnoreCase) &&
                     fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase));
        }
    }
}