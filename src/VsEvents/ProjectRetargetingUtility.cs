using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.VsEvents
{
    internal static class ProjectRetargetingUtility
    {
        /// <summary>
        /// Get the list of packages to be reinstalled in the project. This can be run right after a project is retargeted or during every build
        /// </summary>
        /// <remarks>
        /// 1. Using the project instance, check if packages.config file exists. If NOT, RETURN empty list
        /// 2. If it does, get the full path of packages.config and create a PackageReferenceFile instance of packages.config
        /// 3. Get the list of PackageReference objects using PackageReferenceFile. Each PackageReference contains the package ID, version and the targetFramework
        /// 4. Now, for every PackageReference object, the IPackage instance for the INSTALLED PACKAGE can be obtained using LocalRepository.FindPackage(string packageid)
        ///    4.a) Note that the LocalRepository is obtained from IVsPackageManager created using the global service of IVsPackageManagerFactory
        /// 5. Before iterating over the list of PackageReferences and obtaining IPackages, Get the NEW targetframework of the project
        /// 6. Now, for every packagereference from the list obtained in Step-3, determine if it needs to be reinstalled using the following information in Step 7
        ///    6.a) CURRENT target framework of the project
        ///    6.b) target framework of the package
        ///    6.c) Files of the package obtained via IPackage.GetFiles()
        /// 7. Get Compatible items for old and new targetframework using IPackage.GetFiles() AND compare the lists. If they are not the same, that package needs to be reinstalled
        /// </remarks>
        /// <param name="project">Project for which packages to be reinstalled are determined</param>
        /// <returns>The list of packages to be reinstalled. If packages.config does not exist in the project or is not part of the project, empty list is returned</returns>
        internal static IList<IPackage> GetPackagesToBeReinstalled(Project project)
        {
            return GetPackagesToBeReinstalled(project, GetLocalRepository(ServiceLocator.GetInstance<IVsPackageManagerFactory>()));
        }

        /// <summary>
        /// Gets the list of packages to be reinstalled based on the Project instance and the localRepository to find packages in
        /// </summary>
        /// <param name="project">Project for which packages to be reinstalled are determined</param>
        /// <param name="localRepository">Local Repository from which packages listed in project's packages.config are loaded</param>
        /// <returns></returns>
        internal static IList<IPackage> GetPackagesToBeReinstalled(Project project, IPackageRepository localRepository)
        {
            Debug.Assert(project != null);

            // First call to VsUtility.PackageConfigExists(project) checks if there is a packages.config file under the project folder, Otherwise, return emtpy list
            // If present, then call VsUtility.IsNuGetInUse to see if NuGet is used in the project. The second call might result in loading of NuGet.VisualStudio.dll
            if (localRepository != null && VsUtility.PackagesConfigExists(project) && project.IsNuGetInUse())
            {
                return GetPackagesToBeReinstalled(project.GetTargetFrameworkName(), GetPackageReferences(project), localRepository);
            }

            return new List<IPackage>();
        }

        /// <summary>
        /// Get the list of packages to be reinstalled given the project framework, packageReferences and the localRepository
        /// </summary>
        /// <param name="projectFramework">Current target framework of the project</param>
        /// <param name="packageReferences">List of package references in the project from which packages to be reinstalled are determined</param>
        /// <param name="localRepository">Project for which packages to be reinstalled are determined</param>
        /// <returns></returns>
        internal static List<IPackage> GetPackagesToBeReinstalled(FrameworkName projectFramework, IEnumerable<PackageReference> packageReferences, IPackageRepository localRepository)
        {
            Debug.Assert(projectFramework != null);
            Debug.Assert(packageReferences != null);
            Debug.Assert(localRepository != null);

            List<IPackage> packagesToBeReinstalled = new List<IPackage>();

            foreach (PackageReference packageReference in packageReferences)
            {
                IPackage package = localRepository.FindPackage(packageReference.Id);
                if (package != null && ShouldPackageBeReinstalled(projectFramework, packageReference.TargetFramework, package))
                {
                    packagesToBeReinstalled.Add(package);
                }
            }

            return packagesToBeReinstalled;
        }

        /// <summary>
        /// Marks the packages to be reinstalled on the projects' packages.config
        /// </summary>
        internal static void MarkPackagesForReinstallation(Project project, IList<IPackage> packagesToBeReinstalled)
        {
            Debug.Assert(project != null);
            Debug.Assert(packagesToBeReinstalled != null);

            PackageReferenceFile packageReferenceFile = GetPackageReferenceFile(project);
            if (packageReferenceFile != null)
            {
                MarkPackagesForReinstallation(packageReferenceFile, packagesToBeReinstalled);
            }
        }

        /// <summary>
        /// Marks the packages in packageReferenceFile for reinstallation based on packagesToBeReinstalled
        /// </summary>
        internal static void MarkPackagesForReinstallation(PackageReferenceFile packageReferenceFile, IList<IPackage> packagesToBeReinstalled)
        {
            Debug.Assert(packageReferenceFile != null);
            Debug.Assert(packagesToBeReinstalled != null);

            IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();

            foreach (PackageReference packageReference in packageReferences)
            {
                bool requireReinstallation = packagesToBeReinstalled.Any(p => p.Id.Equals(packageReference.Id, StringComparison.OrdinalIgnoreCase));
                packageReferenceFile.MarkEntryForReinstallation(packageReference.Id, packageReference.Version, packageReference.TargetFramework, requireReinstallation);
            }
        }

        /// <summary>
        /// Returns a list of package references that were marked for reinstallation in packages.config of the project
        /// </summary>
        internal static IList<PackageReference> GetPackageReferencesMarkedForReinstallation(Project project)
        {
            Debug.Assert(project != null);

            // First call to VsUtility.PackageConfigExists(project) checks if there is a packages.config file under the project folder, Otherwise, return emtpy list
            // If present, then call VsUtility.IsNuGetInUse to see if NuGet is used in the project. The second call might result in loading of NuGet.VisualStudio.dll
            if (VsUtility.PackagesConfigExists(project) && project.IsNuGetInUse())
            {
                PackageReferenceFile packageReferenceFile = GetPackageReferenceFile(project);
                Debug.Assert(packageReferenceFile != null);

                IEnumerable<PackageReference> packageReferences = packageReferenceFile.GetPackageReferences();
                return packageReferences.Where(p => p.RequireReinstallation).ToList();
            }

            return new List<PackageReference>();
        }

        internal static IPackageRepository GetLocalRepository(IVsPackageManagerFactory packageManagerFactory)
        {
            if (packageManagerFactory == null)
            {
                throw new ArgumentNullException("packageManagerFactory");
            }

            IPackageRepository localRepository = null;
            try
            {
                var packageManager = packageManagerFactory.CreatePackageManager();
                if (packageManager != null)
                {
                    localRepository = packageManager.LocalRepository;
                }
            }
            catch (InvalidOperationException)
            {
                localRepository = null;
            }
            return localRepository;
        }

        private static PackageReferenceFile GetPackageReferenceFile(Project project)
        {
            Debug.Assert(project != null);
            Tuple<string, string> packageReferenceFiles = VsUtility.GetPackageReferenceFileFullPaths(project);
            if (File.Exists(packageReferenceFiles.Item1))
            {
                return new PackageReferenceFile(packageReferenceFiles.Item1);
            }
            else if (File.Exists(packageReferenceFiles.Item2))
            {
                return new PackageReferenceFile(packageReferenceFiles.Item2);
            }
            return null;
        }

        /// <summary>
        /// Gets the package references of all the packages installed in the project
        /// </summary>
        /// <param name="project">Project from which the list of package references are obtained</param>
        /// <returns></returns>
        private static IEnumerable<PackageReference> GetPackageReferences(Project project)
        {
            Debug.Assert(project != null);

            PackageReferenceFile packageReferenceFile = GetPackageReferenceFile(project);
            Debug.Assert(packageReferenceFile != null);

            return packageReferenceFile.GetPackageReferences();
        }

        /// <summary>
        /// Determines if package needs to be reinstalled for the new target framework of the project
        /// </summary>
        /// <param name="newProjectFramework">current target framework of the project</param>
        /// <param name="oldProjectFramework">target framework of the project against which the package was installed</param>
        /// <param name="package">package for which reinstallation is being determined</param>
        /// <returns></returns>
        private static bool ShouldPackageBeReinstalled(FrameworkName newProjectFramework, FrameworkName oldProjectFramework, IPackage package)
        {
            Debug.Assert(newProjectFramework != null);
            Debug.Assert(oldProjectFramework != null);
            Debug.Assert(package != null);

            var packageFiles = package.GetFiles().ToList();
            IEnumerable<IPackageFile> oldProjectFrameworkCompatibleItems;
            IEnumerable<IPackageFile> newProjectFrameworkCompatibleItems;

            bool result = VersionUtility.TryGetCompatibleItems(oldProjectFramework, packageFiles, out oldProjectFrameworkCompatibleItems);

            if (!result)
            {
                // If the package is NOT compatible with oldProjectFramework, suggest reinstalling the package
                return true;
            }

            result = VersionUtility.TryGetCompatibleItems(newProjectFramework, packageFiles, out newProjectFrameworkCompatibleItems);

            if (!result)
            {
                // If the package is compatible with oldProjectFramework and not the newTargetFramework, suggest reinstaling the package
                return true;
            }

            return !Enumerable.SequenceEqual<IPackageFile>(oldProjectFrameworkCompatibleItems, newProjectFrameworkCompatibleItems);
        }
    }
}
