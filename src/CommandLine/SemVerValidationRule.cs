using System;
using System.Collections.Generic;
using System.Globalization;

namespace NuGet {
    public class SemVerValidationRule : IPackageRule {
        public IEnumerable<PackageIssue> Validate(IPackage package) {
            SemanticVersion semVer;
            if (!SemanticVersion.TryParseStrict(package.Version.ToString(), out semVer)) {
                yield return new PackageIssue(NuGetResources.Warning_SemVerTitle, 
                    String.Format(CultureInfo.CurrentCulture, NuGetResources.Warning_SemVer, package.Version), 
                    NuGetResources.Warning_SemVerSolution);
            }
        }
    }
}
