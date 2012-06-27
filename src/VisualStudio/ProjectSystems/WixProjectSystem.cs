using System;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class WixProjectSystem : VsProjectSystem
    {
        public WixProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
        }

        private const string RootNamespace = "RootNamespace";
        private const string OutputName = "OutputName";
        private const string DefaultNamespace = "WiX";

        public override bool IsBindingRedirectSupported
        {
            get
            {
                // Binding redirect just doesn't make sense in Wix project
                return false;
            }
        }

        public override void AddReference(string referencePath, System.IO.Stream stream)
        {
            // References aren't allowed for WiX projects
        }

        public override void RemoveReference(string name)
        {
            // References aren't allowed for WiX projects
        }

        public override bool ReferenceExists(string name)
        {
            // References aren't allowed for WiX projects
            return true;
        }

        protected override void AddGacReference(string name)
        {
            // GAC references aren't allowed for WiX projects
        }

        public override bool IsSupportedFile(string path)
        {
            return true;
        }

        public override dynamic GetPropertyValue(string propertyName)
        {
            if (propertyName.Equals(RootNamespace, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return base.GetPropertyValue(OutputName);
                }
                catch
                {
                    return DefaultNamespace;
                }
            }
            return base.GetPropertyValue(propertyName);
        }

        protected override bool ExcludeFile(string path)
        {
            // Exclude nothing from WiX projects
            return false;
        }
    }
}