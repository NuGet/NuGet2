using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Common
{
    public class MSBuildPackageManager : PackageManager
    {
        MSBuildProjectSystem _project;
        ProjectManager _projectManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceRepository"></param>
        /// <param name="pathResolver"></param>
        /// <param name="fileSystem">The file system where the packages are installed.</param>
        /// <param name="localRepository"></param>
        /// <param name="projectFile"></param>
        /// <param name="solutionDirectory"></param>
        public MSBuildPackageManager(IPackageRepository sourceRepository, 
            IPackagePathResolver pathResolver, 
            IFileSystem fileSystem, 
            IPackageRepository localRepository, string projectFile, string solutionDirectory)
            : base(sourceRepository, pathResolver, fileSystem, localRepository)
        {
            _project = MSBuildProjectSystem.Create(projectFile);
            var sharedPackageRepository = new SharedPackageRepository(
                pathResolver,
                fileSystem,
                new PhysicalFileSystem(Path.Combine(solutionDirectory, ".nuget")));

            var packageReferenceRepository = new PackageReferenceRepository(
                _project,
                _project.ProjectName,
                sharedPackageRepository);
            _projectManager = new ProjectManager(sourceRepository, pathResolver, _project, packageReferenceRepository);
        }

        public override void InstallPackage(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            base.InstallPackage(package, ignoreDependencies, allowPrereleaseVersions);
            _projectManager.AddPackageReference(package, ignoreDependencies, allowPrereleaseVersions);
            _project.Save();
        }

        public override void UninstallPackage(IPackage package, bool forceRemove, bool removeDependencies)
        {
            base.UninstallPackage(package, forceRemove, removeDependencies);
            _projectManager.RemovePackageReference(package, forceRemove, removeDependencies);
            _project.Save();
        }
    }
}
