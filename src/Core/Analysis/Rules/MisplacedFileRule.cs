using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using NuGet.Resources;

namespace NuGet.Analysis.Rules
{
    internal class MisplacedFileRule : IPackageRule
    {
        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            foreach (IPackageFile file in package.GetFiles())
            {
                string path = file.Path;
                string directory = Path.GetDirectoryName(path);

                // if under 'lib' directly
                if (VersionUtility.WellKnownFolders.Any(folder => directory.Equals(folder, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return CreatePackageIssueForFile(path, directory);
                }
                else if (!directory.StartsWith(Constants.LibDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    // when checking for assemblies outside 'lib' folder, only check .dll files.
                    // .exe files are often legitimate outside 'lib'.
                    if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return CreatePackageIssueForAssembliesOutsideLib(path);
                    }
                }
            }
        }

        private static PackageIssue CreatePackageIssueForFile(string file, string directory)
        {
            return new PackageIssue(
                AnalysisResources.FileUnderWellKnownTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.FileUnderWellKnownFolderDescription, file, directory),
                AnalysisResources.FileUnderWellKnownSolution
            );
        }

        private static PackageIssue CreatePackageIssueForAssembliesOutsideLib(string target)
        {
            return new PackageIssue(
                AnalysisResources.AssemblyOutsideLibTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.AssemblyOutsideLibDescription, target),
                AnalysisResources.AssemblyOutsideLibSolution
            );
        }
    }
}