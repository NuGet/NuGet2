using System;
using NuGet.Client.Diagnostics;
using NuGet.Client.Installation;

namespace NuGet.Client.Interop
{
    internal class CoreInteropProjectManager : IProjectManager
    {
        private readonly InstallationTarget _target;
        private readonly IProjectSystem _projectSystem;
        private readonly ISharedPackageRepository _sharedRepo;
        private readonly IPackageRepository _refRepo;
        private readonly CoreInteropPackageManager _pacman;
        private readonly CoreInteropSourceRepository _sourceRepo;

        public IPackageRepository LocalRepository
        {
            get { return _refRepo; }
        }

        public IPackageManager PackageManager
        {
            get { return _pacman; }
        }

        public IProjectSystem Project
        {
            get { return _projectSystem; }
        }

        public IPackageConstraintProvider ConstraintProvider
        {
            get
            {
                return new CoreInteropConstraintProvider(_target.InstalledPackages);
            }
            set
            {
                NuGetTraceSources.CoreInterop.Error("setconstraintprovider", "Someone tried to set the constraint provider for an interop Project Manager! It won't work!!");
            }
        }

        public CoreInteropProjectManager(
            InstallationTarget target, 
            SourceRepository activeSource,
            IDependencyResolver2 dependencyResolver)
        {
            // Get the required features from the target
            _sharedRepo = target.GetRequiredFeature<ISharedPackageRepository>();
            _refRepo = target.GetRequiredFeature<IProjectManager>().LocalRepository;
            _projectSystem = target.TryGetFeature<IProjectSystem>();
            _target = target;

            _sourceRepo = new CoreInteropSourceRepository(activeSource);
            _pacman = new CoreInteropPackageManager(
                _sharedRepo,
                dependencyResolver,
                _sourceRepo);
        }

        #region Unimplemented stuff.

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

        public void Execute(PackageOperation operation)
        {
            System.Diagnostics.Debug.Assert(false, "Didn't expect this to be called!");
            throw new NotImplementedException();
        }

        // Suppress 'The event ... is never used' warning
#pragma warning disable 0067

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdding;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving;

#pragma warning restore 0067

        #endregion Unimplemented stuff.
    }
}