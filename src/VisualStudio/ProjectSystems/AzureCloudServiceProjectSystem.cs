using System;
using System.IO;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class AzureCloudServiceProjectSystem : VsProjectSystem
    {
       public AzureCloudServiceProjectSystem( Project project, IFileSystemProvider fileSystemProvider )
            : base(project, fileSystemProvider)
        {
        }

        private const string RootNamespace = "RootNamespace";
        private const string OutputName = "OutputName";
        private const string DefaultNamespace = "Azure";

        public override bool IsBindingRedirectSupported
        {
            get
            {
                // Binding redirect just doesn't make sense in Azure project
                return false;
            }
        }

        public override void AddReference(string referencePath, System.IO.Stream stream)
        {
            // References aren't allowed for Azure projects
        }

        protected override void AddFileToContainer( string fullPath, ProjectItems container )
        {
            // You can't add files to an Azure project
        }

        protected override void AddFileToProject(string path)
        {
            // You can't add files to an Azure project
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            var fileSystem = new PhysicalFileSystem( Root );
            fileSystem.DeleteDirectory( path, recursive ); 
        }

        public override void DeleteFile( string path )
        {
            var fileSystem = new PhysicalFileSystem( Root );
            fileSystem.DeleteFile( path ); 
        }

        public override void RemoveReference(string name)
        {
            // References aren't allowed for Azure projects
        }

        public override bool ReferenceExists(string name)
        {
            // References aren't allowed for Azure projects
            return true;
        }

        protected override void AddGacReference(string name)
        {
            // GAC references aren't allowed for Azure projects
        }

        public override bool IsSupportedFile(string path)
        {
            string fileName = Path.GetFileName(path);

            bool isWebConfigFile = (fileName.StartsWith("web.", StringComparison.OrdinalIgnoreCase) &&
                                    fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase));

            bool isAppConfigFile = (fileName.StartsWith("app.", StringComparison.OrdinalIgnoreCase) &&
                                    fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase));

            return !(isAppConfigFile || isWebConfigFile);
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
            // Exclude nothing from Azure projects
            return false;
        }
    }
}