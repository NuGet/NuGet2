using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NuGet.Commands;

namespace NuGet
{
    public class DefaultManifestValuesRule : IPackageRule
    {
        public IEnumerable<PackageIssue> Validate(IPackage package)
        {
            if (package.ProjectUrl != null && package.ProjectUrl.OriginalString.Equals(SpecCommand.SampleProjectUrl, StringComparison.Ordinal))
            {
                yield return CreateIssueFor("ProjectUrl", package.ProjectUrl.OriginalString);
            }
            if (package.LicenseUrl != null && package.LicenseUrl.OriginalString.Equals(SpecCommand.SampleLicenseUrl, StringComparison.Ordinal))
            {
                yield return CreateIssueFor("LicenseUrl", package.LicenseUrl.OriginalString);
            }
            if (package.IconUrl != null && package.IconUrl.OriginalString.Equals(SpecCommand.SampleIconUrl, StringComparison.Ordinal))
            {
                yield return CreateIssueFor("IconUrl", package.IconUrl.OriginalString);
            }
            if (!String.IsNullOrEmpty(package.Tags) && package.Tags.Trim().Equals(SpecCommand.SampleTags, StringComparison.Ordinal))
            {
                yield return CreateIssueFor("Tags", SpecCommand.SampleTags);
            }
            if (SpecCommand.SampleReleaseNotes.Equals(package.ReleaseNotes, StringComparison.Ordinal))
            {
                yield return CreateIssueFor("ReleaseNotes", SpecCommand.SampleReleaseNotes);
            }
            if (SpecCommand.SampleDescription.Equals(package.Description, StringComparison.Ordinal))
            {
                yield return CreateIssueFor("Description", SpecCommand.SampleDescription);
            }

            var dependency = package.Dependencies.FirstOrDefault();
            if (dependency != null && dependency.Id.Equals(SpecCommand.SampleManifestDependency.Id, StringComparison.Ordinal)
                                   && dependency.VersionSpec.ToString().Equals("[" + SpecCommand.SampleManifestDependency.Version + "]", StringComparison.Ordinal))
            {
                yield return CreateIssueFor("Dependency", dependency.ToString());
            }
        }

        private static PackageIssue CreateIssueFor(string field, string value)
        {
            return new PackageIssue(NuGetResources.Warning_DefaultSpecValueTitle,
                String.Format(CultureInfo.CurrentCulture, NuGetResources.Warning_DefaultSpecValue, value, field),
                NuGetResources.Warning_DefaultSpecValueSolution);
        }
    }
}
