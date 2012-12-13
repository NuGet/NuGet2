using System;
using System.IO;
using System.Linq;
using EnvDTE;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    public class AzureCloudServiceProjectSystem : VsProjectSystem
    {
        private const string RootNamespace = "RootNamespace";
        private const string OutputName = "OutputName";
        private const string DefaultNamespace = "Azure";

       public AzureCloudServiceProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
        }

        public override bool IsBindingRedirectSupported
        {
            get
            {
                // Binding redirect just doesn't make sense in Azure project
                return false;
            }
        }

        public override void AddReference(string referencePath, Stream stream)
        {
            // References aren't allowed for Azure projects
        }

        protected override void AddFileToContainer(string fullPath, ProjectItems container)
        {
            // You can't add files to an Azure project
        }

        public override void AddFile(string path, Stream stream)
        {
            Project.EnsureCheckedOutIfExists(this, path);
            BaseFileSystem.AddFile(path, stream);
        }

        public override void DeleteDirectory(string path, bool recursive = false)
        {
            // Only delete this folder if it is empty and we didn't specify that we want to recurse
            if (!recursive && (base.GetFiles(path, "*.*", recursive).Any() || base.GetDirectories(path).Any()))
            {
                Logger.Log(MessageLevel.Warning, VsResources.Warning_DirectoryNotEmpty, path);
                return;
            }

            // Workaround for TFS update issue. If we're bound to TFS, do not try and delete directories.
            if (!(BaseFileSystem is ISourceControlFileSystem))
            {
                BaseFileSystem.DeleteDirectory(path, recursive);
                Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFolder, path);
            }
        }

        public override void DeleteFile(string path)
        {
            BaseFileSystem.DeleteFile(path);

            string folderPath = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(folderPath))
            {
                Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFileFromFolder, Path.GetFileName(path), folderPath);
            }
            else
            {
                Logger.Log(MessageLevel.Debug, VsResources.Debug_RemovedFile, Path.GetFileName(path));
            }
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