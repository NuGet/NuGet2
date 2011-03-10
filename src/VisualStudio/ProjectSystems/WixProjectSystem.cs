using System;
using EnvDTE;

namespace NuGet.VisualStudio.ProjectSystems {
    public class WixProjectSystem : VsProjectSystem {
        public WixProjectSystem(Project project)
            : base(project) {
        }

        public override void AddReference(string referencePath, System.IO.Stream stream) {
            // References aren't allowed for WiX projects
        }

        public override void RemoveReference(string name) {
            // References aren't allowed for WiX projects
        }

        public override bool ReferenceExists(string name) {
            // References aren't allowed for WiX projects
            return true;
        }

        protected override void AddGacReference(string name) {
            // References aren't allowed for WiX projects
        }

        public override bool IsSupportedFile(string path)
        {
            // TODO: Determine if any file types are not supported
            return base.IsSupportedFile(path);
        }
    }
}
