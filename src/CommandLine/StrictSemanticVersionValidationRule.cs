using System;
using System.Collections.Generic;
using System.Globalization;

namespace NuGet
{
    public class StrictSemanticVersionValidationRule : IPackageRule
    {
        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            SemanticVersion semVer;
            if (!SemanticVersion.TryParseStrict(package.Version.ToString(), out semVer))
            {
                yield return new PackageIssue(NuGetResources.Warning_SemanticVersionTitle,
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Warning_SemanticVersion, package.Version),
                    NuGetResources.Warning_SemanticVersionSolution);
            }
        }
    }
}
