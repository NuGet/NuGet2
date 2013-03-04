using System;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using NuGet.VisualStudio;

namespace NuGet.TeamFoundationServer
{
    public class TfsSourceControlFileSystemProvider : ISourceControlFileSystemProvider
    {
        private const string TfsProviderName = "{4CA58AB2-18FA-4F8D-95D4-32DDF27D184C}";

        public ISourceControlFileSystem GetFileSystem(string path, SourceControlBindings binding)
        {
            // Return null if this binding isn't for us then return null
            if ((ProviderConstants.SupportedVsVersion != VsVersionHelper.VsMajorVersion) ||
                String.IsNullOrEmpty(binding.ProviderName) ||
                !binding.ProviderName.Equals(TfsProviderName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return GetFileSystemInternal(path, binding);
        }

        private static ISourceControlFileSystem GetFileSystemInternal(string path, SourceControlBindings binding)
        {
            TfsTeamProjectCollection projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(binding.ServerName));
            var versionControl = projectCollection.GetService<VersionControlServer>();
            Workspace workspace = versionControl.TryGetWorkspace(binding.LocalBinding);

            return new TfsFileSystem(workspace, path);
        }
    }
}
