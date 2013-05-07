using System;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class PackageHelperTest
    {
        [Fact]
        public void ResolveUnknownPackageThrows()
        {
            ExceptionAssert.Throws<InvalidOperationException>(
                () => PackageRepositoryHelper.ResolvePackage(new MockPackageRepository(), new MockPackageRepository(), "elmah", null, allowPrereleaseVersions: false),
                "Unable to find package 'elmah'.");
        }

        [Fact]
        public void ResolveSpecificVersionOfUnknownPackageThrows()
        {
            ExceptionAssert.Throws<InvalidOperationException>(
                () => PackageRepositoryHelper.ResolvePackage(new MockPackageRepository(), new MockPackageRepository(), "elmah", new SemanticVersion("1.1"), allowPrereleaseVersions: false),
                "Unable to find version '1.1' of package 'elmah'.");
        }
    }
}
