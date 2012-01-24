using System;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class WebProjectSystem : VsProjectSystem
    {
        public WebProjectSystem(Project project)
            : base(project)
        {
        }

        public override bool IsSupportedFile(string path)
        {
            if ("app.config".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }
    }
}
