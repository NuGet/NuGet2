using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Providers {
    internal class SolutionUpdatesProvider : UpdatesProvider, IPackageOperationEventListener {

        private IVsPackageManager _activePackageManager;

        public SolutionUpdatesProvider(
            IPackageRepository localRepository,
            ResourceDictionary resources,
            IPackageRepositoryFactory packageRepositoryFactory,
            IPackageSourceProvider packageSourceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ProviderServices providerServices,
            IProgressProvider progressProvider,
            ISolutionManager solutionManager) :
            base(
                null,
                localRepository,
                resources,
                packageRepositoryFactory,
                packageSourceProvider,
                packageManagerFactory,
                providerServices,
                progressProvider,
                solutionManager) {
        }

        protected override bool ExecuteAfterLicenseAgreement(PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            _activePackageManager = activePackageManager;

            activePackageManager.UpdatePackage(
                item.PackageIdentity.Id, 
                item.PackageIdentity.Version, 
                updateDependencies: true, 
                logger: this, 
                packageOperationEventListener: this);

            return true;
        }

        protected override void ExecuteCommand(IProjectManager projectManager, PackageItem item, IVsPackageManager activePackageManager, IList<PackageOperation> operations) {
            Debug.Fail("This method should not get called.");
        }

        public void OnBeforeAddPackageReference(Project project) {
            RegisterPackageOperationEvents(
                _activePackageManager,
                _activePackageManager.GetProjectManager(project));
        }

        public void OnAfterAddPackageReference(Project project) {
            UnregisterPackageOperationEvents(
                _activePackageManager,
                _activePackageManager.GetProjectManager(project));
        }

        public void OnAddPackageReferenceError(Project project, Exception exception) {
            AddFailedProject(project, exception);
        }
    }
}