using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet.Analysis.Rules
{
    internal class InvalidFrameworkFolderRule : IPackageRule
    {
        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in package.GetFiles())
            {
                string path = file.Path;
                string[] parts = path.Split(Path.DirectorySeparatorChar);
                if (parts.Length >= 3 && parts[0].Equals(Constants.LibDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    set.Add(parts[1]);
                }
            }

            return set.Where(IsInvalidFrameworkName).Select(CreatePackageIssue);
        }

        private bool IsInvalidFrameworkName(string name)
        {
            return VersionUtility.ParseFrameworkName(name) == VersionUtility.UnsupportedFrameworkName;
        }

        private PackageIssue CreatePackageIssue(string target)
        {
            return new PackageIssue(
                AnalysisResources.InvalidFrameworkTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.InvalidFrameworkDescription, target),
                AnalysisResources.InvalidFrameworkSolution
            );
        }
    }
}