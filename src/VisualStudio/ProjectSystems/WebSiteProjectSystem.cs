using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using EnvDTE;
using NuGet.VisualStudio.Resources;
using System.Globalization;

namespace NuGet.VisualStudio {
    public class WebSiteProjectSystem : WebProjectSystem {
        private const string RootNamespace = "RootNamespace";
        private const string DefaultNamespace = "ASP";

        public WebSiteProjectSystem(Project project)
            : base(project) {
        }

        public override string ProjectName {
            get {
                return Path.GetFileName(Path.GetDirectoryName(Project.FullName));
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public override void AddReference(string referencePath, Stream stream) {
            string name = Path.GetFileNameWithoutExtension(referencePath);

            try {
                // Add a reference to the project
                Project.Object.References.AddFromFile(PathUtility.GetAbsolutePath(Root, referencePath));

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddReference, name, ProjectName);
            }
            catch (Exception e) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, VsResources.FailedToAddReference, name), e);
            }
        }

        protected override void AddGacReference(string name) {
            Project.Object.References.AddFromGAC(name);
        }

        public override dynamic GetPropertyValue(string propertyName) {
            if (propertyName.Equals(RootNamespace, StringComparison.OrdinalIgnoreCase)) {
                return DefaultNamespace;
            }
            return base.GetPropertyValue(propertyName);
        }

        protected override bool ExcludeFile(string path) {
            // Exclude nothing from website projects
            return false;
        }
    }
}
