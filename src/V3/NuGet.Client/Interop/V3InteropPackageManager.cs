using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Client.Interop
{
    class V3InteropPackageManager : IPackageManager
    {
        public IFileSystem FileSystem
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ISharedPackageRepository LocalRepository
        {
            get { throw new NotImplementedException(); }
        }

        public ILogger Logger
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public DependencyVersion DependencyVersion
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public IPackageRepository SourceRepository
        {
            get { throw new NotImplementedException(); }
        }

        public IPackagePathResolver PathResolver
        {
            get { throw new NotImplementedException(); }
        }

        // Suppress 'The event ... is never used' warning
#pragma warning disable 0067
        public event EventHandler<PackageOperationEventArgs> PackageInstalled;

        public event EventHandler<PackageOperationEventArgs> PackageInstalling;

        public event EventHandler<PackageOperationEventArgs> PackageUninstalled;

        public event EventHandler<PackageOperationEventArgs> PackageUninstalling;
#pragma warning restore 0067

        public void Execute(PackageOperation operation)
        {
            throw new NotImplementedException();
        }

        public bool IsProjectLevel(IPackage package)
        {
            throw new NotImplementedException();
        }

        public bool BindingRedirectEnabled
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void AddBindingRedirects(IProjectManager projectManager)
        {
            throw new NotImplementedException();
        }

        public IPackage LocatePackageToUninstall(IProjectManager projectManager, string id, SemanticVersion version)
        {
            throw new NotImplementedException();
        }
    }
}
