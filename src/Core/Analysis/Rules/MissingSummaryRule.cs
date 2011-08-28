using System;
using System.Collections.Generic;

namespace NuGet.Analysis.Rules {

    internal class MissingSummaryRule : IPackageRule {
        const int ThresholdDescriptionLength = 300;

        public IEnumerable<PackageIssue> Validate(IPackage package) {
            if (package.Description.Length > ThresholdDescriptionLength && String.IsNullOrEmpty(package.Summary)) {
                yield return new PackageIssue(
                    "Consider providing Summary text",
                    "The Description text is long but the Summary text is empty. This means the Description text will be truncated in the 'Manage NuGet packages' dialog.",
                    "Provide a brief summary of the package in the Summary field.");
            }
        }
    }
}