namespace NuPack.VisualStudio {
    using System;
    using System.Diagnostics;
    using EnvDTE;

    internal class VSProjectManager : ProjectManager {
        private VSPackageManager _vsPackageManager;
        public VSProjectManager(VSPackageManager vsPackageManager, IPackageAssemblyPathResolver assemblyPathResolver, Project project)
            : base(vsPackageManager.SolutionRepository, assemblyPathResolver, ProjectSystemFactory.CreateProjectSystem(project)) {
            _vsPackageManager = vsPackageManager;
        }

        public override void AddPackageReference(string packageId, Version version = null, bool ignoreDependencies = false) {
            // Make sure the package is installed before we add a reference to it
            _vsPackageManager.InstallPackage(packageId, version, ignoreDependencies);

            base.AddPackageReference(packageId, version, ignoreDependencies);
        }

        protected override void RemovePackageReference(IPackage package, bool force, bool removeDependencies) {
            base.RemovePackageReference(package, force, removeDependencies);

            _vsPackageManager.OnPackageReferenceRemoved(package, force, removeDependencies);            
        }

        public override void UpdatePackageReference(string packageId, Version version = null, bool updateDependencies = true) {
            IPackage oldPackage = GetPackageReference(packageId);
            // Only install the package to the solution if the project has a reference to it.
            if (oldPackage != null) {
                _vsPackageManager.InstallPackage(packageId, version, !updateDependencies);
            }

            base.UpdatePackageReference(packageId, version, updateDependencies);

            // If oldPackage is null then the above code will fail so we don't need to check
            Debug.Assert(oldPackage != null);
            _vsPackageManager.OnPackageReferenceRemoved(oldPackage, removeDependencies: updateDependencies);
        }
    }
}
