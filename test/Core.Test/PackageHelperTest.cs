using System;
using Xunit;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    
    public class PackageHelperTest {
        [Fact]
        public void ResolveUnknownPackageThrows() {
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageHelper.ResolvePackage(new MockPackageRepository(), new MockPackageRepository(), "elmah", null), "Unable to find package 'elmah'.");
        }

        [Fact]
        public void ResolveSpecificVersionOfUnknownPackageThrows() {
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageHelper.ResolvePackage(new MockPackageRepository(), new MockPackageRepository(), "elmah", new Version("1.1")), "Unable to find version '1.1' of package 'elmah'.");
        }
    }
}
