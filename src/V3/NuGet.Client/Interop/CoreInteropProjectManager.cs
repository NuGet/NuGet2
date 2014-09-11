using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Interop
{
    internal class CoreInteropProjectManager : IProjectManager
    {
        private readonly SourceRepository _source;
        private readonly InstallationTarget _target;
        private readonly CoreInteropSharedRepository _sharedRepo;
        private readonly CoreInteropPackageManager _pacman;

        public IPackageRepository LocalRepository
        {
            get { return _sharedRepo; }
        }

        public IPackageManager PackageManager
        {
            get { return _pacman; }
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

        public IProjectSystem Project
        {
            get { throw new NotImplementedException(); }
        }

        public IPackageConstraintProvider ConstraintProvider
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

        // Suppress 'The event ... is never used' warning
#pragma warning disable 0067
        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdding;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving;
#pragma warning restore 0067

        public CoreInteropProjectManager(InstallationTarget target, SourceRepository activeSource)
        {
            _target = target;
            _source = activeSource;

            _sharedRepo = new CoreInteropSharedRepository(target);
            _pacman = new CoreInteropPackageManager(_sharedRepo);
        }

        public void Execute(PackageOperation operation)
        {
            throw new NotImplementedException();
        }
    }
}
