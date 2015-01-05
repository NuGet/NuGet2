using System;
using System.Diagnostics;
using NuGet.Client.Diagnostics;
using NuGet.V3Interop;

namespace NuGet.Client.Interop
{
    internal class CoreInteropPackageManager : IPackageManager
    {
        private ISharedPackageRepository _sharedRepo;
        private CoreInteropSourceRepository _sourceRepo;

        public ISharedPackageRepository LocalRepository
        {
            get { return _sharedRepo; }
        }

        public IPackageRepository SourceRepository
        {
            get { return _sourceRepo; }
        }

        public IDependencyResolver2 DependencyResolver
        {
            get;
            private set;
        }

        public CoreInteropPackageManager(
            ISharedPackageRepository sharedRepo,
            IDependencyResolver2 dependencyResolver,
            CoreInteropSourceRepository sourceRepo)
        {
            _sharedRepo = sharedRepo;
            _sourceRepo = sourceRepo;
            DependencyResolver = dependencyResolver;
        }

        public bool IsProjectLevel(IPackage package)
        {
            NuGetTraceSources.CoreInterop.Verbose("isprojectlevel", "IsProjectLevel? {0} {1}", package.Id, package.Version);

            var v3Package = package as IV3PackageMetadata;
            Debug.Assert(v3Package != null, "This method should only have been called with a CoreInteropPackage...");
            return v3Package.PackageTarget.HasFlag(PackageTargets.Project);
        }

        #region Unimplemented Stuff

        public IFileSystem FileSystem
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
            set
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
        }

        public ILogger Logger
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
            set
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
        }

        public DependencyVersion DependencyVersion
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
            set
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
        }

        public IPackagePathResolver PathResolver
        {
            get { System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!"); throw new NotImplementedException(); }
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
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public bool BindingRedirectEnabled
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
            set
            {
                System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
                throw new NotImplementedException();
            }
        }

        public void AddBindingRedirects(IProjectManager projectManager)
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        public IPackage LocatePackageToUninstall(IProjectManager projectManager, string id, SemanticVersion version)
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        #endregion Unimplemented Stuff
    }
}