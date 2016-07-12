using NuGet.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Analysis.Rules
{
    internal class SpecialVersionLengthRule : IPackageRule
    {
        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            if (!ValidateSpecialVersionLength(package.Version))
            {
                yield return CreatePackageIssueForSpecialVersionLength(package.Version.ToString());
            }
        }

        private static bool ValidateSpecialVersionLength(SemanticVersion version)
        {
            return version == null || version.SpecialVersion == null || version.SpecialVersion.Length <= 20;
        }

        private static PackageIssue CreatePackageIssueForSpecialVersionLength(string version)
        {
            return new PackageIssue(
                AnalysisResources.SpecialVersionTooLongTitle,
                String.Format(CultureInfo.CurrentCulture, AnalysisResources.SpecialVersionTooLongDescription, version),
                AnalysisResources.SpecialVersionTooLongSolution
            );
        }
    }
}
