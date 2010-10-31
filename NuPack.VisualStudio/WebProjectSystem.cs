using System;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio {
    public class WebProjectSystem : VsProjectSystem {
        public WebProjectSystem(Project project)
            : base(project) {
        }

        public override bool IsSupportedFile(string path) {
            if (Path.GetFileName(path).Equals("app.config", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
            return true;
        }
    }
}
