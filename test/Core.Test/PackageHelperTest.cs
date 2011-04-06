using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class PackageHelperTest {
        [TestMethod]
        public void ResolveUnknownPackageThrows() {
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageHelper.ResolvePackage(new MockPackageRepository(), new MockPackageRepository(), "elmah", null), "Unable to find package 'elmah'.");
        }

        [TestMethod]
        public void ResolveSpecificVersionOfUnknownPackageThrows() {
            ExceptionAssert.Throws<InvalidOperationException>(() => PackageHelper.ResolvePackage(new MockPackageRepository(), new MockPackageRepository(), "elmah", new Version("1.1")), "Unable to find version '1.1' of package 'elmah'.");
        }
    }
}
