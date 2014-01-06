using System;
using System.ComponentModel.Composition;
using System.Globalization;
using EnvDTE;

namespace NuGet.VisualStudio
{
    [Export(typeof(IVsPackageUninstaller))]
    public class VsPackageUninstaller : IVsPackageUninstaller
    {
        private readonly IVsPackageManagerFactory _packageManagerFactory;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IPackageRepository _packageRepository;

        [ImportingConstructor]
        public VsPackageUninstaller(IVsPackageManagerFactory packageManagerFactory,
                                    IPackageRepository packageRepository,
                                    IScriptExecutor scriptExecutor)
        {
            _packageManagerFactory = packageManagerFactory;
            _scriptExecutor = scriptExecutor;
            _packageRepository = packageRepository;
        }

        public void UninstallPackage(Project project, string packageId, bool removeDependencies)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }
            if (String.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, CommonResources.Argument_Cannot_Be_Null_Or_Empty, "packageId"));
            }

            IVsPackageManager packageManager = _packageManagerFactory.CreatePackageManager(_packageRepository, useFallbackForDependencies: false);
            IProjectManager projectManager = packageManager.GetProjectManager(project);

            EventHandler<PackageOperationEventArgs> packageReferenceRemovingHandler = (sender, e) =>
            {
                _scriptExecutor.Execute(
                    e.InstallPath, 
                    PowerShellScripts.Uninstall, 
                    e.Package, 
                    project, 
                    projectManager.GetTargetFrameworkForPackage(packageId), 
                    NullLogger.Instance);
            };

            try
            {
                projectManager.PackageReferenceRemoving += packageReferenceRemovingHandler;
                packageManager.UninstallPackage(projectManager, packageId, version: null, forceRemove: false, removeDependencies: removeDependencies);
            }
            finally
            {
                projectManager.PackageReferenceRemoving -= packageReferenceRemovingHandler;
            }
        }
    }
}
