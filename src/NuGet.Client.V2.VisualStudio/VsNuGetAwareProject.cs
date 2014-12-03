#if VS14
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client.Installation;

using Microsoft.VisualStudio.ProjectSystem.Interop;
using System.IO;
using System.Runtime.Versioning;

namespace NuGet.Client.VisualStudio
{
    internal class VsNuGetAwareProject : NuGetAwareProject
    {
        private INuGetPackageManager _nugetAwareProject;

        public VsNuGetAwareProject(INuGetPackageManager nugetAwareProject)
        {
            _nugetAwareProject = nugetAwareProject;
        }

        public override Task InstallPackage(
            PackageIdentity id, 
            IEnumerable<FrameworkName> packageSupportedFrameworks,
            IExecutionLogger logger, 
            CancellationToken cancelToken)
        {
            var args = new Dictionary<string, object>();
            var projectFrameworks = _nugetAwareProject.GetSupportedFrameworksAsync(cancelToken).Result;
            args["Frameworks"] = projectFrameworks.Where(
                projectFramework =>
                    NuGet.VersionUtility.IsCompatible(
                        projectFramework,
                        packageSupportedFrameworks)).ToArray();
            var task = _nugetAwareProject.InstallPackageAsync(
                new NuGetPackageMoniker
                {
                    Id = id.Id,
                    Version = id.Version.ToString()
                },
                args,
                logger: null,
                progress: null,
                cancellationToken: cancelToken);
            return task;
        }

        public override Task UninstallPackage(PackageIdentity id, IExecutionLogger logger, CancellationToken cancelToken)
        {
            var args = new Dictionary<string, object>();
            var task = _nugetAwareProject.UninstallPackageAsync(
                new NuGetPackageMoniker
                {
                    Id = id.Id,
                    Version = id.Version.ToString()
                },
                args,
                logger: null,
                progress: null,
                cancellationToken: cancelToken);
            return task;
        }
    }
}
#endif
