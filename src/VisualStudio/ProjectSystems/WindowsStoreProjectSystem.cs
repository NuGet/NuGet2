using System;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class WindowsStoreProjectSystem : VsProjectSystem
    {
        public WindowsStoreProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
        }

        public override bool IsSupportedFile(string path)
        {
            string fileName = Path.GetFileName(path);
            if (fileName.Equals("app.config", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return base.IsSupportedFile(path);
        }

        public override bool IsBindingRedirectSupported
        {
            get
            {
                return false;
            }
        }
    }
}